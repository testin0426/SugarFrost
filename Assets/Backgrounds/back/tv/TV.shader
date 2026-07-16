// TV static-noise Shadertoy port.
// Ported to Unity URP (HLSL) for use as an unlit background plane.
//   - iResolution/fragCoord uv -> IN.uv (already 0..1)
//   - iTime -> _Time.y * _Speed
//   - fract -> frac ; mix -> lerp
Shader "Unlit/TV"
{
    Properties
    {
        _TintColor ("Tint Color (RGB multiply)", Color) = (1,1,1,1)
        _Alpha     ("Alpha", Range(0,1)) = 1.0
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

            float4 _TintColor;   // RGB multiplies the scene; A unused (alpha driven by _Alpha)
            float  _Alpha;       // output alpha, faded 0..1 by VisibilityFadeBridge
            float  _Speed;

            // ---- aliases so the original GLSL body needs minimal edits ----
            #define vec2 float2
            #define vec3 float3
            #define vec4 float4
            #define mix  lerp
            #define fract frac

            vec3 rand (vec2 coord)
            {
              vec3 a = fract (cos (coord.x * 8.3e-3 + coord.y) * vec3 (1.3e5, 4.7e5, 2.9e5));
              vec3 b = fract (sin (coord.x * 8.3e-3 + coord.y) * vec3 (8.1e5, 1.0e5, 0.1e5));
              vec3 c = mix (a, b, 0.5);
              return c;
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
              float iTime = _Time.y * _Speed;
              vec2 uv = IN.uv;
              vec4 color = vec4 (rand (uv * sin (10.0 * iTime)), 1.) * 2. - 1.;
              vec3 col = color.rgb;
              col *= _TintColor.rgb;          // tint multiply (goes to black while hidden)
              return half4 (col, _Alpha);     // straight alpha; hardware blends via SrcAlpha
            }
            ENDHLSL
        }
    }
    Fallback Off
}
