using System;
using System.Collections.Generic;
using UILib;
using UnityEngine;
using UnityEngine.UI;
using static MaterialEditorAPI.MaterialEditorPluginBase;

namespace MaterialEditorAPI
{
    internal class SelectListPanel
    {
        private readonly string _name;
        private readonly string _title;
        private readonly string _emptyEntriesText;
        private readonly bool _selectionChrome;
        private readonly Action<bool> _expandedChanged;
        private readonly Text _titleText;
        private readonly InputField _filterInputField;
        private readonly ScrollRect _scrollRect;
        private readonly Text _emptyStateText;
        private readonly Button _collapseButton;
        private readonly Text _collapseLabel;
        private readonly Dictionary<string, Entry> _listItems;
        private MaterialEditorFilterPattern _filterPattern;
        private int _visibleEntryCount;
        private bool _expanded = true;

        public SelectListPanel(Transform parent, string name, string title)
            : this(parent, name, title, null, false, null)
        {
        }

        internal SelectListPanel(
            Transform parent,
            string name,
            string title,
            string emptyEntriesText,
            bool selectionChrome,
            Action<bool> expandedChanged)
        {
            _listItems = new Dictionary<string, Entry>();
            _filterPattern = MaterialEditorFilter.Prepare(string.Empty);
            _name = name;
            _title = title;
            _emptyEntriesText = emptyEntriesText;
            _selectionChrome = selectionChrome;
            _expandedChanged = expandedChanged;

            Panel = MaterialEditorControlFactory.CreatePanel(
                name + "Panel",
                parent,
                MaterialEditorPanelRole.RightPanel);

            _titleText = MaterialEditorControlFactory.CreateText(
                name + "Title",
                Panel.transform,
                title,
                selectionChrome
                    ? MaterialEditorTextRole.Title
                    : MaterialEditorTextRole.SecondaryChrome);

            _filterInputField = MaterialEditorControlFactory.CreateInputField(
                name + "Filter",
                Panel.transform,
                "Filter");
            _filterInputField.text = string.Empty;

            _scrollRect = MaterialEditorControlFactory.CreateScrollView(
                name,
                Panel.transform);
            _scrollRect.gameObject.AddComponent<Mask>();
            var listLayout =
                _scrollRect.content.gameObject.AddComponent<VerticalLayoutGroup>();
            listLayout.childControlWidth = true;
            listLayout.childForceExpandWidth = true;
            listLayout.childControlHeight = true;
            listLayout.childForceExpandHeight = false;
            _scrollRect.content.gameObject.AddComponent<ContentSizeFitter>()
                .verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _scrollRect.verticalScrollbar.GetComponent<RectTransform>().offsetMin =
                new Vector2(MaterialEditorUI.ScrollOffsetX, 0f);
            _scrollRect.viewport.offsetMax =
                new Vector2(MaterialEditorUI.ScrollOffsetX, 0f);
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;
            MaterialEditorScrollStyleState.Assign(_scrollRect, false).SideList = true;
            MaterialEditorStyles.ApplyScrollView(_scrollRect);

            if (selectionChrome)
            {
                _emptyStateText = MaterialEditorControlFactory.CreateText(
                    name + "EmptyState",
                    _scrollRect.viewport,
                    string.Empty,
                    MaterialEditorTextRole.Label);
                _emptyStateText.transform.SetRect(0f, 0f, 1f, 1f);
                MaterialEditorTextFitting.ApplyAdaptiveSingleLine(
                    _emptyStateText,
                    MaterialEditorTheme.Typography.PrimaryFontSize);
                _emptyStateText.alignment = TextAnchor.MiddleCenter;
                _emptyStateText.raycastTarget = false;

                ConfigureSelectionChrome();
                _collapseButton = MaterialEditorControlFactory.CreateButton(
                    name + "Collapse",
                    Panel.transform,
                    FoldGlyphs.Expanded);
                _collapseButton.transform.SetRect(
                    0f, 1f, 0f, 1f,
                    MaterialEditorTheme.Spacing.SelectionPanelContentInset,
                    -MaterialEditorTheme.Metrics.SelectionPanelHeaderHeight,
                    MaterialEditorTheme.Metrics.SelectionPanelHeaderHeight,
                    0f);
                _collapseLabel =
                    _collapseButton.GetComponentInChildren<Text>();
                _collapseButton.onClick.AddListener(
                    () => SetExpanded(!_expanded));
                ApplyExpandedVisual();
            }
            else
            {
                ConfigureLegacyChrome();
            }

            _filterInputField.onValueChanged.AddListener(FilterList);
            MaterialEditorStyles.ApplyTypography(Panel.gameObject);
            if (_emptyStateText != null)
                _emptyStateText.alignment = TextAnchor.MiddleCenter;
            UpdateHeader();
            UpdateEmptyState();
        }

        public Image Panel { get; }
        internal bool Expanded => _expanded;
        internal int EntryCount => _listItems.Count;
        internal int VisibleEntryCount => _visibleEntryCount;

        public void AddEntry(string name, Action<bool> onValueChanged)
        {
            AddEntry(name, false, onValueChanged);
        }

        internal void AddEntry(
            string name,
            bool selected,
            Action<bool> onValueChanged)
        {
            if (_listItems.ContainsKey(name))
                return;

            var contentList =
                MaterialEditorControlFactory.CreateStencilMaskPanel(
                    _name + "Entry",
                    _scrollRect.content.transform);
            var contentLayout = contentList.gameObject.AddComponent<LayoutElement>();
            contentLayout.minHeight = MaterialEditorUI.PanelHeight;
            contentLayout.preferredHeight = MaterialEditorUI.PanelHeight;

            var itemPanel = MaterialEditorControlFactory.CreatePanel(
                _name + "EntryPanel",
                contentList.transform,
                MaterialEditorPanelRole.TransparentRow);
            itemPanel.gameObject.AddComponent<CanvasGroup>();
            var itemLayout =
                itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
            itemLayout.padding = new RectOffset(
                MaterialEditorTheme.Spacing.SelectionEntryPadding,
                MaterialEditorTheme.Spacing.SelectionEntryPadding,
                MaterialEditorTheme.Spacing.SelectionEntryPadding,
                MaterialEditorTheme.Spacing.SelectionEntryPadding);
            itemLayout.childControlWidth = true;
            itemLayout.childForceExpandWidth = true;
            itemLayout.childControlHeight = true;
            itemLayout.childForceExpandHeight = true;

            var toggle = MaterialEditorControlFactory.CreateToggle(
                _name + "Toggle",
                itemPanel.transform,
                name);
            var toggleLayout = toggle.gameObject.AddComponent<LayoutElement>();
            toggleLayout.minWidth = 0f;
            toggleLayout.preferredWidth = 0f;
            toggleLayout.flexibleWidth = 1f;
            var toggleGraphic =
                toggle.gameObject.GetComponentInChildren<CanvasRenderer>(true);
            if (toggleGraphic != null)
            {
                toggleGraphic.transform.SetRect(
                    0f,
                    1f,
                    0f,
                    1f,
                    MaterialEditorTheme.Spacing.SelectionToggleInset,
                    -MaterialEditorTheme.Metrics.SelectionToggleSize,
                    MaterialEditorTheme.Metrics.SelectionToggleSize,
                    -MaterialEditorTheme.Spacing.SelectionToggleInset);
            }

            var label = toggle.GetComponentInChildren<Text>(true);
            ConfigureEntryLabel(label);

            var rowButton = itemPanel.gameObject.AddComponent<Button>();
            rowButton.targetGraphic = itemPanel;
            MaterialEditorStyles.ApplySelectionListRowButton(rowButton);

            var entry = new Entry(
                name,
                contentList,
                toggle,
                label,
                rowButton);
            MaterialEditorStyles.ApplyRow(contentList.gameObject);
            toggle.Set(selected, false);
            ApplySelectedState(entry, selected);
            toggle.onValueChanged.AddListener(
                value =>
                {
                    ApplySelectedState(entry, value);
                    onValueChanged(value);
                });
            rowButton.onClick.AddListener(
                () => toggle.isOn = !toggle.isOn);
            TooltipBinding.Bind(rowButton.gameObject, null, name);

            _listItems[name] = entry;
            contentList.gameObject.SetActive(_filterPattern.Matches(name));
            if (contentList.gameObject.activeSelf)
                _visibleEntryCount++;
            UpdateHeader();
            UpdateEmptyState();
        }

        public void ClearList()
        {
            if (!PersistFilter.Value)
            {
                _filterInputField.Set(string.Empty);
                _filterPattern = MaterialEditorFilter.Prepare(string.Empty);
            }
            ReleaseEntries();
        }

        internal void ReleaseEntries()
        {
            foreach (var entry in _listItems.Values)
            {
                entry.Toggle.onValueChanged.RemoveAllListeners();
                entry.RowButton.onClick.RemoveAllListeners();
                TooltipBinding.Bind(entry.RowButton.gameObject, null, null);
                entry.Root.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(entry.Root.gameObject);
            }
            _listItems.Clear();
            _visibleEntryCount = 0;
            UpdateHeader();
            UpdateEmptyState();
        }

        public void ToggleVisibility(bool visible)
        {
            Panel.gameObject.SetActive(visible);
        }

        internal void SetExpanded(bool expanded)
        {
            if (!_selectionChrome || _expanded == expanded)
                return;

            _expanded = expanded;
            ApplyExpandedVisual();
            UpdateEmptyState();
            _expandedChanged?.Invoke(_expanded);
        }

        internal void ApplyTheme()
        {
            foreach (var entry in _listItems.Values)
                ApplySelectedState(entry, entry.Toggle.isOn);
        }


        private void ConfigureSelectionChrome()
        {
            _titleText.alignment = TextAnchor.MiddleLeft;
            _titleText.fontStyle = FontStyle.Normal;
            MaterialEditorTextFitting.ApplyAdaptiveSingleLine(
                _titleText,
                MaterialEditorTheme.Typography.PrimaryFontSize);
            _titleText.transform.SetRect(
                0f,
                1f,
                1f,
                1f,
                MaterialEditorTheme.Metrics.SelectionPanelHeaderHeight
                + MaterialEditorTheme.Spacing.Control,
                -MaterialEditorTheme.Metrics.SelectionPanelHeaderHeight,
                -MaterialEditorTheme.Spacing.SelectionPanelContentInset,
                0f);
            TooltipBinding.Bind(_titleText.gameObject, null, _title);

            _filterInputField.transform.SetRect(
                0f,
                1f,
                1f,
                1f,
                MaterialEditorTheme.Spacing.SelectionPanelContentInset,
                -MaterialEditorTheme.Metrics.SelectionPanelHeaderHeight
                - MaterialEditorTheme.Metrics.SelectionPanelFilterHeight,
                -MaterialEditorTheme.Spacing.SelectionPanelContentInset,
                -MaterialEditorTheme.Metrics.SelectionPanelHeaderHeight);

            _scrollRect.transform.SetRect(
                0f,
                0f,
                1f,
                1f,
                MaterialEditorTheme.Spacing.SelectionPanelContentInset,
                MaterialEditorTheme.Spacing.SelectionPanelContentInset,
                -MaterialEditorTheme.Spacing.SelectionPanelContentInset,
                -MaterialEditorTheme.Metrics.SelectionPanelHeaderHeight
                - MaterialEditorTheme.Metrics.SelectionPanelFilterHeight);
        }

        private void ConfigureLegacyChrome()
        {
            _titleText.transform.SetRect(
                0f,
                1f,
                MaterialEditorTheme.Metrics.SelectionPanelTitleFraction,
                1f,
                MaterialEditorTheme.Spacing.SelectionPanelTitleInset,
                -MaterialEditorUI.HeaderSize,
                -MaterialEditorTheme.Spacing.SelectionPanelContentInset,
                -MaterialEditorTheme.Spacing.SelectionPanelContentInset);

            _filterInputField.transform.SetRect(
                MaterialEditorTheme.Metrics.SelectionPanelTitleFraction,
                1f,
                1f,
                1f,
                MaterialEditorTheme.Spacing.SelectionPanelContentInset,
                -MaterialEditorUI.HeaderSize,
                -MaterialEditorTheme.Spacing.SelectionPanelContentInset,
                -MaterialEditorTheme.Spacing.SelectionPanelContentInset);

            _scrollRect.transform.SetRect(
                0f,
                0f,
                1f,
                1f,
                MaterialEditorTheme.Spacing.SelectionPanelContentInset,
                MaterialEditorTheme.Spacing.SelectionPanelContentInset,
                -MaterialEditorTheme.Spacing.SelectionPanelContentInset,
                -MaterialEditorUI.HeaderSize);
        }

        private void ApplyExpandedVisual()
        {
            _filterInputField.gameObject.SetActive(_expanded);
            _scrollRect.gameObject.SetActive(_expanded);
            if (_collapseLabel != null)
            {
                _collapseLabel.text = _expanded
                    ? FoldGlyphs.Expanded
                    : FoldGlyphs.Collapsed;
            }
            if (_collapseButton != null)
            {
                TooltipBinding.Bind(
                    _collapseButton.gameObject,
                    null,
                    (_expanded ? "Collapse " : "Expand ") + _title);
            }
        }


        private void FilterList(string filter)
        {
            _filterPattern = MaterialEditorFilter.Prepare(filter);
            var visibleEntryCount = 0;
            foreach (var entry in _listItems.Values)
            {
                var visible = _filterPattern.Matches(entry.Name);
                entry.Root.gameObject.SetActive(visible);
                if (visible)
                    visibleEntryCount++;
            }
            _visibleEntryCount = visibleEntryCount;
            UpdateEmptyState();
        }

        private void UpdateHeader()
        {
            if (_selectionChrome)
                _titleText.text = _title;
        }

        private void UpdateEmptyState()
        {
            if (_emptyStateText == null)
                return;

            var text = MaterialEditorEmptyState.ForSelectionList(
                _listItems.Count,
                _visibleEntryCount,
                _emptyEntriesText);
            var visible = _expanded && !string.IsNullOrEmpty(text);
            if (_emptyStateText.text != (text ?? string.Empty))
                _emptyStateText.text = text ?? string.Empty;
            if (_emptyStateText.gameObject.activeSelf != visible)
                _emptyStateText.gameObject.SetActive(visible);
        }

        private static void ConfigureEntryLabel(Text label)
        {
            if (label == null)
                return;

            MaterialEditorStyles.ApplyText(label, MaterialEditorTextRole.Input);
            MaterialEditorTextFitting.ApplyAdaptiveSingleLine(
                label,
                MaterialEditorTheme.Typography.PrimaryFontSize);
            label.alignment = TextAnchor.MiddleLeft;
        }

        private static void ApplySelectedState(Entry entry, bool selected)
        {
            MaterialEditorStyles.SetSelectionListSelected(
                entry.RowButton,
                selected);

            if (entry.Label != null)
            {
                entry.Label.fontStyle = selected
                    ? FontStyle.Bold
                    : FontStyle.Normal;
                entry.Label.SetVerticesDirty();
            }
        }
        private sealed class Entry
        {
            internal Entry(
                string name,
                Image root,
                Toggle toggle,
                Text label,
                Button rowButton)
            {
                Name = name;
                Root = root;
                Toggle = toggle;
                Label = label;
                RowButton = rowButton;
            }

            internal string Name { get; }
            internal Image Root { get; }
            internal Toggle Toggle { get; }
            internal Text Label { get; }
            internal Button RowButton { get; }
        }
    }
}
