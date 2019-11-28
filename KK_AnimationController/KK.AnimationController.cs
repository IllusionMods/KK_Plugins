using BepInEx;
using KKAPI;

namespace KK_Plugins
{
    [BepInProcess(Constants.StudioProcessName)]
    [BepInDependency(KoikatuAPI.GUID)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class AnimationController : BaseUnityPlugin
    {
        public const string PluginNameInternal = "KK_AnimationController";
    }
}