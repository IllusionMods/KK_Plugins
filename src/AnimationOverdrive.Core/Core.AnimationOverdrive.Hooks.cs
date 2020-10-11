using HarmonyLib;
using Studio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine.UI;

namespace KK_Plugins
{
    public partial class AnimationOverdrive
    {
        internal static class Hooks
        {
            /// <summary>
            /// Increase the max value of animation speed
            /// </summary>
            [HarmonyTranspiler, HarmonyPatch(typeof(AnimeControl), "OnEndEditSpeed")]
            private static IEnumerable<CodeInstruction> OnEndEditSpeedTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> instructionsList = instructions.ToList();

                for (var index = 0; index < instructionsList.Count; index++)
                {
                    var x = instructionsList[index];
                    if (x.opcode == OpCodes.Ldc_R4 && x.operand?.ToString() == "3")
                        x.operand = AnimationSpeedMax;
                }

                return instructionsList;
            }

            /// <summary>
            /// Expand the max value of the slider based on the entered value
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(AnimeControl), "OnEndEditSpeed")]
            private static void OnEndEditSpeed(string _text, ref Slider ___sliderSpeed)
            {
                float speed = float.TryParse(_text, out float num) ? num : 0f;

                if (speed > 3)
                    ___sliderSpeed.maxValue = Math.Min(speed, AnimationSpeedMax);
                else
                    ___sliderSpeed.maxValue = 3;
            }
        }
    }
}
