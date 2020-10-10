using HarmonyLib;
using KKAPI.Maker;
#if AI || HS2
using AIChara;
#endif

namespace KK_Plugins.MaterialEditor
{
    internal partial class Hooks
    {
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetClothesState))]
        internal static void SetClothesStatePostfix(ChaControl __instance)
        {
            var controller = MaterialEditorPlugin.GetCharaController(__instance);
            if (controller != null)
                controller.ClothesStateChangeEvent();
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCustomClothes))]
        internal static void ChangeCustomClothes(ChaControl __instance, int kind)
        {
            var controller = MaterialEditorPlugin.GetCharaController(__instance);
            if (controller != null)
                controller.ChangeCustomClothesEvent(kind);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeAccessory), typeof(int), typeof(int), typeof(int), typeof(string), typeof(bool))]
        internal static void ChangeAccessory(ChaControl __instance, int slotNo, int type)
        {
            var controller = MaterialEditorPlugin.GetCharaController(__instance);
            if (controller != null)
                controller.ChangeAccessoryEvent(slotNo, type);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeHairAsync), typeof(int), typeof(int), typeof(bool), typeof(bool))]
        internal static void ChangeHair(ChaControl __instance, int kind)
        {
            var controller = MaterialEditorPlugin.GetCharaController(__instance);
            if (controller != null)
                controller.ChangeHairEvent(kind);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.CreateBodyTexture))]
        internal static void CreateBodyTextureHook(ChaControl __instance)
        {
            var controller = MaterialEditorPlugin.GetCharaController(__instance);
            if (controller != null)
                controller.RefreshBodyMainTex();
        }

#if AI || HS2
        internal static void ClothesColorChangeHook()
        {
            var controller = MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl());
            controller.CustomClothesOverride = true;
            controller.RefreshClothesMainTex();
        }

        [HarmonyPrefix, HarmonyPatch(typeof(CharaCustom.CvsA_Copy), "CopyAccessory")]
        internal static void CopyAccessoryOverride() => MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).CustomClothesOverride = true;
#else
        internal static void AccessoryTransferHook() => MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).CustomClothesOverride = true;

        /// <summary>
        /// Transfer accessory hook
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(ChaCustom.CvsAccessoryChange), "CopyAcs")]
        internal static void CopyAcsHook() => MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).CustomClothesOverride = true;

        //Clothing color change hooks
        [HarmonyPrefix, HarmonyPatch(typeof(ChaCustom.CvsClothes), nameof(ChaCustom.CvsClothes.FuncUpdateCosColor))]
        internal static void FuncUpdateCosColorHook()
        {
            var controller = MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl());
            controller.CustomClothesOverride = true;
            controller.RefreshClothesMainTex();
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ChaCustom.CvsClothes), nameof(ChaCustom.CvsClothes.FuncUpdatePattern01))]
        internal static void FuncUpdatePattern01Hook()
        {
            var controller = MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl());
            controller.CustomClothesOverride = true;
            controller.RefreshClothesMainTex();
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ChaCustom.CvsClothes), nameof(ChaCustom.CvsClothes.FuncUpdatePattern02))]
        internal static void FuncUpdatePattern02Hook()
        {
            var controller = MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl());
            controller.CustomClothesOverride = true;
            controller.RefreshClothesMainTex();
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ChaCustom.CvsClothes), nameof(ChaCustom.CvsClothes.FuncUpdatePattern03))]
        internal static void FuncUpdatePattern03Hook()
        {
            var controller = MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl());
            controller.CustomClothesOverride = true;
            controller.RefreshClothesMainTex();
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ChaCustom.CvsClothes), nameof(ChaCustom.CvsClothes.FuncUpdatePattern04))]
        internal static void FuncUpdatePattern04Hook()
        {
            var controller = MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl());
            controller.CustomClothesOverride = true;
            controller.RefreshClothesMainTex();
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ChaCustom.CvsClothes), nameof(ChaCustom.CvsClothes.FuncUpdateAllPtnAndColor))]
        internal static void FuncUpdateAllPtnAndColorHook()
        {
            var controller = MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl());
            controller.CustomClothesOverride = true;
            controller.RefreshClothesMainTex();
        }
#endif
    }
}
