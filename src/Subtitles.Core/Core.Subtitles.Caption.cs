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
            internal static GameObject PaneVR { get; set; }
            internal static Text SubtitleText { get; set; }
            internal static Text SubtitleTextVR { get; set; }

            private static void InitGUI()
            {
                if (Pane != null)
                    return;

                Pane = new GameObject("KK_Subtitles_Caption");

                var cscl = Pane.GetComponent<CanvasScaler>() ?? Pane.AddComponent<CanvasScaler>();
                cscl.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                cscl.referenceResolution = new Vector2(Screen.width, Screen.height);

                var canvas = Pane.GetComponent<Canvas>() ?? Pane.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 500;
                (Pane.GetComponent<CanvasGroup>() ?? Pane.AddComponent<CanvasGroup>()).blocksRaycasts = false;

                var vlg = Pane.GetComponent<VerticalLayoutGroup>() ?? Pane.AddComponent<VerticalLayoutGroup>();
                vlg.childForceExpandHeight = false;
                vlg.childForceExpandWidth = false;
                vlg.childAlignment = TextAlign.Value;
                vlg.padding = new RectOffset(0, 0, 0, TextOffset.Value);

                Font fontFace = (Font)Resources.GetBuiltinResource(typeof(Font), $"Arial.ttf");
                int fsize = FontSize.Value;
                fsize = (int)(fsize < 0 ? (fsize * Screen.height / -100.0) : fsize);

                GameObject subtitle = new GameObject("SubtitleText");
                subtitle.transform.SetParent(Pane.transform);

                var rect = subtitle.GetComponent<RectTransform>() ?? subtitle.AddComponent<RectTransform>();
                rect.pivot = new Vector2(0.5f, 0);
                rect.sizeDelta = new Vector2(Screen.width * 0.990f, fsize + (fsize * 0.05f));

                SubtitleText = subtitle.GetComponent<Text>() ?? subtitle.AddComponent<Text>();
                SubtitleText.font = fontFace;
                SubtitleText.fontSize = fsize;
                SubtitleText.fontStyle = fontFace.dynamic ? FontStyle.Value : UnityEngine.FontStyle.Normal;
                SubtitleText.alignment = TextAlign.Value;
                SubtitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
                SubtitleText.verticalOverflow = VerticalWrapMode.Overflow;
                SubtitleText.color = TextColor.Value;

                var subOutline = subtitle.GetComponent<Outline>() ?? subtitle.AddComponent<Outline>();
                subOutline.effectColor = OutlineColor.Value;
                subOutline.effectDistance = new Vector2(OutlineThickness.Value, OutlineThickness.Value);
            }

            private static void InitVRGUI()
            {
                if (PaneVR != null)
                    return;

                PaneVR = new GameObject("KK_Subtitles_Caption");
                var c = PaneVR.AddComponent<Canvas>();
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = Camera.main;
                c.sortingOrder = 1000;

                // The resolution of a single HMD screen seems to be 600x800 so we force the scaler to that reference res
                // Using the scaler we'll get crispier text
                var scaler = PaneVR.AddComponent<CanvasScaler>();
                scaler.referenceResolution = new Vector2(600f, 800f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

                var VRTextContainer = new GameObject("VRTextContainer");
                VRTextContainer.transform.parent = PaneVR.transform;
                // Move the actual text back so that it doesn't feel "too close"
                VRTextContainer.transform.localPosition = new Vector3(0, -500f, 2000f);
                VRTextContainer.transform.localRotation = Quaternion.identity;

                var subtitle = new GameObject("VRText");
                subtitle.transform.parent = VRTextContainer.transform;

                SubtitleTextVR = subtitle.AddComponent<Text>();
                var myFont = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");
                SubtitleTextVR.font = myFont;
                SubtitleTextVR.material = myFont.material;
                SubtitleTextVR.fontSize = 10;
                SubtitleTextVR.alignment = TextAnchor.MiddleCenter;
                SubtitleTextVR.color = TextColor.Value;
                SubtitleTextVR.text = "";

                var rect = subtitle.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(200, 100);

                subtitle.AddComponent<FollowUI>();

                var outline = subtitle.AddComponent<Outline>();
                outline.effectDistance = new Vector2(0.6f, 0.6f);
            }

            /// <summary>
            /// Display text on screen. When the voice GameObject is destroyed, text will be removed from the screen.
            /// </summary>
            /// <param name="voice">GameObject to watch. When destroyed, text is removed from the screen.</param>
            /// <param name="text">Text to display</param>
            public static void DisplaySubtitle(GameObject voice, string text)
            {
                if (!ShowSubtitles.Value)
                    text = "";
                InitGUI();

                DisplaySubtitle(text);
                if (text != "")
                {
                    Logger.LogDebug(text);

                    voice.OnDestroyAsObservable().Subscribe(delegate (Unit _) { DisplaySubtitle(""); });
                }
            }
            /// <summary>
            /// Display text on screen. Will stay until the text is set to blank.
            /// </summary>
            /// <param name="text">Text to display</param>
            public static void DisplaySubtitle(string text)
            {
                if (!ShowSubtitles.Value)
                    text = "";
                InitGUI();

                SubtitleText.text = text;
            }
            /// <summary>
            /// Display text on screen, for VR. When the voice GameObject is destroyed, text will be removed from the screen.
            /// </summary>
            /// <param name="voice">GameObject to watch. When destroyed, text is removed from the screen.</param>
            /// <param name="text">Text to display</param>
            public static void DisplayVRSubtitle(GameObject voice, string text)
            {
                if (!ShowSubtitles.Value)
                    text = "";
                InitVRGUI();

                DisplayVRSubtitle(text);
                if (text != "")
                {
                    Logger.LogDebug(text);

                    voice.OnDestroyAsObservable().Subscribe(delegate (Unit _) { DisplayVRSubtitle(""); });
                }
            }
            /// <summary>
            /// Display text on screen, for VR. Will stay until the text is set to blank.
            /// </summary>
            /// <param name="text">Text to display</param>
            public static void DisplayVRSubtitle(string text)
            {
                if (!ShowSubtitles.Value)
                    text = "";
                InitVRGUI();

                SubtitleTextVR.text = text;
            }
        }
    }
}