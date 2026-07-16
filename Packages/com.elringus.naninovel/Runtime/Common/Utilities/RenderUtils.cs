using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Optional camera blit configuration.
    /// </summary>
    public struct CameraBlitOptions
    {
        /// <summary>
        /// Whether to force-render the camera stack before copying (URP-only).
        /// Required to include UI overlay cameras onto the blit result.
        /// </summary>
        public bool RenderStack { get; set; }
        /// <summary>
        /// Delegate to invoke just before executing the copy operation.
        /// Useful because the actual blit happens with a delay, as it waits for the end of the frame.
        /// </summary>
        [CanBeNull] public Action OnBlit { get; set; }
    }

    public static class RenderUtils
    {
        /// <summary>
        /// Ensures <see cref="Camera.current"/> is a valid game camera and not editor's scene view camera.
        /// </summary>
        /// <remarks>
        /// This is a workaround for a Unity quirk, when scene view camera "leaks"
        /// into the <see cref="Camera.current"/> breaking manual rendering with <see cref="GL"/>,
        /// specifically when the scene view is set to 3D mode.
        /// </remarks>
        public static void EnsureGLTarget ()
        {
            var cam = Engine.GetServiceOrErr<ICameraManager>().Camera;
            if (Camera.current != cam) Camera.SetupCurrent(cam);
        }

        /// <summary>
        /// Rents a temporary render texture configured to be a target of the specified camera.
        /// Dispose the returned object to release the texture.
        /// </summary>
        public static IDisposable RentCameraRT (Camera cam, out RenderTexture tex)
        {
            var fmt = cam.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
            var depth = cam.depthTextureMode != DepthTextureMode.None ? 24 : 0;
            var space = QualitySettings.activeColorSpace == ColorSpace.Linear
                ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.Default;
            tex = RenderTexture.GetTemporary(cam.scaledPixelWidth, cam.scaledPixelHeight, depth, fmt, space);
            return Defer.With(tex, RenderTexture.ReleaseTemporary);
        }

        /// <summary>
        /// Waits until specified camera finishes current frame and copies the content into the specified texture.
        /// </summary>
        /// <remarks>
        /// In URP, in case the specified camera is not base, it'll be replaced with the current base one,
        /// as overlay cameras can't be rendered.
        /// </remarks>
        public static async Awaitable BlitCamera (Camera src, RenderTexture dst, CameraBlitOptions opt = default)
        {
            await Async.EndOfFrame();
            if (!src || !dst) return;
            opt.OnBlit?.Invoke();
            #if URP_AVAILABLE
            var data = UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(src);
            if (data.renderType != UnityEngine.Rendering.Universal.CameraRenderType.Base)
                src = FindBaseCamera(out data);
            if (opt.RenderStack && data?.cameraStack != null)
                foreach (var camera in data.cameraStack)
                    camera.Render();
            var req = new UnityEngine.Rendering.RenderPipeline.StandardRequest { destination = dst };
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(src, req);
            #else
            var initialRenderTexture = src.targetTexture;
            src.targetTexture = dst;
            src.Render();
            src.targetTexture = initialRenderTexture;
            #endif
        }

        /// <summary>
        /// Performs <see cref="BlitCamera"/> each frame, until the returned object is disposed.
        /// </summary>
        public static IDisposable StartBlitCamera (Camera src, RenderTexture dst, CameraBlitOptions opt = default)
        {
            void Blit () => BlitCamera(src, dst, opt).Forget();
            Engine.Behaviour.OnUpdate += Blit;
            return new Defer(() => {
                if (Engine.Behaviour != null)
                    Engine.Behaviour.OnUpdate -= Blit;
            });
        }

        #if URP_AVAILABLE
        /// <summary>
        /// Finds the first active base URP camera or null when not found.
        /// </summary>
        [CanBeNull] public static Camera FindBaseCamera (
            [CanBeNull] out UnityEngine.Rendering.Universal.UniversalAdditionalCameraData data,
            [CanBeNull] Predicate<Camera> filter = null)
        {
            using var _ = ArrayPool<Camera>.Rent(Camera.allCamerasCount, out var cameras);
            Camera.GetAllCameras(cameras);
            for (int i = 0; i < Camera.allCamerasCount; i++)
                if (cameras[i].cameraType == CameraType.Game && (filter == null || filter(cameras[i])) &&
                    cameras[i].isActiveAndEnabled && cameras[i].gameObject.activeInHierarchy &&
                    UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(cameras[i]) is
                        { renderType: UnityEngine.Rendering.Universal.CameraRenderType.Base } d)
                {
                    data = d;
                    return cameras[i];
                }
            data = null;
            return null;
        }
        #endif
    }
}
