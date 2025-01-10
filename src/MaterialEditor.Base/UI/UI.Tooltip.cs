using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal class TooltipManager : MonoBehaviour
    {
        private static TooltipManager Instance;
        public Image Panel { get; private set; } = null;
        private RectTransform panelTransform = null;
        private Text tooltipText;

        private void Update()
        {
            var newPos = Input.mousePosition / panelTransform.localScale.x;
            panelTransform.position = new Vector3(newPos.x + 5, newPos.y + 5, newPos.z);
        }

        internal static void Init(Transform parent)
        {
            var tooltip = parent.gameObject.AddComponent<TooltipManager>();

            var panel = UIUtility.CreatePanel($"TooltipPanel", parent);
            var panelTransform = (RectTransform)panel.transform;

            panel.color = new Color(0.2f, 0.2f, 0.2f, 0.98f);
            panelTransform.pivot = Vector3.zero;
            panelTransform.anchorMax = Vector3.zero;
            panelTransform.anchorMin = Vector3.zero;
            panelTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 200);

            var tooltipText = UIUtility.CreateText($"ToolTipText", panel.transform, "");
            tooltipText.alignment = TextAnchor.MiddleCenter;
            tooltipText.resizeTextForBestFit = false;
            tooltipText.fontSize = 11;
            var layout = panel.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 2, 2);
            var contentSizeFitter = panel.gameObject.AddComponent<ContentSizeFitter>();
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            panel.gameObject.SetActive(false);

            tooltip.Panel = panel;
            tooltip.panelTransform = panelTransform;
            tooltip.tooltipText = tooltipText;
            Instance = tooltip;
        }

        public void SetToolTipText(string text, bool setActive = false)
        {
            tooltipText.text = text;
            if (setActive)
                SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelTransform);
        }

        public void SetActive(bool active) 
        {
            Panel.gameObject.SetActive(active);
        }

        public static PointerEnterHandler AddTooltip(GameObject go, string text)
        {
            var pointerEnter = go.AddComponent<PointerEnterHandler>();
            pointerEnter.onPointerEnter = (e) => {
                Instance.SetToolTipText(text, true);
            };
            pointerEnter.onPointerExit = (e) => { Instance.SetActive(false); };
            return pointerEnter;
        }
    }
}
