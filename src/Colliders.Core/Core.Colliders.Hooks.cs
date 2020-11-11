#if KK
using HarmonyLib;

namespace KK_Plugins
{
    public partial class Colliders
    {
        internal static class Hooks
        {
            [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCoordinateType), typeof(ChaFileDefine.CoordinateType), typeof(bool))]
            private static void ChangeCoordinateTypeHook(ChaControl __instance)
            {
                var controller = GetController(__instance);
                if (controller != null)
                    controller.ApplyColliders();
            }
        }
    }
}
#endif