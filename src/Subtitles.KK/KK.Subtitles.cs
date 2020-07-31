using BepInEx;

namespace KK_Plugins
{
    [BepInProcess(Constants.MainGameProcessNameSteam)]
    [BepInProcess(Constants.MainGameProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class Subtitles { }
}
