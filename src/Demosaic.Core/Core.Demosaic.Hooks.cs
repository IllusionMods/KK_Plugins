using HarmonyLib;
#if AI || HS2
using AIChara;
#endif

namespace KK_Plugins
{
    public partial class Demosaic
    {
        internal static class Hooks
        {
            [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), "LateUpdateForce")]
            private static void LateUpdateForce(ChaControl __instance)
            {
                if (Enabled.Value)
                    __instance.hideMoz = true;
            }
        }
    }
}