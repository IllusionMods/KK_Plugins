using System;
using System.Collections.Generic;
using UILib;
using UnityEngine;
using UnityEngine.UI;
using static MaterialEditorAPI.MaterialEditorPluginBase;

namespace MaterialEditorAPI
{
    internal class TooltipManager : MonoBehaviour
    {
        private static TooltipManager Instance;
        public Image Panel { get; private set; } = null;
        private RectTransform panelTransform = null;
        private Text tooltipText;

        internal static void Init(Transform parent)
        {
            var tooltip = parent.gameObject.AddComponent<TooltipManager>();

            var panel = UIUtility.CreatePanel($"TooltipPanel", parent);
            panel.color = new Color(0.42f, 0.42f, 0.42f);

            var tooltipText = UIUtility.CreateText($"ToolTipText", panel.transform, "");
            tooltipText.alignment = TextAnchor.MiddleCenter;
            tooltipText.resizeTextForBestFit = false;
            tooltipText.resizeTextMaxSize = 11;
            tooltipText.resizeTextMinSize = 11;

            var layout = panel.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 2, 2);
            var contentSizeFitter = panel.gameObject.AddComponent<ContentSizeFitter>();
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            panel.gameObject.SetActive(false);

            tooltip.Panel = panel;
            tooltip.panelTransform = (RectTransform)panel.transform;
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

        public void SetPosition(GameObject go)
        {
            if (go.transform is RectTransform transform)
            {
                var newPos = new Vector3(transform.position.x, transform.position.y + transform.rect.height + 15);
                var leftX = newPos.x - panelTransform.rect.width / 2;
                var rightX = newPos.x + panelTransform.rect.width / 2;
                var topY = newPos.y + panelTransform.rect.height / 2;

                MaterialEditorPluginBase.Logger.LogInfo(transform.position);
                if (leftX < 0)
                {
                    MaterialEditorPluginBase.Logger.LogInfo("Shift right");
                    newPos.x += leftX * -1 + 15;
                }
                else if (rightX > Screen.width)
                {
                    MaterialEditorPluginBase.Logger.LogInfo("Shift left");
                    newPos.x -= rightX - Screen.width + 15;
                }
                if (topY > Screen.height)
                {
                    MaterialEditorPluginBase.Logger.LogInfo("Shift down");
                    newPos.y = transform.position.y - transform.rect.height - 15;
                }

                Panel.transform.position = newPos;
            }
        }

        public static PointerEnterHandler AddTooltip(GameObject go, string text)
        {
            var pointerEnter = go.AddComponent<PointerEnterHandler>();
            pointerEnter.onPointerEnter = (e) => {
                Instance.SetToolTipText(text, true);
                Instance.SetPosition(go);
            };
            pointerEnter.onPointerExit = (e) => { Instance.SetActive(false); };
            return pointerEnter;
        }
    }
}
