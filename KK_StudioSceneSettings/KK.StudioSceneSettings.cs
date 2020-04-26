using BepInEx;
using KKAPI;
using KKAPI.Studio.SaveLoad;

namespace KK_Plugins.StudioSceneSettings
{
    [BepInProcess(Constants.StudioProcessName)]
    [BepInDependency(KoikatuAPI.GUID, "1.11")]
    [BepInPlugin(GUID, PluginName, Version)]
    public class StudioSceneSettingsPlugin : StudioSceneSettingsCore
    {
        public const string PluginNameInternal = "KK_StudioSceneSettings";

        internal void Start() => StudioSaveLoadApi.RegisterExtraBehaviour<SceneController>(GUID);
    }
}