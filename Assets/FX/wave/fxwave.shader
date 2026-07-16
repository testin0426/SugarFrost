Shader "Custom/URP_FXWaveOverlay"
{
    // Expanding shockwave ripple, drawn as a transparent overlay quad (like URP_UIFXOverlay).
    // Sits IN FRONT of the image but never occludes it: ZWrite Off + drawn after, so it only
    // blends its ripple ring over the framebuffer.
    //
    // Because a transparent overlay cannot read the pixels behind it in URP (no GrabPass),
    // this does NOT refract the underlying image (that's what the full-screen wave.shader does).
    // Instead it renders the wave's "blow-out" highlight ring as a self-contained tinted band
    // that expands from the centre. Drive _Progress 0 -> 1 from script to fire one ripple.
    //   _Progress = 0 -> ring at centre (invisible)
    //   _Progress = 1 -> ring reached edge / faded out
    Properties
    {
        _WaveColor  ("Wave Color", Color) = (1, 1, 1, 1)
        _WaveParams ("Wave Params (Sharpness, Falloff, Thickness)", Vector) = (10.0, 0.8, 0.1, 0.0)
        _WaveCentre ("Wave Centre (UV)", Vector) = (0.5, 0.5, 0.0, 0.0)
        _Aspect     ("Aspect (W/H, for round ring)", Float) = 1.0
        _Progress   ("Progress", Range(0, 1)) = 0.0
        _Strength   ("Strength", Range(0, 4)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent+100"   // draw AFTER the UI so it composites over it
            "RenderPipeline" = "UniversalPipeline"
        }

        // Alpha blend over the framebuffer, same as URP_UIFXOverlay. Use a bright _WaveColor
        // (e.g. white) for a whiteout shockwave ring.
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off        // do not write depth -> cannot occlude what is behind it
        ZTest Always      // always draw on top regardless of depth
        Cull Off

        Pass
        {
            Name "FXWave"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4  _WaveColor;
                float4 _WaveParams;
                float4 _WaveCentre;
                float  _Aspect;
                float  _Progress;
                float  _Strength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 WaveParams = _WaveParams.xyz;   // x=sharpness, y=falloff, z=thickness

                // Aspect-correct so the ring stays circular on a non-square quad.
                float2 texCoord = input.uv;
                float2 centre   = _WaveCentre.xy;
                texCoord.x *= _Aspect;
                centre.x   *= _Aspect;

                float Dist = distance(texCoord, centre);

                // The ring radius sweeps outward with progress.
                float CurrentTime = _Progress;

                // Ring band: only the pixels near the current radius light up.
                float Diff = Dist - CurrentTime;
                float ScaleDiff = 1.0 - pow(abs(Diff * WaveParams.x), WaveParams.y);

                // Mask to the band thickness, and fade the whole ripple out as it expands.
                float band = step(abs(Diff), WaveParams.z);
                float fade = saturate(1.0 - CurrentTime);              // fade with distance travelled
                float intensity = saturate(ScaleDiff) * band * fade * _Strength;

                // Envelope so Progress=0 and Progress=1 are both silent (no pop on trigger).
                intensity *= smoothstep(0.0, 0.05, _Progress);

                half a = saturate(intensity) * _WaveColor.a;
                return half4(_WaveColor.rgb, a);
            }
            ENDHLSL
        }
    }
}
