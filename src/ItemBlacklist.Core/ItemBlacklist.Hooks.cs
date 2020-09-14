using ChaCustom;
using HarmonyLib;
using UnityEngine;

namespace KK_Plugins
{
    public partial class ItemBlacklist
    {
        internal static class Hooks
        {
            [HarmonyPostfix, HarmonyPatch(typeof(CustomSelectListCtrl), "OnPointerEnter")]
            internal static void OnPointerEnterPostfix(CustomSelectListCtrl __instance, GameObject obj)
            {
                CustomSelectListCtrlInstance = __instance;
                CurrentCustomSelectInfoComponent = obj?.GetComponent<CustomSelectInfoComponent>();
                MouseIn = true;
            }
            [HarmonyPostfix, HarmonyPatch(typeof(CustomSelectListCtrl), "OnPointerExit")]
            internal static void OnPointerExitPostfix() => MouseIn = false;

            [HarmonyPostfix, HarmonyPatch(typeof(CustomSelectListCtrl), "Create")]
            internal static void CustomSelectKindInitialize(CustomSelectListCtrl __instance) => ChangeListFilter(__instance, ListVisibilityType.Filtered);
        }
    }
}