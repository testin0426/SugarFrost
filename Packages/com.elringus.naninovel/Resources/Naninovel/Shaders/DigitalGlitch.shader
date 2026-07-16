Shader "Naninovel/FX/Digital Glitch"
{
    Properties
    {
        _MainTex("Base (RGB)", 2D) = "white" {}
        _GlitchTex("Glitch Texture", 2D) = "white" {}
        _Intensity("Glitch Intensity", Range(0.5, 2)) = 1.0
        _ColorTint("Color Tint", Color) = (0.2, 0.2, 0.0, 0.0)
        _BurnColors("Burn Colors", Range(0, 1)) = 1
        _DodgeColors("Dodge Colors", Range(0, 1)) = 0
        _PerformUVShifting("Perform UV Shifting", Range(0, 1)) = 1
        _PerformColorShifting("Perform Color Shifting", Range(0, 1)) = 1
        _PerformScreenShifting("Perform Screen Shifting", Range(0, 1)) = 1
    }

    SubShader
    {
        Pass
        {
            ZTest Always
            Cull Off
            ZWrite Off
            Fog { Mode off }

            CGPROGRAM

            #include "UnityCG.cginc"

            #pragma target 3.0
            #pragma exclude_renderers flash
            #pragma vertex ComputeVertex
            #pragma fragment ComputeFragment

            uniform sampler2D _MainTex;
            uniform sampler2D _GlitchTex;
            uniform float4 _GlitchTex_ST;
            uniform float _Intensity;
            uniform float4 _ColorTint;
            uniform float _BurnColors;
            uniform float _DodgeColors;
            uniform float _PerformUVShifting;
            uniform float _PerformColorShifting;
            uniform float _PerformScreenShifting;

            uniform float flipURP;
            uniform float filterRadius;
            uniform float flipUp, flipDown;
            uniform float displace;

            struct vertexOutput
            {
                float4 pos : POSITION;
                float2 uv : TEXCOORD0;
            };

            float4 ColorBurn(float4 a, float4 b)
            {
                float4 r = 1.0 - (1.0 - a) / b;
                r.a = b.a;
                return r;
            }

            float4 Divide(float4 a, float4 b)
            {
                float4 r = a / b;
                r.a = b.a;
                return r;
            }

            float4 Subtract(float4 a, float4 b)
            {
                float4 r = a - b;
                r.a = b.a;
                return r;
            }

            float4 Difference(float4 a, float4 b)
            {
                float4 r = abs(a - b);
                r.a = b.a;
                return r;
            }

            vertexOutput ComputeVertex(appdata_img v)
            {
                vertexOutput o;

                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord.xy;

                #if UNITY_UV_STARTS_AT_TOP
                // flipURP = 1 means the screen texture is flipped on URP — revert
                o.uv.y = (1.0 - 2.0 * flipURP) * o.uv.y + flipURP;
                #endif

                return o;
            }

            float4 ComputeFragment(vertexOutput o) : COLOR
            {
                float4 mainColor = tex2D(_MainTex, o.uv.xy);
                float4 glitchColor = tex2D(_GlitchTex, o.uv.xy * _GlitchTex_ST.xy + _GlitchTex_ST.zw);

                float sinTime = abs(sin(_Time.y * _Intensity));
                float cosTime = abs(cos(_Time.z * _Intensity));

                if ((o.uv.y < sinTime + filterRadius / 10.0 && o.uv.y > sinTime - filterRadius / 10.0) ||
                    (o.uv.y < cosTime + filterRadius / 10.0 && o.uv.y > cosTime - filterRadius / 10.0))
                {
                    if (o.uv.y < flipUp)
                        o.uv.y = 1.0 - (o.uv.y + flipUp);

                    if (o.uv.y > flipDown)
                        o.uv.y = 1.0 - (o.uv.y - flipDown);

                    o.uv.xy += displace * _Intensity;
                }

                float4 shiftedSample = tex2D(_MainTex, lerp(o.uv.xy, o.uv.xy + 0.01 * filterRadius * _Intensity, _PerformScreenShifting));
                mainColor = lerp(mainColor, shiftedSample, _PerformUVShifting);

                mainColor = lerp(mainColor, mainColor * (1.0 + _ColorTint), _PerformColorShifting);
                float4 burnedGlitch = lerp(ColorBurn(mainColor, glitchColor), Divide(mainColor, glitchColor), floor(abs(filterRadius)));
                float4 finalGlitch = lerp(mainColor * glitchColor, burnedGlitch, _BurnColors);
                float4 dodgedGlitch = lerp(Subtract(finalGlitch, glitchColor), Difference(finalGlitch, glitchColor), floor(abs(filterRadius)));
                finalGlitch = lerp(finalGlitch, dodgedGlitch, _DodgeColors);
                mainColor = lerp(mainColor, finalGlitch, _PerformColorShifting);

                return mainColor;
            }

            ENDCG
        }
    }

    Fallback off
}
