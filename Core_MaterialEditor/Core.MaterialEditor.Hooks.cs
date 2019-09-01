using HarmonyLib;
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

        }
    }
}