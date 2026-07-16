using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(10000)]
public class MouseCameraController : MonoBehaviour
{
    public enum ActivationMode
    {
        Always,
        HoldRightMouse,
        HoldLeftMouse,
        HoldMiddleMouse
    }

    [Header("Activation")]
    public ActivationMode activation = ActivationMode.HoldRightMouse;
    [Tooltip("Lock and hide the cursor while the controller is active.")]
    public bool lockCursorWhileActive = true;

    [Header("Look (Yaw / Pitch)")]
    [Tooltip("Degrees per pixel of mouse delta.")]
    public float mouseSensitivity = 0.15f;
    public bool invertY = false;
    [Tooltip("Smoothing time for look input. 0 = no smoothing.")]
    [Range(0f, 0.5f)] public float lookSmoothTime = 0.03f;

    [Header("Look Limits (degrees, offset from the original rotation)")]
    [Tooltip("How far the view may turn left/right of where the camera was authored.")]
    public float minYaw = -45f;
    public float maxYaw = 45f;
    [Tooltip("How far the view may tilt up/down from where the camera was authored.")]
    public float minPitch = -30f;
    public float maxPitch = 30f;

    [Header("Field of View (Scroll)")]
    public bool controlFov = true;
    [Tooltip("FOV change per scroll tick.")]
    public float fovZoomStep = 4f;
    public float minFov = 20f;
    public float maxFov = 90f;
    [Tooltip("Smoothing for FOV. 0 = instant.")]
    [Range(0f, 0.5f)] public float fovSmoothTime = 0.08f;

    Camera _cam;
    Quaternion _baseRot;   // the camera's authored rotation; (yaw,pitch) are offsets from this
    float _yaw;
    float _pitch;
    float _targetFov;
    float _fovVelocity;
    Vector2 _smoothedLook;
    Vector2 _lookVelocity;
    bool _cursorLockedByUs;
    bool _wasActive;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        if (_cam == null) _cam = Camera.main;

        // Capture the authored orientation. With zero look offset, ApplyTransform reproduces
        // it exactly — the camera is never moved or rotated unless the user looks around.
        _baseRot = transform.rotation;
        _yaw = 0f;
        _pitch = 0f;

        if (_cam != null) _targetFov = _cam.fieldOfView;
    }

    void LateUpdate()
    {
        bool active = IsActive();
        ApplyCursorState(active);

        // Locking the cursor warps it to screen center, which injects a large bogus mouse
        // delta on the activation frame. Skip look input that frame to avoid a sudden twist.
        bool justActivated = active && !_wasActive;
        _wasActive = active;

        if (active && !justActivated)
        {
            float mx = Input.GetAxisRaw("Mouse X");
            float my = Input.GetAxisRaw("Mouse Y");
            Vector2 raw = new Vector2(mx, my) * mouseSensitivity;

            if (lookSmoothTime > 0f)
                _smoothedLook = Vector2.SmoothDamp(_smoothedLook, raw, ref _lookVelocity, lookSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
            else
                _smoothedLook = raw;

            _yaw += _smoothedLook.x;
            _pitch += (invertY ? _smoothedLook.y : -_smoothedLook.y);
        }
        else
        {
            _smoothedLook = Vector2.zero;
            _lookVelocity = Vector2.zero;
        }

        _yaw = Mathf.Clamp(_yaw, minYaw, maxYaw);
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

        if (_cam != null && controlFov)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.0001f)
            {
                _targetFov = Mathf.Clamp(_targetFov - scroll * fovZoomStep * 10f, minFov, maxFov);
            }
            if (fovSmoothTime > 0f)
                _cam.fieldOfView = Mathf.SmoothDamp(_cam.fieldOfView, _targetFov, ref _fovVelocity, fovSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
            else
                _cam.fieldOfView = _targetFov;
        }

        ApplyTransform();
    }

    void ApplyTransform()
    {
        // Position is never touched. Yaw/pitch are small offsets applied on top of the
        // authored rotation, so the camera only ever turns slightly around where it started.
        transform.rotation = _baseRot * Quaternion.Euler(_pitch, _yaw, 0f);
    }

    bool IsActive()
    {
        switch (activation)
        {
            case ActivationMode.Always: return true;
            case ActivationMode.HoldLeftMouse: return Input.GetMouseButton(0);
            case ActivationMode.HoldRightMouse: return Input.GetMouseButton(1);
            case ActivationMode.HoldMiddleMouse: return Input.GetMouseButton(2);
            default: return false;
        }
    }

    void ApplyCursorState(bool active)
    {
        if (!lockCursorWhileActive) return;
        bool shouldLock = active;
        if (shouldLock == _cursorLockedByUs) return;
        if (shouldLock)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            _cursorLockedByUs = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _cursorLockedByUs = false;
        }
    }

    void OnDisable()
    {
        if (_cursorLockedByUs)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _cursorLockedByUs = false;
        }
    }

    // Re-center the view on the authored orientation.
    public void ResetView()
    {
        _yaw = 0f;
        _pitch = 0f;
        ApplyTransform();
    }
}
