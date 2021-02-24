using System;
using UILib;
using UnityEngine;
using UnityEngine.UI;
using static KK_Plugins.DynamicBoneEditor.UI;

namespace KK_Plugins.DynamicBoneEditor
{
    public class SliderSet
    {
        public string Label;
        public float ValueOriginal;
        public Action<float> OnChange;
        private Slider ValueSlider;
        private Text LabelText;

        private float _Value;
        public float Value
        {
            get => _Value;
            set
            {
                if (_Value != value)
                {
                    _Value = value;
                    ValueSlider.value = value;
                    if (OnChange != null)
                        OnChange(value);
                    if (Value == ValueOriginal)
                        LabelText.text = Label;
                    else
                        LabelText.text = $"{Label}*";
                }
            }
        }

        public SliderSet(string label)
        {
            Label = label;
        }

        public void CreateUI(Transform parent)
        {
            var contentList = UIUtility.CreatePanel("ListEntry", parent);
            contentList.gameObject.AddComponent<LayoutElement>().preferredHeight = PanelHeight;
            contentList.gameObject.AddComponent<Mask>();
            contentList.color = RowColor;

            var itemPanel = UIUtility.CreatePanel("SliderSetPanel", contentList.transform);
            itemPanel.gameObject.AddComponent<CanvasGroup>();
            itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>().padding = Padding;
            itemPanel.color = ItemColor;

            LabelText = UIUtility.CreateText("SliderSetLabel", itemPanel.transform, Label);
            LabelText.alignment = TextAnchor.MiddleLeft;
            LabelText.color = Color.black;
            var labelLE = LabelText.gameObject.AddComponent<LayoutElement>();
            labelLE.preferredWidth = LabelWidth;
            labelLE.flexibleWidth = LabelWidth;

            ValueSlider = UIUtility.CreateSlider("SliderSetSlider", itemPanel.transform);
            var sliderFloatLE = ValueSlider.gameObject.AddComponent<LayoutElement>();
            sliderFloatLE.preferredWidth = SliderWidth;
            sliderFloatLE.flexibleWidth = 0;

            InputField textBoxFloat = UIUtility.CreateInputField("SliderSetInputField", itemPanel.transform);
            textBoxFloat.text = "0";
            var textBoxFloatLE = textBoxFloat.gameObject.AddComponent<LayoutElement>();
            textBoxFloatLE.preferredWidth = TextBoxWidth;
            textBoxFloatLE.flexibleWidth = 0;

            var reset = UIUtility.CreateButton($"SliderSetResetButton", itemPanel.transform, "Reset");
            var resetLE = reset.gameObject.AddComponent<LayoutElement>();
            resetLE.preferredWidth = ResetButtonWidth;
            resetLE.flexibleWidth = 0;

            ValueSlider.onValueChanged.AddListener(value =>
            {
                textBoxFloat.text = value.ToString();
                textBoxFloat.onEndEdit.Invoke(value.ToString());
            });

            textBoxFloat.onEndEdit.AddListener(value =>
            {
                if (!float.TryParse(value, out float input))
                {
                    textBoxFloat.text = Value.ToString();
                    return;
                }
                Value = input;
            });

            reset.onClick.AddListener(() =>
            {
                Value = ValueOriginal;
            });
        }
    }
}
