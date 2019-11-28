using BepInEx;

namespace KK_Plugins
{
    /// <summary>
    /// Dumps untranslated text to .txt files
    /// </summary>
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class TextDump : BaseUnityPlugin
    {
        public const string PluginNameInternal = "KK_TextDump";

        private readonly TextResourceHelper textResourceHelper = new KK_TextResourceHelper();
    }
}
