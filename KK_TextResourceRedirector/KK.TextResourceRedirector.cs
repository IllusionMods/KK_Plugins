using BepInEx;

namespace KK_Plugins
{
    [BepInDependency(XUnity.ResourceRedirector.Constants.PluginData.Identifier)]
    [BepInDependency(XUnity.AutoTranslator.Plugin.Core.Constants.PluginData.Identifier)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class TextResourceRedirector : BaseUnityPlugin
    {
        public const string PluginNameInternal = "KK_TextResourceRedirector";

        private TextResourceHelper GetTextResourceHelper() => new KK_TextResourceHelper();
        private TextAssetHelper GetTextAssetHelper() => new TextAssetHelper(new string[] { "\r\n", "\r", "\n" }, new string[] { "\t" });
    }
}