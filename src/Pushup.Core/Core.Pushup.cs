using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using HarmonyLib;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KK_Plugins
{
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInDependency(ExtendedSave.GUID, ExtendedSave.Version)]
    [BepInDependency("com.deathweasel.bepinex.moreoutfits", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class Pushup : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.pushup";
        public const string PluginName = "Pushup";
        public const string PluginNameInternal = Constants.Prefix + "_Pushup";
        public const string Version = "1.5.1";
        internal static new ManualLogSource Logger;

        private static Type MoreOutfitsType;
        public static ConfigEntry<bool> ConfigEnablePushup { get; private set; }
        public static ConfigEntry<float> ConfigFirmnessDefault { get; private set; }
        public static ConfigEntry<float> ConfigLiftDefault { get; private set; }
        public static ConfigEntry<float> ConfigPushTogetherDefault { get; private set; }
        public static ConfigEntry<float> ConfigSqueezeDefault { get; private set; }
        public static ConfigEntry<float> ConfigNippleCenteringDefault { get; private set; }
        public static ConfigEntry<bool> ConfigFlattenNipplesDefault { get; private set; }
        public static ConfigEntry<int> ConfigSliderMin { get; private set; }
        public static ConfigEntry<int> ConfigSliderMax { get; private set; }

        private void Start()
        {
            Logger = base.Logger;

            ConfigEnablePushup = Config.Bind("Config", "Enable Pushup By Default", false, new ConfigDescription("Whether the pushup effect is enabled by default when a bra is worn.", null, new ConfigurationManagerAttributes { Order = 10 }));
            ConfigFirmnessDefault = Config.Bind("Config", "Firmness Default Value", 0.9f, new ConfigDescription("Firmness of the breasts. More firm means less bounce.", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 9 }));
            ConfigLiftDefault = Config.Bind("Config", "Lift Default Value", 0.6f, new ConfigDescription("Lift of the breasts. Lift is the minimum height position of the breasts when a bra is worn.", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 8 }));
            ConfigPushTogetherDefault = Config.Bind("Config", "Push Together Default Value", 0.55f, new ConfigDescription("How much the breasts will be pushed together when a bra is worn, if they are set far apart.", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 7 }));
            ConfigSqueezeDefault = Config.Bind("Config", "Squeeze Default Value", 0.6f, new ConfigDescription("Long breasts will be flattened by this amount.", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 6 }));
            ConfigNippleCenteringDefault = Config.Bind("Config", "Nipple Centering Default Value", 0.5f, new ConfigDescription("If the nipples point up or down, wearing a bra will make them point forwards.", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 5 }));
            ConfigFlattenNipplesDefault = Config.Bind("Config", "Flatten Nipples Default", true, new ConfigDescription("Flatten nipples while a bra is worn.", null, new ConfigurationManagerAttributes { Order = 4 }));
            ConfigSliderMin = Config.Bind("Config", "Advanced Mode Slider Minimum", -100, new ConfigDescription("Minimum value of advanced mode sliders.", new AcceptableValueRange<int>(-500, 0), new ConfigurationManagerAttributes { Order = 3 }));
            ConfigSliderMax = Config.Bind("Config", "Advanced Mode Slider Maximum", 200, new ConfigDescription("Maximum value of advanced mode sliders.", new AcceptableValueRange<int>(100, 500), new ConfigurationManagerAttributes { Order = 2 }));

            CharacterApi.RegisterExtraBehaviour<PushupController>(GUID);
            MakerAPI.RegisterCustomSubCategories += RegisterCustomSubCategories;
            MakerAPI.ReloadCustomInterface += ReloadCustomInterface;
            MakerAPI.MakerExiting += MakerExiting;
            MakerAPI.MakerFinishedLoading += MakerFinishedLoading;
#if EC || KKS
            ExtendedSave.CardBeingImported += ExtendedSave_CardBeingImported;
#endif
#if KK || KKS
            //No studio for EC
            RegisterStudioControls();
            KKAPI.Studio.StudioAPI.StudioLoadedChanged += StudioInterfaceInitialised;
#endif
            var harmony = Harmony.CreateAndPatchAll(typeof(Hooks));

            //Patch all the slider onValueChanged events to return false and cancel original code
            //Pushup adds its own onValueChanged event that manages this stuff
            for (var i = 0; i < typeof(ChaCustom.CvsBreast).GetNestedTypes(AccessTools.all).Length; i++)
            {
                var anonType = typeof(ChaCustom.CvsBreast).GetNestedTypes(AccessTools.all)[i];
                if (anonType.Name.Contains("<Start>"))
                    for (var index = 0; index < anonType.GetMethods(AccessTools.all).Length; index++)
                    {
                        var anonTypeMethod = anonType.GetMethods(AccessTools.all)[index];
                        if (anonTypeMethod.Name.Contains("<>m"))
                            if (anonTypeMethod.GetParameters().Any(x => x.ParameterType == typeof(float)))
                                harmony.Patch(anonTypeMethod, new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.SliderHook), AccessTools.all)));
                    }
            }

            var sliders = typeof(ChaCustom.CvsBreast).GetMethods(AccessTools.all).Where(x => x.Name.Contains("<Start>") && x.GetParameters().Any(y => y.ParameterType == typeof(float)));
            //Don't patch areola size or nipple gloss since they are not managed by this plugin
            foreach (var slider in sliders)
            {
                if (Application.productName == Constants.MainGameProcessName)
                {
                    if (slider.Name == "<Start>m__E") { }//areola size
                    else if (slider.Name == "<Start>m__14") { }//nipple gloss
                    else
                        harmony.Patch(slider, new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.SliderHook), AccessTools.all)));
                }
#if KK
                //EC sliders match non-Steam version of KK
                else if (Application.productName == Constants.MainGameProcessNameSteam)
                {
                    if (slider.Name == "<Start>m__10") { }//areola size
                    else if (slider.Name == "<Start>m__17") { }//nipple gloss
                    else
                        harmony.Patch(slider, new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.SliderHook), AccessTools.all)));
                }
#endif
            }
#if !EC
            MoreOutfitsType = Type.GetType($"KK_Plugins.MoreOutfits.Plugin, {Constants.Prefix}_MoreOutfits", throwOnError: false);
            if (MoreOutfitsType != null)
                PatchMoreOutfits();

            void PatchMoreOutfits()
            {
                harmony.Patch(
                    MoreOutfitsType.GetMethod("AddCoordinateSlot", AccessTools.all),
                    new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.CoordinateCountChangedPostHook), AccessTools.all))
                );
                harmony.Patch(
                    MoreOutfitsType.GetMethod("RemoveCoordinateSlot", AccessTools.all),
                    new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.CoordinateCountChangedPostHook), AccessTools.all))
                );
            }
#endif
        }

#if EC
        private void ExtendedSave_CardBeingImported(Dictionary<string, PluginData> importedExtendedData)
        {
            if (importedExtendedData.TryGetValue(GUID, out var pluginData))
            {
                if (pluginData != null)
                {
                    Dictionary<int, ClothData> braDataDictionary = new Dictionary<int, ClothData>();
                    Dictionary<int, ClothData> topDataDictionary = new Dictionary<int, ClothData>();
                    BodyData baseData = null;

                    if (pluginData != null && pluginData.data.TryGetValue(PushupConstants.Pushup_BraData, out var loadedBraData) && loadedBraData != null)
                        braDataDictionary = MessagePackSerializer.Deserialize<Dictionary<int, ClothData>>((byte[])loadedBraData);
                    if (pluginData != null && pluginData.data.TryGetValue(PushupConstants.Pushup_TopData, out var loadedTopData) && loadedTopData != null)
                        topDataDictionary = MessagePackSerializer.Deserialize<Dictionary<int, ClothData>>((byte[])loadedTopData);
                    if (pluginData != null && pluginData.data.TryGetValue(PushupConstants.Pushup_BodyData, out var loadedBodyData) && loadedBodyData != null)
                        baseData = MessagePackSerializer.Deserialize<BodyData>((byte[])loadedBodyData);

                    //Remove all data except for the first outfit
                    List<int> keysToRemove = new List<int>();

                    foreach (var entry in braDataDictionary)
                        if (entry.Key != 0)
                            keysToRemove.Add(entry.Key);
                    foreach (var key in keysToRemove)
                        braDataDictionary.Remove(key);

                    foreach (var entry in topDataDictionary)
                        if (entry.Key != 0)
                            keysToRemove.Add(entry.Key);
                    foreach (var key in keysToRemove)
                        topDataDictionary.Remove(key);

                    if (braDataDictionary.Count == 0 && topDataDictionary.Count == 00)
                    {
                        importedExtendedData.Remove(GUID);
                    }
                    else
                    {
                        var data = new PluginData();
                        data.data.Add(PushupConstants.Pushup_BraData, MessagePackSerializer.Serialize(braDataDictionary));
                        data.data.Add(PushupConstants.Pushup_TopData, MessagePackSerializer.Serialize(topDataDictionary));
                        data.data.Add(PushupConstants.Pushup_BodyData, MessagePackSerializer.Serialize(baseData));
                        importedExtendedData[GUID] = data;
                    }
                }
            }
        }
#elif KKS
        private void ExtendedSave_CardBeingImported(Dictionary<string, PluginData> importedExtendedData, Dictionary<int, int?> coordinateMapping)
        {
            if (importedExtendedData.TryGetValue(GUID, out var pluginData))
            {
                if (pluginData != null)
                {
                    Dictionary<int, ClothData> braDataDictionary = new Dictionary<int, ClothData>();
                    Dictionary<int, ClothData> topDataDictionary = new Dictionary<int, ClothData>();
                    Dictionary<int, ClothData> braDataDictionaryNew = new Dictionary<int, ClothData>();
                    Dictionary<int, ClothData> topDataDictionaryNew = new Dictionary<int, ClothData>();

                    BodyData baseData = null;

                    if (pluginData != null && pluginData.data.TryGetValue(PushupConstants.Pushup_BraData, out var loadedBraData) && loadedBraData != null)
                        braDataDictionary = MessagePackSerializer.Deserialize<Dictionary<int, ClothData>>((byte[])loadedBraData);
                    if (pluginData != null && pluginData.data.TryGetValue(PushupConstants.Pushup_TopData, out var loadedTopData) && loadedTopData != null)
                        topDataDictionary = MessagePackSerializer.Deserialize<Dictionary<int, ClothData>>((byte[])loadedTopData);
                    if (pluginData != null && pluginData.data.TryGetValue(PushupConstants.Pushup_BodyData, out var loadedBodyData) && loadedBodyData != null)
                        baseData = MessagePackSerializer.Deserialize<BodyData>((byte[])loadedBodyData);

                    foreach (var entry in braDataDictionary)
                    {
                        if (coordinateMapping.TryGetValue(entry.Key, out int? newIndex) && newIndex != null)
                        {
                            braDataDictionaryNew[(int)newIndex] = entry.Value;
                        }
                    }

                    foreach (var entry in topDataDictionary)
                    {
                        if (coordinateMapping.TryGetValue(entry.Key, out int? newIndex) && newIndex != null)
                        {
                            topDataDictionaryNew[(int)newIndex] = entry.Value;
                        }
                    }

                    if (braDataDictionaryNew.Count == 0 && topDataDictionaryNew.Count == 00)
                    {
                        importedExtendedData.Remove(GUID);
                    }
                    else
                    {
                        var data = new PluginData();
                        data.data.Add(PushupConstants.Pushup_BraData, MessagePackSerializer.Serialize(braDataDictionaryNew));
                        data.data.Add(PushupConstants.Pushup_TopData, MessagePackSerializer.Serialize(topDataDictionaryNew));
                        data.data.Add(PushupConstants.Pushup_BodyData, MessagePackSerializer.Serialize(baseData));
                        importedExtendedData[GUID] = data;
                    }
                }
            }
        }
#endif

        public static PushupController GetCharaController(ChaControl character) => character == null ? null : character.gameObject.GetComponent<PushupController>();

        internal enum Wearing { None, Bra, Top, Both }
    }
}
