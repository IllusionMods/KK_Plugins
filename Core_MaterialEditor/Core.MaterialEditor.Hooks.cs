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

            [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.CreateBodyTexture))]
            public static void CreateBodyTextureHook(ChaControl __instance) => GetCharaController(__instance).RefreshBodyMainTex();

#if AI
            public static void ClothesColorChangeHook()
            {
                var controller = GetCharaController(MakerAPI.GetCharacterControl());
                controller.CustomClothesOverride = true;
                controller.RefreshClothesMainTex();
            }

            [HarmonyPrefix, HarmonyPatch(typeof(CharaCustom.CvsA_Copy), "CopyAccessory")]
            public static void CopyAccessoryOverride() => GetCharaController(MakerAPI.GetCharacterControl()).CustomClothesOverride = true;
#else
            public static void ClothesColorChangeHook() => GetCharaController(MakerAPI.GetCharacterControl()).CustomClothesOverride = true;

            [HarmonyPrefix, HarmonyPatch(typeof(ChaCustom.CvsAccessoryChange), "CopyAcs")]
            public static void CopyAcsHook() => GetCharaController(MakerAPI.GetCharacterControl()).CustomClothesOverride = true;

            [HarmonyPrefix, HarmonyPatch(typeof(ChaCustom.CvsClothes), nameof(ChaCustom.CvsClothes.FuncUpdateCosColor))]
            public static void FuncUpdateCosColorHook()
            {
                var controller = GetCharaController(MakerAPI.GetCharacterControl());
                controller.CustomClothesOverride = true;
                controller.RefreshClothesMainTex();
            }

            [HarmonyPrefix, HarmonyPatch(typeof(ChaCustom.CvsClothes), nameof(ChaCustom.CvsClothes.FuncUpdatePattern01))]
            public static void FuncUpdatePattern01Hook()
            {
                var controller = GetCharaController(MakerAPI.GetCharacterControl());
                controller.CustomClothesOverride = true;
                controller.RefreshClothesMainTex();
            }

            [HarmonyPrefix, HarmonyPatch(typeof(ChaCustom.CvsClothes), nameof(ChaCustom.CvsClothes.FuncUpdatePattern02))]
            public static void FuncUpdatePattern02Hook()
            {
                var controller = GetCharaController(MakerAPI.GetCharacterControl());
                controller.CustomClothesOverride = true;
                controller.RefreshClothesMainTex();
            }

            [HarmonyPrefix, HarmonyPatch(typeof(ChaCustom.CvsClothes), nameof(ChaCustom.CvsClothes.FuncUpdatePattern03))]
            public static void FuncUpdatePattern03Hook()
            {
                var controller = GetCharaController(MakerAPI.GetCharacterControl());
                controller.CustomClothesOverride = true;
                controller.RefreshClothesMainTex();
            }

            [HarmonyPrefix, HarmonyPatch(typeof(ChaCustom.CvsClothes), nameof(ChaCustom.CvsClothes.FuncUpdatePattern04))]
            public static void FuncUpdatePattern04Hook()
            {
                var controller = GetCharaController(MakerAPI.GetCharacterControl());
                controller.CustomClothesOverride = true;
                controller.RefreshClothesMainTex();
            }

            [HarmonyPrefix, HarmonyPatch(typeof(ChaCustom.CvsClothes), nameof(ChaCustom.CvsClothes.FuncUpdateAllPtnAndColor))]
            public static void FuncUpdateAllPtnAndColorHook()
            {
                var controller = GetCharaController(MakerAPI.GetCharacterControl());
                controller.CustomClothesOverride = true;
                controller.RefreshClothesMainTex();
            }
#endif
        }
    }
}