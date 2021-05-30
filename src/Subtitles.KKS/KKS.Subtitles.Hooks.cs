using HarmonyLib;
using UnityEngine;

namespace KK_Plugins
{
    public partial class Subtitles
    {
        internal static class Hooks
        {
            [HarmonyPostfix, HarmonyPatch(typeof(Manager.Sound), nameof(Manager.Sound.Play_Standby), typeof(AudioSource), typeof(Manager.Sound.Loader))]
            private static void PlayVoice(AudioSource audioSource, Manager.Sound.Loader loader)
            {
                if (loader.asset.IsNullOrEmpty() || loader.asset.Contains("_bgm_"))
                    return;

                if (SubtitleDictionary.TryGetValue(loader.asset, out string text))
                    Caption.DisplaySubtitle(audioSource.gameObject, text);
            }
        }
    }
}