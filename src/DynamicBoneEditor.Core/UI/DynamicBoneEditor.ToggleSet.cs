using System;
using UILib;
using UnityEngine;
using UnityEngine.UI;
using static KK_Plugins.DynamicBoneEditor.UI;

namespace KK_Plugins.DynamicBoneEditor
{
    public class ToggleSet
    {
        public string Label;
        public string[] Options;
        public int ValueOriginal;
        private Toggle[] Toggles;
        public Action<int> OnChange;
        private Text LabelText;

        private int _Value;
        public int Value
        {
            get => _Value;
            set
            {
                Toggles[value].isOn = true;
                if (_Value != value)
                {
                    _Value = value;
                    if (OnChange != null)
                        OnChange(value);
                    if (Value == ValueOriginal)
                        LabelText.text = Label;
                    else
                        LabelText.text = $"{Label}*";
                }
            }
        }

        public ToggleSet(string label, string[] options)
        {
            Label = label;
            Options = options;
        }

        public void CreateUI(Transform parent)
        {
            {
                var contentList = UIUtility.CreatePanel("ListEntry", parent);
                contentList.gameObject.AddComponent<LayoutElement>().preferredHeight = PanelHeight;
                contentList.gameObject.AddComponent<Mask>();
                contentList.color = RowColor;

                var itemPanel = UIUtility.CreatePanel("ToggleSetPanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>().padding = Padding;
                itemPanel.color = ItemColor;

                var toggleGroup = itemPanel.gameObject.AddComponent<ToggleGroup>();
                toggleGroup.allowSwitchOff = false;

                LabelText = UIUtility.CreateText("ToggleSetLabel", itemPanel.transform, Label);
                LabelText.alignment = TextAnchor.MiddleLeft;
                LabelText.color = Color.black;
                var labelLE = LabelText.gameObject.AddComponent<LayoutElement>();
                labelLE.preferredWidth = LabelWidth;
                labelLE.flexibleWidth = LabelWidth;

                Toggles = new Toggle[Options.Length];

                for (int i = 0; i < Options.Length; i++)
                {
                    int index = i;
                    Toggles[index] = UIUtility.CreateToggle(Options[index], itemPanel.transform, Options[index]);
                    Toggles[index].transform.SetRect(0f, 0f, 0f, 1f, 0f, 0f, 100f);
                    Toggles[index].isOn = false;
                    Toggles[index].group = toggleGroup;

                    Toggles[index].onValueChanged.AddListener(value =>
                    {
                        if (value)
                            Value = index;
                    });

                    var toggleLE = Toggles[index].gameObject.AddComponent<LayoutElement>();
                    toggleLE.preferredWidth = ToggleWidth;
                    toggleLE.flexibleWidth = 0;
                }

                var reset = UIUtility.CreateButton($"ToggleSetResetButton", itemPanel.transform, "Reset");
                var resetLE = reset.gameObject.AddComponent<LayoutElement>();
                resetLE.preferredWidth = ResetButtonWidth;
                resetLE.flexibleWidth = 0;

                reset.onClick.AddListener(() =>
                {
                    Value = ValueOriginal;
                });
            }
        }
    }
}
