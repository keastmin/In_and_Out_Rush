using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

namespace KIM.Dev
{
    internal sealed class FogOfWarMaskRenderer
    {
        private const int MaskTextureSize = 512;
        private const float OverlayHeightOffset = 0.05f;

        private static readonly int[] OverlayTriangles = { 0, 2, 1, 0, 3, 2 };
        private static readonly int MainTextureId = Shader.PropertyToID("_MainTex");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BlurStepId = Shader.PropertyToID("_BlurStep");
        private static readonly int VisionMaskId = Shader.PropertyToID("_VisionMask");
        private static readonly int FogColorId = Shader.PropertyToID("_FogColor");
        private static readonly int FogDensityId = Shader.PropertyToID("_FogDensity");
        private static readonly int FogBoundsId = Shader.PropertyToID("_FogBounds");
        private static readonly ProfilerMarker TerritoryMaskUpdateMarker =
            new("FogOfWar.TerritoryMaskUpdate");

        private readonly Vector3[] _overlayVertices = new Vector3[4];

        private RenderTexture _visionMask;
        private RenderTexture _territoryMask;
        private RenderTexture _blurTempMask;
        private RenderTexture _cachedTerritoryVisionMask;
        private Material _overlayMaterial;
        private Material _maskBrushMaterial;
        private Material _maskBlurMaterial;
        private Material _glSolidMaterial;
        private Mesh _overlayMesh;
        private MeshRenderer _overlayRenderer;
        private MeshFilter _overlayFilter;
        private GameObject _overlayObject;
        private float _cachedTerritoryVisibleRange = float.NaN;
        private float _cachedWorldSize = float.NaN;
        private Vector2 _cachedWorldCenter;
        private float _cachedOverlayWorldSize = float.NaN;
        private Vector2 _cachedOverlayWorldCenter;
        private float _cachedOverlayHeight = float.NaN;
        private float _cachedFogDensity = float.NaN;
        private bool _hasWorldCenter;
        private bool _hasOverlayBounds;

        public void Update(
            FogOfWarTerritoryVisibility territoryVisibility,
            bool territoryGeometryChanged,
            Transform runnerTransform,
            Texture runnerVisionBrush,
            float playerRunnerVisibleRange,
            float territoryVisibleRange,
            float fogDensity,
            float worldSize,
            Vector2 worldCenter)
        {
            bool resourcesChanged = EnsureResources();
            bool territoryMaskDirty = resourcesChanged ||
                                      territoryGeometryChanged ||
                                      !_hasWorldCenter ||
                                      _cachedWorldSize != worldSize ||
                                      _cachedWorldCenter != worldCenter ||
                                      _cachedTerritoryVisibleRange != territoryVisibleRange;

            if (territoryMaskDirty)
            {
                using (TerritoryMaskUpdateMarker.Auto())
                {
                    RebuildTerritoryMask(
                        territoryVisibility,
                        territoryVisibleRange,
                        worldSize,
                        worldCenter);
                }

                _cachedWorldSize = worldSize;
                _cachedWorldCenter = worldCenter;
                _cachedTerritoryVisibleRange = territoryVisibleRange;
                _hasWorldCenter = true;
            }

            RenderVisionMask(
                runnerTransform,
                runnerVisionBrush,
                playerRunnerVisibleRange,
                worldSize,
                worldCenter);
            UpdateOverlay(fogDensity, worldSize, worldCenter);
        }

        public void Dispose()
        {
            ReleaseRenderTexture(_visionMask);
            ReleaseRenderTexture(_territoryMask);
            ReleaseRenderTexture(_blurTempMask);
            ReleaseRenderTexture(_cachedTerritoryVisionMask);
            DestroyIfNeeded(_overlayMaterial);
            DestroyIfNeeded(_maskBrushMaterial);
            DestroyIfNeeded(_maskBlurMaterial);
            DestroyIfNeeded(_glSolidMaterial);
            DestroyIfNeeded(_overlayMesh);

            if (_overlayObject != null)
                Object.Destroy(_overlayObject);
        }

        private bool EnsureResources()
        {
            bool resourcesChanged = EnsureRenderTextures();
            EnsureMaterials();
            EnsureOverlayMesh();
            EnsureOverlayObject();
            return resourcesChanged;
        }

        private bool EnsureRenderTextures()
        {
            bool resourcesChanged = false;
            resourcesChanged |= EnsureRenderTexture(ref _visionMask, "Fog Of War Vision Mask");
            resourcesChanged |= EnsureRenderTexture(ref _territoryMask, "Fog Of War Territory Mask");
            resourcesChanged |= EnsureRenderTexture(ref _blurTempMask, "Fog Of War Blur Temp Mask");
            resourcesChanged |= EnsureRenderTexture(
                ref _cachedTerritoryVisionMask,
                "Fog Of War Cached Territory Vision Mask");
            return resourcesChanged;
        }

        private static bool EnsureRenderTexture(ref RenderTexture renderTexture, string textureName)
        {
            if (renderTexture != null && renderTexture.IsCreated())
                return false;

            if (renderTexture == null)
            {
                renderTexture = new RenderTexture(
                    MaskTextureSize,
                    MaskTextureSize,
                    0,
                    RenderTextureFormat.ARGB32)
                {
                    name = textureName,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    useMipMap = false,
                    autoGenerateMips = false
                };
            }

            renderTexture.Create();
            return true;
        }

        private void EnsureMaterials()
        {
            _overlayMaterial ??= CreateMaterial("ProjectIO/FogOfWar/Overlay");
            _maskBrushMaterial ??= CreateMaterial("ProjectIO/FogOfWar/MaskBrush");
            _maskBlurMaterial ??= CreateMaterial("ProjectIO/FogOfWar/MaskBlur");
            _glSolidMaterial ??= CreateGlSolidMaterial();
        }

        private static Material CreateMaterial(string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError($"Missing fog of war shader: {shaderName}");
                return null;
            }

            return new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        private static Material CreateGlSolidMaterial()
        {
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null)
            {
                Debug.LogError("Missing fog of war GL solid shader: Hidden/Internal-Colored");
                return null;
            }

            Material material = new(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            material.SetInt("_SrcBlend", (int)BlendMode.One);
            material.SetInt("_DstBlend", (int)BlendMode.One);
            material.SetInt("_Cull", (int)CullMode.Off);
            material.SetInt("_ZWrite", 0);
            return material;
        }

        private void EnsureOverlayMesh()
        {
            if (_overlayMesh != null)
                return;

            _overlayMesh = new Mesh
            {
                name = "Fog Of War Overlay Mesh",
                hideFlags = HideFlags.HideAndDontSave
            };
            _overlayMesh.MarkDynamic();
        }

        private void EnsureOverlayObject()
        {
            if (_overlayObject != null)
                return;

            _overlayObject = new GameObject("Fog Of War Overlay")
            {
                hideFlags = HideFlags.DontSave
            };
            _overlayObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            _overlayObject.transform.localScale = Vector3.one;

            _overlayFilter = _overlayObject.AddComponent<MeshFilter>();
            _overlayRenderer = _overlayObject.AddComponent<MeshRenderer>();
            _overlayRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _overlayRenderer.receiveShadows = false;
            _overlayRenderer.sharedMaterial = _overlayMaterial;
            _overlayFilter.sharedMesh = _overlayMesh;
        }

        private void RebuildTerritoryMask(
            FogOfWarTerritoryVisibility territoryVisibility,
            float territoryVisibleRange,
            float worldSize,
            Vector2 worldCenter)
        {
            if (_territoryMask == null ||
                _blurTempMask == null ||
                _cachedTerritoryVisionMask == null)
            {
                return;
            }

            RenderTexture previousTarget = RenderTexture.active;
            try
            {
                RenderTexture.active = _territoryMask;
                GL.Clear(false, true, Color.black);
                DrawTerritoryMask(territoryVisibility, worldSize, worldCenter);

                float blurReach = territoryVisibleRange / Mathf.Max(1f, worldSize);
                float blurStep = blurReach * 0.25f;
                if (_maskBlurMaterial != null)
                {
                    _maskBlurMaterial.SetVector(BlurStepId, new Vector4(blurStep, 0f, 0f, 0f));
                    Graphics.Blit(_territoryMask, _blurTempMask, _maskBlurMaterial);
                    _maskBlurMaterial.SetVector(BlurStepId, new Vector4(0f, blurStep, 0f, 0f));
                    Graphics.Blit(_blurTempMask, _cachedTerritoryVisionMask, _maskBlurMaterial);
                }
                else
                {
                    Graphics.Blit(_territoryMask, _cachedTerritoryVisionMask);
                }

                RenderTexture.active = _cachedTerritoryVisionMask;
                DrawTerritoryMask(territoryVisibility, worldSize, worldCenter);
            }
            finally
            {
                RenderTexture.active = previousTarget;
            }
        }

        private void DrawTerritoryMask(
            FogOfWarTerritoryVisibility territoryVisibility,
            float worldSize,
            Vector2 worldCenter)
        {
            Vector3[] vertices = territoryVisibility.WorldTriangleVertices;
            if (vertices == null || vertices.Length == 0 || _glSolidMaterial == null)
                return;

            _glSolidMaterial.SetPass(0);
            GL.PushMatrix();
            GL.LoadPixelMatrix(0f, MaskTextureSize, 0f, MaskTextureSize);
            GL.Begin(GL.TRIANGLES);
            GL.Color(Color.white);

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 worldVertex = vertices[i];
                Vector2 pixel = WorldToMaskPixel(
                    new Vector2(worldVertex.x, worldVertex.z),
                    worldSize,
                    worldCenter);
                GL.Vertex3(pixel.x, pixel.y, 0f);
            }

            GL.End();
            GL.PopMatrix();
        }

        private void RenderVisionMask(
            Transform runnerTransform,
            Texture runnerVisionBrush,
            float playerRunnerVisibleRange,
            float worldSize,
            Vector2 worldCenter)
        {
            if (_visionMask == null || _cachedTerritoryVisionMask == null)
                return;

            Graphics.Blit(_cachedTerritoryVisionMask, _visionMask);
            if (runnerTransform == null ||
                runnerVisionBrush == null ||
                _maskBrushMaterial == null ||
                playerRunnerVisibleRange <= 0f)
            {
                return;
            }

            RenderTexture previousTarget = RenderTexture.active;
            try
            {
                RenderTexture.active = _visionMask;
                DrawRunnerVision(
                    runnerTransform.position,
                    runnerVisionBrush,
                    playerRunnerVisibleRange,
                    worldSize,
                    worldCenter);
            }
            finally
            {
                RenderTexture.active = previousTarget;
            }
        }

        private void DrawRunnerVision(
            Vector3 runnerPosition,
            Texture runnerVisionBrush,
            float visibleRange,
            float worldSize,
            Vector2 worldCenter)
        {
            _maskBrushMaterial.SetTexture(MainTextureId, runnerVisionBrush);
            _maskBrushMaterial.SetColor(ColorId, Color.white);

            Vector2 center = WorldToMaskPixel(
                new Vector2(runnerPosition.x, runnerPosition.z),
                worldSize,
                worldCenter);
            float radiusInPixels = visibleRange / Mathf.Max(1f, worldSize) * MaskTextureSize;
            Rect drawRect = new(
                center.x - radiusInPixels,
                center.y - radiusInPixels,
                radiusInPixels * 2f,
                radiusInPixels * 2f);

            _maskBrushMaterial.SetPass(0);
            GL.PushMatrix();
            GL.LoadPixelMatrix(0f, MaskTextureSize, 0f, MaskTextureSize);
            GL.Begin(GL.QUADS);
            GL.Color(Color.white);
            GL.TexCoord2(0f, 0f);
            GL.Vertex3(drawRect.xMin, drawRect.yMin, 0f);
            GL.TexCoord2(1f, 0f);
            GL.Vertex3(drawRect.xMax, drawRect.yMin, 0f);
            GL.TexCoord2(1f, 1f);
            GL.Vertex3(drawRect.xMax, drawRect.yMax, 0f);
            GL.TexCoord2(0f, 1f);
            GL.Vertex3(drawRect.xMin, drawRect.yMax, 0f);
            GL.End();
            GL.PopMatrix();
        }

        private void UpdateOverlay(float fogDensity, float worldSize, Vector2 worldCenter)
        {
            if (_overlayMesh == null || _overlayMaterial == null)
                return;

            float overlayHeight = InfiniteGrid.Instance != null
                ? InfiniteGrid.Instance.GridHeight + OverlayHeightOffset
                : OverlayHeightOffset;
            bool geometryDirty = !_hasOverlayBounds ||
                                 _cachedOverlayHeight != overlayHeight ||
                                 _cachedOverlayWorldSize != worldSize ||
                                 _cachedOverlayWorldCenter != worldCenter;
            if (geometryDirty)
            {
                float half = worldSize * 0.5f;
                float minX = worldCenter.x - half;
                float maxX = worldCenter.x + half;
                float minZ = worldCenter.y - half;
                float maxZ = worldCenter.y + half;

                _overlayVertices[0] = new Vector3(minX, overlayHeight, minZ);
                _overlayVertices[1] = new Vector3(maxX, overlayHeight, minZ);
                _overlayVertices[2] = new Vector3(maxX, overlayHeight, maxZ);
                _overlayVertices[3] = new Vector3(minX, overlayHeight, maxZ);
                _overlayMesh.Clear();
                _overlayMesh.vertices = _overlayVertices;
                _overlayMesh.triangles = OverlayTriangles;
                _overlayMesh.bounds = new Bounds(
                    new Vector3(worldCenter.x, overlayHeight, worldCenter.y),
                    new Vector3(worldSize, 0.1f, worldSize));
                _cachedOverlayWorldSize = worldSize;
                _cachedOverlayWorldCenter = worldCenter;
                _cachedOverlayHeight = overlayHeight;
                _hasOverlayBounds = true;
            }

            if (_cachedFogDensity != fogDensity || geometryDirty)
            {
                _overlayMaterial.SetFloat(FogDensityId, fogDensity);
                _cachedFogDensity = fogDensity;
            }

            _overlayMaterial.SetTexture(VisionMaskId, _visionMask);
            _overlayMaterial.SetColor(FogColorId, Color.black);
            if (geometryDirty)
            {
                _overlayMaterial.SetVector(
                    FogBoundsId,
                    new Vector4(worldCenter.x, worldCenter.y, worldSize, worldSize));
            }
        }

        private static Vector2 WorldToMaskPixel(
            Vector2 worldPoint,
            float worldSize,
            Vector2 worldCenter)
        {
            float safeWorldSize = Mathf.Max(1f, worldSize);
            float half = safeWorldSize * 0.5f;
            float u = (worldPoint.x - (worldCenter.x - half)) / safeWorldSize;
            float v = (worldPoint.y - (worldCenter.y - half)) / safeWorldSize;
            return new Vector2(u * MaskTextureSize, v * MaskTextureSize);
        }

        private static void ReleaseRenderTexture(RenderTexture texture)
        {
            if (texture == null)
                return;

            texture.Release();
            Object.Destroy(texture);
        }

        private static void DestroyIfNeeded(Object target)
        {
            if (target != null)
                Object.Destroy(target);
        }
    }
}
