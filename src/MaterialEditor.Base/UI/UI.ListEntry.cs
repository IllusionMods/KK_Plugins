﻿using System;
using UnityEngine;
using UnityEngine.UI;
using static UILib.Extensions;

namespace MaterialEditorAPI
{
    internal class ListEntry : MonoBehaviour
    {
        public CanvasGroup RendererPanel;
        public Text RendererLabel;
        public Text RendererText;
        public Button SelectInterpolableRendererButton;
        public Button ExportUVButton;
        public Button ExportObjButton;

        public CanvasGroup RendererEnabledPanel;
        public Text RendererEnabledLabel;
        public Toggle RendererEnabledToggle;
        public Button RendererEnabledResetButton;

        public CanvasGroup RendererShadowCastingModePanel;
        public Text RendererShadowCastingModeLabel;
        public Dropdown RendererShadowCastingModeDropdown;
        public Button RendererShadowCastingModeResetButton;

        public CanvasGroup RendererReceiveShadowsPanel;
        public Text RendererReceiveShadowsLabel;
        public Toggle RendererReceiveShadowsToggle;
        public Button RendererReceiveShadowsResetButton;

        public CanvasGroup RendererUpdateWhenOffscreenPanel;
        public Text RendererUpdateWhenOffscreenLabel;
        public Toggle RendererUpdateWhenOffscreenToggle;
        public Button RendererUpdateWhenOffscreenResetButton;

        public CanvasGroup RendererRecalculateNormalsPanel;
        public Text RendererRecalculateNormalsLabel;
        public Toggle RendererRecalculateNormalsToggle;
        public Button RendererRecalculateNormalsResetButton;

        public CanvasGroup MaterialPanel;
        public Text MaterialLabel;
        public Text MaterialText;
        public Button SelectInterpolableMaterialButton;
        public Button MaterialCopyButton;
        public Button MaterialPasteButton;
        public Button MaterialCopyRemove;

        public CanvasGroup ShaderPanel;
        public Text ShaderLabel;
        public Dropdown ShaderDropdown;
        public Button SelectInterpolableShaderButton;
        public Button ShaderResetButton;

        public CanvasGroup ShaderRenderQueuePanel;
        public Text ShaderRenderQueueLabel;
        public InputField ShaderRenderQueueInput;
        public Button ShaderRenderQueueResetButton;

        public CanvasGroup TexturePanel;
        public Text TextureLabel;
        public Button SelectInterpolableTextureButton;
        public Button ExportTextureButton;
        public Button ImportTextureButton;
        public Button TextureResetButton;

        public CanvasGroup OffsetScalePanel;
        public Text OffsetScaleLabel;
        public Text OffsetXText;
        public InputField OffsetXInput;
        public Text OffsetYText;
        public InputField OffsetYInput;
        public Text ScaleXText;
        public InputField ScaleXInput;
        public Text ScaleYText;
        public InputField ScaleYInput;
        public Button OffsetScaleResetButton;

        public CanvasGroup ColorPanel;
        public Text ColorLabel;
        public Text ColorRText;
        public Text ColorGText;
        public Text ColorBText;
        public Text ColorAText;
        public InputField ColorRInput;
        public InputField ColorGInput;
        public InputField ColorBInput;
        public InputField ColorAInput;
        public Button SelectInterpolableColorButton;
        public Button ColorResetButton;
        public Button ColorEditButton;

        public CanvasGroup FloatPanel;
        public Text FloatLabel;
        public Button SelectInterpolableFloatButton;
        public Slider FloatSlider;
        public InputField FloatInputField;
        public Button FloatResetButton;

        public CanvasGroup KeywordPanel;
        public Text KeywordLabel;
        public Toggle KeywordToggle;
        public Button KeywordResetButton;

        private ItemInfo _currentItem;

        public ItemInfo CurrentItem
        {
            get => _currentItem;
            set => SetItem(value, false);
        }

        public void SetItem(ItemInfo item, bool force)
        {
            if (!force && ReferenceEquals(item, _currentItem)) return;

            _currentItem = item;

            HideAll();
            if (item != null)
            {
                switch (item.ItemType)
                {
                    case ItemInfo.RowItemType.Renderer:
                        ShowRenderer();
                        SetLabelText(RendererLabel, item.LabelText);
                        ExportUVButton.onClick.RemoveAllListeners();
                        ExportUVButton.onClick.AddListener(() => item.ExportUVOnClick());
                        ExportObjButton.onClick.RemoveAllListeners();
                        ExportObjButton.onClick.AddListener(() => item.ExportObjOnClick());
                        SelectInterpolableRendererButton.onClick.RemoveAllListeners();
                        SelectInterpolableRendererButton.onClick.AddListener(() => item.SelectInterpolableButtonRendererOnClick());
                        RendererText.text = item.RendererName;
                        break;
                    case ItemInfo.RowItemType.RendererEnabled:
                        ShowRendererEnabled();
                        SetLabelText(RendererEnabledLabel, item.LabelText, item.RendererEnabled != item.RendererEnabledOriginal);
                        RendererEnabledToggle.onValueChanged.RemoveAllListeners();
                        RendererEnabledToggle.isOn = item.RendererEnabled;
                        RendererEnabledToggle.onValueChanged.AddListener(value =>
                        {
                            item.RendererEnabled = value;
                            if (item.RendererEnabled != item.RendererEnabledOriginal)
                                item.RendererEnabledOnChange(value);
                            else
                                item.RendererEnabledOnReset();
                            SetLabelText(RendererEnabledLabel, item.LabelText, item.RendererEnabled != item.RendererEnabledOriginal);
                        });

                        RendererEnabledResetButton.onClick.RemoveAllListeners();
                        RendererEnabledResetButton.onClick.AddListener(() => RendererEnabledToggle.isOn = item.RendererEnabledOriginal);

                        break;
                    case ItemInfo.RowItemType.RendererShadowCastingMode:
                        ShowRendererShadowCastingMode();
                        SetLabelText(RendererShadowCastingModeLabel, item.LabelText, item.RendererShadowCastingMode != item.RendererShadowCastingModeOriginal);
                        RendererShadowCastingModeDropdown.onValueChanged.RemoveAllListeners();
                        RendererShadowCastingModeDropdown.value = item.RendererShadowCastingMode;
                        RendererShadowCastingModeDropdown.onValueChanged.AddListener(value =>
                        {
                            item.RendererShadowCastingMode = value;
                            if (item.RendererShadowCastingMode != item.RendererShadowCastingModeOriginal)
                                item.RendererShadowCastingModeOnChange(value);
                            else
                                item.RendererShadowCastingModeOnReset();
                            SetLabelText(RendererShadowCastingModeLabel, item.LabelText, item.RendererShadowCastingMode != item.RendererShadowCastingModeOriginal);
                        });

                        RendererShadowCastingModeResetButton.onClick.RemoveAllListeners();
                        RendererShadowCastingModeResetButton.onClick.AddListener(() => RendererShadowCastingModeDropdown.value = item.RendererShadowCastingModeOriginal);

                        break;
                    case ItemInfo.RowItemType.RendererReceiveShadows:
                        ShowRendererReceiveShadows();
                        SetLabelText(RendererReceiveShadowsLabel, item.LabelText, item.RendererReceiveShadows != item.RendererReceiveShadowsOriginal);
                        RendererReceiveShadowsToggle.onValueChanged.RemoveAllListeners();
                        RendererReceiveShadowsToggle.isOn = item.RendererReceiveShadows;
                        RendererReceiveShadowsToggle.onValueChanged.AddListener(value =>
                        {
                            item.RendererReceiveShadows = value;
                            if (item.RendererReceiveShadows != item.RendererReceiveShadowsOriginal)
                                item.RendererReceiveShadowsOnChange(value);
                            else
                                item.RendererReceiveShadowsOnReset();
                            SetLabelText(RendererReceiveShadowsLabel, item.LabelText, item.RendererReceiveShadows != item.RendererReceiveShadowsOriginal);
                        });

                        RendererReceiveShadowsResetButton.onClick.RemoveAllListeners();
                        RendererReceiveShadowsResetButton.onClick.AddListener(() => RendererReceiveShadowsToggle.isOn = item.RendererReceiveShadowsOriginal);

                        break;
                    case ItemInfo.RowItemType.RendererUpdateWhenOffscreen:
                        ShowRendererUpdateWhenOffscreen();
                        SetLabelText(RendererUpdateWhenOffscreenLabel, item.LabelText, item.RendererUpdateWhenOffscreen != item.RendererUpdateWhenOffscreenOriginal);
                        RendererUpdateWhenOffscreenToggle.onValueChanged.RemoveAllListeners();
                        RendererUpdateWhenOffscreenToggle.isOn = item.RendererUpdateWhenOffscreen;
                        RendererUpdateWhenOffscreenToggle.onValueChanged.AddListener(value =>
                        {
                            item.RendererUpdateWhenOffscreen = value;
                            if (item.RendererUpdateWhenOffscreen != item.RendererUpdateWhenOffscreenOriginal)
                                item.RendererUpdateWhenOffscreenOnChange(value);
                            else
                                item.RendererUpdateWhenOffscreenOnReset();
                            SetLabelText(RendererUpdateWhenOffscreenLabel, item.LabelText, item.RendererUpdateWhenOffscreen != item.RendererUpdateWhenOffscreenOriginal);
                        });

                        RendererUpdateWhenOffscreenResetButton.onClick.RemoveAllListeners();
                        RendererUpdateWhenOffscreenResetButton.onClick.AddListener(() => RendererUpdateWhenOffscreenToggle.isOn = item.RendererUpdateWhenOffscreenOriginal);

                        break;
                    case ItemInfo.RowItemType.RendererRecalculateNormals:
                        ShowRendererRecalculateNormals();
                        SetLabelText(RendererRecalculateNormalsLabel, item.LabelText, item.RendererRecalculateNormals != item.RendererRecalculateNormalsOriginal);
                        RendererRecalculateNormalsToggle.onValueChanged.RemoveAllListeners();
                        RendererRecalculateNormalsToggle.isOn = item.RendererRecalculateNormals;
                        RendererRecalculateNormalsToggle.onValueChanged.AddListener(value =>
                        {
                            item.RendererRecalculateNormals = value;
                            if (item.RendererRecalculateNormals != item.RendererRecalculateNormalsOriginal)
                                item.RendererRecalculateNormalsOnChange(value);
                            else
                                item.RendererRecalculateNormalsOnReset();
                            SetLabelText(RendererRecalculateNormalsLabel, item.LabelText, item.RendererRecalculateNormals != item.RendererRecalculateNormalsOriginal);
                        });

                        RendererRecalculateNormalsResetButton.onClick.RemoveAllListeners();
                        RendererRecalculateNormalsResetButton.onClick.AddListener(() => RendererRecalculateNormalsToggle.isOn = item.RendererRecalculateNormalsOriginal);

                        break;
                    case ItemInfo.RowItemType.Material:
                        ShowMaterial();
                        SetLabelText(MaterialLabel, item.LabelText);
                        MaterialText.text = item.MaterialName;
                        MaterialCopyButton.onClick.RemoveAllListeners();
                        MaterialCopyButton.onClick.AddListener(() => item.MaterialOnCopy.Invoke());
                        MaterialPasteButton.onClick.RemoveAllListeners();
                        MaterialPasteButton.onClick.AddListener(() => item.MaterialOnPaste.Invoke());
                        if (MaterialEditorPluginBase.CopyData.IsEmpty)
                        {
                            MaterialPasteButton.enabled = false;
                            Text text = MaterialPasteButton.GetComponentInChildren<Text>();
                            text.color = Color.gray;
                        }
                        else
                        {
                            MaterialPasteButton.enabled = true;
                            Text text = MaterialPasteButton.GetComponentInChildren<Text>();
                            text.color = Color.black;
                        }

                        if (item.MaterialName.Contains(MaterialAPI.MaterialCopyPostfix))
                        {
                            Text text = MaterialCopyRemove.GetComponentInChildren<Text>();
                            text.text = "Remove Material";
                        }
                        else
                        {
                            Text text = MaterialCopyRemove.GetComponentInChildren<Text>();
                            text.text = "Copy Material";
                        }
                        if (item.MaterialOnCopyRemove != null)
                        {
                            MaterialCopyRemove.onClick.RemoveAllListeners();
                            MaterialCopyRemove.onClick.AddListener(delegate { item.MaterialOnCopyRemove.Invoke(); });
                        }
                        else
                            MaterialCopyRemove.gameObject.SetActive(false);

                        break;
                    case ItemInfo.RowItemType.Shader:
                        ShowShader();
                        SetLabelText(ShaderLabel, item.LabelText, item.ShaderName != item.ShaderNameOriginal);
                        ShaderDropdown.onValueChanged.RemoveAllListeners();
                        ShaderDropdown.value = ShaderDropdown.OptionIndex(item.ShaderName);
                        ShaderDropdown.captionText.text = item.ShaderName;
                        ShaderDropdown.onValueChanged.AddListener(value =>
                        {
                            var selected = ShaderDropdown.OptionText(value);
                            if (value == 0 || selected.IsNullOrEmpty())
                                selected = item.ShaderNameOriginal;
                            item.ShaderName = selected;

                            if (item.ShaderName != item.ShaderNameOriginal)
                                item.ShaderNameOnChange(item.ShaderName);
                            else
                                item.ShaderNameOnReset();
                            SetLabelText(ShaderLabel, item.LabelText, item.ShaderName != item.ShaderNameOriginal);
                        });

                        ShaderResetButton.onClick.RemoveAllListeners();
                        ShaderResetButton.onClick.AddListener(() => ShaderDropdown.value = ShaderDropdown.OptionIndex(item.ShaderNameOriginal));
                        SelectInterpolableShaderButton.onClick.RemoveAllListeners();
                        SelectInterpolableShaderButton.onClick.AddListener(() => item.SelectInterpolableButtonShaderOnClick());

                        AutoScrollToSelectionWithDropdown.Setup(ShaderDropdown);
                        DropdownFilter.AddFilterUI(ShaderDropdown, "ShaderDropDown");

                        break;
                    case ItemInfo.RowItemType.ShaderRenderQueue:
                        ShowShaderRenderQueue();
                        SetLabelText(ShaderRenderQueueLabel, item.LabelText, item.ShaderRenderQueue != item.ShaderRenderQueueOriginal);
                        ShaderRenderQueueInput.onEndEdit.RemoveAllListeners();
                        ShaderRenderQueueInput.text = item.ShaderRenderQueue.ToString();
                        ShaderRenderQueueInput.onEndEdit.AddListener(value =>
                        {
                            if (!int.TryParse(value, out int intValue))
                            {
                                ShaderRenderQueueInput.text = item.ShaderRenderQueue.ToString();
                                return;
                            }

                            item.ShaderRenderQueue = intValue;
                            ShaderRenderQueueInput.text = item.ShaderRenderQueue.ToString();

                            if (item.ShaderRenderQueue != item.ShaderRenderQueueOriginal)
                                item.ShaderRenderQueueOnChange(item.ShaderRenderQueue);
                            else
                                item.ShaderRenderQueueOnReset();
                            SetLabelText(ShaderRenderQueueLabel, item.LabelText, item.ShaderRenderQueue != item.ShaderRenderQueueOriginal);
                        });

                        ShaderRenderQueueResetButton.onClick.RemoveAllListeners();
                        ShaderRenderQueueResetButton.onClick.AddListener(() =>
                        {
                            ShaderRenderQueueInput.text = item.ShaderRenderQueueOriginal.ToString();
                            item.ShaderRenderQueue = item.ShaderRenderQueueOriginal;
                            item.ShaderRenderQueueOnReset();
                            SetLabelText(ShaderRenderQueueLabel, item.LabelText, item.ShaderRenderQueue != item.ShaderRenderQueueOriginal);
                        });

                        break;
                    case ItemInfo.RowItemType.TextureProperty:
                        ShowTexture();
                        SetLabelText(TextureLabel, item.LabelText, item.TextureChanged);

                        ConfigureExportButton();
                        void ConfigureExportButton()
                        {
                            if (item.TextureExists)
                            {
                                ExportTextureButton.enabled = true;
                                Text text = ExportTextureButton.GetComponentInChildren<Text>();
                                text.text = "Export Texture";
                                text.color = Color.black;
                            }
                            else
                            {
                                ExportTextureButton.enabled = false;
                                Text text = ExportTextureButton.GetComponentInChildren<Text>();
                                text.text = "No Texture";
                                text.color = Color.gray;
                            }
                        }

                        ExportTextureButton.onClick.RemoveAllListeners();
                        ExportTextureButton.onClick.AddListener(() => item.TextureOnExport());
                        ImportTextureButton.onClick.RemoveAllListeners();
                        ImportTextureButton.onClick.AddListener(() =>
                        {
                            item.TextureChanged = true;
                            item.TextureExists = true;
                            item.TextureOnImport();
                            ConfigureExportButton();
                            SetLabelText(TextureLabel, item.LabelText, item.TextureChanged);
                        });

                        TextureResetButton.onClick.RemoveAllListeners();
                        TextureResetButton.onClick.AddListener(() =>
                        {
                            item.TextureChanged = false;
                            item.TextureOnReset();
                            SetLabelText(TextureLabel, item.LabelText, item.TextureChanged);
                        });
                        SelectInterpolableTextureButton.onClick.RemoveAllListeners();
                        SelectInterpolableTextureButton.onClick.AddListener(() => item.SelectInterpolableButtonTextureOnClick());
                        break;
                    case ItemInfo.RowItemType.TextureOffsetScale:
                        ShowOffsetScale();
                        SetLabelText(OffsetScaleLabel, item.LabelText, item.Offset != item.OffsetOriginal || item.Scale != item.ScaleOriginal);

                        OffsetXInput.onEndEdit.RemoveAllListeners();
                        OffsetYInput.onEndEdit.RemoveAllListeners();
                        ScaleXInput.onEndEdit.RemoveAllListeners();
                        ScaleYInput.onEndEdit.RemoveAllListeners();

                        OffsetXInput.text = item.Offset.x.ToString();
                        OffsetYInput.text = item.Offset.y.ToString();
                        ScaleXInput.text = item.Scale.x.ToString();
                        ScaleYInput.text = item.Scale.y.ToString();

                        OffsetXInput.onEndEdit.AddListener(value =>
                        {
                            if (!float.TryParse(value, out float input))
                            {
                                OffsetXInput.text = item.Offset.x.ToString();
                                return;
                            }

                            item.Offset = new Vector2(input, item.Offset.y);
                            OffsetXInput.text = item.Offset.x.ToString();

                            if (item.Offset == item.OffsetOriginal)
                                item.OffsetOnReset();
                            else
                                item.OffsetOnChange(item.Offset);

                            SetLabelText(OffsetScaleLabel, item.LabelText, item.Offset != item.OffsetOriginal || item.Scale != item.ScaleOriginal);
                        });

                        OffsetYInput.onEndEdit.AddListener(value =>
                        {
                            if (!float.TryParse(value, out float input))
                            {
                                OffsetYInput.text = item.Offset.y.ToString();
                                return;
                            }

                            item.Offset = new Vector2(item.Offset.x, input);
                            OffsetYInput.text = item.Offset.y.ToString();

                            if (item.Offset == item.OffsetOriginal)
                                item.OffsetOnReset();
                            else
                                item.OffsetOnChange(item.Offset);

                            SetLabelText(OffsetScaleLabel, item.LabelText, item.Offset != item.OffsetOriginal || item.Scale != item.ScaleOriginal);
                        });

                        ScaleXInput.onEndEdit.AddListener(value =>
                        {
                            if (!float.TryParse(value, out float input))
                            {
                                ScaleXInput.text = item.Scale.x.ToString();
                                return;
                            }

                            item.Scale = new Vector2(input, item.Scale.y);
                            ScaleXInput.text = item.Scale.x.ToString();

                            if (item.Scale == item.ScaleOriginal)
                                item.ScaleOnReset();
                            else
                                item.ScaleOnChange(item.Scale);

                            SetLabelText(OffsetScaleLabel, item.LabelText, item.Offset != item.OffsetOriginal || item.Scale != item.ScaleOriginal);
                        });

                        ScaleYInput.onEndEdit.AddListener(value =>
                        {
                            if (!float.TryParse(value, out float input))
                            {
                                ScaleYInput.text = item.Scale.y.ToString();
                                return;
                            }

                            item.Scale = new Vector2(item.Scale.x, input);
                            ScaleYInput.text = item.Scale.y.ToString();

                            if (item.Scale == item.ScaleOriginal)
                                item.ScaleOnReset();
                            else
                                item.ScaleOnChange(item.Scale);

                            SetLabelText(OffsetScaleLabel, item.LabelText, item.Offset != item.OffsetOriginal || item.Scale != item.ScaleOriginal);
                        });

                        OffsetScaleResetButton.onClick.RemoveAllListeners();
                        OffsetScaleResetButton.onClick.AddListener(() =>
                        {
                            item.Offset = item.OffsetOriginal;
                            item.Scale = item.ScaleOriginal;

                            OffsetXInput.text = item.Offset.x.ToString();
                            OffsetYInput.text = item.Offset.y.ToString();
                            ScaleXInput.text = item.Scale.x.ToString();
                            ScaleYInput.text = item.Scale.y.ToString();

                            item.OffsetOnReset();
                            item.ScaleOnReset();
                            SetLabelText(OffsetScaleLabel, item.LabelText, item.Offset != item.OffsetOriginal || item.Scale != item.ScaleOriginal);
                        });

                        break;
                    case ItemInfo.RowItemType.ColorProperty:
                        ShowColor();
                        SetLabelText(ColorLabel, item.LabelText, item.ColorValue != item.ColorValueOriginal);

                        ColorRInput.onEndEdit.RemoveAllListeners();
                        ColorGInput.onEndEdit.RemoveAllListeners();
                        ColorBInput.onEndEdit.RemoveAllListeners();
                        ColorAInput.onEndEdit.RemoveAllListeners();

                        ColorRInput.text = item.ColorValue.r.ToString();
                        ColorGInput.text = item.ColorValue.g.ToString();
                        ColorBInput.text = item.ColorValue.b.ToString();
                        ColorAInput.text = item.ColorValue.a.ToString();

                        ColorEditButton.image.color = item.ColorValue;

                        ColorRInput.onEndEdit.AddListener(value =>
                        {
                            if (!float.TryParse(value, out float input))
                            {
                                ColorRInput.text = item.ColorValue.r.ToString();
                                return;
                            }

                            item.ColorValue = new Color(input, item.ColorValue.g, item.ColorValue.b, item.ColorValue.a);
                            ColorRInput.text = item.ColorValue.r.ToString();

                            if (item.ColorValue == item.ColorValueOriginal)
                                item.ColorValueOnReset();
                            else
                                item.ColorValueOnChange(item.ColorValue);

                            ColorEditButton.image.color = item.ColorValue;
                            item.ColorValueSetToPalette(item.LabelText, item.ColorValue);

                            SetLabelText(ColorLabel, item.LabelText, item.ColorValue != item.ColorValueOriginal);
                        });

                        ColorGInput.onEndEdit.AddListener(value =>
                        {
                            if (!float.TryParse(value, out float input))
                            {
                                ColorGInput.text = item.ColorValue.g.ToString();
                                return;
                            }

                            item.ColorValue = new Color(item.ColorValue.r, input, item.ColorValue.b, item.ColorValue.a);
                            ColorGInput.text = item.ColorValue.g.ToString();

                            if (item.ColorValue == item.ColorValueOriginal)
                                item.ColorValueOnReset();
                            else
                                item.ColorValueOnChange(item.ColorValue);

                            ColorEditButton.image.color = item.ColorValue;
                            item.ColorValueSetToPalette(item.LabelText, item.ColorValue);

                            SetLabelText(ColorLabel, item.LabelText, item.ColorValue != item.ColorValueOriginal);
                        });

                        ColorBInput.onEndEdit.AddListener(value =>
                        {
                            if (!float.TryParse(value, out float input))
                            {
                                ColorBInput.text = item.ColorValue.b.ToString();
                                return;
                            }

                            item.ColorValue = new Color(item.ColorValue.r, item.ColorValue.g, input, item.ColorValue.a);
                            ColorBInput.text = item.ColorValue.b.ToString();

                            if (item.ColorValue == item.ColorValueOriginal)
                                item.ColorValueOnReset();
                            else
                                item.ColorValueOnChange(item.ColorValue);

                            ColorEditButton.image.color = item.ColorValue;
                            item.ColorValueSetToPalette(item.LabelText, item.ColorValue);

                            SetLabelText(ColorLabel, item.LabelText, item.ColorValue != item.ColorValueOriginal);
                        });

                        ColorAInput.onEndEdit.AddListener(value =>
                        {
                            if (!float.TryParse(value, out float input))
                            {
                                ColorAInput.text = item.ColorValue.a.ToString();
                                return;
                            }

                            item.ColorValue = new Color(item.ColorValue.r, item.ColorValue.g, item.ColorValue.b, input);
                            ColorAInput.text = item.ColorValue.a.ToString();

                            if (item.ColorValue == item.ColorValueOriginal)
                                item.ColorValueOnReset();
                            else
                                item.ColorValueOnChange(item.ColorValue);

                            ColorEditButton.image.color = item.ColorValue;
                            item.ColorValueSetToPalette(item.LabelText, item.ColorValue);

                            SetLabelText(ColorLabel, item.LabelText, item.ColorValue != item.ColorValueOriginal);
                        });

                        ColorResetButton.onClick.RemoveAllListeners();
                        ColorResetButton.onClick.AddListener(() =>
                        {
                            item.ColorValue = item.ColorValueOriginal;

                            ColorRInput.text = item.ColorValue.r.ToString();
                            ColorGInput.text = item.ColorValue.g.ToString();
                            ColorBInput.text = item.ColorValue.b.ToString();
                            ColorAInput.text = item.ColorValue.a.ToString();

                            ColorEditButton.image.color = item.ColorValue;
                            item.ColorValueSetToPalette(item.LabelText, item.ColorValue);

                            item.ColorValueOnReset();
                            SetLabelText(ColorLabel, item.LabelText, item.ColorValue != item.ColorValueOriginal);
                        });

                        ColorEditButton.onClick.RemoveAllListeners();
                        ColorEditButton.onClick.AddListener(() =>
                        {
                            item.ColorValueOnEdit(item.LabelText, item.ColorValue, onChanged);

                            void onChanged(Color c)
                            {
                                ColorEditButton.image.color = c;
                                item.ColorValue = c;

                                ColorRInput.text = c.r.ToString();
                                ColorGInput.text = c.g.ToString();
                                ColorBInput.text = c.b.ToString();
                                ColorAInput.text = c.a.ToString();

                                if (item.ColorValue == item.ColorValueOriginal)
                                    item.ColorValueOnReset();
                                else
                                    item.ColorValueOnChange(item.ColorValue);

                                SetLabelText(ColorLabel, item.LabelText, item.ColorValue != item.ColorValueOriginal);
                            }
                        });
                        SelectInterpolableColorButton.onClick.RemoveAllListeners();
                        SelectInterpolableColorButton.onClick.AddListener(() => item.SelectInterpolableButtonColorOnClick());

                        break;
                    case ItemInfo.RowItemType.FloatProperty:
                        ShowFloat();
                        SetLabelText(FloatLabel, item.LabelText, item.FloatValue != item.FloatValueOriginal);
                        FloatSlider.onValueChanged.RemoveAllListeners();
                        FloatInputField.onEndEdit.RemoveAllListeners();

                        FloatSlider.minValue = item.FloatValueSliderMin;
                        FloatSlider.maxValue = item.FloatValueSliderMax;
                        FloatSlider.value = item.FloatValue;
                        FloatInputField.text = item.FloatValue.ToString();

                        FloatSlider.onValueChanged.AddListener(value =>
                        {
                            FloatInputField.text = value.ToString();
                            FloatInputField.onEndEdit.Invoke(value.ToString());
                        });

                        FloatInputField.onEndEdit.AddListener(value =>
                        {
                            if (!float.TryParse(value, out float input))
                            {
                                FloatInputField.text = item.FloatValue.ToString();
                                return;
                            }
                            item.FloatValue = input;

                            FloatSlider.Set(item.FloatValue, sendCallback: false);

                            if (item.FloatValue == item.FloatValueOriginal)
                                item.FloatValueOnReset();
                            else
                                item.FloatValueOnChange(item.FloatValue);

                            SetLabelText(FloatLabel, item.LabelText, item.FloatValue != item.FloatValueOriginal);
                        });

                        FloatResetButton.onClick.RemoveAllListeners();
                        FloatResetButton.onClick.AddListener(() =>
                        {
                            item.FloatValue = item.FloatValueOriginal;

                            FloatSlider.Set(item.FloatValue);
                            FloatInputField.text = item.FloatValue.ToString();

                            item.FloatValueOnReset();
                            SetLabelText(FloatLabel, item.LabelText, item.FloatValue != item.FloatValueOriginal);
                        });
                        SelectInterpolableFloatButton.onClick.RemoveAllListeners();
                        SelectInterpolableFloatButton.onClick.AddListener(() => item.SelectInterpolableButtonFloatOnClick());
                        break;
                    case ItemInfo.RowItemType.KeywordProperty:
                        ShowKeyword();
                        SetLabelText(KeywordLabel, item.LabelText, item.KeywordValue != item.KeywordValueOriginal);
                        KeywordToggle.onValueChanged.RemoveAllListeners();

                        KeywordToggle.isOn = item.KeywordValue;
                        KeywordToggle.onValueChanged.AddListener(value =>
                        {
                            item.KeywordValue = value;

                            if (item.KeywordValue == item.KeywordValueOriginal)
                                item.KeywordValueOnReset();
                            else
                                item.KeywordValueOnChange(item.KeywordValue);

                            SetLabelText(KeywordLabel, item.LabelText, item.KeywordValue != item.KeywordValueOriginal);
                        });

                        KeywordResetButton.onClick.RemoveAllListeners();
                        KeywordResetButton.onClick.AddListener(() =>
                        {
                            item.KeywordValue = item.KeywordValueOriginal;

                            KeywordToggle.Set(item.KeywordValue);

                            item.KeywordValueOnReset();
                            SetLabelText(KeywordLabel, item.LabelText, item.KeywordValue != item.KeywordValueOriginal);
                        });
                        break;
                }
            }
        }

        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible)
                gameObject.SetActive(visible);
        }

        private static void SetLabelText(Text label, string text, bool valueChanged = false)
        {
            if (text.IsNullOrEmpty())
            {
                if (valueChanged)
                    label.text = "*";
                else
                    label.text = "";
            }
            else
            {
                if (valueChanged)
                    label.text = text + ":*";
                else
                    label.text = text + ":";
            }
        }
        private void HideAll()
        {
            ShowRenderer(false);
            ShowRendererEnabled(false);
            ShowRendererShadowCastingMode(false);
            ShowRendererReceiveShadows(false);
            ShowRendererUpdateWhenOffscreen(false);
            ShowRendererRecalculateNormals(false);
            ShowMaterial(false);
            ShowShader(false);
            ShowShaderRenderQueue(false);
            ShowTexture(false);
            ShowOffsetScale(false);
            ShowColor(false);
            ShowFloat(false);
            ShowKeyword(false);
        }

        private void ShowRenderer(bool visible = true)
        {
            RendererPanel.alpha = visible ? 1 : 0;
            RendererPanel.blocksRaycasts = visible;
        }

        private void ShowRendererEnabled(bool visible = true)
        {
            RendererEnabledPanel.alpha = visible ? 1 : 0;
            RendererEnabledPanel.blocksRaycasts = visible;
        }
        private void ShowRendererShadowCastingMode(bool visible = true)
        {
            RendererShadowCastingModePanel.alpha = visible ? 1 : 0;
            RendererShadowCastingModePanel.blocksRaycasts = visible;
        }
        private void ShowRendererReceiveShadows(bool visible = true)
        {
            RendererReceiveShadowsPanel.alpha = visible ? 1 : 0;
            RendererReceiveShadowsPanel.blocksRaycasts = visible;
        }
        private void ShowRendererUpdateWhenOffscreen(bool visible = true)
        {
            RendererUpdateWhenOffscreenPanel.alpha = visible ? 1 : 0;
            RendererUpdateWhenOffscreenPanel.blocksRaycasts = visible;
        }
        private void ShowRendererRecalculateNormals(bool visible = true)
        {
            RendererRecalculateNormalsPanel.alpha = visible ? 1 : 0;
            RendererRecalculateNormalsPanel.blocksRaycasts = visible;
        }
        private void ShowMaterial(bool visible = true)
        {
            MaterialPanel.alpha = visible ? 1 : 0;
            MaterialPanel.blocksRaycasts = visible;
        }
        private void ShowShader(bool visible = true)
        {
            ShaderPanel.alpha = visible ? 1 : 0;
            ShaderPanel.blocksRaycasts = visible;
        }
        private void ShowShaderRenderQueue(bool visible = true)
        {
            ShaderRenderQueuePanel.alpha = visible ? 1 : 0;
            ShaderRenderQueuePanel.blocksRaycasts = visible;
        }

        private void ShowTexture(bool visible = true)
        {
            TexturePanel.alpha = visible ? 1 : 0;
            TexturePanel.blocksRaycasts = visible;
        }

        private void ShowOffsetScale(bool visible = true)
        {
            OffsetScalePanel.alpha = visible ? 1 : 0;
            OffsetScalePanel.blocksRaycasts = visible;
        }

        private void ShowColor(bool visible = true)
        {
            ColorPanel.alpha = visible ? 1 : 0;
            ColorPanel.blocksRaycasts = visible;
        }

        private void ShowFloat(bool visible = true)
        {
            FloatPanel.alpha = visible ? 1 : 0;
            FloatPanel.blocksRaycasts = visible;
        }
        
        private void ShowKeyword(bool visible = true)
        {
            KeywordPanel.alpha = visible ? 1 : 0;
            KeywordPanel.blocksRaycasts = visible;
        }

        public T GetUIComponent<T>(string gameObjectName) where T : Component
        {
            GameObject uiObject = transform.FindLoop(gameObjectName).gameObject;
            if (uiObject == null)
                throw new ArgumentException($"Couldn't find {gameObjectName}");
            var component = uiObject.GetComponent<T>();
            if (component == null)
                throw new ArgumentException($"Couldn't find {gameObjectName}");
            return component;
        }
    }
}
