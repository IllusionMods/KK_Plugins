using BepInEx;
using KKAPI;
using KKAPI.Studio.SaveLoad;

namespace KK_Plugins.StudioSceneSettings
{
    [BepInProcess(Constants.StudioProcessName)]
    [BepInDependency(KoikatuAPI.GUID)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class StudioSceneSettingsPlugin : StudioSceneSettingsCore
    {
        public const string PluginNameInternal = "AI_StudioSceneSettings";

        internal void Start() => StudioSaveLoadApi.RegisterExtraBehaviour<SceneController>(GUID);
    }
}