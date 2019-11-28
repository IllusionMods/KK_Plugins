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
            internal static void AddSonyuKokanPlay(HFlag __instance) => GetController(__instance.lstHeroine[0].chaCtrl).OnInsert();
            /// <summary>
            /// Insert anal
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuAnalPlay))]
            internal static void AddSonyuAnalPlay(HFlag __instance) => GetController(__instance.lstHeroine[0].chaCtrl).OnInsert();
            /// <summary>
            /// Something that happens at the end of H scene loading, good enough place to hook
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(HSceneProc), "MapSameObjectDisable")]
            internal static void MapSameObjectDisable(HSceneProc __instance)
            {
                SaveData.Heroine heroine = __instance.flags.lstHeroine[0];
                GetController(heroine.chaCtrl).HSceneStart(heroine.isVirgin && heroine.isAnalVirgin);
            }

            [HarmonyPrefix, HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuOrg))]
            internal static void AddSonyuOrg(HFlag __instance) => GetController(__instance.lstHeroine[0].chaCtrl).AddOrgasm();
            [HarmonyPrefix, HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuSame))]
            internal static void AddSonyuSame(HFlag __instance) => GetController(__instance.lstHeroine[0].chaCtrl).AddOrgasm();
            [HarmonyPrefix, HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuAnalOrg))]
            internal static void AddSonyuAnalOrg(HFlag __instance) => GetController(__instance.lstHeroine[0].chaCtrl).AddOrgasm();
            [HarmonyPrefix, HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuAnalSame))]
            internal static void AddSonyuAnalSame(HFlag __instance) => GetController(__instance.lstHeroine[0].chaCtrl).AddOrgasm();

            [HarmonyPrefix, HarmonyPatch(typeof(HSceneProc), "EndProc")]
            internal static void EndProc(HSceneProc __instance) => GetController(__instance.flags.lstHeroine[0].chaCtrl).HSceneEnd();
        }
    }
}