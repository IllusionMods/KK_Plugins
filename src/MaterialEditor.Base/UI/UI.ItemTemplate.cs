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
                TooltipManager.AddTooltip(labelRenderer.gameObject, "Renderer name");

                CreateInterpolableButton("SelectInterpolableRendererButton", itemPanel.transform, "Select the properties (Enabled, Shadow casting mode and Receive shadows) of the currently selected renderer as interpolables in timeline");

                Button exportUVButton = UIUtility.CreateButton("ExportUVButton", itemPanel.transform, "Export UV Map");
                var exportUVButtonLE = exportUVButton.gameObject.AddComponent<LayoutElement>();
                exportUVButtonLE.minWidth = RendererButtonWidth;
                exportUVButtonLE.preferredWidth = RendererButtonWidth;
                exportUVButtonLE.flexibleWidth = 0f;
                TooltipManager.AddTooltip(exportUVButton.gameObject, "Export the UV map of this renderer.\n\nThe UV map is the 2d projection of the renderer with which to map textures to the 3d model. You can use this UV map as a guide to drawing on textures");

                Button exportMeshButton = UIUtility.CreateButton("ExportObjButton", itemPanel.transform, "Export .obj");
                var exportMeshButtonLE = exportMeshButton.gameObject.AddComponent<LayoutElement>();
                exportMeshButtonLE.minWidth = RendererButtonWidth;
                exportMeshButtonLE.preferredWidth = RendererButtonWidth;
                exportMeshButtonLE.flexibleWidth = 0f;
                TooltipManager.AddTooltip(exportMeshButton.gameObject, "Export the renderer as a .obj.\n\nYou can use the <i>ExportBakedMesh</i> and <i>ExportBakedWorldPosition</i> config options to change the exporting behaviour");
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
                TooltipManager.AddTooltip(toggleEnabled.gameObject, "Toggle the visibility of this renderer on/off");

                var reset = UIUtility.CreateButton($"RendererEnabledResetButton", itemPanel.transform, "Reset");
                var resetLE = reset.gameObject.AddComponent<LayoutElement>();
                resetLE.minWidth = ResetButtonWidth;
                resetLE.preferredWidth = ResetButtonWidth;
                resetLE.flexibleWidth = 0f;
                TooltipManager.AddTooltip(reset.gameObject, "Reset this property to its original value");
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
                TooltipManager.AddTooltip(dropdownShadowCastingMode.gameObject, @"- Off: Renderer casts no shadows
- On: Renderer casts shadows
- Two Sided: Always cast shadows from any direction, even for single sided objects
- Shadows Only: Renderer is invisible but still casts shadows");

                var reset = UIUtility.CreateButton($"RendererShadowCastingModeResetButton", itemPanel.transform, "Reset");
                var resetLE = reset.gameObject.AddComponent<LayoutElement>();
                resetLE.minWidth = ResetButtonWidth;
                resetLE.preferredWidth = ResetButtonWidth;
                resetLE.flexibleWidth = 0f;
                TooltipManager.AddTooltip(reset.gameObject, "Reset this property to its original value");
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
                TooltipManager.AddTooltip(toggleReceiveShadows.gameObject, "Toggle if the renderer can have shadows cast on it on/off");

                var reset = UIUtility.CreateButton($"RendererReceiveShadowsResetButton", itemPanel.transform, "Reset");
                var resetLE = reset.gameObject.AddComponent<LayoutElement>();
                resetLE.minWidth = ResetButtonWidth;
                resetLE.preferredWidth = ResetButtonWidth;
                resetLE.flexibleWidth = 0f;
                TooltipManager.AddTooltip(reset.gameObject, "Reset this property to its original value");
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
                TooltipManager.AddTooltip(toggleRendererUpdateWhenOffscreen.gameObject, "When on, a renderer will always stay renderer, even when considered to be off-screen.\n\n This is handy for when the bounding box of an object is configured improperly and dissapears when it should still be visible");

                var reset = UIUtility.CreateButton($"RendererUpdateWhenOffscreenResetButton", itemPanel.transform, "Reset");
                var resetLE = reset.gameObject.AddComponent<LayoutElement>();
                resetLE.minWidth = ResetButtonWidth;
                resetLE.preferredWidth = ResetButtonWidth;
                resetLE.flexibleWidth = 0f;
                TooltipManager.AddTooltip(reset.gameObject, "Reset this property to its original value");
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
                TooltipManager.AddTooltip(toggleRecalculateNormals.gameObject, "Recalculate the normals of this renderer based on its current shape, instead of its original shape.\n\nOnly available on skinned mesh renderers");

                var reset = UIUtility.CreateButton($"RendererRecalculateNormalsResetButton", itemPanel.transform, "Reset");
                var resetLE = reset.gameObject.AddComponent<LayoutElement>();
                resetLE.minWidth = ResetButtonWidth;
                resetLE.preferredWidth = ResetButtonWidth;
                resetLE.flexibleWidth = 0f;
                TooltipManager.AddTooltip(reset.gameObject, "Reset this property to its original value.\n\nIn order for the reset to take effect you need to either save and re-load the scene, or copy the object and delete the old one");
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
                TooltipManager.AddTooltip(labelMaterial.gameObject, "Material name");

                var copyEdits = UIUtility.CreateButton($"MaterialCopy", itemPanel.transform, "Copy Edits");
                var copyEditsLE = copyEdits.gameObject.AddComponent<LayoutElement>();
                copyEditsLE.minWidth = MaterialButtonWidth;
                copyEditsLE.preferredWidth = MaterialButtonWidth;
                copyEditsLE.flexibleWidth = 0f;
                TooltipManager.AddTooltip(copyEdits.gameObject, "Copy all the <b>edits</b> of this material");

                var pasteEdits = UIUtility.CreateButton($"MaterialPaste", itemPanel.transform, "Paste Edits");
                var pasteEditsLE = pasteEdits.gameObject.AddComponent<LayoutElement>();
                pasteEditsLE.minWidth = MaterialButtonWidth;
                pasteEditsLE.preferredWidth = MaterialButtonWidth;
                pasteEditsLE.flexibleWidth = 0f;
                TooltipManager.AddTooltip(pasteEdits.gameObject, "Paste all the copied edits");

                var copy = UIUtility.CreateButton($"MaterialCopyRemove", itemPanel.transform, "Copy Material");
                var copyLE = copy.gameObject.AddComponent<LayoutElement>();
                copyLE.minWidth = MaterialButtonWidth;
                copyLE.preferredWidth = MaterialButtonWidth;
                copyLE.flexibleWidth = 0f;
                TooltipManager.AddTooltip(copy.gameObject, "Make a copy of this material.\n\nUseful for overlaying different effects onto an object with different material shaders/properties");

                var rename = UIUtility.CreateButton($"MaterialRename", itemPanel.transform, ">");
                var renameLE = rename.gameObject.AddComponent<LayoutElement>();
                renameLE.minWidth = MaterialRenameButtonWidth;
                renameLE.preferredWidth = MaterialRenameButtonWidth;
                renameLE.flexibleWidth = 0f;
                TooltipManager.AddTooltip(rename.gameObject, "Rename material instances");
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

                CreateInterpolableButton("SelectInterpolableShaderButton", itemPanel.transform, "Select the currently selected shader property and its render queue as interpolables in timeline");

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

                var reset = UIUtility.CreateButton($"ShaderResetButton", itemPanel.transform, "Reset");
                var resetLE = reset.gameObject.AddComponent<LayoutElement>();
                resetLE.minWidth = ResetButtonWidth;
                resetLE.preferredWidth = ResetButtonWidth;
                resetLE.flexibleWidth = 0f;
                TooltipManager.AddTooltip(reset.gameObject, "Reset this property to its original value.\n\nIf the original shader is not one known by Material Editor, it will not be able to reset the shader to its original value. In order for the reset to take effect you to either save and re-load the scene, or copy the object and delete the old one");
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
                TooltipManager.AddTooltip(textBoxShaderRenderQueue.gameObject, "The order in which a material is rendered. Higher render queues get rendered later");

                var reset = UIUtility.CreateButton($"ShaderRenderQueueResetButton", itemPanel.transform, "Reset");
                var resetLE = reset.gameObject.AddComponent<LayoutElement>();
                resetLE.minWidth = ResetButtonWidth;
                resetLE.preferredWidth = ResetButtonWidth;
                resetLE.flexibleWidth = 0f;
                TooltipManager.AddTooltip(reset.gameObject, "Reset this property to its original value");
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
                TooltipManager.AddTooltip(label.gameObject, "Category name");
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

                CreateInterpolableButton("SelectInterpolableTextureButton", itemPanel.transform, "Select the currently selected texture property and its offset and scale properties as interpolables in timeline");

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

                var reset = UIUtility.CreateButton($"TextureResetButton", itemPanel.transform, "Reset");
                var resetLE = reset.gameObject.AddComponent<LayoutElement>();
                resetLE.minWidth = ResetButtonWidth;
                resetLE.preferredWidth = ResetButtonWidth;
                resetLE.flexibleWidth = 0f;
                TooltipManager.AddTooltip(reset.gameObject, "Reset this property to its original value.\n\nIn order for the reset to take effect you need to either save and re-load the scene, or copy the object and delete the old one");
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
                TooltipManager.AddTooltip(textBoxOffsetX.gameObject, "Adjust the horizontal offset of the texture. It can move the texture left or right.");

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
                TooltipManager.AddTooltip(textBoxOffsetY.gameObject, "Adjust the vertical offset of the texture. It can move the texture up or down.");

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
                TooltipManager.AddTooltip(textBoxScaleX.gameObject, "Adjust the horizontal scale of the texture. Values greater than 1 make the texture appear smaller horizontally, values less than 1 make it appear larger horizontally.");

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
                TooltipManager.AddTooltip(textBoxScaleY.gameObject, "Adjust the vertical scale of the texture. Values greater than 1 make the texture appear smaller vertically, values less than 1 make it appear larger vertically.");

                var reset = UIUtility.CreateButton($"OffsetScaleResetButton", itemPanel.transform, "Reset");
                var resetLE = reset.gameObject.AddComponent<LayoutElement>();
                resetLE.minWidth = ResetButtonWidth;
                resetLE.preferredWidth = ResetButtonWidth;
                resetLE.flexibleWidth = 0f;
                TooltipManager.AddTooltip(reset.gameObject, "Reset both the scale and offset properties to their original values");

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

                CreateInterpolableButton("SelectInterpolableColorButton", itemPanel.transform, "Select currently selected color property as interpolable in timeline");

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

                var reset = UIUtility.CreateButton($"ColorResetButton", itemPanel.transform, "Reset");
                var resetLE = reset.gameObject.AddComponent<LayoutElement>();
                resetLE.minWidth = ResetButtonWidth;
                resetLE.preferredWidth = ResetButtonWidth;
                resetLE.flexibleWidth = 0f;
                TooltipManager.AddTooltip(reset.gameObject, "Reset the selected property to its original value");

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

                CreateInterpolableButton("SelectInterpolableFloatButton", itemPanel.transform, "Select currently selected float property as interpolable in timeline");

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

                var reset = UIUtility.CreateButton($"FloatResetButton", itemPanel.transform, "Reset");
                var resetLE = reset.gameObject.AddComponent<LayoutElement>();
                resetLE.minWidth = ResetButtonWidth;
                resetLE.preferredWidth = ResetButtonWidth;
                resetLE.flexibleWidth = 0f;
                TooltipManager.AddTooltip(reset.gameObject, "Reset the selected property to its original value");
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

                var reset = UIUtility.CreateButton($"KeywordResetButton", itemPanel.transform, "Reset");
                var resetLE = reset.gameObject.AddComponent<LayoutElement>();
                resetLE.minWidth = ResetButtonWidth;
                resetLE.preferredWidth = ResetButtonWidth;
                resetLE.flexibleWidth = 0f;
                TooltipManager.AddTooltip(reset.gameObject, "Reset the selected property to its original value");
            }

            return contentList.gameObject;
        }

        private static void CreateInterpolableButton(string objectName, Transform parent, string tooltipText)
        {
            Button interpolableButton = UIUtility.CreateButton(objectName, parent, "O");
            var sinterpolableButtonLE = interpolableButton.gameObject.AddComponent<LayoutElement>();
            sinterpolableButtonLE.minWidth = InterpolableButtonWidth;
            sinterpolableButtonLE.preferredWidth = InterpolableButtonWidth;
            sinterpolableButtonLE.flexibleWidth = 0f;
            interpolableButton.gameObject.SetActive(false);
            TooltipManager.AddTooltip(interpolableButton.gameObject, tooltipText);

#if !API && !EC
            if (TimelineCompatibilityHelper.IsTimelineAvailable())
                interpolableButton.gameObject.SetActive(true);
#endif
        }
    }
}
