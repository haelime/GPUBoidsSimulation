Shader "Hidden/GPUBoids/BoidsRender"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.75, 0.92, 1.0, 1.0)
        _MainTex ("Albedo", 2D) = "white" {}
        _Smoothness ("Smoothness", Range(0, 1)) = 0.35
        _Metallic ("Metallic", Range(0, 1)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:SetupProcedural

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
            };

            struct BoidData
            {
                float3 velocity;
                float3 position;
            };

            StructuredBuffer<BoidData> _BoidDataBuffer;

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _MainTex_ST;
                float3 _ObjectScale;
                float _Smoothness;
                float _Metallic;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            void SetupProcedural()
            {
            }

            void BuildBoidBasis(float3 velocity, out float3 right, out float3 up, out float3 forward)
            {
                forward = length(velocity) > 0.0001 ? normalize(velocity) : float3(0.0, 0.0, 1.0);
                float3 worldUp = abs(forward.y) > 0.96 ? float3(1.0, 0.0, 0.0) : float3(0.0, 1.0, 0.0);
                right = normalize(cross(worldUp, forward));
                up = normalize(cross(forward, right));
            }

            Varyings Vert(Attributes input, uint instanceID : SV_InstanceID)
            {
                Varyings output;

                BoidData boid = _BoidDataBuffer[instanceID];

                float3 right;
                float3 up;
                float3 forward;
                BuildBoidBasis(boid.velocity, right, up, forward);

                float3 scaledPosition = input.positionOS * _ObjectScale;
                float3 positionWS = boid.position
                    + right * scaledPosition.x
                    + up * scaledPosition.y
                    + forward * scaledPosition.z;

                float3 normalWS = normalize(
                    right * input.normalOS.x
                    + up * input.normalOS.y
                    + forward * input.normalOS.z
                );

                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.positionWS = positionWS;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _BaseColor;
                Light mainLight = GetMainLight();
                half ndotl = saturate(dot(normalize(input.normalWS), mainLight.direction));
                half3 ambient = SampleSH(normalize(input.normalWS));
                half3 diffuse = albedo.rgb * (ambient + mainLight.color * (0.18 + ndotl * 0.82));
                half3 specular = LightingSpecular(mainLight.color, mainLight.direction, normalize(input.normalWS), GetWorldSpaceNormalizeViewDir(input.positionWS), half4(_Smoothness, _Smoothness, _Smoothness, _Smoothness), _Smoothness);
                return half4(diffuse + specular * (1.0 - _Metallic), albedo.a);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
