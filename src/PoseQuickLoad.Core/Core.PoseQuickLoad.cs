using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Studio;
using UnityEngine;

namespace KK_Plugins
{
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class PoseQuickLoad : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.posequickload";
        public const string PluginName = "Pose Quick Load";
        public const string PluginNameInternal = Constants.Prefix + "_PoseQuickLoad";
        public const string Version = "1.1";
        internal static new ManualLogSource Logger;

        public static ConfigEntry<QuickLoad> ConfigPoseQuickLoad { get; private set; }

        internal void Main()
        {
            Logger = base.Logger;
            ConfigPoseQuickLoad = Config.Bind("Config", "Pose Quick Loading", QuickLoad.EnabledWithShift, "Whether poses in Studio will be loaded by clicking on them. Vanilla behavior requires you to select the pose and then press load.");
            Harmony.CreateAndPatchAll(typeof(Hooks));
        }

        public enum QuickLoad { Disabled, Enabled, EnabledWithShift }
    }

    internal static class Hooks
    {
        [HarmonyPostfix, HarmonyPatch(typeof(PauseRegistrationList), nameof(PauseRegistrationList.OnClickSelect))]
        private static void OnClickSelect(PauseRegistrationList __instance)
        {
            if (PoseQuickLoad.ConfigPoseQuickLoad.Value == PoseQuickLoad.QuickLoad.Enabled)
            {
                __instance.OnClickLoad();
            }
            else if (PoseQuickLoad.ConfigPoseQuickLoad.Value == PoseQuickLoad.QuickLoad.EnabledWithShift)
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    __instance.OnClickLoad();
            }
        }
    }
}
