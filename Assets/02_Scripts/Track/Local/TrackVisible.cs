using System.Collections.Generic;
using Dev;
using ProjectIO.Tracks;
using UnityEngine;

namespace Dev.Local
{
    public sealed class TrackVisible : Visible
    {
        [SerializeField] private Transform _vertexContainer;
        [SerializeField] private LineRenderer _lineRenderer;

        private GameObject[] _vertexObjects;
        private readonly List<LineRenderer> lineRenderers = new();

        public float LineWidth => _lineRenderer != null ? _lineRenderer.widthMultiplier : 0f;

        public void RenderTrack(Track track)
        {
            if (_vertexObjects != null)
            {
                foreach (GameObject vertexObject in _vertexObjects)
                {
                    if (vertexObject != null)
                    {
                        Destroy(vertexObject);
                    }
                }
            }

            var vertexObjects = new List<GameObject>();
            EnsureLineRendererCount(track != null ? track.Paths.Count : 0);
            int pathCount = track != null ? Mathf.Min(2, track.Paths.Count) : 0;

            for (int pathIndex = 0; pathIndex < lineRenderers.Count; pathIndex++)
            {
                LineRenderer renderer = lineRenderers[pathIndex];
                bool active = pathIndex < pathCount;
                renderer.gameObject.SetActive(active);
                if (!active)
                {
                    continue;
                }

                TrackPath path = track.Paths[pathIndex];
                renderer.positionCount = path.Vertices.Length;
                renderer.SetPositions(path.Vertices);
                renderer.loop = path.IsClosed;

                if (_vertexContainer == null)
                {
                    continue;
                }

                for (int vertexIndex = 0; vertexIndex < path.Vertices.Length; vertexIndex++)
                {
                    var vertexObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    vertexObject.name = $"Path {pathIndex + 1} Vertex {vertexIndex}";
                    vertexObject.transform.position = path.Vertices[vertexIndex];
                    vertexObject.transform.localScale = Vector3.one * 0.1f;
                    vertexObject.transform.SetParent(_vertexContainer, true);
                    vertexObjects.Add(vertexObject);
                }
            }

            _vertexObjects = vertexObjects.ToArray();
        }

        private void EnsureLineRendererCount(int requestedCount)
        {
            if (_lineRenderer == null)
            {
                return;
            }

            if (lineRenderers.Count == 0)
            {
                lineRenderers.Add(_lineRenderer);
            }

            int targetCount = Mathf.Clamp(requestedCount, 1, 2);
            while (lineRenderers.Count < targetCount)
            {
                var child = new GameObject($"Track Path {lineRenderers.Count + 1}");
                child.transform.SetParent(_lineRenderer.transform, false);
                LineRenderer renderer = child.AddComponent<LineRenderer>();
                renderer.sharedMaterials = _lineRenderer.sharedMaterials;
                renderer.widthMultiplier = _lineRenderer.widthMultiplier;
                renderer.widthCurve = _lineRenderer.widthCurve;
                renderer.colorGradient = _lineRenderer.colorGradient;
                renderer.alignment = _lineRenderer.alignment;
                renderer.textureMode = _lineRenderer.textureMode;
                renderer.textureScale = _lineRenderer.textureScale;
                renderer.numCapVertices = _lineRenderer.numCapVertices;
                renderer.numCornerVertices = _lineRenderer.numCornerVertices;
                renderer.useWorldSpace = _lineRenderer.useWorldSpace;
                renderer.sortingLayerID = _lineRenderer.sortingLayerID;
                renderer.sortingOrder = _lineRenderer.sortingOrder;
                lineRenderers.Add(renderer);
            }
        }

    }
}
