using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UniRx;

namespace KK_Plugins
{
    internal partial class Hooks
    {
        /// <summary>
        /// Change the male _low asset to the female _low asset. Female has more bones so trying to change male body to female doesn't work. Load as female and change to male as a workaround.
        /// </summary>
        internal static IEnumerable<CodeInstruction> LoadAsyncTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();

            foreach (var x in instructionsList)
            {
                switch (x.operand?.ToString())
                {
                    case "p_cm_body_00_low":
                        x.opcode = OpCodes.Call;
                        x.operand = typeof(UncensorSelector).GetMethod(nameof(UncensorSelector.SetMaleBodyLow), AccessTools.all);
                        break;
                }
            }

            return instructions;
        }
    }
}
