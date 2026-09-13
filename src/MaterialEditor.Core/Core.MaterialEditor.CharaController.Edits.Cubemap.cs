using MaterialEditorAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;

namespace KK_Plugins.MaterialEditor
{
    public partial class MaterialEditorCharaController
    {
        /// <summary>
        /// Import and persist a native Cubemap from an equirectangular PNG or Radiance HDR file.
        /// </summary>
        public void SetMaterialCubemapFromFile(
            int slot,
            ObjectType objectType,
            Material material,
            string propertyName,
            string filePath,
            GameObject go)
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
                slot,
                objectType,
                material,
                propertyName,
                File.ReadAllBytes(filePath),
                null,
                go,
                true);
        }

        /// <summary>
        /// Import and persist a native Cubemap from encoded equirectangular PNG or Radiance HDR data.
        /// </summary>
        public void SetMaterialCubemap(
            int slot,
            ObjectType objectType,
            Material material,
            string propertyName,
            byte[] data,
            GameObject go)
        {
            MaterialEditRequestQueue.CancelTarget(go, material == null ? null : material.NameFormatted(), propertyName);
            SetMaterialCubemap(
                slot,
                objectType,
                material,
                propertyName,
                data,
                null,
                go,
                false);
        }

        /// <summary>
        /// Internal warm-cache persistence path. The opaque key belongs to the
        /// exact byte array and avoids a second main-thread SHA-256 pass.
        /// </summary>
        internal bool SetMaterialCubemap(
            int slot,
            ObjectType objectType,
            Material material,
            string propertyName,
            byte[] data,
            MaterialEditorCubemapContentKey contentKey,
            GameObject go)
        {
            return SetMaterialCubemap(
                slot,
                objectType,
                material,
                propertyName,
                data,
                contentKey,
                go,
                false);
        }

        private bool SetMaterialCubemap(
            int slot,
            ObjectType objectType,
            Material material,
            string propertyName,
            byte[] data,
            MaterialEditorCubemapContentKey contentKey,
            GameObject go,
            bool logNormalizationWarning)
        {
            if (material == null) return false;
            var existing = FindMaterialCubemapProperty(slot, objectType, material, propertyName);
            var result = MaterialCubemapImportTransaction.Execute(data, contentKey,
                go, material.NameFormatted(), propertyName, logNormalizationWarning,
                TextureDictionary, CubemapLeases, ApplyAndCommit, PurgeUnusedCubemapLeases);
            if (!result.Succeeded)
                MaterialEditorPluginBase.Logger?.LogWarning("Cubemap import: " + result.Stage + ": " + result.Diagnostic);
            return result.Succeeded;

            bool ApplyAndCommit(int texId)
            {
                var candidate = new MaterialCubemapProperty(objectType, GetCoordinateIndex(objectType), slot, material.NameFormatted(), propertyName, texId);
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


        /// <summary>
        /// Get the persisted native Cubemap value, or null when no override exists.
        /// </summary>
        public Cubemap GetMaterialCubemap(
            int slot,
            ObjectType objectType,
            Material material,
            string propertyName,
            GameObject go)
        {
            var cubemapProperty = FindMaterialCubemapProperty(
                slot,
                objectType,
                material,
                propertyName);
            if (cubemapProperty == null || !cubemapProperty.TexID.HasValue)
                return null;

            Cubemap cubemap;
            string error;
            return TryGetCubemap(cubemapProperty.TexID.Value, out cubemap, out error)
                ? cubemap
                : null;
        }

        /// <summary>
        /// Get whether the Cubemap property is still in its original state.
        /// </summary>
        public bool GetMaterialCubemapOriginal(
            int slot,
            ObjectType objectType,
            Material material,
            string propertyName,
            GameObject go)
        {
            return FindMaterialCubemapProperty(slot, objectType, material, propertyName)?.TexID == null;
        }

        /// <summary>
        /// Remove a persisted Cubemap override and restore the exact original value.
        /// </summary>
        public void RemoveMaterialCubemap(
            int slot,
            ObjectType objectType,
            Material material,
            string propertyName,
            GameObject go,
            bool setProperty = true)
        {
            MaterialEditRequestQueue.CancelTarget(go, material == null ? null : material.NameFormatted(), propertyName);
            var cubemapProperty = FindMaterialCubemapProperty(
                slot,
                objectType,
                material,
                propertyName);
            if (cubemapProperty == null)
                return;

            if (setProperty)
            {
                if (!cubemapProperty.RestoreCubemapOriginalSnapshot(go))
                    return;
            }

            cubemapProperty.ClearCubemapOriginalSnapshot();
            cubemapProperty.TexID = null;
            RemoveCubemapPropertyIfNull(cubemapProperty);
            PurgeUnusedTextures();
        }

        private bool SetCubemapWithProperty(GameObject go, MaterialCubemapProperty cubemapProperty)
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

            if (!cubemapProperty.SynchronizeCubemapOriginalSnapshot(go))
                return false;
            return SetCubemap(
                go,
                cubemapProperty.MaterialName,
                cubemapProperty.Property,
                cubemap);
        }

        private MaterialCubemapProperty FindMaterialCubemapProperty(
            int slot,
            ObjectType objectType,
            Material material,
            string propertyName)
        {
            var coordinateIndex = GetCoordinateIndex(objectType);
            var materialName = material.NameFormatted();
            return MaterialCubemapPropertyList.FirstOrDefault(x =>
                x.ObjectType == objectType
                && x.CoordinateIndex == coordinateIndex
                && x.Slot == slot
                && x.Property == propertyName
                && x.MaterialName == materialName);
        }

        private void RemoveCubemapPropertyIfNull(MaterialCubemapProperty cubemapProperty)
        {
            if (!cubemapProperty.NullCheck())
                return;
            MaterialCubemapPropertyList.Remove(cubemapProperty);
        }
    }
}
