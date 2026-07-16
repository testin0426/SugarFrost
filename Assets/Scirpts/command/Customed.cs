using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


public class Customed : ScriptableRendererFeature
{
    class CustomedDrawPass : ScriptableRenderPass
    {
        private FilteringSettings filteringSettings;
        private RenderStateBlock renderStateBlock;
        private ShaderTagId shaderTagId =  new ShaderTagId("UniversalForward");
        private Material overrideMaterial;


        public CustomedDrawPass(RenderPassEvent passEvent, LayerMask layerMask, Material mat)
        {
            this.renderPassEvent = passEvent;
            this.filteringSettings = new FilteringSettings(RenderQueueRange.opaque, layerMask);
            this.renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
            this.overrideMaterial = mat;
        }
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("CustomLayerDrawPass");
            SortingCriteria sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;
            DrawingSettings drawingSettings = CreateDrawingSettings(shaderTagId, ref renderingData, sortingCriteria);

            if(overrideMaterial != null)
            {
                drawingSettings.overrideMaterial = overrideMaterial;
                drawingSettings.overrideMaterialPassIndex = 0;
            }
            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings, ref renderStateBlock);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        public LayerMask targetLayer;
        public Material customMaterial;
    }

    public Settings settings = new Settings();
    CustomedDrawPass customPass;

    public override void Create()
    {
       customPass = new CustomedDrawPass(settings.renderPassEvent, settings.targetLayer, settings.customMaterial);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(customPass);
    }
}
