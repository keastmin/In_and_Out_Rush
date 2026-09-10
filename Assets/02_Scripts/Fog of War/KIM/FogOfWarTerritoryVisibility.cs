using UnityEngine;

namespace KIM.Dev
{
    internal sealed class FogOfWarTerritoryVisibility
    {
        private readonly struct Triangle
        {
            public Triangle(Vector2 a, Vector2 b, Vector2 c)
            {
                A = a;
                B = b;
                C = c;
                Min = Vector2.Min(a, Vector2.Min(b, c));
                Max = Vector2.Max(a, Vector2.Max(b, c));
            }

            public Vector2 A { get; }
            public Vector2 B { get; }
            public Vector2 C { get; }
            public Vector2 Min { get; }
            public Vector2 Max { get; }
        }

        private MeshFilter _meshFilter;
        private Mesh _mesh;
        private Matrix4x4 _localToWorld;
        private Triangle[] _triangles;
        private Vector3[] _worldTriangleVertices;
        private Vector2 _boundsMin;
        private Vector2 _boundsMax;

        public bool HasMesh => _triangles != null && _triangles.Length > 0;
        public Vector3[] WorldTriangleVertices => _worldTriangleVertices;

        public bool Refresh(MeshFilter meshFilter)
        {
            Mesh mesh = meshFilter != null ? meshFilter.sharedMesh : null;
            Matrix4x4 localToWorld = meshFilter != null
                ? meshFilter.transform.localToWorldMatrix
                : Matrix4x4.identity;

            if (_meshFilter == meshFilter && _mesh == mesh && _localToWorld == localToWorld)
                return false;

            _meshFilter = meshFilter;
            _mesh = mesh;
            _localToWorld = localToWorld;

            if (mesh == null)
            {
                _triangles = null;
                _worldTriangleVertices = null;
                return true;
            }

            Vector3[] localVertices = mesh.vertices;
            int[] indices = mesh.triangles;
            int triangleCount = indices.Length / 3;
            if (localVertices.Length == 0 || triangleCount == 0)
            {
                _triangles = null;
                _worldTriangleVertices = null;
                return true;
            }

            _triangles = new Triangle[triangleCount];
            _worldTriangleVertices = new Vector3[triangleCount * 3];
            _boundsMin = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            _boundsMax = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

            for (int triangleIndex = 0; triangleIndex < triangleCount; triangleIndex++)
            {
                int sourceIndex = triangleIndex * 3;
                Vector3 a3 = localToWorld.MultiplyPoint3x4(localVertices[indices[sourceIndex]]);
                Vector3 b3 = localToWorld.MultiplyPoint3x4(localVertices[indices[sourceIndex + 1]]);
                Vector3 c3 = localToWorld.MultiplyPoint3x4(localVertices[indices[sourceIndex + 2]]);

                _worldTriangleVertices[sourceIndex] = a3;
                _worldTriangleVertices[sourceIndex + 1] = b3;
                _worldTriangleVertices[sourceIndex + 2] = c3;

                Triangle triangle = new(
                    new Vector2(a3.x, a3.z),
                    new Vector2(b3.x, b3.z),
                    new Vector2(c3.x, c3.z));
                _triangles[triangleIndex] = triangle;
                _boundsMin = Vector2.Min(_boundsMin, triangle.Min);
                _boundsMax = Vector2.Max(_boundsMax, triangle.Max);
            }

            return true;
        }

        public bool IsVisible(Vector3 worldPosition, float visibleRange)
        {
            if (!HasMesh)
                return false;

            Vector2 point = new(worldPosition.x, worldPosition.z);
            float safeRange = Mathf.Max(0f, visibleRange);
            if (!IsInsideExpandedBounds(point, _boundsMin, _boundsMax, safeRange))
                return false;

            float rangeSqr = safeRange * safeRange;
            bool hasRange = safeRange > 0f;
            for (int i = 0; i < _triangles.Length; i++)
            {
                Triangle triangle = _triangles[i];
                if (!IsInsideExpandedBounds(point, triangle.Min, triangle.Max, safeRange))
                    continue;

                if (IsPointInTriangle(point, triangle.A, triangle.B, triangle.C))
                    return true;

                if (hasRange &&
                    (DistanceToSegmentSqr(point, triangle.A, triangle.B) <= rangeSqr ||
                     DistanceToSegmentSqr(point, triangle.B, triangle.C) <= rangeSqr ||
                     DistanceToSegmentSqr(point, triangle.C, triangle.A) <= rangeSqr))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsInsideExpandedBounds(
            Vector2 point,
            Vector2 boundsMin,
            Vector2 boundsMax,
            float range)
        {
            return point.x >= boundsMin.x - range &&
                   point.x <= boundsMax.x + range &&
                   point.y >= boundsMin.y - range &&
                   point.y <= boundsMax.y + range;
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
    }
}
