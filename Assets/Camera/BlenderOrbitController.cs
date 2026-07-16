using UnityEngine;

/// <summary>
/// Blender-style viewport navigation: hold middle mouse to orbit around a pivot point,
/// scroll to dolly in/out, and Shift+middle-mouse to pan. The pivot ("magical point") is
/// chosen by raycasting from the camera the moment you press the middle button, so the view
/// orbits whatever you're currently looking at (Blender's "Auto Depth" feel). Falls back to a
/// fixed distance in front of the camera when the ray hits nothing.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(10000)]
public class BlenderOrbitController : MonoBehaviour
{
    [Header("Orbit (hold Middle Mouse)")]
    [Tooltip("Degrees per pixel of mouse delta while orbiting.")]
    public float orbitSensitivity = 0.25f;
    public bool invertY = false;
    [Tooltip("Clamp how far the view can tilt up/down (degrees). Keep < 90 to avoid flipping over the poles.")]
    public float minPitch = -89f;
    public float maxPitch = 89f;
    [Tooltip("Smoothing time for orbit input. 0 = no smoothing.")]
    [Range(0f, 0.3f)] public float orbitSmoothTime = 0.03f;

    [Header("Pivot Raycast (Auto Depth)")]
    [Tooltip("Layers the pivot raycast can hit.")]
    public LayerMask raycastMask = ~0;
    [Tooltip("Max ray length when looking for a pivot.")]
    public float raycastDistance = 1000f;
    [Tooltip("Pivot distance used when the ray hits nothing.")]
    public float fallbackDistance = 10f;

    [Header("Dolly (Scroll)")]
    [Tooltip("Fraction of the current distance moved per scroll tick. Distance scales so zoom feels constant at any range.")]
    public float dollyStep = 0.15f;
    public float minDistance = 0.2f;
    public float maxDistance = 500f;

    [Header("Pan (Shift + Middle Mouse)")]
    public float panSensitivity = 1f;

    [Header("Cursor")]
    [Tooltip("Lock & hide the cursor while navigating so the mouse can't run off-screen.")]
    public bool lockCursorWhileActive = true;

    Vector3 _pivot;
    float _distance;
    float _yaw;
    float _pitch;
    Vector2 _smoothedOrbit;
    Vector2 _orbitVelocity;
    bool _navigating;
    bool _cursorLockedByUs;

    void Awake()
    {
        // Seed pivot/distance/angles from the current pose so the first orbit doesn't snap.
        _distance = fallbackDistance;
        _pivot = transform.position + transform.forward * _distance;
        DeriveAnglesFromTransform();
    }

    void LateUpdate()
    {
        bool mmb = Input.GetMouseButton(2);
        bool panning = mmb && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

        // Pressing MMB this frame begins navigation: pick the pivot under the view now.
        if (Input.GetMouseButtonDown(2))
        {
            BeginNavigation();
        }

        ApplyCursorState(mmb);

        // Cursor lock warps the pointer to screen center, injecting one bogus delta. Skip the
        // press frame so the view doesn't jump.
        bool justPressed = Input.GetMouseButtonDown(2);

        if (mmb && !justPressed)
        {
            float mx = Input.GetAxisRaw("Mouse X");
            float my = Input.GetAxisRaw("Mouse Y");

            if (panning)
            {
                Pan(mx, my);
                _smoothedOrbit = Vector2.zero;
                _orbitVelocity = Vector2.zero;
            }
            else
            {
                Orbit(mx, my);
            }
        }
        else
        {
            _smoothedOrbit = Vector2.zero;
            _orbitVelocity = Vector2.zero;
            if (!mmb) _navigating = false;
        }

        // Scroll dollies in/out at any time (Blender zooms even without MMB held).
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            // If we're not mid-navigation, refresh the pivot so the zoom targets what we see.
            if (!_navigating) RefreshPivotFromTransform();
            _distance = Mathf.Clamp(_distance * (1f - scroll * dollyStep * 10f), minDistance, maxDistance);
        }

        ApplyTransform();
    }

    void BeginNavigation()
    {
        _navigating = true;

        Vector3 origin = transform.position;
        Vector3 dir = transform.forward;
        if (Physics.Raycast(origin, dir, out RaycastHit hit, raycastDistance, raycastMask, QueryTriggerInteraction.Ignore))
            _pivot = hit.point;
        else
            _pivot = origin + dir * fallbackDistance;

        _distance = Mathf.Clamp((origin - _pivot).magnitude, minDistance, maxDistance);
        DeriveAnglesFromTransform();
    }

    void Orbit(float mx, float my)
    {
        Vector2 raw = new Vector2(mx, my) * orbitSensitivity;
        if (orbitSmoothTime > 0f)
            _smoothedOrbit = Vector2.SmoothDamp(_smoothedOrbit, raw, ref _orbitVelocity, orbitSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
        else
            _smoothedOrbit = raw;

        _yaw += _smoothedOrbit.x;
        _pitch += (invertY ? _smoothedOrbit.y : -_smoothedOrbit.y);
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
    }

    void Pan(float mx, float my)
    {
        // Move the pivot (and thus the camera with it) along the camera's right/up axes.
        // Scaling by distance keeps the pan speed feeling constant at any zoom level.
        float scale = panSensitivity * _distance * 0.001f;
        Vector3 move = (-transform.right * mx + -transform.up * my) * scale;
        _pivot += move;
    }

    void ApplyTransform()
    {
        Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
        transform.rotation = rot;
        transform.position = _pivot - rot * Vector3.forward * _distance;
    }

    // Decompose the current forward into roll-free yaw/pitch so orbiting continues smoothly
    // from wherever the camera is pointing.
    void DeriveAnglesFromTransform()
    {
        Vector3 f = transform.forward;
        _pitch = Mathf.Clamp(-Mathf.Asin(Mathf.Clamp(f.y, -1f, 1f)) * Mathf.Rad2Deg, minPitch, maxPitch);
        _yaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
    }

    void RefreshPivotFromTransform()
    {
        Vector3 origin = transform.position;
        Vector3 dir = transform.forward;
        if (Physics.Raycast(origin, dir, out RaycastHit hit, raycastDistance, raycastMask, QueryTriggerInteraction.Ignore))
            _pivot = hit.point;
        else
            _pivot = origin + dir * Mathf.Clamp(_distance, minDistance, maxDistance);
        _distance = Mathf.Clamp((origin - _pivot).magnitude, minDistance, maxDistance);
        DeriveAnglesFromTransform();
    }

    void ApplyCursorState(bool active)
    {
        if (!lockCursorWhileActive) return;
        if (active == _cursorLockedByUs) return;
        if (active)
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
        _navigating = false;
    }

    /// <summary>Frame an explicit point: orbit it from the current direction at the current distance.</summary>
    public void FocusOn(Vector3 worldPoint)
    {
        _pivot = worldPoint;
        DeriveAnglesFromTransform();
        ApplyTransform();
    }
}
