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
        public static ConfigEntry<PolyMode> PolySetting { get; private set; }
        internal void Main()
        {
            var hasEnoughRam = KKAPI.Utilities.MemoryInfo.GetCurrentStatus().ullTotalPhys > 16L * 1000L * 1000L * 1000L; // At least 16GB
            PolySetting = Config.Bind("Config", "Set High Poly Mode State", hasEnoughRam ? PolyMode.Full : PolyMode.Partial, "Whether or not to load high poly assets. Improves quality of characters in roaming mode and fixes some modded items not appearing.\nMay require exiting to main menu to take effect.\nNone: disable plugin\n\tSome characters may throw errors and walk around without clothing.\nPartial: Loads Characters with High Poly if clothing lack a low poly model at a loading time cost\n\tLower Requirments than Full.\nFull: This option has high memory requirements, at least 8GB of RAM is recommended (with ~10 characters).");
            PolySetting.SettingChanged += PolySetting_SettingChanged;
            Enabled = Config.Bind("Config", "High poly mode", hasEnoughRam, new ConfigDescription("Original Setting: kept due to externnal dependency\nWhether or not to load high poly assets. Improves quality of characters in roaming mode and fixes some modded items not appearing.\nMay require exiting to main menu to take effect. This option has high memory requirements, at least 8GB of RAM is recommended (with ~10 characters).", null, new ConfigurationManagerAttributes() { Browsable = false }));
            Enabled.SettingChanged += Enabled_SettingChanged;
            HarmonyLib.Harmony.CreateAndPatchAll(typeof(Hooks));
        }


        private static bool LoopCheck = false;// stops having to click partial twice when swapping out

        /// <summary>
        /// Update PolySetting when setting is changed.
        /// </summary>
        private void Enabled_SettingChanged(object sender, System.EventArgs e)
        {
            if (LoopCheck) return;
            LoopCheck = true;
            PolySetting.Value = (Enabled.Value) ? PolyMode.Full : PolyMode.None;
            Config.Save();
            LoopCheck = false;
        }

        /// <summary>
        /// Update Enabled when setting is changed. To maintain compatiility with other plugins
        /// </summary>
        private void PolySetting_SettingChanged(object sender, System.EventArgs e)
        {
            if (LoopCheck) return;
            LoopCheck = true;
            Enabled.Value = PolySetting.Value == PolyMode.Full;
            Config.Save();
            LoopCheck = false;
        }

        public enum PolyMode
        {
            None,
            Partial,
            Full
        }
    }
}
