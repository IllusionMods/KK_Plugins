using System;
using System.Collections.Generic;
using UnityEngine;

namespace MaterialEditorAPI
{
    internal static class MaterialEditorSectionKeys
    {
        internal static string Material(GameObject gameObject, UnityEngine.Material material)
        {
            return GetInstanceId(gameObject) + ":" + GetInstanceId(material);
        }

        internal static string Shader(
            GameObject gameObject,
            UnityEngine.Material material,
            string shaderName)
        {
            return Material(gameObject, material) + ":" + (shaderName ?? string.Empty);
        }

        internal static string Category(
            GameObject gameObject,
            UnityEngine.Material material,
            string shaderName,
            string source,
            string categoryName)
        {
            return Shader(gameObject, material, shaderName)
                   + ":" + (source ?? string.Empty)
                   + ":" + (categoryName ?? string.Empty);
        }

        private static int GetInstanceId(UnityEngine.Object value)
        {
            return value == null ? 0 : value.GetInstanceID();
        }
    }

    internal sealed class MaterialEditorPresentation
    {
        internal readonly List<RowModel> Rows = new List<RowModel>();
        internal readonly List<MaterialSectionPresentation> MaterialSections =
            new List<MaterialSectionPresentation>();

        internal MaterialSectionPresentation FindSectionAtRow(int rowIndex)
        {
            MaterialSectionPresentation result = null;
            foreach (var section in MaterialSections)
            {
                if (rowIndex < section.MaterialRowIndex)
                    break;
                result = section;
                if (rowIndex <= section.EndRowIndex)
                    break;
            }
            return result;
        }

        internal CategoryNavigationTarget FindCategory(
            string sectionId,
            string categoryId)
        {
            foreach (var section in MaterialSections)
            {
                if (section.Id != sectionId)
                    continue;
                foreach (var category in section.Categories)
                    if (category.Id == categoryId)
                        return category;
            }
            return null;
        }
    }

    internal sealed class MaterialSectionPresentation
    {
        private readonly Action _ensureParentsExpanded;

        internal MaterialSectionPresentation(
            string id,
            string materialName,
            string shaderName,
            int materialRowIndex,
            Action ensureParentsExpanded)
        {
            Id = id;
            MaterialName = materialName ?? string.Empty;
            ShaderName = shaderName ?? string.Empty;
            MaterialRowIndex = materialRowIndex;
            EndRowIndex = materialRowIndex;
            _ensureParentsExpanded = ensureParentsExpanded;
        }

        internal string Id { get; }
        internal string MaterialName { get; }
        internal string ShaderName { get; }
        internal int MaterialRowIndex { get; }
        internal int EndRowIndex { get; set; }
        internal readonly List<CategoryNavigationTarget> Categories =
            new List<CategoryNavigationTarget>();

        internal CategoryNavigationTarget AddCategory(
            string name,
            int rowIndex,
            string stateId,
            Func<bool> isCollapsed,
            Action<bool> setCollapsed,
            string tooltipText)
        {
            CategoryNavigationTarget target = null;
            foreach (var existing in Categories)
            {
                if (existing.Name == name)
                {
                    target = existing;
                    break;
                }
            }

            if (target == null)
            {
                target = new CategoryNavigationTarget(
                    Id + ":" + (name ?? string.Empty),
                    Id,
                    name,
                    rowIndex,
                    _ensureParentsExpanded,
                    tooltipText);
                Categories.Add(target);
            }
            else
            {
                target.RecordRowIndex(rowIndex);
                if (string.IsNullOrEmpty(target.TooltipText))
                    target.TooltipText = tooltipText;
            }

            target.AddCollapseState(stateId, isCollapsed, setCollapsed);
            return target;
        }

        internal bool AllCategoriesCollapsed
        {
            get
            {
                if (Categories.Count == 0)
                    return false;
                foreach (var category in Categories)
                    if (!category.Collapsed)
                        return false;
                return true;
            }
        }

        internal void SetAllCategoriesCollapsed(bool collapsed)
        {
            foreach (var category in Categories)
                category.SetCollapsed(collapsed);
        }

        internal CategoryNavigationTarget FindCategoryAtRow(int rowIndex)
        {
            CategoryNavigationTarget result = null;
            foreach (var category in Categories)
            {
                if (category.RowIndex < 0)
                    continue;
                if (category.RowIndex > rowIndex)
                    break;
                result = category;
            }
            return result ?? (Categories.Count == 0 ? null : Categories[0]);
        }
    }

    internal sealed class CategoryNavigationTarget
    {
        private readonly List<CategoryCollapseState> _collapseStates =
            new List<CategoryCollapseState>();
        private readonly Action _ensureParentsExpanded;

        internal CategoryNavigationTarget(
            string id,
            string sectionId,
            string name,
            int rowIndex,
            Action ensureParentsExpanded,
            string tooltipText)
        {
            Id = id;
            SectionId = sectionId;
            Name = name ?? string.Empty;
            RowIndex = rowIndex;
            _ensureParentsExpanded = ensureParentsExpanded;
            TooltipText = tooltipText;
        }

        internal string Id { get; }
        internal string SectionId { get; }
        internal string Name { get; }
        internal int RowIndex { get; private set; }
        internal string TooltipText { get; set; }

        internal bool Collapsed
        {
            get
            {
                if (_collapseStates.Count == 0)
                    return false;
                foreach (var state in _collapseStates)
                    if (!state.IsCollapsed())
                        return false;
                return true;
            }
        }

        internal void RecordRowIndex(int rowIndex)
        {
            if (rowIndex < 0)
                return;
            if (RowIndex < 0 || rowIndex < RowIndex)
                RowIndex = rowIndex;
        }

        internal void AddCollapseState(
            string id,
            Func<bool> isCollapsed,
            Action<bool> setCollapsed)
        {
            foreach (var state in _collapseStates)
                if (state.Id == id)
                    return;
            _collapseStates.Add(new CategoryCollapseState(id, isCollapsed, setCollapsed));
        }

        internal void EnsureParentsExpanded()
        {
            _ensureParentsExpanded?.Invoke();
        }

        internal void SetCollapsed(bool collapsed)
        {
            foreach (var state in _collapseStates)
                state.SetCollapsed(collapsed);
        }

        private sealed class CategoryCollapseState
        {
            internal CategoryCollapseState(
                string id,
                Func<bool> isCollapsed,
                Action<bool> setCollapsed)
            {
                Id = id;
                IsCollapsed = isCollapsed;
                SetCollapsed = setCollapsed;
            }

            internal string Id { get; }
            internal Func<bool> IsCollapsed { get; }
            internal Action<bool> SetCollapsed { get; }
        }
    }
}
