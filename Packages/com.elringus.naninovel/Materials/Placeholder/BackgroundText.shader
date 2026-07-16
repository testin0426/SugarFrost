Shader "Naninovel/Placeholder/BackgroundText"
{
    Properties
    {
        _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
        _Opacity ("Opacity", float) = 1
        _TileScale ("Tile Scale", Vector) = (1, 1, 0, 0)
        _ScrollSpeed ("Scroll Speed", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
        }

        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #include "UnityCG.cginc"

            #pragma vertex ComputeVertex
            #pragma fragment ComputeFragment

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Opacity;
            float4 _TileScale;
            float4 _ScrollSpeed;

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

            VertexOutput ComputeVertex(VertexInput vertexInput)
            {
                VertexOutput vertexOutput;
                vertexOutput.Vertex = UnityObjectToClipPos(vertexInput.Vertex);
                float2 scrollOffset = _ScrollSpeed.xy * _Time.y;
                float2 scrolledUv = TRANSFORM_TEX(vertexInput.TexCoord, _MainTex) + scrollOffset;
                vertexOutput.TexCoord = scrolledUv * _TileScale.xy;
                return vertexOutput;
            }

            fixed4 ComputeFragment(VertexOutput vertexOutput) : SV_Target
            {
                float2 tiledUv = vertexOutput.TexCoord;
                tiledUv.x += fmod(floor(tiledUv.y), 2.0) * 0.5; // offset odd rows for brick pattern
                fixed4 color = tex2D(_MainTex, tiledUv);
                float a = _Opacity;
                #if !UNITY_COLORSPACE_GAMMA
                a *= 0.65;
                #endif
                color *= a;
                return color;
            }
            ENDCG
        }
    }
}
