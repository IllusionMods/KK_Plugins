using HarmonyLib;
using Studio;

namespace KK_Plugins
{
    public partial class MaterialEditor
    {
        internal partial class Hooks
        {
            [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCoordinateType), typeof(ChaFileDefine.CoordinateType), typeof(bool))]
            internal static void ChangeCoordinateTypePrefix(ChaControl __instance) => GetCharaController(__instance)?.CoordinateChangeEvent();

            [HarmonyPrefix, HarmonyPatch(typeof(OCIItem), nameof(OCIItem.OnDelete))]
            internal static void OCIItemOnDelete(OCIItem __instance) => GetSceneController()?.ItemDeleteEvent(__instance.objectInfo.dicKey);
        }
    }
}