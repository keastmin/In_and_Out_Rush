using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectIO.Territory
{
    public sealed class TerritoryBackgroundExpansionWorker : IDisposable
    {
        private readonly object _sync = new();
        private readonly CancellationTokenSource _cancellation = new();
        private Task<TerritoryBackgroundExpansionResult> _activeTask;
        private TerritoryBackgroundExpansionWorkItem _activeItem;
        private bool _disposed;

        public bool IsBusy
        {
            get
            {
                lock (_sync)
                    return _activeTask != null;
            }
        }

        public bool TrySchedule(
            TerritoryBackgroundExpansionWorkItem item,
            out string reason)
        {
            if (item == null)
            {
                reason = "Background expansion requires a work item.";
                return false;
            }

            lock (_sync)
            {
                if (_disposed)
                {
                    reason = "Background expansion worker is disposed.";
                    return false;
                }
                if (_activeTask != null)
                {
                    reason = "A Territory expansion is already being calculated.";
                    return false;
                }

                CancellationToken cancellation = _cancellation.Token;
                _activeItem = item;
                _activeTask = Task.Run(() => Process(item, cancellation), cancellation);
            }

            reason = null;
            return true;
        }

        public bool TryTakeCompleted(out TerritoryBackgroundExpansionResult result)
        {
            Task<TerritoryBackgroundExpansionResult> completed;
            TerritoryBackgroundExpansionWorkItem item;
            lock (_sync)
            {
                if (_activeTask == null || !_activeTask.IsCompleted)
                {
                    result = null;
                    return false;
                }

                completed = _activeTask;
                item = _activeItem;
                _activeTask = null;
                _activeItem = null;
            }

            try
            {
                result = completed.GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                result = TerritoryBackgroundExpansionResult.Failure(
                    item?.SessionId ?? 0UL,
                    item?.SourceRevision ?? 0UL,
                    exception.Message,
                    TimeSpan.Zero,
                    Environment.CurrentManagedThreadId);
            }

            return true;
        }

        public void Dispose()
        {
            lock (_sync)
            {
                if (_disposed)
                    return;

                _disposed = true;
                _cancellation.Cancel();
                _activeTask = null;
                _activeItem = null;
            }

            _cancellation.Dispose();
        }

        private static TerritoryBackgroundExpansionResult Process(
            TerritoryBackgroundExpansionWorkItem item,
            CancellationToken cancellation)
        {
            long started = Stopwatch.GetTimestamp();
            int threadId = Environment.CurrentManagedThreadId;
            try
            {
                cancellation.ThrowIfCancellationRequested();
                if (!global::Territory.TryCalculateExpansion(
                        item.SourceVertices,
                        item.TrailPoints,
                        out TerritoryMeshData meshData))
                {
                    return Failure(item, "Territory polygon expansion rejected the submitted Trail.", started, threadId);
                }

                cancellation.ThrowIfCancellationRequested();
                var vertices = meshData.Vertices.ToArray();
                var triangles = meshData.Triangles.ToArray();
                var presentation = new TerritoryExpansionPresentationData(
                    item.SourceRevision,
                    item.SourceRevision + 1UL,
                    vertices,
                    triangles);
                var packetizer = new TerritoryExpansionResultPacketizer();
                if (!packetizer.TryCreate(presentation, out var packets, out string reason))
                    return Failure(item, reason, started, threadId);

                return TerritoryBackgroundExpansionResult.Success(
                    item,
                    presentation,
                    packets,
                    Elapsed(started),
                    threadId);
            }
            catch (OperationCanceledException)
            {
                return Failure(item, "Background Territory expansion was cancelled.", started, threadId);
            }
            catch (Exception exception)
            {
                return Failure(item, exception.Message, started, threadId);
            }
        }

        private static TerritoryBackgroundExpansionResult Failure(
            TerritoryBackgroundExpansionWorkItem item,
            string reason,
            long started,
            int threadId)
            => TerritoryBackgroundExpansionResult.Failure(
                item,
                reason,
                Elapsed(started),
                threadId);

        private static TimeSpan Elapsed(long started)
            => TimeSpan.FromSeconds(
                (Stopwatch.GetTimestamp() - started) / (double)Stopwatch.Frequency);
    }
}
