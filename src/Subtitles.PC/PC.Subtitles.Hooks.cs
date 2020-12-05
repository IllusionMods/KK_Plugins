using BepInEx;
using HarmonyLib;
using System.Collections.Generic;

namespace KK_Plugins
{
    public partial class Subtitles
    {
        internal static class Hooks
        {
            private static readonly HashSet<string> AssetTypesToSkip = new HashSet<string>(new[]
            {
                "op", "r", "ed"
            });

            private static string GetAssetType(string assetName)
            {
                if (assetName.IsNullOrWhiteSpace())
                    return string.Empty;
                string[] assetNameSplit = assetName.Split('_');
                return assetNameSplit.Length <= 1 ? string.Empty : assetNameSplit[1].Split("0123456789".ToCharArray(), 2)[0];
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(Human), nameof(Human.Talk))]
            private static void PlayPostfix(Human __instance, string asset, bool isOneShot, bool __result)
            {
                if (!__result || !isOneShot || AssetTypesToSkip.Contains(GetAssetType(asset)))
                    return;

                if (SubtitleDictionary.TryGetValue(asset, out var text))
                    Caption.DisplaySubtitle(__instance.voice.voiceSource, text, asset);
            }
        }
    }
}