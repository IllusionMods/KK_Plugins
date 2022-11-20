using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using HarmonyLib;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using MessagePack;
using System.Collections.Generic;
using System.Linq;
using KKABMX.Core;
using UnityEngine;

namespace KK_Plugins
{
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInDependency(KKABMX_Core.GUID, KKABMX_Core.Version)]
    public partial class Pushup : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.pushup";
        public const string PluginName = "Pushup";
        public const string PluginNameInternal = Constants.Prefix + "_Pushup";
        public const string Version = "2.0";
        internal static new ManualLogSource Logger;

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
            ExtendedSave.CardBeingImported += ExtendedSave_CardBeingImported; //todo handle for new format
#endif
#if KK || KKS
            //No studio for EC
            RegisterStudioControls();
#endif
            var harmony = Harmony.CreateAndPatchAll(typeof(Hooks));
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