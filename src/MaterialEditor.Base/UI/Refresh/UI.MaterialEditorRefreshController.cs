using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MaterialEditorAPI
{
    /// <summary>
    /// Owns deferred population and coalesced presentation invalidation for
    /// one Material Editor UI host.
    /// </summary>
    internal sealed class MaterialEditorRefreshController
    {
        private readonly MaterialEditorUI _host;
        private readonly DeferredRefreshCoordinator _deferredRefresh =
            new DeferredRefreshCoordinator();
        private Coroutine _deferredRefreshCoroutine;
        private static readonly WaitForEndOfFrame PresentationEndOfFrame =
            new WaitForEndOfFrame();
        private readonly PresentationInvalidationCoordinator<
            MaterialConditionInvalidationHandle>
            _presentationInvalidation =
                new PresentationInvalidationCoordinator<
                    MaterialConditionInvalidationHandle>();
        private Coroutine _presentationInvalidationCoroutine;
        private long _presentationInvalidationCoroutineLeaseId;

        internal MaterialEditorRefreshController(MaterialEditorUI host)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        /// <summary>
        /// Defers rebuilding the material list until the dropdown fade has completed.
        /// </summary>
        internal IEnumerator PopulateListCoroutine(GameObject go, object data, string filter = "")
        {
            var version = ScheduleDeferredPopulate(go, data, filter);
            yield return WaitForDeferredPopulate(version);
        }

        internal void SchedulePopulateList(GameObject go, object data, string filter)
        {
            ScheduleDeferredPopulate(
                go,
                data,
                ResolveFilterForCurrentTarget(go, data, filter));
        }

        private int ScheduleDeferredPopulate(GameObject go, object data, string filter)
        {
            CancelPresentationInvalidation();
            var version = _deferredRefresh.Schedule(go, data, filter);
            if (_deferredRefresh.TryStartWorker())
            {
                try
                {
                    _deferredRefreshCoroutine = _host.StartRefreshCoroutine(DeferredPopulateWorker());
                    if (_deferredRefreshCoroutine == null)
                        // No worker can finish this request or release its waiters.
                        _deferredRefresh.Cancel();
                }
                catch
                {
                    _deferredRefresh.Cancel();
                    throw;
                }
            }
            return version;
        }

        private IEnumerator WaitForDeferredPopulate(int version)
        {
            while (_deferredRefresh.IsCurrent(version))
                yield return null;
        }

        private IEnumerator DeferredPopulateWorker()
        {
            while (_deferredRefresh.HasPending)
            {
                yield return null;

                object target;
                object data;
                string filter;
                if (!_deferredRefresh.AdvanceFrame(
                        10,
                        out target,
                        out data,
                        out filter))
                    continue;

                _deferredRefresh.WorkerStopped();
                _deferredRefreshCoroutine = null;
                _host.PopulateListCoreForRefresh((GameObject)target, data, filter, null, false);
                yield break;
            }

            _deferredRefresh.WorkerStopped();
            _deferredRefreshCoroutine = null;
        }

        private void CancelDeferredPopulate()
        {
            _deferredRefresh.Cancel();
            if (_deferredRefreshCoroutine == null)
                return;
            _host.StopRefreshCoroutine(_deferredRefreshCoroutine);
            _deferredRefreshCoroutine = null;
        }

        internal string ResolveFilterForCurrentTarget(
            GameObject go,
            object data,
            string filter)
        {
            return ReferenceEquals(go, _host.CurrentGameObject)
                   && ReferenceEquals(data, _host.CurrentData)
                ? _host.RefreshFilter
                : filter;
        }

        internal void HandleFilterChanged(string filter)
        {
            _host.RefreshFilter = filter;

            // Search is newer than any pending shader refresh and therefore wins.
            // Do not cancel this coordinator here: repeated keystrokes must share
            // the same worker lease and accumulate into one end-of-frame batch.
            CancelDeferredPopulate();
            if (!MaterialEditorUI.Visible || _host.CurrentGameObject == null)
            {
                CancelPresentationInvalidation();
                return;
            }

            _presentationInvalidation.RequestSearch();
            TryStartPresentationInvalidationWorker();
        }

        internal void HandleConditionChanged(
            MaterialConditionInvalidationHandle handle)
        {
            var presentation = _host.RefreshPresentation;
            if (!MaterialEditorUI.Visible
                || _host.CurrentGameObject == null
                || presentation == null
                || !presentation.Owns(handle))
                return;

            // A valid condition edit is newer than a pending shader rebuild.
            // Search remains dominant inside the shared presentation batch.
            CancelDeferredPopulate();
            _presentationInvalidation.RequestCondition(handle);
            TryStartPresentationInvalidationWorker();
        }

        private void TryStartPresentationInvalidationWorker()
        {
            PresentationInvalidationWorkerLease lease;
            if (!_presentationInvalidation.TryAcquireWorker(out lease))
                return;

            _presentationInvalidationCoroutineLeaseId = lease.LeaseId;
            Coroutine coroutine;
            try
            {
                coroutine = _host.StartRefreshCoroutine(PresentationInvalidationWorker(lease));
            }
            catch (Exception ex)
            {
                RecoverPresentationInvalidationWorkerStart(lease, ex);
                return;
            }

            if (coroutine == null)
            {
                RecoverPresentationInvalidationWorkerStart(lease, null);
                return;
            }
            _presentationInvalidationCoroutine = coroutine;
        }

        private IEnumerator PresentationInvalidationWorker(
            PresentationInvalidationWorkerLease lease)
        {
            try
            {
                while (_presentationInvalidation.IsWorkerLeaseCurrent(lease))
                {
                    yield return PresentationEndOfFrame;

                    PresentationInvalidationBatch<
                        MaterialConditionInvalidationHandle> batch;
                    if (!_presentationInvalidation.TryBeginFlush(
                            lease,
                            Time.frameCount,
                            out batch))
                        continue;

                    if (!ExecutePresentationInvalidationBatch(lease, batch, false))
                        yield break;
                }
            }
            finally
            {
                _presentationInvalidation.AbandonWorker(lease);
                ClearPresentationInvalidationCoroutine(lease);
            }
        }

        private void RecoverPresentationInvalidationWorkerStart(
            PresentationInvalidationWorkerLease lease,
            Exception exception)
        {
            if (exception != null)
            {
                MaterialEditorPluginBase.Logger?.LogError(
                    "Could not start the Material Editor presentation refresh "
                    + "worker; draining its pending batch synchronously: "
                    + exception);
            }

            try
            {
                const int recoveryFlushLimit = 16;
                var flushCount = 0;
                while (_presentationInvalidation.IsWorkerLeaseCurrent(lease)
                       && flushCount < recoveryFlushLimit)
                {
                    PresentationInvalidationBatch<
                        MaterialConditionInvalidationHandle> batch;
                    if (!_presentationInvalidation.TryBeginRecoveryFlush(
                            lease,
                            out batch))
                        break;

                    var waitForNextBatch = ExecutePresentationInvalidationBatch(lease, batch, true);
                    flushCount++;
                    if (!waitForNextBatch)
                        break;
                }

                if (_presentationInvalidation.IsWorkerLeaseCurrent(lease))
                {
                    _presentationInvalidation.AbandonWorker(lease);
                    if (_presentationInvalidation.HasPending)
                    {
                        _presentationInvalidation.Cancel();
                        MaterialEditorPluginBase.Logger?.LogError(
                            "Material Editor presentation refresh recovery "
                            + "exceeded its bounded synchronous flush limit.");
                    }
                }
            }
            finally
            {
                ClearPresentationInvalidationCoroutine(lease);
            }
        }

        private bool ExecutePresentationInvalidationBatch(
            PresentationInvalidationWorkerLease lease,
            PresentationInvalidationBatch<MaterialConditionInvalidationHandle> batch,
            bool synchronousRecovery)
        {
            var hasMore = false;
            try
            {
                if (_presentationInvalidation.IsGenerationCurrent(batch.Generation))
                    ApplyPresentationInvalidationBatch(batch);
            }
            catch (Exception ex)
            {
                MaterialEditorPluginBase.Logger?.LogError(
                    (synchronousRecovery
                        ? "Exception while applying the synchronous Material Editor presentation refresh fallback: "
                        : "Exception while applying a coalesced Material Editor presentation refresh: ") + ex);
            }
            finally
            {
                hasMore = _presentationInvalidation.CompleteFlush(lease);
            }
            return hasMore;
        }

        private void ApplyPresentationInvalidationBatch(
            PresentationInvalidationBatch<
                MaterialConditionInvalidationHandle> batch)
        {
            if (!MaterialEditorUI.Visible || _host.CurrentGameObject == null)
                return;
            if ((batch.Reason & PresentationInvalidationReason.Search) != 0)
            {
                ApplySearchRefresh();
                return;
            }
            if ((batch.Reason & PresentationInvalidationReason.Conditions) != 0)
                ApplyConditionRefresh(batch.ConditionSources);
        }

        private void ApplyConditionRefresh(
            IEnumerable<MaterialConditionInvalidationHandle> handles)
        {
            var presentation = _host.RefreshPresentation;
            if (presentation == null)
                return;

            var grouped = new Dictionary<
                MaterialConditionDependencyGraph,
                List<MaterialConditionInvalidationHandle>>();
            foreach (var handle in handles)
            {
                if (!presentation.Owns(handle))
                    continue;
                List<MaterialConditionInvalidationHandle> graphHandles;
                if (!grouped.TryGetValue(handle.Graph, out graphHandles))
                {
                    graphHandles =
                        new List<MaterialConditionInvalidationHandle>();
                    grouped.Add(handle.Graph, graphHandles);
                }
                graphHandles.Add(handle);
            }
            if (grouped.Count == 0)
                return;

            var visibilityChanged = false;
            foreach (var entry in grouped)
            {
                var result = entry.Key.EvaluateHandles(entry.Value);
                visibilityChanged |= result.VisibilityChanged;
            }

            // Finish the complete batch before rebuilding so all coalesced
            // ShowIf sources are evaluated against the same presentation.
            if (visibilityChanged)
            {
                var topRowAnchor = _host.RefreshVirtualList.CaptureTopRowAnchor();
                _host.PopulateListCoreForRefresh(
                    _host.CurrentGameObject,
                    _host.CurrentData,
                    _host.RefreshFilter,
                    topRowAnchor,
                    false);
            }
        }

        private void ApplySearchRefresh()
        {
            _host.PopulateListCoreForRefresh(
                _host.CurrentGameObject,
                _host.CurrentData,
                _host.RefreshFilter,
                null,
                false);
        }

        private void CancelPresentationInvalidation()
        {
            _presentationInvalidation.Cancel();
            var coroutine = _presentationInvalidationCoroutine;
            _presentationInvalidationCoroutine = null;
            _presentationInvalidationCoroutineLeaseId = 0;
            if (coroutine != null)
                _host.StopRefreshCoroutine(coroutine);
        }

        private void ClearPresentationInvalidationCoroutine(
            PresentationInvalidationWorkerLease lease)
        {
            if (_presentationInvalidationCoroutineLeaseId != lease.LeaseId)
                return;
            _presentationInvalidationCoroutine = null;
            _presentationInvalidationCoroutineLeaseId = 0;
        }

        internal void CancelPendingRefreshes()
        {
            CancelDeferredPopulate();
            CancelPresentationInvalidation();
        }
    }
}
