using MaterialEditorAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;

namespace KK_Plugins.MaterialEditor
{
    public partial class SceneController
    {
        /// <summary>
        /// Import and persist a native Cubemap from an equirectangular PNG or Radiance HDR file.
        /// </summary>
        public void SetMaterialCubemapFromFile(
            int id,
            Material material,
            string propertyName,
            string filePath)
        {
            if (!File.Exists(filePath))
                return;

            string fileError;
            if (!MaterialEditorCubemapProjection.TryValidateSourceFileLength(
                    new FileInfo(filePath).Length,
                    out fileError))
            {
                MaterialEditorPlugin.Logger.LogMessage(fileError);
                return;
            }

            SetMaterialCubemap(
                id,
                material,
                propertyName,
                File.ReadAllBytes(filePath),
                null,
                true);
        }

        /// <summary>
        /// Import and persist a native Cubemap from encoded equirectangular PNG or Radiance HDR data.
        /// </summary>
        public void SetMaterialCubemap(
            int id,
            Material material,
            string propertyName,
            byte[] data)
        {
            MaterialEditRequestQueue.CancelTarget(GetObjectByID(id), material == null ? null : material.NameFormatted(), propertyName);
            SetMaterialCubemap(id, material, propertyName, data, null, false);
        }

        internal bool SetMaterialCubemap(
            int id,
            Material material,
            string propertyName,
            byte[] data,
            MaterialEditorCubemapContentKey contentKey)
        {
            return SetMaterialCubemap(
                id,
                material,
                propertyName,
                data,
                contentKey,
                false);
        }

        private bool SetMaterialCubemap(
            int id,
            Material material,
            string propertyName,
            byte[] data,
            MaterialEditorCubemapContentKey contentKey,
            bool logNormalizationWarning)
        {
            if (material == null) return false;
            var go = GetObjectByID(id);
            var existing = MaterialCubemapPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted());
            var result = MaterialCubemapImportTransaction.Execute(data, contentKey,
                go, material.NameFormatted(), propertyName, logNormalizationWarning,
                TextureDictionary, CubemapLeases, ApplyAndCommit, PurgeUnusedCubemapLeases);
            if (!result.Succeeded)
                MaterialEditorPluginBase.Logger?.LogWarning("Cubemap import: " + result.Stage + ": " + result.Diagnostic);
            return result.Succeeded;

            bool ApplyAndCommit(int texId)
            {
                var candidate = new MaterialCubemapProperty(id, material.NameFormatted(), propertyName, texId);
                var committed = false;
                try
                {
                    if (existing != null)
                        candidate.CubemapOriginalState.RestoreCheckpoint(
                            existing.CubemapOriginalState.CaptureCheckpoint(), false);
                    if (!SetCubemapWithProperty(go, candidate))
                        return false;

                    if (existing == null)
                        MaterialCubemapPropertyList.Add(candidate);
                    else
                    {
                        // Finish all potentially throwing preparation before updating the
                        // existing record. Checkpoint restoration only assigns state fields.
                        var state = candidate.CubemapOriginalState.CaptureCheckpoint();
                        existing.CubemapOriginalState.RestoreCheckpoint(state, false);
                        existing.TexID = texId;
                    }
                    committed = true;
                    return true;
                }
                finally
                {
                    if (!committed)
                        candidate.CubemapOriginalState.Clear();
                }
            }
        }


        private bool SetCubemapWithProperty(
            GameObject gameObject,
            MaterialCubemapProperty cubemapProperty)
        {
            if (cubemapProperty == null
                || !cubemapProperty.TexID.HasValue
                || cubemapProperty.NullCheck())
                return false;

            Cubemap cubemap;
            string error;
            if (!TryGetCubemap(cubemapProperty.TexID.Value, out cubemap, out error))
            {
                MaterialEditorPluginBase.Logger.LogWarning(error);
                return false;
            }

            if (!cubemapProperty.SynchronizeCubemapOriginalSnapshot(gameObject))
                return false;
            return SetCubemap(
                gameObject,
                cubemapProperty.MaterialName,
                cubemapProperty.Property,
                cubemap);
        }

        public Cubemap GetMaterialCubemap(
            int id,
            Material material,
            string propertyName)
        {
            var cubemapProperty = MaterialCubemapPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted());
            if (cubemapProperty == null || !cubemapProperty.TexID.HasValue)
                return null;

            Cubemap cubemap;
            string error;
            return TryGetCubemap(cubemapProperty.TexID.Value, out cubemap, out error)
                ? cubemap
                : null;
        }

        public bool GetMaterialCubemapOriginal(
            int id,
            Material material,
            string propertyName)
        {
            return MaterialCubemapPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.TexID == null;
        }

        public void RemoveMaterialCubemap(
            int id,
            Material material,
            string propertyName)
        {
            MaterialEditRequestQueue.CancelTarget(GetObjectByID(id), material == null ? null : material.NameFormatted(), propertyName);
            var cubemapProperty = MaterialCubemapPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted());
            if (cubemapProperty == null)
                return;

            var gameObject = GetObjectByID(id);
            if (!cubemapProperty.RestoreCubemapOriginalSnapshot(gameObject))
                return;
            cubemapProperty.ClearCubemapOriginalSnapshot();
            cubemapProperty.TexID = null;
            if (cubemapProperty.NullCheck())
                MaterialCubemapPropertyList.Remove(cubemapProperty);
            PurgeUnusedTextures();
        }
    }
}
