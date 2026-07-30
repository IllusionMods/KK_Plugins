using System;
using System.Collections.Generic;

namespace KK_Plugins.MaterialEditor
{
    internal sealed class SiruPendingWriteBuffer<TPart>
    {
        private readonly Dictionary<TPart, byte> _writes =
            new Dictionary<TPart, byte>();

        internal int Count => _writes.Count;

        internal void Set(TPart part, byte value)
        {
            _writes[part] = value;
        }

        internal Dictionary<TPart, byte> CollectReadyWrites(
            Func<TPart, bool> canApply)
        {
            var readyWrites = new Dictionary<TPart, byte>();
            var writesToDiscard = new List<TPart>();
            foreach (KeyValuePair<TPart, byte> pendingWrite in _writes)
            {
                if (canApply(pendingWrite.Key))
                    readyWrites.Add(pendingWrite.Key, pendingWrite.Value);
                else
                    writesToDiscard.Add(pendingWrite.Key);
            }

            for (int i = 0; i < writesToDiscard.Count; i++)
                _writes.Remove(writesToDiscard[i]);

            return readyWrites;
        }

        internal void Complete(Dictionary<TPart, byte> appliedWrites)
        {
            foreach (KeyValuePair<TPart, byte> appliedWrite in appliedWrites)
            {
                byte pendingValue;
                if (_writes.TryGetValue(appliedWrite.Key, out pendingValue)
                    && pendingValue == appliedWrite.Value)
                {
                    _writes.Remove(appliedWrite.Key);
                }
            }
        }

        internal void Clear()
        {
            _writes.Clear();
        }
    }

    internal static class SiruSyncPolicy
    {
        internal static bool CanApplyPendingWrite(
            bool highPoly,
            bool primaryMaterialAvailable)
        {
            return highPoly && primaryMaterialAvailable;
        }

        internal static bool MaterialNameMatchesFamily(
            string materialName,
            IEnumerable<string> familyNames,
            string materialCopyPostfix)
        {
            if (string.IsNullOrEmpty(materialName) || familyNames == null)
                return false;

            foreach (string familyName in familyNames)
            {
                if (string.IsNullOrEmpty(familyName))
                    continue;

                if (string.Equals(materialName, familyName, StringComparison.Ordinal))
                    return true;

                if (!string.IsNullOrEmpty(materialCopyPostfix)
                    && materialName.StartsWith(
                        familyName + materialCopyPostfix,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
