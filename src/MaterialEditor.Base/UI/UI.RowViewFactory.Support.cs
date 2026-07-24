using UnityEngine;
using UnityEngine.UI;
using static MaterialEditorAPI.MaterialEditorUI;

namespace MaterialEditorAPI
{
    internal static class RowViewFactorySupport
    {
        internal static Image CreatePanel(string name, Transform parent, Color color)
        {
            var panel = MaterialEditorControlFactory.CreatePanel(name, parent);
            panel.gameObject.AddComponent<CanvasGroup>();
            panel.color = color;

            var layout = panel.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = Padding;
            layout.childForceExpandWidth = false;
            layout.childAlignment = TextAnchor.MiddleLeft;
            return panel;
        }

        internal static Text CreateLabel(
            string name,
            Transform parent,
            string value,
            float width,
            float flexibleWidth)
        {
            var label = MaterialEditorControlFactory.CreateText(name, parent, value);
            label.alignment = TextAnchor.MiddleLeft;
            label.color = Color.black;
            SetWidth(label, width, flexibleWidth);
            return label;
        }

        internal static LayoutElement SetWidth(
            Component component,
            float width,
            float flexibleWidth = 0f)
        {
            var layout = component.gameObject.AddComponent<LayoutElement>();
            layout.minWidth = width;
            layout.preferredWidth = width;
            layout.flexibleWidth = flexibleWidth;
            return layout;
        }

        internal static void CreateInterpolableButton(
            string objectName,
            Transform parent,
            string tooltipText,
            bool layoutOwnedBySpec = false)
        {
            var button = MaterialEditorControlFactory.CreateButton(objectName, parent, "O");
            if (!layoutOwnedBySpec)
                SetWidth(button, InterpolableButtonWidth);

            button.gameObject.SetActive(false);
            TooltipManager.AddTooltip(button.gameObject, tooltipText);

#if !API && !EC
            if (TimelineCompatibilityHelper.IsTimelineAvailable())
                button.gameObject.SetActive(true);
#endif
        }
    }
}
