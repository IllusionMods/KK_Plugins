using Harmony;
using UnityEngine;
using BepInEx;

namespace KK_HeadFix
{
    [BepInPlugin(GUID, PluginName, Version)]
    public class KK_HeadFix : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.headfix";
        public const string PluginName = "Head Fix";
        public const string Version = "1.0";
        private void Main() => HarmonyInstance.Create(GUID).PatchAll(typeof(KK_HeadFix));

        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), "GetTexture")]
        public static void GetTexture(ChaListDefine.CategoryNo type, int id, ChaListDefine.KeyType assetBundleKey, ChaListDefine.KeyType assetKey, string addStr, ChaControl __instance, ref Texture2D __result)
        {
            if (__result == null && !addStr.IsNullOrEmpty())
                __result = Traverse.Create(__instance).Method("GetTexture", new object[] { type, id, assetBundleKey, assetKey, "" }).GetValue() as Texture2D;
        }
    }
}
