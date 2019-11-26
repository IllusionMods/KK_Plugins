using System.Collections.Generic;
using System.Linq;

namespace KK_Plugins
{
    public class AI_TextResourceHelper : TextResourceHelper
    {
        public AI_TextResourceHelper()
        {
            CalcKeys = new HashSet<string>(new string[] { "want" });
            FormatKeys = new HashSet<string>(new string[] { "パターン", "セリフ" });
            TextKeysBlacklist = new HashSet<string>(CalcKeys.Concat(FormatKeys).ToArray());

            SupportedCommands[ADV.Command.Calc] = true;
            SupportedCommands[ADV.Command.Format] = true;
            SupportedCommands[ADV.Command.Choice] = true;
            SupportedCommands[ADV.Command.Switch] = true;

        }

    }
}
