using BepInEx;
using BepInEx.Harmony;
using Illusion.Game;
using UnityEngine.SceneManagement;

namespace KK_Plugins
{
    /// <summary>
    /// When a Studio scene is loaded or imported, play a sound
    /// </summary>
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class StudioSceneLoadedSound : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.studiosceneloadedsound";
        public const string PluginName = "Studio Scene Loaded Sound";
        public const string PluginNameInternal = "KK_StudioSceneLoadedSound";
        public const string Version = "1.0";
        private static bool LoadOrImportClicked = false;

        internal void Main()
        {
            SceneManager.sceneLoaded += SceneLoaded;
            HarmonyWrapper.PatchAll(typeof(StudioSceneLoadedSound));
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
    }
}
