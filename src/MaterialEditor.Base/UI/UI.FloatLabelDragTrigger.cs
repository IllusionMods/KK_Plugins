using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    /// <summary>
    /// Allows dragging to adjust the value of an InputField.
    /// </summary>
    [DisallowMultipleComponent]
    public class FloatLabelDragTrigger : MonoBehaviour, IDragHandler
    {
        /// <summary>
        /// The InputField that will be adjusted when dragging.
        /// </summary>
        public InputField InputField = null;
        /// <summary>
        /// Optional paired InputFields that will also be adjusted when dragging.
        /// </summary>
        public InputField[] PairedInputFields = null;

        /// <summary>
        /// Initializes the FloatLabelDragTrigger with the specified InputField and optional paired InputFields.
        /// </summary>
        public void Initialize(InputField inputField, InputField[] pairedInputFields = null)
        {
            InputField = inputField;
            PairedInputFields = pairedInputFields;
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            if (InputField == null) return;

            float multiplier = 0f;
            float delta = eventData.delta.x / Screen.dpi * (Input.GetKey(KeyCode.LeftShift) ? 10f : 1f) / (Input.GetKey(KeyCode.LeftControl) ? 10f : 1f) * (MaterialEditorPluginBase.DragSensitivity.Value / 100f);
            if (TryGetValue(InputField, out float input))
            {
                multiplier = delta / input + 1;
                InvokeValue(InputField, input + delta);
            }
            if (PairedInputFields?.Length > 0 && Input.GetKey(KeyCode.LeftAlt))
                foreach (var pairedInputField in PairedInputFields)
                    if (TryGetValue(pairedInputField, out float pairedInput))
                    {
                        if (Input.GetKey(KeyCode.Mouse1) && !float.IsInfinity(multiplier) && !float.IsNaN(multiplier))
                            InvokeValue(pairedInputField, pairedInput * multiplier);
                        else
                            InvokeValue(pairedInputField, pairedInput + delta);
                    }
        }

        private static bool TryGetValue(InputField inputField, out float value)
        {
            var numeric = inputField.GetComponent<NumericInputView>();
            if (numeric != null && numeric.HasValue)
            {
                value = numeric.Value;
                return true;
            }

            return float.TryParse(inputField.text, out value);
        }

        private static void InvokeValue(InputField inputField, float value)
        {
            var numeric = inputField.GetComponent<NumericInputView>();
            inputField.onEndEdit.Invoke(
                numeric != null ? numeric.FormatEdit(value) : value.ToString());
        }
    }
}
