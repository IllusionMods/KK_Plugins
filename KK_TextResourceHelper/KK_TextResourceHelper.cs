using System.Collections.Generic;
using ADV;
using BepInEx.Logging;

namespace KK_Plugins
{
    public class KK_TextResourceHelper : TextResourceHelper
    {
        public KK_TextResourceHelper()
        {
            CalcKeys = new HashSet<string>();
            FormatKeys = new HashSet<string>();
            TextKeysBlacklist = new HashSet<string>();

            SupportedCommands[ADV.Command.Choice] = true;
        }

        override public bool IsReplacement(ScenarioData.Param param) => (int)param.Command == 223; // only Party has ADV.Command.ReplaceLanguage
    }
}
