using BepInEx;

namespace KK_Plugins
{
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class HairAccessoryCustomizer : BaseUnityPlugin { }
}
