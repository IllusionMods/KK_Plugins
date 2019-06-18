using Harmony;
using KKAPI.Maker;
using System.Collections;

namespace HairAccessoryCustomizer
{
    internal partial class Hooks
    {
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCoordinateType), new[] { typeof(ChaFileDefine.CoordinateType), typeof(bool) })]
        public static void ChangeCoordinateType(ChaControl __instance) => __instance.StartCoroutine(ChangeCoordinateActions(__instance));
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

    }
}
