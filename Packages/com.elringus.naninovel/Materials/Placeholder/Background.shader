Shader "Naninovel/Placeholder/Background"
{
    Properties
    {
        _Radial ("Radial", Float) = 0
        _Angle ("Gradient Angle", Range(0, 360)) = 0
        _ColorCount ("Color Count", Int) = 2
        _ScrollSpeed ("Scroll Speed", Float) = 0
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
        }

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

            float _Angle;
            int _ColorCount;
            float _ScrollSpeed;
            float _Radial;

            #define MAX_GRADIENT_COLORS 32
            float4 _Colors[MAX_GRADIENT_COLORS];

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
                float angleRad = radians(_Angle);
                float2 gradientDir = float2(cos(angleRad), sin(angleRad));
                float projection = dot(centeredUV, gradientDir);
                float maxProjection = 0.5 * (abs(gradientDir.x) + abs(gradientDir.y));
                float linearPos = (projection / maxProjection) * 0.5 + 0.5;
                float radialPos = length(centeredUV) * 2.0;
                float unwrappedPos = lerp(linearPos, radialPos, _Radial);
                float scrolledPos = unwrappedPos + _ScrollSpeed * _Time.y;
                float gradientPos = frac(scrolledPos);

                int colorCount = clamp(_ColorCount, 1, MAX_GRADIENT_COLORS);
                float segmentFloat = gradientPos * max(0, colorCount - 1);
                int segmentIndex = floor(segmentFloat);
                float segmentLerp = segmentFloat - segmentIndex;
                int maxIndex = max(0, colorCount - 1);
                segmentIndex = clamp(segmentIndex, 0, maxIndex);
                int nextIndex = (int)((uint(segmentIndex) + 1u) % uint(colorCount));

                float4 color1 = _Colors[segmentIndex];
                float4 color2 = _Colors[nextIndex];

                #if !UNITY_COLORSPACE_GAMMA
                color1.rgb = GammaToLinearSpace(color1.rgb);
                color2.rgb = GammaToLinearSpace(color2.rgb);
                #endif

                return lerp(color1, color2, smoothstep(0.0, 1.0, segmentLerp));
            }
            ENDCG
        }
    }
}
