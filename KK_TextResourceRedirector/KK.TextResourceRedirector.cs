using BepInEx;

namespace KK_Plugins
{
    [BepInDependency(XUnity.ResourceRedirector.Constants.PluginData.Identifier)]
    [BepInDependency(XUnity.AutoTranslator.Plugin.Core.Constants.PluginData.Identifier)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class TextResourceRedirector : BaseUnityPlugin {
        private TextResourceHelper GetTextResourceHelper() => new KK_TextResourceHelper();
    }
}