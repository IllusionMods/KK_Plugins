using System.Linq;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace KK_Plugins.MaterialEditor
{
    internal static class ItemTemplate
    {
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
        private static readonly RectOffset padding = new RectOffset(3, 3, 0, 1);
        private static readonly Color rowColor = new Color(1f, 1f, 1f, 1f);

        internal static GameObject CreateTemplate(Transform parent)
        {
            var contentList = UIUtility.CreatePanel("ListEntry", parent);
            contentList.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
            contentList.gameObject.AddComponent<Mask>();
            contentList.gameObject.AddComponent<HorizontalLayoutGroup>().padding = padding;
            contentList.color = rowColor;

            var label = UIUtility.CreateText("Label", contentList.transform, "");
            label.alignment = TextAnchor.MiddleLeft;
            label.color = Color.black;
            var labelLE = label.gameObject.AddComponent<LayoutElement>();
            labelLE.preferredWidth = labelWidth;
            labelLE.flexibleWidth = labelWidth;

            //Renderer
            {
                Text labelRenderer = UIUtility.CreateText("RendererText", contentList.transform);
                labelRenderer.alignment = TextAnchor.MiddleRight;
                labelRenderer.color = Color.black;
                var labelRendererLE = labelRenderer.gameObject.AddComponent<LayoutElement>();
                labelRendererLE.preferredWidth = 200;
                labelRendererLE.flexibleWidth = 0;

                Button exportUVButton = UIUtility.CreateButton("ExportUVButton", contentList.transform, "Export UV Map");
                var exportUVButtonLE = exportUVButton.gameObject.AddComponent<LayoutElement>();
                exportUVButtonLE.preferredWidth = 110;
                exportUVButtonLE.flexibleWidth = 0;

                Button exportMeshButton = UIUtility.CreateButton("ExportObjButton", contentList.transform, "Export .obj");
                var exportMeshButtonLE = exportMeshButton.gameObject.AddComponent<LayoutElement>();
                exportMeshButtonLE.preferredWidth = 85;
                exportMeshButtonLE.flexibleWidth = 0;
            }

            //Renderer Enabled
            {
                Dropdown dropdownEnabled = UIUtility.CreateDropdown("RendererEnabledDropdown", contentList.transform);
                dropdownEnabled.transform.SetRect(0f, 0f, 0f, 1f, 0f, 0f, 100f);
                dropdownEnabled.captionText.transform.SetRect(0f, 0f, 1f, 1f, 0f, 2f, -15f, -2f);
                dropdownEnabled.captionText.alignment = TextAnchor.MiddleLeft;
                dropdownEnabled.options.Clear();
                dropdownEnabled.options.Add(new Dropdown.OptionData("Off"));
                dropdownEnabled.options.Add(new Dropdown.OptionData("On"));
                dropdownEnabled.value = 0;
                dropdownEnabled.captionText.text = "Off";
                var dropdownEnabledLE = dropdownEnabled.gameObject.AddComponent<LayoutElement>();
                dropdownEnabledLE.preferredWidth = dropdownWidth;
                dropdownEnabledLE.flexibleWidth = 0;
            }

            //Renderer ShadowCastingMode
            {
                Dropdown dropdownShadowCastingMode = UIUtility.CreateDropdown("RendererShadowCastingModeDropdown", contentList.transform);
                dropdownShadowCastingMode.transform.SetRect(0f, 0f, 0f, 1f, 0f, 0f, 100f);
                dropdownShadowCastingMode.captionText.transform.SetRect(0f, 0f, 1f, 1f, 0f, 2f, -15f, -2f);
                dropdownShadowCastingMode.captionText.alignment = TextAnchor.MiddleLeft;
                dropdownShadowCastingMode.options.Clear();
                dropdownShadowCastingMode.options.Add(new Dropdown.OptionData("Off"));
                dropdownShadowCastingMode.options.Add(new Dropdown.OptionData("On"));
                dropdownShadowCastingMode.options.Add(new Dropdown.OptionData("Two Sided"));
                dropdownShadowCastingMode.options.Add(new Dropdown.OptionData("Shadows Only"));
                dropdownShadowCastingMode.value = 0;
                dropdownShadowCastingMode.captionText.text = "Off";
                var dropdownShadowCastingModeLE = dropdownShadowCastingMode.gameObject.AddComponent<LayoutElement>();
                dropdownShadowCastingModeLE.preferredWidth = dropdownWidth;
                dropdownShadowCastingModeLE.flexibleWidth = 0;
            }

            //Renderer ReceiveShadows
            {
                Dropdown dropdownReceiveShadows = UIUtility.CreateDropdown("RendererReceiveShadowsDropdown", contentList.transform);
                dropdownReceiveShadows.transform.SetRect(0f, 0f, 0f, 1f, 0f, 0f, 100f);
                dropdownReceiveShadows.captionText.transform.SetRect(0f, 0f, 1f, 1f, 0f, 2f, -15f, -2f);
                dropdownReceiveShadows.captionText.alignment = TextAnchor.MiddleLeft;
                dropdownReceiveShadows.options.Clear();
                dropdownReceiveShadows.options.Add(new Dropdown.OptionData("Off"));
                dropdownReceiveShadows.options.Add(new Dropdown.OptionData("On"));
                dropdownReceiveShadows.value = 0;
                dropdownReceiveShadows.captionText.text = "Off";
                var dropdownReceiveShadowsLE = dropdownReceiveShadows.gameObject.AddComponent<LayoutElement>();
                dropdownReceiveShadowsLE.preferredWidth = dropdownWidth;
                dropdownReceiveShadowsLE.flexibleWidth = 0;
            }

            //Material
            {
                Text materialText = UIUtility.CreateText("MaterialText", contentList.transform);
                materialText.alignment = TextAnchor.MiddleRight;
                materialText.color = Color.black;
                var materialTextLE = materialText.gameObject.AddComponent<LayoutElement>();
                materialTextLE.preferredWidth = 200;
                materialTextLE.flexibleWidth = 0;
            }

            //Material Shader
            {
                Dropdown dropdownShader = UIUtility.CreateDropdown("ShaderDropdown", contentList.transform);
                dropdownShader.transform.SetRect(0f, 0f, 0f, 1f, 0f, 0f, 100f);
                dropdownShader.captionText.transform.SetRect(0f, 0f, 1f, 1f, 0f, 2f, -15f, -2f);
                dropdownShader.captionText.alignment = TextAnchor.MiddleLeft;
                dropdownShader.options.Clear();
                dropdownShader.options.Add(new Dropdown.OptionData("Reset"));
                foreach (var shader in MaterialEditorPlugin.XMLShaderProperties.Where(x => x.Key != "default"))
                    dropdownShader.options.Add(new Dropdown.OptionData(shader.Key));
                var dropdownShaderLE = dropdownShader.gameObject.AddComponent<LayoutElement>();
                dropdownShaderLE.preferredWidth = dropdownWidth * 3;
                dropdownShaderLE.flexibleWidth = 0;
            }

            //Material RenderQueue
            {
                InputField textBoxShaderRenderQueue = UIUtility.CreateInputField("ShaderRenderQueueInput", contentList.transform);
                textBoxShaderRenderQueue.text = "0";
                var textBoxShaderRenderQueueLE = textBoxShaderRenderQueue.gameObject.AddComponent<LayoutElement>();
                textBoxShaderRenderQueueLE.preferredWidth = textBoxWidth;
                textBoxShaderRenderQueueLE.flexibleWidth = 0;
            }

            //Texture properties
            {
                Button exportButton = UIUtility.CreateButton($"ExportTextureButton", contentList.transform, $"Export Texture");
                var exportButtonLE = exportButton.gameObject.AddComponent<LayoutElement>();
                exportButtonLE.preferredWidth = buttonWidth;
                exportButtonLE.flexibleWidth = 0;

                Button importButton = UIUtility.CreateButton($"ImportTextureButton", contentList.transform, $"Import Texture");
                var importButtonLE = importButton.gameObject.AddComponent<LayoutElement>();
                importButtonLE.preferredWidth = buttonWidth;
                importButtonLE.flexibleWidth = 0;
            }

            //Offset and Scale
            {
                Text labelOffsetX = UIUtility.CreateText("OffsetXText", contentList.transform, "Offset X");
                labelOffsetX.alignment = TextAnchor.MiddleLeft;
                labelOffsetX.color = Color.black;
                var labelOffsetXLE = labelOffsetX.gameObject.AddComponent<LayoutElement>();
                labelOffsetXLE.preferredWidth = labelXWidth;
                labelOffsetXLE.flexibleWidth = 0;

                InputField textBoxOffsetX = UIUtility.CreateInputField("OffsetXInput", contentList.transform);
                textBoxOffsetX.text = "0";
                var textBoxOffsetXLE = textBoxOffsetX.gameObject.AddComponent<LayoutElement>();
                textBoxOffsetXLE.preferredWidth = textBoxXYWidth;
                textBoxOffsetXLE.flexibleWidth = 0;

                Text labelOffsetY = UIUtility.CreateText("OffsetYText", contentList.transform, "Y");
                labelOffsetY.alignment = TextAnchor.MiddleLeft;
                labelOffsetY.color = Color.black;
                var labelOffsetYLE = labelOffsetY.gameObject.AddComponent<LayoutElement>();
                labelOffsetYLE.preferredWidth = labelYWidth;
                labelOffsetYLE.flexibleWidth = 0;

                InputField textBoxOffsetY = UIUtility.CreateInputField("OffsetYInput", contentList.transform);
                textBoxOffsetY.text = "0";
                var textBoxOffsetYLE = textBoxOffsetY.gameObject.AddComponent<LayoutElement>();
                textBoxOffsetYLE.preferredWidth = textBoxXYWidth;
                textBoxOffsetYLE.flexibleWidth = 0;

                //Scale
                Text labelScaleX = UIUtility.CreateText("ScaleXText", contentList.transform, "Scale X");
                labelScaleX.alignment = TextAnchor.MiddleLeft;
                labelScaleX.color = Color.black;
                var labelScaleXLE = labelScaleX.gameObject.AddComponent<LayoutElement>();
                labelScaleXLE.preferredWidth = labelXWidth;
                labelScaleXLE.flexibleWidth = 0;

                InputField textBoxScaleX = UIUtility.CreateInputField("ScaleXInput", contentList.transform);
                textBoxScaleX.text = "0";
                var textBoxScaleXLE = textBoxScaleX.gameObject.AddComponent<LayoutElement>();
                textBoxScaleXLE.preferredWidth = textBoxXYWidth;
                textBoxScaleXLE.flexibleWidth = 0;

                Text labelScaleY = UIUtility.CreateText("ScaleYText", contentList.transform, "Y");
                labelScaleY.alignment = TextAnchor.MiddleLeft;
                labelScaleY.color = Color.black;
                var labelScaleYLE = labelScaleY.gameObject.AddComponent<LayoutElement>();
                labelScaleYLE.preferredWidth = labelYWidth;
                labelScaleYLE.flexibleWidth = 0;

                InputField textBoxScaleY = UIUtility.CreateInputField("ScaleYInput", contentList.transform);
                textBoxScaleY.text = "0";
                var textBoxScaleYLE = textBoxScaleY.gameObject.AddComponent<LayoutElement>();
                textBoxScaleYLE.preferredWidth = textBoxXYWidth;
                textBoxScaleYLE.flexibleWidth = 0;
            }

            //Color properties
            {
                Text labelR = UIUtility.CreateText("ColorRText", contentList.transform, "R");
                labelR.alignment = TextAnchor.MiddleLeft;
                labelR.color = Color.black;
                var labelRLE = labelR.gameObject.AddComponent<LayoutElement>();
                labelRLE.preferredWidth = colorLabelWidth;
                labelRLE.flexibleWidth = 0;

                InputField textBoxR = UIUtility.CreateInputField("ColorRInput", contentList.transform);
                textBoxR.text = "0";
                var textBoxRLE = textBoxR.gameObject.AddComponent<LayoutElement>();
                textBoxRLE.preferredWidth = textBoxWidth;
                textBoxRLE.flexibleWidth = 0;

                Text labelG = UIUtility.CreateText("ColorGText", contentList.transform, "G");
                labelG.alignment = TextAnchor.MiddleLeft;
                labelG.color = Color.black;
                var labelGLE = labelG.gameObject.AddComponent<LayoutElement>();
                labelGLE.preferredWidth = colorLabelWidth;
                labelGLE.flexibleWidth = 0;

                InputField textBoxG = UIUtility.CreateInputField("ColorGInput", contentList.transform);
                textBoxG.text = "0";
                var textBoxGLE = textBoxG.gameObject.AddComponent<LayoutElement>();
                textBoxGLE.preferredWidth = textBoxWidth;
                textBoxGLE.flexibleWidth = 0;

                Text labelB = UIUtility.CreateText("ColorBText", contentList.transform, "B");
                labelB.alignment = TextAnchor.MiddleLeft;
                labelB.color = Color.black;
                var labelBLE = labelB.gameObject.AddComponent<LayoutElement>();
                labelBLE.preferredWidth = colorLabelWidth;
                labelBLE.flexibleWidth = 0;

                InputField textBoxB = UIUtility.CreateInputField("ColorBInput", contentList.transform);
                textBoxB.text = "0";
                var textBoxBLE = textBoxB.gameObject.AddComponent<LayoutElement>();
                textBoxBLE.preferredWidth = textBoxWidth;
                textBoxBLE.flexibleWidth = 0;

                Text labelA = UIUtility.CreateText("ColorAText", contentList.transform, "A");
                labelA.alignment = TextAnchor.MiddleLeft;
                labelA.color = Color.black;
                var labelALE = labelA.gameObject.AddComponent<LayoutElement>();
                labelALE.preferredWidth = colorLabelWidth;
                labelALE.flexibleWidth = 0;

                InputField textBoxA = UIUtility.CreateInputField("ColorAInput", contentList.transform);
                textBoxA.text = "0";
                var textBoxALE = textBoxA.gameObject.AddComponent<LayoutElement>();
                textBoxALE.preferredWidth = textBoxWidth;
                textBoxALE.flexibleWidth = 0;
            }

            //Float properties
            {
                Slider sliderFloat = UIUtility.CreateSlider("FloatSlider", contentList.transform);
                var sliderFloatLE = sliderFloat.gameObject.AddComponent<LayoutElement>();
                sliderFloatLE.preferredWidth = sliderWidth;
                sliderFloatLE.flexibleWidth = 0;

                InputField textBoxFloat = UIUtility.CreateInputField("FloatInputField", contentList.transform);
                textBoxFloat.text = "0";
                var textBoxFloatLE = textBoxFloat.gameObject.AddComponent<LayoutElement>();
                textBoxFloatLE.preferredWidth = textBoxWidth;
                textBoxFloatLE.flexibleWidth = 0;
            }

            var resetFloat = UIUtility.CreateButton($"ResetButton", contentList.transform, "Reset");
            var resetEnabledLE = resetFloat.gameObject.AddComponent<LayoutElement>();
            resetEnabledLE.preferredWidth = resetButtonWidth;
            resetEnabledLE.flexibleWidth = 0;

            return contentList.gameObject;
        }
    }
}