using System;
using System.Collections.Generic;
using UnityEngine;

namespace MaterialEditorAPI
{
    internal sealed class MaterialEditorCubemapLease : IDisposable
    {
        private readonly string _key;
        private bool _disposed;

        internal MaterialEditorCubemapLease(string key, Cubemap cubemap)
        {
            _key = key;
            Cubemap = cubemap;
        }

        internal Cubemap Cubemap { get; private set; }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            MaterialEditorCubemapCache.Release(_key);
            Cubemap = null;
        }
    }

    internal sealed class MaterialEditorCubemapLeaseStore
    {
        private readonly Dictionary<int, MaterialEditorCubemapLease> _leases =
            new Dictionary<int, MaterialEditorCubemapLease>();

        internal int Count
        {
            get { return _leases.Count; }
        }

        internal bool TryAcquire(
            int textureId,
            byte[] encodedData,
            out Cubemap cubemap,
            out string error)
        {
            cubemap = null;
            error = null;

            MaterialEditorCubemapLease existing;
            if (_leases.TryGetValue(textureId, out existing))
            {
                if (existing.Cubemap != null)
                {
                    cubemap = existing.Cubemap;
                    return true;
                }

                // Unity's destroyed-object null semantics can invalidate a
                // retained Cubemap without disposing its lease. Remove that
                // stale owner before replacing it so Store cannot discard the
                // newly acquired live lease.
                _leases.Remove(textureId);
                existing.Dispose();
            }

            MaterialEditorCubemapLease acquired;
            if (!MaterialEditorCubemapCache.TryAcquire(
                    encodedData,
                    out acquired,
                    out error))
                return false;

            Store(textureId, acquired);
            cubemap = acquired.Cubemap;
            return cubemap != null;
        }

        internal void Store(int textureId, MaterialEditorCubemapLease lease)
        {
            if (lease == null)
                return;

            MaterialEditorCubemapLease existing;
            if (_leases.TryGetValue(textureId, out existing))
            {
                lease.Dispose();
                return;
            }
            _leases.Add(textureId, lease);
        }

        internal void Release(int textureId)
        {
            MaterialEditorCubemapLease lease;
            if (!_leases.TryGetValue(textureId, out lease))
                return;
            _leases.Remove(textureId);
            lease.Dispose();
        }

        internal void Purge(IEnumerable<int> usedTextureIds)
        {
            if (_leases.Count == 0)
                return;

            var used = usedTextureIds == null
                ? new HashSet<int>()
                : new HashSet<int>(usedTextureIds);
            var unused = new List<int>();
            foreach (var textureId in _leases.Keys)
                if (!used.Contains(textureId))
                    unused.Add(textureId);
            for (var index = 0; index < unused.Count; index++)
                Release(unused[index]);
        }

        internal void DisposeAll()
        {
            if (_leases.Count == 0)
                return;

            var leases = new List<MaterialEditorCubemapLease>(_leases.Values);
            _leases.Clear();
            for (var index = 0; index < leases.Count; index++)
                leases[index].Dispose();
        }
    }

    /// <summary>
    /// Incremental cache-miss acquisition. The caller owns this operation and
    /// must dispose it. Once complete, TakeLease transfers the published cache
    /// reference to the caller.
    /// </summary>
    internal sealed class MaterialEditorCubemapAcquireOperation : IDisposable
    {
        private readonly string _key;
        private MaterialEditorCubemapImportOperation _import;
        private MaterialEditorCubemapLease _lease;
        private MaterialEditorCubemapMemoryReservation _memoryReservation;
        private int _completedRows;
        private readonly int _totalRows;
        private bool _disposed;

        internal MaterialEditorCubemapAcquireOperation(
            string key,
            MaterialEditorCubemapImportOperation import,
            MaterialEditorCubemapMemoryReservation memoryReservation)
        {
            _key = key;
            _import = import;
            _memoryReservation = memoryReservation;
            _totalRows = import == null ? 0 : import.TotalRows;
        }

        internal bool IsComplete { get; private set; }

        internal int CompletedRows
        {
            get { return _import == null ? _completedRows : _import.CompletedRows; }
        }

        internal int TotalRows
        {
            get { return _totalRows; }
        }

        internal bool ProcessRows(int maxRows, out string error)
        {
            error = null;
            if (_disposed)
            {
                error = "The Cubemap cache acquisition has been disposed.";
                return false;
            }
            if (IsComplete)
                return true;
            if (_import == null || !_import.ProcessRows(maxRows, out error))
                return false;
            _completedRows = _import.CompletedRows;
            if (!_import.IsComplete)
                return true;

            var converted = _import.TakeResult();
            _import.Dispose();
            _import = null;
            if (converted == null)
            {
                error = "Cubemap conversion completed without a cache candidate.";
                return false;
            }

            _lease = MaterialEditorCubemapCache.PublishConverted(_key, converted);
            ReleaseMemoryReservation();
            IsComplete = true;
            return true;
        }

        internal MaterialEditorCubemapLease TakeLease()
        {
            if (_disposed || !IsComplete)
                return null;
            var lease = _lease;
            _lease = null;
            return lease;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            if (_import != null)
                _import.Dispose();
            _import = null;
            if (_lease != null)
                _lease.Dispose();
            _lease = null;
            ReleaseMemoryReservation();
        }

        private void ReleaseMemoryReservation()
        {
            if (_memoryReservation == null)
                return;
            _memoryReservation.Dispose();
            _memoryReservation = null;
        }
    }

    internal static class MaterialEditorCubemapCache
    {
        private sealed class Entry
        {
            internal Cubemap Cubemap;
            internal int References;
        }

        private static readonly object Sync = new object();
        private static readonly Dictionary<string, Entry> Entries =
            new Dictionary<string, Entry>(StringComparer.Ordinal);

        internal static bool TryAcquire(
            byte[] pngData,
            out MaterialEditorCubemapLease lease,
            out string error)
        {
            string warning;
            return TryAcquire(pngData, out lease, out warning, out error);
        }

        /// <summary>
        /// Acquires a cached Cubemap. This method creates and destroys Unity
        /// objects and therefore must be invoked on Unity's main thread.
        /// Expensive conversion is deliberately performed outside the global
        /// cache lock; the second lookup safely deduplicates concurrent misses.
        /// </summary>
        internal static bool TryAcquire(
            byte[] pngData,
            out MaterialEditorCubemapLease lease,
            out string warning,
            out string error)
        {
            MaterialEditorCubemapAcquireOperation operation;
            if (!TryBeginAcquire(
                    pngData,
                    out lease,
                    out operation,
                    out warning,
                    out error))
                return false;
            return CompleteAcquire(lease, operation, out lease, out error);
        }

        private static bool CompleteAcquire(
            MaterialEditorCubemapLease immediateLease,
            MaterialEditorCubemapAcquireOperation operation,
            out MaterialEditorCubemapLease lease,
            out string error)
        {
            lease = immediateLease;
            error = null;
            if (lease != null)
                return true;
            if (operation == null)
            {
                error = "Cubemap cache acquisition returned neither a lease nor an operation.";
                return false;
            }

            using (operation)
            {
                if (!operation.ProcessRows(operation.TotalRows, out error))
                    return false;
                lease = operation.TakeLease();
                if (lease != null)
                    return true;
                error = "Cubemap cache acquisition completed without a lease.";
                return false;
            }
        }

        /// <summary>
        /// Prepares a cache acquisition. CompleteAcquire finishes any conversion synchronously.
        /// </summary>
        private static bool TryBeginAcquire(
            byte[] pngData,
            out MaterialEditorCubemapLease lease,
            out MaterialEditorCubemapAcquireOperation operation,
            out string warning,
            out string error)
        {
            lease = null;
            operation = null;
            warning = null;
            error = null;
            if (pngData != null
                && !MaterialEditorCubemapProjection.TryValidateSourceFileLength(
                    pngData.LongLength,
                    out error))
                return false;

            MaterialEditorCubemapContentKey contentKey;
            if (!MaterialEditorCubemapContentKey.TryCompute(
                    pngData,
                    out contentKey,
                    out error))
            {
                return false;
            }
            return TryBeginAcquire(
                pngData,
                contentKey,
                out lease,
                out operation,
                out warning,
                out error);
        }

        /// <summary>
        /// Begins acquisition using the key computed for the exact byte-array instance.
        /// </summary>
        private static bool TryBeginAcquire(
            byte[] pngData,
            MaterialEditorCubemapContentKey contentKey,
            out MaterialEditorCubemapLease lease,
            out MaterialEditorCubemapAcquireOperation operation,
            out string warning,
            out string error)
        {
            lease = null;
            operation = null;
            warning = null;
            error = null;
            if (contentKey == null || !contentKey.Matches(pngData))
            {
                error = "The Cubemap content key does not belong to the supplied data.";
                return false;
            }
            MaterialEditorCubemapSourceInfo sourceInfo;
            if (!MaterialEditorCubemapSourceParser.TryInspect(
                    pngData,
                    out sourceInfo,
                    out warning,
                    out error))
                return false;

            int normalizedWidth;
            int normalizedHeight;
            MaterialEditorCubemapProjection.GetNormalizedEquirectangularSize(
                sourceInfo.Width,
                sourceInfo.Height,
                out normalizedWidth,
                out normalizedHeight);
            var faceSize = Math.Min(normalizedWidth / 4, normalizedHeight / 2);
            long estimatedPeakBytes;
            if (!MaterialEditorCubemapMemoryBudget.TryValidateImport(
                    pngData.LongLength,
                    sourceInfo.Width,
                    sourceInfo.Height,
                    faceSize,
                    sourceInfo.IsHighDynamicRange,
                    out estimatedPeakBytes,
                    out error))
                return false;

            var key = contentKey.Value;

            lock (Sync)
            {
                Entry existing;
                if (Entries.TryGetValue(key, out existing))
                {
                    existing.References++;
                    lease = new MaterialEditorCubemapLease(key, existing.Cubemap);
                    return true;
                }
            }

            MaterialEditorCubemapMemoryReservation memoryReservation;
            if (!MaterialEditorCubemapMemoryBudget.TryReserveConversion(
                    estimatedPeakBytes,
                    out memoryReservation,
                    out error))
                return false;

            try
            {
                MaterialEditorCubemapImportOperation import;
                if (!MaterialEditorCubemapImportOperation.TryBegin(
                        pngData,
                        sourceInfo,
                        out import,
                        out error))
                    return false;
                operation = new MaterialEditorCubemapAcquireOperation(
                    key,
                    import,
                    memoryReservation);
                memoryReservation = null;
                return true;
            }
            finally
            {
                if (memoryReservation != null)
                    memoryReservation.Dispose();
            }
        }

        internal static MaterialEditorCubemapLease PublishConverted(
            string key,
            Cubemap converted)
        {
            Cubemap duplicate = null;
            MaterialEditorCubemapLease lease;
            lock (Sync)
            {
                Entry existing;
                if (Entries.TryGetValue(key, out existing))
                {
                    existing.References++;
                    lease = new MaterialEditorCubemapLease(key, existing.Cubemap);
                    duplicate = converted;
                }
                else
                {
                    Entries.Add(
                        key,
                        new Entry
                        {
                            Cubemap = converted,
                            References = 1
                        });
                    lease = new MaterialEditorCubemapLease(key, converted);
                }
            }

            // The acquisition operation is main-thread-only. A concurrent loser
            // is destroyed only after leaving the cache lock.
            if (duplicate != null)
                UnityEngine.Object.Destroy(duplicate);
            return lease;
        }

        internal static void Release(string key)
        {
            if (string.IsNullOrEmpty(key))
                return;

            Cubemap destroy = null;
            lock (Sync)
            {
                Entry entry;
                if (!Entries.TryGetValue(key, out entry))
                    return;
                entry.References--;
                if (entry.References > 0)
                    return;
                Entries.Remove(key);
                destroy = entry.Cubemap;
            }

            // Lease stores are owned by Unity-facing controllers, so release is
            // main-thread-confined just like acquire.
            if (destroy != null)
                UnityEngine.Object.Destroy(destroy);
        }
    }
}
