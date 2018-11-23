using BepInEx;
using Harmony;
using System;
using System.ComponentModel;
/// <summary>
/// Replaces all _low assets with normal assets, forcing everything to load as high poly
/// </summary>
namespace KK_ForceHighPoly
{
    [BepInPlugin("com.deathweasel.bepinex.forcehighpoly", "Force High Poly", Version)]
    public class KK_ForceHighPoly : BaseUnityPlugin
    {
        public const string Version = "1.1";
        [Category("Settings")]
        [DisplayName("High poly mode")]
        [Description("Whether or not to load high poly assets. May require exiting to main menu to take effect.")]
        public static ConfigWrapper<bool> Enabled { get; private set; }

        void Main()
        {
            var harmony = HarmonyInstance.Create("com.deathweasel.bepinex.forcehighpoly");
            harmony.PatchAll(typeof(KK_ForceHighPoly));
            Enabled = new ConfigWrapper<bool>("Enabled", "KK_ForceHighPoly", true);
        }

        [HarmonyPrefix]
        [HarmonyBefore(new string[] { "com.bepis.bepinex.resourceredirector" })]
        [HarmonyPatch(typeof(AssetBundleManager), nameof(AssetBundleManager.LoadAsset), new[] { typeof(string), typeof(string), typeof(Type), typeof(string) })]
        public static void LoadAssetPrefix(ref string assetName)
        {
            if (Enabled.Value && assetName.EndsWith("_low"))
                assetName = assetName.Substring(0, assetName.Length - 4);
        }

        [HarmonyPrefix]
        [HarmonyBefore(new string[] { "com.bepis.bepinex.resourceredirector" })]
        [HarmonyPatch(typeof(AssetBundleManager), nameof(AssetBundleManager.LoadAssetAsync), new[] { typeof(string), typeof(string), typeof(Type), typeof(string) })]
        public static void LoadAssetAsyncPrefix(ref string assetName)
        {
            if (Enabled.Value && assetName.EndsWith("_low"))
                assetName = assetName.Substring(0, assetName.Length - 4);
        }
    }
}
