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
            internal static void SetClothesStatePostfix(ChaControl __instance) => GetCharaController(__instance)?.ClothesStateChangeEvent();

            [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCustomClothes))]
            internal static void ChangeCustomClothes(ChaControl __instance, int kind) => GetCharaController(__instance)?.ChangeCustomClothesEvent(kind);

            [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeAccessory), typeof(int), typeof(int), typeof(int), typeof(string), typeof(bool))]
            internal static void ChangeAccessory(ChaControl __instance, int slotNo, int type) => GetCharaController(__instance)?.ChangeAccessoryEvent(slotNo, type);

            [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeHairAsync), typeof(int), typeof(int), typeof(bool), typeof(bool))]
            internal static void ChangeHair(ChaControl __instance, int kind) => GetCharaController(__instance)?.ChangeHairEvent(kind);

            [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.CreateBodyTexture))]
            internal static void CreateBodyTextureHook(ChaControl __instance) => GetCharaController(__instance).RefreshBodyMainTex();

#if AI
            internal static void ClothesColorChangeHook()
            {
                var controller = GetCharaController(MakerAPI.GetCharacterControl());
                controller.CustomClothesOverride = true;
                controller.RefreshClothesMainTex();
            }

            [HarmonyPrefix, HarmonyPatch(typeof(CharaCustom.CvsA_Copy), "CopyAccessory")]
            internal static void CopyAccessoryOverride() => GetCharaController(MakerAPI.GetCharacterControl()).CustomClothesOverride = true;
#else
            internal static void AccessoryTransferHook() => GetCharaController(MakerAPI.GetCharacterControl()).CustomClothesOverride = true;

            /// <summary>
            /// Transfer accessory hook
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(ChaCustom.CvsAccessoryChange), "CopyAcs")]
            internal static void CopyAcsHook() => GetCharaController(MakerAPI.GetCharacterControl()).CustomClothesOverride = true;

            //Clothing color change hooks
            [HarmonyPrefix, HarmonyPatch(typeof(ChaCustom.CvsClothes), nameof(ChaCustom.CvsClothes.FuncUpdateCosColor))]
            internal static void FuncUpdateCosColorHook()
            {
                var controller = GetCharaController(MakerAPI.GetCharacterControl());
                controller.CustomClothesOverride = true;
                controller.RefreshClothesMainTex();
            }

            [HarmonyPrefix, HarmonyPatch(typeof(ChaCustom.CvsClothes), nameof(ChaCustom.CvsClothes.FuncUpdatePattern01))]
            internal static void FuncUpdatePattern01Hook()
            {
                var controller = GetCharaController(MakerAPI.GetCharacterControl());
                controller.CustomClothesOverride = true;
                controller.RefreshClothesMainTex();
            }

            [HarmonyPrefix, HarmonyPatch(typeof(ChaCustom.CvsClothes), nameof(ChaCustom.CvsClothes.FuncUpdatePattern02))]
            internal static void FuncUpdatePattern02Hook()
            {
                var controller = GetCharaController(MakerAPI.GetCharacterControl());
                controller.CustomClothesOverride = true;
                controller.RefreshClothesMainTex();
            }

            [HarmonyPrefix, HarmonyPatch(typeof(ChaCustom.CvsClothes), nameof(ChaCustom.CvsClothes.FuncUpdatePattern03))]
            internal static void FuncUpdatePattern03Hook()
            {
                var controller = GetCharaController(MakerAPI.GetCharacterControl());
                controller.CustomClothesOverride = true;
                controller.RefreshClothesMainTex();
            }

            [HarmonyPrefix, HarmonyPatch(typeof(ChaCustom.CvsClothes), nameof(ChaCustom.CvsClothes.FuncUpdatePattern04))]
            internal static void FuncUpdatePattern04Hook()
            {
                var controller = GetCharaController(MakerAPI.GetCharacterControl());
                controller.CustomClothesOverride = true;
                controller.RefreshClothesMainTex();
            }

            [HarmonyPrefix, HarmonyPatch(typeof(ChaCustom.CvsClothes), nameof(ChaCustom.CvsClothes.FuncUpdateAllPtnAndColor))]
            internal static void FuncUpdateAllPtnAndColorHook()
            {
                var controller = GetCharaController(MakerAPI.GetCharacterControl());
                controller.CustomClothesOverride = true;
                controller.RefreshClothesMainTex();
            }
#endif
        }
    }
}