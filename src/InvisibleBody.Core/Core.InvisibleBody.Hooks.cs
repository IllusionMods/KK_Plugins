using HarmonyLib;
#if AI || HS2
using AIChara;
#endif

namespace KK_Plugins
{
    public partial class InvisibleBody
    {
        internal static class Hooks
        {
            /// <summary>
            /// For changing head shape. Also for low poly.
            /// </summary>
            /// <param name="__instance"></param>
            [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.InitShapeFace))]
            private static void InitShapeFace(ChaControl __instance) => GetController(__instance).UpdateVisible(true);
        }
    }
}
