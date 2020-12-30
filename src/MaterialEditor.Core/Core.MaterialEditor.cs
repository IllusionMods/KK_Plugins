using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using MaterialEditorAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using UniRx;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;
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

namespace KK_Plugins.MaterialEditor
{
    /// <summary>
    /// MaterialEditor plugin base
    /// </summary>
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInDependency(XUnity.ResourceRedirector.Constants.PluginData.Identifier, XUnity.ResourceRedirector.Constants.PluginData.Version)]
#if !PH
    [BepInDependency(Sideloader.Sideloader.GUID, Sideloader.Sideloader.Version)]
#endif
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public partial class MaterialEditorPlugin : MaterialEditorAPI.MaterialEditorPluginBase
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
        public const string PluginVersion = "2.6";

#if KK || EC
        internal static ConfigEntry<bool> RimRemover { get; private set; }
#endif
        internal static ConfigEntry<KeyboardShortcut> DisableShadowCastingHotkey { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> EnableShadowCastingHotkey { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> ResetShadowCastingHotkey { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> DisableReceiveShadows { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> EnableReceiveShadows { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> ResetReceiveShadows { get; private set; }

        /// <summary>
        /// Parts of the body
        /// </summary>
#if AI || HS2
        public static HashSet<string> BodyParts = new HashSet<string> {
            "o_eyebase_L", "o_eyebase_R", "o_eyelashes", "o_eyeshadow", "o_head", "o_namida", "o_tang", "o_tooth", "o_body_cf", "o_mnpa", "o_mnpb", "cm_o_dan00", "o_tang",
            "cm_o_dan00", "o_tang", "o_silhouette_cf", "o_body_cf", "o_body_cm", "o_head" };
#elif KK || EC
        public static HashSet<string> BodyParts = new HashSet<string> {
            "cf_O_tooth", "cf_O_canine", "cf_O_tang", "o_tang", "n_tang", "n_tang_silhouette",  "cf_O_eyeline", "cf_O_eyeline_low", "cf_O_mayuge", "cf_Ohitomi_L", "cf_Ohitomi_R",
            "cf_Ohitomi_L02", "cf_Ohitomi_R02", "cf_O_noseline", "cf_O_namida_L", "cf_O_namida_M", "o_dankon", "o_gomu", "o_dan_f", "cf_O_namida_S", "cf_O_gag_eye_00", "cf_O_gag_eye_01",
            "cf_O_gag_eye_02", "o_shadowcaster", "o_shadowcaster_cm", "o_mnpa", "o_mnpb", "n_body_silhouette", "o_body_a", "cf_O_face" };
#elif PH
        public static HashSet<string> BodyParts = new HashSet<string> {
            "cf_O_eyekage1", "cf_O_ha", "cf_O_head", "cf_O_matuge", "cf_O_mayuge", "cf_O_sita", "cf_O_eyehikari_L", "cf_O_eyewhite_L", "cf_O_eyehikari_R", "cf_O_eyewhite_R",
            "cf_O_namida01", "cf_O_namida02", "cf_O_namida03", "cf_O_body_00", "cf_O_mnpk", "cf_O_mnpk_00_00", "cf_O_mnpk_00_01", "cf_O_mnpk_00_02", "cf_O_mnpk_00_03",
            "cf_O_mnpk_00_04", "cf_O_mnpk_00_05", "cf_O_nail", "cf_O_tang", "cf_O_tikubiL_00", "cf_O_tikubiR_00","cm_O_eye_L", "cm_O_eye_R", "cm_O_ha", "cm_O_head", "cm_O_mayuge",
            "cm_O_sita", "cm_O_eyeHi_R", "cm_O_eyeHi_L", "O_hige00", "O_body", "O_mnpk", "cm_O_dan00", "cm_O_dan_f", "cm_O_tang" };
#endif

        /// <summary>
        /// Parts of the body hierarchy which contains clothes and should not be traversed
        /// </summary>
#if AI || HS2
        public static HashSet<string> ClothesParts = new HashSet<string> { "ct_clothesTop", "ct_clothesBot", "ct_inner_t", "ct_inner_b", "ct_gloves", "ct_panst", "ct_socks", "ct_shoes" };
#elif KK
        public static HashSet<string> ClothesParts = new HashSet<string> { "ct_clothesTop", "ct_clothesBot", "ct_bra", "ct_shorts", "ct_gloves", "ct_panst", "ct_socks", "ct_shoes_inner", "ct_shoes_outer" };
#elif EC
        public static HashSet<string> ClothesParts = new HashSet<string> { "ct_clothesTop", "ct_clothesBot", "ct_bra", "ct_shorts", "ct_gloves", "ct_panst", "ct_socks", "ct_shoes" };
#elif PH
        public static HashSet<string> ClothesParts = new HashSet<string> { "Wears" };
#endif

#if KK || EC
        /// <summary>
        /// Parts of the mouth that need special handling
        /// </summary>
        public static HashSet<string> MouthParts = new HashSet<string> { "cf_O_tooth", "cf_O_canine", "cf_O_tang", "o_tang", "n_tang" };
#endif

        internal void Main()
        {
            MakerAPI.MakerExiting += (s, e) => MaterialEditorUI.Visible = false;
            CharacterApi.RegisterExtraBehaviour<MaterialEditorCharaController>(PluginGUID);
            AccessoriesApi.SelectedMakerAccSlotChanged += AccessoriesApi_SelectedMakerAccSlotChanged;
            AccessoriesApi.AccessoryKindChanged += AccessoriesApi_AccessoryKindChanged;
            AccessoriesApi.AccessoryTransferred += AccessoriesApi_AccessoryTransferred;
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
#if PH
            //Disable ShaderOptimization since it doesn't work properly
            ShaderOptimization.Value = false;
#endif

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
#if PH
            yield return null;
#else
            yield return new WaitUntil(() => AssetBundleManager.ManifestBundlePack.Count != 0);
#endif

            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{nameof(KK_Plugins)}.Resources.default.xml"))
                if (stream != null)
                    using (XmlReader reader = XmlReader.Create(stream))
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.Load(reader);
                        LoadXML(doc.DocumentElement);
                    }

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
                        XmlDocument doc = new XmlDocument();
                        doc.Load(fileName);
                        LoadXML(doc.DocumentElement);
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
                var element = manifest.manifestDocument?.Root?.Element("MaterialEditor");
                if (element == null)
                    element = manifest.manifestDocument?.Root?.Element(PluginNameInternal);
                if (element != null)
                {
                    //Convert XElement in to XmlElement
                    var doc = new XmlDocument();
                    doc.Load(element.CreateReader());
                    LoadXML(doc.DocumentElement);
                }
            }
#endif
        }

        private static void LoadXML(XmlElement materialEditorElement)
        {
            if (materialEditorElement == null) return;
            var shaderElements = materialEditorElement.GetElementsByTagName("Shader");
            foreach (var shaderElementObj in shaderElements)
            {
                if (shaderElementObj != null)
                {
                    var shaderElement = (XmlElement)shaderElementObj;
                    string shaderName = shaderElement.GetAttribute("Name");

                    if (LoadedShaders.ContainsKey(shaderName))
                    {
                        Destroy(LoadedShaders[shaderName].Shader);
                        LoadedShaders.Remove(shaderName);
                    }
                    var shader = LoadShader(shaderName, shaderElement.GetAttribute("AssetBundle"), shaderElement.GetAttribute("Asset"));
                    LoadedShaders[shaderName] = new ShaderData(shader, shaderName, shaderElement.GetAttribute("RenderQueue"), shaderElement.GetAttribute("ShaderOptimization"));

                    XMLShaderProperties[shaderName] = new Dictionary<string, ShaderPropertyData>();
                    var shaderPropertyElements = shaderElement.GetElementsByTagName("Property");
                    foreach (var shaderPropertyElementObj in shaderPropertyElements)
                    {
                        if (shaderPropertyElementObj != null)
                        {
                            var shaderPropertyElement = (XmlElement)shaderPropertyElementObj;

                            string propertyName = shaderPropertyElement.GetAttribute("Name");
                            ShaderPropertyType propertyType = (ShaderPropertyType)Enum.Parse(typeof(ShaderPropertyType), shaderPropertyElement.GetAttribute("Type"));
                            string defaultValue = shaderPropertyElement.GetAttribute("DefaultValue");
                            string defaultValueAB = shaderPropertyElement.GetAttribute("DefaultValueAssetBundle");
                            string range = shaderPropertyElement.GetAttribute("Range");
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
                            return CommonLib.LoadAsset<Shader>(assetBundlePath, $"{shaderName}");
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
                            bundle.Unload(false);

                            if (shader == null)
                            {
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
                            }
                            Destroy(go);

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

#if KK || EC
        public override bool CheckBlacklist(string materialName, string propertyName)
        {
            if (materialName == "cf_m_body" || materialName == "cm_m_body")
                if (propertyName == "alpha_a" || propertyName == "alpha_b" || propertyName == "AlphaMask")
                    return true;
            return false;
        }
#endif

#if PH
        /// <summary>
        /// Disable ShaderOptimization if the user enables it since it doesn't work properly
        /// </summary>
        internal override void ShaderOptimization_SettingChanged(object sender, EventArgs e)
        {
            if (ShaderOptimization.Value)
                ShaderOptimization.Value = false;
        }
#endif

        /// <summary>
        /// Get the KKAPI character controller for MaterialEditor. Provides access to methods for getting and setting material changes.
        /// </summary>
        /// <param name="chaControl"></param>
        /// <returns>KKAPI character controller</returns>
        public static MaterialEditorCharaController GetCharaController(ChaControl chaControl) => chaControl == null ? null : chaControl.gameObject.GetComponent<MaterialEditorCharaController>();
    }
}
