using HarmonyLib;
using KKAPI.Maker;
using UnityEngine.UI;

namespace KK_Plugins
{
    public partial class HairAccessoryCustomizer
    {
        internal partial class Hooks
        {
            [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingHairGlossMask))]
            internal static void ChangeSettingHairGlossMask(ChaControl __instance) => GetController(__instance).UpdateAccessories(!ReloadingChara);
            [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingHairColor))]
            internal static void ChangeSettingHairColor(ChaControl __instance) => GetController(__instance).UpdateAccessories(!ReloadingChara);
            [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingHairOutlineColor))]
            internal static void ChangeSettingHairOutlineColor(ChaControl __instance) => GetController(__instance).UpdateAccessories(!ReloadingChara);
            [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingHairAcsColor))]
            internal static void ChangeSettingHairAcsColor(ChaControl __instance) => GetController(__instance).UpdateAccessories(!ReloadingChara);
            [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeAccessoryColor))]
            internal static void ChangeAccessoryColor(ChaControl __instance, int slotNo) => GetController(__instance).UpdateAccessory(slotNo, false);
            [HarmonyPostfix, HarmonyPatch(typeof(ChaCustom.CvsAccessory), nameof(ChaCustom.CvsAccessory.ChangeUseColorVisible))]
            internal static void ChangeUseColorVisible(ChaCustom.CvsAccessory __instance)
            {
                if (!MakerAPI.InsideAndLoaded) return;
                if (AccessoriesApi.SelectedMakerAccSlot == (int)__instance.slotNo && GetController(MakerAPI.GetCharacterControl()).IsHairAccessory((int)__instance.slotNo) && ColorMatchToggle.GetSelectedValue())
                    HideAccColors();
            }
            [HarmonyPostfix, HarmonyPatch(typeof(ChaCustom.CvsAccessory), nameof(ChaCustom.CvsAccessory.ChangeSettingVisible))]
            internal static void ChangeSettingVisible(ChaCustom.CvsAccessory __instance)
            {
                if (!MakerAPI.InsideAndLoaded) return;
                if (GetController(MakerAPI.GetCharacterControl()).IsHairAccessory((int)__instance.slotNo) && ColorMatchToggle.GetSelectedValue())
                    Traverse.Create(AccessoriesApi.GetCvsAccessory((int)__instance.slotNo)).Field("btnInitColor").GetValue<Button>().transform.parent.gameObject.SetActive(false);
            }
        }
    }
}