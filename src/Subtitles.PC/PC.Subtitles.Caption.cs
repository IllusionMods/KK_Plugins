using BepInEx;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KK_Plugins
{
    public partial class Subtitles
    {
        public partial class Caption
        {
            private static readonly YieldInstruction SubtitleRemovalDelay = new WaitForSeconds(0.75f);

            private static readonly Color DefaultTextColor = Color.white;
            private static readonly Color DefaultOutlineColor = Color.black;
            private static readonly Dictionary<string, Color> TextColors = new Dictionary<string, Color>
            {
                // Asae
                {"c01", new Color(0f, 0.4f, 1f)},
                // Yayoi
                {"c02", new Color(1f, 1f, 0.6f)},
                // Akane
                {"c03", new Color(1f, 0.6f, 0.4f)},
                // Momiji
                {"c04", new Color(0.5f, 0.89f, 1f)},
                // Rinko
                {"c05", new Color(0f, 0f, 1f)}

            };

            /// <summary>
            ///     Display text on screen. When the voice GameObject is destroyed, text will be removed from the screen.
            /// </summary>
            /// <param name="audioSource">AudioSource to watch, when it stops playing or clip changes text is removed from the screen.</param>
            /// <param name="text">Text to display</param>
            /// <param name="assetName">name of asset to be played (used to avoid duplicates and select color)
            public static void DisplaySubtitle(AudioSource audioSource, string text, string assetName)
            {
                if (!ShowSubtitles.Value || text.IsNullOrWhiteSpace()) return;
                DisplaySubtitle(text, GetTextColor(assetName), DefaultOutlineColor, subtitle => Instance.StartCoroutine(MonitorAudioSource(subtitle, audioSource)));
            }

            private static Color GetTextColor(string assetName) => TextColors.TryGetValue(assetName.Substring(0, 3), out var color) ? color : DefaultTextColor;

            private static IEnumerator MonitorAudioSource(GameObject subtitle, AudioSource audioSource)
            {
                var audioClip = audioSource.clip;
                while (audioSource.isPlaying && audioSource.clip == audioClip)
                    yield return null;

                yield return SubtitleRemovalDelay;
                Destroy(subtitle);
            }
        }
    }
}