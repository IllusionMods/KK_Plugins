using KKAPI.Maker;
using KKAPI.Maker.UI;
using KKAPI.Utilities;
using Studio;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace KK_MaterialEditor
{
    public partial class KK_MaterialEditor
    {
        private Canvas UISystem;
        private ScrollRect MaterialEditorWindow;

        public const string FileExt = ".png";
        public const string FileFilter = "Images (*.png)|*.png|All files|*.*";

        private string TexPath = "";
        private string PropertyToSet = "";
        private Material MatToSet;
        private GameObject GameObjectToSet;

        private void MakerAPI_MakerBaseLoaded(object s, RegisterCustomControlsEvent e)
        {
            InitUI();

            MakerAPI.AddAccessoryWindowControl(new MakerButton("Open Material Editor", null, this)).OnClick.AddListener(delegate { PopulateListAccessory(); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Top, this)).OnClick.AddListener(delegate { PopulateListClothes(0); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Bottom, this)).OnClick.AddListener(delegate { PopulateListClothes(1); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Bra, this)).OnClick.AddListener(delegate { PopulateListClothes(2); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Shorts, this)).OnClick.AddListener(delegate { PopulateListClothes(3); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Gloves, this)).OnClick.AddListener(delegate { PopulateListClothes(4); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Panst, this)).OnClick.AddListener(delegate { PopulateListClothes(5); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Socks, this)).OnClick.AddListener(delegate { PopulateListClothes(6); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.InnerShoes, this)).OnClick.AddListener(delegate { PopulateListClothes(7); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.OuterShoes, this)).OnClick.AddListener(delegate { PopulateListClothes(8); });
        }

        private void InitUI()
        {
            UIUtility.Init(nameof(KK_MaterialEditor));

            float marginSize = 5f;
            float headerSize = 20f;
            float scrollOffsetX = -15f;
            float windowMargin = 130f;

            UISystem = UIUtility.CreateNewUISystem("MaterialEditorCanvas");
            UISystem.gameObject.SetActive(false);
            UISystem.gameObject.transform.SetParent(transform);

            var mainPanel = UIUtility.CreatePanel("Panel", UISystem.transform);
            mainPanel.color = Color.white;
            mainPanel.transform.SetRect(0.25f, 0f, 1f, 1f, windowMargin, windowMargin, -windowMargin, -windowMargin);
            UIUtility.AddOutlineToObject(mainPanel.transform, Color.black);

            var drag = UIUtility.CreatePanel("Draggable", mainPanel.transform);
            drag.transform.SetRect(0f, 1f, 1f, 1f, 0f, -headerSize);
            drag.color = Color.gray;
            UIUtility.MakeObjectDraggable(drag.rectTransform, mainPanel.rectTransform);

            var nametext = UIUtility.CreateText("Nametext", drag.transform, "Material Editor");
            nametext.transform.SetRect(0f, 0f, 1f, 1f, 0f, 0f, 0f);
            nametext.alignment = TextAnchor.MiddleCenter;

            var close = UIUtility.CreateButton("CloseButton", drag.transform, "");
            close.transform.SetRect(1f, 0f, 1f, 1f, -20f);
            close.onClick.AddListener(() => UISystem.gameObject.SetActive(false));

            //X button
            var x1 = UIUtility.CreatePanel("x1", close.transform);
            x1.transform.SetRect(0f, 0f, 1f, 1f, 8f, 0f, -8f);
            x1.rectTransform.eulerAngles = new Vector3(0f, 0f, 45f);
            x1.color = new Color(0f, 0f, 0f, 1f);
            var x2 = UIUtility.CreatePanel("x2", close.transform);
            x2.transform.SetRect(0f, 0f, 1f, 1f, 8f, 0f, -8f);
            x2.rectTransform.eulerAngles = new Vector3(0f, 0f, -45f);
            x2.color = new Color(0f, 0f, 0f, 1f);

            MaterialEditorWindow = UIUtility.CreateScrollView("MaterialEditorWindow", mainPanel.transform);
            MaterialEditorWindow.transform.SetRect(0f, 0f, 1f, 1f, marginSize, marginSize, -marginSize, -headerSize - marginSize / 2f);
            MaterialEditorWindow.gameObject.AddComponent<Mask>();
            MaterialEditorWindow.content.gameObject.AddComponent<VerticalLayoutGroup>();
            MaterialEditorWindow.content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            MaterialEditorWindow.verticalScrollbar.GetComponent<RectTransform>().offsetMin = new Vector2(scrollOffsetX, 0f);
            MaterialEditorWindow.viewport.offsetMax = new Vector2(scrollOffsetX, 0f);
            MaterialEditorWindow.movementType = ScrollRect.MovementType.Clamped;
        }

        private enum ObjectType { StudioItem, Clothes };

        private void PopulateListStudio()
        {
            if (Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNodes.Length != 1)
                return;

            TreeNodeObject[] selectNodes = Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNodes;
            for (int i = 0; i < selectNodes.Length; i++)
                if (Singleton<Studio.Studio>.Instance.dicInfo.TryGetValue(selectNodes[i], out ObjectCtrlInfo objectCtrlInfo))
                    if (objectCtrlInfo is OCIItem ociItem)
                        PopulateList(ociItem?.objectItem, ObjectType.StudioItem, GetObjectID(objectCtrlInfo));
        }

        private void PopulateListClothes(int index)
        {
            var chaControl = MakerAPI.GetCharacterControl();
            int coordinateIndex = GetCharaController(chaControl).CurrentCoordinateIndex;
            PopulateList(chaControl.objClothes[index], ObjectType.Clothes, 0, chaControl, coordinateIndex, index);
        }

        private void PopulateListAccessory()
        {
            //ChaAccessoryComponent chaAccessoryComponent = AccessoriesApi.GetAccessory(MakerAPI.GetCharacterControl(), AccessoriesApi.SelectedMakerAccSlot);
            //PopulateList(chaAccessoryComponent?.gameObject);
        }

        private void PopulateList(GameObject go, ObjectType objectType, int id = 0, ChaControl chaControl = null, int coordinateIndex = 0, int slot = 0)
        {
            UISystem.gameObject.SetActive(true);

            foreach (Transform child in MaterialEditorWindow.content)
                Destroy(child.gameObject);

            if (go == null)
                return;

            float labelWidth = 50f;
            float buttonWidth = 100f;
            float dropdownWidth = 100f;
            float textBoxWidth = 150f;
            float colorLabelWidth = 10f;
            float colorTextBoxWidth = 75f;

            List<string> mats = new List<string>();
            foreach (var rend in go.GetComponentsInChildren<Renderer>())
            {
                var contentListHeader = UIUtility.CreatePanel("ContentList", MaterialEditorWindow.content.transform);
                contentListHeader.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
                contentListHeader.gameObject.AddComponent<Mask>();
                contentListHeader.gameObject.AddComponent<HorizontalLayoutGroup>();

                var labelRenderer = UIUtility.CreateText(FormatObjectName(rend), contentListHeader.transform, "Renderer:");
                labelRenderer.alignment = TextAnchor.MiddleLeft;
                labelRenderer.color = Color.black;
                var labelRenderer2 = UIUtility.CreateText(FormatObjectName(rend), contentListHeader.transform, FormatObjectName(rend));
                labelRenderer2.alignment = TextAnchor.MiddleRight;
                labelRenderer2.color = Color.black;

                var contentItem1 = UIUtility.CreatePanel("ContentItem1", MaterialEditorWindow.content.transform);
                contentItem1.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
                contentItem1.gameObject.AddComponent<Mask>();
                contentItem1.gameObject.AddComponent<HorizontalLayoutGroup>();

                var labelShadowCastingMode = UIUtility.CreateText("ShadowCastingMode", contentItem1.transform, "ShadowCastingMode:");
                labelShadowCastingMode.alignment = TextAnchor.MiddleLeft;
                labelShadowCastingMode.color = Color.black;
                var labelShadowCastingModeLE = labelShadowCastingMode.gameObject.AddComponent<LayoutElement>();
                labelShadowCastingModeLE.preferredWidth = labelWidth;
                labelShadowCastingModeLE.flexibleWidth = labelWidth;

                var dropdownShadowCastingMode = UIUtility.CreateDropdown("ShadowCastingMode", contentItem1.transform, "ShadowCastingMode");
                dropdownShadowCastingMode.transform.SetRect(0f, 0f, 0f, 1f, 0f, 0f, 100f);
                dropdownShadowCastingMode.captionText.transform.SetRect(0f, 0f, 1f, 1f, 0f, 2f, -15f, -2f);
                dropdownShadowCastingMode.captionText.alignment = TextAnchor.MiddleLeft;
                dropdownShadowCastingMode.options.Clear();
                dropdownShadowCastingMode.options.Add(new Dropdown.OptionData("Off"));
                dropdownShadowCastingMode.options.Add(new Dropdown.OptionData("On"));
                dropdownShadowCastingMode.options.Add(new Dropdown.OptionData("TwoSided"));
                dropdownShadowCastingMode.options.Add(new Dropdown.OptionData("ShadowsOnly"));
                dropdownShadowCastingMode.value = (int)rend.shadowCastingMode;
                dropdownShadowCastingMode.captionText.text = rend.shadowCastingMode.ToString();
                dropdownShadowCastingMode.onValueChanged.AddListener((value) =>
                {
                    if (objectType == ObjectType.StudioItem)
                        GetSceneController().AddRendererProperty(id, FormatObjectName(rend), RendererProperties.ShadowCastingMode, value.ToString(), ((int)rend.shadowCastingMode).ToString());
                    else if (objectType == ObjectType.Clothes)
                        GetCharaController(chaControl).AddRendererProperty(coordinateIndex, slot, FormatObjectName(rend), RendererProperties.ShadowCastingMode, value.ToString(), ((int)rend.shadowCastingMode).ToString());
                    SetRendererProperty(rend, RendererProperties.ShadowCastingMode, value);
                });
                var dropdownShadowCastingModeLE = dropdownShadowCastingMode.gameObject.AddComponent<LayoutElement>();
                dropdownShadowCastingModeLE.preferredWidth = dropdownWidth;
                dropdownShadowCastingModeLE.flexibleWidth = 0;

                var contentItem2 = UIUtility.CreatePanel("ContentItem2", MaterialEditorWindow.content.transform);
                contentItem2.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
                contentItem2.gameObject.AddComponent<Mask>();
                contentItem2.gameObject.AddComponent<HorizontalLayoutGroup>();

                var labelReceiveShadows = UIUtility.CreateText("ReceiveShadows", contentItem2.transform, $"ReceiveShadows:");
                labelReceiveShadows.alignment = TextAnchor.MiddleLeft;
                labelReceiveShadows.color = Color.black;
                var labelReceiveShadowsLE = labelReceiveShadows.gameObject.AddComponent<LayoutElement>();
                labelReceiveShadowsLE.preferredWidth = labelWidth;
                labelReceiveShadowsLE.flexibleWidth = labelWidth;

                int receiveShadowsValue = rend.receiveShadows ? 1 : 0;
                var dropdownReceiveShadows = UIUtility.CreateDropdown("ReceiveShadows", contentItem2.transform, "ReceiveShadows");
                dropdownReceiveShadows.transform.SetRect(0f, 0f, 0f, 1f, 0f, 0f, 100f);
                dropdownReceiveShadows.captionText.transform.SetRect(0f, 0f, 1f, 1f, 0f, 2f, -15f, -2f);
                dropdownReceiveShadows.captionText.alignment = TextAnchor.MiddleLeft;
                dropdownReceiveShadows.options.Clear();
                dropdownReceiveShadows.options.Add(new Dropdown.OptionData("Off"));
                dropdownReceiveShadows.options.Add(new Dropdown.OptionData("On"));
                dropdownReceiveShadows.value = rend.receiveShadows ? 1 : 0;
                dropdownReceiveShadows.captionText.text = rend.receiveShadows ? "On" : "Off";
                dropdownReceiveShadows.onValueChanged.AddListener((value) =>
                {
                    if (objectType == ObjectType.StudioItem)
                        GetSceneController().AddRendererProperty(id, FormatObjectName(rend), RendererProperties.ReceiveShadows, value.ToString(), receiveShadowsValue.ToString());
                    else if (objectType == ObjectType.Clothes)
                        GetCharaController(chaControl).AddRendererProperty(coordinateIndex, slot, FormatObjectName(rend), RendererProperties.ReceiveShadows, value.ToString(), receiveShadowsValue.ToString());
                    SetRendererProperty(rend, RendererProperties.ReceiveShadows, value);
                });
                var dropdownReceiveShadowsLE = dropdownReceiveShadows.gameObject.AddComponent<LayoutElement>();
                dropdownReceiveShadowsLE.preferredWidth = dropdownWidth;
                dropdownReceiveShadowsLE.flexibleWidth = 0;
            }

            foreach (var rend in go.GetComponentsInChildren<Renderer>())
            {
                foreach (var mat in rend.materials)
                {
                    if (mats.Contains(FormatObjectName(mat)))
                        return;

                    mats.Add(FormatObjectName(mat));

                    var contentListHeader1 = UIUtility.CreatePanel("ContentList", MaterialEditorWindow.content.transform);
                    contentListHeader1.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
                    contentListHeader1.gameObject.AddComponent<Mask>();
                    contentListHeader1.gameObject.AddComponent<HorizontalLayoutGroup>();

                    var labelMat = UIUtility.CreateText(FormatObjectName(mat), contentListHeader1.transform, "Material:");
                    labelMat.alignment = TextAnchor.MiddleLeft;
                    labelMat.color = Color.black;
                    var labelMat2 = UIUtility.CreateText(FormatObjectName(mat), contentListHeader1.transform, FormatObjectName(mat));
                    labelMat2.alignment = TextAnchor.MiddleRight;
                    labelMat2.color = Color.black;

                    var contentListHeader2 = UIUtility.CreatePanel("ContentList", MaterialEditorWindow.content.transform);
                    contentListHeader2.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
                    contentListHeader2.gameObject.AddComponent<Mask>();
                    contentListHeader2.gameObject.AddComponent<HorizontalLayoutGroup>();

                    var labelShader = UIUtility.CreateText(FormatObjectName(mat.shader), contentListHeader2.transform, "Shader:");
                    labelShader.alignment = TextAnchor.MiddleLeft;
                    labelShader.color = Color.black;
                    var labelShader2 = UIUtility.CreateText(FormatObjectName(mat.shader), contentListHeader2.transform, FormatObjectName(mat.shader));
                    labelShader2.alignment = TextAnchor.MiddleRight;
                    labelShader2.color = Color.black;

                    foreach (var colorProperty in ColorProperties)
                    {
                        if (mat.HasProperty($"_{colorProperty}"))
                        {
                            var contentList = UIUtility.CreatePanel("ContentList", MaterialEditorWindow.content.transform);
                            contentList.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
                            contentList.gameObject.AddComponent<Mask>();
                            contentList.gameObject.AddComponent<HorizontalLayoutGroup>();

                            var label = UIUtility.CreateText(colorProperty, contentList.transform, $"{colorProperty}:");
                            label.alignment = TextAnchor.MiddleLeft;
                            label.color = Color.black;
                            var labelLE = label.gameObject.AddComponent<LayoutElement>();
                            labelLE.preferredWidth = labelWidth;
                            labelLE.flexibleWidth = labelWidth;

                            Color color = mat.GetColor($"_{colorProperty}");
                            var labelR = UIUtility.CreateText("R", contentList.transform, "R");
                            labelR.alignment = TextAnchor.MiddleLeft;
                            labelR.color = Color.black;
                            var labelRLE = labelR.gameObject.AddComponent<LayoutElement>();
                            labelRLE.preferredWidth = colorLabelWidth;
                            labelRLE.flexibleWidth = 0;

                            var textBoxR = UIUtility.CreateInputField(colorProperty, contentList.transform);
                            textBoxR.text = color.r.ToString();
                            textBoxR.onEndEdit.AddListener((value) =>
                            {
                                SetColorRProperty(go, mat, colorProperty, value);
                                if (objectType == ObjectType.StudioItem)
                                    GetSceneController().AddMaterialColorProperty(id, FormatObjectName(mat), colorProperty, mat.GetColor($"_{colorProperty}"), color);
                            });
                            var textBoxRLE = textBoxR.gameObject.AddComponent<LayoutElement>();
                            textBoxRLE.preferredWidth = colorTextBoxWidth;
                            textBoxRLE.flexibleWidth = 0;

                            var labelG = UIUtility.CreateText("G", contentList.transform, "G");
                            labelG.alignment = TextAnchor.MiddleLeft;
                            labelG.color = Color.black;
                            var labelGLE = labelG.gameObject.AddComponent<LayoutElement>();
                            labelGLE.preferredWidth = colorLabelWidth;
                            labelGLE.flexibleWidth = 0;

                            var textBoxG = UIUtility.CreateInputField(colorProperty, contentList.transform);
                            textBoxG.text = color.g.ToString();
                            textBoxG.onEndEdit.AddListener((value) =>
                            {
                                SetColorGProperty(go, mat, colorProperty, value);
                                if (objectType == ObjectType.StudioItem)
                                    GetSceneController().AddMaterialColorProperty(id, FormatObjectName(mat), colorProperty, mat.GetColor($"_{colorProperty}"), color);
                            });
                            var textBoxGLE = textBoxG.gameObject.AddComponent<LayoutElement>();
                            textBoxGLE.preferredWidth = colorTextBoxWidth;
                            textBoxGLE.flexibleWidth = 0;

                            var labelB = UIUtility.CreateText("B", contentList.transform, "B");
                            labelB.alignment = TextAnchor.MiddleLeft;
                            labelB.color = Color.black;
                            var labelBLE = labelB.gameObject.AddComponent<LayoutElement>();
                            labelBLE.preferredWidth = colorLabelWidth;
                            labelBLE.flexibleWidth = 0;

                            var textBoxB = UIUtility.CreateInputField(colorProperty, contentList.transform);
                            textBoxB.text = color.b.ToString();
                            textBoxB.onEndEdit.AddListener((value) =>
                            {
                                SetColorBProperty(go, mat, colorProperty, value);
                                if (objectType == ObjectType.StudioItem)
                                    GetSceneController().AddMaterialColorProperty(id, FormatObjectName(mat), colorProperty, mat.GetColor($"_{colorProperty}"), color);
                            });

                            var textBoxBLE = textBoxB.gameObject.AddComponent<LayoutElement>();
                            textBoxBLE.preferredWidth = colorTextBoxWidth;
                            textBoxBLE.flexibleWidth = 0;

                            var labelA = UIUtility.CreateText("A", contentList.transform, "A");
                            labelA.alignment = TextAnchor.MiddleLeft;
                            labelA.color = Color.black;
                            var labelALE = labelA.gameObject.AddComponent<LayoutElement>();
                            labelALE.preferredWidth = colorLabelWidth;
                            labelALE.flexibleWidth = 0;

                            var textBoxA = UIUtility.CreateInputField(colorProperty, contentList.transform);
                            textBoxA.text = color.a.ToString();
                            textBoxA.onEndEdit.AddListener((value) =>
                            {
                                SetColorAProperty(go, mat, colorProperty, value);
                                if (objectType == ObjectType.StudioItem)
                                    GetSceneController().AddMaterialColorProperty(id, FormatObjectName(mat), colorProperty, mat.GetColor($"_{colorProperty}"), color);
                            });

                            var textBoxALE = textBoxA.gameObject.AddComponent<LayoutElement>();
                            textBoxALE.preferredWidth = colorTextBoxWidth;
                            textBoxALE.flexibleWidth = 0;
                        }
                    }
                    foreach (var imageProperty in ImageProperties)
                    {
                        if (mat.HasProperty($"_{imageProperty}"))
                        {
                            var contentList = UIUtility.CreatePanel("ContentList", MaterialEditorWindow.content.transform);
                            contentList.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
                            contentList.gameObject.AddComponent<Mask>();
                            contentList.gameObject.AddComponent<HorizontalLayoutGroup>();

                            var label = UIUtility.CreateText(imageProperty, contentList.transform, $"{imageProperty}:");
                            label.alignment = TextAnchor.MiddleLeft;
                            label.color = Color.black;
                            var labelLE = label.gameObject.AddComponent<LayoutElement>();
                            labelLE.preferredWidth = labelWidth;
                            labelLE.flexibleWidth = labelWidth;

                            var texture = mat.GetTexture($"_{imageProperty}");

                            if (texture == null)
                            {
                                var labelNoTexture = UIUtility.CreateText($"NoTexture{imageProperty}", contentList.transform, "No Texture");
                                labelNoTexture.alignment = TextAnchor.MiddleCenter;
                                labelNoTexture.color = Color.black;
                                var labelNoTextureLE = labelNoTexture.gameObject.AddComponent<LayoutElement>();
                                labelNoTextureLE.preferredWidth = buttonWidth;
                                labelNoTextureLE.flexibleWidth = 0;
                            }
                            else
                            {
                                var exportButton = UIUtility.CreateButton($"ExportTexture{imageProperty}", contentList.transform, $"Export Texture");
                                exportButton.onClick.AddListener(() => ExportTexture(mat, imageProperty));
                                var exportButtonLE = exportButton.gameObject.AddComponent<LayoutElement>();
                                exportButtonLE.preferredWidth = buttonWidth;
                                exportButtonLE.flexibleWidth = 0;
                            }

                            var importButton = UIUtility.CreateButton($"ImportTexture{imageProperty}", contentList.transform, $"Import Texture");
                            importButton.onClick.AddListener(() => ImportTexture(go, mat, imageProperty));
                            var importButtonLE = importButton.gameObject.AddComponent<LayoutElement>();
                            importButtonLE.preferredWidth = buttonWidth;
                            importButtonLE.flexibleWidth = 0;
                        }
                    }
                    foreach (var floatProperty in FloatProperties)
                    {
                        if (mat.HasProperty($"_{floatProperty}"))
                        {
                            var contentList = UIUtility.CreatePanel("ContentList", MaterialEditorWindow.content.transform);
                            contentList.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
                            contentList.gameObject.AddComponent<Mask>();
                            contentList.gameObject.AddComponent<HorizontalLayoutGroup>();

                            var label = UIUtility.CreateText(floatProperty, contentList.transform, $"{floatProperty}:");
                            label.alignment = TextAnchor.MiddleLeft;
                            label.color = Color.black;
                            var labelLE = label.gameObject.AddComponent<LayoutElement>();
                            labelLE.preferredWidth = labelWidth;
                            labelLE.flexibleWidth = labelWidth;

                            float propertyValue = mat.GetFloat($"_{floatProperty}");
                            var textBoxProperty = UIUtility.CreateInputField(floatProperty, contentList.transform);
                            textBoxProperty.text = propertyValue.ToString();
                            textBoxProperty.onEndEdit.AddListener((value) =>
                            {
                                if (objectType == ObjectType.StudioItem)
                                    GetSceneController().AddMaterialFloatProperty(id, FormatObjectName(mat), floatProperty, value.ToString(), propertyValue.ToString());
                                else if (objectType == ObjectType.Clothes)
                                    GetCharaController(chaControl).AddMaterialFloatProperty(coordinateIndex, slot, FormatObjectName(mat), floatProperty, value.ToString(), propertyValue.ToString());
                                SetFloatProperty(go, mat, floatProperty, value);
                            });
                            var textBoxPropertyLE = textBoxProperty.gameObject.AddComponent<LayoutElement>();
                            textBoxPropertyLE.preferredWidth = textBoxWidth;
                            textBoxPropertyLE.flexibleWidth = 0;
                        }
                    }
                }
            }
        }

        private void InitStudioUI(string sceneName)
        {
            if (sceneName != "Studio")
                return;

            InitUI();

            RectTransform original = GameObject.Find("StudioScene").transform.Find("Canvas Object List/Image Bar/Button Route").GetComponent<RectTransform>();
            Button testButton = Instantiate(original.gameObject).GetComponent<Button>();
            RectTransform testButtonRectTransform = testButton.transform as RectTransform;
            testButton.transform.SetParent(original.parent, true);
            testButton.transform.localScale = original.localScale;
            testButtonRectTransform.SetRect(original.anchorMin, original.anchorMax, original.offsetMin, original.offsetMax);
            testButtonRectTransform.anchoredPosition = original.anchoredPosition + new Vector2(-48f, 0f);

            Texture2D texture2D = new Texture2D(32, 32);
            texture2D.LoadImage(LoadIcon());
            var testImage = testButton.targetGraphic as Image;
            testImage.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, 32, 32), new Vector2(16, 16));
            testImage.color = Color.white;

            testButton.onClick = new Button.ButtonClickedEvent();
            testButton.onClick.AddListener(() => { PopulateListStudio(); });
        }

        private void Update()
        {
            try
            {
                if (!TexPath.IsNullOrEmpty())
                {
                    Texture2D tex = new Texture2D(2, 2);
                    var imageBytes = File.ReadAllBytes(TexPath);
                    tex.LoadImage(imageBytes);

                    foreach (var obj in GameObjectToSet.GetComponentsInChildren<Renderer>())
                        foreach (var objMat in obj.materials)
                            if (objMat.name == MatToSet.name)
                                objMat.SetTexture($"_{PropertyToSet}", tex);
                }
            }
            catch
            {
                BepInEx.Logger.Log(BepInEx.Logging.LogLevel.Error | BepInEx.Logging.LogLevel.Message, "Failed to load texture.");
            }
            finally
            {
                TexPath = "";
                PropertyToSet = "";
                MatToSet = null;
                GameObjectToSet = null;
            }
        }
        private void ImportTexture(GameObject go, Material mat, string property)
        {
            OpenFileDialog.Show(strings => OnFileAccept(strings), "Open image", Application.dataPath, FileFilter, FileExt);

            void OnFileAccept(string[] strings)
            {
                if (strings == null || strings.Length == 0)
                    return;

                if (strings[0].IsNullOrEmpty())
                    return;

                TexPath = strings[0];
                PropertyToSet = property;
                MatToSet = mat;
                GameObjectToSet = go;
            }
        }

        private void ExportTexture(Material mat, string property)
        {
            var tex = mat.GetTexture($"_{property}");
            var dirPath = $"{Application.dataPath}/../UserData/MaterialEditor/";
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);
            SaveTex(tex, $"{dirPath}{FormatObjectName(mat)}_{property}.png");
        }

        private byte[] LoadIcon()
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{nameof(KK_MaterialEditor)}.Resources.MaterialEditorIcon.png"))
            {
                byte[] bytesInStream = new byte[stream.Length];
                stream.Read(bytesInStream, 0, bytesInStream.Length);
                return bytesInStream;
            }
        }

        public static HashSet<string> ColorProperties = new HashSet<string>()
        {
            "Clock", "Color", "Color1_2", "Color2", "Color2_2", "Color3", "Color3_2", "Color4", "Color_4", "Emission", "Line", "LiquidTiling", "Patternuv1", "Patternuv2", "Patternuv3", "Shadow",
            "Spec", "Specular", "SpecularColor", "linecolor", "overcolor1", "overcolor2", "overcolor3", "refcolor", "shadowcolor"
        };

        public static HashSet<string> ImageProperties = new HashSet<string>()
        {
            "AlphaMask", "AnotherRamp", "BumpMap", "ColorMask", "DetailMask", "EmissionMap", "GlassRamp", "HairGloss", "LineMask", "MainTex", "MetallicGlossMap", "NormalMap", "ParallaxMap",
            "PatternMask1", "PatternMask2", "PatternMask3", "Texture2", "Texture3", "liquidmask"
        };
        public static HashSet<string> FloatProperties = new HashSet<string>()
        {
            "AnotherRampFull", "BlendNormalMapScale", "BumpScale", "Cutoff", "CutoutClip", "DetailBLineG", "DetailNormalMapScale", "DetailRLineR", "DstBlend", "EmissionPower",
            "Glossiness", "GlossyReflections", "HairEffect", "LightCancel", "LineWidthS", "Metallic", "Mode", "OcclusionStrength", "Parallax", "SelectEmissionMap", "ShadowExtend",
            "ShadowExtendAnother", "Smoothness", "SmoothnessTextureChannel", "SpecularHeight", "SpecularPower", "SpecularPowerNail", "SrcBlend", "UVSec", "ZWrite", "alpha", "alpha_a",
            "alpha_b", "ambientshadowOFF", "linetexon", "linewidth", "liquidbbot", "liquidbtop", "liquidface", "liquidfbot", "liquidftop", "nip", "nipsize", "node_6948", "notusetexspecular",
            "patternclamp1", "patternclamp2", "patternclamp3", "patternrotator1", "patternrotator2", "patternrotator3", "refpower", "rimV", "rimpower"
        };
        public enum RendererProperties
        {
            ShadowCastingMode, ReceiveShadows
        }

        #region Helper Methods
        internal static Texture2D GetT2D(RenderTexture renderTexture)
        {
            var currentActiveRT = RenderTexture.active;
            RenderTexture.active = renderTexture;
            var tex = new Texture2D(renderTexture.width, renderTexture.height);
            tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
            RenderTexture.active = currentActiveRT;
            return tex;
        }

        internal static void SaveTexR(RenderTexture renderTexture, string path)
        {
            var tex = GetT2D(renderTexture);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            DestroyImmediate(tex);
        }
        internal static void SaveTex(Texture tex, string path, RenderTextureFormat rtf = RenderTextureFormat.Default, RenderTextureReadWrite cs = RenderTextureReadWrite.Default)
        {
            var tmp = RenderTexture.GetTemporary(tex.width, tex.height, 0, rtf, cs);
            var currentActiveRT = RenderTexture.active;
            RenderTexture.active = tmp;
            GL.Clear(false, true, new Color(0, 0, 0, 0));
            Graphics.Blit(tex, tmp);
            SaveTexR(tmp, path);
            RenderTexture.active = currentActiveRT;
            RenderTexture.ReleaseTemporary(tmp);
        }
        #endregion

    }
}
