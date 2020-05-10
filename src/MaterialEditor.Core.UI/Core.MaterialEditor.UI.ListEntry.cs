using UnityEngine;
using UnityEngine.UI;
using static UILib.Extensions;

namespace KK_Plugins.MaterialEditor
{
    internal class ListEntry : MonoBehaviour
    {
        public Text LabelText;

        public Text RendererText;
        public Button ExportUVButton;
        public Button ExportObjButton;

        public Dropdown RendererEnabledDropdown;
        public Dropdown RendererShadowCastingModeDropdown;
        public Dropdown RendererReceiveShadowsDropdown;

        public Text MaterialText;
        public Dropdown ShaderDropdown;
        public InputField ShaderRenderQueueInput;

        public Button ExportTextureButton;
        public Button ImportTextureButton;

        public Text OffsetXText;
        public InputField OffsetXInput;
        public Text OffsetYText;
        public InputField OffsetYInput;
        public Text ScaleXText;
        public InputField ScaleXInput;
        public Text ScaleYText;
        public InputField ScaleYInput;

        public Text ColorRText;
        public Text ColorGText;
        public Text ColorBText;
        public Text ColorAText;
        public InputField ColorRInput;
        public InputField ColorGInput;
        public InputField ColorBInput;
        public InputField ColorAInput;

        public Slider FloatSlider;
        public InputField FloatInputField;

        public Button ResetButton;

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
                void SetLabelText(bool valueChanged = false)
                {
                    if (item.LabelText.IsNullOrEmpty())
                    {
                        if (valueChanged)
                            LabelText.text = "*";
                        else
                            LabelText.text = "";
                    }
                    else
                    {
                        if (valueChanged)
                            LabelText.text = item.LabelText + ":*";
                        else
                            LabelText.text = item.LabelText + ":";
                    }
                }
                ResetButton.onClick.RemoveAllListeners();

                switch (item.ItemType)
                {
                    case ItemInfo.RowItemType.Renderer:
                        ShowRenderer();
                        RendererText.text = item.RendererName;
                        break;
                    case ItemInfo.RowItemType.RendererEnabled:
                        ShowRendererEnabled();
                        SetLabelText(item.RendererEnabled != item.RendererEnabledOriginal);
                        RendererEnabledDropdown.onValueChanged.RemoveAllListeners();
                        RendererEnabledDropdown.value = item.RendererEnabled;
                        RendererEnabledDropdown.onValueChanged.AddListener(delegate (int value)
                        {
                            item.RendererEnabled = value;
                            if (item.RendererEnabled != item.RendererEnabledOriginal)
                                item.RendererEnabledOnChange(value);
                            else
                                item.RendererEnabledOnReset();
                            SetLabelText(item.RendererEnabled != item.RendererEnabledOriginal);
                        });

                        ResetButton.onClick.AddListener(delegate { RendererEnabledDropdown.value = item.RendererEnabledOriginal; });

                        break;
                    case ItemInfo.RowItemType.RendererShadowCastingMode:
                        ShowRendererShadowCastingMode();
                        SetLabelText(item.RendererShadowCastingMode != item.RendererShadowCastingModeOriginal);
                        RendererShadowCastingModeDropdown.onValueChanged.RemoveAllListeners();
                        RendererShadowCastingModeDropdown.value = item.RendererShadowCastingMode;
                        RendererShadowCastingModeDropdown.onValueChanged.AddListener(delegate (int value)
                        {
                            item.RendererShadowCastingMode = value;
                            if (item.RendererShadowCastingMode != item.RendererShadowCastingModeOriginal)
                                item.RendererShadowCastingModeOnChange(value);
                            else
                                item.RendererShadowCastingModeOnReset();
                            SetLabelText(item.RendererShadowCastingMode != item.RendererShadowCastingModeOriginal);
                        });

                        ResetButton.onClick.AddListener(delegate { RendererShadowCastingModeDropdown.value = item.RendererShadowCastingModeOriginal; });

                        break;
                    case ItemInfo.RowItemType.RendererReceiveShadows:
                        ShowRendererReceiveShadows();
                        SetLabelText(item.RendererReceiveShadows != item.RendererReceiveShadowsOriginal);
                        RendererReceiveShadowsDropdown.onValueChanged.RemoveAllListeners();
                        RendererReceiveShadowsDropdown.value = item.RendererReceiveShadows;
                        RendererReceiveShadowsDropdown.onValueChanged.AddListener(delegate (int value)
                        {
                            item.RendererReceiveShadows = value;
                            if (item.RendererReceiveShadows != item.RendererReceiveShadowsOriginal)
                                item.RendererReceiveShadowsOnChange(value);
                            else
                                item.RendererReceiveShadowsOnReset();
                            SetLabelText(item.RendererReceiveShadows != item.RendererReceiveShadowsOriginal);
                        });

                        ResetButton.onClick.AddListener(delegate { RendererReceiveShadowsDropdown.value = item.RendererReceiveShadowsOriginal; });

                        break;
                    case ItemInfo.RowItemType.Material:
                        ShowMaterial();
                        SetLabelText();
                        MaterialText.text = item.MaterialName;
                        break;
                    case ItemInfo.RowItemType.Shader:
                        ShowShader();
                        SetLabelText(item.ShaderName != item.ShaderNameOriginal);
                        ShaderDropdown.onValueChanged.RemoveAllListeners();
                        ShaderDropdown.value = ShaderDropdown.OptionIndex(item.ShaderName);
                        ShaderDropdown.captionText.text = item.ShaderName;
                        ShaderDropdown.onValueChanged.AddListener(delegate (int value)
                        {
                            var selected = ShaderDropdown.OptionText(value);
                            if (value == 0 || selected.IsNullOrEmpty())
                                selected = item.ShaderNameOriginal;
                            item.ShaderName = selected;

                            if (item.ShaderName != item.ShaderNameOriginal)
                                item.ShaderNameOnChange(item.ShaderName);
                            else
                                item.ShaderNameOnReset();
                            SetLabelText(item.ShaderName != item.ShaderNameOriginal);
                        });

                        ResetButton.onClick.AddListener(delegate { ShaderDropdown.value = ShaderDropdown.OptionIndex(item.ShaderNameOriginal); });

                        break;
                    case ItemInfo.RowItemType.ShaderRenderQueue:
                        ShowShaderRenderQueue();
                        SetLabelText(item.ShaderRenderQueue != item.ShaderRenderQueueOriginal);
                        ShaderRenderQueueInput.text = item.ShaderRenderQueue.ToString();
                        ShaderRenderQueueInput.onEndEdit.AddListener(delegate (string value)
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
                            SetLabelText(item.ShaderRenderQueue != item.ShaderRenderQueueOriginal);
                        });

                        ResetButton.onClick.AddListener(delegate
                        {
                            ShaderRenderQueueInput.text = item.ShaderRenderQueueOriginal.ToString();
                            item.ShaderRenderQueue = item.ShaderRenderQueueOriginal;
                            item.ShaderRenderQueueOnReset();
                            SetLabelText(item.ShaderRenderQueue != item.ShaderRenderQueueOriginal);
                        });

                        break;
                    case ItemInfo.RowItemType.TextureProperty:
                        ShowTexture();
                        SetLabelText(item.TextureChanged);

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
                        ExportTextureButton.onClick.AddListener(delegate { item.TextureOnExport(); });
                        ImportTextureButton.onClick.RemoveAllListeners();
                        ImportTextureButton.onClick.AddListener(delegate
                        {
                            item.TextureChanged = true;
                            item.TextureExists = true;
                            item.TextureOnImport();
                            ConfigureExportButton();
                            SetLabelText(item.TextureChanged);
                        });
                        break;
                    case ItemInfo.RowItemType.TextureOffsetScale:
                        ShowTextureOffsetScale();
                        SetLabelText(item.Offset != item.OffsetOriginal || item.Scale != item.ScaleOriginal);

                        OffsetXInput.onEndEdit.RemoveAllListeners();
                        OffsetYInput.onEndEdit.RemoveAllListeners();
                        ScaleXInput.onEndEdit.RemoveAllListeners();
                        ScaleYInput.onEndEdit.RemoveAllListeners();

                        OffsetXInput.text = item.Offset.x.ToString();
                        OffsetYInput.text = item.Offset.y.ToString();
                        ScaleXInput.text = item.Scale.x.ToString();
                        ScaleYInput.text = item.Scale.y.ToString();

                        OffsetXInput.onEndEdit.AddListener(delegate (string value)
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

                            SetLabelText(item.Offset != item.OffsetOriginal || item.Scale != item.ScaleOriginal);
                        });

                        OffsetYInput.onEndEdit.AddListener(delegate (string value)
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

                            SetLabelText(item.Offset != item.OffsetOriginal || item.Scale != item.ScaleOriginal);
                        });

                        ScaleXInput.onEndEdit.AddListener(delegate (string value)
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

                            SetLabelText(item.Offset != item.OffsetOriginal || item.Scale != item.ScaleOriginal);
                        });

                        ScaleYInput.onEndEdit.AddListener(delegate (string value)
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

                            SetLabelText(item.Offset != item.OffsetOriginal || item.Scale != item.ScaleOriginal);
                        });

                        ResetButton.onClick.AddListener(delegate
                        {
                            item.Offset = item.OffsetOriginal;
                            item.Scale = item.ScaleOriginal;

                            OffsetXInput.text = item.Offset.x.ToString();
                            OffsetYInput.text = item.Offset.y.ToString();
                            ScaleXInput.text = item.Scale.x.ToString();
                            ScaleYInput.text = item.Scale.y.ToString();

                            item.OffsetOnReset();
                            item.ScaleOnReset();
                            SetLabelText(item.Offset != item.OffsetOriginal || item.Scale != item.ScaleOriginal);
                        });


                        break;
                    case ItemInfo.RowItemType.ColorProperty:
                        ShowColor();
                        SetLabelText(item.ColorValue != item.ColorValueOriginal);

                        ColorRInput.onEndEdit.RemoveAllListeners();
                        ColorGInput.onEndEdit.RemoveAllListeners();
                        ColorBInput.onEndEdit.RemoveAllListeners();
                        ColorAInput.onEndEdit.RemoveAllListeners();

                        ColorRInput.text = item.ColorValue.r.ToString();
                        ColorGInput.text = item.ColorValue.g.ToString();
                        ColorBInput.text = item.ColorValue.b.ToString();
                        ColorAInput.text = item.ColorValue.a.ToString();

                        ColorRInput.onEndEdit.AddListener(delegate (string value)
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

                            SetLabelText(item.ColorValue != item.ColorValueOriginal);
                        });

                        ColorGInput.onEndEdit.AddListener(delegate (string value)
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

                            SetLabelText(item.ColorValue != item.ColorValueOriginal);
                        });

                        ColorBInput.onEndEdit.AddListener(delegate (string value)
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

                            SetLabelText(item.ColorValue != item.ColorValueOriginal);
                        });

                        ColorAInput.onEndEdit.AddListener(delegate (string value)
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

                            SetLabelText(item.ColorValue != item.ColorValueOriginal);
                        });

                        ResetButton.onClick.AddListener(delegate
                        {
                            item.ColorValue = item.ColorValueOriginal;

                            ColorRInput.text = item.ColorValue.r.ToString();
                            ColorGInput.text = item.ColorValue.g.ToString();
                            ColorBInput.text = item.ColorValue.b.ToString();
                            ColorAInput.text = item.ColorValue.a.ToString();

                            item.ColorValueOnReset();
                            SetLabelText(item.ColorValue != item.ColorValueOriginal);
                        });
                        break;
                    case ItemInfo.RowItemType.FloatProperty:
                        ShowFloat();
                        SetLabelText(item.FloatValue != item.FloatValueOriginal);
                        FloatSlider.onValueChanged.RemoveAllListeners();
                        FloatInputField.onEndEdit.RemoveAllListeners();

                        FloatSlider.value = item.FloatValue;
                        FloatInputField.text = item.FloatValue.ToString();

                        FloatSlider.onValueChanged.AddListener(delegate (float value)
                        {
                            FloatInputField.text = value.ToString();
                            FloatInputField.onEndEdit.Invoke(value.ToString());
                        });

                        FloatInputField.onEndEdit.AddListener(delegate (string value)
                        {
                            if (!float.TryParse(value, out float input))
                            {
                                FloatInputField.text = item.FloatValue.ToString();
                                return;
                            }
                            item.FloatValue = input;

                                FloatSlider.Set(item.FloatValue);

                            if (item.FloatValue == item.FloatValueOriginal)
                                item.FloatValueOnReset();
                            else
                                item.FloatValueOnChange(item.FloatValue);

                            SetLabelText(item.FloatValue != item.FloatValueOriginal);
                        });

                        ResetButton.onClick.AddListener(delegate
                        {
                            item.FloatValue = item.FloatValueOriginal;

                            FloatSlider.Set(item.FloatValue);
                            FloatInputField.text = item.FloatValue.ToString();

                            item.FloatValueOnReset();
                            SetLabelText(item.FloatValue != item.FloatValueOriginal);
                        });
                        break;
                }
            }
            else
            {
                LabelText.text = string.Empty;
            }
        }

        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible)
                gameObject.SetActive(visible);
        }

        private void HideAll()
        {
            RendererText.gameObject.SetActive(false);
            ExportUVButton.gameObject.SetActive(false);
            ExportObjButton.gameObject.SetActive(false);

            RendererEnabledDropdown.gameObject.SetActive(false);
            RendererShadowCastingModeDropdown.gameObject.SetActive(false);
            RendererReceiveShadowsDropdown.gameObject.SetActive(false);

            MaterialText.gameObject.SetActive(false);
            ShaderDropdown.gameObject.SetActive(false);
            ShaderRenderQueueInput.gameObject.SetActive(false);

            ExportTextureButton.gameObject.SetActive(false);
            ImportTextureButton.gameObject.SetActive(false);

            OffsetXText.gameObject.SetActive(false);
            OffsetXInput.gameObject.SetActive(false);
            OffsetYText.gameObject.SetActive(false);
            OffsetYInput.gameObject.SetActive(false);
            ScaleXText.gameObject.SetActive(false);
            ScaleXInput.gameObject.SetActive(false);
            ScaleYText.gameObject.SetActive(false);
            ScaleYInput.gameObject.SetActive(false);

            ColorRText.gameObject.SetActive(false);
            ColorGText.gameObject.SetActive(false);
            ColorBText.gameObject.SetActive(false);
            ColorAText.gameObject.SetActive(false);
            ColorRInput.gameObject.SetActive(false);
            ColorGInput.gameObject.SetActive(false);
            ColorBInput.gameObject.SetActive(false);
            ColorAInput.gameObject.SetActive(false);

            FloatSlider.gameObject.SetActive(false);
            FloatInputField.gameObject.SetActive(false);

            ResetButton.gameObject.SetActive(false);
        }

        private void ShowRenderer()
        {
            RendererText.gameObject.SetActive(true);
            ExportUVButton.gameObject.SetActive(true);
            ExportObjButton.gameObject.SetActive(true);
        }

        private void ShowRendererEnabled()
        {
            RendererEnabledDropdown.gameObject.SetActive(true);
            ResetButton.gameObject.SetActive(true);
        }
        private void ShowRendererShadowCastingMode()
        {
            RendererShadowCastingModeDropdown.gameObject.SetActive(true);
            ResetButton.gameObject.SetActive(true);
        }
        private void ShowRendererReceiveShadows()
        {
            RendererReceiveShadowsDropdown.gameObject.SetActive(true);
            ResetButton.gameObject.SetActive(true);
        }
        private void ShowMaterial()
        {
            MaterialText.gameObject.SetActive(true);
        }
        private void ShowShader()
        {
            ShaderDropdown.gameObject.SetActive(true);
            ResetButton.gameObject.SetActive(true);
        }
        private void ShowShaderRenderQueue()
        {
            ShaderRenderQueueInput.gameObject.SetActive(true);
            ResetButton.gameObject.SetActive(true);
        }

        private void ShowTexture()
        {
            ExportTextureButton.gameObject.SetActive(true);
            ImportTextureButton.gameObject.SetActive(true);
            ResetButton.gameObject.SetActive(true);
        }

        private void ShowTextureOffsetScale()
        {
            OffsetXText.gameObject.SetActive(true);
            OffsetXInput.gameObject.SetActive(true);
            OffsetYText.gameObject.SetActive(true);
            OffsetYInput.gameObject.SetActive(true);
            ScaleXText.gameObject.SetActive(true);
            ScaleXInput.gameObject.SetActive(true);
            ScaleYText.gameObject.SetActive(true);
            ScaleYInput.gameObject.SetActive(true);
            ResetButton.gameObject.SetActive(true);
        }

        private void ShowColor()
        {
            ColorRText.gameObject.SetActive(true);
            ColorGText.gameObject.SetActive(true);
            ColorBText.gameObject.SetActive(true);
            ColorAText.gameObject.SetActive(true);
            ColorRInput.gameObject.SetActive(true);
            ColorGInput.gameObject.SetActive(true);
            ColorBInput.gameObject.SetActive(true);
            ColorAInput.gameObject.SetActive(true);
            ResetButton.gameObject.SetActive(true);
        }

        private void ShowFloat()
        {
            FloatSlider.gameObject.SetActive(true);
            FloatInputField.gameObject.SetActive(true);
            ResetButton.gameObject.SetActive(true);
        }
    }
}
