using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using ExtensibleSaveFormat;
using HarmonyLib;
using KKAPI.Maker;
using KKAPI.Utilities;
using MaterialEditorAPI;
using MessagePack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using static KK_Plugins.MaterialEditor.MaterialEditorPlugin;
using static MaterialEditorAPI.MaterialEditorPluginBase;
#if !EC
using KKAPI.Studio;
#endif

namespace KK_Plugins.MaterialEditor
{
    internal static class TextureSaveHandler
    {
        // Saveload variables
        // Do not change constants, lest you break all existing local cards
        public const string LocalTexPrefix = "ME_LocalTex_";
        public const string LocalTexSavePreFix = "LOCAL_";
        public const string DedupedTexSavePreFix = "DEDUPED_";
        public const string DedupedTexSavePostFix = "_DATA";
        public const string LocalTexUnusedFolder = "_Unused";
        private static Dictionary<string, byte[]> DedupedTextureData = null;

        // Audit variables
        private static int auditAllFiles = 0;
        private static int auditProcessedFiles = 0;
        private static int auditRunningThread = 0;
        private static Dictionary<string, string> auditUnusedTextures = null;
        private static Dictionary<string, List<string>> auditMissingTextures = null;
        private static readonly Dictionary<string, List<string>> auditFoundHashToFiles = new Dictionary<string, List<string>>();
        private static readonly object auditLock = new object();
        private static bool auditShow = false;
        private static Coroutine auditDoneCoroutine = null;
        private static Rect auditRect = new Rect();
        private static Vector2 auditUnusedScroll = Vector2.zero;
        private static Vector2 auditMissingScroll = Vector2.zero;
        private static GUIStyle _auditLabel = null;
        private static GUIStyle AuditLabel
        {
            get
            {
                if (_auditLabel == null)
                {
                    _auditLabel = new GUIStyle(GUI.skin.label)
                    {
                        font = new Font(new[] { GUI.skin.font.name }, Mathf.RoundToInt(GUI.skin.font.fontSize * 1.25f))
                    };
                }
                return _auditLabel;
            }
        }
        private static GUIStyle _auditButton = null;
        private static GUIStyle AuditButton
        {
            get
            {
                if (_auditButton == null)
                {
                    _auditButton = new GUIStyle(GUI.skin.button)
                    {
                        font = AuditLabel.font
                    };
                }
                return _auditButton;
            }
        }
        private static GUIStyle _auditWindow = null;
        private static GUIStyle AuditWindow
        {
            get
            {
                if (_auditWindow == null)
                {
#if PH
                    _auditWindow = new GUIStyle(GUI.skin.window);
#else
                    _auditWindow = new GUIStyle(KKAPI.Utilities.IMGUIUtils.SolidBackgroundGuiSkin.window);
#endif
                }
                return _auditWindow;
            }
        }
        private static GUIStyle _auditBigText = null;
        private static GUIStyle AuditBigText
        {
            get
            {
                if (_auditBigText == null)
                {
                    _auditBigText = new GUIStyle(AuditLabel)
                    {
                        font = new Font(new[] { AuditLabel.font.name }, Mathf.RoundToInt(AuditLabel.font.fontSize * 1.5f))
                    };
                }
                return _auditBigText;
            }
        }
        private static GUIStyle _auditWarnButton = null;
        private static GUIStyle AuditWarnButton
        {
            get
            {
                if (_auditWarnButton == null)
                {
                    _auditWarnButton = new GUIStyle(AuditButton)
                    {
                        font = AuditButton.font
                    };
                    var warnColor = new Color(1, 0.25f, 0.20f);
                    _auditWarnButton.normal.textColor = warnColor;
                    _auditWarnButton.active.textColor = warnColor;
                    _auditWarnButton.hover.textColor = warnColor;
                    _auditWarnButton.focused.textColor = warnColor;
                }
                return _auditWarnButton;
            }
        }

        internal static void AuditOptionDrawer(ConfigEntryBase configEntry)
        {
            if (GUILayout.Button("Audit Local Files", GUILayout.ExpandWidth(true)))
            {
                TextureSaveHandler.AuditLocalFiles();
                try
                {
                    if (Chainloader.PluginInfos.TryGetValue("com.bepis.bepinex.configurationmanager", out var cfgMgrInfo) && cfgMgrInfo != null)
                    {
                        var displaying = cfgMgrInfo.Instance.GetType().GetProperty("DisplayingWindow", AccessTools.all);
                        displaying.SetValue(cfgMgrInfo.Instance, false, null);
                    }
                }
                catch { }
            }
        }

        internal static void AuditLocalFiles()
        {
            if (!Directory.Exists(LocalTexturePath))
            {
                MaterialEditorPluginBase.Logger.LogMessage("[MaterialEditor] Local texture directory doesn't exist, nothing to clean up!");
                return;
            }

            string[] localTexFolderFiles = Directory.GetFiles(LocalTexturePath, LocalTexPrefix + "*", SearchOption.TopDirectoryOnly);
            if (localTexFolderFiles.Length == 0)
            {
                MaterialEditorPluginBase.Logger.LogMessage("[MaterialEditor] No local textures found!");
                return;
            }

            auditUnusedTextures = new Dictionary<string, string>();
            foreach (string file in localTexFolderFiles)
                auditUnusedTextures.Add(Regex.Match(file, "(?<=_)[A-F0-9]{16}(?=.)").Value, file.Split(Path.DirectorySeparatorChar).Last());

            var pngs = new List<string>();
            pngs.AddRange(Directory.GetFiles(Path.Combine(BepInEx.Paths.GameRootPath, @"UserData\chara"), "*.png", SearchOption.AllDirectories));
            pngs.AddRange(Directory.GetFiles(Path.Combine(BepInEx.Paths.GameRootPath, @"UserData\Studio\scene"), "*.png", SearchOption.AllDirectories));
            auditAllFiles = pngs.Count;
            auditProcessedFiles = 0;
            auditRect = new Rect();
            auditShow = true;

            int numThreads = Environment.ProcessorCount;
            auditRunningThread = numThreads;
            auditDoneCoroutine = Instance.StartCoroutine(AuditLocalFilesDone());
            for (int i = 0; i < numThreads; i++)
            {
                int nowOffset = i;
                BepInEx.ThreadingHelper.Instance.StartAsyncInvoke(delegate
                {
                    AuditLocalFilesProcessor(pngs, numThreads, nowOffset);
                    --auditRunningThread;
                    return null;
                });
            }
        }

        private static void AuditLocalFilesProcessor(List<string> pngs, int period, int offset)
        {
            lock (MaterialEditorPluginBase.Logger)
                MaterialEditorPluginBase.Logger.LogDebug($"Starting new local file processor with period {period} and offset {offset}!");

            string searchStringStart = LocalTexSavePreFix + MaterialEditorCharaController.TexDicSaveKey;
            byte[] searchStringStartBytes = System.Text.Encoding.ASCII.GetBytes(searchStringStart);
            string searchStringEnd = MaterialEditorCharaController.RendererSaveKey;
            byte[] searchStringEndBytes = System.Text.Encoding.ASCII.GetBytes(searchStringEnd);

            DateTime cutoff = new DateTime(2025, 10, 21);
            string file;
            int i = offset;
            while (i < pngs.Count)
            {
                if (auditDoneCoroutine == null) return;

                file = pngs[i];
                if (file != null && File.Exists(file) && File.GetLastWriteTime(file) > cutoff)
                {
                    if (new FileInfo(file).Length <= int.MaxValue)
                    {
                        byte[] fileData = File.ReadAllBytes(file);
                        int readingAt = 0;
                        while (true)
                        {
                            int patternStart = FindPosition(fileData, searchStringStartBytes, readingAt);
                            if (patternStart > 0)
                            {
                                int patternEnd = FindPosition(fileData, searchStringEndBytes, patternStart);
                                if (patternEnd > 0)
                                {
                                    Dictionary<int, string> hashDict;
                                    List<byte> data = fileData.SubSet(patternStart + searchStringStartBytes.Length + 2, patternEnd - 1).ToList();
                                    for (int j = 0; j < 3; j++)
                                    {
                                        try
                                        {
                                            hashDict = MessagePackSerializer.Deserialize<Dictionary<int, string>>(data.ToArray());
                                            if (hashDict != null && hashDict.Count > 0)
                                            {
                                                foreach (var kvp in hashDict)
                                                    lock (auditFoundHashToFiles)
                                                        if (!auditFoundHashToFiles.ContainsKey(kvp.Value))
                                                            auditFoundHashToFiles.Add(kvp.Value, new List<string> { file });
                                                        else
                                                            lock (auditFoundHashToFiles[kvp.Value])
                                                                auditFoundHashToFiles[kvp.Value].Add(file);
                                                break;
                                            }
                                        }
                                        catch
                                        {
                                            data.RemoveAt(0);
                                        }
                                    }
                                    readingAt = patternEnd;
                                }
                                else break;
                            }
                            else break;
                        }
                    }
                }
                lock (auditLock)
                    ++auditProcessedFiles;
                i += period;
            }
            lock (MaterialEditorPluginBase.Logger)
                MaterialEditorPluginBase.Logger.LogDebug($"Local file processor with offset {offset} done!");
        }

        private static int FindPosition(byte[] data, byte[] pattern, int startPos)
        {
            int pos = startPos - 1;
            int foundPosition = -1;
            int at = 0;

            while (++pos < data.Length)
            {
                if (data[pos] == pattern[at])
                {
                    at++;
                    if (at == 1) foundPosition = pos;
                    if (at == pattern.Length) return foundPosition;
                }
                else
                {
                    at = 0;
                }
            }
            return -1;
        }

        private static IEnumerator AuditLocalFilesDone()
        {
            while (auditRunningThread > 0)
                yield return null;

            auditMissingTextures = new Dictionary<string, List<string>>();
            foreach (var kvp in auditFoundHashToFiles)
                if (!auditUnusedTextures.Remove(kvp.Key))
                    auditMissingTextures.Add(kvp.Key, kvp.Value);

            auditDoneCoroutine = null;

            yield break;
        }

        private static void AuditWindowFunction(int windowID)
        {
            if (auditDoneCoroutine != null)
            {
                GUILayout.BeginVertical(GUILayout.ExpandWidth(true)); GUILayout.FlexibleSpace();
                {
                    GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace();
                    {
                        GUILayout.BeginVertical(GUI.skin.box);
                        {
                            GUILayout.Space(10);
                            GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace(); GUILayout.Label("Processing cards and scenes...", AuditBigText); GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
                            GUILayout.Space(10);
                            GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace(); GUILayout.Label($"{auditProcessedFiles} / {auditAllFiles}", AuditLabel); GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace(); GUILayout.Label($"{Math.Round((double)auditProcessedFiles / auditAllFiles, 3) * 100:0.0}%", AuditLabel); GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
                            GUILayout.Space(10);
                            GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace();
                            if (GUILayout.Button("Cancel", AuditButton, GUILayout.Width(100), GUILayout.Height(30)))
                            {
                                auditShow = false;
                                Instance.StopCoroutine(auditDoneCoroutine);
                                auditDoneCoroutine = null;
                            }
                            GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
                            GUILayout.Space(10);
                        }
                        GUILayout.EndVertical();
                    }
                    GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
                }
                GUILayout.FlexibleSpace(); GUILayout.EndVertical();
            }
            else
            {
                GUILayout.BeginVertical();
                {
                    GUILayout.Space(5);
                    GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace();
                    GUILayout.Label("MaterialEditor local file audit results", AuditBigText);
                    GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
                    GUILayout.Space(10);
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(300));
                        {
                            if (auditUnusedTextures == null || auditUnusedTextures.Count == 0)
                            {
                                GUILayout.BeginVertical(); GUILayout.FlexibleSpace();
                                GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace();
                                GUILayout.Label("No unused textures found!", AuditLabel);
                                GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
                                GUILayout.FlexibleSpace(); GUILayout.EndVertical();
                            }
                            else
                            {
                                GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace();
                                GUILayout.Label("Unused textures", AuditLabel);
                                GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
                                GUILayout.Space(5);
                                auditUnusedScroll = GUILayout.BeginScrollView(auditUnusedScroll, false, true, GUI.skin.label, GUI.skin.verticalScrollbar, GUI.skin.box, GUILayout.ExpandHeight(true));
                                {
                                    GUILayout.BeginVertical();
                                    {
                                        foreach (var kvp in auditUnusedTextures)
                                            GUILayout.Label(kvp.Value);
                                    }
                                    GUILayout.EndVertical();
                                }
                                GUILayout.EndScrollView();
                            }
                        }
                        GUILayout.EndVertical();
                        GUILayout.Space(10);
                        GUILayout.BeginVertical(GUI.skin.box);
                        {
                            if (auditMissingTextures == null || auditMissingTextures.Count == 0)
                            {
                                GUILayout.BeginVertical(); GUILayout.FlexibleSpace();
                                GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace();
                                GUILayout.Label("No missing textures found!", AuditLabel);
                                GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
                                GUILayout.FlexibleSpace(); GUILayout.EndVertical();
                            }
                            else
                            {
                                GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace();
                                GUILayout.Label("Missing textures", AuditLabel);
                                GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
                                GUILayout.Space(5);
                                auditMissingScroll = GUILayout.BeginScrollView(auditMissingScroll, false, true, GUI.skin.label, GUI.skin.verticalScrollbar, GUI.skin.box, GUILayout.ExpandHeight(true));
                                {
                                    GUILayout.BeginVertical();
                                    {
                                        foreach (var kvp in auditMissingTextures)
                                        {
                                            GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));
                                            GUILayout.Label($"Missing texture hash: {kvp.Key}", AuditLabel);
                                            GUILayout.Label($"Used by:\n{string.Join(",\n", kvp.Value.ToArray())}", AuditLabel);
                                            GUILayout.EndVertical();
                                            GUILayout.Space(3);
                                        }
                                    }
                                    GUILayout.EndVertical();
                                }
                                GUILayout.EndScrollView();
                            }
                        }
                        GUILayout.EndVertical();
                        GUILayout.Space(4);
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.Space(10);
                    GUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button("Delete unused files", AuditWarnButton, GUILayout.Height(30)))
                        {
                            foreach (var kvp in auditUnusedTextures)
                                File.Delete(Path.Combine(LocalTexturePath, kvp.Value));
                            auditUnusedTextures.Clear();
                        }
                        GUILayout.Space(5);
                        if (GUILayout.Button("Move unused files to '_Unused' folder", AuditButton, GUILayout.Height(30)))
                        {
                            string unusedFolder = Path.Combine(LocalTexturePath, LocalTexUnusedFolder);
                            if (!Directory.Exists(unusedFolder))
                                Directory.CreateDirectory(unusedFolder);
                            foreach (var kvp in auditUnusedTextures)
                                File.Move(
                                    Path.Combine(LocalTexturePath, kvp.Value),
                                    Path.Combine(Path.Combine(LocalTexturePath, LocalTexUnusedFolder), kvp.Value));
                            auditUnusedTextures.Clear();
                        }
                        GUILayout.Space(5);
                        if (GUILayout.Button("Close", AuditButton, GUILayout.Height(30)))
                        {
                            auditMissingTextures = null;
                            auditUnusedTextures = null;
                            auditShow = false;
                        }
                        GUILayout.Space(4);
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.Space(4);
                }
                GUILayout.EndVertical();
            }
        }

        internal static void DoOnGUI()
        {
            if (auditShow)
            {
                Rect screenRect = new Rect(0, 0, Screen.width, Screen.height);
                for (int i = 0; i < 4; i++) GUI.Box(screenRect, "");
                auditRect.position = new Vector2((Screen.width - auditRect.size.x) / 2, (Screen.height - auditRect.size.y) / 2);
                float minWidth = Mathf.Clamp(Screen.width / 2, 960, 1280);
                auditRect = GUILayout.Window(42069, auditRect, AuditWindowFunction, "", AuditWindow, GUILayout.MinWidth(minWidth), GUILayout.MinHeight(Screen.height * 4 / 5));
                IMGUIUtils.EatInputInRect(screenRect);
            }
        }

        internal static void Save(PluginData data, string key, Dictionary<int, TextureContainer> dict, bool isCharaController)
        {
            int saveType = DetermineSaveType();
            if (dict.Count > 0
#if !EC
                || (
                    StudioAPI.InsideStudio
                    && MaterialEditorCharaController.charaControllers.Any(x => x.TextureDictionary.Count > 0)
                    && saveType == (int)SceneTextureSaveType.Deduped
                )
#endif
            )
                switch (saveType)
                {
                    case (int)CharaTextureSaveType.Local:
                        SaveLocally(data, LocalTexSavePreFix + key, dict);
                        break;
#if !EC
                    case (int)SceneTextureSaveType.Deduped:
                        if (isCharaController)
                            data.data.Add(DedupedTexSavePreFix + key, MessagePackSerializer.Serialize(dict.ToDictionary(pair => pair.Key, pair => pair.Value.Hash.ToString("X16"))));
                        else
                            SaveDeduped(data, DedupedTexSavePreFix + key, dict);
                        break;
#endif
                    default:
                        data.data.Add(key, MessagePackSerializer.Serialize(dict.ToDictionary(pair => pair.Key, pair => pair.Value.Data)));
                        break;
                }
            else
                data.data.Add(key, null);
        }

        internal static Dictionary<int, TextureContainer> Load(PluginData data, string key, bool isCharaController)
        {
            if (data.data.TryGetValue(key, out var texDic) && texDic != null)
                return MessagePackSerializer.Deserialize<Dictionary<int, byte[]>>((byte[])texDic).ToDictionary(pair => pair.Key, pair => new TextureContainer(pair.Value));
#if !EC
            else if (data.data.TryGetValue(DedupedTexSavePreFix + key, out var texDicDeduped) && texDicDeduped != null)
                return LoadDeduped((byte[])texDicDeduped, key, isCharaController);
#endif
            else if (data.data.TryGetValue(LocalTexSavePreFix + key, out var texDicLocal) && texDicLocal != null)
                return MessagePackSerializer.Deserialize<Dictionary<int, string>>((byte[])texDicLocal).ToDictionary(pair => pair.Key, pair => new TextureContainer(LoadLocally(pair.Value)));
            MaterialEditorPluginBase.Logger.LogMessage($"[MaterialEditor] Couldn't load {(isCharaController ? "character" : "scene")} texture data!");
            return null;
        }

#if !EC

        private static void SaveDeduped(PluginData data, string key, Dictionary<int, TextureContainer> dict)
        {
            HashSet<long> hashes = new HashSet<long>();
            Dictionary<int, string> dicKeyToHash = new Dictionary<int, string>();
            Dictionary<string, byte[]> dicHashToData = new Dictionary<string, byte[]>();
            foreach (var kvp in dict)
            {
                string hashString = kvp.Value.Hash.ToString("X16");
                hashes.Add(kvp.Value.Hash);
                dicKeyToHash.Add(kvp.Key, hashString);
                dicHashToData.Add(hashString, kvp.Value.Data);
            }

            foreach (var controller in MaterialEditorCharaController.charaControllers)
            {
                foreach (var textureContainer in controller.TextureDictionary.Values)
                    if (!hashes.Contains(textureContainer.Hash))
                    {
                        hashes.Add(textureContainer.Hash);
                        dicHashToData.Add(textureContainer.Hash.ToString("X16"), textureContainer.Data);
                    }
            }

            data.data.Add(key, MessagePackSerializer.Serialize(dicKeyToHash));
            data.data.Add(key + DedupedTexSavePostFix, MessagePackSerializer.Serialize(dicHashToData));
        }

        private static Dictionary<int, TextureContainer> LoadDeduped(byte[] texDicDeduped, string key, bool isCharaController)
        {
            if (DedupedTextureData == null)
                if (MEStudio.GetSceneController().GetExtendedData()?.data.TryGetValue(DedupedTexSavePreFix + key + DedupedTexSavePostFix, out var dataBytes) != null && dataBytes != null)
                    DedupedTextureData = MessagePackSerializer.Deserialize<Dictionary<string, byte[]>>((byte[])dataBytes);
                else
                    MaterialEditorPluginBase.Logger.LogMessage("[MaterialEditor] Failed to load deduped scene textures!");
            Dictionary<int, TextureContainer> result = new Dictionary<int, TextureContainer>();
            if (DedupedTextureData != null)
                result = MessagePackSerializer.Deserialize<Dictionary<int, string>>(texDicDeduped).ToDictionary(pair => pair.Key, pair => new TextureContainer(DedupedTextureData[pair.Value]));
            if (!isCharaController)
                DedupedTextureData = null;
            return result;
        }

#endif
        private static void SaveLocally(PluginData data, string key, Dictionary<int, TextureContainer> dict)
        {
            if (!Directory.Exists(LocalTexturePath))
                Directory.CreateDirectory(LocalTexturePath);

            var hashDict = dict.ToDictionary(pair => pair.Key, pair => pair.Value.Hash.ToString("X16"));
            foreach (var kvp in hashDict)
            {
                string fileName = LocalTexPrefix + kvp.Value + "." + ImageTypeIdentifier.Identify(dict[kvp.Key].Data);
                string filePath = Path.Combine(LocalTexturePath, fileName);
                if (!File.Exists(filePath))
                    File.WriteAllBytes(filePath, dict[kvp.Key].Data);
            }

            data.data.Add(key, MessagePackSerializer.Serialize(hashDict));
        }

        private static byte[] LoadLocally(string hash)
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

        private static bool IsAutoSave()
        {
            if (Chainloader.PluginInfos.TryGetValue("com.deathweasel.bepinex.autosave", out PluginInfo pluginInfo) && pluginInfo?.Instance != null)
                return (bool)(pluginInfo.Instance.GetType().GetField("Autosaving")?.GetValue(null) ?? false);
            return false;
        }

#if !PH
        internal static string AddLocalPrefixToCard(string current)
        {
            if (!IsAutoSave() && TextureSaveTypeChara.Value == CharaTextureSaveType.Local)
                return LocalTexSavePreFix + current;
            return current;
        }
#endif

        private static int DetermineSaveType()
        {
#if !EC
            if (StudioAPI.InsideStudio)
            {
                foreach (SceneTextureSaveType option in Enum.GetValues(typeof(SceneTextureSaveType)))
                {
                    if (
                        (IsAutoSave() && TextureSaveTypeSceneAuto.Value == option.ToString()) ||
                        ((TextureSaveTypeSceneAuto.Value == "-" || !IsAutoSave()) && TextureSaveTypeScene.Value == option)
                    )
                        return (int)option;
                }
                return (int)SceneTextureSaveType.Bundled;
            }
#endif
            if (MakerAPI.InsideMaker)
            {
                foreach (CharaTextureSaveType option in Enum.GetValues(typeof(CharaTextureSaveType)))
                {
                    if (
                        (IsAutoSave() && TextureSaveTypeCharaAuto.Value == option.ToString()) ||
                        ((TextureSaveTypeCharaAuto.Value == "-" || !IsAutoSave()) && TextureSaveTypeChara.Value == option)
                    )
                        return (int)option;
                }
                return (int)CharaTextureSaveType.Bundled;
            }
            throw new ArgumentException("Not inside Studio or Maker!");
        }
    }
}
