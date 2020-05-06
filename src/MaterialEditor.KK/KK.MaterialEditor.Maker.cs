using BepInEx;

namespace KK_Plugins.MaterialEditor
{
    [BepInProcess(Constants.MainGameProcessName)]
    [BepInProcess(Constants.MainGameProcessNameSteam)]
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    [BepInDependency(MaterialEditorPlugin.GUID)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class MEMaker { }
}
