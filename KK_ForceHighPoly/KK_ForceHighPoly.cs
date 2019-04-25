using BepInEx;
using Harmony;
using System;
using System.ComponentModel;
using System.Collections;

namespace KK_ForceHighPoly
{
    /// <summary>
    /// Replaces all _low assets with normal assets, forcing everything to load as high poly
    /// </summary>
    [BepInPlugin(GUID, PluginName, Version)]
    public class KK_ForceHighPoly : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.forcehighpoly";
        public const string PluginName = "Force High Poly";
        public const string PluginNameInternal = "KK_ForceHighPoly";
        public const string Version = "1.1";
        [Category("Settings")]
        [DisplayName("High poly mode")]
        [Description("Whether or not to load high poly assets. May require exiting to main menu to take effect.")]
        public static ConfigWrapper<bool> Enabled { get; private set; }

        void Main()
        {
            var harmony = HarmonyInstance.Create(GUID);
            harmony.PatchAll(typeof(KK_ForceHighPoly));
            Enabled = new ConfigWrapper<bool>("Enabled", PluginNameInternal, true);
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

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ChaControl), "ChangeHairAsync", new Type[] { typeof(int), typeof(int), typeof(bool), typeof(bool) })]
        public static void ChangeHairAsyncPostHook(ChaControl __instance, int kind, ref IEnumerator __result)
        {
            var orig = __result;
            __result = new IEnumerator[] { orig, ChangeHairAsyncPostfix(__instance, kind) }.GetEnumerator();
        }

        private static IEnumerator ChangeHairAsyncPostfix(ChaControl instance, int kind)
        {
            var hairObject = instance.objHair[kind];
            if (hairObject != null)
                foreach (var dynamicBone in hairObject.GetComponentsInChildren<DynamicBone>(true))
                    dynamicBone.enabled = true;

            yield break;
        }
    }
}
