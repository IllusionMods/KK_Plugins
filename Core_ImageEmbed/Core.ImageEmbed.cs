using BepInEx;
using BepInEx.Configuration;
using BepInEx.Harmony;
using BepInEx.Logging;

namespace KK_Plugins
{
    public partial class ImageEmbed
    {
        public const string GUID = "com.deathweasel.bepinex.imageembed";
        public const string PluginName = "Image Embed";
        public const string Version = "1.0";
        internal static new ManualLogSource Logger;

        public static ConfigEntry<bool> SavePattern { get; private set; }
        public static ConfigEntry<bool> SaveBG { get; private set; }

        internal void Awake()
        {
            Logger = base.Logger;

            SavePattern = Config.Bind("Config", "Save pattern images to scene data", true, new ConfigDescription("Whether images from the userdata/pattern folder will be saved to scene data. False is vanilla behavior and such images can only be loaded if the same image exists on disk.", null, new ConfigurationManagerAttributes { Order = 2 }));
            SaveBG = Config.Bind("Config", "Save BG images to scene data", true, new ConfigDescription("Whether images from the userdata/bg folder folder will be saved to scene data. False is vanilla behavior and such images can only be loaded if the same image exists on disk.", null, new ConfigurationManagerAttributes { Order = 1 }));

            HarmonyWrapper.PatchAll(typeof(Hooks));
        }
    }
}
