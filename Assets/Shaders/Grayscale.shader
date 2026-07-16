Shader "Hidden/Custom/Grayscale"
{
    HLSLINCLUDE
    #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"
    TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
    TEXTURE2D_SAMPLER2D(_MangaDot, sampler_MangaDot);
    float _Blend;
    float _DotTiling;
    float _DotThreshold;
    float _DotSoftness;
    float4 Frag(VaryingsDefault i) : SV_Target
    {
        float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);

        float luminance = dot(color.rgb, float3(0.2126729, 0.715122, 0.0721750));
        float aspect = _ScreenParams.x / _ScreenParams.y;
        float2 screenUV = i.texcoord * float2(_DotTiling * aspect, _DotTiling);
        
        float3 dotPattern = SAMPLE_TEXTURE2D(_MangaDot, sampler_MangaDot, screenUV).rgb;

        float ramp = smoothstep(_DotThreshold - _DotSoftness, _DotThreshold + _DotSoftness, luminance);

        float3 halftoneColor = luminance.xxx * (lerp(dotPattern, float3(1.0, 1.0, 1.0), ramp));

        color.rgb = lerp(color.rgb, halftoneColor.xxx, _Blend.xxx);
        return color;
    }
    ENDHLSL
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment Frag
            ENDHLSL
        }
    }
}