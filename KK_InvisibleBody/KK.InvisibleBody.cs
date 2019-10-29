using BepInEx;
/// <summary>
/// Sets the selected characters invisible in Studio. Invisible state saves and loads with the scene.
/// Also sets female characters invisible in H scenes.
/// </summary>
namespace KK_Plugins
{
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    [BepInDependency(ExtensibleSaveFormat.ExtendedSave.GUID)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class InvisibleBody : BaseUnityPlugin { }
}
