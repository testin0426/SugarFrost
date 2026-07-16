Shader "CustomURP/Wave"
{
    Properties
    {
        // Wave shape: x = sharpness, y = falloff power, z = band thickness
        _WaveParams ("Wave Params (Sharpness, Falloff, Thickness)", Vector) = (10.0, 0.8, 0.1, 0.0)
        _WaveCentre ("Wave Centre (UV)", Vector) = (0.5, 0.5, 0.0, 0.0)
        _Speed ("Speed", Range(0.01, 5.0)) = 1.0
        _Strength ("Distortion Strength", Range(0.0, 4.0)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Wave"
            Cull Off ZWrite Off ZTest Always Blend Off

            HLSLPROGRAM
            #pragma vertex FullscreenVert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings FullscreenVert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
                return output;
            }

            TEXTURE2D_X(_BlitTexture);
            SAMPLER(sampler_BlitTexture);
            float4 _BlitTexture_TexelSize;

            float4 _WaveParams;
            float4 _WaveCentre;
            float _Speed;
            float _Strength;

            float4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.uv;

                #if UNITY_UV_STARTS_AT_TOP
                if (_BlitTexture_TexelSize.y < 0)
                    uv.y = 1.0 - uv.y;
                #endif

                // Sawtooth function to pulse from centre. (iTime -> _Time.y)
                float t = _Time.y * _Speed;
                float offset = (t - floor(t)) / max(t, 1e-5);
                float CurrentTime = t * offset;

                float3 WaveParams = _WaveParams.xyz;

                // Aspect ratio correction (iResolution.y / iResolution.x -> screen h / w)
                float ratio = _ScreenParams.y / _ScreenParams.x;

                float2 WaveCentre = _WaveCentre.xy;
                WaveCentre.y *= ratio;

                float2 texCoord = uv;
                texCoord.y *= ratio;
                float Dist = distance(texCoord, WaveCentre);

                float4 Color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv);

                // Only distort pixels within the parameter distance from the centre.
                if ((Dist <= (CurrentTime + WaveParams.z)) &&
                    (Dist >= (CurrentTime - WaveParams.z)))
                {
                    // The pixel offset distance based on the input parameters.
                    float Diff = Dist - CurrentTime;
                    float ScaleDiff = 1.0 - pow(abs(Diff * WaveParams.x), WaveParams.y);
                    float DiffTime = Diff * ScaleDiff;

                    // The direction of the distortion.
                    float2 DiffTexCoord = normalize(texCoord - WaveCentre);

                    // Perform the distortion and reduce the effect over time.
                    // Undo the aspect-ratio scaling on y before sampling the blit texture.
                    float2 distortedCoord = texCoord + (DiffTexCoord * DiffTime * _Strength) / (CurrentTime * Dist * 40.0);
                    distortedCoord.y /= ratio;

                    Color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, distortedCoord);

                    // Blow out the colour and reduce the effect over time.
                    Color += (Color * ScaleDiff) / (CurrentTime * Dist * 40.0);
                }

                return Color;
            }
            ENDHLSL
        }
    }
}
