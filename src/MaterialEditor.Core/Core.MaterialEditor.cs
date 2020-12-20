using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using MaterialEditor;
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
using static MaterialEditor.MaterialAPI;
using static MaterialEditor.MaterialEditorPlugin;
#if AI || HS2
using AIChara;
using ChaAccessoryComponent = AIChara.CmpAccessory;
#endif
#if EC
using Map;
#else
using Studio;
#endif
#if PH
using ChaControl = Human;
#endif

namespace KK_Plugins.MaterialEditorWrapper
{
    /// <summary>
    /// MaterialEditor plugin base
    /// </summary>
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInDependency(XUnity.ResourceRedirector.Constants.PluginData.Identifier, XUnity.ResourceRedirector.Constants.PluginData.Version)]
    [BepInDependency(MaterialEditorPlugin.PluginGUID, MaterialEditorPlugin.PluginVersion)]
#if !PH
    [BepInDependency(Sideloader.Sideloader.GUID, Sideloader.Sideloader.Version)]
#endif
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public partial class Plugin : BaseUnityPlugin
    {
        /// <summary>
        /// MaterialEditor plugin GUID
        /// </summary>
        public const string PluginGUID = "com.deathweasel.bepinex.materialeditor";
        /// <summary>
        /// MaterialEditor plugin name
        /// </summary>
        public const string PluginName = "Material Editor";
        internal const string PluginNameInternal = Constants.Prefix + "_MaterialEditor";
        /// <summary>
        /// MaterialEditor plugin version
        /// </summary>
        public const string PluginVersion = "2.4.1";
        internal static new ManualLogSource Logger;

#if KK || EC
        internal static ConfigEntry<bool> RimRemover { get; private set; }
#endif
        internal static ConfigEntry<KeyboardShortcut> DisableShadowCastingHotkey { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> EnableShadowCastingHotkey { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> ResetShadowCastingHotkey { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> DisableReceiveShadows { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> EnableReceiveShadows { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> ResetReceiveShadows { get; private set; }

#if AI || HS2
        public static HashSet<string> BodyParts = new HashSet<string> {
            "o_eyebase_L", "o_eyebase_R", "o_eyelashes", "o_eyeshadow", "o_head", "o_namida", "o_tang", "o_tooth", "o_body_cf", "o_mnpa", "o_mnpb", "cm_o_dan00", "o_tang",
            "cm_o_dan00", "o_tang", "o_silhouette_cf", "o_body_cf", "o_body_cm", "o_head" };
#elif KK || EC
        public static HashSet<string> BodyParts = new HashSet<string> {
            "cf_O_tooth", "cf_O_canine", "cf_O_tang", "o_tang", "n_tang", "n_tang_silhouette",  "cf_O_eyeline", "cf_O_eyeline_low", "cf_O_mayuge", "cf_Ohitomi_L", "cf_Ohitomi_R",
            "cf_Ohitomi_L02", "cf_Ohitomi_R02", "cf_O_noseline", "cf_O_namida_L", "cf_O_namida_M", "o_dankon", "o_gomu", "o_dan_f", "cf_O_namida_S", "cf_O_gag_eye_00", "cf_O_gag_eye_01",
            "cf_O_gag_eye_02", "o_shadowcaster", "o_shadowcaster_cm", "o_mnpa", "o_mnpb", "n_body_silhouette", "o_body_a", "cf_O_face" };
        /// <summary>
        /// Parts of the mouth that need special handling
        /// </summary>
        public static HashSet<string> MouthParts = new HashSet<string> { "cf_O_tooth", "cf_O_canine", "cf_O_tang", "o_tang", "n_tang" };
#elif PH
        public static HashSet<string> BodyParts = new HashSet<string> {
            "cf_O_eyekage1", "cf_O_ha", "cf_O_head", "cf_O_matuge", "cf_O_mayuge", "cf_O_sita", "cf_O_eyehikari_L", "cf_O_eyewhite_L", "cf_O_eyehikari_R", "cf_O_eyewhite_R",
            "cf_O_namida01", "cf_O_namida02", "cf_O_namida03", "cf_O_body_00", "cf_O_mnpk", "cf_O_mnpk_00_00", "cf_O_mnpk_00_01", "cf_O_mnpk_00_02", "cf_O_mnpk_00_03",
            "cf_O_mnpk_00_04", "cf_O_mnpk_00_05", "cf_O_nail", "cf_O_tang", "cf_O_tikubiL_00", "cf_O_tikubiR_00" };
#endif

        internal void Main()
        {
            Logger = base.Logger;

            MakerAPI.MakerExiting += (s, e) => MaterialEditorUI.Visible = false;
            CharacterApi.RegisterExtraBehaviour<MaterialEditorCharaController>(PluginGUID);
#if !PH
            AccessoriesApi.SelectedMakerAccSlotChanged += AccessoriesApi_SelectedMakerAccSlotChanged;
            AccessoriesApi.AccessoryKindChanged += AccessoriesApi_AccessoryKindChanged;
            AccessoriesApi.AccessoryTransferred += AccessoriesApi_AccessoryTransferred;
#endif
#if KK
            AccessoriesApi.AccessoriesCopied += AccessoriesApi_AccessoriesCopied;
#endif

#if KK || EC
            RimRemover = Config.Bind("Config", "Remove Rim Lighting", false, new ConfigDescription("Remove rim lighting for all characters clothes, hair, accessories, etc. Will save modified values to the card.\n\nUse with caution as it cannot be undone except by manually resetting all the changes.", null, new ConfigurationManagerAttributes { Order = 0, IsAdvanced = true }));
#endif
            DisableShadowCastingHotkey = Config.Bind("Keyboard Shortcuts", "Disable ShadowCasting", new KeyboardShortcut(KeyCode.M, KeyCode.LeftControl), "Disable ShadowCasting for all selected items and their child items in Studio");
            EnableShadowCastingHotkey = Config.Bind("Keyboard Shortcuts", "Enable ShadowCasting", new KeyboardShortcut(KeyCode.M, KeyCode.LeftAlt), "Enable ShadowCasting for all selected items and their child items in Studio");
            ResetShadowCastingHotkey = Config.Bind("Keyboard Shortcuts", "Reset ShadowCasting", new KeyboardShortcut(KeyCode.M, KeyCode.LeftControl, KeyCode.LeftAlt), "Reset ShadowCasting for all selected items and their child items in Studio");
            DisableReceiveShadows = Config.Bind("Keyboard Shortcuts", "Disable ReceiveShadows", new KeyboardShortcut(KeyCode.N, KeyCode.LeftControl), "Disable ReceiveShadows for all selected items and their child items in Studio");
            EnableReceiveShadows = Config.Bind("Keyboard Shortcuts", "Enable ReceiveShadows", new KeyboardShortcut(KeyCode.N, KeyCode.LeftAlt), "Enable ReceiveShadows for all selected items and their child items in Studio");
            ResetReceiveShadows = Config.Bind("Keyboard Shortcuts", "Reset ReceiveShadows", new KeyboardShortcut(KeyCode.N, KeyCode.LeftControl, KeyCode.LeftAlt), "Reset ReceiveShadows for all selected items and their child items in Studio");

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

#if !PH
        /// <summary>
        /// Return a list of accessory indices for the character
        /// </summary>
        public static IEnumerable<int> GetAcccessoryIndices(ChaControl chaControl)
        {
            var accessories = chaControl.GetComponentsInChildren<ChaAccessoryComponent>();
            for (int i = 0; i < accessories.Length; i++)
            {
                var accessory = accessories[i];
                if (int.TryParse(accessory.gameObject.name.Replace("ca_slot", ""), out int index))
                    yield return index;
            }
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
#endif
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
#if PH
            yield return null;
#else
            yield return new WaitUntil(() => AssetBundleManager.ManifestBundlePack.Count != 0);
#endif

            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{nameof(KK_Plugins)}.Resources.default.xml"))
                if (stream != null)
                    using (XmlReader reader = XmlReader.Create(stream))
                        LoadXML(XDocument.Load(reader).Element("MaterialEditor"));

#if PH
            var di = new DirectoryInfo("abdata/MaterialEditor");
            if (di.Exists)
            {
                var files = di.GetFiles("*.xml", SearchOption.AllDirectories);
                for (var i = 0; i < files.Length; i++)
                {
                    var fileName = files[i].FullName;
                    try
                    {
                        XDocument doc = XDocument.Load(fileName);
                        LoadXML(doc.Element("MaterialEditor"));
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogLevel.Error, $"Failed to load {PluginNameInternal} xml file.");
                        Logger.Log(LogLevel.Error, ex);
                    }
                }
            }
#else
            var loadedManifests = Sideloader.Sideloader.Manifests;
            foreach (var manifest in loadedManifests.Values)
            {
                LoadXML(manifest.manifestDocument?.Root?.Element(PluginNameInternal));
                LoadXML(manifest.manifestDocument?.Root?.Element("MaterialEditor"));
            }
#endif
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
                var shader = LoadShader(shaderName, shaderElement.Attribute("AssetBundle")?.Value, shaderElement.Attribute("Asset")?.Value);
                LoadedShaders[shaderName] = new ShaderData(shader, shaderName, shaderElement.Attribute("RenderQueue")?.Value, shaderElement.Attribute("ShaderOptimization")?.Value);

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

        private static Shader LoadShader(string shaderName, string assetBundlePath, string assetPath)
        {
            Shader shader = null;
            if (assetBundlePath.IsNullOrEmpty())
            {
                return shader;
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
                            shader = bundle.LoadAsset<Shader>(shaderName);
                            bundle.Unload(false);
                            return shader;
                        }
                        else
                            return CommonLib.LoadAsset<Shader>(assetBundlePath, $"{shaderName}"); ;
                    }
                    catch
                    {
                        Logger.LogWarning($"Unable to load shader: {shaderName}");
                        return null;
                    }
                }
                else
                {
                    try
                    {
                        if (assetBundlePath.StartsWith("Resources."))
                        {
                            AssetBundle bundle = AssetBundle.LoadFromMemory(UILib.Resource.LoadEmbeddedResource($"{nameof(KK_Plugins)}.{assetBundlePath}"));
                            shader = bundle.LoadAsset<Shader>(shaderName);

                            var go = bundle.LoadAsset<GameObject>(assetPath);
                            var renderers = go.GetComponentsInChildren<Renderer>();
                            for (var i = 0; i < renderers.Length; i++)
                            {
                                var renderer = renderers[i];
                                for (var j = 0; j < renderer.materials.Length; j++)
                                {
                                    var material = renderer.materials[j];
                                    if (material.shader.NameFormatted() == shaderName)
                                        shader = material.shader;
                                }
                            }
                            Destroy(go);

                            bundle.Unload(false);
                            return shader;
                        }
                        else
                        {
#if PH
                            AssetBundle bundle = AssetBundle.LoadFromFile(assetBundlePath);
                            var go = bundle.LoadAsset<GameObject>(assetPath);
                            bundle.Unload(false);
#else
                            var go = CommonLib.LoadAsset<GameObject>(assetBundlePath, assetPath);
#endif
                            var renderers = go.GetComponentsInChildren<Renderer>();
                            for (var i = 0; i < renderers.Length; i++)
                            {
                                var renderer = renderers[i];
                                for (var j = 0; j < renderer.materials.Length; j++)
                                {
                                    var material = renderer.materials[j];
                                    if (material.shader.NameFormatted() == shaderName)
                                        shader = material.shader;
                                }
                            }
                            Destroy(go);
                            return shader;
                        }
                    }
                    catch
                    {
                        Logger.LogWarning($"Unable to load shader: {shaderName}");
                        return shader;
                    }
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
    }
}
