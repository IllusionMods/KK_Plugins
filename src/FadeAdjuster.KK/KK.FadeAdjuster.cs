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

        public static ConfigEntry<bool> DisableFade { get; private set; }
        public static ConfigEntry<Color> FadeColor { get; private set; }

        internal void Awake()
        {
            DisableFade = Config.Bind("Config", "Disable Fade", false, "Disables fade on loading screens, may reveal ugly things not meant to be seen");
            FadeColor = Config.Bind("Config", "Fade Color", Color.white, "Color of the fade for loading screens, requires game restart to take effect");

            Harmony.CreateAndPatchAll(typeof(Hooks));
        }

        internal static class Hooks
        {
            [HarmonyPostfix, HarmonyPatch(typeof(Manager.Scene), "Awake")]
            internal static void Awake(ref Color ___initFadeColor)
            {
                if (FadeColor.Value != Color.white)
                    ___initFadeColor = FadeColor.Value;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(SimpleFade), "Update")]
            internal static bool SimpleFadeUpdate(SimpleFade __instance)
            {
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