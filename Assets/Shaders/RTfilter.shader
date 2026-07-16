Shader "Unlit/RTfilter"
{
    Properties
    {
        _MainTex ("Render Texture", 2D) = "white" {}
        _OutlineColor("Outline Color", Color) = (1, 1, 1, 1)
        _OutlineWidth("Outline Width", Range(0.0, 10.0)) = 2.0

        _OutlineColor2("Outline Color 2", Color) = (0, 0, 0, 1)
        _OutlineWidth2("Outline Width 2", Range(0.0, 10.0)) = 1.0
        _OutlineAA("Outline Edge Softness (texels)", Range(0.0, 3.0)) = 1.0

        [Toggle] _ApplyTint ("Tint", Float) = 1.0
        _TintColor ("Tint Color", Color) = (1, 1, 1, 1)

        _Alpha ("Alpha", Range(0.0, 1.0)) = 1.0

        [Toggle] _ApplyWave ("Wave Distortion", Float) = 0.0
        _WaveAmplitude ("Wave Amplitude", Range(0.0, 0.2)) = 0.02
        _WaveFrequency ("Wave Frequency", Range(0.0, 50.0)) = 10.0
        _WaveSpeed ("Wave Speed", Range(0.0, 10.0)) = 1.0
        _WaveDirection ("Wave Direction (xy=axis, z=phase mix)", Vector) = (1, 0, 0.5, 0)

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

        [Toggle] _UseDepthScale ("Use Depth-Edge Outline", Float) = 0.0
        _DepthEdgeThreshold ("Depth Edge Threshold", Range(0.0, 0.05)) = 0.002
        [Toggle] _DebugDepth ("Debug: Show edge masks", Float) = 0.0

        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull Mode", Float) = 2
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4
        [Enum(Off, 0, On, 1)] _ZWrite ("ZWrite", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent"}

        Pass
        {
            Blend [_SrcBlend][_DstBlend]
            Cull[_Cull]
            ZWrite[_ZWrite]
            ZTest[_ZTest]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _APPLYTINT_ON
            #pragma multi_compile _ _APPLYWAVE_ON
            #pragma multi_compile _ _USEDEPTHSCALE_ON
            #pragma multi_compile _ _DEBUGDEPTH_ON
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

            float4 _OutlineColor;
            float _OutlineWidth;
            float4 _OutlineColor2;
            float _OutlineWidth2;
            float _OutlineAA;
            float4 _TintColor;
            float _Alpha;

            float _WaveAmplitude;
            float _WaveFrequency;
            float _WaveSpeed;
            float4 _WaveDirection;

            TEXTURE2D(_BgDepthTex);
            SAMPLER(sampler_BgDepthTex);
            float _DepthEdgeThreshold;


            float2 ApplyWave(float2 uv)
            {
                float2 dir = normalize(_WaveDirection.xy + 1e-6);
                float2 perp = float2(-dir.y, dir.x);
                float along = dot(uv, dir);
                float t = _TimeParameters.x * _WaveSpeed;

                float wave = sin(along * _WaveFrequency + t);
                float wave2 = sin(along * _WaveFrequency * 0.5 + t * 1.3 + 1.57);
                float mixed = lerp(wave, wave2, saturate(_WaveDirection.z));

                return uv + perp * mixed * _WaveAmplitude;
            }

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }
            // Sampled at _BlurLod so outlines soften together with the blurred
            // content instead of staying crisp around a blurry character.
            float samplealpha(float2 uv)
            {
                return SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv, _BlurLod).a;
            }

            // Circular 12-direction dilation, sampled at two radii spanning an
            // _OutlineAA-texel feather band. Averaging the two rings turns the
            // stroke's outer edge into a linear ramp instead of a hard step.
            float getneighboralpha(float2 uv, float2 offset)
            {
                static const float2 dirs[12] = {
                    float2( 1.0,        0.0      ), float2( 0.8660254,  0.5      ),
                    float2( 0.5,        0.8660254), float2( 0.0,        1.0      ),
                    float2(-0.5,        0.8660254), float2(-0.8660254,  0.5      ),
                    float2(-1.0,        0.0      ), float2(-0.8660254, -0.5      ),
                    float2(-0.5,       -0.8660254), float2( 0.0,       -1.0      ),
                    float2( 0.5,       -0.8660254), float2( 0.8660254, -0.5      )
                };

                float2 feather = _OutlineAA * 0.5 * _MainTex_TexelSize.xy;
                float2 rNear = max(offset - feather, 0.0);
                float2 rFar  = offset + feather;

                float mNear = 0.0;
                float mFar  = 0.0;
                [unroll]
                for (int k = 0; k < 12; k++)
                {
                    mNear = max(mNear, samplealpha(uv + dirs[k] * rNear));
                    mFar  = max(mFar,  samplealpha(uv + dirs[k] * rFar));
                }
                return 0.5 * (mNear + mFar);
            }

            float getmaxneighbordepthdelta(float2 uv, float2 offset, float centerD)
            {
                float d1 = SAMPLE_TEXTURE2D(_BgDepthTex, sampler_BgDepthTex, uv + float2(-offset.x, offset.y)).r;
                float d2 = SAMPLE_TEXTURE2D(_BgDepthTex, sampler_BgDepthTex, uv + float2(0, offset.y)).r;
                float d3 = SAMPLE_TEXTURE2D(_BgDepthTex, sampler_BgDepthTex, uv + float2(offset.x, offset.y)).r;
                float d4 = SAMPLE_TEXTURE2D(_BgDepthTex, sampler_BgDepthTex, uv + float2(-offset.x, 0)).r;
                float d5 = SAMPLE_TEXTURE2D(_BgDepthTex, sampler_BgDepthTex, uv + float2(offset.x, 0)).r;
                float d6 = SAMPLE_TEXTURE2D(_BgDepthTex, sampler_BgDepthTex, uv + float2(-offset.x, -offset.y)).r;
                float d7 = SAMPLE_TEXTURE2D(_BgDepthTex, sampler_BgDepthTex, uv + float2(0, -offset.y)).r;
                float d8 = SAMPLE_TEXTURE2D(_BgDepthTex, sampler_BgDepthTex, uv + float2(offset.x, -offset.y)).r;

                float m = max(abs(d1 - centerD), abs(d2 - centerD));
                m = max(m, abs(d3 - centerD));
                m = max(m, abs(d4 - centerD));
                m = max(m, abs(d5 - centerD));
                m = max(m, abs(d6 - centerD));
                m = max(m, abs(d7 - centerD));
                m = max(m, abs(d8 - centerD));
                return m;
            }

            float4 frag (Varyings i) : SV_Target
            {
                float2 uv = i.uv;
                #if _APPLYWAVE_ON
                    uv = ApplyWave(uv);
                #endif

                float4 col = LayerFX_SampleBlurred(TEXTURE2D_ARGS(_MainTex, sampler_MainTex),
                                                   uv, _BlurLod, _MainTex_TexelSize.xy);

                float mask = LayerFX_MaskAlpha(i.uv, GetNormalizedScreenSpaceUV(i.positionCS));

                float2 offset  = _OutlineWidth * _MainTex_TexelSize.xy;
                float2 offset2 = (_OutlineWidth + _OutlineWidth2) * _MainTex_TexelSize.xy;

                #if _USEDEPTHSCALE_ON
                    // Depth-edge outline: paint outlines onto opaque background
                    // at depth discontinuities (object silhouettes).
                    float centerD = SAMPLE_TEXTURE2D(_BgDepthTex, sampler_BgDepthTex, uv).r;
                    float dInner = getmaxneighbordepthdelta(uv, offset,  centerD);
                    float dOuter = getmaxneighbordepthdelta(uv, offset2, centerD);
                    float innerEdge = step(_DepthEdgeThreshold, dInner);
                    float outerEdge = step(_DepthEdgeThreshold, dOuter);

                    float4 finalColor = col;
                    finalColor = lerp(finalColor, _OutlineColor2, outerEdge);
                    finalColor = lerp(finalColor, _OutlineColor,  innerEdge);

                    #if _APPLYTINT_ON
                        finalColor.rgb *= _TintColor.rgb;
                    #endif

                    #if _DEBUGDEPTH_ON
                        return float4(innerEdge, outerEdge, 0, 1);
                    #endif

                    return float4(finalColor.rgb, col.a * _Alpha * mask);
                #else
                    float a = col.a;

                    float neighborAlpha  = getneighboralpha(uv, offset);
                    float neighborAlpha2 = getneighboralpha(uv, offset2);

                    // max keeps the feathered ramp from getneighboralpha intact;
                    // a saturated sum would steepen it back into a hard edge.
                    float innerMask = max(a, neighborAlpha);
                    float outMask   = max(innerMask, neighborAlpha2);

                    float4 finalColor = float4(0, 0, 0, 0);
                    finalColor = lerp(finalColor, _OutlineColor2, outMask);
                    finalColor = lerp(finalColor, _OutlineColor,  innerMask);
                    finalColor = lerp(finalColor, col, a);

                    #if _APPLYTINT_ON
                        finalColor.rgb *= _TintColor.rgb;
                        outMask *= _TintColor.a;
                    #endif

                    outMask *= _Alpha * mask;

                    return float4(finalColor.rgb, outMask);
                #endif
            }
            ENDHLSL
        }
    }
}
