using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using HarmonyLib;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using MaterialEditorAPI;
using MessagePack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using UniRx;
using UnityEngine;
using XUnity.ResourceRedirector;
using System.Text.RegularExpressions;
using static MaterialEditorAPI.MaterialAPI;
using KKAPI.Utilities;
using BepInEx.Bootstrap;
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
#if !EC
using KKAPI.Studio;
#endif

namespace KK_Plugins.MaterialEditor
{
    /// <summary>
    /// MaterialEditor plugin base
    /// </summary>
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInDependency(XUnity.ResourceRedirector.Constants.PluginData.Identifier, XUnity.ResourceRedirector.Constants.PluginData.Version)]
    [BepInDependency(ExtendedSave.GUID, ExtendedSave.Version)]
#if !PH
    [BepInDependency(Sideloader.Sideloader.GUID, Sideloader.Sideloader.Version)]
#elif KK || KKS
    [BepInDependency("com.deathweasel.bepinex.moreoutfits", BepInDependency.DependencyFlags.SoftDependency)]
#endif
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public partial class MaterialEditorPlugin : MaterialEditorPluginBase
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
        public const string PluginVersion = "3.13.5";


        /// <summary>
        /// Material which is used in normal map conversion
        /// </summary>
        private static Material NormalMapConvertMaterial;
        private static Material NormalMapOpenGLConvertMaterial;
        private static Material NormalMapUnpackDXT5Material;

#if KK || EC || KKS
        internal static ConfigEntry<bool> RimRemover { get; private set; }
#endif
#if EC || KKS
        internal static ConfigEntry<bool> ConfigConvertNormalMaps { get; private set; }
#endif
        internal static ConfigEntry<KeyboardShortcut> DisableShadowCastingHotkey { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> EnableShadowCastingHotkey { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> TwoSidedShadowCastingHotkey { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> ShadowsOnlyShadowCastingHotkey { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> ResetShadowCastingHotkey { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> DisableReceiveShadows { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> EnableReceiveShadows { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> ResetReceiveShadows { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> PasteEditsHotkey { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> PurgeOrphanedPropertiesHotkey { get; private set; }

        internal static ConfigEntry<bool> RendererCachingEnabled { get; private set; }

#if !EC
        internal static ConfigEntry<SceneTextureSaveType> TextureSaveTypeScene { get; private set; }
        internal static ConfigEntry<string> TextureSaveTypeSceneAuto { get; private set; }
#endif
        internal static ConfigEntry<CharaTextureSaveType> TextureSaveTypeChara { get; private set; }
        internal static ConfigEntry<string> TextureSaveTypeCharaAuto { get; private set; }

        /// <summary>
        /// Parts of the body
        /// </summary>
#if AI || HS2
        public static HashSet<string> BodyParts = new HashSet<string> {
            "o_eyebase_L", "o_eyebase_R", "o_eyelashes", "o_eyeshadow", "o_head", "o_namida", "o_tang", "o_tooth", "o_body_cf", "o_mnpa", "o_mnpb", "cm_o_dan00", "o_tang",
            "cm_o_dan00", "o_tang", "o_silhouette_cf", "o_body_cf", "o_body_cm", "o_head" };
#elif KK || EC || KKS
        public static HashSet<string> BodyParts = new HashSet<string> {
            "cf_O_tooth", "cf_O_canine", "cf_O_tang", "o_tang", "n_tang", "n_tang_silhouette",  "cf_O_eyeline", "cf_O_eyeline_low", "cf_O_mayuge", "cf_O_mayuge2", "cf_Ohitomi_L", "cf_Ohitomi_R",
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
#elif KK || KKS
        public static HashSet<string> ClothesParts = new HashSet<string> { "ct_clothesTop", "ct_clothesBot", "ct_bra", "ct_shorts", "ct_gloves", "ct_panst", "ct_socks", "ct_shoes_inner", "ct_shoes_outer" };
#elif EC
        public static HashSet<string> ClothesParts = new HashSet<string> { "ct_clothesTop", "ct_clothesBot", "ct_bra", "ct_shorts", "ct_gloves", "ct_panst", "ct_socks", "ct_shoes" };
#elif PH
        public static HashSet<string> ClothesParts = new HashSet<string> { "Wears" };
#endif

#if KK || EC || KKS
        /// <summary>
        /// Parts of the mouth that need special handling
        /// </summary>
        public static HashSet<string> MouthParts = new HashSet<string> { "cf_O_tooth", "cf_O_canine", "cf_O_tang", "o_tang", "n_tang" };
        public static HashSet<string> EyeMaterials = new HashSet<string> { "cf_m_eyeline_00_up", "cf_m_eyeline_kage", "cf_m_eyeline_down", "cf_m_sirome_00", "cf_m_hitomi_00", "cf_m_mayuge_00" };

#elif AI || HS2
        /// <summary>
        /// Parts of the mouth that need special handling
        /// </summary>
        public static HashSet<string> MouthParts = new HashSet<string> { "o_tooth", "o_tang" };
#endif

        // Do not change, lest you break all existing local cards
        public const string LocalTexPrefix = "ME_LocalTex_";
        public const string LocalTexSavePreFix = "LOCAL_";
        public const string DedupedTexSavePreFix = "DEDUPED_";
        public const string DedupedTexSavePostFix = "_DATA";
        public const string LocalTexUnusedFolder = "_Unused";

        // Local texture audit screen variables
        internal static int auditAllFiles = 0;
        internal static int auditProcessedFiles = 0;
        internal static int auditRunningThread = 0;
        internal static Dictionary<string, string> auditUnusedTextures = null;
        internal static Dictionary<string, List<string>> auditMissingTextures = null;
        internal static Dictionary<string, List<string>> auditFoundHashToFiles = new Dictionary<string, List<string>>();
        internal static object auditLock = new object();
        internal static bool auditShow = false;
        internal static Coroutine auditDoneCoroutine = null;
        internal static Rect auditRect = new Rect();
        internal static Vector2 auditUnusedScroll = Vector2.zero;
        internal static Vector2 auditMissingScroll = Vector2.zero;
        private static GUIStyle _auditLabel = null;
        internal static GUIStyle AuditLabel
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
        internal static GUIStyle AuditButton
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
        internal static GUIStyle AuditWindow
        {
            get
            {
                if (_auditWindow == null)
                {
#if PH
                    _auditWindow = new GUIStyle(GUI.skin.window);
#else
                    _auditWindow = new GUIStyle(IMGUIUtils.SolidBackgroundGuiSkin.window);
#endif
                }
                return _auditWindow;
            }
        }
        private static GUIStyle _auditBigText = null;
        internal static GUIStyle AuditBigText
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
        internal static GUIStyle AuditWarnButton
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

        public override void Awake()
        {
            base.Awake();

            //Load any image loading dependencies before any images are actually ever loaded
            ImageHelper.LoadDependencies(typeof(MaterialEditorPlugin));

#if KK || EC || KKS
            RimRemover = Config.Bind("Config", "Remove Rim Lighting", false, new ConfigDescription("Remove rim lighting for all characters clothes, hair, accessories, etc. Will save modified values to the card.\n\nUse with caution as it cannot be undone except by manually resetting all the changes.", null, new ConfigurationManagerAttributes { Order = 0, IsAdvanced = true }));
#endif
#if EC || KKS
            ConfigConvertNormalMaps = Config.Bind("Config", "Convert Normal Maps", true, new ConfigDescription("Convert grey normal maps to red normal maps for compatibility with Koikatsu mods.", null, new ConfigurationManagerAttributes { Order = 1 }));
#endif
            DisableShadowCastingHotkey = Config.Bind("Keyboard Shortcuts", "Disable ShadowCasting", new KeyboardShortcut(KeyCode.M, KeyCode.LeftControl), "Disable ShadowCasting for all selected items and their child items in Studio, or everything except body renderers in Maker");
            EnableShadowCastingHotkey = Config.Bind("Keyboard Shortcuts", "Enable ShadowCasting", new KeyboardShortcut(KeyCode.M, KeyCode.LeftAlt), "Enable ShadowCasting for all selected items and their child items in Studio, or everything except body renderers in Maker");
            TwoSidedShadowCastingHotkey = Config.Bind("Keyboard Shortcuts", "Two Sided ShadowCasting", new KeyboardShortcut(KeyCode.K, KeyCode.LeftAlt), "Set ShadowCasting to 'Two Sided' for all selected items and their child items in Studio, or everything except body renderers in Maker");
            ShadowsOnlyShadowCastingHotkey = Config.Bind("Keyboard Shortcuts", "Shadows Only ShadowCasting", new KeyboardShortcut(KeyCode.L, KeyCode.LeftAlt), "Set ShadowCasting to 'Shadows Only' for all selected items and their child items in Studio, or everything except body renderers in Maker");
            ResetShadowCastingHotkey = Config.Bind("Keyboard Shortcuts", "Reset ShadowCasting", new KeyboardShortcut(KeyCode.M, KeyCode.LeftControl, KeyCode.LeftAlt), "Reset ShadowCasting for all selected items and their child items in Studio, or everything except body renderers in Maker");
            DisableReceiveShadows = Config.Bind("Keyboard Shortcuts", "Disable ReceiveShadows", new KeyboardShortcut(KeyCode.N, KeyCode.LeftControl), "Disable ReceiveShadows for all selected items and their child items in Studio, or everything except body renderers in Maker");
            EnableReceiveShadows = Config.Bind("Keyboard Shortcuts", "Enable ReceiveShadows", new KeyboardShortcut(KeyCode.N, KeyCode.LeftAlt), "Enable ReceiveShadows for all selected items and their child items in Studio, or everything except body renderers in Maker");
            ResetReceiveShadows = Config.Bind("Keyboard Shortcuts", "Reset ReceiveShadows", new KeyboardShortcut(KeyCode.N, KeyCode.LeftControl, KeyCode.LeftAlt), "Reset ReceiveShadows for all selected items and their child items in Studio, or everything except body renderers in Maker");
            PasteEditsHotkey = Config.Bind("Keyboard Shortcuts", "Paste Edits", new KeyboardShortcut(KeyCode.N), "Paste any copied edits for all selected items and their child items in Studio");
            PurgeOrphanedPropertiesHotkey = Config.Bind("Keyboard Shortcuts", "Purge Orphaned Properties", new KeyboardShortcut(KeyCode.R, KeyCode.LeftShift, KeyCode.LeftControl), "Remove any properties no longer associated with anything on the current outfit");
#if PH
            //Disable ShaderOptimization since it doesn't work properly
            ShaderOptimization.Value = false;
#endif
            RendererCachingEnabled = Config.Bind("Config", "Renderer Cache", true, "Turning this off will fix cache related issues but may have a negative impact on performance.");

            // Texture saving configs
            ConfigLocalTexturePath = Config.Bind("Textures", "Local Texture Path Override", "", new ConfigDescription($"Local textures will be exported to / imported from this folder. If empty, defaults to {LocalTexturePathDefault}.\nWARNING: If you change this, make sure to move all files to the new path!", null, new ConfigurationManagerAttributes { Order = 10, IsAdvanced = true }));
            ConfigLocalTexturePath.SettingChanged += ConfigLocalTexturePath_SettingChanged;

            CharaLocalTextures.Activate();
            TextureSaveTypeChara = Config.Bind("Textures", "Chara Texture Save Type", CharaLocalTextures.SaveType, new ConfigDescription("Texture save type for characters set in the Modding API.", null, new ConfigurationManagerAttributes { Order = 7, IsAdvanced = true, CustomDrawer = new Action<ConfigEntryBase>(KKAPISettingDrawer<CharaTextureSaveType>), HideDefaultButton = true }));
            TextureSaveTypeCharaAuto = Config.Bind("Textures", "Chara Autosave Type Override", "-", new ConfigDescription("Save type override for autosaves. Set to \"-\" to disable the override.", AutoSaveTypeOptions(false), new ConfigurationManagerAttributes { Order = 6, IsAdvanced = true }));
            CharaLocalTextures.SaveTypeChangedEvent += (x, y) => { TextureSaveTypeChara.Value = y.NewSetting; };
#if !EC
            SceneLocalTextures.Activate();
            TextureSaveTypeScene = Config.Bind("Textures", "Scene Texture Save Type", SceneLocalTextures.SaveType, new ConfigDescription("Texture save type for scenes set in the Modding API.", null, new ConfigurationManagerAttributes { Order = 5, IsAdvanced = true, CustomDrawer = new Action<ConfigEntryBase>(KKAPISettingDrawer<SceneTextureSaveType>), HideDefaultButton = true }));
            TextureSaveTypeSceneAuto = Config.Bind("Textures", "Scene Autosave Type Override", "-", new ConfigDescription("Save type override for autosaves. Set to \"-\" to disable the override.", AutoSaveTypeOptions(true), new ConfigurationManagerAttributes { Order = 4, IsAdvanced = true }));
            SceneLocalTextures.SaveTypeChangedEvent += (x, y) => { TextureSaveTypeScene.Value = y.NewSetting; };
#endif
            Config.Bind("Textures", "Audit Local Files", 0, new ConfigDescription("Parse all character / scene files and check for missing or unused local files. Takes a long times if you have many cards and scenes.", null, new ConfigurationManagerAttributes
            {
                CustomDrawer = new Action<ConfigEntryBase>(AuditOptionDrawer),
                Order = 0,
                HideDefaultButton = true,
                IsAdvanced = true, 
            }));
#if !PH
            MakerCardSave.RegisterNewCardSavePathModifier(null, AddLocalPrefixToCard);
#endif
        }

        internal void Main()
        {
            MakerAPI.MakerExiting += (s, e) => MaterialEditorUI.Visible = false;
            CharacterApi.RegisterExtraBehaviour<MaterialEditorCharaController>(PluginGUID);
            AccessoriesApi.SelectedMakerAccSlotChanged += AccessoriesApi_SelectedMakerAccSlotChanged;
            AccessoriesApi.AccessoryKindChanged += AccessoriesApi_AccessoryKindChanged;
            AccessoriesApi.AccessoryTransferred += AccessoriesApi_AccessoryTransferred;
#if KK || KKS
            AccessoriesApi.AccessoriesCopied += AccessoriesApi_AccessoriesCopied;
#endif
#if EC
            ExtensibleSaveFormat.ExtendedSave.CardBeingImported += ExtendedSave_CardBeingImported;
            ExtensibleSaveFormat.ExtendedSave.CoordinateBeingImported += ExtendedSave_CoordinateBeingImported;
#elif KKS
            ExtensibleSaveFormat.ExtendedSave.CardBeingImported += ExtendedSave_CardBeingImported;
#endif
            var harmony = Harmony.CreateAndPatchAll(typeof(Hooks));

#if KK || EC
            //Hooks for transferring accessories (MoreAccessories compatibility)
            foreach (var method in typeof(ChaCustom.CvsAccessoryChange).GetMethods(AccessTools.all).Where(x => x.Name.Contains("<Start>m__4")))
                harmony.Patch(method, new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.AccessoryTransferHook), AccessTools.all)));
#elif KKS
            foreach (var method in typeof(ChaCustom.CvsAccessoryChange).GetMethods(AccessTools.all).Where(x => x.Name.Contains("<Start>b__25_4")))
                harmony.Patch(method, new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.AccessoryTransferHook), AccessTools.all)));
#elif AI || HS2
            //Hooks for changing clothing pattern
            foreach (var method in typeof(CharaCustom.CustomClothesPatternSelect).GetMethods(AccessTools.all).Where(x => x.Name.Contains("<ChangeLink>")))
                harmony.Patch(method, new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.ClothesColorChangeHook), AccessTools.all)));

            //hooks for changing clothing color
            foreach (var method in typeof(CharaCustom.CustomClothesColorSet).GetMethods(AccessTools.all).Where(x => x.Name.StartsWith("<Initialize>")))
                harmony.Patch(method, new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.ClothesColorChangeHook), AccessTools.all)));
#endif

            //Hooks for changing uncensors in maker and studio
            var uncensorSelectorType = Type.GetType($"KK_Plugins.UncensorSelector, {Constants.Prefix}_UncensorSelector");
            if (uncensorSelectorType != null)
            {
                var method = uncensorSelectorType.GetMethod("BodyDropdownChanged", AccessTools.all);
                if (method != null)
                    harmony.Patch(method, new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.UncensorSelectorHook), AccessTools.all)));
                method = uncensorSelectorType.GetMethod("PenisDropdownChanged", AccessTools.all);
                if (method != null)
                    harmony.Patch(method, new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.UncensorSelectorHook), AccessTools.all)));
                method = uncensorSelectorType.GetMethod("BallsDropdownChanged", AccessTools.all);
                if (method != null)
                    harmony.Patch(method, new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.UncensorSelectorHook), AccessTools.all)));

                method = uncensorSelectorType.GetMethod("BodyDropdownChangedStudio", AccessTools.all);
                if (method != null)
                    harmony.Patch(method, new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.UncensorSelectorHookStudio), AccessTools.all)));
                method = uncensorSelectorType.GetMethod("PenisDropdownChangedStudio", AccessTools.all);
                if (method != null)
                    harmony.Patch(method, new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.UncensorSelectorHookStudio), AccessTools.all)));
                method = uncensorSelectorType.GetMethod("BallsDropdownChangedStudio", AccessTools.all);
                if (method != null)
                    harmony.Patch(method, new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.UncensorSelectorHookStudio), AccessTools.all)));
            }

#if KK || KKS
            //Hook to delete properties of an outfit that gets removed
            var moreOutfitsType = Type.GetType($"KK_Plugins.MoreOutfits.Plugin, {Constants.Prefix}_MoreOutfits");
            if (moreOutfitsType != null)
            {
                var method = moreOutfitsType.GetMethod("RemoveCoordinateSlot", AccessTools.all);
                if (method != null)
                    harmony.Patch(method, postfix: new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.RemoveCoordinateSlotHook), AccessTools.all)));
            }
#endif

            StartCoroutine(LoadXML());
            StartCoroutine(GetUncensorSelectorParts());

#if KK || EC || KKS
            NormalMapProperties.Add("NormalMap");
            NormalMapProperties.Add("NormalMapDetail");
            NormalMapProperties.Add("BumpMap");
#endif
            LoadNormalMapConverter();
        }

        internal virtual void ConfigLocalTexturePath_SettingChanged(object sender, EventArgs e)
        {
            SetLocalTexturePath();
        }

        private void SetLocalTexturePath()
        {
            if (ConfigLocalTexturePath.Value == "")
                LocalTexturePath = LocalTexturePathDefault;
            else
                LocalTexturePath = ConfigExportPath.Value;
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
#if KK || KKS
        private static void AccessoriesApi_AccessoriesCopied(object sender, AccessoryCopyEventArgs e)
        {
            var controller = GetCharaController(MakerAPI.GetCharacterControl());
            if (controller != null)
                controller.AccessoriesCopiedEvent(sender, e);
        }
#endif

#if EC
        private void ExtendedSave_CardBeingImported(Dictionary<string, ExtensibleSaveFormat.PluginData> importedExtendedData)
        {
            if (importedExtendedData.TryGetValue(PluginGUID, out var data))
            {
                if (data.data.TryGetValue("RendererPropertyList", out var rendererProperties) && rendererProperties != null)
                {
                    var properties = MessagePackSerializer.Deserialize<List<MaterialEditorCharaController.RendererProperty>>((byte[])rendererProperties);
                    properties.RemoveAll(x => x.CoordinateIndex != 0); //Remove all but the first coordinate

                    properties.RemoveAll(x => x.ObjectType == MaterialEditorCharaController.ObjectType.Clothing && x.Slot == 7); //Remove indoor shoes
                    for (int i = 0; i < properties.Count; i++)
                    {
                        var property = properties[i];
                        if (property.Slot == 8)//Change slot index for outdoor shoes to the one used by EC
                            property.Slot = 7;
                    }

                    if (properties.Count > 0)
                        data.data["RendererPropertyList"] = MessagePackSerializer.Serialize(properties);
                    else
                        data.data["RendererPropertyList"] = null;
                }

                if (data.data.TryGetValue("MaterialFloatPropertyList", out var floatProperties) && floatProperties != null)
                {
                    var properties = MessagePackSerializer.Deserialize<List<MaterialEditorCharaController.MaterialFloatProperty>>((byte[])floatProperties);
                    properties.RemoveAll(x => x.CoordinateIndex != 0); //Remove all but the first coordinate

                    properties.RemoveAll(x => x.ObjectType == MaterialEditorCharaController.ObjectType.Clothing && x.Slot == 7); //Remove indoor shoes
                    for (int i = 0; i < properties.Count; i++)
                    {
                        var property = properties[i];
                        if (property.Slot == 8)//Change slot index for outdoor shoes to the one used by EC
                            property.Slot = 7;
                    }

                    if (properties.Count > 0)
                        data.data["MaterialFloatPropertyList"] = MessagePackSerializer.Serialize(properties);
                    else
                        data.data["MaterialFloatPropertyList"] = null;
                }
                if (data.data.TryGetValue("MaterialColorPropertyList", out var colorProperties) && colorProperties != null)
                {
                    var properties = MessagePackSerializer.Deserialize<List<MaterialEditorCharaController.MaterialColorProperty>>((byte[])colorProperties);
                    properties.RemoveAll(x => x.CoordinateIndex != 0); //Remove all but the first coordinate

                    properties.RemoveAll(x => x.ObjectType == MaterialEditorCharaController.ObjectType.Clothing && x.Slot == 7); //Remove indoor shoes
                    for (int i = 0; i < properties.Count; i++)
                    {
                        var property = properties[i];
                        if (property.Slot == 8)//Change slot index for outdoor shoes to the one used by EC
                            property.Slot = 7;
                    }

                    if (properties.Count > 0)
                        data.data["MaterialColorPropertyList"] = MessagePackSerializer.Serialize(properties);
                    else
                        data.data["MaterialColorPropertyList"] = null;
                }
                if (data.data.TryGetValue("MaterialTexturePropertyList", out var textureProperties) && textureProperties != null)
                {
                    var properties = MessagePackSerializer.Deserialize<List<MaterialEditorCharaController.MaterialTextureProperty>>((byte[])textureProperties);
                    properties.RemoveAll(x => x.CoordinateIndex != 0); //Remove all but the first coordinate

                    properties.RemoveAll(x => x.ObjectType == MaterialEditorCharaController.ObjectType.Clothing && x.Slot == 7); //Remove indoor shoes
                    for (int i = 0; i < properties.Count; i++)
                    {
                        var property = properties[i];
                        if (property.Slot == 8)//Change slot index for outdoor shoes to the one used by EC
                            property.Slot = 7;
                    }

                    if (properties.Count > 0)
                        data.data["MaterialTexturePropertyList"] = MessagePackSerializer.Serialize(properties);
                    else
                        data.data["MaterialTexturePropertyList"] = null;
                }
                if (data.data.TryGetValue("MaterialShaderList", out var shaderProperties) && shaderProperties != null)
                {
                    var properties = MessagePackSerializer.Deserialize<List<MaterialEditorCharaController.MaterialShader>>((byte[])shaderProperties);
                    properties.RemoveAll(x => x.CoordinateIndex != 0); //Remove all but the first coordinate

                    properties.RemoveAll(x => x.ObjectType == MaterialEditorCharaController.ObjectType.Clothing && x.Slot == 7); //Remove indoor shoes
                    for (int i = 0; i < properties.Count; i++)
                    {
                        var property = properties[i];
                        if (property.Slot == 8)//Change slot index for outdoor shoes to the one used by EC
                            property.Slot = 7;
                    }

                    if (properties.Count > 0)
                        data.data["MaterialShaderList"] = MessagePackSerializer.Serialize(properties);
                    else
                        data.data["MaterialShaderList"] = null;
                }
                if (data.data.TryGetValue("MaterialCopyList", out var copyProperties) && copyProperties != null)
                {
                    var properties = MessagePackSerializer.Deserialize<List<MaterialEditorCharaController.MaterialCopy>>((byte[])copyProperties);
                    properties.RemoveAll(x => x.CoordinateIndex != 0); //Remove all but the first coordinate

                    properties.RemoveAll(x => x.ObjectType == MaterialEditorCharaController.ObjectType.Clothing && x.Slot == 7); //Remove indoor shoes
                    for (int i = 0; i < properties.Count; i++)
                    {
                        var property = properties[i];
                        if (property.Slot == 8)//Change slot index for outdoor shoes to the one used by EC
                            property.Slot = 7;
                    }

                    if (properties.Count > 0)
                        data.data["MaterialCopyList"] = MessagePackSerializer.Serialize(properties);
                    else
                        data.data["MaterialCopyList"] = null;
                }
            }
        }
#elif KKS
        private void ExtendedSave_CardBeingImported(Dictionary<string, ExtensibleSaveFormat.PluginData> importedExtendedData, Dictionary<int, int?> coordinateMapping)
        {
            if (importedExtendedData.TryGetValue(PluginGUID, out var data))
            {
                if (data.data.TryGetValue("RendererPropertyList", out var rendererProperties) && rendererProperties != null)
                {
                    List<MaterialEditorCharaController.RendererProperty> properties = MessagePackSerializer.Deserialize<List<MaterialEditorCharaController.RendererProperty>>((byte[])rendererProperties);
                    List<MaterialEditorCharaController.RendererProperty> propertiesNew = new List<MaterialEditorCharaController.RendererProperty>();

                    foreach (var property in properties)
                    {
                        if (property.ObjectType == MaterialEditorCharaController.ObjectType.Accessory || property.ObjectType == MaterialEditorCharaController.ObjectType.Clothing)
                        {
                            if (coordinateMapping.TryGetValue(property.CoordinateIndex, out int? newIndex) && newIndex != null)
                            {
                                property.CoordinateIndex = (int)newIndex;
                                propertiesNew.Add(property);
                            }
                        }
                        else
                        {
                            propertiesNew.Add(property);
                        }
                    }

                    if (propertiesNew.Count > 0)
                        data.data["RendererPropertyList"] = MessagePackSerializer.Serialize(propertiesNew);
                    else
                        data.data["RendererPropertyList"] = null;
                }

                if (data.data.TryGetValue("MaterialFloatPropertyList", out var floatProperties) && floatProperties != null)
                {
                    List<MaterialEditorCharaController.MaterialFloatProperty> properties = MessagePackSerializer.Deserialize<List<MaterialEditorCharaController.MaterialFloatProperty>>((byte[])floatProperties);
                    List<MaterialEditorCharaController.MaterialFloatProperty> propertiesNew = new List<MaterialEditorCharaController.MaterialFloatProperty>();

                    foreach (var property in properties)
                    {
                        if (property.ObjectType == MaterialEditorCharaController.ObjectType.Accessory || property.ObjectType == MaterialEditorCharaController.ObjectType.Clothing)
                        {
                            if (coordinateMapping.TryGetValue(property.CoordinateIndex, out int? newIndex) && newIndex != null)
                            {
                                property.CoordinateIndex = (int)newIndex;
                                propertiesNew.Add(property);
                            }
                        }
                        else
                        {
                            propertiesNew.Add(property);
                        }
                    }

                    if (propertiesNew.Count > 0)
                        data.data["MaterialFloatPropertyList"] = MessagePackSerializer.Serialize(propertiesNew);
                    else
                        data.data["MaterialFloatPropertyList"] = null;
                }
                if (data.data.TryGetValue("MaterialColorPropertyList", out var colorProperties) && colorProperties != null)
                {
                    List<MaterialEditorCharaController.MaterialColorProperty> properties = MessagePackSerializer.Deserialize<List<MaterialEditorCharaController.MaterialColorProperty>>((byte[])colorProperties);
                    List<MaterialEditorCharaController.MaterialColorProperty> propertiesNew = new List<MaterialEditorCharaController.MaterialColorProperty>();

                    foreach (var property in properties)
                    {
                        if (property.ObjectType == MaterialEditorCharaController.ObjectType.Accessory || property.ObjectType == MaterialEditorCharaController.ObjectType.Clothing)
                        {
                            if (coordinateMapping.TryGetValue(property.CoordinateIndex, out int? newIndex) && newIndex != null)
                            {
                                property.CoordinateIndex = (int)newIndex;
                                propertiesNew.Add(property);
                            }
                        }
                        else
                        {
                            propertiesNew.Add(property);
                        }
                    }

                    if (propertiesNew.Count > 0)
                        data.data["MaterialColorPropertyList"] = MessagePackSerializer.Serialize(propertiesNew);
                    else
                        data.data["MaterialColorPropertyList"] = null;
                }
                if (data.data.TryGetValue("MaterialTexturePropertyList", out var textureProperties) && textureProperties != null)
                {
                    List<MaterialEditorCharaController.MaterialTextureProperty> properties = MessagePackSerializer.Deserialize<List<MaterialEditorCharaController.MaterialTextureProperty>>((byte[])textureProperties);
                    List<MaterialEditorCharaController.MaterialTextureProperty> propertiesNew = new List<MaterialEditorCharaController.MaterialTextureProperty>();

                    foreach (var property in properties)
                    {
                        if (property.ObjectType == MaterialEditorCharaController.ObjectType.Accessory || property.ObjectType == MaterialEditorCharaController.ObjectType.Clothing)
                        {
                            if (coordinateMapping.TryGetValue(property.CoordinateIndex, out int? newIndex) && newIndex != null)
                            {
                                property.CoordinateIndex = (int)newIndex;
                                propertiesNew.Add(property);
                            }
                        }
                        else
                        {
                            propertiesNew.Add(property);
                        }
                    }

                    if (propertiesNew.Count > 0)
                        data.data["MaterialTexturePropertyList"] = MessagePackSerializer.Serialize(propertiesNew);
                    else
                        data.data["MaterialTexturePropertyList"] = null;
                }
                if (data.data.TryGetValue("MaterialShaderList", out var shaderProperties) && shaderProperties != null)
                {
                    List<MaterialEditorCharaController.MaterialShader> properties = MessagePackSerializer.Deserialize<List<MaterialEditorCharaController.MaterialShader>>((byte[])shaderProperties);
                    List<MaterialEditorCharaController.MaterialShader> propertiesNew = new List<MaterialEditorCharaController.MaterialShader>();

                    foreach (var property in properties)
                    {
                        if (property.ObjectType == MaterialEditorCharaController.ObjectType.Accessory || property.ObjectType == MaterialEditorCharaController.ObjectType.Clothing)
                        {
                            if (coordinateMapping.TryGetValue(property.CoordinateIndex, out int? newIndex) && newIndex != null)
                            {
                                property.CoordinateIndex = (int)newIndex;
                                propertiesNew.Add(property);
                            }
                        }
                        else
                        {
                            propertiesNew.Add(property);
                        }
                    }

                    if (propertiesNew.Count > 0)
                        data.data["MaterialShaderList"] = MessagePackSerializer.Serialize(propertiesNew);
                    else
                        data.data["MaterialShaderList"] = null;
                }
                if (data.data.TryGetValue("MaterialCopyList", out var copyProperties) && copyProperties != null)
                {
                    List<MaterialEditorCharaController.MaterialCopy> properties = MessagePackSerializer.Deserialize<List<MaterialEditorCharaController.MaterialCopy>>((byte[])copyProperties);
                    List<MaterialEditorCharaController.MaterialCopy> propertiesNew = MessagePackSerializer.Deserialize<List<MaterialEditorCharaController.MaterialCopy>>((byte[])copyProperties);

                    foreach (var property in properties)
                    {
                        if (property.ObjectType == MaterialEditorCharaController.ObjectType.Accessory || property.ObjectType == MaterialEditorCharaController.ObjectType.Clothing)
                        {
                            if (coordinateMapping.TryGetValue(property.CoordinateIndex, out int? newIndex) && newIndex != null)
                            {
                                property.CoordinateIndex = (int)newIndex;
                                propertiesNew.Add(property);
                            }
                        }
                        else
                        {
                            propertiesNew.Add(property);
                        }
                    }

                    if (propertiesNew.Count > 0)
                        data.data["MaterialCopyList"] = MessagePackSerializer.Serialize(propertiesNew);
                    else
                        data.data["MaterialCopyList"] = null;
                }
            }
        }
#endif

#if EC
        private void ExtendedSave_CoordinateBeingImported(Dictionary<string, ExtensibleSaveFormat.PluginData> importedExtendedData)
        {
            if (importedExtendedData.TryGetValue(PluginGUID, out var data))
            {
                if (data.data.TryGetValue("RendererPropertyList", out var rendererProperties) && rendererProperties != null)
                {
                    var properties = MessagePackSerializer.Deserialize<List<MaterialEditorCharaController.RendererProperty>>((byte[])rendererProperties);
                    properties.RemoveAll(x => x.ObjectType == MaterialEditorCharaController.ObjectType.Clothing && x.Slot == 7); //Remove indoor shoes
                    for (int i = 0; i < properties.Count; i++)
                    {
                        var property = properties[i];
                        if (property.Slot == 8)//Change slot index for outdoor shoes to the one used by EC
                            property.Slot = 7;
                    }

                    if (properties.Count > 0)
                        data.data["RendererPropertyList"] = MessagePackSerializer.Serialize(properties);
                    else
                        data.data["RendererPropertyList"] = null;
                }

                if (data.data.TryGetValue("MaterialFloatPropertyList", out var floatProperties) && floatProperties != null)
                {
                    var properties = MessagePackSerializer.Deserialize<List<MaterialEditorCharaController.MaterialFloatProperty>>((byte[])floatProperties);
                    properties.RemoveAll(x => x.ObjectType == MaterialEditorCharaController.ObjectType.Clothing && x.Slot == 7); //Remove indoor shoes
                    for (int i = 0; i < properties.Count; i++)
                    {
                        var property = properties[i];
                        if (property.Slot == 8)//Change slot index for outdoor shoes to the one used by EC
                            property.Slot = 7;
                    }

                    if (properties.Count > 0)
                        data.data["MaterialFloatPropertyList"] = MessagePackSerializer.Serialize(properties);
                    else
                        data.data["MaterialFloatPropertyList"] = null;
                }
                if (data.data.TryGetValue("MaterialColorProperty", out var colorProperties) && colorProperties != null)
                {
                    var properties = MessagePackSerializer.Deserialize<List<MaterialEditorCharaController.MaterialColorProperty>>((byte[])colorProperties);
                    properties.RemoveAll(x => x.ObjectType == MaterialEditorCharaController.ObjectType.Clothing && x.Slot == 7); //Remove indoor shoes
                    for (int i = 0; i < properties.Count; i++)
                    {
                        var property = properties[i];
                        if (property.Slot == 8)//Change slot index for outdoor shoes to the one used by EC
                            property.Slot = 7;
                    }

                    if (properties.Count > 0)
                        data.data["MaterialColorProperty"] = MessagePackSerializer.Serialize(properties);
                    else
                        data.data["MaterialColorProperty"] = null;
                }
                if (data.data.TryGetValue("MaterialTexturePropertyList", out var textureProperties) && textureProperties != null)
                {
                    var properties = MessagePackSerializer.Deserialize<List<MaterialEditorCharaController.MaterialTextureProperty>>((byte[])textureProperties);
                    properties.RemoveAll(x => x.ObjectType == MaterialEditorCharaController.ObjectType.Clothing && x.Slot == 7); //Remove indoor shoes
                    for (int i = 0; i < properties.Count; i++)
                    {
                        var property = properties[i];
                        if (property.Slot == 8)//Change slot index for outdoor shoes to the one used by EC
                            property.Slot = 7;
                    }

                    if (properties.Count > 0)
                        data.data["MaterialTexturePropertyList"] = MessagePackSerializer.Serialize(properties);
                    else
                        data.data["MaterialTexturePropertyList"] = null;
                }
                if (data.data.TryGetValue("MaterialShaderList", out var shaderProperties) && shaderProperties != null)
                {
                    var properties = MessagePackSerializer.Deserialize<List<MaterialEditorCharaController.MaterialShader>>((byte[])shaderProperties);
                    properties.RemoveAll(x => x.ObjectType == MaterialEditorCharaController.ObjectType.Clothing && x.Slot == 7); //Remove indoor shoes
                    for (int i = 0; i < properties.Count; i++)
                    {
                        var property = properties[i];
                        if (property.Slot == 8)//Change slot index for outdoor shoes to the one used by EC
                            property.Slot = 7;
                    }

                    if (properties.Count > 0)
                        data.data["MaterialShaderList"] = MessagePackSerializer.Serialize(properties);
                    else
                        data.data["MaterialShaderList"] = null;
                }
                if (data.data.TryGetValue("MaterialCopyList", out var copyProperties) && copyProperties != null)
                {
                    var properties = MessagePackSerializer.Deserialize<List<MaterialEditorCharaController.MaterialCopy>>((byte[])copyProperties);
                    properties.RemoveAll(x => x.ObjectType == MaterialEditorCharaController.ObjectType.Clothing && x.Slot == 7); //Remove indoor shoes
                    for (int i = 0; i < properties.Count; i++)
                    {
                        var property = properties[i];
                        if (property.Slot == 8)//Change slot index for outdoor shoes to the one used by EC
                            property.Slot = 7;
                    }

                    if (properties.Count > 0)
                        data.data["MaterialCopyList"] = MessagePackSerializer.Serialize(properties);
                    else
                        data.data["MaterialCopyList"] = null;
                }
            }
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
                var element = manifest.ManifestDocument?.Root?.Element("MaterialEditor");
                if (element == null)
                    element = manifest.ManifestDocument?.Root?.Element(PluginNameInternal);
                if (element != null)
                {
                    //Convert XElement in to XmlElement
                    var doc = new XmlDocument();
                    doc.Load(element.CreateReader());
                    LoadXML(doc.DocumentElement);
                }
            }
#endif
            RefreshPropertyOrganization();
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
                    if (shader != null && shader.name != shaderName)
                        XMLShaderProperties[shader.name] = new Dictionary<string, ShaderPropertyData>();

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
                            string anisoLevel = shaderPropertyElement.GetAttribute("AnisoLevel");
                            string filterMode = shaderPropertyElement.GetAttribute("FilterMode");
                            string wrapMode = shaderPropertyElement.GetAttribute("WrapMode");
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
                            string hidden = shaderPropertyElement.GetAttribute("Hidden");
                            string category = shaderPropertyElement.GetAttribute("Category");

                            ShaderPropertyData shaderPropertyData = new ShaderPropertyData(
                                propertyName, propertyType,
                                defaultValue, defaultValueAB,
                                anisoLevel, filterMode, wrapMode,
                                min, max,
                                hidden, category
                            );

                            XMLShaderProperties["default"][propertyName] = shaderPropertyData;
                            XMLShaderProperties[shaderName][propertyName] = shaderPropertyData;
                            if (shader != null && shader.name != shaderName)
                                XMLShaderProperties[shader.name][propertyName] = shaderPropertyData;
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

#if KK || EC || KKS
        public override bool CheckBlacklist(string materialName, string propertyName)
        {
            if (propertyName == "alpha_a" || propertyName == "alpha_b") return true;

            if (Regex.IsMatch(materialName, @"^c[mf]_m_body(" + Regex.Escape(MaterialCopyPostfix) + @"\d+)?$") && propertyName == "AlphaMask") return true;

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

        private void LoadNormalMapConverter()
        {
            AssetBundle bundle = AssetBundle.LoadFromMemory(UILib.Resource.LoadEmbeddedResource($"{nameof(KK_Plugins)}.Resources.normal_convert.unity3d"));
            var shader = bundle.LoadAsset<Shader>("normal_convert");
            var shader_opengl = bundle.LoadAsset<Shader>("normal_convert_opengl");
            var unpack_shader = bundle.LoadAsset<Shader>("unpack_normal");
            NormalMapConvertMaterial = new Material(shader);
            NormalMapOpenGLConvertMaterial = new Material(shader_opengl);
            NormalMapUnpackDXT5Material = new Material(unpack_shader);
        }

#if EC || KKS
        protected override void AssetLoadedHook(AssetLoadedContext context)
        {
            if (!ShaderOptimization.Value && !ConfigConvertNormalMaps.Value)
                return;

            if (context.Asset is GameObject go)
            {
                var renderers = go.GetComponentsInChildren<Renderer>();
                for (var i = 0; i < renderers.Length; i++)
                {
                    var renderer = renderers[i];
                    for (var j = 0; j < renderer.materials.Length; j++)
                    {
                        var material = renderer.materials[j];

                        ReplaceShaders(material);
                        ConvertNormalMaps(material);
                    }
                }
                var projectors = go.GetComponentsInChildren<Projector>();
                foreach (var projector in projectors)
                    ReplaceShaders(projector.material);
            }
            else if (context.Asset is Material material)
            {
                ReplaceShaders(material);
                ConvertNormalMaps(material);
            }
            else if (context.Asset is Shader shader)
            {
                if (ShaderOptimization.Value)
                {
                    string shaderName = shader.name;

                    if (LoadedShaders.TryGetValue(shaderName, out var shaderData) && shaderData.Shader != null && shaderData.ShaderOptimization)
                        context.Asset = shaderData.Shader;
                }
            }
        }

        private static void ReplaceShaders(Material material)
        {
            if (!ShaderOptimization.Value)
                return;

            string shaderName = material.shader.name;

            if (LoadedShaders.TryGetValue(shaderName, out var shaderData) && shaderData.Shader != null && shaderData.ShaderOptimization)
            {
                int renderQueue = material.renderQueue;
                material.shader = shaderData.Shader;
                material.renderQueue = renderQueue;
            }
        }

        /// <summary>
        /// Convert normal maps from grey to red for all normal maps on the material
        /// </summary>
        private static void ConvertNormalMaps(Material material)
        {
            if (!ConfigConvertNormalMaps.Value)
                return;

            for (int i = 0; i < NormalMapProperties.Count; i++)
                if (material.HasProperty($"_{NormalMapProperties[i]}"))
                    ConvertNormalMap(material, NormalMapProperties[i]);
        }

        /// <summary>
        /// Convert a normal map texture from grey to red by setting the entire red color channel to white
        /// </summary>
        internal static void ConvertNormalMap(Material material, string propertyName)
        {
            if (!NormalMapProperties.Contains(propertyName))
                return;

            if (material.HasProperty($"_{propertyName}"))
            {
                var tex = material.GetTexture($"_{propertyName}");
                if (tex != null)
                    if (Instance.ConvertNormalMap(ref tex, propertyName))
                        material.SetTexture($"_{propertyName}", tex);
            }
        }
#endif

        internal static int DetermineSaveType()
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

        internal static void SaveDeduped(PluginData data, string key, Dictionary<int, TextureContainer> dict)
        {
            HashSet<long> hashes = new HashSet<long>();
            Dictionary<int, string> dicKeyToHash = new Dictionary<int, string>();
            Dictionary<string, byte[]> dicHashToData = new Dictionary<string, byte[]>();
            foreach (var kvp in dict)
            {
                var texKey = kvp.Value._token.key;
                string hashString = texKey.hash.ToString("X16");
                hashes.Add(texKey.hash);
                dicKeyToHash.Add(kvp.Key, hashString);
                dicHashToData.Add(hashString, texKey.data);
            }

            foreach (var controller in MaterialEditorCharaController.charaControllers)
            {
                var controllerKeys = controller.TextureDictionary.Values.Select(x => x._token.key);
                foreach (var controllerKey in controllerKeys)
                    if (!hashes.Contains(controllerKey.hash))
                    {
                        hashes.Add(controllerKey.hash);
                        dicHashToData.Add(controllerKey.hash.ToString("X16"), controllerKey.data);
                    }
            }

            data.data.Add(key, MessagePackSerializer.Serialize(dicKeyToHash));
            data.data.Add(key + DedupedTexSavePostFix, MessagePackSerializer.Serialize(dicHashToData));
        }

        internal static void SaveLocally(PluginData data, string key, Dictionary<int, TextureContainer> dict)
        {
            if (!Directory.Exists(LocalTexturePath))
                Directory.CreateDirectory(LocalTexturePath);

            var hashDict = dict.ToDictionary(pair => pair.Key, pair => pair.Value._token.key.hash.ToString("X16"));
            foreach (var kvp in hashDict)
            {
                string fileName = LocalTexPrefix + kvp.Value + "." + ImageTypeIdentifier.Identify(dict[kvp.Key].Data);
                string filePath = Path.Combine(LocalTexturePath, fileName);
                if (!File.Exists(filePath))
                    File.WriteAllBytes(filePath, dict[kvp.Key].Data);
            }

            data.data.Add(key, MessagePackSerializer.Serialize(hashDict));
        }

        internal static byte[] LoadLocally(string hash)
        {
            if (!Directory.Exists(LocalTexturePath))
            {
                Logger.LogMessage("[MaterialEditor] Local texture directory doesn't exist, can't load texture!");
                return new byte[0];
            }

            string searchPattern = LocalTexPrefix + hash + ".*";
            string[] files = Directory.GetFiles(LocalTexturePath, searchPattern, SearchOption.TopDirectoryOnly);
            if (files == null || files.Length == 0)
            {
                Logger.LogMessage($"[MaterialEditor] No local texture found with hash {hash}!");
                return new byte[0];
            }
            if (files.Length > 1)
            {
                Logger.LogMessage($"[MaterialEditor] Multiple local textures found with hash {hash}, aborting!");
                return new byte[0];
            }

            return File.ReadAllBytes(files[0]);
        }

#if !PH
        private static string AddLocalPrefixToCard(string current)
        {
            if (!IsAutoSave() && TextureSaveTypeChara.Value == CharaTextureSaveType.Local)
                return LocalTexSavePreFix + current;
            return current;
        }
#endif

        internal void AuditOptionDrawer(ConfigEntryBase configEntry)
        {
            if (GUILayout.Button("Audit Local Files", GUILayout.ExpandWidth(true)))
            {
                AuditLocalFiles();
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

        internal void KKAPISettingDrawer<T>(ConfigEntryBase configEntry) where T : Enum
        {
            GUILayout.Label(((T)configEntry.BoxedValue).ToString(), GUILayout.ExpandWidth(true));
        }

        internal static void AuditLocalFiles()
        {
            if (!Directory.Exists(LocalTexturePath))
            {
                Logger.LogMessage("[MaterialEditor] Local texture directory doesn't exist, nothing to clean up!");
                return;
            }

            string[] localTexFolderFiles = Directory.GetFiles(LocalTexturePath, LocalTexPrefix + "*", SearchOption.TopDirectoryOnly);
            if (localTexFolderFiles.Length == 0)
            {
                Logger.LogMessage("[MaterialEditor] No local textures found!");
                return;
            }

            auditUnusedTextures = new Dictionary<string, string>();
            foreach (string file in localTexFolderFiles)
                auditUnusedTextures.Add(Regex.Match(file, "(?<=_)[A-F0-9]{16}(?=.)").Value, file.Split(Path.DirectorySeparatorChar).Last());

            var pngs = new List<string>();
            pngs.AddRange(Directory.GetFiles(Path.Combine(Paths.GameRootPath, @"UserData\chara"), "*.png", SearchOption.AllDirectories));
            pngs.AddRange(Directory.GetFiles(Path.Combine(Paths.GameRootPath, @"UserData\Studio\scene"), "*.png", SearchOption.AllDirectories));
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
                ThreadingHelper.Instance.StartAsyncInvoke(delegate
                {
                    AuditLocalFilesProcessor(pngs, numThreads, nowOffset);
                    --auditRunningThread;
                    return null;
                });
            }
        }

        internal static void AuditLocalFilesProcessor(List<string> pngs, int period, int offset)
        {
            lock (Logger)
                Logger.LogDebug($"Starting new local file processor with period {period} and offset {offset}!");

            string searchStringStart = LocalTexSavePreFix + nameof(MaterialEditorCharaController.TextureDictionary);
            byte[] searchStringStartBytes = System.Text.Encoding.ASCII.GetBytes(searchStringStart);
            string searchStringEnd = nameof(MaterialEditorCharaController.RendererPropertyList);
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
                        while(true)
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
            lock (Logger)
                Logger.LogDebug($"Local file processor with offset {offset} done!");
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
                } else
                {
                    at = 0;
                }
            }
            return -1;
        }

        internal static IEnumerator AuditLocalFilesDone()
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

        private void OnGUI()
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

        private void AuditWindowFunction(int windowID)
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

        protected override Texture ConvertNormalMap(Texture tex, bool unpack = false)
        {
            var material = NormalMapConvertMaterial;
            if (unpack)
            {
                MaterialEditorPluginBase.Logger.LogInfo("Unpacking Normal");
                material = NormalMapUnpackDXT5Material;
            }
            else if (IsUncompressedNormalMap(tex))
                material = NormalMapOpenGLConvertMaterial;
            RenderTexture rt = new RenderTexture(tex.width, tex.height, 0);
            rt.useMipMap = true;
            rt.autoGenerateMips = true;
            Graphics.Blit(tex, rt, material);
            rt.wrapMode = tex.wrapMode;
            rt.anisoLevel = tex.anisoLevel;
            rt.filterMode = tex.filterMode;

            return rt;
        }

        /// <summary>
        /// Get the KKAPI character controller for MaterialEditor. Provides access to methods for getting and setting material changes.
        /// </summary>
        /// <param name="chaControl"></param>
        /// <returns>KKAPI character controller</returns>
        public static MaterialEditorCharaController GetCharaController(ChaControl chaControl) => chaControl == null ? null : chaControl.gameObject.GetComponent<MaterialEditorCharaController>();

        /// <summary>
        /// Clears all GameObjects from the Renderer Cache.
        /// </summary>
        public static void ClearCache()
        {
            Hooks.ClearCache();
        }

        /// <summary>
        /// Clears a specific GameObject from the RendererCache.
        /// </summary>
        /// <param name="gameObject"></param>
        public static void ClearCache(GameObject gameObject)
        {
            Hooks.ClearCache(gameObject);
        }

        internal static AcceptableValueBase AutoSaveTypeOptions(bool forStudio)
        {
            var options = new List<string> { "-" };
            if (forStudio)
            {
#if !EC
                options.AddRange(((SceneTextureSaveType[])Enum.GetValues(typeof(SceneTextureSaveType))).Select(x => x.ToString()));
#endif
            }
            else
            {
                options.AddRange(((CharaTextureSaveType[])Enum.GetValues(typeof(CharaTextureSaveType))).Select(x => x.ToString()));
            }
            return new AcceptableValueList<string>(options.ToArray());
        }
    }
}
