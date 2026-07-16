Shader "Custom/URP/Cloud"
{
    Properties
    {
        _VoluimeTex  ("3D Noise Texture", 3D) = "white" {}
        _StepSize    ("Ray Step Size",    Float) = 0.05
        _MaxSteps    ("Max Steps",        Int)   = 100
        _DensityScale("Density Scale",    Float) = 5.0
        _CloudColor  ("Cloud Color",      Color) = (1, 1, 1, 1)
        _Brightness  ("Brightness",       Float) = 1.0

        _WindDir     ("Wind Direction (XYZ)",  Vector) = (1, 0, 0.3, 0)
        _WindSpeed   ("Wind Speed",            Float)  = 0.05
        _DetailSpeed ("Detail Layer Speed",    Float)  = 0.13
        _DetailScale ("Detail Blend",          Float)  = 0.35

        [Header(Render State)]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dest Blend",   Float) = 10
        [Enum(Off, 0, On, 1)]                   _ZWrite   ("Z Write",       Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)]  _Cull     ("Cull Mode",     Float) = 2
    }

    SubShader
    {
        Tags
        {
            "Queue"          = "Transparent"
            "RenderType"     = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector"= "True"
        }
        LOD 100

        Blend  [_SrcBlend][_DstBlend]
        ZWrite [_ZWrite]
        Cull   [_Cull]

        Pass
        {
            Name "CloudVolumePass"

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 objPos      : TEXCOORD0;  
                float3 viewDirOS   : TEXCOORD1; 
                float  fogFactor   : TEXCOORD2;
            };


            TEXTURE3D(_VoluimeTex);
            SAMPLER(sampler_VoluimeTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _VoluimeTex_ST; 
                float  _StepSize;
                int    _MaxSteps;
                float  _DensityScale;
                float4 _CloudColor;
                float4 _WindDir;
                float  _WindSpeed;
                float  _DetailSpeed;
                float  _DetailScale;
                float  _Brightness;
            CBUFFER_END


            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.objPos      = IN.positionOS.xyz;


                float3 camPosWS = _WorldSpaceCameraPos;
                float3 camPosOS = mul(unity_WorldToObject, float4(camPosWS, 1.0)).xyz;
                OUT.viewDirOS   = normalize(IN.positionOS.xyz - camPosOS);


                OUT.fogFactor = ComputeFogFactor(OUT.positionHCS.z);

                return OUT;
            }


            half4 frag(Varyings IN) : SV_Target
            {
                // ===== 1. 射线方向 =====
                // unity_OrthoParams.w : 1 = 正交  0 = 透视
                float isOrtho = unity_OrthoParams.w;

                // 正交：所有像素共享相机朝前方向
                // UNITY_MATRIX_V 第 2 行 = view-space Z 轴（world-space 反前向），取反得正前向
                float3 camFwdWS = -UNITY_MATRIX_V[2].xyz;
                float3 camFwdOS = normalize(mul((float3x3)unity_WorldToObject, camFwdWS));

                float3 rd = lerp(IN.viewDirOS, camFwdOS, isOrtho);

                // ===== 2. 射线起点 =====
                float3 camPosWS = _WorldSpaceCameraPos;
                float3 camPosOS = mul(unity_WorldToObject, float4(camPosWS, 1.0)).xyz;

                // 透视 & 相机在盒内：从相机位置开始；否则从盒表面顶点开始
                // 正交：始终从表面顶点开始（相机在物体空间不是一个确定点）
                bool isInsideBox = (abs(camPosOS.x) < 0.5 &&
                                    abs(camPosOS.y) < 0.5 &&
                                    abs(camPosOS.z) < 0.5);
                float3 ro = (isInsideBox && isOrtho < 0.5) ? camPosOS : IN.objPos;

                // ===== 3. 动画偏移 =====
                float3 windDir    = normalize(_WindDir.xyz);
                float3 windOffset = windDir * _Time.y * _WindSpeed;
                // 细节层以不同速度、轻微偏转方向滚动
                float3 detailOffset = float3(-windDir.z, 0, windDir.x) * _Time.y * _DetailSpeed;

                // ===== 4. Ray march =====
                float  transmittance = 1.0;
                float3 finalColor    = float3(0, 0, 0);
                float  alpha         = 0.0;

                for (int step = 0; step < _MaxSteps; step++)
                {
                    // 物体空间 [-0.5, 0.5] → UV [0, 1]
                    float3 uvw = ro + 0.5;

                    // 超出单位立方体则停止
                    if (any(uvw < -0.001) || any(uvw > 1.001))
                        break;

                    // --- 基础噪声（R 通道）---
                    float3 baseUVW   = frac(uvw + windOffset);
                    float  baseNoise = SAMPLE_TEXTURE3D_LOD(
                                            _VoluimeTex,
                                            sampler_VoluimeTex,
                                            baseUVW, 0).r;

                    // --- 细节噪声（G 通道，需在 Compute 里写入，否则 = baseNoise）---
                    float3 detailUVW   = frac(uvw * 1.5 + detailOffset);
                    float  detailNoise = SAMPLE_TEXTURE3D_LOD(
                                            _VoluimeTex,
                                            sampler_VoluimeTex,
                                            detailUVW, 0).g;

                    // 混合：基础 + 细节侵蚀
                    float density = baseNoise - detailNoise * _DetailScale;
                    density = saturate(density) * _DensityScale;

                    if (density > 0.01)
                    {
                        float a    = exp(-density * _StepSize);
                        transmittance *= a;
                        alpha         += (1.0 - a) * transmittance;
                        finalColor    += _CloudColor.rgb * density * _StepSize * transmittance;
                    }

                    // 完全不透明，提前退出
                    if (transmittance < 0.01)
                    {
                        alpha = 1.0;
                        break;
                    }

                    ro += rd * _StepSize;
                }

                half4 col = half4(finalColor * _Brightness, saturate(alpha));
                // 应用 URP Fog
                col.rgb = MixFog(col.rgb, IN.fogFactor);
                return col;
            }
            ENDHLSL
        }
    }

    // Fallback 为空：在 Built-in 管线下不渲染
    FallBack Off
}
