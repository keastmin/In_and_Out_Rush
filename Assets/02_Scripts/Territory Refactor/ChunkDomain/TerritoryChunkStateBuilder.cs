using System;
using System.Collections.Generic;

namespace ProjectIO.Territory
{
    public sealed class TerritoryChunkStateBuilder
    {
        private const long MaximumChunkBuildCount = 1_000_000L;

        private readonly List<FixedTerritoryPoint> _normalizedPolygon = new();
        private readonly List<TerritorySegmentChunkTraversal.SegmentPart> _segmentParts = new();
        private readonly List<decimal> _scanlineIntersections = new();

        public bool TryBuild(
            IReadOnlyList<FixedTerritoryPoint> polygon,
            ulong revision,
            out TerritoryChunkSnapshot snapshot,
            out string reason)
        {
            snapshot = null;
            if (revision == 0)
            {
                reason = "A built Chunk snapshot requires a nonzero revision.";
                return false;
            }

            try
            {
                if (!TryNormalizeAndValidate(polygon, out reason))
                    return false;

                var boundaryByChunk = new Dictionary<
                    TerritoryChunkCoordinate,
                    List<TerritoryChunkBoundarySegment>>();
                if (!TryBuildBoundarySegments(boundaryByChunk, out reason))
                    return false;

                GetBounds(
                    out int minimumX,
                    out int minimumY,
                    out int maximumX,
                    out int maximumY);
                TerritoryChunkCoordinate minimumChunk = TerritoryChunkCoordinate.FromPoint(
                    new FixedTerritoryPoint(minimumX, minimumY));
                TerritoryChunkCoordinate maximumChunk = TerritoryChunkCoordinate.FromPoint(
                    new FixedTerritoryPoint(maximumX, maximumY));

                long width = (long)maximumChunk.X - minimumChunk.X + 1L;
                long height = (long)maximumChunk.Y - minimumChunk.Y + 1L;
                if (width <= 0L || height <= 0L || width > MaximumChunkBuildCount ||
                    height > MaximumChunkBuildCount || width * height > MaximumChunkBuildCount)
                {
                    reason = $"Polygon spans an unsupported Chunk build area of {width} x {height}.";
                    return false;
                }

                var chunks = new Dictionary<TerritoryChunkCoordinate, TerritoryChunkCoverage>();
                for (int y = minimumChunk.Y; y <= maximumChunk.Y; y++)
                {
                    long centerY =
                        (long)y * TerritoryChunkCoordinate.SizeInFixedUnits +
                        TerritoryChunkCoordinate.SizeInFixedUnits / 2;
                    BuildScanlineIntersections(centerY);
                    for (int x = minimumChunk.X; x <= maximumChunk.X; x++)
                    {
                        var chunk = new TerritoryChunkCoordinate(x, y);
                        long centerX = chunk.MinimumX + TerritoryChunkCoordinate.SizeInFixedUnits / 2;

                        if (boundaryByChunk.TryGetValue(
                                chunk,
                                out List<TerritoryChunkBoundarySegment> segments))
                        {
                            bool centerInside = IsPointInsideOrOnBoundary(centerX, centerY);
                            chunks.Add(
                                chunk,
                                TerritoryChunkCoverage.Boundary(chunk, centerInside, segments));
                        }
                        else if (IsInsideFromCurrentScanline(centerX))
                        {
                            chunks.Add(chunk, TerritoryChunkCoverage.Full(chunk));
                        }
                    }
                }

                snapshot = new TerritoryChunkSnapshot(revision, chunks);
                reason = null;
                return true;
            }
            catch (Exception exception) when (
                exception is ArgumentException ||
                exception is ArithmeticException ||
                exception is InvalidOperationException)
            {
                reason = $"Chunk state build failed: {exception.Message}";
                snapshot = null;
                return false;
            }
        }

        private bool TryNormalizeAndValidate(
            IReadOnlyList<FixedTerritoryPoint> polygon,
            out string reason)
        {
            _normalizedPolygon.Clear();
            if (polygon == null)
            {
                reason = "Polygon cannot be null.";
                return false;
            }

            for (int i = 0; i < polygon.Count; i++)
            {
                FixedTerritoryPoint point = polygon[i];
                if (_normalizedPolygon.Count == 0 || _normalizedPolygon[^1] != point)
                    _normalizedPolygon.Add(point);
            }

            if (_normalizedPolygon.Count > 1 &&
                _normalizedPolygon[0] == _normalizedPolygon[^1])
            {
                _normalizedPolygon.RemoveAt(_normalizedPolygon.Count - 1);
            }

            RemoveExactCollinearMiddlePoints();
            if (_normalizedPolygon.Count < 3)
            {
                reason = "Polygon requires at least three distinct non-collinear points.";
                return false;
            }

            decimal twiceArea = 0m;
            for (int i = 0; i < _normalizedPolygon.Count; i++)
            {
                FixedTerritoryPoint current = _normalizedPolygon[i];
                FixedTerritoryPoint next = _normalizedPolygon[(i + 1) % _normalizedPolygon.Count];
                twiceArea += (decimal)current.X * next.Y - (decimal)next.X * current.Y;
            }

            if (twiceArea == 0m)
            {
                reason = "Polygon area must be nonzero.";
                return false;
            }

            if (HasSelfIntersection())
            {
                reason = "Polygon must be simple and cannot self-intersect or repeat a boundary point.";
                return false;
            }

            reason = null;
            return true;
        }

        private void RemoveExactCollinearMiddlePoints()
        {
            bool removed;
            do
            {
                removed = false;
                if (_normalizedPolygon.Count < 3)
                    return;

                for (int i = 0; i < _normalizedPolygon.Count; i++)
                {
                    FixedTerritoryPoint previous =
                        _normalizedPolygon[(i - 1 + _normalizedPolygon.Count) % _normalizedPolygon.Count];
                    FixedTerritoryPoint current = _normalizedPolygon[i];
                    FixedTerritoryPoint next = _normalizedPolygon[(i + 1) % _normalizedPolygon.Count];
                    if (Orientation(previous, current, next) != 0m ||
                        !IsBetween(previous, current, next))
                    {
                        continue;
                    }

                    _normalizedPolygon.RemoveAt(i);
                    removed = true;
                    break;
                }
            }
            while (removed);
        }

        private bool HasSelfIntersection()
        {
            int count = _normalizedPolygon.Count;
            for (int first = 0; first < count; first++)
            {
                FixedTerritoryPoint a = _normalizedPolygon[first];
                FixedTerritoryPoint b = _normalizedPolygon[(first + 1) % count];
                for (int second = first + 1; second < count; second++)
                {
                    if (second == first || second == first + 1 ||
                        (first == 0 && second == count - 1))
                    {
                        continue;
                    }

                    FixedTerritoryPoint c = _normalizedPolygon[second];
                    FixedTerritoryPoint d = _normalizedPolygon[(second + 1) % count];
                    if (SegmentsIntersect(a, b, c, d))
                        return true;
                }
            }

            return false;
        }

        private bool TryBuildBoundarySegments(
            Dictionary<TerritoryChunkCoordinate, List<TerritoryChunkBoundarySegment>> boundaryByChunk,
            out string reason)
        {
            int sequence = 0;
            for (int edgeIndex = 0; edgeIndex < _normalizedPolygon.Count; edgeIndex++)
            {
                FixedTerritoryPoint start = _normalizedPolygon[edgeIndex];
                FixedTerritoryPoint end = _normalizedPolygon[(edgeIndex + 1) % _normalizedPolygon.Count];
                TerritorySegmentChunkTraversal.Split(start, end, _segmentParts);
                for (int partIndex = 0; partIndex < _segmentParts.Count; partIndex++)
                {
                    TerritorySegmentChunkTraversal.SegmentPart part = _segmentParts[partIndex];
                    if (part.Start == part.End)
                        continue;
                    if (sequence == int.MaxValue)
                    {
                        reason = "Boundary segment sequence exhausted Int32 range.";
                        return false;
                    }

                    if (!boundaryByChunk.TryGetValue(
                            part.Chunk,
                            out List<TerritoryChunkBoundarySegment> segments))
                    {
                        segments = new List<TerritoryChunkBoundarySegment>();
                        boundaryByChunk.Add(part.Chunk, segments);
                    }

                    segments.Add(new TerritoryChunkBoundarySegment(
                        sequence,
                        ToLocal(part.Chunk, part.Start),
                        ToLocal(part.Chunk, part.End)));
                    sequence++;
                }
            }

            if (sequence == 0)
            {
                reason = "Polygon produced no nonzero Chunk boundary segments.";
                return false;
            }

            reason = null;
            return true;
        }

        private static TerritoryChunkLocalPoint ToLocal(
            TerritoryChunkCoordinate chunk,
            FixedTerritoryPoint point)
        {
            long localX = point.X - chunk.MinimumX;
            long localY = point.Y - chunk.MinimumY;
            if (localX < 0L || localX > TerritoryChunkCoordinate.SizeInFixedUnits ||
                localY < 0L || localY > TerritoryChunkCoordinate.SizeInFixedUnits)
            {
                throw new InvalidOperationException($"Boundary point {point} is outside Chunk {chunk}.");
            }

            return new TerritoryChunkLocalPoint((int)localX, (int)localY);
        }

        private void GetBounds(
            out int minimumX,
            out int minimumY,
            out int maximumX,
            out int maximumY)
        {
            minimumX = maximumX = _normalizedPolygon[0].X;
            minimumY = maximumY = _normalizedPolygon[0].Y;
            for (int i = 1; i < _normalizedPolygon.Count; i++)
            {
                FixedTerritoryPoint point = _normalizedPolygon[i];
                minimumX = Math.Min(minimumX, point.X);
                minimumY = Math.Min(minimumY, point.Y);
                maximumX = Math.Max(maximumX, point.X);
                maximumY = Math.Max(maximumY, point.Y);
            }
        }

        private bool IsPointInsideOrOnBoundary(long x, long y)
        {
            bool inside = false;
            for (int i = 0, j = _normalizedPolygon.Count - 1;
                 i < _normalizedPolygon.Count;
                 j = i++)
            {
                FixedTerritoryPoint a = _normalizedPolygon[j];
                FixedTerritoryPoint b = _normalizedPolygon[i];
                if (PointOnSegment(x, y, a, b))
                    return true;

                bool crosses = (a.Y > y) != (b.Y > y);
                if (!crosses)
                    continue;

                decimal atX = a.X +
                    (decimal)(b.X - (long)a.X) * (y - a.Y) / (b.Y - (long)a.Y);
                if (x < atX)
                    inside = !inside;
            }

            return inside;
        }

        private void BuildScanlineIntersections(long y)
        {
            _scanlineIntersections.Clear();
            for (int i = 0; i < _normalizedPolygon.Count; i++)
            {
                FixedTerritoryPoint start = _normalizedPolygon[i];
                FixedTerritoryPoint end = _normalizedPolygon[(i + 1) % _normalizedPolygon.Count];
                if ((start.Y > y) == (end.Y > y))
                    continue;

                decimal atX = start.X +
                    (decimal)(end.X - (long)start.X) * (y - start.Y) /
                    (end.Y - (long)start.Y);
                _scanlineIntersections.Add(atX);
            }

            _scanlineIntersections.Sort();
        }

        private bool IsInsideFromCurrentScanline(long x)
        {
            int low = 0;
            int high = _scanlineIntersections.Count;
            while (low < high)
            {
                int middle = low + (high - low) / 2;
                if (_scanlineIntersections[middle] <= x)
                    low = middle + 1;
                else
                    high = middle;
            }

            int intersectionsToRight = _scanlineIntersections.Count - low;
            return (intersectionsToRight & 1) != 0;
        }

        private static bool SegmentsIntersect(
            FixedTerritoryPoint a,
            FixedTerritoryPoint b,
            FixedTerritoryPoint c,
            FixedTerritoryPoint d)
        {
            decimal abC = Orientation(a, b, c);
            decimal abD = Orientation(a, b, d);
            decimal cdA = Orientation(c, d, a);
            decimal cdB = Orientation(c, d, b);

            if (abC == 0m && PointOnSegment(c.X, c.Y, a, b))
                return true;
            if (abD == 0m && PointOnSegment(d.X, d.Y, a, b))
                return true;
            if (cdA == 0m && PointOnSegment(a.X, a.Y, c, d))
                return true;
            if (cdB == 0m && PointOnSegment(b.X, b.Y, c, d))
                return true;

            return Math.Sign(abC) != Math.Sign(abD) && Math.Sign(cdA) != Math.Sign(cdB);
        }

        private static decimal Orientation(
            FixedTerritoryPoint a,
            FixedTerritoryPoint b,
            FixedTerritoryPoint c)
            => (decimal)(b.X - (long)a.X) * (c.Y - (long)a.Y) -
               (decimal)(b.Y - (long)a.Y) * (c.X - (long)a.X);

        private static bool IsBetween(
            FixedTerritoryPoint start,
            FixedTerritoryPoint point,
            FixedTerritoryPoint end)
            => point.X >= Math.Min(start.X, end.X) && point.X <= Math.Max(start.X, end.X) &&
               point.Y >= Math.Min(start.Y, end.Y) && point.Y <= Math.Max(start.Y, end.Y);

        private static bool PointOnSegment(
            long x,
            long y,
            FixedTerritoryPoint start,
            FixedTerritoryPoint end)
        {
            decimal cross =
                (decimal)(end.X - (long)start.X) * (y - start.Y) -
                (decimal)(end.Y - (long)start.Y) * (x - start.X);
            return cross == 0m &&
                   x >= Math.Min(start.X, end.X) && x <= Math.Max(start.X, end.X) &&
                   y >= Math.Min(start.Y, end.Y) && y <= Math.Max(start.Y, end.Y);
        }
    }
}
