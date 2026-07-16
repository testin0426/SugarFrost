#ifndef UNIVERSAL_TOON_INPUT_INCLUDED
#define UNIVERSAL_TOON_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

#define fixed  half
#define fixed3 half3
#define fixed4 half4

CBUFFER_START(UnityPerMaterial)
float _utsTechnique;
float4 _MainTex_ST;

float4 _Color;
fixed _Use_BaseAs1st;
fixed _Use_1stAs2nd;
fixed _Is_LightColor_Base;
//
float4 _1st_ShadeMap_ST;
float4 _1st_ShadeColor;
fixed _Is_LightColor_1st_Shade;
float4 _2nd_ShadeMap_ST;
float4 _2nd_ShadeColor;
fixed _Is_LightColor_2nd_Shade;
float4 _NormalMap_ST;

fixed _Is_NormalMapToBase;
fixed _Set_SystemShadowsToBase;
float _Tweak_SystemShadowsLevel;
float _BaseColor_Step;
float _BaseShade_Feather;

float4 _Set_1st_ShadePosition_ST;
float _ShadeColor_Step;
float _1st2nd_Shades_Feather;
float4 _Set_2nd_ShadePosition_ST;

float4 _ShadingGradeMap_ST;

float _Tweak_ShadingGradeMapLevel;
fixed _BlurLevelSGM;

float _1st_ShadeColor_Step;
float _1st_ShadeColor_Feather;
float _2nd_ShadeColor_Step;
float _2nd_ShadeColor_Feather;

float4 _HighColor;
float4 _HighColor_Tex_ST;
fixed _Is_LightColor_HighColor;
fixed _Is_NormalMapToHighColor;
float _HighColor_Power;

fixed _Is_SpecularToHighColor;
fixed _Is_BlendAddToHiColor;
fixed _Is_UseTweakHighColorOnShadow;
float _TweakHighColorOnShadow;

float4 _Set_HighColorMask_ST;

float _Tweak_HighColorMaskLevel;
float _HighColor_ChromaAberration;
fixed _RimLight;
float4 _RimLightColor;
fixed _Is_LightColor_RimLight;
fixed _Is_NormalMapToRimLight;
float _RimLight_Power;
float _RimLight_InsideMask;
fixed _RimLight_FeatherOff;
float _RimLight_ChromaAberration;
fixed _LightDirection_MaskOn;
float _Tweak_LightDirection_MaskLevel;
fixed _Add_Antipodean_RimLight;
float4 _Ap_RimLightColor;
fixed _Is_LightColor_Ap_RimLight;
float _Ap_RimLight_Power;
fixed _Ap_RimLight_FeatherOff;
float4 _Set_RimLightMask_ST;
float _Tweak_RimLightMaskLevel;
fixed _MatCap;

float4 _MatCap_Sampler_ST;

float4 _MatCapColor;
fixed _Is_LightColor_MatCap;
fixed _Is_BlendAddToMatCap;
float _Tweak_MatCapUV;
float _Rotate_MatCapUV;
fixed _Is_NormalMapForMatCap;

float4 _NormalMapForMatCap_ST;
float _Rotate_NormalMapForMatCapUV;
fixed _Is_UseTweakMatCapOnShadow;
float _TweakMatCapOnShadow;
//MatcapMask
// 
float4 _Set_MatcapMask_ST;
float _Tweak_MatcapMaskLevel;

fixed _Is_Ortho;

float _CameraRolling_Stabilizer;
fixed _BlurLevelMatcap;
fixed _Inverse_MatcapMask;

float _BumpScaleMatcap;

float4 _Emissive_Tex_ST;
float4 _Emissive_Color;

uniform fixed _Is_ViewCoord_Scroll;
float _Rotate_EmissiveUV;
float _Base_Speed;
float _Scroll_EmissiveU;
float _Scroll_EmissiveV;
fixed _Is_PingPong_Base;
float4 _ColorShift;
float4 _ViewShift;
float _ColorShift_Speed;
fixed _Is_ColorShift;
fixed _Is_ViewShift;
float3 emissive;
// 

float _Unlit_Intensity;

fixed _Is_Filter_HiCutPointLightColor;
fixed _Is_Filter_LightColor;

float _StepOffset;
fixed _Is_BLD;
float _Offset_X_Axis_BLD;
float _Offset_Y_Axis_BLD;
fixed _Inverse_Z_Axis_BLD;

float4 _ClippingMask_ST;

fixed _IsBaseMapAlphaAsClippingMask;
float _Clipping_Level;
fixed _Inverse_Clipping;
float _Tweak_transparency;

float _GI_Intensity;
fixed  _AngelRing;
float4 _AngelRing_Sampler_ST;
float4 _AngelRing_Color;
fixed _Is_LightColor_AR;
float _AR_OffsetU;
float _AR_OffsetV;
fixed _ARSampler_AlphaOn;

// Anisotropic Hair Highlight (Kajiya-Kay)
fixed _KajiyaKay;
float4 _Kajiya_TangentShiftMap_ST;
float _Kajiya_Shift;
fixed _Kajiya_UseBitangent;
float4 _Kajiya_Mask_ST;
fixed _Is_LightColor_Kajiya;
fixed _Kajiya_ShadowAttenuate;
float4 _Kajiya_PrimaryColor;
float _Kajiya_PrimaryShift;
float _Kajiya_PrimaryExponent;
float _Kajiya_PrimaryStrength;
float4 _Kajiya_SecondaryColor;
float _Kajiya_SecondaryShift;
float _Kajiya_SecondaryExponent;
float _Kajiya_SecondaryStrength;

// Bangs See-Through (blend over eyeliner drawn earlier)
fixed _HairBlend;
float4 _HairBlend_Mask_ST;
fixed _HairBlend_InvertMask;
float _HairBlend_Opacity;

// OUTLINE


fixed _Is_LightColor_Outline;

float _Outline_Width;
float _Farthest_Distance;
float _Nearest_Distance;
float4 _Outline_Sampler_ST;
float4 _Outline_Color;
fixed _Is_BlendBaseColor;
float _Offset_Z;

float4 _OutlineTex_ST;
fixed _Is_OutlineTex;

float4 _BakedNormal_ST;
fixed _Is_BakedNormal;

float _ZOverDrawMode;

//
// 
//
float4 _BaseMap_ST;
half4 _BaseColor;
half4 _SpecColor;
half4 _EmissionColor;
half _Cutoff;
half _Smoothness;
half _Metallic;
half _BumpScale;
half _OcclusionStrength;
half _Surface;


CBUFFER_END

//sampler2D _MainTex;
//sampler2D _1st_ShadeMap;
//sampler2D _2nd_ShadeMap;
//sampler2D _NormalMap;

TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
TEXTURE2D(_1st_ShadeMap);
TEXTURE2D(_2nd_ShadeMap);
TEXTURE2D(_NormalMap);

sampler2D _Set_1st_ShadePosition; 
sampler2D _Set_2nd_ShadePosition;
sampler2D _ShadingGradeMap;
sampler2D _HighColor_Tex;
sampler2D _Set_HighColorMask;
sampler2D _Set_RimLightMask;
sampler2D _MatCap_Sampler;
sampler2D _NormalMapForMatCap;
sampler2D _Set_MatcapMask;
sampler2D _Emissive_Tex;
//sampler2D _ClippingMask;
TEXTURE2D(_ClippingMask);
sampler2D _AngelRing_Sampler;
sampler2D _Outline_Sampler;
sampler2D _OutlineTex;
sampler2D _BakedNormal;
sampler2D _Kajiya_TangentShiftMap;
sampler2D _Kajiya_Mask;

// Anisotropic (Kajiya-Kay) hair highlight.
// Returns an additive RGB term to composite into finalColor.
//   uv          : UV0 (used to sample the tangent-shift map + mask)
//   T, B        : world-space tangent / bitangent (strand direction is one of these)
//   N           : world-space (normal-mapped) shading normal
//   lightDir    : world-space light direction (toward light)
//   viewDir     : world-space view direction (toward camera)
//   lightColor  : main light color
//   shadowMask  : UTS Set_FinalShadowMask (0 = lit, 1 = fully shadowed)
float3 UTS_KajiyaKaySpecular(float2 uv, float3 T, float3 B, float3 N,
                             float3 lightDir, float3 viewDir, float3 lightColor, float shadowMask)
{
    // Strand direction: tangent (strands along U) or bitangent (strands along V).
    float3 strandDir = normalize(lerp(T, B, _Kajiya_UseBitangent));

    // Tangent-shift / flow map: R=0.5 is neutral, remapped to [-1,1] * strength.
    float shiftTex = tex2D(_Kajiya_TangentShiftMap, TRANSFORM_TEX(uv, _Kajiya_TangentShiftMap)).r;
    float shift = (shiftTex - 0.5) * 2.0 * _Kajiya_Shift;

    float3 H = normalize(lightDir + viewDir);

    // Two shifted-tangent layers (Kajiya-Kay): highlight band is perpendicular to the
    // shifted strand tangent. sin(angle between T and H) drives the streak.
    float3 t1 = normalize(strandDir + N * (shift + _Kajiya_PrimaryShift));
    float dotTH1 = dot(t1, H);
    float spec1 = pow(sqrt(max(1.0 - dotTH1 * dotTH1, 0.0)), _Kajiya_PrimaryExponent) * _Kajiya_PrimaryStrength;

    float3 t2 = normalize(strandDir + N * (shift + _Kajiya_SecondaryShift));
    float dotTH2 = dot(t2, H);
    float spec2 = pow(sqrt(max(1.0 - dotTH2 * dotTH2, 0.0)), _Kajiya_SecondaryExponent) * _Kajiya_SecondaryStrength;

    // Soft NdotL gate so the highlight stays on the lit side.
    float dirAtten = saturate(dot(N, lightDir) * 2.0);
    float mask = tex2D(_Kajiya_Mask, TRANSFORM_TEX(uv, _Kajiya_Mask)).r;

    float3 spec = (spec1 * _Kajiya_PrimaryColor.rgb + spec2 * _Kajiya_SecondaryColor.rgb) * dirAtten * mask;
    spec = lerp(spec, spec * lightColor, _Is_LightColor_Kajiya);
    spec *= lerp(1.0, 1.0 - shadowMask, _Kajiya_ShadowAttenuate);
    return spec;
}

sampler2D _HairBlend_Mask;

// Bangs "see-through": where the mask R marks the face-overlap zone, reduce the output
// alpha so the ForwardLit SrcAlpha/OneMinusSrcAlpha blend reveals what was drawn earlier
// (e.g. the eyeliner at queue 2450). This hair material must render AFTER it (queue 2452).
float UTS_HairSeeThroughAlpha(float baseAlpha, float2 uv)
{
    if (_HairBlend < 0.5)
        return baseAlpha;
    float m = tex2D(_HairBlend_Mask, TRANSFORM_TEX(uv, _HairBlend_Mask)).r;
    m = lerp(m, 1.0 - m, _HairBlend_InvertMask);
    return baseAlpha * lerp(1.0, _HairBlend_Opacity, m);
}



TEXTURE2D(_OcclusionMap);       SAMPLER(sampler_OcclusionMap);
TEXTURE2D(_MetallicGlossMap);   SAMPLER(sampler_MetallicGlossMap);
TEXTURE2D(_SpecGlossMap);       SAMPLER(sampler_SpecGlossMap);


#ifdef _SPECULAR_SETUP
#define SAMPLE_METALLICSPECULAR(uv) SAMPLE_TEXTURE2D(_SpecGlossMap, sampler_SpecGlossMap, uv)
#else
#define SAMPLE_METALLICSPECULAR(uv) SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, uv)
#endif

half4 SampleMetallicSpecGloss(float2 uv, half albedoAlpha)
{
    half4 specGloss;

#ifdef _METALLICSPECGLOSSMAP
    specGloss = SAMPLE_METALLICSPECULAR(uv);
#ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
    specGloss.a = albedoAlpha * _Smoothness;
#else
    specGloss.a *= _Smoothness;
#endif
#else // _METALLICSPECGLOSSMAP
#if _SPECULAR_SETUP
    specGloss.rgb = _SpecColor.rgb;
#else
    specGloss.rgb = _Metallic.rrr;
#endif

#ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
    specGloss.a = albedoAlpha * _Smoothness;
#else
    specGloss.a = _Smoothness;
#endif
#endif

    return specGloss;
}

half SampleOcclusion(float2 uv)
{
#ifdef _OCCLUSIONMAP
    // TODO: Controls things like these by exposing SHADER_QUALITY levels (low, medium, high)
#if defined(SHADER_API_GLES)
    return SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, uv).g;
#else
    half occ = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, uv).g;
    return LerpWhiteTo(occ, _OcclusionStrength);
#endif
#else
    return 1.0;
#endif
}

inline void InitializeStandardLitSurfaceData(float2 uv, out SurfaceData outSurfaceData)
{
    half4 albedoAlpha = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    outSurfaceData.alpha = Alpha(albedoAlpha.a, _BaseColor, _Cutoff);

    half4 specGloss = SampleMetallicSpecGloss(uv, albedoAlpha.a);
    outSurfaceData.albedo = albedoAlpha.rgb * _BaseColor.rgb;

#if _SPECULAR_SETUP
    outSurfaceData.metallic = 1.0h;
    outSurfaceData.specular = specGloss.rgb;
#else
    outSurfaceData.metallic = specGloss.r;
    outSurfaceData.specular = half3(0.0h, 0.0h, 0.0h);
#endif

    outSurfaceData.smoothness = specGloss.a;
    outSurfaceData.normalTS = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
    outSurfaceData.occlusion = SampleOcclusion(uv);
    outSurfaceData.emission = SampleEmission(uv, _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));
}

#endif // UNIVERSAL_INPUT_SURFACE_PBR_INCLUDED
