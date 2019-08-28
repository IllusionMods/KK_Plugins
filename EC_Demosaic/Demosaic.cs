using BepInEx;
using BepInEx.Configuration;
using BepInEx.Harmony;
using HarmonyLib;

namespace KK_Plugins
{
    [BepInPlugin(GUID, PluginName, Version)]
    public class Demosaic : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.demosaic";
        public const string PluginName = "Demosaic";
        public const string Version = "1.1";

        private static bool _enabled = true;

        public static ConfigWrapper<bool> Enabled { get; private set; }

        private void Main()
        {
            Enabled = Config.GetSetting("Settings", "Enabled", true, new ConfigDescription("Whether the plugin is enabled"));
            Enabled.SettingChanged += Enabled_SettingChanged;
            _enabled = Enabled.Value;
            HarmonyWrapper.PatchAll(typeof(Demosaic));
        }

        private void Enabled_SettingChanged(object sender, System.EventArgs e) => _enabled = Enabled.Value;

        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), "LateUpdateForce")]
        public static void LateUpdateForce(ChaControl __instance)
        {
            if (_enabled)
                __instance.hideMoz = true;
        }
    }
}
