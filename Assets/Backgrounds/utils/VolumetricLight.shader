Shader "Custom/VolumetricLight"
{
    Properties
    {
        [Header(Base)]
        _MainTex ("Base Map (RGB=Color, A=Alpha)", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 0.95, 0.8, 1)
        _AlphaMultiplier ("Alpha Multiplier", Range(0, 1)) = 0.192

        [Header(Emission)]
        _EmissionMap ("Emission Map", 2D) = "black" {}
        [HDR] _EmissionColor ("Emission Color", Color) = (1, 0.95, 0.8, 1)
        _EmissionIntensity ("Emission Intensity", Range(0, 10)) = 1.0
        
        [Header(Refraction)]
        _IOR ("Distortion Strength", Range(0, 0.5)) = 0.05
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent+100"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "FakeVolumetricLight"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float  _AlphaMultiplier;
                float4 _EmissionMap_ST;
                float4 _EmissionColor;
                float  _EmissionIntensity;
                float  _IOR;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_EmissionMap);
            SAMPLER(sampler_EmissionMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float4 screenPos   : TEXCOORD2;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs posInputs = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = posInputs.positionCS;
                o.uv         = TRANSFORM_TEX(v.uv, _MainTex);
                o.normalWS   = TransformObjectToWorldNormal(v.normalOS);
                o.screenPos  = ComputeScreenPos(o.positionCS);
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                float3 N = normalize(i.normalWS);

                // Sample base map
                float4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

                // Alpha from base map alpha channel × multiplier (like Blender's alpha factor)
                float alpha = baseTex.a * _AlphaMultiplier;

                // Refraction: offset screen UV by normal for IOR distortion
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                float2 distortion = N.xy * _IOR;
                float2 refractedUV = screenUV + distortion;
                float3 sceneColor = SampleSceneColor(refractedUV);

                // Base color (flat, no metallic/smoothness)
                float3 baseColor = baseTex.rgb * _Color.rgb;

                // Emission
                float3 emissionTex = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, i.uv).rgb;
                float3 emission = emissionTex * _EmissionColor.rgb * _EmissionIntensity;

                // Final: blend base color over refracted scene, then add emission
                float3 finalColor = lerp(sceneColor, baseColor, alpha) + emission * alpha;

                // Fog
                float fogFactor = ComputeFogFactor(i.positionCS.z);
                finalColor = MixFog(finalColor, fogFactor);

                return float4(finalColor, alpha);
            }

            ENDHLSL
        }
    }

    FallBack Off
}
