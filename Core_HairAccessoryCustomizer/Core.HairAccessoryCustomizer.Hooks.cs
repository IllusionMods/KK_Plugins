using KKAPI.Maker;
using UnityEngine.UI;
#if KK
using Harmony;
#else
using HarmonyLib;
#endif

namespace HairAccessoryCustomizer
{
    internal partial class Hooks
    {
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingHairGlossMask))]
        public static void ChangeSettingHairGlossMask(ChaControl __instance) => HairAccessoryCustomizer.GetController(__instance).UpdateAccessories(!HairAccessoryCustomizer.ReloadingChara);
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingHairColor))]
        public static void ChangeSettingHairColor(ChaControl __instance) => HairAccessoryCustomizer.GetController(__instance).UpdateAccessories(!HairAccessoryCustomizer.ReloadingChara);
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingHairOutlineColor))]
        public static void ChangeSettingHairOutlineColor(ChaControl __instance) => HairAccessoryCustomizer.GetController(__instance).UpdateAccessories(!HairAccessoryCustomizer.ReloadingChara);
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingHairAcsColor))]
        public static void ChangeSettingHairAcsColor(ChaControl __instance) => HairAccessoryCustomizer.GetController(__instance).UpdateAccessories(!HairAccessoryCustomizer.ReloadingChara);
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeAccessoryColor))]
        public static void ChangeAccessoryColor(ChaControl __instance, int slotNo) => HairAccessoryCustomizer.GetController(__instance).UpdateAccessory(slotNo, false);
        [HarmonyPostfix, HarmonyPatch(typeof(ChaCustom.CvsAccessory), nameof(ChaCustom.CvsAccessory.ChangeUseColorVisible))]
        public static void ChangeUseColorVisible(ChaCustom.CvsAccessory __instance)
        {
            if (AccessoriesApi.SelectedMakerAccSlot == (int)__instance.slotNo && HairAccessoryCustomizer.GetController(MakerAPI.GetCharacterControl()).IsHairAccessory((int)__instance.slotNo) && HairAccessoryCustomizer.ColorMatchToggle.GetSelectedValue())
                HairAccessoryCustomizer.HideAccColors();
        }
        [HarmonyPostfix, HarmonyPatch(typeof(ChaCustom.CvsAccessory), nameof(ChaCustom.CvsAccessory.ChangeSettingVisible))]
        public static void ChangeSettingVisible(ChaCustom.CvsAccessory __instance)
        {
            if (HairAccessoryCustomizer.GetController(MakerAPI.GetCharacterControl()).IsHairAccessory((int)__instance.slotNo) && HairAccessoryCustomizer.ColorMatchToggle.GetSelectedValue())
                Traverse.Create(AccessoriesApi.GetCvsAccessory((int)__instance.slotNo)).Field("btnInitColor").GetValue<Button>().transform.parent.gameObject.SetActive(false);
        }
    }
}
