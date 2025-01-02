using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        public const string Version = "1.0.3";

        internal static new ManualLogSource Logger;

        public static ConfigEntry<bool> DisableFade { get; private set; }
        public static ConfigEntry<Color> FadeColor { get; private set; }

        private void Awake()
        {
            Logger = base.Logger;
            DisableFade = Config.Bind("Config", "Disable Fade", false, "Disables fade on loading screens");
            FadeColor = Config.Bind("Config", "Fade Color", Color.white, "Color of loading screens");

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
                if (FadeColor.Value != Color.white)
                    __instance._Color = FadeColor.Value;
                if (DisableFade.Value)
                    __instance.ForceEnd();
            }
#elif KKS
            [HarmonyPostfix, HarmonyPatch(typeof(SceneFadeCanvas), nameof(SceneFadeCanvas.Awake))]
            private static void SceneFadeCanvasAwake(SceneFadeCanvas __instance)
            {
                if (FadeColor.Value != Color.white)
                    __instance.SetColor(FadeColor.Value);
            }
            [HarmonyPrefix, HarmonyPatch(typeof(SceneFadeCanvas), nameof(SceneFadeCanvas.SetColor))]
            private static void SceneFadeCanvasSetColor(ref Color _color)
            {
                if (FadeColor.Value != Color.white)
                    _color = FadeColor.Value;
            }
            [HarmonyPrefix, HarmonyPatch(typeof(FadeCanvas), nameof(FadeCanvas.StartAysnc))]
            private static void SceneFadeCanvasSetColor(ref float duration)
            {
                // Fix lockup in H scenes
                Scene scene = SceneManager.GetActiveScene();
                if (scene.buildIndex == -1)
                    return;

                if (DisableFade.Value)
                    duration = 0f;
            }
#endif
        }
    }
}
