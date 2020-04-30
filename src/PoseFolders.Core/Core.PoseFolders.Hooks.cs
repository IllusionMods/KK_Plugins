using HarmonyLib;
using Studio;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KK_Plugins
{
    public partial class PoseFolders
    {
        internal static class Hooks
        {
            [HarmonyTranspiler, HarmonyPatch(typeof(PauseRegistrationList), "InitList")]
            internal static IEnumerable<CodeInstruction> tpl_PauseRegistrationList_InitList(IEnumerable<CodeInstruction> _instructions) => ReplacePath(_instructions);

            [HarmonyTranspiler, HarmonyPatch(typeof(PauseCtrl), "Save")]
            internal static IEnumerable<CodeInstruction> tpl_PauseCtrl_Save(IEnumerable<CodeInstruction> _instructions) => ReplacePath(_instructions);

            [HarmonyPostfix, HarmonyPatch(typeof(PauseRegistrationList), "InitList")]
            internal static void post_PauseRegistrationList_InitList(PauseRegistrationList __instance)
            {
                if (v_prefabNode == null)
                {
                    v_prefabNode = (GameObject)AccessTools.Field(typeof(PauseRegistrationList), "prefabNode").GetValue(__instance);
                    v_transformRoot = (Transform)AccessTools.Field(typeof(PauseRegistrationList), "transformRoot").GetValue(__instance);
                }

                foreach (var subDir in CurrentDirectory.GetDirectories().Reverse())
                {
                    AddListButton($"[{subDir.Name}]", () =>
                    {
                        CurrentDirectory = subDir;
                        Traverse.Create(__instance).Method("InitList").GetValue();
                    }).SetAsFirstSibling();
                }
                var fn = CurrentDirectory.FullName;
                if (fn.Length > DefaultRootLength)
                {
                    AddListButton("..", () =>
                    {
                        CurrentDirectory = CurrentDirectory.Parent;
                        Traverse.Create(__instance).Method("InitList").GetValue();
                    }).SetAsFirstSibling();
                }
            }
        }
    }
}
