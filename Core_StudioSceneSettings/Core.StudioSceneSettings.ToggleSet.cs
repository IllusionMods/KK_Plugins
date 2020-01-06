using System;
using TMPro;
using UnityEngine.UI;

namespace KK_Plugins.StudioSceneSettings
{
    public class ToggleSet
    {
        private bool CurrentValue = false;
        private bool EventsEnabled = true;

        #region Backing Fields
        private string _text;
        #endregion

        public TextMeshProUGUI Label { get; set; }
        public Toggle Toggle { get; set; }
        public Action<bool> Setter { get; set; }
        public bool InitialValue { get; set; } = false;
        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                Label.gameObject.name = $"Label {value}";
                Toggle.gameObject.name = $"Toggle {value}";
                Label.text = value;
            }
        }
        public bool Value
        {
            get => GetValue();
            set => SetValue(value);
        }

        public ToggleSet(TextMeshProUGUI label, Toggle toggle, string text, Action<bool> setter, bool initialValue)
        {
            Label = label;
            Toggle = toggle;
            Setter = setter;
            InitialValue = initialValue;
            Value = InitialValue;
            Text = text;

            Toggle.isOn = Value;
            Toggle.onValueChanged.RemoveAllListeners();
            Toggle.onValueChanged.AddListener(delegate (bool value)
            {
                if (!EventsEnabled) return;
                Value = value;
            });
        }

        public bool GetValue() => CurrentValue;
        public void SetValue(bool value) => SetValue(value, true);
        public void SetValue(bool value, bool triggerEvents)
        {
            EventsEnabled = false;
            CurrentValue = value;
            Toggle.isOn = value;
            EventsEnabled = true;
            if (triggerEvents)
                Setter.Invoke(value);
        }

        public void Reset() => Reset(true);
        public void Reset(bool triggerEvents)
        {
            EventsEnabled = false;
            CurrentValue = InitialValue;
            Toggle.isOn = InitialValue;
            EventsEnabled = true;
            if (triggerEvents)
                Setter.Invoke(InitialValue);
        }
    }
}