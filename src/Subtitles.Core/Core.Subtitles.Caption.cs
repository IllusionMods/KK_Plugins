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
            internal static GameObject Pane;
            internal static GameObject PaneVR;
            internal static GameObject VRTextContainer;

            private static void InitGUI()
            {
                if (Pane != null)
                    return;

                Pane = new GameObject("KK_Subtitles_Caption");

                var cscl = Pane.AddComponent<CanvasScaler>();
                cscl.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                cscl.referenceResolution = new Vector2(Screen.width, Screen.height);

                var canvas = Pane.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 500;
                Pane.AddComponent<CanvasGroup>().blocksRaycasts = false;

                var vlg = Pane.AddComponent<VerticalLayoutGroup>();
                vlg.childForceExpandHeight = false;
                vlg.childForceExpandWidth = false;
                vlg.childAlignment = TextAlign.Value;
                vlg.padding = new RectOffset(0, 0, 0, TextOffset.Value);
            }

            private static void InitVRGUI()
            {
                if (PaneVR != null)
                    return;

                PaneVR = new GameObject("KK_Subtitles_Caption");
                PaneVR.layer = 5;
                PaneVR.transform.parent = Camera.main.transform;
                PaneVR.transform.localPosition = Vector3.zero;
                PaneVR.transform.localRotation = Quaternion.identity;
                var c = PaneVR.AddComponent<Canvas>();
                c.renderMode = RenderMode.WorldSpace;
                c.worldCamera = Camera.main;
                c.sortingOrder = 500;

                //The resolution of a single HMD screen seems to be 600x800 so we force the scaler to that reference res
                var scaler = PaneVR.AddComponent<CanvasScaler>();
                scaler.referenceResolution = new Vector2(600f, 800f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

                VRTextContainer = new GameObject("VRTextContainer");
                VRTextContainer.layer = 5;
                VRTextContainer.transform.parent = PaneVR.transform;
                VRTextContainer.transform.localPosition = VRTextOffset.Value;
                VRTextContainer.transform.localRotation = Quaternion.identity;
            }

            /// <summary>
            /// Display text on screen. When the voice GameObject is destroyed, text will be removed from the screen.
            /// </summary>
            /// <param name="voice">GameObject to watch. When destroyed, text is removed from the screen.</param>
            /// <param name="text">Text to display</param>
            public static void DisplaySubtitle(GameObject voice, string text)
            {
                if (!ShowSubtitles.Value) return;
                if (text.IsNullOrWhiteSpace()) return;

                InitGUI();

                Font fontFace = (Font)Resources.GetBuiltinResource(typeof(Font), $"Arial.ttf");
                int fsize = FontSize.Value;
                fsize = (int)(fsize < 0 ? (fsize * Screen.height / -100.0) : fsize);

                GameObject subtitle = new GameObject("SubtitleText");
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

                voice.OnDestroyAsObservable().Subscribe(delegate (Unit _) { Destroy(subtitle); });
            }
            /// <summary>
            /// Display text on screen, for VR. When the voice GameObject is destroyed, text will be removed from the screen.
            /// </summary>
            /// <param name="voice">GameObject to watch. When destroyed, text is removed from the screen.</param>
            /// <param name="text">Text to display</param>
            public static void DisplayVRSubtitle(GameObject voice, string text)
            {
                if (!ShowSubtitles.Value) return;
                if (text.IsNullOrWhiteSpace()) return;

                InitVRGUI();

                var subtitle = new GameObject("VRText");
                subtitle.layer = 5;
                subtitle.transform.parent = VRTextContainer.transform;
                subtitle.transform.localPosition = new Vector3(0f, 0f, 1f);
                subtitle.transform.localRotation = Quaternion.identity;
                subtitle.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);

                Font fontFace = (Font)Resources.GetBuiltinResource(typeof(Font), $"Arial.ttf");

                var subtitleText = subtitle.AddComponent<Text>();
                subtitleText.font = fontFace;
                subtitleText.material = fontFace.material;
                subtitleText.fontSize = 20;
                subtitleText.alignment = TextAnchor.MiddleCenter;
                subtitleText.color = TextColor.Value;
                subtitleText.text = "";

                var rect = subtitle.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(600f, 800f);

                subtitle.AddComponent<FollowUI>();

                var outline = subtitle.AddComponent<Outline>();
                outline.effectDistance = new Vector2(0.8f, 0.8f);

                subtitleText.text = text;
                Logger.LogDebug(text);

                voice.OnDestroyAsObservable().Subscribe(delegate (Unit _) { Destroy(subtitle); });
            }
        }
    }
}