using HarmonyLib;

namespace KK_Plugins
{
    public partial class Colliders
    {
        internal static class Hooks
        {
            [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCoordinateType), typeof(ChaFileDefine.CoordinateType), typeof(bool))]
            internal static void ChangeCoordinateTypeHook(ChaControl __instance) => GetController(__instance)?.ApplyColliders();
        }
    }
}
