using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace KIM.Dev
{
    internal sealed class FogOfWarRuntimeDriver
    {
        private const float HiddenObjectRefreshInterval = 0.15f;

        private static readonly ProfilerMarker UpdateMarker = new("FogOfWar.Update");
        private static readonly ProfilerMarker RegistrySyncMarker = new("FogOfWar.RegistrySync");
        private static readonly ProfilerMarker HiddenVisibilityMarker = new("FogOfWar.HiddenVisibility");

        private readonly FogOfWarTerritoryVisibility _territoryVisibility = new();
        private readonly FogOfWarMaskRenderer _maskRenderer = new();
        private readonly Dictionary<Renderer, bool> _rendererOriginalStates = new();
        private readonly Dictionary<Canvas, bool> _canvasOriginalStates = new();
        private readonly List<Renderer> _knownHiddenRenderers = new();
        private readonly List<Canvas> _knownHiddenCanvases = new();
        private readonly List<Transform> _registeredRoots = new();
        private readonly List<Renderer> _rendererBuffer = new();
        private readonly List<Canvas> _canvasBuffer = new();
        private readonly List<MonoBehaviour> _visibilityComponents = new();
        private readonly HashSet<Renderer> _seenRenderers = new();
        private readonly HashSet<Canvas> _seenCanvases = new();

        private Transform _runnerTransform;
        private float _playerRunnerVisibleRange;
        private float _territoryVisibleRange;
        private float _hiddenObjectRefreshTimer;
        private int _observedRegistryRevision = -1;
        private int _observedHiddenLayerMask = int.MinValue;

        public void UpdateFogOfWar(
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
            using (UpdateMarker.Auto())
            {
                _runnerTransform = runnerTransform;
                _playerRunnerVisibleRange = playerRunnerVisibleRange;
                _territoryVisibleRange = territoryVisibleRange;

                bool territoryGeometryChanged = _territoryVisibility.Refresh(territoryMeshFilter);
                _maskRenderer.Update(
                    _territoryVisibility,
                    territoryGeometryChanged,
                    runnerTransform,
                    runnerVisionBrush,
                    playerRunnerVisibleRange,
                    territoryVisibleRange,
                    fogDensity,
                    worldSize,
                    worldCenter);

                SynchronizeHiddenObjectsIfNeeded(fogHiddenLayers);
                UpdateHiddenObjectVisibility();
            }
        }

        public void Dispose()
        {
            RestoreOriginalStates();
            _rendererOriginalStates.Clear();
            _canvasOriginalStates.Clear();
            _knownHiddenRenderers.Clear();
            _knownHiddenCanvases.Clear();
            _maskRenderer.Dispose();
        }

        private void SynchronizeHiddenObjectsIfNeeded(LayerMask fogHiddenLayers)
        {
            int registryRevision = FogOfWarHiddenObjectController.Revision;
            if (_observedRegistryRevision == registryRevision &&
                _observedHiddenLayerMask == fogHiddenLayers.value)
            {
                return;
            }

            using (RegistrySyncMarker.Auto())
            {
                RestoreOriginalStates();
                ClearHiddenObjectCache();
                FogOfWarHiddenObjectController.CopyRegisteredRoots(_registeredRoots);

                for (int rootIndex = 0; rootIndex < _registeredRoots.Count; rootIndex++)
                {
                    Transform root = _registeredRoots[rootIndex];
                    if (root == null || IsFogOfWarAlwaysVisible(root))
                        continue;

                    AddHiddenRenderers(root, fogHiddenLayers);
                    AddHiddenCanvases(root, fogHiddenLayers);
                }

                _observedRegistryRevision = FogOfWarHiddenObjectController.Revision;
                _observedHiddenLayerMask = fogHiddenLayers.value;
                _hiddenObjectRefreshTimer = 0f;
            }
        }

        private void AddHiddenRenderers(Transform root, LayerMask fogHiddenLayers)
        {
            _rendererBuffer.Clear();
            root.GetComponentsInChildren(true, _rendererBuffer);
            for (int i = 0; i < _rendererBuffer.Count; i++)
            {
                Renderer hiddenRenderer = _rendererBuffer[i];
                if (hiddenRenderer == null ||
                    !_seenRenderers.Add(hiddenRenderer) ||
                    !IsObjectOrParentLayerInMask(hiddenRenderer.transform, fogHiddenLayers) ||
                    IsFogOfWarAlwaysVisible(hiddenRenderer.transform))
                {
                    continue;
                }

                _knownHiddenRenderers.Add(hiddenRenderer);
                _rendererOriginalStates.Add(hiddenRenderer, hiddenRenderer.enabled);
            }
        }

        private void AddHiddenCanvases(Transform root, LayerMask fogHiddenLayers)
        {
            _canvasBuffer.Clear();
            root.GetComponentsInChildren(true, _canvasBuffer);
            for (int i = 0; i < _canvasBuffer.Count; i++)
            {
                Canvas hiddenCanvas = _canvasBuffer[i];
                if (hiddenCanvas == null ||
                    !_seenCanvases.Add(hiddenCanvas) ||
                    !IsObjectOrParentLayerInMask(hiddenCanvas.transform, fogHiddenLayers) ||
                    IsFogOfWarAlwaysVisible(hiddenCanvas.transform))
                {
                    continue;
                }

                _knownHiddenCanvases.Add(hiddenCanvas);
                _canvasOriginalStates.Add(hiddenCanvas, hiddenCanvas.enabled);
            }
        }

        private void UpdateHiddenObjectVisibility()
        {
            _hiddenObjectRefreshTimer -= Time.deltaTime;
            if (_hiddenObjectRefreshTimer > 0f)
                return;

            _hiddenObjectRefreshTimer = HiddenObjectRefreshInterval;
            using (HiddenVisibilityMarker.Auto())
            {
                for (int i = 0; i < _knownHiddenRenderers.Count; i++)
                {
                    Renderer hiddenRenderer = _knownHiddenRenderers[i];
                    if (hiddenRenderer == null)
                        continue;

                    bool originalState =
                        _rendererOriginalStates.TryGetValue(hiddenRenderer, out bool state) && state;
                    hiddenRenderer.enabled =
                        originalState && IsWorldPositionVisible(hiddenRenderer.bounds.center);
                }

                for (int i = 0; i < _knownHiddenCanvases.Count; i++)
                {
                    Canvas hiddenCanvas = _knownHiddenCanvases[i];
                    if (hiddenCanvas == null)
                        continue;

                    bool originalState =
                        _canvasOriginalStates.TryGetValue(hiddenCanvas, out bool state) && state;
                    hiddenCanvas.enabled =
                        originalState && IsWorldPositionVisible(hiddenCanvas.transform.position);
                }
            }
        }

        private bool IsWorldPositionVisible(Vector3 worldPosition)
        {
            if (_runnerTransform != null)
            {
                Vector3 runnerPosition = _runnerTransform.position;
                Vector2 runnerPoint = new(runnerPosition.x, runnerPosition.z);
                Vector2 targetPoint = new(worldPosition.x, worldPosition.z);
                float runnerRangeSqr = _playerRunnerVisibleRange * _playerRunnerVisibleRange;
                if ((targetPoint - runnerPoint).sqrMagnitude <= runnerRangeSqr)
                    return true;
            }

            return _territoryVisibility.IsVisible(worldPosition, _territoryVisibleRange);
        }

        private bool IsFogOfWarAlwaysVisible(Transform target)
        {
            while (target != null)
            {
                _visibilityComponents.Clear();
                target.GetComponents(_visibilityComponents);
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
                    rendererState.Key.enabled = rendererState.Value;
            }

            foreach (KeyValuePair<Canvas, bool> canvasState in _canvasOriginalStates)
            {
                if (canvasState.Key != null)
                    canvasState.Key.enabled = canvasState.Value;
            }
        }

        private void ClearHiddenObjectCache()
        {
            _rendererOriginalStates.Clear();
            _canvasOriginalStates.Clear();
            _knownHiddenRenderers.Clear();
            _knownHiddenCanvases.Clear();
            _seenRenderers.Clear();
            _seenCanvases.Clear();
        }

        private static bool IsObjectOrParentLayerInMask(Transform target, LayerMask mask)
        {
            while (target != null)
            {
                if ((mask.value & (1 << target.gameObject.layer)) != 0)
                    return true;

                target = target.parent;
            }

            return false;
        }
    }
}
