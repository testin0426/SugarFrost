Shader "Custom/URP_ColorEnhanceGlow"
{
    // Transparent color-enhancer for sprites / UI / textured quads.
    //  - Brightness  : multiplies RGB (exposure-style gain).
    //  - Lightness   : lifts toward white without crushing color (HSV-ish value lift).
    //  - Saturation  : pushes color away from / toward grey.
    //  - GlowIntensity: scales output into HDR (>1) so URP's Bloom post-process blooms it.
    //  - HaloStrength : self-glow halo so it still reads as glowing with NO Bloom volume,
    //                   by re-adding bright pixels with a softened, expanded alpha.
    // Stays transparent: SrcAlpha OneMinusSrcAlpha, ZWrite Off, alpha sourced from texture * vertex color.
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Tint        ("Tint",            Color)        = (1, 1, 1, 1)
        _Brightness  ("Brightness",      Range(0, 4))  = 1.2
        _Lightness   ("Lightness Lift",  Range(0, 1))  = 0.15
        _Saturation  ("Saturation",      Range(0, 3))  = 1.25
        _GlowIntensity ("Glow (HDR for Bloom)", Range(1, 8)) = 1.6
        _HaloStrength  ("Self Halo",     Range(0, 2))  = 0.4
        _Alpha       ("Alpha",           Range(0, 1))  = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector"= "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "ColorEnhance"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4  _Tint;
                float  _Brightness;
                float  _Lightness;
                float  _Saturation;
                float  _GlowIntensity;
                float  _HaloStrength;
                float  _Alpha;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                half4  color       : COLOR;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv          = TRANSFORM_TEX(input.uv, _MainTex);
                output.color       = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 src = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                src *= _Tint * input.color;

                half3 rgb = src.rgb;

                // Saturation around perceptual luma.
                half luma = dot(rgb, half3(0.299, 0.587, 0.114));
                rgb = lerp(luma.xxx, rgb, _Saturation);

                // Brightness (gain) then lightness lift toward white.
                rgb *= _Brightness;
                rgb = lerp(rgb, 1.0.xxx, _Lightness);

                // Self halo: brighter pixels glow harder; this is the part that reads
                // as "bloom" even without a post-process Bloom volume.
                half bright = saturate(luma * _Brightness);
                rgb += rgb * bright * _HaloStrength;

                // Push into HDR so a URP Bloom volume picks it up.
                rgb *= _GlowIntensity;

                half alpha = src.a * _Alpha;
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Unlit"
}
