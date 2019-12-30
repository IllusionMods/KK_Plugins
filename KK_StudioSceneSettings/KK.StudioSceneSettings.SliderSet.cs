using System;
using TMPro;
using UnityEngine.UI;

namespace KK_Plugins
{

    public partial class StudioSceneSettings
    {
        public class SliderSet
        {
            private float CurrentValue = 0f;
            private bool EventsEnabled = true;

            #region Backing Fields
            private string _text;
            private float _sliderMinimum;
            private float _sliderMaximum;
            #endregion

            public TextMeshProUGUI Label { get; set; }
            public Slider Slider { get; set; }
            public InputField Input { get; set; }
            public Button Button { get; set; }
            public Action<float> Setter { get; set; }
            public float InitialValue { get; set; }

            public float SliderMinimum
            {
                get => _sliderMinimum;
                set
                {
                    _sliderMinimum = value;
                    Slider.minValue = value;
                }
            }
            public float SliderMaximum
            {
                get => _sliderMaximum;
                set
                {
                    _sliderMaximum = value;
                    Slider.maxValue = value;
                }
            }
            public bool EnforceSliderMinimum { get; set; } = true;
            public bool EnforceSliderMaximum { get; set; } = true;
            public string Text
            {
                get => _text;
                set
                {
                    _text = value;
                    Label.gameObject.name = $"Label {value}";
                    Slider.gameObject.name = $"Slider {value}";
                    Input.gameObject.name = $"InputField {value}";
                    Button.gameObject.name = $"Button {value}";

                    Label.text = value;
                }
            }
            public float Value
            {
                get => GetValue();
                set => SetValue(value);
            }

            public SliderSet(TextMeshProUGUI label, Slider slider, InputField input, Button button, string text, Action<float> setter, float initialValue, float sliderMinimum, float sliderMaximum)
            {
                Label = label;
                Slider = slider;
                Input = input;
                Button = button;
                Text = text;
                Setter = setter;
                InitialValue = initialValue;
                SliderMinimum = sliderMinimum;
                SliderMaximum = sliderMaximum;

                Init();
            }

            private void Init()
            {
                Slider.onValueChanged.RemoveAllListeners();
                Input.onValueChanged.RemoveAllListeners();
                Input.onEndEdit.RemoveAllListeners();
                Button.onClick.RemoveAllListeners();

                Slider.maxValue = SliderMaximum;
                Slider.value = InitialValue;
                Input.text = InitialValue.ToString();

                Slider.onValueChanged.AddListener(delegate (float value)
                {
                    if (!EventsEnabled) return;
                    SetValue(value);
                });

                Input.onEndEdit.AddListener(delegate (string value)
                {
                    if (!EventsEnabled) return;
                    SetValue(value);
                });

                Button.onClick.AddListener(Reset);
            }

            public float GetValue() => CurrentValue;
            public void SetValue(string value) => SetValue(value, true);
            public void SetValue(string value, bool triggerEvents)
            {
                if (!float.TryParse(value, out float valuef))
                    valuef = CurrentValue;
                SetValue(valuef, triggerEvents);
            }

            public void SetValue(float value) => SetValue(value, true);
            public void SetValue(float value, bool triggerEvents)
            {
                if (EnforceSliderMinimum && value < SliderMinimum)
                    value = SliderMinimum;
                if (EnforceSliderMaximum && value > SliderMaximum)
                    value = SliderMaximum;

                EventsEnabled = false;
                CurrentValue = value;
                Slider.value = value;
                Input.text = value.ToString();
                EventsEnabled = true;
                if (triggerEvents)
                    Setter.Invoke(value);
            }

            public void Reset() => Reset(true);
            public void Reset(bool triggerEvents)
            {
                EventsEnabled = false;
                CurrentValue = InitialValue;
                Slider.value = InitialValue;
                Input.text = InitialValue.ToString();
                EventsEnabled = true;
                if (triggerEvents)
                    Setter.Invoke(InitialValue);
            }
        }
    }
}