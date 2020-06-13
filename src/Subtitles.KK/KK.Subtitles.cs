using ActionGame.Communication;
using BepInEx;

namespace KK_Plugins
{
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class Subtitles : BaseUnityPlugin
    {
        internal static Info ActionGameInfoInstance;
        internal static HSceneProc HSceneProcInstance;
    }
}
