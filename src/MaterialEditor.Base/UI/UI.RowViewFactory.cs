using UILib;
using UnityEngine;
using UnityEngine.UI;
using static MaterialEditorAPI.MaterialEditorUI;

namespace MaterialEditorAPI
{
    // Builds the row hierarchy. Existing object names are retained for plugin compatibility.
    internal static class RowViewFactory
    {
        internal static GameObject CreateTemplate(Transform parent)
        {
            var contentList = MaterialEditorControlFactory.CreatePanel("ListEntry", parent);
            contentList.gameObject.AddComponent<LayoutElement>().preferredHeight = PanelHeight;
            contentList.gameObject.AddComponent<Mask>();
            contentList.color = RowColor;

            //Renderer
            {
                var itemPanel = MaterialEditorControlFactory.CreatePanel("RendererPanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.color = RendererColor;
                var itemHLG = itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
                itemHLG.padding = Padding;
                itemHLG.childForceExpandWidth = false;
                itemHLG.childAlignment = TextAnchor.MiddleLeft;

                var label = MaterialEditorControlFactory.CreateText("RendererLabel", itemPanel.transform, "");
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.black;
                var labelLE = label.gameObject.AddComponent<LayoutElement>();
                labelLE.minWidth = 0f;
                labelLE.preferredWidth = 0f;
                labelLE.flexibleWidth = 0f;

                Text labelRenderer = MaterialEditorControlFactory.CreateText("RendererText", itemPanel.transform);
                labelRenderer.gameObject.AddComponent<LabelClickTrigger>();
                labelRenderer.alignment = TextAnchor.MiddleLeft;
                labelRenderer.color = Color.black;
                var labelRendererLE = labelRenderer.gameObject.AddComponent<LayoutElement>();
                labelRendererLE.minWidth = LabelWidth;
                labelRendererLE.preferredWidth = LabelWidth;
                labelRendererLE.flexibleWidth = 1f;
                TooltipManager.AddTooltip(labelRenderer.gameObject, "Renderer name");

                CreateInterpolableButton("SelectInterpolableRendererButton", itemPanel.transform, "Select the properties (Enabled, Shadow casting mode and Receive shadows) of the currently selected renderer as interpolables in timeline");

                Button exportUVButton = MaterialEditorControlFactory.CreateButton("ExportUVButton", itemPanel.transform, "Export UV Map");
                var exportUVButtonLE = exportUVButton.gameObject.AddComponent<LayoutElement>();
                exportUVButtonLE.minWidth = RendererButtonWidth;
                exportUVButtonLE.preferredWidth = RendererButtonWidth;
                exportUVButtonLE.flexibleWidth = 0f;
                TooltipManager.AddTooltip(exportUVButton.gameObject, "Export the UV map of this renderer.\n\nThe UV map is the 2d projection of the renderer with which to map textures to the 3d model. You can use this UV map as a guide to drawing on textures");

                Button exportMeshButton = MaterialEditorControlFactory.CreateButton("ExportObjButton", itemPanel.transform, "Export .obj");
                var exportMeshButtonLE = exportMeshButton.gameObject.AddComponent<LayoutElement>();
                exportMeshButtonLE.minWidth = RendererButtonWidth;
                exportMeshButtonLE.preferredWidth = RendererButtonWidth;
                exportMeshButtonLE.flexibleWidth = 0f;
                TooltipManager.AddTooltip(exportMeshButton.gameObject, "Export the renderer as a .obj.\n\nYou can use the <i>ExportBakedMesh</i> and <i>ExportBakedWorldPosition</i> config options to change the exporting behaviour");
            }

            //Renderer Enabled
            {
                var itemPanel = MaterialEditorControlFactory.CreatePanel("RendererEnabledPanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.color = ItemColor;
                var itemHLG = itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
                itemHLG.padding = Padding;
                itemHLG.childForceExpandWidth = false;
                itemHLG.childAlignment = TextAnchor.MiddleLeft;

                var label = MaterialEditorControlFactory.CreateText("RendererEnabledLabel", itemPanel.transform, "");
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.black;
                var labelLE = label.gameObject.AddComponent<LayoutElement>();
                labelLE.minWidth = LabelWidth;
                labelLE.preferredWidth = LabelWidth;
                labelLE.flexibleWidth = 1f;

                Toggle toggleEnabled = MaterialEditorControlFactory.CreateToggle("RendererEnabledToggle", itemPanel.transform, "");
                toggleEnabled.isOn = true;
                var toggleEnabledLE = toggleEnabled.gameObject.AddComponent<LayoutElement>();
                toggleEnabledLE.minWidth = RendererToggleWidth;
                toggleEnabledLE.preferredWidth = RendererToggleWidth;
                toggleEnabledLE.flexibleWidth = 0f;
                TooltipManager.AddTooltip(toggleEnabled.gameObject, "Toggle the visibility of this renderer on/off");

                var reset = MaterialEditorControlFactory.CreateButton($"RendererEnabledResetButton", itemPanel.transform, "Reset");
                var resetLE = reset.gameObject.AddComponent<LayoutElement>();
                resetLE.minWidth = ResetButtonWidth;
                resetLE.preferredWidth = ResetButtonWidth;
                resetLE.flexibleWidth = 0f;
                TooltipManager.AddTooltip(reset.gameObject, "Reset this property to its original value");
            }

            //Renderer ShadowCastingMode
            {
                var itemPanel = MaterialEditorControlFactory.CreatePanel("RendererShadowCastingModePanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.color = ItemColor;
                var itemHLG = itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
                itemHLG.padding = Padding;
                itemHLG.childForceExpandWidth = false;
                itemHLG.childAlignment = TextAnchor.MiddleLeft;

                var label = MaterialEditorControlFactory.CreateText("RendererShadowCastingModeLabel", itemPanel.transform, "");
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.black;
                var labelLE = label.gameObject.AddComponent<LayoutElement>();
                labelLE.minWidth = LabelWidth;
                labelLE.preferredWidth = LabelWidth;
                labelLE.flexibleWidth = 1f;

                Dropdown dropdownShadowCastingMode = MaterialEditorControlFactory.CreateDropdown("RendererShadowCastingModeDropdown", itemPanel.transform);
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

                var reset = MaterialEditorControlFactory.CreateButton($"RendererShadowCastingModeResetButton", itemPanel.transform, "Reset");
                var resetLE = reset.gameObject.AddComponent<LayoutElement>();
                resetLE.minWidth = ResetButtonWidth;
                resetLE.preferredWidth = ResetButtonWidth;
                resetLE.flexibleWidth = 0f;
                TooltipManager.AddTooltip(reset.gameObject, "Reset this property to its original value");
            }

            //Renderer ReceiveShadows
            {
                var itemPanel = MaterialEditorControlFactory.CreatePanel("RendererReceiveShadowsPanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.color = ItemColor;
                var itemHLG = itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
                itemHLG.padding = Padding;
                itemHLG.childForceExpandWidth = false;
                itemHLG.childAlignment = TextAnchor.MiddleLeft;

                var label = MaterialEditorControlFactory.CreateText("RendererReceiveShadowsLabel", itemPanel.transform, "");
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.black;
                var labelLE = label.gameObject.AddComponent<LayoutElement>();
                labelLE.minWidth = LabelWidth;
                labelLE.preferredWidth = LabelWidth;
                labelLE.flexibleWidth = 1f;

                Toggle toggleReceiveShadows = MaterialEditorControlFactory.CreateToggle("RendererReceiveShadowsToggle", itemPanel.transform, "");
                toggleReceiveShadows.isOn = true;
                var toggleReceiveShadowsLE = toggleReceiveShadows.gameObject.AddComponent<LayoutElement>();
                toggleReceiveShadowsLE.minWidth = RendererToggleWidth;
                toggleReceiveShadowsLE.preferredWidth = RendererToggleWidth;
                toggleReceiveShadowsLE.flexibleWidth = 0f;
                TooltipManager.AddTooltip(toggleReceiveShadows.gameObject, "Toggle if the renderer can have shadows cast on it on/off");

                var reset = MaterialEditorControlFactory.CreateButton($"RendererReceiveShadowsResetButton", itemPanel.transform, "Reset");
                var resetLE = reset.gameObject.AddComponent<LayoutElement>();
                resetLE.minWidth = ResetButtonWidth;
                resetLE.preferredWidth = ResetButtonWidth;
                resetLE.flexibleWidth = 0f;
                TooltipManager.AddTooltip(reset.gameObject, "Reset this property to its original value");
            }

            //Renderer RendererUpdateWhenOffscreen
            {
                var itemPanel = MaterialEditorControlFactory.CreatePanel("RendererUpdateWhenOffscreenPanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.color = ItemColor;
                var itemHLG = itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
                itemHLG.padding = Padding;
                itemHLG.childForceExpandWidth = false;
                itemHLG.childAlignment = TextAnchor.MiddleLeft;

                var label = MaterialEditorControlFactory.CreateText("RendererUpdateWhenOffscreenLabel", itemPanel.transform, "");
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.black;
                var labelLE = label.gameObject.AddComponent<LayoutElement>();
                labelLE.minWidth = LabelWidth;
                labelLE.preferredWidth = LabelWidth;
                labelLE.flexibleWidth = 1f;

                Toggle toggleRendererUpdateWhenOffscreen = MaterialEditorControlFactory.CreateToggle("RendererUpdateWhenOffscreenToggle", itemPanel.transform, "");
                toggleRendererUpdateWhenOffscreen.isOn = false;
                var toggleRendererUpdateWhenOffscreenLE = toggleRendererUpdateWhenOffscreen.gameObject.AddComponent<LayoutElement>();
                toggleRendererUpdateWhenOffscreenLE.minWidth = RendererToggleWidth;
                toggleRendererUpdateWhenOffscreenLE.preferredWidth = RendererToggleWidth;
                toggleRendererUpdateWhenOffscreenLE.flexibleWidth = 0f;
                TooltipManager.AddTooltip(toggleRendererUpdateWhenOffscreen.gameObject, "When on, a renderer will always stay renderer, even when considered to be off-screen.\n\n This is handy for when the bounding box of an object is configured improperly and dissapears when it should still be visible");

                var reset = MaterialEditorControlFactory.CreateButton($"RendererUpdateWhenOffscreenResetButton", itemPanel.transform, "Reset");
                var resetLE = reset.gameObject.AddComponent<LayoutElement>();
                resetLE.minWidth = ResetButtonWidth;
                resetLE.preferredWidth = ResetButtonWidth;
                resetLE.flexibleWidth = 0f;
                TooltipManager.AddTooltip(reset.gameObject, "Reset this property to its original value");
            }

            //Renderer RecalulateNormals
            {
                var itemPanel = MaterialEditorControlFactory.CreatePanel("RendererRecalculateNormalsPanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.color = ItemColor;
                var itemHLG = itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
                itemHLG.padding = Padding;
                itemHLG.childForceExpandWidth = false;
                itemHLG.childAlignment = TextAnchor.MiddleLeft;

                var label = MaterialEditorControlFactory.CreateText("RendererRecalculateNormalsLabel", itemPanel.transform, "");
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.black;
                var labelLE = label.gameObject.AddComponent<LayoutElement>();
                labelLE.minWidth = LabelWidth;
                labelLE.preferredWidth = LabelWidth;
                labelLE.flexibleWidth = 1f;

                Toggle toggleRecalculateNormals = MaterialEditorControlFactory.CreateToggle("RendererRecalculateNormalsToggle", itemPanel.transform, "");
                toggleRecalculateNormals.isOn = false;
                var toggleRecalculateNormalsLE = toggleRecalculateNormals.gameObject.AddComponent<LayoutElement>();
                toggleRecalculateNormalsLE.minWidth = RendererToggleWidth;
                toggleRecalculateNormalsLE.preferredWidth = RendererToggleWidth;
                toggleRecalculateNormalsLE.flexibleWidth = 0f;
                TooltipManager.AddTooltip(toggleRecalculateNormals.gameObject, "Recalculate the normals of this renderer based on its current shape, instead of its original shape.\n\nOnly available on skinned mesh renderers");

                var reset = MaterialEditorControlFactory.CreateButton($"RendererRecalculateNormalsResetButton", itemPanel.transform, "Reset");
                var resetLE = reset.gameObject.AddComponent<LayoutElement>();
                resetLE.minWidth = ResetButtonWidth;
                resetLE.preferredWidth = ResetButtonWidth;
                resetLE.flexibleWidth = 0f;
                TooltipManager.AddTooltip(reset.gameObject, "Reset this property to its original value.\n\nIn order for the reset to take effect you need to either save and re-load the scene, or copy the object and delete the old one");
            }

            //Material
            {
                var itemPanel = MaterialEditorControlFactory.CreatePanel("MaterialPanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.color = MaterialColor;
                var itemHLG = itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
                itemHLG.padding = Padding;
                itemHLG.childForceExpandWidth = false;
                itemHLG.childAlignment = TextAnchor.MiddleLeft;

                var label = MaterialEditorControlFactory.CreateText("MaterialLabel", itemPanel.transform, "");
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.black;
                var labelLE = label.gameObject.AddComponent<LayoutElement>();
                labelLE.minWidth = 0f;
                labelLE.preferredWidth = 0f;
                labelLE.flexibleWidth = 0f;

                Text labelMaterial = MaterialEditorControlFactory.CreateText("MaterialText", itemPanel.transform);
                labelMaterial.gameObject.AddComponent<LabelClickTrigger>();
                labelMaterial.alignment = TextAnchor.MiddleLeft;
                labelMaterial.color = Color.black;
                var labelMaterialLE = labelMaterial.gameObject.AddComponent<LayoutElement>();
                labelMaterialLE.minWidth = LabelWidth;
                labelMaterialLE.preferredWidth = LabelWidth;
                labelMaterialLE.flexibleWidth = 1f;
                TooltipManager.AddTooltip(labelMaterial.gameObject, "Material name");

                var copyEdits = MaterialEditorControlFactory.CreateButton($"MaterialCopy", itemPanel.transform, "Copy Edits");
                var copyEditsLE = copyEdits.gameObject.AddComponent<LayoutElement>();
                copyEditsLE.minWidth = MaterialButtonWidth;
                copyEditsLE.preferredWidth = MaterialButtonWidth;
                copyEditsLE.flexibleWidth = 0f;
                TooltipManager.AddTooltip(copyEdits.gameObject, "Copy all the <b>edits</b> of this material");

                var pasteEdits = MaterialEditorControlFactory.CreateButton($"MaterialPaste", itemPanel.transform, "Paste Edits");
                var pasteEditsLE = pasteEdits.gameObject.AddComponent<LayoutElement>();
                pasteEditsLE.minWidth = MaterialButtonWidth;
                pasteEditsLE.preferredWidth = MaterialButtonWidth;
                pasteEditsLE.flexibleWidth = 0f;
                TooltipManager.AddTooltip(pasteEdits.gameObject, "Paste all the copied edits");

                var copy = MaterialEditorControlFactory.CreateButton($"MaterialCopyRemove", itemPanel.transform, "Copy Material");
                var copyLE = copy.gameObject.AddComponent<LayoutElement>();
                copyLE.minWidth = MaterialButtonWidth;
                copyLE.preferredWidth = MaterialButtonWidth;
                copyLE.flexibleWidth = 0f;
                TooltipManager.AddTooltip(copy.gameObject, "Make a copy of this material.\n\nUseful for overlaying different effects onto an object with different material shaders/properties");

                var rename = MaterialEditorControlFactory.CreateButton($"MaterialRename", itemPanel.transform, ">");
                var renameLE = rename.gameObject.AddComponent<LayoutElement>();
                renameLE.minWidth = MaterialRenameButtonWidth;
                renameLE.preferredWidth = MaterialRenameButtonWidth;
                renameLE.flexibleWidth = 0f;
                TooltipManager.AddTooltip(rename.gameObject, "Rename material instances");
            }

            //Material Shader
            {
                var itemPanel = MaterialEditorControlFactory.CreatePanel("ShaderPanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.color = ItemColor;
                var itemHLG = itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
                itemHLG.padding = Padding;
                itemHLG.childForceExpandWidth = false;
                itemHLG.childAlignment = TextAnchor.MiddleLeft;

                var label = MaterialEditorControlFactory.CreateText("ShaderLabel", itemPanel.transform, "");
                label.gameObject.AddComponent<LabelClickTrigger>();
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.black;
                var labelLE = label.gameObject.AddComponent<LayoutElement>();
                labelLE.minWidth = LabelWidth;
                labelLE.preferredWidth = LabelWidth;
                labelLE.flexibleWidth = 1f;

                CreateInterpolableButton("SelectInterpolableShaderButton", itemPanel.transform, "Select the currently selected shader property and its render queue as interpolables in timeline");

                Dropdown dropdownShader = MaterialEditorControlFactory.CreateDropdown("ShaderDropdown", itemPanel.transform);
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

                var reset = MaterialEditorControlFactory.CreateButton($"ShaderResetButton", itemPanel.transform, "Reset");
                var resetLE = reset.gameObject.AddComponent<LayoutElement>();
                resetLE.minWidth = ResetButtonWidth;
                resetLE.preferredWidth = ResetButtonWidth;
                resetLE.flexibleWidth = 0f;
                TooltipManager.AddTooltip(reset.gameObject, "Reset this property to its original value.\n\nIf the original shader is not one known by Material Editor, it will not be able to reset the shader to its original value. In order for the reset to take effect you to either save and re-load the scene, or copy the object and delete the old one");
            }

            //Material RenderQueue
            {
                var itemPanel = MaterialEditorControlFactory.CreatePanel("ShaderRenderQueuePanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.color = ItemColor;
                var itemHLG = itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
                itemHLG.padding = Padding;
                itemHLG.childForceExpandWidth = false;
                itemHLG.childAlignment = TextAnchor.MiddleLeft;

                var label = MaterialEditorControlFactory.CreateText("ShaderRenderQueueLabel", itemPanel.transform, "");
                label.gameObject.AddComponent<LabelClickTrigger>();
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.black;

                InputField textBoxShaderRenderQueue = MaterialEditorControlFactory.CreateInputField("ShaderRenderQueueInput", itemPanel.transform);
                textBoxShaderRenderQueue.text = "0";
                TooltipManager.AddTooltip(textBoxShaderRenderQueue.gameObject, "The order in which a material is rendered. Higher render queues get rendered later");

                var reset = MaterialEditorControlFactory.CreateButton($"ShaderRenderQueueResetButton", itemPanel.transform, "Reset");
                TooltipManager.AddTooltip(reset.gameObject, "Reset this property to its original value");
            }

            // Property Category
            {
                var itemPanel = MaterialEditorControlFactory.CreatePanel("PropertyCategoryPanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.color = CategoryColor;
                var itemHLG = itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
                itemHLG.padding = Padding;
                itemHLG.childForceExpandWidth = false;
                itemHLG.childAlignment = TextAnchor.MiddleLeft;
                itemHLG.spacing = 2f;

                var collapseButton = MaterialEditorControlFactory.CreateButton("PropertyCategoryCollapseButton", itemPanel.transform, "-");
                var collapseButtonLE = collapseButton.gameObject.AddComponent<LayoutElement>();
                collapseButtonLE.minWidth = SmallButtonWidth;
                collapseButtonLE.preferredWidth = SmallButtonWidth;
                collapseButtonLE.flexibleWidth = 0f;
                TooltipManager.AddTooltip(collapseButton.gameObject, "Expand or collapse this category");

                var label = MaterialEditorControlFactory.CreateText("PropertyCategoryLabel", itemPanel.transform, "");
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
                var itemPanel = MaterialEditorControlFactory.CreatePanel("TexturePanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.color = ItemColor;
                var itemHLG = itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
                itemHLG.padding = Padding;
                itemHLG.childForceExpandWidth = false;
                itemHLG.childAlignment = TextAnchor.MiddleLeft;

                var label = MaterialEditorControlFactory.CreateText("TextureLabel", itemPanel.transform, "");
                label.gameObject.AddComponent<LabelClickTrigger>();
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.black;
                var labelLE = label.gameObject.AddComponent<LayoutElement>();
                labelLE.minWidth = LabelWidth;
                labelLE.preferredWidth = LabelWidth;
                labelLE.flexibleWidth = 1f;

                CreateInterpolableButton("SelectInterpolableTextureButton", itemPanel.transform, "Select the currently selected texture property and its offset and scale properties as interpolables in timeline");

                Button exportButton = MaterialEditorControlFactory.CreateButton($"TextureExportButton", itemPanel.transform, $"Export Texture");
                var exportButtonLE = exportButton.gameObject.AddComponent<LayoutElement>();
                exportButtonLE.minWidth = TextureButtonWidth;
                exportButtonLE.preferredWidth = TextureButtonWidth;
                exportButtonLE.flexibleWidth = 0f;

                Button importButton = MaterialEditorControlFactory.CreateButton($"TextureImportButton", itemPanel.transform, $"Import Texture");
                var importButtonLE = importButton.gameObject.AddComponent<LayoutElement>();
                importButtonLE.minWidth = TextureButtonWidth;
                importButtonLE.preferredWidth = TextureButtonWidth;
                importButtonLE.flexibleWidth = 0f;

                var reset = MaterialEditorControlFactory.CreateButton($"TextureResetButton", itemPanel.transform, "Reset");
                var resetLE = reset.gameObject.AddComponent<LayoutElement>();
                resetLE.minWidth = ResetButtonWidth;
                resetLE.preferredWidth = ResetButtonWidth;
                resetLE.flexibleWidth = 0f;
                TooltipManager.AddTooltip(reset.gameObject, "Reset this property to its original value.\n\nIn order for the reset to take effect you need to either save and re-load the scene, or copy the object and delete the old one");
            }

            //Offset and Scale
            {
                var itemPanel = MaterialEditorControlFactory.CreatePanel("OffsetScalePanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.color = ItemColor;
                var itemHLG = itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
                itemHLG.padding = Padding;
                itemHLG.childForceExpandWidth = false;
                itemHLG.childAlignment = TextAnchor.MiddleLeft;

                var label = MaterialEditorControlFactory.CreateText("OffsetScaleLabel", itemPanel.transform, "");
                label.gameObject.AddComponent<LabelClickTrigger>();
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.black;

                Text emptySpace = MaterialEditorControlFactory.CreateText("EmptySpace", itemPanel.transform, "");
                emptySpace.alignment = TextAnchor.MiddleLeft;

                Text labelOffsetX = MaterialEditorControlFactory.CreateText("OffsetXText", itemPanel.transform, "OffsetX");
                labelOffsetX.gameObject.AddComponent<LabelClickTrigger>();
                labelOffsetX.alignment = TextAnchor.MiddleLeft;
                labelOffsetX.color = Color.black;

                var offsetX = MaterialEditorControlFactory.CreateNumericInput(
                    "OffsetXInput",
                    itemPanel.transform,
                    NumericInputSpec.FloatingPoint);
                offsetX.SetValue(0f);
                TooltipManager.AddTooltip(offsetX.gameObject, "Adjust the horizontal offset of the texture. It can move the texture left or right.");

                Text labelOffsetY = MaterialEditorControlFactory.CreateText("OffsetYText", itemPanel.transform, "Y");
                labelOffsetY.alignment = TextAnchor.MiddleLeft;
                labelOffsetY.color = Color.black;

                var offsetY = MaterialEditorControlFactory.CreateNumericInput(
                    "OffsetYInput",
                    itemPanel.transform,
                    NumericInputSpec.FloatingPoint);
                offsetY.SetValue(0f);
                TooltipManager.AddTooltip(offsetY.gameObject, "Adjust the vertical offset of the texture. It can move the texture up or down.");

                labelOffsetX.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(
                    offsetX.InputField,
                    new[] { offsetY.InputField });
                labelOffsetY.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(
                    offsetY.InputField,
                    new[] { offsetX.InputField });

                //Scale
                Text labelScaleX = MaterialEditorControlFactory.CreateText("ScaleXText", itemPanel.transform, "ScaleX");
                labelScaleX.alignment = TextAnchor.MiddleLeft;
                labelScaleX.color = Color.black;

                var scaleX = MaterialEditorControlFactory.CreateNumericInput(
                    "ScaleXInput",
                    itemPanel.transform,
                    NumericInputSpec.FloatingPoint);
                scaleX.SetValue(0f);
                TooltipManager.AddTooltip(scaleX.gameObject, "Adjust the horizontal scale of the texture. Values greater than 1 make the texture appear smaller horizontally, values less than 1 make it appear larger horizontally.");

                Text labelScaleY = MaterialEditorControlFactory.CreateText("ScaleYText", itemPanel.transform, "Y");
                labelScaleY.alignment = TextAnchor.MiddleLeft;
                labelScaleY.color = Color.black;

                var scaleY = MaterialEditorControlFactory.CreateNumericInput(
                    "ScaleYInput",
                    itemPanel.transform,
                    NumericInputSpec.FloatingPoint);
                scaleY.SetValue(0f);
                TooltipManager.AddTooltip(scaleY.gameObject, "Adjust the vertical scale of the texture. Values greater than 1 make the texture appear smaller vertically, values less than 1 make it appear larger vertically.");

                var reset = MaterialEditorControlFactory.CreateButton($"OffsetScaleResetButton", itemPanel.transform, "Reset");
                TooltipManager.AddTooltip(reset.gameObject, "Reset both the scale and offset properties to their original values");

                labelScaleX.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(
                    scaleX.InputField,
                    new[] { scaleY.InputField });
                labelScaleY.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(
                    scaleY.InputField,
                    new[] { scaleX.InputField });
            }

            //Color properties
            {
                var itemPanel = MaterialEditorControlFactory.CreatePanel("ColorPanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.color = ItemColor;
                var itemHLG = itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
                itemHLG.padding = Padding;
                itemHLG.childForceExpandWidth = false;
                itemHLG.childControlWidth = true;
                itemHLG.childAlignment = TextAnchor.MiddleLeft;

                var label = MaterialEditorControlFactory.CreateText("ColorLabel", itemPanel.transform, "");
                label.gameObject.AddComponent<LabelClickTrigger>();
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.black;

                CreateInterpolableButton(
                    "SelectInterpolableColorButton",
                    itemPanel.transform,
                    "Select currently selected color property as interpolable in timeline",
                    true);

                Text labelR = MaterialEditorControlFactory.CreateText("ColorRText", itemPanel.transform, "R");
                labelR.alignment = TextAnchor.MiddleLeft;
                labelR.color = Color.black;

                var red = MaterialEditorControlFactory.CreateNumericInput(
                    "ColorRInput",
                    itemPanel.transform,
                    NumericInputSpec.FloatingPoint);
                red.SetValue(0f);


                Text labelG = MaterialEditorControlFactory.CreateText("ColorGText", itemPanel.transform, "G");
                labelG.alignment = TextAnchor.MiddleLeft;
                labelG.color = Color.black;

                var green = MaterialEditorControlFactory.CreateNumericInput(
                    "ColorGInput",
                    itemPanel.transform,
                    NumericInputSpec.FloatingPoint);
                green.SetValue(0f);

                Text labelB = MaterialEditorControlFactory.CreateText("ColorBText", itemPanel.transform, "B");
                labelB.alignment = TextAnchor.MiddleLeft;
                labelB.color = Color.black;

                var blue = MaterialEditorControlFactory.CreateNumericInput(
                    "ColorBInput",
                    itemPanel.transform,
                    NumericInputSpec.FloatingPoint);
                blue.SetValue(0f);

                Text labelA = MaterialEditorControlFactory.CreateText("ColorAText", itemPanel.transform, "A");
                labelA.alignment = TextAnchor.MiddleLeft;
                labelA.color = Color.black;

                var alpha = MaterialEditorControlFactory.CreateNumericInput(
                    "ColorAInput",
                    itemPanel.transform,
                    NumericInputSpec.FloatingPoint);
                alpha.SetValue(0f);

                var edit = MaterialEditorControlFactory.CreateButton("ColorEditButton", itemPanel.transform, "");

                var reset = MaterialEditorControlFactory.CreateButton($"ColorResetButton", itemPanel.transform, "Reset");
                TooltipManager.AddTooltip(reset.gameObject, "Reset the selected property to its original value");

                labelR.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(
                    red.InputField,
                    new[] { green.InputField, blue.InputField });
                labelG.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(
                    green.InputField,
                    new[] { red.InputField, blue.InputField });
                labelB.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(
                    blue.InputField,
                    new[] { red.InputField, green.InputField });
                labelA.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(alpha.InputField);
            }

            //Float properties
            {
                var itemPanel = MaterialEditorControlFactory.CreatePanel("FloatPanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.color = ItemColor;
                var itemHLG = itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
                itemHLG.padding = Padding;
                itemHLG.childForceExpandWidth = false;
                itemHLG.childAlignment = TextAnchor.MiddleLeft;

                var label = MaterialEditorControlFactory.CreateText("FloatLabel", itemPanel.transform, "");
                label.gameObject.AddComponent<LabelClickTrigger>();
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.black;

                CreateInterpolableButton(
                    "SelectInterpolableFloatButton",
                    itemPanel.transform,
                    "Select currently selected float property as interpolable in timeline",
                    true);

                Slider sliderFloat = MaterialEditorControlFactory.CreateSlider("FloatSlider", itemPanel.transform);

                var floatInput = MaterialEditorControlFactory.CreateNumericInput(
                    "FloatInputField",
                    itemPanel.transform,
                    NumericInputSpec.FloatingPoint);
                floatInput.SetValue(0f);

                var reset = MaterialEditorControlFactory.CreateButton($"FloatResetButton", itemPanel.transform, "Reset");
                TooltipManager.AddTooltip(reset.gameObject, "Reset the selected property to its original value");
                label.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(floatInput.InputField);
            }

            //Keyword properties
            {
                var itemPanel = MaterialEditorControlFactory.CreatePanel("KeywordPanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.color = ItemColor;
                var itemHLG = itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
                itemHLG.padding = Padding;
                itemHLG.childForceExpandWidth = false;
                itemHLG.childAlignment = TextAnchor.MiddleLeft;

                var label = MaterialEditorControlFactory.CreateText("KeywordLabel", itemPanel.transform, "");
                label.gameObject.AddComponent<LabelClickTrigger>();
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.black;
                var labelLE = label.gameObject.AddComponent<LayoutElement>();
                labelLE.minWidth = LabelWidth;
                labelLE.preferredWidth = LabelWidth;
                labelLE.flexibleWidth = 1f;

                Text emptySpace = MaterialEditorControlFactory.CreateText("EmptySpace", itemPanel.transform, "");
                emptySpace.alignment = TextAnchor.MiddleLeft;
                var emptySpaceLE = emptySpace.gameObject.AddComponent<LayoutElement>();
                emptySpaceLE.minWidth = InterpolableButtonWidth;
                emptySpaceLE.preferredWidth = InterpolableButtonWidth;
                emptySpaceLE.flexibleWidth = 0f;

                Toggle toggleKeyword = MaterialEditorControlFactory.CreateToggle("KeywordToggle", itemPanel.transform, "");
                var toggleKeywordLE = toggleKeyword.gameObject.AddComponent<LayoutElement>();
                toggleKeywordLE.minWidth = KeywordToggleWidth;
                toggleKeywordLE.preferredWidth = KeywordToggleWidth;
                toggleKeywordLE.flexibleWidth = 0f;

                var reset = MaterialEditorControlFactory.CreateButton($"KeywordResetButton", itemPanel.transform, "Reset");
                var resetLE = reset.gameObject.AddComponent<LayoutElement>();
                resetLE.minWidth = ResetButtonWidth;
                resetLE.preferredWidth = ResetButtonWidth;
                resetLE.flexibleWidth = 0f;
                TooltipManager.AddTooltip(reset.gameObject, "Reset the selected property to its original value");
            }

            RowStyle.Apply(contentList.gameObject);
            RowLayoutCatalog.Apply(contentList.gameObject);
            return contentList.gameObject;
        }

        private static void CreateInterpolableButton(
            string objectName,
            Transform parent,
            string tooltipText,
            bool layoutOwnedBySpec = false)
        {
            Button interpolableButton = MaterialEditorControlFactory.CreateButton(objectName, parent, "O");
            if (!layoutOwnedBySpec)
            {
                var interpolableButtonLE = interpolableButton.gameObject.AddComponent<LayoutElement>();
                interpolableButtonLE.minWidth = InterpolableButtonWidth;
                interpolableButtonLE.preferredWidth = InterpolableButtonWidth;
                interpolableButtonLE.flexibleWidth = 0f;
            }
            interpolableButton.gameObject.SetActive(false);
            TooltipManager.AddTooltip(interpolableButton.gameObject, tooltipText);

#if !API && !EC
            if (TimelineCompatibilityHelper.IsTimelineAvailable())
                interpolableButton.gameObject.SetActive(true);
#endif
        }
    }
}
