using System.Collections.Generic;
using UnityEngine;

namespace Dev.Network
{
    [AddComponentMenu("ProjectIO/Sacred Zone System")]
    public sealed class SacredZoneSystem : MonoBehaviour
    {
        [SerializeField, Range(0f, 0.99f)] private float innerRadiusRatio = 0.8f;
        [SerializeField, Range(0.01f, 1f)] private float quotaRatio = 0.1f;
        [SerializeField, Min(8)] private int meshSegmentCount = 128;
        [SerializeField, Min(0.1f)] private float sampleSpacing = 10f;
        [SerializeField] private SacredZoneView view;

        private readonly List<Vector2> samplePoints = new();
        private TerritorySystem territorySystem;
        private Gate gate;
        private float worldBoundaryRadius;
        private float innerRadius;
        private float sampleArea;
        private bool isQuotaReached;
        private bool isInitialized;

        public Vector3 Center => transform.position;
        public float InnerRadius => innerRadius;
        public float OuterRadius => worldBoundaryRadius;
        public float CapturedArea { get; private set; }
        public float TotalArea { get; private set; }
        public float ProgressRatio => TotalArea > 0f ? Mathf.Clamp01(CapturedArea / TotalArea) : 0f;
        public bool IsQuotaReached => isQuotaReached;

        public event global::System.Action<float, float, SacredZoneSystem> OnProgressChanged;
        public event global::System.Action<SacredZoneSystem> OnQuotaReached;

        public void Initialize(TerritorySystem territorySystem, float worldBoundaryRadius)
        {
            this.worldBoundaryRadius = Mathf.Max(0.01f, worldBoundaryRadius);
            isInitialized = true;

            RebuildSacredZone();
            SetTerritorySystem(territorySystem);
            RecalculateProgress();
        }

        public void SetTerritorySystem(TerritorySystem nextTerritorySystem)
        {
            if (territorySystem == nextTerritorySystem)
                return;

            if (territorySystem != null)
                territorySystem.OnTerritoryExpandedEvent -= HandleTerritoryExpanded;

            territorySystem = nextTerritorySystem;

            if (territorySystem != null)
                territorySystem.OnTerritoryExpandedEvent += HandleTerritoryExpanded;
        }

        public void SetGate(Gate gate)
        {
            this.gate = gate;
            ApplyGateUnlockState();
        }

        public float CalculateInnerRadius(float outerRadius)
            => Mathf.Max(0f, outerRadius) * Mathf.Clamp01(innerRadiusRatio);

        private void RebuildSacredZone()
        {
            float outerRadius = worldBoundaryRadius;
            innerRadius = CalculateInnerRadius(outerRadius);

            if (view != null)
                view.Initialize(innerRadius, outerRadius, meshSegmentCount);
            else
                Debug.LogWarning("SacredZoneSystem requires a SacredZoneView reference in the hierarchy.");

            RebuildSamples(innerRadius, outerRadius);
        }

        private void RebuildSamples(float innerRadius, float outerRadius)
        {
            samplePoints.Clear();
            sampleArea = sampleSpacing * sampleSpacing;

            float innerRadiusSqr = innerRadius * innerRadius;
            float outerRadiusSqr = outerRadius * outerRadius;
            float first = -outerRadius + sampleSpacing * 0.5f;
            float last = outerRadius;

            for (float x = first; x <= last; x += sampleSpacing)
            {
                for (float y = first; y <= last; y += sampleSpacing)
                {
                    float sqrDistance = x * x + y * y;
                    if (sqrDistance < innerRadiusSqr || sqrDistance > outerRadiusSqr)
                        continue;

                    samplePoints.Add(new Vector2(x, y));
                }
            }

            TotalArea = samplePoints.Count * sampleArea;
            CapturedArea = 0f;
            isQuotaReached = false;
        }

        private void HandleTerritoryExpanded(Territory territory, TerritorySystem sender)
        {
            RecalculateProgress(territory);
        }

        private void RecalculateProgress()
        {
            RecalculateProgress(territorySystem != null ? territorySystem.Territory : null);
        }

        private void RecalculateProgress(Territory territory)
        {
            int capturedSampleCount = 0;

            if (territory != null)
            {
                for (int i = 0; i < samplePoints.Count; i++)
                {
                    if (territory.IsPointInPolygon(samplePoints[i]))
                        capturedSampleCount++;
                }
            }

            CapturedArea = capturedSampleCount * sampleArea;
            float progressRatio = ProgressRatio;
            if (view != null)
                view.UpdateProgress(progressRatio);

            OnProgressChanged?.Invoke(CapturedArea, TotalArea, this);

            if (!isQuotaReached && progressRatio >= quotaRatio)
            {
                isQuotaReached = true;
                ApplyGateUnlockState();
                OnQuotaReached?.Invoke(this);
            }
        }

        private void ApplyGateUnlockState()
        {
            if (gate == null)
                return;

            gate.SetUnlocked(isQuotaReached);
        }

        private void OnValidate()
        {
            if (!Application.isPlaying || !isInitialized)
                return;

            RebuildSacredZone();
            RecalculateProgress();
            ApplyGateUnlockState();
        }

        private void OnDestroy()
        {
            if (territorySystem != null)
                territorySystem.OnTerritoryExpandedEvent -= HandleTerritoryExpanded;
        }
    }
}
