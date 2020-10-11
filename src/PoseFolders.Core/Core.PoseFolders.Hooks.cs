using HarmonyLib;
using Studio;
using System.Collections.Generic;
using UnityEngine;

namespace KK_Plugins
{
    public partial class PoseFolders
    {
        internal static class Hooks
        {
            [HarmonyTranspiler, HarmonyPatch(typeof(PauseRegistrationList), "InitList")]
            private static IEnumerable<CodeInstruction> tpl_PauseRegistrationList_InitList(IEnumerable<CodeInstruction> _instructions) => ReplacePath(_instructions);

            [HarmonyTranspiler, HarmonyPatch(typeof(PauseCtrl), "Save")]
            private static IEnumerable<CodeInstruction> tpl_PauseCtrl_Save(IEnumerable<CodeInstruction> _instructions) => ReplacePath(_instructions);

            [HarmonyPostfix, HarmonyPatch(typeof(PauseRegistrationList), "InitList")]
            private static void post_PauseRegistrationList_InitList(PauseRegistrationList __instance)
            {
                if (v_prefabNode == null)
                {
                    v_prefabNode = (GameObject)AccessTools.Field(typeof(PauseRegistrationList), "prefabNode").GetValue(__instance);
                    v_transformRoot = (Transform)AccessTools.Field(typeof(PauseRegistrationList), "transformRoot").GetValue(__instance);
                }

                var dirs = CurrentDirectory.GetDirectories();
                for (var i = dirs.Length - 1; i >= 0; i--)
                {
                    var subDir = dirs[i];
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
