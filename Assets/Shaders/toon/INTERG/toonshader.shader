// Unity Toon Shader (URP)
// nobuyuki@unity3d.com
// toshiyuki@unity3d.com (Integrated)

Shader "Toon12" {
    Properties {
        // ===== Internal / Versioning =====
        [HideInInspector] _simpleUI ("SimpleUI", Int) = 0
        [HideInInspector][Enum(OFF, 0, ON, 1)] _isUnityToonshader("Material is touched by Unity Toon Shader", Int) = 1
        [HideInInspector] _utsVersionX("VersionX", Float) = 0
        [HideInInspector] _utsVersionY("VersionY", Float) = 11
        [HideInInspector] _utsVersionZ("VersionZ", Float) = 0
        [HideInInspector] _utsTechnique ("Technique", int) = 0 // DWF

        // ===== Render Queue =====
        _AutoRenderQueue("Automatic Render Queue ", int) = 1

        // ===== Stencil =====
        [Enum(OFF, 0, StencilOut, 1, StencilMask, 2)] _StencilMode("StencilMode", int) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp("Stencil Comparison", Float) = 0
        [IntRange] _StencilNo("Stencil No", Range(0, 255)) = 1
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilOpPass("Stencil Pass Operation", Float) = 0
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilOpFail("Stencil Fail Operation", Float) = 0
        [Enum(OFF, 0, ON, 1,)] _TransparentEnabled("Transparent Mode", int) = 0

        // ===== Render State =====
        [Enum(OFF, 0, FRONT, 1, BACK, 2)] _CullMode("Cull Mode", int) = 2
        [Enum(OFF, 0, ONT, 1)] _ZWriteMode("ZWrite Mode", int) = 1
        [Enum(OFF, 0, ONT, 1)] _ZOverDrawMode("ZOver Draw Mode", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTestMode("ZTest Mode", Float) = 4
        _SPRDefaultUnlitColorMask("SPRDefaultUnlit Path Color Mask", int) = 15
        [Enum(OFF, 0, FRONT, 1, BACK, 2)] _SRPDefaultUnlitColMode("SPRDefaultUnlit Cull Mode", int) = 1

        // ===== Clipping =====
        // 0:_IS_CLIPPING_OFF  1:_IS_CLIPPING_MODE  2:_IS_CLIPPING_TRANSMODE
        // (ShadingGradeMap uses _IS_TRANSCLIPPING_OFF/_ON instead)
        [Enum(OFF, 0, ON, 1, TRANSMODE, 2)] _ClippingMode("CliippingMode", int) = 0
        _ClippingMask("ClippingMask", 2D) = "white" {}
        _IsBaseMapAlphaAsClippingMask("IsBaseMapAlphaAsClippingMask", Float) = 0
        [Toggle(_)] _Inverse_Clipping("Inverse_Clipping", Float) = 0
        _Clipping_Level("Clipping_Level", Range(0, 1)) = 0
        _Tweak_transparency("Tweak_transparency", Range(-1, 1)) = 0

        // ===== Base Color =====
        _MainTex ("BaseMap", 2D) = "white" {}
        _BaseMap ("BaseMap", 2D) = "white" {}
        _BaseColor ("BaseColor", Color) = (1,1,1,1)
        _Color ("Color", Color) = (1,1,1,1)
        [Toggle(_)] _Is_LightColor_Base ("Is_LightColor_Base", Float) = 1

        // ===== 1st Shade =====
        _1st_ShadeMap ("1st_ShadeMap", 2D) = "white" {}
        [Toggle(_)] _Use_BaseAs1st ("Use BaseMap as 1st_ShadeMap", Float) = 0
        _1st_ShadeColor ("1st_ShadeColor", Color) = (1,1,1,1)
        [Toggle(_)] _Is_LightColor_1st_Shade ("Is_LightColor_1st_Shade", Float) = 1

        // ===== 2nd Shade =====
        _2nd_ShadeMap ("2nd_ShadeMap", 2D) = "white" {}
        [Toggle(_)] _Use_1stAs2nd ("Use 1st_ShadeMap as 2nd_ShadeMap", Float) = 0
        _2nd_ShadeColor ("2nd_ShadeColor", Color) = (1,1,1,1)
        [Toggle(_)] _Is_LightColor_2nd_Shade ("Is_LightColor_2nd_Shade", Float) = 1

        // ===== Normal Map =====
        _NormalMap ("NormalMap", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Range(0, 1)) = 1
        [Toggle(_)] _Is_NormalMapToBase ("Is_NormalMapToBase", Float) = 0

        // ===== System Shadows =====
        [Toggle(_)] _Set_SystemShadowsToBase ("Set_SystemShadowsToBase", Float) = 1
        _Tweak_SystemShadowsLevel ("Tweak_SystemShadowsLevel", Range(-0.5, 0.5)) = 0

        // ===== Shading Steps & Feathers =====
        _BaseColor_Step ("BaseColor_Step", Range(0, 1)) = 0.5
        _BaseShade_Feather ("Base/Shade_Feather", Range(0.0001, 1)) = 0.0001
        _ShadeColor_Step ("ShadeColor_Step", Range(0, 1)) = 0
        _1st2nd_Shades_Feather ("1st/2nd_Shades_Feather", Range(0.0001, 1)) = 0.0001
        _1st_ShadeColor_Step ("1st_ShadeColor_Step", Range(0, 1)) = 0.5
        _1st_ShadeColor_Feather ("1st_ShadeColor_Feather", Range(0.0001, 1)) = 0.0001
        _2nd_ShadeColor_Step ("2nd_ShadeColor_Step", Range(0, 1)) = 0
        _2nd_ShadeColor_Feather ("2nd_ShadeColor_Feather", Range(0.0001, 1)) = 0.0001

        // ===== ForwardAdd =====
        _StepOffset ("Step_Offset (ForwardAdd Only)", Range(-0.5, 0.5)) = 0
        [Toggle(_)] _Is_Filter_HiCutPointLightColor ("PointLights HiCut_Filter (ForwardAdd Only)", Float) = 1

        // ===== Shade Position / Grade Maps =====
        _Set_1st_ShadePosition ("Set_1st_ShadePosition", 2D) = "white" {}
        _Set_2nd_ShadePosition ("Set_2nd_ShadePosition", 2D) = "white" {}
        _ShadingGradeMap("ShadingGradeMap", 2D) = "white" {}
        _Tweak_ShadingGradeMapLevel("Tweak_ShadingGradeMapLevel", Range(-0.5, 0.5)) = 0
        _BlurLevelSGM("Blur Level of ShadingGradeMap", Range(0, 10)) = 0

        // ===== High Color =====
        _HighColor ("HighColor", Color) = (0,0,0,1)
        _HighColor_Tex ("HighColor_Tex", 2D) = "white" {}
        [Toggle(_)] _Is_LightColor_HighColor ("Is_LightColor_HighColor", Float) = 1
        [Toggle(_)] _Is_NormalMapToHighColor ("Is_NormalMapToHighColor", Float) = 0
        _HighColor_Power ("HighColor_Power", Range(0, 1)) = 0
        [Toggle(_)] _Is_SpecularToHighColor ("Is_SpecularToHighColor", Float) = 0
        [Toggle(_)] _Is_BlendAddToHiColor ("Is_BlendAddToHiColor", Float) = 0
        [Toggle(_)] _Is_UseTweakHighColorOnShadow ("Is_UseTweakHighColorOnShadow", Float) = 0
        _TweakHighColorOnShadow ("TweakHighColorOnShadow", Range(0, 1)) = 0
        _Set_HighColorMask ("Set_HighColorMask", 2D) = "white" {}
        _Tweak_HighColorMaskLevel ("Tweak_HighColorMaskLevel", Range(-1, 1)) = 0
        _HighColor_ChromaAberration ("HighColor_ChromaAberration (RGB Split)", Range(0, 1)) = 0

        // ===== Rim Light =====
        [Toggle(_)] _RimLight ("RimLight", Float) = 0
        _RimLightColor ("RimLightColor", Color) = (1,1,1,1)
        [Toggle(_)] _Is_LightColor_RimLight ("Is_LightColor_RimLight", Float) = 1
        [Toggle(_)] _Is_NormalMapToRimLight ("Is_NormalMapToRimLight", Float) = 0
        _RimLight_Power ("RimLight_Power", Range(0, 1)) = 0.1
        _RimLight_InsideMask ("RimLight_InsideMask", Range(0.0001, 1)) = 0.0001
        [Toggle(_)] _RimLight_FeatherOff ("RimLight_FeatherOff", Float) = 0
        _RimLight_ChromaAberration ("RimLight_ChromaAberration (RGB Split)", Range(0, 1)) = 0
        [Toggle(_)] _LightDirection_MaskOn ("LightDirection_MaskOn", Float) = 0
        _Tweak_LightDirection_MaskLevel ("Tweak_LightDirection_MaskLevel", Range(0, 0.5)) = 0
        [Toggle(_)] _Add_Antipodean_RimLight ("Add_Antipodean_RimLight", Float) = 0
        _Ap_RimLightColor ("Ap_RimLightColor", Color) = (1,1,1,1)
        [Toggle(_)] _Is_LightColor_Ap_RimLight ("Is_LightColor_Ap_RimLight", Float) = 1
        _Ap_RimLight_Power ("Ap_RimLight_Power", Range(0, 1)) = 0.1
        [Toggle(_)] _Ap_RimLight_FeatherOff ("Ap_RimLight_FeatherOff", Float) = 0
        _Set_RimLightMask ("Set_RimLightMask", 2D) = "white" {}
        _Tweak_RimLightMaskLevel ("Tweak_RimLightMaskLevel", Range(-1, 1)) = 0

        // ===== MatCap =====
        [Toggle(_)] _MatCap ("MatCap", Float) = 0
        _MatCap_Sampler ("MatCap_Sampler", 2D) = "black" {}
        _BlurLevelMatcap ("Blur Level of MatCap_Sampler", Range(0, 10)) = 0
        _MatCapColor ("MatCapColor", Color) = (1,1,1,1)
        [Toggle(_)] _Is_LightColor_MatCap ("Is_LightColor_MatCap", Float) = 1
        [Toggle(_)] _Is_BlendAddToMatCap ("Is_BlendAddToMatCap", Float) = 1
        _Tweak_MatCapUV ("Tweak_MatCapUV", Range(-0.5, 0.5)) = 0
        _Rotate_MatCapUV ("Rotate_MatCapUV", Range(-1, 1)) = 0
        [Toggle(_)] _CameraRolling_Stabilizer ("Activate CameraRolling_Stabilizer", Float) = 0
        [Toggle(_)] _Is_NormalMapForMatCap ("Is_NormalMapForMatCap", Float) = 0
        _NormalMapForMatCap ("NormalMapForMatCap", 2D) = "bump" {}
        _BumpScaleMatcap ("Scale for NormalMapforMatCap", Range(0, 1)) = 1
        _Rotate_NormalMapForMatCapUV ("Rotate_NormalMapForMatCapUV", Range(-1, 1)) = 0
        [Toggle(_)] _Is_UseTweakMatCapOnShadow ("Is_UseTweakMatCapOnShadow", Float) = 0
        _TweakMatCapOnShadow ("TweakMatCapOnShadow", Range(0, 1)) = 0
        _Set_MatcapMask ("Set_MatcapMask", 2D) = "white" {}
        _Tweak_MatcapMaskLevel ("Tweak_MatcapMaskLevel", Range(-1, 1)) = 0
        [Toggle(_)] _Inverse_MatcapMask ("Inverse_MatcapMask", Float) = 0
        [Toggle(_)] _Is_Ortho ("Orthographic Projection for MatCap", Float) = 0

        // ===== Angel Ring =====
        [Toggle(_)] _AngelRing("AngelRing", Float) = 0
        _AngelRing_Sampler("AngelRing_Sampler", 2D) = "black" {}
        _AngelRing_Color("AngelRing_Color", Color) = (1, 1, 1, 1)
        [Toggle(_)] _Is_LightColor_AR("Is_LightColor_AR", Float) = 1
        _AR_OffsetU("AR_OffsetU", Range(0, 0.5)) = 0
        _AR_OffsetV("AR_OffsetV", Range(0, 1)) = 0.3
        [Toggle(_)] _ARSampler_AlphaOn("ARSampler_AlphaOn", Float) = 0

        // ===== Anisotropic Hair Highlight (Kajiya-Kay) =====
        // Independent, additive specular for strip-unwrapped hair. Strands should run
        // along one UV axis; the tangent-shift map (R channel, 0.5 = neutral) breaks the
        // highlight up to follow the strand flow.
        [Toggle(_)] _KajiyaKay ("Anisotropic Hair Highlight", Float) = 0
        _Kajiya_TangentShiftMap ("Tangent Shift / Flow Map (R)", 2D) = "gray" {}
        _Kajiya_Shift ("Shift Map Strength", Range(-1, 1)) = 0.3
        [Toggle(_)] _Kajiya_UseBitangent ("Strands run along V (use bitangent)", Float) = 1
        _Kajiya_Mask ("Highlight Mask (R)", 2D) = "white" {}
        [Toggle(_)] _Is_LightColor_Kajiya ("Tint by Light Color", Float) = 1
        [Toggle(_)] _Kajiya_ShadowAttenuate ("Attenuate in Shadow", Float) = 1
        // Primary highlight (sharp, bright)
        _Kajiya_PrimaryColor ("Primary Color", Color) = (1, 1, 1, 1)
        _Kajiya_PrimaryShift ("Primary Shift", Range(-1, 1)) = 0
        _Kajiya_PrimaryExponent ("Primary Sharpness", Range(1, 256)) = 80
        _Kajiya_PrimaryStrength ("Primary Strength", Range(0, 4)) = 1
        // Secondary highlight (broad, tinted)
        _Kajiya_SecondaryColor ("Secondary Color", Color) = (0.6, 0.6, 0.6, 1)
        _Kajiya_SecondaryShift ("Secondary Shift", Range(-1, 1)) = 0.2
        _Kajiya_SecondaryExponent ("Secondary Sharpness", Range(1, 256)) = 20
        _Kajiya_SecondaryStrength ("Secondary Strength", Range(0, 4)) = 0.5

        // ===== Bangs See-Through (blend over eyeliner drawn earlier) =====
        // Pairs with the unlit eyeliner shader: eyeliner draws at queue 2450, this hair
        // material draws AFTER at 2452 and alpha-blends over it. The mask R channel marks
        // the face-overlap zone where the bangs become semi-transparent so the eyeliner
        // shows through. Set this material's render queue to 2452 for the effect to work.
        [Toggle(_)] _HairBlend ("Bangs See-Through (Eyeliner Blend)", Float) = 0
        _HairBlend_Mask ("See-Through Mask (R)", 2D) = "black" {}
        [Toggle(_)] _HairBlend_InvertMask ("Invert Mask", Float) = 0
        _HairBlend_Opacity ("See-Through Opacity (in masked area)", Range(0, 1)) = 0.3

        // ===== Emissive =====
        [KeywordEnum(SIMPLE,ANIMATION)] _EMISSIVE("EMISSIVE MODE", Float) = 0
        _Emissive_Tex ("Emissive_Tex", 2D) = "white" {}
        [HDR] _Emissive_Color ("Emissive_Color", Color) = (0,0,0,1)
        _Base_Speed ("Base_Speed", Float) = 0
        _Scroll_EmissiveU ("Scroll_EmissiveU", Range(-1, 1)) = 0
        _Scroll_EmissiveV ("Scroll_EmissiveV", Range(-1, 1)) = 0
        _Rotate_EmissiveUV ("Rotate_EmissiveUV", Float) = 0
        [Toggle(_)] _Is_PingPong_Base ("Is_PingPong_Base", Float) = 0
        [Toggle(_)] _Is_ColorShift ("Activate ColorShift", Float) = 0
        [HDR] _ColorShift ("ColorSift", Color) = (0,0,0,1)
        _ColorShift_Speed ("ColorShift_Speed", Float) = 0
        [Toggle(_)] _Is_ViewShift ("Activate ViewShift", Float) = 0
        [HDR] _ViewShift ("ViewSift", Color) = (0,0,0,1)
        [Toggle(_)] _Is_ViewCoord_Scroll ("Is_ViewCoord_Scroll", Float) = 0

        // ===== Outline =====
        [KeywordEnum(NML,POS)] _OUTLINE("OUTLINE MODE", Float) = 0
        _Outline_Width ("Outline_Width", Float) = 2.0
        _Farthest_Distance ("Farthest_Distance", Float) = 100
        _Nearest_Distance ("Nearest_Distance", Float) = 0.5
        _Outline_Sampler ("Outline_Sampler", 2D) = "white" {}
        _Outline_Color ("Outline_Color", Color) = (0,0,0,1)
        [Toggle(_)] _Is_BlendBaseColor ("Is_BlendBaseColor", Float) = 0
        [Toggle(_)] _Is_LightColor_Outline ("Is_LightColor_Outline", Float) = 1
        [Toggle(_)] _Is_OutlineTex ("Is_OutlineTex", Float) = 0
        _OutlineTex ("OutlineTex", 2D) = "white" {}
        _Offset_Z ("Offset_Camera_Z", Float) = 0
        [Toggle(_)] _Is_BakedNormal ("Is_BakedNormal", Float) = 0
        _BakedNormal ("Baked Normal for Outline", 2D) = "white" {}

        // ===== GI / Lighting =====
        _GI_Intensity ("GI_Intensity", Range(0, 1)) = 0
        _Unlit_Intensity ("Unlit_Intensity", Range(0, 4)) = 0
        [Toggle(_UNLIT_ON)] _Unlit ("Unlit (Skip Toon Shading)", Float) = 0
        [Toggle(_)] _Is_Filter_LightColor ("VRChat : SceneLights HiCut_Filter", Float) = 1

        // ===== Built-in Light Direction =====
        [Toggle(_)] _Is_BLD ("Advanced : Activate Built-in Light Direction", Float) = 0
        _Offset_X_Axis_BLD (" Offset X-Axis (Built-in Light Direction)", Range(-1, 1)) = -0.05
        _Offset_Y_Axis_BLD (" Offset Y-Axis (Built-in Light Direction)", Range(-1, 1)) = 0.09
        [Toggle(_)] _Inverse_Z_Axis_BLD (" Inverse Z-Axis (Built-in Light Direction)", Float) = 1

        // ===== GI baking compatibility =====
        // Unity GI baking expects these names; do not remove.
        _EmissionColor("Color", Color) = (1, 1, 1)
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        [HideInInspector] emissive("emissive", Color) = (0, 0, 0, 1)
    }

    HLSLINCLUDE
    #define DISABLE_RP_SHADERS
    ENDHLSL

    // *************************** //
    // ****** URP Subshader  ***** //
    // *************************** //
    SubShader
    {
        PackageRequirements
        {
             "com.unity.render-pipelines.universal": "10.5.0"
        }
        Tags {
            "RenderType"="Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

//ToonCoreStart
        Pass {
            Name "ForwardLit"
            Tags{"LightMode" = "UniversalForward"}
            ZWrite[_ZWriteMode]
            ZTest[_ZTestMode]
            Cull[_CullMode]
            Blend SrcAlpha OneMinusSrcAlpha
            Stencil {

                Ref[_StencilNo]

                Comp[_StencilComp]
                Pass[_StencilOpPass]
                Fail[_StencilOpFail]

            }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x

            #pragma vertex vert
            #pragma fragment frag

#ifndef DISABLE_RP_SHADERS
            // -------------------------------------
            // urp Material Keywords
            // -------------------------------------
            #pragma shader_feature_local _ALPHAPREMULTIPLY_ON
            #pragma shader_feature_local _EMISSION
            #pragma shader_feature_local _METALLICSPECGLOSSMAP
            #pragma shader_feature_local _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
//            #pragma shader_feature _OCCLUSIONMAP

            #pragma shader_feature_local _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local _ENVIRONMENTREFLECTIONS_OFF
            #pragma shader_feature_local _SPECULAR_SETUP
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
#endif
            // -------------------------------------
            // Lightweight Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _FORWARD_PLUS
            #if (!defined(UNITY_COMPILER_DXC) && (defined(UNITY_PLATFORM_OSX) || defined(UNITY_PLATFORM_IOS))) || defined(SHADER_API_PS5)

                #if defined(SHADER_API_PS5) || defined(SHADER_API_METAL)

                    #define SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER 1

                    // On Metal Foveated Rendering is currently not supported with DXC
                    #pragma warning (disable : 3568) // unknown pragma ignored

                    #pragma never_use_dxc metal
                    #pragma dynamic_branch _ _FOVEATED_RENDERING_NON_UNIFORM_RASTER

                    #pragma warning (default : 3568) // restore unknown pragma ignored

                #endif

            #endif
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _LIGHT_LAYERS

            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile_fog

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
        #if UNITY_VERSION >= 202230 // Requires Universal RP 14.0.7
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #else
            #pragma multi_compile _ DOTS_INSTANCING_ON
        #endif

            #define _IS_PASS_FWDBASE
            // DoubleShadeWithFeather and ShadingGradeMap use different fragment shader.
            #pragma shader_feature_local _ _SHADINGGRADEMAP


            // used in ShadingGradeMap
            #pragma shader_feature _IS_TRANSCLIPPING_OFF _IS_TRANSCLIPPING_ON
            #pragma shader_feature _IS_ANGELRING_OFF _IS_ANGELRING_ON

            // used in Shadow calculation
            #pragma shader_feature_local _ UTS_USE_RAYTRACING_SHADOW
            // used in DoubleShadeWithFeather
            #pragma shader_feature _IS_CLIPPING_OFF _IS_CLIPPING_MODE _IS_CLIPPING_TRANSMODE

            #pragma shader_feature _EMISSIVE_SIMPLE _EMISSIVE_ANIMATION
            #pragma shader_feature_local _UNLIT_ON
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#ifdef UNIVERSAL_PIPELINE_CORE_INCLUDED
            #include "Assets/Shaders/toon/UniversalRP/Shaders/UniversalToonInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"
            #include "Assets/Shaders/toon/UniversalRP/Shaders/UniversalToonHead.hlsl"
            #include "Assets/Shaders/toon/UniversalRP/Shaders/UniversalToonBody.hlsl"
#endif
            ENDHLSL

        }

        Pass {
            Name "Outline"
            Tags {
                "LightMode" = "SRPDefaultUnlit"
            }
            Cull [_SRPDefaultUnlitColMode]
            ColorMask [_SPRDefaultUnlitColorMask]
            Blend SrcAlpha OneMinusSrcAlpha
            Stencil
            {
                Ref[_StencilNo]
                Comp[_StencilComp]
                Pass[_StencilOpPass]
                Fail[_StencilOpFail]

            }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag


            //V.2.0.4
            #pragma multi_compile _IS_OUTLINE_CLIPPING_NO _IS_OUTLINE_CLIPPING_YES
            #pragma multi_compile _OUTLINE_NML _OUTLINE_POS
            // Outline is implemented in UniversalToonOutline.hlsl.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
        #if UNITY_VERSION >= 202230 // Requires Universal RP 14.0.7
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #else
            #pragma multi_compile _ DOTS_INSTANCING_ON
        #endif

#ifdef UNIVERSAL_PIPELINE_CORE_INCLUDED
            #include "Assets/Shaders/toon/UniversalRP/Shaders/UniversalToonInput.hlsl"
            #include "Assets/Shaders/toon/UniversalRP/Shaders/UniversalToonHead.hlsl"
            #include "Assets/Shaders/toon/UniversalRP/Shaders/UniversalToonOutline.hlsl"
#endif
            ENDHLSL
        }

        Pass
        {
            Name "Universal2D"
            Tags
            {
                "LightMode" = "Universal2D"
            }

            // -------------------------------------
            // Render State Commands
            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex vert
            #pragma fragment frag

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Assets/Shaders/toon/UniversalRP/Shaders/UniversalBasic2D.hlsl"
            ENDHLSL
        }


        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On
            ZTest LEqual
            Cull[_CullMode]

            HLSLPROGRAM
            #pragma target 2.0

            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
        #if UNITY_VERSION >= 202230 // Requires Universal RP 14.0.7
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #else
            #pragma multi_compile _ DOTS_INSTANCING_ON
        #endif

            #pragma shader_feature_local _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Assets/Shaders/toon/UniversalRP/Shaders/UniversalToonInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}

            ZWrite On
            ColorMask 0
            Cull[_CullMode]

            HLSLPROGRAM
            #pragma target 2.0

            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
        #if UNITY_VERSION >= 202230 // Requires Universal RP 14.0.7
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #else
            #pragma multi_compile _ DOTS_INSTANCING_ON
        #endif


            #include "Assets/Shaders/toon/UniversalRP/Shaders/UniversalToonInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
        // This pass is used when drawing to a _CameraNormalsTexture texture
        Pass
        {
            Name "DepthNormals"
            Tags{"LightMode" = "DepthNormals"}

            ZWrite On
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Version.hlsl"


            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x

            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A


            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
        #if UNITY_VERSION >= 202230 // Requires Universal RP 14.0.7
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #else
            #pragma multi_compile _ DOTS_INSTANCING_ON
        #endif

            #include "Assets/Shaders/toon/UniversalRP/Shaders/UniversalToonInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl"

            ENDHLSL
        }

//ToonCoreEnd
    }

    //CustomEditor "UnityEditor.Rendering.Toon.UTS3GUI"
}
