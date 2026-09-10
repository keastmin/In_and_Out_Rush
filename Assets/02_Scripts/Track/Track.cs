using System.Collections.Generic;
using ProjectIO.Tracks;

namespace Dev
{
    public sealed class Track
    {
        public Track(TrackStage stage, IReadOnlyList<TrackPath> paths)
        {
            Stage = stage;
            Paths = paths ?? new List<TrackPath>();
            Segments = TrackGeometryGenerator.CreateSegments(Paths);
        }

        public TrackStage Stage { get; }

        public int Level => (int)Stage;

        public IReadOnlyList<TrackPath> Paths { get; }

        public IReadOnlyList<TrackSegment> Segments { get; }
    }
}
