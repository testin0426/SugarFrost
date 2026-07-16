Shader "Custom/PBR_DoubleSided"
{
    Properties
    {
        _MainTex ("Albedo Map", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _MetalnessMap ("Metalness Map", 2D) = "black" {}
        _RoughnessMap ("Roughness Map", 2D) = "black" {}
        _OcclusionMap ("Occlusion Map", 2D) = "white" {}

        _AlbedoColor ("Albedo Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _FresnelColor ("Fresnel Color (F0)", Color) = (1.0, 1.0, 1.0, 1.0)

        _Roughness ("Roughness", Range(0,1)) = 0
        _Metalness ("Metalness", Range(0,1)) = 0
        _Anisotropy ("Anisotropy", Range(0,1)) = 0

        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull Mode", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "LightMode"="ForwardBase" }
        

        Cull [_Cull]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "Lighting.cginc"
            #include "PBRLib.cginc"

            #define ENV_MAP_MIP_LVL 14
            #define EPS 1e-5

            struct v_in 
            {
                float4 position : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float4 tangent: TANGENT;
            };

            struct v_out 
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float3 tangent: TEXCOORD2;
                float3 bitangent: TEXCOORD3;
                float3 worldPos : TEXCOORD4;
                float3 tangentLocal: TEXCOORD5;
                float3 bitangentLocal: TEXCOORD6;
            };

            sampler2D _MainTex;
            sampler2D _NormalMap;
            sampler2D _MetalnessMap;
            sampler2D _RoughnessMap;
            sampler2D _OcclusionMap;
            float3 _AlbedoColor;
            float3 _FresnelColor;
            float _Roughness;
            float _Metalness;
            float _Anisotropy;

            v_out vert (v_in v) 
            {
                v_out o;           
                o.uv = v.uv;
                o.position = UnityObjectToClipPos(v.position);
                o.worldPos = mul(unity_ObjectToWorld, v.position);

                o.tangent = normalize(mul(unity_ObjectToWorld, v.tangent).xyz);
                o.normal = normalize(UnityObjectToWorldNormal(v.normal));
                o.bitangent = normalize(cross(o.normal, o.tangent.xyz));

                o.tangentLocal = v.tangent.xyz;
                o.bitangentLocal = normalize(cross(v.normal, o.tangentLocal));
                return o;
            }

            float4 frag (v_out i, fixed facing : VFACE) : SV_Target
            {
                // VFACE 在 D3D 中：正面为正，背面为负
                float isFront = facing > 0 ? 1.0 : -1.0;
                float3 worldNormal = i.normal * isFront;
                float3 worldTangent = i.tangent * isFront;
                float3 worldBitangent = i.bitangent * isFront;

                float2 uv = i.uv;
                float3 lightVec = normalize(_WorldSpaceLightPos0.xyz);
                float3 viewVec = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);
                float3 halfVec = normalize(lightVec + viewVec);

                float3x3 tangentMatrix = transpose(float3x3(worldTangent, worldBitangent, worldNormal));
                float3 normal = mul(tangentMatrix, tex2D(_NormalMap, uv).xyz * 2 - 1);
                normal = normalize(normal);

                float3 reflectVec = -reflect(viewVec, normal);

        
                float NdotL = max(dot(normal, lightVec), 0.0);
                float NdotH = max(dot(normal, halfVec), 0.0);
                float NdotV = max(dot(normal, viewVec), 0.0);

                NdotV = max(NdotV, EPS); 

                float HdotV = max(dot(halfVec, viewVec), 0.0);
                float HdotT = dot(halfVec, i.tangentLocal);
                float HdotB = dot(halfVec, i.bitangentLocal);

                float3 albedo = sRGB2Lin(tex2D(_MainTex, uv));
                float roughness = saturate(max(_Roughness + EPS, tex2D(_RoughnessMap, uv).r));
                float metalness = saturate(max(_Metalness + EPS, tex2D(_MetalnessMap, uv).r));
                float occlusion = saturate(tex2D(_OcclusionMap, uv).r);

                float3 F0 = lerp(float3(0.04, 0.04, 0.04), _FresnelColor * albedo, metalness);

                float D = trowbridgeReitzAnisotropicNDF(NdotH, roughness, _Anisotropy, HdotT, HdotB);
                float3 F = fresnel(F0, NdotV, roughness);
                float G = schlickBeckmannGAF(NdotV, roughness) * schlickBeckmannGAF(NdotL, roughness);
                
                float3 directRadiance = _LightColor0.rgb * occlusion;
                float3 diffuseIrradiance = sRGB2Lin(UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, normal, 10).rgb) * occlusion;
                float3 specularIrradiance = sRGB2Lin(UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, reflectVec, roughness * 10).rgb) * occlusion;

                float3 diffuseDirectTerm = (albedo / 3.1415926) * (1 - F) * (1 - metalness) * _AlbedoColor;
                float3 specularDirectTerm = (G * D * F) / (4 * NdotV * NdotL + EPS);

                float3 brdfDirectOutput = (diffuseDirectTerm + specularDirectTerm) * NdotL * directRadiance;
                float3 ambientDiffuse = diffuseIrradiance * (albedo / 3.1415926) * (1 - F) * (1 - metalness);
                float3 ambientSpecular = specularIrradiance * F;

                return float4(gammaCorrection(brdfDirectOutput + ambientDiffuse + ambientSpecular), 1.0);
            }
            ENDCG
        }
    }
    FallBack "Standard"
}