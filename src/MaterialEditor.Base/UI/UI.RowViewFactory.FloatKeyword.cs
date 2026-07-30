using UnityEngine;
using UnityEngine.UI;
using static MaterialEditorAPI.MaterialEditorUI;

namespace MaterialEditorAPI
{
    internal static class FloatKeywordRowViewFactory
    {
        internal static void CreateRows(Transform parent)
        {
            CreateFloatRow(parent);
            CreateKeywordRow(parent);
        }

        private static void CreateFloatRow(Transform parent)
        {
            var panel = RowViewFactorySupport.CreatePanel(
                "FloatPanel",
                parent,
                ItemColor,
                true);

            var label = RowViewFactorySupport.CreateLabel(
                "FloatLabel",
                panel.transform,
                string.Empty,
                0f,
                0f);
            label.gameObject.AddComponent<LabelClickTrigger>();

            RowViewFactorySupport.CreateInterpolableButton(
                "SelectInterpolableFloatButton",
                panel.transform,
                "Select currently selected float property as interpolable in timeline",
                true);

            MaterialEditorControlFactory.CreateSlider("FloatSlider", panel.transform);

            var input = MaterialEditorControlFactory.CreateNumericInput(
                "FloatInputField",
                panel.transform,
                NumericInputSpec.FloatingPoint);
            input.SetValue(0f);

            var reset = MaterialEditorControlFactory.CreateButton(
                "FloatResetButton",
                panel.transform,
                "Reset");
            TooltipManager.AddTooltip(
                reset.gameObject,
                "Reset the selected property to its original value");
            label.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(input.InputField);
        }

        private static void CreateKeywordRow(Transform parent)
        {
            var panel = RowViewFactorySupport.CreatePanel(
                "KeywordPanel",
                parent,
                ItemColor,
                true);

            var label = RowViewFactorySupport.CreateLabel(
                "KeywordLabel",
                panel.transform,
                string.Empty,
                LabelWidth,
                1f);
            label.gameObject.AddComponent<LabelClickTrigger>();

            RowViewFactorySupport.CreateLabel(
                "EmptySpace",
                panel.transform,
                string.Empty,
                InterpolableButtonWidth,
                0f);

            var toggle = MaterialEditorControlFactory.CreateToggle(
                "KeywordToggle",
                panel.transform,
                string.Empty);
            RowViewFactorySupport.SetWidth(toggle, KeywordToggleWidth);

            var reset = MaterialEditorControlFactory.CreateButton(
                "KeywordResetButton",
                panel.transform,
                "Reset");
            RowViewFactorySupport.SetWidth(reset, ResetButtonWidth);
            TooltipManager.AddTooltip(
                reset.gameObject,
                "Reset the selected property to its original value");
        }
    }
}
