Shader "Universal Render Pipeline/Unlit1"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map (RGB)", 2D) = "white" {}
        [MainColor]   _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _Cutoff("Alpha Cutout", Range(0.0, 1.0)) = 0.5

        // Optional separate alpha texture. Sampled when _USEALPHAMAP is on.
        // Channel: red is used as the alpha multiplier (a single-channel mask works directly).
        [Toggle(_USEALPHAMAP)] _UseAlphaMap("Use Alpha Map", Float) = 0.0
        [NoScaleOffset] _AlphaMap("Alpha Map (R)", 2D) = "white" {}

        // Surface / blend
        [Enum(Opaque, 0, Transparent, 1)] _Surface("Surface Type", Float) = 0.0
        // _Blend is informational only here; actual factors live in _SrcBlend/_DstBlend below.
        [HideInInspector] _Blend("__mode", Float) = 0.0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull Mode", Float) = 2.0
        // Toggling this drives the _ALPHATEST_ON keyword which is what the alpha-test code path is gated on.
        [Toggle(_ALPHATEST_ON)] _AlphaClip("Alpha Clipping", Float) = 1.0
        [Toggle(_LIGHTCOLOR_ON)] _LightColorEffect("Light Color Effect", Float) = 1.0
        [HideInInspector] _BlendOp("__blendop", Float) = 0.0
        // Defaults below match URP's stock "Unlit/Transparent Cutout" state:
        // SrcBlend=One, DstBlend=Zero, ZWrite=On, queue=AlphaTest (set via SubShader Tags).
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _SrcBlendAlpha("__srcA", Float) = 1.0
        [HideInInspector] _DstBlendAlpha("__dstA", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTestMode("ZTest Mode", Float) = 4 // Default 4 = LEqual

        // View-direction depth offset (MToon/lilToon-style "see-through" trick).
        // Positive values push the surface toward the camera so it renders in front of overlapping
        // geometry (e.g. eyes through hair) without visibly translating it much.
        _ZOffset("Z Offset (View Dir)", Range(-0.1, 0.1)) = 0.0

        // Full stencil control. Default Ref=1, Comp=Always, Pass=Keep, Fail=Keep -> no-op.
        // Common pattern for "draw eyeline through hair":
        //   Face material:    Ref=2, Comp=Always, Pass=Replace
        //   Eyeline material: Ref=2, Comp=Equal,  Pass=Keep    (with ZTest=Always)
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp("Stencil Comparison", Float) = 8 // 8 = Always
        [IntRange] _StencilRef("Stencil No", Range(0, 255)) = 1
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilOpPass("Stencil Pass Operation", Float) = 0 // 0 = Keep
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilOpFail("Stencil Fail Operation", Float) = 0 // 0 = Keep

        [HideInInspector] _AlphaToMask("__alphaToMask", Float) = 0.0

        // Editmode props
        _QueueOffset("Queue offset", Float) = 0.0

        // ObsoleteProperties
        [HideInInspector] _MainTex("BaseMap", 2D) = "white" {}
        [HideInInspector] _Color("Base Color", Color) = (0.5, 0.5, 0.5, 1)
        [HideInInspector] _SampleGI("SampleGI", float) = 0.0 // needed from bakedlit
    }

    SubShader
    {
        // Default queue matches URP's "Unlit/Transparent Cutout" (AlphaTest = 2450).
        // Materials can still override via m_CustomRenderQueue on a per-material basis
        // if you need an opaque (2000) or transparent (3000) variant of this shader.
        Tags
        {
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "IgnoreProjector" = "True"
            "UniversalMaterialType" = "Unlit"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100

        // -------------------------------------
        // Render State Commands (apply to all passes unless a pass overrides)
        Blend [_SrcBlend][_DstBlend], [_SrcBlendAlpha][_DstBlendAlpha]
        ZWrite [_ZWrite]
        ZTest  [_ZTestMode]
        Cull   [_Cull]
        Stencil
        {
            Ref  [_StencilRef]
            Comp [_StencilComp]
            Pass [_StencilOpPass]
            Fail [_StencilOpFail]
        }

        // =====================================================================
        // Forward Unlit
        // =====================================================================
        Pass
        {
            Name "Unlit"

            AlphaToMask [_AlphaToMask]

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex   UnlitPassVertexZOffset
            #pragma fragment UnlitPassFragmentAlpha

            // Material keywords
            #pragma shader_feature_local_fragment _SURFACE_TYPE_TRANSPARENT
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ALPHAMODULATE_ON
            #pragma shader_feature_local_fragment _USEALPHAMAP
            #pragma shader_feature_local_fragment _LIGHTCOLOR_ON

            // Unity-defined keywords
            #pragma multi_compile_fog
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitForwardPass.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            float _ZOffset;
            TEXTURE2D(_AlphaMap); SAMPLER(sampler_AlphaMap);

            Varyings UnlitPassVertexZOffset(Attributes input)
            {
                float4 posWS = mul(GetObjectToWorldMatrix(), float4(input.positionOS.xyz, 1.0));
                float3 viewDirWS = normalize(GetCameraPositionWS() - posWS.xyz);
                posWS.xyz += viewDirWS * _ZOffset;
                input.positionOS.xyz = mul(GetWorldToObjectMatrix(), float4(posWS.xyz, 1.0)).xyz;
                return UnlitPassVertex(input);
            }

            // URP 14's UnlitPassFragment is void with SV_Target out params (and a second
            // MRT under _WRITE_RENDERING_LAYERS). Mirror that signature exactly.
            void UnlitPassFragmentAlpha(
                Varyings input
                , out half4 outColor : SV_Target0
            #ifdef _WRITE_RENDERING_LAYERS
                , out float4 outRenderingLayers : SV_Target1
            #endif
            )
            {
            #ifdef _WRITE_RENDERING_LAYERS
                UnlitPassFragment(input, outColor, outRenderingLayers);
            #else
                UnlitPassFragment(input, outColor);
            #endif

            #if defined(_LIGHTCOLOR_ON)
                Light mainLight = GetMainLight();
                half3 ambientTint = SampleSH(half4(0.0h, 0.0h, 0.0h, 1.0h)).rgb * 0.2h;
                half3 lightTint = mainLight.color * (mainLight.distanceAttenuation * mainLight.shadowAttenuation);
                half3 tint = max(ambientTint, lightTint);
                outColor.rgb *= tint;
            #endif

            #if defined(_USEALPHAMAP)
                half a = SAMPLE_TEXTURE2D(_AlphaMap, sampler_AlphaMap, input.uv).r;
                outColor.a *= a;
                #if defined(_ALPHATEST_ON)
                    clip(outColor.a - _Cutoff);
                #endif
            #endif
            }
            ENDHLSL
        }

        // =====================================================================
        // GBuffer (deferred) - fills GBuffer; deferred lighting is stenciled out.
        // =====================================================================
        Pass
        {
            Name "GBuffer"
            Tags { "LightMode" = "UniversalGBuffer" }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma exclude_renderers gles3 glcore

            #pragma vertex   UnlitPassVertexZOffset
            #pragma fragment UnlitGBufferFragmentAlpha

            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ALPHAMODULATE_ON
            #pragma shader_feature_local_fragment _USEALPHAMAP

            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitGBufferPass.hlsl"

            float _ZOffset;
            TEXTURE2D(_AlphaMap); SAMPLER(sampler_AlphaMap);

            Varyings UnlitPassVertexZOffset(Attributes input)
            {
                float4 posWS = mul(GetObjectToWorldMatrix(), float4(input.positionOS.xyz, 1.0));
                float3 viewDirWS = normalize(GetCameraPositionWS() - posWS.xyz);
                posWS.xyz += viewDirWS * _ZOffset;
                input.positionOS.xyz = mul(GetWorldToObjectMatrix(), float4(posWS.xyz, 1.0)).xyz;
                return UnlitPassVertex(input);
            }

            FragmentOutput UnlitGBufferFragmentAlpha(Varyings input)
            {
            #if defined(_USEALPHAMAP)
                half a = SAMPLE_TEXTURE2D(_AlphaMap, sampler_AlphaMap, input.uv).r;
                #if defined(_ALPHATEST_ON)
                    clip(a - _Cutoff);
                #endif
            #endif
                return UnlitPassFragment(input);
            }
            ENDHLSL
        }

        // =====================================================================
        // DepthOnly
        // =====================================================================
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex   DepthOnlyVertexZOffset
            #pragma fragment DepthOnlyFragmentAlpha

            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _USEALPHAMAP

            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"

            float _ZOffset;
            TEXTURE2D(_AlphaMap); SAMPLER(sampler_AlphaMap);

            Varyings DepthOnlyVertexZOffset(Attributes input)
            {
                float4 posWS = mul(GetObjectToWorldMatrix(), float4(input.position.xyz, 1.0));
                float3 viewDirWS = normalize(GetCameraPositionWS() - posWS.xyz);
                posWS.xyz += viewDirWS * _ZOffset;
                input.position.xyz = mul(GetWorldToObjectMatrix(), float4(posWS.xyz, 1.0)).xyz;
                return DepthOnlyVertex(input);
            }

            half4 DepthOnlyFragmentAlpha(Varyings input) : SV_Target
            {
            #if defined(_USEALPHAMAP) && defined(_ALPHATEST_ON)
                half a = SAMPLE_TEXTURE2D(_AlphaMap, sampler_AlphaMap, input.uv).r;
                clip(a - _Cutoff);
            #endif
                return DepthOnlyFragment(input);
            }
            ENDHLSL
        }

        // =====================================================================
        // DepthNormalsOnly
        // =====================================================================
        Pass
        {
            Name "DepthNormalsOnly"
            Tags { "LightMode" = "DepthNormalsOnly" }

            ZWrite On

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex   DepthNormalsVertexZOffset
            #pragma fragment DepthNormalsFragmentAlpha

            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _USEALPHAMAP

            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT // forward-only variant
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitDepthNormalsPass.hlsl"

            float _ZOffset;
            TEXTURE2D(_AlphaMap); SAMPLER(sampler_AlphaMap);

            Varyings DepthNormalsVertexZOffset(Attributes input)
            {
                float4 posWS = mul(GetObjectToWorldMatrix(), float4(input.positionOS.xyz, 1.0));
                float3 viewDirWS = normalize(GetCameraPositionWS() - posWS.xyz);
                posWS.xyz += viewDirWS * _ZOffset;
                input.positionOS.xyz = mul(GetWorldToObjectMatrix(), float4(posWS.xyz, 1.0)).xyz;
                return DepthNormalsVertex(input);
            }

            // URP 14's DepthNormalsFragment is void with SV_Target out params (and a second
            // MRT under _WRITE_RENDERING_LAYERS). Mirror that signature exactly.
            void DepthNormalsFragmentAlpha(
                Varyings input
                , out half4 outNormalWS : SV_Target0
            #ifdef _WRITE_RENDERING_LAYERS
                , out float4 outRenderingLayers : SV_Target1
            #endif
            )
            {
            #if defined(_USEALPHAMAP) && defined(_ALPHATEST_ON)
                half a = SAMPLE_TEXTURE2D(_AlphaMap, sampler_AlphaMap, input.uv).r;
                clip(a - _Cutoff);
            #endif
            #ifdef _WRITE_RENDERING_LAYERS
                DepthNormalsFragment(input, outNormalWS, outRenderingLayers);
            #else
                DepthNormalsFragment(input, outNormalWS);
            #endif
            }
            ENDHLSL
        }

        // =====================================================================
        // Meta (lightmap baking only)
        // =====================================================================
        Pass
        {
            Name "Meta"
            Tags { "LightMode" = "Meta" }

            Cull Off

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex   UniversalVertexMeta
            #pragma fragment UniversalFragmentMetaUnlit

            #pragma shader_feature EDITOR_VISUALIZATION

            #include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitMetaPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    //CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.UnlitShader"
}
