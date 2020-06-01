using BepInEx;

namespace KK_Plugins
{
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class StudioObjectMoveHotkeys : BaseUnityPlugin
    {
        public const string PluginNameInternal = "HS2_StudioObjectMoveHotkeys";
    }
}
