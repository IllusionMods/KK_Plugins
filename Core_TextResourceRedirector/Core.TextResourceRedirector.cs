using BepInEx;
using BepInEx.Logging;

//Adopted from gravydevsupreme's TextResourceRedirector
namespace KK_Plugins
{
    public partial class TextResourceRedirector : BaseUnityPlugin
    {
        public const string PluginName = "Text Resource Redirector";
        public const string GUID = "com.deathweasel.bepinex.textresourceredirector";
        public const string Version = "1.1.1";
        internal static new ManualLogSource Logger;

        internal ExcelDataResourceRedirector _excelRedirector;
        internal ScenarioDataResourceRedirector _scenarioRedirector;
        internal TextAssetResourceRedirector _textAssetResourceRedirector;
        internal TextResourceHelper _textResourceHelper;
        internal static TextAssetHelper _textAssetHelper;

        internal void Awake()
        {
            Logger = Logger ?? base.Logger;
            _textResourceHelper = GetTextResourceHelper();
            _excelRedirector = new ExcelDataResourceRedirector();
            _scenarioRedirector = new ScenarioDataResourceRedirector(_textResourceHelper);
            _textAssetHelper = GetTextAssetHelper();
            _textAssetResourceRedirector = new TextAssetResourceRedirector(_textAssetHelper);

            enabled = false;
        }

        internal void Main()
        {
            Logger = Logger ?? base.Logger;
        }
    }
}