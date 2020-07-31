using BepInEx;

namespace KK_Plugins
{
    [BepInProcess(Constants.VRProcessName)]
    [BepInProcess(Constants.VRProcessNameSteam)]
    [BepInPlugin(GUIDVR, PluginNameVR, Version)]
    public partial class Subtitles
    {
        public const string GUIDVR = GUID + ".vr";
        public const string PluginNameVR = PluginName + " VR";
    }
}
