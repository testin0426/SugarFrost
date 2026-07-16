Shader "Naninovel/Placeholder/Character"
{
    Properties
    {
        _Size ("Size", Range(0, 1)) = 0.99
        _Color ("Color", Color) = (1, 1, 1, 1)
        _OutlineWidth ("Outline Width", Range(0, 0.5)) = 0.1
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineGradient ("Outline Gradient", Range(0, 10)) = 1
        _OutlineGradientPower ("Outline Gradient Power", Range(0, 10)) = 1
        _OutlineGradientRotation ("Outline Gradient Rotation", Range(0, 360)) = 0
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
        }

        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #include "UnityCG.cginc"

            #pragma vertex ComputeVertex
            #pragma fragment ComputeFragment

            struct VertexInput
            {
                float4 Vertex : POSITION;
                float2 TexCoord : TEXCOORD0;
            };

            struct VertexOutput
            {
                float4 Vertex : SV_POSITION;
                float2 TexCoord : TEXCOORD0;
            };

            float _Size;
            float4 _Color;
            float _OutlineWidth;
            float4 _OutlineColor;
            float _OutlineGradient;
            float _OutlineGradientPower;
            float _OutlineGradientRotation;

            VertexOutput ComputeVertex(VertexInput vertexInput)
            {
                VertexOutput vertexOutput;
                vertexOutput.Vertex = UnityObjectToClipPos(vertexInput.Vertex);
                vertexOutput.TexCoord = vertexInput.TexCoord;
                return vertexOutput;
            }

            float4 ComputeFragment(VertexOutput vertexOutput) : SV_Target
            {
                float2 centeredUV = vertexOutput.TexCoord - 0.5;
                float dist = length(centeredUV) * 2.0;
                float aa = fwidth(dist) * 0.5;
                float outerEdge = _Size;
                float innerEdge = _Size - _OutlineWidth;
                float circle = 1.0 - smoothstep(innerEdge - aa, innerEdge + aa, dist);
                float mask = 1.0 - smoothstep(outerEdge - aa, outerEdge + aa, dist);

                float4 outlineColor = _OutlineColor;
                float2 dir = normalize(centeredUV);

                float rotationRad = _OutlineGradientRotation * (3.14159265 / 180.0);
                float s = sin(rotationRad);
                float c = cos(rotationRad);
                float2 rotatedDir = float2(dir.x * c - dir.y * s, dir.x * s + dir.y * c);

                float radialGradient = rotatedDir.x * 0.5 + 0.5;
                radialGradient = pow(radialGradient, _OutlineGradientPower);
                float gradient = lerp(1.0, radialGradient, _OutlineGradient);
                outlineColor.a *= gradient * mask;

                float4 premultipliedOutline = float4(outlineColor.rgb * outlineColor.a, outlineColor.a);
                float4 premultipliedFill = float4(_Color.rgb * _Color.a, _Color.a);
                float4 finalColor = lerp(premultipliedOutline, premultipliedFill, circle);
                finalColor.a *= mask;

                return finalColor;
            }
            ENDCG
        }
    }
}
