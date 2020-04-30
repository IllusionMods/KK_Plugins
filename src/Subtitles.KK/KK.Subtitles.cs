using ActionGame.Communication;
using BepInEx;

namespace KK_Plugins
{
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class Subtitles : BaseUnityPlugin
    {
        public const string PluginNameInternal = "KK_Subtitles";
        internal static Info ActionGameInfoInstance;
        internal static HSceneProc HSceneProcInstance;
    }
}
