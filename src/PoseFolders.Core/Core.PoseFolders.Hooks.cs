using HarmonyLib;
using Studio;
using System.Collections.Generic;

namespace KK_Plugins
{
    public partial class PoseFolders
    {
        internal static class Hooks
        {
            [HarmonyTranspiler, HarmonyPatch(typeof(PauseRegistrationList), nameof(PauseRegistrationList.InitList))]
            private static IEnumerable<CodeInstruction> tpl_PauseRegistrationList_InitList(IEnumerable<CodeInstruction> _instructions) => ReplacePath(_instructions);

            [HarmonyTranspiler, HarmonyPatch(typeof(PauseCtrl), nameof(PauseCtrl.Save))]
            private static IEnumerable<CodeInstruction> tpl_PauseCtrl_Save(IEnumerable<CodeInstruction> _instructions) => ReplacePath(_instructions);

            [HarmonyPostfix, HarmonyPatch(typeof(PauseRegistrationList), nameof(PauseRegistrationList.InitList))]
            private static void post_PauseRegistrationList_InitList(PauseRegistrationList __instance)
            {
                if (v_prefabNode == null)
                {
                    v_prefabNode = __instance.prefabNode;
                    v_transformRoot = __instance.transformRoot;
                }

                var dirs = CurrentDirectory.GetDirectories();
                for (var i = dirs.Length - 1; i >= 0; i--)
                {
                    var subDir = dirs[i];
                    AddListButton($"[{subDir.Name}]", () =>
                    {
                        CurrentDirectory = subDir;
                        __instance.InitList();
                    }).SetAsFirstSibling();
                }
                var fn = CurrentDirectory.FullName;
                if (fn.Length > DefaultRootLength)
                {
                    AddListButton("..", () =>
                    {
                        CurrentDirectory = CurrentDirectory.Parent;
                        __instance.InitList();
                    }).SetAsFirstSibling();
                }
            }
        }
    }
}
