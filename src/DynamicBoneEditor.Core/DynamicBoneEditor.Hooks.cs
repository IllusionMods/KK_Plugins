using HarmonyLib;
#if AI || HS2
using AIChara;
#endif

namespace KK_Plugins.DynamicBoneEditor
{
    internal class Hooks
    {
#if KK || KKS
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCoordinateType), typeof(ChaFileDefine.CoordinateType), typeof(bool))]
        private static void ChangeCoordinateTypePrefix(ChaControl __instance)
        {
            var controller = Plugin.GetCharaController(__instance);
            if (controller != null)
                controller.CoordinateChangeEvent();
        }
#endif

#if !PH
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeAccessory), typeof(int), typeof(int), typeof(int), typeof(string), typeof(bool))]
        private static void ChangeAccessory()
        {
            UI.ToggleButtonVisibility();
        }
#endif
    }
}
