﻿using HarmonyLib;
using KKAPI.Maker;

namespace KK_Plugins
{
    public partial class Pushup
    {
        internal partial class Hooks
        {
            /// <summary>
            /// Trigger the ClothesStateChangeEvent for tops and bras
            /// </summary>
            [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetClothesState))]
            private static void SetClothesStatePostfix(ChaControl __instance, int clothesKind)
            {
                if (clothesKind == 0 || clothesKind == 2) //tops and bras
                {
                    var controller = GetCharaController(__instance);
                    if (controller != null)
                        controller.ClothesStateChangeEvent();
                }
            }

            /// <summary>
            /// Set the CharacterLoading flag
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.CreateBodyTexture))]
            private static void CreateBodyTextureHook(ChaControl __instance)
            {
                var controller = GetCharaController(__instance);
                if (controller != null)
                    controller.CharacterLoading = true;
            }

            /// <summary>
            /// When the Breast tab of the character maker is set active, disable Pushup because the game will try to set the sliders to the current body values.
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(ChaCustom.CustomBase), nameof(ChaCustom.CustomBase.updateCvsBreast), MethodType.Setter)]
            private static void UpdateCvsBreastPrefix()
            {
                var controller = GetCharaController(MakerAPI.GetCharacterControl());
                if (controller != null)
                    controller.MapBodyInfoToChaFile(controller.BaseData);
            }

            /// <summary>
            /// Re-enable Pushup
            /// </summary>
            [HarmonyPostfix, HarmonyPatch(typeof(ChaCustom.CustomBase), nameof(ChaCustom.CustomBase.updateCvsBreast), MethodType.Setter)]
            private static void UpdateCvsBreastPostfix()
            {
                var controller = GetCharaController(MakerAPI.GetCharacterControl());
                if (controller != null)
                    controller.RecalculateBody();
            }

            [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCustomClothes))]
            private static void ChangeCustomClothes(ChaControl __instance, int kind)
            {
                if (MakerAPI.InsideAndLoaded)
                    if (kind == 0 || kind == 2) //Tops and bras
                    {
                        var controller = GetCharaController(__instance);
                        if (controller != null)
                            controller.ClothesChangeEvent();
                    }
            }

            /// <summary>
            /// Cancel the original slider onValueChanged event
            /// </summary>
            internal static bool SliderHook() => false;

#if !EC
            internal static void CoordinateCountChangedPostHook()
            {
                ReloadCoordinateDropdown();
            }
#endif
        }
    }
}
