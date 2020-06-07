using HarmonyLib;

namespace KK_Plugins.MaterialEditor
{
    internal static partial class Hooks
    {
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCoordinateType), typeof(ChaFileDefine.CoordinateType), typeof(bool))]
        internal static void ChangeCoordinateTypePrefix(ChaControl __instance) => MaterialEditorPlugin.GetCharaController(__instance)?.CoordinateChangeEvent();
    }
}