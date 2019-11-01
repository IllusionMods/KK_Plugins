using BepInEx;
using KKAPI;

namespace KK_Plugins
{
    [BepInProcess("StudioNEOV2")]
    [BepInDependency(KoikatuAPI.GUID)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class KK_AnimationController : BaseUnityPlugin { }
}