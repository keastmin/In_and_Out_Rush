using System.Collections.Generic;
using UnityEngine;

namespace KIM.Dev
{
    public sealed class InfiniteGridTerritoryChangeTracker
    {
        private readonly HashSet<InfiniteGridTerritoryEdgeKey> _previousEdges = new();
        private readonly HashSet<InfiniteGridTerritoryEdgeKey> _currentEdges = new();

        private bool _hasSnapshot;

        public bool RequiresFullInvalidation { get; private set; }
        public Vector2 ChangedMin { get; private set; }
        public Vector2 ChangedMax { get; private set; }

        public bool Capture(Territory territory)
        {
            IReadOnlyList<Vector2> vertices = territory != null ? territory.Vertices : null;
            _currentEdges.Clear();
            AddEdges(vertices, _currentEdges);

            if (!_hasSnapshot)
            {
                ReplaceSnapshot();
                RequiresFullInvalidation = true;
                return true;
            }

            if (_previousEdges.SetEquals(_currentEdges))
            {
                RequiresFullInvalidation = false;
                return false;
            }

            bool hasChangedPoint = false;
            Vector2 changedMin = default;
            Vector2 changedMax = default;

            foreach (InfiniteGridTerritoryEdgeKey edge in _previousEdges)
            {
                if (!_currentEdges.Contains(edge))
                {
                    Encapsulate(edge.A, ref hasChangedPoint, ref changedMin, ref changedMax);
                    Encapsulate(edge.B, ref hasChangedPoint, ref changedMin, ref changedMax);
                }
            }

            foreach (InfiniteGridTerritoryEdgeKey edge in _currentEdges)
            {
                if (!_previousEdges.Contains(edge))
                {
                    Encapsulate(edge.A, ref hasChangedPoint, ref changedMin, ref changedMax);
                    Encapsulate(edge.B, ref hasChangedPoint, ref changedMin, ref changedMax);
                }
            }

            ReplaceSnapshot();
            RequiresFullInvalidation = !hasChangedPoint;
            ChangedMin = changedMin;
            ChangedMax = changedMax;
            return true;
        }

        public void Clear()
        {
            _previousEdges.Clear();
            _currentEdges.Clear();
            _hasSnapshot = false;
            RequiresFullInvalidation = false;
            ChangedMin = default;
            ChangedMax = default;
        }

        private void ReplaceSnapshot()
        {
            _previousEdges.Clear();
            _previousEdges.UnionWith(_currentEdges);
            _hasSnapshot = true;
        }

        private static void AddEdges(
            IReadOnlyList<Vector2> vertices,
            ISet<InfiniteGridTerritoryEdgeKey> results)
        {
            if (vertices == null || vertices.Count < 3)
            {
                return;
            }

            for (int i = 0; i < vertices.Count; i++)
            {
                results.Add(
                    new InfiniteGridTerritoryEdgeKey(
                        vertices[i],
                        vertices[(i + 1) % vertices.Count]));
            }
        }

        private static void Encapsulate(
            Vector2 point,
            ref bool hasPoint,
            ref Vector2 minimum,
            ref Vector2 maximum)
        {
            if (!hasPoint)
            {
                minimum = point;
                maximum = point;
                hasPoint = true;
                return;
            }

            minimum = Vector2.Min(minimum, point);
            maximum = Vector2.Max(maximum, point);
        }
    }
}
