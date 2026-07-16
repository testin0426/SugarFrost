Shader "OC/CrystalEmboss"
{
    // Photoshop-style Bevel & Emboss with a "crystal" gloss contour.
    // Port of the WebGL prototype: the text/glyph texture goes in _MainTex
    // (its red channel is used as a height field, like white-on-black text).
    // Apply this material to a Quad / Sprite / RawImage showing that texture.
    Properties
    {
        [Header(Source)]
        _MainTex ("Height (R channel)", 2D) = "black" {}
        _BgColor ("Background", Color) = (0.102, 0.102, 0.180, 1)
        _SurfaceLow  ("Surface Color (edge)", Color) = (0.7, 0.75, 0.85, 1)
        _SurfaceHigh ("Surface Color (core)", Color) = (1, 1, 1, 1)

        [Header(Bevel and Emboss)]
        _Depth   ("Depth", Range(0.5, 8)) = 3.0
        _Blur    ("Bevel Softness", Range(1, 12)) = 4.0
        _SpecPow ("Specular Power", Range(4, 200)) = 48
        _SpecI   ("Specular Intensity", Range(0, 3)) = 1.2

        [Header(Crystal Gloss Contour)]
        _Bands   ("Contour Bands", Range(1, 8)) = 3
        _CSharp  ("Contour Sharpness", Range(0.5, 8)) = 2.5

        [Header(Lighting)]
        _Angle   ("Light Angle (deg)", Range(0, 360)) = 135
        _Elev    ("Light Elevation (deg)", Range(5, 85)) = 45
        _Ambient ("Ambient", Range(0, 0.5)) = 0.15
        _Fresnel ("Fresnel Strength", Range(0, 1)) = 0.4
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f     { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize; // (1/w, 1/h, w, h)

            fixed4 _BgColor, _SurfaceLow, _SurfaceHigh;
            float  _Depth, _Blur, _SpecPow, _SpecI;
            float  _Bands, _CSharp;
            float  _Angle, _Elev, _Ambient, _Fresnel;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            // Gaussian-weighted disc blur of the height (R) channel.
            float sampleHeight(float2 uv, float radius)
            {
                float h = 0.0;
                float total = 0.0;
                float2 texel = _MainTex_TexelSize.xy;
                float inv2sig2 = 0.5 / (radius * radius * 0.25);
                float r2 = radius * radius;
                [loop]
                for (int y = -6; y <= 6; y++)
                {
                    [loop]
                    for (int x = -6; x <= 6; x++)
                    {
                        float d2 = float(x * x + y * y);
                        if (d2 > r2) continue;
                        float w = exp(-d2 * inv2sig2);
                        h += tex2D(_MainTex, uv + float2(x, y) * texel).r * w;
                        total += w;
                    }
                }
                return h / total;
            }

            // Crystal gloss contour: remap specular through periodic bands.
            float glossContour(float spec, float bands, float sharpness)
            {
                float contour = sin(spec * bands * 3.14159265) * 0.5 + 0.5;
                contour = pow(contour, 1.0 / sharpness);
                return contour * spec;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 texel = _MainTex_TexelSize.xy;
                float radius = _Blur;

                // Height samples for central-difference normal.
                float hC = sampleHeight(uv, radius);
                float hL = sampleHeight(uv - float2(texel.x * 1.5, 0.0), radius);
                float hR = sampleHeight(uv + float2(texel.x * 1.5, 0.0), radius);
                float hD = sampleHeight(uv - float2(0.0, texel.y * 1.5), radius);
                float hU = sampleHeight(uv + float2(0.0, texel.y * 1.5), radius);

                float3 N = normalize(float3((hL - hR) * _Depth,
                                            (hD - hU) * _Depth,
                                            1.0));

                // Light from angle + elevation (Photoshop convention).
                float a = radians(_Angle);
                float e = radians(_Elev);
                float3 L = normalize(float3(cos(a) * cos(e), sin(a) * cos(e), sin(e)));
                float3 V = float3(0.0, 0.0, 1.0);
                float3 H = normalize(L + V);

                float diff = max(dot(N, L), 0.0);
                float rawSpec = pow(max(dot(N, H), 0.0), _SpecPow);
                float crystalSpec = glossContour(rawSpec, _Bands, _CSharp) * _SpecI;
                float fres = pow(1.0 - max(dot(N, V), 0.0), 3.0) * _Fresnel;

                float mask = tex2D(_MainTex, uv).r;

                float3 baseColor = lerp(_SurfaceLow.rgb, _SurfaceHigh.rgb, mask * 0.3);
                float3 lit = baseColor * (_Ambient + diff * 0.8)
                           + float3(1.0, 1.0, 1.0) * crystalSpec
                           + float3(0.6, 0.7, 1.0) * fres;

                // Coverage from the beveled height (edges feather out a bit).
                float alpha = smoothstep(0.01, 0.15, hC);
                float3 col = lerp(_BgColor.rgb, lit, alpha);

                // Subtle drop shadow on the background.
                float shadowH = sampleHeight(uv + float2(texel.x * 3.0, -texel.y * 3.0), radius * 1.5);
                float shadow = smoothstep(0.0, 0.3, shadowH) * 0.3;
                col = lerp(col, col * 0.4, shadow * (1.0 - alpha));

                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }
    FallBack Off
}
