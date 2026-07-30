using System.Collections.Generic;
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
        private readonly HashSet<Tooltip> _tooltips = new HashSet<Tooltip>();

        public Image Panel { get; private set; } = null;
        private RectTransform panelTransform = null;
        private Text tooltipText;
        private Tooltip _hoveredTooltip;
        private bool _shiftPressed;
        private bool _standardTooltipsEnabled;
        private bool _shaderHintsEnabled;

        private void Update()
        {
            var shiftPressed = Input.GetKey(KeyCode.LeftShift)
                               || Input.GetKey(KeyCode.RightShift);
            var standardTooltipsEnabled =
                MaterialEditorPluginBase.Showtooltips == null
                || MaterialEditorPluginBase.Showtooltips.Value;
            var shaderHintsEnabled =
                MaterialEditorPluginBase.EnableShaderHints == null
                || MaterialEditorPluginBase.EnableShaderHints.Value;

            if (_shiftPressed != shiftPressed
                || _standardTooltipsEnabled != standardTooltipsEnabled
                || _shaderHintsEnabled != shaderHintsEnabled)
            {
                _shiftPressed = shiftPressed;
                _standardTooltipsEnabled = standardTooltipsEnabled;
                _shaderHintsEnabled = shaderHintsEnabled;
                RefreshHintIndicators();
                RefreshTooltipState();
            }

            if (Panel.gameObject.activeSelf)
                UpdatePanelPosition();
        }

        internal static void Init(Transform parent)
        {
            var tooltip = parent.gameObject.AddComponent<TooltipManager>();

            var panel = MaterialEditorControlFactory.CreatePanel("TooltipPanel", parent);
            var panelTransform = (RectTransform)panel.transform;

            panel.color = new Color(0.2f, 0.2f, 0.2f, 0.98f);
            panelTransform.pivot = Vector3.zero;
            panelTransform.anchorMax = Vector3.zero;
            panelTransform.anchorMin = Vector3.zero;
            panelTransform.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                TooltipWidth);

            var tooltipText = MaterialEditorControlFactory.CreateText(
                "ToolTipText",
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
            tooltip._shiftPressed = Input.GetKey(KeyCode.LeftShift)
                                      || Input.GetKey(KeyCode.RightShift);
            tooltip._standardTooltipsEnabled =
                MaterialEditorPluginBase.Showtooltips == null
                || MaterialEditorPluginBase.Showtooltips.Value;
            tooltip._shaderHintsEnabled =
                MaterialEditorPluginBase.EnableShaderHints == null
                || MaterialEditorPluginBase.EnableShaderHints.Value;
            Instance = tooltip;

            foreach (var existing in parent.GetComponentsInChildren<Tooltip>(true))
                tooltip.RegisterInternal(existing);
        }

        internal static void Register(Tooltip tooltip)
        {
            if (Instance != null)
                Instance.RegisterInternal(tooltip);
        }

        internal static void Unregister(Tooltip tooltip)
        {
            if (Instance != null)
                Instance.UnregisterInternal(tooltip);
        }

        internal static void NotifyTooltipChanged(Tooltip tooltip)
        {
            if (Instance == null || tooltip == null)
                return;

            if (tooltip.isActiveAndEnabled)
                Instance.RegisterInternal(tooltip);
            else
                Instance.UnregisterInternal(tooltip);
            tooltip.SetHintIndicatorVisible(
                Instance._shiftPressed && Instance._shaderHintsEnabled);
            if (Instance._hoveredTooltip == tooltip)
                Instance.RefreshTooltipState();
        }

        internal static void PointerStateChanged(Tooltip tooltip)
        {
            if (Instance == null || tooltip == null)
                return;

            if (tooltip.IsHovered)
                Instance._hoveredTooltip = tooltip;
            else if (Instance._hoveredTooltip == tooltip)
                Instance._hoveredTooltip = null;
            Instance.RefreshTooltipState();
        }

        public static Tooltip AddTooltip(GameObject go, string text)
        {
            var tooltip = go.GetComponent<Tooltip>();
            if (tooltip == null)
                tooltip = go.AddComponent<Tooltip>();
            tooltip.SetStandardTooltipText(text);
            return tooltip;
        }

        private void RegisterInternal(Tooltip tooltip)
        {
            if (tooltip == null)
                return;

            _tooltips.Add(tooltip);
            tooltip.SetHintIndicatorVisible(_shiftPressed && _shaderHintsEnabled);
        }

        private void UnregisterInternal(Tooltip tooltip)
        {
            if (tooltip == null)
                return;

            _tooltips.Remove(tooltip);
            tooltip.SetHintIndicatorVisible(false);
            if (_hoveredTooltip == tooltip)
            {
                _hoveredTooltip = null;
                HideTooltip();
            }
        }

        private void RefreshHintIndicators()
        {
            _tooltips.RemoveWhere(item => item == null);
            foreach (var tooltip in _tooltips)
                tooltip.SetHintIndicatorVisible(_shiftPressed && _shaderHintsEnabled);
        }

        private void RefreshTooltipState()
        {
            if (_hoveredTooltip == null || !_hoveredTooltip.isActiveAndEnabled)
            {
                HideTooltip();
                return;
            }

            var displayKind = TooltipDisplayPolicy.Resolve(
                _hoveredTooltip.IsHovered,
                _hoveredTooltip.InteractionSuppressed,
                _standardTooltipsEnabled,
                _shaderHintsEnabled,
                _shiftPressed,
                _hoveredTooltip.HasStandardTooltip,
                _hoveredTooltip.HasShaderHint);
            switch (displayKind)
            {
                case TooltipDisplayKind.ShaderHint:
                    ShowTooltip(_hoveredTooltip.ShaderHintText);
                    break;
                case TooltipDisplayKind.Standard:
                    ShowTooltip(_hoveredTooltip.StandardTooltipText);
                    break;
                default:
                    HideTooltip();
                    break;
            }
        }

        private void ShowTooltip(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                HideTooltip();
                return;
            }

            if (tooltipText.text != text)
            {
                tooltipText.text = text;
                RefreshLayout();
            }
            Panel.gameObject.SetActive(true);
        }

        private void HideTooltip()
        {
            Panel.gameObject.SetActive(false);
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

        private void UpdatePanelPosition()
        {
            var parent = panelTransform.parent as RectTransform;
            Vector2 position;
            if (parent == null
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parent,
                    Input.mousePosition,
                    null,
                    out position))
            {
                return;
            }

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

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
