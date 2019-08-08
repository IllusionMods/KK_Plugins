using System.Collections;
using System.Collections.Generic;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

namespace Subtitles
{
    public partial class Caption
    {
        public static GameObject Pane { get; internal set; }

        internal static void InitGUI()
        {
            if (Pane != null)
                return;

            Pane = new GameObject("KK_Subtitles_Caption");

            var cscl = Pane.GetComponent<CanvasScaler>() ?? Pane.AddComponent<CanvasScaler>();
            cscl.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cscl.referenceResolution = new Vector2(1920, 1080);
            cscl.matchWidthOrHeight = 0.5f;

            var canvas = Pane.GetComponent<Canvas>() ?? Pane.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            (Pane.GetComponent<CanvasGroup>() ?? Pane.AddComponent<CanvasGroup>()).blocksRaycasts = false;

            var vlg = Pane.GetComponent<VerticalLayoutGroup>() ?? Pane.AddComponent<VerticalLayoutGroup>();
#if KK
            vlg.childControlHeight = false;
            vlg.childControlWidth = false;
#endif
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.childAlignment = TextAnchor.LowerCenter;
            vlg.padding = new RectOffset(0, 0, 0, Subtitles.TextOffset);
        }

        internal static void DisplaySubtitle(LoadAudioBase voice, string text, string speaker = "", bool init = true)
        {
            if (!Subtitles.ShowSubtitles) return;
            if (text.IsNullOrWhiteSpace()) return;
            if (Pane == null && !init) return;

            voice.StartCoroutine(_DisplaySubtitle(voice, text, speaker, init));
        }
        private static IEnumerator _DisplaySubtitle(LoadAudioBase voice, string text, string speaker, bool init)
        {
            if (init)
            {
                InitGUI();
                yield return null;
            }

            Font fontFace = (Font)Resources.GetBuiltinResource(typeof(Font), $"{Subtitles.FontName}.ttf");
            int fsize = Subtitles.FontSize;
            fsize = (int)(fsize < 0 ? (fsize * Screen.height / -100.0) : fsize);

            GameObject subtitle = new GameObject(voice.assetName);
            subtitle.transform.SetParent(Pane.transform);

            var rect = subtitle.GetComponent<RectTransform>() ?? subtitle.AddComponent<RectTransform>();
            rect.pivot = new Vector2(0.5f, 0);
            rect.sizeDelta = new Vector2(Screen.width * 0.990f, fsize + (fsize * 0.05f));

            var subtitleText = subtitle.GetComponent<Text>() ?? subtitle.AddComponent<Text>();
            subtitleText.font = fontFace;
            subtitleText.fontSize = fsize;
            subtitleText.fontStyle = fontFace.dynamic ? Subtitles.FontStyle : FontStyle.Normal;
            subtitleText.alignment = Subtitles.TextAlign;
            subtitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            subtitleText.verticalOverflow = VerticalWrapMode.Overflow;
            subtitleText.color = Subtitles.TextColor;

            var subOutline = subtitle.GetComponent<Outline>() ?? subtitle.AddComponent<Outline>();
            subOutline.effectColor = Subtitles.OutlineColor;
            subOutline.effectDistance = new Vector2(Subtitles.OutlineThickness, Subtitles.OutlineThickness);

            subtitleText.text = speaker.IsNullOrEmpty() ? text : $"{speaker}:{text}";

            Subtitles.LogDebug($"{Subtitles.PluginNameInternal}:{text}");

            voice.OnDestroyAsObservable().Subscribe(delegate (Unit _)
            {
                subtitle.transform.SetParent(null);
                Object.Destroy(subtitle);
            });
        }


        private static readonly HashSet<char> JPChars = new HashSet<char>("あいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほまみむめもやゆよらりるれろわんアイウエカキクケコサシスセソタチツテトナニヌネノハヒフヘホマミムメモヤユヨラリルレロワヲン");
    }
}