using Naninovel;
using UnityEngine;

[DisallowMultipleComponent]
public class MouseLookController : MonoBehaviour
{
    [Header("Bones")]
    [Tooltip("Eye bones — will aim at the mouse cursor in world space.")]
    [SerializeField] private Transform eyeBoneL;
    [SerializeField] private Transform eyeBoneR;
    [Tooltip("Armature transform — yaws on its Y axis based on mouse X.")]
    [SerializeField] private Transform armature;

    [Header("Armature Y range (degrees)")]
    [SerializeField] private float armatureYMin = 180f;
    [SerializeField] private float armatureYMax = 200f;

    [Header("Eye limits (degrees from rest)")]
    [SerializeField] private float eyeYawLimit = 8f;
    [SerializeField] private float eyePitchLimit = 2f;

    [Header("Weights")]
    [Range(0f, 1f)] [SerializeField] private float eyeWeight = 1f;
    [Range(0f, 1f)] [SerializeField] private float armatureWeight = 1f;

    [Header("Smoothing")]
    [SerializeField] private float followSpeed = 8f;

    [Header("Look zone (relative to character)")]
    [Tooltip("Half-width of the active look zone, in fraction of screen width. " +
             "Mouse X within ±this distance from the character's screen X drives the look; " +
             "beyond it, the look clamps to the corresponding extreme.")]
    [Range(0.05f, 1f)] [SerializeField] private float lookZoneHalfWidth = 0.35f;

    private IScriptPlayer player;
    private IUIManager ui;
    private ICameraManager cameras;
    private float currentArmatureY;
    private Quaternion eyeLRest, eyeRRest;
    private bool eyeRestCached;

    private void OnEnable ()
    {
        if (Engine.Initialized) Bind();
        else Engine.OnInitializationFinished += Bind;
    }

    private void OnDisable ()
    {
        Engine.OnInitializationFinished -= Bind;
    }

    private void Bind ()
    {
        player = Engine.GetServiceOrErr<IScriptPlayer>();
        ui = Engine.GetServiceOrErr<IUIManager>();
        cameras = Engine.GetServiceOrErr<ICameraManager>();
        currentArmatureY = Mathf.Lerp(armatureYMin, armatureYMax, 0.5f);
    }

    // Mouse X mapped to [-1, +1] over a window centered on the character's
    // current screen X. Beyond the window it clamps. This makes the look
    // "follow the mouse only when it's near the character" — when the actor
    // is on the right and the mouse is on the far left, the result stays at -1
    // (fully facing left) rather than tracking small left/right wiggles there.
    private float CharacterRelativeMouseX ()
    {
        var anchor = transform.parent ? transform.parent : transform;
        var cam = cameras != null ? cameras.Camera : Camera.main;
        if (!cam) return Mathf.Clamp(Input.mousePosition.x / Mathf.Max(1f, Screen.width) * 2f - 1f, -1f, 1f);

        var anchorScreenX = cam.WorldToScreenPoint(anchor.position).x;
        var halfPx = Mathf.Max(1f, lookZoneHalfWidth * Screen.width);
        return Mathf.Clamp((Input.mousePosition.x - anchorScreenX) / halfPx, -1f, 1f);
    }

    private bool InNovelMode ()
    {
        return player != null && player.Playing && !(ui != null && ui.AnyModal);
    }

    private void CacheEyeRest ()
    {
        if (eyeRestCached) return;
        if (eyeBoneL) eyeLRest = eyeBoneL.localRotation;
        if (eyeBoneR) eyeRRest = eyeBoneR.localRotation;
        eyeRestCached = true;
    }

    [Header("Eye direction flips")]
    [Tooltip("Flip if eyes look the wrong horizontal way.")]
    [SerializeField] private bool invertEyeYaw;
    [Tooltip("Flip if eyes look the wrong vertical way.")]
    [SerializeField] private bool invertEyePitch;

    private Quaternion currentEyeL, currentEyeR;

    private void LateUpdate ()
    {
        if (!InNovelMode()) return;
        CacheEyeRest();

        var t = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);

        // Mouse X is measured relative to the character's screen position so the
        // actor only "looks at" the cursor when it's near them; far away on the
        // wrong side, the look saturates at the corresponding extreme.
        var relX = CharacterRelativeMouseX();

        // --- Armature yaw on Y, driven by relative mouse X, smoothstepped ---
        if (armature && armatureWeight > 0f)
        {
            var nx = (relX + 1f) * 0.5f; // [-1,1] -> [0,1]
            var sx = nx * nx * (3f - 2f * nx);
            var targetY = Mathf.Lerp(armatureYMin, armatureYMax, sx);
            currentArmatureY = Mathf.Lerp(currentArmatureY, targetY, t);

            var e = armature.localEulerAngles;
            e.y += Mathf.DeltaAngle(e.y, currentArmatureY) * armatureWeight;
            armature.localEulerAngles = e;
        }

        // --- Eyes: build rotation in the SHARED parent (head) space, then convert into each
        //     eye's local space. This sidesteps mirrored eye-bone axes (the cause of the
        //     "one eye goes up while the other goes down" bug). ---
        if (eyeWeight > 0f && (eyeBoneL || eyeBoneR))
        {
            // X is character-relative (clamped at the look-zone edges); Y stays screen-based.
            var nx = Smooth(relX);
            var ny = Smooth(Mathf.Clamp(Input.mousePosition.y / Mathf.Max(1f, Screen.height) * 2f - 1f, -1f, 1f));

            var yaw = nx * eyeYawLimit * (invertEyeYaw ? -1f : 1f);
            var pitch = -ny * eyePitchLimit * (invertEyePitch ? -1f : 1f); // mouse up = look up

            // Rotation expressed in the head/parent's local axes (same for both eyes).
            // Yaw around parent's UP, pitch around parent's RIGHT.
            var parentSpaceOffset = Quaternion.AngleAxis(yaw, Vector3.up) *
                                    Quaternion.AngleAxis(pitch, Vector3.right);

            var targetL = ComputeEyeTarget(eyeBoneL, eyeLRest, parentSpaceOffset);
            var targetR = ComputeEyeTarget(eyeBoneR, eyeRRest, parentSpaceOffset);

            if (currentEyeL == default) currentEyeL = eyeBoneL ? eyeBoneL.localRotation : Quaternion.identity;
            if (currentEyeR == default) currentEyeR = eyeBoneR ? eyeBoneR.localRotation : Quaternion.identity;

            currentEyeL = Quaternion.Slerp(currentEyeL, targetL, t);
            currentEyeR = Quaternion.Slerp(currentEyeR, targetR, t);

            if (eyeBoneL) eyeBoneL.localRotation = Quaternion.Slerp(eyeBoneL.localRotation, currentEyeL, eyeWeight);
            if (eyeBoneR) eyeBoneR.localRotation = Quaternion.Slerp(eyeBoneR.localRotation, currentEyeR, eyeWeight);
        }
    }

    // Given a parent-space offset rotation, return the eye's local rotation that produces it
    // when composed with the eye's rest pose. Math: parent_world = parent.rot * eye.localRest * X
    // where X is what we want to apply, but expressed in PARENT space.
    // Equivalently: localRotation = rest * (rest^-1 * offset * rest)
    // — i.e. we conjugate the parent-space offset into the eye's local frame.
    private static Quaternion ComputeEyeTarget (Transform eye, Quaternion rest, Quaternion parentSpaceOffset)
    {
        if (!eye) return Quaternion.identity;
        var localOffset = Quaternion.Inverse(rest) * parentSpaceOffset * rest;
        return rest * localOffset;
    }

    // signed smoothstep on [-1, 1] -> [-1, 1]
    private static float Smooth (float v)
    {
        var s = Mathf.Abs(v);
        s = s * s * (3f - 2f * s);
        return Mathf.Sign(v) * s;
    }
}
