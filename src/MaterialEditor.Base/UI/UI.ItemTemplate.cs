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
                itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>().padding = Padding;
                itemPanel.color = SeparatorItemColor;

                var label = UIUtility.CreateText("RendererLabel", itemPanel.transform, "");
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.black;
                var labelLE = label.gameObject.AddComponent<LayoutElement>();
                labelLE.preferredWidth = LabelWidth;
                labelLE.flexibleWidth = LabelWidth;

                Text labelRenderer = UIUtility.CreateText("RendererText", itemPanel.transform);
                labelRenderer.alignment = TextAnchor.MiddleRight;
                labelRenderer.color = Color.black;
                var labelRendererLE = labelRenderer.gameObject.AddComponent<LayoutElement>();
                labelRendererLE.preferredWidth = 200;
                labelRendererLE.flexibleWidth = 0;

                Button exportUVButton = UIUtility.CreateButton("ExportUVButton", itemPanel.transform, "Export UV Map");
                var exportUVButtonLE = exportUVButton.gameObject.AddComponent<LayoutElement>();
                exportUVButtonLE.preferredWidth = 110;
                exportUVButtonLE.flexibleWidth = 0;

                Button exportMeshButton = UIUtility.CreateButton("ExportObjButton", itemPanel.transform, "Export .obj");
                var exportMeshButtonLE = exportMeshButton.gameObject.AddComponent<LayoutElement>();
                exportMeshButtonLE.preferredWidth = 85;
                exportMeshButtonLE.flexibleWidth = 0;
            }

            //Renderer Enabled
            {
                var itemPanel = UIUtility.CreatePanel("RendererEnabledPanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>().padding = Padding;
                itemPanel.color = ItemColor;

                var label = UIUtility.CreateText("RendererEnabledLabel", itemPanel.transform, "");
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.black;
                var labelLE = label.gameObject.AddComponent<LayoutElement>();
                labelLE.preferredWidth = LabelWidth;
                labelLE.flexibleWidth = LabelWidth;

                Dropdown dropdownEnabled = UIUtility.CreateDropdown("RendererEnabledDropdown", itemPanel.transform);
                dropdownEnabled.transform.SetRect(0f, 0f, 0f, 1f, 0f, 0f, 100f);
                dropdownEnabled.captionText.transform.SetRect(0f, 0f, 1f, 1f, 5f, 2f, -15f, -2f);
                dropdownEnabled.captionText.alignment = TextAnchor.MiddleLeft;
                dropdownEnabled.options.Clear();
                dropdownEnabled.options.Add(new Dropdown.OptionData("Off"));
                dropdownEnabled.options.Add(new Dropdown.OptionData("On"));
                dropdownEnabled.value = 0;
                dropdownEnabled.captionText.text = "Off";
                var dropdownEnabledLE = dropdownEnabled.gameObject.AddComponent<LayoutElement>();
                dropdownEnabledLE.preferredWidth = DropdownWidth;
                dropdownEnabledLE.flexibleWidth = 0;

                var reset = UIUtility.CreateButton($"RendererEnabledResetButton", itemPanel.transform, "Reset");
                var resetLE = reset.gameObject.AddComponent<LayoutElement>();
                resetLE.preferredWidth = ResetButtonWidth;
                resetLE.flexibleWidth = 0;
            }

            //Renderer ShadowCastingMode
            {
                var itemPanel = UIUtility.CreatePanel("RendererShadowCastingModePanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>().padding = Padding;
                itemPanel.color = ItemColor;

                var label = UIUtility.CreateText("RendererShadowCastingModeLabel", itemPanel.transform, "");
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.black;
                var labelLE = label.gameObject.AddComponent<LayoutElement>();
                labelLE.preferredWidth = LabelWidth;
                labelLE.flexibleWidth = LabelWidth;

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
                dropdownShadowCastingModeLE.preferredWidth = DropdownWidth;
                dropdownShadowCastingModeLE.flexibleWidth = 0;

                var reset = UIUtility.CreateButton($"RendererShadowCastingModeResetButton", itemPanel.transform, "Reset");
                var resetLE = reset.gameObject.AddComponent<LayoutElement>();
                resetLE.preferredWidth = ResetButtonWidth;
                resetLE.flexibleWidth = 0;
            }

            //Renderer ReceiveShadows
            {
                var itemPanel = UIUtility.CreatePanel("RendererReceiveShadowsPanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>().padding = Padding;
                itemPanel.color = ItemColor;

                var label = UIUtility.CreateText("RendererReceiveShadowsLabel", itemPanel.transform, "");
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.black;
                var labelLE = label.gameObject.AddComponent<LayoutElement>();
                labelLE.preferredWidth = LabelWidth;
                labelLE.flexibleWidth = LabelWidth;

                Dropdown dropdownReceiveShadows = UIUtility.CreateDropdown("RendererReceiveShadowsDropdown", itemPanel.transform);
                dropdownReceiveShadows.transform.SetRect(0f, 0f, 0f, 1f, 0f, 0f, 100f);
                dropdownReceiveShadows.captionText.transform.SetRect(0f, 0f, 1f, 1f, 5f, 2f, -15f, -2f);
                dropdownReceiveShadows.captionText.alignment = TextAnchor.MiddleLeft;
                dropdownReceiveShadows.options.Clear();
                dropdownReceiveShadows.options.Add(new Dropdown.OptionData("Off"));
                dropdownReceiveShadows.options.Add(new Dropdown.OptionData("On"));
                dropdownReceiveShadows.value = 0;
                dropdownReceiveShadows.captionText.text = "Off";
                var dropdownReceiveShadowsLE = dropdownReceiveShadows.gameObject.AddComponent<LayoutElement>();
                dropdownReceiveShadowsLE.preferredWidth = DropdownWidth;
                dropdownReceiveShadowsLE.flexibleWidth = 0;

                var reset = UIUtility.CreateButton($"RendererReceiveShadowsResetButton", itemPanel.transform, "Reset");
                var resetLE = reset.gameObject.AddComponent<LayoutElement>();
                resetLE.preferredWidth = ResetButtonWidth;
                resetLE.flexibleWidth = 0;
            }

            //Material
            {
                var itemPanel = UIUtility.CreatePanel("MaterialPanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>().padding = Padding;
                itemPanel.color = SeparatorItemColor;

                var label = UIUtility.CreateText("MaterialLabel", itemPanel.transform, "");
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.black;
                var labelLE = label.gameObject.AddComponent<LayoutElement>();
                labelLE.preferredWidth = LabelWidth;
                labelLE.flexibleWidth = LabelWidth;

                Text materialText = UIUtility.CreateText("MaterialText", itemPanel.transform);
                materialText.alignment = TextAnchor.MiddleRight;
                materialText.color = Color.black;
                var materialTextLE = materialText.gameObject.AddComponent<LayoutElement>();
                materialTextLE.preferredWidth = 200;
                materialTextLE.flexibleWidth = 0;

                var copyEdits = UIUtility.CreateButton($"MaterialCopy", itemPanel.transform, "Copy Edits");
                var copyEditsLE = copyEdits.gameObject.AddComponent<LayoutElement>();
                copyEditsLE.preferredWidth = ButtonWidth;
                copyEditsLE.flexibleWidth = 0;

                var pasteEdits = UIUtility.CreateButton($"MaterialPaste", itemPanel.transform, "Paste Edits");
                var pasteEditsLE = pasteEdits.gameObject.AddComponent<LayoutElement>();
                pasteEditsLE.preferredWidth = ButtonWidth;
                pasteEditsLE.flexibleWidth = 0;

                var copy = UIUtility.CreateButton($"MaterialCopyRemove", itemPanel.transform, "Copy Material");
                var copyLE = copy.gameObject.AddComponent<LayoutElement>();
                copyLE.preferredWidth = ButtonWidth;
                copyLE.flexibleWidth = 0;
                copy.gameObject.SetActive(false);
            }

            //Material Shader
            {
                var itemPanel = UIUtility.CreatePanel("ShaderPanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>().padding = Padding;
                itemPanel.color = ItemColor;

                var label = UIUtility.CreateText("ShaderLabel", itemPanel.transform, "");
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.black;
                var labelLE = label.gameObject.AddComponent<LayoutElement>();
                labelLE.preferredWidth = LabelWidth;
                labelLE.flexibleWidth = LabelWidth;

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
                dropdownShaderLE.preferredWidth = DropdownWidth * 3;
                dropdownShaderLE.flexibleWidth = 0;

                var reset = UIUtility.CreateButton($"ShaderResetButton", itemPanel.transform, "Reset");
                var resetLE = reset.gameObject.AddComponent<LayoutElement>();
                resetLE.preferredWidth = ResetButtonWidth;
                resetLE.flexibleWidth = 0;
            }

            //Material RenderQueue
            {
                var itemPanel = UIUtility.CreatePanel("ShaderRenderQueuePanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>().padding = Padding;
                itemPanel.color = ItemColor;

                var label = UIUtility.CreateText("ShaderRenderQueueLabel", itemPanel.transform, "");
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.black;
                var labelLE = label.gameObject.AddComponent<LayoutElement>();
                labelLE.preferredWidth = LabelWidth;
                labelLE.flexibleWidth = LabelWidth;

                InputField textBoxShaderRenderQueue = UIUtility.CreateInputField("ShaderRenderQueueInput", itemPanel.transform);
                textBoxShaderRenderQueue.text = "0";
                var textBoxShaderRenderQueueLE = textBoxShaderRenderQueue.gameObject.AddComponent<LayoutElement>();
                textBoxShaderRenderQueueLE.preferredWidth = TextBoxWidth;
                textBoxShaderRenderQueueLE.flexibleWidth = 0;

                var reset = UIUtility.CreateButton($"ShaderRenderQueueResetButton", itemPanel.transform, "Reset");
                var resetLE = reset.gameObject.AddComponent<LayoutElement>();
                resetLE.preferredWidth = ResetButtonWidth;
                resetLE.flexibleWidth = 0;
            }

            //Texture properties
            {
                var itemPanel = UIUtility.CreatePanel("TexturePanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>().padding = Padding;
                itemPanel.color = ItemColor;

                var label = UIUtility.CreateText("TextureLabel", itemPanel.transform, "");
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.black;
                var labelLE = label.gameObject.AddComponent<LayoutElement>();
                labelLE.preferredWidth = LabelWidth;
                labelLE.flexibleWidth = LabelWidth;

                Button exportButton = UIUtility.CreateButton($"TextureExportButton", itemPanel.transform, $"Export Texture");
                var exportButtonLE = exportButton.gameObject.AddComponent<LayoutElement>();
                exportButtonLE.preferredWidth = ButtonWidth;
                exportButtonLE.flexibleWidth = 0;

                Button importButton = UIUtility.CreateButton($"TextureImportButton", itemPanel.transform, $"Import Texture");
                var importButtonLE = importButton.gameObject.AddComponent<LayoutElement>();
                importButtonLE.preferredWidth = ButtonWidth;
                importButtonLE.flexibleWidth = 0;

                var reset = UIUtility.CreateButton($"TextureResetButton", itemPanel.transform, "Reset");
                var resetLE = reset.gameObject.AddComponent<LayoutElement>();
                resetLE.preferredWidth = ResetButtonWidth;
                resetLE.flexibleWidth = 0;
            }

            //Offset and Scale
            {
                var itemPanel = UIUtility.CreatePanel("OffsetScalePanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>().padding = Padding;
                itemPanel.color = ItemColor;

                var label = UIUtility.CreateText("OffsetScaleLabel", itemPanel.transform, "");
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.black;
                var labelLE = label.gameObject.AddComponent<LayoutElement>();
                labelLE.preferredWidth = LabelWidth;
                labelLE.flexibleWidth = LabelWidth;

                Text labelOffsetX = UIUtility.CreateText("OffsetXText", itemPanel.transform, "Offset X");
                labelOffsetX.alignment = TextAnchor.MiddleLeft;
                labelOffsetX.color = Color.black;
                var labelOffsetXLE = labelOffsetX.gameObject.AddComponent<LayoutElement>();
                labelOffsetXLE.preferredWidth = LabelXWidth;
                labelOffsetXLE.flexibleWidth = 0;

                InputField textBoxOffsetX = UIUtility.CreateInputField("OffsetXInput", itemPanel.transform);
                textBoxOffsetX.text = "0";
                var textBoxOffsetXLE = textBoxOffsetX.gameObject.AddComponent<LayoutElement>();
                textBoxOffsetXLE.preferredWidth = TextBoxXYWidth;
                textBoxOffsetXLE.flexibleWidth = 0;

                Text labelOffsetY = UIUtility.CreateText("OffsetYText", itemPanel.transform, "Y");
                labelOffsetY.alignment = TextAnchor.MiddleLeft;
                labelOffsetY.color = Color.black;
                var labelOffsetYLE = labelOffsetY.gameObject.AddComponent<LayoutElement>();
                labelOffsetYLE.preferredWidth = LabelYWidth;
                labelOffsetYLE.flexibleWidth = 0;

                InputField textBoxOffsetY = UIUtility.CreateInputField("OffsetYInput", itemPanel.transform);
                textBoxOffsetY.text = "0";
                var textBoxOffsetYLE = textBoxOffsetY.gameObject.AddComponent<LayoutElement>();
                textBoxOffsetYLE.preferredWidth = TextBoxXYWidth;
                textBoxOffsetYLE.flexibleWidth = 0;

                //Scale
                Text labelScaleX = UIUtility.CreateText("ScaleXText", itemPanel.transform, "Scale X");
                labelScaleX.alignment = TextAnchor.MiddleLeft;
                labelScaleX.color = Color.black;
                var labelScaleXLE = labelScaleX.gameObject.AddComponent<LayoutElement>();
                labelScaleXLE.preferredWidth = LabelXWidth;
                labelScaleXLE.flexibleWidth = 0;

                InputField textBoxScaleX = UIUtility.CreateInputField("ScaleXInput", itemPanel.transform);
                textBoxScaleX.text = "0";
                var textBoxScaleXLE = textBoxScaleX.gameObject.AddComponent<LayoutElement>();
                textBoxScaleXLE.preferredWidth = TextBoxXYWidth;
                textBoxScaleXLE.flexibleWidth = 0;

                Text labelScaleY = UIUtility.CreateText("ScaleYText", itemPanel.transform, "Y");
                labelScaleY.alignment = TextAnchor.MiddleLeft;
                labelScaleY.color = Color.black;
                var labelScaleYLE = labelScaleY.gameObject.AddComponent<LayoutElement>();
                labelScaleYLE.preferredWidth = LabelYWidth;
                labelScaleYLE.flexibleWidth = 0;

                InputField textBoxScaleY = UIUtility.CreateInputField("ScaleYInput", itemPanel.transform);
                textBoxScaleY.text = "0";
                var textBoxScaleYLE = textBoxScaleY.gameObject.AddComponent<LayoutElement>();
                textBoxScaleYLE.preferredWidth = TextBoxXYWidth;
                textBoxScaleYLE.flexibleWidth = 0;

                var reset = UIUtility.CreateButton($"OffsetScaleResetButton", itemPanel.transform, "Reset");
                var resetLE = reset.gameObject.AddComponent<LayoutElement>();
                resetLE.preferredWidth = ResetButtonWidth;
                resetLE.flexibleWidth = 0;
            }

            //Color properties
            {
                var itemPanel = UIUtility.CreatePanel("ColorPanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>().padding = Padding;
                itemPanel.color = ItemColor;

                var label = UIUtility.CreateText("ColorLabel", itemPanel.transform, "");
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.black;
                var labelLE = label.gameObject.AddComponent<LayoutElement>();
                labelLE.preferredWidth = LabelWidth;
                labelLE.flexibleWidth = LabelWidth;

                Text labelR = UIUtility.CreateText("ColorRText", itemPanel.transform, "R");
                labelR.alignment = TextAnchor.MiddleLeft;
                labelR.color = Color.black;
                var labelRLE = labelR.gameObject.AddComponent<LayoutElement>();
                labelRLE.preferredWidth = ColorLabelWidth;
                labelRLE.flexibleWidth = 0;

                InputField textBoxR = UIUtility.CreateInputField("ColorRInput", itemPanel.transform);
                textBoxR.text = "0";
                var textBoxRLE = textBoxR.gameObject.AddComponent<LayoutElement>();
                textBoxRLE.preferredWidth = TextBoxWidth;
                textBoxRLE.flexibleWidth = 0;

                Text labelG = UIUtility.CreateText("ColorGText", itemPanel.transform, "G");
                labelG.alignment = TextAnchor.MiddleLeft;
                labelG.color = Color.black;
                var labelGLE = labelG.gameObject.AddComponent<LayoutElement>();
                labelGLE.preferredWidth = ColorLabelWidth;
                labelGLE.flexibleWidth = 0;

                InputField textBoxG = UIUtility.CreateInputField("ColorGInput", itemPanel.transform);
                textBoxG.text = "0";
                var textBoxGLE = textBoxG.gameObject.AddComponent<LayoutElement>();
                textBoxGLE.preferredWidth = TextBoxWidth;
                textBoxGLE.flexibleWidth = 0;

                Text labelB = UIUtility.CreateText("ColorBText", itemPanel.transform, "B");
                labelB.alignment = TextAnchor.MiddleLeft;
                labelB.color = Color.black;
                var labelBLE = labelB.gameObject.AddComponent<LayoutElement>();
                labelBLE.preferredWidth = ColorLabelWidth;
                labelBLE.flexibleWidth = 0;

                InputField textBoxB = UIUtility.CreateInputField("ColorBInput", itemPanel.transform);
                textBoxB.text = "0";
                var textBoxBLE = textBoxB.gameObject.AddComponent<LayoutElement>();
                textBoxBLE.preferredWidth = TextBoxWidth;
                textBoxBLE.flexibleWidth = 0;

                Text labelA = UIUtility.CreateText("ColorAText", itemPanel.transform, "A");
                labelA.alignment = TextAnchor.MiddleLeft;
                labelA.color = Color.black;
                var labelALE = labelA.gameObject.AddComponent<LayoutElement>();
                labelALE.preferredWidth = ColorLabelWidth;
                labelALE.flexibleWidth = 0;

                InputField textBoxA = UIUtility.CreateInputField("ColorAInput", itemPanel.transform);
                textBoxA.text = "0";
                var textBoxALE = textBoxA.gameObject.AddComponent<LayoutElement>();
                textBoxALE.preferredWidth = TextBoxWidth;
                textBoxALE.flexibleWidth = 0;

                var reset = UIUtility.CreateButton($"ColorResetButton", itemPanel.transform, "Reset");
                var resetLE = reset.gameObject.AddComponent<LayoutElement>();
                resetLE.preferredWidth = ResetButtonWidth;
                resetLE.flexibleWidth = 0;
            }

            //Float properties
            {
                var itemPanel = UIUtility.CreatePanel("FloatPanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>().padding = Padding;
                itemPanel.color = ItemColor;

                var label = UIUtility.CreateText("FloatLabel", itemPanel.transform, "");
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.black;
                var labelLE = label.gameObject.AddComponent<LayoutElement>();
                labelLE.preferredWidth = LabelWidth;
                labelLE.flexibleWidth = LabelWidth;

                Slider sliderFloat = UIUtility.CreateSlider("FloatSlider", itemPanel.transform);
                var sliderFloatLE = sliderFloat.gameObject.AddComponent<LayoutElement>();
                sliderFloatLE.preferredWidth = SliderWidth;
                sliderFloatLE.flexibleWidth = 0;

                InputField textBoxFloat = UIUtility.CreateInputField("FloatInputField", itemPanel.transform);
                textBoxFloat.text = "0";
                var textBoxFloatLE = textBoxFloat.gameObject.AddComponent<LayoutElement>();
                textBoxFloatLE.preferredWidth = TextBoxWidth;
                textBoxFloatLE.flexibleWidth = 0;

                var reset = UIUtility.CreateButton($"FloatResetButton", itemPanel.transform, "Reset");
                var resetLE = reset.gameObject.AddComponent<LayoutElement>();
                resetLE.preferredWidth = ResetButtonWidth;
                resetLE.flexibleWidth = 0;
            }

            return contentList.gameObject;
        }
    }
}