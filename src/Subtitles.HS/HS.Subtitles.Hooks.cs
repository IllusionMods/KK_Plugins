using HarmonyLib;

namespace KK_Plugins
{
    public partial class Subtitles
    {
        internal static class Hooks
        {
            [HarmonyPostfix, HarmonyPatch(typeof(LoadAudioBase), "Play")]
            private static void PlayVoice(LoadAudioBase __instance)
            {
                string[] assetNameSplit = __instance.assetName.Split('_');

                if (assetNameSplit[0] == "he") return; //Dialogue
                if (assetNameSplit[0] == "hs" && assetNameSplit.Length >= 3 && int.TryParse(assetNameSplit[2], out int result) && result <= 226) return; //Breath

                if (SubtitleDictionary.TryGetValue(__instance.assetName, out string text))
                    Caption.DisplaySubtitle(__instance.gameObject, text);
            }
        }
    }
}
