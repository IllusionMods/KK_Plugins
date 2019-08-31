using HarmonyLib;

namespace KK_Plugins
{
    public partial class Subtitles
    {
        internal static class Hooks
        {
            [HarmonyPostfix, HarmonyPatch(typeof(LoadAudioBase), "Play")]
            public static void PlayVoice(LoadAudioBase __instance)
            {
                if (SubtitleDictionary.TryGetValue(__instance.assetName, out string text))
                    Caption.DisplaySubtitle(__instance, text);
            }
        }
    }
}
