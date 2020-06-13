using BepInEx;

namespace KK_Plugins
{
    [BepInProcess(Constants.MainGameProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class Subtitles : BaseUnityPlugin { }
}
