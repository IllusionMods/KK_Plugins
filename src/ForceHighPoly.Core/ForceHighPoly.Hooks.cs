using HarmonyLib;
using System;

namespace KK_Plugins
{
    public partial class ForceHighPoly
    {
        internal static class Hooks
        {
            [HarmonyPrefix, HarmonyPatch(typeof(AssetBundleManager), nameof(AssetBundleManager.LoadAsset), typeof(string), typeof(string), typeof(Type), typeof(string))]
            private static void LoadAssetPrefix(ref string assetName) => RemoveLow(ref assetName);

            [HarmonyPrefix, HarmonyPatch(typeof(AssetBundleManager), nameof(AssetBundleManager.LoadAssetAsync), typeof(string), typeof(string), typeof(Type), typeof(string))]
            private static void LoadAssetAsyncPrefix(ref string assetName) => RemoveLow(ref assetName);

            /// <summary>
            /// Strip "_low" from the end of an asset name
            /// </summary>
            private static void RemoveLow(ref string assetName)
            {
                if (Enabled.Value)
                {
                    if (assetName.EndsWith("_low"))
                    {
#if KKS
                        if (assetName != "p_cf_body_00_hit_low") //Loading the high poly version of this breaks special H modes
#endif
                        {
                            assetName = assetName.Substring(0, assetName.Length - 4);
                        }
                    }
                }
            }
        }
    }
}