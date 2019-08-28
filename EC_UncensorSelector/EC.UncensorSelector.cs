using BepInEx;

namespace KK_Plugins
{
    [BepInDependency(Sideloader.Sideloader.GUID)]
    [BepInDependency(ExtensibleSaveFormat.ExtendedSave.GUID)]
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    [BepInPlugin(GUID, PluginName, Version)]
    internal partial class UncensorSelector : BaseUnityPlugin { }
}
