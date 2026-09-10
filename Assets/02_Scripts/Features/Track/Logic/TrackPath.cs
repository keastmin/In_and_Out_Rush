using System;
using UnityEngine;

namespace ProjectIO.Tracks
{
    public sealed class TrackPath
    {
        public TrackPath(Vector3[] vertices, bool isClosed)
        {
            if (vertices == null)
            {
                throw new ArgumentNullException(nameof(vertices));
            }

            if (vertices.Length < 2)
            {
                throw new ArgumentException("A track path requires at least two vertices.", nameof(vertices));
            }

            Vertices = (Vector3[])vertices.Clone();
            IsClosed = isClosed;
        }

        public Vector3[] Vertices { get; }

        public bool IsClosed { get; }
    }
}
