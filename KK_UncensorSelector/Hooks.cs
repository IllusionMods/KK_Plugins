using Harmony;
using KKAPI.Maker;

namespace KK_UncensorSelector
{
    class Hooks
    {
        /// <summary>
        /// Do color matching whenever the body texture is changed
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetBodyBaseMaterial))]
        public static void SetBodyBaseMaterial(ChaControl __instance)
        {
            if (MakerAPI.InsideAndLoaded)
                ColorMatch.ColorMatchMaterials(__instance, KK_UncensorSelector.SelectedUncensor, KK_UncensorSelector.SelectedPenis, KK_UncensorSelector.SelectedBalls);
        }
        /// <summary>
        /// LineMask texture assigned to the material, toggled on and off for any color matching parts along with the body
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.VisibleAddBodyLine))]
        public static void VisibleAddBodyLine(ChaControl __instance) => ColorMatch.SetLineVisibility(__instance, KK_UncensorSelector.SelectedUncensor, KK_UncensorSelector.SelectedPenis, KK_UncensorSelector.SelectedBalls);
        /// <summary>
        /// Skin gloss slider level, as assigned in the character maker. This corresponds to the red coloring in the DetailMask texture.
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingSkinGlossPower))]
        public static void ChangeSettingSkinGlossPower(ChaControl __instance) => ColorMatch.SetSkinGloss(__instance, KK_UncensorSelector.SelectedUncensor, KK_UncensorSelector.SelectedPenis, KK_UncensorSelector.SelectedBalls);
        /// <summary>
        /// Demosaic
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), "LateUpdateForce")]
        public static void LateUpdateForce(ChaControl __instance) => __instance.hideMoz = true;
    }
}
