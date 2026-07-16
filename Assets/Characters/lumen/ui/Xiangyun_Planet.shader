Shader "OC/Xiangyun_Planet"
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

            // ── helpers ──

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

            float sdEllipse(float2 p, float2 ab)
            {
                p = abs(p);
                if (p.x > p.y) { p = p.yx; ab = ab.yx; }
                float l = ab.y * ab.y - ab.x * ab.x;
                float m = ab.x * p.x / l;
                float n = ab.y * p.y / l;
                float m2 = m * m, n2 = n * n;
                float c = (m2 + n2 - 1.0) / 3.0;
                float c3 = c * c * c;
                float q = c3 + m2 * n2 * 2.0;
                float d = c3 + m2 * n2;
                float g = m + m * n2;
                float co;
                if (d < 0.0)
                {
                    float h = acos(q / c3) / 3.0;
                    float s = cos(h);
                    float t = sin(h) * sqrt(3.0);
                    float rx = sqrt(-c * (s + t + 2.0) + m2);
                    float ry = sqrt(-c * (s - t + 2.0) + m2);
                    co = (ry + sign(l) * rx + abs(g) / (rx * ry) - m) / 2.0;
                }
                else
                {
                    float h = 2.0 * m * n * sqrt(d);
                    float s = sign(q + h) * pow(abs(q + h), 1.0 / 3.0);
                    float u = sign(q - h) * pow(abs(q - h), 1.0 / 3.0);
                    float rx = -s - u - c * 4.0 + 2.0 * m2;
                    float ry = (s - u) * sqrt(3.0);
                    float rm = sqrt(rx * rx + ry * ry);
                    co = (ry / sqrt(rm - rx) + 2.0 * g / rm - m) / 2.0;
                }
                float2 r2 = ab * float2(co, sqrt(1.0 - co * co));
                return length(r2 - p) * sign(p.y - r2.y);
            }

            float Hash(float n)
            {
                return frac(sin(n) * 43758.5453);
            }

            #define PI  3.14159265
            #define TAU 6.28318530

            float3 Scene(float2 fragUV, float t)
            {
                float2 uv = (fragUV - 0.5) * _Zoom;
                float3 col = 0;

                // palette
                float3 w1 = float3(0.91, 0.51, 0.35);
                float3 w2 = float3(0.95, 0.75, 0.47);
                float3 w3 = float3(0.81, 0.31, 0.25);
                float3 w4 = float3(1.0, 0.94, 0.87);
                float3 w5 = float3(0.78, 0.38, 0.25);

                uv = Rot(uv, sin(t * 0.15) * 0.05);

                // planet
                float planet = sdCircle(uv, 0.55);
                col += GlowStroke(planet, 0.012, w1 * 0.7);
                col += GlowStroke(sdCircle(uv, 0.59), 0.015, w2 * 0.15);
                col += GlowStroke(sdCircle(uv, 0.64), 0.02, w4 * 0.07);

                // surface bands
                float inside = 1.0 - smoothstep(-0.01, 0.01, planet);
                float bandShift = sin(t * 0.3) * 0.02;
                [unroll] for (int i = 0; i < 6; i++)
                {
                    float by = -0.35 + float(i) * 0.13 + bandShift;
                    float halfW = sqrt(max(0.0, 0.55 * 0.55 - by * by));
                    float bandD = abs(uv.y - by - sin(uv.x * 8.0) * 0.02);
                    float bandMask = smoothstep(halfW, halfW - 0.02, abs(uv.x));
                    col += GlowStroke(bandD, 0.008, lerp(w5, w2, float(i) / 5.0) * bandMask * inside * 0.25);
                }

                // red spot
                float2 spotUV = uv - float2(0.18 + sin(t * 0.2) * 0.08, -0.1);
                float spotD = sdEllipse(spotUV, float2(0.12, 0.07));
                col += GlowStroke(spotD, 0.008, w3 * 0.4 * inside);

                // rings
                float ringTilt = 0.3;
                float behindPlanet = (planet < 0.0 && uv.y < 0.0) ? 0.3 : 1.0;

                [unroll] for (int j = 0; j < 8; j++)
                {
                    float r = 0.85 + float(j) * 0.07;
                    float2 ab = float2(r, r * ringTilt);
                    float d = abs(sdEllipse(uv, ab));
                    float op = (0.5 - float(j) * 0.04) + 0.05 * sin(t * 0.8 + r * 5.0);
                    float3 ringCol = j < 4 ? w1 : (j < 6 ? w2 : w4);
                    col += GlowStroke(d, 0.006, ringCol * op * behindPlanet);
                }

                // moons
                [unroll] for (int k = 0; k < 3; k++)
                {
                    float dist = 1.55 + float(k) * 0.2;
                    float spd = 0.4 - float(k) * 0.12;
                    float tiltM = 0.25 + float(k) * 0.05;
                    float a = t * spd + float(k) * 2.1;
                    float2 mp = float2(dist * cos(a), dist * tiltM * sin(a));
                    float sz = 0.035 - float(k) * 0.005;
                    float crossD = min(
                        sdLine(uv, mp - float2(sz, 0), mp + float2(sz, 0)),
                        sdLine(uv, mp - float2(0, sz), mp + float2(0, sz))
                    );
                    col += GlowStroke(crossD, 0.005, w4 * 0.6);

                    float orbitD = abs(sdEllipse(uv, float2(dist, dist * tiltM)));
                    col += GlowStroke(orbitD, 0.004, w2 * 0.06);
                }

                // debris
                [unroll] for (int m = 0; m < 20; m++)
                {
                    float fm = float(m);
                    float a = Hash(fm) * TAU + t * (0.15 + Hash(fm + 99.0) * 0.15);
                    float r = 0.82 + Hash(fm + 50.0) * 0.55;
                    float2 dp = float2(r * cos(a), r * ringTilt * sin(a));
                    float d = length(uv - dp);
                    col += GlowStroke(d, 0.006, w4 * (0.15 + Hash(fm + 77.0) * 0.15) * behindPlanet);
                }

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
