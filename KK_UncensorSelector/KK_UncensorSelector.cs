using BepInEx;
using BepInEx.Logging;
using Logger = BepInEx.Logger;
using Harmony;
using UnityEngine;
using System;
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
/// <summary>
/// Proof of concept plugin for assigning uncensors to characters individually
/// </summary>
namespace KK_UncensorSelector
{
    [BepInPlugin("com.deathweasel.bepinex.uncensorselector", "Uncensor Selector", Version)]
    public class KK_UncensorSelector : BaseUnityPlugin
    {
        public const string Version = "0.1";
        private static string CharacterName = "";

        void Main()
        {
            var harmony = HarmonyInstance.Create("com.deathweasel.bepinex.uncensorselector");
            harmony.PatchAll(typeof(KK_UncensorSelector));

            Type LoadAsyncIterator = typeof(ChaControl).GetNestedTypes(AccessTools.all).Where(x => x.Name.StartsWith("<LoadAsync>c__Iterator")).First();
            MethodInfo LoadAsyncIteratorMoveNext = LoadAsyncIterator.GetMethod("MoveNext");
            harmony.Patch(LoadAsyncIteratorMoveNext, null, null, new HarmonyMethod(typeof(KK_UncensorSelector).GetMethod(nameof(LoadAsyncTranspiler), BindingFlags.Static | BindingFlags.Public)));
        }

        public static IEnumerable<CodeInstruction> LoadAsyncTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();

            int IndexMaleBody = instructionsList.FindIndex(instruction => instruction.opcode == OpCodes.Ldstr && instruction.operand.ToString() == "p_cm_body_00");
            int IndexMaleBodyLow = instructionsList.FindIndex(instruction => instruction.opcode == OpCodes.Ldstr && instruction.operand.ToString() == "p_cm_body_00_low");
            int IndexFemaleBody = instructionsList.FindIndex(instruction => instruction.opcode == OpCodes.Ldstr && instruction.operand.ToString() == "p_cf_body_00");
            int IndexFemaleBodyLow = instructionsList.FindIndex(instruction => instruction.opcode == OpCodes.Ldstr && instruction.operand.ToString() == "p_cf_body_00_low");

            instructionsList[IndexMaleBody].opcode = OpCodes.Call;
            instructionsList[IndexMaleBody].operand = typeof(KK_UncensorSelector).GetMethod(nameof(MaleBody), BindingFlags.NonPublic | BindingFlags.Static);
            instructionsList[IndexMaleBodyLow].opcode = OpCodes.Call;
            instructionsList[IndexMaleBodyLow].operand = typeof(KK_UncensorSelector).GetMethod(nameof(MaleBodyLow), BindingFlags.NonPublic | BindingFlags.Static);
            instructionsList[IndexFemaleBody].opcode = OpCodes.Call;
            instructionsList[IndexFemaleBody].operand = typeof(KK_UncensorSelector).GetMethod(nameof(FemaleBody), BindingFlags.NonPublic | BindingFlags.Static);
            instructionsList[IndexFemaleBodyLow].opcode = OpCodes.Call;
            instructionsList[IndexFemaleBodyLow].operand = typeof(KK_UncensorSelector).GetMethod(nameof(FemaleBodyLow), BindingFlags.NonPublic | BindingFlags.Static);

            return instructions;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.LoadAsync))]
        public static void LoadAsyncPrefix(ChaControl __instance)
        {
            CharacterName = __instance.chaFile.parameter.fullname.Trim();
        }

        private static string MaleBody()
        {
            return "p_cm_body_00";
        }
        private static string MaleBodyLow()
        {
            return "p_cm_body_00_low";
        }
        private static string FemaleBody()
        {
            if (CharacterName == "Michelle")
                return "p_cf_body_02";
            else if (CharacterName == "Bessie")
                return "p_cf_body_01";
            return "p_cf_body_00";
        }
        private static string FemaleBodyLow()
        {
            return "p_cf_body_00_low";
        }
    }
}
