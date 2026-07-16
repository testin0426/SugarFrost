using System.Collections.Generic;
using Naninovel.Commands;
using System.Linq;
using UnityEngine;

#if URP_AVAILABLE
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;
#endif

namespace Naninovel.FX
{
    public class DepthOfField : MonoBehaviour, Spawn.IParameterized, Spawn.IAwaitable, DestroySpawned.IParameterized, DestroySpawned.IAwaitable
    {
        protected float FocusDistance { get; private set; }
        protected float FocalLength { get; private set; }
        protected float Duration { get; private set; }
        protected float StopDuration { get; private set; }

        [SerializeField] private float defaultFocusDistance = 10f;
        [SerializeField] private float defaultFocalLength = 3.75f;
        [SerializeField] private float defaultDuration = 1f;

        private readonly Tweener<FloatTween> focusTweener = new();
        private readonly Tweener<FloatTween> focalTweener = new();
        private CameraComponent cam;

        public virtual void SetSpawnParameters (IReadOnlyList<string> parameters, bool asap)
        {
            if (cam is null)
            {
                var cameraManager = Engine.GetServiceOrErr<ICameraManager>().Camera;
                cam = cameraManager.gameObject.AddComponent<CameraComponent>();
                cam.UseCameraFov = false;
            }

            if (cam.PointOfFocus)
            {
                cam.FocusDistance = Vector3.Dot(cam.PointOfFocus.position - cam.transform.position,
                    cam.transform.forward);
                cam.PointOfFocus = null;
            }

            var focusObjectName = parameters?.ElementAtOrDefault(0);
            if (string.IsNullOrEmpty(focusObjectName))
                FocusDistance = Mathf.Max(0.01f, parameters?.ElementAtOrDefault(1)?.AsInvariantFloat() ??
                                                 defaultFocusDistance);
            else
            {
                var obj = GameObject.Find(focusObjectName);
                if (ObjectUtils.IsValid(obj))
                    cam.PointOfFocus = obj.transform;
                else
                {
                    Engine.Warn($"Failed to find game object with name '{focusObjectName}'; " +
                                "depth of field effect will use a default focus distance.");
                    FocusDistance = defaultFocusDistance;
                }
            }
            FocalLength = Mathf.Abs(parameters?.ElementAtOrDefault(2)?.AsInvariantFloat() ?? defaultFocalLength);
            Duration = asap ? 0 : Mathf.Abs(parameters?.ElementAtOrDefault(3)?.AsInvariantFloat() ?? defaultDuration);
        }

        public Awaitable AwaitSpawn (AsyncToken token = default)
        {
            if (focusTweener.Running) focusTweener.Complete();
            if (focalTweener.Running) focalTweener.Complete();

            var duration = token.Completed ? 0 : Duration;
            var focusDistanceTw = new FloatTween(cam.FocusDistance, FocusDistance, new(duration), ApplyFocusDistance);
            var focalLengthTw = new FloatTween(cam.FocalLength, FocalLength, new(duration), ApplyFocalLength);

            return Async.All(focusTweener.Run(focusDistanceTw, token, cam),
                focalTweener.Run(focalLengthTw, token, cam));
        }

        public void SetDestroyParameters (IReadOnlyList<string> parameters)
        {
            StopDuration = Mathf.Abs(parameters?.ElementAtOrDefault(0)?.AsInvariantFloat() ?? defaultDuration);
        }

        public Awaitable AwaitDestroy (AsyncToken token = default)
        {
            if (focusTweener.Running)
                focusTweener.Complete();
            if (focalTweener.Running)
                focalTweener.Complete();

            var duration = token.Completed ? 0 : StopDuration;
            var focalLengthTween = new FloatTween(cam.FocalLength, 0, new(duration), ApplyFocalLength);
            return focalTweener.Run(focalLengthTween, token);
        }

        private void ApplyFocusDistance (float value)
        {
            cam.FocusDistance = value;
        }

        private void ApplyFocalLength (float value)
        {
            cam.FocalLength = value;
        }

        private void OnDestroy ()
        {
            // Required to disable the effect on rollback.
            if (cam) Destroy(cam);
        }

        public class CameraComponent : MonoBehaviour
        {
            public Transform PointOfFocus { get; set; }
            public float FocusDistance { get; set; }
            public bool UseCameraFov { get; set; } = true;
            public float FocalLength { get; set; }

            private const float fNumber = 1.4f;
            private const float filmHeight = 0.024f;

            private static readonly int mainTexId = Shader.PropertyToID("_MainTex");
            private static readonly int blurTexId = Shader.PropertyToID("_BlurTex");
            private static readonly int distanceId = Shader.PropertyToID("_Distance");
            private static readonly int lensCoeffId = Shader.PropertyToID("_LensCoeff");
            private static readonly int maxCoCId = Shader.PropertyToID("_MaxCoC");
            private static readonly int rcpMaxCoCId = Shader.PropertyToID("_RcpMaxCoC");
            private static readonly int rcpAspectId = Shader.PropertyToID("_RcpAspect");

            private Camera cam;
            private Material mat;
            #if URP_AVAILABLE
            private BokehPass pass;
            #endif

            private void Awake ()
            {
                #if URP_AVAILABLE
                pass = new(this) { renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing };
                RenderPipelineManager.beginCameraRendering += HandleCameraBeginRendering;
                var shader = Shader.Find("Naninovel/FX/BokehURP");
                #else
                var shader = Shader.Find("Naninovel/FX/BokehBiRP");
                #endif
                mat = new(shader);
                mat.hideFlags = HideFlags.HideAndDontSave;
                cam = GetComponent<Camera>();
            }

            private void OnDestroy ()
            {
                #if URP_AVAILABLE
                RenderPipelineManager.beginCameraRendering -= HandleCameraBeginRendering;
                #endif
                ObjectUtils.DestroyOrImmediate(mat);
            }

            private void OnRenderImage (RenderTexture source, RenderTexture destination)
            {
                if (!mat) return;

                var width = source.width;
                var height = source.height;

                UpdateMaterial(width, height);

                var rt1 = RenderTexture.GetTemporary(width / 2, height / 2, 0, RenderTextureFormat.ARGBHalf);
                source.filterMode = FilterMode.Point;
                Graphics.Blit(source, rt1, mat, 0);
                var rt2 = RenderTexture.GetTemporary(width / 2, height / 2, 0, RenderTextureFormat.ARGBHalf);
                rt1.filterMode = FilterMode.Bilinear;
                Graphics.Blit(rt1, rt2, mat, 1);
                rt2.filterMode = FilterMode.Bilinear;
                Graphics.Blit(rt2, rt1, mat, 2);
                mat.SetTexture(blurTexId, rt1);
                Graphics.Blit(source, destination, mat, 3);

                RenderTexture.ReleaseTemporary(rt1);
                RenderTexture.ReleaseTemporary(rt2);
            }

            private void UpdateMaterial (float width, float height)
            {
                var dist = PointOfFocus ?
                    Vector3.Dot(PointOfFocus.position - cam.transform.position, cam.transform.forward) :
                    FocusDistance;
                var f = CalculateFocalLength();
                var s1 = Mathf.Max(dist, f);
                mat.SetFloat(distanceId, s1);

                var cf = f * f / (fNumber * (s1 - f) * filmHeight * 2);
                cf = Mathf.Max(.001f, cf);
                mat.SetFloat(lensCoeffId, cf);

                var maxCoC = CalculateMaxCoCRadius(height);
                mat.SetFloat(maxCoCId, maxCoC);
                mat.SetFloat(rcpMaxCoCId, 1 / maxCoC);

                var rcpAspect = height / width;
                mat.SetFloat(rcpAspectId, rcpAspect);
            }

            private float CalculateFocalLength ()
            {
                if (!UseCameraFov) return FocalLength;
                var fov = cam.fieldOfView * Mathf.Deg2Rad;
                return 0.5f * filmHeight / Mathf.Tan(0.5f * fov);
            }

            private float CalculateMaxCoCRadius (float screenHeight)
            {
                const float radiusInPixels = 14;
                return Mathf.Min(0.05f, radiusInPixels / screenHeight);
            }

            #if URP_AVAILABLE

            private void HandleCameraBeginRendering (ScriptableRenderContext _, Camera cam)
            {
                if (cam == this.cam)
                    cam.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass(pass);
            }

            private sealed class BokehPass : ScriptableRenderPass
            {
                private class PassData
                {
                    public int PassId;
                    public Vector2 Size;
                    public TextureHandle Src;
                    public TextureHandle Blur;
                    public CameraComponent Ctrl;
                    public MaterialPropertyBlock Props;
                }

                private readonly MaterialPropertyBlock props = new();
                private readonly CameraComponent ctrl;

                public BokehPass (CameraComponent ctrl)
                {
                    this.ctrl = ctrl;
                }

                public override void RecordRenderGraph (RenderGraph graph, ContextContainer ctx)
                {
                    var res = ctx.Get<UniversalResourceData>();
                    var src = res.activeColorTexture;
                    var t1 = GetHalfRT(graph, src, "Naninovel.Bokeh.Temp1");
                    var t2 = GetHalfRT(graph, src, "Naninovel.Bokeh.Temp2");
                    var dest = GetFullRT(graph, src, "Naninovel.Bokeh.Grab");
                    var depth = res.cameraDepthTexture.IsValid() ? res.cameraDepthTexture : res.activeDepthTexture;

                    AddBlit(graph, "Naninovel.Bokeh.Prefilter", src, t1, 0, depth: depth);
                    AddBlit(graph, "Naninovel.Bokeh.DiskBlur", t1, t2, 1);
                    AddBlit(graph, "Naninovel.Bokeh.FinalBlur", t2, t1, 2);
                    AddBlit(graph, "Naninovel.Bokeh.Compose", src, dest, 3, t1);

                    res.cameraColor = dest;
                }

                private void AddBlit (RenderGraph graph, string name, TextureHandle src, TextureHandle dest, int pass,
                    TextureHandle? blur = null, TextureHandle? depth = null)
                {
                    using var builder = graph.AddRasterRenderPass<PassData>(name, out var data);
                    var desk = src.GetDescriptor(graph);
                    data.Size = new(desk.width, desk.height);
                    data.Ctrl = ctrl;
                    props.Clear();
                    data.Props = props;
                    data.PassId = pass;
                    if (blur.HasValue) builder.UseTexture(data.Blur = blur.Value);
                    if (depth.HasValue) builder.UseTexture(depth.Value);
                    builder.UseTexture(data.Src = src);
                    builder.SetRenderAttachment(dest, 0);
                    builder.SetRenderFunc(static (PassData data, RasterGraphContext ctx) => {
                        if (data.PassId == 0) data.Ctrl.UpdateMaterial(data.Size.x, data.Size.y);
                        data.Props.SetTexture(mainTexId, data.Src);
                        if (data.Blur.IsValid()) data.Props.SetTexture(blurTexId, data.Blur);
                        CoreUtils.DrawFullScreen(ctx.cmd, data.Ctrl.mat, data.Props, data.PassId);
                    });
                }

                private static TextureHandle GetFullRT (RenderGraph graph, TextureHandle src, string name)
                {
                    var desc = graph.GetTextureDesc(src);
                    desc.name = name;
                    desc.clearBuffer = false;
                    desc.depthBufferBits = 0;
                    desc.filterMode = FilterMode.Bilinear;
                    return graph.CreateTexture(desc);
                }

                private static TextureHandle GetHalfRT (RenderGraph graph, TextureHandle src, string name)
                {
                    var desc = graph.GetTextureDesc(src);
                    desc.name = name;
                    desc.width /= 2;
                    desc.height /= 2;
                    desc.clearBuffer = false;
                    desc.depthBufferBits = 0;
                    desc.filterMode = FilterMode.Bilinear;
                    desc.colorFormat = GraphicsFormat.R16G16B16A16_SFloat;
                    return graph.CreateTexture(desc);
                }
            }

            #endif
        }
    }
}
