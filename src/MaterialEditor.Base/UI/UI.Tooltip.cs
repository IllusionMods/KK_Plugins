using System;
using System.Collections.Generic;
using UILib;
using UnityEngine;
using UnityEngine.UI;
using static MaterialEditorAPI.MaterialEditorPluginBase;

namespace MaterialEditorAPI
{
    internal class Tooltip
    {
        private static Tooltip instance;
        public static Tooltip Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Tooltip();
                }
                return instance;
            }
        }
        public Image Panel { get; private set; } = null;
        private RectTransform panelTransform = null;
        private Canvas parent = null;
        private readonly Text tooltipText;

        private Tooltip()
        {
            Panel = UIUtility.CreatePanel($"TooltipPanel");
            panelTransform = (RectTransform)Panel.transform;
            Panel.color = new Color(0.42f, 0.42f, 0.42f);

            tooltipText = UIUtility.CreateText($"ToolTipText", Panel.transform, "");
            //tooltipText.transform.SetRect(0f, 1f, 0.4f, 1f, 5f, -MaterialEditorUI.HeaderSize, -2f, -2f);
            tooltipText.alignment = TextAnchor.MiddleCenter;
            tooltipText.resizeTextForBestFit = false;
            tooltipText.resizeTextMaxSize = 11;
            tooltipText.resizeTextMinSize = 11;

            var layout = Panel.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 2, 2);
            var contentSizeFitter = Panel.gameObject.AddComponent<ContentSizeFitter>();
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Panel.gameObject.SetActive(false);
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


        public void SetParent(Canvas parent)
        {
            var transform = parent.transform;
                Panel.transform.SetParent(transform, false);
            this.parent = parent;
        }

        public bool IsActive()
        {
            return Panel.gameObject.activeInHierarchy;
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
