using ChaCustom;
using HarmonyLib;
using KKAPI.Maker;
using System.Collections;
using UnityEngine.UI;

namespace KK_Plugins
{
    public partial class HairAccessoryCustomizer
    {
        internal partial class Hooks
        {
            [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingHairGlossMask))]
            private static void ChangeSettingHairGlossMask(ChaControl __instance) => GetController(__instance).UpdateAccessories(!ReloadingChara);
            [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingHairColor))]
            private static void ChangeSettingHairColor(ChaControl __instance) => GetController(__instance).UpdateAccessories(!ReloadingChara);
            [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingHairOutlineColor))]
            private static void ChangeSettingHairOutlineColor(ChaControl __instance) => GetController(__instance).UpdateAccessories(!ReloadingChara);
            [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingHairAcsColor))]
            private static void ChangeSettingHairAcsColor(ChaControl __instance) => GetController(__instance).UpdateAccessories(!ReloadingChara);
            [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeAccessoryColor))]
            private static void ChangeAccessoryColor(ChaControl __instance, int slotNo) => GetController(__instance).UpdateAccessory(slotNo, false);
            [HarmonyPostfix, HarmonyPatch(typeof(ChaCustom.CvsAccessory), nameof(ChaCustom.CvsAccessory.ChangeUseColorVisible))]
            private static void ChangeUseColorVisible(ChaCustom.CvsAccessory __instance)
            {
                if (!MakerAPI.InsideAndLoaded) return;
                if (AccessoriesApi.SelectedMakerAccSlot == (int)__instance.slotNo && GetController(MakerAPI.GetCharacterControl()).IsHairAccessory((int)__instance.slotNo) && ColorMatchToggle.GetSelectedValue())
                    HideAccColors();
            }
            [HarmonyPostfix, HarmonyPatch(typeof(ChaCustom.CvsAccessory), nameof(ChaCustom.CvsAccessory.ChangeSettingVisible))]
            private static void ChangeSettingVisible(ChaCustom.CvsAccessory __instance)
            {
                if (!MakerAPI.InsideAndLoaded) return;
                if (GetController(MakerAPI.GetCharacterControl()).IsHairAccessory((int)__instance.slotNo) && ColorMatchToggle.GetSelectedValue())
                    Traverse.Create(AccessoriesApi.GetMakerAccessoryPageObject((int)__instance.slotNo).GetComponent<CvsAccessory>()).Field("btnInitColor").GetValue<Button>().transform.parent.gameObject.SetActive(false);
            }
#if KK
            [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCoordinateType), typeof(ChaFileDefine.CoordinateType), typeof(bool))]
            private static void ChangeCoordinateType(ChaControl __instance) => __instance.StartCoroutine(ChangeCoordinateActions(__instance));

            private static IEnumerator ChangeCoordinateActions(ChaControl __instance)
            {
                var controller = GetController(__instance);
                if (controller == null) yield break;
                if (ReloadingChara) yield break;

                ReloadingChara = true;
                yield return null;

                if (MakerAPI.InsideAndLoaded)
                {
                    if (controller.InitHairAccessoryInfo(AccessoriesApi.SelectedMakerAccSlot))
                    {
                        //switching to a hair accessory that previously had no data. Meaning this card was made before this plugin. ColorMatch and HairGloss should be off.
                        controller.SetColorMatch(false);
                        controller.SetHairGloss(false);
                    }

                    InitCurrentSlot(controller);
                }

                controller.UpdateAccessories();
                ReloadingChara = false;
            }
#endif
        }
    }
}