// "[2TC 15] Mystery Mountains" by David Hoskins.
// License: assume CC-BY-NC-SA (Shadertoy default for the original two-tweet challenge).
// Ported to Unity URP (HLSL) for use as an unlit background plane, matching the
// conventions of port.shader in this project:
//   - GLSL texture() -> SAMPLE_TEXTURE2D_LOD (explicit LOD 0: the raymarch loop has
//     varying flow control, where auto-gradient sampling is illegal in HLSL)
//   - fract -> frac ; mix -> lerp
//   - iChannel0 -> _MainTex ; iResolution-normalized coord -> IN.uv
//   - _Aspect corrects horizontal stretch on non-square planes (no-op at 1.0)
//   - _Alpha is driven 0..1 by VisibilityFadeBridge; _TintColor multiplies the scene
Shader "Unlit/jiang"
{
    Properties
    {
        _MainTex     ("Detail Texture (iChannel0)", 2D) = "gray" {}
        _TintColor   ("Tint Color (RGB multiply)", Color) = (1,1,1,1)
        _Alpha       ("Alpha", Range(0,1)) = 1.0
        _Aspect      ("Aspect (width / height)", Float) = 1.0
        _Speed       ("Time Speed", Float) = 1.0
        _ScrollSpeed ("Forward Scroll Speed", Float) = 12.0
    }
    SubShader
    {
        // Queue 2998 = "Transparent-2": draws just before regular transparents so the
        // background plane sits behind them. Material also pins m_CustomRenderQueue: 2998.
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent-2" }
        LOD 300

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;

            float4 _TintColor;   // RGB multiplies the scene; A unused (alpha driven by _Alpha)
            float  _Alpha;       // output alpha, faded 0..1 by VisibilityFadeBridge
            float  _Aspect;
            float  _Speed;
            float  _ScrollSpeed; // forward travel speed (original hard-coded 20.)

            // ---- aliases so the original GLSL body needs minimal edits ----
            #define vec2 float2
            #define vec3 float3
            #define vec4 float4
            #define fract frac
            #define mix   lerp

            // Original macro:  #define F +texture(iChannel0,.3+p.xz*s/3e3)/(s+=s)
            // Add texture layers of differing frequencies and magnitudes; s doubles each tap.
            #define F +SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, .3 + p.xz * s / 3e3, 0.0) / (s += s)

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip (IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float tCur = _Time.y * _Speed;

                // p = vec4(w/iResolution.xy, 1, 1) - .5   (IN.uv is already the [0,1] coord)
                vec4 p = vec4 (IN.uv, 1., 1.) - .5;
                p.x *= _Aspect;                 // aspect-correct horizontal FOV (no-op at 1.0)
                vec4 d = p;
                vec4 t;
                p.z += tCur * _ScrollSpeed;     // forward travel (was iTime*20.)
                d.y -= .4;

                vec4 c = (vec4)0.;
                [loop] for (float i = 1.5; i > 0.; i -= .002)
                {
                    float s = .5;
                    t = F F F F F F;
                    c = 1. + d.x - t * i; c.z -= .1;
                    if (t.x > p.y * .007 + 1.3) break;
                    p += d;
                }

                vec3 col = c.rgb;
                col *= _TintColor.rgb;          // tint multiply (goes to black while hidden)
                return half4 (col, _Alpha);     // straight alpha; hardware blends via SrcAlpha
            }
            ENDHLSL
        }
    }
    Fallback Off
}
