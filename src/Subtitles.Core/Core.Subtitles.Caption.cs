using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KK_Plugins
{
    public partial class Subtitles
    {
        public partial class Caption
        {
            internal static GameObject Pane;
            internal static GameObject PaneVR;
            internal static Text VRText1;
            internal static Text VRText2;
            internal static Scene ActiveScene;

            private static void InitGUI()
            {
                if (Pane != null)
                    return;


                Pane = new GameObject("KK_Subtitles_Caption");

                var cscl = Pane.GetOrAddComponent<CanvasScaler>();
                cscl.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                cscl.referenceResolution = new Vector2(Screen.width, Screen.height);

                var canvas = Pane.GetOrAddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 500;
                Pane.GetOrAddComponent<CanvasGroup>().blocksRaycasts = false;

                var vlg = Pane.GetOrAddComponent<VerticalLayoutGroup>();
                vlg.childForceExpandHeight = false;
                vlg.childForceExpandWidth = false;
                vlg.childAlignment = TextAlign.Value;
                vlg.padding = new RectOffset(0, 0, 0, TextOffset.Value);

                UpdateScene(ActiveScene);
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
                var c = PaneVR.GetOrAddComponent<Canvas>();
                c.renderMode = RenderMode.WorldSpace;
                c.worldCamera = Camera.main;
                c.sortingOrder = 500;

                //The resolution of a single HMD screen seems to be 600x800 so we force the scaler to that reference res
                var scaler = PaneVR.GetOrAddComponent<CanvasScaler>();
                scaler.referenceResolution = new Vector2(600f, 800f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

                {
                    var textContainer = new GameObject("VRTextContainer1");
                    textContainer.transform.parent = PaneVR.transform;
                    textContainer.transform.localPosition = VRTextOffset.Value;
                    textContainer.transform.localRotation = Quaternion.identity;

                    var subtitle = new GameObject("VRText1");
                    subtitle.layer = 5;
                    subtitle.transform.parent = textContainer.transform;
                    subtitle.transform.localPosition = Vector3.zero;
                    subtitle.transform.localRotation = Quaternion.identity;
                    subtitle.transform.localScale = new Vector3(0.001f * WorldScale, 0.001f * WorldScale, 0.001f * WorldScale);

                    Font fontFace = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");

                    VRText1 = subtitle.AddComponent<Text>();
                    VRText1.font = fontFace;
                    VRText1.material = fontFace.material;
                    VRText1.fontSize = 20;
                    VRText1.alignment = TextAnchor.MiddleCenter;
                    VRText1.color = TextColor.Value;
                    VRText1.text = "";

                    var rect = subtitle.GetComponent<RectTransform>();
                    rect.sizeDelta = new Vector2(400f, 400f);

                    subtitle.AddComponent<FollowUI>();

                    var outline = subtitle.AddComponent<Outline>();
                    outline.effectDistance = new Vector2(0.8f, 0.8f);
                }
                {
                    var textContainer = new GameObject("VRTextContainer2");
                    textContainer.transform.parent = PaneVR.transform;
                    textContainer.transform.localPosition = VRText2Offset.Value;
                    textContainer.transform.localRotation = Quaternion.identity;

                    var subtitle = new GameObject("VRText2") { layer = 5 };
                    subtitle.transform.parent = textContainer.transform;
                    subtitle.transform.localPosition = Vector3.zero;
                    subtitle.transform.localRotation = Quaternion.identity;
                    subtitle.transform.localScale = new Vector3(0.001f * WorldScale, 0.001f * WorldScale, 0.001f * WorldScale);

                    Font fontFace = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");

                    VRText2 = subtitle.AddComponent<Text>();
                    VRText2.font = fontFace;
                    VRText2.material = fontFace.material;
                    VRText2.fontSize = 20;
                    VRText2.alignment = TextAnchor.MiddleCenter;
                    VRText2.color = TextColor.Value;
                    VRText2.text = "";

                    var rect = subtitle.GetComponent<RectTransform>();
                    rect.sizeDelta = new Vector2(400f, 400f);

                    subtitle.AddComponent<FollowUI>();

                    var outline = subtitle.AddComponent<Outline>();
                    outline.effectDistance = new Vector2(0.8f, 0.8f);
                }
                UpdateScene(ActiveScene);
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

                Font fontFace = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");
                int fsize = FontSize.Value;
                fsize = (int)(fsize < 0 ? (fsize * Screen.height / -100.0) : fsize);

                GameObject subtitle = new GameObject("SubtitleText");
                subtitle.transform.SetParent(Pane.transform);

                var rect = subtitle.GetOrAddComponent<RectTransform>();
                rect.pivot = new Vector2(0.5f, 0);
                rect.sizeDelta = new Vector2(Screen.width * 0.990f, fsize + (fsize * 0.05f));

                var subtitleText = subtitle.GetOrAddComponent<Text>();
                subtitleText.font = fontFace;
                subtitleText.fontSize = fsize;
                subtitleText.fontStyle = fontFace.dynamic ? FontStyle.Value : UnityEngine.FontStyle.Normal;
                subtitleText.alignment = TextAlign.Value;
                subtitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
                subtitleText.verticalOverflow = VerticalWrapMode.Overflow;
                subtitleText.color = TextColor.Value;

                var subOutline = subtitle.GetOrAddComponent<Outline>();
                subOutline.effectColor = OutlineColor.Value;
                subOutline.effectDistance = new Vector2(OutlineThickness.Value, OutlineThickness.Value);

                subtitleText.text = text;
                Logger.LogDebug(text);

                voice.OnDestroyAsObservable().Subscribe(obj => Destroy(subtitle));
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

                Logger.LogDebug(text);
                if (VRText1.text == "")
                {
                    VRText1.text = text;
                    voice.OnDestroyAsObservable().Subscribe(obj => VRText1.text = "");
                }
                else
                {
                    VRText2.text = text;
                    voice.OnDestroyAsObservable().Subscribe(obj => VRText2.text = "");
                }
            }

            internal static void UpdateScene() => UpdateScene(SceneManager.GetActiveScene());

            /// <summary>
            /// Move the pane to the scene so that it is properly scoped for XUnity.AutoTranslator compatiblity
            /// </summary>
            internal static void UpdateScene(Scene newScene)
            {
                ActiveScene = newScene;
                if (Pane != null && Pane.scene != newScene)
                    SceneManager.MoveGameObjectToScene(Pane, newScene);
                if (PaneVR != null && PaneVR.scene != newScene)
                    SceneManager.MoveGameObjectToScene(PaneVR, newScene);
            }

            public static void SceneUnloaded(Scene scene)
            {
                if (scene == ActiveScene)
                    UpdateScene();
            }

            internal static void SceneLoaded(Scene scene, LoadSceneMode mode)
            {
                if (mode == LoadSceneMode.Single)
                    UpdateScene(scene);
            }
        }
    }
}