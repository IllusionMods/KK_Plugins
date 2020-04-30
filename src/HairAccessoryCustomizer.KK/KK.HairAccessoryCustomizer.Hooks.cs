using HarmonyLib;
using KKAPI.Maker;
using System.Collections;

namespace KK_Plugins
{
    public partial class HairAccessoryCustomizer
    {
        internal partial class Hooks
        {
            [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCoordinateType), typeof(ChaFileDefine.CoordinateType), typeof(bool))]
            internal static void ChangeCoordinateType(ChaControl __instance) => __instance.StartCoroutine(ChangeCoordinateActions(__instance));

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

                controller.UpdateAccessories(true);
                ReloadingChara = false;
            }
        }
    }
}