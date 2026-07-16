// Rotating moon-sigil magic circle (Shadertoy port).
// Ported to Unity URP (HLSL) for use as an unlit background plane, following port.shader conventions:
//   - GLSL mod() -> negative-safe ModG()
//   - GLSL p *= rot(a) (row-vector * column-major mat2) -> explicit RotG(p, a)
//   - atan(y,x) -> atan2 ; fract -> frac
//   - iTime -> static tCur = _Time.y * _Speed
Shader "Unlit/moonmagic"
{
    Properties
    {
        _TintColor ("Tint Color (RGB multiply)", Color) = (1,1,1,1)
        _Alpha     ("Alpha", Range(0,1)) = 1.0
        _Aspect    ("Aspect (width / height)", Float) = 1.0
        _Speed     ("Time Speed", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
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

            float4 _TintColor;   // RGB multiplies the pattern; A unused (alpha driven by _Alpha)
            float  _Alpha;       // output alpha, faded 0..1 by VisibilityFadeBridge
            float  _Aspect;
            float  _Speed;

            // ---- aliases so the original GLSL body needs minimal edits ----
            #define vec2 float2
            #define vec3 float3
            #define vec4 float4
            #define fract frac

            // NOTE: URP Core.hlsl already #defines PI — do not redeclare it here
            static const float TAU = 6.2831853071;

            static float tCur;   // Shadertoy iTime

            // negative-safe GLSL mod
            float ModG (float x, float y) { return x - y * floor (x / y); }

            // AA threshold step (fwidth-based, from the original)
            #define AAstep(thre, val) smoothstep(-.7, .7, ((val) - (thre)) / min(.05, fwidth((val) - (thre))))

            // reference for animation curves: https://easings.net/
            float easeInOutCirc (float x)
            {
                return x < 0.5
                  ? (1. - sqrt (1. - pow (2. * x, 2.))) / 2.
                  : (sqrt (1. - pow (-2. * x + 2., 2.)) + 1.) / 2.;
            }

            #define dt easeInOutCirc(fract(tCur * 0.5))
            #define anim ((TAU / 4.) * dt)

            // GLSL: p *= rot(a), rot(a) = mat2(cos,sin,-sin,cos)
            vec2 RotG (vec2 p, float a)
            {
                float c = cos (a), s = sin (a);
                return vec2 (p.x * c + p.y * s, -p.x * s + p.y * c);
            }

            void moda (inout vec2 p, float rep)
            {
                float per = TAU / rep;
                float a = atan2 (p.y, p.x);
                a = ModG (a - per * 0.5, per) - per * 0.5;
                p = vec2 (cos (a), sin (a)) * length (p);
            }

            float circle (vec2 p, float r)
            { return AAstep (r, length (p)); }

            float circle_empty (vec2 p, float size, float thick)
            {
                return 1. - (circle (p, size) + (1. - circle (p, size * thick)));
            }

            float square (vec2 p, float r)
            {
                p = abs (p);
                return AAstep (r, max (p.x, p.y));
            }

            float square_empty (vec2 p, float size)
            { return 1. - (square (p, size) + (1. - square (p, size * 0.95))); }

            float moon (vec2 p)
            {
                float rad = 0.3;
                return 1. - (circle (p, rad) + (1. - circle (p + vec2 (rad * 0.3, 0.), rad * 0.75)));
            }

            vec2 sigles (vec2 p, float spread)
            {
                p = RotG (p, anim);
                moda (p, 4.);
                p.x -= spread;
                float mask = circle (p, 0.37);
                return vec2 (circle_empty (p, 0.34, 0.8) + moon (abs (abs (p * 3.5) - 0.1) - 0.3),
                             mask);
            }

            float artefact (vec2 p)
            {
                float img = 0.;
                float s = 0.2;
                vec2 pp = p;
                for (int i = 0; i < 5; i ++)
                {
                    pp = RotG (pp, TAU / 10.);
                    pp = RotG (pp, -anim);
                    s += 0.15;
                    img += square_empty (pp, s);
                }

                float spread = 1.45;
                vec2 si = sigles (p, spread * 1.05);
                return img
                    + moon (p)
                    + (circle_empty (p, spread, 0.98) + circle_empty (p, spread * 1.1, 0.98)) * si.y
                    + si.x;
            }

            // =================== Unity plumbing ===================
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
              vec2 uv = 2. * IN.uv - 1.;    // Shadertoy: (2*fragCoord - iResolution.xy) / iResolution.y
              uv.x *= _Aspect;
              tCur = _Time.y * _Speed;
              // inverted from the original: black magic strokes on a white background
              vec3 col = vec3 ((1. - clamp (artefact (uv * 2.), 0., 1.)).xxx);
              col *= _TintColor.rgb;          // tint multiply (goes to black while hidden)
              return half4 (col, _Alpha);     // straight alpha; hardware blends via SrcAlpha
            }
            ENDHLSL
        }
    }
    Fallback Off
}
