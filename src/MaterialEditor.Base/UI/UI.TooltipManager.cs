using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal class TooltipManager : MonoBehaviour
    {
        private const float TooltipWidth = 280f;
        private const float TooltipMaximumHeight = 360f;
        private static TooltipManager Instance;
        public Image Panel { get; private set; } = null;
        private RectTransform panelTransform = null;
        private Text tooltipText;

        private void Update()
        {
            if (MaterialEditorPluginBase.Showtooltips.Value && Panel.gameObject.activeSelf)
            {
                var parent = panelTransform.parent as RectTransform;
                Vector2 position;
                if (parent != null
                    && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        parent,
                        Input.mousePosition,
                        null,
                        out position))
                {
                    position += new Vector2(5f, 5f);
                    var parentRect = parent.rect;
                    var size = panelTransform.rect.size;
                    position.x = Mathf.Clamp(
                        position.x,
                        parentRect.xMin,
                        parentRect.xMax - size.x);
                    position.y = Mathf.Clamp(
                        position.y,
                        parentRect.yMin,
                        parentRect.yMax - size.y);
                    panelTransform.localPosition = position;
                }
            }
        }

        internal static void Init(Transform parent)
        {
            var tooltip = parent.gameObject.AddComponent<TooltipManager>();

            var panel = MaterialEditorControlFactory.CreatePanel($"TooltipPanel", parent);
            var panelTransform = (RectTransform)panel.transform;

            panel.color = new Color(0.2f, 0.2f, 0.2f, 0.98f);
            panelTransform.pivot = Vector3.zero;
            panelTransform.anchorMax = Vector3.zero;
            panelTransform.anchorMin = Vector3.zero;
            panelTransform.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                TooltipWidth);

            var tooltipText = MaterialEditorControlFactory.CreateText(
                $"ToolTipText",
                panel.transform,
                "",
                MaterialEditorTextRole.Tooltip);
            tooltipText.alignment = TextAnchor.MiddleCenter;
            tooltipText.resizeTextForBestFit = false;
            tooltipText.fontSize = 11;
            tooltipText.supportRichText = false;
            tooltipText.horizontalOverflow = HorizontalWrapMode.Wrap;
            tooltipText.verticalOverflow = VerticalWrapMode.Overflow;
            var layout = panel.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 2, 2);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            var contentSizeFitter = panel.gameObject.AddComponent<ContentSizeFitter>();
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            panel.gameObject.SetActive(false);

            tooltip.Panel = panel;
            tooltip.panelTransform = panelTransform;
            tooltip.tooltipText = tooltipText;
            Instance = tooltip;
        }

        public static void SetToolTipText(string text, bool setActive = false)
        {
            Instance.tooltipText.text = text;
            Instance.RefreshLayout();
            if (setActive)
                SetActive(true);
        }

        private void RefreshLayout()
        {
            var textWidth = TooltipWidth - 8f;
            tooltipText.rectTransform.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                textWidth);
            var height = Mathf.Min(
                TooltipMaximumHeight,
                tooltipText.preferredHeight + 4f);
            panelTransform.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                height);
            tooltipText.verticalOverflow = VerticalWrapMode.Truncate;
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelTransform);
        }

        public static void SetActive(bool active)
        {
            if (MaterialEditorPluginBase.Showtooltips.Value || !active)
                Instance.Panel.gameObject.SetActive(active);
        }

        public static Tooltip AddTooltip(GameObject go, string text)
        {
            var tooltip = go.AddComponent<Tooltip>();
            tooltip.TooltipText = text;
            return tooltip;
        }
    }
}
