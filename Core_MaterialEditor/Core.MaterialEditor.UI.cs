using KK_Plugins.CommonCode;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UILib;
using UnityEngine;
using UnityEngine.UI;
#if KK || AI
using Studio;
#endif
#if AI
using AIChara;
#endif

namespace KK_Plugins
{
    public partial class MaterialEditor
    {
        private static Canvas UISystem;
        private static ScrollRect MaterialEditorWindow;

        public const string FileExt = ".png";
        public const string FileFilter = "Images (*.png;.jpg)|*.png;*.jpg|All files|*.*";
#if AI
        private static readonly HashSet<string> BodyParts = new HashSet<string> {
            "o_eyebase_L", "o_eyebase_R", "o_eyelashes", "o_eyeshadow", "o_head", "o_namida", "o_tang", "o_tooth", "o_body_cf", "o_mnpa", "o_mnpb", "cm_o_dan00", "o_tang",
            "cm_o_dan00", "o_tang", "o_silhouette_cf" };
#else
        private static readonly HashSet<string> BodyParts = new HashSet<string> {
            "cf_O_tooth", "cf_O_canine", "cf_O_tang", "o_tang", "n_tang", "n_tang_silhouette",  "cf_O_eyeline", "cf_O_eyeline_low", "cf_O_mayuge", "cf_Ohitomi_L", "cf_Ohitomi_R",
            "cf_Ohitomi_L02", "cf_Ohitomi_R02", "cf_O_noseline", "cf_O_namida_L", "cf_O_namida_M", "o_dankon", "o_gomu", "o_dan_f", "cf_O_namida_S", "cf_O_gag_eye_00", "cf_O_gag_eye_01",
            "cf_O_gag_eye_02", "o_shadowcaster", "o_shadowcaster_cm", "o_mnpa", "o_mnpb", "n_body_silhouette" };
#endif

        private void MakerAPI_MakerBaseLoaded(object s, RegisterCustomControlsEvent e)
        {
            InitUI();

            MakerAPI.AddAccessoryWindowControl(new MakerButton("Open Material Editor", null, this)).OnClick.AddListener(delegate { PopulateListAccessory(); });
#if AI
            if (AdvancedMode.Value)
            {
                e.AddControl(new MakerButton("Open Material Editor (Body)", MakerConstants.Body.All, this)).OnClick.AddListener(delegate { PopulateListBody(); });
                e.AddControl(new MakerButton("Open Material Editor (Face)", MakerConstants.Body.All, this)).OnClick.AddListener(delegate { PopulateListFace(); });
                e.AddControl(new MakerButton("Open Material Editor (All)", MakerConstants.Body.All, this)).OnClick.AddListener(delegate { PopulateListCharacter(); });
            }
#else
            if (AdvancedMode.Value)
            {
                e.AddControl(new MakerButton("Open Material Editor (Body)", MakerConstants.Face.All, this)).OnClick.AddListener(delegate { PopulateListBody(); });
                e.AddControl(new MakerButton("Open Material Editor (Face)", MakerConstants.Face.All, this)).OnClick.AddListener(delegate { PopulateListFace(); });
                e.AddControl(new MakerButton("Open Material Editor (All)", MakerConstants.Face.All, this)).OnClick.AddListener(delegate { PopulateListCharacter(); });
            }
#endif

#if !AI
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Top, this)).OnClick.AddListener(delegate { PopulateListClothes(0); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Bottom, this)).OnClick.AddListener(delegate { PopulateListClothes(1); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Bra, this)).OnClick.AddListener(delegate { PopulateListClothes(2); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Shorts, this)).OnClick.AddListener(delegate { PopulateListClothes(3); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Gloves, this)).OnClick.AddListener(delegate { PopulateListClothes(4); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Panst, this)).OnClick.AddListener(delegate { PopulateListClothes(5); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Socks, this)).OnClick.AddListener(delegate { PopulateListClothes(6); });
#if KK
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.InnerShoes, this)).OnClick.AddListener(delegate { PopulateListClothes(7); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.OuterShoes, this)).OnClick.AddListener(delegate { PopulateListClothes(8); });
#else
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Shoes, this)).OnClick.AddListener(delegate { PopulateListClothes(7); });
#endif
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Hair.Back, this)).OnClick.AddListener(delegate { PopulateListHair(0); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Hair.Front, this)).OnClick.AddListener(delegate { PopulateListHair(1); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Hair.Side, this)).OnClick.AddListener(delegate { PopulateListHair(2); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Hair.Extension, this)).OnClick.AddListener(delegate { PopulateListHair(3); });
#endif
        }

        private void MakerAPI_RegisterCustomSubCategories(object sender, RegisterSubCategoriesEvent e)
        {
#if AI
            MakerCategory hairCategory = new MakerCategory(MakerConstants.Hair.CategoryName, "ME", 0, "Material Editor");
            e.AddControl(new MakerButton("Open Material Editor (Back)", hairCategory, this)).OnClick.AddListener(delegate { PopulateListHair(0); });
            e.AddControl(new MakerButton("Open Material Editor (Front)", hairCategory, this)).OnClick.AddListener(delegate { PopulateListHair(1); });
            e.AddControl(new MakerButton("Open Material Editor (Side)", hairCategory, this)).OnClick.AddListener(delegate { PopulateListHair(2); });
            e.AddControl(new MakerButton("Open Material Editor (Extension)", hairCategory, this)).OnClick.AddListener(delegate { PopulateListHair(3); });
            e.AddSubCategory(hairCategory);

            MakerCategory clothesCategory = new MakerCategory(MakerConstants.Clothes.CategoryName, "ME", 0, "Material Editor");
            e.AddControl(new MakerButton("Open Material Editor (Top)", clothesCategory, this)).OnClick.AddListener(delegate { PopulateListClothes(0); });
            e.AddControl(new MakerButton("Open Material Editor (Bottom)", clothesCategory, this)).OnClick.AddListener(delegate { PopulateListClothes(1); });
            e.AddControl(new MakerButton("Open Material Editor (Bra)", clothesCategory, this)).OnClick.AddListener(delegate { PopulateListClothes(2); });
            e.AddControl(new MakerButton("Open Material Editor (Underwear)", clothesCategory, this)).OnClick.AddListener(delegate { PopulateListClothes(3); });
            e.AddControl(new MakerButton("Open Material Editor (Gloves)", clothesCategory, this)).OnClick.AddListener(delegate { PopulateListClothes(4); });
            e.AddControl(new MakerButton("Open Material Editor (Pantyhose)", clothesCategory, this)).OnClick.AddListener(delegate { PopulateListClothes(5); });
            e.AddControl(new MakerButton("Open Material Editor (Socks)", clothesCategory, this)).OnClick.AddListener(delegate { PopulateListClothes(6); });
            e.AddControl(new MakerButton("Open Material Editor (Shoes)", clothesCategory, this)).OnClick.AddListener(delegate { PopulateListClothes(7); });
            e.AddSubCategory(clothesCategory);
#endif
        }

        private void InitUI()
        {
            UIUtility.Init(nameof(KK_Plugins));

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

#if KK || AI
        private static void PopulateListStudio()
        {
            if (Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNodes.Length != 1)
                return;

            TreeNodeObject[] selectNodes = Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNodes;
            for (int i = 0; i < selectNodes.Length; i++)
                if (Singleton<Studio.Studio>.Instance.dicInfo.TryGetValue(selectNodes[i], out ObjectCtrlInfo objectCtrlInfo))
                    if (objectCtrlInfo is OCIItem ociItem)
                        PopulateList(ociItem?.objectItem, ObjectType.StudioItem, GetObjectID(objectCtrlInfo));
                    else if (objectCtrlInfo is OCIChar ociChar)
                        PopulateList(ociChar?.charInfo.gameObject, ObjectType.Character, GetObjectID(objectCtrlInfo), ociChar?.charInfo);
        }
#endif

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
            var chaAccessoryComponent = AccessoriesApi.GetAccessory(MakerAPI.GetCharacterControl(), AccessoriesApi.SelectedMakerAccSlot);
            PopulateList(chaAccessoryComponent?.gameObject, ObjectType.Accessory, 0, chaControl, coordinateIndex, AccessoriesApi.SelectedMakerAccSlot);
        }

        private static void PopulateListHair(int index)
        {
            var chaControl = MakerAPI.GetCharacterControl();
            PopulateList(chaControl.objHair[index], ObjectType.Hair, 0, chaControl, 0, index);
        }

        internal static void PopulateListBody()
        {
            var chaControl = MakerAPI.GetCharacterControl();
            PopulateList(chaControl.gameObject, ObjectType.Character, 0, chaControl, body: true);
        }

        internal static void PopulateListFace()
        {
            var chaControl = MakerAPI.GetCharacterControl();
            PopulateList(chaControl.gameObject, ObjectType.Character, 0, chaControl, face: true);
        }

        internal static void PopulateListCharacter()
        {
            var chaControl = MakerAPI.GetCharacterControl();
            PopulateList(chaControl.gameObject, ObjectType.Character, 0, chaControl);
        }

        private static void PopulateList(GameObject go, ObjectType objectType, int id = 0, ChaControl chaControl = null, int coordinateIndex = 0, int slot = 0, bool body = false, bool face = false)
        {
            UISystem.gameObject.SetActive(true);

            foreach (Transform child in MaterialEditorWindow.content)
                Destroy(child.gameObject);

            if (go == null)
                return;

            if (objectType == ObjectType.Hair || objectType == ObjectType.Character)
                coordinateIndex = 0;

            float labelWidth = 50f;
            float buttonWidth = 100f;
            float dropdownWidth = 100f;
            float textBoxWidth = 75f;
            float colorLabelWidth = 10f;
            float resetButtonWidth = 50f;
            float sliderWidth = 150f;
            RectOffset padding = new RectOffset(3, 3, 0, 1);

            List<Renderer> rendList = GetRendererList(go, objectType);
            List<string> mats = new List<string>();

            Dictionary<string, Material> matList = new Dictionary<string, Material>();
            if (body)
                matList[chaControl.customMatBody.NameFormatted()] = chaControl.customMatBody;
            if (face)
                matList[chaControl.customMatFace.NameFormatted()] = chaControl.customMatFace;

            if (!body && !face)
                foreach (var rend in rendList)
                {
                    foreach (var mat in rend.materials)
                        if (objectType == ObjectType.Character && !BodyParts.Contains(rend.NameFormatted()))
                            continue;
                        else
                            matList[mat.NameFormatted()] = mat;

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
                    labelRenderer2LE.preferredWidth = 200;
                    labelRenderer2LE.flexibleWidth = 0;

                    var exportUVButton = UIUtility.CreateButton("ExportUVButton", contentListHeader.transform, "Export UV Map");
                    exportUVButton.onClick.AddListener(() => { Export.ExportUVMaps(rend); });
                    var exportUVButtonLE = exportUVButton.gameObject.AddComponent<LayoutElement>();
                    exportUVButtonLE.preferredWidth = 110;
                    exportUVButtonLE.flexibleWidth = 0;

                    var exportMeshButton = UIUtility.CreateButton("ExportUVButton", contentListHeader.transform, "Export .obj");
                    exportMeshButton.onClick.AddListener(() => { Export.ExportObj(rend); });
                    var exportMeshButtonLE = exportMeshButton.gameObject.AddComponent<LayoutElement>();
                    exportMeshButtonLE.preferredWidth = 85;
                    exportMeshButtonLE.flexibleWidth = 0;

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

                    var dropdownEnabled = UIUtility.CreateDropdown("Enabled", contentItem1.transform);
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
                        SetRendererProperty(go, rend.NameFormatted(), RendererProperties.Enabled, value, objectType);
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
                        SetRendererProperty(go, rend.NameFormatted(), RendererProperties.Enabled, valueEnabledInitial ? 1 : 0, objectType);
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

                    var dropdownShadowCastingMode = UIUtility.CreateDropdown("ShadowCastingMode", contentItem2.transform);
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
                        SetRendererProperty(go, rend.NameFormatted(), RendererProperties.ShadowCastingMode, value, objectType);
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
                        SetRendererProperty(go, rend.NameFormatted(), RendererProperties.ShadowCastingMode, (int)valueShadowCastingModeInitial, objectType);
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

                    var dropdownReceiveShadows = UIUtility.CreateDropdown("ReceiveShadows", contentItem3.transform);
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
                        SetRendererProperty(go, rend.NameFormatted(), RendererProperties.ReceiveShadows, value, objectType);
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
                        SetRendererProperty(go, rend.NameFormatted(), RendererProperties.ReceiveShadows, valueReceiveShadowsInitial ? 1 : 0, objectType);
                        dropdownReceiveShadows.value = valueReceiveShadowsInitial ? 1 : 0;
                    });
                    var resetReceiveShadowsLE = resetReceiveShadows.gameObject.AddComponent<LayoutElement>();
                    resetReceiveShadowsLE.preferredWidth = resetButtonWidth;
                    resetReceiveShadowsLE.flexibleWidth = 0;
                }

            foreach (var mat in matList.Values)
            {
                string materialName = mat.NameFormatted();
                string shaderName = mat.shader.NameFormatted();

                var contentListHeader1 = UIUtility.CreatePanel("ContentList", MaterialEditorWindow.content.transform);
                contentListHeader1.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
                contentListHeader1.gameObject.AddComponent<Mask>();
                contentListHeader1.gameObject.AddComponent<HorizontalLayoutGroup>().padding = padding;

                var labelMat = UIUtility.CreateText(materialName, contentListHeader1.transform, "Material:");
                labelMat.alignment = TextAnchor.MiddleLeft;
                labelMat.color = Color.black;
                var labelMat2 = UIUtility.CreateText(materialName, contentListHeader1.transform, materialName);
                labelMat2.alignment = TextAnchor.MiddleRight;
                labelMat2.color = Color.black;

                var contentListHeader2 = UIUtility.CreatePanel("ContentList", MaterialEditorWindow.content.transform);
                contentListHeader2.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
                contentListHeader2.gameObject.AddComponent<Mask>();
                contentListHeader2.gameObject.AddComponent<HorizontalLayoutGroup>().padding = padding;

                var labelShader = UIUtility.CreateText(mat.shader.NameFormatted(), contentListHeader2.transform, "Shader:");
                labelShader.alignment = TextAnchor.MiddleLeft;
                labelShader.color = Color.black;
                var labelShaderLE = labelShader.gameObject.AddComponent<LayoutElement>();
                labelShaderLE.preferredWidth = labelWidth;
                labelShaderLE.flexibleWidth = labelWidth;

                if (XMLShaderProperties.Count == 0)
                {
                    var labelShader2 = UIUtility.CreateText(mat.shader.NameFormatted(), contentListHeader2.transform, shaderName);
                    labelShader2.alignment = TextAnchor.MiddleRight;
                    labelShader2.color = Color.black;
                }
                else
                {
                    string shaderNameInitial = shaderName;
                    if (objectType == ObjectType.Other) { }
                    if (objectType == ObjectType.StudioItem)
                    {
                        shaderNameInitial = GetSceneController().GetMaterialShaderValue(id, materialName)?.ShaderNameOriginal;
                        if (shaderNameInitial.IsNullOrEmpty())
                            shaderNameInitial = shaderName;
                    }
                    else
                    {
                        shaderNameInitial = GetCharaController(chaControl).GetMaterialShaderValue(objectType, coordinateIndex, slot, materialName)?.ShaderNameOriginal;
                        if (shaderNameInitial.IsNullOrEmpty())
                            shaderNameInitial = shaderName;
                    }

                    var dropdownShader = UIUtility.CreateDropdown("Shader", contentListHeader2.transform);
                    dropdownShader.transform.SetRect(0f, 0f, 0f, 1f, 0f, 0f, 100f);
                    dropdownShader.captionText.transform.SetRect(0f, 0f, 1f, 1f, 0f, 2f, -15f, -2f);
                    dropdownShader.captionText.alignment = TextAnchor.MiddleLeft;
                    dropdownShader.options.Clear();
                    dropdownShader.options.Add(new Dropdown.OptionData(shaderNameInitial));
                    foreach (var shader in XMLShaderProperties.Where(x => x.Key != "default" && x.Key != shaderNameInitial))
                    {
                        dropdownShader.options.Add(new Dropdown.OptionData(shader.Key));
                    }
                    dropdownShader.value = ShaderSelectedIndex();
                    dropdownShader.captionText.text = shaderName;
                    dropdownShader.onValueChanged.AddListener((value) =>
                    {
                        if (value == 0)
                        {
                            if (objectType == ObjectType.Other) { }
                            else if (objectType == ObjectType.StudioItem)
                                GetSceneController().RemoveMaterialShaderName(id, materialName);
                            else
                                GetCharaController(chaControl).RemoveMaterialShaderName(objectType, coordinateIndex, slot, materialName);

                            if (XMLShaderProperties.ContainsKey(shaderNameInitial))
                            {
                                if (objectType == ObjectType.Character)
                                {
                                    if (SetShader(chaControl, materialName, shaderNameInitial))
                                        PopulateList(go, objectType, id, chaControl, coordinateIndex, slot, body: body, face: face);
                                }
                                else if (SetShader(go, materialName, shaderNameInitial, objectType))
                                    PopulateList(go, objectType, id, chaControl, coordinateIndex, slot, body: body, face: face);
                            }
                            else
                                Logger.LogMessage("Save and reload to refresh shader.");
                        }
                        else
                        {
                            int counter = 0;
                            foreach (var shader in XMLShaderProperties.Where(x => x.Key != "default" && x.Key != shaderNameInitial))
                            {
                                counter++;
                                if (counter == value)
                                {
                                    if (objectType == ObjectType.Other) { }
                                    else if (objectType == ObjectType.StudioItem)
                                    {
                                        GetSceneController().AddMaterialShader(id, materialName, shader.Key, shaderNameInitial);
                                        GetSceneController().RemoveMaterialShaderRenderQueue(id, materialName);
                                    }
                                    else
                                    {
                                        GetCharaController(chaControl).AddMaterialShader(objectType, coordinateIndex, slot, materialName, shader.Key, shaderNameInitial);
                                        GetCharaController(chaControl).RemoveMaterialShaderRenderQueue(objectType, coordinateIndex, slot, materialName);
                                    }

                                    if (objectType == ObjectType.Character)
                                    {
                                        if (SetShader(chaControl, materialName, shader.Key))
                                            PopulateList(go, objectType, id, chaControl, coordinateIndex, slot, body: body, face: face);

                                    }
                                    else if (SetShader(go, materialName, shader.Key, objectType))
                                        PopulateList(go, objectType, id, chaControl, coordinateIndex, slot, body: body, face: face);
                                }
                            }
                        }
                    });
                    var dropdownShaderLE = dropdownShader.gameObject.AddComponent<LayoutElement>();
                    dropdownShaderLE.preferredWidth = dropdownWidth * 3;
                    dropdownShaderLE.flexibleWidth = 0;

                    int ShaderSelectedIndex()
                    {
                        string currentShaderName = mat.shader.NameFormatted();
                        if (currentShaderName == shaderNameInitial)
                            return 0;

                        int counter = 0;
                        foreach (var shader in XMLShaderProperties.Where(x => x.Key != "default" && x.Key != shaderNameInitial))
                        {
                            counter++;
                            if (currentShaderName == shader.Key)
                                return counter;
                        }
                        return 0;
                    }

                    var resetShader = UIUtility.CreateButton("ResetShader", contentListHeader2.transform, "Reset");
                    resetShader.onClick.AddListener(() =>
                    {
                        if (objectType == ObjectType.Other) { }
                        else if (objectType == ObjectType.StudioItem)
                            GetSceneController().RemoveMaterialShaderName(id, materialName);
                        else
                            GetCharaController(chaControl).RemoveMaterialShaderName(objectType, coordinateIndex, slot, materialName);

                        if (XMLShaderProperties.ContainsKey(shaderNameInitial))
                        {
                            if (objectType == ObjectType.Character)
                            {
                                if (SetShader(chaControl, materialName, shaderNameInitial))
                                    PopulateList(go, objectType, id, chaControl, coordinateIndex, slot, body: body, face: face);
                            }
                            else if (SetShader(go, materialName, shaderNameInitial, objectType))
                                PopulateList(go, objectType, id, chaControl, coordinateIndex, slot, body: body, face: face);
                        }
                        else
                            Logger.LogMessage("Save and reload to refresh shader.");
                    });
                    var resetShaderLE = resetShader.gameObject.AddComponent<LayoutElement>();
                    resetShaderLE.preferredWidth = resetButtonWidth;
                    resetShaderLE.flexibleWidth = 0;

                    var contentListHeader3 = UIUtility.CreatePanel("ContentList", MaterialEditorWindow.content.transform);
                    contentListHeader3.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
                    contentListHeader3.gameObject.AddComponent<Mask>();
                    contentListHeader3.gameObject.AddComponent<HorizontalLayoutGroup>().padding = padding;

                    var labelShaderRenderQueue = UIUtility.CreateText("ShaderRenderQueue", contentListHeader3.transform, "Render Queue:");
                    labelShaderRenderQueue.alignment = TextAnchor.MiddleLeft;
                    labelShaderRenderQueue.color = Color.black;
                    var labelShaderRenderQueueLE = labelShaderRenderQueue.gameObject.AddComponent<LayoutElement>();
                    labelShaderRenderQueueLE.preferredWidth = labelWidth;
                    labelShaderRenderQueueLE.flexibleWidth = labelWidth;

                    int renderQueue = mat.renderQueue;
                    int renderQueueOriginal = mat.renderQueue;
                    if (objectType == ObjectType.Other) { }
                    if (objectType == ObjectType.StudioItem)
                    {
                        int? renderQueueOriginalTemp = GetSceneController().GetMaterialShaderValue(id, materialName)?.RenderQueueOriginal;
                        renderQueueOriginal = renderQueueOriginalTemp == null ? renderQueue : (int)renderQueueOriginalTemp;
                    }
                    else
                    {
                        int? renderQueueOriginalTemp = GetCharaController(chaControl).GetMaterialShaderValue(objectType, coordinateIndex, slot, materialName)?.RenderQueueOriginal;
                        renderQueueOriginal = renderQueueOriginalTemp == null ? renderQueue : (int)renderQueueOriginalTemp;
                    }

                    var textBoxShaderRenderQueue = UIUtility.CreateInputField("ShaderRenderQueue", contentListHeader3.transform);
                    textBoxShaderRenderQueue.text = renderQueue.ToString();
                    textBoxShaderRenderQueue.onEndEdit.AddListener((value) =>
                    {
                        if (!int.TryParse(value, out int intValue))
                            return;

                        if (objectType == ObjectType.Other) { }
                        else if (objectType == ObjectType.StudioItem)
                            GetSceneController().AddMaterialShader(id, materialName, intValue, renderQueueOriginal);
                        else
                            GetCharaController(chaControl).AddMaterialShader(objectType, coordinateIndex, slot, materialName, intValue, renderQueueOriginal);

                        if (objectType == ObjectType.Character)
                            SetRenderQueue(chaControl, materialName, intValue);
                        else
                            SetRenderQueue(go, materialName, intValue, objectType);
                    });
                    var textBoxShaderRenderQueueLE = textBoxShaderRenderQueue.gameObject.AddComponent<LayoutElement>();
                    textBoxShaderRenderQueueLE.preferredWidth = textBoxWidth;
                    textBoxShaderRenderQueueLE.flexibleWidth = 0;

                    var resetShaderRenderQueue = UIUtility.CreateButton($"ResetRenderQueue", contentListHeader3.transform, "Reset");
                    resetShaderRenderQueue.onClick.AddListener(() =>
                    {
                        if (objectType == ObjectType.Other) { }
                        else if (objectType == ObjectType.StudioItem)
                            GetSceneController().RemoveMaterialShaderRenderQueue(id, materialName);
                        else
                            GetCharaController(chaControl).RemoveMaterialShaderRenderQueue(objectType, coordinateIndex, slot, materialName);

                        if (objectType == ObjectType.Character)
                            SetRenderQueue(chaControl, materialName, renderQueueOriginal);
                        else
                            SetRenderQueue(go, materialName, renderQueueOriginal, objectType);
                        textBoxShaderRenderQueue.text = renderQueueOriginal.ToString();
                    });
                    var resetShaderRenderQueueLE = resetShaderRenderQueue.gameObject.AddComponent<LayoutElement>();
                    resetShaderRenderQueueLE.preferredWidth = resetButtonWidth;
                    resetShaderRenderQueueLE.flexibleWidth = 0;
                }

                foreach (var property in XMLShaderProperties[XMLShaderProperties.ContainsKey(shaderName) ? shaderName : "default"].OrderBy(x => x.Value.Type).ThenBy(x => x.Key))
                {
                    string propertyName = property.Key;
                    if (CheckBlacklist(objectType, propertyName)) continue;

                    if (property.Value.Type == ShaderPropertyType.Color)
                    {
                        if (mat.HasProperty($"_{propertyName}"))
                        {
                            var contentList = UIUtility.CreatePanel("ContentList", MaterialEditorWindow.content.transform);
                            contentList.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
                            contentList.gameObject.AddComponent<Mask>();
                            contentList.gameObject.AddComponent<HorizontalLayoutGroup>().padding = padding;

                            var label = UIUtility.CreateText(propertyName, contentList.transform, $"{propertyName}:");
                            label.alignment = TextAnchor.MiddleLeft;
                            label.color = Color.black;
                            var labelLE = label.gameObject.AddComponent<LayoutElement>();
                            labelLE.preferredWidth = labelWidth;
                            labelLE.flexibleWidth = labelWidth;

                            Color valueColor = mat.GetColor($"_{propertyName}");
                            Color valueColorInitial = valueColor;
                            if (objectType == ObjectType.Other) { }
                            if (objectType == ObjectType.StudioItem)
                            {
                                Color c = GetSceneController().GetMaterialColorPropertyValueOriginal(id, materialName, propertyName);
                                if (c.r != -1 && c.g != -1 && c.b != -1 && c.a != -1)
                                    valueColorInitial = c;
                            }
                            else
                            {
                                Color c = GetCharaController(chaControl).GetMaterialColorPropertyValueOriginal(objectType, coordinateIndex, slot, materialName, propertyName);
                                if (c.r != -1 && c.g != -1 && c.b != -1 && c.a != -1)
                                    valueColorInitial = c;
                            }


                            var labelR = UIUtility.CreateText("R", contentList.transform, "R");
                            labelR.alignment = TextAnchor.MiddleLeft;
                            labelR.color = Color.black;
                            var labelRLE = labelR.gameObject.AddComponent<LayoutElement>();
                            labelRLE.preferredWidth = colorLabelWidth;
                            labelRLE.flexibleWidth = 0;

                            var textBoxR = UIUtility.CreateInputField(propertyName, contentList.transform);
                            textBoxR.text = valueColor.r.ToString();
                            textBoxR.onEndEdit.AddListener((value) =>
                            {
                                Color colorOrig = mat.GetColor($"_{propertyName}");
                                Color colorNew = new Color(float.Parse(value), colorOrig.g, colorOrig.b, colorOrig.a);

                                if (objectType == ObjectType.Other) { }
                                else if (objectType == ObjectType.StudioItem)
                                    GetSceneController().AddMaterialColorProperty(id, materialName, propertyName, colorNew, valueColorInitial);
                                else
                                    GetCharaController(chaControl).AddMaterialColorProperty(objectType, coordinateIndex, slot, materialName, propertyName, colorNew, valueColorInitial);

                                if (objectType == ObjectType.Character)
                                    SetColorProperty(chaControl, materialName, propertyName, colorNew);
                                else
                                    SetColorProperty(go, materialName, propertyName, colorNew, objectType);
                            });
                            var textBoxRLE = textBoxR.gameObject.AddComponent<LayoutElement>();
                            textBoxRLE.preferredWidth = textBoxWidth;
                            textBoxRLE.flexibleWidth = 0;

                            var labelG = UIUtility.CreateText("G", contentList.transform, "G");
                            labelG.alignment = TextAnchor.MiddleLeft;
                            labelG.color = Color.black;
                            var labelGLE = labelG.gameObject.AddComponent<LayoutElement>();
                            labelGLE.preferredWidth = colorLabelWidth;
                            labelGLE.flexibleWidth = 0;

                            var textBoxG = UIUtility.CreateInputField(propertyName, contentList.transform);
                            textBoxG.text = valueColor.g.ToString();
                            textBoxG.onEndEdit.AddListener((value) =>
                            {
                                Color colorOrig = mat.GetColor($"_{propertyName}");
                                Color colorNew = new Color(colorOrig.r, float.Parse(value), colorOrig.b, colorOrig.a);

                                if (objectType == ObjectType.Other) { }
                                else if (objectType == ObjectType.StudioItem)
                                    GetSceneController().AddMaterialColorProperty(id, materialName, propertyName, colorNew, valueColorInitial);
                                else
                                    GetCharaController(chaControl).AddMaterialColorProperty(objectType, coordinateIndex, slot, materialName, propertyName, colorNew, valueColorInitial);

                                if (objectType == ObjectType.Character)
                                    SetColorProperty(chaControl, materialName, propertyName, colorNew);
                                else
                                    SetColorProperty(go, materialName, propertyName, colorNew, objectType);
                            });
                            var textBoxGLE = textBoxG.gameObject.AddComponent<LayoutElement>();
                            textBoxGLE.preferredWidth = textBoxWidth;
                            textBoxGLE.flexibleWidth = 0;

                            var labelB = UIUtility.CreateText("B", contentList.transform, "B");
                            labelB.alignment = TextAnchor.MiddleLeft;
                            labelB.color = Color.black;
                            var labelBLE = labelB.gameObject.AddComponent<LayoutElement>();
                            labelBLE.preferredWidth = colorLabelWidth;
                            labelBLE.flexibleWidth = 0;

                            var textBoxB = UIUtility.CreateInputField(propertyName, contentList.transform);
                            textBoxB.text = valueColor.b.ToString();
                            textBoxB.onEndEdit.AddListener((value) =>
                            {
                                Color colorOrig = mat.GetColor($"_{propertyName}");
                                Color colorNew = new Color(colorOrig.r, colorOrig.g, float.Parse(value), colorOrig.a);

                                if (objectType == ObjectType.Other) { }
                                else if (objectType == ObjectType.StudioItem)
                                    GetSceneController().AddMaterialColorProperty(id, materialName, propertyName, colorNew, valueColorInitial);
                                else
                                    GetCharaController(chaControl).AddMaterialColorProperty(objectType, coordinateIndex, slot, materialName, propertyName, colorNew, valueColorInitial);

                                if (objectType == ObjectType.Character)
                                    SetColorProperty(chaControl, materialName, propertyName, colorNew);
                                else
                                    SetColorProperty(go, materialName, propertyName, colorNew, objectType);
                            });

                            var textBoxBLE = textBoxB.gameObject.AddComponent<LayoutElement>();
                            textBoxBLE.preferredWidth = textBoxWidth;
                            textBoxBLE.flexibleWidth = 0;

                            var labelA = UIUtility.CreateText("A", contentList.transform, "A");
                            labelA.alignment = TextAnchor.MiddleLeft;
                            labelA.color = Color.black;
                            var labelALE = labelA.gameObject.AddComponent<LayoutElement>();
                            labelALE.preferredWidth = colorLabelWidth;
                            labelALE.flexibleWidth = 0;

                            var textBoxA = UIUtility.CreateInputField(propertyName, contentList.transform);
                            textBoxA.text = valueColor.a.ToString();
                            textBoxA.onEndEdit.AddListener((value) =>
                            {
                                Color colorOrig = mat.GetColor($"_{propertyName}");
                                Color colorNew = new Color(colorOrig.r, colorOrig.g, colorOrig.b, float.Parse(value));

                                if (objectType == ObjectType.Other) { }
                                else if (objectType == ObjectType.StudioItem)
                                    GetSceneController().AddMaterialColorProperty(id, materialName, propertyName, colorNew, valueColorInitial);
                                else
                                    GetCharaController(chaControl).AddMaterialColorProperty(objectType, coordinateIndex, slot, materialName, propertyName, colorNew, valueColorInitial);

                                if (objectType == ObjectType.Character)
                                    SetColorProperty(chaControl, materialName, propertyName, colorNew);
                                else
                                    SetColorProperty(go, materialName, propertyName, colorNew, objectType);
                            });

                            var textBoxALE = textBoxA.gameObject.AddComponent<LayoutElement>();
                            textBoxALE.preferredWidth = textBoxWidth;
                            textBoxALE.flexibleWidth = 0;

                            var resetColor = UIUtility.CreateButton($"Reset{propertyName}", contentList.transform, "Reset");
                            resetColor.onClick.AddListener(() =>
                            {
                                if (objectType == ObjectType.Other) { }
                                else if (objectType == ObjectType.StudioItem)
                                    GetSceneController().RemoveMaterialColorProperty(id, materialName, propertyName);
                                else
                                    GetCharaController(chaControl).RemoveMaterialColorProperty(objectType, coordinateIndex, slot, materialName, propertyName);
                                if (objectType == ObjectType.Character)
                                    SetColorProperty(chaControl, materialName, propertyName, valueColorInitial);
                                else
                                    SetColorProperty(go, materialName, propertyName, valueColorInitial, objectType);
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
                    if (property.Value.Type == ShaderPropertyType.Texture)
                    {
                        if (mat.HasProperty($"_{propertyName}"))
                        {
                            var contentList = UIUtility.CreatePanel("ContentList", MaterialEditorWindow.content.transform);
                            contentList.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
                            contentList.gameObject.AddComponent<Mask>();
                            contentList.gameObject.AddComponent<HorizontalLayoutGroup>().padding = padding;

                            var label = UIUtility.CreateText(propertyName, contentList.transform, $"{propertyName}:");
                            label.alignment = TextAnchor.MiddleLeft;
                            label.color = Color.black;
                            var labelLE = label.gameObject.AddComponent<LayoutElement>();
                            labelLE.preferredWidth = labelWidth;
                            labelLE.flexibleWidth = labelWidth;

                            var texture = mat.GetTexture($"_{propertyName}");

                            if (texture == null)
                            {
                                var labelNoTexture = UIUtility.CreateText($"NoTexture{propertyName}", contentList.transform, "No Texture");
                                labelNoTexture.alignment = TextAnchor.MiddleCenter;
                                labelNoTexture.color = Color.black;
                                var labelNoTextureLE = labelNoTexture.gameObject.AddComponent<LayoutElement>();
                                labelNoTextureLE.preferredWidth = buttonWidth;
                                labelNoTextureLE.flexibleWidth = 0;
                            }
                            else
                            {
                                var exportButton = UIUtility.CreateButton($"ExportTexture{propertyName}", contentList.transform, $"Export Texture");
                                exportButton.onClick.AddListener(() => ExportTexture(mat, propertyName));
                                var exportButtonLE = exportButton.gameObject.AddComponent<LayoutElement>();
                                exportButtonLE.preferredWidth = buttonWidth;
                                exportButtonLE.flexibleWidth = 0;
                            }

                            var importButton = UIUtility.CreateButton($"ImportTexture{propertyName}", contentList.transform, $"Import Texture");
                            importButton.onClick.AddListener(() =>
                            {
                                if (objectType == ObjectType.Other) { }
                                else if (objectType == ObjectType.StudioItem)
                                    GetSceneController().AddMaterialTextureProperty(id, materialName, propertyName, go);
                                else
                                    GetCharaController(chaControl).AddMaterialTextureProperty(objectType, coordinateIndex, slot, materialName, propertyName, go);
                            });
                            var importButtonLE = importButton.gameObject.AddComponent<LayoutElement>();
                            importButtonLE.preferredWidth = buttonWidth;
                            importButtonLE.flexibleWidth = 0;

                            var resetTexture = UIUtility.CreateButton($"Reset{propertyName}", contentList.transform, "Reset");
                            resetTexture.onClick.AddListener(() =>
                            {
                                if (objectType == ObjectType.Other) { }
                                else if (objectType == ObjectType.StudioItem)
                                    GetSceneController().RemoveMaterialTextureProperty(id, materialName, propertyName);
                                else
                                    GetCharaController(chaControl).RemoveMaterialTextureProperty(objectType, coordinateIndex, slot, materialName, propertyName);
                            });
                            var reseTextureLE = resetTexture.gameObject.AddComponent<LayoutElement>();
                            reseTextureLE.preferredWidth = resetButtonWidth;
                            reseTextureLE.flexibleWidth = 0;
                        }
                    }
                    if (property.Value.Type == ShaderPropertyType.Float)
                    {
                        if (mat.HasProperty($"_{propertyName}"))
                        {
                            var contentList = UIUtility.CreatePanel("ContentList", MaterialEditorWindow.content.transform);
                            contentList.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
                            contentList.gameObject.AddComponent<Mask>();
                            contentList.gameObject.AddComponent<HorizontalLayoutGroup>().padding = padding;

                            var label = UIUtility.CreateText(propertyName, contentList.transform, $"{propertyName}:");
                            label.alignment = TextAnchor.MiddleLeft;
                            label.color = Color.black;
                            var labelLE = label.gameObject.AddComponent<LayoutElement>();
                            labelLE.preferredWidth = labelWidth;
                            labelLE.flexibleWidth = labelWidth;

                            string valueFloat = mat.GetFloat($"_{propertyName}").ToString();
                            string valueFloatInitial = valueFloat;
                            if (objectType == ObjectType.Other) { }
                            else if (objectType == ObjectType.StudioItem)
                            {
                                if (GetSceneController().GetMaterialFloatPropertyValueOriginal(id, materialName, propertyName) != null)
                                    valueFloatInitial = GetSceneController().GetMaterialFloatPropertyValueOriginal(id, materialName, propertyName);
                            }
                            else if (GetCharaController(chaControl).GetMaterialFloatPropertyValueOriginal(objectType, coordinateIndex, slot, materialName, propertyName) != null)
                                valueFloatInitial = GetCharaController(chaControl).GetMaterialFloatPropertyValueOriginal(objectType, coordinateIndex, slot, materialName, propertyName);
                            float.TryParse(valueFloat, out float valueFloatF);
                            bool doSlider = property.Value.MinValue != null && property.Value.MaxValue != null;

                            Slider sliderFloat = null;
                            if (doSlider)
                            {
                                sliderFloat = UIUtility.CreateSlider("Slider" + propertyName, contentList.transform);
                                var sliderFloatLE = sliderFloat.gameObject.AddComponent<LayoutElement>();
                                sliderFloatLE.preferredWidth = sliderWidth;
                                sliderFloatLE.flexibleWidth = 0;
                            }
                            else
                            {
                                var placeholderPanel = UIUtility.CreatePanel(propertyName, contentList.transform);
                                placeholderPanel.enabled = false;
                                var placeholderPanelLE = placeholderPanel.gameObject.AddComponent<LayoutElement>();
                                placeholderPanelLE.preferredWidth = sliderWidth;
                                placeholderPanelLE.flexibleWidth = 0;
                            }

                            var textBoxFloat = UIUtility.CreateInputField(propertyName, contentList.transform);
                            textBoxFloat.text = valueFloat;
                            textBoxFloat.onEndEdit.AddListener((value) =>
                            {
                                if (objectType == ObjectType.Other) { }
                                else if (objectType == ObjectType.StudioItem)
                                    GetSceneController().AddMaterialFloatProperty(id, materialName, propertyName, value, valueFloatInitial);
                                else
                                    GetCharaController(chaControl).AddMaterialFloatProperty(objectType, coordinateIndex, slot, materialName, propertyName, value, valueFloatInitial);

                                if (objectType == ObjectType.Character)
                                    SetFloatProperty(chaControl, materialName, propertyName, value);
                                else
                                    SetFloatProperty(go, materialName, propertyName, value, objectType);

                                if (doSlider && float.TryParse(value, out float valueF) && valueF <= sliderFloat.maxValue && valueF >= sliderFloat.minValue)
                                    sliderFloat.value = valueF;
                            });
                            var textBoxFloatLE = textBoxFloat.gameObject.AddComponent<LayoutElement>();
                            textBoxFloatLE.preferredWidth = textBoxWidth;
                            textBoxFloatLE.flexibleWidth = 0;

                            if (doSlider)
                            {
                                sliderFloat.minValue = (float)property.Value.MinValue;
                                sliderFloat.maxValue = (float)property.Value.MaxValue;
                                sliderFloat.value = valueFloatF;
                                sliderFloat.onValueChanged.AddListener((value) =>
                                {
                                    textBoxFloat.text = value.ToString();
                                    textBoxFloat.onEndEdit.Invoke(value.ToString());
                                });
                            }

                            var resetFloat = UIUtility.CreateButton($"Reset{propertyName}", contentList.transform, "Reset");
                            resetFloat.onClick.AddListener(() =>
                            {
                                if (objectType == ObjectType.Other) { }
                                else if (objectType == ObjectType.StudioItem)
                                    GetSceneController().RemoveMaterialFloatProperty(id, materialName, propertyName);
                                else
                                    GetCharaController(chaControl).RemoveMaterialFloatProperty(objectType, coordinateIndex, slot, materialName, propertyName);

                                if (objectType == ObjectType.Character)
                                    SetFloatProperty(chaControl, materialName, propertyName, valueFloatInitial);
                                else
                                    SetFloatProperty(go, materialName, propertyName, valueFloatInitial, objectType);
                                textBoxFloat.text = valueFloatInitial;
                                if (doSlider && float.TryParse(valueFloatInitial, out float valueFloatInitialF))
                                    sliderFloat.value = valueFloatInitialF;
                            });
                            var resetEnabledLE = resetFloat.gameObject.AddComponent<LayoutElement>();
                            resetEnabledLE.preferredWidth = resetButtonWidth;
                            resetEnabledLE.flexibleWidth = 0;
                        }
                    }
                }
            }
        }
#if KK || AI
        private void InitStudioUI(string sceneName)
        {
            if (sceneName != "Studio")
                return;

            InitUI();

            RectTransform original = GameObject.Find("StudioScene").transform.Find("Canvas Object List/Image Bar/Button Route").GetComponent<RectTransform>();
            Button materialEditorButton = Instantiate(original.gameObject).GetComponent<Button>();
            RectTransform materialEditorButtonRectTransform = materialEditorButton.transform as RectTransform;
            materialEditorButton.transform.SetParent(original.parent, true);
            materialEditorButton.transform.localScale = original.localScale;
            materialEditorButtonRectTransform.SetRect(original.anchorMin, original.anchorMax, original.offsetMin, original.offsetMax);
            materialEditorButtonRectTransform.anchoredPosition = original.anchoredPosition + new Vector2(-48f, 0f);

            Texture2D texture2D = new Texture2D(32, 32);
            texture2D.LoadImage(LoadIcon());
            var MatEditorIcon = materialEditorButton.targetGraphic as Image;
            MatEditorIcon.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, 32, 32), new Vector2(16, 16));
            MatEditorIcon.color = Color.white;

            materialEditorButton.onClick = new Button.ButtonClickedEvent();
            materialEditorButton.onClick.AddListener(() => { PopulateListStudio(); });
        }
#endif
        private static void ExportTexture(Material mat, string property)
        {
            var tex = mat.GetTexture($"_{property}");
            string filename = Path.Combine(ExportPath, $"_Export_{System.DateTime.Now:yyyy-MM-dd-HH-mm-ss}_{mat.NameFormatted()}_{property}.png");
            SaveTex(tex, filename);
            Logger.LogInfo($"Exported {filename}");
            CC.OpenFileInExplorer(filename);
        }

        internal byte[] LoadIcon()
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{nameof(KK_Plugins)}.Resources.MaterialEditorIcon.png"))
            {
                byte[] bytesInStream = new byte[stream.Length];
                stream.Read(bytesInStream, 0, bytesInStream.Length);
                return bytesInStream;
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

        public static HashSet<string> CharacterBlacklist = new HashSet<string>()
        {
            "alpha_a", "alpha_b"
        };

        public static bool CheckBlacklist(ObjectType objectType, string propertyName)
        {
            if (objectType == ObjectType.Clothing && ClothesBlacklist.Contains(propertyName))
                return true;
            if (objectType == ObjectType.Accessory && AccessoryBlacklist.Contains(propertyName))
                return true;
            if (objectType == ObjectType.Hair && HairBlacklist.Contains(propertyName))
                return true;
            if (objectType == ObjectType.Character && CharacterBlacklist.Contains(propertyName))
                return true;
            return false;
        }

        public enum RendererProperties { Enabled, ShadowCastingMode, ReceiveShadows }

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
