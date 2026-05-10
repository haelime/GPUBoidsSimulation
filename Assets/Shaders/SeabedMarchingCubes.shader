Shader "GPUBoids/Environment/Seabed Marching Cubes"
{
    Properties
    {
        _SandColor ("Sand Color", Color) = (0.47, 0.42, 0.32, 1)
        _RockColor ("Rock Color", Color) = (0.20, 0.25, 0.24, 1)
        _AlgaeColor ("Algae Color", Color) = (0.12, 0.28, 0.22, 1)
        _NoiseScale ("Noise Scale", Range(0.01, 1)) = 0.12
        _SlopeBlend ("Slope Blend", Range(0.01, 1)) = 0.45
        _DepthFade ("Depth Fade", Range(0, 0.2)) = 0.035
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _SandColor;
                half4 _RockColor;
                half4 _AlgaeColor;
                float _NoiseScale;
                float _SlopeBlend;
                float _DepthFade;
            CBUFFER_END

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float slope = saturate((1.0 - normalWS.y) / _SlopeBlend);
                float noise = ValueNoise(input.positionWS.xz * _NoiseScale);
                float depth = saturate(-input.positionWS.y * _DepthFade);

                half3 baseColor = lerp(_SandColor.rgb, _RockColor.rgb, slope);
                baseColor = lerp(baseColor, _AlgaeColor.rgb, saturate((noise - 0.58) * 2.2) * (1.0 - slope));
                baseColor *= lerp(1.12, 0.72, depth);

                Light light = GetMainLight();
                half ndotl = saturate(dot(normalWS, light.direction));
                half3 ambient = SampleSH(normalWS);
                half3 color = baseColor * (ambient + light.color * (0.25 + ndotl * 0.75));
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
