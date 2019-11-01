using BepInEx;

namespace KK_Plugins
{
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    [BepInDependency(ExtensibleSaveFormat.ExtendedSave.GUID)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class InvisibleBody : BaseUnityPlugin { }
}
