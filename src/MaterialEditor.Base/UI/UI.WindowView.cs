using System;
using UILib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static MaterialEditorAPI.MaterialEditorPluginBase;

namespace MaterialEditorAPI
{
    internal sealed class MaterialEditorWindowView
    {
        internal Canvas Window { get; private set; }
        internal Image MainPanel { get; private set; }
        internal Image HeaderPanel { get; private set; }
        internal Image ModePanel { get; private set; }
        internal Text HeaderTitle { get; private set; }
        internal ScrollRect ScrollableUI { get; private set; }
        internal InputField FilterInputField { get; private set; }
        internal Button CollapseAllCategoriesButton { get; private set; }
        internal Button CollapseAllSectionsButton { get; private set; }
        internal Transform HeaderContextSlot => _topBar.HeaderContextSlot;

        private MaterialEditorTopBarView _topBar;
        private MaterialEditorThemeRepaintCoordinator _themeRepaint;
        private Text _emptyStateText;
        private bool _categoriesVisible;
        private bool _selectionPanelsVisible;
        private bool _renameListVisible;
        private bool _panelStateInitialized;
        private MaterialEditorResponsiveLayout _responsiveLayout;
        private Vector2 _responsiveBaseAnchoredPosition;
        private bool _hasResponsiveBasePosition;
        private bool _applyingSettings;
        private MovableWindow _movableWindow;

        internal SelectListPanel RendererList { get; private set; }
        internal SelectListPanel MaterialList { get; private set; }
        internal SelectListPanel RenameList { get; private set; }
        internal InputField RenameField { get; private set; }
        internal Button RenameButton { get; private set; }
        internal Text RenameMaterial { get; private set; }
        internal CategoryNavigatorView CategoryNavigator { get; private set; }
        internal VirtualList VirtualList { get; private set; }

        internal MaterialEditorWindowView(
            Transform owner,
            string filter,
            Action<string> filterChanged,
            Action close,
            Action toggleCategoriesPanel,
            Action toggleSelectionPanels,
            Action toggleAllCategories,
            Action toggleAllSections,
            Action<CategoryNavigationTarget> navigateToCategory,
            Action<CategoryNavigationTarget> toggleCategory)
        {
            Build(
                owner, filter, filterChanged, close,
                toggleCategoriesPanel, toggleSelectionPanels,
                toggleAllCategories, toggleAllSections,
                navigateToCategory, toggleCategory);
        }

        internal void PrepareForDisplay(string filter)
        {
            Window.gameObject.SetActive(true);
            ApplySettings();
            _topBar.PrepareForDisplay(filter);
            // Startup does not raise a theme configuration event. Apply the current
            // theme after activation, then let the coordinator settle render caches.
            ApplyTheme();
        }

        internal void ApplyTheme()
        {
            if (Window == null)
                return;

            MaterialEditorStyles.ReapplyTheme(Window.gameObject);
            RendererList?.ApplyTheme();
            MaterialList?.ApplyTheme();
            RenameList?.ApplyTheme();
            _topBar?.RefreshThemeButton();
            _themeRepaint?.RequestRepaint();
        }

        internal void ApplySettings()
        {
            if (_applyingSettings)
                return;

            _applyingSettings = true;
            try
            {
                _responsiveLayout = CalculateResponsiveLayout();
                if (Window != null)
                {
                    var scaler = Window.GetComponent<CanvasScaler>();
                    var referenceResolution = new Vector2(
                        MaterialEditorTheme.Metrics.CanvasReferenceWidth
                        / _responsiveLayout.UiScale,
                        MaterialEditorTheme.Metrics.CanvasReferenceHeight
                        / _responsiveLayout.UiScale);
                    if (scaler.referenceResolution != referenceResolution)
                        scaler.referenceResolution = referenceResolution;
                    _responsiveLayout = CalculateResponsiveLayout();
                }

                if (MainPanel != null)
                    SetMainRectWithMemory(
                        _responsiveLayout.MainLeftAnchor,
                        _responsiveLayout.MainBottomAnchor,
                        _responsiveLayout.MainRightAnchor,
                        _responsiveLayout.MainTopAnchor);

                ApplySelectionPanelLayout();

                if (RenameList != null)
                    RenameList.Panel.transform.SetRect(
                        1f,
                        0.5f,
                        1f,
                        1f,
                        MaterialEditorLayout.Margin,
                        MaterialEditorLayout.Margin / 2f,
                        MaterialEditorLayout.Margin
                        + _responsiveLayout.RightPanelWidth);

                if (CategoryNavigator != null)
                {
                    CategoryNavigator.ApplySettings(
                        _responsiveLayout.LeftPanelWidth);
                }

                VirtualList?.EnsureViewportCapacity(
                    _responsiveLayout.ViewportHeight);
            }
            finally
            {
                _applyingSettings = false;
            }
        }

        internal void SetMainRectWithMemory(float anchorLeft, float anchorBottom, float anchorRight, float anchorTop)
        {
            if (MainPanel == null)
                return;

            var rect = MainPanel.rectTransform;
            var positionMemory = rect.anchoredPosition;
            var dragOffset = _hasResponsiveBasePosition
                ? positionMemory - _responsiveBaseAnchoredPosition
                : Vector2.zero;
            MainPanel.transform.SetRect(anchorLeft, anchorBottom, anchorRight, anchorTop);
            _responsiveBaseAnchoredPosition = rect.anchoredPosition;
            _hasResponsiveBasePosition = true;
            if (Input.GetKey(KeyCode.LeftControl))
                return;

            if (_responsiveLayout != null)
            {
                var dragX = dragOffset.x;
                var dragY = dragOffset.y;
                _responsiveLayout.ClampDragOffset(
                    GetWindowDragMode(),
                    ref dragX,
                    ref dragY);
                dragOffset = new Vector2(dragX, dragY);
            }
            rect.anchoredPosition =
                _responsiveBaseAnchoredPosition + dragOffset;
        }

        internal void ClampResponsiveDragPosition()
        {
            if (_responsiveLayout == null
                || MainPanel == null
                || !_hasResponsiveBasePosition)
                return;

            var rect = MainPanel.rectTransform;
            var dragOffset =
                rect.anchoredPosition - _responsiveBaseAnchoredPosition;
            var dragX = dragOffset.x;
            var dragY = dragOffset.y;
            _responsiveLayout.ClampDragOffset(
                GetWindowDragMode(),
                ref dragX,
                ref dragY);
            rect.anchoredPosition = _responsiveBaseAnchoredPosition
                                    + new Vector2(dragX, dragY);
        }

        private static MaterialEditorWindowDragMode GetWindowDragMode()
        {
            if (WindowDragMode != null)
                return WindowDragMode.Value;
            return MaterialEditorWindowBoundsPolicy.FromLegacy(
                PreventDragout != null && PreventDragout.Value);
        }

        private void ClampResponsiveDrag(PointerEventData eventData)
        {
            ClampResponsiveDragPosition();
        }

        private void OnCanvasDimensionsChanged()
        {
            if (MainPanel != null)
                ApplySettings();
        }

        internal void SetPanelState(
            bool categoriesVisible,
            bool selectionPanelsVisible,
            bool renameListVisible)
        {
            if (_panelStateInitialized
                && _categoriesVisible == categoriesVisible
                && _selectionPanelsVisible == selectionPanelsVisible
                && _renameListVisible == renameListVisible)
                return;

            _panelStateInitialized = true;
            _categoriesVisible = categoriesVisible;
            _selectionPanelsVisible = selectionPanelsVisible;
            _renameListVisible = renameListVisible;
            CategoryNavigator?.SetVisible(categoriesVisible);
            UpdateRightPanelVisibility();
            ApplySettings();
        }

        internal void SetHeaderTitleHorizontalOffset(float offset)
        {
            _topBar.SetHeaderTitleHorizontalOffset(offset);
        }

        internal void SetHeaderContextControlVisible(bool visible)
        {
            _topBar.SetHeaderContextControlVisible(visible);
        }

        private void Build(
            Transform owner,
            string filter,
            Action<string> filterChanged,
            Action close,
            Action toggleCategoriesPanel,
            Action toggleSelectionPanels,
            Action toggleAllCategories,
            Action toggleAllSections,
            Action<CategoryNavigationTarget> navigateToCategory,
            Action<CategoryNavigationTarget> toggleCategory)
        {
            Window = MaterialEditorControlFactory.CreateNewUISystem("MaterialEditorCanvas");
            _responsiveLayout = CalculateResponsiveLayout();
            Window.GetComponent<CanvasScaler>().referenceResolution = new Vector2(
                MaterialEditorTheme.Metrics.CanvasReferenceWidth
                / _responsiveLayout.UiScale,
                MaterialEditorTheme.Metrics.CanvasReferenceHeight
                / _responsiveLayout.UiScale);
            Window.gameObject.transform.SetParent(owner);
            Window.sortingOrder = 1000;
            Window.gameObject
                .AddComponent<MaterialEditorResponsiveCanvasWatcher>()
                .Initialize(OnCanvasDimensionsChanged);
            _themeRepaint = Window.gameObject
                .AddComponent<MaterialEditorThemeRepaintCoordinator>();

            MainPanel = MaterialEditorControlFactory.CreatePanel(
                "Panel",
                Window.transform,
                MaterialEditorPanelRole.CenterPanel);
            MainPanel.transform.SetRect(
                _responsiveLayout.MainLeftAnchor,
                _responsiveLayout.MainBottomAnchor,
                _responsiveLayout.MainRightAnchor,
                _responsiveLayout.MainTopAnchor);
            UIUtility.AddOutlineToObject(
                MainPanel.transform,
                MaterialEditorTheme.Colors.Outline);

            TooltipManager.Init(Window.transform);

            _topBar = new MaterialEditorTopBarView(
                MainPanel.transform,
                filter,
                filterChanged,
                close,
                toggleCategoriesPanel,
                toggleSelectionPanels,
                toggleAllCategories,
                toggleAllSections);
            HeaderPanel = _topBar.HeaderPanel;
            ModePanel = _topBar.ModePanel;
            HeaderTitle = _topBar.HeaderTitle;
            FilterInputField = _topBar.FilterInputField;
            CollapseAllCategoriesButton =
                _topBar.CollapseAllCategoriesButton;
            CollapseAllSectionsButton =
                _topBar.CollapseAllSectionsButton;
            _movableWindow = UIUtility.MakeObjectDraggable(
                HeaderPanel.rectTransform,
                MainPanel.rectTransform,
                false);
            _movableWindow.OnDragEvent += ClampResponsiveDrag;

            ScrollableUI = MaterialEditorControlFactory.CreateScrollView("MaterialEditorWindow", MainPanel.transform);
            ScrollableUI.transform.SetRect(
                0f,
                0f,
                1f,
                1f,
                MaterialEditorLayout.Margin,
                MaterialEditorLayout.Margin,
                -MaterialEditorLayout.Margin,
                -MaterialEditorTheme.Metrics.TopBarHeight
                - MaterialEditorLayout.Margin / 2f);
            ScrollableUI.gameObject.AddComponent<Mask>();
            ScrollableUI.content.gameObject.AddComponent<VerticalLayoutGroup>();
            ScrollableUI.content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ScrollableUI.verticalScrollbar.GetComponent<RectTransform>().offsetMin = new Vector2(MaterialEditorLayout.ScrollbarOffset, 0f);
            ScrollableUI.viewport.offsetMax = new Vector2(MaterialEditorLayout.ScrollbarOffset, 0f);
            ScrollableUI.movementType = ScrollRect.MovementType.Clamped;
            MaterialEditorStyles.ApplyScrollView(ScrollableUI);

            _emptyStateText = MaterialEditorControlFactory.CreateText(
                "MaterialEditorEmptyState",
                ScrollableUI.viewport,
                string.Empty,
                MaterialEditorTextRole.Label);
            _emptyStateText.transform.SetRect(0f, 0f, 1f, 1f);
            MaterialEditorTextFitting.ApplyAdaptiveSingleLine(
                _emptyStateText,
                MaterialEditorTheme.Typography.PrimaryFontSize);
            _emptyStateText.alignment = TextAnchor.MiddleCenter;
            _emptyStateText.raycastTarget = false;
            _emptyStateText.gameObject.SetActive(false);

            var template = RowViewFactory.CreateTemplate(ScrollableUI.content.transform);
            VirtualList = ScrollableUI.gameObject.AddComponent<VirtualList>();
            VirtualList.ScrollRect = ScrollableUI;
            VirtualList.EntryTemplate = template;
            VirtualList.Initialize();

            CategoryNavigator = new CategoryNavigatorView(
                MainPanel.transform,
                _responsiveLayout.LeftPanelWidth,
                navigateToCategory,
                toggleCategory);
            VirtualList.ViewportAnchorIndexChanged += rowIndex =>
            {
                var wasVisible = CategoryNavigator.Visible;
                CategoryNavigator.SetViewportAnchor(rowIndex);
                if (wasVisible != CategoryNavigator.Visible)
                    ApplySettings();
            };

            BuildSelectionPanels();
            BuildRenamePanel();
            ApplySettings();
        }

        internal void SetPresentation(
            MaterialEditorPresentation presentation,
            bool deferCategoryAnchor = false)
        {
            var categoriesWereVisible = CategoryNavigator.Visible;
            CategoryNavigator.SetPresentation(
                presentation,
                deferCategoryAnchor);
            _topBar.SetPresentation(presentation);
            if (categoriesWereVisible != CategoryNavigator.Visible)
                ApplySettings();
            SetEmptyState(
                presentation == null
                    ? null
                    : MaterialEditorEmptyState.ForPresentation(
                        presentation.Rows.Count,
                        presentation.HasActiveFilter));
        }

        internal void RefreshSectionCollapseState(
            MaterialEditorPresentation presentation)
        {
            _topBar.RefreshSectionCollapseState(presentation);
        }

        internal void ReleasePresentation()
        {
            var categoriesWereVisible = CategoryNavigator.Visible;
            CategoryNavigator.ReleasePresentation();
            _topBar.ReleasePresentation();
            if (categoriesWereVisible != CategoryNavigator.Visible)
                ApplySettings();
            SetEmptyState(null);
        }

        private void SetEmptyState(string text)
        {
            if (_emptyStateText == null)
                return;

            var visible = !string.IsNullOrEmpty(text);
            if (_emptyStateText.text != (text ?? string.Empty))
                _emptyStateText.text = text ?? string.Empty;
            if (_emptyStateText.gameObject.activeSelf != visible)
                _emptyStateText.gameObject.SetActive(visible);
        }

        private void BuildSelectionPanels()
        {
            RendererList = new SelectListPanel(
                MainPanel.transform,
                "RendererList",
                "Renderers",
                "No renderers",
                true,
                expanded => ApplySelectionPanelLayout());
            RendererList.ToggleVisibility(false);

            MaterialList = new SelectListPanel(
                MainPanel.transform,
                "MaterialList",
                "Materials",
                "No materials",
                true,
                expanded => ApplySelectionPanelLayout());
            MaterialList.ToggleVisibility(false);
        }

        private void BuildRenamePanel()
        {
            RenameList = new SelectListPanel(MainPanel.transform, "MaterialRenameList", "Mat. Renderers");
            RenameList.ToggleVisibility(false);

            RenameField = MaterialEditorControlFactory.CreateInputField("MaterialEditorRenameField", RenameList.Panel.transform, "");
            RenameField.transform.SetRect(0f, 0f, 1f, 0f, 0f, -(MaterialEditorLayout.RowHeight + MaterialEditorLayout.Margin / 2f), 0f, -(MaterialEditorLayout.Margin / 2f));

            RenameButton = MaterialEditorControlFactory.CreateButton("MaterialEditorRenameButton", RenameList.Panel.transform, "Rename");
            RenameButton.transform.SetRect(0f, 0f, 1f, 0f, 0f, -(2f * MaterialEditorLayout.RowHeight + MaterialEditorLayout.Margin), 0f, -(MaterialEditorLayout.RowHeight + MaterialEditorLayout.Margin));

            RenameList.Panel.transform.GetChild(0).SetRect(0f, 1f, 0.4f, 1f, 5f, -40f, -2f, -27.5f);
            RenameList.Panel.transform.GetChild(1).SetRect(0.4f, 1f, 1f, 1f, 2f, -42.5f, -2f, -25f);
            RenameList.Panel.transform.GetChild(2).SetRect(0f, 0f, 1f, 1f, 2f, 2f, -2f, -42.5f);

            RenameMaterial = UnityEngine.Object.Instantiate(RenameList.Panel.transform.GetChild(0), RenameList.Panel.transform).GetComponent<Text>();
            RenameMaterial.gameObject.name = "MaterialEditorRenameMaterial";
            RenameMaterial.transform.SetRect(0f, 1f, 1f, 1f, 5f, -20f, -2f, -5f);
            MaterialEditorStyles.ApplyText(
                RenameMaterial,
                MaterialEditorTextRole.Chrome);
            MaterialEditorStyles.ApplyTypography(RenameList.Panel.gameObject);
        }

        private void ApplySelectionPanelLayout()
        {
            if (RendererList == null || MaterialList == null)
                return;

            var left = MaterialEditorLayout.Margin;
            var right = MaterialEditorLayout.Margin
                        + (_responsiveLayout == null
                            ? MaterialEditorTheme.Metrics.SidePanelDefaultWidth
                            : _responsiveLayout.RightPanelWidth);
            var gap = MaterialEditorLayout.Margin;
            var headerHeight =
                MaterialEditorTheme.Metrics.SelectionPanelHeaderHeight;

            if (RendererList.Expanded && MaterialList.Expanded)
            {
                RendererList.Panel.transform.SetRect(
                    1f, 0.5f, 1f, 1f,
                    left, gap / 2f, right, 0f);
                MaterialList.Panel.transform.SetRect(
                    1f, 0f, 1f, 0.5f,
                    left, 0f, right, -gap / 2f);
                return;
            }

            if (!RendererList.Expanded && MaterialList.Expanded)
            {
                RendererList.Panel.transform.SetRect(
                    1f, 1f, 1f, 1f,
                    left, -headerHeight, right, 0f);
                MaterialList.Panel.transform.SetRect(
                    1f, 0f, 1f, 1f,
                    left, 0f, right, -headerHeight);
                return;
            }

            if (RendererList.Expanded && !MaterialList.Expanded)
            {
                RendererList.Panel.transform.SetRect(
                    1f, 0f, 1f, 1f,
                    left, headerHeight, right, 0f);
                MaterialList.Panel.transform.SetRect(
                    1f, 0f, 1f, 0f,
                    left, 0f, right, headerHeight);
                return;
            }

            RendererList.Panel.transform.SetRect(
                1f, 1f, 1f, 1f,
                left, -headerHeight, right, 0f);
            MaterialList.Panel.transform.SetRect(
                1f, 1f, 1f, 1f,
                left, -2f * headerHeight, right, -headerHeight);
        }

        private void UpdateRightPanelVisibility()
        {
            if (RendererList == null || MaterialList == null || RenameList == null)
                return;

            var showSelectionPanels =
                _selectionPanelsVisible && !_renameListVisible;
            RendererList.ToggleVisibility(showSelectionPanels);
            MaterialList.ToggleVisibility(showSelectionPanels);
            RenameList.ToggleVisibility(_renameListVisible);
        }

        private MaterialEditorResponsiveLayout CalculateResponsiveLayout()
        {
            var leftState = CategoryNavigator != null
                            && CategoryNavigator.Visible
                ? MaterialEditorResponsiveSideState.Expanded
                : MaterialEditorResponsiveSideState.Hidden;
            var rightState =
                MaterialEditorResponsiveLayoutPolicy.GetRightSideState(
                    _selectionPanelsVisible && !_renameListVisible,
                    _renameListVisible);
            return MaterialEditorResponsiveLayoutPolicy.Calculate(
                UIScale.Value,
                UIWidth.Value,
                UIHeight.Value,
                UICategoriesWidth.Value,
                UIListWidth.Value,
                leftState,
                rightState,
                Window == null
                    ? float.NaN
                    : ((RectTransform)Window.transform).rect.width,
                Window == null
                    ? float.NaN
                    : ((RectTransform)Window.transform).rect.height);
        }
    }
}
