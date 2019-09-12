using HarmonyLib;
using KKAPI.Studio;
using Studio;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace KK_Plugins
{
    public partial class FKIK
    {
        internal class UI
        {
            private const int PositionBase = -85;
            private const int PositionOffset = -25;

            private static readonly string[] RemoveText = { "Text Draw", "Text Hair", "Text Neck", "Text Breast", "Text Body", "Text Right Hand", "Text Left Hand", "Text Skirt" };
            private static readonly string[] RemoveToggle = { "Toggle Visible", "Toggle Hair", "Toggle Neck", "Toggle Breast", "Toggle Body", "Toggle Right Hand", "Toggle Left Hand", "Toggle Skirt" };
            private static readonly string[] RemoveButton = { "Button To IK", "Button Hair Init", "Button For Anime (1)", "Button For Anime (2)", "Button For Anime (3)", "Button For Anime (4)", "Button Skirt Init" };

            internal static GameObject FKIKPanel = null;
            private static Toggle ActiveButton;

            /// <summary>
            /// Add the UI button
            /// </summary>
            internal static void InitUI()
            {
                if (FKIKPanel != null) return;

                CreateMenuEntry();
                CreatePanel();
                SetUpButtons();
            }

            internal static void UpdateUI(OCIChar _char) => ActiveButton.isOn = _char.oiCharInfo.enableFK && _char.oiCharInfo.enableIK;

            private static void CreateMenuEntry()
            {
                GameObject listmenu = GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/00_Chara/02_Kinematic/Viewport/Content");
                GameObject fkButton = GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/00_Chara/02_Kinematic/Viewport/Content/FK");
                var newSelect = Instantiate(fkButton, listmenu.transform, true);
                newSelect.name = "FK & IK";
                TextMeshProUGUI tmp = newSelect.transform.GetChild(0).GetComponentInChildren(typeof(TextMeshProUGUI)) as TextMeshProUGUI;
                tmp.text = "FK & IK";

                Button[] buttons = listmenu.GetComponentsInChildren<Button>();

                GameObject originalPanel = GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/00_Chara/02_Kinematic/00_FK");
                GameObject kineMenu = GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/00_Chara/02_Kinematic");

                FKIKPanel = Instantiate(originalPanel, kineMenu.transform, true);

                RectTransform rect = FKIKPanel.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, 395);

                Button fkikSelectButton = newSelect.GetComponent<Button>();

                foreach (Button button in buttons)
                {
                    button.onClick.AddListener(delegate ()
                    {
                        FKIKPanel.SetActive(false);
                        fkikSelectButton.image.color = Color.white;
                    });
                }

                fkikSelectButton.onClick.RemoveAllListeners();
                fkikSelectButton.onClick.AddListener(delegate ()
                {
                    foreach (Transform child in kineMenu.transform)
                    {
                        if (child.name != "Viewport" && child.name != "Scrollbar Vertical")
                        {
                            child.gameObject.SetActive(false);
                        }
                    }
                    foreach (Button button in buttons)
                    {
                        button.image.color = Color.white;
                    }
                    FKIKPanel.SetActive(true);
                    fkikSelectButton.image.color = Color.green;
                    Traverse.Create(FindObjectOfType<MPCharCtrl>()).Field("kinematic").SetValue(-1);
                    foreach (OCIChar ociChar in StudioAPI.GetSelectedCharacters())
                    {
                        ActiveButton.isOn = ociChar.oiCharInfo.enableIK && ociChar.oiCharInfo.enableFK;
                        break;
                    }
                });
            }

            private static void CreatePanel()
            {
                Texture2D tex = new Texture2D(152, 272);
                tex.LoadImage(LoadPanel());
                Image backingPanel = FKIKPanel.GetComponent<Image>();
                Sprite replacement = Sprite.Create(tex, backingPanel.sprite.rect, backingPanel.sprite.pivot);
                backingPanel.sprite = replacement;
                backingPanel.name = "FK & IK";
            }

            private static void SetUpButtons()
            {
                ActiveButton = GetPanelObject<Toggle>("Toggle Function");
                ActiveButton.onValueChanged.RemoveAllListeners();
                ActiveButton.onValueChanged.AddListener(delegate (bool val)
                {
                    ToggleFKIK(val);
                });

                GetPanelObject<Button>("Button For Anime").onClick.AddListener(delegate ()
                {
                    Traverse.Create(FindObjectOfType<MPCharCtrl>()).Field("fkInfo").Field("buttonAnime").GetValue<Button>().onClick.Invoke();
                    Traverse.Create(FindObjectOfType<MPCharCtrl>()).Field("ikInfo").Field("buttonAnime").GetValue<Button>().onClick.Invoke();
                });

                SetupText("FKIK Text Hair", 0, "Hair (FK)");
                SetupToggle("FKIK Toggle Hair", 0, KinematicsType.FK, "toggleHair");
                SetupButton("FKIK Button Hair", 0, KinematicsType.FK, ButtonType.Init, 0);

                SetupText("FKIK Text Neck", 1, "Neck (FK)");
                SetupToggle("FKIK Toggle Neck", 1, KinematicsType.FK, "toggleNeck");
                SetupButton("FKIK Button Neck", 1, KinematicsType.FK, ButtonType.Anime, 1);

                SetupText("FKIK Text Breasts", 2, "Chest (FK)");
                SetupToggle("FKIK Toggle Breasts", 2, KinematicsType.FK, "toggleBreast");

                SetupText("FKIK Text Right Hand", 3, "Right Hand (FK)");
                SetupToggle("FKIK Toggle Right Hand", 3, KinematicsType.FK, "toggleRightHand");
                SetupButton("FKIK Button Right Hand", 3, KinematicsType.FK, ButtonType.Anime, 3);

                SetupText("FKIK Text Left Hand", 4, "Left Hand (FK)");
                SetupToggle("FKIK Toggle Left Hand", 4, KinematicsType.FK, "toggleLeftHand");
                SetupButton("FKIK Button Left Hand", 4, KinematicsType.FK, ButtonType.Anime, 2);

                SetupText("FKIK Text Skirt", 5, "Skirt (FK)");
                SetupToggle("FKIK Toggle Skirt", 5, KinematicsType.FK, "toggleSkirt");
                SetupButton("FKIK Button Skirt", 5, KinematicsType.FK, ButtonType.Init, 1);

                SetupText("FKIK Text Body", 6, "Body (IK)");
                SetupToggle("FKIK Toggle Body", 6, KinematicsType.IK, "toggleBody");
                SetupButton("FKIK Button Body", 6, KinematicsType.IK, ButtonType.Anime, 0);

                SetupText("FKIK Text Right Arm", 7, "Right Arm (IK)");
                SetupToggle("FKIK Toggle Right Arm", 7, KinematicsType.IK, "toggleRightHand");
                SetupButton("FKIK Button Right Arm", 7, KinematicsType.IK, ButtonType.Anime, 2);

                SetupText("FKIK Text Left Arm", 8, "Left Arm (IK)");
                SetupToggle("FKIK Toggle Left Arm", 8, KinematicsType.IK, "toggleLeftHand");
                SetupButton("FKIK Button Left Arm", 8, KinematicsType.IK, ButtonType.Anime, 1);

                SetupText("FKIK Text Right Leg", 9, "Right Leg (IK)");
                SetupToggle("FKIK Toggle Right Leg", 9, KinematicsType.IK, "toggleRightLeg");
                SetupButton("FKIK Button Right Leg", 9, KinematicsType.IK, ButtonType.Anime, 4);

                SetupText("FKIK Text Left Leg", 10, "Left Leg (IK)");
                SetupToggle("FKIK Toggle Left Leg", 10, KinematicsType.IK, "toggleLeftLeg");
                SetupButton("FKIK Button Left Leg", 10, KinematicsType.IK, ButtonType.Anime, 3);

                var txtSize = GetPanelObject<Text>("Text Size");
                txtSize.transform.localPosition = new Vector3(txtSize.transform.localPosition.x, PositionBase + (PositionOffset * 11), txtSize.transform.localPosition.z);
                var sldSize = GetPanelObject<Slider>("Slider Size");
                sldSize.transform.localPosition = new Vector3(sldSize.transform.localPosition.x, PositionBase + (PositionOffset * 11), sldSize.transform.localPosition.z);
                sldSize.onValueChanged.AddListener(delegate (float value)
                {
                    Traverse.Create(FindObjectOfType<MPCharCtrl>()).Field("fkInfo").Field("sliderSize").GetValue<Slider>().value = value;
                    Traverse.Create(FindObjectOfType<MPCharCtrl>()).Field("ikInfo").Field("sliderSize").GetValue<Slider>().value = value;
                });

                foreach (var remove in RemoveText)
                    Destroy(GetPanelObject<Text>(remove).gameObject);
                foreach (var remove in RemoveToggle)
                    Destroy(GetPanelObject<Toggle>(remove).gameObject);
                foreach (var remove in RemoveButton)
                    Destroy(GetPanelObject<Button>(remove).gameObject);
            }

            private static void SetupText(string name, int offset, string text)
            {
                Text txt = Instantiate(GetPanelObject<Text>("Text Neck"), FKIKPanel.transform);
                txt.name = name;
                txt.text = text;
                txt.transform.localPosition = new Vector3(txt.transform.localPosition.x, PositionBase + (PositionOffset * offset), txt.transform.localPosition.z);
            }

            private static void SetupToggle(string name, int offset, KinematicsType kinematicsType, string referenceField)
            {
                Toggle tglOriginal = GetPanelObject<Toggle>("Toggle Neck");
                Toggle tgl = Instantiate(tglOriginal, FKIKPanel.transform);
                tgl.name = name;
                tgl.transform.localPosition = new Vector3(tgl.transform.localPosition.x, PositionBase + (PositionOffset * offset), tgl.transform.localPosition.z);

                string fieldname = kinematicsType == KinematicsType.FK ? "fkInfo" : "ikInfo";
                Toggle tglRef = Traverse.Create(FindObjectOfType<MPCharCtrl>()).Field(fieldname).Field(referenceField).GetValue<Toggle>();

                tgl.onValueChanged.RemoveAllListeners();
                tgl.isOn = tglRef.isOn;
                tgl.onValueChanged.AddListener(delegate (bool value) { tglRef.onValueChanged.Invoke(value); });
                tglRef.onValueChanged.AddListener(delegate (bool value) { tgl.isOn = value; });
            }

            private static void SetupButton(string name, int offset, KinematicsType kinematicsType, ButtonType buttonType, int index)
            {
                Button btn = Instantiate(GetPanelObject<Button>("Button For Anime (2)"), FKIKPanel.transform);
                btn.name = name;
                btn.transform.localPosition = new Vector3(btn.transform.localPosition.x, PositionBase + (PositionOffset * offset), btn.transform.localPosition.z);

                string fieldname1 = kinematicsType == KinematicsType.FK ? "fkInfo" : "ikInfo";
                string fieldname2 = buttonType == ButtonType.Anime ? "buttonAnimeSingle" : "buttonInitSingle";

                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(delegate () { Traverse.Create(FindObjectOfType<MPCharCtrl>()).Field(fieldname1).Field(fieldname2).GetValue<Button[]>()[index].onClick.Invoke(); });
            }

            private static byte[] LoadPanel()
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"KK_Plugins.Resources.Panel.png"))
                {
                    byte[] bytesInStream = new byte[stream.Length];
                    stream.Read(bytesInStream, 0, bytesInStream.Length);
                    return bytesInStream;
                }
            }

            private static T GetPanelObject<T>(string name) => FKIKPanel.GetComponentsInChildren<RectTransform>(true).First(x => x.name == name).GetComponent<T>();

            private enum KinematicsType { FK, IK }
            private enum ButtonType { Anime, Init }
        }
    }
}
