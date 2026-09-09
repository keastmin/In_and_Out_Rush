using System.Collections.Generic;
using UnityEngine;

namespace ProjectIO.Territory
{
    public sealed class TerritorySpatialQuadtree
    {
        private const int MaximumDepth = 5;
        private const int MaximumEdgesPerLeaf = 8;
        private const float Padding = 0.0001f;

        private sealed class Node
        {
            public Vector2 Minimum;
            public Vector2 Maximum;
            public int[] EdgeIndices;
            public Node[] Children;
        }

        private readonly IReadOnlyList<TerritorySpatialEdge> _edges;
        private readonly Node _root;

        public TerritorySpatialQuadtree(
            Vector2 minimum,
            Vector2 maximum,
            IReadOnlyList<int> edgeIndices,
            IReadOnlyList<TerritorySpatialEdge> edges)
        {
            _edges = edges;
            _root = Build(minimum, maximum, edgeIndices, 0);
        }

        public int NodeCount { get; private set; }

        public void Query(
            Vector2 minimum,
            Vector2 maximum,
            List<int> results,
            HashSet<int> unique)
        {
            Query(_root, minimum, maximum, results, unique);
        }

        private Node Build(
            Vector2 minimum,
            Vector2 maximum,
            IReadOnlyList<int> edgeIndices,
            int depth)
        {
            NodeCount++;
            var node = new Node
            {
                Minimum = minimum,
                Maximum = maximum
            };

            if (depth >= MaximumDepth || edgeIndices.Count <= MaximumEdgesPerLeaf)
            {
                node.EdgeIndices = Copy(edgeIndices);
                return node;
            }

            Vector2 center = (minimum + maximum) * 0.5f;
            Vector2[] childMinimums =
            {
                minimum,
                new(center.x, minimum.y),
                new(minimum.x, center.y),
                center
            };
            Vector2[] childMaximums =
            {
                center,
                new(maximum.x, center.y),
                new(center.x, maximum.y),
                maximum
            };
            var childEdges = new List<int>[4];
            int referenceCount = 0;
            for (int childIndex = 0; childIndex < 4; childIndex++)
            {
                List<int> matches = new();
                for (int edgeOffset = 0; edgeOffset < edgeIndices.Count; edgeOffset++)
                {
                    int edgeIndex = edgeIndices[edgeOffset];
                    TerritorySpatialEdge edge = _edges[edgeIndex];
                    if (IntersectsRectangle(edge, childMinimums[childIndex], childMaximums[childIndex]))
                        matches.Add(edgeIndex);
                }

                childEdges[childIndex] = matches;
                referenceCount += matches.Count;
            }

            if (referenceCount == 0 || referenceCount > edgeIndices.Count * 3)
            {
                node.EdgeIndices = Copy(edgeIndices);
                return node;
            }

            node.Children = new Node[4];
            for (int childIndex = 0; childIndex < 4; childIndex++)
            {
                node.Children[childIndex] = Build(
                    childMinimums[childIndex],
                    childMaximums[childIndex],
                    childEdges[childIndex],
                    depth + 1);
            }

            return node;
        }

        private static int[] Copy(IReadOnlyList<int> source)
        {
            int[] result = new int[source.Count];
            for (int i = 0; i < source.Count; i++)
                result[i] = source[i];
            return result;
        }

        private static void Query(
            Node node,
            Vector2 minimum,
            Vector2 maximum,
            List<int> results,
            HashSet<int> unique)
        {
            if (!Overlaps(node.Minimum, node.Maximum, minimum, maximum))
                return;

            if (node.Children != null)
            {
                for (int i = 0; i < node.Children.Length; i++)
                    Query(node.Children[i], minimum, maximum, results, unique);
                return;
            }

            int[] edgeIndices = node.EdgeIndices;
            for (int i = 0; i < edgeIndices.Length; i++)
            {
                if (unique.Add(edgeIndices[i]))
                    results.Add(edgeIndices[i]);
            }
        }

        private static bool IntersectsRectangle(
            TerritorySpatialEdge edge,
            Vector2 minimum,
            Vector2 maximum)
        {
            if (!edge.Overlaps(minimum, maximum, Padding))
                return false;

            float tMinimum = 0f;
            float tMaximum = 1f;
            Vector2 delta = edge.End - edge.Start;
            return Clip(-delta.x, edge.Start.x - minimum.x - Padding, ref tMinimum, ref tMaximum) &&
                   Clip(delta.x, maximum.x - edge.Start.x + Padding, ref tMinimum, ref tMaximum) &&
                   Clip(-delta.y, edge.Start.y - minimum.y - Padding, ref tMinimum, ref tMaximum) &&
                   Clip(delta.y, maximum.y - edge.Start.y + Padding, ref tMinimum, ref tMaximum);
        }

        private static bool Clip(float direction, float distance, ref float minimum, ref float maximum)
        {
            if (Mathf.Abs(direction) <= Padding)
                return distance >= -Padding;

            float ratio = distance / direction;
            if (direction < 0f)
            {
                if (ratio > maximum)
                    return false;
                if (ratio > minimum)
                    minimum = ratio;
            }
            else
            {
                if (ratio < minimum)
                    return false;
                if (ratio < maximum)
                    maximum = ratio;
            }

            return true;
        }

        private static bool Overlaps(
            Vector2 firstMinimum,
            Vector2 firstMaximum,
            Vector2 secondMinimum,
            Vector2 secondMaximum)
            => firstMaximum.x >= secondMinimum.x - Padding &&
               firstMinimum.x <= secondMaximum.x + Padding &&
               firstMaximum.y >= secondMinimum.y - Padding &&
               firstMinimum.y <= secondMaximum.y + Padding;
    }
}
