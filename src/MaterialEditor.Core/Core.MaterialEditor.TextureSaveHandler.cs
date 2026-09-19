using System.Collections.Generic;
using ExtensibleSaveFormat;
using MaterialEditorAPI;
using MessagePack;
using System.Linq;
using System.IO;

namespace KK_Plugins.MaterialEditor
{
    /// <summary>
    /// Reads local and deduplicated version-2 texture data.
    /// Writes bundled version-1 or local/deduplicated version-2 data according to the save mode.
    /// </summary>
    internal sealed class TextureSaveHandler
    {
        internal static TextureSaveHandler Instance;
        internal string LocalTexturePath { get; set; }
        internal readonly string LocalTexPrefix;
        internal readonly string LocalTexSavePrefix;
        internal readonly string DedupedTexSavePrefix;
        internal readonly string DedupedTexSavePostfix;
        internal readonly string LocalTexUnusedFolder;
        // Older game-specific KKAPI builds do not expose local saving. Keep
        // their bundled behavior without raising the minimum API dependency.
        private static readonly System.Reflection.PropertyInfo CardSaveType =
            typeof(KKAPI.Maker.MakerAPI).Assembly.GetType("KKAPI.Maker.CharaLocalTextures")?.GetProperty("SaveType");
#if !EC
        private static readonly System.Reflection.PropertyInfo SceneSaveType =
            typeof(KKAPI.Maker.MakerAPI).Assembly.GetType("KKAPI.Studio.SceneLocalTextures")?.GetProperty("SaveType");
        private Dictionary<string, byte[]> DedupedTextureData = null;
#endif

        public TextureSaveHandler(
            string localTexturePath,
            string localTexPrefix = "ME_LocalTex_",
            string localTexSavePrefix = "LOCAL_",
            string dedupedTexSavePrefix = "DEDUPED_",
            string dedupedTexSavePostfix = "_DATA",
            string localTexUnusedFolder = "_Unused"
        )
        {
            LocalTexturePath = localTexturePath;
            LocalTexPrefix = localTexPrefix;
            LocalTexSavePrefix = localTexSavePrefix;
            DedupedTexSavePrefix = dedupedTexSavePrefix;
            DedupedTexSavePostfix = dedupedTexSavePostfix;
            LocalTexUnusedFolder = localTexUnusedFolder;
            Instance = this;
            // Reading the setting activates KKAPI's local-save controls.
            CardSaveType?.GetValue(null, null);
#if !EC
            SceneSaveType?.GetValue(null, null);
#endif
        }

        private static object DefaultData()
        {
            return new Dictionary<int, TextureContainer>();
        }

        internal static void DisposeTextureContainers(
            IDictionary<int, TextureContainer> textures)
        {
            if (textures == null)
                return;

            var disposed = new HashSet<TextureContainer>();
            foreach (var texture in textures.Values)
                if (texture != null && disposed.Add(texture))
                    texture.Dispose();
            textures.Clear();
        }

        internal static TextureContainer CreateTextureContainer(byte[] data)
        {
            // TextureContainer acquisition computes the content hash used by its
            // shared backing store.
            return new TextureContainer(data);
        }

        /// <summary>
        /// Honors the selected save mode. Failures are reported to the user and
        /// propagated; external texture failures must not silently embed textures.
        /// </summary>
        public void Save(PluginData pluginData, string key, object data, bool isCharaController)
        {
            string mode = "Bundled";
            try
            {
#if !EC
                if (KKAPI.Studio.StudioAPI.InsideStudio)
                    mode = SceneSaveType?.GetValue(null, null)?.ToString() ?? mode;
                else
#endif
                if (KKAPI.Maker.MakerAPI.InsideMaker)
                    mode = CardSaveType?.GetValue(null, null)?.ToString() ?? mode;

#if !EC
                // Even a scene with no item textures must collect character payloads.
                if (mode == "Deduped")
                    SaveDeduped(pluginData, key, data, isCharaController);
                else
#endif
                if (data is Dictionary<int, TextureContainer> textures && textures.Count == 0)
                    pluginData.data[key] = null;
                else if (mode == "Local")
                    SaveLocal(pluginData, key, data);
                else
                    SaveBundled(pluginData, key, data, isCharaController);
            }
            catch (System.Exception ex)
            {
                MaterialEditorPluginBase.Logger.LogError(ex);
                MaterialEditorPluginBase.Logger.Log(
                    BepInEx.Logging.LogLevel.Error | BepInEx.Logging.LogLevel.Message,
                    $"[MaterialEditor] {mode} texture save failed. Material edits were NOT updated in this save. "
                    + "Keep the character/scene open, check the log and save again after fixing the error."
                    + (mode == "Local" ? $" Local texture folder: {LocalTexturePath}" : ""));
                throw;
            }
        }

#if !EC
        private void SaveDeduped(PluginData pluginData, string key, object dictRaw, bool isCharaController)
        {
            if (!(dictRaw is Dictionary<int, TextureContainer> textures))
                throw new System.ArgumentException("dictRaw must be Dictionary<int, TextureContainer> and not null!");

            var references = MessagePackSerializer.Serialize(
                textures.ToDictionary(pair => pair.Key, pair => pair.Value.Hash.ToString("X16")));
            byte[] payload = null;
            if (!isCharaController)
            {
                var sharedTextures = new Dictionary<string, byte[]>();
                AddTextures(textures.Values);
                foreach (var character in Studio.Studio.Instance.dicObjectCtrl.Values.OfType<Studio.OCIChar>())
                {
                    var controller = character.charInfo.gameObject.GetComponent<MaterialEditorCharaController>();
                    if (controller != null)
                        AddTextures(controller.TextureDictionary.Values);
                }
                payload = MessagePackSerializer.Serialize(sharedTextures);

                void AddTextures(IEnumerable<TextureContainer> values)
                {
                    foreach (var texture in values)
                    {
                        var hash = texture.Hash.ToString("X16");
                        if (!sharedTextures.ContainsKey(hash))
                            sharedTextures.Add(hash, texture.Data);
                    }
                }
            }

            pluginData.data[DedupedTexSavePrefix + key] = references;
            if (!isCharaController)
                pluginData.data[DedupedTexSavePrefix + key + DedupedTexSavePostfix] = payload;
            pluginData.version = 2;
        }
#endif

        private void SaveLocal(PluginData pluginData, string key, object dictRaw)
        {
            if (!(dictRaw is Dictionary<int, TextureContainer> textures))
                throw new System.ArgumentException("dictRaw must be Dictionary<int, TextureContainer> and not null!");

            Directory.CreateDirectory(LocalTexturePath);
            var hashes = new Dictionary<int, string>();
            foreach (var pair in textures)
            {
                var hash = pair.Value.Hash.ToString("X16");
                var bytes = pair.Value.Data;
                var path = Path.Combine(LocalTexturePath, LocalTexPrefix + hash + "." + IdentifyImageExtension(bytes));
                if (!File.Exists(path))
                {
                    // Publish only complete files; a failed write must not leave a
                    // truncated texture that a subsequent save would reuse.
                    var temporaryPath = Path.Combine(LocalTexturePath, System.Guid.NewGuid().ToString("N") + ".tmp");
                    try
                    {
                        File.WriteAllBytes(temporaryPath, bytes);
                        File.Move(temporaryPath, path);
                    }
                    finally
                    {
                        if (File.Exists(temporaryPath))
                            File.Delete(temporaryPath);
                    }
                }
                hashes.Add(pair.Key, hash);
            }

            // Publish references only after every external texture has been saved.
            var serialized = MessagePackSerializer.Serialize(hashes);
            pluginData.data.Remove(key);
            pluginData.data.Remove(DedupedTexSavePrefix + key);
            pluginData.data.Remove(DedupedTexSavePrefix + key + DedupedTexSavePostfix);
            pluginData.data[LocalTexSavePrefix + key] = serialized;
            pluginData.version = 2;
        }

        /// <summary>
        /// Loads bundled data first, then the version-2 deduplicated or local formats.
        /// Malformed or incomplete external texture data degrades to an empty dictionary.
        /// </summary>
        public T Load<T>(PluginData pluginData, string key, bool isCharaController)
        {
            object loaded = DefaultData();
            try
            {
                if (pluginData?.data == null)
                    return (T)loaded;

                if (pluginData.version > 2)
                {
                    MaterialEditorPluginBase.Logger.LogWarning(
                        $"[MaterialEditor] Texture save format version {pluginData.version} is not supported; "
                        + "the texture data was left untouched and skipped.");
                    return (T)loaded;
                }

                if (pluginData.data.TryGetValue(key, out var bundledData) && bundledData != null)
                    loaded = LoadBundled(pluginData, key, bundledData, isCharaController);
#if !EC
                else if (pluginData.data.TryGetValue(DedupedTexSavePrefix + key, out var dedupedData)
                         && dedupedData != null)
                    loaded = LoadDeduped(pluginData, key, dedupedData, isCharaController);
#endif
                else if (pluginData.data.TryGetValue(LocalTexSavePrefix + key, out var localData)
                         && localData != null)
                    loaded = LoadLocal(pluginData, key, localData, isCharaController);
            }
            catch (System.Exception ex)
            {
                MaterialEditorPluginBase.Logger.LogError(ex);
                MaterialEditorPluginBase.Logger.LogWarning(
                    "[MaterialEditor] Texture data could not be loaded; continuing without those textures.");
                loaded = DefaultData();
            }

            if (loaded is T typed)
                return typed;

            MaterialEditorPluginBase.Logger.LogWarning(
                "[MaterialEditor] Texture data had an unexpected type; continuing without those textures.");
            return (T)DefaultData();
        }

        private static void SaveBundled(PluginData pluginData, string key, object dictRaw, bool isCharaController = false)
        {
            if (!(dictRaw is Dictionary<int, TextureContainer> dict && dict != null))
                throw new System.ArgumentException("dictRaw must be Dictionary<int, TextureContainer> and not null!");
            pluginData.version = 1;
            pluginData.data[key] = MessagePackSerializer.Serialize(
                dict.ToDictionary(pair => pair.Key, pair => pair.Value.Data));
        }

        private static object LoadBundled(PluginData data, string key, object dataBundled, bool isCharaController = false)
        {
            var serializedTextures =
                MessagePackSerializer.Deserialize<Dictionary<int, byte[]>>((byte[])dataBundled);
            var result = new Dictionary<int, TextureContainer>();
            try
            {
                foreach (var pair in serializedTextures)
                    result.Add(pair.Key, CreateTextureContainer(pair.Value));
                return result;
            }
            catch
            {
                DisposeTextureContainers(result);
                throw;
            }
        }

#if !EC
        private object LoadDeduped(PluginData data, string key, object dataDeduped, bool isCharaController = false)
        {
            var textureReferences = MessagePackSerializer.Deserialize<Dictionary<int, string>>(
                (byte[])dataDeduped);

            if (DedupedTextureData == null)
            {
                object textureBytes = null;
                data.data.TryGetValue(
                    DedupedTexSavePrefix + key + DedupedTexSavePostfix,
                    out textureBytes);

                if (textureBytes == null)
                {
                    var sceneData = MEStudio.GetSceneController()?.GetExtendedData();
                    sceneData?.data?.TryGetValue(
                        DedupedTexSavePrefix + key + DedupedTexSavePostfix,
                        out textureBytes);
                }

                if (textureBytes != null)
                    DedupedTextureData = MessagePackSerializer.Deserialize<Dictionary<string, byte[]>>(
                        (byte[])textureBytes);
            }

            var result = new Dictionary<int, TextureContainer>();
            try
            {
                if (DedupedTextureData == null)
                {
                    MaterialEditorPluginBase.Logger.LogWarning(
                        $"[MaterialEditor] Missing deduplicated texture payload for {(isCharaController ? "character" : "scene")} data.");
                }
                else
                {
                    foreach (var pair in textureReferences)
                    {
                        if (pair.Value != null
                            && DedupedTextureData.TryGetValue(pair.Value, out var bytes)
                            && bytes != null
                            && bytes.Length > 0)
                        {
                            result[pair.Key] = CreateTextureContainer(bytes);
                        }
                        else
                        {
                            MaterialEditorPluginBase.Logger.LogWarning(
                                $"[MaterialEditor] Deduplicated texture '{pair.Value}' is missing; it was skipped.");
                        }
                    }
                }

                return result;
            }
            catch
            {
                DisposeTextureContainers(result);
                throw;
            }
            finally
            {
                if (!isCharaController)
                    DedupedTextureData = null;
            }
        }

#endif
        private object LoadLocal(PluginData data, string key, object dataLocal, bool isCharaController = false)
        {
            var hashDictionary = MessagePackSerializer.Deserialize<Dictionary<int, string>>(
                (byte[])dataLocal);
            var result = new Dictionary<int, TextureContainer>();
            try
            {
                foreach (var pair in hashDictionary)
                {
                    var bytes = LoadLocal(pair.Value);
                    if (bytes.Length > 0)
                        result[pair.Key] = CreateTextureContainer(bytes);
                }
                return result;
            }
            catch
            {
                DisposeTextureContainers(result);
                throw;
            }
        }

        private byte[] LoadLocal(string hash)
        {
            if (!IsSafeTextureHash(hash))
            {
                MaterialEditorPluginBase.Logger.LogWarning(
                    "[MaterialEditor] Invalid local texture identifier; it was skipped.");
                return new byte[0];
            }

            if (!Directory.Exists(LocalTexturePath))
            {
                MaterialEditorPluginBase.Logger.LogMessage("[MaterialEditor] Local texture directory doesn't exist, can't load texture!");
                return new byte[0];
            }

            string searchPattern = LocalTexPrefix + hash + ".*";
            string[] files = Directory.GetFiles(LocalTexturePath, searchPattern, SearchOption.TopDirectoryOnly);
            if (files == null || files.Length == 0)
            {
                MaterialEditorPluginBase.Logger.LogMessage($"[MaterialEditor] No local texture found with hash {hash}!");
                return new byte[0];
            }
            if (files.Length > 1)
            {
                MaterialEditorPluginBase.Logger.LogMessage($"[MaterialEditor] Multiple local textures found with hash {hash}, aborting!");
                return new byte[0];
            }

            return File.ReadAllBytes(files[0]);
        }

        private static bool IsSafeTextureHash(string hash)
        {
            if (hash == null || hash.Length != 16)
                return false;

            for (var i = 0; i < hash.Length; i++)
            {
                var c = hash[i];
                if (!((c >= '0' && c <= '9')
                      || (c >= 'a' && c <= 'f')
                      || (c >= 'A' && c <= 'F')))
                    return false;
            }

            return true;
        }

        internal static string IdentifyImageExtension(byte[] data, string fallback = "bin")
        {
            if (data == null)
                return fallback;
            if (HasBytes(data, 0, 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A))
                return "png";
            if (HasBytes(data, 0, 0xFF, 0xD8, 0xFF))
                return "jpg";
            if (HasBytes(data, 0, 0x47, 0x49, 0x46, 0x38, 0x37, 0x61)
                || HasBytes(data, 0, 0x47, 0x49, 0x46, 0x38, 0x39, 0x61))
                return "gif";
            if (HasBytes(data, 0, 0x42, 0x4D))
                return "bmp";
            if (HasBytes(data, 0, 0x44, 0x44, 0x53, 0x20))
                return "dds";
            if (HasBytes(data, 0, 0x52, 0x49, 0x46, 0x46)
                && HasBytes(data, 8, 0x57, 0x45, 0x42, 0x50))
                return "webp";
            return fallback;
        }

        private static bool HasBytes(byte[] data, int offset, params byte[] expected)
        {
            if (data.Length < offset + expected.Length)
                return false;
            for (var i = 0; i < expected.Length; i++)
                if (data[offset + i] != expected[i])
                    return false;
            return true;
        }

        // Preserve the first equal payload and allocate above the highest existing ID.
        internal static int GetOrAddTexture(Dictionary<int, TextureContainer> textures, byte[] data)
        {
            int highestId = 0;
            foreach (var texture in textures)
                if (texture.Value.Data.SequenceEqualFast(data))
                    return texture.Key;
                else if (texture.Key > highestId)
                    highestId = texture.Key;

            highestId++;
            textures.Add(highestId, CreateTextureContainer(data));
            return highestId;
        }
    }
}
