using BepInEx;
using BepInEx.Logging;
using Harmony;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Logger = BepInEx.Logger;
/// <summary>
/// Proof of concept plugin for assigning uncensors to characters individually
/// </summary>
namespace KK_UncensorSelector
{
    [BepInPlugin("com.deathweasel.bepinex.uncensorselector", "Uncensor Selector", Version)]
    public class KK_UncensorSelector : BaseUnityPlugin
    {
        public const string Version = "0.2";
        private static string CharacterName = "";

        void Main()
        {
            var harmony = HarmonyInstance.Create("com.deathweasel.bepinex.uncensorselector");
            harmony.PatchAll(typeof(KK_UncensorSelector));

            Type LoadAsyncIterator = typeof(ChaControl).GetNestedTypes(AccessTools.all).Where(x => x.Name.StartsWith("<LoadAsync>c__Iterator")).First();
            MethodInfo LoadAsyncIteratorMoveNext = LoadAsyncIterator.GetMethod("MoveNext");
            harmony.Patch(LoadAsyncIteratorMoveNext, null, null, new HarmonyMethod(typeof(KK_UncensorSelector).GetMethod(nameof(LoadAsyncTranspiler), BindingFlags.Static | BindingFlags.Public)));
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.LoadAsync))]
        public static void LoadAsyncPrefix(ChaControl __instance)
        {
            CharacterName = __instance.chaFile.parameter.fullname.Trim();
        }

        public static IEnumerable<CodeInstruction> LoadAsyncTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            List<int> OOBaseIndex = new List<int>();

            int IndexMaleBody = instructionsList.FindIndex(instruction => instruction.opcode == OpCodes.Ldstr && instruction.operand.ToString() == "p_cm_body_00");

            for (int i = 0; i < instructionsList.Count; i++)
            {
                if (instructionsList[i].operand?.ToString() == "chara/oo_base.unity3d")
                {
                    Logger.Log(LogLevel.Info, $"i:{i} opcode:[{instructionsList[i].opcode}] operand:[{instructionsList[i].operand?.ToString()}]");
                    OOBaseIndex.Add(i);
                }
            }
            Logger.Log(LogLevel.Info, IndexMaleBody);

            //p_cf_body_00, p_cm_body_00, and _low variants
            instructionsList[OOBaseIndex[2]].opcode = OpCodes.Call;
            instructionsList[OOBaseIndex[2]].operand = typeof(KK_UncensorSelector).GetMethod(nameof(SetOOBase), BindingFlags.NonPublic | BindingFlags.Static);

            //p_cf_body_00_Nml
            instructionsList[OOBaseIndex[3]].opcode = OpCodes.Call;
            instructionsList[OOBaseIndex[3]].operand = typeof(KK_UncensorSelector).GetMethod(nameof(SetOOBase), BindingFlags.NonPublic | BindingFlags.Static);

            return instructions;
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.CreateBodyTexture))]
        public static IEnumerable<CodeInstruction> CreateBodyTextureTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();

            //cf_body_00_t, cf_body_00_mc, cm_body_00_mc, and _low variants
            foreach (var x in instructionsList)
            {
                Logger.Log(LogLevel.Info, $"opcode:[{x.opcode}] operand:[{x.operand?.ToString()}]");
                if (x.operand?.ToString() == "chara/oo_base.unity3d")
                {
                    x.opcode = OpCodes.Call;
                    x.operand = typeof(KK_UncensorSelector).GetMethod(nameof(SetOOBase), BindingFlags.NonPublic | BindingFlags.Static);
                }
            }

            return instructions;
        }

        private static string SetOOBase()
        {
            Logger.Log(LogLevel.Info, $"SetOOBase {CharacterName}");
            if (CharacterName == "Bessie")
                return "chara/oo_base_KK_LO.unity3d";
            return "chara/oo_base.unity3d";
        }

    }
}
