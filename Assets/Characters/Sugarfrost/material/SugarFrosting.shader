Shader "OC/SugarFrosting"
{
    // URP port of the Blender "SugarFrosting" procedural material.
    // Apply to a sphere slightly larger than the hawthorn ball (糖葫芦 shell).
    // All three node groups are reproduced with object-space fbm noise:
    //   Coverage Noise  -> where sugar sits vs. bare (drives alpha)
    //   Detail Noise    -> fine sugar grain (drives roughness)
    //   Sparkle Noise   -> sparse crystal glints (drives emission)
    Properties
    {
        [Header(Sugar Surface)]
        _BaseColor      ("Base Color", Color) = (0.95, 0.93, 0.85, 1)
        _Metallic       ("Metallic", Range(0,1)) = 0.0
        // IOR 1.5 in Blender ~ a faint dielectric spec; tune to taste.
        _Specular       ("Specular", Range(0,1)) = 0.5

        [Header(Coverage Mask    big sugar patches)]
        _CovScale       ("Coverage Scale", Float) = 4.0
        _CovDistortion  ("Coverage Distortion", Range(0,1)) = 0.3
        // Maps to the Blender ColorRamp stops (0.35 -> 0.55). Widen = softer edge.
        _CovLow         ("Coverage Edge Low", Range(0,1)) = 0.35
        _CovHigh        ("Coverage Edge High", Range(0,1)) = 0.55

        [Header(Roughness Variation    fine grain)]
        _DetScale       ("Detail Scale", Float) = 18.0
        _RoughMin       ("Roughness Min", Range(0,1)) = 0.15
        _RoughMax       ("Roughness Max", Range(0,1)) = 0.45

        [Header(Sparkle    crystal glints)]
        _SpkScale       ("Sparkle Scale", Float) = 50.0
        _SpkLow         ("Sparkle Edge Low", Range(0,1)) = 0.78
        _SpkHigh        ("Sparkle Edge High", Range(0,1)) = 0.88
        [HDR] _SpkColor ("Sparkle Color", Color) = (1, 1, 1, 1)
        _SpkStrength    ("Sparkle Strength", Range(0,8)) = 2.0

        [HideInInspector] _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float  _Metallic;
                float  _Specular;
                float  _CovScale;
                float  _CovDistortion;
                float  _CovLow;
                float  _CovHigh;
                float  _DetScale;
                float  _RoughMin;
                float  _RoughMax;
                float  _SpkScale;
                float  _SpkLow;
                float  _SpkHigh;
                float4 _SpkColor;
                float  _SpkStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 positionOS  : TEXCOORD2; // object-space coord = Blender's "Object" output
                float  fogFactor   : TEXCOORD3;
            };

            // ---- 3D gradient (Perlin) noise, math inlined ----
            // Per-cell pseudo-random gradient vector in [-1,1]^3.
            float3 hashGrad(float3 p)
            {
                p = float3(dot(p, float3(127.1, 311.7,  74.7)),
                           dot(p, float3(269.5, 183.3, 246.1)),
                           dot(p, float3(113.5, 271.9, 124.6)));
                return -1.0 + 2.0 * frac(sin(p) * 43758.5453123);
            }

            // Classic gradient noise: dot of corner gradients with the offset to the
            // sample point, trilinearly blended with a quintic fade. Output ~[-1, 1].
            float perlin(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                float3 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0); // quintic fade

                float n000 = dot(hashGrad(i + float3(0,0,0)), f - float3(0,0,0));
                float n100 = dot(hashGrad(i + float3(1,0,0)), f - float3(1,0,0));
                float n010 = dot(hashGrad(i + float3(0,1,0)), f - float3(0,1,0));
                float n110 = dot(hashGrad(i + float3(1,1,0)), f - float3(1,1,0));
                float n001 = dot(hashGrad(i + float3(0,0,1)), f - float3(0,0,1));
                float n101 = dot(hashGrad(i + float3(1,0,1)), f - float3(1,0,1));
                float n011 = dot(hashGrad(i + float3(0,1,1)), f - float3(0,1,1));
                float n111 = dot(hashGrad(i + float3(1,1,1)), f - float3(1,1,1));

                float nx00 = lerp(n000, n100, u.x);
                float nx10 = lerp(n010, n110, u.x);
                float nx01 = lerp(n001, n101, u.x);
                float nx11 = lerp(n011, n111, u.x);
                float nxy0 = lerp(nx00, nx10, u.y);
                float nxy1 = lerp(nx01, nx11, u.y);
                return lerp(nxy0, nxy1, u.z);
            }

            // fbm summing octaves (Blender Detail), then remapped to a high-contrast
            // 0..1 range. NOT amplitude-normalized — that flattening was killing the
            // variation. roughness = per-octave gain (Blender Roughness).
            float fbm01(float3 p, int octaves, float roughness)
            {
                float sum = 0.0;
                float amp = 1.0;
                [loop] for (int i = 0; i < octaves; i++)
                {
                    sum += amp * perlin(p);
                    p *= 2.0;
                    amp *= roughness;
                }
                return saturate(0.5 + 0.9 * sum); // gradient noise is ~[-0.7,0.7]
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.positionOS = IN.positionOS.xyz;
                OUT.fogFactor  = ComputeFogFactor(pos.positionCS.z);
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                // Sample on the normalized object-space direction so patch frequency
                // is independent of the mesh's object-space size. Unity's preview
                // sphere and your fbx ball then look identical at the same scale.
                // (Assumes the shell sphere is roughly centered on its own pivot.)
                float3 oc = normalize(IN.positionOS + 1e-5);

                // -- Coverage noise (big sugar patches) -> alpha mask --
                float3 covP = oc * _CovScale;
                // cheap distortion: warp the sample point like Blender's Distortion input
                covP += _CovDistortion * perlin(covP + 11.7);
                float cov = fbm01(covP, 6, 0.6);
                float coverage = smoothstep(_CovLow, _CovHigh, cov);

                // -- Detail noise -> roughness variation --
                float det = fbm01(oc * _DetScale, 8, 0.7);
                float roughness = lerp(_RoughMin, _RoughMax, smoothstep(0.3, 0.7, det));

                // -- Sparkle noise -> emissive crystal glints (gated by coverage) --
                float spk = fbm01(oc * _SpkScale, 2, 0.9);
                float sparkle = smoothstep(_SpkLow, _SpkHigh, spk) * coverage;

                // ---- assemble URP surface + lighting ----
                SurfaceData surface = (SurfaceData)0;
                surface.albedo    = _BaseColor.rgb;
                surface.metallic  = _Metallic;
                surface.specular  = 0;
                surface.smoothness = saturate(1.0 - roughness);
                surface.occlusion = 1.0;
                surface.emission  = _SpkColor.rgb * sparkle * _SpkStrength;
                surface.alpha     = coverage * _BaseColor.a;

                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.normalWS   = normalize(IN.normalWS);
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                inputData.fogCoord   = IN.fogFactor;
                inputData.bakedGI    = SampleSH(inputData.normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);

                half4 color = UniversalFragmentPBR(inputData, surface);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                color.a = surface.alpha;
                return color;
            }
            ENDHLSL
        }

        // Shadow caster so the sugar shell casts/receives shadows on the candy.
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex shadowVert
            #pragma fragment shadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct ShadowAttributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct ShadowVaryings   { float4 positionCS : SV_POSITION; };

            ShadowVaryings shadowVert(ShadowAttributes IN)
            {
                ShadowVaryings OUT;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 nrmWS = TransformObjectToWorldNormal(IN.normalOS);
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDir = normalize(_LightPosition - posWS);
            #else
                float3 lightDir = _LightDirection;
            #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(posWS, nrmWS, lightDir));
            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif
                OUT.positionCS = positionCS;
                return OUT;
            }

            half4 shadowFrag(ShadowVaryings IN) : SV_Target { return 0; }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
