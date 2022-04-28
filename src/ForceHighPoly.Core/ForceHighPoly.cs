using BepInEx;
using BepInEx.Configuration;
using KKAPI;
using System.Collections.Generic;

namespace KK_Plugins
{
    /// <summary>
    /// Replaces all _low assets with normal assets, forcing everything to load as high poly
    /// </summary>
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInDependency(UncensorSelector.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    public partial class ForceHighPoly : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.forcehighpoly";
        public const string PluginName = "Force High Poly";
        public const string PluginNameInternal = Constants.Prefix + "_ForceHighPoly";
        public const string Version = "2.1";
        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<PolyMode> PolySetting { get; private set; }

        private static HashSet<ChaControl> ForcedControls = new HashSet<ChaControl>();

        internal void Main()
        {
            var hasEnoughRam = KKAPI.Utilities.MemoryInfo.GetCurrentStatus().ullTotalPhys > 16L * 1000L * 1000L * 1000L; // At least 16GB
            PolySetting = Config.Bind("Config", "Force High Poly Models", hasEnoughRam ? PolyMode.Full : PolyMode.Partial, "Whether or not to load high poly models in the main game roaming mode (has no effect outside of that). Improves quality of characters and fixes some modded items not appearing.\nMay require exiting to main menu to take effect.\nNone: Original behavior - Some characters may throw errors and walk around with glitched clothing.\nPartial: Use high poly models only if necessary. Higher resource usage and longer load times when using some modded clothes.\nFull: Always use high poly models. Best quality but very high resource usage. At least 8GB of RAM is recommended with ~10 characters. Load times can be 3x longer.");
            PolySetting.SettingChanged += PolySetting_SettingChanged;
            Enabled = Config.Bind("Config", "High poly mode", hasEnoughRam, new ConfigDescription("Original setting kept due to external dependency, no longer used.\nUse the 'Force High Poly Models' setting instead.", null, new ConfigurationManagerAttributes() { Browsable = false }));
            var test = HarmonyLib.Harmony.CreateAndPatchAll(typeof(Hooks));
            test.PatchAll(typeof(Hooks.UncensorSelectorPatch)); //seems to transpile twice for some reason and wont transpile from above hook
        }

        /// <summary>
        /// Update Enabled when setting is changed. To maintain compatiility with other plugins
        /// </summary>
        private void PolySetting_SettingChanged(object sender, System.EventArgs e)
        {
            Enabled.Value = PolySetting.Value == PolyMode.Full;
            Config.Save();
        }

        public static bool IsHiPoly(ChaControl cha)
        {

            return cha.hiPoly || PolySetting.Value != PolyMode.None && ForcedControls.Contains(cha);
        }

        public enum PolyMode
        {
            None,
            Partial,
            Full
        }
    }
}
