using HarmonyLib;

namespace KK_Plugins
{
    public partial class Pushup
    {
        internal partial class Hooks
        {
            /// <summary>
            /// Trigger the ClothesStateChangeEvent
            /// </summary>
            [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetClothesState))]
            internal static void SetClothesStatePostfix(ChaControl __instance) => GetCharaController(__instance)?.ClothesStateChangeEvent();

            /// <summary>
            /// Set the CharacterLoading flag
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.CreateBodyTexture))]
            internal static void CreateBodyTextureHook(ChaControl __instance)
            {
                var controller = GetCharaController(__instance);
                if (controller != null)
                    controller.CharacterLoading = true;
            }
        }
    }
}
