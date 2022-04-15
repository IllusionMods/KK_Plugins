using BepInEx;
using BepInEx.Configuration;
using KKAPI;

namespace KK_Plugins
{
    /// <summary>
    /// Replaces all _low assets with normal assets, forcing everything to load as high poly
    /// </summary>
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    public partial class ForceHighPoly : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.forcehighpoly";
        public const string PluginName = "Force High Poly";
        public const string PluginNameInternal = Constants.Prefix + "_ForceHighPoly";
        public const string Version = "1.2.2";

        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<bool> PartialPoly { get; private set; }

        internal void Main()
        {
            var hasEnoughRam = KKAPI.Utilities.MemoryInfo.GetCurrentStatus().ullTotalPhys > 16L * 1000L * 1000L * 1000L; // At least 16GB
            Enabled = Config.Bind("Config", "High poly mode", hasEnoughRam, "Whether or not to load high poly assets. Improves quality of characters in roaming mode and fixes some modded items not appearing.\nMay require exiting to main menu to take effect. This option has high memory requirements, at least 8GB of RAM is recommended (with ~10 characters).");
            PartialPoly = Config.Bind("Config", "Partial High poly mode", true, "Use High Poly Assets on Characters who wear clothing that lack low poly models and prevents the resulting errors.\nMay require exiting to main menu to take effect. This option has Lower memory requirements, at least 8GB of RAM is recommended (with ~10 characters).");

            HarmonyLib.Harmony.CreateAndPatchAll(typeof(Hooks));
        }
    }
}
