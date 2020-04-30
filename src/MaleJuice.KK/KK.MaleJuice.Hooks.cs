using HarmonyLib;
using Studio;

namespace KK_Plugins
{
    public partial class MaleJuice
    {
        internal static class Hooks
        {
            /// <summary>
            /// Set juice flags for males in Studio
            /// </summary>
            [HarmonyPostfix, HarmonyPatch(typeof(OCIChar), nameof(OCIChar.SetSiruFlags))]
            internal static void SetSiruFlags(ChaFileDefine.SiruParts _parts, byte _state, OCIChar __instance)
            {
                if (__instance is OCICharMale charMale)
                    charMale.male.SetSiruFlags(_parts, _state);
            }
            /// <summary>
            /// Get juice flags for males in Studio
            /// </summary>
            [HarmonyPostfix, HarmonyPatch(typeof(OCIChar), nameof(OCIChar.GetSiruFlags))]
            internal static void GetSiruFlags(ChaFileDefine.SiruParts _parts, OCIChar __instance, ref byte __result)
            {
                if (__instance is OCICharMale charMale)
                    __result = charMale.male.GetSiruFlags(_parts);
            }
            /// <summary>
            /// Enable the juice section in Studio always, not just for females
            /// </summary>
            [HarmonyPostfix, HarmonyPatch(typeof(MPCharCtrl.LiquidInfo), nameof(MPCharCtrl.LiquidInfo.UpdateInfo))]
            internal static void LiquidInfoUpdateInfo(OCIChar _char, MPCharCtrl.LiquidInfo __instance)
            {
                __instance.active = true;
                __instance.face.select = _char.GetSiruFlags(ChaFileDefine.SiruParts.SiruKao);
                __instance.breast.select = _char.GetSiruFlags(ChaFileDefine.SiruParts.SiruFrontUp);
                __instance.back.select = _char.GetSiruFlags(ChaFileDefine.SiruParts.SiruBackUp);
                __instance.belly.select = _char.GetSiruFlags(ChaFileDefine.SiruParts.SiruFrontDown);
                __instance.hip.select = _char.GetSiruFlags(ChaFileDefine.SiruParts.SiruBackDown);
            }
        }
    }
}