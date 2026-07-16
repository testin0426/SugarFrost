using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[Serializable]
[PostProcess(typeof(GrayscaleRenderer), PostProcessEvent.AfterStack, "Custom/Grayscale")]
public sealed class Grayscale : PostProcessEffectSettings
{
    [Range(0f, 1f), Tooltip("Grayscale effect intensity")]
    public FloatParameter blend = new FloatParameter { value = 0.5f };

    [Tooltip("DotTexture")]
    public TextureParameter mangaDot = new TextureParameter { value = null};

    [Range(1f, 200f), Tooltip("Dot Density")]
    public FloatParameter dotTiling = new FloatParameter { value = 60f };

    [Range(0f, 1f), Tooltip("Light Threshold")]
    public FloatParameter dotThreshold = new FloatParameter { value = 0.5f };

    [Range(0f, 0.2f), Tooltip("dotSoftness")]
    public FloatParameter dotSoftness = new FloatParameter { value = 0.02f };
};

public sealed class GrayscaleRenderer : PostProcessEffectRenderer<Grayscale>
{
    public override void Render(PostProcessRenderContext context)
    {
        var sheet = context.propertySheets.Get(Shader.Find("Hidden/Custom/Grayscale"));
        sheet.properties.SetFloat("_Blend", settings.blend);
        sheet.properties.SetFloat("_DotTiling", settings.dotTiling);
        sheet.properties.SetFloat("_DotThreshold", settings.dotThreshold);
        sheet.properties.SetFloat("_DotSoftness", settings.dotSoftness);

        if (settings.mangaDot.value != null)
        {
            sheet.properties.SetTexture("_MangaDot", settings.mangaDot);
        }

        context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
    }
}