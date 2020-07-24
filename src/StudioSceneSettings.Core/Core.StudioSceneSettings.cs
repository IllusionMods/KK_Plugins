using BepInEx;
using BepInEx.Logging;
using KKAPI;
using KKAPI.Studio.SaveLoad;

namespace KK_Plugins.StudioSceneSettings
{
    [BepInProcess(Constants.StudioProcessName)]
    [BepInDependency(KoikatuAPI.GUID, "1.11")]
    [BepInPlugin(GUID, PluginName, Version)]
    public class StudioSceneSettings : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.studioscenesettings";
        public const string PluginName = "StudioSceneSettings";
        public const string PluginNameInternal = Constants.Prefix + "_StudioSceneSettings";
        public const string Version = "1.1";
        internal static new ManualLogSource Logger;

        internal const int CameraMapMaskingLayer = 26;

        internal void Main()
        {
            Logger = base.Logger;
            StudioSaveLoadApi.RegisterExtraBehaviour<SceneController>(GUID);
        }
    }
}