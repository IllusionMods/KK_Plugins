using HarmonyLib;

namespace KK_Plugins
{
    public partial class EyeShaking
    {
        internal static class Hooks
        {
            /// <summary>
            /// Insert vaginal
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuKokanPlay))]
            private static void AddSonyuKokanPlay(HFlag __instance) => GetController(__instance.lstHeroine[0].chaCtrl).OnInsert();
            /// <summary>
            /// Insert anal
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuAnalPlay))]
            private static void AddSonyuAnalPlay(HFlag __instance) => GetController(__instance.lstHeroine[0].chaCtrl).OnInsert();

            [HarmonyPrefix, HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuOrg))]
            private static void AddSonyuOrg(HFlag __instance) => GetController(__instance.lstHeroine[0].chaCtrl).AddOrgasm();
            [HarmonyPrefix, HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuSame))]
            private static void AddSonyuSame(HFlag __instance) => GetController(__instance.lstHeroine[0].chaCtrl).AddOrgasm();
            [HarmonyPrefix, HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuAnalOrg))]
            private static void AddSonyuAnalOrg(HFlag __instance) => GetController(__instance.lstHeroine[0].chaCtrl).AddOrgasm();
            [HarmonyPrefix, HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuAnalSame))]
            private static void AddSonyuAnalSame(HFlag __instance) => GetController(__instance.lstHeroine[0].chaCtrl).AddOrgasm();

            /// <summary>
            /// Something that happens at the end of H scene loading, good enough place to hook
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(HSceneProc), "MapSameObjectDisable")]
            private static void MapSameObjectDisable(HSceneProc __instance)
            {
                SaveData.Heroine heroine = __instance.flags.lstHeroine[0];
                GetController(heroine.chaCtrl).HSceneStart(heroine.isVirgin && heroine.isAnalVirgin);
            }

            [HarmonyPrefix, HarmonyPatch(typeof(HSceneProc), "EndProc")]
            private static void EndProc(HSceneProc __instance)
            {
                GetController(__instance.flags.lstHeroine[0].chaCtrl).HSceneEnd();
            }

            internal static void MapSameObjectDisableVR(object __instance)
            {
                HFlag flags = (HFlag)Traverse.Create(__instance).Field("flags").GetValue();
                SaveData.Heroine heroine = flags.lstHeroine[0];
                GetController(heroine.chaCtrl).HSceneStart(heroine.isVirgin && heroine.isAnalVirgin);
            }

            internal static void EndProcVR(object __instance)
            {
                HFlag flags = (HFlag)Traverse.Create(__instance).Field("flags").GetValue();
                GetController(flags.lstHeroine[0].chaCtrl).HSceneEnd();
            }
        }
    }
}