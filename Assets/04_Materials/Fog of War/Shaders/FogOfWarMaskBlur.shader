Shader "ProjectIO/FogOfWar/MaskBlur"
{
    Properties
    {
        _MainTex ("Source", 2D) = "black" {}
        _BlurStep ("Blur Step", Vector) = (0.001, 0, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BlurStep;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 stepUv = _BlurStep.xy;
                half value = 0;
                value += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + stepUv * -4.0).r * 0.05;
                value += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + stepUv * -3.0).r * 0.09;
                value += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + stepUv * -2.0).r * 0.12;
                value += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + stepUv * -1.0).r * 0.15;
                value += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).r * 0.18;
                value += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + stepUv * 1.0).r * 0.15;
                value += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + stepUv * 2.0).r * 0.12;
                value += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + stepUv * 3.0).r * 0.09;
                value += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + stepUv * 4.0).r * 0.05;
                return half4(value, value, value, value);
            }
            ENDHLSL
        }
    }
}
