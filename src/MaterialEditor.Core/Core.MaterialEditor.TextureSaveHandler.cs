using System.Collections.Generic;
using ExtensibleSaveFormat;
using MaterialEditorAPI;
using KKAPI.Utilities;
using MessagePack;
using System.Linq;
using System.IO;

namespace KK_Plugins.MaterialEditor
{
    internal class TextureSaveHandler : TextureSaveHandlerBase
    {
        internal static TextureSaveHandler Instance;
#if !EC
        private Dictionary<string, byte[]> DedupedTextureData = null;
#endif

        public TextureSaveHandler(
            string localTexturePath,
            string localTexPrefix = "ME_LocalTex_",
            string localTexSavePrefix = "LOCAL_",
            string dedupedTexSavePrefix = "DEDUPED_",
            string dedupedTexSavePostfix = "_DATA",
            string localTexUnusedFolder = "_Unused"
        ) : base(
            localTexturePath,
            localTexPrefix,
            localTexSavePrefix,
            dedupedTexSavePrefix,
            dedupedTexSavePostfix,
            localTexUnusedFolder
        ) { Instance = this; }

        protected override object DefaultData()
        {
            return new Dictionary<int, TextureContainer>();
        }

        public override void Save(PluginData pluginData, string key, object data, bool isCharaController)
        {
            try
            {
                base.Save(pluginData, key, data, isCharaController);
            }
            catch
            {
                SaveBundled(pluginData, key, data, isCharaController);
            }
        }

        protected override bool IsBundled(PluginData pluginData, string key, out object data)
        {
            return pluginData.data.TryGetValue(key, out data) && data != null;
        }

#if !EC
        protected override bool IsDeduped(PluginData pluginData, string key, out object data)
        {
            return pluginData.data.TryGetValue(DedupedTexSavePrefix + key, out data) && data != null;
        }
#endif

        protected override bool IsLocal(PluginData pluginData, string key, out object data)
        {
            return pluginData.data.TryGetValue(LocalTexSavePrefix + key, out data) && data != null;
        }

        protected override void SaveBundled(PluginData pluginData, string key, object dictRaw, bool isCharaController = false)
        {
            var dict = dictRaw as Dictionary<int, TextureContainer>;
            pluginData.data.Add(key, MessagePackSerializer.Serialize(dict.ToDictionary(pair => pair.Key, pair => pair.Value.Data)));
        }

        protected override object LoadBundled(PluginData data, string key, object dataBundled, bool isCharaController = false)
        {
            return MessagePackSerializer.Deserialize<Dictionary<int, byte[]>>((byte[])dataBundled)
                .ToDictionary(pair => pair.Key, pair => new TextureContainer(pair.Value));
        }

#if !EC

        protected override void SaveDeduped(PluginData data, string key, object dictRaw, bool isCharaController = false)
        {
            var dict = dictRaw as Dictionary<int, TextureContainer>;

            data.data.Add(DedupedTexSavePrefix + key, MessagePackSerializer.Serialize(
                dict.ToDictionary(pair => pair.Key, pair => pair.Value.Hash.ToString("X16"))
            ));
            if (isCharaController)
                return;

            HashSet<long> hashes = new HashSet<long>();
            Dictionary<string, byte[]> dicHashToData = new Dictionary<string, byte[]>();
            foreach (var kvp in dict)
            {
                string hashString = kvp.Value.Hash.ToString("X16");
                hashes.Add(kvp.Value.Hash);
                dicHashToData.Add(hashString, kvp.Value.Data);
            }

            foreach (var controller in MaterialEditorCharaController.charaControllers)
                foreach (var textureContainer in controller.TextureDictionary.Values)
                    if (!hashes.Contains(textureContainer.Hash))
                    {
                        hashes.Add(textureContainer.Hash);
                        dicHashToData.Add(textureContainer.Hash.ToString("X16"), textureContainer.Data);
                    }

            data.data.Add(DedupedTexSavePrefix + key + DedupedTexSavePostfix, MessagePackSerializer.Serialize(dicHashToData));
        }

        protected override object LoadDeduped(PluginData data, string key, object dataDeduped, bool isCharaController = false)
        {
            if (data.data.TryGetValue(DedupedTexSavePrefix + key, out var dedupedData) && dedupedData != null)
            {
                if (DedupedTextureData == null)
                    if (MEStudio.GetSceneController().GetExtendedData()?.data.TryGetValue(DedupedTexSavePrefix + key + DedupedTexSavePostfix, out var dataBytes) != null && dataBytes != null)
                        DedupedTextureData = MessagePackSerializer.Deserialize<Dictionary<string, byte[]>>((byte[])dataBytes);
                    else
                        MaterialEditorPluginBase.Logger.LogMessage($"[MaterialEditor] Failed to load deduped {(isCharaController ? "character" : "scene")} textures!");
                Dictionary<int, TextureContainer> result = new Dictionary<int, TextureContainer>();
                if (DedupedTextureData != null)
                    result = MessagePackSerializer.Deserialize<Dictionary<int, string>>((byte[])dedupedData).ToDictionary(pair => pair.Key, pair => new TextureContainer(DedupedTextureData[pair.Value]));
                if (!isCharaController)
                    DedupedTextureData = null;
                return result;
            }

            return DefaultData();
        }

#endif
        protected override void SaveLocal(PluginData data, string key, object dictRaw, bool isCharaController = false)
        {
            if (!Directory.Exists(LocalTexturePath))
                Directory.CreateDirectory(LocalTexturePath);

            var dict = dictRaw as Dictionary<int, TextureContainer>;
            var hashDict = dict.ToDictionary(pair => pair.Key, pair => pair.Value.Hash.ToString("X16"));
            foreach (var kvp in hashDict)
            {
                string fileName = LocalTexPrefix + kvp.Value + "." + ImageTypeIdentifier.Identify(dict[kvp.Key].Data);
                string filePath = Path.Combine(LocalTexturePath, fileName);
                if (!File.Exists(filePath))
                    File.WriteAllBytes(filePath, dict[kvp.Key].Data);
            }

            data.data.Add(LocalTexSavePrefix + key, MessagePackSerializer.Serialize(hashDict));
        }

        protected override object LoadLocal(PluginData data, string key, object dataLocal, bool isCharaController = false)
        {
            var hashDic = MessagePackSerializer.Deserialize<Dictionary<int, string>>((byte[])data.data[LocalTexSavePrefix + key]);
            return hashDic.ToDictionary(kvp => kvp.Key, kvp => new TextureContainer(LoadLocal(kvp.Value)));
        }

        private byte[] LoadLocal(string hash)
        {
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
    }
}
