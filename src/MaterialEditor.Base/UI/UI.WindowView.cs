using System;
using UILib;
using UnityEngine;
using UnityEngine.UI;
using static MaterialEditorAPI.MaterialEditorPluginBase;

namespace MaterialEditorAPI
{
    internal sealed class MaterialEditorWindowView
    {
        internal Canvas Window { get; private set; }
        internal Image MainPanel { get; private set; }
        internal Image HeaderPanel { get; private set; }
        internal ScrollRect ScrollableUI { get; private set; }
        internal InputField FilterInputField { get; private set; }
        internal Button CategoryNavigatorButton { get; private set; }
        internal Button ViewListButton { get; private set; }

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
            Action<string> refresh,
            Action close,
            Action toggleSidePanels,
            Action<CategoryNavigationTarget> navigateToCategory,
            Action<CategoryNavigationTarget> toggleCategory)
        {
            Build(
                owner, filter, refresh, close, toggleSidePanels,
                navigateToCategory, toggleCategory);
        }

        internal void PrepareForDisplay(string filter)
        {
            Window.gameObject.SetActive(true);
            ApplySettings();
            FilterInputField.Set(filter);
        }

        internal void ApplySettings()
        {
            if (Window != null)
                Window.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f / UIScale.Value, 1080f / UIScale.Value);

            if (MainPanel != null)
                SetMainRectWithMemory(
                    GetDefaultMainLeftAnchor(),
                    0.05f,
                    GetDefaultMainRightAnchor(),
                    UIHeight.Value * UIScale.Value);

            if (RendererList != null)
                RendererList.Panel.transform.SetRect(1f, 0.5f, 1f, 1f, MaterialEditorLayout.Margin, MaterialEditorLayout.Margin / 2f, MaterialEditorLayout.Margin + UIListWidth.Value);

            if (MaterialList != null)
                MaterialList.Panel.transform.SetRect(1f, 0f, 1f, 0.5f, MaterialEditorLayout.Margin, 0f, MaterialEditorLayout.Margin + UIListWidth.Value, -MaterialEditorLayout.Margin);

            if (RenameList != null)
                RenameList.Panel.transform.SetRect(1f, 0.5f, 1f, 1f, MaterialEditorLayout.Margin, MaterialEditorLayout.Margin / 2f, MaterialEditorLayout.Margin + UIListWidth.Value);

            CategoryNavigator?.ApplySettings();
        }

        internal void SetMainRectWithMemory(float anchorLeft, float anchorBottom, float anchorRight, float anchorTop)
        {
            if (MainPanel == null)
                return;

            var positionMemory = MainPanel.transform.position;
            MainPanel.transform.SetRect(anchorLeft, anchorBottom, anchorRight, anchorTop);
            if (!Input.GetKey(KeyCode.LeftControl))
                MainPanel.transform.position = positionMemory;
        }

        internal void SetSelectionListsVisible(bool visible)
        {
            RendererList.ToggleVisibility(visible);
            MaterialList.ToggleVisibility(visible);
        }

        internal void SetRenameListVisible(bool visible)
        {
            RenameList.ToggleVisibility(visible);
        }

        internal void SetViewListGlyph(string glyph)
        {
            ViewListButton.GetComponentInChildren<Text>().text = glyph;
        }

        private void Build(
            Transform owner,
            string filter,
            Action<string> refresh,
            Action close,
            Action toggleSidePanels,
            Action<CategoryNavigationTarget> navigateToCategory,
            Action<CategoryNavigationTarget> toggleCategory)
        {
            Window = MaterialEditorControlFactory.CreateNewUISystem("MaterialEditorCanvas");
            Window.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f / UIScale.Value, 1080f / UIScale.Value);
            Window.gameObject.transform.SetParent(owner);
            Window.sortingOrder = 1000;

            MainPanel = MaterialEditorControlFactory.CreatePanel("Panel", Window.transform, MaterialEditorPanelRole.Main);
            MainPanel.transform.SetRect(
                GetDefaultMainLeftAnchor(),
                0.05f,
                GetDefaultMainRightAnchor(),
                UIHeight.Value * UIScale.Value);
            UIUtility.AddOutlineToObject(MainPanel.transform, Color.black);

            TooltipManager.Init(Window.transform);

            HeaderPanel = MaterialEditorControlFactory.CreatePanel("Draggable", MainPanel.transform, MaterialEditorPanelRole.Header);
            HeaderPanel.transform.SetRect(0f, 1f, 1f, 1f, 0f, -MaterialEditorLayout.HeaderHeight);
            UIUtility.MakeObjectDraggable(HeaderPanel.rectTransform, MainPanel.rectTransform, PreventDragout.Value);

            var nameText = MaterialEditorControlFactory.CreateText(
                "Nametext",
                HeaderPanel.transform,
                "Material Editor",
                MaterialEditorTextRole.Title);
            nameText.transform.SetRect();

            CategoryNavigatorButton = MaterialEditorControlFactory.CreateButton(
                "CategoryNavigatorButton",
                HeaderPanel.transform,
                ">");
            CategoryNavigatorButton.transform.SetRect(
                0f, 0f, 0f, 1f,
                1f, 1f, 20f, -1f);
            CategoryNavigatorButton.onClick.AddListener(ToggleCategoryNavigator);
            TooltipManager.AddTooltip(
                CategoryNavigatorButton.gameObject,
                "Show or hide the category navigator");

            FilterInputField = MaterialEditorControlFactory.CreateInputField("Filter", HeaderPanel.transform, "Filter");
            FilterInputField.text = filter;
            FilterInputField.transform.SetRect(
                0f, 0f, 0f, 1f,
                21f, 1f, 100f, -1f);
            FilterInputField.onValueChanged.AddListener(value => refresh(value));
            TooltipManager.AddTooltip(FilterInputField.gameObject, @"Filter visible items in the window.

- Searches for renderers, materials and projectors
- Searches starting with '_' will search for material properties
- Combine multiple statements using a comma (an entry just has to match any of the search terms)
- Use a '*' as a wildcard for any amount of characters (e.g. ""_pattern*1"" will find the ""PatternMask1"" property)
- Use a '?' as a wildcard for a single character");

            var persistSearch = MaterialEditorControlFactory.CreateToggle("PersistSearch", HeaderPanel.transform, "");
            persistSearch.transform.SetRect(0f, 1f, 1f, 0.5f, 100f, 0f, 0f, 10f);
            persistSearch.Set(PersistFilter.Value);
            persistSearch.gameObject.GetComponentInChildren<CanvasRenderer>(true).transform.SetRect(0f, 1f, 0f, 0f, 0f, -19f, 19f, -1f);
            persistSearch.onValueChanged.AddListener(value => PersistFilter.Value = value);
            TooltipManager.AddTooltip(persistSearch.gameObject, "Keeps the filter between instances of this window instead of resetting them");

            var persistSearchText = MaterialEditorControlFactory.CreateText(
                "PersistSearchText",
                HeaderPanel.transform,
                "Persist search",
                MaterialEditorTextRole.Label);
            persistSearchText.transform.SetRect(0f, 0.15f, 1f, 0.85f, 120f, 0f, 0f, 0f);

            var closeButton = MaterialEditorControlFactory.CreateButton("CloseButton", HeaderPanel.transform, "");
            closeButton.transform.SetRect(1f, 0f, 1f, 1f, -40f, 1f, -21f, -1f);
            closeButton.onClick.AddListener(() => close());
            CreateCloseGlyph(closeButton.transform);

            ViewListButton = MaterialEditorControlFactory.CreateButton("ViewListButton", HeaderPanel.transform, ">");
            ViewListButton.transform.SetRect(1f, 0f, 1f, 1f, -20f, 1f, -1f, -1f);
            ViewListButton.onClick.AddListener(() => toggleSidePanels());

            MaterialEditorStyles.ApplyTypography(HeaderPanel.gameObject);

            ScrollableUI = MaterialEditorControlFactory.CreateScrollView("MaterialEditorWindow", MainPanel.transform);
            ScrollableUI.transform.SetRect(
                0f,
                0f,
                1f,
                1f,
                MaterialEditorLayout.Margin,
                MaterialEditorLayout.Margin,
                -MaterialEditorLayout.Margin,
                -MaterialEditorLayout.HeaderHeight - MaterialEditorLayout.Margin / 2f);
            ScrollableUI.gameObject.AddComponent<Mask>();
            ScrollableUI.content.gameObject.AddComponent<VerticalLayoutGroup>();
            ScrollableUI.content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ScrollableUI.verticalScrollbar.GetComponent<RectTransform>().offsetMin = new Vector2(MaterialEditorLayout.ScrollbarOffset, 0f);
            ScrollableUI.viewport.offsetMax = new Vector2(MaterialEditorLayout.ScrollbarOffset, 0f);
            ScrollableUI.movementType = ScrollRect.MovementType.Clamped;
            MaterialEditorStyles.ApplyScrollView(ScrollableUI);

            var template = RowViewFactory.CreateTemplate(ScrollableUI.content.transform);
            VirtualList = ScrollableUI.gameObject.AddComponent<VirtualList>();
            VirtualList.ScrollRect = ScrollableUI;
            VirtualList.EntryTemplate = template;
            VirtualList.Initialize();

            CategoryNavigator = new CategoryNavigatorView(
                MainPanel.transform,
                navigateToCategory,
                toggleCategory);
            SetCategoryNavigatorGlyph(CategoryNavigator.Expanded);
            VirtualList.ViewportAnchorIndexChanged += CategoryNavigator.SetViewportAnchor;

            BuildSelectionPanels();
            BuildRenamePanel();
            ApplySettings();
        }

        internal void SetPresentation(MaterialEditorPresentation presentation)
        {
            CategoryNavigator.SetPresentation(presentation);
        }

        private void ToggleCategoryNavigator()
        {
            if (CategoryNavigator == null)
                return;

            SetCategoryNavigatorGlyph(CategoryNavigator.ToggleExpanded());
        }

        private void SetCategoryNavigatorGlyph(bool expanded)
        {
            CategoryNavigatorButton.GetComponentInChildren<Text>().text =
                expanded ? ">" : "<";
        }

        private void BuildSelectionPanels()
        {
            RendererList = new SelectListPanel(MainPanel.transform, "RendererList", "Renderers");
            RendererList.ToggleVisibility(false);

            MaterialList = new SelectListPanel(MainPanel.transform, "MaterialList", "Materials");
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
            MaterialEditorStyles.ApplyText(RenameMaterial);
            MaterialEditorStyles.ApplyTypography(RenameList.Panel.gameObject);
        }

        private static void CreateCloseGlyph(Transform parent)
        {
            var firstLine = MaterialEditorControlFactory.CreatePanel("x1", parent);
            firstLine.transform.SetRect(0f, 0f, 1f, 1f, 8f, 0f, -8f);
            firstLine.rectTransform.eulerAngles = new Vector3(0f, 0f, 45f);
            firstLine.color = Color.black;

            var secondLine = MaterialEditorControlFactory.CreatePanel("x2", parent);
            secondLine.transform.SetRect(0f, 0f, 1f, 1f, 8f, 0f, -8f);
            secondLine.rectTransform.eulerAngles = new Vector3(0f, 0f, -45f);
            secondLine.color = Color.black;
        }

        private static float GetDefaultMainLeftAnchor()
        {
            var canvasWidth = 1920f / UIScale.Value;
            var navigatorSpace =
                MaterialEditorLayout.CategoryNavigatorWidth
                + MaterialEditorLayout.Margin * 2f;
            return Mathf.Max(0.05f, navigatorSpace / canvasWidth);
        }

        private static float GetDefaultMainRightAnchor()
        {
            var left = GetDefaultMainLeftAnchor();
            return UIWidth.Value * UIScale.Value + left - 0.05f;
        }
    }
}
