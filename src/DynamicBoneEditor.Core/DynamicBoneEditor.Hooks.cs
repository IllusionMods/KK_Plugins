using HarmonyLib;

namespace KK_Plugins.DynamicBoneEditor
{
    internal class Hooks
    {
#if KK
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCoordinateType), typeof(ChaFileDefine.CoordinateType), typeof(bool))]
        private static void ChangeCoordinateTypePrefix(ChaControl __instance)
        {
            var controller = Plugin.GetCharaController(__instance);
            if (controller != null)
                controller.CoordinateChangeEvent();
        }
#endif
    }
}
