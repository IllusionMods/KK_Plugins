using Harmony;
using KKAPI.Maker;
using System.Collections;
using UnityEngine.UI;

namespace HairAccessoryCustomizer
{
    internal class Hooks
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
#if KK
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCoordinateType), new[] { typeof(ChaFileDefine.CoordinateType), typeof(bool) })]
        public static void ChangeCoordinateType(ChaControl __instance) => __instance.StartCoroutine(ChangeCoordinateActions(__instance));
#endif
        private static IEnumerator ChangeCoordinateActions(ChaControl __instance)
        {
            var controller = HairAccessoryCustomizer.GetController(__instance);
            if (controller == null) yield break;
            if (HairAccessoryCustomizer.ReloadingChara) yield break;

            HairAccessoryCustomizer.ReloadingChara = true;
            yield return null;

            if (MakerAPI.InsideAndLoaded)
            {
                if (controller.InitHairAccessoryInfo(AccessoriesApi.SelectedMakerAccSlot))
                {
                    //switching to a hair accessory that previously had no data. Meaning this card was made before this plugin. ColorMatch and HairGloss should be off.
                    controller.SetColorMatch(false);
                    controller.SetHairGloss(false);
                }

                HairAccessoryCustomizer.InitCurrentSlot(controller);
            }

            controller.UpdateAccessories(!HairAccessoryCustomizer.ReloadingChara);
            HairAccessoryCustomizer.ReloadingChara = false;
        }
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
