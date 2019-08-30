using HarmonyLib;
using Studio;

namespace KK_Plugins
{
    public partial class MaterialEditor
    {
        internal partial class Hooks
        {
            [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCoordinateType), new[] { typeof(ChaFileDefine.CoordinateType), typeof(bool) })]
            public static void ChangeCoordinateTypePrefix(ChaControl __instance) => GetCharaController(__instance)?.CoordinateChangeEvent();

            [HarmonyPrefix, HarmonyPatch(typeof(OCIItem), nameof(OCIItem.OnDelete))]
            public static void OCIItemOnDelete(OCIItem __instance) => GetSceneController()?.ItemDeleteEvent(__instance.objectInfo.dicKey);
        }
    }
}