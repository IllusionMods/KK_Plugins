using System;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal static class MaterialEditorDropdownStyles
    {
        internal static void ApplyDropdown(Dropdown dropdown)
        {
            if (dropdown == null)
                return;

            MaterialEditorControlStyleState.Assign(
                dropdown,
                MaterialEditorControlStyleRole.Dropdown);
            MaterialEditorScrollSelectableStyles.ApplySelectable(
                dropdown,
                MaterialEditorTheme.Colors.DropdownSurface,
                MaterialEditorTheme.Colors.ControlHover,
                MaterialEditorTheme.Colors.ControlPressed,
                MaterialEditorTheme.Colors.ControlDisabled);
            MaterialEditorScrollSelectableStyles.ApplyControlOutline(
                dropdown.targetGraphic,
                MaterialEditorThemeColorRole.InputBorder);
            ApplyDropdownCaptionText(dropdown.captionText);
            ApplyDropdownItemText(dropdown.itemText, dropdown.captionText);

            var arrow = dropdown.transform.Find("Arrow");
            if (arrow != null)
            {
                var arrowImage = arrow.GetComponent<Image>();
                if (arrowImage != null)
                    arrowImage.color = MaterialEditorTheme.Colors.SecondaryText;
            }

            if (dropdown.template != null)
            {
                var templateImage = dropdown.template.GetComponent<Image>();
                if (templateImage != null)
                {
                    templateImage.color = MaterialEditorTheme.Colors.PopupSurface;
                    templateImage.maskable = true;
                    MaterialEditorScrollSelectableStyles.ApplyControlOutline(
                        templateImage,
                        MaterialEditorThemeColorRole.StrongBorder);
                }

                var templateScroll = dropdown.template.GetComponentInChildren<ScrollRect>(true);
                if (templateScroll != null)
                    MaterialEditorScrollSelectableStyles.ApplyDropdownScrollView(templateScroll);

                var popupStyle = dropdown.template.GetComponent<MaterialEditorDropdownPopupStyle>()
                                 ?? dropdown.template.gameObject.AddComponent<MaterialEditorDropdownPopupStyle>();
                popupStyle.Configure(dropdown);
            }

            if (dropdown.itemText != null)
            {
                var itemToggle = dropdown.itemText.GetComponentInParent<Toggle>();
                if (itemToggle != null)
                {
                    ApplyDropdownItem(itemToggle, dropdown.itemText);
                    ApplyDropdownItemText(dropdown.itemText, dropdown.captionText);
                }
            }
            MaterialEditorPanelTextStyles.ApplyTypography(dropdown.gameObject);
            ApplyDropdownCaptionText(dropdown.captionText);
            ApplyDropdownItemText(dropdown.itemText, dropdown.captionText);
            MaterialEditorDropdownCaptionFitter.Configure(dropdown);
        }

        internal static void ReapplyTheme(
            MaterialEditorControlStyleState state)
        {
            if (state == null
                || state.Role != MaterialEditorControlStyleRole.Dropdown)
                return;

            ApplyDropdown(state.GetComponent<Dropdown>());
        }

        internal static void ApplyDropdownPopup(
            Dropdown dropdown,
            Transform popupRoot,
            InputField filter,
            Button clearButton)
        {
            if (popupRoot == null)
                return;

            var popupImage = popupRoot.GetComponent<Image>();
            if (popupImage != null)
            {
                popupImage.color = MaterialEditorTheme.Colors.PopupSurface;
                popupImage.maskable = true;
                MaterialEditorScrollSelectableStyles.ApplyControlOutline(
                    popupImage,
                    MaterialEditorThemeColorRole.StrongBorder);
            }

            var scroll = popupRoot.GetComponentInChildren<ScrollRect>(true);
            if (scroll != null)
                MaterialEditorScrollSelectableStyles.ApplyDropdownScrollView(scroll);

            if (filter != null)
                MaterialEditorInputStyles.ApplyInputField(filter);
            if (clearButton != null)
                MaterialEditorSelectionStyles.ApplyButton(clearButton);

            if (dropdown != null)
            {
                ApplyDropdownCaptionText(dropdown.captionText);
                ApplyDropdownItemText(dropdown.itemText, dropdown.captionText);
            }

            var items = popupRoot.GetComponentsInChildren<Toggle>(true);
            for (var i = 0; i < items.Length; i++)
            {
                var itemText = items[i].GetComponentInChildren<Text>(true);
                ApplyDropdownItem(items[i], itemText);
                ApplyDropdownItemState(items[i], itemText, items[i].isOn);
            }
        }

        private static void ApplyDropdownCaptionText(Text text)
        {
            if (text == null)
                return;

            MaterialEditorPanelTextStyles.ApplyText(text, MaterialEditorTextRole.Input);
            MaterialEditorPanelTextStyles.ApplyTextRendering(text, null);
            text.maskable = true;
            text.raycastTarget = false;
            text.fontSize = MaterialEditorLayout.DropdownFontSize;
            text.resizeTextForBestFit = false;
            text.resizeTextMinSize = MaterialEditorLayout.DropdownFontSize;
            text.resizeTextMaxSize = MaterialEditorLayout.DropdownFontSize;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            ApplyDropdownTextInsets(text);
        }

        private static void ApplyDropdownItemText(
            Text text,
            Text renderingSource)
        {
            if (text == null)
                return;

            MaterialEditorPanelTextStyles.ApplyText(
                text,
                MaterialEditorTextRole.Input);
            MaterialEditorPanelTextStyles.ApplyTextRendering(
                text,
                renderingSource);
            text.maskable = true;
            text.raycastTarget = false;
            MaterialEditorTextFitting.ApplyAdaptiveSingleLine(
                text,
                MaterialEditorLayout.DropdownFontSize);
            ApplyDropdownTextInsets(text);
        }

        private static void ApplyDropdownTextInsets(Text text)
        {
            if (text == null)
                return;

            var rect = text.rectTransform;
            rect.offsetMin = new Vector2(
                rect.offsetMin.x,
                MaterialEditorLayout.DropdownTextVerticalInset);
            rect.offsetMax = new Vector2(
                rect.offsetMax.x,
                -MaterialEditorLayout.DropdownTextVerticalInset);

            text.SetVerticesDirty();
        }

        private static void ApplyDropdownItem(Toggle toggle, Text text)
        {
            if (toggle == null)
                return;

            if (text != null)
                TooltipManager.AddTooltip(toggle.gameObject, text.text);

            MaterialEditorScrollSelectableStyles.ApplySelectable(
                toggle,
                MaterialEditorTheme.Colors.PopupSurface,
                MaterialEditorTheme.Colors.Hover,
                MaterialEditorTheme.Colors.Pressed,
                MaterialEditorTheme.Colors.DisabledSurface);
            MaterialEditorInputStyles.ApplyToggleMark(toggle);
            ApplyDropdownItemText(text, null);

            var state = toggle.GetComponent<MaterialEditorDropdownItemStyle>()
                        ?? toggle.gameObject.AddComponent<MaterialEditorDropdownItemStyle>();
            state.Configure(toggle, text);
        }

        internal static void ApplyDropdownItemState(
            Toggle toggle,
            Text text,
            bool selected)
        {
            if (toggle == null)
                return;

            var colors = toggle.colors;
            colors.normalColor = selected
                ? MaterialEditorTheme.Colors.Selected
                : MaterialEditorTheme.Colors.PopupSurface;
            colors.highlightedColor = selected
                ? MaterialEditorTheme.Colors.Selected
                : MaterialEditorTheme.Colors.Hover;
            colors.pressedColor = selected
                ? MaterialEditorTheme.Colors.Selected
                : MaterialEditorTheme.Colors.Pressed;
            colors.disabledColor = MaterialEditorTheme.Colors.DisabledSurface;
            colors.colorMultiplier = 1f;
            colors.fadeDuration =
                MaterialEditorTheme.States.SelectableFadeDuration;
            toggle.colors = colors;

            MaterialEditorScrollSelectableStyles.SynchronizeCurrentState(toggle);

            if (text != null)
            {
                if (!toggle.interactable
                    && MaterialEditorTheme.Mode == MaterialEditorThemeMode.Dark)
                {
                    text.color = MaterialEditorTheme.Colors.DisabledText;
                }
                else if (selected)
                {
                    text.color = MaterialEditorTheme.Colors.SelectedText;
                }
                else if (MaterialEditorTheme.Mode == MaterialEditorThemeMode.Dark)
                {
                    text.color = MaterialEditorTheme.Colors.PrimaryText;
                }
                else
                {
                    text.color = MaterialEditorPanelTextStyles.ResolveTextColor(
                        MaterialEditorPanelTextStyles.GetAssignedTextRole(text));
                }
                MaterialEditorPanelTextStyles.RefreshTextRendering(text);
            }
        }
    }

    internal sealed class MaterialEditorDropdownCaptionFitter : MonoBehaviour
    {
        private const string Ellipsis = "...";

        [SerializeField] private Dropdown _dropdown;
        // Runtime listeners are not copied with the serialized dropdown reference.
        private bool _listenerBound;
        private Text _caption;
        private string _lastFullText;
        private string _lastDisplayText;
        private float _lastWidth = -1f;

        internal static void Configure(Dropdown dropdown)
        {
            if (dropdown == null)
                return;

            var fitter =
                dropdown.GetComponent<MaterialEditorDropdownCaptionFitter>()
                ?? dropdown.gameObject
                    .AddComponent<MaterialEditorDropdownCaptionFitter>();
            fitter.Bind(dropdown);
            fitter.RefreshText(true);
        }

        internal static void Refresh(Dropdown dropdown)
        {
            if (dropdown == null)
                return;

            Configure(dropdown);
        }

        internal static void Refresh(
            Dropdown dropdown,
            string fullText)
        {
            if (dropdown == null)
                return;

            var fitter =
                dropdown.GetComponent<MaterialEditorDropdownCaptionFitter>()
                ?? dropdown.gameObject
                    .AddComponent<MaterialEditorDropdownCaptionFitter>();
            fitter.Bind(dropdown);
            fitter.RefreshText(true, fullText ?? string.Empty);
        }

        private void Bind(Dropdown dropdown)
        {
            if (!_listenerBound || _dropdown != dropdown)
            {
                if (_listenerBound && _dropdown != null)
                    _dropdown.onValueChanged.RemoveListener(HandleValueChanged);

                _dropdown = dropdown;
                _dropdown.onValueChanged.AddListener(HandleValueChanged);
                _listenerBound = true;
            }
            _caption = dropdown.captionText;
        }

        private void OnEnable()
        {
            if (_dropdown != null)
                Bind(_dropdown);
            RefreshText(true);
        }

        private void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled)
                RefreshText(false);
        }

        private void OnDestroy()
        {
            if (_listenerBound && _dropdown != null)
                _dropdown.onValueChanged.RemoveListener(HandleValueChanged);
        }

        private void HandleValueChanged(int value)
        {
            RefreshText(true);
        }

        private void RefreshText(
            bool force,
            string explicitFullText = null)
        {
            if (_dropdown == null || _caption == null)
                return;

            var width = _caption.rectTransform.rect.width;
            if (width <= 0f)
                return;

            var fullText = explicitFullText ?? ResolveFullText();
            if (!force
                && string.Equals(
                    fullText,
                    _lastFullText,
                    StringComparison.Ordinal)
                && Mathf.Approximately(width, _lastWidth))
                return;

            var displayText = FitText(fullText, width);
            _lastFullText = fullText;
            _lastDisplayText = displayText;
            _lastWidth = width;
            if (!string.Equals(
                    _caption.text,
                    displayText,
                    StringComparison.Ordinal))
            {
                _caption.text = displayText;
            }
            MaterialEditorPanelTextStyles.RefreshTextRendering(_caption);
        }

        private string ResolveFullText()
        {
            var value = _dropdown.value;
            if (value >= 0 && value < _dropdown.options.Count)
            {
                var option = _dropdown.options[value];
                if (option != null)
                    return option.text ?? string.Empty;
            }

            var current = _caption.text ?? string.Empty;
            return string.Equals(
                       current,
                       _lastDisplayText,
                       StringComparison.Ordinal)
                   && _lastFullText != null
                ? _lastFullText
                : current;
        }

        private string FitText(string fullText, float availableWidth)
        {
            fullText = fullText ?? string.Empty;
            var fontSize = MaterialEditorLayout.DropdownFontSize;
            if (MaterialEditorTextFitting.MeasurePreferredWidth(
                    _caption,
                    fullText,
                    fontSize) <= availableWidth)
                return fullText;

            if (MaterialEditorTextFitting.MeasurePreferredWidth(
                    _caption,
                    Ellipsis,
                    fontSize) > availableWidth)
                return string.Empty;

            var low = 0;
            var high = fullText.Length;
            while (low < high)
            {
                var middle = (low + high + 1) / 2;
                var safeMiddle = GetSafeSubstringLength(fullText, middle);
                var candidate =
                    fullText.Substring(0, safeMiddle).TrimEnd() + Ellipsis;
                if (MaterialEditorTextFitting.MeasurePreferredWidth(
                        _caption,
                        candidate,
                        fontSize) <= availableWidth)
                    low = middle;
                else
                    high = middle - 1;
            }

            var safeLength = GetSafeSubstringLength(fullText, low);
            return fullText.Substring(0, safeLength).TrimEnd() + Ellipsis;
        }

        private static int GetSafeSubstringLength(string value, int length)
        {
            var safeLength = Mathf.Clamp(length, 0, value.Length);
            if (safeLength > 0
                && safeLength < value.Length
                && char.IsHighSurrogate(value[safeLength - 1])
                && char.IsLowSurrogate(value[safeLength]))
            {
                safeLength--;
            }
            return safeLength;
        }
    }

    // Unity clones Dropdown.Template into a separate runtime hierarchy. The
    // clone can restore DefaultControls colors, so every dropdown reapplies the
    // shared popup style after Unity has finished creating its option items.
    internal sealed class MaterialEditorDropdownPopupStyle : MonoBehaviour
    {
        private static int _openPopupCount;

        [SerializeField] private Dropdown _dropdown;
        private bool _started;
        private bool _registeredOpen;

        internal static bool AnyPopupOpen => _openPopupCount > 0;

        internal void Configure(Dropdown dropdown)
        {
            _dropdown = dropdown;
            if (_started && isActiveAndEnabled)
                Apply();
        }

        private void OnEnable()
        {
            RegisterRuntimePopup();
            if (_started)
                Apply();
        }

        private void Start()
        {
            _started = true;
            RegisterRuntimePopup();
            Apply();
        }

        private void OnDisable()
        {
            UnregisterRuntimePopup();
        }

        private void OnDestroy()
        {
            UnregisterRuntimePopup();
        }

        private void RegisterRuntimePopup()
        {
            // uGUI renames the cloned template root before activating it. The
            // original disabled template remains named "Template" and is never
            // counted, while every Material Editor dropdown clone is tracked.
            if (_registeredOpen || gameObject.name != "Dropdown List")
                return;
            _registeredOpen = true;
            _openPopupCount++;
        }

        private void UnregisterRuntimePopup()
        {
            if (!_registeredOpen)
                return;
            _registeredOpen = false;
            if (_openPopupCount > 0)
                _openPopupCount--;
        }

        internal void ReapplyTheme()
        {
            Apply();
        }

        private void Apply()
        {
            MaterialEditorStyles.ApplyDropdownPopup(
                _dropdown,
                transform,
                null,
                null);
            FitRuntimePopupWidth();
        }

        private void FitRuntimePopupWidth()
        {
            if (gameObject.name != "Dropdown List" || _dropdown == null)
                return;

            var popupRect = transform as RectTransform;
            var dropdownRect = _dropdown.transform as RectTransform;
            var measurementText = _dropdown.itemText ?? _dropdown.captionText;
            if (popupRect == null || measurementText == null)
                return;

            Canvas.ForceUpdateCanvases();

            var preferredTextWidth = 0f;
            var options = _dropdown.options;
            for (var i = 0; i < options.Count; i++)
            {
                var option = options[i];
                if (option == null)
                    continue;
                preferredTextWidth = Mathf.Max(
                    preferredTextWidth,
                    MaterialEditorTextFitting.MeasurePreferredWidth(
                        measurementText,
                        option.text,
                        MaterialEditorLayout.DropdownFontSize));
            }

            var currentWidth = dropdownRect != null
                               && dropdownRect.rect.width > 0f
                ? dropdownRect.rect.width
                : popupRect.rect.width;
            var desiredWidth = Mathf.Max(
                currentWidth,
                preferredTextWidth
                + MaterialEditorTheme.Metrics.DropdownPopupHorizontalPadding);
            desiredWidth = Mathf.Min(
                desiredWidth,
                MaterialEditorTheme.Metrics.DropdownPopupMaximumWidth);

            var canvas = GetComponentInParent<Canvas>();
            var canvasRect = canvas != null
                ? canvas.rootCanvas.transform as RectTransform
                : null;
            if (canvasRect != null)
            {
                var availableWidth = canvasRect.rect.width
                                     - MaterialEditorTheme.Metrics
                                         .DropdownPopupScreenMargin * 2f;
                if (availableWidth <= 0f)
                    return;
                desiredWidth = Mathf.Min(desiredWidth, availableWidth);
            }

            popupRect.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                desiredWidth);
            LayoutRebuilder.ForceRebuildLayoutImmediate(popupRect);
            Canvas.ForceUpdateCanvases();

            if (canvasRect != null)
            {
                AlignRightEdgeToDropdown(
                    popupRect,
                    dropdownRect,
                    canvasRect);
                ClampHorizontallyToCanvas(popupRect, canvasRect);
            }

            RefreshAdaptiveItemText();
        }

        private static void ClampHorizontallyToCanvas(
            RectTransform popupRect,
            RectTransform canvasRect)
        {
            var bounds = GetHorizontalBoundsInCanvas(popupRect, canvasRect);
            var minimum = canvasRect.rect.xMin
                          + MaterialEditorTheme.Metrics.DropdownPopupScreenMargin;
            var maximum = canvasRect.rect.xMax
                          - MaterialEditorTheme.Metrics.DropdownPopupScreenMargin;
            var shift = bounds.x < minimum
                ? minimum - bounds.x
                : bounds.y > maximum
                    ? maximum - bounds.y
                    : 0f;
            if (Mathf.Approximately(shift, 0f) || popupRect.parent == null)
                return;

            MoveHorizontallyInCanvasUnits(popupRect, canvasRect, shift);
        }

        private static void AlignRightEdgeToDropdown(
            RectTransform popupRect,
            RectTransform dropdownRect,
            RectTransform canvasRect)
        {
            if (dropdownRect == null || popupRect.parent == null)
                return;

            var dropdownBounds =
                GetHorizontalBoundsInCanvas(dropdownRect, canvasRect);
            var popupBounds =
                GetHorizontalBoundsInCanvas(popupRect, canvasRect);
            var shift = dropdownBounds.y - popupBounds.y;
            if (Mathf.Approximately(shift, 0f))
                return;

            MoveHorizontallyInCanvasUnits(popupRect, canvasRect, shift);
            Canvas.ForceUpdateCanvases();
        }

        private void RefreshAdaptiveItemText()
        {
            var fitters =
                GetComponentsInChildren<MaterialEditorAdaptiveTextFitter>(true);
            for (var i = 0; i < fitters.Length; i++)
                fitters[i].RefreshNow();

            var popupRect = transform as RectTransform;
            if (popupRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(popupRect);
        }

        private static void MoveHorizontallyInCanvasUnits(
            RectTransform popupRect,
            RectTransform canvasRect,
            float shift)
        {
            if (popupRect == null
                || canvasRect == null
                || popupRect.parent == null)
                return;

            var worldShift = canvasRect.TransformVector(new Vector3(shift, 0f, 0f));
            var parentShift = popupRect.parent.InverseTransformVector(worldShift);
            popupRect.anchoredPosition +=
                new Vector2(parentShift.x, parentShift.y);
        }

        private static Vector2 GetHorizontalBoundsInCanvas(
            RectTransform rectTransform,
            RectTransform canvasRect)
        {
            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            var minimum = float.PositiveInfinity;
            var maximum = float.NegativeInfinity;
            for (var i = 0; i < corners.Length; i++)
            {
                var canvasPoint = canvasRect.InverseTransformPoint(corners[i]);
                minimum = Mathf.Min(minimum, canvasPoint.x);
                maximum = Mathf.Max(maximum, canvasPoint.x);
            }

            return new Vector2(minimum, maximum);
        }
    }

    // Dropdown item templates are cloned by uGUI. Each clone owns exactly one
    // guarded listener and releases it as soon as the popup item is disabled.
    internal sealed class MaterialEditorDropdownItemStyle : MonoBehaviour
    {
        [SerializeField] private Toggle _toggle;
        [SerializeField] private Text _text;
        private bool _listening;

        internal void Configure(Toggle toggle, Text text)
        {
            if (_listening && !ReferenceEquals(_toggle, toggle))
                Unbind();
            _toggle = toggle;
            _text = text;
            if (isActiveAndEnabled)
                Bind();
            Refresh();
        }

        private void OnEnable()
        {
            Bind();
            Refresh();
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void Bind()
        {
            if (_listening || _toggle == null)
                return;
            _toggle.onValueChanged.AddListener(OnValueChanged);
            _listening = true;
        }

        private void Unbind()
        {
            if (!_listening)
                return;
            if (_toggle != null)
                _toggle.onValueChanged.RemoveListener(OnValueChanged);
            _listening = false;
        }

        private void OnValueChanged(bool selected)
        {
            MaterialEditorStyles.ApplyDropdownItemState(_toggle, _text, selected);
        }

        internal void ReapplyTheme()
        {
            Refresh();
        }

        private void Refresh()
        {
            if (_toggle != null)
                MaterialEditorStyles.ApplyDropdownItemState(
                    _toggle,
                    _text,
                    _toggle.isOn);
        }
    }
}
