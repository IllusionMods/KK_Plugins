using HarmonyLib;
using KKAPI.Maker;
using UnityEngine.UI;

namespace KK_Plugins
{
    internal partial class Hooks
    {
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingHairGlossMask))]
        internal static void ChangeSettingHairGlossMask(ChaControl __instance) => HairAccessoryCustomizer.GetController(__instance).UpdateAccessories(!HairAccessoryCustomizer.ReloadingChara);
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingHairColor))]
        internal static void ChangeSettingHairColor(ChaControl __instance) => HairAccessoryCustomizer.GetController(__instance).UpdateAccessories(!HairAccessoryCustomizer.ReloadingChara);
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingHairOutlineColor))]
        internal static void ChangeSettingHairOutlineColor(ChaControl __instance) => HairAccessoryCustomizer.GetController(__instance).UpdateAccessories(!HairAccessoryCustomizer.ReloadingChara);
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingHairAcsColor))]
        internal static void ChangeSettingHairAcsColor(ChaControl __instance) => HairAccessoryCustomizer.GetController(__instance).UpdateAccessories(!HairAccessoryCustomizer.ReloadingChara);
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeAccessoryColor))]
        internal static void ChangeAccessoryColor(ChaControl __instance, int slotNo) => HairAccessoryCustomizer.GetController(__instance).UpdateAccessory(slotNo, false);
        [HarmonyPostfix, HarmonyPatch(typeof(ChaCustom.CvsAccessory), nameof(ChaCustom.CvsAccessory.ChangeUseColorVisible))]
        internal static void ChangeUseColorVisible(ChaCustom.CvsAccessory __instance)
        {
            if (AccessoriesApi.SelectedMakerAccSlot == (int)__instance.slotNo && HairAccessoryCustomizer.GetController(MakerAPI.GetCharacterControl()).IsHairAccessory((int)__instance.slotNo) && HairAccessoryCustomizer.ColorMatchToggle.GetSelectedValue())
                HairAccessoryCustomizer.HideAccColors();
        }
        [HarmonyPostfix, HarmonyPatch(typeof(ChaCustom.CvsAccessory), nameof(ChaCustom.CvsAccessory.ChangeSettingVisible))]
        internal static void ChangeSettingVisible(ChaCustom.CvsAccessory __instance)
        {
            if (HairAccessoryCustomizer.GetController(MakerAPI.GetCharacterControl()).IsHairAccessory((int)__instance.slotNo) && HairAccessoryCustomizer.ColorMatchToggle.GetSelectedValue())
                Traverse.Create(AccessoriesApi.GetCvsAccessory((int)__instance.slotNo)).Field("btnInitColor").GetValue<Button>().transform.parent.gameObject.SetActive(false);
        }
    }
}
