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
        private Canvas parent = null;
        private readonly Text tooltipText;

        private Tooltip()
        {
            Panel = UIUtility.CreatePanel($"TooltipPanel");
            Panel.color = new Color(0.42f, 0.42f, 0.42f);

            tooltipText = UIUtility.CreateText($"ToolTipText", Panel.transform, "");
            //tooltipText.transform.SetRect(0f, 1f, 0.4f, 1f, 5f, -MaterialEditorUI.HeaderSize, -2f, -2f);
            tooltipText.alignment = TextAnchor.MiddleCenter;
            tooltipText.fontSize = 16;

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
        }

        public void SetActive(bool active) 
        {
            Panel.gameObject.SetActive(active);
        }


        public void SetParent(Canvas parent)
        {
            MaterialEditorPluginBase.Logger.LogInfo("1");
            var transform = parent.transform;
            MaterialEditorPluginBase.Logger.LogInfo("2");
            MaterialEditorPluginBase.Logger.LogInfo(Panel == null);
                Panel.transform.SetParent(transform, false);
            MaterialEditorPluginBase.Logger.LogInfo("3");
            this.parent = parent;
        }

        public bool IsActive()
        {
            return Panel.gameObject.activeInHierarchy;
        }

        public void UpdatePosition()
        {
            if (parent != null)
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)parent.transform, Input.mousePosition, parent.worldCamera, out Vector2 localPoint))
                {
                    Panel.transform.position = parent.transform.TransformPoint(localPoint);
                    Panel.transform.position += new Vector3(0, Screen.height / 60);
                }
        }

        public void SetPosition(GameObject go)
        {
            if (go.transform is RectTransform transform)
            {
                Panel.transform.position = new Vector3(transform.position.x, transform.position.y + transform.rect.height + 15);
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
