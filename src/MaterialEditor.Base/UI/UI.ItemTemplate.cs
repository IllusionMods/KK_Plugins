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

            //Renderer Section Header
            {
                var itemPanel = UIUtility.CreatePanel("RendererSectionPanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.color = RendererSectionColor;
                itemPanel.gameObject.AddComponent<LayoutElement>().preferredHeight = PanelHeight + 4f;
                var itemHLG = itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
                itemHLG.padding = new RectOffset(7, 1, 1, 1);
                itemHLG.spacing = 4f;
                itemHLG.childForceExpandWidth = false;

                //Left accent bar
                var accentBar = UIUtility.CreatePanel("RendererSectionAccentBar", itemPanel.transform);
                accentBar.color = RendererSectionAccent;
                var accentBarLE = accentBar.gameObject.AddComponent<LayoutElement>();
                accentBarLE.minWidth = 3f;
                accentBarLE.preferredWidth = 3f;
                accentBarLE.flexibleWidth = 0f;

                var collapseButton = UIUtility.CreateButton("RendererSectionCollapseButton", itemPanel.transform, "-");
                var collapseButtonLE = collapseButton.gameObject.AddComponent<LayoutElement>();
                collapseButtonLE.minWidth = SmallButtonWidth;
                collapseButtonLE.preferredWidth = SmallButtonWidth;
                collapseButtonLE.flexibleWidth = 0f;
                TooltipManager.AddTooltip(collapseButton.gameObject, "Collapse/expand all renderers");

                Text labelSection = UIUtility.CreateText("RendererSectionText", itemPanel.transform);
                labelSection.alignment = TextAnchor.MiddleLeft;
                labelSection.color = RendererSectionText;
                labelSection.fontStyle = FontStyle.Bold;
                labelSection.fontSize = 14;
                var labelSectionLE = labelSection.gameObject.AddComponent<LayoutElement>();
                labelSectionLE.minWidth = LabelWidth;
                labelSectionLE.preferredWidth = LabelWidth;
                labelSectionLE.flexibleWidth = 1f;

            }

            //Renderer
            {
                var itemPanel = UIUtility.CreatePanel("RendererPanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.color = RendererColor;
                var itemHLG = itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
                itemHLG.padding = new RectOffset(18, 1, 1, 1);
                itemHLG.spacing = 4f;
                itemHLG.childForceExpandWidth = false;

                //Left accent bar
                var accentBar = UIUtility.CreatePanel("RendererAccentBar", itemPanel.transform);
                accentBar.color = RendererSectionAccent;
                var accentBarLE = accentBar.gameObject.AddComponent<LayoutElement>();
                accentBarLE.minWidth = 2f;
                accentBarLE.preferredWidth = 2f;
                accentBarLE.flexibleWidth = 0f;

                var collapseButton = UIUtility.CreateButton("RendererCollapseButton", itemPanel.transform, "-");
                var collapseButtonLE = collapseButton.gameObject.AddComponent<LayoutElement>();
                collapseButtonLE.minWidth = SmallButtonWidth;
                collapseButtonLE.preferredWidth = SmallButtonWidth;
                collapseButtonLE.flexibleWidth = 0f;
                TooltipManager.AddTooltip(collapseButton.gameObject, "Collapse/expand renderer properties");

                var label = UIUtility.CreateText("RendererLabel", itemPanel.transform, "");
                label.enabled = false;
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

                Toggle toggleEnabledInline = UIUtility.CreateToggle("RendererEnabledInlineToggle", itemPanel.transform, "");
                toggleEnabledInline.isOn = true;
                var toggleEnabledInlineLE = toggleEnabledInline.gameObject.AddComponent<LayoutElement>();
                toggleEnabledInlineLE.minWidth = RendererToggleWidth;
                toggleEnabledInlineLE.preferredWidth = RendererToggleWidth;
                toggleEnabledInlineLE.flexibleWidth = 0f;
                TooltipManager.AddTooltip(toggleEnabledInline.gameObject, "Toggle the visibility of this renderer on/off");

                CreateInterpolableButton("SelectInterpolableRendererButton", itemPanel.transform, "Select the properties (Enabled, Shadow casting mode and Receive shadows) of the currently selected renderer as interpolables in timeline");

                Button exportUVButton = UIUtility.CreateButton("ExportUVButton", itemPanel.transform, "UV Map");
                var exportUVButtonLE = exportUVButton.gameObject.AddComponent<LayoutElement>();
                exportUVButtonLE.minWidth = RendererButtonWidth;
                exportUVButtonLE.preferredWidth = RendererButtonWidth;
                exportUVButtonLE.flexibleWidth = 0f;
                TooltipManager.AddTooltip(exportUVButton.gameObject, "Export the UV map of this renderer.\n\nThe UV map is the 2d projection of the renderer with which to map textures to the 3d model. You can use this UV map as a guide to drawing on textures");

                Button exportMeshButton = UIUtility.CreateButton("ExportObjButton", itemPanel.transform, ".obj");
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
                itemHLG.padding = SubRowPadding;
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
                itemHLG.padding = SubRowPadding;
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
                var shadowTemplate = dropdownShadowCastingMode.transform.Find("Template");
                if (shadowTemplate != null) shadowTemplate.gameObject.AddComponent<DropdownThemer>();
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
                itemHLG.padding = SubRowPadding;
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
                itemHLG.padding = SubRowPadding;
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
                itemHLG.padding = SubRowPadding;
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

            //Material Section Header
            {
                var itemPanel = UIUtility.CreatePanel("MaterialSectionPanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.color = MaterialSectionColor;
                itemPanel.gameObject.AddComponent<LayoutElement>().preferredHeight = PanelHeight + 4f;
                var itemHLG = itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
                itemHLG.padding = new RectOffset(7, 1, 1, 1);
                itemHLG.spacing = 4f;
                itemHLG.childForceExpandWidth = false;

                //Left accent bar
                var accentBar = UIUtility.CreatePanel("MaterialSectionAccentBar", itemPanel.transform);
                accentBar.color = MaterialSectionAccent;
                var accentBarLE = accentBar.gameObject.AddComponent<LayoutElement>();
                accentBarLE.minWidth = 3f;
                accentBarLE.preferredWidth = 3f;
                accentBarLE.flexibleWidth = 0f;

                var collapseButton = UIUtility.CreateButton("MaterialSectionCollapseButton", itemPanel.transform, "-");
                var collapseButtonLE = collapseButton.gameObject.AddComponent<LayoutElement>();
                collapseButtonLE.minWidth = SmallButtonWidth;
                collapseButtonLE.preferredWidth = SmallButtonWidth;
                collapseButtonLE.flexibleWidth = 0f;
                TooltipManager.AddTooltip(collapseButton.gameObject, "Collapse/expand all materials");

                Text labelSection = UIUtility.CreateText("MaterialSectionText", itemPanel.transform);
                labelSection.alignment = TextAnchor.MiddleLeft;
                labelSection.color = MaterialSectionText;
                labelSection.fontStyle = FontStyle.Bold;
                labelSection.fontSize = 14;
                var labelSectionLE = labelSection.gameObject.AddComponent<LayoutElement>();
                labelSectionLE.minWidth = LabelWidth;
                labelSectionLE.preferredWidth = LabelWidth;
                labelSectionLE.flexibleWidth = 1f;
            }

            //Material
            {
                var itemPanel = UIUtility.CreatePanel("MaterialPanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.color = MaterialColor;
                var itemHLG = itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
                itemHLG.padding = new RectOffset(18, 1, 1, 1);
                itemHLG.spacing = 2f;
                itemHLG.childForceExpandWidth = false;

                //Left accent bar
                var accentBar = UIUtility.CreatePanel("MaterialAccentBar", itemPanel.transform);
                accentBar.color = MaterialSectionAccent;
                var accentBarLE = accentBar.gameObject.AddComponent<LayoutElement>();
                accentBarLE.minWidth = 2f;
                accentBarLE.preferredWidth = 2f;
                accentBarLE.flexibleWidth = 0f;

                var collapseButton = UIUtility.CreateButton("MaterialCollapseButton", itemPanel.transform, "-");
                var collapseButtonLE = collapseButton.gameObject.AddComponent<LayoutElement>();
                collapseButtonLE.minWidth = SmallButtonWidth;
                collapseButtonLE.preferredWidth = SmallButtonWidth;
                collapseButtonLE.flexibleWidth = 0f;
                TooltipManager.AddTooltip(collapseButton.gameObject, "Collapse/expand material properties");

                var label = UIUtility.CreateText("MaterialLabel", itemPanel.transform, "");
                label.enabled = false;
                var labelLE = label.gameObject.AddComponent<LayoutElement>();
                labelLE.minWidth = 0f;
                labelLE.preferredWidth = 0f;
                labelLE.flexibleWidth = 0f;

                Text labelMaterial = UIUtility.CreateText("MaterialText", itemPanel.transform);
                labelMaterial.alignment = TextAnchor.MiddleLeft;
                labelMaterial.color = Color.black;
                labelMaterial.raycastTarget = true;
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

                var copy = UIUtility.CreateButton($"MaterialCopyRemove", itemPanel.transform, "Copy Mat");
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
                itemHLG.padding = SubRowPadding;
                itemHLG.spacing = 2f;
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
                //Attach themer to the popup template so it gets styled when opened
                var shaderTemplate = dropdownShader.transform.Find("Template");
                if (shaderTemplate != null) shaderTemplate.gameObject.AddComponent<DropdownThemer>();

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
                itemHLG.padding = SubRowPadding;
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
                itemHLG.padding = SubRowPadding;
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
                itemHLG.padding = SubRowPadding;
                itemHLG.spacing = 2f;
                itemHLG.childForceExpandWidth = false;

                var label = UIUtility.CreateText("TextureLabel", itemPanel.transform, "");
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.black;
                label.raycastTarget = true;
                TooltipManager.AddTooltip(label.gameObject, "Left-click to show in preview panel. Right-click to toggle preview (if enabled in F1).");
                var labelLE = label.gameObject.AddComponent<LayoutElement>();
                labelLE.minWidth = LabelWidth;
                labelLE.preferredWidth = LabelWidth;
                labelLE.flexibleWidth = 1f;

                CreateInterpolableButton("SelectInterpolableTextureButton", itemPanel.transform, "Select the currently selected texture property and its offset and scale properties as interpolables in timeline");

                Button exportButton = UIUtility.CreateButton($"TextureExportButton", itemPanel.transform, $"Export");
                var exportButtonLE = exportButton.gameObject.AddComponent<LayoutElement>();
                exportButtonLE.minWidth = TextureButtonWidth;
                exportButtonLE.preferredWidth = TextureButtonWidth;
                exportButtonLE.flexibleWidth = 0f;

                Button importButton = UIUtility.CreateButton($"TextureImportButton", itemPanel.transform, $"Import");
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
                itemHLG.padding = SubRowPadding;
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
                labelOffsetX.color = ItemTextColor;
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
                labelOffsetY.color = ItemTextColor;
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
                labelScaleX.color = ItemTextColor;
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
                labelScaleY.color = ItemTextColor;
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
                itemHLG.padding = SubRowPadding;
                //Tighter spacing: the colour row has many children, so 2px gaps accumulate and
                //shrink the flexible name label, shifting the row left relative to simpler rows
                itemHLG.spacing = 1f;
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
                labelR.color = ItemTextColor;
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
                labelG.color = ItemTextColor;
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
                labelB.color = ItemTextColor;
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
                labelA.color = ItemTextColor;
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
                itemHLG.padding = SubRowPadding;
                itemHLG.spacing = 2f;
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
                StyleSlider(sliderFloat);
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
                //Drag the float label (property name) to adjust the value
                label.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(textBoxFloat);
            }

            //Keyword properties
            {
                var itemPanel = UIUtility.CreatePanel("KeywordPanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.color = ItemColor;
                var itemHLG = itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
                itemHLG.padding = SubRowPadding;
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

        internal static void StyleSlider(Slider slider)
        {
            bool dark = MaterialEditorPluginBase.DarkMode.Value;
            bool hacker = MaterialEditorUI.HackerMode;
            var trackColor  = hacker ? new Color(0.00f, 0.25f, 0.05f, 1f) : dark ? new Color(0.28f, 0.28f, 0.30f, 1f) : new Color(0.55f, 0.55f, 0.57f, 1f);
            var fillColor   = hacker ? new Color(0.00f, 0.55f, 0.12f, 1f) : dark ? new Color(0.45f, 0.45f, 0.50f, 1f) : new Color(0.38f, 0.38f, 0.42f, 1f);
            var handleColor = hacker ? new Color(0.00f, 0.90f, 0.20f, 1f) : dark ? new Color(0.88f, 0.88f, 0.92f, 1f) : new Color(0.15f, 0.15f, 0.18f, 1f);

            //Track background: raycast target matching the handle circle (~12px)
            var bg = slider.transform.Find("Background")?.GetComponent<Image>();
            if (bg != null)
            {
                // Transparent: catches clicks in a band matching the handle diameter, no visible box
                bg.color = new Color(0f, 0f, 0f, 0f);
                var bgRT = bg.GetComponent<RectTransform>();
                bgRT.anchorMin = new Vector2(0f, 0.2f);
                bgRT.anchorMax = new Vector2(1f, 0.8f);
                bgRT.offsetMin = Vector2.zero;
                bgRT.offsetMax = Vector2.zero;
                bg.raycastTarget = true;
            }

            //Decorative track line, no raycast
            var trackLine = slider.transform.Find("TrackLine")?.GetComponent<Image>();
            if (trackLine == null)
            {
                var trackLineGO = new GameObject("TrackLine");
                trackLineGO.transform.SetParent(slider.transform, false);
                // Insert before Fill Area so it sits behind the fill
                var fillAreaT = slider.transform.Find("Fill Area");
                if (fillAreaT != null) trackLineGO.transform.SetSiblingIndex(fillAreaT.GetSiblingIndex());
                var trackLineRT = trackLineGO.AddComponent<RectTransform>();
                trackLineRT.anchorMin = new Vector2(0f, 0.42f);
                trackLineRT.anchorMax = new Vector2(1f, 0.58f);
                trackLineRT.offsetMin = Vector2.zero;
                trackLineRT.offsetMax = Vector2.zero;
                trackLine = trackLineGO.AddComponent<Image>();
                trackLine.raycastTarget = false;
            }
            trackLine.color = trackColor;

            //Fill area, centered strip
            var fillArea = slider.transform.Find("Fill Area")?.GetComponent<RectTransform>();
            if (fillArea != null)
            {
                fillArea.anchorMin = new Vector2(0f, 0.42f);
                fillArea.anchorMax = new Vector2(1f, 0.58f);
                fillArea.offsetMin = new Vector2(2f, 0f);
                fillArea.offsetMax = new Vector2(-2f, 0f);
            }

            //Handle slide area, same band as background
            var handleSlideArea = slider.transform.Find("Handle Slide Area")?.GetComponent<RectTransform>();
            if (handleSlideArea != null)
            {
                handleSlideArea.anchorMin = new Vector2(0f, 0.2f);
                handleSlideArea.anchorMax = new Vector2(1f, 0.8f);
                handleSlideArea.offsetMin = new Vector2(2f, 0f);
                handleSlideArea.offsetMax = new Vector2(-2f, 0f);
            }

            //Fill colour
            var fill = slider.transform.Find("Fill Area/Fill")?.GetComponent<Image>();
            if (fill != null)
                fill.color = fillColor;

            //Handle, UILib knob sprite
            var handle = slider.transform.Find("Handle Slide Area/Handle")?.GetComponent<Image>();
            if (handle != null)
            {
                handle.color = handleColor;
                handle.sprite = UILib.UIUtility.knob;
                handle.type = Image.Type.Simple;
                handle.preserveAspect = true;
                var handleRT = handle.GetComponent<RectTransform>();
                handleRT.sizeDelta = new Vector2(12f, 12f);
                handleRT.localEulerAngles = Vector3.zero;
            }
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
