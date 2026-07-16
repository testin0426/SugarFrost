using UnityEngine;
using Naninovel.Commands;

/// <summary>
/// Bridge between Naninovel's <c>@char</c> command and a character's armature.
///
/// Attach to a character prefab (the same object that carries the
/// <c>GenericCharacterBehaviour</c>, or any parent of the skeleton). The command
/// <c>@char &lt;id&gt; armature:x,y,z</c> rotates ONLY the referenced bone — not the whole
/// prefab / render-texture rig — and <c>@char &lt;id&gt; mouseFollow:false</c> toggles the
/// cursor-follow behaviour (on by default).
///
/// The rotation is applied additively on top of the CURRENT animation pose: every
/// <see cref="LateUpdate"/> (after the Animator has written the frame) the bone's live
/// local rotation is read and our offset is post-multiplied onto it. A guard detects when
/// the Animator did not touch the bone this frame and falls back to the rest pose, so a
/// non-animated bone never accumulates rotation.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(20000)] // after the Animator's pose write and other LateUpdates
public class CharacterArmatureBridge : MonoBehaviour, IArmatureBridge
{
    [Header("Target bone")]
    [Tooltip("Bone rotated on top of the current animation pose. If empty, the first child " +
             "named by 'Armature Bone Name' is used. Point this at whichever bone should turn " +
             "(e.g. the Armature root, a spine, or the neck bone).")]
    [SerializeField] private Transform armature;
    [Tooltip("Fallback bone name searched under this object when 'Armature' is not assigned.")]
    [SerializeField] private string armatureBoneName = "Armature";

    [Header("Mouse Follow (armature turns toward the cursor)")]
    [Tooltip("When enabled, the bone rotates toward the mouse cursor. Enabled by default; " +
             "toggle at runtime via '@char <id> mouseFollow:false'.")]
    [SerializeField] private bool mouseFollow = true;
    [Tooltip("Max left/right turn (degrees) when the cursor is at the screen edge.")]
    [SerializeField] private float maxYaw = 30f;
    [Tooltip("Max up/down tilt (degrees) when the cursor is at the screen edge.")]
    [SerializeField] private float maxPitch = 18f;
    [Tooltip("Smoothing time for the follow motion. 0 = instant.")]
    [Range(0f, 0.5f)] [SerializeField] private float followSmoothTime = 0.08f;
    [Tooltip("Invert the vertical follow direction.")]
    [SerializeField] private bool invertY = false;

    // Rest pose captured on Awake and the last rotation we wrote (used to tell whether the
    // Animator overwrote the bone this frame).
    private Quaternion initialLocal = Quaternion.identity;
    private Quaternion lastApplied = Quaternion.identity;

    // Scripted rotation (set by @char armature:) lerped over the command's duration.
    private Vector3 scriptedCurrent;
    private Vector3 scriptedFrom;
    private Vector3 scriptedTarget;
    private float scriptedDuration;
    private float scriptedElapsed;

    // Smoothed mouse-follow offset, stored as (yaw, pitch) in degrees.
    private Vector2 follow;
    private Vector2 followVelocity;

    public Vector3 ScriptedRotation => scriptedTarget;

    private void Awake ()
    {
        if (armature == null) armature = FindBone(transform, armatureBoneName);
        if (armature == null)
        {
            Debug.LogWarning($"[CharacterArmatureBridge] No bone named '{armatureBoneName}' found under '{name}'; " +
                             "assign the 'Armature' field. Armature rotation will be ignored.", this);
            enabled = false;
            return;
        }
        initialLocal = armature.localRotation;
        lastApplied = initialLocal;
    }

    private static Transform FindBone (Transform root, string boneName)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == boneName) return t;
        return null;
    }

    // --- IArmatureBridge (driven by @char) ---

    public void SetMouseFollow (bool enabled) => mouseFollow = enabled;

    public void SetScriptedRotation (Vector3 euler, float duration)
    {
        scriptedFrom = scriptedCurrent;
        scriptedTarget = euler;
        scriptedDuration = Mathf.Max(0f, duration);
        scriptedElapsed = 0f;
        if (scriptedDuration <= 0f) scriptedCurrent = euler;
    }

    private void LateUpdate ()
    {
        if (armature == null) return;

        AdvanceScriptedRotation();
        UpdateMouseFollow();

        var offsetEuler = new Vector3(
            follow.y + scriptedCurrent.x,
            follow.x + scriptedCurrent.y,
            scriptedCurrent.z);
        var offset = Quaternion.Euler(offsetEuler);

        // Base the offset on the current animation pose. If the Animator wrote the bone this
        // frame, its live value differs from what we last applied; otherwise fall back to the
        // rest pose so a non-animated bone doesn't accumulate rotation frame over frame.
        var current = armature.localRotation;
        var animationBase = Quaternion.Angle(current, lastApplied) < 0.01f ? initialLocal : current;

        var result = animationBase * offset;
        armature.localRotation = result;
        lastApplied = result;
    }

    private void AdvanceScriptedRotation ()
    {
        if (scriptedDuration > 0f && scriptedElapsed < scriptedDuration)
        {
            scriptedElapsed += Time.deltaTime;
            var k = Mathf.Clamp01(scriptedElapsed / scriptedDuration);
            k = k * k * (3f - 2f * k); // smoothstep
            scriptedCurrent = Vector3.LerpUnclamped(scriptedFrom, scriptedTarget, k);
        }
        else scriptedCurrent = scriptedTarget;
    }

    private void UpdateMouseFollow ()
    {
        var target = Vector2.zero;
        if (mouseFollow)
        {
            var mouse = Input.mousePosition;
            var nx = Mathf.Clamp(mouse.x / Mathf.Max(1, Screen.width) * 2f - 1f, -1f, 1f);
            var ny = Mathf.Clamp(mouse.y / Mathf.Max(1, Screen.height) * 2f - 1f, -1f, 1f);
            target = new Vector2(nx * maxYaw, (invertY ? ny : -ny) * maxPitch);
        }

        if (followSmoothTime > 0f)
            follow = Vector2.SmoothDamp(follow, target, ref followVelocity, followSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
        else follow = target;
    }
}