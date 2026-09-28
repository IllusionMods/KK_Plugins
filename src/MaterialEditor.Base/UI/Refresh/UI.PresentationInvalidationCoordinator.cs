using System;
using System.Collections.Generic;

namespace MaterialEditorAPI
{
    [Flags]
    internal enum PresentationInvalidationReason
    {
        None = 0,
        Search = 1,
        Conditions = 2
    }

    internal struct PresentationInvalidationWorkerLease
    {
        private readonly int _generation;
        private readonly long _leaseId;

        internal PresentationInvalidationWorkerLease(int generation, long leaseId)
        {
            _generation = generation;
            _leaseId = leaseId;
        }

        internal int Generation
        {
            get { return _generation; }
        }

        internal long LeaseId
        {
            get { return _leaseId; }
        }
    }

    internal sealed class PresentationInvalidationBatch<TConditionSource>
    {
        private readonly int _generation;
        private readonly PresentationInvalidationReason _reason;
        private readonly TConditionSource[] _conditionSources;

        internal PresentationInvalidationBatch(
            int generation,
            PresentationInvalidationReason reason,
            TConditionSource[] conditionSources)
        {
            _generation = generation;
            _reason = reason;
            _conditionSources = conditionSources;
        }

        internal int Generation
        {
            get { return _generation; }
        }

        internal PresentationInvalidationReason Reason
        {
            get { return _reason; }
        }

        internal TConditionSource[] ConditionSources
        {
            get { return _conditionSources; }
        }
    }

    // Pure coordinator for a Unity owner to drive from its end-of-frame worker.
    // It deliberately has no locking: construct and use it only on the main thread.
    internal sealed class PresentationInvalidationCoordinator<TConditionSource>
    {
        private static readonly TConditionSource[] NoConditionSources =
            new TConditionSource[0];

        private readonly HashSet<TConditionSource> _pendingConditionSet;
        private readonly List<TConditionSource> _pendingConditionSources =
            new List<TConditionSource>();

        private int _generation = 1;
        private long _nextLeaseId;
        private long _activeLeaseId;
        private bool _searchPending;
        private bool _workerActive;
        private bool _flushActive;
        private bool _hasLastFlushFrame;
        private int _lastFlushFrameId;

        internal PresentationInvalidationCoordinator()
            : this(null)
        {
        }

        internal PresentationInvalidationCoordinator(
            IEqualityComparer<TConditionSource> conditionSourceComparer)
        {
            _pendingConditionSet = conditionSourceComparer == null
                ? new HashSet<TConditionSource>()
                : new HashSet<TConditionSource>(conditionSourceComparer);
        }

        internal bool HasPending
        {
            get { return _searchPending || _pendingConditionSources.Count != 0; }
        }

        internal bool RequestSearch()
        {
            var changed = !_searchPending || _pendingConditionSources.Count != 0;
            _searchPending = true;
            _pendingConditionSources.Clear();
            _pendingConditionSet.Clear();
            return changed;
        }

        internal bool RequestCondition(TConditionSource source)
        {
            // A search rebuild observes all condition state, so retaining individual
            // condition sources in the same batch would only duplicate work.
            if (_searchPending || !_pendingConditionSet.Add(source))
                return false;

            _pendingConditionSources.Add(source);
            return true;
        }

        internal bool TryAcquireWorker(
            out PresentationInvalidationWorkerLease lease)
        {
            if (_workerActive || !HasPending)
            {
                lease = new PresentationInvalidationWorkerLease();
                return false;
            }

            _workerActive = true;
            _activeLeaseId = NextLeaseId();
            lease = new PresentationInvalidationWorkerLease(
                _generation,
                _activeLeaseId);
            return true;
        }

        internal bool IsWorkerLeaseCurrent(
            PresentationInvalidationWorkerLease lease)
        {
            return _workerActive &&
                   lease.Generation == _generation &&
                   lease.LeaseId != 0 &&
                   lease.LeaseId == _activeLeaseId;
        }

        internal bool IsGenerationCurrent(int generation)
        {
            return generation == _generation;
        }

        internal bool TryBeginFlush(
            PresentationInvalidationWorkerLease lease,
            int frameId,
            out PresentationInvalidationBatch<TConditionSource> batch)
        {
            batch = null;
            if (!IsWorkerLeaseCurrent(lease) ||
                _flushActive ||
                !HasPending ||
                (_hasLastFlushFrame && frameId == _lastFlushFrameId))
                return false;

            batch = BeginFlush();
            _hasLastFlushFrame = true;
            _lastFlushFrameId = frameId;
            return true;
        }

        // Exceptional synchronous recovery path for an owner whose coroutine
        // could not be started. It drains the already-acquired lease without an
        // end-of-frame gate so pending work cannot be left without a worker.
        internal bool TryBeginRecoveryFlush(
            PresentationInvalidationWorkerLease lease,
            out PresentationInvalidationBatch<TConditionSource> batch)
        {
            batch = null;
            if (!IsWorkerLeaseCurrent(lease)
                || _flushActive
                || !HasPending)
                return false;

            batch = BeginFlush();
            return true;
        }

        private PresentationInvalidationBatch<TConditionSource> BeginFlush()
        {
            var batch = new PresentationInvalidationBatch<TConditionSource>(
                _generation,
                _searchPending ? PresentationInvalidationReason.Search : PresentationInvalidationReason.Conditions,
                _searchPending ? NoConditionSources : _pendingConditionSources.ToArray());

            // Snapshot before clearing so requests from the callback form a new batch.
            ClearPending();
            _flushActive = true;
            return batch;
        }

        private void ClearPending()
        {
            _searchPending = false;
            _pendingConditionSources.Clear();
            _pendingConditionSet.Clear();
        }

        // Returns true when the same worker lease must wait for another frame.
        internal bool CompleteFlush(
            PresentationInvalidationWorkerLease lease)
        {
            if (!IsWorkerLeaseCurrent(lease) || !_flushActive)
                return false;

            _flushActive = false;
            if (HasPending)
                return true;

            ReleaseWorkerLease();
            return false;
        }

        // Used when the owner could not start its coroutine. Pending work is kept
        // so a later request can acquire a fresh worker lease and retry it.
        internal bool AbandonWorker(
            PresentationInvalidationWorkerLease lease)
        {
            if (!IsWorkerLeaseCurrent(lease) || _flushActive)
                return false;

            ReleaseWorkerLease();
            return true;
        }

        internal void Cancel()
        {
            AdvanceGeneration();
            ClearPending();
            _flushActive = false;
            ReleaseWorkerLease();

            // Do not reset the last frame marker. A cancelled owner and its
            // replacement must still be unable to flush twice in one frame.
        }

        private void ReleaseWorkerLease()
        {
            _workerActive = false;
            _activeLeaseId = 0;
        }

        private long NextLeaseId()
        {
            unchecked
            {
                _nextLeaseId++;
                if (_nextLeaseId == 0)
                    _nextLeaseId++;
            }
            return _nextLeaseId;
        }

        private void AdvanceGeneration()
        {
            unchecked
            {
                _generation++;
                if (_generation == 0)
                    _generation++;
            }
        }
    }

    // Owns one mutable deferred request without allocating a request object for
    // every replacement. Coroutine scheduling remains in MaterialEditorUI.
    internal sealed class DeferredRefreshCoordinator
    {
        private int _version;
        private bool _hasPending;
        private object _target;
        private object _data;
        private string _filter;
        private bool _workerRunning;
        private int _countdownVersion;
        private int _framesRemaining;

        internal int Schedule(object target, object data, string filter)
        {
            _version++;
            _target = target;
            _data = data;
            _filter = filter;
            _hasPending = true;
            return _version;
        }

        internal void Cancel()
        {
            _version++;
            _hasPending = false;
            _target = null;
            _data = null;
            _filter = null;
            _workerRunning = false;
            _framesRemaining = 0;
        }

        internal bool IsCurrent(int version)
        {
            return _hasPending && version == _version;
        }

        internal bool TryStartWorker()
        {
            if (_workerRunning || !_hasPending)
                return false;
            _workerRunning = true;
            return true;
        }

        internal void WorkerStopped()
        {
            _workerRunning = false;
        }

        internal bool AdvanceFrame(
            int delayFrames,
            out object target,
            out object data,
            out string filter)
        {
            if (!_hasPending)
            {
                target = null;
                data = null;
                filter = null;
                return false;
            }

            if (_countdownVersion != _version)
            {
                _countdownVersion = _version;
                _framesRemaining = delayFrames < 1 ? 1 : delayFrames;
            }

            _framesRemaining--;
            if (_framesRemaining > 0)
            {
                target = null;
                data = null;
                filter = null;
                return false;
            }

            return TryTake(_countdownVersion, out target, out data, out filter);
        }

        internal bool TryTake(
            int version,
            out object target,
            out object data,
            out string filter)
        {
            if (!IsCurrent(version))
            {
                target = null;
                data = null;
                filter = null;
                return false;
            }

            _hasPending = false;
            target = _target;
            data = _data;
            filter = _filter;
            _target = null;
            _data = null;
            _filter = null;
            _framesRemaining = 0;
            return true;
        }

        internal bool HasPending
        {
            get { return _hasPending; }
        }
    }
}
