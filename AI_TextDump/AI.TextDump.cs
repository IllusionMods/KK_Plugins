using BepInEx;

namespace KK_Plugins
{
    /// <summary>
    /// Dumps untranslated text to .txt files
    /// </summary>
    //[BepInProcess("StudioNEOV2")]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class TextDump : BaseUnityPlugin
    {
        private TextResourceHelper textResourceHelper = new AI_TextResourceHelper();
    }
}
