using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectIO.Territory
{
    public sealed class TerritoryCompactExpansionWorker : IDisposable
    {
        private const int WorkBatchSize = 2048;

        private readonly object _sync = new();
        private readonly Queue<TerritoryCompactExpansionWorkItem> _pending = new();
        private readonly Queue<TerritoryCompactExpansionWorkResult> _completed = new();
        private readonly CancellationTokenSource _cancellation = new();

        private TerritoryCompactSnapshot _workerCurrent;
        private ulong _nextExpectedSourceRevision;
        private ulong _lastEnqueuedSessionId;
        private bool _processorRunning;
        private bool _faulted;
        private bool _disposed;

        public TerritoryCompactExpansionWorker(TerritoryCompactSnapshot initialSnapshot)
        {
            _workerCurrent = initialSnapshot ?? throw new ArgumentNullException(nameof(initialSnapshot));
            _nextExpectedSourceRevision = initialSnapshot.Revision;
        }

        public int PendingCount
        {
            get
            {
                lock (_sync)
                    return _pending.Count + (_processorRunning ? 1 : 0);
            }
        }

        public int CompletedCount
        {
            get
            {
                lock (_sync)
                    return _completed.Count;
            }
        }

        public bool IsFaulted
        {
            get
            {
                lock (_sync)
                    return _faulted;
            }
        }

        public bool TryEnqueue(TerritoryCompactExpansionWorkItem item, out string reason)
        {
            if (item == null)
            {
                reason = "A compact expansion worker requires a work item.";
                return false;
            }

            lock (_sync)
            {
                if (_disposed)
                {
                    reason = "The compact expansion worker is disposed.";
                    return false;
                }
                if (_faulted)
                {
                    reason = "The compact expansion worker stopped after an earlier failure.";
                    return false;
                }
                if (item.ExpectedSourceRevision != _nextExpectedSourceRevision)
                {
                    reason = $"Expected work for source revision {_nextExpectedSourceRevision}, received {item.ExpectedSourceRevision}.";
                    return false;
                }
                if (item.SessionId <= _lastEnqueuedSessionId)
                {
                    reason = $"Work SessionId {item.SessionId} is stale; the last queued SessionId is {_lastEnqueuedSessionId}.";
                    return false;
                }

                _pending.Enqueue(item);
                _nextExpectedSourceRevision++;
                _lastEnqueuedSessionId = item.SessionId;
                if (!_processorRunning)
                {
                    _processorRunning = true;
                    _ = Task.Run(ProcessPending);
                }
            }

            reason = null;
            return true;
        }

        public bool TryTakeCompleted(out TerritoryCompactExpansionWorkResult result)
        {
            lock (_sync)
            {
                if (_completed.Count == 0)
                {
                    result = null;
                    return false;
                }

                result = _completed.Dequeue();
                return true;
            }
        }

        public void Dispose()
        {
            lock (_sync)
            {
                if (_disposed)
                    return;

                _disposed = true;
                _cancellation.Cancel();
                _pending.Clear();
                _completed.Clear();
            }
        }

        private void ProcessPending()
        {
            while (true)
            {
                TerritoryCompactExpansionWorkItem item;
                TerritoryCompactSnapshot source;
                lock (_sync)
                {
                    if (_disposed || _cancellation.IsCancellationRequested || _faulted)
                    {
                        _processorRunning = false;
                        return;
                    }
                    if (_pending.Count == 0)
                    {
                        _processorRunning = false;
                        return;
                    }

                    item = _pending.Dequeue();
                    source = _workerCurrent;
                }

                TerritoryCompactExpansionWorkResult result = ProcessOne(source, item, _cancellation.Token);
                lock (_sync)
                {
                    if (_disposed || _cancellation.IsCancellationRequested)
                    {
                        _processorRunning = false;
                        return;
                    }

                    _completed.Enqueue(result);
                    if (!result.IsSuccess)
                    {
                        _faulted = true;
                        _pending.Clear();
                        _processorRunning = false;
                        return;
                    }

                    _workerCurrent = result.Candidate;
                }
            }
        }

        private static TerritoryCompactExpansionWorkResult ProcessOne(
            TerritoryCompactSnapshot source,
            TerritoryCompactExpansionWorkItem item,
            CancellationToken cancellation)
        {
            try
            {
                if (source.Revision != item.ExpectedSourceRevision)
                    return TerritoryCompactExpansionWorkResult.Failure(item, "Worker source revision does not match the queued request.");

                long started = Stopwatch.GetTimestamp();
                if (!TerritoryBoundaryLoopIndex.TryCreate(source, out TerritoryBoundaryLoopIndex index, out string reason))
                    return TerritoryCompactExpansionWorkResult.Failure(item, reason);

                var expansion = new TerritoryChunkExpansionSession();
                if (!expansion.TryBegin(index, item.SessionId, out reason))
                    return TerritoryCompactExpansionWorkResult.Failure(item, reason);
                for (int i = 0; i < item.Fragments.Count; i++)
                {
                    cancellation.ThrowIfCancellationRequested();
                    if (!expansion.TryAppendFragment(item.Fragments[i], out reason))
                        return TerritoryCompactExpansionWorkResult.Failure(item, reason);
                }
                if (!expansion.TryComplete(item.SessionId, out TerritoryChunkExpansionPlan plan, out reason))
                    return TerritoryCompactExpansionWorkResult.Failure(item, reason);

                var materializer = new TerritoryChunkExpansionMaterializationSession();
                if (!materializer.TryBegin(source, index, plan, out reason))
                    return TerritoryCompactExpansionWorkResult.Failure(item, reason);
                if (!CompleteMaterialization(materializer, cancellation, out reason) ||
                    !materializer.TryGetResult(out TerritoryChunkExpansionMaterialization materialization, out reason))
                {
                    return TerritoryCompactExpansionWorkResult.Failure(item, reason);
                }

                var apply = new TerritoryCompactApplySession();
                if (!apply.TryBegin(source, materialization, out reason))
                    return TerritoryCompactExpansionWorkResult.Failure(item, reason);
                if (!CompleteApply(apply, cancellation, out reason) ||
                    !apply.TryGetResult(out TerritoryCompactSnapshot candidate, out TerritoryCompactCommitResult commit, out reason))
                {
                    return TerritoryCompactExpansionWorkResult.Failure(item, reason);
                }

                long elapsedTicks = Stopwatch.GetTimestamp() - started;
                TimeSpan elapsed = TimeSpan.FromSeconds(elapsedTicks / (double)Stopwatch.Frequency);
                var metrics = new TerritoryCompactExpansionWorkerMetrics(
                    item.Fragments.Count,
                    elapsed,
                    Environment.CurrentManagedThreadId,
                    plan.Metrics,
                    materialization.Metrics,
                    commit.Metrics);
                return TerritoryCompactExpansionWorkResult.Success(item, candidate, apply, metrics);
            }
            catch (OperationCanceledException)
            {
                return TerritoryCompactExpansionWorkResult.Failure(item, "Compact expansion work was cancelled.");
            }
            catch (Exception exception)
            {
                return TerritoryCompactExpansionWorkResult.Failure(item, exception.Message);
            }
        }

        private static bool CompleteMaterialization(
            TerritoryChunkExpansionMaterializationSession session,
            CancellationToken cancellation,
            out string reason)
        {
            bool completed = false;
            while (!completed)
            {
                cancellation.ThrowIfCancellationRequested();
                if (!session.TryStep(WorkBatchSize, out _, out completed, out reason))
                    return false;
            }

            reason = null;
            return true;
        }

        private static bool CompleteApply(
            TerritoryCompactApplySession session,
            CancellationToken cancellation,
            out string reason)
        {
            bool completed = false;
            while (!completed)
            {
                cancellation.ThrowIfCancellationRequested();
                if (!session.TryStep(WorkBatchSize, out _, out completed, out reason))
                    return false;
            }

            reason = null;
            return true;
        }
    }
}
