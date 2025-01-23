using HarmonyLib;

namespace KK_Plugins
{
    public partial class HCharaAdjustment
    {
        internal static class Hooks
        {
            /// <summary>
            /// Something that happens at the end of H scene loading, good enough place to initialize stuff
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(HSceneProc), nameof(HSceneProc.SetShortcutKey))]
            private static void HSceneProc_SetShortcutKey(HSceneProc __instance)
            {
                for (int i = 0; i < __instance.flags.lstHeroine.Count; i++)
                    if (i == 0)
                        GetController(__instance.flags.lstHeroine[i].chaCtrl).CreateGuideObject(__instance, HCharaAdjustmentController.CharacterType.Female1);
                    else if (i == 1)
                        GetController(__instance.flags.lstHeroine[i].chaCtrl).CreateGuideObject(__instance, HCharaAdjustmentController.CharacterType.Female2);
                GetController(__instance.flags.player.chaCtrl).CreateGuideObject(__instance, HCharaAdjustmentController.CharacterType.Male);
            }

            /// <summary>
            /// Hide the guide objects when the H point picker scene is displayed
            /// </summary>
            /// <param name="__instance"></param>
            [HarmonyPrefix, HarmonyPatch(typeof(HSceneProc), nameof(HSceneProc.GotoPointMoveScene))]
            private static void GotoPointMoveScene(HSceneProc __instance)
            {
                HideGuideObjects();
            }

#if KKS
            /// <summary>
            /// Hide guide objects when changing H-points due to animation changes
            /// </summary>
            /// <param name="__instance"></param>
            [HarmonyPostfix, HarmonyPatch(typeof(HSceneProc), nameof(HSceneProc.MovePointByChangeAnim))]
            private static void MovePointByChangeAnim(HSceneProc __instance)
            {
                HideGuideObjects();
            }
#endif

            private static void HideGuideObjects()
            {
                foreach (var controller in GetAllControllers())
                    controller.HideGuideObject();
            }

            /// <summary>
            /// Set the new original position when changing positions via the H point picker scene
            /// </summary>
            /// <param name="__instance"></param>
            [HarmonyPostfix, HarmonyPatch(typeof(HSceneProc), nameof(HSceneProc.ChangeCategory))]
            private static void ChangeCategory(HSceneProc __instance)
            {
                foreach (var controller in GetAllControllers())
                    controller.SetGuideObjectOriginalPosition();
            }
        }
    }
}
