using CommonCode;
using KKAPI.Maker;
using KKAPI.Maker.UI;
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
        private static Canvas UISystem;
        private static ScrollRect MaterialEditorWindow;

        public const string FileExt = ".png";
        public const string FileFilter = "Images (*.png)|*.png|All files|*.*";

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

            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Hair.Back, this)).OnClick.AddListener(delegate { PopulateListHair(0); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Hair.Front, this)).OnClick.AddListener(delegate { PopulateListHair(1); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Hair.Side, this)).OnClick.AddListener(delegate { PopulateListHair(2); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Hair.Extension, this)).OnClick.AddListener(delegate { PopulateListHair(3); });
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
            UISystem.sortingOrder = 1000;

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

        private static void PopulateListStudio()
        {
            if (Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNodes.Length != 1)
                return;

            TreeNodeObject[] selectNodes = Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNodes;
            for (int i = 0; i < selectNodes.Length; i++)
                if (Singleton<Studio.Studio>.Instance.dicInfo.TryGetValue(selectNodes[i], out ObjectCtrlInfo objectCtrlInfo))
                    if (objectCtrlInfo is OCIItem ociItem)
                        PopulateList(ociItem?.objectItem, ObjectType.StudioItem, GetObjectID(objectCtrlInfo));
        }

        private static void PopulateListClothes(int index)
        {
            var chaControl = MakerAPI.GetCharacterControl();
            int coordinateIndex = GetCharaController(chaControl).CurrentCoordinateIndex;
            PopulateList(chaControl.objClothes[index], ObjectType.Clothing, 0, chaControl, coordinateIndex, index);
        }

        private static void PopulateListAccessory()
        {
            var chaControl = MakerAPI.GetCharacterControl();
            int coordinateIndex = GetCharaController(chaControl).CurrentCoordinateIndex;
            ChaAccessoryComponent chaAccessoryComponent = AccessoriesApi.GetAccessory(MakerAPI.GetCharacterControl(), AccessoriesApi.SelectedMakerAccSlot);
            PopulateList(chaAccessoryComponent?.gameObject, ObjectType.Accessory, 0, chaControl, coordinateIndex, AccessoriesApi.SelectedMakerAccSlot);
        }

        private static void PopulateListHair(int index)
        {
            var chaControl = MakerAPI.GetCharacterControl();
            PopulateList(chaControl.objHair[index], ObjectType.Hair, 0, chaControl, 0, index);
        }

        private static void PopulateList(GameObject go, ObjectType objectType, int id = 0, ChaControl chaControl = null, int coordinateIndex = 0, int slot = 0)
        {
            UISystem.gameObject.SetActive(true);

            foreach (Transform child in MaterialEditorWindow.content)
                Destroy(child.gameObject);

            if (go == null)
                return;

            if (objectType == ObjectType.Hair)
                coordinateIndex = 0;

            float labelWidth = 50f;
            float buttonWidth = 100f;
            float dropdownWidth = 100f;
            float textBoxWidth = 150f;
            float colorLabelWidth = 10f;
            float colorTextBoxWidth = 75f;
            float resetButtonWidth = 50f;
            RectOffset padding = new RectOffset(3, 3, 0, 1);

            List<string> mats = new List<string>();
            foreach (var rend in go.GetComponentsInChildren<Renderer>())
            {
                var contentListHeader = UIUtility.CreatePanel("ContentList", MaterialEditorWindow.content.transform);
                contentListHeader.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
                contentListHeader.gameObject.AddComponent<Mask>();
                contentListHeader.gameObject.AddComponent<HorizontalLayoutGroup>().padding = padding;

                var labelRenderer = UIUtility.CreateText(rend.NameFormatted(), contentListHeader.transform, "Renderer:");
                labelRenderer.alignment = TextAnchor.MiddleLeft;
                labelRenderer.color = Color.black;
                var labelRendererLE = labelRenderer.gameObject.AddComponent<LayoutElement>();
                labelRendererLE.preferredWidth = labelWidth;
                labelRendererLE.flexibleWidth = labelWidth;

                var labelRenderer2 = UIUtility.CreateText(rend.NameFormatted(), contentListHeader.transform, rend.NameFormatted());
                labelRenderer2.alignment = TextAnchor.MiddleRight;
                labelRenderer2.color = Color.black;
                var labelRenderer2LE = labelRenderer2.gameObject.AddComponent<LayoutElement>();
                labelRenderer2LE.preferredWidth = 250;
                labelRenderer2LE.flexibleWidth = 0;

                var exportUVButton = UIUtility.CreateButton("ExportUVButton", contentListHeader.transform, "Export UV Map");
                exportUVButton.onClick.AddListener(() => { ExportUVMap(rend); });
                var exportUVButtonLE = exportUVButton.gameObject.AddComponent<LayoutElement>();
                exportUVButtonLE.preferredWidth = 110;
                exportUVButtonLE.flexibleWidth = 0;

                var contentItem1 = UIUtility.CreatePanel("ContentItem1", MaterialEditorWindow.content.transform);
                contentItem1.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
                contentItem1.gameObject.AddComponent<Mask>();
                contentItem1.gameObject.AddComponent<HorizontalLayoutGroup>().padding = padding;

                var labelEnabled = UIUtility.CreateText("Enabled", contentItem1.transform, "Enabled:");
                labelEnabled.alignment = TextAnchor.MiddleLeft;
                labelEnabled.color = Color.black;
                var labelEnabledLE = labelEnabled.gameObject.AddComponent<LayoutElement>();
                labelEnabledLE.preferredWidth = labelWidth;
                labelEnabledLE.flexibleWidth = labelWidth;

                bool valueEnabled = rend.enabled;
                bool valueEnabledInitial = rend.enabled;
                if (objectType == ObjectType.Other) { }
                else if (objectType == ObjectType.StudioItem)
                {
                    if (GetSceneController().GetRendererPropertyValueOriginal(id, rend.NameFormatted(), RendererProperties.Enabled) != null)
                        valueEnabledInitial = GetSceneController().GetRendererPropertyValueOriginal(id, rend.NameFormatted(), RendererProperties.Enabled) == "1";
                }
                else if (GetCharaController(chaControl).GetRendererPropertyValueOriginal(objectType, coordinateIndex, slot, rend.NameFormatted(), RendererProperties.Enabled) != null)
                    valueEnabledInitial = GetCharaController(chaControl).GetRendererPropertyValueOriginal(objectType, coordinateIndex, slot, rend.NameFormatted(), RendererProperties.Enabled) == "1";

                var dropdownEnabled = UIUtility.CreateDropdown("Enabled", contentItem1.transform, "Enabled");
                dropdownEnabled.transform.SetRect(0f, 0f, 0f, 1f, 0f, 0f, 100f);
                dropdownEnabled.captionText.transform.SetRect(0f, 0f, 1f, 1f, 0f, 2f, -15f, -2f);
                dropdownEnabled.captionText.alignment = TextAnchor.MiddleLeft;
                dropdownEnabled.options.Clear();
                dropdownEnabled.options.Add(new Dropdown.OptionData("Off"));
                dropdownEnabled.options.Add(new Dropdown.OptionData("On"));
                dropdownEnabled.value = valueEnabled ? 1 : 0;
                dropdownEnabled.captionText.text = valueEnabled ? "On" : "Off";
                dropdownEnabled.onValueChanged.AddListener((value) =>
                {
                    if (objectType == ObjectType.Other) { }
                    else if (objectType == ObjectType.StudioItem)
                        GetSceneController().AddRendererProperty(id, rend.NameFormatted(), RendererProperties.Enabled, value.ToString(), valueEnabledInitial ? "1" : "0");
                    else
                        GetCharaController(chaControl).AddRendererProperty(objectType, coordinateIndex, slot, rend.NameFormatted(), RendererProperties.Enabled, value.ToString(), valueEnabledInitial ? "1" : "0");
                    SetRendererProperty(rend, RendererProperties.Enabled, value);
                });
                var dropdownEnabledLE = dropdownEnabled.gameObject.AddComponent<LayoutElement>();
                dropdownEnabledLE.preferredWidth = dropdownWidth;
                dropdownEnabledLE.flexibleWidth = 0;

                var resetEnabled = UIUtility.CreateButton("ResetEnabled", contentItem1.transform, "Reset");
                resetEnabled.onClick.AddListener(() =>
                {
                    if (objectType == ObjectType.Other) { }
                    else if (objectType == ObjectType.StudioItem)
                        GetSceneController().RemoveRendererProperty(id, rend.NameFormatted(), RendererProperties.Enabled);
                    else
                        GetCharaController(chaControl).RemoveRendererProperty(objectType, coordinateIndex, slot, rend.NameFormatted(), RendererProperties.Enabled);
                    SetRendererProperty(rend, RendererProperties.Enabled, valueEnabledInitial ? 1 : 0);
                    dropdownEnabled.value = valueEnabledInitial ? 1 : 0;
                });
                var resetEnabledLE = resetEnabled.gameObject.AddComponent<LayoutElement>();
                resetEnabledLE.preferredWidth = resetButtonWidth;
                resetEnabledLE.flexibleWidth = 0;

                var contentItem2 = UIUtility.CreatePanel("ContentItem2", MaterialEditorWindow.content.transform);
                contentItem2.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
                contentItem2.gameObject.AddComponent<Mask>();
                contentItem2.gameObject.AddComponent<HorizontalLayoutGroup>().padding = padding;

                var labelShadowCastingMode = UIUtility.CreateText("ShadowCastingMode", contentItem2.transform, "ShadowCastingMode:");
                labelShadowCastingMode.alignment = TextAnchor.MiddleLeft;
                labelShadowCastingMode.color = Color.black;
                var labelShadowCastingModeLE = labelShadowCastingMode.gameObject.AddComponent<LayoutElement>();
                labelShadowCastingModeLE.preferredWidth = labelWidth;
                labelShadowCastingModeLE.flexibleWidth = labelWidth;

                var valueShadowCastingMode = rend.shadowCastingMode;
                var valueShadowCastingModeInitial = rend.shadowCastingMode;
                if (objectType == ObjectType.Other) { }
                else if (objectType == ObjectType.StudioItem)
                {
                    if (GetSceneController().GetRendererPropertyValueOriginal(id, rend.NameFormatted(), RendererProperties.ShadowCastingMode) != null)
                        valueShadowCastingModeInitial = (UnityEngine.Rendering.ShadowCastingMode)int.Parse(GetSceneController().GetRendererPropertyValueOriginal(id, rend.NameFormatted(), RendererProperties.ShadowCastingMode));
                }
                else if (GetCharaController(chaControl).GetRendererPropertyValueOriginal(objectType, coordinateIndex, slot, rend.NameFormatted(), RendererProperties.ShadowCastingMode) != null)
                    valueShadowCastingModeInitial = (UnityEngine.Rendering.ShadowCastingMode)int.Parse(GetCharaController(chaControl).GetRendererPropertyValueOriginal(objectType, coordinateIndex, slot, rend.NameFormatted(), RendererProperties.ShadowCastingMode));

                var dropdownShadowCastingMode = UIUtility.CreateDropdown("ShadowCastingMode", contentItem2.transform, "ShadowCastingMode");
                dropdownShadowCastingMode.transform.SetRect(0f, 0f, 0f, 1f, 0f, 0f, 100f);
                dropdownShadowCastingMode.captionText.transform.SetRect(0f, 0f, 1f, 1f, 0f, 2f, -15f, -2f);
                dropdownShadowCastingMode.captionText.alignment = TextAnchor.MiddleLeft;
                dropdownShadowCastingMode.options.Clear();
                dropdownShadowCastingMode.options.Add(new Dropdown.OptionData("Off"));
                dropdownShadowCastingMode.options.Add(new Dropdown.OptionData("On"));
                dropdownShadowCastingMode.options.Add(new Dropdown.OptionData("TwoSided"));
                dropdownShadowCastingMode.options.Add(new Dropdown.OptionData("ShadowsOnly"));
                dropdownShadowCastingMode.value = (int)valueShadowCastingMode;
                dropdownShadowCastingMode.captionText.text = valueShadowCastingMode.ToString();
                dropdownShadowCastingMode.onValueChanged.AddListener((value) =>
                {
                    if (objectType == ObjectType.Other) { }
                    else if (objectType == ObjectType.StudioItem)
                        GetSceneController().AddRendererProperty(id, rend.NameFormatted(), RendererProperties.ShadowCastingMode, value.ToString(), ((int)valueShadowCastingModeInitial).ToString());
                    else
                        GetCharaController(chaControl).AddRendererProperty(objectType, coordinateIndex, slot, rend.NameFormatted(), RendererProperties.ShadowCastingMode, value.ToString(), ((int)valueShadowCastingModeInitial).ToString());
                    SetRendererProperty(rend, RendererProperties.ShadowCastingMode, value);
                });
                var dropdownShadowCastingModeLE = dropdownShadowCastingMode.gameObject.AddComponent<LayoutElement>();
                dropdownShadowCastingModeLE.preferredWidth = dropdownWidth;
                dropdownShadowCastingModeLE.flexibleWidth = 0;

                var resetShadowCastingMode = UIUtility.CreateButton("ResetShadowCastingMode", contentItem2.transform, "Reset");
                resetShadowCastingMode.onClick.AddListener(() =>
                {
                    if (objectType == ObjectType.Other) { }
                    else if (objectType == ObjectType.StudioItem)
                        GetSceneController().RemoveRendererProperty(id, rend.NameFormatted(), RendererProperties.ShadowCastingMode);
                    else
                        GetCharaController(chaControl).RemoveRendererProperty(objectType, coordinateIndex, slot, rend.NameFormatted(), RendererProperties.ShadowCastingMode);
                    SetRendererProperty(rend, RendererProperties.ShadowCastingMode, (int)valueShadowCastingModeInitial);
                    dropdownShadowCastingMode.value = (int)valueShadowCastingModeInitial;
                });
                var resetShadowCastingModeLE = resetShadowCastingMode.gameObject.AddComponent<LayoutElement>();
                resetShadowCastingModeLE.preferredWidth = resetButtonWidth;
                resetShadowCastingModeLE.flexibleWidth = 0;

                var contentItem3 = UIUtility.CreatePanel("ContentItem3", MaterialEditorWindow.content.transform);
                contentItem3.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
                contentItem3.gameObject.AddComponent<Mask>();
                contentItem3.gameObject.AddComponent<HorizontalLayoutGroup>().padding = padding;

                var labelReceiveShadows = UIUtility.CreateText("ReceiveShadows", contentItem3.transform, $"ReceiveShadows:");
                labelReceiveShadows.alignment = TextAnchor.MiddleLeft;
                labelReceiveShadows.color = Color.black;
                var labelReceiveShadowsLE = labelReceiveShadows.gameObject.AddComponent<LayoutElement>();
                labelReceiveShadowsLE.preferredWidth = labelWidth;
                labelReceiveShadowsLE.flexibleWidth = labelWidth;

                bool valueReceiveShadows = rend.receiveShadows;
                bool valueReceiveShadowsInitial = rend.receiveShadows;
                if (objectType == ObjectType.Other) { }
                else if (objectType == ObjectType.StudioItem)
                {
                    if (GetSceneController().GetRendererPropertyValueOriginal(id, rend.NameFormatted(), RendererProperties.ReceiveShadows) != null)
                        valueReceiveShadowsInitial = GetSceneController().GetRendererPropertyValueOriginal(id, rend.NameFormatted(), RendererProperties.ReceiveShadows) == "1";
                }
                else if (GetCharaController(chaControl).GetRendererPropertyValueOriginal(objectType, coordinateIndex, slot, rend.NameFormatted(), RendererProperties.ReceiveShadows) != null)
                    valueReceiveShadowsInitial = GetCharaController(chaControl).GetRendererPropertyValueOriginal(objectType, coordinateIndex, slot, rend.NameFormatted(), RendererProperties.ReceiveShadows) == "1";

                var dropdownReceiveShadows = UIUtility.CreateDropdown("ReceiveShadows", contentItem3.transform, "ReceiveShadows");
                dropdownReceiveShadows.transform.SetRect(0f, 0f, 0f, 1f, 0f, 0f, 100f);
                dropdownReceiveShadows.captionText.transform.SetRect(0f, 0f, 1f, 1f, 0f, 2f, -15f, -2f);
                dropdownReceiveShadows.captionText.alignment = TextAnchor.MiddleLeft;
                dropdownReceiveShadows.options.Clear();
                dropdownReceiveShadows.options.Add(new Dropdown.OptionData("Off"));
                dropdownReceiveShadows.options.Add(new Dropdown.OptionData("On"));
                dropdownReceiveShadows.value = valueReceiveShadows ? 1 : 0;
                dropdownReceiveShadows.captionText.text = valueReceiveShadows ? "On" : "Off";
                dropdownReceiveShadows.onValueChanged.AddListener((value) =>
                {
                    if (objectType == ObjectType.Other) { }
                    else if (objectType == ObjectType.StudioItem)
                        GetSceneController().AddRendererProperty(id, rend.NameFormatted(), RendererProperties.ReceiveShadows, value.ToString(), valueReceiveShadowsInitial ? "1" : "0");
                    else
                        GetCharaController(chaControl).AddRendererProperty(objectType, coordinateIndex, slot, rend.NameFormatted(), RendererProperties.ReceiveShadows, value.ToString(), valueReceiveShadowsInitial ? "1" : "0");
                    SetRendererProperty(rend, RendererProperties.ReceiveShadows, value);
                });
                var dropdownReceiveShadowsLE = dropdownReceiveShadows.gameObject.AddComponent<LayoutElement>();
                dropdownReceiveShadowsLE.preferredWidth = dropdownWidth;
                dropdownReceiveShadowsLE.flexibleWidth = 0;

                var resetReceiveShadows = UIUtility.CreateButton("ResetReceiveShadows", contentItem3.transform, "Reset");
                resetReceiveShadows.onClick.AddListener(() =>
                {
                    if (objectType == ObjectType.Other) { }
                    else if (objectType == ObjectType.StudioItem)
                        GetSceneController().RemoveRendererProperty(id, rend.NameFormatted(), RendererProperties.ReceiveShadows);
                    else
                        GetCharaController(chaControl).RemoveRendererProperty(objectType, coordinateIndex, slot, rend.NameFormatted(), RendererProperties.ReceiveShadows);
                    SetRendererProperty(rend, RendererProperties.ReceiveShadows, valueReceiveShadowsInitial ? 1 : 0);
                    dropdownReceiveShadows.value = valueReceiveShadowsInitial ? 1 : 0;
                });
                var resetReceiveShadowsLE = resetReceiveShadows.gameObject.AddComponent<LayoutElement>();
                resetReceiveShadowsLE.preferredWidth = resetButtonWidth;
                resetReceiveShadowsLE.flexibleWidth = 0;
            }

            foreach (var rend in go.GetComponentsInChildren<Renderer>())
            {
                foreach (var mat in rend.materials)
                {
                    if (mats.Contains(mat.NameFormatted()))
                        return;

                    mats.Add(mat.NameFormatted());

                    var contentListHeader1 = UIUtility.CreatePanel("ContentList", MaterialEditorWindow.content.transform);
                    contentListHeader1.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
                    contentListHeader1.gameObject.AddComponent<Mask>();
                    contentListHeader1.gameObject.AddComponent<HorizontalLayoutGroup>().padding = padding;

                    var labelMat = UIUtility.CreateText(mat.NameFormatted(), contentListHeader1.transform, "Material:");
                    labelMat.alignment = TextAnchor.MiddleLeft;
                    labelMat.color = Color.black;
                    var labelMat2 = UIUtility.CreateText(mat.NameFormatted(), contentListHeader1.transform, mat.NameFormatted());
                    labelMat2.alignment = TextAnchor.MiddleRight;
                    labelMat2.color = Color.black;

                    var contentListHeader2 = UIUtility.CreatePanel("ContentList", MaterialEditorWindow.content.transform);
                    contentListHeader2.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
                    contentListHeader2.gameObject.AddComponent<Mask>();
                    contentListHeader2.gameObject.AddComponent<HorizontalLayoutGroup>().padding = padding;

                    var labelShader = UIUtility.CreateText(mat.shader.NameFormatted(), contentListHeader2.transform, "Shader:");
                    labelShader.alignment = TextAnchor.MiddleLeft;
                    labelShader.color = Color.black;
                    var labelShader2 = UIUtility.CreateText(mat.shader.NameFormatted(), contentListHeader2.transform, mat.shader.NameFormatted());
                    labelShader2.alignment = TextAnchor.MiddleRight;
                    labelShader2.color = Color.black;

                    foreach (var colorProperty in ColorProperties)
                    {
                        if (objectType == ObjectType.Clothing && ClothesBlacklist.Contains(colorProperty))
                            continue;
                        if (objectType == ObjectType.Accessory && AccessoryBlacklist.Contains(colorProperty))
                            continue;
                        if (objectType == ObjectType.Hair && HairBlacklist.Contains(colorProperty))
                            continue;

                        if (mat.HasProperty($"_{colorProperty}"))
                        {
                            var contentList = UIUtility.CreatePanel("ContentList", MaterialEditorWindow.content.transform);
                            contentList.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
                            contentList.gameObject.AddComponent<Mask>();
                            contentList.gameObject.AddComponent<HorizontalLayoutGroup>().padding = padding;

                            var label = UIUtility.CreateText(colorProperty, contentList.transform, $"{colorProperty}:");
                            label.alignment = TextAnchor.MiddleLeft;
                            label.color = Color.black;
                            var labelLE = label.gameObject.AddComponent<LayoutElement>();
                            labelLE.preferredWidth = labelWidth;
                            labelLE.flexibleWidth = labelWidth;

                            Color valueColor = mat.GetColor($"_{colorProperty}");
                            Color valueColorInitial = valueColor;
                            if (objectType == ObjectType.StudioItem)
                            {
                                Color c = GetSceneController().GetMaterialColorPropertyValueOriginal(id, mat.NameFormatted(), colorProperty);
                                if (c.r != -1 && c.g != -1 && c.b != -1 && c.a != -1)
                                    valueColorInitial = c;
                            }

                            var labelR = UIUtility.CreateText("R", contentList.transform, "R");
                            labelR.alignment = TextAnchor.MiddleLeft;
                            labelR.color = Color.black;
                            var labelRLE = labelR.gameObject.AddComponent<LayoutElement>();
                            labelRLE.preferredWidth = colorLabelWidth;
                            labelRLE.flexibleWidth = 0;

                            var textBoxR = UIUtility.CreateInputField(colorProperty, contentList.transform);
                            textBoxR.text = valueColor.r.ToString();
                            textBoxR.onEndEdit.AddListener((value) =>
                            {
                                SetColorRProperty(go, mat, colorProperty, value);
                                if (objectType == ObjectType.Other) { }
                                else if (objectType == ObjectType.StudioItem)
                                    GetSceneController().AddMaterialColorProperty(id, mat.NameFormatted(), colorProperty, mat.GetColor($"_{colorProperty}"), valueColorInitial);
                                else
                                    GetCharaController(chaControl).AddMaterialColorProperty(objectType, coordinateIndex, slot, mat.NameFormatted(), colorProperty, mat.GetColor($"_{colorProperty}"), valueColorInitial);
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
                            textBoxG.text = valueColor.g.ToString();
                            textBoxG.onEndEdit.AddListener((value) =>
                            {
                                SetColorGProperty(go, mat, colorProperty, value);
                                if (objectType == ObjectType.Other) { }
                                else if (objectType == ObjectType.StudioItem)
                                    GetSceneController().AddMaterialColorProperty(id, mat.NameFormatted(), colorProperty, mat.GetColor($"_{colorProperty}"), valueColorInitial);
                                else
                                    GetCharaController(chaControl).AddMaterialColorProperty(objectType, coordinateIndex, slot, mat.NameFormatted(), colorProperty, mat.GetColor($"_{colorProperty}"), valueColorInitial);
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
                            textBoxB.text = valueColor.b.ToString();
                            textBoxB.onEndEdit.AddListener((value) =>
                            {
                                SetColorBProperty(go, mat, colorProperty, value);
                                if (objectType == ObjectType.Other) { }
                                else if (objectType == ObjectType.StudioItem)
                                    GetSceneController().AddMaterialColorProperty(id, mat.NameFormatted(), colorProperty, mat.GetColor($"_{colorProperty}"), valueColorInitial);
                                else
                                    GetCharaController(chaControl).AddMaterialColorProperty(objectType, coordinateIndex, slot, mat.NameFormatted(), colorProperty, mat.GetColor($"_{colorProperty}"), valueColorInitial);
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
                            textBoxA.text = valueColor.a.ToString();
                            textBoxA.onEndEdit.AddListener((value) =>
                            {
                                SetColorAProperty(go, mat, colorProperty, value);
                                if (objectType == ObjectType.Other) { }
                                else if (objectType == ObjectType.StudioItem)
                                    GetSceneController().AddMaterialColorProperty(id, mat.NameFormatted(), colorProperty, mat.GetColor($"_{colorProperty}"), valueColorInitial);
                                else
                                    GetCharaController(chaControl).AddMaterialColorProperty(objectType, coordinateIndex, slot, mat.NameFormatted(), colorProperty, mat.GetColor($"_{colorProperty}"), valueColorInitial);
                            });

                            var textBoxALE = textBoxA.gameObject.AddComponent<LayoutElement>();
                            textBoxALE.preferredWidth = colorTextBoxWidth;
                            textBoxALE.flexibleWidth = 0;

                            var resetColor = UIUtility.CreateButton($"Reset{colorProperty}", contentList.transform, "Reset");
                            resetColor.onClick.AddListener(() =>
                            {
                                if (objectType == ObjectType.Other) { }
                                else if (objectType == ObjectType.StudioItem)
                                    GetSceneController().RemoveMaterialColorProperty(id, mat.NameFormatted(), colorProperty);
                                else
                                    GetCharaController(chaControl).RemoveMaterialColorProperty(objectType, coordinateIndex, slot, mat.NameFormatted(), colorProperty);
                                SetColorProperty(go, mat, colorProperty, valueColorInitial);
                                textBoxR.text = valueColorInitial.r.ToString();
                                textBoxG.text = valueColorInitial.g.ToString();
                                textBoxB.text = valueColorInitial.b.ToString();
                                textBoxA.text = valueColorInitial.a.ToString();
                            });
                            var resetColorLE = resetColor.gameObject.AddComponent<LayoutElement>();
                            resetColorLE.preferredWidth = resetButtonWidth;
                            resetColorLE.flexibleWidth = 0;
                        }
                    }
                    foreach (var textureProperty in TextureProperties)
                    {
                        if (objectType == ObjectType.Clothing && ClothesBlacklist.Contains(textureProperty))
                            continue;
                        if (objectType == ObjectType.Accessory && AccessoryBlacklist.Contains(textureProperty))
                            continue;
                        if (objectType == ObjectType.Hair && HairBlacklist.Contains(textureProperty))
                            continue;

                        if (mat.HasProperty($"_{textureProperty}"))
                        {
                            var contentList = UIUtility.CreatePanel("ContentList", MaterialEditorWindow.content.transform);
                            contentList.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
                            contentList.gameObject.AddComponent<Mask>();
                            contentList.gameObject.AddComponent<HorizontalLayoutGroup>().padding = padding;

                            var label = UIUtility.CreateText(textureProperty, contentList.transform, $"{textureProperty}:");
                            label.alignment = TextAnchor.MiddleLeft;
                            label.color = Color.black;
                            var labelLE = label.gameObject.AddComponent<LayoutElement>();
                            labelLE.preferredWidth = labelWidth;
                            labelLE.flexibleWidth = labelWidth;

                            var texture = mat.GetTexture($"_{textureProperty}");

                            if (texture == null)
                            {
                                var labelNoTexture = UIUtility.CreateText($"NoTexture{textureProperty}", contentList.transform, "No Texture");
                                labelNoTexture.alignment = TextAnchor.MiddleCenter;
                                labelNoTexture.color = Color.black;
                                var labelNoTextureLE = labelNoTexture.gameObject.AddComponent<LayoutElement>();
                                labelNoTextureLE.preferredWidth = buttonWidth;
                                labelNoTextureLE.flexibleWidth = 0;
                            }
                            else
                            {
                                var exportButton = UIUtility.CreateButton($"ExportTexture{textureProperty}", contentList.transform, $"Export Texture");
                                exportButton.onClick.AddListener(() => ExportTexture(mat, textureProperty));
                                var exportButtonLE = exportButton.gameObject.AddComponent<LayoutElement>();
                                exportButtonLE.preferredWidth = buttonWidth;
                                exportButtonLE.flexibleWidth = 0;
                            }

                            var importButton = UIUtility.CreateButton($"ImportTexture{textureProperty}", contentList.transform, $"Import Texture");
                            importButton.onClick.AddListener(() =>
                            {
                                if (objectType == ObjectType.Other) { }
                                else if (objectType == ObjectType.StudioItem)
                                    GetSceneController().AddMaterialTextureProperty(id, mat.NameFormatted(), textureProperty, go);
                                else
                                    GetCharaController(chaControl).AddMaterialTextureProperty(objectType, coordinateIndex, slot, mat.NameFormatted(), textureProperty, go);
                            });
                            var importButtonLE = importButton.gameObject.AddComponent<LayoutElement>();
                            importButtonLE.preferredWidth = buttonWidth;
                            importButtonLE.flexibleWidth = 0;

                            var resetTexture = UIUtility.CreateButton($"Reset{textureProperty}", contentList.transform, "Reset");
                            resetTexture.onClick.AddListener(() =>
                            {
                                if (objectType == ObjectType.Other) { }
                                else if (objectType == ObjectType.StudioItem)
                                    GetSceneController().RemoveMaterialTextureProperty(id, mat.NameFormatted(), textureProperty);
                                else
                                    GetCharaController(chaControl).RemoveMaterialTextureProperty(objectType, coordinateIndex, slot, mat.NameFormatted(), textureProperty);
                            });
                            var reseTextureLE = resetTexture.gameObject.AddComponent<LayoutElement>();
                            reseTextureLE.preferredWidth = resetButtonWidth;
                            reseTextureLE.flexibleWidth = 0;
                        }
                    }
                    foreach (var floatProperty in FloatProperties)
                    {
                        if (objectType == ObjectType.Clothing && ClothesBlacklist.Contains(floatProperty))
                            continue;
                        if (objectType == ObjectType.Accessory && AccessoryBlacklist.Contains(floatProperty))
                            continue;
                        if (objectType == ObjectType.Hair && HairBlacklist.Contains(floatProperty))
                            continue;

                        if (mat.HasProperty($"_{floatProperty}"))
                        {
                            var contentList = UIUtility.CreatePanel("ContentList", MaterialEditorWindow.content.transform);
                            contentList.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
                            contentList.gameObject.AddComponent<Mask>();
                            contentList.gameObject.AddComponent<HorizontalLayoutGroup>().padding = padding;

                            var label = UIUtility.CreateText(floatProperty, contentList.transform, $"{floatProperty}:");
                            label.alignment = TextAnchor.MiddleLeft;
                            label.color = Color.black;
                            var labelLE = label.gameObject.AddComponent<LayoutElement>();
                            labelLE.preferredWidth = labelWidth;
                            labelLE.flexibleWidth = labelWidth;

                            string valueFloat = mat.GetFloat($"_{floatProperty}").ToString();
                            string valueFloatInitial = valueFloat;
                            if (objectType == ObjectType.Other) { }
                            else if (objectType == ObjectType.StudioItem)
                            {
                                if (GetSceneController().GetMaterialFloatPropertyValueOriginal(id, mat.NameFormatted(), floatProperty) != null)
                                    valueFloatInitial = GetSceneController().GetMaterialFloatPropertyValueOriginal(id, mat.NameFormatted(), floatProperty);
                            }
                            else if (GetCharaController(chaControl).GetMaterialFloatPropertyValueOriginal(objectType, coordinateIndex, slot, mat.NameFormatted(), floatProperty) != null)
                                valueFloatInitial = GetCharaController(chaControl).GetMaterialFloatPropertyValueOriginal(objectType, coordinateIndex, slot, mat.NameFormatted(), floatProperty);

                            var textBoxFloat = UIUtility.CreateInputField(floatProperty, contentList.transform);
                            textBoxFloat.text = valueFloat;
                            textBoxFloat.onEndEdit.AddListener((value) =>
                            {
                                if (objectType == ObjectType.Other) { }
                                else if (objectType == ObjectType.StudioItem)
                                    GetSceneController().AddMaterialFloatProperty(id, mat.NameFormatted(), floatProperty, value, valueFloatInitial);
                                else
                                    GetCharaController(chaControl).AddMaterialFloatProperty(objectType, coordinateIndex, slot, mat.NameFormatted(), floatProperty, value, valueFloatInitial);
                                SetFloatProperty(go, mat, floatProperty, value);
                            });
                            var textBoxFloatLE = textBoxFloat.gameObject.AddComponent<LayoutElement>();
                            textBoxFloatLE.preferredWidth = textBoxWidth;
                            textBoxFloatLE.flexibleWidth = 0;

                            var resetFloat = UIUtility.CreateButton($"Reset{floatProperty}", contentList.transform, "Reset");
                            resetFloat.onClick.AddListener(() =>
                            {
                                if (objectType == ObjectType.Other) { }
                                else if (objectType == ObjectType.StudioItem)
                                    GetSceneController().RemoveMaterialFloatProperty(id, mat.NameFormatted(), floatProperty);
                                else
                                    GetCharaController(chaControl).RemoveMaterialFloatProperty(objectType, coordinateIndex, slot, mat.NameFormatted(), floatProperty);
                                SetFloatProperty(go, mat, floatProperty, valueFloatInitial);
                                textBoxFloat.text = valueFloatInitial;
                            });
                            var resetEnabledLE = resetFloat.gameObject.AddComponent<LayoutElement>();
                            resetEnabledLE.preferredWidth = resetButtonWidth;
                            resetEnabledLE.flexibleWidth = 0;
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
            var MatEditorIcon = testButton.targetGraphic as Image;
            MatEditorIcon.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, 32, 32), new Vector2(16, 16));
            MatEditorIcon.color = Color.white;

            testButton.onClick = new Button.ButtonClickedEvent();
            testButton.onClick.AddListener(() => { PopulateListStudio(); });
        }

        private static void ExportTexture(Material mat, string property)
        {
            var tex = mat.GetTexture($"_{property}");
            string filename = Path.Combine(ExportPath, $"{mat.NameFormatted()}_{property}_{System.DateTime.Now:yyyy-MM-dd-HH-mm-ss}.png");
            SaveTex(tex, filename);
            CC.Log($"Exported {filename}");
            CC.OpenFileInExplorer(filename);
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

        private static void ExportUVMap(Renderer rend)
        {
            bool openedFile = false;
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            var lineMaterial = new Material(shader);
            lineMaterial.hideFlags = HideFlags.HideAndDontSave;
            lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            lineMaterial.SetInt("_ZWrite", 0);

            Mesh mr;
            if (rend is MeshRenderer meshRenderer)
                mr = meshRenderer.GetComponent<MeshFilter>().mesh;
            else if (rend is SkinnedMeshRenderer skinnedMeshRenderer)
                mr = skinnedMeshRenderer.sharedMesh;
            else return;

            for (int x = 0; x < mr.subMeshCount; x++)
            {
                var tris = mr.GetTriangles(x);
                var uvs = mr.uv;

                const int size = 4096;
                var _renderTexture = RenderTexture.GetTemporary(size, size);
                var lineColor = Color.black;
                Graphics.SetRenderTarget(_renderTexture);
                GL.PushMatrix();
                GL.LoadOrtho();
                //GL.LoadPixelMatrix(); // * 2 - 1, maps differently.
                GL.Clear(false, true, Color.clear);

                lineMaterial.SetPass(0);
                GL.Begin(GL.LINES);
                GL.Color(lineColor);

                for (var i = 0; i < tris.Length; i += 3)
                {
                    var v = uvs[tris[i]];
                    var n1 = uvs[tris[i + 1]];
                    var n2 = uvs[tris[i + 2]];

                    GL.Vertex(v);
                    GL.Vertex(n1);

                    GL.Vertex(v);
                    GL.Vertex(n2);

                    GL.Vertex(n1);
                    GL.Vertex(n2);
                }
                GL.End();

                GL.PopMatrix();
                Graphics.SetRenderTarget(null);

                var png = GetT2D(_renderTexture);
                RenderTexture.ReleaseTemporary(_renderTexture);

                string filename = Path.Combine(ExportPath, $"{rend.NameFormatted()}_{x}.png");
                File.WriteAllBytes(filename, png.EncodeToPNG());
                DestroyImmediate(png);
                CC.Log($"Exported {filename}");
                if (!openedFile)
                    CC.OpenFileInExplorer(filename);
                openedFile = true;
            }
        }

        public static HashSet<string> ClothesBlacklist = new HashSet<string>()
        {
            "MainTex"
        };

        public static HashSet<string> AccessoryBlacklist = new HashSet<string>()
        {
            "Color", "Color2", "Color3", "Color4", "HairGloss"
        };

        public static HashSet<string> HairBlacklist = new HashSet<string>()
        {
            "Color", "Color2", "Color3", "Color4", "HairGloss"
        };


        public static HashSet<string> ColorProperties = new HashSet<string>()
        {
            "Clock", "Color", "Color1_2", "Color2", "Color2_2", "Color3", "Color3_2", "Color4", "Color_4", "Emission", "Line", "LiquidTiling", "Patternuv1", "Patternuv2", "Patternuv3", "Shadow",
            "Spec", "Specular", "SpecularColor", "linecolor", "overcolor1", "overcolor2", "overcolor3", "refcolor", "shadowcolor"
        };

        public static HashSet<string> TextureProperties = new HashSet<string>()
        {
            "AnimationMask", "AlphaMask", "AnotherRamp", "BumpMap", "ColorMask", "DetailMask", "EmissionMap", "GlassRamp", "HairGloss", "LineMask", "MainTex", "MetallicGlossMap", "NormalMap",
            "ParallaxMap", "PatternMask1", "PatternMask2", "PatternMask3", "Texture2", "Texture3", "liquidmask"
        };
        public static HashSet<string> FloatProperties = new HashSet<string>()
        {
            "AnotherRampFull", "BlendNormalMapScale", "BumpScale", "Cutoff", "CutoutClip", "DetailBLineG", "DetailNormalMapScale", "DetailRLineR", "DstBlend", "EmissionPower", "GlossMapScale",
            "Glossiness", "GlossyReflections", "HairEffect", "LightCancel", "LineWidthS", "Metallic", "Mode", "OcclusionStrength", "Parallax", "SelectEmissionMap", "ShadowExtend",
            "ShadowExtendAnother", "Smoothness", "SmoothnessTextureChannel", "SpecularHeight", "SpecularPower", "SpecularPowerNail", "SrcBlend", "UPanner", "UVSec", "ZWrite", "alpha", "alpha_a",
            "alpha_b", "ambientshadowOFF", "linetexon", "linewidth", "liquidbbot", "liquidbtop", "liquidface", "liquidfbot", "liquidftop", "nip", "nipsize", "node_6948", "notusetexspecular",
            "patternclamp1", "patternclamp2", "patternclamp3", "patternrotator1", "patternrotator2", "patternrotator3", "refpower", "rimV", "rimpower"
        };
        public enum RendererProperties
        {
            Enabled, ShadowCastingMode, ReceiveShadows
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
