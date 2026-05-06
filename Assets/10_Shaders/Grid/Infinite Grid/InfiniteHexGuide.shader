Shader "GridVisualize/InfiniteHexGuide"
{
    Properties
    {
        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Color", Color) = (1,1,1,1)
        [Normal] _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Range(0,2)) = 1
        _ParallaxMap ("Height Map", 2D) = "black" {}
        _Parallax ("Height Strength", Range(0,0.08)) = 0
        _MetallicGlossMap ("Metallic Map (R) Smoothness (A)", 2D) = "black" {}
        _Metallic ("Metallic", Range(0,1)) = 0
        _Smoothness ("Smoothness", Range(0,1)) = 0
        _OcclusionMap ("Occlusion Map", 2D) = "white" {}
        _OcclusionStrength ("Occlusion Strength", Range(0,1)) = 1
        [HDR] _EmissionColor ("Emission Color", Color) = (0,0,0,0)
        _EmissionMap ("Emission Map", 2D) = "black" {}

        _GridOriginWS ("Grid Origin (World)", Vector) = (0,0,0,0)
        _HexSize ("Hex Size", Float) = 1.6
        _PrimaryGuideColor ("Primary Guide Color", Color) = (0.2,0.45,1,0.28)
        _SecondaryGuideColor ("Secondary Guide Color", Color) = (1,0.2,0.2,0.30)
        _PreviewValidGuideColor ("Preview Valid Guide Color", Color) = (0.1,1,0.2,0.35)
        _PreviewBlockedGuideColor ("Preview Blocked Guide Color", Color) = (1,0.55,0.1,0.42)
        _CellFill ("Cell Fill", Range(0,1)) = 0.2
        _StateOverlayEnabled ("State Overlay Enabled", Float) = 1
        _OccupiedCellsTex ("Occupied Cells Tex", 2D) = "black" {}
        _PreviewCellsTex ("Preview Cells Tex", 2D) = "black" {}
        _BlockedPreviewCellsTex ("Blocked Preview Cells Tex", 2D) = "black" {}
        _BuffCellsTex ("Buff Cells Tex", 2D) = "black" {}
        _BuffCellColorsTex ("Buff Cell Colors Tex", 2D) = "black" {}
        _TerritoryVerticesTex ("Territory Vertices Tex", 2D) = "black" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

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
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ParallaxMapping.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                half4 tangentWS : TEXCOORD3;
            };

            TEXTURE2D(_ParallaxMap);
            SAMPLER(sampler_ParallaxMap);
            TEXTURE2D(_MetallicGlossMap);
            SAMPLER(sampler_MetallicGlossMap);
            TEXTURE2D(_OcclusionMap);
            SAMPLER(sampler_OcclusionMap);
            #define MAX_OCCUPIED_CELLS 64
            #define MAX_PREVIEW_CELLS 64
            #define MAX_BLOCKED_PREVIEW_CELLS 64
            #define MAX_BUFF_CELLS 64
            #define MAX_TERRITORY_VERTICES 64

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _BaseColor;
            float4 _EmissionColor;
            float4 _GridOriginWS;
            float4 _PrimaryGuideColor;
            float4 _SecondaryGuideColor;
            float4 _PreviewValidGuideColor;
            float4 _PreviewBlockedGuideColor;
            float _BumpScale;
            float _Parallax;
            float _Metallic;
            float _Smoothness;
            float _OcclusionStrength;
            float _HexSize;
            float _CellFill;
            float _StateOverlayEnabled;
            float _OccupiedCellCount;
            float _PreviewCellCount;
            float _BlockedPreviewCellCount;
            float _BuffCellCount;
            float _TerritoryVertexCount;
            CBUFFER_END
            TEXTURE2D(_OccupiedCellsTex);
            SAMPLER(sampler_OccupiedCellsTex);
            TEXTURE2D(_PreviewCellsTex);
            SAMPLER(sampler_PreviewCellsTex);
            TEXTURE2D(_BlockedPreviewCellsTex);
            SAMPLER(sampler_BlockedPreviewCellsTex);
            TEXTURE2D(_BuffCellsTex);
            SAMPLER(sampler_BuffCellsTex);
            TEXTURE2D(_BuffCellColorsTex);
            SAMPLER(sampler_BuffCellColorsTex);
            TEXTURE2D(_TerritoryVerticesTex);
            SAMPLER(sampler_TerritoryVerticesTex);

            float2 GetDataUV(int index, float capacity)
            {
                return float2(((float)index + 0.5) / capacity, 0.5);
            }

            // 오브젝트 좌표를 월드 좌표화 화면 좌표로 바꿈
            // 노멀과 탄젠트를 준비함
            // UV를 계산해서 다음 단계로 넘김
            // 요약: 픽셀 단계에서 계산하기 좋게 기본 재료를 미리 챙겨 놓는 함수
            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionHCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = NormalizeNormalPerVertex(normalInputs.normalWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.tangentWS = half4(normalInputs.tangentWS.xyz, input.tangentOS.w * GetOddNegativeScale());
                return output;
            }

            // 육각형 좌표를 반올림해서 가장 가까운 칸을 찾음
            // 월드 위치를 육각 좌표로 바꾸면 소수점이 나오지만 실제 칸 번호는 정수여야 하므로 가장 가까운 육각형 칸으로 반올림
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

            float IsInsideFlatTopHex(float2 localPosition, float hexSize)
            {
                const float SQRT3 = 1.73205080757;
                float halfHeight = hexSize * (SQRT3 * 0.5);
                float2 localAbs = abs(localPosition);

                float inBounds = step(localAbs.x, hexSize + 1e-4) * step(localAbs.y, halfHeight + 1e-4);
                float inDiagonal = step(localAbs.y + SQRT3 * localAbs.x, SQRT3 * hexSize + 1e-4);
                return inBounds * inDiagonal;
            }

            float IsOccupiedCell(float2 axial)
            {
                int occupiedCount = (int)_OccupiedCellCount;

                [loop]
                for (int i = 0; i < MAX_OCCUPIED_CELLS; i++)
                {
                    if (i >= occupiedCount)
                    {
                        break;
                    }

                    float2 occupiedAxial = SAMPLE_TEXTURE2D_LOD(_OccupiedCellsTex, sampler_OccupiedCellsTex, GetDataUV(i, MAX_OCCUPIED_CELLS), 0).xy;
                    if (all(abs(occupiedAxial - axial) < 0.01))
                    {
                        return 1.0;
                    }
                }

                return 0.0;
            }

            float IsPreviewCell(float2 axial)
            {
                int previewCount = (int)_PreviewCellCount;

                [loop]
                for (int i = 0; i < MAX_PREVIEW_CELLS; i++)
                {
                    if (i >= previewCount)
                    {
                        break;
                    }

                    float2 previewAxial = SAMPLE_TEXTURE2D_LOD(_PreviewCellsTex, sampler_PreviewCellsTex, GetDataUV(i, MAX_PREVIEW_CELLS), 0).xy;
                    if (all(abs(previewAxial - axial) < 0.01))
                    {
                        return 1.0;
                    }
                }

                return 0.0;
            }

            float IsBlockedPreviewCell(float2 axial)
            {
                int previewCount = (int)_BlockedPreviewCellCount;

                [loop]
                for (int i = 0; i < MAX_BLOCKED_PREVIEW_CELLS; i++)
                {
                    if (i >= previewCount)
                    {
                        break;
                    }

                    float2 previewAxial = SAMPLE_TEXTURE2D_LOD(_BlockedPreviewCellsTex, sampler_BlockedPreviewCellsTex, GetDataUV(i, MAX_BLOCKED_PREVIEW_CELLS), 0).xy;
                    if (all(abs(previewAxial - axial) < 0.01))
                    {
                        return 1.0;
                    }
                }

                return 0.0;
            }

            float4 GetBuffCellColor(float2 axial)
            {
                int buffCount = (int)_BuffCellCount;

                [loop]
                for (int i = 0; i < MAX_BUFF_CELLS; i++)
                {
                    if (i >= buffCount)
                    {
                        break;
                    }

                    float2 buffAxial = SAMPLE_TEXTURE2D_LOD(_BuffCellsTex, sampler_BuffCellsTex, GetDataUV(i, MAX_BUFF_CELLS), 0).xy;
                    if (all(abs(buffAxial - axial) < 0.01))
                    {
                        return SAMPLE_TEXTURE2D_LOD(_BuffCellColorsTex, sampler_BuffCellColorsTex, GetDataUV(i, MAX_BUFF_CELLS), 0);
                    }
                }

                return float4(0, 0, 0, 0);
            }

            float IsCellCenterInTerritory(float2 cellCenterWS)
            {
                int vertexCount = (int)_TerritoryVertexCount;
                if (vertexCount < 3)
                {
                    return 0.0;
                }

                bool isInside = false;
                float2 previous = SAMPLE_TEXTURE2D_LOD(_TerritoryVerticesTex, sampler_TerritoryVerticesTex, GetDataUV(vertexCount - 1, MAX_TERRITORY_VERTICES), 0).xy;

                [loop]
                for (int i = 0; i < MAX_TERRITORY_VERTICES; i++)
                {
                    if (i >= vertexCount)
                    {
                        break;
                    }

                    float2 current = SAMPLE_TEXTURE2D_LOD(_TerritoryVerticesTex, sampler_TerritoryVerticesTex, GetDataUV(i, MAX_TERRITORY_VERTICES), 0).xy;
                    bool intersects = ((current.y > cellCenterWS.y) != (previous.y > cellCenterWS.y)) &&
                                      (cellCenterWS.x < ((previous.x - current.x) * (cellCenterWS.y - current.y) / ((previous.y - current.y) + 1e-5) + current.x));

                    if (intersects)
                    {
                        isInside = !isInside;
                    }

                    previous = current;
                }

                return isInside ? 1.0 : 0.0;
            }

            half3 SampleNormalTS(float2 uv)
            {
                half4 normalSample = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv);
                return UnpackNormalScale(normalSample, _BumpScale);
            }

            float2 ApplyHeightOffset(float2 uv, half3 viewDirTS)
            {
                if (_Parallax <= 0.0001)
                {
                    return uv;
                }

                return uv + ParallaxMapping(TEXTURE2D_ARGS(_ParallaxMap, sampler_ParallaxMap), viewDirTS, _Parallax, uv);
            }

            half SampleOcclusionMap(float2 uv)
            {
                half occlusion = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, uv).g;
                return LerpWhiteTo(occlusion, _OcclusionStrength);
            }

            void BuildSurfaceData(float2 uv, half3 albedo, half alpha, out SurfaceData surfaceData)
            {
                half4 metallicGloss = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, uv);

                surfaceData.albedo = albedo;
                surfaceData.specular = half3(0.0h, 0.0h, 0.0h);
                surfaceData.metallic = saturate(_Metallic + metallicGloss.r);
                surfaceData.smoothness = saturate(max(_Smoothness, metallicGloss.a));
                surfaceData.normalTS = SampleNormalTS(uv);
                surfaceData.emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uv).rgb * _EmissionColor.rgb;
                surfaceData.occlusion = SampleOcclusionMap(uv);
                surfaceData.alpha = alpha;
                surfaceData.clearCoatMask = 0.0h;
                surfaceData.clearCoatSmoothness = 0.0h;
            }

            half3 EvaluateLightingPBR(float3 positionWS, half3 normalWS, half3 viewDirWS, SurfaceData surfaceData)
            {
                BRDFData brdfData;
                InitializeBRDFData(surfaceData, brdfData);

                Light mainLight = GetMainLight();
                half3 litColor = surfaceData.albedo * (0.2h * surfaceData.occlusion);
                litColor += LightingPhysicallyBased(brdfData, mainLight, normalWS, viewDirWS);

                #ifdef _ADDITIONAL_LIGHTS
                uint lightCount = GetAdditionalLightsCount();
                for (uint lightIndex = 0; lightIndex < lightCount; lightIndex++)
                {
                    Light light = GetAdditionalLight(lightIndex, positionWS);
                    litColor += LightingPhysicallyBased(brdfData, light, normalWS, viewDirWS);
                }
                #endif

                return litColor + surfaceData.emission;
            }

            half4 frag(Varyings input) : SV_Target
            {
                const float SQRT3 = 1.73205080757;

                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half3 viewDirTS = GetViewDirectionTangentSpace(input.tangentWS, input.normalWS, viewDirWS);
                float2 materialUV = ApplyHeightOffset(input.uv, viewDirTS);

                half4 baseSample = SampleAlbedoAlpha(materialUV, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
                half4 baseColor = baseSample * _BaseColor;

                float2 positionXZ = input.positionWS.xz - _GridOriginWS.xz;
                float qf = (2.0 / 3.0) * positionXZ.x / _HexSize;
                float rf = ((-1.0 / 3.0) * positionXZ.x + (SQRT3 / 3.0) * positionXZ.y) / _HexSize;
                float2 qr = CubeRoundAxial(float2(qf, rf));

                float2 centerXZ;
                centerXZ.x = _HexSize * 1.5 * qr.x;
                centerXZ.y = _HexSize * SQRT3 * (qr.y + qr.x * 0.5);

                float2 localPosition = positionXZ - centerXZ;
                float outerMask = IsInsideFlatTopHex(localPosition, _HexSize);

                float innerScale = saturate(1.0 - _CellFill);
                float innerSize = _HexSize * innerScale;
                float innerEnabled = step(1e-4, innerSize);
                float innerMask = IsInsideFlatTopHex(localPosition, innerSize) * innerEnabled;
                float guideMask = outerMask * (1.0 - innerMask) * saturate(_StateOverlayEnabled);

                float2 cellCenterWS = centerXZ + _GridOriginWS.xz;
                float territoryMask = IsCellCenterInTerritory(cellCenterWS);
                float occupiedMask = IsOccupiedCell(qr);
                float previewValidMask = IsPreviewCell(qr);
                float previewBlockedMask = IsBlockedPreviewCell(qr);
                float4 buffColor = GetBuffCellColor(qr);
                float canBuildMask = territoryMask * (1.0 - occupiedMask);
                half4 guideColor = lerp(_SecondaryGuideColor, _PrimaryGuideColor, canBuildMask);
                half3 mixedAlbedo = lerp(baseColor.rgb, guideColor.rgb, guideColor.a * guideMask);
                mixedAlbedo = lerp(mixedAlbedo, _PreviewValidGuideColor.rgb, _PreviewValidGuideColor.a * outerMask * previewValidMask);
                mixedAlbedo = lerp(mixedAlbedo, _PreviewBlockedGuideColor.rgb, _PreviewBlockedGuideColor.a * outerMask * previewBlockedMask);
                mixedAlbedo = lerp(mixedAlbedo, buffColor.rgb, buffColor.a * guideMask);

                SurfaceData surfaceData;
                BuildSurfaceData(materialUV, mixedAlbedo, baseColor.a, surfaceData);

                float sgn = input.tangentWS.w;
                float3 bitangentWS = sgn * cross(input.normalWS, input.tangentWS.xyz);
                half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangentWS, input.normalWS);
                half3 normalWS = NormalizeNormalPerPixel(TransformTangentToWorld(surfaceData.normalTS, tangentToWorld));
                half3 litColor = EvaluateLightingPBR(input.positionWS, normalWS, viewDirWS, surfaceData);
                return half4(litColor, surfaceData.alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
