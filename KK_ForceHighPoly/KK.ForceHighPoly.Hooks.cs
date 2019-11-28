using HarmonyLib;
using System;
using System.Collections;

namespace KK_Plugins
{
    public partial class ForceHighPoly
    {
        internal static class Hooks
        {
            [HarmonyPrefix, HarmonyPatch(typeof(AssetBundleManager), nameof(AssetBundleManager.LoadAsset), typeof(string), typeof(string), typeof(Type), typeof(string))]
            internal static void LoadAssetPrefix(ref string assetName)
            {
                if (Enabled.Value && assetName.EndsWith("_low"))
                    assetName = assetName.Substring(0, assetName.Length - 4);
            }

            [HarmonyPrefix, HarmonyBefore(new string[] { "com.bepis.bepinex.resourceredirector" })]
            [HarmonyPatch(typeof(AssetBundleManager), nameof(AssetBundleManager.LoadAssetAsync), typeof(string), typeof(string), typeof(Type), typeof(string))]
            internal static void LoadAssetAsyncPrefix(ref string assetName)
            {
                if (Enabled.Value && assetName.EndsWith("_low"))
                    assetName = assetName.Substring(0, assetName.Length - 4);
            }

            [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), "ChangeHairAsync", typeof(int), typeof(int), typeof(bool), typeof(bool))]
            internal static void ChangeHairAsyncPostHook(ChaControl __instance, int kind, ref IEnumerator __result)
            {
                if (!Enabled.Value) return;

                var orig = __result;
                __result = new IEnumerator[] { orig, ChangeHairAsyncPostfix(__instance, kind) }.GetEnumerator();
            }
        }
    }
}