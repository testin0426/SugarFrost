Shader "OC/Qianhong_Snowflake"
{
    Properties
    {
        // Required so Unity UI (Image/RawImage) can bind its sprite; unused by the shader.
        [HideInInspector] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Zoom ("Zoom", Float) = 2.8
        _BgAlpha ("Background Alpha", Range(0, 1)) = 1.0
        [HDR] _BloomColor ("Bloom Color", Color) = (1, 1, 1, 1)
        _BloomIntensity ("Bloom Intensity", Range(0, 4)) = 1.2
        _BloomSpread ("Glow Width", Range(0, 0.1)) = 0.02
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

            float Stroke(float d, float w)
            {
                return 1.0 - smoothstep(0.0, w, abs(d));
            }

            // Sharp core stroke plus a wider, soft halo around the same SDF —
            // this added width is what reads as bloom on thin line art.
            float3 GlowStroke(float d, float w, float3 c)
            {
                float core = 1.0 - smoothstep(0.0, w, abs(d));
                float halo = 1.0 - smoothstep(0.0, w + _BloomSpread, abs(d));
                return c * core + c * _BloomColor.rgb * halo * halo * _BloomIntensity * 0.4;
            }

            float sdLine(float2 p, float2 a, float2 b)
            {
                float2 pa = p - a, ba = b - a;
                float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
                return length(pa - ba * h);
            }

            // hexagon SDF
            float sdHex(float2 p, float r)
            {
                p = abs(p);
                float d = dot(p, normalize(float2(1.0, 1.732)));
                return max(d, p.x) - r;
            }

            // diamond SDF
            float sdDiamond(float2 p, float r)
            {
                p = abs(p);
                return dot(p, float2(0.707, 0.707)) - r;
            }

            float SStep(float t)
            {
                float c = saturate(t);
                return c * c * (3.0 - 2.0 * c);
            }

            float Hash(float n)
            {
                return frac(sin(n) * 43758.5453);
            }

            // draw a branch: line from startR to endR along angle, with growth 0→1
            float Branch(float2 uv, float angle, float startR, float endR, float growth)
            {
                float actualEnd = startR + (endR - startR) * growth;
                float2 a = float2(cos(angle), sin(angle)) * startR;
                float2 b = float2(cos(angle), sin(angle)) * actualEnd;
                return sdLine(uv, a, b);
            }

            float3 Scene(float2 fragUV, float t)
            {
                float2 uv = (fragUV - 0.5) * _Zoom;
                float3 col = 0;

                float3 ice1 = float3(0.55, 0.72, 0.82);
                float3 ice2 = float3(0.63, 0.80, 0.88);
                float3 ice3 = float3(0.72, 0.87, 0.91);
                float3 ice4 = float3(0.85, 0.92, 0.94);

                // slow rotation
                uv = Rot(uv, t * 0.04);

                // growth cycle
                float cycle = fmod(t * 0.22, 4.5);

                // ── 6-fold branches ──
                [loop] for (int arm = 0; arm < 6; arm++)
                {
                    float baseAngle = float(arm) / 6.0 * TAU;

                    // main branch (depth 0)
                    float g0 = cycle < 2.8
                        ? SStep(cycle / 0.6)
                        : SStep(2.8 / 0.6) * max(0.0, 1.0 - (cycle - 2.8) / 1.7);

                    float d0 = Branch(uv, baseAngle, 0.06, 1.15, g0);
                    col += GlowStroke(d0, 0.006, ice1) * g0 * 0.8;

                    // sub-branches at two depths
                    [unroll] for (int sub = 1; sub <= 2; sub++)
                    {
                        float parentR = 0.06 + (1.15 - 0.06) * (0.3 + float(sub) * 0.2);
                        float gDelay = float(sub) * 0.7 + Hash(float(arm * 10 + sub)) * 0.2;
                        float gDur = 0.5 + Hash(float(arm * 10 + sub + 50)) * 0.3;

                        float g = cycle < 2.8
                            ? SStep((cycle - gDelay) / gDur)
                            : SStep((2.8 - gDelay) / gDur) * max(0.0, 1.0 - (cycle - 2.8) / 1.7);

                        [unroll] for (int side = 0; side < 2; side++)
                        {
                            float subAngle = baseAngle + (side == 0 ? 0.52 : -0.52);
                            float2 origin = float2(cos(baseAngle), sin(baseAngle)) * parentR;
                            float subLen = 0.35 * g;
                            float2 tip = origin + float2(cos(subAngle), sin(subAngle)) * subLen;
                            float d = sdLine(uv, origin, tip);
                            float3 subCol = sub == 1 ? ice2 : ice3;
                            col += GlowStroke(d, 0.005, subCol) * g * (0.65 - float(sub) * 0.1);
                        }
                    }

                    // tip diamond
                    float tipShow = (cycle > 1.5 && cycle < 3.5)
                        ? SStep((cycle - 1.5) / 0.5) : 0.0;
                    if (cycle >= 3.0) tipShow *= max(0.0, 1.0 - (cycle - 3.0));
                    float2 tipPos = float2(cos(baseAngle), sin(baseAngle)) * 1.2 * g0;
                    float tipD = sdDiamond(uv - tipPos, 0.05);
                    col += GlowStroke(tipD, 0.006, ice1) * tipShow * 0.5;
                }

                // ── hexagonal halos ──
                float hexR[4] = { 0.35, 0.7, 1.05, 1.4 };
                [unroll] for (int hi = 0; hi < 4; hi++)
                {
                    float delay = 0.5 + float(hi) * 0.5;
                    float op = cycle < 2.8
                        ? SStep((cycle - delay) / 0.6) * 0.18
                        : 0.18 * max(0.0, 1.0 - (cycle - 2.8) / 1.2);
                    float d = abs(sdHex(uv, hexR[hi]));
                    col += GlowStroke(d, 0.005, ice4) * op;
                }

                // center hexagon
                float cD = abs(sdHex(uv, 0.08));
                col += GlowStroke(cD, 0.005, ice1) * 0.5;

                // satellite crosses
                [loop] for (int si = 0; si < 12; si++)
                {
                    float fi = float(si);
                    float a = fi / 12.0 * TAU + PI / 12.0;
                    float r = 0.9 + Hash(fi + 30.0) * 0.5;
                    float2 sp = float2(cos(a), sin(a)) * r;
                    float twinkle = sin(t * 2.0 + fi * 1.3) * 0.5 + 0.5;
                    float show = (cycle > 0.8 && cycle < 3.8) ? 0.25 : 0.0;
                    float sz = 0.025;
                    float crossD = min(
                        sdLine(uv, sp - float2(sz, 0), sp + float2(sz, 0)),
                        sdLine(uv, sp - float2(0, sz), sp + float2(0, sz))
                    );
                    col += GlowStroke(crossD, 0.004, ice2) * show * twinkle;
                }

                return col;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float3 col = Scene(IN.uv, _Time.y);
                float alpha = max(max(col.r, col.g), col.b) > 0.01 ? 1.0 : _BgAlpha;
                return float4(col, alpha);
            }
            ENDHLSL
        }
    }
}
