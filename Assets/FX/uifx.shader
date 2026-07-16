Shader "Custom/URP_UIFXOverlay"
{
    // Transparent tint overlay for a camera-space UI.
    // Sits IN FRONT of the UI but never occludes it: ZWrite Off + renders after the UI,
    // so it only blends over (influences) the framebuffer color.
    //   _Threshold = 0 -> invisible (no influence, default state)
    //   _Threshold > 0 -> tint toward _BaseColor (black = darken, white = whiteout)
    Properties
    {
        _BaseColor ("Tint Color", Color) = (0, 0, 0, 1)   // default black
        _Threshold ("Influence", Range(0, 1)) = 0.0        // default = no influence
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent+100"   // draw AFTER the UI so it composites over it
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off        // do not write depth -> cannot occlude the UI behind it
        ZTest Always      // always draw on top regardless of depth
        Cull Off

        Pass
        {
            Name "UIFX"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _Threshold;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // _Threshold is the blend strength. Clamp below 1 so the UI is never fully hidden.
                float a = saturate(_Threshold) * 0.95;
                return half4(_BaseColor.rgb, a);
            }
            ENDHLSL
        }
    }
}
