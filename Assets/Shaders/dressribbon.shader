Shader "Custom/SkirtEnergyFlow"
{
    Properties{
        [HDR] _BaseColor("Color", Color) = (1, 1, 1, 1)
        _Tex("TEX", 2D) = "white" {}
        _Speed("Flow Speed", Float) = 0.1
        _Tilling("Tilling", Float) = 0.1
        _Offset("Offset", Float) = 10
        _Threshold("Threshold", Float) = 20
        _Intensity("Intensity", Float) = 10
    }
    SubShader
    {
        Tags {"RenderType"="Transparent" "Queue"="Transparent"}
        
        Blend srcAlpha OneMinusSrcAlpha 
        ZWrite Off
        Cull Off
        
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct v_in
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v_out
            {
                float4 positionCS_SV : SV_POSITION;
                float2 uv : TEXCOORD0;

            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;            
                float _Speed;
                float _Tilling;
                float _Offset;
                float _Threshold;
                float _Intensity;
            CBUFFER_END
                sampler2D _TEX;
      

            v_out vert(v_in i)
            {
                v_out o;
                o.positionCS_SV = TransformObjectToHClip(i.positionOS.xyz);
                o.uv = i.uv;
                return o;
            }

            float4 frag(v_out i) : SV_Target
            {

                float3 V = normalize(GetCameraPositionWS() - i.positionCS_SV);
                float uvy = i.uv.y * _Tilling;
                float uvx = i.uv.x * _Tilling;
                float alpha = frac(_Time.y * _Speed + (uvy * _Offset));
                // sample the texture
                
                //float isBlock = step(_Threshold,1, alpha);
                float beam = pow(alpha, _Threshold);
                float3 finalColor = _BaseColor * _Intensity;

                return float4(finalColor, beam);
            }
            ENDHLSL
        }      
    }
}