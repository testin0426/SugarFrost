////////////////////////////////////////////
// CameraFilterPack - by VETASOFT 2020 /////
// URP plane port. Flat-coloring manga filter: collapses the rendered
// image into flat color regions by averaging the most uniform
// surrounding color block (edge-preserving, Kuwahara-style), quantizes
// to discrete levels, then overlays black ink edges. No gradients,
// no halftone screentone, no lighting/PBR.
////////////////////////////////////////////

Shader "CameraFilterPack/Plane/Drawing_Manga_Color"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _Radius ("Flatten Radius (px)", Range(1, 8)) = 3
        _ColorLevels ("Color Levels", Range(2, 16)) = 5
        _EdgeThreshold ("Ink Edge Threshold", Range(0.0, 1.0)) = 0.12
        _EdgeColor ("Ink Color", Color) = (0, 0, 0, 1)

        _Alpha ("Alpha", Range(0.0, 1.0)) = 1.0

        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull Mode", Float) = 2
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4
        [Enum(Off, 0, On, 1)] _ZWrite ("ZWrite", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Blend [_SrcBlend][_DstBlend]
            Cull [_Cull]
            ZWrite [_ZWrite]
            ZTest [_ZTest]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
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

            float _Radius;
            float _ColorLevels;
            float _EdgeThreshold;
            float4 _EdgeColor;
            float _Alpha;

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float Luma(float3 c)
            {
                return dot(c, float3(0.299, 0.587, 0.114));
            }

            // Mean color and luminance variance of one rectangular block of
            // surrounding pixels. Low variance == flat region.
            void Region(float2 uv, int x0, int x1, int y0, int y1, float2 ts,
                        out float3 mean, out float variance)
            {
                float3 sum = 0;
                float sum2 = 0;
                float n = 0;

                [loop]
                for (int y = y0; y <= y1; y++)
                {
                    [loop]
                    for (int x = x0; x <= x1; x++)
                    {
                        float3 c = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex,
                                                        uv + float2(x, y) * ts, 0).rgb;
                        sum += c;
                        float l = Luma(c);
                        sum2 += l * l;
                        n += 1.0;
                    }
                }

                mean = sum / n;
                float meanL = Luma(mean);
                variance = abs(sum2 / n - meanL * meanL);
            }

            // Sobel luminance edge magnitude of the original image.
            float EdgeMask(float2 uv, float2 ts)
            {
                float l00 = Luma(SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv + float2(-1, -1) * ts, 0).rgb);
                float l10 = Luma(SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv + float2( 0, -1) * ts, 0).rgb);
                float l20 = Luma(SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv + float2( 1, -1) * ts, 0).rgb);
                float l01 = Luma(SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv + float2(-1,  0) * ts, 0).rgb);
                float l21 = Luma(SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv + float2( 1,  0) * ts, 0).rgb);
                float l02 = Luma(SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv + float2(-1,  1) * ts, 0).rgb);
                float l12 = Luma(SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv + float2( 0,  1) * ts, 0).rgb);
                float l22 = Luma(SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv + float2( 1,  1) * ts, 0).rgb);

                float gx = (l20 + 2.0 * l21 + l22) - (l00 + 2.0 * l01 + l02);
                float gy = (l02 + 2.0 * l12 + l22) - (l00 + 2.0 * l10 + l20);
                return sqrt(gx * gx + gy * gy);
            }

            float4 frag (Varyings i) : SV_Target
            {
                float2 uv = i.uv;
                int r = (int)round(_Radius);
                float2 ts = _MainTex_TexelSize.xy;

                // Carry the source coverage so the plane composites over the
                // scene instead of drawing an opaque rectangle.
                float srcA = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv, 0).a;

                // Four overlapping blocks around the pixel; keep the flattest.
                float3 m0, m1, m2, m3;
                float v0, v1, v2, v3;
                Region(uv, -r, 0, -r, 0, ts, m0, v0);
                Region(uv,  0, r, -r, 0, ts, m1, v1);
                Region(uv, -r, 0,  0, r, ts, m2, v2);
                Region(uv,  0, r,  0, r, ts, m3, v3);

                float3 result = m0;
                float minV = v0;
                if (v1 < minV) { minV = v1; result = m1; }
                if (v2 < minV) { minV = v2; result = m2; }
                if (v3 < minV) { minV = v3; result = m3; }

                // Snap to discrete levels so regions are perfectly flat.
                float levels = _ColorLevels;
                result = floor(result * levels + 0.5) / levels;

                // Overlay solid ink edges (manga linework) on top of flat color.
                float edge = step(_EdgeThreshold, EdgeMask(uv, ts));
                result = lerp(result, _EdgeColor.rgb, edge);

                return float4(result, srcA * _Alpha);
            }
            ENDHLSL
        }
    }
}
