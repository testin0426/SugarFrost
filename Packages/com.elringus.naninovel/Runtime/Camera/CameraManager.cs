using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Naninovel.UI;
using UnityEngine;

namespace Naninovel
{
    /// <inheritdoc cref="ICameraManager"/>
    /// <remarks>Initialization order lowered, so the user could see something while waiting for the engine initialization.</remarks>
    [InitializeAtRuntime(-1)]
    public class CameraManager : ICameraManager, IStatefulService<GameStateMap>, IStatefulService<SettingsStateMap>
    {
        [Serializable]
        public class Settings
        {
            public int QualityLevel = -1;
        }

        [Serializable]
        public class GameState
        {
            public Vector3 Offset = Vector3.zero;
            public Quaternion Rotation = Quaternion.identity;
            public float Zoom;
            public bool Orthographic = true;
            public CameraLookState LookMode;
            public CameraComponentState[] CameraComponents;
            public bool RenderUI = true;
        }

        public event Action<bool> OnRenderUIChanged;
        public event Action<Vector3> OnOffsetChanged;
        public event Action<Quaternion> OnRotationChanged;
        public event Action<float> OnZoomChanged;

        public virtual CameraConfiguration Configuration { get; }
        public virtual bool Enabled { get => enabled; set => SetEnabled(value); }
        public virtual Camera Camera { get; protected set; }
        public virtual Camera UICamera { get; protected set; }
        public virtual bool RenderUI { get => GetRenderUI(); set => SetRenderUI(value); }
        public virtual Vector3 Offset { get => offset; set => SetOffset(value); }
        public virtual Quaternion Rotation { get => rotation; set => SetRotation(value); }
        public virtual float Zoom { get => zoom; set => SetZoom(value); }
        public virtual float OrthographicSize { get; private set; }
        public virtual float FOV { get; private set; }
        public virtual bool Orthographic { get => Camera.orthographic; set => SetOrthographic(value); }
        public virtual int QualityLevel { get => QualitySettings.GetQualityLevel(); set => QualitySettings.SetQualityLevel(value, true); }

        protected virtual CameraLookController LookController { get; private set; }
        protected virtual GameObject GameObject { get; private set; }
        protected virtual Transform LookContainer { get; private set; }
        protected virtual int UILayer { get; private set; }
        protected virtual IReadOnlyCollection<CameraComponentState> InitialComponentState { get; private set; }

        private readonly IInputManager input;
        private readonly IEngineBehaviour engine;
        [CanBeNull] private readonly RenderTexture thumbnailRT;
        private readonly List<MonoBehaviour> cameraComponentsCache = new();
        private readonly Tweener<VectorTween> offsetTweener = new();
        private readonly Tweener<VectorTween> rotationTweener = new();
        private readonly Tweener<FloatTween> zoomTweener = new();
        private Vector3 offset = Vector3.zero;
        private Quaternion rotation = Quaternion.identity;
        private float zoom;
        private bool enabled = true;

        public CameraManager (CameraConfiguration cfg, IInputManager input, IEngineBehaviour engine)
        {
            Configuration = cfg;
            this.input = input;
            this.engine = engine;

            thumbnailRT = cfg.CaptureThumbnails
                ? new(cfg.ThumbnailResolution.x, cfg.ThumbnailResolution.y, 24) : null;
        }

        public virtual async Awaitable InitializeService ()
        {
            UILayer = Engine.GetConfiguration<UIConfiguration>().ObjectsLayer;
            GameObject = Engine.CreateObject(new() { Name = nameof(CameraManager) });
            LookContainer = Engine.CreateObject(new() { Name = "MainCameraLookContainer", Parent = GameObject.transform }).transform;
            LookContainer.position = Configuration.InitialPosition;
            Camera = await InitializeMainCamera(LookContainer, UILayer);
            InitialComponentState = GetComponentState(Camera);
            OrthographicSize = Camera.orthographicSize;
            FOV = Camera.fieldOfView;
            if (Configuration.UseUICamera)
                UICamera = await InitializeUICamera(GameObject.transform, UILayer);
            LookController = new(Camera.transform, input.GetCameraLook());
            engine.OnUpdate += LookController.Update;
            SetupCamerasForURP();
            if (Configuration.DisableRendering) Enabled = false;
        }

        public virtual void ResetService ()
        {
            LookController.Enabled = false;
            Offset = Vector3.zero;
            Rotation = Quaternion.identity;
            Zoom = 0f;
            Orthographic = !Configuration.CustomCameraPrefab || Configuration.CustomCameraPrefab.orthographic;
            ApplyComponentState(Camera, InitialComponentState);
        }

        public virtual void DestroyService ()
        {
            if (engine != null)
                engine.OnUpdate -= LookController.Update;

            ObjectUtils.DestroyOrImmediate(thumbnailRT);
            ObjectUtils.DestroyOrImmediate(GameObject);
        }

        public virtual void SaveServiceState (SettingsStateMap stateMap)
        {
            var settings = new Settings {
                QualityLevel = QualityLevel
            };
            stateMap.SetState(settings);
        }

        public virtual Awaitable LoadServiceState (SettingsStateMap stateMap)
        {
            var settings = stateMap.GetState<Settings>() ?? new Settings();
            if (settings.QualityLevel >= 0 && settings.QualityLevel != QualityLevel)
                QualityLevel = settings.QualityLevel;

            return Async.Completed;
        }

        public virtual void SaveServiceState (GameStateMap stateMap)
        {
            var gameState = new GameState {
                Offset = Offset,
                Rotation = Rotation,
                Zoom = Zoom,
                Orthographic = Orthographic,
                LookMode = LookController.GetState(),
                RenderUI = RenderUI,
                CameraComponents = GetComponentState(Camera)
            };
            stateMap.SetState(gameState);
        }

        public virtual Awaitable LoadServiceState (GameStateMap stateMap)
        {
            var state = stateMap.GetState<GameState>();
            if (state is null)
            {
                ResetService();
                return Async.Completed;
            }

            Offset = state.Offset;
            Rotation = state.Rotation;
            Zoom = state.Zoom;
            Orthographic = state.Orthographic;
            RenderUI = state.RenderUI;
            SetLookMode(state.LookMode.Enabled, state.LookMode.Zone, state.LookMode.Speed, state.LookMode.Gravity);
            ApplyComponentState(Camera, state.CameraComponents);
            return Async.Completed;
        }

        public virtual void SetLookMode (bool enabled, Vector2 lookZone, Vector2 lookSpeed, bool gravity)
        {
            LookController.LookZone = lookZone;
            LookController.LookSpeed = lookSpeed;
            LookController.Gravity = gravity;
            LookController.Enabled = enabled;
        }

        public virtual async Awaitable<Texture2D> CaptureThumbnail ()
        {
            if (!Configuration.CaptureThumbnails) return null;

            var wasRenderingUI = RenderUI;
            if (Configuration.HideUIInThumbnails && RenderUI) RenderUI = false;

            #if !URP_AVAILABLE // In URP UI camera is inside the base camera's stack and not rendered separately.
            await RenderUtils.BlitCamera(Camera, thumbnailRT);
            if (RenderUI)
                #endif
            {
                using var _ = ListPool<Canvas>.Rent(out var disabledCanvases);
                await RenderUtils.BlitCamera(UICamera, thumbnailRT, new() {
                    RenderStack = true,
                    OnBlit = () => {
                        using var _ = Engine.GetServiceOrErr<IUIManager>().RentUIs(out var uis);
                        foreach (var ui in uis)
                        {
                            if (ui is not CustomUI { HideInThumbnail: true, TopmostCanvas: { enabled: true } canvas })
                                continue;
                            canvas.enabled = false;
                            disabledCanvases.Add(canvas);
                        }
                    }
                });
                foreach (var canvas in disabledCanvases)
                    canvas.enabled = true;
            }

            if (RenderUI != wasRenderingUI) RenderUI = wasRenderingUI;
            return thumbnailRT.ToTexture2D();
        }

        public virtual Awaitable ChangeOffset (Vector3 offset, Tween tween, AsyncToken token = default)
        {
            CompleteOffsetTween();

            if (tween.Instant)
            {
                Offset = offset;
                return Async.Completed;
            }

            this.offset = offset;
            var tw = new VectorTween(GetCameraOffset(), offset, tween, SetCameraOffset);
            return offsetTweener.Run(tw, token, Camera);
        }

        public virtual Awaitable ChangeRotation (Quaternion rotation, Tween tween, AsyncToken token = default)
        {
            CompleteRotationTween();

            if (tween.Instant)
            {
                Rotation = rotation;
                return Async.Completed;
            }

            this.rotation = rotation;
            var tw = new VectorTween(GetCameraRotation().ClampedEulerAngles(),
                rotation.ClampedEulerAngles(), tween, SetCameraRotation);
            return rotationTweener.Run(tw, token, Camera);
        }

        public virtual Awaitable ChangeZoom (float zoom, Tween tween, AsyncToken token = default)
        {
            CompleteZoomTween();

            if (tween.Instant)
            {
                Zoom = zoom;
                return Async.Completed;
            }

            this.zoom = zoom;
            var tw = new FloatTween(GetCameraZoom(), zoom, tween, SetCameraZoom);
            return zoomTweener.Run(tw, token, Camera);
        }

        protected virtual async Awaitable<Camera> InitializeMainCamera (Transform parent, int uiLayer)
        {
            if (Configuration.CustomCameraPrefab)
            {
                var cam = await Engine.Instantiate(Configuration.CustomCameraPrefab, new() { Parent = parent });
                cam.transform.localPosition = Vector3.zero; // Position is controlled via look container.
                return cam;
            }

            var camera = Engine.CreateObject<Camera>(new() { Name = "MainCamera", Parent = parent });
            camera.transform.localPosition = Vector3.zero;
            camera.depth = 0;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(25, 25, 25, 255);
            camera.orthographic = true;
            camera.orthographicSize = Configuration.SceneRect.height / 2;
            camera.fieldOfView = 60f;
            camera.useOcclusionCulling = false;
            camera.depthTextureMode |= DepthTextureMode.Depth;
            if (!Configuration.UseUICamera)
                camera.allowHDR = false; // Otherwise text artifacts appear when printing.
            if (Engine.Configuration.OverrideObjectsLayer)
                // When culling is enabled, render only the engine object and UI (when not using UI camera) layers.
                camera.cullingMask = Configuration.UseUICamera
                    ? 1 << Engine.Configuration.ObjectsLayer
                    : (1 << Engine.Configuration.ObjectsLayer) | (1 << uiLayer);
            else if (Configuration.UseUICamera) camera.cullingMask = ~(1 << uiLayer);
            return camera;
        }

        protected virtual async Awaitable<Camera> InitializeUICamera (Transform parent, int uiLayer)
        {
            if (Configuration.CustomUICameraPrefab)
            {
                var cam = await Engine.Instantiate(Configuration.CustomUICameraPrefab, new() { Parent = parent });
                cam.transform.position = Configuration.InitialPosition;
                return cam;
            }

            var camera = Engine.CreateObject<Camera>(new() { Name = "UICamera", Parent = parent });
            camera.depth = 1;
            camera.orthographic = true;
            camera.allowHDR = false; // Otherwise text artifacts appear when printing.
            camera.cullingMask = 1 << uiLayer;
            camera.clearFlags = CameraClearFlags.Depth;
            camera.useOcclusionCulling = false;
            camera.transform.position = Configuration.InitialPosition;
            return camera;
        }

        protected virtual void SetupCamerasForURP ()
        {
            #if URP_AVAILABLE
            RenderUtils.FindBaseCamera(out var baseData, cam => cam != Camera && cam != UICamera);

            var mainData = UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(Camera);
            mainData.renderType = baseData != null
                ? UnityEngine.Rendering.Universal.CameraRenderType.Overlay
                : UnityEngine.Rendering.Universal.CameraRenderType.Base;
            mainData.requiresColorTexture = true;
            mainData.requiresDepthTexture = true;
            mainData.renderPostProcessing = true;
            mainData.volumeLayerMask = Camera.cullingMask;
            if (baseData != null) baseData.cameraStack.Add(Camera);

            if (UICamera)
            {
                var uiData = UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(UICamera);
                uiData.renderType = UnityEngine.Rendering.Universal.CameraRenderType.Overlay;
                if (baseData != null) baseData.cameraStack.Add(UICamera);
                else mainData.cameraStack.Add(UICamera);
            }
            #endif
        }

        protected virtual void SetEnabled (bool enabled)
        {
            this.enabled = enabled;
            Camera.enabled = enabled;
            if (UICamera != null) UICamera.enabled = enabled;
        }

        protected virtual CameraComponentState[] GetComponentState (Camera camera)
        {
            camera.GetComponents(cameraComponentsCache);
            // Why zero? Camera is not a MonoBehaviour, so don't count it; others are considered custom effect.
            if (cameraComponentsCache.Count == 0) return Array.Empty<CameraComponentState>();
            return cameraComponentsCache.Select(c => new CameraComponentState(c)).ToArray();
        }

        protected virtual void ApplyComponentState (Camera camera, IReadOnlyCollection<CameraComponentState> state)
        {
            if (state is null) return;
            foreach (var compState in state)
                if (camera.GetComponent(compState.TypeName) is MonoBehaviour component)
                    component.enabled = compState.Enabled;
        }

        protected virtual bool GetRenderUI ()
        {
            if (UICamera) return UICamera.enabled;
            return MaskUtils.GetLayer(Camera.cullingMask, UILayer);
        }

        protected virtual void SetRenderUI (bool value)
        {
            if (UICamera) UICamera.enabled = value;
            else Camera.cullingMask = MaskUtils.SetLayer(Camera.cullingMask, UILayer, value);
            OnRenderUIChanged?.Invoke(value);
        }

        protected virtual void SetOffset (Vector3 value)
        {
            CompleteOffsetTween();
            offset = value;
            SetCameraOffset(value);
            OnOffsetChanged?.Invoke(value);
        }

        protected virtual void SetRotation (Quaternion value)
        {
            CompleteRotationTween();
            rotation = value;
            SetCameraRotation(value);
            OnRotationChanged?.Invoke(value);
        }

        protected virtual void SetZoom (float value)
        {
            CompleteZoomTween();
            zoom = value;
            SetCameraZoom(value);
            OnZoomChanged?.Invoke(value);
        }

        protected virtual void SetOrthographic (bool value)
        {
            Camera.orthographic = value;
            Zoom = Zoom;
        }

        protected virtual void SetCameraOffset (Vector3 offset)
        {
            LookContainer.position = Configuration.InitialPosition + offset;
        }

        protected virtual Vector3 GetCameraOffset ()
        {
            return LookContainer.position - Configuration.InitialPosition;
        }

        protected virtual void SetCameraRotation (Quaternion rotation)
        {
            LookContainer.rotation = rotation;
        }

        protected virtual void SetCameraRotation (Vector3 rotation)
        {
            LookContainer.rotation = Quaternion.Euler(rotation);
        }

        protected virtual Quaternion GetCameraRotation ()
        {
            return LookContainer.rotation;
        }

        protected virtual void SetCameraZoom (float zoom)
        {
            if (Orthographic) Camera.orthographicSize = OrthographicSize * (1f - Mathf.Clamp(zoom, 0, .99f));
            else Camera.fieldOfView = Mathf.Lerp(5f, FOV, 1f - zoom);
        }

        protected virtual float GetCameraZoom ()
        {
            if (Orthographic) return Mathf.Clamp(1f - Camera.orthographicSize / OrthographicSize, 0, .99f);
            return Mathf.Clamp(1f - Mathf.InverseLerp(5f, FOV, Camera.fieldOfView), 0, .99f);
        }

        private void CompleteOffsetTween ()
        {
            if (offsetTweener.Running)
                offsetTweener.Complete();
        }

        private void CompleteRotationTween ()
        {
            if (rotationTweener.Running)
                rotationTweener.Complete();
        }

        private void CompleteZoomTween ()
        {
            if (zoomTweener.Running)
                zoomTweener.Complete();
        }
    }
}
