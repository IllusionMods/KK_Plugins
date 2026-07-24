using System;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal enum RowColumnRole
    {
        Label,
        Timeline,
        Editor,
        Reset,
        Auxiliary
    }

    internal sealed class RowColumnLayoutOverride : MonoBehaviour, ILayoutElement
    {
        [SerializeField] private RowColumnRole _role;
        [SerializeField] private float _minWidth;
        [SerializeField] private float _preferredWidth;
        [SerializeField] private float _flexibleWidth;

        internal RowColumnRole Role => _role;

        internal void Configure(RowColumnSpec spec)
        {
            _role = spec.Role;
            _minWidth = spec.MinWidth;
            _preferredWidth = spec.PreferredWidth;
            _flexibleWidth = spec.FlexibleWidth;
            RestoreLayout();
        }

        internal void RestoreLayout()
        {
            var rect = transform as RectTransform;
            if (rect != null && _flexibleWidth <= 0f && _preferredWidth >= 0f)
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _preferredWidth);

            var parentRect = transform.parent as RectTransform;
            if (parentRect != null)
                LayoutRebuilder.MarkLayoutForRebuild(parentRect);
        }

        public void CalculateLayoutInputHorizontal()
        {
        }

        public void CalculateLayoutInputVertical()
        {
        }

        public float minWidth => _minWidth;
        public float preferredWidth => _preferredWidth;
        public float flexibleWidth => _flexibleWidth;
        public float minHeight => -1f;
        public float preferredHeight => -1f;
        public float flexibleHeight => -1f;
        public int layoutPriority => 100;
    }

    internal sealed class RowColumnSpec
    {
        internal RowColumnSpec(
            string objectName,
            RowColumnRole role,
            float minWidth,
            float preferredWidth,
            float flexibleWidth)
        {
            ObjectName = objectName;
            Role = role;
            MinWidth = minWidth;
            PreferredWidth = preferredWidth;
            FlexibleWidth = flexibleWidth;
        }

        internal string ObjectName { get; }
        internal RowColumnRole Role { get; }
        internal float MinWidth { get; }
        internal float PreferredWidth { get; }
        internal float FlexibleWidth { get; }

        internal static RowColumnSpec Fixed(
            string objectName,
            RowColumnRole role,
            float width)
        {
            return new RowColumnSpec(objectName, role, width, width, 0f);
        }

        internal static RowColumnSpec Flexible(
            string objectName,
            RowColumnRole role)
        {
            return new RowColumnSpec(objectName, role, 0f, 0f, 1f);
        }
    }

    internal sealed class RowLayoutSpec
    {
        internal RowLayoutSpec(string panelName, params RowColumnSpec[] columns)
        {
            PanelName = panelName;
            Columns = columns ?? new RowColumnSpec[0];
        }

        internal string PanelName { get; }
        internal RowColumnSpec[] Columns { get; }

        internal void Apply(GameObject rowRoot)
        {
            var panel = rowRoot.transform.Find(PanelName);
            if (panel == null)
                throw new InvalidOperationException("Missing row panel " + PanelName);

            var group = panel.GetComponent<HorizontalLayoutGroup>();
            if (group == null)
                throw new InvalidOperationException("Missing HorizontalLayoutGroup on " + PanelName);

            group.childControlWidth = true;
            group.childForceExpandWidth = false;
            group.childAlignment = TextAnchor.MiddleLeft;

            foreach (var column in Columns)
            {
                var child = panel.Find(column.ObjectName);
                if (child == null)
                    throw new InvalidOperationException(
                        "Missing " + column.ObjectName + " in " + PanelName);

                var layout = child.GetComponent<RowColumnLayoutOverride>()
                             ?? child.gameObject.AddComponent<RowColumnLayoutOverride>();
                layout.Configure(column);
            }

            LayoutRebuilder.MarkLayoutForRebuild((RectTransform)panel);
        }
    }

    internal static class RowLayoutCatalog
    {
        private static readonly RowLayoutSpec[] Specs =
        {
            new RowLayoutSpec(
                "ShaderRenderQueuePanel",
                RowColumnSpec.Flexible("ShaderRenderQueueLabel", RowColumnRole.Label),
                RowColumnSpec.Fixed(
                    "ShaderRenderQueueInput",
                    RowColumnRole.Editor,
                    MaterialEditorLayout.RenderQueueInputWidth),
                RowColumnSpec.Fixed(
                    "ShaderRenderQueueResetButton",
                    RowColumnRole.Reset,
                    MaterialEditorLayout.ResetButtonWidth)),
            new RowLayoutSpec(
                "OffsetScalePanel",
                RowColumnSpec.Flexible("OffsetScaleLabel", RowColumnRole.Label),
                RowColumnSpec.Fixed(
                    "EmptySpace",
                    RowColumnRole.Timeline,
                    MaterialEditorLayout.InterpolableButtonWidth),
                RowColumnSpec.Fixed(
                    "OffsetXText",
                    RowColumnRole.Editor,
                    MaterialEditorLayout.OffsetScaleLabelXWidth),
                RowColumnSpec.Fixed(
                    "OffsetXInput",
                    RowColumnRole.Editor,
                    MaterialEditorLayout.OffsetScaleInputWidth),
                RowColumnSpec.Fixed(
                    "OffsetYText",
                    RowColumnRole.Editor,
                    MaterialEditorLayout.OffsetScaleLabelYWidth),
                RowColumnSpec.Fixed(
                    "OffsetYInput",
                    RowColumnRole.Editor,
                    MaterialEditorLayout.OffsetScaleInputWidth),
                RowColumnSpec.Fixed(
                    "ScaleXText",
                    RowColumnRole.Editor,
                    MaterialEditorLayout.OffsetScaleLabelXWidth),
                RowColumnSpec.Fixed(
                    "ScaleXInput",
                    RowColumnRole.Editor,
                    MaterialEditorLayout.OffsetScaleInputWidth),
                RowColumnSpec.Fixed(
                    "ScaleYText",
                    RowColumnRole.Editor,
                    MaterialEditorLayout.OffsetScaleLabelYWidth),
                RowColumnSpec.Fixed(
                    "ScaleYInput",
                    RowColumnRole.Editor,
                    MaterialEditorLayout.OffsetScaleInputWidth),
                RowColumnSpec.Fixed(
                    "OffsetScaleResetButton",
                    RowColumnRole.Reset,
                    MaterialEditorLayout.ResetButtonWidth)),
            new RowLayoutSpec(
                "ColorPanel",
                RowColumnSpec.Flexible("ColorLabel", RowColumnRole.Label),
                RowColumnSpec.Fixed(
                    "SelectInterpolableColorButton",
                    RowColumnRole.Timeline,
                    MaterialEditorLayout.InterpolableButtonWidth),
                RowColumnSpec.Fixed(
                    "ColorRText",
                    RowColumnRole.Editor,
                    MaterialEditorLayout.ColorLabelWidth),
                RowColumnSpec.Fixed(
                    "ColorRInput",
                    RowColumnRole.Editor,
                    MaterialEditorLayout.ColorInputWidth),
                RowColumnSpec.Fixed(
                    "ColorGText",
                    RowColumnRole.Editor,
                    MaterialEditorLayout.ColorLabelWidth),
                RowColumnSpec.Fixed(
                    "ColorGInput",
                    RowColumnRole.Editor,
                    MaterialEditorLayout.ColorInputWidth),
                RowColumnSpec.Fixed(
                    "ColorBText",
                    RowColumnRole.Editor,
                    MaterialEditorLayout.ColorLabelWidth),
                RowColumnSpec.Fixed(
                    "ColorBInput",
                    RowColumnRole.Editor,
                    MaterialEditorLayout.ColorInputWidth),
                RowColumnSpec.Fixed(
                    "ColorAText",
                    RowColumnRole.Editor,
                    MaterialEditorLayout.ColorLabelWidth),
                RowColumnSpec.Fixed(
                    "ColorAInput",
                    RowColumnRole.Editor,
                    MaterialEditorLayout.ColorInputWidth),
                RowColumnSpec.Fixed(
                    "ColorEditButton",
                    RowColumnRole.Auxiliary,
                    MaterialEditorLayout.ColorEditButtonWidth),
                RowColumnSpec.Fixed(
                    "ColorResetButton",
                    RowColumnRole.Reset,
                    MaterialEditorLayout.ResetButtonWidth)),
            new RowLayoutSpec(
                "FloatPanel",
                RowColumnSpec.Flexible("FloatLabel", RowColumnRole.Label),
                RowColumnSpec.Fixed(
                    "SelectInterpolableFloatButton",
                    RowColumnRole.Timeline,
                    MaterialEditorLayout.InterpolableButtonWidth),
                RowColumnSpec.Fixed(
                    "FloatSlider",
                    RowColumnRole.Editor,
                    MaterialEditorLayout.FloatSliderWidth),
                RowColumnSpec.Fixed(
                    "FloatInputField",
                    RowColumnRole.Editor,
                    MaterialEditorLayout.FloatInputWidth),
                RowColumnSpec.Fixed(
                    "FloatResetButton",
                    RowColumnRole.Reset,
                    MaterialEditorLayout.ResetButtonWidth))
        };

        internal static void Apply(GameObject rowRoot)
        {
            foreach (var spec in Specs)
                spec.Apply(rowRoot);
        }

        internal static void Restore(GameObject rowRoot)
        {
            Apply(rowRoot);
            foreach (var layout in rowRoot.GetComponentsInChildren<RowColumnLayoutOverride>(true))
                layout.RestoreLayout();
        }
    }
}
