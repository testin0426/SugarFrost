// Desaturate.shader
Shader "Hidden/Custom/Desaturate"
{
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    
    struct Attributes {
        float4 positionOS : POSITION;
        float2 uv : TEXCOORD0;
    };

    struct Varyings {
        float4 positionCS : SV_POSITION;
        float2 uv : TEXCOORD0;
    };

    TEXTURE2D(_MainTex);
    SAMPLER(sampler_MainTex);
    float _Intensity; // 0 为原色，1 为纯黑白

    Varyings vert(Attributes input) {
        Varyings output;
        output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
        output.uv = input.uv;
        return output;
    }

    float4 frag(Varyings input) : SV_Target {
        float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
        
        // 计算亮度 (Grayscale)
        float luminance = dot(color.rgb, float3(0.2126, 0.7152, 0.0722));
        float3 grayscale = float3(luminance, luminance, luminance);
        
        // 根据比例混合
        float3 finalColor = lerp(color.rgb, grayscale, _Intensity);
        
        return float4(finalColor, color.a);
    }
    ENDHLSL

    SubShader {
        Pass {
            ZTest Always ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }
}