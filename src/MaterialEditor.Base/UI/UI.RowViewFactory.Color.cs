using UnityEngine;
using UnityEngine.UI;
using static MaterialEditorAPI.MaterialEditorUI;

namespace MaterialEditorAPI
{
    internal static class ColorRowViewFactory
    {
        internal static void CreateRows(Transform parent)
        {
            var panel = RowViewFactorySupport.CreatePanel(
                "ColorPanel",
                parent,
                ItemColor,
                true);
            panel.GetComponent<HorizontalLayoutGroup>().childControlWidth = true;

            var label = RowViewFactorySupport.CreateLabel(
                "ColorLabel",
                panel.transform,
                string.Empty,
                0f,
                0f);
            label.gameObject.AddComponent<LabelClickTrigger>();

            RowViewFactorySupport.CreateInterpolableButton(
                "SelectInterpolableColorButton",
                panel.transform,
                "Select currently selected color property as interpolable in timeline",
                true);

            var red = CreateChannel(panel.transform, "R", "ColorRText", "ColorRInput");
            var green = CreateChannel(panel.transform, "G", "ColorGText", "ColorGInput");
            var blue = CreateChannel(panel.transform, "B", "ColorBText", "ColorBInput");
            var alpha = CreateChannel(panel.transform, "A", "ColorAText", "ColorAInput");

            MaterialEditorControlFactory.CreateButton(
                "ColorEditButton",
                panel.transform,
                string.Empty);

            var reset = MaterialEditorControlFactory.CreateButton(
                "ColorResetButton",
                panel.transform,
                "Reset");
            TooltipManager.AddTooltip(
                reset.gameObject,
                "Reset the selected property to its original value");

            red.Label.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(
                red.Input.InputField,
                new[] { green.Input.InputField, blue.Input.InputField });
            green.Label.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(
                green.Input.InputField,
                new[] { red.Input.InputField, blue.Input.InputField });
            blue.Label.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(
                blue.Input.InputField,
                new[] { red.Input.InputField, green.Input.InputField });
            alpha.Label.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(
                alpha.Input.InputField);
        }

        private static ColorChannelControls CreateChannel(
            Transform parent,
            string channel,
            string labelName,
            string inputName)
        {
            var label = MaterialEditorControlFactory.CreateText(labelName, parent, channel);
            label.alignment = TextAnchor.MiddleLeft;
            label.color = Color.black;

            var input = MaterialEditorControlFactory.CreateNumericInput(
                inputName,
                parent,
                NumericInputSpec.FloatingPoint);
            input.SetValue(0f);

            return new ColorChannelControls(label, input);
        }

        private sealed class ColorChannelControls
        {
            internal ColorChannelControls(Text label, NumericInputView input)
            {
                Label = label;
                Input = input;
            }

            internal Text Label { get; }
            internal NumericInputView Input { get; }
        }
    }
}
