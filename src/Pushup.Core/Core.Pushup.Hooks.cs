using HarmonyLib;
using KKAPI.Maker;
using System.Collections;

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
            /// When the Breast tab of the character maker is set active. disable the sliders because the game will try to set them to the current body values.
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(ChaCustom.CustomBase), nameof(ChaCustom.CustomBase.updateCvsBreast), MethodType.Setter)]
            private static void UpdateCvsBreastPrefix()
            {
                SliderManager.SlidersActive = false;

                //Set the sliders active again after a delay, just in case they aren't set active by the user mouse entering the slider area (for example on slow computers where switching tabs locks the game)
                ChaCustom.CustomBase.Instance.StartCoroutine(SetSlidersActive());
                IEnumerator SetSlidersActive()
                {
                    yield return null;
                    yield return null;
                    yield return null;
                    SliderManager.SlidersActive = true;
                }
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
        }
    }
}
