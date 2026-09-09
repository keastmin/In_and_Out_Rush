using System;
using System.Collections.Generic;

namespace ProjectIO.Territory
{
    public static class TerritorySegmentChunkTraversal
    {
        public readonly struct SegmentPart
        {
            public SegmentPart(
                TerritoryChunkCoordinate chunk,
                FixedTerritoryPoint start,
                FixedTerritoryPoint end)
            {
                Chunk = chunk;
                Start = start;
                End = end;
            }

            public TerritoryChunkCoordinate Chunk { get; }
            public FixedTerritoryPoint Start { get; }
            public FixedTerritoryPoint End { get; }
        }

        public static void Split(
            FixedTerritoryPoint start,
            FixedTerritoryPoint end,
            List<SegmentPart> results)
        {
            if (results == null)
                throw new ArgumentNullException(nameof(results));

            results.Clear();
            TerritoryChunkCoordinate currentChunk = TerritoryChunkCoordinate.FromPoint(start);
            if (start == end)
            {
                results.Add(new SegmentPart(currentChunk, start, end));
                return;
            }

            long deltaX = (long)end.X - start.X;
            long deltaY = (long)end.Y - start.Y;
            int stepX = Math.Sign(deltaX);
            int stepY = Math.Sign(deltaY);

            decimal nextX = GetFirstBoundaryTime(start.X, deltaX, currentChunk.MinimumX, currentChunk.MaximumX);
            decimal nextY = GetFirstBoundaryTime(start.Y, deltaY, currentChunk.MinimumY, currentChunk.MaximumY);
            decimal stepTimeX = GetBoundaryStepTime(deltaX);
            decimal stepTimeY = GetBoundaryStepTime(deltaY);
            decimal currentTime = 0m;

            long maximumTransitions =
                Math.Abs((long)TerritoryChunkCoordinate.FromPoint(end).X - currentChunk.X) +
                Math.Abs((long)TerritoryChunkCoordinate.FromPoint(end).Y - currentChunk.Y) + 2L;
            long transitions = 0L;

            while (true)
            {
                decimal nextTime = Math.Min(1m, Math.Min(nextX, nextY));
                bool crossesX = nextX == nextTime && nextX <= 1m;
                bool crossesY = nextY == nextTime && nextY <= 1m;

                if (nextTime > currentTime)
                {
                    FixedTerritoryPoint partStart = Interpolate(start, deltaX, deltaY, currentTime);
                    FixedTerritoryPoint partEnd = Interpolate(start, deltaX, deltaY, nextTime);
                    results.Add(new SegmentPart(currentChunk, partStart, partEnd));
                }

                if (nextTime >= 1m)
                    break;

                if (crossesX)
                {
                    currentChunk = new TerritoryChunkCoordinate(currentChunk.X + stepX, currentChunk.Y);
                    nextX += stepTimeX;
                }

                if (crossesY)
                {
                    currentChunk = new TerritoryChunkCoordinate(currentChunk.X, currentChunk.Y + stepY);
                    nextY += stepTimeY;
                }

                currentTime = nextTime;
                transitions++;
                if (transitions > maximumTransitions)
                    throw new InvalidOperationException("Chunk traversal exceeded its deterministic transition bound.");
            }
        }

        private static decimal GetFirstBoundaryTime(
            int start,
            long delta,
            long minimum,
            long maximum)
        {
            if (delta > 0L)
                return (decimal)(maximum - start) / delta;
            if (delta < 0L)
                return (decimal)(start - minimum) / -delta;

            return decimal.MaxValue;
        }

        private static decimal GetBoundaryStepTime(long delta)
        {
            if (delta == 0L)
                return decimal.MaxValue;

            return (decimal)TerritoryChunkCoordinate.SizeInFixedUnits / Math.Abs(delta);
        }

        private static FixedTerritoryPoint Interpolate(
            FixedTerritoryPoint start,
            long deltaX,
            long deltaY,
            decimal time)
        {
            decimal x = start.X + deltaX * time;
            decimal y = start.Y + deltaY * time;
            return new FixedTerritoryPoint(RoundToInt(x), RoundToInt(y));
        }

        private static int RoundToInt(decimal value)
        {
            decimal rounded = Math.Round(value, 0, MidpointRounding.AwayFromZero);
            if (rounded < int.MinValue || rounded > int.MaxValue)
                throw new OverflowException("Interpolated fixed-point coordinate exceeds Int32 range.");

            return (int)rounded;
        }
    }
}
