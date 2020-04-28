using BepInEx;

namespace KK_Plugins
{
    [BepInDependency(MaterialEditor.GUID)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class ImageEmbed : BaseUnityPlugin
    {
        public const string PluginNameInternal = "KK_ImageEmbed";
    }
}
