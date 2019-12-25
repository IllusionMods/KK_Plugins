using BepInEx;
using BepInEx.Configuration;
using BepInEx.Harmony;
using BepInEx.Logging;
using KKAPI;
using KKAPI.Chara;

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

        internal void Start()
        {
            Logger = base.Logger;

            ConfigEnablePushup = Config.Bind("Config", "Enable Pushup By Default", false, new ConfigDescription("Whether the pushup effect is enabled by default when a bra is worn.", null, new ConfigurationManagerAttributes { Order = 3 }));
            ConfigFirmnessDefault = Config.Bind("Config", "Firmness Default Value", 0.9f, new ConfigDescription("Firmness of the breasts. More firm means less bounce.", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 3 }));
            ConfigLiftDefault = Config.Bind("Config", "Lift Default Value", 0.6f, new ConfigDescription("Lift of the breasts. Lift is the minimum height position of the breasts when a bra is worn.", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 3 }));
            ConfigPushTogetherDefault = Config.Bind("Config", "Push Together Default Value", 0.65f, new ConfigDescription("How much the breasts will be pushed together when a bra is worn, if they are set far apart.", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 3 }));
            ConfigSqueezeDefault = Config.Bind("Config", "Squeeze Default Value", 0.6f, new ConfigDescription("Long breasts will be flattened by this amount.", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 3 }));
            ConfigNippleCenteringDefault = Config.Bind("Config", "Nipple Centering Default Value", 0.5f, new ConfigDescription("If the nipples point up or down, wearing a bra will make them point forwards.", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 3 }));
            ConfigFlattenNipplesDefault = Config.Bind("Config", "Flatten Nipples Default", false, new ConfigDescription("Flatten nipples while a bra is worn.", null, new ConfigurationManagerAttributes { Order = 3 }));

            CharacterApi.RegisterExtraBehaviour<PushupController>(GUID);
            HarmonyWrapper.PatchAll(typeof(Hooks));
        }

        public static PushupController GetCharaController(ChaControl character) => character?.gameObject?.GetComponent<PushupController>();

        internal enum Wearing { None, Bra, Top, Both }

        internal class PushupConstants
        {
            internal const int IndexSize = 4;
            internal const int IndexVerticalPosition = 5;
            internal const int IndexHorizontalAngle = 6;
            internal const int IndexHorizontalPosition = 7;
            internal const int IndexVerticalAngle = 8;
            internal const int IndexDepth = 9;
            internal const int IndexRoundness = 10;
            internal const int IndexAreolaDepth = 11;
            internal const int IndexNippleWidth = 12;
            internal const int IndexNippleDepth = 13;
        }
    }
}