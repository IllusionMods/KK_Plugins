using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal sealed class NumericInputSpec
    {
        internal static readonly NumericInputSpec FloatingPoint =
            new NumericInputSpec("0.####", "R");

        internal NumericInputSpec(string displayFormat, string editFormat)
        {
            DisplayFormat = displayFormat;
            EditFormat = editFormat;
        }

        internal string DisplayFormat { get; }
        internal string EditFormat { get; }
    }

    internal static class NumericText
    {
        internal static string FormatDisplay(float value, string format)
        {
            return value.ToString(format, CultureInfo.CurrentCulture);
        }

        internal static string FormatEdit(float value, string format)
        {
            return value.ToString(format, CultureInfo.CurrentCulture);
        }

        internal static bool TryParse(string text, out float value)
        {
            if (float.TryParse(
                text,
                NumberStyles.Float,
                CultureInfo.CurrentCulture,
                out value))
                return true;

            return float.TryParse(
                text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value);
        }
    }

    // Owns the complete numeric input hierarchy and display/edit text behavior.
    // The InputField GameObject name and component remain unchanged for API and
    // UI-hook compatibility.
    internal sealed class NumericInputView :
        MonoBehaviour,
        ISelectHandler,
        IDeselectHandler
    {
        [SerializeField] private InputField _inputField;
        [SerializeField] private RectTransform _viewport;
        [SerializeField] private string _displayFormat = "0.####";
        [SerializeField] private string _editFormat = "R";

        private bool _editing;
        private bool _hasValue;
        private float _value;

        internal InputField InputField =>
            _inputField ?? (_inputField = GetComponent<InputField>());

        internal float Value => _value;
        internal bool HasValue => _hasValue;

        internal void Initialize(NumericInputSpec spec)
        {
            _inputField = GetComponent<InputField>();
            _displayFormat = spec.DisplayFormat;
            _editFormat = spec.EditFormat;
            RestoreConfiguration();
        }

        internal void RestoreConfiguration()
        {
            var input = InputField;
            input.characterLimit = 0;
            input.lineType = InputField.LineType.SingleLine;
            input.textComponent.resizeTextForBestFit = false;
            input.textComponent.horizontalOverflow = HorizontalWrapMode.Overflow;
            input.textComponent.verticalOverflow = VerticalWrapMode.Truncate;

            EnsureViewport();
            if (GetComponent<RowColumnLayoutOverride>() == null)
                gameObject.AddComponent<RowColumnLayoutOverride>();
        }

        internal void SetValue(float value)
        {
            _value = value;
            _hasValue = true;
            RefreshText();
        }

        internal void CommitValue(float value)
        {
            _editing = false;
            _value = value;
            _hasValue = true;
            RefreshText();
        }

        internal bool TryParse(string text, out float value)
        {
            return NumericText.TryParse(text, out value);
        }

        internal string FormatEdit(float value)
        {
            return NumericText.FormatEdit(value, _editFormat);
        }

        public void OnSelect(BaseEventData eventData)
        {
            _editing = true;
            RefreshText();
            InputField.caretPosition = InputField.text.Length;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _editing = false;
        }

        private void OnDisable()
        {
            _editing = false;
        }

        private void RefreshText()
        {
            if (!_hasValue)
                return;

            SetTextWithoutNotification(
                InputField,
                _editing
                    ? NumericText.FormatEdit(_value, _editFormat)
                    : NumericText.FormatDisplay(_value, _displayFormat));
        }

        private static void SetTextWithoutNotification(InputField input, string text)
        {
            text = text ?? string.Empty;
            input.m_Text = text;
            input.m_CaretPosition = Mathf.Clamp(input.m_CaretPosition, 0, text.Length);
            input.m_CaretSelectPosition =
                Mathf.Clamp(input.m_CaretSelectPosition, 0, text.Length);
            input.UpdateLabel();
        }

        private void EnsureViewport()
        {
            var input = InputField;
            var textRect = input.textComponent.rectTransform;
            if (_viewport == null)
                _viewport = input.transform.Find(input.gameObject.name + "Viewport") as RectTransform;

            if (_viewport == null)
            {
                var leftInset = Mathf.Max(0f, textRect.offsetMin.x);
                var bottomInset = textRect.offsetMin.y;
                var rightInset = Mathf.Max(0f, -textRect.offsetMax.x);
                var topInset = -textRect.offsetMax.y;

                var viewportObject = new GameObject(
                    input.gameObject.name + "Viewport",
                    typeof(RectTransform),
                    typeof(RectMask2D));
                _viewport = viewportObject.GetComponent<RectTransform>();
                _viewport.SetParent(input.transform, false);
                _viewport.anchorMin = Vector2.zero;
                _viewport.anchorMax = Vector2.one;
                _viewport.offsetMin = new Vector2(leftInset, bottomInset);
                _viewport.offsetMax = new Vector2(-rightInset, -topInset);
            }

            if (input.placeholder is Graphic placeholder)
            {
                placeholder.rectTransform.SetParent(_viewport, false);
                SetRectToFill(placeholder.rectTransform);
            }

            textRect.SetParent(_viewport, false);
            SetRectToFill(textRect);
        }

        private static void SetRectToFill(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
