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
            if (float.TryParse(InputField.text, out float input))
            {
                multiplier = delta / input + 1;
                InputField.onEndEdit.Invoke((input + delta).ToString());
            }
            if (PairedInputFields?.Length > 0 && Input.GetKey(KeyCode.LeftAlt))
                foreach (var pairedInputField in PairedInputFields)
                    if (float.TryParse(pairedInputField.text, out float pairedInput))
                    {
                        if (Input.GetKey(KeyCode.Mouse1) && !float.IsInfinity(multiplier) && !float.IsNaN(multiplier))
                            pairedInputField.onEndEdit.Invoke((pairedInput * multiplier).ToString());
                        else
                            pairedInputField.onEndEdit.Invoke((pairedInput + delta).ToString());
                    }
        }
    }
}
