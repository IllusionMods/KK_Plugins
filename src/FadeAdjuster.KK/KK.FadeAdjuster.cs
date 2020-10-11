using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace KK_Plugins
{
    /// <summary>
    /// Adjust fade color or disable it
    /// </summary>
    [BepInPlugin(GUID, PluginName, Version)]
    public class FadeAdjuster : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.fadeadjuster";
        public const string PluginName = "Fade Adjuster";
        public const string PluginNameInternal = "KK_FadeAdjuster";
        public const string Version = "1.0";

        private static bool UpdateColor;

        public static ConfigEntry<bool> DisableFade { get; private set; }
        public static ConfigEntry<Color> FadeColor { get; private set; }

        private void Awake()
        {
            DisableFade = Config.Bind("Config", "Disable Fade", false, "Disables fade on loading screens");
            FadeColor = Config.Bind("Config", "Fade Color", Color.white, "Color of loading screens");
            FadeColor.SettingChanged += (a, b) => UpdateColor = true;

            Harmony.CreateAndPatchAll(typeof(Hooks));
        }

        internal static class Hooks
        {
            [HarmonyPostfix, HarmonyPatch(typeof(Manager.Scene), "Awake")]
            private static void Awake(ref Color ___initFadeColor)
            {
                if (FadeColor.Value != Color.white)
                    ___initFadeColor = FadeColor.Value;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(SimpleFade), "Update")]
            private static bool SimpleFadeUpdate(SimpleFade __instance)
            {
                if (UpdateColor)
                    __instance._Color = FadeColor.Value;
                if (DisableFade.Value)
                {
                    __instance.ForceEnd();
                    return false;
                }
                return true;
            }
        }
    }
}