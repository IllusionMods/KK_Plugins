using BepInEx;
using BepInEx.Configuration;
using BepInEx.Harmony;
using HarmonyLib;
#if AI
using AIChara;
#endif

namespace KK_Plugins
{
    public partial class Demosaic : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.demosaic";
        public const string PluginName = "Demosaic";
        public const string Version = "1.1";

        private static bool _enabled = true;

        public static ConfigEntry<bool> Enabled { get; private set; }

        internal void Main()
        {
            Enabled = Config.AddSetting("Settings", "Enabled", true, "Whether the plugin is enabled");
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
