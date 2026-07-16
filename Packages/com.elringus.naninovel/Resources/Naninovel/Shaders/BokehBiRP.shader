Shader "Naninovel/FX/BokehBiRP"
{
    Properties
    {
        _MainTex("", 2D) = ""{}
        _BlurTex("", 2D) = ""{}
    }

    Subshader
    {
        CGINCLUDE
        #include "UnityCG.cginc"

        UNITY_DECLARE_TEX2D(_MainTex);
        float4 _MainTex_TexelSize;
        UNITY_DECLARE_TEX2D(_BlurTex);
        UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

        float _Distance;
        float _LensCoeff;
        float _MaxCoC;
        float _RcpMaxCoC;
        float _RcpAspect;

        static const int ITERATION_COUNT = 43;
        static const float2 ITERATIONS[ITERATION_COUNT] = {
            float2(0, 0),
            float2(0.36363637, 0),
            float2(0.22672357, 0.28430238),
            float2(-0.08091671, 0.35451925),
            float2(-0.32762504, 0.15777594),
            float2(-0.32762504, -0.15777591),
            float2(-0.08091656, -0.35451928),
            float2(0.22672352, -0.2843024),
            float2(0.6818182, 0),
            float2(0.614297, 0.29582983),
            float2(0.42510667, 0.5330669),
            float2(0.15171885, 0.6647236),
            float2(-0.15171883, 0.6647236),
            float2(-0.4251068, 0.53306687),
            float2(-0.614297, 0.29582986),
            float2(-0.6818182, 0),
            float2(-0.614297, -0.29582983),
            float2(-0.42510656, -0.53306705),
            float2(-0.15171856, -0.66472363),
            float2(0.1517192, -0.6647235),
            float2(0.4251066, -0.53306705),
            float2(0.614297, -0.29582983),
            float2(1, 0),
            float2(0.9555728, 0.2947552),
            float2(0.82623875, 0.5633201),
            float2(0.6234898, 0.7818315),
            float2(0.36534098, 0.93087375),
            float2(0.07473, 0.9972038),
            float2(-0.22252095, 0.9749279),
            float2(-0.50000006, 0.8660254),
            float2(-0.73305196, 0.6801727),
            float2(-0.90096885, 0.43388382),
            float2(-0.98883086, 0.14904208),
            float2(-0.9888308, -0.14904249),
            float2(-0.90096885, -0.43388376),
            float2(-0.73305184, -0.6801728),
            float2(-0.4999999, -0.86602545),
            float2(-0.222521, -0.9749279),
            float2(0.07473029, -0.99720377),
            float2(0.36534148, -0.9308736),
            float2(0.6234897, -0.7818316),
            float2(0.8262388, -0.56332),
            float2(0.9555729, -0.29475483),
        };

        struct Vertex
        {
            float4 pos : SV_POSITION;
            float2 uv0 : TEXCOORD0;
            float2 uv1 : TEXCOORD1;
        };

        Vertex ComputeVertex(appdata_img app)
        {
            float2 uv1 = app.texcoord;
            #if UNITY_UV_STARTS_AT_TOP
            if (_MainTex_TexelSize.y < 0.0) uv1.y = 1 - uv1.y;
            #endif

            Vertex o;
            o.pos = UnityObjectToClipPos(app.vertex);
            o.uv0 = app.texcoord;
            o.uv1 = uv1;
            return o;
        }

        float Max3(float3 xyz)
        {
            return max(xyz.x, max(xyz.y, xyz.z));
        }

        float OrthoEyeDepth(float rawDepth)
        {
            // Corrected LinearEyeDepth() to support ortho camera mode.
            float persp = LinearEyeDepth(rawDepth);
            #if defined(UNITY_REVERSED_Z)
            float depth = 1 - rawDepth;
            #else
            float depth = rawDepth;
            #endif
            float ortho = (_ProjectionParams.z - _ProjectionParams.y) * depth + _ProjectionParams.y;
            return lerp(persp, ortho, unity_OrthoParams.w);
        }

        float4 Prefilter(Vertex v) : SV_Target
        {
            // Sample source colors
            float3 duv = _MainTex_TexelSize.xyx * float3(0.5, 0.5, -0.5);
            float3 c0 = UNITY_SAMPLE_TEX2D(_MainTex, v.uv0 - duv.xy).rgb;
            float3 c1 = UNITY_SAMPLE_TEX2D(_MainTex, v.uv0 - duv.zy).rgb;
            float3 c2 = UNITY_SAMPLE_TEX2D(_MainTex, v.uv0 + duv.zy).rgb;
            float3 c3 = UNITY_SAMPLE_TEX2D(_MainTex, v.uv0 + duv.xy).rgb;

            // Sample linear depths
            float d0 = OrthoEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, v.uv1 - duv.xy));
            float d1 = OrthoEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, v.uv1 - duv.zy));
            float d2 = OrthoEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, v.uv1 + duv.zy));
            float d3 = OrthoEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, v.uv1 + duv.xy));
            float4 depths = float4(d0, d1, d2, d3);

            // Calculate the radiuses of CoCs at these sample points
            float4 cocs = (depths - _Distance) * _LensCoeff / depths;
            cocs = clamp(cocs, -_MaxCoC, _MaxCoC);

            // Premultiply CoC to reduce background bleeding
            float4 weights = saturate(abs(cocs) * _RcpMaxCoC);

            #if defined(PREFILTER_LUMA_WEIGHT)
            // Apply luma weights to reduce flickering
            weights.x *= 1 / (Max3(c0) + 1);
            weights.y *= 1 / (Max3(c1) + 1);
            weights.z *= 1 / (Max3(c2) + 1);
            weights.w *= 1 / (Max3(c3) + 1);
            #endif

            // Weighted average of the color samples
            float3 avg = c0 * weights.x + c1 * weights.y + c2 * weights.z + c3 * weights.w;
            avg /= dot(weights, 1);

            // Output CoC = average of CoCs
            float coc = dot(cocs, 0.25);

            // Premultiply CoC again.
            avg *= smoothstep(0, _MainTex_TexelSize.y * 2, abs(coc));

            #if defined(UNITY_COLORSPACE_GAMMA)
            avg = GammaToLinearSpace(avg);
            #endif

            return float4(avg, coc);
        }

        float4 BlurDisk(Vertex v) : SV_Target
        {
            float4 samp0 = UNITY_SAMPLE_TEX2D(_MainTex, v.uv0);

            float4 bgAcc = 0; // Background: far field bokeh
            float4 fgAcc = 0; // Foreground: near field bokeh

            UNITY_LOOP for (int si = 0; si < ITERATION_COUNT; si++)
            {
                float2 disp = ITERATIONS[si] * _MaxCoC;
                float dist = length(disp);

                float2 duv = float2(disp.x * _RcpAspect, disp.y);
                float4 samp = UNITY_SAMPLE_TEX2D(_MainTex, v.uv0 + duv);

                // BG: Compare CoC of the current sample and the center sample and select smaller one.
                float bgCoC = max(min(samp0.a, samp.a), 0);

                // Compare the CoC to the sample distance.
                // Add a small margin to smooth out.
                const float margin = _MainTex_TexelSize.y * 2;
                float bgWeight = saturate((bgCoC - dist + margin) / margin);
                float fgWeight = saturate((-samp.a - dist + margin) / margin);

                // Cut influence from focused areas because they're darkened by CoC
                // premultiplying. This is only needed for near field.
                fgWeight *= step(_MainTex_TexelSize.y, -samp.a);

                // Accumulation
                bgAcc += float4(samp.rgb, 1) * bgWeight;
                fgAcc += float4(samp.rgb, 1) * fgWeight;
            }

            // Get the weighted average.
            bgAcc.rgb /= bgAcc.a + (bgAcc.a == 0);
            fgAcc.rgb /= fgAcc.a + (fgAcc.a == 0);

            // BG: Calculate the alpha value only based on the center CoC.
            // This is a rather aggressive approximation but provides stable results.
            bgAcc.a = smoothstep(_MainTex_TexelSize.y, _MainTex_TexelSize.y * 2, samp0.a);

            // FG: Normalize the total of the weights.
            fgAcc.a *= UNITY_PI / ITERATION_COUNT;

            // Alpha premultiplying
            float3 rgb = 0;
            rgb = lerp(rgb, bgAcc.rgb, saturate(bgAcc.a));
            rgb = lerp(rgb, fgAcc.rgb, saturate(fgAcc.a));

            // Combined alpha value
            float alpha = (1 - saturate(bgAcc.a)) * (1 - saturate(fgAcc.a));

            return float4(rgb, alpha);
        }

        float4 BlurFinal(Vertex v) : SV_Target
        {
            float4 duv = _MainTex_TexelSize.xyxy * float4(1, 1, -1, 0);

            float4 acc = UNITY_SAMPLE_TEX2D(_MainTex, v.uv0 - duv.xy);
            acc += UNITY_SAMPLE_TEX2D(_MainTex, v.uv0 - duv.wy) * 2;
            acc += UNITY_SAMPLE_TEX2D(_MainTex, v.uv0 - duv.zy);

            acc += UNITY_SAMPLE_TEX2D(_MainTex, v.uv0 + duv.zw) * 2;
            acc += UNITY_SAMPLE_TEX2D(_MainTex, v.uv0) * 4;
            acc += UNITY_SAMPLE_TEX2D(_MainTex, v.uv0 + duv.xw) * 2;

            acc += UNITY_SAMPLE_TEX2D(_MainTex, v.uv0 + duv.zy);
            acc += UNITY_SAMPLE_TEX2D(_MainTex, v.uv0 + duv.wy) * 2;
            acc += UNITY_SAMPLE_TEX2D(_MainTex, v.uv0 + duv.xy);

            return acc / 16;
        }

        // Fragment shader: Upsampling and composition
        float4 Compose(Vertex i) : SV_Target
        {
            float4 cs = UNITY_SAMPLE_TEX2D(_MainTex, i.uv0);
            float4 cb = UNITY_SAMPLE_TEX2D(_BlurTex, i.uv1);
            #if defined(UNITY_COLORSPACE_GAMMA)
            cs.rgb = GammaToLinearSpace(cs.rgb);
            #endif
            float3 rgb = cs * cb.a + cb.rgb;
            #if defined(UNITY_COLORSPACE_GAMMA)
            rgb = LinearToGammaSpace(rgb);
            #endif

            return float4(rgb, cs.a);
        }
        ENDCG

        Pass
        {
            CGPROGRAM
            #pragma vertex ComputeVertex
            #pragma fragment Prefilter
            #pragma multi_compile _ UNITY_COLORSPACE_GAMMA
            #define PREFILTER_LUMA_WEIGHT
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex ComputeVertex
            #pragma fragment BlurDisk
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex ComputeVertex
            #pragma fragment BlurFinal
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex ComputeVertex
            #pragma multi_compile _ UNITY_COLORSPACE_GAMMA
            #pragma fragment Compose
            ENDCG
        }
    }
}
