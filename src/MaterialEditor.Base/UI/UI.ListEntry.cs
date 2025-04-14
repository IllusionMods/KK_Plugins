using System;
using UILib;
using UnityEngine;
using UnityEngine.UI;
using static MaterialEditorAPI.MaterialEditorUI;

namespace MaterialEditorAPI
{
    internal static class ListEntry
    {
        internal static GameObject CreateItem(Transform parent, ItemInfo item)
        {
            var contentList = UIUtility.CreatePanel("ListEntry", parent);
            contentList.gameObject.AddComponent<LayoutElement>().preferredHeight = PanelHeight;
            contentList.gameObject.AddComponent<Mask>();
            contentList.color = RowColor;

            switch(item.ItemType)
            {
                case ItemInfo.RowItemType.Renderer:
                    {
                        var panel = UIUtility.CreatePanel("RendererPanel", contentList.transform);
                        panel.gameObject.AddComponent<CanvasGroup>();
                        panel.color = RendererColor;
                        AddHorizontalLayoutGroup(panel);

                        var label = UIUtility.CreateText("RendererLabel", panel.transform, "");
                        label.alignment = TextAnchor.MiddleLeft;
                        label.color = Color.black;
                        AddLayoutElement(label.gameObject, 0f, 0f, 0f);
                        SetLabelText(label, item.LabelText);

                        var text = UIUtility.CreateText("RendererText", panel.transform);
                        text.alignment = TextAnchor.MiddleLeft;
                        text.color = Color.black;
                        AddLayoutElement(text.gameObject, LabelWidth, LabelWidth, 1f);
                        text.text = item.RendererName;
                        TooltipManager.AddTooltip(text.gameObject, "Renderer name");

                        var interpolableButton = CreateInterpolableButton("SelectInterpolableRendererButton", panel.transform);
                        interpolableButton.onClick.AddListener(() => item.SelectInterpolableButtonRendererOnClick());
                        TooltipManager.AddTooltip(interpolableButton.gameObject, "Select the properties (Enabled, Shadow casting mode and Receive shadows) of the currently selected renderer as interpolables in timeline");

                        var exportUVButton = UIUtility.CreateButton("ExportUVButton", panel.transform, "Export UV Map");
                        AddLayoutElement(exportUVButton.gameObject, RendererButtonWidth, RendererButtonWidth, 0f);
                        exportUVButton.onClick.AddListener(() => item.ExportUVOnClick());
                        TooltipManager.AddTooltip(exportUVButton.gameObject, "Export the UV map of this renderer.\n\nThe UV map is the 2d projection of the renderer with which to map textures to the 3d model. You can use this UV map as a guide to drawing on textures");

                        var exportMeshButton = UIUtility.CreateButton("ExportObjButton", panel.transform, "Export .obj");
                        AddLayoutElement(exportMeshButton.gameObject, RendererButtonWidth, RendererButtonWidth, 0f);
                        exportMeshButton.onClick.AddListener(() => item.ExportObjOnClick());
                        TooltipManager.AddTooltip(exportMeshButton.gameObject, "Export the renderer as a .obj.\n\nYou can use the <i>ExportBakedMesh</i> and <i>ExportBakedWorldPosition</i> config options to change the exporting behaviour");
                        break;
                    }

                case ItemInfo.RowItemType.RendererEnabled:
                    {
                        var panel = UIUtility.CreatePanel("RendererEnabledPanel", contentList.transform);
                        var itemPanelCanvas = panel.gameObject.AddComponent<CanvasGroup>();
                        panel.color = ItemColor;
                        AddHorizontalLayoutGroup(panel);

                        var label = UIUtility.CreateText("RendererEnabledLabel", panel.transform, "");
                        label.alignment = TextAnchor.MiddleLeft;
                        label.color = Color.black;
                        AddLayoutElement(label.gameObject, LabelWidth, LabelWidth, 1f);

                        var toggle = UIUtility.CreateToggle("RendererEnabledToggle", panel.transform, "");
                        toggle.isOn = true;
                        AddLayoutElement(toggle.gameObject, RendererToggleWidth, RendererToggleWidth, 0f);

                        var reset = UIUtility.CreateButton($"RendererEnabledResetButton", panel.transform, "Reset");
                        AddLayoutElement(reset.gameObject, ResetButtonWidth, ResetButtonWidth, 0f);
                        
                        SetLabelText(label, item.LabelText, item.RendererEnabled != item.RendererEnabledOriginal, reset, itemPanelCanvas);
                        toggle.isOn = item.RendererEnabled;
                        toggle.onValueChanged.AddListener(value =>
                        {
                            item.RendererEnabled = value;
                            if (item.RendererEnabled != item.RendererEnabledOriginal)
                                item.RendererEnabledOnChange(value);
                            else
                                item.RendererEnabledOnReset();
                            SetLabelText(label, item.LabelText, item.RendererEnabled != item.RendererEnabledOriginal, reset, itemPanelCanvas);
                        });
                        TooltipManager.AddTooltip(toggle.gameObject, "Toggle the visibility of this renderer on/off");

                        reset.onClick.AddListener(() => toggle.isOn = item.RendererEnabledOriginal);
                        TooltipManager.AddTooltip(reset.gameObject, "Reset this property to its original value");
                        
                        break;
                    }
                
                case ItemInfo.RowItemType.RendererShadowCastingMode:
                    {
                        var panel = UIUtility.CreatePanel("RendererShadowCastingModePanel", contentList.transform);
                        var itemPanelCanvas = panel.gameObject.AddComponent<CanvasGroup>();
                        panel.color = ItemColor;
                        AddHorizontalLayoutGroup(panel);

                        var label = UIUtility.CreateText("RendererShadowCastingModeLabel", panel.transform, "");
                        label.alignment = TextAnchor.MiddleLeft;
                        label.color = Color.black;
                        AddLayoutElement(label.gameObject, LabelWidth, LabelWidth, 1f);

                        Dropdown dropdown = UIUtility.CreateDropdown("RendererShadowCastingModeDropdown", panel.transform);
                        dropdown.transform.SetRect(0f, 0f, 0f, 1f, 0f, 0f, 100f);
                        dropdown.captionText.transform.SetRect(0f, 0f, 1f, 1f, 5f, 2f, -15f, -2f);
                        dropdown.captionText.alignment = TextAnchor.MiddleLeft;
                        dropdown.options.Clear();
                        dropdown.options.Add(new Dropdown.OptionData("Off"));
                        dropdown.options.Add(new Dropdown.OptionData("On"));
                        dropdown.options.Add(new Dropdown.OptionData("Two Sided"));
                        dropdown.options.Add(new Dropdown.OptionData("Shadows Only"));
                        dropdown.value = 0;
                        dropdown.captionText.text = "Off";
                        AddLayoutElement(dropdown.gameObject, RendererDropdownWidth, RendererDropdownWidth, 0f);

                        var reset = UIUtility.CreateButton($"RendererShadowCastingModeResetButton", panel.transform, "Reset");
                        AddLayoutElement(reset.gameObject, ResetButtonWidth, ResetButtonWidth, 0f);
                        
                        SetLabelText(label, item.LabelText, item.RendererShadowCastingMode != item.RendererShadowCastingModeOriginal, reset, itemPanelCanvas);
                        dropdown.value = item.RendererShadowCastingMode;
                        dropdown.onValueChanged.AddListener(value =>
                        {
                            item.RendererShadowCastingMode = value;
                            if (item.RendererShadowCastingMode != item.RendererShadowCastingModeOriginal)
                                item.RendererShadowCastingModeOnChange(value);
                            else
                                item.RendererShadowCastingModeOnReset();
                            SetLabelText(label, item.LabelText, item.RendererShadowCastingMode != item.RendererShadowCastingModeOriginal, reset, itemPanelCanvas);
                        });
                        TooltipManager.AddTooltip(dropdown.gameObject, @"- Off: Renderer casts no shadows
- On: Renderer casts shadows
- Two Sided: Always cast shadows from any direction, even for single sided objects
- Shadows Only: Renderer is invisible but still casts shadows");

                        reset.onClick.AddListener(() => dropdown.value = item.RendererShadowCastingModeOriginal);
                        TooltipManager.AddTooltip(reset.gameObject, "Reset this property to its original value");
                        
                        break;
                    }
                
                case ItemInfo.RowItemType.RendererReceiveShadows:
                    {
                        var panel = UIUtility.CreatePanel("RendererReceiveShadowsPanel", contentList.transform);
                        var itemPanelCanvas = panel.gameObject.AddComponent<CanvasGroup>();
                        panel.color = ItemColor;
                        AddHorizontalLayoutGroup(panel);

                        var label = UIUtility.CreateText("RendererReceiveShadowsLabel", panel.transform, "");
                        label.alignment = TextAnchor.MiddleLeft;
                        label.color = Color.black;
                        AddLayoutElement(label.gameObject, LabelWidth, LabelWidth, 1f);

                        Toggle toggle = UIUtility.CreateToggle("RendererReceiveShadowsToggle", panel.transform, "");
                        toggle.isOn = true;
                        AddLayoutElement(toggle.gameObject, RendererToggleWidth, RendererToggleWidth, 0f);

                        var reset = UIUtility.CreateButton($"RendererReceiveShadowsResetButton", panel.transform, "Reset");
                        AddLayoutElement(reset.gameObject, ResetButtonWidth, ResetButtonWidth, 0f);
                        
                        SetLabelText(label, item.LabelText, item.RendererReceiveShadows != item.RendererReceiveShadowsOriginal, reset, itemPanelCanvas);
                        toggle.isOn = item.RendererReceiveShadows;
                        toggle.onValueChanged.AddListener(value =>
                        {
                            item.RendererReceiveShadows = value;
                            if (item.RendererReceiveShadows != item.RendererReceiveShadowsOriginal)
                                item.RendererReceiveShadowsOnChange(value);
                            else
                                item.RendererReceiveShadowsOnReset();
                            SetLabelText(label, item.LabelText, item.RendererReceiveShadows != item.RendererReceiveShadowsOriginal, reset, itemPanelCanvas);
                        });
                        TooltipManager.AddTooltip(toggle.gameObject, "Toggle if the renderer can have shadows cast on it on/off");

                        reset.onClick.AddListener(() => toggle.isOn = item.RendererReceiveShadowsOriginal);
                        TooltipManager.AddTooltip(reset.gameObject, "Reset this property to its original value");
                        
                        break;
                    }
                
                case ItemInfo.RowItemType.RendererUpdateWhenOffscreen:
                    {
                        var panel = UIUtility.CreatePanel("RendererUpdateWhenOffscreenPanel", contentList.transform);
                        var itemPanelCanvas = panel.gameObject.AddComponent<CanvasGroup>();
                        panel.color = ItemColor;
                        AddHorizontalLayoutGroup(panel);

                        var label = UIUtility.CreateText("RendererUpdateWhenOffscreenLabel", panel.transform, "");
                        label.alignment = TextAnchor.MiddleLeft;
                        label.color = Color.black;
                        AddLayoutElement(label.gameObject, LabelWidth, LabelWidth, 1f);

                        Toggle toggle = UIUtility.CreateToggle("RendererUpdateWhenOffscreenToggle", panel.transform, "");
                        toggle.isOn = false;
                        AddLayoutElement(toggle.gameObject, RendererToggleWidth, RendererToggleWidth, 0f);

                        var reset = UIUtility.CreateButton($"RendererUpdateWhenOffscreenResetButton", panel.transform, "Reset");
                        AddLayoutElement(reset.gameObject, ResetButtonWidth, ResetButtonWidth, 0f);
                        
                        SetLabelText(label, item.LabelText, item.RendererUpdateWhenOffscreen != item.RendererUpdateWhenOffscreenOriginal, reset, itemPanelCanvas);
                        toggle.isOn = item.RendererUpdateWhenOffscreen;
                        toggle.onValueChanged.AddListener(value =>
                        {
                            item.RendererUpdateWhenOffscreen = value;
                            if (item.RendererUpdateWhenOffscreen != item.RendererUpdateWhenOffscreenOriginal)
                                item.RendererUpdateWhenOffscreenOnChange(value);
                            else
                                item.RendererUpdateWhenOffscreenOnReset();
                            SetLabelText(label, item.LabelText, item.RendererUpdateWhenOffscreen != item.RendererUpdateWhenOffscreenOriginal, reset, itemPanelCanvas);
                        });
                        TooltipManager.AddTooltip(toggle.gameObject, "When on, a renderer will always stay renderer, even when considered to be off-screen.\n\n This is handy for when the bounding box of an object is configured improperly and dissapears when it should still be visible");

                        reset.onClick.AddListener(() => toggle.isOn = item.RendererUpdateWhenOffscreenOriginal);
                        TooltipManager.AddTooltip(reset.gameObject, "Reset this property to its original value");
                        
                        break;
                    }
                
                case ItemInfo.RowItemType.RendererRecalculateNormals:
                    {
                        var panel = UIUtility.CreatePanel("RendererRecalculateNormalsPanel", contentList.transform);
                        var itemPanelCanvas = panel.gameObject.AddComponent<CanvasGroup>();
                        panel.color = ItemColor;
                        AddHorizontalLayoutGroup(panel);

                        var label = UIUtility.CreateText("RendererRecalculateNormalsLabel", panel.transform, "");
                        label.alignment = TextAnchor.MiddleLeft;
                        label.color = Color.black;
                        AddLayoutElement(label.gameObject, LabelWidth, LabelWidth, 1f);

                        Toggle toggle = UIUtility.CreateToggle("RendererRecalculateNormalsToggle", panel.transform, "");
                        toggle.isOn = false;
                        AddLayoutElement(toggle.gameObject, RendererToggleWidth, RendererToggleWidth, 0f);

                        var reset = UIUtility.CreateButton($"RendererRecalculateNormalsResetButton", panel.transform, "Reset");
                        AddLayoutElement(reset.gameObject, ResetButtonWidth, ResetButtonWidth, 0f);
                        
                        SetLabelText(label, item.LabelText, item.RendererRecalculateNormals != item.RendererRecalculateNormalsOriginal, reset, itemPanelCanvas);
                        toggle.isOn = item.RendererRecalculateNormals;
                        toggle.onValueChanged.AddListener(value =>
                        {
                            item.RendererRecalculateNormals = value;
                            if (item.RendererRecalculateNormals != item.RendererRecalculateNormalsOriginal)
                                item.RendererRecalculateNormalsOnChange(value);
                            else
                                item.RendererRecalculateNormalsOnReset();
                            SetLabelText(label, item.LabelText, item.RendererRecalculateNormals != item.RendererRecalculateNormalsOriginal, reset, itemPanelCanvas);
                        });
                        TooltipManager.AddTooltip(toggle.gameObject, "Recalculate the normals of this renderer based on its current shape, instead of its original shape.\n\nOnly available on skinned mesh renderers");

                        reset.onClick.AddListener(() => toggle.isOn = item.RendererRecalculateNormalsOriginal);
                        TooltipManager.AddTooltip(reset.gameObject, "Reset this property to its original value.\n\nIn order for the reset to take effect you need to either save and re-load the scene, or copy the object and delete the old one");
                        
                        break;
                    }
                
                case ItemInfo.RowItemType.Material:
                    {
                        var panel = UIUtility.CreatePanel("MaterialPanel", contentList.transform);
                        panel.gameObject.AddComponent<CanvasGroup>();
                        panel.color = MaterialColor;
                        AddHorizontalLayoutGroup(panel);

                        var label = UIUtility.CreateText("MaterialLabel", panel.transform, "");
                        label.alignment = TextAnchor.MiddleLeft;
                        label.color = Color.black;
                        AddLayoutElement(label.gameObject, 0f, 0f, 0f);

                        Text text = UIUtility.CreateText("MaterialText", panel.transform);
                        text.alignment = TextAnchor.MiddleLeft;
                        text.color = Color.black;
                        AddLayoutElement(text.gameObject, LabelWidth, LabelWidth, 1f);

                        var copyButton = UIUtility.CreateButton($"MaterialCopy", panel.transform, "Copy Edits");
                        AddLayoutElement(copyButton.gameObject, MaterialButtonWidth, MaterialButtonWidth, 0f);

                        var pasteButton = UIUtility.CreateButton($"MaterialPaste", panel.transform, "Paste Edits");
                        AddLayoutElement(pasteButton.gameObject, MaterialButtonWidth, MaterialButtonWidth, 0f);

                        var copyRemoveButton = UIUtility.CreateButton($"MaterialCopyRemove", panel.transform, "Copy Material");
                        AddLayoutElement(copyRemoveButton.gameObject, MaterialButtonWidth, MaterialButtonWidth, 0f);

                        var renameButton = UIUtility.CreateButton($"MaterialRename", panel.transform, ">");
                        AddLayoutElement(renameButton.gameObject, MaterialButtonWidth, MaterialButtonWidth, 0f);
                        
                        SetLabelText(label, item.LabelText);
                        text.text = item.MaterialName;
                        TooltipManager.AddTooltip(text.gameObject, "Material name");
                        copyButton.onClick.AddListener(() => item.MaterialOnCopy.Invoke());
                        TooltipManager.AddTooltip(copyButton.gameObject, "Copy all the <b>edits</b> of this material");
                        pasteButton.onClick.AddListener(() => item.MaterialOnPaste.Invoke());
                        TooltipManager.AddTooltip(pasteButton.gameObject, "Paste all the copied edits");
                        if (MaterialEditorPluginBase.CopyData.IsEmpty)
                        {
                            pasteButton.enabled = false;
                            pasteButton.GetComponentInChildren<Text>().color = Color.gray;
                        }
                        else
                        {
                            pasteButton.enabled = true;
                            pasteButton.GetComponentInChildren<Text>().color = Color.black;
                        }

                        if (item.MaterialName.Contains(MaterialAPI.MaterialCopyPostfix))
                        {
                            copyRemoveButton.GetComponentInChildren<Text>().text = "Remove Material";
                            TooltipManager.AddTooltip(copyRemoveButton.gameObject, "Remove this copied material");
                        }
                        else
                        {
                            copyRemoveButton.GetComponentInChildren<Text>().text = "Copy Material";
                            TooltipManager.AddTooltip(copyRemoveButton.gameObject, "Make a copy of this material.\n\nUseful for overlaying different effects onto an object with different material shaders/properties");
                        }
                        if (item.MaterialOnCopyRemove != null)
                        {
                            copyRemoveButton.onClick.AddListener(delegate { item.MaterialOnCopyRemove.Invoke(); });
                        }
                        else
                            copyRemoveButton.gameObject.SetActive(false);
                        if (item.MaterialOnRename != null)
                        {
                            renameButton.gameObject.SetActive(true);
                            renameButton.onClick.AddListener(delegate { item.MaterialOnRename.Invoke(); });
                        }
                        else
                            renameButton.gameObject.SetActive(false);
                        TooltipManager.AddTooltip(renameButton.gameObject, "Rename material instances");
                        
                        break;
                    }
                
                case ItemInfo.RowItemType.Shader:
                    {
                        var panel = UIUtility.CreatePanel("ShaderPanel", contentList.transform);
                        var itemPanelCanvas = panel.gameObject.AddComponent<CanvasGroup>();
                        panel.color = ItemColor;
                        AddHorizontalLayoutGroup(panel);

                        var label = UIUtility.CreateText("ShaderLabel", panel.transform, "");
                        label.alignment = TextAnchor.MiddleLeft;
                        label.color = Color.black;
                        AddLayoutElement(label.gameObject, LabelWidth, LabelWidth, 1f);

                        var interpolableButton = CreateInterpolableButton("SelectInterpolableShaderButton", panel.transform);

                        Dropdown dropdown = UIUtility.CreateDropdown("ShaderDropdown", panel.transform);
                        dropdown.transform.SetRect(0f, 0f, 0f, 1f, 0f, 0f, 100f);
                        dropdown.captionText.transform.SetRect(0f, 0f, 1f, 1f, 5f, 2f, -15f, -2f);
                        dropdown.captionText.alignment = TextAnchor.MiddleLeft;
                        dropdown.options.Clear();
                        dropdown.options.Add(new Dropdown.OptionData("Reset"));
                        foreach (var shader in MaterialEditorPluginBase.XMLShaderProperties)
                            if (shader.Key != "default")
                                dropdown.options.Add(new Dropdown.OptionData(shader.Key));
                        AddLayoutElement(dropdown.gameObject, ShaderDropdownWidth, ShaderDropdownWidth, 0f);

                        var reset = UIUtility.CreateButton($"ShaderResetButton", panel.transform, "Reset");
                        AddLayoutElement(reset.gameObject, ResetButtonWidth, ResetButtonWidth, 0f);
                        
                        SetLabelText(label, item.LabelText, item.ShaderName != item.ShaderNameOriginal, reset, itemPanelCanvas);
                        dropdown.value = dropdown.OptionIndex(item.ShaderName);
                        dropdown.captionText.text = item.ShaderName;
                        dropdown.onValueChanged.AddListener(value =>
                        {
                            var selected = dropdown.OptionText(value);
                            if (value == 0 || selected.IsNullOrEmpty())
                                selected = item.ShaderNameOriginal;
                            item.ShaderName = selected;

                            if (item.ShaderName != item.ShaderNameOriginal)
                                item.ShaderNameOnChange(item.ShaderName);
                            else
                                item.ShaderNameOnReset();
                            SetLabelText(label, item.LabelText, item.ShaderName != item.ShaderNameOriginal, reset, itemPanelCanvas);
                        });

                        reset.onClick.AddListener(() => dropdown.value = dropdown.OptionIndex(item.ShaderNameOriginal));
                        TooltipManager.AddTooltip(reset.gameObject, "Reset this property to its original value.\n\nIf the original shader is not one known by Material Editor, it will not be able to reset the shader to its original value. In order for the reset to take effect you to either save and re-load the scene, or copy the object and delete the old one");
                        interpolableButton.onClick.AddListener(() => item.SelectInterpolableButtonShaderOnClick());
                        TooltipManager.AddTooltip(interpolableButton.gameObject, "Select the currently selected shader property and its render queue as interpolables in timeline");

                        AutoScrollToSelectionWithDropdown.Setup(dropdown);
                        DropdownFilter.AddFilterUI(dropdown, "ShaderDropDown");
                        
                        break;
                    }
                
                case ItemInfo.RowItemType.ShaderRenderQueue:
                    {
                        var panel = UIUtility.CreatePanel("ShaderRenderQueuePanel", contentList.transform);
                        var itemPanelCanvas = panel.gameObject.AddComponent<CanvasGroup>();
                        panel.color = ItemColor;
                        AddHorizontalLayoutGroup(panel);

                        var label = UIUtility.CreateText("ShaderRenderQueueLabel", panel.transform, "");
                        label.alignment = TextAnchor.MiddleLeft;
                        label.color = Color.black;
                        AddLayoutElement(label.gameObject, LabelWidth, LabelWidth, 1f);

                        InputField inputField = UIUtility.CreateInputField("ShaderRenderQueueInput", panel.transform);
                        inputField.text = "0";
                        AddLayoutElement(inputField.gameObject, RenderQueueInputFieldWidth, RenderQueueInputFieldWidth, 0f);

                        var reset = UIUtility.CreateButton($"ShaderRenderQueueResetButton", panel.transform, "Reset");
                        AddLayoutElement(reset.gameObject, ResetButtonWidth, ResetButtonWidth, 0f);
                        
                        SetLabelText(label, item.LabelText, item.ShaderRenderQueue != item.ShaderRenderQueueOriginal, reset, itemPanelCanvas);
                        inputField.text = item.ShaderRenderQueue.ToString();
                        inputField.onEndEdit.AddListener(value =>
                        {
                            if (!int.TryParse(value, out int intValue))
                            {
                                inputField.text = item.ShaderRenderQueue.ToString();
                                return;
                            }

                            item.ShaderRenderQueue = intValue;
                            inputField.text = item.ShaderRenderQueue.ToString();

                            if (item.ShaderRenderQueue != item.ShaderRenderQueueOriginal)
                                item.ShaderRenderQueueOnChange(item.ShaderRenderQueue);
                            else
                                item.ShaderRenderQueueOnReset();
                            SetLabelText(label, item.LabelText, item.ShaderRenderQueue != item.ShaderRenderQueueOriginal, reset, itemPanelCanvas);
                        });
                        TooltipManager.AddTooltip(inputField.gameObject, "The order in which a material is rendered. Higher render queues get rendered later");

                        reset.onClick.AddListener(() =>
                        {
                            inputField.text = item.ShaderRenderQueueOriginal.ToString();
                            item.ShaderRenderQueue = item.ShaderRenderQueueOriginal;
                            item.ShaderRenderQueueOnReset();
                            SetLabelText(label, item.LabelText, item.ShaderRenderQueue != item.ShaderRenderQueueOriginal, reset, itemPanelCanvas);
                        });
                        TooltipManager.AddTooltip(reset.gameObject, "Reset this property to its original value");
                        
                        break;
                    }
                
                case ItemInfo.RowItemType.PropertyCategory:
                    {
                        var panel = UIUtility.CreatePanel("PropertyCategoryPanel", contentList.transform);
                        panel.gameObject.AddComponent<CanvasGroup>();
                        panel.color = CategoryColor;
                        AddHorizontalLayoutGroup(panel);

                        var label = UIUtility.CreateText("PropertyCategoryLabel", panel.transform, "");
                        label.alignment = TextAnchor.MiddleLeft;
                        label.color = Color.black;
                        AddLayoutElement(label.gameObject, LabelWidth, LabelWidth, 1f);
                        
                        SetLabelText(label, item.LabelText);
                        TooltipManager.AddTooltip(label.gameObject, "Category name");
                        
                        break;
                    }
                
                case ItemInfo.RowItemType.TextureProperty:
                    {
                        var panel = UIUtility.CreatePanel("TexturePanel", contentList.transform);
                        var itemPanelCanvas = panel.gameObject.AddComponent<CanvasGroup>();
                        panel.color = ItemColor;
                        AddHorizontalLayoutGroup(panel);

                        var label = UIUtility.CreateText("TextureLabel", panel.transform, "");
                        label.alignment = TextAnchor.MiddleLeft;
                        label.color = Color.black;
                        AddLayoutElement(label.gameObject, LabelWidth, LabelWidth, 1f);

                        var interpolableButton = CreateInterpolableButton("SelectInterpolableTextureButton", panel.transform);

                        Button exportButton = UIUtility.CreateButton($"TextureExportButton", panel.transform, $"Export Texture");
                        AddLayoutElement(exportButton.gameObject, TextureButtonWidth, TextureButtonWidth, 0f);

                        Button importButton = UIUtility.CreateButton($"TextureImportButton", panel.transform, $"Import Texture");
                        AddLayoutElement(importButton.gameObject, TextureButtonWidth, TextureButtonWidth, 0f);

                        var reset = UIUtility.CreateButton($"TextureResetButton", panel.transform, "Reset");
                        AddLayoutElement(reset.gameObject, ResetButtonWidth, ResetButtonWidth, 0f);
                        
                        SetLabelText(label, item.LabelText, item.TextureChanged, reset, itemPanelCanvas);

                        ConfigureExportButton();
                        void ConfigureExportButton()
                        {
                            if (item.TextureExists)
                            {
                                exportButton.enabled = true;
                                Text text = exportButton.GetComponentInChildren<Text>();
                                text.text = "Export Texture";
                                text.color = Color.black;
                            }
                            else
                            {
                                exportButton.enabled = false;
                                Text text = exportButton.GetComponentInChildren<Text>();
                                text.text = "No Texture";
                                text.color = Color.gray;
                            }
                        }

                        exportButton.onClick.AddListener(() => item.TextureOnExport());
                        importButton.onClick.AddListener(() =>
                        {
                            item.TextureChanged = true;
                            item.TextureExists = true;
                            item.TextureOnImport();
                            ConfigureExportButton();
                            SetLabelText(label, item.LabelText, item.TextureChanged, reset, itemPanelCanvas);
                        });

                        reset.onClick.AddListener(() =>
                        {
                            item.TextureChanged = false;
                            item.TextureOnReset();
                            SetLabelText(label, item.LabelText, item.TextureChanged, reset, itemPanelCanvas);
                        });
                        TooltipManager.AddTooltip(reset.gameObject, "Reset this property to its original value.\n\nIn order for the reset to take effect you need to either save and re-load the scene, or copy the object and delete the old one");
                        interpolableButton.onClick.AddListener(() => item.SelectInterpolableButtonTextureOnClick());
                        TooltipManager.AddTooltip(interpolableButton.gameObject, "Select the currently selected texture property and its offset and scale properties as interpolables in timeline");
                        
                        break;
                    }
                
                case ItemInfo.RowItemType.TextureOffsetScale:
                    {
                        var panel = UIUtility.CreatePanel("OffsetScalePanel", contentList.transform);
                        var itemPanelCanvas = panel.gameObject.AddComponent<CanvasGroup>();
                        panel.color = ItemColor;
                        AddHorizontalLayoutGroup(panel);

                        var label = UIUtility.CreateText("OffsetScaleLabel", panel.transform, "");
                        label.alignment = TextAnchor.MiddleLeft;
                        label.color = Color.black;
                        AddLayoutElement(label.gameObject, LabelWidth, LabelWidth, 1f);

                        Text emptySpace = UIUtility.CreateText("EmptySpace", panel.transform, "");
                        emptySpace.alignment = TextAnchor.MiddleLeft;
                        AddLayoutElement(emptySpace.gameObject, InterpolableButtonWidth, InterpolableButtonWidth, 0f);

                        Text labelOffsetX = UIUtility.CreateText("OffsetXText", panel.transform, "OffsetX");
                        labelOffsetX.alignment = TextAnchor.MiddleLeft;
                        labelOffsetX.color = Color.black;
                        AddLayoutElement(labelOffsetX.gameObject, OffsetScaleLabelXWidth, OffsetScaleLabelXWidth, 0f);

                        InputField textBoxOffsetX = UIUtility.CreateInputField("OffsetXInput", panel.transform);
                        textBoxOffsetX.text = "0";
                        textBoxOffsetX.characterLimit = 7;
                        AddLayoutElement(textBoxOffsetX.gameObject, OffsetScaleInputFieldWidth, OffsetScaleInputFieldWidth, 0f);

                        Text labelOffsetY = UIUtility.CreateText("OffsetYText", panel.transform, "Y");
                        labelOffsetY.alignment = TextAnchor.MiddleLeft;
                        labelOffsetY.color = Color.black;
                        AddLayoutElement(labelOffsetY.gameObject, OffsetScaleLabelYWidth, OffsetScaleLabelYWidth, 0f);

                        InputField textBoxOffsetY = UIUtility.CreateInputField("OffsetYInput", panel.transform);
                        textBoxOffsetY.text = "0";
                        textBoxOffsetY.characterLimit = 7;
                        AddLayoutElement(textBoxOffsetY.gameObject, OffsetScaleInputFieldWidth, OffsetScaleInputFieldWidth, 0f);

                        labelOffsetX.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(textBoxOffsetX, new[] { textBoxOffsetY });
                        labelOffsetY.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(textBoxOffsetY, new[] { textBoxOffsetX });

                        //Scale
                        Text labelScaleX = UIUtility.CreateText("ScaleXText", panel.transform, "ScaleX");
                        labelScaleX.alignment = TextAnchor.MiddleLeft;
                        labelScaleX.color = Color.black;
                        AddLayoutElement(labelScaleX.gameObject, OffsetScaleLabelXWidth, OffsetScaleLabelXWidth, 0f);

                        InputField textBoxScaleX = UIUtility.CreateInputField("ScaleXInput", panel.transform);
                        textBoxScaleX.text = "0";
                        textBoxScaleX.characterLimit = 7;
                        AddLayoutElement(textBoxScaleX.gameObject, OffsetScaleInputFieldWidth, OffsetScaleInputFieldWidth, 0f);

                        Text labelScaleY = UIUtility.CreateText("ScaleYText", panel.transform, "Y");
                        labelScaleY.alignment = TextAnchor.MiddleLeft;
                        labelScaleY.color = Color.black;
                        AddLayoutElement(labelScaleY.gameObject, OffsetScaleLabelYWidth, OffsetScaleLabelYWidth, 0f);

                        InputField textBoxScaleY = UIUtility.CreateInputField("ScaleYInput", panel.transform);
                        textBoxScaleY.text = "0";
                        textBoxScaleY.characterLimit = 7;
                        AddLayoutElement(textBoxScaleY.gameObject, OffsetScaleInputFieldWidth, OffsetScaleInputFieldWidth, 0f);

                        var reset = UIUtility.CreateButton($"OffsetScaleResetButton", panel.transform, "Reset");
                        AddLayoutElement(reset.gameObject, ResetButtonWidth, ResetButtonWidth, 0f);

                        labelScaleX.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(textBoxScaleX, new[] { textBoxScaleY });
                        labelScaleY.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(textBoxScaleY, new[] { textBoxScaleX });
                        
                        SetLabelText(label, item.LabelText, item.Offset != item.OffsetOriginal || item.Scale != item.ScaleOriginal, reset, itemPanelCanvas);

                        textBoxOffsetX.text = item.Offset.x.ToString();
                        textBoxOffsetY.text = item.Offset.y.ToString();
                        textBoxScaleX.text = item.Scale.x.ToString();
                        textBoxScaleY.text = item.Scale.y.ToString();

                        textBoxOffsetX.onEndEdit.AddListener(value =>
                        {
                            if (!float.TryParse(value, out float input))
                            {
                                textBoxOffsetX.text = item.Offset.x.ToString();
                                return;
                            }

                            item.Offset = new Vector2(input, item.Offset.y);
                            textBoxOffsetX.text = item.Offset.x.ToString();

                            if (item.Offset == item.OffsetOriginal)
                                item.OffsetOnReset();
                            else
                                item.OffsetOnChange(item.Offset);

                            SetLabelText(label, item.LabelText, item.Offset != item.OffsetOriginal || item.Scale != item.ScaleOriginal, reset, itemPanelCanvas);
                        });

                        textBoxOffsetY.onEndEdit.AddListener(value =>
                        {
                            if (!float.TryParse(value, out float input))
                            {
                                textBoxOffsetY.text = item.Offset.y.ToString();
                                return;
                            }

                            item.Offset = new Vector2(item.Offset.x, input);
                            textBoxOffsetY.text = item.Offset.y.ToString();

                            if (item.Offset == item.OffsetOriginal)
                                item.OffsetOnReset();
                            else
                                item.OffsetOnChange(item.Offset);

                            SetLabelText(label, item.LabelText, item.Offset != item.OffsetOriginal || item.Scale != item.ScaleOriginal, reset, itemPanelCanvas);
                        });

                        textBoxScaleX.onEndEdit.AddListener(value =>
                        {
                            if (!float.TryParse(value, out float input))
                            {
                                textBoxScaleX.text = item.Scale.x.ToString();
                                return;
                            }

                            item.Scale = new Vector2(input, item.Scale.y);
                            textBoxScaleX.text = item.Scale.x.ToString();

                            if (item.Scale == item.ScaleOriginal)
                                item.ScaleOnReset();
                            else
                                item.ScaleOnChange(item.Scale);

                            SetLabelText(label, item.LabelText, item.Offset != item.OffsetOriginal || item.Scale != item.ScaleOriginal, reset, itemPanelCanvas);
                        });

                        textBoxScaleY.onEndEdit.AddListener(value =>
                        {
                            if (!float.TryParse(value, out float input))
                            {
                                textBoxScaleY.text = item.Scale.y.ToString();
                                return;
                            }

                            item.Scale = new Vector2(item.Scale.x, input);
                            textBoxScaleY.text = item.Scale.y.ToString();

                            if (item.Scale == item.ScaleOriginal)
                                item.ScaleOnReset();
                            else
                                item.ScaleOnChange(item.Scale);

                            SetLabelText(label, item.LabelText, item.Offset != item.OffsetOriginal || item.Scale != item.ScaleOriginal, reset, itemPanelCanvas);
                        });

                        reset.onClick.AddListener(() =>
                        {
                            item.Offset = item.OffsetOriginal;
                            item.Scale = item.ScaleOriginal;

                            textBoxOffsetX.text = item.Offset.x.ToString();
                            textBoxOffsetY.text = item.Offset.y.ToString();
                            textBoxScaleX.text = item.Scale.x.ToString();
                            textBoxScaleY.text = item.Scale.y.ToString();

                            item.OffsetOnReset();
                            item.ScaleOnReset();
                            SetLabelText(label, item.LabelText, item.Offset != item.OffsetOriginal || item.Scale != item.ScaleOriginal, reset, itemPanelCanvas);
                        });
                        TooltipManager.AddTooltip(textBoxOffsetX.gameObject, "Adjust the horizontal offset of the texture. It can move the texture left or right.");
                        TooltipManager.AddTooltip(textBoxOffsetY.gameObject, "Adjust the vertical offset of the texture. It can move the texture up or down.");
                        TooltipManager.AddTooltip(textBoxScaleX.gameObject, "Adjust the horizontal scale of the texture. Values greater than 1 make the texture appear smaller horizontally, values less than 1 make it appear larger horizontally.");
                        TooltipManager.AddTooltip(textBoxScaleY.gameObject, "Adjust the vertical scale of the texture. Values greater than 1 make the texture appear smaller vertically, values less than 1 make it appear larger vertically.");
                        TooltipManager.AddTooltip(reset.gameObject, "Reset both the scale and offset properties to their original values");
                        
                        break;
                    }
                
                case ItemInfo.RowItemType.ColorProperty:
                    {
                        var panel = UIUtility.CreatePanel("ColorPanel", contentList.transform);
                        var itemPanelCanvas = panel.gameObject.AddComponent<CanvasGroup>();
                        panel.color = ItemColor;
                        AddHorizontalLayoutGroup(panel);

                        var label = UIUtility.CreateText("ColorLabel", panel.transform, "");
                        label.alignment = TextAnchor.MiddleLeft;
                        label.color = Color.black;
                        AddLayoutElement(label.gameObject, LabelWidth, LabelWidth, 1f);

                        var interpolableButton = CreateInterpolableButton("SelectInterpolableColorButton", panel.transform);

                        Text labelR = UIUtility.CreateText("ColorRText", panel.transform, "R");
                        labelR.alignment = TextAnchor.MiddleLeft;
                        labelR.color = Color.black;
                        AddLayoutElement(labelR.gameObject, ColorLabelWidth, ColorLabelWidth, 0f);

                        InputField textBoxR = UIUtility.CreateInputField("ColorRInput", panel.transform);
                        textBoxR.text = "0";
                        textBoxR.characterLimit = 7;
                        AddLayoutElement(textBoxR.gameObject, ColorInputFieldWidth, ColorInputFieldWidth, 0f);

                        Text labelG = UIUtility.CreateText("ColorGText", panel.transform, "G");
                        labelG.alignment = TextAnchor.MiddleLeft;
                        labelG.color = Color.black;
                        AddLayoutElement(labelG.gameObject, ColorLabelWidth, ColorLabelWidth, 0f);

                        InputField textBoxG = UIUtility.CreateInputField("ColorGInput", panel.transform);
                        textBoxG.text = "0";
                        textBoxG.characterLimit = 7;
                        AddLayoutElement(textBoxG.gameObject, ColorInputFieldWidth, ColorInputFieldWidth, 0f);

                        Text labelB = UIUtility.CreateText("ColorBText", panel.transform, "B");
                        labelB.alignment = TextAnchor.MiddleLeft;
                        labelB.color = Color.black;
                        AddLayoutElement(labelB.gameObject, ColorLabelWidth, ColorLabelWidth, 0f);

                        InputField textBoxB = UIUtility.CreateInputField("ColorBInput", panel.transform);
                        textBoxB.text = "0";
                        textBoxB.characterLimit = 7;
                        AddLayoutElement(textBoxB.gameObject, ColorInputFieldWidth, ColorInputFieldWidth, 0f);

                        Text labelA = UIUtility.CreateText("ColorAText", panel.transform, "A");
                        labelA.alignment = TextAnchor.MiddleLeft;
                        labelA.color = Color.black;
                        AddLayoutElement(labelA.gameObject, ColorLabelWidth, ColorLabelWidth, 0f);

                        InputField textBoxA = UIUtility.CreateInputField("ColorAInput", panel.transform);
                        textBoxA.text = "0";
                        textBoxA.characterLimit = 7;
                        AddLayoutElement(textBoxA.gameObject, ColorInputFieldWidth, ColorInputFieldWidth, 0f);

                        var edit = UIUtility.CreateButton("ColorEditButton", panel.transform, "");
                        AddLayoutElement(edit.gameObject, ColorEditButtonWidth, ColorEditButtonWidth, 0f);

                        var reset = UIUtility.CreateButton($"ColorResetButton", panel.transform, "Reset");
                        AddLayoutElement(reset.gameObject, ResetButtonWidth, ResetButtonWidth, 0f);

                        labelR.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(textBoxR, new[] { textBoxG, textBoxB });
                        labelG.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(textBoxG, new[] { textBoxR, textBoxB });
                        labelB.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(textBoxB, new[] { textBoxR, textBoxG });
                        labelA.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(textBoxA);
                        
                        SetLabelText(label, item.LabelText, item.ColorValue != item.ColorValueOriginal, reset, itemPanelCanvas);
                        
                        textBoxR.text = item.ColorValue.r.ToString();
                        textBoxG.text = item.ColorValue.g.ToString();
                        textBoxB.text = item.ColorValue.b.ToString();
                        textBoxA.text = item.ColorValue.a.ToString();

                        edit.image.color = item.ColorValue;

                        textBoxR.onEndEdit.AddListener(value =>
                        {
                            if (!float.TryParse(value, out float input))
                            {
                                textBoxR.text = item.ColorValue.r.ToString();
                                return;
                            }

                            item.ColorValue = new Color(input, item.ColorValue.g, item.ColorValue.b, item.ColorValue.a);
                            textBoxR.text = item.ColorValue.r.ToString();

                            if (item.ColorValue == item.ColorValueOriginal)
                                item.ColorValueOnReset();
                            else
                                item.ColorValueOnChange(item.ColorValue);

                            edit.image.color = item.ColorValue;
                            item.ColorValueSetToPalette(item.LabelText, item.ColorValue);

                            SetLabelText(label, item.LabelText, item.ColorValue != item.ColorValueOriginal, reset, itemPanelCanvas);
                        });

                        textBoxG.onEndEdit.AddListener(value =>
                        {
                            if (!float.TryParse(value, out float input))
                            {
                                textBoxG.text = item.ColorValue.g.ToString();
                                return;
                            }

                            item.ColorValue = new Color(item.ColorValue.r, input, item.ColorValue.b, item.ColorValue.a);
                            textBoxG.text = item.ColorValue.g.ToString();

                            if (item.ColorValue == item.ColorValueOriginal)
                                item.ColorValueOnReset();
                            else
                                item.ColorValueOnChange(item.ColorValue);

                            edit.image.color = item.ColorValue;
                            item.ColorValueSetToPalette(item.LabelText, item.ColorValue);

                            SetLabelText(label, item.LabelText, item.ColorValue != item.ColorValueOriginal, reset, itemPanelCanvas);
                        });

                        textBoxB.onEndEdit.AddListener(value =>
                        {
                            if (!float.TryParse(value, out float input))
                            {
                                textBoxB.text = item.ColorValue.b.ToString();
                                return;
                            }

                            item.ColorValue = new Color(item.ColorValue.r, item.ColorValue.g, input, item.ColorValue.a);
                            textBoxB.text = item.ColorValue.b.ToString();

                            if (item.ColorValue == item.ColorValueOriginal)
                                item.ColorValueOnReset();
                            else
                                item.ColorValueOnChange(item.ColorValue);

                            edit.image.color = item.ColorValue;
                            item.ColorValueSetToPalette(item.LabelText, item.ColorValue);

                            SetLabelText(label, item.LabelText, item.ColorValue != item.ColorValueOriginal, reset, itemPanelCanvas);
                        });

                        textBoxA.onEndEdit.AddListener(value =>
                        {
                            if (!float.TryParse(value, out float input))
                            {
                                textBoxA.text = item.ColorValue.a.ToString();
                                return;
                            }

                            item.ColorValue = new Color(item.ColorValue.r, item.ColorValue.g, item.ColorValue.b, input);
                            textBoxA.text = item.ColorValue.a.ToString();

                            if (item.ColorValue == item.ColorValueOriginal)
                                item.ColorValueOnReset();
                            else
                                item.ColorValueOnChange(item.ColorValue);

                            edit.image.color = item.ColorValue;
                            item.ColorValueSetToPalette(item.LabelText, item.ColorValue);

                            SetLabelText(label, item.LabelText, item.ColorValue != item.ColorValueOriginal, reset, itemPanelCanvas);
                        });

                        reset.onClick.AddListener(() =>
                        {
                            item.ColorValue = item.ColorValueOriginal;

                            textBoxR.text = item.ColorValue.r.ToString();
                            textBoxG.text = item.ColorValue.g.ToString();
                            textBoxB.text = item.ColorValue.b.ToString();
                            textBoxA.text = item.ColorValue.a.ToString();

                            edit.image.color = item.ColorValue;
                            item.ColorValueSetToPalette(item.LabelText, item.ColorValue);

                            item.ColorValueOnReset();
                            SetLabelText(label, item.LabelText, item.ColorValue != item.ColorValueOriginal, reset, itemPanelCanvas);
                        });
                        TooltipManager.AddTooltip(reset.gameObject, "Reset the selected property to its original value");

                        edit.onClick.AddListener(() =>
                        {
                            item.ColorValueOnEdit(item.LabelText, item.ColorValue, onChanged);

                            void onChanged(Color c)
                            {
                                edit.image.color = c;
                                item.ColorValue = c;

                                textBoxR.text = c.r.ToString();
                                textBoxG.text = c.g.ToString();
                                textBoxB.text = c.b.ToString();
                                textBoxA.text = c.a.ToString();

                                if (item.ColorValue == item.ColorValueOriginal)
                                    item.ColorValueOnReset();
                                else
                                    item.ColorValueOnChange(item.ColorValue);

                                SetLabelText(label, item.LabelText, item.ColorValue != item.ColorValueOriginal, reset, itemPanelCanvas);
                            }
                        });
                        interpolableButton.onClick.AddListener(() => item.SelectInterpolableButtonColorOnClick());
                        TooltipManager.AddTooltip(interpolableButton.gameObject, "Select currently selected color property as interpolable in timeline");
                        
                        break;
                    }
                
                case ItemInfo.RowItemType.FloatProperty:
                    {
                        var panel = UIUtility.CreatePanel("FloatPanel", contentList.transform);
                        var itemPanelCanvas = panel.gameObject.AddComponent<CanvasGroup>();
                        panel.color = ItemColor;
                        AddHorizontalLayoutGroup(panel);

                        var label = UIUtility.CreateText("FloatLabel", panel.transform, "");
                        label.alignment = TextAnchor.MiddleLeft;
                        label.color = Color.black;
                        AddLayoutElement(label.gameObject, LabelWidth, LabelWidth, 1f);

                        var interpolableButton = CreateInterpolableButton("SelectInterpolableFloatButton", panel.transform);

                        Slider sliderFloat = UIUtility.CreateSlider("FloatSlider", panel.transform);
                        AddLayoutElement(sliderFloat.gameObject, FloatInputFieldWidth, FloatInputFieldWidth, 0f);

                        InputField textBoxFloat = UIUtility.CreateInputField("FloatInputField", panel.transform);
                        textBoxFloat.text = "0";
                        textBoxFloat.characterLimit = 9;
                        AddLayoutElement(textBoxFloat.gameObject, FloatInputFieldWidth, FloatInputFieldWidth, 0f);

                        var reset = UIUtility.CreateButton($"FloatResetButton", panel.transform, "Reset");
                        AddLayoutElement(reset.gameObject, ResetButtonWidth, ResetButtonWidth, 0f);
                        label.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(textBoxFloat);
                        
                        SetLabelText(label, item.LabelText, item.FloatValue != item.FloatValueOriginal, reset, itemPanelCanvas);

                        sliderFloat.minValue = item.FloatValueSliderMin;
                        sliderFloat.maxValue = item.FloatValueSliderMax;
                        sliderFloat.value = item.FloatValue;
                        textBoxFloat.text = item.FloatValue.ToString();

                        sliderFloat.onValueChanged.AddListener(value =>
                        {
                            textBoxFloat.text = value.ToString();
                            textBoxFloat.onEndEdit.Invoke(value.ToString());
                        });

                        textBoxFloat.onEndEdit.AddListener(value =>
                        {
                            if (!float.TryParse(value, out float input))
                            {
                                textBoxFloat.text = item.FloatValue.ToString();
                                return;
                            }
                            item.FloatValue = input;
                            textBoxFloat.text = item.FloatValue.ToString();

                            sliderFloat.Set(item.FloatValue, sendCallback: false);

                            if (item.FloatValue == item.FloatValueOriginal)
                                item.FloatValueOnReset();
                            else
                                item.FloatValueOnChange(item.FloatValue);

                            SetLabelText(label, item.LabelText, item.FloatValue != item.FloatValueOriginal, reset, itemPanelCanvas);
                        });

                        reset.onClick.AddListener(() =>
                        {
                            item.FloatValue = item.FloatValueOriginal;

                            sliderFloat.Set(item.FloatValue);
                            textBoxFloat.text = item.FloatValue.ToString();

                            item.FloatValueOnReset();
                            SetLabelText(label, item.LabelText, item.FloatValue != item.FloatValueOriginal, reset, itemPanelCanvas);
                        });
                        TooltipManager.AddTooltip(reset.gameObject, "Reset the selected property to its original value");
                        interpolableButton.onClick.AddListener(() => item.SelectInterpolableButtonFloatOnClick());
                        TooltipManager.AddTooltip(interpolableButton.gameObject, "Select currently selected float property as interpolable in timeline");
                        
                        break;
                    }
                
                case ItemInfo.RowItemType.KeywordProperty:
                    {
                        var panel = UIUtility.CreatePanel("KeywordPanel", contentList.transform);
                        var itemPanelCanvas = panel.gameObject.AddComponent<CanvasGroup>();
                        panel.color = ItemColor;
                        AddHorizontalLayoutGroup(panel);

                        var label = UIUtility.CreateText("KeywordLabel", panel.transform, "");
                        label.alignment = TextAnchor.MiddleLeft;
                        label.color = Color.black;
                        AddLayoutElement(label.gameObject, LabelWidth, LabelWidth, 1f);

                        Text text = UIUtility.CreateText("EmptySpace", panel.transform, "");
                        text.alignment = TextAnchor.MiddleLeft;
                        AddLayoutElement(text.gameObject, InterpolableButtonWidth, InterpolableButtonWidth, 0f);

                        Toggle toggle = UIUtility.CreateToggle("KeywordToggle", panel.transform, "");
                        AddLayoutElement(toggle.gameObject, RendererToggleWidth, RendererToggleWidth, 0f);

                        var reset = UIUtility.CreateButton($"KeywordResetButton", panel.transform, "Reset");
                        AddLayoutElement(reset.gameObject, ResetButtonWidth, ResetButtonWidth, 0f);
                        
                        SetLabelText(label, item.LabelText, item.KeywordValue != item.KeywordValueOriginal, reset, itemPanelCanvas);

                        toggle.isOn = item.KeywordValue;
                        toggle.onValueChanged.AddListener(value =>
                        {
                            item.KeywordValue = value;

                            if (item.KeywordValue == item.KeywordValueOriginal)
                                item.KeywordValueOnReset();
                            else
                                item.KeywordValueOnChange(item.KeywordValue);

                            SetLabelText(label, item.LabelText, item.KeywordValue != item.KeywordValueOriginal, reset, itemPanelCanvas);
                        });

                        reset.onClick.AddListener(() =>
                        {
                            item.KeywordValue = item.KeywordValueOriginal;

                            toggle.Set(item.KeywordValue);

                            item.KeywordValueOnReset();
                            SetLabelText(label, item.LabelText, item.KeywordValue != item.KeywordValueOriginal, reset, itemPanelCanvas);
                        });
                        TooltipManager.AddTooltip(reset.gameObject, "Reset the selected property to its original value");
                        
                        break;
                    }
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(item.ItemType), item.ItemType, null);
            }
            
            return contentList.gameObject;
        }
        private static void AddHorizontalLayoutGroup(Image panel)
        {
            var itemHLG = panel.gameObject.AddComponent<HorizontalLayoutGroup>();
            itemHLG.padding = Padding;
            itemHLG.childForceExpandWidth = false;
        }

        private static void AddLayoutElement(GameObject gameObject, float minWidth, float preferredWidth, float  flexibleWidth)
        {
            var layoutElement = gameObject.AddComponent<LayoutElement>();
            layoutElement.minWidth = minWidth;
            layoutElement.preferredWidth = preferredWidth;
            layoutElement.flexibleWidth = flexibleWidth;
        }
        
        private static void SetLabelText(Text label, string text) { 
            label.text = text ?? "";
        }

        private static void SetLabelText(Text label, string text, bool valueChanged, Button resetBtn, CanvasGroup panel)
        {
            label.text = text ?? "";

            if (valueChanged)
            {
                panel.gameObject.GetComponent<Image>().color = MaterialEditorUI.ItemColorChanged;
                if (resetBtn)
                    resetBtn.interactable = true;
            }
            else
            {
                panel.gameObject.GetComponent<Image>().color = MaterialEditorUI.ItemColor;
                if (resetBtn)
                    resetBtn.interactable = false;
            }
        }

        private static Button CreateInterpolableButton(string objectName, Transform parent)
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

            return interpolableButton;
        }
    }
}
