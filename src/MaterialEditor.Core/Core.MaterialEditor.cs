using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using KKAPI.Chara;
using KKAPI.Maker;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using UniRx;
using UnityEngine;
using XUnity.ResourceRedirector;
using static KK_Plugins.MaterialEditor.MaterialAPI;
#if AI || HS2
using AIChara;
#endif

namespace KK_Plugins.MaterialEditor
{
    /// <summary>
    /// MaterialEditor plugin base
    /// </summary>
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    [BepInDependency(XUnity.ResourceRedirector.Constants.PluginData.Identifier)]
    [BepInDependency(Sideloader.Sideloader.GUID)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class MaterialEditorPlugin : BaseUnityPlugin
    {
        /// <summary>
        /// MaterialEditor plugin GUID
        /// </summary>
        public const string GUID = "com.deathweasel.bepinex.materialeditor";
        /// <summary>
        /// MaterialEditor plugin name
        /// </summary>
        public const string PluginName = "Material Editor";
        internal const string PluginNameInternal = Constants.Prefix + "_MaterialEditor";
        /// <summary>
        /// MaterialEditor plugin version
        /// </summary>
        public const string Version = "2.2";
        internal static new ManualLogSource Logger;

        internal const string FileExt = ".png";
        internal const string FileFilter = "Images (*.png;.jpg)|*.png;*.jpg|All files|*.*";
        /// <summary>
        /// Path where textures will be exported
        /// </summary>
        public static readonly string ExportPath = Path.Combine(Paths.GameRootPath, @"UserData\MaterialEditor");
        /// <summary>
        /// Saved material edits
        /// </summary>
        public static CopyContainer CopyData = new CopyContainer();

        internal static Dictionary<string, ShaderData> LoadedShaders = new Dictionary<string, ShaderData>();
        internal static SortedDictionary<string, Dictionary<string, ShaderPropertyData>> XMLShaderProperties = new SortedDictionary<string, Dictionary<string, ShaderPropertyData>>();

        internal static ConfigEntry<float> UIScale { get; private set; }
        internal static ConfigEntry<float> UIWidth { get; private set; }
        internal static ConfigEntry<float> UIHeight { get; private set; }
        internal static ConfigEntry<bool> WatchTexChanges { get; private set; }
        internal static ConfigEntry<bool> ShaderOptimization { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> DisableShadowCastingHotkey { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> EnableShadowCastingHotkey { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> ResetShadowCastingHotkey { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> DisableReceiveShadows { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> EnableReceiveShadows { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> ResetReceiveShadows { get; private set; }

        internal void Main()
        {
            Logger = base.Logger;
            Directory.CreateDirectory(ExportPath);

            MakerAPI.MakerExiting += (s, e) => UI.Visible = false;
            CharacterApi.RegisterExtraBehaviour<MaterialEditorCharaController>(GUID);
            AccessoriesApi.SelectedMakerAccSlotChanged += AccessoriesApi_SelectedMakerAccSlotChanged;
            AccessoriesApi.AccessoryKindChanged += AccessoriesApi_AccessoryKindChanged;
            AccessoriesApi.AccessoryTransferred += AccessoriesApi_AccessoryTransferred;
#if KK
            AccessoriesApi.AccessoriesCopied += AccessoriesApi_AccessoriesCopied;
#endif

            UIScale = Config.Bind("Config", "UI Scale", 1.75f, new ConfigDescription("Controls the size of the window.", new AcceptableValueRange<float>(1f, 3f), new ConfigurationManagerAttributes { Order = 5 }));
            UIWidth = Config.Bind("Config", "UI Width", 0.3f, new ConfigDescription("Controls the size of the window.", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 4, ShowRangeAsPercent = false }));
            UIHeight = Config.Bind("Config", "UI Height", 0.3f, new ConfigDescription("Controls the size of the window.", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 3, ShowRangeAsPercent = false }));
            WatchTexChanges = Config.Bind("Config", "Watch File Changes", true, new ConfigDescription("Watch for file changes and reload textures on change. Can be toggled in the UI.", null, new ConfigurationManagerAttributes { Order = 2 }));
            ShaderOptimization = Config.Bind("Config", "Shader Optimization", true, new ConfigDescription("Replaces every loaded shader with the MaterialEditor copy of the shader. Reduces the number of copies of shaders loaded which reduces RAM usage and improves performance.", null, new ConfigurationManagerAttributes { Order = 1 }));
            DisableShadowCastingHotkey = Config.Bind("Keyboard Shortcuts", "Disable ShadowCasting", new KeyboardShortcut(KeyCode.M, KeyCode.LeftControl), "Disable ShadowCasting for all selected items and their child items in Studio");
            EnableShadowCastingHotkey = Config.Bind("Keyboard Shortcuts", "Enable ShadowCasting", new KeyboardShortcut(KeyCode.M, KeyCode.LeftAlt), "Enable ShadowCasting for all selected items and their child items in Studio");
            ResetShadowCastingHotkey = Config.Bind("Keyboard Shortcuts", "Reset ShadowCasting", new KeyboardShortcut(KeyCode.M, KeyCode.LeftControl, KeyCode.LeftAlt), "Reset ShadowCasting for all selected items and their child items in Studio");
            DisableReceiveShadows = Config.Bind("Keyboard Shortcuts", "Disable ReceiveShadows", new KeyboardShortcut(KeyCode.N, KeyCode.LeftControl), "Disable ReceiveShadows for all selected items and their child items in Studio");
            EnableReceiveShadows = Config.Bind("Keyboard Shortcuts", "Enable ReceiveShadows", new KeyboardShortcut(KeyCode.N, KeyCode.LeftAlt), "Enable ReceiveShadows for all selected items and their child items in Studio");
            ResetReceiveShadows = Config.Bind("Keyboard Shortcuts", "Reset ReceiveShadows", new KeyboardShortcut(KeyCode.N, KeyCode.LeftControl, KeyCode.LeftAlt), "Reset ReceiveShadows for all selected items and their child items in Studio");
            WatchTexChanges.SettingChanged += WatchTexChanges_SettingChanged;

            var harmony = Harmony.CreateAndPatchAll(typeof(Hooks));

#if KK || EC
            //Hooks for transferring accessories (MoreAccessories compatibility)
            foreach (var method in typeof(ChaCustom.CvsAccessoryChange).GetMethods(AccessTools.all).Where(x => x.Name.Contains("<Start>m__4")))
                harmony.Patch(method, new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.AccessoryTransferHook), AccessTools.all)));
#elif AI || HS2
            //Hooks for changing clothing pattern
            foreach (var method in typeof(CharaCustom.CustomClothesPatternSelect).GetMethods(AccessTools.all).Where(x => x.Name.Contains("<ChangeLink>")))
                harmony.Patch(method, new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.ClothesColorChangeHook), AccessTools.all)));

            //hooks for changing clothing color
            foreach (var method in typeof(CharaCustom.CustomClothesColorSet).GetMethods(AccessTools.all).Where(x => x.Name.StartsWith("<Initialize>")))
                harmony.Patch(method, new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.ClothesColorChangeHook), AccessTools.all)));
#endif
            StartCoroutine(LoadXML());
            StartCoroutine(GetUncensorSelectorParts());

            ResourceRedirection.RegisterAssetLoadedHook(HookBehaviour.OneCallbackPerResourceLoaded, AssetLoadedHook);
        }

        /// <summary>
        /// Every time an asset is loaded, swap its shader for the one loaded by MaterialEditor. This reduces the number of instances of a shader once they are cleaned up by garbage collection
        /// which reduce RAM usage, etc. Also fixes KK mods in EC by swapping them to the equivalent EC shader.
        /// </summary>
        private static void AssetLoadedHook(AssetLoadedContext context)
        {
            if (!ShaderOptimization.Value) return;

            if (context.Asset is GameObject go)
            {
                var renderers = go.GetComponentsInChildren<Renderer>();
                for (var i = 0; i < renderers.Length; i++)
                {
                    var renderer = renderers[i];
                    for (var j = 0; j < renderer.materials.Length; j++)
                    {
                        var material = renderer.materials[j];
                        if (LoadedShaders.TryGetValue(material.shader.name, out var shaderData) && shaderData.Shader != null && shaderData.ShaderOptimization)
                        {
                            int renderQueue = material.renderQueue;
                            material.shader = shaderData.Shader;
                            material.renderQueue = renderQueue;
                        }
                    }
                }
            }
            else if (context.Asset is Material mat)
            {
                if (LoadedShaders.TryGetValue(mat.shader.name, out var shaderData) && shaderData.Shader != null && shaderData.ShaderOptimization)
                {
                    int renderQueue = mat.renderQueue;
                    mat.shader = shaderData.Shader;
                    mat.renderQueue = renderQueue;
                }
            }
            else if (context.Asset is Shader shader)
            {
                if (LoadedShaders.TryGetValue(shader.name, out var shaderData) && shaderData.Shader != null && shaderData.ShaderOptimization)
                    context.Asset = shaderData.Shader;
            }
        }

        /// <summary>
        /// Get the list of additional body parts from UncensorSelector and add them to the body parts list
        /// </summary>
        private static IEnumerator GetUncensorSelectorParts()
        {
            yield return null;

            var uncensorSelectorType = Type.GetType($"KK_Plugins.UncensorSelector, {Constants.Prefix}_UncensorSelector");
            if (uncensorSelectorType == null) yield break;
            var uncensorSelectorObject = FindObjectOfType(uncensorSelectorType);
            if (uncensorSelectorObject == null) yield break;

            var AllAdditionalParts = Traverse.Create(uncensorSelectorObject).Field("AllAdditionalParts").GetValue<HashSet<string>>();
            foreach (var parts in AllAdditionalParts)
                BodyParts.Add(parts);
        }

        private static void WatchTexChanges_SettingChanged(object sender, EventArgs e)
        {
            if (!WatchTexChanges.Value)
                UI.TexChangeWatcher?.Dispose();
        }
        private static void AccessoriesApi_AccessoryTransferred(object sender, AccessoryTransferEventArgs e)
        {
            var controller = GetCharaController(MakerAPI.GetCharacterControl());
            if (controller != null)
                controller.AccessoryTransferredEvent(sender, e);
        }
        private static void AccessoriesApi_AccessoryKindChanged(object sender, AccessorySlotEventArgs e)
        {
            var controller = GetCharaController(MakerAPI.GetCharacterControl());
            if (controller != null)
                controller.AccessoryKindChangeEvent(sender, e);
        }
        private static void AccessoriesApi_SelectedMakerAccSlotChanged(object sender, AccessorySlotEventArgs e)
        {
            var controller = GetCharaController(MakerAPI.GetCharacterControl());
            if (controller != null)
                controller.AccessorySelectedSlotChangeEvent(sender, e);
        }
#if KK
        private static void AccessoriesApi_AccessoriesCopied(object sender, AccessoryCopyEventArgs e)
        {
            var controller = GetCharaController(MakerAPI.GetCharacterControl());
            if (controller != null)
                controller.AccessoriesCopiedEvent(sender, e);
        }
#endif

        private static IEnumerator LoadXML()
        {
            yield return new WaitUntil(() => AssetBundleManager.ManifestBundlePack.Count != 0);

            var loadedManifests = Sideloader.Sideloader.Manifests;
            XMLShaderProperties["default"] = new Dictionary<string, ShaderPropertyData>();

            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{nameof(KK_Plugins)}.Resources.default.xml"))
                if (stream != null)
                    using (XmlReader reader = XmlReader.Create(stream))
                        LoadXML(XDocument.Load(reader).Element("MaterialEditor"));

            foreach (var manifest in loadedManifests.Values)
            {
                LoadXML(manifest.manifestDocument?.Root?.Element(PluginNameInternal));
                LoadXML(manifest.manifestDocument?.Root?.Element("MaterialEditor"));
            }
        }

        private static void LoadXML(XElement materialEditorElement)
        {
            if (materialEditorElement == null) return;

            foreach (var shaderElement in materialEditorElement.Elements("Shader"))
            {
                string shaderName = shaderElement.Attribute("Name").Value;

                if (LoadedShaders.ContainsKey(shaderName))
                {
                    Destroy(LoadedShaders[shaderName].Shader);
                    LoadedShaders.Remove(shaderName);
                }
                LoadedShaders[shaderName] = new ShaderData(shaderName, shaderElement.Attribute("AssetBundle")?.Value, shaderElement.Attribute("RenderQueue")?.Value, shaderElement.Attribute("Asset")?.Value, shaderElement.Attribute("ShaderOptimization")?.Value);

                XMLShaderProperties[shaderName] = new Dictionary<string, ShaderPropertyData>();

                foreach (XElement element in shaderElement.Elements("Property"))
                {
                    string propertyName = element.Attribute("Name").Value;
                    ShaderPropertyType propertyType = (ShaderPropertyType)Enum.Parse(typeof(ShaderPropertyType), element.Attribute("Type").Value);
                    string defaultValue = element.Attribute("DefaultValue")?.Value;
                    string defaultValueAB = element.Attribute("DefaultValueAssetBundle")?.Value;
                    string range = element.Attribute("Range")?.Value;
                    string min = null;
                    string max = null;
                    if (!range.IsNullOrWhiteSpace())
                    {
                        var rangeSplit = range.Split(',');
                        if (rangeSplit.Length == 2)
                        {
                            min = rangeSplit[0];
                            max = rangeSplit[1];
                        }
                    }
                    ShaderPropertyData shaderPropertyData = new ShaderPropertyData(propertyName, propertyType, defaultValue, defaultValueAB, min, max);

                    XMLShaderProperties["default"][propertyName] = shaderPropertyData;
                    XMLShaderProperties[shaderName][propertyName] = shaderPropertyData;
                }
            }
        }

        internal static Texture2D TextureFromBytes(byte[] texBytes, TextureFormat format = TextureFormat.ARGB32, bool mipmaps = true)
        {
            if (texBytes == null || texBytes.Length == 0) return null;

            var tex = new Texture2D(2, 2, format, mipmaps);
            tex.LoadImage(texBytes);
            return tex;
        }

        internal static bool CheckBlacklist(string materialName, string propertyName)
        {
            if (materialName == "cf_m_body" || materialName == "cm_m_body")
                if (propertyName == "alpha_a" || propertyName == "alpha_b" || propertyName == "AlphaMask")
                    return true;
            return false;
        }

        /// <summary>
        /// Get the KKAPI character controller for MaterialEditor. Provides access to methods for getting and setting material changes.
        /// </summary>
        /// <param name="chaControl"></param>
        /// <returns>KKAPI character controller</returns>
        public static MaterialEditorCharaController GetCharaController(ChaControl chaControl) => chaControl == null ? null : chaControl.gameObject.GetComponent<MaterialEditorCharaController>();

        internal static Texture2D GetT2D(RenderTexture renderTexture)
        {
            var currentActiveRT = RenderTexture.active;
            RenderTexture.active = renderTexture;
            var tex = new Texture2D(renderTexture.width, renderTexture.height);
            tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
            RenderTexture.active = currentActiveRT;
            return tex;
        }

        internal static void SaveTexR(RenderTexture renderTexture, string path)
        {
            var tex = GetT2D(renderTexture);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            DestroyImmediate(tex);
        }

        internal static void SaveTex(Texture tex, string path, RenderTextureFormat rtf = RenderTextureFormat.Default, RenderTextureReadWrite cs = RenderTextureReadWrite.Default)
        {
            var tmp = RenderTexture.GetTemporary(tex.width, tex.height, 0, rtf, cs);
            var currentActiveRT = RenderTexture.active;
            RenderTexture.active = tmp;
            GL.Clear(false, true, new Color(0, 0, 0, 0));
            Graphics.Blit(tex, tmp);
            SaveTexR(tmp, path);
            RenderTexture.active = currentActiveRT;
            RenderTexture.ReleaseTemporary(tmp);
        }

        internal class ShaderData
        {
            public string ShaderName;
            public Shader Shader;
            public int? RenderQueue;
            public bool ShaderOptimization;

            public ShaderData(string shaderName, string assetBundlePath = "", string renderQueue = "", string assetPath = "", string shaderOptimization = null)
            {
                ShaderName = shaderName;

                if (renderQueue.IsNullOrEmpty())
                    RenderQueue = null;
                else if (int.TryParse(renderQueue, out int result))
                    RenderQueue = result;
                else
                    RenderQueue = null;

                if (bool.TryParse(shaderOptimization, out bool shaderOptimizationBool))
                    ShaderOptimization = shaderOptimizationBool;
                else
                    ShaderOptimization = true;

                if (assetBundlePath.IsNullOrEmpty())
                {
                    Shader = null;
                }
                else
                {
                    if (assetPath.IsNullOrEmpty())
                    {
                        try
                        {
                            if (assetBundlePath.StartsWith("Resources."))
                            {
                                AssetBundle bundle = AssetBundle.LoadFromMemory(UILib.Resource.LoadEmbeddedResource($"{nameof(KK_Plugins)}.{assetBundlePath}"));
                                Shader = bundle.LoadAsset<Shader>(shaderName);
                                bundle.Unload(false);
                            }
                            else
                                Shader = CommonLib.LoadAsset<Shader>(assetBundlePath, $"{shaderName}");
                        }
                        catch
                        {
                            Logger.LogWarning($"Unable to load shader: {shaderName}");
                            Shader = null;
                        }
                    }
                    else
                    {
                        try
                        {
                            if (assetBundlePath.StartsWith("Resources."))
                            {
                                AssetBundle bundle = AssetBundle.LoadFromMemory(UILib.Resource.LoadEmbeddedResource($"{nameof(KK_Plugins)}.{assetBundlePath}"));
                                Shader = bundle.LoadAsset<Shader>(shaderName);

                                var go = bundle.LoadAsset<GameObject>(assetPath);
                                var renderers = go.GetComponentsInChildren<Renderer>();
                                for (var i = 0; i < renderers.Length; i++)
                                {
                                    var renderer = renderers[i];
                                    for (var j = 0; j < renderer.materials.Length; j++)
                                    {
                                        var material = renderer.materials[j];
                                        if (material.shader.NameFormatted() == ShaderName)
                                            Shader = material.shader;
                                    }
                                }
                                Destroy(go);

                                bundle.Unload(false);
                            }
                            else
                            {
                                var go = CommonLib.LoadAsset<GameObject>(assetBundlePath, assetPath);
                                var renderers = go.GetComponentsInChildren<Renderer>();
                                for (var i = 0; i < renderers.Length; i++)
                                {
                                    var renderer = renderers[i];
                                    for (var j = 0; j < renderer.materials.Length; j++)
                                    {
                                        var material = renderer.materials[j];
                                        if (material.shader.NameFormatted() == ShaderName)
                                            Shader = material.shader;
                                    }
                                }
                                Destroy(go);
                            }
                        }
                        catch
                        {
                            Logger.LogWarning($"Unable to load shader: {shaderName}");
                            Shader = null;
                        }
                    }
                }
            }
        }

        internal class ShaderPropertyData
        {
            public string Name;
            public ShaderPropertyType Type;
            public string DefaultValue;
            public string DefaultValueAssetBundle;
            public float? MinValue;
            public float? MaxValue;

            public ShaderPropertyData(string name, ShaderPropertyType type, string defaultValue = null, string defaultValueAB = null, string minValue = null, string maxValue = null)
            {
                Name = name;
                Type = type;
                DefaultValue = defaultValue.IsNullOrEmpty() ? null : defaultValue;
                DefaultValueAssetBundle = defaultValueAB.IsNullOrEmpty() ? null : defaultValueAB;
                if (!minValue.IsNullOrWhiteSpace() && !maxValue.IsNullOrWhiteSpace())
                {
                    if (float.TryParse(minValue, out float min) && float.TryParse(maxValue, out float max))
                    {
                        MinValue = min;
                        MaxValue = max;
                    }
                }
            }
        }
    }
}
