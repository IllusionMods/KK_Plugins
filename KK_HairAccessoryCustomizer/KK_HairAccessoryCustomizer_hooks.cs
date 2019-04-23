using Harmony;
using KKAPI.Maker;
using System.Collections;
using UnityEngine.UI;

namespace KK_HairAccessoryCustomizer
{
    internal class KK_HairAccessoryCustomizer_hooks
    {
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingHairGlossMask))]
        public static void ChangeSettingHairGlossMask(ChaControl __instance)
        {
            if (!KK_HairAccessoryCustomizer.ReloadingChara)
                KK_HairAccessoryCustomizer.GetController(__instance).UpdateAccessories();
        }
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingHairColor))]
        public static void ChangeSettingHairColor(ChaControl __instance)
        {
            if (!KK_HairAccessoryCustomizer.ReloadingChara)
                KK_HairAccessoryCustomizer.GetController(__instance).UpdateAccessories();
        }
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingHairOutlineColor))]
        public static void ChangeSettingHairOutlineColor(ChaControl __instance)
        {
            if (!KK_HairAccessoryCustomizer.ReloadingChara)
                KK_HairAccessoryCustomizer.GetController(__instance).UpdateAccessories();
        }
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingHairAcsColor))]
        public static void ChangeSettingHairAcsColor(ChaControl __instance)
        {
            if (!KK_HairAccessoryCustomizer.ReloadingChara)
                KK_HairAccessoryCustomizer.GetController(__instance).UpdateAccessories();
        }
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeAccessoryColor))]
        public static void ChangeAccessoryColor(ChaControl __instance, int slotNo) => KK_HairAccessoryCustomizer.GetController(__instance).UpdateHairGloss(slotNo);
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCoordinateType), new[] { typeof(ChaFileDefine.CoordinateType), typeof(bool) })]
        public static void ChangeCoordinateType(ChaControl __instance)
        {
            if (!KK_HairAccessoryCustomizer.ReloadingChara)
                __instance.StartCoroutine(ChangeCoordinateActions(__instance));
        }
        private static IEnumerator ChangeCoordinateActions(ChaControl __instance)
        {
            var controller = KK_HairAccessoryCustomizer.GetController(__instance);
            if (controller == null) yield break;
            KK_HairAccessoryCustomizer.ReloadingChara = true;
            yield return null;

            if (MakerAPI.InsideAndLoaded)
            {
                if (controller.InitHairAccessoryInfo(AccessoriesApi.SelectedMakerAccSlot))
                {
                    //switching to a hair accessory that previously had no data. Meaning this card was made before this plugin. ColorMatch and HairGloss should be off.
                    controller.SetColorMatch(false);
                    controller.SetHairGloss(false);
                }

                KK_HairAccessoryCustomizer.InitCurrentSlot(controller);
            }

            controller.UpdateAccessories();
            KK_HairAccessoryCustomizer.ReloadingChara = false;
        }
        [HarmonyPostfix, HarmonyPatch(typeof(ChaCustom.CvsAccessory), nameof(ChaCustom.CvsAccessory.ChangeUseColorVisible))]
        public static void ChangeUseColorVisible(ChaCustom.CvsAccessory __instance)
        {
            if (AccessoriesApi.SelectedMakerAccSlot == (int)__instance.slotNo && KK_HairAccessoryCustomizer.GetController(MakerAPI.GetCharacterControl()).IsHairAccessory((int)__instance.slotNo) && KK_HairAccessoryCustomizer.ColorMatchToggle.GetSelectedValue())
                KK_HairAccessoryCustomizer.HideAccColors();
        }
        [HarmonyPostfix, HarmonyPatch(typeof(ChaCustom.CvsAccessory), nameof(ChaCustom.CvsAccessory.ChangeSettingVisible))]
        public static void ChangeSettingVisible(ChaCustom.CvsAccessory __instance)
        {
            if (KK_HairAccessoryCustomizer.GetController(MakerAPI.GetCharacterControl()).IsHairAccessory((int)__instance.slotNo) && KK_HairAccessoryCustomizer.ColorMatchToggle.GetSelectedValue())
                Traverse.Create(AccessoriesApi.GetCvsAccessory((int)__instance.slotNo)).Field("btnInitColor").GetValue<Button>().transform.parent.gameObject.SetActive(false);
        }
    }
}
