using UnityEngine;

namespace ProjectIO.Tracks
{
    public readonly struct TrackSegment
    {
        public TrackSegment(Vector3 start, Vector3 end)
        {
            Start = start;
            End = end;
        }

        public Vector3 Start { get; }

        public Vector3 End { get; }
    }
}
