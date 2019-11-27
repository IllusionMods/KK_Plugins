using BepInEx;
//Adopted from gravydevsupreme's TextResourceRedirector
namespace KK_Plugins
{
    public partial class TextResourceRedirector : BaseUnityPlugin
    {
        public const string PluginName = "Text Resource Redirector";
        public const string GUID = "com.deathweasel.bepinex.textresourceredirector";
        public const string Version = "1.1";

        internal ExcelDataResourceRedirector _excelRedirector;
        internal ScenarioDataResourceRedirector _scenarioRedirector;
        internal TextResourceHelper _textResourceHelper;

        internal void Awake()
        {
            _textResourceHelper = GetTextResourceHelper();
            _excelRedirector = new ExcelDataResourceRedirector();
            _scenarioRedirector = new ScenarioDataResourceRedirector(_textResourceHelper);

            enabled = false;
        }
    }
}