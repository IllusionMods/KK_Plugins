using HarmonyLib;
using KKAPI.Maker;

namespace KK_Plugins.MaterialEditor
{
    internal static class MakerHooks
    {
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCoordinateType), typeof(ChaFileDefine.CoordinateType), typeof(bool))]
        internal static void ChangeCoordinateTypePrefix()
        {
            if (MakerAPI.InsideAndLoaded)
                UI.HideUI();
        }
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeAccessory), typeof(int), typeof(int), typeof(int), typeof(string), typeof(bool))]
        internal static void ChangeAccessory()
        {
            if (MakerAPI.InsideAndLoaded)
                UI.HideUI();
        }
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCustomClothes))]
        internal static void ChangeCustomClothes()
        {
            if (MakerAPI.InsideAndLoaded)
                UI.HideUI();
        }
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeHairAsync), typeof(int), typeof(int), typeof(bool), typeof(bool))]
        internal static void ChangeHair()
        {
            if (MakerAPI.InsideAndLoaded)
                UI.HideUI();
        }
    }
}
