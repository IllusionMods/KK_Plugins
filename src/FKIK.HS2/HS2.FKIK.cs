using BepInEx;

namespace KK_Plugins
{
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class FKIK : BaseUnityPlugin
    {
        public const string PluginNameInternal = "HS2_FKIK";
    }
}
