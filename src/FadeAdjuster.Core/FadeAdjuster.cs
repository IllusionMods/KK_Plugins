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
        public const string Version = "1.0.1";

#if KK
        private static bool UpdateColor;
#endif

        public static ConfigEntry<bool> DisableFade { get; private set; }
        public static ConfigEntry<Color> FadeColor { get; private set; }

        private void Awake()
        {
            Logger.LogInfo($"Awake");
            DisableFade = Config.Bind("Config", "Disable Fade", false, "Disables fade on loading screens");
            FadeColor = Config.Bind("Config", "Fade Color", Color.white, "Color of loading screens");
#if KK
            FadeColor.SettingChanged += (a, b) => UpdateColor = true;
#endif

            Harmony.CreateAndPatchAll(typeof(Hooks));
        }

        internal static class Hooks
        {
#if KK
            [HarmonyPostfix, HarmonyPatch(typeof(Manager.Scene), nameof(Manager.Scene.Awake))]
            private static void Awake(ref Color ___initFadeColor)
            {
                if (FadeColor.Value != Color.white)
                    ___initFadeColor = FadeColor.Value;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(SimpleFade), nameof(SimpleFade.Update))]
            private static void SimpleFadeUpdate(SimpleFade __instance)
            {
                if (UpdateColor)
                    __instance._Color = FadeColor.Value;
                if (DisableFade.Value)
                    __instance.ForceEnd();
            }
#endif

#if KKS
            [HarmonyPostfix, HarmonyPatch(typeof(SceneFadeCanvas), nameof(SceneFadeCanvas.Awake))]
            private static void SceneFadeCanvasAwake(SceneFadeCanvas __instance)
            {
                __instance.SetColor(FadeColor.Value);
            }
            [HarmonyPrefix, HarmonyPatch(typeof(SceneFadeCanvas), nameof(SceneFadeCanvas.SetColor))]
            private static void SceneFadeCanvasSetColor(ref Color _color)
            {
                _color = FadeColor.Value;
            }
            [HarmonyPrefix, HarmonyPatch(typeof(FadeCanvas), nameof(FadeCanvas.StartAysnc))]
            private static void SceneFadeCanvasSetColor(ref float duration)
            {
                if (DisableFade.Value)
                    duration = 0f;
            }
#endif
        }
    }
}