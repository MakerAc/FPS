Shader "Custom/URP/EmissionShader"
{
    Properties
    {
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)

            // 自发光相关属性
            [HDR] _EmissionColor("Emission Color", Color) = (0, 0, 0, 1)
            _EmissionIntensity("Emission Intensity", Range(0, 10)) = 1
            _EmissionMap("Emission Map", 2D) = "white" {}

        // 高级控制
        _EmissionScrollSpeed("Emission Scroll Speed", Vector) = (0, 0, 0, 0)
        _EmissionPulseSpeed("Emission Pulse Speed", Float) = 0
        _EmissionPulseMin("Emission Pulse Min", Range(0, 1)) = 0.5
        _EmissionPulseMax("Emission Pulse Max", Range(0, 2)) = 1.5

            // 其他标准属性
            _Smoothness("Smoothness", Range(0, 1)) = 0.5
            _Metallic("Metallic", Range(0, 1)) = 0
    }

        SubShader
        {
            Tags
            {
                "RenderType" = "Opaque"
                "RenderPipeline" = "UniversalPipeline"
                "UniversalMaterialType" = "Lit"
                "IgnoreProjector" = "True"
            }

            LOD 300

            Pass
            {
                Name "ForwardLit"
                Tags { "LightMode" = "UniversalForward" }

                HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag

            // 必要的编译指令
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            // 确保在URP中使用核心库
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 texcoord : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float fogFactor : TEXCOORD3;
            };

            // 属性变量声明
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_EmissionMap);
            SAMPLER(sampler_EmissionMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _EmissionColor;
                float _EmissionIntensity;
                float2 _EmissionScrollSpeed;
                float _EmissionPulseSpeed;
                float _EmissionPulseMin;
                float _EmissionPulseMax;
                float _Smoothness;
                float _Metallic;
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

                // UV变换
                output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);

                // 雾效因子
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);

                return output;
            }

            // 脉冲函数
            float GetPulseFactor()
            {
                if (_EmissionPulseSpeed <= 0)
                    return 1.0;

                float pulse = sin(_Time.y * _EmissionPulseSpeed) * 0.5 + 0.5;
                return lerp(_EmissionPulseMin, _EmissionPulseMax, pulse);
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 采样基础贴图
                half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half4 albedo = baseMap * _BaseColor;

                // 采样自发光贴图
                float2 emissionUV = input.uv;
                if (_Time.y > 0)
                {
                    emissionUV += _EmissionScrollSpeed * _Time.y;
                }
                half4 emissionMap = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, emissionUV);

                // 计算脉冲效果
                float pulseFactor = GetPulseFactor();

                // 计算最终的自发光颜色
                half4 emission = emissionMap * _EmissionColor;
                emission.rgb *= _EmissionIntensity * pulseFactor;

                // 获取主光源
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));

                // 计算光照
                float3 diffuse = mainLight.color * mainLight.shadowAttenuation * mainLight.distanceAttenuation;
                float3 ambient = SampleSH(input.normalWS);

                // 组合最终颜色
                half3 finalColor = albedo.rgb * (diffuse + ambient) + emission.rgb;

                // 应用雾效
                finalColor = MixFog(finalColor, input.fogFactor);

                return half4(finalColor, albedo.a);
            }
            ENDHLSL
        }

            // 阴影投射Pass
            Pass
            {
                Name "ShadowCaster"
                Tags { "LightMode" = "ShadowCaster" }

                ZWrite On
                ZTest LEqual
                ColorMask 0
                Cull Back

                HLSLPROGRAM
                #pragma vertex ShadowPassVertex
                #pragma fragment ShadowPassFragment

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
                ENDHLSL
            }

                // 深度Only Pass
                Pass
                {
                    Name "DepthOnly"
                    Tags { "LightMode" = "DepthOnly" }

                    ZWrite On
                    ColorMask 0
                    Cull Back

                    HLSLPROGRAM
                    #pragma vertex DepthOnlyVertex
                    #pragma fragment DepthOnlyFragment

                    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
                    #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
                    ENDHLSL
                }
        }

            FallBack "Universal Render Pipeline/Lit"
                CustomEditor "UnityEditor.ShaderGraph.Inspector.ShaderGraphInspector"
}