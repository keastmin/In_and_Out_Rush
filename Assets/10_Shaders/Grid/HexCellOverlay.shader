Shader "GridVisualize/HexCellOverlay"
{
    Properties
    {
        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Color", Color) = (1,1,1,1)

        _GridOriginWS ("Grid Origin (World)", Vector) = (0,0,0,0)
        _HexSize ("Hex Size", Float) = 1.0
        _GridCols ("Grid Cols", Float) = 20
        _GridRows ("Grid Rows", Float) = 20

        _StateTex ("Cell State Tex", 2D) = "black" {}
        _PreviewTex ("Preview Tex", 2D) = "black" {}
        _BuffTex ("Buff Tex", 2D) = "black" {}

        _NoneColor ("None Color", Color) = (0.2,0.45,1,0.28)
        _BuildColor ("Build Color", Color) = (1,0.2,0.2,0.30)
        _PreviewTintColor ("Preview Tint Color", Color) = (0.1,1,0.2,0.45)
        _CellFill ("Cell Fill", Range(0,1)) = 1
        _StateOverlayEnabled ("State Overlay Enabled", Float) = 1
        _PreviewEnabled ("Preview Enabled", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalRenderPipeline" }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            float4 _BaseMap_ST;
            float4 _BaseColor;

            TEXTURE2D(_StateTex);
            SAMPLER(sampler_StateTex);
            TEXTURE2D(_PreviewTex);
            SAMPLER(sampler_PreviewTex);
            TEXTURE2D(_BuffTex);
            SAMPLER(sampler_BuffTex);

            float4 _GridOriginWS;
            float _HexSize;
            float _GridCols;
            float _GridRows;

            float4 _NoneColor;
            float4 _BuildColor;
            float4 _PreviewTintColor;
            float _CellFill;
            float _StateOverlayEnabled;
            float _PreviewEnabled;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS = NormalizeNormalPerVertex(normalInputs.normalWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            float2 CubeRoundAxial(float2 qr)
            {
                float q = qr.x;
                float r = qr.y;
                float s = -q - r;

                float rq = round(q);
                float rr = round(r);
                float rs = round(s);

                float qDiff = abs(rq - q);
                float rDiff = abs(rr - r);
                float sDiff = abs(rs - s);

                if (qDiff > rDiff && qDiff > sDiff)
                {
                    rq = -rr - rs;
                }
                else if (rDiff > sDiff)
                {
                    rr = -rq - rs;
                }

                return float2(rq, rr);
            }

            float IsInsideFlatTopHex(float2 local, float hexSize)
            {
                const float SQRT3 = 1.73205080757;
                float h = hexSize * (SQRT3 * 0.5);
                float2 a = abs(local);

                float inAABB = step(a.x, hexSize + 1e-4) * step(a.y, h + 1e-4);
                float inDiagonal = step(a.y + SQRT3 * a.x, SQRT3 * hexSize + 1e-4);
                return inAABB * inDiagonal;
            }

            half3 EvaluateLighting(float3 positionWS, float3 normalWS, half3 albedo)
            {
                float3 n = normalize(normalWS);
                Light mainLight = GetMainLight();
                half ndl = saturate(dot(n, mainLight.direction));
                half3 lit = albedo * (0.2h + ndl * mainLight.color);

                #ifdef _ADDITIONAL_LIGHTS
                uint addLightCount = GetAdditionalLightsCount();
                for (uint lightIndex = 0; lightIndex < addLightCount; lightIndex++)
                {
                    Light light = GetAdditionalLight(lightIndex, positionWS);
                    half ndlAdd = saturate(dot(n, light.direction));
                    lit += albedo * ndlAdd * light.color * light.distanceAttenuation * light.shadowAttenuation;
                }
                #endif

                return lit;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                const float SQRT3 = 1.73205080757;

                half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                half4 baseColor = baseSample * _BaseColor;

                float2 p = IN.positionWS.xz - _GridOriginWS.xz;

                // Pixel -> axial (flat-top), then cube-round to nearest hex.
                float qf = (2.0 / 3.0) * p.x / _HexSize;
                float rf = ((-1.0 / 3.0) * p.x + (SQRT3 / 3.0) * p.y) / _HexSize;
                float2 qr = CubeRoundAxial(float2(qf, rf));

                float q = qr.x; // row axis used on x in current grid layout
                float r = qr.y;

                float parity = fmod(abs(q), 2.0);
                float row = r + (q - parity) * 0.5;

                float validRow = step(0.0, q) * step(q, _GridRows - 1.0);
                float validCol = step(0.0, row) * step(row, _GridCols - 1.0);
                float validCell = validRow * validCol;

                float2 centerXZ;
                centerXZ.x = _HexSize * 1.5 * q;
                centerXZ.y = _HexSize * SQRT3 * (r + q * 0.5);

                float2 local = p - centerXZ;
                float insideOuter = IsInsideFlatTopHex(local, _HexSize) * validCell;

                // 0 = border-only (large inner hole), 1 = fully filled cell.
                float innerScale = saturate(1.0 - _CellFill);
                float innerSize = _HexSize * innerScale;
                float innerEnabled = step(1e-4, innerSize);
                float insideInner = IsInsideFlatTopHex(local, innerSize) * innerEnabled;
                float inside = insideOuter * (1.0 - insideInner);

                float2 cellUV = float2((q + 0.5) / max(_GridRows, 1.0), (row + 0.5) / max(_GridCols, 1.0));
                half stateValue = SAMPLE_TEXTURE2D(_StateTex, sampler_StateTex, cellUV).r;
                half previewValue = SAMPLE_TEXTURE2D(_PreviewTex, sampler_PreviewTex, cellUV).r;
                half4 buffValue = SAMPLE_TEXTURE2D(_BuffTex, sampler_BuffTex, cellUV);

                // 1) Buff fill: whole cell interior.
                half buffMask = (half)insideOuter * step(0.001h, buffValue.a);
                half3 buffLayer = lerp(baseColor.rgb, buffValue.rgb, buffValue.a);
                half3 afterBuff = lerp(baseColor.rgb, buffLayer, buffMask);

                // 2) CellState overlay: ring only, always composited from base texture color.
                half4 stateColor = lerp(_NoneColor, _BuildColor, step(0.5h, stateValue));
                half3 stateLayer = lerp(baseColor.rgb, stateColor.rgb, stateColor.a);
                half3 mixedAlbedo = lerp(afterBuff, stateLayer, (half)(inside * saturate(_StateOverlayEnabled)));

                // 3) Build range preview: tint current cell-state color toward green.
                half previewMask = (half)(inside * saturate(_StateOverlayEnabled) * saturate(_PreviewEnabled)) * step(0.5h, previewValue);
                half3 previewLayer = lerp(mixedAlbedo, _PreviewTintColor.rgb, _PreviewTintColor.a);
                mixedAlbedo = lerp(mixedAlbedo, previewLayer, previewMask);

                half3 litColor = EvaluateLighting(IN.positionWS, IN.normalWS, mixedAlbedo);

                return half4(litColor, baseColor.a);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
