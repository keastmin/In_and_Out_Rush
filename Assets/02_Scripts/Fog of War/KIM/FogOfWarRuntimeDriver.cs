using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace KIM.Dev
{
    internal sealed class FogOfWarRuntimeDriver
    {
        private const int MaskTextureSize = 512;
        private const float OverlayHeightOffset = 0.05f;
        private const float HiddenObjectRefreshInterval = 0.15f;
        private const float RunnerSoftEdgeStartRatio = 0.65f;
        private const int RunnerBrushTextureSize = 128;

        private readonly Dictionary<Renderer, bool> _rendererOriginalStates = new();
        private readonly Dictionary<Canvas, bool> _canvasOriginalStates = new();
        private readonly List<Renderer> _knownHiddenRenderers = new();
        private readonly List<Canvas> _knownHiddenCanvases = new();
        private readonly List<MonoBehaviour> _visibilityComponents = new();

        private RenderTexture _visionMask;
        private RenderTexture _territoryMask;
        private RenderTexture _blurTempMask;
        private Material _overlayMaterial;
        private Material _maskSolidMaterial;
        private Material _maskBrushMaterial;
        private Material _maskBlurMaterial;
        private Material _glSolidMaterial;
        private Mesh _overlayMesh;
        private Mesh _brushQuadMesh;
        private MeshRenderer _overlayRenderer;
        private MeshFilter _overlayFilter;
        private GameObject _overlayObject;
        private Texture2D _generatedRunnerBrush;
        private Vector3[] _territoryVertices;
        private int[] _territoryTriangles;
        private Matrix4x4 _territoryLocalToWorld;
        private MeshFilter _territoryMeshFilter;
        private Mesh _cachedTerritoryMesh;
        private Transform _runnerTransform;
        private bool _hasTerritoryMesh;
        private float _playerRunnerVisibleRange;
        private float _territoryVisibleRange;
        private float _hiddenObjectRefreshTimer;

        public void UpdateFogOfWar(
            Transform ownerTransform,
            LayerMask fogHiddenLayers,
            MeshFilter territoryMeshFilter,
            Transform runnerTransform,
            Texture runnerVisionBrush,
            float playerRunnerVisibleRange,
            float territoryVisibleRange,
            float fogDensity,
            float worldSize,
            Vector2 worldCenter)
        {
            _territoryMeshFilter = territoryMeshFilter;
            _runnerTransform = runnerTransform;
            _playerRunnerVisibleRange = playerRunnerVisibleRange;
            _territoryVisibleRange = territoryVisibleRange;

            EnsureResources(ownerTransform);
            RefreshTerritoryCache();
            RenderVisionMask(runnerVisionBrush, worldSize, worldCenter);
            UpdateOverlay(fogDensity, worldSize, worldCenter);
            UpdateHiddenObjects(fogHiddenLayers);
        }

        public void Dispose()
        {
            RestoreOriginalStates();
            ReleaseRenderTexture(_visionMask);
            ReleaseRenderTexture(_territoryMask);
            ReleaseRenderTexture(_blurTempMask);
            DestroyIfNeeded(_overlayMaterial);
            DestroyIfNeeded(_maskSolidMaterial);
            DestroyIfNeeded(_maskBrushMaterial);
            DestroyIfNeeded(_maskBlurMaterial);
            DestroyIfNeeded(_glSolidMaterial);
            DestroyIfNeeded(_overlayMesh);
            DestroyIfNeeded(_brushQuadMesh);
            DestroyIfNeeded(_generatedRunnerBrush);
            if (_overlayObject != null)
            {
                Object.Destroy(_overlayObject);
            }
        }

        private void EnsureResources(Transform ownerTransform)
        {
            EnsureRenderTextures();
            EnsureMaterials();
            EnsureMeshes();
            EnsureGeneratedRunnerBrush();
            EnsureOverlayObject(ownerTransform);
        }

        private void EnsureRenderTextures()
        {
            _visionMask = EnsureRenderTexture(_visionMask, "Fog Of War Vision Mask");
            _territoryMask = EnsureRenderTexture(_territoryMask, "Fog Of War Territory Mask");
            _blurTempMask = EnsureRenderTexture(_blurTempMask, "Fog Of War Blur Temp Mask");
        }

        private RenderTexture EnsureRenderTexture(RenderTexture renderTexture, string textureName)
        {
            if (renderTexture != null)
                return renderTexture;

            RenderTexture texture = new RenderTexture(MaskTextureSize, MaskTextureSize, 0, RenderTextureFormat.ARGB32)
            {
                name = textureName,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                useMipMap = false,
                autoGenerateMips = false
            };
            texture.Create();
            return texture;
        }

        private void EnsureMaterials()
        {
            _overlayMaterial ??= CreateMaterial("ProjectIO/FogOfWar/Overlay");
            _maskSolidMaterial ??= CreateMaterial("ProjectIO/FogOfWar/MaskSolid");
            _maskBrushMaterial ??= CreateMaterial("ProjectIO/FogOfWar/MaskBrush");
            _maskBlurMaterial ??= CreateMaterial("ProjectIO/FogOfWar/MaskBlur");
            _glSolidMaterial ??= CreateGlSolidMaterial();
        }

        private Material CreateMaterial(string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError($"Missing fog of war shader: {shaderName}");
                return null;
            }

            Material material = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            return material;
        }

        private Material CreateGlSolidMaterial()
        {
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null)
            {
                Debug.LogError("Missing fog of war GL solid shader: Hidden/Internal-Colored");
                return null;
            }

            Material material = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            material.SetInt("_SrcBlend", (int)BlendMode.One);
            material.SetInt("_DstBlend", (int)BlendMode.One);
            material.SetInt("_Cull", (int)CullMode.Off);
            material.SetInt("_ZWrite", 0);
            return material;
        }

        private void EnsureMeshes()
        {
            _overlayMesh ??= CreateWorldOverlayMesh();
            _brushQuadMesh ??= CreateBrushQuadMesh();
        }

        private Mesh CreateWorldOverlayMesh()
        {
            Mesh mesh = new Mesh
            {
                name = "Fog Of War Overlay Mesh",
                hideFlags = HideFlags.HideAndDontSave
            };
            return mesh;
        }

        private Mesh CreateBrushQuadMesh()
        {
            Mesh mesh = new Mesh
            {
                name = "Fog Of War Brush Quad",
                hideFlags = HideFlags.HideAndDontSave
            };

            mesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, 0.5f),
                new Vector3(-0.5f, 0f, 0.5f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f)
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();
            return mesh;
        }

        private void EnsureGeneratedRunnerBrush()
        {
            if (_generatedRunnerBrush != null)
                return;

            _generatedRunnerBrush = new Texture2D(RunnerBrushTextureSize, RunnerBrushTextureSize, TextureFormat.RGBA32, false)
            {
                name = "Fog Of War Generated Runner Brush",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            Vector2 center = new Vector2((RunnerBrushTextureSize - 1) * 0.5f, (RunnerBrushTextureSize - 1) * 0.5f);
            float radius = RunnerBrushTextureSize * 0.5f;
            for (int y = 0; y < RunnerBrushTextureSize; y++)
            {
                for (int x = 0; x < RunnerBrushTextureSize; x++)
                {
                    float normalizedDistance = Vector2.Distance(new Vector2(x, y), center) / radius;
                    float alpha = normalizedDistance >= 1f
                        ? 0f
                        : 1f - Mathf.SmoothStep(RunnerSoftEdgeStartRatio, 1f, normalizedDistance);
                    _generatedRunnerBrush.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            _generatedRunnerBrush.Apply(false, true);
        }

        private void EnsureOverlayObject(Transform ownerTransform)
        {
            if (_overlayObject != null)
                return;

            _overlayObject = new GameObject("Fog Of War Overlay");
            _overlayObject.hideFlags = HideFlags.DontSave;
            _overlayObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            _overlayObject.transform.localScale = Vector3.one;

            _overlayFilter = _overlayObject.AddComponent<MeshFilter>();
            _overlayRenderer = _overlayObject.AddComponent<MeshRenderer>();
            _overlayRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _overlayRenderer.receiveShadows = false;
            _overlayRenderer.sharedMaterial = _overlayMaterial;
            _overlayFilter.sharedMesh = _overlayMesh;
        }

        private void RefreshTerritoryCache()
        {
            if (_territoryMeshFilter == null || _territoryMeshFilter.sharedMesh == null)
            {
                _hasTerritoryMesh = false;
                _cachedTerritoryMesh = null;
                return;
            }

            Mesh mesh = _territoryMeshFilter.sharedMesh;
            if (_hasTerritoryMesh && _cachedTerritoryMesh == mesh && !_territoryMeshFilter.transform.hasChanged)
                return;

            _territoryVertices = mesh.vertices;
            _territoryTriangles = mesh.triangles;
            _territoryLocalToWorld = _territoryMeshFilter.transform.localToWorldMatrix;
            _cachedTerritoryMesh = mesh;
            _territoryMeshFilter.transform.hasChanged = false;
            _hasTerritoryMesh = _territoryVertices != null &&
                                _territoryTriangles != null &&
                                _territoryVertices.Length > 0 &&
                                _territoryTriangles.Length >= 3;
        }

        private void RenderVisionMask(Texture runnerVisionBrush, float worldSize, Vector2 worldCenter)
        {
            if (_visionMask == null || _territoryMask == null || _blurTempMask == null)
                return;

            RenderTexture previousTarget = RenderTexture.active;
            try
            {
                RenderTexture.active = _territoryMask;
                GL.Clear(false, true, Color.black);
                DrawTerritoryMask(worldSize, worldCenter);

                BlurTerritoryMaskToVisionMask(worldSize);

                RenderTexture.active = _visionMask;
                DrawTerritoryMask(worldSize, worldCenter);
                DrawRunnerVision(worldSize, worldCenter);
            }
            finally
            {
                RenderTexture.active = previousTarget;
            }
        }

        private void BlurTerritoryMaskToVisionMask(float worldSize)
        {
            float blurReach = _territoryVisibleRange / Mathf.Max(1f, worldSize);
            float blurStep = blurReach * 0.25f;
            if (_maskBlurMaterial != null)
            {
                _maskBlurMaterial.SetVector("_BlurStep", new Vector4(blurStep, 0f, 0f, 0f));
                Graphics.Blit(_territoryMask, _blurTempMask, _maskBlurMaterial);
                _maskBlurMaterial.SetVector("_BlurStep", new Vector4(0f, blurStep, 0f, 0f));
                Graphics.Blit(_blurTempMask, _visionMask, _maskBlurMaterial);
            }
            else
            {
                Graphics.Blit(_territoryMask, _visionMask);
            }
        }

        private void DrawTerritoryMask(float worldSize, Vector2 worldCenter)
        {
            if (!_hasTerritoryMesh || _glSolidMaterial == null)
                return;

            _glSolidMaterial.SetPass(0);
            GL.PushMatrix();
            GL.LoadPixelMatrix(0f, MaskTextureSize, 0f, MaskTextureSize);
            GL.Begin(GL.TRIANGLES);
            GL.Color(Color.white);

            for (int i = 0; i < _territoryTriangles.Length; i++)
            {
                Vector3 worldVertex = _territoryLocalToWorld.MultiplyPoint3x4(_territoryVertices[_territoryTriangles[i]]);
                Vector2 pixel = WorldToMaskPixel(new Vector2(worldVertex.x, worldVertex.z), worldSize, worldCenter);
                GL.Vertex3(pixel.x, pixel.y, 0f);
            }

            GL.End();
            GL.PopMatrix();
        }

        private void DrawRunnerVision(float worldSize, Vector2 worldCenter)
        {
            if (_runnerTransform == null || _brushQuadMesh == null || _maskBrushMaterial == null || _generatedRunnerBrush == null || _playerRunnerVisibleRange <= 0f)
                return;

            _maskBrushMaterial.SetTexture("_MainTex", _generatedRunnerBrush);
            _maskBrushMaterial.SetColor("_Color", Color.white);

            Vector3 position = _runnerTransform.position;
            Vector2 center = WorldToMaskPixel(new Vector2(position.x, position.z), worldSize, worldCenter);
            float radiusInPixels = _playerRunnerVisibleRange / Mathf.Max(1f, worldSize) * MaskTextureSize;
            Rect drawRect = new Rect(
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

        private Vector2 WorldToMaskPixel(Vector2 worldPoint, float worldSize, Vector2 worldCenter)
        {
            float safeWorldSize = Mathf.Max(1f, worldSize);
            float half = safeWorldSize * 0.5f;
            float u = (worldPoint.x - (worldCenter.x - half)) / safeWorldSize;
            float v = (worldPoint.y - (worldCenter.y - half)) / safeWorldSize;
            return new Vector2(u * MaskTextureSize, v * MaskTextureSize);
        }

        private void UpdateOverlay(float fogDensity, float worldSize, Vector2 worldCenter)
        {
            if (_overlayMesh == null || _overlayMaterial == null)
                return;

            float half = worldSize * 0.5f;
            float y = InfiniteGrid.Instance != null ? InfiniteGrid.Instance.GridHeight + OverlayHeightOffset : OverlayHeightOffset;
            float minX = worldCenter.x - half;
            float maxX = worldCenter.x + half;
            float minZ = worldCenter.y - half;
            float maxZ = worldCenter.y + half;

            _overlayMesh.Clear();
            _overlayMesh.vertices = new[]
            {
                new Vector3(minX, y, minZ),
                new Vector3(maxX, y, minZ),
                new Vector3(maxX, y, maxZ),
                new Vector3(minX, y, maxZ)
            };
            _overlayMesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            _overlayMesh.RecalculateBounds();

            _overlayMaterial.SetTexture("_VisionMask", _visionMask);
            _overlayMaterial.SetColor("_FogColor", Color.black);
            _overlayMaterial.SetFloat("_FogDensity", fogDensity);
            _overlayMaterial.SetVector("_FogBounds", new Vector4(worldCenter.x, worldCenter.y, worldSize, worldSize));
        }

        private void UpdateHiddenObjects(LayerMask fogHiddenLayers)
        {
            _hiddenObjectRefreshTimer -= Time.deltaTime;
            if (_hiddenObjectRefreshTimer > 0f)
                return;

            _hiddenObjectRefreshTimer = HiddenObjectRefreshInterval;
            RefreshHiddenObjectLists(fogHiddenLayers);

            for (int i = 0; i < _knownHiddenRenderers.Count; i++)
            {
                Renderer hiddenRenderer = _knownHiddenRenderers[i];
                if (hiddenRenderer == null)
                    continue;

                bool originalState = _rendererOriginalStates.TryGetValue(hiddenRenderer, out bool state) && state;
                hiddenRenderer.enabled = originalState && IsWorldPositionVisible(hiddenRenderer.bounds.center);
            }

            for (int i = 0; i < _knownHiddenCanvases.Count; i++)
            {
                Canvas hiddenCanvas = _knownHiddenCanvases[i];
                if (hiddenCanvas == null)
                    continue;

                bool originalState = _canvasOriginalStates.TryGetValue(hiddenCanvas, out bool state) && state;
                hiddenCanvas.enabled = originalState && IsWorldPositionVisible(hiddenCanvas.transform.position);
            }
        }

        private void RefreshHiddenObjectLists(LayerMask fogHiddenLayers)
        {
            _knownHiddenRenderers.Clear();
            Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer hiddenRenderer = renderers[i];
                if (hiddenRenderer == null ||
                    !IsObjectOrParentLayerInMask(hiddenRenderer.transform, fogHiddenLayers) ||
                    IsFogOfWarAlwaysVisible(hiddenRenderer.transform))
                {
                    continue;
                }

                _knownHiddenRenderers.Add(hiddenRenderer);
                if (!_rendererOriginalStates.ContainsKey(hiddenRenderer))
                {
                    _rendererOriginalStates.Add(hiddenRenderer, hiddenRenderer.enabled);
                }
            }

            _knownHiddenCanvases.Clear();
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas hiddenCanvas = canvases[i];
                if (hiddenCanvas == null ||
                    !IsObjectOrParentLayerInMask(hiddenCanvas.transform, fogHiddenLayers) ||
                    IsFogOfWarAlwaysVisible(hiddenCanvas.transform))
                {
                    continue;
                }

                _knownHiddenCanvases.Add(hiddenCanvas);
                if (!_canvasOriginalStates.ContainsKey(hiddenCanvas))
                {
                    _canvasOriginalStates.Add(hiddenCanvas, hiddenCanvas.enabled);
                }
            }
        }

        private bool IsWorldPositionVisible(Vector3 worldPosition)
        {
            if (_runnerTransform != null)
            {
                Vector3 runnerPosition = _runnerTransform.position;
                Vector2 runnerPoint = new Vector2(runnerPosition.x, runnerPosition.z);
                Vector2 targetPoint = new Vector2(worldPosition.x, worldPosition.z);
                if ((targetPoint - runnerPoint).sqrMagnitude <= _playerRunnerVisibleRange * _playerRunnerVisibleRange)
                    return true;
            }

            return IsWorldPositionVisibleByTerritory(worldPosition);
        }

        private bool IsWorldPositionVisibleByTerritory(Vector3 worldPosition)
        {
            if (!_hasTerritoryMesh)
                return false;

            Vector2 point = new Vector2(worldPosition.x, worldPosition.z);
            float rangeSqr = _territoryVisibleRange * _territoryVisibleRange;
            bool hasRange = _territoryVisibleRange > 0f;

            for (int i = 0; i < _territoryTriangles.Length; i += 3)
            {
                Vector3 a3 = _territoryLocalToWorld.MultiplyPoint3x4(_territoryVertices[_territoryTriangles[i]]);
                Vector3 b3 = _territoryLocalToWorld.MultiplyPoint3x4(_territoryVertices[_territoryTriangles[i + 1]]);
                Vector3 c3 = _territoryLocalToWorld.MultiplyPoint3x4(_territoryVertices[_territoryTriangles[i + 2]]);
                Vector2 a = new Vector2(a3.x, a3.z);
                Vector2 b = new Vector2(b3.x, b3.z);
                Vector2 c = new Vector2(c3.x, c3.z);

                if (IsPointInTriangle(point, a, b, c))
                    return true;

                if (!hasRange)
                    continue;

                if (DistanceToSegmentSqr(point, a, b) <= rangeSqr ||
                    DistanceToSegmentSqr(point, b, c) <= rangeSqr ||
                    DistanceToSegmentSqr(point, c, a) <= rangeSqr)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsFogOfWarAlwaysVisible(Transform target)
        {
            while (target != null)
            {
                _visibilityComponents.Clear();
                target.GetComponents<MonoBehaviour>(_visibilityComponents);
                for (int i = 0; i < _visibilityComponents.Count; i++)
                {
                    if (_visibilityComponents[i] is IFogOfWarAlwaysVisible)
                        return true;
                }

                target = target.parent;
            }

            return false;
        }

        private void RestoreOriginalStates()
        {
            foreach (KeyValuePair<Renderer, bool> rendererState in _rendererOriginalStates)
            {
                if (rendererState.Key != null)
                {
                    rendererState.Key.enabled = rendererState.Value;
                }
            }

            foreach (KeyValuePair<Canvas, bool> canvasState in _canvasOriginalStates)
            {
                if (canvasState.Key != null)
                {
                    canvasState.Key.enabled = canvasState.Value;
                }
            }
        }

        private static bool IsPointInTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Sign(point, a, b);
            float d2 = Sign(point, b, c);
            float d3 = Sign(point, c, a);

            bool hasNeg = d1 < 0f || d2 < 0f || d3 < 0f;
            bool hasPos = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(hasNeg && hasPos);
        }

        private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }

        private static float DistanceToSegmentSqr(Vector2 point, Vector2 start, Vector2 end)
        {
            Vector2 segment = end - start;
            float lengthSqr = segment.sqrMagnitude;
            if (lengthSqr <= Mathf.Epsilon)
                return (point - start).sqrMagnitude;

            float t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSqr);
            Vector2 closest = start + segment * t;
            return (point - closest).sqrMagnitude;
        }

        private static bool IsLayerInMask(int layer, LayerMask mask)
        {
            return (mask.value & (1 << layer)) != 0;
        }

        private static bool IsObjectOrParentLayerInMask(Transform target, LayerMask mask)
        {
            while (target != null)
            {
                if (IsLayerInMask(target.gameObject.layer, mask))
                    return true;

                target = target.parent;
            }

            return false;
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
            {
                Object.Destroy(target);
            }
        }
    }
}
