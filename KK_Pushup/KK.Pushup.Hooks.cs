using HarmonyLib;

namespace KK_Plugins
{
    public partial class Pushup
    {
        internal partial class Hooks
        {
            [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetClothesState))]
            internal static void SetClothesStatePostfix(ChaControl __instance) => GetCharaController(__instance)?.ClothesStateChangeEvent();
        }
    }
}
