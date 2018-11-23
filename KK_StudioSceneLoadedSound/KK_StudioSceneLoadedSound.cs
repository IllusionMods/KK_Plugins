using BepInEx;
using Harmony;
using Illusion.Game;
using UnityEngine.SceneManagement;

namespace KK_StudioSceneLoadedSound
{
    /// <summary>
    /// When a Studio scene is loaded or imported, play a sound
    /// </summary>
    [BepInPlugin("com.deathweasel.bepinex.studiosceneloadedsound", "Studio Scene Loaded Sound", Version)]
    public class KK_StudioSceneLoadedSound : BaseUnityPlugin
    {
        public const string Version = "1.0";
        private static bool LoadOrImportClicked = false;

        void Main()
        {
            var harmony = HarmonyInstance.Create("com.deathweasel.bepinex.studiosceneloadedsound");
            harmony.PatchAll(typeof(KK_StudioSceneLoadedSound));
            //Add our own method to the sceneLoaded event
            SceneManager.sceneLoaded += SceneLoaded;
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

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Studio.SceneLoadScene), "OnClickLoad")]
        public static void OnClickLoadPrefix()
        {
            LoadOrImportClicked = true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Studio.SceneLoadScene), "OnClickImport")]
        public static void OnClickImportPrefix()
        {
            LoadOrImportClicked = true;
        }
    }
}
