Shader "Unlit/skin_builtin"
{
    Properties
    {
        _BaseColor("BaseColor", Color) = (1, 1, 1, 1)
        _MainTex ("Texture", 2D) = "white" {}
        _OutlineColor ("OutlineColor", Color) = (0, 0, 0, 1)
        _OutlineWidth ("OutlineWidth", Range(0.0, 0.1)) = 0.001
        _EdgeThreshold ("Edge Detection Threshold", Range(0, 1)) = 0.2
        _SecColor("SecColor", Color) = (0.7,0.7,0.6,1)
        _MangaTex("Texture", 2D) = "white" {}
        _RimColor("RimColor", Color) = (1, 1, 1, 1)

        _LightDir ("Fake Light Dir", Vector) = (0.2, 0.2, 1, 0)
        [Toggle(_USE_OUTLINE)] _UseOutline ("Enable Outline", Float) = 1
        
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull Mode", Float) = 2
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4
        [Enum(Off, 0, On, 1)] _ZWrite ("ZWrite", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
    }
    
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        
        Pass
        {   
            Name "ToonShading"
            //Tags { "LightMode"="SRPDefaultUnlit" }
            Cull [_Cull]
            ZTest [_ZTest]
            ZWrite[_ZWrite]
            Blend [_SrcBlend][_DstBlend]
    
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdadd
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
    
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };
    
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : NORMAL;
            };
    
            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            float4 _BaseColor;
            float4 _SecColor;
            float _EdgeThreshold;
            float4 _LightDir;
            sampler2D _MangaTex;
            float4 _MangaTex_ST;
    
            float4 _RimColor;
    
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normalWS =TransformObjectToWorldNormal(v.normalOS);
                return o;
            }
            
            float4 frag(v2f i) : SV_Target
            {
    
                float4 col = tex2D(_MainTex, i.uv) * _BaseColor;
                Light mainLight = GetMainLight();
                float3 L = normalize(_LightDir.xyz);
                float3 N = normalize(i.normalWS);
    
                float NdotL = saturate(dot(N, L));
                float toonstep = step(0.5, NdotL);
    
    
                float blackValue = col.r + col.g + col.b;
                float threshold = 0.03;
                clip(blackValue - threshold);
    
                float3 litcolor = lerp(col, _SecColor, 1 - toonstep);
                //float alpha = smoothstep()
                return float4(litcolor , col.a);
            }
            ENDHLSL
        }
    
        Pass
        {
            Name "Outline"
            //Tags { "LightMode"="SRPDefaultUnlit" }

            Cull Front
            ZWrite On
            ColorMask RGB
            
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature _USE_OUTLINE
            #pragma multi_compile_fwdadd
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE



            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            float4 _OutlineColor;
            float _OutlineWidth;

            v2f vert(appdata v)
            {
                v2f o;
                float3 normalCS = TransformObjectToHClip(v.normal);
                v.vertex.xyz += v.normal * _OutlineWidth;
                
                o.pos = TransformObjectToHClip(v.vertex);
                o.pos.xy += normalize(normalCS.xy) * _OutlineWidth;

                float aspect = _ScreenParams.y / _ScreenParams.x;
                o.pos.xy *= aspect;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                #ifndef _USE_OUTLINE
                    clip(-1.0);
                #endif

                return _OutlineColor;
            }
            ENDHLSL
        }
    }
}