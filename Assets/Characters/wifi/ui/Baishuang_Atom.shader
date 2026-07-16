Shader "OC/Baishuang_Atom"
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

            #define PI  3.14159265
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

            float Hash(float n)
            {
                return frac(sin(n) * 43758.5453);
            }

            // parametric point on rotated ellipse
            float2 EllipsePoint(float a, float b, float angle, float rotation)
            {
                float2 p = float2(a * cos(angle), b * sin(angle));
                return Rot(p, rotation);
            }

            // approximate ellipse ring SDF
            float sdEllipseRing(float2 p, float a, float b, float rotation)
            {
                p = Rot(p, -rotation);
                float ang = atan2(p.y * a, p.x * b);
                float2 nearest = float2(a * cos(ang), b * sin(ang));
                return length(p - nearest);
            }

            float3 Scene(float2 fragUV, float t)
            {
                float2 uv = (fragUV - 0.5) * _Zoom;
                float3 col = 0;

                float3 c1 = float3(0.58, 0.68, 0.70); // cool grey-blue
                float3 c2 = float3(0.75, 0.83, 0.85);
                float3 c3 = float3(0.69, 0.78, 0.82);
                float3 c4 = float3(0.48, 0.62, 0.66);

                // very slow drift
                uv = Rot(uv, t * 0.005);

                // global breath
                float breath = 0.55 + 0.45 * sin(t * 0.3);

                // orbit config
                float oA[4]   = { 0.35, 0.6,  0.85, 1.1   };
                float oB[4]   = { 0.2,  0.25, 0.4,  0.35  };
                float oR[4]   = { 0.0,  0.9,  2.1, -0.5   };
                float oSpd[4] = { 1.4, -0.9,  0.6, -0.45  };
                float oOp[4]  = { 0.35, 0.3, 0.25, 0.2    };

                // elliptical orbits + particles + trails
                [unroll] for (int i = 0; i < 4; i++)
                {
                    // orbit path
                    float d = sdEllipseRing(uv, oA[i], oB[i], oR[i]);
                    col += GlowStroke(d, 0.005, c1 * oOp[i] * breath);

                    // orbiting particle
                    float angle = t * oSpd[i] * 0.5 + float(i) * 1.7;
                    float2 pp = EllipsePoint(oA[i], oB[i], angle, oR[i]);
                    float sz = 0.035;
                    float crossD = min(
                        sdLine(uv, pp - float2(sz, 0), pp + float2(sz, 0)),
                        sdLine(uv, pp - float2(0, sz), pp + float2(0, sz))
                    );
                    col += GlowStroke(crossD, 0.005, c2 * 0.7 * breath);

                    // trail dots
                    [unroll] for (int j = 1; j <= 5; j++)
                    {
                        float trailAngle = angle - float(j) * 0.08 * sign(oSpd[i]);
                        float2 tp = EllipsePoint(oA[i], oB[i], trailAngle, oR[i]);
                        float td = length(uv - tp);
                        float trailOp = (1.0 - float(j) / 6.0) * 0.2;
                        col += GlowStroke(td, 0.008, c4 * trailOp * breath);
                    }
                }

                // water ripple rings
                [unroll] for (int ri = 0; ri < 6; ri++)
                {
                    float phase = float(ri) / 6.0 * TAU;
                    float p = fmod(t * 0.5 + phase, TAU) / TAU;
                    float r = 0.05 + p * 1.6;
                    float d = abs(sdCircle(uv, r));
                    float fadeIn  = smoothstep(0.0, 0.08, p);
                    float fadeOut = 1.0 - smoothstep(0.08, 1.0, p);
                    col += GlowStroke(d, 0.004, c1 * fadeIn * fadeOut * 0.2 * breath);
                }

                // nucleus circles
                col += GlowStroke(sdCircle(uv, 0.06), 0.005, c3 * 0.4 * breath);
                col += GlowStroke(sdCircle(uv, 0.10), 0.004, c3 * 0.3 * breath);

                // nucleus crosshair
                float chSz = 0.14;
                float chD = min(
                    sdLine(uv, float2(-chSz, 0), float2(chSz, 0)),
                    sdLine(uv, float2(0, -chSz), float2(0, chSz))
                );
                col += GlowStroke(chD, 0.004, c1 * 0.2 * breath);

                // radial dashed field lines
                [unroll] for (int fi = 0; fi < 8; fi++)
                {
                    float a = float(fi) / 8.0 * TAU;
                    float2 dir = float2(cos(a), sin(a));
                    float r = 0.18;
                    [unroll] for (int di = 0; di < 4; di++)
                    {
                        float len = 0.05 + Hash(float(fi * 10 + di)) * 0.05;
                        float2 p1 = dir * r;
                        float2 p2 = dir * (r + len);
                        float ld = sdLine(uv, p1, p2);
                        col += GlowStroke(ld, 0.004, c1 * 0.07 * breath);
                        r += len + 0.08 + Hash(float(fi * 10 + di + 99)) * 0.1;
                    }
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
