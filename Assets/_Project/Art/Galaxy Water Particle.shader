Shader "Galaxy Water Particle"
{
    Properties
    {
        _Texture("Texture", 2D) = "white" {}
        [HDR] _Colour("Colour", Color) = (1, 1, 1, 1)
        _AlphaClipThreshold("Alpha Clip Threshold", Range(0, 1)) = 0

        _NoiseScale("Noise Scale", Float) = 2
        _NoiseTiling("Noise Tiling", Vector) = (1, 1, 1, 0)
        _NoiseAnimation("Noise Animation", Vector) = (0, 0, 0, 0)
        _NoiseSmoothness("Noise Smoothness", Range(0, 1)) = 0
        [HDR] _NoiseColour("Noise Colour", Color) = (1, 1, 1, 1)
        _NoiseColourPower("Noise Colour Power", Float) = 1
        _NoiseVertexOffset1("Noise Vertex Offset", Float) = 0
        _NoiseVertexOffsetPower1("Noise Vertex Offset Power", Float) = 1

        _Amplitude("Amplitude", Float) = 1
        _Frequency("Frequency", Float) = 2
        _Steepness("Steepness", Float) = 0.5
        _Animation("Animation", Vector) = (0, 0, 0, 0)
        _Rotation("Rotation", Range(-180, 180)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "UniversalMaterialType" = "Unlit"
        }

        Cull Back
        AlphaToMask Off

        // ─────────────────────────────────────────────
        // Forward pass
        // ─────────────────────────────────────────────
        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForwardOnly" }

            Blend One Zero
            ZWrite On
            ZTest LEqual
            ColorMask RGBA

            HLSLPROGRAM
            #pragma target 4.5

            #define _ALPHATEST_ON

            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_instancing
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_fog

            #pragma vertex vert
            #pragma fragment frag

            #define SHADERPASS SHADERPASS_UNLIT

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

            #if defined(LOD_FADE_CROSSFADE)
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

            #include "Functions.hlsl"

            // ── Voronoi helpers ──

            float2 voronoihash(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453);
            }

            float voronoi(float2 v, float time, inout float2 id, inout float2 mr,
                           float smoothness, inout float2 smoothId)
            {
                float2 n = floor(v);
                float2 f = frac(v);
                float F1 = 8.0;
                float2 mg = 0;
                int i, j;

                for (j = -1; j <= 1; j++)
                {
                    for (i = -1; i <= 1; i++)
                    {
                        float2 g = float2(i, j);
                        float2 o = voronoihash(n + g);
                        o = (sin(time + o * 6.2831) * 0.5 + 0.5);
                        float2 r = f - g - o;
                        float d = 0.5 * dot(r, r);
                        float h = smoothstep(0.0, 1.0, 0.5 + 0.5 * (F1 - d) / smoothness);
                        F1 = lerp(F1, d, h) - smoothness * h * (1.0 - h);
                        mg = g; mr = r; id = o;
                    }
                }
                return F1;
            }

            // Two-octave FBM voronoi.
            float VoronoiFBM(float3 noiseUV, float smoothness)
            {
                float time  = noiseUV.z;
                float2 smoothId = 0;
                float2 coords = noiseUV.xy;
                float2 id = 0;
                float2 uv = 0;
                float fade = 0.5;
                float voroi = 0;
                float rest  = 0;

                for (int it = 0; it < 2; it++)
                {
                    voroi += fade * voronoi(coords, time, id, uv, smoothness, smoothId);
                    rest  += fade;
                    coords *= 2;
                    fade  *= 0.5;
                }
                return saturate(voroi / rest);
            }

            // ── Material properties ──

            CBUFFER_START(UnityPerMaterial)
                float4 _Colour;
                float4 _NoiseColour;
                float4 _Texture_ST;
                float3 _NoiseTiling;
                float3 _NoiseAnimation;
                float3 _Animation;
                float  _NoiseScale;
                float  _NoiseSmoothness;
                float  _NoiseVertexOffsetPower1;
                float  _NoiseVertexOffset1;
                float  _Amplitude;
                float  _Frequency;
                float  _Steepness;
                float  _Rotation;
                float  _NoiseColourPower;
                float  _AlphaClipThreshold;
            CBUFFER_END

            sampler2D _Texture;

            // ── Shared computation ──

            float3 ComputeNoiseUV(float3 particleCenterXZY)
            {
                return ((particleCenterXZY * _NoiseScale) * _NoiseTiling)
                     - (_NoiseAnimation * _TimeParameters.x);
            }

            float ComputeNoise(float3 noiseUV)
            {
                return VoronoiFBM(noiseUV, _NoiseSmoothness);
            }

            float3 ComputeVertexOffset(float noise, float3 particleCenter, float3 objectPos)
            {
                float noiseOffset = pow(noise, _NoiseVertexOffsetPower1) * _NoiseVertexOffset1;

                float3 normalTS;
                float3 wave = GerstnerWave(
                    particleCenter, objectPos,
                    _Amplitude, _Frequency,
                    _Steepness, _Animation,
                    _Rotation, normalTS);

                return float3(0, 1, 0) * noiseOffset + wave;
            }

            // ── Structs ──

            struct Attributes
            {
                float4 positionOS  : POSITION;
                half3  normalOS    : NORMAL;
                float4 texcoord0   : TEXCOORD0;
                float4 texcoord1   : TEXCOORD1;
                float4 color       : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS          : SV_POSITION;
                float4 positionWSAndFog    : TEXCOORD0;
                half3  normalWS            : TEXCOORD1;
                float4 texcoord0           : TEXCOORD2;
                float4 texcoord1           : TEXCOORD3;
                float4 color               : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ── Vertex ──

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // Particle center packed in texcoord0.zw + texcoord1.x.
                float3 particleCenter    = float3(input.texcoord0.z, input.texcoord0.w, input.texcoord1.x);
                float3 particleCenterXZY = particleCenter.xzy;

                float3 noiseUV = ComputeNoiseUV(particleCenterXZY);
                float  noise   = ComputeNoise(noiseUV);

                float3 objectPos     = GetAbsolutePositionWS(UNITY_MATRIX_M._m03_m13_m23);
                float3 vertexOffset  = ComputeVertexOffset(noise, particleCenter, objectPos);

                input.positionOS.xyz += vertexOffset;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs   normalInput = GetVertexNormalInputs(input.normalOS);

                float fogFactor = ComputeFogFactor(vertexInput.positionCS.z);

                output.positionCS       = vertexInput.positionCS;
                output.positionWSAndFog = float4(vertexInput.positionWS, fogFactor);
                output.normalWS         = normalInput.normalWS;
                output.texcoord0        = input.texcoord0;
                output.texcoord1        = input.texcoord1;
                output.color            = input.color;

                return output;
            }

            // ── Fragment ──

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                #if defined(LOD_FADE_CROSSFADE)
                    LODFadeCrossFade(input.positionCS);
                #endif

                // Rebuild particle center + noise.
                float3 particleCenter    = float3(input.texcoord0.z, input.texcoord0.w, input.texcoord1.x);
                float3 particleCenterXZY = particleCenter.xzy;

                float3 noiseUV = ComputeNoiseUV(particleCenterXZY);
                float  noise   = ComputeNoise(noiseUV);

                // Colour blending driven by noise.
                float  noisePow  = smoothstep(0.0, 1.0, pow(noise, _NoiseColourPower));
                float4 blended   = lerp(_Colour, _NoiseColour, noisePow);

                // Sample texture.
                float2 uv      = input.texcoord0.xy * _Texture_ST.xy + _Texture_ST.zw;
                float4 texColor = tex2D(_Texture, uv);

                float4 finalColour = blended * input.color * texColor;

                // Alpha clip.
                float alpha = finalColour.a;
                clip(alpha - _AlphaClipThreshold);

                // Fog.
                finalColour.rgb = MixFog(finalColour.rgb, input.positionWSAndFog.w);

                return half4(finalColour.rgb, alpha);
            }
            ENDHLSL
        }

        // ─────────────────────────────────────────────
        // Shadow caster pass
        // ─────────────────────────────────────────────
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            AlphaToMask Off
            ColorMask 0

            HLSLPROGRAM
            #pragma target 4.5

            #define _ALPHATEST_ON

            #pragma multi_compile_instancing
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #pragma vertex vert
            #pragma fragment frag

            #define SHADERPASS SHADERPASS_SHADOWCASTER

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

            #if defined(LOD_FADE_CROSSFADE)
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

            #include "Functions.hlsl"

            // ── Voronoi (duplicated per pass — HLSL has no cross-pass linking) ──

            float2 voronoihash(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453);
            }

            float voronoi(float2 v, float time, inout float2 id, inout float2 mr,
                           float smoothness, inout float2 smoothId)
            {
                float2 n = floor(v);
                float2 f = frac(v);
                float F1 = 8.0;
                float2 mg = 0;
                int i, j;

                for (j = -1; j <= 1; j++)
                {
                    for (i = -1; i <= 1; i++)
                    {
                        float2 g = float2(i, j);
                        float2 o = voronoihash(n + g);
                        o = (sin(time + o * 6.2831) * 0.5 + 0.5);
                        float2 r = f - g - o;
                        float d = 0.5 * dot(r, r);
                        float h = smoothstep(0.0, 1.0, 0.5 + 0.5 * (F1 - d) / smoothness);
                        F1 = lerp(F1, d, h) - smoothness * h * (1.0 - h);
                        mg = g; mr = r; id = o;
                    }
                }
                return F1;
            }

            float VoronoiFBM(float3 noiseUV, float smoothness)
            {
                float time  = noiseUV.z;
                float2 smoothId = 0;
                float2 coords = noiseUV.xy;
                float2 id = 0;
                float2 uv = 0;
                float fade = 0.5;
                float voroi = 0;
                float rest  = 0;

                for (int it = 0; it < 2; it++)
                {
                    voroi += fade * voronoi(coords, time, id, uv, smoothness, smoothId);
                    rest  += fade;
                    coords *= 2;
                    fade  *= 0.5;
                }
                return saturate(voroi / rest);
            }

            CBUFFER_START(UnityPerMaterial)
                float4 _Colour;
                float4 _NoiseColour;
                float4 _Texture_ST;
                float3 _NoiseTiling;
                float3 _NoiseAnimation;
                float3 _Animation;
                float  _NoiseScale;
                float  _NoiseSmoothness;
                float  _NoiseVertexOffsetPower1;
                float  _NoiseVertexOffset1;
                float  _Amplitude;
                float  _Frequency;
                float  _Steepness;
                float  _Rotation;
                float  _NoiseColourPower;
                float  _AlphaClipThreshold;
            CBUFFER_END

            sampler2D _Texture;

            float3 _LightDirection;
            float3 _LightPosition;

            float3 ComputeNoiseUV(float3 particleCenterXZY)
            {
                return ((particleCenterXZY * _NoiseScale) * _NoiseTiling)
                     - (_NoiseAnimation * _TimeParameters.x);
            }

            float ComputeNoise(float3 noiseUV)
            {
                return VoronoiFBM(noiseUV, _NoiseSmoothness);
            }

            float3 ComputeVertexOffset(float noise, float3 particleCenter, float3 objectPos)
            {
                float noiseOffset = pow(noise, _NoiseVertexOffsetPower1) * _NoiseVertexOffset1;

                float3 normalTS;
                float3 wave = GerstnerWave(
                    particleCenter, objectPos,
                    _Amplitude, _Frequency,
                    _Steepness, _Animation,
                    _Rotation, normalTS);

                return float3(0, 1, 0) * noiseOffset + wave;
            }

            struct Attributes
            {
                float4 positionOS  : POSITION;
                half3  normalOS    : NORMAL;
                float4 texcoord0   : TEXCOORD0;
                float4 texcoord1   : TEXCOORD1;
                float4 color       : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 texcoord0  : TEXCOORD0;
                float4 texcoord1  : TEXCOORD1;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4 GetShadowPositionHClip(Attributes input, float3 normalWS)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                return positionCS;
            }

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 particleCenter    = float3(input.texcoord0.z, input.texcoord0.w, input.texcoord1.x);
                float3 particleCenterXZY = particleCenter.xzy;

                float3 noiseUV = ComputeNoiseUV(particleCenterXZY);
                float  noise   = ComputeNoise(noiseUV);

                float3 objectPos    = GetAbsolutePositionWS(UNITY_MATRIX_M._m03_m13_m23);
                float3 vertexOffset = ComputeVertexOffset(noise, particleCenter, objectPos);

                input.positionOS.xyz += vertexOffset;

                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                output.positionCS = GetShadowPositionHClip(input, normalInput.normalWS);
                output.texcoord0  = input.texcoord0;
                output.texcoord1  = input.texcoord1;
                output.color      = input.color;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                #if defined(LOD_FADE_CROSSFADE)
                    LODFadeCrossFade(input.positionCS);
                #endif

                float3 particleCenter    = float3(input.texcoord0.z, input.texcoord0.w, input.texcoord1.x);
                float3 particleCenterXZY = particleCenter.xzy;

                float3 noiseUV = ComputeNoiseUV(particleCenterXZY);
                float  noise   = ComputeNoise(noiseUV);

                float  noisePow  = smoothstep(0.0, 1.0, pow(noise, _NoiseColourPower));
                float4 blended   = lerp(_Colour, _NoiseColour, noisePow);

                float2 uv      = input.texcoord0.xy * _Texture_ST.xy + _Texture_ST.zw;
                float4 texColor = tex2D(_Texture, uv);

                float alpha = (blended * input.color * texColor).a;
                clip(alpha - _AlphaClipThreshold);

                return 0;
            }
            ENDHLSL
        }

        // ─────────────────────────────────────────────
        // Depth only pass
        // ─────────────────────────────────────────────
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 4.5

            #define _ALPHATEST_ON

            #pragma multi_compile_instancing
            #pragma multi_compile _ LOD_FADE_CROSSFADE

            #pragma vertex vert
            #pragma fragment frag

            #define SHADERPASS SHADERPASS_DEPTHONLY

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

            #if defined(LOD_FADE_CROSSFADE)
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

            #include "Functions.hlsl"

            float2 voronoihash(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453);
            }

            float voronoi(float2 v, float time, inout float2 id, inout float2 mr,
                           float smoothness, inout float2 smoothId)
            {
                float2 n = floor(v);
                float2 f = frac(v);
                float F1 = 8.0;
                float2 mg = 0;
                int i, j;

                for (j = -1; j <= 1; j++)
                {
                    for (i = -1; i <= 1; i++)
                    {
                        float2 g = float2(i, j);
                        float2 o = voronoihash(n + g);
                        o = (sin(time + o * 6.2831) * 0.5 + 0.5);
                        float2 r = f - g - o;
                        float d = 0.5 * dot(r, r);
                        float h = smoothstep(0.0, 1.0, 0.5 + 0.5 * (F1 - d) / smoothness);
                        F1 = lerp(F1, d, h) - smoothness * h * (1.0 - h);
                        mg = g; mr = r; id = o;
                    }
                }
                return F1;
            }

            float VoronoiFBM(float3 noiseUV, float smoothness)
            {
                float time  = noiseUV.z;
                float2 smoothId = 0;
                float2 coords = noiseUV.xy;
                float2 id = 0;
                float2 uv = 0;
                float fade = 0.5;
                float voroi = 0;
                float rest  = 0;

                for (int it = 0; it < 2; it++)
                {
                    voroi += fade * voronoi(coords, time, id, uv, smoothness, smoothId);
                    rest  += fade;
                    coords *= 2;
                    fade  *= 0.5;
                }
                return saturate(voroi / rest);
            }

            CBUFFER_START(UnityPerMaterial)
                float4 _Colour;
                float4 _NoiseColour;
                float4 _Texture_ST;
                float3 _NoiseTiling;
                float3 _NoiseAnimation;
                float3 _Animation;
                float  _NoiseScale;
                float  _NoiseSmoothness;
                float  _NoiseVertexOffsetPower1;
                float  _NoiseVertexOffset1;
                float  _Amplitude;
                float  _Frequency;
                float  _Steepness;
                float  _Rotation;
                float  _NoiseColourPower;
                float  _AlphaClipThreshold;
            CBUFFER_END

            sampler2D _Texture;

            float3 ComputeNoiseUV(float3 particleCenterXZY)
            {
                return ((particleCenterXZY * _NoiseScale) * _NoiseTiling)
                     - (_NoiseAnimation * _TimeParameters.x);
            }

            float ComputeNoise(float3 noiseUV)
            {
                return VoronoiFBM(noiseUV, _NoiseSmoothness);
            }

            float3 ComputeVertexOffset(float noise, float3 particleCenter, float3 objectPos)
            {
                float noiseOffset = pow(noise, _NoiseVertexOffsetPower1) * _NoiseVertexOffset1;

                float3 normalTS;
                float3 wave = GerstnerWave(
                    particleCenter, objectPos,
                    _Amplitude, _Frequency,
                    _Steepness, _Animation,
                    _Rotation, normalTS);

                return float3(0, 1, 0) * noiseOffset + wave;
            }

            struct Attributes
            {
                float4 positionOS  : POSITION;
                half3  normalOS    : NORMAL;
                float4 texcoord0   : TEXCOORD0;
                float4 texcoord1   : TEXCOORD1;
                float4 color       : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 texcoord0  : TEXCOORD0;
                float4 texcoord1  : TEXCOORD1;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 particleCenter    = float3(input.texcoord0.z, input.texcoord0.w, input.texcoord1.x);
                float3 particleCenterXZY = particleCenter.xzy;

                float3 noiseUV = ComputeNoiseUV(particleCenterXZY);
                float  noise   = ComputeNoise(noiseUV);

                float3 objectPos    = GetAbsolutePositionWS(UNITY_MATRIX_M._m03_m13_m23);
                float3 vertexOffset = ComputeVertexOffset(noise, particleCenter, objectPos);

                input.positionOS.xyz += vertexOffset;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

                output.positionCS = vertexInput.positionCS;
                output.texcoord0  = input.texcoord0;
                output.texcoord1  = input.texcoord1;
                output.color      = input.color;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                #if defined(LOD_FADE_CROSSFADE)
                    LODFadeCrossFade(input.positionCS);
                #endif

                float3 particleCenter    = float3(input.texcoord0.z, input.texcoord0.w, input.texcoord1.x);
                float3 particleCenterXZY = particleCenter.xzy;

                float3 noiseUV = ComputeNoiseUV(particleCenterXZY);
                float  noise   = ComputeNoise(noiseUV);

                float  noisePow  = smoothstep(0.0, 1.0, pow(noise, _NoiseColourPower));
                float4 blended   = lerp(_Colour, _NoiseColour, noisePow);

                float2 uv      = input.texcoord0.xy * _Texture_ST.xy + _Texture_ST.zw;
                float4 texColor = tex2D(_Texture, uv);

                float alpha = (blended * input.color * texColor).a;
                clip(alpha - _AlphaClipThreshold);

                return input.positionCS.z;
            }
            ENDHLSL
        }

        // ─────────────────────────────────────────────
        // Depth normals pass
        // ─────────────────────────────────────────────
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 4.5

            #define _ALPHATEST_ON

            #pragma multi_compile_instancing
            #pragma multi_compile _ LOD_FADE_CROSSFADE

            #pragma vertex vert
            #pragma fragment frag

            #define SHADERPASS SHADERPASS_DEPTHNORMALS

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

            #if defined(LOD_FADE_CROSSFADE)
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

            #include "Functions.hlsl"

            float2 voronoihash(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453);
            }

            float voronoi(float2 v, float time, inout float2 id, inout float2 mr,
                           float smoothness, inout float2 smoothId)
            {
                float2 n = floor(v);
                float2 f = frac(v);
                float F1 = 8.0;
                float2 mg = 0;
                int i, j;

                for (j = -1; j <= 1; j++)
                {
                    for (i = -1; i <= 1; i++)
                    {
                        float2 g = float2(i, j);
                        float2 o = voronoihash(n + g);
                        o = (sin(time + o * 6.2831) * 0.5 + 0.5);
                        float2 r = f - g - o;
                        float d = 0.5 * dot(r, r);
                        float h = smoothstep(0.0, 1.0, 0.5 + 0.5 * (F1 - d) / smoothness);
                        F1 = lerp(F1, d, h) - smoothness * h * (1.0 - h);
                        mg = g; mr = r; id = o;
                    }
                }
                return F1;
            }

            float VoronoiFBM(float3 noiseUV, float smoothness)
            {
                float time  = noiseUV.z;
                float2 smoothId = 0;
                float2 coords = noiseUV.xy;
                float2 id = 0;
                float2 uv = 0;
                float fade = 0.5;
                float voroi = 0;
                float rest  = 0;

                for (int it = 0; it < 2; it++)
                {
                    voroi += fade * voronoi(coords, time, id, uv, smoothness, smoothId);
                    rest  += fade;
                    coords *= 2;
                    fade  *= 0.5;
                }
                return saturate(voroi / rest);
            }

            CBUFFER_START(UnityPerMaterial)
                float4 _Colour;
                float4 _NoiseColour;
                float4 _Texture_ST;
                float3 _NoiseTiling;
                float3 _NoiseAnimation;
                float3 _Animation;
                float  _NoiseScale;
                float  _NoiseSmoothness;
                float  _NoiseVertexOffsetPower1;
                float  _NoiseVertexOffset1;
                float  _Amplitude;
                float  _Frequency;
                float  _Steepness;
                float  _Rotation;
                float  _NoiseColourPower;
                float  _AlphaClipThreshold;
            CBUFFER_END

            sampler2D _Texture;

            float3 ComputeNoiseUV(float3 particleCenterXZY)
            {
                return ((particleCenterXZY * _NoiseScale) * _NoiseTiling)
                     - (_NoiseAnimation * _TimeParameters.x);
            }

            float ComputeNoise(float3 noiseUV)
            {
                return VoronoiFBM(noiseUV, _NoiseSmoothness);
            }

            float3 ComputeVertexOffset(float noise, float3 particleCenter, float3 objectPos)
            {
                float noiseOffset = pow(noise, _NoiseVertexOffsetPower1) * _NoiseVertexOffset1;

                float3 normalTS;
                float3 wave = GerstnerWave(
                    particleCenter, objectPos,
                    _Amplitude, _Frequency,
                    _Steepness, _Animation,
                    _Rotation, normalTS);

                return float3(0, 1, 0) * noiseOffset + wave;
            }

            struct Attributes
            {
                float4 positionOS  : POSITION;
                half3  normalOS    : NORMAL;
                float4 texcoord0   : TEXCOORD0;
                float4 texcoord1   : TEXCOORD1;
                float4 color       : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half3  normalWS   : TEXCOORD0;
                float4 texcoord0  : TEXCOORD1;
                float4 texcoord1  : TEXCOORD2;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 particleCenter    = float3(input.texcoord0.z, input.texcoord0.w, input.texcoord1.x);
                float3 particleCenterXZY = particleCenter.xzy;

                float3 noiseUV = ComputeNoiseUV(particleCenterXZY);
                float  noise   = ComputeNoise(noiseUV);

                float3 objectPos    = GetAbsolutePositionWS(UNITY_MATRIX_M._m03_m13_m23);
                float3 vertexOffset = ComputeVertexOffset(noise, particleCenter, objectPos);

                input.positionOS.xyz += vertexOffset;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs   normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.normalWS   = normalInput.normalWS;
                output.texcoord0  = input.texcoord0;
                output.texcoord1  = input.texcoord1;
                output.color      = input.color;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                #if defined(LOD_FADE_CROSSFADE)
                    LODFadeCrossFade(input.positionCS);
                #endif

                float3 particleCenter    = float3(input.texcoord0.z, input.texcoord0.w, input.texcoord1.x);
                float3 particleCenterXZY = particleCenter.xzy;

                float3 noiseUV = ComputeNoiseUV(particleCenterXZY);
                float  noise   = ComputeNoise(noiseUV);

                float  noisePow  = smoothstep(0.0, 1.0, pow(noise, _NoiseColourPower));
                float4 blended   = lerp(_Colour, _NoiseColour, noisePow);

                float2 uv      = input.texcoord0.xy * _Texture_ST.xy + _Texture_ST.zw;
                float4 texColor = tex2D(_Texture, uv);

                float alpha = (blended * input.color * texColor).a;
                clip(alpha - _AlphaClipThreshold);

                float3 normalWS = NormalizeNormalPerPixel(input.normalWS);
                return half4(normalWS, 0.0);
            }
            ENDHLSL
        }

        // ─────────────────────────────────────────────
        // Scene selection pass (editor only)
        // ─────────────────────────────────────────────
        Pass
        {
            Name "SceneSelectionPass"
            Tags { "LightMode" = "SceneSelectionPass" }

            Cull Off
            ColorMask 0

            HLSLPROGRAM
            #pragma target 4.5

            #define _ALPHATEST_ON

            #pragma multi_compile_instancing

            #pragma vertex vert
            #pragma fragment frag

            #define SHADERPASS SHADERPASS_DEPTHONLY
            #define SCENESELECTIONPASS 1

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

            #include "Functions.hlsl"

            float2 voronoihash(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453);
            }

            float voronoi(float2 v, float time, inout float2 id, inout float2 mr,
                           float smoothness, inout float2 smoothId)
            {
                float2 n = floor(v);
                float2 f = frac(v);
                float F1 = 8.0;
                float2 mg = 0;
                int i, j;
                for (j = -1; j <= 1; j++)
                {
                    for (i = -1; i <= 1; i++)
                    {
                        float2 g = float2(i, j);
                        float2 o = voronoihash(n + g);
                        o = (sin(time + o * 6.2831) * 0.5 + 0.5);
                        float2 r = f - g - o;
                        float d = 0.5 * dot(r, r);
                        float h = smoothstep(0.0, 1.0, 0.5 + 0.5 * (F1 - d) / smoothness);
                        F1 = lerp(F1, d, h) - smoothness * h * (1.0 - h);
                        mg = g; mr = r; id = o;
                    }
                }
                return F1;
            }

            float VoronoiFBM(float3 noiseUV, float smoothness)
            {
                float time  = noiseUV.z;
                float2 smoothId = 0;
                float2 coords = noiseUV.xy;
                float2 id = 0;
                float2 uv = 0;
                float fade = 0.5;
                float voroi = 0;
                float rest  = 0;
                for (int it = 0; it < 2; it++)
                {
                    voroi += fade * voronoi(coords, time, id, uv, smoothness, smoothId);
                    rest  += fade;
                    coords *= 2;
                    fade  *= 0.5;
                }
                return saturate(voroi / rest);
            }

            CBUFFER_START(UnityPerMaterial)
                float4 _Colour;
                float4 _NoiseColour;
                float4 _Texture_ST;
                float3 _NoiseTiling;
                float3 _NoiseAnimation;
                float3 _Animation;
                float  _NoiseScale;
                float  _NoiseSmoothness;
                float  _NoiseVertexOffsetPower1;
                float  _NoiseVertexOffset1;
                float  _Amplitude;
                float  _Frequency;
                float  _Steepness;
                float  _Rotation;
                float  _NoiseColourPower;
                float  _AlphaClipThreshold;
            CBUFFER_END

            sampler2D _Texture;

            float3 ComputeNoiseUV(float3 particleCenterXZY)
            {
                return ((particleCenterXZY * _NoiseScale) * _NoiseTiling)
                     - (_NoiseAnimation * _TimeParameters.x);
            }

            float ComputeNoise(float3 noiseUV)
            {
                return VoronoiFBM(noiseUV, _NoiseSmoothness);
            }

            float3 ComputeVertexOffset(float noise, float3 particleCenter, float3 objectPos)
            {
                float noiseOffset = pow(noise, _NoiseVertexOffsetPower1) * _NoiseVertexOffset1;
                float3 normalTS;
                float3 wave = GerstnerWave(
                    particleCenter, objectPos,
                    _Amplitude, _Frequency,
                    _Steepness, _Animation,
                    _Rotation, normalTS);
                return float3(0, 1, 0) * noiseOffset + wave;
            }

            int _ObjectId;
            int _PassValue;

            struct Attributes
            {
                float4 positionOS  : POSITION;
                half3  normalOS    : NORMAL;
                float4 texcoord0   : TEXCOORD0;
                float4 texcoord1   : TEXCOORD1;
                float4 color       : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 texcoord0  : TEXCOORD0;
                float4 texcoord1  : TEXCOORD1;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 particleCenter    = float3(input.texcoord0.z, input.texcoord0.w, input.texcoord1.x);
                float3 particleCenterXZY = particleCenter.xzy;
                float3 noiseUV = ComputeNoiseUV(particleCenterXZY);
                float  noise   = ComputeNoise(noiseUV);
                float3 objectPos    = GetAbsolutePositionWS(UNITY_MATRIX_M._m03_m13_m23);
                float3 vertexOffset = ComputeVertexOffset(noise, particleCenter, objectPos);

                input.positionOS.xyz += vertexOffset;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.texcoord0  = input.texcoord0;
                output.texcoord1  = input.texcoord1;
                output.color      = input.color;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float3 particleCenter    = float3(input.texcoord0.z, input.texcoord0.w, input.texcoord1.x);
                float3 particleCenterXZY = particleCenter.xzy;
                float3 noiseUV = ComputeNoiseUV(particleCenterXZY);
                float  noise   = ComputeNoise(noiseUV);

                float  noisePow  = smoothstep(0.0, 1.0, pow(noise, _NoiseColourPower));
                float4 blended   = lerp(_Colour, _NoiseColour, noisePow);
                float2 uv      = input.texcoord0.xy * _Texture_ST.xy + _Texture_ST.zw;
                float4 texColor = tex2D(_Texture, uv);

                float alpha = (blended * input.color * texColor).a;
                clip(alpha - _AlphaClipThreshold);

                return half4(_ObjectId, _PassValue, 1.0, 1.0);
            }
            ENDHLSL
        }

        // ─────────────────────────────────────────────
        // Scene picking pass (editor only)
        // ─────────────────────────────────────────────
        Pass
        {
            Name "ScenePickingPass"
            Tags { "LightMode" = "Picking" }

            Cull Back

            HLSLPROGRAM
            #pragma target 4.5

            #define _ALPHATEST_ON

            #pragma multi_compile_instancing

            #pragma vertex vert
            #pragma fragment frag

            #define SHADERPASS SHADERPASS_DEPTHONLY
            #define SCENEPICKINGPASS 1

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

            #include "Functions.hlsl"

            float2 voronoihash(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453);
            }

            float voronoi(float2 v, float time, inout float2 id, inout float2 mr,
                           float smoothness, inout float2 smoothId)
            {
                float2 n = floor(v);
                float2 f = frac(v);
                float F1 = 8.0;
                float2 mg = 0;
                int i, j;
                for (j = -1; j <= 1; j++)
                {
                    for (i = -1; i <= 1; i++)
                    {
                        float2 g = float2(i, j);
                        float2 o = voronoihash(n + g);
                        o = (sin(time + o * 6.2831) * 0.5 + 0.5);
                        float2 r = f - g - o;
                        float d = 0.5 * dot(r, r);
                        float h = smoothstep(0.0, 1.0, 0.5 + 0.5 * (F1 - d) / smoothness);
                        F1 = lerp(F1, d, h) - smoothness * h * (1.0 - h);
                        mg = g; mr = r; id = o;
                    }
                }
                return F1;
            }

            float VoronoiFBM(float3 noiseUV, float smoothness)
            {
                float time  = noiseUV.z;
                float2 smoothId = 0;
                float2 coords = noiseUV.xy;
                float2 id = 0;
                float2 uv = 0;
                float fade = 0.5;
                float voroi = 0;
                float rest  = 0;
                for (int it = 0; it < 2; it++)
                {
                    voroi += fade * voronoi(coords, time, id, uv, smoothness, smoothId);
                    rest  += fade;
                    coords *= 2;
                    fade  *= 0.5;
                }
                return saturate(voroi / rest);
            }

            CBUFFER_START(UnityPerMaterial)
                float4 _Colour;
                float4 _NoiseColour;
                float4 _Texture_ST;
                float3 _NoiseTiling;
                float3 _NoiseAnimation;
                float3 _Animation;
                float  _NoiseScale;
                float  _NoiseSmoothness;
                float  _NoiseVertexOffsetPower1;
                float  _NoiseVertexOffset1;
                float  _Amplitude;
                float  _Frequency;
                float  _Steepness;
                float  _Rotation;
                float  _NoiseColourPower;
                float  _AlphaClipThreshold;
            CBUFFER_END

            sampler2D _Texture;

            float3 ComputeNoiseUV(float3 particleCenterXZY)
            {
                return ((particleCenterXZY * _NoiseScale) * _NoiseTiling)
                     - (_NoiseAnimation * _TimeParameters.x);
            }

            float ComputeNoise(float3 noiseUV)
            {
                return VoronoiFBM(noiseUV, _NoiseSmoothness);
            }

            float3 ComputeVertexOffset(float noise, float3 particleCenter, float3 objectPos)
            {
                float noiseOffset = pow(noise, _NoiseVertexOffsetPower1) * _NoiseVertexOffset1;
                float3 normalTS;
                float3 wave = GerstnerWave(
                    particleCenter, objectPos,
                    _Amplitude, _Frequency,
                    _Steepness, _Animation,
                    _Rotation, normalTS);
                return float3(0, 1, 0) * noiseOffset + wave;
            }

            float4 _SelectionID;

            struct Attributes
            {
                float4 positionOS  : POSITION;
                half3  normalOS    : NORMAL;
                float4 texcoord0   : TEXCOORD0;
                float4 texcoord1   : TEXCOORD1;
                float4 color       : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 texcoord0  : TEXCOORD0;
                float4 texcoord1  : TEXCOORD1;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 particleCenter    = float3(input.texcoord0.z, input.texcoord0.w, input.texcoord1.x);
                float3 particleCenterXZY = particleCenter.xzy;
                float3 noiseUV = ComputeNoiseUV(particleCenterXZY);
                float  noise   = ComputeNoise(noiseUV);
                float3 objectPos    = GetAbsolutePositionWS(UNITY_MATRIX_M._m03_m13_m23);
                float3 vertexOffset = ComputeVertexOffset(noise, particleCenter, objectPos);

                input.positionOS.xyz += vertexOffset;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.texcoord0  = input.texcoord0;
                output.texcoord1  = input.texcoord1;
                output.color      = input.color;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float3 particleCenter    = float3(input.texcoord0.z, input.texcoord0.w, input.texcoord1.x);
                float3 particleCenterXZY = particleCenter.xzy;
                float3 noiseUV = ComputeNoiseUV(particleCenterXZY);
                float  noise   = ComputeNoise(noiseUV);

                float  noisePow  = smoothstep(0.0, 1.0, pow(noise, _NoiseColourPower));
                float4 blended   = lerp(_Colour, _NoiseColour, noisePow);
                float2 uv      = input.texcoord0.xy * _Texture_ST.xy + _Texture_ST.zw;
                float4 texColor = tex2D(_Texture, uv);

                float alpha = (blended * input.color * texColor).a;
                clip(alpha - _AlphaClipThreshold);

                return _SelectionID;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}