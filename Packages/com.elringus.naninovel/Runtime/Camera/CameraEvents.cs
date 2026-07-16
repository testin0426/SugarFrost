using UnityEngine;

namespace Naninovel.UI
{
    /// <summary>
    /// Routes essential <see cref="ICameraManager"/> APIs to <see cref="UnityEngine.Events.UnityEvent"/>.
    /// </summary>
    [AddComponentMenu("Naninovel/ Events/Camera Events")]
    public class CameraEvents : UnityEvents
    {
        [Space]
        [Tooltip("Occurs when availability of the camera manager engine service changes.")]
        public BoolUnityEvent ServiceAvailable;
        [Tooltip("Occurs when UI becomes visible (true) or invisible (false).")]
        public BoolUnityEvent RenderUIChanged;
        [Tooltip("Occurs when the camera offset changes.")]
        public Vector3UnityEvent OffsetChanged;
        [Tooltip("Occurs when the camera rotation changes.")]
        public QuaternionUnityEvent RotationChanged;
        [Tooltip("Occurs when the camera zoom level changes.")]
        public FloatUnityEvent ZoomChanged;

        #if URP_AVAILABLE
        public void SetupBaseCamera (Camera baseCamera)
        {
            if (!Engine.TryGetService<ICameraManager>(out var manager)) return;
            var baseData = UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(baseCamera);
            UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(manager.Camera).renderType =
                UnityEngine.Rendering.Universal.CameraRenderType.Overlay;
            baseData.cameraStack.Clear();
            baseData.cameraStack.Add(manager.Camera);
            if (manager.UICamera) baseData.cameraStack.Add(manager.UICamera);
        }
        #endif

        public void EnableCamera () => SetCameraEnabled(true);
        public void DisableCamera () => SetCameraEnabled(false);
        public void SetCameraEnabled (bool enabled)
        {
            if (Engine.TryGetService<ICameraManager>(out var camera))
                camera.Enabled = enabled;
        }

        public void RenderCameraUI (bool render)
        {
            if (Engine.TryGetService<ICameraManager>(out var camera))
                camera.RenderUI = render;
        }

        public void ChangeCameraOffsetX (float x)
        {
            if (Engine.TryGetService<ICameraManager>(out var camera))
                camera.ChangeOffset(new(x, camera.Offset.y, camera.Offset.z), new(camera.Configuration.DefaultDuration));
        }

        public void ChangeCameraOffsetY (float y)
        {
            if (Engine.TryGetService<ICameraManager>(out var camera))
                camera.ChangeOffset(new(camera.Offset.x, y, camera.Offset.z), new(camera.Configuration.DefaultDuration));
        }

        public void ChangeCameraOffsetZ (float z)
        {
            if (Engine.TryGetService<ICameraManager>(out var camera))
                camera.ChangeOffset(new(camera.Offset.x, camera.Offset.y, z), new(camera.Configuration.DefaultDuration));
        }

        public void RollCamera (float roll)
        {
            if (Engine.TryGetService<ICameraManager>(out var camera))
                camera.ChangeRotation(Quaternion.Euler(camera.Rotation.eulerAngles.x,
                    camera.Rotation.eulerAngles.y, roll), new(camera.Configuration.DefaultDuration));
        }

        public void ChangeCameraZoom (float zoom)
        {
            if (Engine.TryGetService<ICameraManager>(out var camera))
                camera.ChangeZoom(zoom, new(camera.Configuration.DefaultDuration));
        }

        protected override void HandleEngineInitialized ()
        {
            if (Engine.TryGetService<ICameraManager>(out var camera))
            {
                ServiceAvailable?.Invoke(true);

                camera.OnRenderUIChanged -= RenderUIChanged.SafeInvoke;
                camera.OnRenderUIChanged += RenderUIChanged.SafeInvoke;

                camera.OnOffsetChanged -= OffsetChanged.SafeInvoke;
                camera.OnOffsetChanged += OffsetChanged.SafeInvoke;

                camera.OnRotationChanged -= RotationChanged.SafeInvoke;
                camera.OnRotationChanged += RotationChanged.SafeInvoke;

                camera.OnZoomChanged -= ZoomChanged.SafeInvoke;
                camera.OnZoomChanged += ZoomChanged.SafeInvoke;
            }
            else ServiceAvailable?.Invoke(false);
        }

        protected override void HandleEngineDestroyed ()
        {
            ServiceAvailable?.Invoke(false);
        }
    }
}
