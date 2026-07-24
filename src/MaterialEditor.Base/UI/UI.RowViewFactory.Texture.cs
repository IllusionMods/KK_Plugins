using UnityEngine;
using UnityEngine.UI;
using static MaterialEditorAPI.MaterialEditorUI;

namespace MaterialEditorAPI
{
    internal static class TextureRowViewFactory
    {
        internal static void CreateRows(Transform parent)
        {
            CreateCategoryRow(parent);
            CreateTextureRow(parent);
            CreateOffsetScaleRow(parent);
        }

        private static void CreateCategoryRow(Transform parent)
        {
            var panel = RowViewFactorySupport.CreatePanel(
                "PropertyCategoryPanel",
                parent,
                CategoryColor);
            panel.GetComponent<HorizontalLayoutGroup>().spacing = 2f;

            var collapse = MaterialEditorControlFactory.CreateButton(
                "PropertyCategoryCollapseButton",
                panel.transform,
                "-");
            RowViewFactorySupport.SetWidth(collapse, SmallButtonWidth);
            TooltipManager.AddTooltip(
                collapse.gameObject,
                "Expand or collapse this category");

            var label = RowViewFactorySupport.CreateLabel(
                "PropertyCategoryLabel",
                panel.transform,
                string.Empty,
                LabelWidth,
                1f);
            TooltipManager.AddTooltip(label.gameObject, "Category name");
        }

        private static void CreateTextureRow(Transform parent)
        {
            var panel = RowViewFactorySupport.CreatePanel("TexturePanel", parent, ItemColor);
            var label = RowViewFactorySupport.CreateLabel(
                "TextureLabel",
                panel.transform,
                string.Empty,
                LabelWidth,
                1f);
            label.gameObject.AddComponent<LabelClickTrigger>();

            RowViewFactorySupport.CreateInterpolableButton(
                "SelectInterpolableTextureButton",
                panel.transform,
                "Select the currently selected texture property and its offset and scale properties as interpolables in timeline");

            var export = MaterialEditorControlFactory.CreateButton(
                "TextureExportButton",
                panel.transform,
                "Export Texture");
            RowViewFactorySupport.SetWidth(export, TextureButtonWidth);

            var import = MaterialEditorControlFactory.CreateButton(
                "TextureImportButton",
                panel.transform,
                "Import Texture");
            RowViewFactorySupport.SetWidth(import, TextureButtonWidth);

            var reset = MaterialEditorControlFactory.CreateButton(
                "TextureResetButton",
                panel.transform,
                "Reset");
            RowViewFactorySupport.SetWidth(reset, ResetButtonWidth);
            TooltipManager.AddTooltip(
                reset.gameObject,
                "Reset this property to its original value.\n\nIn order for the reset to take effect you need to either save and re-load the scene, or copy the object and delete the old one");
        }

        private static void CreateOffsetScaleRow(Transform parent)
        {
            var panel = RowViewFactorySupport.CreatePanel(
                "OffsetScalePanel",
                parent,
                ItemColor);

            var label = MaterialEditorControlFactory.CreateText(
                "OffsetScaleLabel",
                panel.transform,
                string.Empty);
            label.gameObject.AddComponent<LabelClickTrigger>();
            label.alignment = TextAnchor.MiddleLeft;
            label.color = Color.black;

            var emptySpace = MaterialEditorControlFactory.CreateText(
                "EmptySpace",
                panel.transform,
                string.Empty);
            emptySpace.alignment = TextAnchor.MiddleLeft;

            var offsetXLabel = CreateCoordinateLabel(
                "OffsetXText",
                panel.transform,
                "OffsetX");
            offsetXLabel.gameObject.AddComponent<LabelClickTrigger>();
            var offsetX = CreateNumericInput(
                "OffsetXInput",
                panel.transform,
                "Adjust the horizontal offset of the texture. It can move the texture left or right.");

            var offsetYLabel = CreateCoordinateLabel(
                "OffsetYText",
                panel.transform,
                "Y");
            var offsetY = CreateNumericInput(
                "OffsetYInput",
                panel.transform,
                "Adjust the vertical offset of the texture. It can move the texture up or down.");

            offsetXLabel.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(
                offsetX.InputField,
                new[] { offsetY.InputField });
            offsetYLabel.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(
                offsetY.InputField,
                new[] { offsetX.InputField });

            var scaleXLabel = CreateCoordinateLabel(
                "ScaleXText",
                panel.transform,
                "ScaleX");
            var scaleX = CreateNumericInput(
                "ScaleXInput",
                panel.transform,
                "Adjust the horizontal scale of the texture. Values greater than 1 make the texture appear smaller horizontally, values less than 1 make it appear larger horizontally.");

            var scaleYLabel = CreateCoordinateLabel(
                "ScaleYText",
                panel.transform,
                "Y");
            var scaleY = CreateNumericInput(
                "ScaleYInput",
                panel.transform,
                "Adjust the vertical scale of the texture. Values greater than 1 make the texture appear smaller vertically, values less than 1 make it appear larger vertically.");

            var reset = MaterialEditorControlFactory.CreateButton(
                "OffsetScaleResetButton",
                panel.transform,
                "Reset");
            TooltipManager.AddTooltip(
                reset.gameObject,
                "Reset both the scale and offset properties to their original values");

            scaleXLabel.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(
                scaleX.InputField,
                new[] { scaleY.InputField });
            scaleYLabel.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(
                scaleY.InputField,
                new[] { scaleX.InputField });
        }

        private static Text CreateCoordinateLabel(
            string name,
            Transform parent,
            string text)
        {
            var label = MaterialEditorControlFactory.CreateText(name, parent, text);
            label.alignment = TextAnchor.MiddleLeft;
            label.color = Color.black;
            return label;
        }

        private static NumericInputView CreateNumericInput(
            string name,
            Transform parent,
            string tooltip)
        {
            var input = MaterialEditorControlFactory.CreateNumericInput(
                name,
                parent,
                NumericInputSpec.FloatingPoint);
            input.SetValue(0f);
            TooltipManager.AddTooltip(input.gameObject, tooltip);
            return input;
        }
    }
}
