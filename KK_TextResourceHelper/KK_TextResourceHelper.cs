using System.Collections.Generic;
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
    }
}
