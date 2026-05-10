Shader "GPUBoids/Environment/Water Surface"
{
    Properties
    {
        _ShallowColor ("Shallow Color", Color) = (0.22, 0.70, 0.74, 0.62)
        _DeepColor ("Deep Color", Color) = (0.02, 0.14, 0.28, 0.78)
        _FoamColor ("Foam Color", Color) = (0.86, 0.96, 0.94, 1)
        _WaveAmplitude ("Wave Amplitude", Range(0, 2)) = 0.28
        _WaveFrequency ("Wave Frequency", Range(0.01, 2)) = 0.22
        _WaveSpeed ("Wave Speed", Range(0, 5)) = 0.9
        _FresnelPower ("Fresnel Power", Range(0.5, 8)) = 3.0
        _Alpha ("Alpha", Range(0, 1)) = 0.66
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _DeepColor;
                half4 _FoamColor;
                float _WaveAmplitude;
                float _WaveFrequency;
                float _WaveSpeed;
                float _FresnelPower;
                float _Alpha;
            CBUFFER_END

            float Wave(float2 position, float phase)
            {
                float a = sin(dot(position, float2(1.0, 0.35)) * _WaveFrequency + phase);
                float b = sin(dot(position, float2(-0.45, 1.0)) * _WaveFrequency * 1.7 + phase * 1.31);
                return (a + b * 0.55) * _WaveAmplitude;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionOS = input.positionOS;
                float phase = _Time.y * _WaveSpeed;
                positionOS.y += Wave(positionOS.xz, phase);

                float eps = 0.25;
                float hL = Wave(positionOS.xz - float2(eps, 0), phase);
                float hR = Wave(positionOS.xz + float2(eps, 0), phase);
                float hD = Wave(positionOS.xz - float2(0, eps), phase);
                float hU = Wave(positionOS.xz + float2(0, eps), phase);
                float3 normalOS = normalize(float3(hL - hR, eps * 2.0, hD - hU));

                output.positionWS = TransformObjectToWorld(positionOS);
                output.normalWS = TransformObjectToWorldNormal(normalOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDir = GetWorldSpaceNormalizeViewDir(input.positionWS);
                Light light = GetMainLight();

                float fresnel = pow(1.0 - saturate(dot(viewDir, normalWS)), _FresnelPower);
                float waveLines = sin((input.uv.x + input.uv.y) * 80.0 + _Time.y * _WaveSpeed * 4.0);
                float foam = smoothstep(0.86, 1.0, waveLines * 0.5 + 0.5) * 0.18;
                float ndotl = saturate(dot(normalWS, light.direction));

                half3 baseColor = lerp(_ShallowColor.rgb, _DeepColor.rgb, saturate(input.uv.y * 0.75 + fresnel));
                baseColor += light.color * ndotl * 0.12;
                baseColor = lerp(baseColor, _FoamColor.rgb, foam);

                return half4(baseColor, saturate(_Alpha + fresnel * 0.18 + foam * 0.25));
            }
            ENDHLSL
        }
    }

    FallBack Off
}
