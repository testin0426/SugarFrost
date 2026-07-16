Shader "CustomURP/Drawing_Crosshatch"
{
    Properties 
    {
        _Distortion ("Distortion", Range(0.0, 10.0)) = 2.0
    }
    SubShader 
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        Pass
        {
            Name "Crosshatch"
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

            // URP标准全屏三角形顶点着色器，性能更好
            Varyings FullscreenVert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
                return output;
            }

            // URP标准的屏幕纹理采样
            TEXTURE2D_X(_BlitTexture);
            SAMPLER(sampler_BlitTexture);

            float _TimeX;
            float _Distortion;

            inline float mod(float x, float modu) {
                return x - floor(x * (1.0 / modu)) * modu;
            }   

            inline float lookup(float2 p, float dx, float dy, float2 resStep)
            {
                float2 uv = (p.xy + float2(dx, dy)) * resStep;
                float4 c = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv);
                return 0.2126 * c.r + 0.7152 * c.g + 0.0722 * c.b;
            }

            float4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.uv;
                float2 resStep = 1.0 / _ScreenParams.xy; // 使用Unity内置屏幕参数
                float width = _Distortion;

                float3 res = float3(1.0, 1.0, 1.0);
                float4 tex = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv);
                float brightness = (0.2126 * tex.r) + (0.7152 * tex.g) + (0.0722 * tex.b);

                float dimmestChannel = min(min(tex.r, tex.g), tex.b);
                float brightestChannel = max(max(tex.r, tex.g), tex.b);
                float delta = brightestChannel - dimmestChannel;
                
                if (delta > 0.1)
                    tex = tex * (1.0 / brightestChannel);
                else
                    tex.rgb = float3(1.0, 1.0, 1.0);

                float2 iText = uv.xy * _ScreenParams.xy;
                float iTextA = iText.x + iText.y;
                float iTextS = iText.x - iText.y;

                if (brightness < 0.8) { if (mod(iTextA, 10.0) <= width) res = tex.rgb * 0.8; }
                if (brightness < 0.6) { if (mod(iTextS, 10.0) <= width) res = tex.rgb * 0.6; }
                if (brightness < 0.3) { if (mod(iTextA - 5.0, 10.0) <= width) res = tex.rgb * 0.3; }
                if (brightness < 0.15){ if (mod(iTextS - 5.0, 10.0) <= width) res = float3(0.0, 0.0, 0.0); }

                float2 p = iText;
                float gx = 0.0;
                float gy = 0.0;

                gx += -lookup(p, -1.0, -1.0, resStep);
                gy += -lookup(p, -1.0, -1.0, resStep);
                gx += -2.0 * lookup(p, -1.0, 0.0, resStep);
                gx += -lookup(p, -1.0, 0.0, resStep); // 原代码逻辑，这里似乎有重复累加，保留原状
                gx += lookup(p, 1.0, -1.0, resStep);
                gy += -lookup(p, 1.0, -1.0, resStep);
                gx += 2.0 * lookup(p, 1.0, 0.0, resStep);
                gx += lookup(p, 1.0, 1.0, resStep);
                gy += lookup(p, 1.0, 1.0, resStep);
                gy += -2.0 * lookup(p, 0.0, -1.0, resStep);
                gy += lookup(p, -1.0, 1.0, resStep);
                gy += 2.0 * lookup(p, 0.0, 1.0, resStep);

                float g = gx * gx + gy * gy;
                res *= (1.0 - g);

                return float4(res, 1.0);
            }
            ENDHLSL
        }
    }
}