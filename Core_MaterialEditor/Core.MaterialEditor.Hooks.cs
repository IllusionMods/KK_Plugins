using HarmonyLib;
using KKAPI.Maker;
#if AI
using AIChara;
#endif

namespace KK_Plugins
{
    public partial class MaterialEditor
    {
        internal partial class Hooks
        {
            [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetClothesState))]
            public static void SetClothesStatePostfix(ChaControl __instance) => GetCharaController(__instance)?.ClothesStateChangeEvent();

            [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCustomClothes))]
            public static void ChangeCustomClothes(ChaControl __instance, int kind) => GetCharaController(__instance)?.ChangeCustomClothesEvent(kind);

            [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeAccessory), new[] { typeof(int), typeof(int), typeof(int), typeof(string), typeof(bool) })]
            public static void ChangeAccessory(ChaControl __instance, int slotNo, int type) => GetCharaController(__instance)?.ChangeAccessoryEvent(slotNo, type);

            [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeHairAsync), new[] { typeof(int), typeof(int), typeof(bool), typeof(bool) })]
            public static void ChangeHair(ChaControl __instance, int kind) => GetCharaController(__instance)?.ChangeHairEvent(kind);

#if AI
            public static void OverrideHook() => GetCharaController(MakerAPI.GetCharacterControl()).CustomClothesOverride = true;

            [HarmonyPrefix, HarmonyPatch(typeof(CharaCustom.CvsA_Copy), "CopyAccessory")]
            public static void CopyAccessoryOverride() => GetCharaController(MakerAPI.GetCharacterControl()).CustomClothesOverride = true;
#else
            public static void OverrideHook() => GetCharaController(MakerAPI.GetCharacterControl()).CustomClothesOverride = true;

            [HarmonyPrefix, HarmonyPatch(typeof(ChaCustom.CvsAccessoryChange), "CopyAcs")]
            public static void CopyAcsOverride() => GetCharaController(MakerAPI.GetCharacterControl()).CustomClothesOverride = true;

            [HarmonyPrefix, HarmonyPatch(typeof(ChaCustom.CvsClothes), nameof(ChaCustom.CvsClothes.FuncUpdateCosColor))]
            public static void FuncUpdateCosColorOverride() => GetCharaController(MakerAPI.GetCharacterControl()).CustomClothesOverride = true;

            [HarmonyPrefix, HarmonyPatch(typeof(ChaCustom.CvsClothes), nameof(ChaCustom.CvsClothes.FuncUpdatePattern01))]
            public static void FuncUpdatePattern01Override() => GetCharaController(MakerAPI.GetCharacterControl()).CustomClothesOverride = true;

            [HarmonyPrefix, HarmonyPatch(typeof(ChaCustom.CvsClothes), nameof(ChaCustom.CvsClothes.FuncUpdatePattern02))]
            public static void FuncUpdatePattern02Override() => GetCharaController(MakerAPI.GetCharacterControl()).CustomClothesOverride = true;

            [HarmonyPrefix, HarmonyPatch(typeof(ChaCustom.CvsClothes), nameof(ChaCustom.CvsClothes.FuncUpdatePattern03))]
            public static void FuncUpdatePattern03Override() => GetCharaController(MakerAPI.GetCharacterControl()).CustomClothesOverride = true;

            [HarmonyPrefix, HarmonyPatch(typeof(ChaCustom.CvsClothes), nameof(ChaCustom.CvsClothes.FuncUpdatePattern04))]
            public static void FuncUpdatePattern04Override() => GetCharaController(MakerAPI.GetCharacterControl()).CustomClothesOverride = true;

            [HarmonyPrefix, HarmonyPatch(typeof(ChaCustom.CvsClothes), nameof(ChaCustom.CvsClothes.FuncUpdateAllPtnAndColor))]
            public static void FuncUpdateAllPtnAndColorOverride() => GetCharaController(MakerAPI.GetCharacterControl()).CustomClothesOverride = true;
#endif

        }
    }
}