using BepInEx;
using BepInEx.Harmony;
using HarmonyLib;
using Illusion.Game;
using UnityEngine.SceneManagement;

namespace KK_Plugins
{
    /// <summary>
    /// When a Studio scene is loaded or imported, play a sound
    /// </summary>
    [BepInPlugin(GUID, PluginName, Version)]
    public class KK_StudioSceneLoadedSound : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.studiosceneloadedsound";
        public const string PluginName = "Studio Scene Loaded Sound";
        public const string Version = "1.0";
        private static bool LoadOrImportClicked = false;

        private void Main()
        {
            SceneManager.sceneLoaded += SceneLoaded;
            HarmonyWrapper.PatchAll(typeof(KK_StudioSceneLoadedSound));
        }
        /// <summary>
        /// When the StudioNotification scene loads check if load or import was clicked previously and play a sound
        /// </summary>
        private void SceneLoaded(Scene s, LoadSceneMode lsm)
        {
            if (s.name == "StudioNotification" && LoadOrImportClicked)
            {
                LoadOrImportClicked = false;
                Utils.Sound.Play(SystemSE.result_single);
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(Studio.SceneLoadScene), "OnClickLoad")]
        public static void OnClickLoadPrefix() => LoadOrImportClicked = true;
        [HarmonyPrefix, HarmonyPatch(typeof(Studio.SceneLoadScene), "OnClickImport")]
        public static void OnClickImportPrefix() => LoadOrImportClicked = true;
    }
}
