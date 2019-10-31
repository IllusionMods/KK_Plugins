using BepInEx;

namespace KK_Plugins
{
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    [BepInProcess("StudioNEOV2")]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class FKIK : BaseUnityPlugin { }
}
