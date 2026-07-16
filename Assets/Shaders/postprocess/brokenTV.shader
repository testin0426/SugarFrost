Shader "CustomURP/BrokenTV"
{
    // Original by Frank Force
    // License Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License.
    // Ported from ShaderToy to a URP full-screen pass.
    Properties
    {
        _Scanlines ("Scanlines", Range(0.0, 1.0)) = 0.3
        _Fuzz ("Fuzz", Range(0.0, 0.05)) = 0.005
        _FuzzDensity ("Fuzz Density", Float) = 999.0
        _Chromatic ("Chromatic Aberration", Range(0.0, 0.05)) = 0.005
        _StaticNoise ("Static Noise", Range(0.0, 2.0)) = 0.9
        _Ghost ("Ghosting", Range(0.0, 1.0)) = 0.2
        _VerticalMovement ("Vertical Movement", Float) = 2.0
        _VerticalMovementPercent ("Vertical Movement %", Range(0.0, 1.0)) = 0.2
        _Vignette ("Vignette", Range(0.1, 8.0)) = 2.0
        _VignettePow ("Vignette Power", Range(0.0, 16.0)) = 6.0
        _ChromaDistort ("Chroma Distort", Float) = 5.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        Pass
        {
            Name "BrokenTV"
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
            float4 _BlitTexture_TexelSize;

            float _Scanlines;
            float _Fuzz;
            float _FuzzDensity;
            float _Chromatic;
            float _StaticNoise;
            float _Ghost;
            float _VerticalMovement;
            float _VerticalMovementPercent;
            float _Vignette;
            float _VignettePow;
            float _ChromaDistort;

            // PI is already provided by Unity's Common.hlsl (included via Core.hlsl).

            // https://www.shadertoy.com/view/lsf3WH Noise by iq
            float hash(float2 p)
            {
                p = 50.0 * frac(p * 0.3183099 + float2(0.71, 0.113));
                return -1.0 + 2.0 * frac(p.x * p.y * (p.x + p.y));
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash(i + float2(0.0, 0.0)),
                                 hash(i + float2(1.0, 0.0)), u.x),
                            lerp(hash(i + float2(0.0, 1.0)),
                                 hash(i + float2(1.0, 1.0)), u.x), u.y);
            }

            float3 SampleScene(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, saturate(uv)).rgb;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float t = _Time.y;
                float2 res = _BlitTexture_TexelSize.zw;
                float2 uv = input.uv;

                #if UNITY_UV_STARTS_AT_TOP
                if (_BlitTexture_TexelSize.y < 0)
                    uv.y = 1.0 - uv.y;
                #endif

                float2 baseUV = uv;

                // noise
                float4 c = float4(0, 0, 0, 0);
                c += _StaticNoise * ((sin(t) + 2.0) * 0.3) * sin(0.8 - uv.y + sin(t * 3.0) * 0.1) *
                    noise(float2(uv.y * 999.0 + t * 999.0, (uv.x + 999.0) / (uv.y + 0.1) * 19.0));

                // fuzz on edges
                uv.x += _Fuzz * noise(float2(uv.y * _FuzzDensity, t * 9.0));

                // vertical movement
                uv.y += lerp(0.0, sin(t * _VerticalMovement * 0.1) * 5.0,
                    step(noise(float2(t * 0.5, 0.0)) * 0.5 + 0.5, _VerticalMovementPercent));
                uv.y = frac(uv.y);

                // ghosting
                float g = hash(float2(uv.x + sin(t), uv.y));
                uv.x += lerp(0.0, sin(t / 5.0) * 0.5, step(g, _Ghost - 1.0));
                uv.y += lerp(0.0, 0.2 * sin(t / 5.0) * 0.5, step(g, _Ghost - 1.0));

                // chromatic aberration
                c += float4(
                    SampleScene(uv + float2(-_Chromatic, 0)).r,
                    SampleScene(uv + float2(0, 0)).g,
                    SampleScene(uv + float2(_Chromatic, 0)).b,
                    1.0);

                // chromatic distortion
                c.rgb += ddx(c.rgb) * float3(_ChromaDistort, 0, -_ChromaDistort);

                // scanlines
                uv = baseUV;
                c *= 1.0 + _Scanlines * sin(uv.y * res.y * PI / 2.0);

                // black vignette around edges
                float dx = 2.0 * uv.x - 1.0, dy = 2.0 * uv.y - 1.0;
                c *= 1.0 - pow((dx * dx + dy * dy) / _Vignette, _VignettePow);

                return float4(c.rgb, 1.0);
            }
            ENDHLSL
        }
    }
}
