using System.Collections;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

namespace KK_Plugins
{
    public partial class Subtitles
    {
        public partial class Caption
        {
            internal static GameObject Pane { get; set; }

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
#if !HS
                vlg.childControlHeight = false;
                vlg.childControlWidth = false;
#endif
                vlg.childForceExpandHeight = false;
                vlg.childForceExpandWidth = false;
                vlg.childAlignment = TextAnchor.LowerCenter;
                vlg.padding = new RectOffset(0, 0, 0, TextOffset.Value);
            }

#if HS2
            public static void DisplaySubtitle(AudioSource voice, string assetName, string text, bool init = true)
            {
                if (!ShowSubtitles.Value) return;
                if (text.IsNullOrWhiteSpace()) return;
                if (Pane == null && !init) return;

                Instance.StartCoroutine(_DisplaySubtitle(voice.gameObject, assetName, text, init));
            }
#else
            public static void DisplaySubtitle(LoadAudioBase voice, string text, bool init = true)
            {
                if (!ShowSubtitles.Value) return;
                if (text.IsNullOrWhiteSpace()) return;
                if (Pane == null && !init) return;

                Instance.StartCoroutine(_DisplaySubtitle(voice.gameObject, voice.assetName, text, init));
            }
#endif
            private static IEnumerator _DisplaySubtitle(GameObject voice, string assetName, string text, bool init)
            {
                if (init)
                {
                    InitGUI();
                    yield return null;
                }

                Font fontFace = (Font)Resources.GetBuiltinResource(typeof(Font), $"{FontName.Value}.ttf");
                int fsize = FontSize.Value;
                fsize = (int)(fsize < 0 ? (fsize * Screen.height / -100.0) : fsize);

                GameObject subtitle = new GameObject(assetName);
                subtitle.transform.SetParent(Pane.transform);

                var rect = subtitle.GetComponent<RectTransform>() ?? subtitle.AddComponent<RectTransform>();
                rect.pivot = new Vector2(0.5f, 0);
                rect.sizeDelta = new Vector2(Screen.width * 0.990f, fsize + (fsize * 0.05f));

                var subtitleText = subtitle.GetComponent<Text>() ?? subtitle.AddComponent<Text>();
                subtitleText.font = fontFace;
                subtitleText.fontSize = fsize;
                subtitleText.fontStyle = fontFace.dynamic ? FontStyle.Value : UnityEngine.FontStyle.Normal;
                subtitleText.alignment = TextAlign.Value;
                subtitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
                subtitleText.verticalOverflow = VerticalWrapMode.Overflow;
                subtitleText.color = TextColor.Value;

                var subOutline = subtitle.GetComponent<Outline>() ?? subtitle.AddComponent<Outline>();
                subOutline.effectColor = OutlineColor.Value;
                subOutline.effectDistance = new Vector2(OutlineThickness.Value, OutlineThickness.Value);

                subtitleText.text = text;

                Logger.LogDebug($"{text}");

                voice.OnDestroyAsObservable().Subscribe(delegate (Unit _)
                {
                    subtitle.transform.SetParent(null);
                    Destroy(subtitle);
                });
            }
        }
    }
}