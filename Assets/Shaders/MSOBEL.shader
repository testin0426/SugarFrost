Shader "CustomURP/Sprite_SobelEdge"
{
    Properties 
    {
        [MainTexture] _MainTex("Texture", 2D) = "white" {}
        _EdgeIntensity("Edge Intensity", Range(0.1, 10.0)) = 1.0
        _EdgeColor("Edge Color", Color) = (0, 0, 0, 1)

        [Header(Render State)]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Source Blend", Float) = 5.0 // 默认: SrcAlpha
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Destination Blend", Float) = 10.0 // 默认: OneMinusSrcAlpha
        [Enum(UnityEngine.Rendering.BlendOp)] _BlendOp("Blend Operation", Float) = 0.0 // 默认: Add
        [Enum(Off, 0, On, 1)] _ZWrite("ZWrite", Float) = 0.0 // 默认: Off
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 8.0 // 默认: Always (2D Sprite常用) 或 4.0 (LEqual)
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull Mode", Float) = 0.0 // 默认: Off
    }
    SubShader 
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "SpriteSobel"
            
            // 使用 Properties 中暴露的变量控制硬件渲染状态
            BlendOp [_BlendOp]
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            ZTest [_ZTest]
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR; 
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

            float _EdgeIntensity;
            float4 _EdgeColor;

            inline float GetIntensity(float2 uv)
            {
                float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                return length(color.rgb) * color.a; 
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                
                float dx = _MainTex_TexelSize.x;
                float dy = _MainTex_TexelSize.y;

                float tleft  = GetIntensity(uv + float2(-dx,  dy));
                float left   = GetIntensity(uv + float2(-dx,  0.0));
                float bleft  = GetIntensity(uv + float2(-dx, -dy));
                float top    = GetIntensity(uv + float2( 0.0,  dy));
                float bottom = GetIntensity(uv + float2( 0.0, -dy));
                float tright = GetIntensity(uv + float2( dx,  dy));
                float right  = GetIntensity(uv + float2( dx,  0.0));
                float bright = GetIntensity(uv + float2( dx, -dy));

                float x =  tleft + 2.0 * left + bleft - tright - 2.0 * right - bright;
                float y = -tleft - 2.0 * top  - tright + bleft  + 2.0 * bottom + bright;

                float edge = saturate(sqrt((x * x) + (y * y)) * _EdgeIntensity);

                float4 originalColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * input.color;

                float3 finalRGB = lerp(originalColor.rgb, _EdgeColor.rgb, edge);
                float finalAlpha = saturate(originalColor.a + edge);

                return float4(finalRGB, finalAlpha);
            }
            ENDHLSL
        }
    }
}