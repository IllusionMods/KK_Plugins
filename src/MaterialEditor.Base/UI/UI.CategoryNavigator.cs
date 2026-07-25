using System;
using System.Collections.Generic;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal sealed class CategoryNavigatorView
    {
        private readonly Action<CategoryNavigationTarget> _navigate;
        private readonly Action<CategoryNavigationTarget> _toggle;
        private readonly List<Entry> _entries = new List<Entry>();
        private readonly Text _materialText;
        private readonly Text _shaderText;
        private readonly ScrollRect _scrollRect;
        private MaterialEditorPresentation _presentation;
        private string _sectionId;
        private int _viewportAnchor = -1;

        internal CategoryNavigatorView(
            Transform parent,
            Action<CategoryNavigationTarget> navigate,
            Action<CategoryNavigationTarget> toggle)
        {
            _navigate = navigate;
            _toggle = toggle;

            Panel = MaterialEditorControlFactory.CreatePanel(
                "CategoryNavigatorPanel",
                parent,
                MaterialEditorPanelRole.SidePanel);
            UIUtility.AddOutlineToObject(Panel.transform, Color.black);

            _materialText = MaterialEditorControlFactory.CreateText(
                "CategoryNavigatorMaterial",
                Panel.transform,
                string.Empty,
                MaterialEditorTextRole.CenteredLabel);
            _materialText.transform.SetRect(
                0f, 1f, 1f, 1f,
                MaterialEditorLayout.Margin,
                -MaterialEditorLayout.HeaderHeight,
                -MaterialEditorLayout.Margin,
                0f);

            var shaderHeader = MaterialEditorControlFactory.CreatePanel(
                "CategoryNavigatorShaderHeader",
                Panel.transform);
            shaderHeader.color = MaterialEditorStyles.NavigatorShaderHeaderColor;
            shaderHeader.transform.SetRect(
                0f, 1f, 1f, 1f,
                MaterialEditorLayout.Margin,
                -MaterialEditorLayout.HeaderHeight * 2f,
                -MaterialEditorLayout.Margin,
                -MaterialEditorLayout.HeaderHeight);

            _shaderText = MaterialEditorControlFactory.CreateText(
                "CategoryNavigatorShader",
                shaderHeader.transform,
                string.Empty,
                MaterialEditorTextRole.CenteredLabel);
            _shaderText.color = Color.black;
            _shaderText.transform.SetRect();

            _scrollRect = MaterialEditorControlFactory.CreateScrollView(
                "CategoryNavigatorScrollView",
                Panel.transform);
            _scrollRect.transform.SetRect(
                0f, 0f, 1f, 1f,
                MaterialEditorLayout.Margin,
                MaterialEditorLayout.Margin,
                -MaterialEditorLayout.Margin,
                -MaterialEditorLayout.HeaderHeight * 2f);
            _scrollRect.gameObject.AddComponent<Mask>();
            _scrollRect.verticalScrollbar.GetComponent<RectTransform>().offsetMin =
                new Vector2(MaterialEditorLayout.ScrollbarOffset, 0f);
            _scrollRect.viewport.offsetMax =
                new Vector2(MaterialEditorLayout.ScrollbarOffset, 0f);
            var layout = _scrollRect.content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 1f;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            _scrollRect.content.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;

            ApplySettings();
            Panel.gameObject.SetActive(false);
        }

        internal Image Panel { get; }

        internal void ApplySettings()
        {
            Panel.transform.SetRect(
                0f, 0f, 0f, 1f,
                -MaterialEditorLayout.CategoryNavigatorWidth - MaterialEditorLayout.Margin,
                0f,
                -MaterialEditorLayout.Margin,
                0f);
        }

        internal void SetPresentation(MaterialEditorPresentation presentation)
        {
            _presentation = presentation;
            SetViewportAnchor(_viewportAnchor, true);
        }

        internal void SetViewportAnchor(int rowIndex)
        {
            SetViewportAnchor(rowIndex, false);
        }

        private void SetViewportAnchor(int rowIndex, bool forceRebuild)
        {
            _viewportAnchor = rowIndex;
            var section = _presentation?.FindSectionAtRow(rowIndex);
            if (section == null || section.Categories.Count == 0)
            {
                _sectionId = null;
                Panel.gameObject.SetActive(false);
                return;
            }

            Panel.gameObject.SetActive(true);
            if (forceRebuild || section.Id != _sectionId)
                Rebuild(section);
            UpdateHighlight(section.FindCategoryAtRow(rowIndex));
        }

        private void Rebuild(MaterialSectionPresentation section)
        {
            _sectionId = section.Id;
            _materialText.text = section.MaterialName;
            _shaderText.text = section.ShaderName;

            foreach (var entry in _entries)
            {
                entry.Root.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(entry.Root.gameObject);
            }
            _entries.Clear();

            foreach (var target in section.Categories)
                _entries.Add(CreateEntry(target));

            LayoutRebuilder.ForceRebuildLayoutImmediate(_scrollRect.content);
        }

        private Entry CreateEntry(CategoryNavigationTarget target)
        {
            var root = MaterialEditorControlFactory.CreatePanel(
                "CategoryNavigationEntry",
                _scrollRect.content,
                MaterialEditorPanelRole.CategoryRow);
            var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(1, 1, 1, 1);
            layout.spacing = 2f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = true;
            var rootLayout = root.gameObject.AddComponent<LayoutElement>();
            rootLayout.minHeight = MaterialEditorLayout.RowHeight;
            rootLayout.preferredHeight = MaterialEditorLayout.RowHeight;

            var collapse = MaterialEditorControlFactory.CreateButton(
                "CategoryNavigationCollapse",
                root.transform,
                target.Collapsed ? "+" : "-");
            var collapseLayout = collapse.gameObject.AddComponent<LayoutElement>();
            collapseLayout.minWidth = MaterialEditorLayout.SmallButtonWidth;
            collapseLayout.preferredWidth = MaterialEditorLayout.SmallButtonWidth;
            collapseLayout.flexibleWidth = 0f;
            TooltipManager.AddTooltip(
                collapse.gameObject,
                "Expand or collapse this property category");
            collapse.onClick.AddListener(() => _toggle(target));

            var navigate = MaterialEditorControlFactory.CreateButton(
                "CategoryNavigationButton",
                root.transform,
                target.Name);
            var navigateLayout = navigate.gameObject.AddComponent<LayoutElement>();
            navigateLayout.minWidth = 0f;
            navigateLayout.preferredWidth = 0f;
            navigateLayout.flexibleWidth = 1f;
            TooltipManager.AddTooltip(
                navigate.gameObject,
                string.IsNullOrEmpty(target.TooltipText)
                    ? "Jump to this property category"
                    : target.TooltipText);
            navigate.onClick.AddListener(() => _navigate(target));

            return new Entry(root, target);
        }

        private void UpdateHighlight(CategoryNavigationTarget active)
        {
            foreach (var entry in _entries)
            {
                entry.Root.color = active != null && entry.Target.Id == active.Id
                    ? MaterialEditorStyles.MaterialColor
                    : MaterialEditorStyles.CategoryColor;
            }
        }

        private sealed class Entry
        {
            internal Entry(Image root, CategoryNavigationTarget target)
            {
                Root = root;
                Target = target;
            }

            internal Image Root { get; }
            internal CategoryNavigationTarget Target { get; }
        }
    }
}
