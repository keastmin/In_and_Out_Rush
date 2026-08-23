using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectIO.Territory
{
    public sealed class TerritoryExpansionPresentationData
    {
        private readonly Vector2[] _vertices;
        private readonly int[] _triangles;

        public TerritoryExpansionPresentationData(
            ulong sourceRevision,
            ulong revision,
            Vector2[] vertices,
            int[] triangles)
            : this(sourceRevision, revision, vertices, triangles, false)
        {
        }

        private TerritoryExpansionPresentationData(
            ulong sourceRevision,
            ulong revision,
            Vector2[] vertices,
            int[] triangles,
            bool takeOwnership)
        {
            if (sourceRevision == 0UL || revision != sourceRevision + 1UL)
                throw new ArgumentOutOfRangeException(nameof(revision));
            if (vertices == null || vertices.Length < 3)
                throw new ArgumentException("Expansion result requires at least three vertices.", nameof(vertices));
            if (triangles == null || triangles.LongLength != ((long)vertices.Length - 2L) * 3L)
                throw new ArgumentException("Expansion triangle count does not match its polygon.", nameof(triangles));

            for (int i = 0; i < vertices.Length; i++)
            {
                if (!IsFinite(vertices[i].x) || !IsFinite(vertices[i].y))
                    throw new ArgumentOutOfRangeException(nameof(vertices));
            }
            for (int i = 0; i < triangles.Length; i++)
            {
                if (triangles[i] < 0 || triangles[i] >= vertices.Length)
                    throw new ArgumentOutOfRangeException(nameof(triangles));
            }

            SourceRevision = sourceRevision;
            Revision = revision;
            _vertices = takeOwnership ? vertices : (Vector2[])vertices.Clone();
            _triangles = takeOwnership ? triangles : (int[])triangles.Clone();
        }

        public ulong SourceRevision { get; }
        public ulong Revision { get; }
        public IReadOnlyList<Vector2> Vertices => _vertices;
        public IReadOnlyList<int> Triangles => _triangles;

        internal static TerritoryExpansionPresentationData TakeOwnershipValidated(
            ulong sourceRevision,
            ulong revision,
            Vector2[] vertices,
            int[] triangles)
            => new(sourceRevision, revision, vertices, triangles, true);

        private static bool IsFinite(float value)
            => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
