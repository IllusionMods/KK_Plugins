using BepInEx;

namespace KK_Plugins.MaterialEditor
{
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class MaterialEditorPlugin : BaseUnityPlugin
    {
        public const string PluginNameInternal = "EC_MaterialEditor";
    }
}
