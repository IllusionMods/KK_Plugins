using ActionGame.Communication;
using BepInEx;

namespace KK_Plugins
{
    [BepInProcess(Constants.MainGameProcessName)]
    [BepInProcess(Constants.MainGameProcessNameSteam)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class Subtitles : BaseUnityPlugin
    {
        internal static Info ActionGameInfoInstance;
        internal static HSceneProc HSceneProcInstance;
    }
}
