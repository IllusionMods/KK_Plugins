using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Studio;

namespace KK_Plugins
{
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class PoseQuickLoad : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.posequickload";
        public const string PluginName = "Pose Quick Load";
        public const string PluginNameInternal = Constants.Prefix + "_PoseQuickLoad";
        public const string Version = "1.0";
        internal static new ManualLogSource Logger;

        public static ConfigEntry<bool> Enabled { get; private set; }

        internal void Main()
        {
            Logger = base.Logger;
            Enabled = Config.Bind("Config", "Pose Quick Loading", false, "Whether poses in Studio will be loaded by clicking on them. Vanilla behavior requires you to select the pose and then press load.");
            Harmony.CreateAndPatchAll(typeof(Hooks));
        }
    }

    internal static class Hooks
    {
        [HarmonyPostfix, HarmonyPatch(typeof(PauseRegistrationList), "OnClickSelect")]
        private static void OnClickSelect(PauseRegistrationList __instance)
        {
            if (PoseQuickLoad.Enabled.Value)
                Traverse.Create(__instance).Method("OnClickLoad").GetValue();
        }
    }
}
