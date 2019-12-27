using BepInEx;
using BepInEx.Configuration;
using BepInEx.Harmony;
using BepInEx.Logging;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;

namespace KK_Plugins
{
    [BepInDependency(KoikatuAPI.GUID)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class Pushup : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.pushup";
        public const string PluginName = "Pushup";
        public const string PluginNameInternal = "KK_Pushup";
        public const string Version = "0.1";
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

        internal void Start()
        {
            Logger = base.Logger;

            ConfigEnablePushup = Config.Bind("Config", "Enable Pushup By Default", false, new ConfigDescription("Whether the pushup effect is enabled by default when a bra is worn.", null, new ConfigurationManagerAttributes { Order = 10 }));
            ConfigFirmnessDefault = Config.Bind("Config", "Firmness Default Value", 0.9f, new ConfigDescription("Firmness of the breasts. More firm means less bounce.", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 9 }));
            ConfigLiftDefault = Config.Bind("Config", "Lift Default Value", 0.6f, new ConfigDescription("Lift of the breasts. Lift is the minimum height position of the breasts when a bra is worn.", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 8 }));
            ConfigPushTogetherDefault = Config.Bind("Config", "Push Together Default Value", 0.65f, new ConfigDescription("How much the breasts will be pushed together when a bra is worn, if they are set far apart.", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 7 }));
            ConfigSqueezeDefault = Config.Bind("Config", "Squeeze Default Value", 0.6f, new ConfigDescription("Long breasts will be flattened by this amount.", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 6 }));
            ConfigNippleCenteringDefault = Config.Bind("Config", "Nipple Centering Default Value", 0.5f, new ConfigDescription("If the nipples point up or down, wearing a bra will make them point forwards.", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 5 }));
            ConfigFlattenNipplesDefault = Config.Bind("Config", "Flatten Nipples Default", true, new ConfigDescription("Flatten nipples while a bra is worn.", null, new ConfigurationManagerAttributes { Order = 4 }));
            ConfigSliderMin = Config.Bind("Config", "Advanced Mode Slider Minimum", -100, new ConfigDescription("Minimum value of advanced mode sliders.", new AcceptableValueRange<int>(-500, 0), new ConfigurationManagerAttributes { Order = 3 }));
            ConfigSliderMax = Config.Bind("Config", "Advanced Mode Slider Maximum", 200, new ConfigDescription("Maximum value of advanced mode sliders.", new AcceptableValueRange<int>(100, 500), new ConfigurationManagerAttributes { Order = 2 }));

            CharacterApi.RegisterExtraBehaviour<PushupController>(GUID);
            MakerAPI.RegisterCustomSubCategories += RegisterCustomSubCategories;
            MakerAPI.ReloadCustomInterface += (sender, args) => ReLoadPushUp();
            MakerAPI.MakerExiting += MakerExiting;
            MakerAPI.MakerFinishedLoading += MakerFinishedLoading;
            RegisterStudioControls();

            HarmonyWrapper.PatchAll(typeof(Hooks));
        }

        public static PushupController GetCharaController(ChaControl character) => character?.gameObject?.GetComponent<PushupController>();

        internal enum Wearing { None, Bra, Top, Both }
    }
}