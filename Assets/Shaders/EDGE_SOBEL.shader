Shader "CustomURP/Edge_Sobel"
{
    Properties 
    {
        _EdgeIntensity ("Edge Intensity", Range(0.1, 10.0)) = 1.0
        _EdgeColor("Edge Color", Color) = (0, 0, 0, 0)
    }
    SubShader 
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "SobelEdge"
            Cull Off ZWrite Off ZTest Always Blend Off

            HLSLPROGRAM
            #pragma vertex FullscreenVert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings FullscreenVert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
                return output;
            }

            TEXTURE2D_X(_BlitTexture);
            SAMPLER(sampler_BlitTexture);

            float _EdgeIntensity;
            float4 _EdgeColor;

            inline float GetIntensity(float2 uv)
            {
                float4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv);
                return length(color.rgb);
            }

            float4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.uv;
                
                float2 texelSize = 1.0 / _ScreenParams.xy;
                float dx = texelSize.x;
                float dy = texelSize.y;

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

                float4 originalColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv);

                return float4(lerp(originalColor.rgb, _EdgeColor, edge), 1.0);
            }
            ENDHLSL
        }
    }
}