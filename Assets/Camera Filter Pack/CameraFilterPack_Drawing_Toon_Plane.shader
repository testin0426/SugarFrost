////////////////////////////////////////////
// CameraFilterPack - by VETASOFT 2020 /////
// URP plane port. Flat-coloring filter: collapses the rendered image
// into flat color regions by averaging the most uniform surrounding
// color block (edge-preserving, Kuwahara-style), then quantizing to
// discrete levels. No gradients, no lighting/PBR.
////////////////////////////////////////////

Shader "CameraFilterPack/Plane/Drawing_Toon"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _Radius ("Flatten Radius (px)", Range(1, 8)) = 3
        _ColorLevels ("Color Levels", Range(2, 16)) = 5

        _Alpha ("Alpha", Range(0.0, 1.0)) = 1.0
        _TintColor ("Tint Color", Color) = (1, 1, 1, 1)

        _BlurLod ("Layer Blur (Mip LOD)", Range(0.0, 6.0)) = 0.0

        [Enum(Off, 0, Rect, 1, Ellipse, 2, Band, 3)] _MaskMode ("Mask Shape", Float) = 0.0
        [Enum(Local UV, 0, Screen, 1)] _MaskSpace ("Mask Space", Float) = 0.0
        _MaskCenterX ("Mask Center X", Float) = 0.5
        _MaskCenterY ("Mask Center Y", Float) = 0.5
        _MaskWidth ("Mask Half Width", Float) = 0.4
        _MaskHeight ("Mask Half Height", Float) = 0.4
        _MaskFeather ("Mask Feather", Float) = 0.1
        _MaskAngle ("Mask Angle", Float) = 0.0
        [Toggle] _MaskInvert ("Mask Invert", Float) = 0.0
        [Toggle] _UseGlobalMask ("Receive Global Mask", Float) = 0.0

        [Toggle] _ApplyOutline ("Two-Layer Outline", Float) = 0.0
        _OutlineColor("Outline Color", Color) = (1, 1, 1, 1)
        _OutlineWidth("Outline Width", Range(0.0, 10.0)) = 2.0
        _OutlineColor2("Outline Color 2", Color) = (0, 0, 0, 1)
        _OutlineWidth2("Outline Width 2", Range(0.0, 10.0)) = 1.0

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
            #pragma multi_compile _ _APPLYOUTLINE_ON
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Shaders/LayerFX.hlsl"

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
            float _Alpha;
            float4 _TintColor;

            float4 _OutlineColor;
            float _OutlineWidth;
            float4 _OutlineColor2;
            float _OutlineWidth2;

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

            // Sampled at _BlurLod so outlines soften together with the blurred
            // content instead of staying crisp around a blurry layer.
            float samplealpha(float2 uv)
            {
                return SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv, _BlurLod).a;
            }

            // Max source-coverage alpha over the 8 neighbours at one ring offset.
            float getneighboralpha(float2 uv, float2 offset)
            {
                float a1 = samplealpha(uv + float2(-offset.x, offset.y));
                float a2 = samplealpha(uv + float2(0, offset.y));
                float a3 = samplealpha(uv + float2(offset.x, offset.y));

                float a4 = samplealpha(uv + float2(-offset.x, 0));
                float a5 = samplealpha(uv + float2(offset.x, 0));

                float a6 = samplealpha(uv + float2(-offset.x, -offset.y));
                float a7 = samplealpha(uv + float2(0, -offset.y));
                float a8 = samplealpha(uv + float2(offset.x, -offset.y));

                return max(max(a1, a2), max(a3, max(a4, max(a5, max(a6, max(a7, a8))))));
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
                                                        uv + float2(x, y) * ts, _BlurLod).rgb;
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

            float4 frag (Varyings i) : SV_Target
            {
                float2 uv = i.uv;
                int r = (int)round(_Radius);
                float2 ts = _MainTex_TexelSize.xy;

                // Carry the source coverage so the plane composites over the
                // scene instead of drawing an opaque rectangle.
                float srcA = samplealpha(uv);

                float mask = LayerFX_MaskAlpha(i.uv, GetNormalizedScreenSpaceUV(i.positionCS));

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

                // Tint (RGB only); _Alpha drives coverage. Default white = no change.
                result *= _TintColor.rgb;

                #if _APPLYOUTLINE_ON
                    // Two-layer outline grown from source coverage (ported from
                    // RTfilter): outer ring = _OutlineColor2, inner ring =
                    // _OutlineColor, then the toon content composited on top.
                    float2 offset  = _OutlineWidth * ts;
                    float2 offset2 = (_OutlineWidth + _OutlineWidth2) * ts;

                    float neighborAlpha  = getneighboralpha(uv, offset);
                    float neighborAlpha2 = getneighboralpha(uv, offset2);

                    float outMask   = saturate(srcA + neighborAlpha + neighborAlpha2);
                    float innerMask = saturate(srcA + neighborAlpha);

                    float4 finalColor = (float4)0;
                    finalColor = lerp(finalColor, _OutlineColor2, outMask);
                    finalColor = lerp(finalColor, _OutlineColor,  innerMask);
                    finalColor = lerp(finalColor, float4(result, 1), srcA);

                    return float4(finalColor.rgb, outMask * _Alpha * mask);
                #else
                    return float4(result, srcA * _Alpha * mask);
                #endif
            }
            ENDHLSL
        }
    }
}
