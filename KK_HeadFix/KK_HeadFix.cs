using BepInEx;
using Harmony;
using System.Linq;
using UnityEngine;

namespace KK_HeadFix
{
    [BepInPlugin(GUID, PluginName, Version)]
    public class KK_HeadFix : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.headfix";
        public const string PluginName = "Head Fix";
        public const string Version = "1.1";

        private void Main()
        {
            var harmony = HarmonyInstance.Create(GUID);
            var getTextureMethod = typeof(ChaControl).GetMethod("GetTexture", AccessTools.all);
            if (getTextureMethod.GetParameters().Any(x => x.Name == "addStr"))
                harmony.Patch(typeof(ChaControl).GetMethod("GetTexture", AccessTools.all), null, new HarmonyMethod(typeof(KK_HeadFix).GetMethod(nameof(GetTexture), AccessTools.all)));
        }

        public static void GetTexture(ChaListDefine.CategoryNo type, int id, ChaListDefine.KeyType assetBundleKey, ChaListDefine.KeyType assetKey, string addStr, ChaControl __instance, ref Texture2D __result)
        {
            if (__result == null && !addStr.IsNullOrEmpty())
                __result = Traverse.Create(__instance).Method("GetTexture", new object[] { type, id, assetBundleKey, assetKey, "" }).GetValue() as Texture2D;
        }
    }
}
