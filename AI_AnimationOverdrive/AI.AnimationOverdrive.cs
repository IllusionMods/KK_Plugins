using BepInEx;

namespace KK_Plugins
{
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class AnimationOverdrive : BaseUnityPlugin
    {
        public const string PluginNameInternal = "AI_AnimationOverdrive";
    }
}
