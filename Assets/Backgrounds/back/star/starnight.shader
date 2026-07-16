// Star Nest by Pablo Roman Andrioli
// License: MIT
// Ported to Unity URP (HLSL) for use as an unlit background plane.
//   - GLSL mod() -> negative-safe ModG()
//   - GLSL column-major matrices: v*M -> mul(M,v) (constructor args kept identical)
//   - fract -> frac ; mix -> lerp
//   - iResolution/fragCoord -> plane UVs + _Aspect ; iTime -> _Time.y
//   - iMouse rotation -> _RotX / _RotY properties (defaults = the no-mouse values)
Shader "Unlit/starnight"
{
    Properties
    {
        _TintColor ("Tint Color (RGB multiply)", Color) = (1,1,1,1)
        _Alpha     ("Alpha", Range(0,1)) = 1.0
        _Aspect    ("Aspect (width / height)", Float) = 1.0
        _Speed     ("Time Speed", Float) = 1.0
        _RotX      ("Rotation X", Float) = 0.5
        _RotY      ("Rotation Y", Float) = 0.8
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

            float4 _TintColor;   // RGB multiplies the field; A unused (alpha driven by _Alpha)
            float  _Alpha;       // output alpha, faded 0..1 by VisibilityFadeBridge
            float  _Aspect;
            float  _Speed;
            float  _RotX;
            float  _RotY;

            // ---- aliases so the original GLSL body needs minimal edits ----
            #define vec2 float2
            #define vec3 float3
            #define vec4 float4
            #define mat2 float2x2
            #define mix  lerp
            #define fract frac

            // negative-safe GLSL mod
            float  ModG (float  x, float  y) { return x - y * floor (x / y); }
            float3 ModG (float3 x, float3 y) { return x - y * floor (x / y); }

            // ---- Star Nest constants ----
            #define iterations 17
            #define formuparam 0.53

            #define volsteps 20
            #define stepsize 0.1

            #define zoom   0.800
            #define tile   0.850
            #define speed  0.010

            #define brightness 0.0015
            #define darkmatter 0.300
            #define distfading 0.730
            #define saturation 0.850

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
              // get coords and direction
              vec2 uv = IN.uv - 0.5;
              uv.y /= _Aspect;                       // GLSL: uv.y *= iResolution.y/iResolution.x
              vec3 dir = vec3 (uv * zoom, 1.);
              float time = _Time.y * _Speed * speed + 0.25;

              // rotation (was mouse-driven; now driven by _RotX / _RotY)
              float a1 = _RotX;
              float a2 = _RotY;
              mat2 rot1 = mat2 (cos (a1), sin (a1), - sin (a1), cos (a1));
              mat2 rot2 = mat2 (cos (a2), sin (a2), - sin (a2), cos (a2));
              dir.xz = mul (rot1, dir.xz);           // GLSL: dir.xz *= rot1
              dir.xy = mul (rot2, dir.xy);           // GLSL: dir.xy *= rot2
              vec3 from = vec3 (1., 0.5, 0.5);
              from += vec3 (time * 2., time, -2.);
              from.xz = mul (rot1, from.xz);         // GLSL: from.xz *= rot1
              from.xy = mul (rot2, from.xy);         // GLSL: from.xy *= rot2

              // volumetric rendering
              float s = 0.1, fade = 1.;
              vec3 v = vec3 (0., 0., 0.);
              [loop] for (int r = 0; r < volsteps; r ++) {
                vec3 p = from + s * dir * 0.5;
                p = abs (vec3 (tile, tile, tile) - ModG (p, vec3 (tile * 2., tile * 2., tile * 2.))); // tiling fold
                float pa, a = pa = 0.;
                for (int i = 0; i < iterations; i ++) {
                  p = abs (p) / dot (p, p) - formuparam;  // the magic formula
                  a += abs (length (p) - pa);             // absolute sum of average change
                  pa = length (p);
                }
                float dm = max (0., darkmatter - a * a * 0.001); // dark matter
                a *= a * a;                                 // add contrast
                if (r > 6) fade *= 1. - dm;                 // dark matter, don't render near
                //v += vec3 (dm, dm * 0.5, 0.);
                v += fade;
                v += vec3 (s, s * s, s * s * s * s) * a * brightness * fade; // coloring based on distance
                fade *= distfading;                         // distance fading
                s += stepsize;
              }
              v = mix (vec3 (length (v), length (v), length (v)), v, saturation); // color adjust

              vec3 col = v * 0.01;
              col *= _TintColor.rgb;                 // tint multiply (goes to black while hidden)
              return half4 (col, _Alpha);            // straight alpha; hardware blends via SrcAlpha
            }
            ENDHLSL
        }
    }
    Fallback Off
}
