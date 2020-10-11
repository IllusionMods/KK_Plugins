using HarmonyLib;
using System;

namespace KK_Plugins
{
    public partial class ForceHighPoly
    {
        internal static class Hooks
        {
            [HarmonyPrefix, HarmonyPatch(typeof(AssetBundleManager), nameof(AssetBundleManager.LoadAsset), typeof(string), typeof(string), typeof(Type), typeof(string))]
            private static void LoadAssetPrefix(ref string assetName)
            {
                if (Enabled.Value && assetName.EndsWith("_low"))
                    assetName = assetName.Substring(0, assetName.Length - 4);
            }

            [HarmonyPrefix, HarmonyPatch(typeof(AssetBundleManager), nameof(AssetBundleManager.LoadAssetAsync), typeof(string), typeof(string), typeof(Type), typeof(string))]
            private static void LoadAssetAsyncPrefix(ref string assetName)
            {
                if (Enabled.Value && assetName.EndsWith("_low"))
                    assetName = assetName.Substring(0, assetName.Length - 4);
            }
        }
    }
}