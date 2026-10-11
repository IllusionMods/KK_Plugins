using BepInEx;
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using static MaterialEditorAPI.MaterialEditorPluginBase;

namespace MaterialEditorAPI
{
    /// <summary>
    /// Owns Material Editor asset selection, import, export, file watching, and
    /// Cubemap import coordination. Unity work remains hosted by the owning UI.
    /// </summary>
    internal sealed class MaterialEditorAssetWorkflow : IDisposable
    {
        private readonly MaterialEditorUI _host;
        private readonly MaterialEditService _editService;
        // Texture watching is process-wide so all UI hosts share one active watcher.
        private static FileSystemWatcher _textureWatcher;
        private static MaterialEditorAssetWorkflow _watcherOwner;
        private readonly MaterialEditRequestQueue _imports = new MaterialEditRequestQueue();
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
            _host.StartCoroutine(PumpImports());
        }

        private IEnumerator PumpImports()
        {
            while (!_disposed && _host != null)
            {
                _imports.Pump();
                yield return null;
            }
            Dispose();
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
                    var path = files[0];
                    _imports.Enqueue(target, IsHostAlive, done =>
                    {
                        var runner = _host.gameObject.AddComponent<MaterialEditorCubemapImportRunner>();
                        try
                        {
                            runner.Begin(path, () => IsHostAlive() && target.IsAlive,
                                (bytes, key, lease) =>
                                {
                                    if (lease == null) return MaterialEditResult.FromApplied(false);
                                    if (_editService.SupportsMaterialCubemapDataImport(data))
                                        return MaterialEditResult.FromApplied(_editService.SetMaterialCubemap(
                                            data, material, propertyName, bytes, key, root));
                                    _editService.SetMaterialCubemap(data, material, propertyName, path, root);
                                    return new MaterialEditResult(MaterialEditStatus.Unverified, "Legacy void API");
                                },
                                message => MaterialEditorPluginBase.Logger?.LogInfo(message),
                                message => MaterialEditorPluginBase.Logger?.LogWarning(message),
                                message => MaterialEditorPluginBase.Logger?.LogError(message),
                                done);
                        }
                        catch
                        {
                            UnityEngine.Object.Destroy(runner);
                            throw;
                        }
                        return runner.Cancel;
                    }, result =>
                    {
                        if (!IsHostAlive() || !target.IsAlive) return;
                        row.Changed = !_editService.GetMaterialCubemapValueOriginal(data, material, propertyName, root);
                        row.Exists = MaterialPropertyAccess.GetTexture(material, MaterialPropertyIdCache.Get(propertyName)) is Cubemap;
                        row.RefreshState?.Invoke();
                        ReportImportResult(result, propertyName);
                    });
                }), "Open Cubemap source", ExportPath, filter, ".png");
        }

        private static void ReportImportResult(MaterialEditResult result, string property)
        {
            if (result.Status == MaterialEditStatus.Failed || result.Status == MaterialEditStatus.Unverified)
                MaterialEditorPluginBase.Logger?.LogWarning("Import " + property + ": "
                    + result.Status + " (" + result.Stage + ") " + result.Diagnostic);
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
            _imports.Dispose();
            if (_ownedWatcher != null && ReferenceEquals(_textureWatcher, _ownedWatcher))
                DisposeTextureWatcher();
        }

        private bool IsHostAlive() => !_disposed && _host != null;

        private static string SanitizeMaterialName(Material material)
        {
            var materialName = material.NameFormatted();
            return string.Concat(
                materialName.Split(Path.GetInvalidFileNameChars())).Trim();
        }
    }
}
