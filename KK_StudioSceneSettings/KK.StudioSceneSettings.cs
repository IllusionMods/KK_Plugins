using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using KKAPI;
using KKAPI.Studio.SaveLoad;
using UnityEngine.SceneManagement;

namespace KK_Plugins
{
    [BepInProcess(Constants.StudioProcessName)]
    [BepInDependency(KoikatuAPI.GUID)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class StudioSceneSettings : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.studioscenesettings";
        public const string PluginName = "StudioSceneSettings";
        public const string PluginNameInternal = "KK_StudioSceneSettings";
        public const string Version = "1.0";
        internal static new ManualLogSource Logger;

        internal const int CameraMapMaskingLayer = 26;

        public static ToggleSet MapMasking;
        public static SliderSet NearClipPlane;
        public static SliderSet FarClipPlane;

        internal void Main()
        {
            Logger = base.Logger;

            StudioSaveLoadApi.RegisterExtraBehaviour<StudioSceneSettingsSceneController>(GUID);
            SceneManager.sceneLoaded += (s, lsm) => InitStudioUI(s.name);
        }

        /// <summary>
        /// Reset all the things to their default values
        /// </summary>
        public static void ResetAll()
        {
            NearClipPlane.Reset();
            FarClipPlane.Reset();
            MapMasking.Reset();
        }

        /// <summary>
        /// Returns the instance of the scene controller
        /// </summary>
        /// <returns></returns>
        public static StudioSceneSettingsSceneController GetSceneController() => Chainloader.ManagerObject.transform.GetComponentInChildren<StudioSceneSettingsSceneController>();
    }
}