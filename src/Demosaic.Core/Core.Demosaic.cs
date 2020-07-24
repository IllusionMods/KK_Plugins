using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace KK_Plugins
{
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class Demosaic : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.demosaic";
        public const string PluginName = "Demosaic";
        public const string PluginNameInternal = Constants.Prefix + "_Demosaic";
        public const string Version = "1.1";

        public static ConfigEntry<bool> Enabled { get; private set; }

        internal void Main()
        {
            Enabled = Config.Bind("Settings", "Enabled", true, "Whether the plugin is enabled");
            Harmony.CreateAndPatchAll(typeof(Hooks));
        }
    }
}