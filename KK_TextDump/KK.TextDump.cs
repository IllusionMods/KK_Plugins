using BepInEx;

namespace KK_Plugins
{
    /// <summary>
    /// Dumps untranslated text to .txt files
    /// </summary>
    [BepInProcess("CharaStudio")]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class TextDump : BaseUnityPlugin { }
}
