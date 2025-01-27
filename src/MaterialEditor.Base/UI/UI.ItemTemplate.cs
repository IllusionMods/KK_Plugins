using UILib;
using UnityEngine;
using UnityEngine.UI;
using static MaterialEditorAPI.MaterialEditorUI;

namespace MaterialEditorAPI
{
    internal static class ItemTemplate
    {
        internal static GameObject CreateTemplate(Transform parent)
        {
            var contentList = UIUtility.CreatePanel("ListEntry", parent);
            contentList.gameObject.AddComponent<LayoutElement>().preferredHeight = PanelHeight;
            contentList.gameObject.AddComponent<Mask>();
            contentList.color = RowColor;

            //Renderer
            {
                var itemPanel = UIUtility.CreatePanel("RendererPanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.color = RendererColor;
                var itemHLG = itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
                itemHLG.padding = Padding;
                itemHLG.childForceExpandWidth = false;

                var label = UIUtility.CreateText("RendererLabel", itemPanel.transform, "");
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.black;
                var labelLE = label.gameObject.AddComponent<LayoutElement>();
                labelLE.minWidth = 0f;
                labelLE.preferredWidth = 0f;
                labelLE.flexibleWidth = 0f;

                Text labelRenderer = UIUtility.CreateText("RendererText", itemPanel.transform);
                labelRenderer.alignment = TextAnchor.MiddleLeft;
                labelRenderer.color = Color.black;
                var labelRendererLE = labelRenderer.gameObject.AddComponent<LayoutElement>();
                labelRendererLE.minWidth = LabelWidth;
                labelRendererLE.preferredWidth = LabelWidth;
                labelRendererLE.flexibleWidth = 1f;

                CreateInterpolableButton("SelectInterpolableRendererButton", itemPanel.transform);

                Button exportUVButton = UIUtility.CreateButton("ExportUVButton", itemPanel.transform, "Export UV Map");
                var exportUVButtonLE = exportUVButton.gameObject.AddComponent<LayoutElement>();
                exportUVButtonLE.minWidth = RendererButtonWidth;
                exportUVButtonLE.preferredWidth = RendererButtonWidth;
                exportUVButtonLE.flexibleWidth = 0f;

                Button exportMeshButton = UIUtility.CreateButton("ExportObjButton", itemPanel.transform, "Export .obj");
                var exportMeshButtonLE = exportMeshButton.gameObject.AddComponent<LayoutElement>();
                exportMeshButtonLE.minWidth = RendererButtonWidth;
                exportMeshButtonLE.preferredWidth = RendererButtonWidth;
                exportMeshButtonLE.flexibleWidth = 0f;
            }

            //Renderer Enabled
            {
                var itemPanel = UIUtility.CreatePanel("RendererEnabledPanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.color = ItemColor;
                var itemHLG = itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
                itemHLG.padding = Padding;
                itemHLG.childForceExpandWidth = false;

                var label = UIUtility.CreateText("RendererEnabledLabel", itemPanel.transform, "");
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.black;
                var labelLE = label.gameObject.AddComponent<LayoutElement>();
                labelLE.minWidth = LabelWidth;
                labelLE.preferredWidth = LabelWidth;
                labelLE.flexibleWidth = 1f;

                Toggle toggleEnabled = UIUtility.CreateToggle("RendererEnabledToggle", itemPanel.transform, "");
                toggleEnabled.isOn = true;
                var toggleEnabledLE = toggleEnabled.gameObject.AddComponent<LayoutElement>();
                toggleEnabledLE.minWidth = RendererToggleWidth;
                toggleEnabledLE.preferredWidth = RendererToggleWidth;
                toggleEnabledLE.flexibleWidth = 0f;

                var reset = UIUtility.CreateButton($"RendererEnabledResetButton", itemPanel.transform, "R");
                var resetLE = reset.gameObject.AddComponent<LayoutElement>();
                resetLE.minWidth = ResetButtonWidth;
                resetLE.preferredWidth = ResetButtonWidth;
                resetLE.flexibleWidth = 0f;
            }

            //Renderer ShadowCastingMode
            {
                var itemPanel = UIUtility.CreatePanel("RendererShadowCastingModePanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.color = ItemColor;
                var itemHLG = itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
                itemHLG.padding = Padding;
                itemHLG.childForceExpandWidth = false;

                var label = UIUtility.CreateText("RendererShadowCastingModeLabel", itemPanel.transform, "");
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.black;
                var labelLE = label.gameObject.AddComponent<LayoutElement>();
                labelLE.minWidth = LabelWidth;
                labelLE.preferredWidth = LabelWidth;
                labelLE.flexibleWidth = 1f;

                Dropdown dropdownShadowCastingMode = UIUtility.CreateDropdown("RendererShadowCastingModeDropdown", itemPanel.transform);
                dropdownShadowCastingMode.transform.SetRect(0f, 0f, 0f, 1f, 0f, 0f, 100f);
                dropdownShadowCastingMode.captionText.transform.SetRect(0f, 0f, 1f, 1f, 5f, 2f, -15f, -2f);
                dropdownShadowCastingMode.captionText.alignment = TextAnchor.MiddleLeft;
                dropdownShadowCastingMode.options.Clear();
                dropdownShadowCastingMode.options.Add(new Dropdown.OptionData("Off"));
                dropdownShadowCastingMode.options.Add(new Dropdown.OptionData("On"));
                dropdownShadowCastingMode.options.Add(new Dropdown.OptionData("Two Sided"));
                dropdownShadowCastingMode.options.Add(new Dropdown.OptionData("Shadows Only"));
                dropdownShadowCastingMode.value = 0;
                dropdownShadowCastingMode.captionText.text = "Off";
                var dropdownShadowCastingModeLE = dropdownShadowCastingMode.gameObject.AddComponent<LayoutElement>();
                dropdownShadowCastingModeLE.minWidth = RendererDropdownWidth;
                dropdownShadowCastingModeLE.preferredWidth = RendererDropdownWidth;
                dropdownShadowCastingModeLE.flexibleWidth = 0f;

                var reset = UIUtility.CreateButton($"RendererShadowCastingModeResetButton", itemPanel.transform, "R");
                var resetLE = reset.gameObject.AddComponent<LayoutElement>();
                resetLE.minWidth = ResetButtonWidth;
                resetLE.preferredWidth = ResetButtonWidth;
                resetLE.flexibleWidth = 0f;
            }

            //Renderer ReceiveShadows
            {
                var itemPanel = UIUtility.CreatePanel("RendererReceiveShadowsPanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.color = ItemColor;
                var itemHLG = itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
                itemHLG.padding = Padding;
                itemHLG.childForceExpandWidth = false;

                var label = UIUtility.CreateText("RendererReceiveShadowsLabel", itemPanel.transform, "");
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.black;
                var labelLE = label.gameObject.AddComponent<LayoutElement>();
                labelLE.minWidth = LabelWidth;
                labelLE.preferredWidth = LabelWidth;
                labelLE.flexibleWidth = 1f;

                Toggle toggleReceiveShadows = UIUtility.CreateToggle("RendererReceiveShadowsToggle", itemPanel.transform, "");
                toggleReceiveShadows.isOn = true;
                var toggleReceiveShadowsLE = toggleReceiveShadows.gameObject.AddComponent<LayoutElement>();
                toggleReceiveShadowsLE.minWidth = RendererToggleWidth;
                toggleReceiveShadowsLE.preferredWidth = RendererToggleWidth;
                toggleReceiveShadowsLE.flexibleWidth = 0f;

                var reset = UIUtility.CreateButton($"RendererReceiveShadowsResetButton", itemPanel.transform, "R");
                var resetLE = reset.gameObject.AddComponent<LayoutElement>();
                resetLE.minWidth = ResetButtonWidth;
                resetLE.preferredWidth = ResetButtonWidth;
                resetLE.flexibleWidth = 0f;
            }

            //Renderer RendererUpdateWhenOffscreen
            {
                var itemPanel = UIUtility.CreatePanel("RendererUpdateWhenOffscreenPanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.color = ItemColor;
                var itemHLG = itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
                itemHLG.padding = Padding;
                itemHLG.childForceExpandWidth = false;

                var label = UIUtility.CreateText("RendererUpdateWhenOffscreenLabel", itemPanel.transform, "");
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.black;
                var labelLE = label.gameObject.AddComponent<LayoutElement>();
                labelLE.minWidth = LabelWidth;
                labelLE.preferredWidth = LabelWidth;
                labelLE.flexibleWidth = 1f;

                Toggle toggleRendererUpdateWhenOffscreen = UIUtility.CreateToggle("RendererUpdateWhenOffscreenToggle", itemPanel.transform, "");
                toggleRendererUpdateWhenOffscreen.isOn = false;
                var toggleRendererUpdateWhenOffscreenLE = toggleRendererUpdateWhenOffscreen.gameObject.AddComponent<LayoutElement>();
                toggleRendererUpdateWhenOffscreenLE.minWidth = RendererToggleWidth;
                toggleRendererUpdateWhenOffscreenLE.preferredWidth = RendererToggleWidth;
                toggleRendererUpdateWhenOffscreenLE.flexibleWidth = 0f;

                var reset = UIUtility.CreateButton($"RendererUpdateWhenOffscreenResetButton", itemPanel.transform, "R");
                var resetLE = reset.gameObject.AddComponent<LayoutElement>();
                resetLE.minWidth = ResetButtonWidth;
                resetLE.preferredWidth = ResetButtonWidth;
                resetLE.flexibleWidth = 0f;
            }

            //Renderer RecalulateNormals
            {
                var itemPanel = UIUtility.CreatePanel("RendererRecalculateNormalsPanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.color = ItemColor;
                var itemHLG = itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
                itemHLG.padding = Padding;
                itemHLG.childForceExpandWidth = false;

                var label = UIUtility.CreateText("RendererRecalculateNormalsLabel", itemPanel.transform, "");
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.black;
                var labelLE = label.gameObject.AddComponent<LayoutElement>();
                labelLE.minWidth = LabelWidth;
                labelLE.preferredWidth = LabelWidth;
                labelLE.flexibleWidth = 1f;

                Toggle toggleRecalculateNormals = UIUtility.CreateToggle("RendererRecalculateNormalsToggle", itemPanel.transform, "");
                toggleRecalculateNormals.isOn = false;
                var toggleRecalculateNormalsLE = toggleRecalculateNormals.gameObject.AddComponent<LayoutElement>();
                toggleRecalculateNormalsLE.minWidth = RendererToggleWidth;
                toggleRecalculateNormalsLE.preferredWidth = RendererToggleWidth;
                toggleRecalculateNormalsLE.flexibleWidth = 0f;

                var reset = UIUtility.CreateButton($"RendererRecalculateNormalsResetButton", itemPanel.transform, "R");
                var resetLE = reset.gameObject.AddComponent<LayoutElement>();
                resetLE.minWidth = ResetButtonWidth;
                resetLE.preferredWidth = ResetButtonWidth;
                resetLE.flexibleWidth = 0f;
            }

            //Material
            {
                var itemPanel = UIUtility.CreatePanel("MaterialPanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.color = MaterialColor;
                var itemHLG = itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
                itemHLG.padding = Padding;
                itemHLG.childForceExpandWidth = false;

                var label = UIUtility.CreateText("MaterialLabel", itemPanel.transform, "");
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.black;
                var labelLE = label.gameObject.AddComponent<LayoutElement>();
                labelLE.minWidth = 0f;
                labelLE.preferredWidth = 0f;
                labelLE.flexibleWidth = 0f;

                Text labelMaterial = UIUtility.CreateText("MaterialText", itemPanel.transform);
                labelMaterial.alignment = TextAnchor.MiddleLeft;
                labelMaterial.color = Color.black;
                var labelMaterialLE = labelMaterial.gameObject.AddComponent<LayoutElement>();
                labelMaterialLE.minWidth = LabelWidth;
                labelMaterialLE.preferredWidth = LabelWidth;
                labelMaterialLE.flexibleWidth = 1f;

                var copyEdits = UIUtility.CreateButton($"MaterialCopy", itemPanel.transform, "Copy Edits");
                var copyEditsLE = copyEdits.gameObject.AddComponent<LayoutElement>();
                copyEditsLE.minWidth = MaterialButtonWidth;
                copyEditsLE.preferredWidth = MaterialButtonWidth;
                copyEditsLE.flexibleWidth = 0f;

                var pasteEdits = UIUtility.CreateButton($"MaterialPaste", itemPanel.transform, "Paste Edits");
                var pasteEditsLE = pasteEdits.gameObject.AddComponent<LayoutElement>();
                pasteEditsLE.minWidth = MaterialButtonWidth;
                pasteEditsLE.preferredWidth = MaterialButtonWidth;
                pasteEditsLE.flexibleWidth = 0f;

                var copy = UIUtility.CreateButton($"MaterialCopyRemove", itemPanel.transform, "Copy Material");
                var copyLE = copy.gameObject.AddComponent<LayoutElement>();
                copyLE.minWidth = MaterialButtonWidth;
                copyLE.preferredWidth = MaterialButtonWidth;
                copyLE.flexibleWidth = 0f;

                var rename = UIUtility.CreateButton($"MaterialRename", itemPanel.transform, ">");
                var renameLE = rename.gameObject.AddComponent<LayoutElement>();
                renameLE.minWidth = MaterialRenameButtonWidth;
                renameLE.preferredWidth = MaterialRenameButtonWidth;
                renameLE.flexibleWidth = 0f;
            }

            //Material Shader
            {
                var itemPanel = UIUtility.CreatePanel("ShaderPanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.color = ItemColor;
                var itemHLG = itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
                itemHLG.padding = Padding;
                itemHLG.childForceExpandWidth = false;

                var label = UIUtility.CreateText("ShaderLabel", itemPanel.transform, "");
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.black;
                var labelLE = label.gameObject.AddComponent<LayoutElement>();
                labelLE.minWidth = LabelWidth;
                labelLE.preferredWidth = LabelWidth;
                labelLE.flexibleWidth = 1f;

                CreateInterpolableButton("SelectInterpolableShaderButton", itemPanel.transform);

                Dropdown dropdownShader = UIUtility.CreateDropdown("ShaderDropdown", itemPanel.transform);
                dropdownShader.transform.SetRect(0f, 0f, 0f, 1f, 0f, 0f, 100f);
                dropdownShader.captionText.transform.SetRect(0f, 0f, 1f, 1f, 5f, 2f, -15f, -2f);
                dropdownShader.captionText.alignment = TextAnchor.MiddleLeft;
                dropdownShader.options.Clear();
                dropdownShader.options.Add(new Dropdown.OptionData("Reset"));
                foreach (var shader in MaterialEditorPluginBase.XMLShaderProperties)
                    if (shader.Key != "default")
                        dropdownShader.options.Add(new Dropdown.OptionData(shader.Key));
                var dropdownShaderLE = dropdownShader.gameObject.AddComponent<LayoutElement>();
                dropdownShaderLE.minWidth = ShaderDropdownWidth;
                dropdownShaderLE.preferredWidth = ShaderDropdownWidth;
                dropdownShaderLE.flexibleWidth = 0f;

                var reset = UIUtility.CreateButton($"ShaderResetButton", itemPanel.transform, "R");
                var resetLE = reset.gameObject.AddComponent<LayoutElement>();
                resetLE.minWidth = ResetButtonWidth;
                resetLE.preferredWidth = ResetButtonWidth;
                resetLE.flexibleWidth = 0f;
            }

            //Material RenderQueue
            {
                var itemPanel = UIUtility.CreatePanel("ShaderRenderQueuePanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.color = ItemColor;
                var itemHLG = itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
                itemHLG.padding = Padding;
                itemHLG.childForceExpandWidth = false;

                var label = UIUtility.CreateText("ShaderRenderQueueLabel", itemPanel.transform, "");
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.black;
                var labelLE = label.gameObject.AddComponent<LayoutElement>();
                labelLE.minWidth = LabelWidth;
                labelLE.preferredWidth = LabelWidth;
                labelLE.flexibleWidth = 1f;

                InputField textBoxShaderRenderQueue = UIUtility.CreateInputField("ShaderRenderQueueInput", itemPanel.transform);
                textBoxShaderRenderQueue.text = "0";
                var textBoxShaderRenderQueueLE = textBoxShaderRenderQueue.gameObject.AddComponent<LayoutElement>();
                textBoxShaderRenderQueueLE.minWidth = RenderQueueInputFieldWidth;
                textBoxShaderRenderQueueLE.preferredWidth = RenderQueueInputFieldWidth;
                textBoxShaderRenderQueueLE.flexibleWidth = 0f;

                var reset = UIUtility.CreateButton($"ShaderRenderQueueResetButton", itemPanel.transform, "R");
                var resetLE = reset.gameObject.AddComponent<LayoutElement>();
                resetLE.minWidth = ResetButtonWidth;
                resetLE.preferredWidth = ResetButtonWidth;
                resetLE.flexibleWidth = 0f;
            }

            // Property Category
            {
                var itemPanel = UIUtility.CreatePanel("PropertyCategoryPanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.color = CategoryColor;
                var itemHLG = itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
                itemHLG.padding = Padding;
                itemHLG.childForceExpandWidth = false;

                var label = UIUtility.CreateText("PropertyCategoryLabel", itemPanel.transform, "");
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.black;
                var labelLE = label.gameObject.AddComponent<LayoutElement>();
                labelLE.minWidth = LabelWidth;
                labelLE.preferredWidth = LabelWidth;
                labelLE.flexibleWidth = 1f;
            }

            //Texture properties
            {
                var itemPanel = UIUtility.CreatePanel("TexturePanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.color = ItemColor;
                var itemHLG = itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
                itemHLG.padding = Padding;
                itemHLG.childForceExpandWidth = false;

                var label = UIUtility.CreateText("TextureLabel", itemPanel.transform, "");
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.black;
                var labelLE = label.gameObject.AddComponent<LayoutElement>();
                labelLE.minWidth = LabelWidth;
                labelLE.preferredWidth = LabelWidth;
                labelLE.flexibleWidth = 1f;

                CreateInterpolableButton("SelectInterpolableTextureButton", itemPanel.transform);

                Button exportButton = UIUtility.CreateButton($"TextureExportButton", itemPanel.transform, $"Export Texture");
                var exportButtonLE = exportButton.gameObject.AddComponent<LayoutElement>();
                exportButtonLE.minWidth = TextureButtonWidth;
                exportButtonLE.preferredWidth = TextureButtonWidth;
                exportButtonLE.flexibleWidth = 0f;

                Button importButton = UIUtility.CreateButton($"TextureImportButton", itemPanel.transform, $"Import Texture");
                var importButtonLE = importButton.gameObject.AddComponent<LayoutElement>();
                importButtonLE.minWidth = TextureButtonWidth;
                importButtonLE.preferredWidth = TextureButtonWidth;
                importButtonLE.flexibleWidth = 0f;

                var reset = UIUtility.CreateButton($"TextureResetButton", itemPanel.transform, "R");
                var resetLE = reset.gameObject.AddComponent<LayoutElement>();
                resetLE.minWidth = ResetButtonWidth;
                resetLE.preferredWidth = ResetButtonWidth;
                resetLE.flexibleWidth = 0f;
            }

            //Offset and Scale
            {
                var itemPanel = UIUtility.CreatePanel("OffsetScalePanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.color = ItemColor;
                var itemHLG = itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
                itemHLG.padding = Padding;
                itemHLG.childForceExpandWidth = false;

                var label = UIUtility.CreateText("OffsetScaleLabel", itemPanel.transform, "");
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.black;
                var labelLE = label.gameObject.AddComponent<LayoutElement>();
                labelLE.minWidth = LabelWidth;
                labelLE.preferredWidth = LabelWidth;
                labelLE.flexibleWidth = 1f;

                Text emptySpace = UIUtility.CreateText("EmptySpace", itemPanel.transform, "");
                emptySpace.alignment = TextAnchor.MiddleLeft;
                var emptySpaceLE = emptySpace.gameObject.AddComponent<LayoutElement>();
                emptySpaceLE.minWidth = InterpolableButtonWidth;
                emptySpaceLE.preferredWidth = InterpolableButtonWidth;
                emptySpaceLE.flexibleWidth = 0f;

                Text labelOffsetX = UIUtility.CreateText("OffsetXText", itemPanel.transform, "OffsetX");
                labelOffsetX.alignment = TextAnchor.MiddleLeft;
                labelOffsetX.color = Color.black;
                var labelOffsetXLE = labelOffsetX.gameObject.AddComponent<LayoutElement>();
                labelOffsetXLE.minWidth = OffsetScaleLabelXWidth;
                labelOffsetXLE.preferredWidth = OffsetScaleLabelXWidth;
                labelOffsetXLE.flexibleWidth = 0f;

                InputField textBoxOffsetX = UIUtility.CreateInputField("OffsetXInput", itemPanel.transform);
                textBoxOffsetX.text = "0";
                textBoxOffsetX.characterLimit = 7;
                var textBoxOffsetXLE = textBoxOffsetX.gameObject.AddComponent<LayoutElement>();
                textBoxOffsetXLE.minWidth = OffsetScaleInputFieldWidth;
                textBoxOffsetXLE.preferredWidth = OffsetScaleInputFieldWidth;
                textBoxOffsetXLE.flexibleWidth = 0f;

                Text labelOffsetY = UIUtility.CreateText("OffsetYText", itemPanel.transform, "Y");
                labelOffsetY.alignment = TextAnchor.MiddleLeft;
                labelOffsetY.color = Color.black;
                var labelOffsetYLE = labelOffsetY.gameObject.AddComponent<LayoutElement>();
                labelOffsetYLE.minWidth = OffsetScaleLabelYWidth;
                labelOffsetYLE.preferredWidth = OffsetScaleLabelYWidth;
                labelOffsetYLE.flexibleWidth = 0f;

                InputField textBoxOffsetY = UIUtility.CreateInputField("OffsetYInput", itemPanel.transform);
                textBoxOffsetY.text = "0";
                textBoxOffsetY.characterLimit = 7;
                var textBoxOffsetYLE = textBoxOffsetY.gameObject.AddComponent<LayoutElement>();
                textBoxOffsetYLE.minWidth = OffsetScaleInputFieldWidth;
                textBoxOffsetYLE.preferredWidth = OffsetScaleInputFieldWidth;
                textBoxOffsetYLE.flexibleWidth = 0f;

                labelOffsetX.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(textBoxOffsetX, new[] { textBoxOffsetY });
                labelOffsetY.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(textBoxOffsetY, new[] { textBoxOffsetX });

                //Scale
                Text labelScaleX = UIUtility.CreateText("ScaleXText", itemPanel.transform, "ScaleX");
                labelScaleX.alignment = TextAnchor.MiddleLeft;
                labelScaleX.color = Color.black;
                var labelScaleXLE = labelScaleX.gameObject.AddComponent<LayoutElement>();
                labelScaleXLE.minWidth = OffsetScaleLabelXWidth;
                labelScaleXLE.preferredWidth = OffsetScaleLabelXWidth;
                labelScaleXLE.flexibleWidth = 0f;

                InputField textBoxScaleX = UIUtility.CreateInputField("ScaleXInput", itemPanel.transform);
                textBoxScaleX.text = "0";
                textBoxScaleX.characterLimit = 7;
                var textBoxScaleXLE = textBoxScaleX.gameObject.AddComponent<LayoutElement>();
                textBoxScaleXLE.minWidth = OffsetScaleInputFieldWidth;
                textBoxScaleXLE.preferredWidth = OffsetScaleInputFieldWidth;
                textBoxScaleXLE.flexibleWidth = 0f;

                Text labelScaleY = UIUtility.CreateText("ScaleYText", itemPanel.transform, "Y");
                labelScaleY.alignment = TextAnchor.MiddleLeft;
                labelScaleY.color = Color.black;
                var labelScaleYLE = labelScaleY.gameObject.AddComponent<LayoutElement>();
                labelScaleYLE.minWidth = OffsetScaleLabelYWidth;
                labelScaleYLE.preferredWidth = OffsetScaleLabelYWidth;
                labelScaleYLE.flexibleWidth = 0f;

                InputField textBoxScaleY = UIUtility.CreateInputField("ScaleYInput", itemPanel.transform);
                textBoxScaleY.text = "0";
                textBoxScaleY.characterLimit = 7;
                var textBoxScaleYLE = textBoxScaleY.gameObject.AddComponent<LayoutElement>();
                textBoxScaleYLE.minWidth = OffsetScaleInputFieldWidth;
                textBoxScaleYLE.preferredWidth = OffsetScaleInputFieldWidth;
                textBoxScaleYLE.flexibleWidth = 0f;

                var reset = UIUtility.CreateButton($"OffsetScaleResetButton", itemPanel.transform, "R");
                var resetLE = reset.gameObject.AddComponent<LayoutElement>();
                resetLE.minWidth = ResetButtonWidth;
                resetLE.preferredWidth = ResetButtonWidth;
                resetLE.flexibleWidth = 0f;

                labelScaleX.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(textBoxScaleX, new[] { textBoxScaleY });
                labelScaleY.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(textBoxScaleY, new[] { textBoxScaleX });
            }

            //Color properties
            {
                var itemPanel = UIUtility.CreatePanel("ColorPanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.color = ItemColor;
                var itemHLG = itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
                itemHLG.padding = Padding;
                itemHLG.childForceExpandWidth = false;

                var label = UIUtility.CreateText("ColorLabel", itemPanel.transform, "");
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.black;
                var labelLE = label.gameObject.AddComponent<LayoutElement>();
                labelLE.minWidth = LabelWidth;
                labelLE.preferredWidth = LabelWidth;
                labelLE.flexibleWidth = 1f;

                CreateInterpolableButton("SelectInterpolableColorButton", itemPanel.transform);

                Text labelR = UIUtility.CreateText("ColorRText", itemPanel.transform, "R");
                labelR.alignment = TextAnchor.MiddleLeft;
                labelR.color = Color.black;
                var labelRLE = labelR.gameObject.AddComponent<LayoutElement>();
                labelRLE.minWidth = ColorLabelWidth;
                labelRLE.preferredWidth = ColorLabelWidth;
                labelRLE.flexibleWidth = 0f;

                InputField textBoxR = UIUtility.CreateInputField("ColorRInput", itemPanel.transform);
                textBoxR.text = "0";
                textBoxR.characterLimit = 7;
                var textBoxRLE = textBoxR.gameObject.AddComponent<LayoutElement>();
                textBoxRLE.minWidth = ColorInputFieldWidth;
                textBoxRLE.preferredWidth = ColorInputFieldWidth;
                textBoxRLE.flexibleWidth = 0f;


                Text labelG = UIUtility.CreateText("ColorGText", itemPanel.transform, "G");
                labelG.alignment = TextAnchor.MiddleLeft;
                labelG.color = Color.black;
                var labelGLE = labelG.gameObject.AddComponent<LayoutElement>();
                labelGLE.minWidth = ColorLabelWidth;
                labelGLE.preferredWidth = ColorLabelWidth;
                labelGLE.flexibleWidth = 0f;

                InputField textBoxG = UIUtility.CreateInputField("ColorGInput", itemPanel.transform);
                textBoxG.text = "0";
                textBoxG.characterLimit = 7;
                var textBoxGLE = textBoxG.gameObject.AddComponent<LayoutElement>();
                textBoxGLE.minWidth = ColorInputFieldWidth;
                textBoxGLE.preferredWidth = ColorInputFieldWidth;
                textBoxGLE.flexibleWidth = 0f;

                Text labelB = UIUtility.CreateText("ColorBText", itemPanel.transform, "B");
                labelB.alignment = TextAnchor.MiddleLeft;
                labelB.color = Color.black;
                var labelBLE = labelB.gameObject.AddComponent<LayoutElement>();
                labelBLE.minWidth = ColorLabelWidth;
                labelBLE.preferredWidth = ColorLabelWidth;
                labelBLE.flexibleWidth = 0f;

                InputField textBoxB = UIUtility.CreateInputField("ColorBInput", itemPanel.transform);
                textBoxB.text = "0";
                textBoxB.characterLimit = 7;
                var textBoxBLE = textBoxB.gameObject.AddComponent<LayoutElement>();
                textBoxBLE.minWidth = ColorInputFieldWidth;
                textBoxBLE.preferredWidth = ColorInputFieldWidth;
                textBoxBLE.flexibleWidth = 0f;

                Text labelA = UIUtility.CreateText("ColorAText", itemPanel.transform, "A");
                labelA.alignment = TextAnchor.MiddleLeft;
                labelA.color = Color.black;
                var labelALE = labelA.gameObject.AddComponent<LayoutElement>();
                labelALE.minWidth = ColorLabelWidth;
                labelALE.preferredWidth = ColorLabelWidth;
                labelALE.flexibleWidth = 0f;

                InputField textBoxA = UIUtility.CreateInputField("ColorAInput", itemPanel.transform);
                textBoxA.text = "0";
                textBoxA.characterLimit = 7;
                var textBoxALE = textBoxA.gameObject.AddComponent<LayoutElement>();
                textBoxALE.minWidth = ColorInputFieldWidth;
                textBoxALE.preferredWidth = ColorInputFieldWidth;
                textBoxALE.flexibleWidth = 0f;

                var edit = UIUtility.CreateButton("ColorEditButton", itemPanel.transform, "");
                var editLE = edit.gameObject.AddComponent<LayoutElement>();
                editLE.minWidth = ColorEditButtonWidth;
                editLE.preferredWidth = ColorEditButtonWidth;
                editLE.flexibleWidth = 0f;

                var reset = UIUtility.CreateButton($"ColorResetButton", itemPanel.transform, "R");
                var resetLE = reset.gameObject.AddComponent<LayoutElement>();
                resetLE.minWidth = ResetButtonWidth;
                resetLE.preferredWidth = ResetButtonWidth;
                resetLE.flexibleWidth = 0f;

                labelR.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(textBoxR, new[] { textBoxG, textBoxB });
                labelG.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(textBoxG, new[] { textBoxR, textBoxB });
                labelB.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(textBoxB, new[] { textBoxR, textBoxG });
                labelA.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(textBoxA);
            }

            //Float properties
            {
                var itemPanel = UIUtility.CreatePanel("FloatPanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.color = ItemColor;
                var itemHLG = itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
                itemHLG.padding = Padding;
                itemHLG.childForceExpandWidth = false;

                var label = UIUtility.CreateText("FloatLabel", itemPanel.transform, "");
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.black;
                var labelLE = label.gameObject.AddComponent<LayoutElement>();
                labelLE.minWidth = LabelWidth;
                labelLE.preferredWidth = LabelWidth;
                labelLE.flexibleWidth = 1f;

                CreateInterpolableButton("SelectInterpolableFloatButton", itemPanel.transform);

                Slider sliderFloat = UIUtility.CreateSlider("FloatSlider", itemPanel.transform);
                var sliderFloatLE = sliderFloat.gameObject.AddComponent<LayoutElement>();
                sliderFloatLE.minWidth = FloatSliderWidth;
                sliderFloatLE.preferredWidth = FloatSliderWidth;
                sliderFloatLE.flexibleWidth = 0f;

                InputField textBoxFloat = UIUtility.CreateInputField("FloatInputField", itemPanel.transform);
                textBoxFloat.text = "0";
                textBoxFloat.characterLimit = 9;
                var textBoxFloatLE = textBoxFloat.gameObject.AddComponent<LayoutElement>();
                textBoxFloatLE.minWidth = FloatInputFieldWidth;
                textBoxFloatLE.preferredWidth = FloatInputFieldWidth;
                textBoxFloatLE.flexibleWidth = 0f;

                var reset = UIUtility.CreateButton($"FloatResetButton", itemPanel.transform, "R");
                var resetLE = reset.gameObject.AddComponent<LayoutElement>();
                resetLE.minWidth = ResetButtonWidth;
                resetLE.preferredWidth = ResetButtonWidth;
                resetLE.flexibleWidth = 0f;
                label.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(textBoxFloat);
            }

            //Keyword properties
            {
                var itemPanel = UIUtility.CreatePanel("KeywordPanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.color = ItemColor;
                var itemHLG = itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
                itemHLG.padding = Padding;
                itemHLG.childForceExpandWidth = false;

                var label = UIUtility.CreateText("KeywordLabel", itemPanel.transform, "");
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.black;
                var labelLE = label.gameObject.AddComponent<LayoutElement>();
                labelLE.minWidth = LabelWidth;
                labelLE.preferredWidth = LabelWidth;
                labelLE.flexibleWidth = 1f;

                Text emptySpace = UIUtility.CreateText("EmptySpace", itemPanel.transform, "");
                emptySpace.alignment = TextAnchor.MiddleLeft;
                var emptySpaceLE = emptySpace.gameObject.AddComponent<LayoutElement>();
                emptySpaceLE.minWidth = InterpolableButtonWidth;
                emptySpaceLE.preferredWidth = InterpolableButtonWidth;
                emptySpaceLE.flexibleWidth = 0f;

                Toggle toggleKeyword = UIUtility.CreateToggle("KeywordToggle", itemPanel.transform, "");
                var toggleKeywordLE = toggleKeyword.gameObject.AddComponent<LayoutElement>();
                toggleKeywordLE.minWidth = KeywordToggleWidth;
                toggleKeywordLE.preferredWidth = KeywordToggleWidth;
                toggleKeywordLE.flexibleWidth = 0f;

                var reset = UIUtility.CreateButton($"KeywordResetButton", itemPanel.transform, "R");
                var resetLE = reset.gameObject.AddComponent<LayoutElement>();
                resetLE.minWidth = ResetButtonWidth;
                resetLE.preferredWidth = ResetButtonWidth;
                resetLE.flexibleWidth = 0f;
            }

            return contentList.gameObject;
        }

        private static void CreateInterpolableButton(string objectName, Transform parent)
        {
            Button interpolableButton = UIUtility.CreateButton(objectName, parent, "O");
            var sinterpolableButtonLE = interpolableButton.gameObject.AddComponent<LayoutElement>();
            sinterpolableButtonLE.minWidth = InterpolableButtonWidth;
            sinterpolableButtonLE.preferredWidth = InterpolableButtonWidth;
            sinterpolableButtonLE.flexibleWidth = 0f;
            interpolableButton.gameObject.SetActive(false);

#if !API && !EC
            if (TimelineCompatibilityHelper.IsTimelineAvailable())
                interpolableButton.gameObject.SetActive(true);
#endif
        }
    }
}
