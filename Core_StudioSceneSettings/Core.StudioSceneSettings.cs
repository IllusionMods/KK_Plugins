using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;

namespace KK_Plugins.StudioSceneSettings
{
    public abstract class StudioSceneSettingsCore : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.studioscenesettings";
        public const string PluginName = "StudioSceneSettings";
        public const string Version = "1.0.1";
        internal static new ManualLogSource Logger;

        internal const int CameraMapMaskingLayer = 26;

        internal void Main() => Logger = base.Logger;

        /// <summary>
        /// Returns the instance of the scene controller
        /// </summary>
        /// <returns></returns>
        public static SceneControllerCore GetSceneController() => Chainloader.ManagerObject.transform.GetComponentInChildren<SceneControllerCore>();
    }
}