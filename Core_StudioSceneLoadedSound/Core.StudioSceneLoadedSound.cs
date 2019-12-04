using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Harmony;
using BepInEx.Logging;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace KK_Plugins
{
    /// <summary>
    /// When a Studio scene is loaded or imported, play a sound
    /// </summary>
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    [BepInDependency(DragDrop_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    public partial class StudioSceneLoadedSound
    {
        public const string GUID = "com.deathweasel.bepinex.studiosceneloadedsound";
        public const string PluginName = "Studio Scene Loaded Sound";
        public const string PluginNameInternal = "StudioSceneLoadedSound";
        public const string Version = "1.1";
        private const string DragDrop_GUID = "keelhauled.draganddrop";

        private static bool LoadOrImportClicked = false;
        private static bool DragAndDropped = false;

        internal static new ManualLogSource Logger;

        internal void Main()
        {
            Logger = base.Logger;
            SceneManager.sceneLoaded += SceneLoaded;
            HarmonyWrapper.PatchAll(typeof(Hooks));
        }

        internal void Start()
        {
            if (Chainloader.PluginInfos.TryGetValue(DragDrop_GUID, out PluginInfo dragDropPluginInfo))
            {
                Logger.LogDebug($"Patching {DragDrop_GUID}");
                DragAndDropPatches.InstallPatches(dragDropPluginInfo.Instance);
            }
        }

        /// <summary>
        /// When the StudioNotification scene loads check if load or import was clicked previously and play a sound
        /// </summary>
        private void SceneLoaded(Scene s, LoadSceneMode lsm)
        {
            if (s.name == "StudioNotification" && LoadOrImportClicked)
            {
                LoadOrImportClicked = false;
                PlayAlertSound();
            }
        }
    }
}