using HarmonyLib;
using Studio;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
#if AI || HS2
using AIChara;
#endif

namespace KK_Plugins
{
    public partial class MaleJuice
    {
        internal static class Hooks
        {
            /// <summary>
            /// Set juice flags for males in Studio
            /// </summary>
            [HarmonyPostfix, HarmonyPatch(typeof(OCIChar), nameof(OCIChar.SetSiruFlags))]
            private static void SetSiruFlags(ChaFileDefine.SiruParts _parts, byte _state, OCIChar __instance)
            {
                if (__instance is OCICharMale charMale)
#if KK
                    charMale.male.SetSiruFlags(_parts, _state);
#else
                    charMale.male.SetSiruFlag(_parts, _state);
#endif
            }
            /// <summary>
            /// Get juice flags for males in Studio
            /// </summary>
            [HarmonyPostfix, HarmonyPatch(typeof(OCIChar), nameof(OCIChar.GetSiruFlags))]
            private static void GetSiruFlags(ChaFileDefine.SiruParts _parts, OCIChar __instance, ref byte __result)
            {
                if (__instance is OCICharMale charMale)
#if KK
                    __result = charMale.male.GetSiruFlags(_parts);
#else
                    __result = charMale.male.GetSiruFlag(_parts);
#endif
            }
            /// <summary>
            /// Enable the juice section in Studio always, not just for females
            /// </summary>
            [HarmonyPostfix, HarmonyPatch(typeof(MPCharCtrl.LiquidInfo), nameof(MPCharCtrl.LiquidInfo.UpdateInfo))]
            private static void LiquidInfoUpdateInfo(OCIChar _char, MPCharCtrl.LiquidInfo __instance)
            {
                __instance.active = true;
                __instance.face.select = _char.GetSiruFlags(ChaFileDefine.SiruParts.SiruKao);

#if KK
                __instance.breast.select = _char.GetSiruFlags(ChaFileDefine.SiruParts.SiruFrontUp);
                __instance.back.select = _char.GetSiruFlags(ChaFileDefine.SiruParts.SiruBackUp);
                __instance.belly.select = _char.GetSiruFlags(ChaFileDefine.SiruParts.SiruFrontDown);
                __instance.hip.select = _char.GetSiruFlags(ChaFileDefine.SiruParts.SiruBackDown);
#else
                __instance.breast.select = _char.GetSiruFlags(ChaFileDefine.SiruParts.SiruFrontTop);
                __instance.back.select = _char.GetSiruFlags(ChaFileDefine.SiruParts.SiruBackTop);
                __instance.belly.select = _char.GetSiruFlags(ChaFileDefine.SiruParts.SiruFrontBot);
                __instance.hip.select = _char.GetSiruFlags(ChaFileDefine.SiruParts.SiruBackBot);
#endif
            }

#if AI || HS2
            [HarmonyTranspiler, HarmonyPatch(typeof(ChaControl), "UpdateSiru")]
            private static IEnumerable<CodeInstruction> UpdateSiruTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> instructionsList = instructions.ToList();

                //Remove the gender check
                if (instructionsList[0].opcode == OpCodes.Ldarg_0 && instructionsList[4].opcode == OpCodes.Ret)
                    for (int i = 4; i >= 0; i--)
                        instructionsList.RemoveAt(i);
                else
                    Logger.LogError("Unable to patch UpdateSiru");

                return instructionsList;
            }

            [HarmonyTranspiler, HarmonyPatch(typeof(ChaControl), "UpdateClothesSiru")]
            private static IEnumerable<CodeInstruction> UpdateClothesSiruTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> instructionsList = instructions.ToList();

                //Remove the gender check
                if (instructionsList[0].opcode == OpCodes.Ldarg_0 && instructionsList[4].opcode == OpCodes.Ret)
                    for (int i = 4; i >= 0; i--)
                        instructionsList.RemoveAt(i);
                else
                    Logger.LogError("Unable to patch UpdateClothesSiru");

                return instructionsList;
            }
#endif
        }
    }
}
