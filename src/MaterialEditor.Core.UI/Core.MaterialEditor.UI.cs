using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using KKAPI.Maker;
using KKAPI.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UILib;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using static KK_Plugins.MaterialEditor.MaterialAPI;
#if AI
using AIChara;
#endif

namespace KK_Plugins.MaterialEditor
{
    public abstract class UI : BaseUnityPlugin
    {
        private static Canvas MaterialEditorWindow;
        private static Image MaterialEditorMainPanel;
        private static ScrollRect MaterialEditorScrollableUI;
        internal static new ManualLogSource Logger;

        private static FileSystemWatcher TexChangeWatcher;

        private const float marginSize = 5f;
        private const float headerSize = 20f;
        private const float scrollOffsetX = -15f;
        private const float labelWidth = 50f;
        private const float buttonWidth = 100f;
        private const float dropdownWidth = 100f;
        private const float textBoxWidth = 75f;
        private const float colorLabelWidth = 10f;
        private const float resetButtonWidth = 50f;
        private const float sliderWidth = 150f;
        private const float labelXWidth = 60f;
        private const float labelYWidth = 10f;
        private const float textBoxXYWidth = 50f;
        private static readonly Color evenRowColor = new Color(1f, 1f, 1f, 1f);
        private static readonly Color oddRowColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        private static readonly RectOffset padding = new RectOffset(3, 3, 0, 1);

        public static ConfigEntry<float> UIScale { get; private set; }
        public static ConfigEntry<float> UIWidth { get; private set; }
        public static ConfigEntry<float> UIHeight { get; private set; }
        public static ConfigEntry<bool> WatchTexChanges { get; private set; }

        internal void Main()
        {
            Logger = base.Logger;

            UIScale = Config.Bind("Config", "UI Scale", 1.75f, new ConfigDescription("Controls the size of the window.", new AcceptableValueRange<float>(1f, 3f), new ConfigurationManagerAttributes { Order = 13 }));
            UIWidth = Config.Bind("Config", "UI Width", 0.3f, new ConfigDescription("Controls the size of the window.", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 12, ShowRangeAsPercent = false }));
            UIHeight = Config.Bind("Config", "UI Height", 0.3f, new ConfigDescription("Controls the size of the window.", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 11, ShowRangeAsPercent = false }));
            WatchTexChanges = Config.Bind("Config", "Watch File Changes", true, new ConfigDescription("Watch for file changes and reload textures on change. Can be toggled in the UI."));

            UIScale.SettingChanged += UISettingChanged;
            UIWidth.SettingChanged += UISettingChanged;
            UIHeight.SettingChanged += UISettingChanged;
        }

        protected void InitUI()
        {
            UIUtility.Init(nameof(KK_Plugins));

            MaterialEditorWindow = UIUtility.CreateNewUISystem("MaterialEditorCanvas");
            MaterialEditorWindow.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f / UIScale.Value, 1080f / UIScale.Value);
            HideUI();
            MaterialEditorWindow.gameObject.transform.SetParent(transform);
            MaterialEditorWindow.sortingOrder = 1000;

            MaterialEditorMainPanel = UIUtility.CreatePanel("Panel", MaterialEditorWindow.transform);
            MaterialEditorMainPanel.color = Color.white;
            MaterialEditorMainPanel.transform.SetRect(0.05f, 0.05f, UIWidth.Value * UIScale.Value, UIHeight.Value * UIScale.Value);

            UIUtility.AddOutlineToObject(MaterialEditorMainPanel.transform, Color.black);

            var drag = UIUtility.CreatePanel("Draggable", MaterialEditorMainPanel.transform);
            drag.transform.SetRect(0f, 1f, 1f, 1f, 0f, -headerSize);
            drag.color = Color.gray;
            UIUtility.MakeObjectDraggable(drag.rectTransform, MaterialEditorMainPanel.rectTransform);

            var nametext = UIUtility.CreateText("Nametext", drag.transform, "Material Editor");
            nametext.transform.SetRect(0f, 0f, 1f, 1f, 0f, 0f, 0f);
            nametext.alignment = TextAnchor.MiddleCenter;

            var fileWatcher = UIUtility.CreateToggle("Filewatcher", drag.transform, "");
            fileWatcher.transform.SetRect(0f, 0f, 0f, 1f);
            fileWatcher.isOn = WatchTexChanges.Value;
            fileWatcher.onValueChanged.AddListener((value) =>
            {
                WatchTexChanges.Value = value;
                if (!value)
                    TexChangeWatcher?.Dispose();
            });

            var close = UIUtility.CreateButton("CloseButton", drag.transform, "");
            close.transform.SetRect(1f, 0f, 1f, 1f, -20f);
            close.onClick.AddListener(() => HideUI());

            //X button
            var x1 = UIUtility.CreatePanel("x1", close.transform);
            x1.transform.SetRect(0f, 0f, 1f, 1f, 8f, 0f, -8f);
            x1.rectTransform.eulerAngles = new Vector3(0f, 0f, 45f);
            x1.color = Color.black;
            var x2 = UIUtility.CreatePanel("x2", close.transform);
            x2.transform.SetRect(0f, 0f, 1f, 1f, 8f, 0f, -8f);
            x2.rectTransform.eulerAngles = new Vector3(0f, 0f, -45f);
            x2.color = Color.black;

            MaterialEditorScrollableUI = UIUtility.CreateScrollView("MaterialEditorWindow", MaterialEditorMainPanel.transform);
            MaterialEditorScrollableUI.transform.SetRect(0f, 0f, 1f, 1f, marginSize, marginSize, -marginSize, -headerSize - marginSize / 2f);
            MaterialEditorScrollableUI.gameObject.AddComponent<Mask>();
            MaterialEditorScrollableUI.content.gameObject.AddComponent<VerticalLayoutGroup>();
            MaterialEditorScrollableUI.content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            MaterialEditorScrollableUI.verticalScrollbar.GetComponent<RectTransform>().offsetMin = new Vector2(scrollOffsetX, 0f);
            MaterialEditorScrollableUI.viewport.offsetMax = new Vector2(scrollOffsetX, 0f);
            MaterialEditorScrollableUI.movementType = ScrollRect.MovementType.Clamped;
        }

        public static void HideUI()
        {
            MaterialEditorWindow?.gameObject?.SetActive(false);
            TexChangeWatcher?.Dispose();
        }

        private void UISettingChanged(object sender, EventArgs e)
        {
            MaterialEditorWindow.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f / UIScale.Value, 1080f / UIScale.Value);
            MaterialEditorMainPanel?.transform?.SetRect(0.05f, 0.05f, UIWidth.Value * UIScale.Value, UIHeight.Value * UIScale.Value);
        }

        internal void PopulateListBody()
        {
            var chaControl = MakerAPI.GetCharacterControl();
            PopulateList(chaControl.gameObject, ObjectType.Character, filterType: FilterType.Body);
        }

        internal void PopulateListFace()
        {
            var chaControl = MakerAPI.GetCharacterControl();
            PopulateList(chaControl.gameObject, ObjectType.Character, filterType: FilterType.Face);
        }

        internal void PopulateListCharacter()
        {
            var chaControl = MakerAPI.GetCharacterControl();
            PopulateList(chaControl.gameObject, ObjectType.Character);
        }

        protected void PopulateList(GameObject gameObject, ObjectType objectType, int coordinateIndex = 0, int slot = 0, FilterType filterType = FilterType.All)
        {
            MaterialEditorWindow.gameObject.SetActive(true);
            MaterialEditorWindow.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f / UIScale.Value, 1080f / UIScale.Value);
            MaterialEditorMainPanel.transform.SetRect(0.05f, 0.05f, UIWidth.Value * UIScale.Value, UIHeight.Value * UIScale.Value);

            foreach (Transform child in MaterialEditorScrollableUI.content)
                Destroy(child.gameObject);

            if (gameObject == null) return;
            if (objectType == ObjectType.Hair || objectType == ObjectType.Character)
                coordinateIndex = 0;

            List<Renderer> rendList = new List<Renderer>();
            List<string> mats = new List<string>();
            int rowCounter = 0;
            Color RowColor() => rowCounter % 2 == 0 ? evenRowColor : oddRowColor;

            Dictionary<string, Material> matList = new Dictionary<string, Material>();

            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl == null)
            {
                rendList = GetRendererList(gameObject);
                filterType = FilterType.All;
            }
            else
            {
                if (filterType == FilterType.Body)
                {
                    matList[chaControl.customMatBody.NameFormatted()] = chaControl.customMatBody;
                    rendList.Add(chaControl.rendBody);
                }
                else if (filterType == FilterType.Face)
                {
                    matList[chaControl.customMatFace.NameFormatted()] = chaControl.customMatFace;
                    rendList.Add(chaControl.rendFace);
                }
                else
                    rendList = GetRendererList(gameObject);
            }

            foreach (var rend in rendList)
            {
                foreach (var mat in rend.sharedMaterials)
                    matList[mat.NameFormatted()] = mat;

                var contentListHeader = UIUtility.CreatePanel("ContentList", MaterialEditorScrollableUI.content.transform);
                contentListHeader.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
                contentListHeader.gameObject.AddComponent<Mask>();
                contentListHeader.gameObject.AddComponent<HorizontalLayoutGroup>().padding = padding;
                contentListHeader.color = RowColor();
                rowCounter++;

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

                var contentItem1 = UIUtility.CreatePanel("ContentItem1", MaterialEditorScrollableUI.content.transform);
                contentItem1.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
                contentItem1.gameObject.AddComponent<Mask>();
                contentItem1.gameObject.AddComponent<HorizontalLayoutGroup>().padding = padding;
                contentItem1.color = RowColor();
                rowCounter++;

                bool valueEnabled = rend.enabled;
                bool valueEnabledInitial = rend.enabled;
                var temp = GetRendererPropertyValueOriginal(objectType, coordinateIndex, slot, rend.NameFormatted(), RendererProperties.Enabled);
                if (!temp.IsNullOrEmpty())
                    valueEnabledInitial = temp == "1";
                string EnabledLabelText() => valueEnabled == valueEnabledInitial ? "Enabled:" : "Enabled:*";

                var labelEnabled = UIUtility.CreateText("Enabled", contentItem1.transform, EnabledLabelText());
                labelEnabled.alignment = TextAnchor.MiddleLeft;
                labelEnabled.color = Color.black;
                var labelEnabledLE = labelEnabled.gameObject.AddComponent<LayoutElement>();
                labelEnabledLE.preferredWidth = labelWidth;
                labelEnabledLE.flexibleWidth = labelWidth;

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
                    valueEnabled = value == 1;

                    AddRendererProperty(objectType, coordinateIndex, slot, rend.NameFormatted(), RendererProperties.Enabled, value.ToString(), valueEnabledInitial ? "1" : "0", gameObject);
                    labelEnabled.text = EnabledLabelText();
                });
                var dropdownEnabledLE = dropdownEnabled.gameObject.AddComponent<LayoutElement>();
                dropdownEnabledLE.preferredWidth = dropdownWidth;
                dropdownEnabledLE.flexibleWidth = 0;

                var resetEnabled = UIUtility.CreateButton("ResetEnabled", contentItem1.transform, "Reset");
                resetEnabled.onClick.AddListener(() =>
                {
                    RemoveRendererProperty(objectType, coordinateIndex, slot, rend.NameFormatted(), RendererProperties.Enabled, gameObject);
                    valueEnabled = valueEnabledInitial;
                    dropdownEnabled.value = valueEnabledInitial ? 1 : 0;
                    labelEnabled.text = EnabledLabelText();
                });
                var resetEnabledLE = resetEnabled.gameObject.AddComponent<LayoutElement>();
                resetEnabledLE.preferredWidth = resetButtonWidth;
                resetEnabledLE.flexibleWidth = 0;

                var contentItem2 = UIUtility.CreatePanel("ContentItem2", MaterialEditorScrollableUI.content.transform);
                contentItem2.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
                contentItem2.gameObject.AddComponent<Mask>();
                contentItem2.gameObject.AddComponent<HorizontalLayoutGroup>().padding = padding;
                contentItem2.color = RowColor();
                rowCounter++;

                var valueShadowCastingMode = rend.shadowCastingMode;
                var valueShadowCastingModeInitial = rend.shadowCastingMode;
                temp = GetRendererPropertyValueOriginal(objectType, coordinateIndex, slot, rend.NameFormatted(), RendererProperties.ShadowCastingMode);
                if (!temp.IsNullOrEmpty())
                    valueShadowCastingModeInitial = (UnityEngine.Rendering.ShadowCastingMode)int.Parse(temp);
                string ShadowCastingModeLabelText() => valueShadowCastingMode == valueShadowCastingModeInitial ? "ShadowCastingMode:" : "ShadowCastingMode:*";

                var labelShadowCastingMode = UIUtility.CreateText("ShadowCastingMode", contentItem2.transform, ShadowCastingModeLabelText());
                labelShadowCastingMode.alignment = TextAnchor.MiddleLeft;
                labelShadowCastingMode.color = Color.black;
                var labelShadowCastingModeLE = labelShadowCastingMode.gameObject.AddComponent<LayoutElement>();
                labelShadowCastingModeLE.preferredWidth = labelWidth;
                labelShadowCastingModeLE.flexibleWidth = labelWidth;

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
                    valueShadowCastingMode = (UnityEngine.Rendering.ShadowCastingMode)value;
                    AddRendererProperty(objectType, coordinateIndex, slot, rend.NameFormatted(), RendererProperties.ShadowCastingMode, value.ToString(), ((int)valueShadowCastingModeInitial).ToString(), gameObject);
                    labelShadowCastingMode.text = ShadowCastingModeLabelText();
                });
                var dropdownShadowCastingModeLE = dropdownShadowCastingMode.gameObject.AddComponent<LayoutElement>();
                dropdownShadowCastingModeLE.preferredWidth = dropdownWidth;
                dropdownShadowCastingModeLE.flexibleWidth = 0;

                var resetShadowCastingMode = UIUtility.CreateButton("ResetShadowCastingMode", contentItem2.transform, "Reset");
                resetShadowCastingMode.onClick.AddListener(() =>
                {
                    RemoveRendererProperty(objectType, coordinateIndex, slot, rend.NameFormatted(), RendererProperties.ShadowCastingMode, gameObject);
                    valueShadowCastingMode = valueShadowCastingModeInitial;
                    dropdownShadowCastingMode.value = (int)valueShadowCastingModeInitial;
                    labelShadowCastingMode.text = ShadowCastingModeLabelText();
                });
                var resetShadowCastingModeLE = resetShadowCastingMode.gameObject.AddComponent<LayoutElement>();
                resetShadowCastingModeLE.preferredWidth = resetButtonWidth;
                resetShadowCastingModeLE.flexibleWidth = 0;

                var contentItem3 = UIUtility.CreatePanel("ContentItem3", MaterialEditorScrollableUI.content.transform);
                contentItem3.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
                contentItem3.gameObject.AddComponent<Mask>();
                contentItem3.gameObject.AddComponent<HorizontalLayoutGroup>().padding = padding;
                contentItem3.color = RowColor();
                rowCounter++;

                bool valueReceiveShadows = rend.receiveShadows;
                bool valueReceiveShadowsInitial = rend.receiveShadows;
                temp = GetRendererPropertyValueOriginal(objectType, coordinateIndex, slot, rend.NameFormatted(), RendererProperties.ShadowCastingMode);
                if (!temp.IsNullOrEmpty())
                    valueReceiveShadowsInitial = temp == "1";
                string ReceiveShadowsLabelText() => valueReceiveShadows == valueReceiveShadowsInitial ? "ReceiveShadows:" : "ReceiveShadows:*";

                var labelReceiveShadows = UIUtility.CreateText("ReceiveShadows", contentItem3.transform, ReceiveShadowsLabelText());
                labelReceiveShadows.alignment = TextAnchor.MiddleLeft;
                labelReceiveShadows.color = Color.black;
                var labelReceiveShadowsLE = labelReceiveShadows.gameObject.AddComponent<LayoutElement>();
                labelReceiveShadowsLE.preferredWidth = labelWidth;
                labelReceiveShadowsLE.flexibleWidth = labelWidth;

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
                    valueReceiveShadows = value == 1;
                    AddRendererProperty(objectType, coordinateIndex, slot, rend.NameFormatted(), RendererProperties.ReceiveShadows, value.ToString(), valueReceiveShadowsInitial ? "1" : "0", gameObject);
                    labelReceiveShadows.text = ReceiveShadowsLabelText();
                });
                var dropdownReceiveShadowsLE = dropdownReceiveShadows.gameObject.AddComponent<LayoutElement>();
                dropdownReceiveShadowsLE.preferredWidth = dropdownWidth;
                dropdownReceiveShadowsLE.flexibleWidth = 0;

                var resetReceiveShadows = UIUtility.CreateButton("ResetReceiveShadows", contentItem3.transform, "Reset");
                resetReceiveShadows.onClick.AddListener(() =>
                {
                    RemoveRendererProperty(objectType, coordinateIndex, slot, rend.NameFormatted(), RendererProperties.ReceiveShadows, gameObject);
                    valueReceiveShadows = valueReceiveShadowsInitial;
                    dropdownReceiveShadows.value = valueReceiveShadowsInitial ? 1 : 0;
                    labelReceiveShadows.text = ReceiveShadowsLabelText();
                });
                var resetReceiveShadowsLE = resetReceiveShadows.gameObject.AddComponent<LayoutElement>();
                resetReceiveShadowsLE.preferredWidth = resetButtonWidth;
                resetReceiveShadowsLE.flexibleWidth = 0;
            }

            foreach (var mat in matList.Values)
            {
                string materialName = mat.NameFormatted();
                string shaderName = mat.shader.NameFormatted();

                var contentListHeader1 = UIUtility.CreatePanel("ContentList", MaterialEditorScrollableUI.content.transform);
                contentListHeader1.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
                contentListHeader1.gameObject.AddComponent<Mask>();
                contentListHeader1.gameObject.AddComponent<HorizontalLayoutGroup>().padding = padding;
                contentListHeader1.color = RowColor();
                rowCounter++;

                var labelMat = UIUtility.CreateText(materialName, contentListHeader1.transform, "Material:");
                labelMat.alignment = TextAnchor.MiddleLeft;
                labelMat.color = Color.black;
                var labelMat2 = UIUtility.CreateText(materialName, contentListHeader1.transform, materialName);
                labelMat2.alignment = TextAnchor.MiddleRight;
                labelMat2.color = Color.black;

                var contentListHeader2 = UIUtility.CreatePanel("ContentList", MaterialEditorScrollableUI.content.transform);
                contentListHeader2.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
                contentListHeader2.gameObject.AddComponent<Mask>();
                contentListHeader2.gameObject.AddComponent<HorizontalLayoutGroup>().padding = padding;
                contentListHeader2.color = RowColor();
                rowCounter++;

                var labelShader = UIUtility.CreateText(mat.shader.NameFormatted(), contentListHeader2.transform, "Shader:");
                labelShader.alignment = TextAnchor.MiddleLeft;
                labelShader.color = Color.black;
                var labelShaderLE = labelShader.gameObject.AddComponent<LayoutElement>();
                labelShaderLE.preferredWidth = labelWidth;
                labelShaderLE.flexibleWidth = labelWidth;

                if (MaterialEditorPlugin.XMLShaderProperties.Count == 0)
                {
                    var labelShader2 = UIUtility.CreateText(mat.shader.NameFormatted(), contentListHeader2.transform, shaderName);
                    labelShader2.alignment = TextAnchor.MiddleRight;
                    labelShader2.color = Color.black;
                }
                else
                {
                    string shaderNameInitial = shaderName;
                    var temp = GetMaterialShaderNameOriginal(objectType, coordinateIndex, slot, materialName);
                    if (!temp.IsNullOrEmpty())
                        shaderNameInitial = temp;
                    string ShaderLabelText() => shaderName == shaderNameInitial ? "Shader:" : "Shader:*";

                    labelShader.text = ShaderLabelText();

                    var dropdownShader = UIUtility.CreateDropdown("Shader", contentListHeader2.transform);
                    dropdownShader.transform.SetRect(0f, 0f, 0f, 1f, 0f, 0f, 100f);
                    dropdownShader.captionText.transform.SetRect(0f, 0f, 1f, 1f, 0f, 2f, -15f, -2f);
                    dropdownShader.captionText.alignment = TextAnchor.MiddleLeft;
                    dropdownShader.options.Clear();
                    dropdownShader.options.Add(new Dropdown.OptionData(shaderNameInitial));
                    foreach (var shader in MaterialEditorPlugin.XMLShaderProperties.Where(x => x.Key != "default" && x.Key != shaderNameInitial))
                        dropdownShader.options.Add(new Dropdown.OptionData(shader.Key));
                    dropdownShader.value = ShaderSelectedIndex();
                    dropdownShader.captionText.text = shaderName;
                    dropdownShader.onValueChanged.AddListener((value) =>
                    {
                        if (value == 0)
                        {
                            shaderName = shaderNameInitial;
                            RemoveMaterialShaderName(objectType, coordinateIndex, slot, materialName, gameObject);

                            if (MaterialEditorPlugin.XMLShaderProperties.ContainsKey(shaderNameInitial))
                                PopulateList(gameObject, objectType, coordinateIndex, slot, filterType: filterType);
                            else
                                Logger.LogMessage("Save and reload to refresh shader.");
                        }
                        else
                        {
                            int counter = 0;
                            foreach (var shader in MaterialEditorPlugin.XMLShaderProperties.Where(x => x.Key != "default" && x.Key != shaderNameInitial))
                            {
                                counter++;
                                if (counter == value)
                                {
                                    shaderName = shader.Key;
                                    AddMaterialShaderName(objectType, coordinateIndex, slot, materialName, shader.Key, shaderNameInitial, gameObject);
                                    PopulateList(gameObject, objectType, coordinateIndex, slot, filterType: filterType);

                                    break;
                                }
                            }
                        }

                        labelShader.text = ShaderLabelText();
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
                        foreach (var shader in MaterialEditorPlugin.XMLShaderProperties.Where(x => x.Key != "default" && x.Key != shaderNameInitial))
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
                        shaderName = shaderNameInitial;
                        labelShader.text = ShaderLabelText();

                        RemoveMaterialShaderName(objectType, coordinateIndex, slot, materialName, gameObject);
                        PopulateList(gameObject, objectType, coordinateIndex, slot, filterType: filterType);
                    });
                    var resetShaderLE = resetShader.gameObject.AddComponent<LayoutElement>();
                    resetShaderLE.preferredWidth = resetButtonWidth;
                    resetShaderLE.flexibleWidth = 0;

                    var contentListHeader3 = UIUtility.CreatePanel("ContentList", MaterialEditorScrollableUI.content.transform);
                    contentListHeader3.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
                    contentListHeader3.gameObject.AddComponent<Mask>();
                    contentListHeader3.gameObject.AddComponent<HorizontalLayoutGroup>().padding = padding;
                    contentListHeader3.color = RowColor();
                    rowCounter++;

                    int renderQueue = mat.renderQueue;
                    int renderQueueOriginal = mat.renderQueue;
                    int? renderQueueOriginalTemp = GetMaterialShaderRenderQueueOriginal(objectType, coordinateIndex, slot, materialName);
                    renderQueueOriginal = renderQueueOriginalTemp == null ? renderQueue : (int)renderQueueOriginalTemp;

                    string RenderQueueLabelText() => renderQueue == renderQueueOriginal ? "RenderQueue:" : "RenderQueue:*";

                    var labelShaderRenderQueue = UIUtility.CreateText("ShaderRenderQueue", contentListHeader3.transform, RenderQueueLabelText());
                    labelShaderRenderQueue.alignment = TextAnchor.MiddleLeft;
                    labelShaderRenderQueue.color = Color.black;
                    var labelShaderRenderQueueLE = labelShaderRenderQueue.gameObject.AddComponent<LayoutElement>();
                    labelShaderRenderQueueLE.preferredWidth = labelWidth;
                    labelShaderRenderQueueLE.flexibleWidth = labelWidth;

                    var textBoxShaderRenderQueue = UIUtility.CreateInputField("ShaderRenderQueue", contentListHeader3.transform);
                    textBoxShaderRenderQueue.text = renderQueue.ToString();
                    textBoxShaderRenderQueue.onEndEdit.AddListener((value) =>
                    {
                        if (!int.TryParse(value, out int intValue))
                        {
                            textBoxShaderRenderQueue.text = renderQueue.ToString();
                            return;
                        }
                        renderQueue = intValue;
                        textBoxShaderRenderQueue.text = renderQueue.ToString();

                        AddMaterialShaderRenderQueue(objectType, coordinateIndex, slot, materialName, renderQueue, renderQueueOriginal, gameObject);

                        labelShaderRenderQueue.text = RenderQueueLabelText();
                    });
                    var textBoxShaderRenderQueueLE = textBoxShaderRenderQueue.gameObject.AddComponent<LayoutElement>();
                    textBoxShaderRenderQueueLE.preferredWidth = textBoxWidth;
                    textBoxShaderRenderQueueLE.flexibleWidth = 0;

                    var resetShaderRenderQueue = UIUtility.CreateButton($"ResetRenderQueue", contentListHeader3.transform, "Reset");
                    resetShaderRenderQueue.onClick.AddListener(() =>
                    {
                        RemoveMaterialShaderRenderQueue(objectType, coordinateIndex, slot, materialName, gameObject);
                        renderQueue = renderQueueOriginal;
                        textBoxShaderRenderQueue.text = renderQueueOriginal.ToString();
                        labelShaderRenderQueue.text = RenderQueueLabelText();
                    });
                    var resetShaderRenderQueueLE = resetShaderRenderQueue.gameObject.AddComponent<LayoutElement>();
                    resetShaderRenderQueueLE.preferredWidth = resetButtonWidth;
                    resetShaderRenderQueueLE.flexibleWidth = 0;
                }

                foreach (var property in MaterialEditorPlugin.XMLShaderProperties[MaterialEditorPlugin.XMLShaderProperties.ContainsKey(shaderName) ? shaderName : "default"].OrderBy(x => x.Value.Type).ThenBy(x => x.Key))
                {
                    string propertyName = property.Key;
                    if (MaterialEditorPlugin.CheckBlacklist(objectType, propertyName)) continue;
                    string LabelText(bool defaultValue) => defaultValue ? propertyName + ":" : propertyName + ":*";

                    if (property.Value.Type == ShaderPropertyType.Color)
                    {
                        if (mat.HasProperty($"_{propertyName}"))
                        {
                            var contentList = UIUtility.CreatePanel("ContentList", MaterialEditorScrollableUI.content.transform);
                            contentList.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
                            contentList.gameObject.AddComponent<Mask>();
                            contentList.gameObject.AddComponent<HorizontalLayoutGroup>().padding = padding;
                            contentList.color = RowColor();
                            rowCounter++;

                            Color valueColor = mat.GetColor($"_{propertyName}");
                            Color valueColorInitial = valueColor;
                            Color? c = GetMaterialColorPropertyValueOriginal(objectType, coordinateIndex, slot, materialName, propertyName);
                            if (c != null)
                                valueColorInitial = (Color)c;

                            bool ColorDefault() => valueColor == valueColorInitial;

                            var label = UIUtility.CreateText(propertyName, contentList.transform, LabelText(ColorDefault()));
                            label.alignment = TextAnchor.MiddleLeft;
                            label.color = Color.black;
                            var labelLE = label.gameObject.AddComponent<LayoutElement>();
                            labelLE.preferredWidth = labelWidth;
                            labelLE.flexibleWidth = labelWidth;

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
                                if (!float.TryParse(value, out float valueNew))
                                {
                                    textBoxR.text = valueColor.r.ToString();
                                    return;
                                }

                                Color colorOrig = mat.GetColor($"_{propertyName}");
                                valueColor = new Color(valueNew, colorOrig.g, colorOrig.b, colorOrig.a);

                                AddMaterialColorProperty(objectType, coordinateIndex, slot, materialName, propertyName, valueColor, valueColorInitial, gameObject);

                                label.text = LabelText(ColorDefault());
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
                                if (!float.TryParse(value, out float valueNew))
                                {
                                    textBoxG.text = valueColor.g.ToString();
                                    return;
                                }

                                Color colorOrig = mat.GetColor($"_{propertyName}");
                                valueColor = new Color(colorOrig.r, valueNew, colorOrig.b, colorOrig.a);

                                AddMaterialColorProperty(objectType, coordinateIndex, slot, materialName, propertyName, valueColor, valueColorInitial, gameObject);

                                label.text = LabelText(ColorDefault());
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
                                if (!float.TryParse(value, out float valueNew))
                                {
                                    textBoxB.text = valueColor.b.ToString();
                                    return;
                                }

                                Color colorOrig = mat.GetColor($"_{propertyName}");
                                valueColor = new Color(colorOrig.r, colorOrig.g, valueNew, colorOrig.a);

                                AddMaterialColorProperty(objectType, coordinateIndex, slot, materialName, propertyName, valueColor, valueColorInitial, gameObject);

                                label.text = LabelText(ColorDefault());
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
                                if (!float.TryParse(value, out float valueNew))
                                {
                                    textBoxA.text = valueColor.a.ToString();
                                    return;
                                }

                                Color colorOrig = mat.GetColor($"_{propertyName}");
                                valueColor = new Color(colorOrig.r, colorOrig.g, colorOrig.b, valueNew);

                                AddMaterialColorProperty(objectType, coordinateIndex, slot, materialName, propertyName, valueColor, valueColorInitial, gameObject);

                                label.text = LabelText(ColorDefault());
                            });

                            var textBoxALE = textBoxA.gameObject.AddComponent<LayoutElement>();
                            textBoxALE.preferredWidth = textBoxWidth;
                            textBoxALE.flexibleWidth = 0;

                            var resetColor = UIUtility.CreateButton($"Reset{propertyName}", contentList.transform, "Reset");
                            resetColor.onClick.AddListener(() =>
                            {
                                RemoveMaterialColorProperty(objectType, coordinateIndex, slot, materialName, propertyName, gameObject);

                                textBoxR.text = valueColorInitial.r.ToString();
                                textBoxG.text = valueColorInitial.g.ToString();
                                textBoxB.text = valueColorInitial.b.ToString();
                                textBoxA.text = valueColorInitial.a.ToString();
                                label.text = LabelText(true);
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
                            var contentList = UIUtility.CreatePanel("ContentList", MaterialEditorScrollableUI.content.transform);
                            contentList.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
                            contentList.gameObject.AddComponent<Mask>();
                            contentList.gameObject.AddComponent<HorizontalLayoutGroup>().padding = padding;
                            contentList.color = RowColor();
                            rowCounter++;

                            bool defaultValue = GetMaterialTextureValueOriginal(objectType, coordinateIndex, slot, materialName, propertyName);

                            var label = UIUtility.CreateText(propertyName, contentList.transform, LabelText(defaultValue));
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
                                OpenFileDialog.Show(strings => OnFileAccept(strings), "Open image", Application.dataPath, MaterialEditorPlugin.FileFilter, MaterialEditorPlugin.FileExt);

                                void OnFileAccept(string[] strings)
                                {
                                    if (strings == null || strings.Length == 0) return;
                                    if (strings[0].IsNullOrEmpty()) return;
                                    string filePath = strings[0];

                                    AddMaterialTexture(objectType, coordinateIndex, slot, materialName, propertyName, filePath, gameObject);

                                    TexChangeWatcher?.Dispose();
                                    if (WatchTexChanges.Value)
                                    {
                                        var directory = Path.GetDirectoryName(filePath);
                                        if (directory != null)
                                        {
                                            TexChangeWatcher = new FileSystemWatcher(directory, Path.GetFileName(filePath));
                                            TexChangeWatcher.Changed += (sender, args) =>
                                            {
                                                if (WatchTexChanges.Value && File.Exists(filePath))
                                                    AddMaterialTexture(objectType, coordinateIndex, slot, materialName, propertyName, filePath, gameObject);
                                            };
                                            TexChangeWatcher.Deleted += (sender, args) => TexChangeWatcher?.Dispose();
                                            TexChangeWatcher.Error += (sender, args) => TexChangeWatcher?.Dispose();
                                            TexChangeWatcher.EnableRaisingEvents = true;
                                        }
                                    }
                                }

                                label.text = LabelText(false);
                            });
                            var importButtonLE = importButton.gameObject.AddComponent<LayoutElement>();
                            importButtonLE.preferredWidth = buttonWidth;
                            importButtonLE.flexibleWidth = 0;

                            var resetTexture = UIUtility.CreateButton($"Reset{propertyName}", contentList.transform, "Reset");
                            resetTexture.onClick.AddListener(() =>
                            {
                                RemoveMaterialTexture(objectType, coordinateIndex, slot, materialName, propertyName);

                                label.text = LabelText(true);
                            });
                            var resetTextureLE = resetTexture.gameObject.AddComponent<LayoutElement>();
                            resetTextureLE.preferredWidth = resetButtonWidth;
                            resetTextureLE.flexibleWidth = 0;

                            //Offset & Scale
                            var contentList2 = UIUtility.CreatePanel("ContentList", MaterialEditorScrollableUI.content.transform);
                            contentList2.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
                            contentList2.gameObject.AddComponent<Mask>();
                            contentList2.gameObject.AddComponent<HorizontalLayoutGroup>().padding = padding;
                            contentList2.color = RowColor();
                            rowCounter++;

                            Vector2 textureOffset = mat.GetTextureOffset($"_{propertyName}");
                            Vector2 textureOffsetInitial = textureOffset;
                            Vector2? textureOffsetInitialTemp = GetMaterialTextureOffsetOriginal(objectType, coordinateIndex, slot, materialName, propertyName);
                            if (textureOffsetInitialTemp != null)
                                textureOffsetInitial = (Vector2)textureOffsetInitialTemp;

                            Vector2 textureScale = mat.GetTextureScale($"_{propertyName}");
                            Vector2 textureScaleInitial = textureScale;
                            Vector2? textureScaleInitialTemp = GetMaterialTextureScaleOriginal(objectType, coordinateIndex, slot, materialName, propertyName);
                            if (textureScaleInitialTemp != null)
                                textureScaleInitial = (Vector2)textureScaleInitialTemp;

                            string labelOffsetScaleText() => (textureOffset == textureOffsetInitial && textureScale == textureScaleInitial) ? "" : "*";

                            var label2 = UIUtility.CreateText(propertyName, contentList2.transform, labelOffsetScaleText());
                            label2.alignment = TextAnchor.MiddleLeft;
                            label2.color = Color.black;
                            var label2LE = label2.gameObject.AddComponent<LayoutElement>();
                            label2LE.preferredWidth = labelWidth;
                            label2LE.flexibleWidth = labelWidth;

                            //Offset
                            var labelOffsetX = UIUtility.CreateText("OffsetX", contentList2.transform, "Offset X");
                            labelOffsetX.alignment = TextAnchor.MiddleLeft;
                            labelOffsetX.color = Color.black;
                            var labelOffsetXLE = labelOffsetX.gameObject.AddComponent<LayoutElement>();
                            labelOffsetXLE.preferredWidth = labelXWidth;
                            labelOffsetXLE.flexibleWidth = 0;

                            var textBoxOffsetX = UIUtility.CreateInputField("OffsetX", contentList2.transform);
                            textBoxOffsetX.text = textureOffset.x.ToString();
                            textBoxOffsetX.onEndEdit.AddListener((value) =>
                            {
                                if (!float.TryParse(value, out float offsetX))
                                {
                                    textBoxOffsetX.text = textureOffset.x.ToString();
                                    return;
                                }
                                float offsetY = mat.GetTextureOffset($"_{propertyName}").y;
                                textureOffset = new Vector2(offsetX, offsetY);

                                AddMaterialTextureOffset(objectType, coordinateIndex, slot, materialName, propertyName, textureOffset, textureOffsetInitial, gameObject);

                                label2.text = labelOffsetScaleText();
                            });
                            var textBoxOffsetXLE = textBoxOffsetX.gameObject.AddComponent<LayoutElement>();
                            textBoxOffsetXLE.preferredWidth = textBoxXYWidth;
                            textBoxOffsetXLE.flexibleWidth = 0;

                            var labelOffsetY = UIUtility.CreateText("OffsetY", contentList2.transform, "Y");
                            labelOffsetY.alignment = TextAnchor.MiddleLeft;
                            labelOffsetY.color = Color.black;
                            var labelOffsetYLE = labelOffsetY.gameObject.AddComponent<LayoutElement>();
                            labelOffsetYLE.preferredWidth = labelYWidth;
                            labelOffsetYLE.flexibleWidth = 0;

                            var textBoxOffsetY = UIUtility.CreateInputField("OffsetY", contentList2.transform);
                            textBoxOffsetY.text = textureOffset.y.ToString();
                            textBoxOffsetY.onEndEdit.AddListener((value) =>
                            {
                                if (!float.TryParse(value, out float offsetY))
                                {
                                    textBoxOffsetY.text = textureOffset.y.ToString();
                                    return;
                                }
                                float offsetX = mat.GetTextureOffset($"_{propertyName}").x;
                                textureOffset = new Vector2(offsetX, offsetY);

                                AddMaterialTextureOffset(objectType, coordinateIndex, slot, materialName, propertyName, textureOffset, textureOffsetInitial, gameObject);

                                label2.text = labelOffsetScaleText();
                            });
                            var textBoxOffsetYLE = textBoxOffsetY.gameObject.AddComponent<LayoutElement>();
                            textBoxOffsetYLE.preferredWidth = textBoxXYWidth;
                            textBoxOffsetYLE.flexibleWidth = 0;

                            //Scale
                            var labelScaleX = UIUtility.CreateText("ScaleX", contentList2.transform, "Scale X");
                            labelScaleX.alignment = TextAnchor.MiddleLeft;
                            labelScaleX.color = Color.black;
                            var labelScaleXLE = labelScaleX.gameObject.AddComponent<LayoutElement>();
                            labelScaleXLE.preferredWidth = labelXWidth;
                            labelScaleXLE.flexibleWidth = 0;

                            var textBoxScaleX = UIUtility.CreateInputField("ScaleX", contentList2.transform);
                            textBoxScaleX.text = textureScale.x.ToString();
                            textBoxScaleX.onEndEdit.AddListener((value) =>
                            {
                                if (!float.TryParse(value, out float scaleX))
                                {
                                    textBoxScaleX.text = textureScale.x.ToString();
                                    return;
                                }
                                float scaleY = mat.GetTextureScale($"_{propertyName}").y;
                                textureScale = new Vector2(scaleX, scaleY);

                                AddMaterialTextureScale(objectType, coordinateIndex, slot, materialName, propertyName, textureScale, textureScaleInitial, gameObject);

                                label2.text = labelOffsetScaleText();
                            });
                            var textBoxScaleXLE = textBoxScaleX.gameObject.AddComponent<LayoutElement>();
                            textBoxScaleXLE.preferredWidth = textBoxXYWidth;
                            textBoxScaleXLE.flexibleWidth = 0;

                            var labelScaleY = UIUtility.CreateText("ScaleY", contentList2.transform, "Y");
                            labelScaleY.alignment = TextAnchor.MiddleLeft;
                            labelScaleY.color = Color.black;
                            var labelScaleYLE = labelScaleY.gameObject.AddComponent<LayoutElement>();
                            labelScaleYLE.preferredWidth = labelYWidth;
                            labelScaleYLE.flexibleWidth = 0;

                            var textBoxScaleY = UIUtility.CreateInputField("ScaleY", contentList2.transform);
                            textBoxScaleY.text = textureScale.y.ToString();
                            textBoxScaleY.onEndEdit.AddListener((value) =>
                            {
                                if (!float.TryParse(value, out float scaleY))
                                {
                                    textBoxScaleY.text = textureScale.y.ToString();
                                    return;
                                }
                                float scaleX = mat.GetTextureScale($"_{propertyName}").x;
                                textureScale = new Vector2(scaleX, scaleY);

                                AddMaterialTextureScale(objectType, coordinateIndex, slot, materialName, propertyName, textureOffset, textureOffsetInitial, gameObject);

                                label2.text = labelOffsetScaleText();
                            });
                            var textBoxScaleYLE = textBoxScaleY.gameObject.AddComponent<LayoutElement>();
                            textBoxScaleYLE.preferredWidth = textBoxXYWidth;
                            textBoxScaleYLE.flexibleWidth = 0;

                            var resetTextureOffsetScale = UIUtility.CreateButton($"Reset{propertyName}", contentList2.transform, "Reset");
                            resetTextureOffsetScale.onClick.AddListener(() =>
                            {
                                RemoveMaterialTextureOffset(objectType, coordinateIndex, slot, materialName, propertyName, gameObject);
                                RemoveMaterialTextureScale(objectType, coordinateIndex, slot, materialName, propertyName, gameObject);

                                SetTextureProperty(gameObject, materialName, propertyName, TexturePropertyType.Offset, textureOffsetInitial);
                                SetTextureProperty(gameObject, materialName, propertyName, TexturePropertyType.Scale, textureScaleInitial);

                                textureOffset = textureOffsetInitial;
                                textureScale = textureScaleInitial;
                                textBoxOffsetX.text = textureOffsetInitial.x.ToString();
                                textBoxOffsetY.text = textureOffsetInitial.y.ToString();
                                textBoxScaleX.text = textureScaleInitial.x.ToString();
                                textBoxScaleY.text = textureScaleInitial.y.ToString();
                                label2.text = labelOffsetScaleText();
                            });
                            var resetTextureOffsetScaleLE = resetTextureOffsetScale.gameObject.AddComponent<LayoutElement>();
                            resetTextureOffsetScaleLE.preferredWidth = resetButtonWidth;
                            resetTextureOffsetScaleLE.flexibleWidth = 0;
                        }
                    }
                    if (property.Value.Type == ShaderPropertyType.Float)
                    {
                        if (mat.HasProperty($"_{propertyName}"))
                        {
                            var contentList = UIUtility.CreatePanel("ContentList", MaterialEditorScrollableUI.content.transform);
                            contentList.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
                            contentList.gameObject.AddComponent<Mask>();
                            contentList.gameObject.AddComponent<HorizontalLayoutGroup>().padding = padding;
                            contentList.color = RowColor();
                            rowCounter++;

                            float valueFloat = mat.GetFloat($"_{propertyName}");
                            float valueFloatInitial = valueFloat;
                            string valueFloatInitialTemp = GetMaterialFloatPropertyValueOriginal(objectType, coordinateIndex, slot, materialName, propertyName);
                            if (!valueFloatInitialTemp.IsNullOrEmpty() && float.TryParse(valueFloatInitialTemp, out float valueFloatInitialTempF))
                                valueFloatInitial = valueFloatInitialTempF;
                            bool doSlider = property.Value.MinValue != null && property.Value.MaxValue != null;
                            bool FloatDefault() => valueFloat == valueFloatInitial;

                            var label = UIUtility.CreateText(propertyName, contentList.transform, LabelText(FloatDefault()));
                            label.alignment = TextAnchor.MiddleLeft;
                            label.color = Color.black;
                            var labelLE = label.gameObject.AddComponent<LayoutElement>();
                            labelLE.preferredWidth = labelWidth;
                            labelLE.flexibleWidth = labelWidth;

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
                            textBoxFloat.text = valueFloat.ToString();
                            textBoxFloat.onEndEdit.AddListener((value) =>
                            {
                                if (!float.TryParse(value, out float valueFloatNew))
                                {
                                    textBoxFloat.text = valueFloat.ToString();
                                    return;
                                }
                                valueFloat = valueFloatNew;

                                AddMaterialFloatProperty(objectType, coordinateIndex, slot, materialName, propertyName, valueFloat, valueFloatInitial, gameObject);

                                if (doSlider && valueFloat <= sliderFloat.maxValue && valueFloat >= sliderFloat.minValue)
                                    sliderFloat.value = valueFloat;
                                label.text = LabelText(FloatDefault());
                            });
                            var textBoxFloatLE = textBoxFloat.gameObject.AddComponent<LayoutElement>();
                            textBoxFloatLE.preferredWidth = textBoxWidth;
                            textBoxFloatLE.flexibleWidth = 0;

                            if (doSlider)
                            {
                                sliderFloat.minValue = (float)property.Value.MinValue;
                                sliderFloat.maxValue = (float)property.Value.MaxValue;
                                sliderFloat.value = valueFloat;
                                sliderFloat.onValueChanged.AddListener((value) =>
                                {
                                    textBoxFloat.text = value.ToString();
                                    textBoxFloat.onEndEdit.Invoke(value.ToString());
                                });
                            }

                            var resetFloat = UIUtility.CreateButton($"Reset{propertyName}", contentList.transform, "Reset");
                            resetFloat.onClick.AddListener(() =>
                            {
                                RemoveMaterialFloatProperty(objectType, coordinateIndex, slot, materialName, propertyName, gameObject);
                                SetFloatProperty(gameObject, materialName, propertyName, valueFloatInitial.ToString());

                                valueFloat = valueFloatInitial;
                                textBoxFloat.text = valueFloatInitial.ToString();
                                label.text = LabelText(FloatDefault());
                                if (doSlider)
                                    sliderFloat.value = valueFloatInitial;
                            });
                            var resetEnabledLE = resetFloat.gameObject.AddComponent<LayoutElement>();
                            resetEnabledLE.preferredWidth = resetButtonWidth;
                            resetEnabledLE.flexibleWidth = 0;
                        }
                    }
                }
            }
        }

        private static void ExportTexture(Material mat, string property)
        {
            var tex = mat.GetTexture($"_{property}");
            string filename = Path.Combine(MaterialEditorPlugin.ExportPath, $"_Export_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}_{mat.NameFormatted()}_{property}.png");
            MaterialEditorPlugin.SaveTex(tex, filename);
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

        protected enum FilterType { All, Body, Face }

        internal abstract string GetRendererPropertyValueOriginal(ObjectType objectType, int coordinateIndex, int slot, string rendererName, RendererProperties property);
        internal abstract void AddRendererProperty(ObjectType objectType, int coordinateIndex, int slot, string rendererName, RendererProperties property, string value, string valueOriginal, GameObject gameObject);
        internal abstract void RemoveRendererProperty(ObjectType objectType, int coordinateIndex, int slot, string rendererName, RendererProperties property, GameObject gameObject);

        internal abstract string GetMaterialShaderNameOriginal(ObjectType objectType, int coordinateIndex, int slot, string materialName);
        internal abstract void AddMaterialShaderName(ObjectType objectType, int coordinateIndex, int slot, string materialName, string value, string valueOriginal, GameObject gameObject);
        internal abstract void RemoveMaterialShaderName(ObjectType objectType, int coordinateIndex, int slot, string materialName, GameObject gameObject);

        internal abstract int? GetMaterialShaderRenderQueueOriginal(ObjectType objectType, int coordinateIndex, int slot, string materialName);
        internal abstract void AddMaterialShaderRenderQueue(ObjectType objectType, int coordinateIndex, int slot, string materialName, int value, int valueOriginal, GameObject gameObject);
        internal abstract void RemoveMaterialShaderRenderQueue(ObjectType objectType, int coordinateIndex, int slot, string materialName, GameObject gameObject);

        internal abstract bool GetMaterialTextureValueOriginal(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName);
        internal abstract void AddMaterialTexture(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, string filePath, GameObject gameObject);
        internal abstract void RemoveMaterialTexture(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName);

        internal abstract Vector2? GetMaterialTextureOffsetOriginal(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName);
        internal abstract void AddMaterialTextureOffset(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, Vector2 value, Vector2 valueOriginal, GameObject gameObject);
        internal abstract void RemoveMaterialTextureOffset(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject);

        internal abstract Vector2? GetMaterialTextureScaleOriginal(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName);
        internal abstract void AddMaterialTextureScale(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, Vector2 value, Vector2 valueOriginal, GameObject gameObject);
        internal abstract void RemoveMaterialTextureScale(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject);

        internal abstract Color? GetMaterialColorPropertyValueOriginal(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName);
        internal abstract void AddMaterialColorProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, Color value, Color valueOriginal, GameObject gameObject);
        internal abstract void RemoveMaterialColorProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject);

        internal abstract string GetMaterialFloatPropertyValueOriginal(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName);
        internal abstract void AddMaterialFloatProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, float value, float valueOriginal, GameObject gameObject);
        internal abstract void RemoveMaterialFloatProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject);
    }
}
