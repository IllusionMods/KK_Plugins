using BepInEx;
using System;
using System.IO;
using UnityEngine;
using static MaterialEditorAPI.MaterialEditorPluginBase;

namespace MaterialEditorAPI
{
    /// <summary>
    /// Owns Material Editor asset selection, synchronous import, export, and file watching.
    /// </summary>
    internal sealed class MaterialEditorAssetWorkflow : IDisposable
    {
        private readonly MaterialEditorUI _host;
        private readonly MaterialEditService _editService;
        // Texture watching is process-wide so all UI hosts share one active watcher.
        private static FileSystemWatcher _textureWatcher;
        private static MaterialEditorAssetWorkflow _watcherOwner;
        private FileSystemWatcher _ownedWatcher;
        private bool _disposed;

        internal MaterialEditorAssetWorkflow(
            MaterialEditorUI host,
            MaterialEditService editService)
        {
            if (host == null)
                throw new ArgumentNullException("host");
            if (editService == null)
                throw new ArgumentNullException("editService");
            _host = host;
            _editService = editService;
        }

        internal void ImportTexture(TexturePropertyRowModel row, GameObject root,
            object data, Material material, string propertyName)
        {
#if !API
            string filter = KK_Plugins.ImageHelper.FileFilter;
#else
            string filter = "Images (*.png;.jpg)|*.png;*.jpg|All files|*.*";
#endif
            var target = new MaterialEditTarget(root, material, propertyName);
            KKAPI.Utilities.OpenFileDialog.Show(files =>
                ThreadingHelper.Instance.StartSyncInvoke(() =>
                {
                    if (!IsHostAlive() || !target.IsAlive || files == null
                        || files.Length == 0 || string.IsNullOrEmpty(files[0])) return;
                    ImportTexture(target, data, files[0]);
                    if (!IsHostAlive() || !target.IsAlive) return;
                    row.Changed = !_editService.GetMaterialTextureValueOriginal(data, material, propertyName, root);
                    row.Exists = MaterialPropertyAccess.GetTexture(material, MaterialPropertyIdCache.Get(propertyName)) != null;
                    row.RefreshState?.Invoke();
                    WatchTexture(target, data, files[0]);
                }), "Open image", ExportPath, filter, ".png");
        }

        private void ImportTexture(MaterialEditTarget target, object data, string path)
        {
            try
            {
                _editService.SetMaterialTexture(data, target.Material, target.Property, path, target.Root);
            }
            catch (Exception ex)
            {
                MaterialEditorPluginBase.Logger?.LogWarning("Texture import " + target.Property + ": " + ex);
            }
        }

        internal void ImportCubemap(CubemapPropertyRowModel row, GameObject root,
            object data, Material material, string propertyName)
        {
            const string filter = "Cubemap panoramas (*.png;*.hdr)|*.png;*.hdr|All files|*.*";
            var target = new MaterialEditTarget(root, material, propertyName);
            KKAPI.Utilities.OpenFileDialog.Show(files =>
                ThreadingHelper.Instance.StartSyncInvoke(() =>
                {
                    if (!IsHostAlive() || !target.IsAlive || files == null
                        || files.Length == 0 || string.IsNullOrEmpty(files[0])) return;
                    try
                    {
                        if (!File.Exists(files[0]))
                        {
                            MaterialEditorPluginBase.Logger?.LogWarning("Cubemap source not found or inaccessible: " + files[0]);
                            return;
                        }
                        _editService.SetMaterialCubemap(data, material, propertyName, files[0], root);
                    }
                    catch (Exception ex)
                    {
                        MaterialEditorPluginBase.Logger?.LogWarning("Cubemap import " + propertyName + ": " + ex);
                    }
                    if (!IsHostAlive() || !target.IsAlive) return;
                    row.Changed = !_editService.GetMaterialCubemapValueOriginal(data, material, propertyName, root);
                    row.Exists = MaterialPropertyAccess.GetTexture(material, MaterialPropertyIdCache.Get(propertyName)) is Cubemap;
                    row.RefreshState?.Invoke();
                }), "Open Cubemap source", ExportPath, filter, ".png");
        }

        private void WatchTexture(MaterialEditTarget target, object data, string path)
        {
            DisposeTextureWatcher();
            if (!WatchTexChanges.Value) return;
            var directory = Path.GetDirectoryName(path);
            if (directory == null) return;
            var watcher = new FileSystemWatcher(directory, Path.GetFileName(path));
            _textureWatcher = _ownedWatcher = watcher;
            _watcherOwner = this;
            int notificationPending = 0;
            watcher.Changed += (sender, args) =>
            {
                // No Unity/config access on the watcher thread; at most one marshalled notification.
                if (System.Threading.Interlocked.Exchange(ref notificationPending, 1) != 0) return;
                ThreadingHelper.Instance.StartSyncInvoke(() =>
                {
                    System.Threading.Interlocked.Exchange(ref notificationPending, 0);
                    if (IsHostAlive() && ReferenceEquals(_textureWatcher, watcher)
                        && target.IsAlive && WatchTexChanges.Value && File.Exists(path))
                        ImportTexture(target, data, path);
                });
            };
            Action close = () => ThreadingHelper.Instance.StartSyncInvoke(() =>
            {
                if (ReferenceEquals(_textureWatcher, watcher)) DisposeTextureWatcher();
            });
            watcher.Deleted += (sender, args) => close();
            watcher.Error += (sender, args) => close();
            watcher.EnableRaisingEvents = true;
        }

        internal void ExportTexture(Material material, string propertyName)
        {
            var texture = MaterialPropertyAccess.GetTexture(
                material,
                MaterialPropertyIdCache.Get(propertyName));
            if (texture == null)
                return;
            var materialName = SanitizeMaterialName(material);
            string filename = Path.Combine(
                ExportPath,
                $"_Export_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}_{materialName}_{propertyName}.png");
            Instance.ConvertNormalMap(
                ref texture,
                propertyName,
                ConvertNormalmapsOnExport.Value);
            SaveTex(texture, filename);
            MaterialEditorPluginBase.Logger.LogInfo($"Exported {filename}");
            Utilities.OpenFileInExplorer(filename);
        }

        internal void ExportCubemap(Material material, string propertyName)
        {
            var cubemap = MaterialPropertyAccess.GetTexture(
                material,
                MaterialPropertyIdCache.Get(propertyName)) as Cubemap;
            if (cubemap == null)
                return;

            var materialName = SanitizeMaterialName(material);
            string filename = Path.Combine(
                ExportPath,
                $"_Export_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}_{materialName}_{propertyName}.png");
            byte[] pngData;
            string error;
            if (!MaterialEditorCubemapConversion.TryExport(
                    cubemap,
                    out pngData,
                    out error))
            {
                MaterialEditorPluginBase.Logger.LogError(error);
                MaterialEditorPluginBase.Logger.LogMessage(error);
                return;
            }

            File.WriteAllBytes(filename, pngData);
            MaterialEditorPluginBase.Logger.LogInfo($"Exported {filename}");
            Utilities.OpenFileInExplorer(filename);
        }

        internal void ExportTextureOriginal(
            Material material,
            string propertyName,
            string extension,
            byte[] textureData)
        {
            var materialName = SanitizeMaterialName(material);
            string filename = Path.Combine(
                ExportPath,
                $"_Export_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}_{materialName}_{propertyName}.{extension}");
            File.WriteAllBytes(filename, textureData);
            MaterialEditorPluginBase.Logger.LogInfo($"Exported {filename}");
            Utilities.OpenFileInExplorer(filename);
        }

        internal static void DisposeTextureWatcher()
        {
            var watcher = _textureWatcher;
            _textureWatcher = null;
            if (_watcherOwner != null) _watcherOwner._ownedWatcher = null;
            _watcherOwner = null;
            watcher?.Dispose();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_ownedWatcher != null && ReferenceEquals(_textureWatcher, _ownedWatcher))
                DisposeTextureWatcher();
        }

        private bool IsHostAlive() => !_disposed && _host != null;

        // File dialogs and watcher notifications can outlive their original target.
        private sealed class MaterialEditTarget
        {
            internal readonly GameObject Root;
            internal readonly Material Material;
            internal readonly string Property;
            private readonly string _materialName;
            private readonly Shader _shader;

            internal MaterialEditTarget(GameObject root, Material material, string property)
            {
                Root = root;
                Material = material;
                Property = property;
                _materialName = material == null ? null : material.NameFormatted();
                _shader = material == null ? null : material.shader;
            }

            internal bool IsAlive => Root != null && Material != null
                && Material.shader == _shader && Material.NameFormatted() == _materialName;
        }

        private static string SanitizeMaterialName(Material material)
        {
            var materialName = material.NameFormatted();
            return string.Concat(
                materialName.Split(Path.GetInvalidFileNameChars())).Trim();
        }
    }
}
