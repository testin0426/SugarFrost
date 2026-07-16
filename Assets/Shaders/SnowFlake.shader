Shader "Unlit/SnowFlake"
{
    Properties
    {
        [HDR]_BaseColor("BaseColor", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
        _r("radiance", Float) = 0.4
        s("s", Float) = 3.0
    }
    SubShader
    {
        Tags {"RenderType"="Transparent" "Queue"="Transparent"}
        Blend srcAlpha OneMinusSrcAlpha 
        
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

            float4 _BaseColor;
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _r;
            float s;

            v_out vert(v_in i)
            {
                v_out o;
                o.positionCS_SV = TransformObjectToHClip(i.positionOS.xyz);
                o.uv = i.uv;
                return o;
            }

            float snow(float2 uv, float size)
            {
                const float k = 0.5773550269;
                uv = abs(uv);

                uv -= 2.0 * min(dot(uv, float2(-k, 1.0)), 0.0) * float2(-k, 1.0);
                uv.x -= size; 
                float scale = 1.0;
                float2 n = float2(-0.8660254, 0.5);
 
                for(int i = 0; i < 1; i++)
                {
                    uv -= 2.0 * min(dot(uv, n), 0.0) * n;
                    
                }
                return length(uv) / scale;
            }
           float4 frag(v_out i) : SV_Target
            {
                //i.uv = i.uv * 2.0 - 1.0;
                float dist = length(i.uv) - _r;
                float alpha = smoothstep(fwidth(dist), -fwidth(dist), dist);
                float d = 1 - snow(i.uv, _r);
                float glow = 0.1 / abs(d);

                float4 tex = tex2D(_MainTex, i.uv);
                float4 color = _BaseColor * tex; 
                return color;
            }
            ENDHLSL
        }
    }
}
