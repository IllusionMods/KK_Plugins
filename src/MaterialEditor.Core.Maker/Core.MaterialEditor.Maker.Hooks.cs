using HarmonyLib;
using KKAPI.Maker;
#if AI || HS2
using AIChara;
#endif

namespace KK_Plugins.MaterialEditor
{
    internal static partial class MakerHooks
    {
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
