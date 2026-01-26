Shader "Custom/Simple Emission"
{
    Properties
    {
        _Color("Main Color", Color) = (1,1,1,1)
        [HDR] _EmissionColor("Emission Color", Color) = (1,1,1,1)
        _EmissionIntensity("Emission Intensity", Range(0, 10)) = 1
    }

        SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _EmissionColor;
                float _EmissionIntensity;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                // 顶点变换
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;

                // 法线变换
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                output.normalWS = normalInput.normalWS;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 获取主光源
                Light mainLight = GetMainLight();
                float3 lightColor = mainLight.color;
                float3 lightDir = mainLight.direction;

                // 计算简单的漫反射光照
                float NdotL = saturate(dot(normalize(input.normalWS), lightDir));
                float3 diffuse = lightColor * NdotL;

                // 计算自发光
                float3 emission = _EmissionColor.rgb * _EmissionIntensity;

                // 最终颜色 = 基础颜色 × 光照 + 自发光
                float3 finalColor = _Color.rgb * (diffuse + SampleSH(input.normalWS)) + emission;

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}