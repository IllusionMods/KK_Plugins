using BepInEx;

namespace KK_Plugins
{
    [BepInDependency(MaterialEditor.GUID, "1.10")]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class ImageEmbed : BaseUnityPlugin
    {
        public const string PluginNameInternal = "AI_ImageEmbed";
    }
}
