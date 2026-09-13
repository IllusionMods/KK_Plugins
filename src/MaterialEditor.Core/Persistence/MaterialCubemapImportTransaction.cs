using System;
using System.Collections.Generic;
using MaterialEditorAPI;
using UnityEngine;

namespace KK_Plugins.MaterialEditor
{
    /// <summary>Shared resource transaction; controller-specific records stay with their controller.</summary>
    internal static class MaterialCubemapImportTransaction
    {
        internal static MaterialEditResult Execute(byte[] data, MaterialEditorCubemapContentKey key,
            GameObject root, string materialName, string property, bool logNormalization,
            Dictionary<int, TextureContainer> textures, MaterialEditorCubemapLeaseStore leases,
            Func<int, bool> applyAndCommit, Action purgeLeases)
        {
            if (data == null || root == null)
                return new MaterialEditResult(MaterialEditStatus.Failed, "Validate");

            MaterialEditorCubemapLease lease = null;
            MaterialTextureSnapshot runtime = null;
            var created = false;
            var committed = false;
            var texId = 0;
            var stage = "Acquire";
            try
            {
                string warning;
                string error;
                var acquired = key == null
                    ? MaterialEditorCubemapCache.TryAcquire(data, out lease, out warning, out error)
                    : MaterialEditorCubemapCache.TryAcquire(data, key, out lease, out warning, out error);
                if (!acquired) return new MaterialEditResult(MaterialEditStatus.Failed, stage, error);
                if (logNormalization && !string.IsNullOrEmpty(warning))
                    MaterialEditorPluginBase.Logger?.LogWarning(warning);

                stage = "Store";
                var count = textures.Count;
                texId = TextureSaveHandler.GetOrAddTexture(textures, data);
                created = textures.Count > count;
                leases.Store(texId, lease);
                lease = null;

                stage = "Snapshot";
                runtime = new MaterialTextureSnapshot(root, materialName, property);
                stage = "Apply/commit";
                // The callback leaves the existing record unchanged on failure and
                // publishes the candidate only after successful runtime application.
                if (!applyAndCommit(texId))
                    return new MaterialEditResult(MaterialEditStatus.Failed, stage, "Previous override preserved.");
                committed = true;
                return new MaterialEditResult(MaterialEditStatus.Succeeded, stage);
            }
            catch (Exception ex) { return new MaterialEditResult(MaterialEditStatus.Failed, stage, ex.Message); }
            finally
            {
                if (!committed)
                {
                    runtime?.Restore();
                    if (created) Cleanup(() =>
                    {
                        leases.Release(texId);
                        TextureContainer container;
                        if (!textures.TryGetValue(texId, out container)) return;
                        try { container?.Dispose(); }
                        finally { textures.Remove(texId); }
                    });
                }
                if (lease != null) Cleanup(lease.Dispose);
                Cleanup(purgeLeases);
            }
        }

        private static void Cleanup(Action action)
        {
            try { action(); }
            catch (Exception ex) { MaterialEditorPluginBase.Logger?.LogWarning("Cubemap cleanup: " + ex.Message); }
        }
    }
}
