Shader "OC/Lian_Watch"
{
    Properties
    {
        // Required so Unity UI (Image/RawImage) can bind its sprite; unused by the shader.
        [HideInInspector] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Zoom ("Zoom", Float) = 2.8
        _BgAlpha ("Background Alpha", Range(0, 1)) = 1.0
        [HDR] _BloomColor ("Bloom Color", Color) = (1, 1, 1, 1)
        _BloomIntensity ("Bloom Intensity", Range(0, 4)) = 1.2
        _BloomSpread ("Bloom Spread", Range(0, 0.1)) = 0.02
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Zoom;
                float _BgAlpha;
                float4 _BloomColor;
                float _BloomIntensity;
                float _BloomSpread;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            #define TAU 6.28318530

            float2 Rot(float2 p, float a)
            {
                float c = cos(a), s = sin(a);
                return float2(c * p.x - s * p.y, s * p.x + c * p.y);
            }

            // Crisp stroke + a wide soft HDR halo around the same SDF. This *is*
            // the "bloom" for this line-art: cheap, reads as glow on thin strokes,
            // and avoids re-sampling the whole Scene() per pixel. Halo strength
            // scales with the stroke's own tint so faint marks glow faintly.
            float3 GlowStroke(float d, float w, float3 c)
            {
                float core = 1.0 - smoothstep(0.0, w, abs(d));
                float halo = 1.0 - smoothstep(0.0, w + _BloomSpread, abs(d));
                float weight = max(c.r, max(c.g, c.b));
                return c * core + _BloomColor.rgb * _BloomIntensity * halo * weight;
            }

            float sdCircle(float2 p, float r)
            {
                return length(p) - r;
            }

            float sdLine(float2 p, float2 a, float2 b)
            {
                float2 pa = p - a, ba = b - a;
                float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
                return length(pa - ba * h);
            }

            float sdBox(float2 p, float2 b)
            {
                float2 d = abs(p) - b;
                return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
            }

            // arc SDF: ring clipped to angular aperture
            float sdArc(float2 p, float r, float aperture, float rotation)
            {
                p = Rot(p, -rotation);
                float a = atan2(p.y, p.x);
                float half_ap = aperture * 0.5;
                float ring = abs(length(p) - r);
                float angDist = abs(a) - half_ap;
                if (angDist > 0.0)
                {
                    float2 tip1 = r * float2(cos(half_ap), sin(half_ap));
                    float2 tip2 = r * float2(cos(half_ap), -sin(half_ap));
                    ring = min(length(p - tip1), length(p - tip2));
                }
                return ring;
            }

            // discrete tick with ease-out
            float TickEase(float t, float interval, float spd)
            {
                float target = floor(t / interval) * spd;
                float prev   = floor((t - interval) / interval) * spd;
                float f = frac(t / interval);
                float ease = 1.0 - pow(1.0 - min(f * 3.0, 1.0), 3.0);
                return prev + (target - prev) * ease;
            }

            float3 Scene(float2 fragUV, float t)
            {
                float2 uv = (fragUV - 0.5) * _Zoom;
                float3 col = 0;

                float3 brass  = float3(0.77, 0.64, 0.31);
                float3 dark   = float3(0.35, 0.28, 0.18);

                float tickInt = 0.38;

                // arc config: radius, arc fraction, speed, opacity
                // unrolled for HLSL
                float arcR[6]   = { 0.3, 0.5, 0.7, 0.9, 1.1, 1.3 };
                float arcA[6]   = { 0.8, 0.55, 0.4, 0.6, 0.3, 0.45 };
                float arcSpd[6] = { 2.4, -1.618, 1.0, -0.75, 1.9, -1.2 };
                float arcOp[6]  = { 0.7, 0.55, 0.65, 0.5, 0.45, 0.35 };

                // concentric arcs with tick rotation
                [unroll] for (int i = 0; i < 6; i++)
                {
                    float rotation = TickEase(t, tickInt, arcSpd[i] * 0.11);
                    float aperture = arcA[i] * TAU;
                    float d = sdArc(uv, arcR[i], aperture, rotation);
                    col += GlowStroke(d, 0.006, brass * arcOp[i]);
                }

                // gear teeth
                [unroll] for (int g = 0; g < 36; g++)
                {
                    float a = float(g) / 36.0 * TAU;
                    float inner = 1.45;
                    float outer = inner + (fmod(float(g), 2.0) < 0.5 ? 0.06 : 0.03);
                    float2 p1 = float2(cos(a), sin(a)) * inner;
                    float2 p2 = float2(cos(a), sin(a)) * outer;
                    float d = sdLine(uv, p1, p2);
                    col += GlowStroke(d, 0.004, brass * 0.2);
                }

                // dial marks
                [unroll] for (int dm = 0; dm < 60; dm++)
                {
                    float a = float(dm) / 60.0 * TAU;
                    bool major = fmod(float(dm), 5.0) < 0.5;
                    float r1 = major ? 1.52 : 1.50;
                    float r2 = 1.58;
                    float2 p1 = float2(cos(a), sin(a)) * r1;
                    float2 p2 = float2(cos(a), sin(a)) * r2;
                    float d = sdLine(uv, p1, p2);
                    col += GlowStroke(d, 0.004, brass * (major ? 0.45 : 0.1));
                }

                // inner square
                float sq = sdBox(uv, 0.15);
                col += GlowStroke(sq, 0.005, brass * 0.35);

                // inner diamond
                float2 dUV = Rot(uv, PI * 0.25);
                float dmnd = sdBox(dUV, 0.09);
                col += GlowStroke(dmnd, 0.005, brass * 0.5);

                // radial spokes
                [unroll] for (int sp = 0; sp < 4; sp++)
                {
                    float a = float(sp) / 4.0 * TAU + PI / 8.0;
                    float2 p1 = float2(cos(a), sin(a)) * 0.2;
                    float2 p2 = float2(cos(a), sin(a)) * 0.65;
                    float d = sdLine(uv, p1, p2);
                    col += GlowStroke(d, 0.004, brass * 0.08);
                }

                // pendulum
                float pAngle = sin(t * 1.8) * 0.6 - PI * 0.5;
                float2 pendEnd = float2(cos(pAngle), sin(pAngle)) * 0.85;
                float pendD = sdLine(uv, float2(0, 0), pendEnd);
                col += GlowStroke(pendD, 0.005, dark * 1.8 * 0.35);
                // pendulum bob is a filled disc, keep the fill and add a halo
                float bob = sdCircle(uv - pendEnd, 0.03);
                col += brass * 0.4 * (1.0 - smoothstep(0.0, 0.008, bob));
                col += _BloomColor.rgb * _BloomIntensity * 0.4
                     * (1.0 - smoothstep(0.0, 0.008 + _BloomSpread, abs(bob)));

                // crosshair
                float chD = min(
                    sdLine(uv, float2(-0.08, 0), float2(0.08, 0)),
                    sdLine(uv, float2(0, -0.08), float2(0, 0.08))
                );
                col += GlowStroke(chD, 0.004, brass * 0.25);

                // center pivot is a filled disc, keep the fill and add a halo
                float piv = length(uv) - 0.0225;
                col += brass * 0.5 * (1.0 - smoothstep(-0.0025, 0.0025, piv));
                col += _BloomColor.rgb * _BloomIntensity * 0.5
                     * (1.0 - smoothstep(0.0, _BloomSpread, abs(piv)));

                return col;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float t = _Time.y;
                // Glow is baked into each stroke via GlowStroke (see _BloomColor/
                // _BloomIntensity/_BloomSpread) — no per-pixel scene re-sampling.
                float3 col = Scene(IN.uv, t);
                float alpha = max(max(col.r, col.g), col.b) > 0.01 ? 1.0 : _BgAlpha;
                return float4(col, alpha);
            }
            ENDHLSL
        }
    }
}
