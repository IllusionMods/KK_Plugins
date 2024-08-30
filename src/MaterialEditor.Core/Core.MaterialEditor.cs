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
    [BepInDependency(ExtendedSave.GUID, ExtendedSave.Version)]
#if !PH
    [BepInDependency(Sideloader.Sideloader.GUID, Sideloader.Sideloader.Version)]
#elif KK || KKS
    [BepInDependency("com.deathweasel.bepinex.moreoutfits", BepInDependency.DependencyFlags.SoftDependency)]
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
        public const string PluginVersion = "3.9.2.2";

        /// <summary>
        /// Material which is used in normal map conversion
        /// </summary>
        private static Material NormalMapConvertMaterial;
        private static Material NormalMapOpenGLConvertMaterial;

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
            if(moreOutfitsType != null)
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

        private void LoadNormalMapConverter()
        {
            AssetBundle bundle = AssetBundle.LoadFromMemory(UILib.Resource.LoadEmbeddedResource($"{nameof(KK_Plugins)}.Resources.normal_convert.unity3d"));
            var shader = bundle.LoadAsset<Shader>("normal_convert");
            var shader_opengl = bundle.LoadAsset<Shader>("normal_convert_opengl");
            NormalMapConvertMaterial = new Material(shader);
            NormalMapOpenGLConvertMaterial = new Material(shader_opengl);
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

        protected override Texture ConvertNormalMap(Texture tex)
        {
            var material = NormalMapConvertMaterial;
            if (IsUncompressedNormalMap(tex))
                material = NormalMapOpenGLConvertMaterial;
            RenderTexture rt = new RenderTexture(tex.width, tex.height, 0);
            rt.useMipMap = true;
            rt.autoGenerateMips = true;
            Graphics.Blit(tex, rt, material);
            rt.wrapMode = tex.wrapMode;

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
    }
}