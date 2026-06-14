Shader "ProjectIO/FogOfWar/Overlay"
{
    Properties
    {
        _VisionMask ("Vision Mask", 2D) = "black" {}
        _FogColor ("Fog Color", Color) = (0, 0, 0, 1)
        _FogDensity ("Fog Density", Range(0, 1)) = 0.75
        _FogBounds ("Fog Bounds", Vector) = (0, 0, 512, 512)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+100"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_VisionMask);
            SAMPLER(sampler_VisionMask);

            CBUFFER_START(UnityPerMaterial)
                half4 _FogColor;
                half _FogDensity;
                float4 _FogBounds;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv;
                uv.x = (input.positionWS.x - (_FogBounds.x - _FogBounds.z * 0.5)) / max(0.0001, _FogBounds.z);
                uv.y = (input.positionWS.z - (_FogBounds.y - _FogBounds.w * 0.5)) / max(0.0001, _FogBounds.w);

                half vision = SAMPLE_TEXTURE2D(_VisionMask, sampler_VisionMask, uv).r;
                vision = saturate(vision);

                half alpha = _FogColor.a * _FogDensity * (1.0 - vision);
                return half4(_FogColor.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
