using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;
using static MaterialEditorAPI.MaterialEditorPluginBase;

namespace MaterialEditorAPI
{
    /// <summary>
    /// Builds one material section, including its shader, projector, manifest,
    /// extension-property, and category-navigation rows.
    /// </summary>
    internal sealed class MaterialSectionPresenter
    {
        private readonly MaterialEditService _editService;
        private readonly MaterialEditorSessionState _session;
        private readonly MaterialEditorPresentationActions _actions;
        private readonly PropertyRowModelFactory _propertyRows;

        internal MaterialSectionPresenter(
            MaterialEditService editService,
            MaterialEditorSessionState session,
            MaterialEditorPresentationActions actions)
        {
            _editService = editService;
            _session = session;
            _actions = actions;
            _propertyRows = new PropertyRowModelFactory(editService, actions);
        }

        internal void AddRows(
            MaterialEditorPresentation presentation,
            GameObject gameObject,
            object data,
            string filter,
            IEnumerable<Renderer> allRenderers,
            IList<string> propertyFilter,
            Material material,
            Projector projector)
        {
            var rows = presentation.Rows;
            var materialName = material.NameFormatted();
            var shaderName = material.shader.NameFormatted();
            var materialKey = MaterialEditorSectionKeys.Material(gameObject, material);
            var shaderKey = MaterialEditorSectionKeys.Shader(gameObject, material, shaderName);
            var materialCollapsed = MaterialEditorSessionState.IsCollapsed(
                _session.CollapsedMaterialSections,
                materialKey);
            var shaderCollapsed = MaterialEditorSessionState.IsCollapsed(
                _session.CollapsedShaderSections,
                shaderKey);
            var section = new MaterialSectionPresentation(
                shaderKey,
                materialName,
                shaderName,
                rows.Count,
                () =>
                {
                    MaterialEditorSessionState.SetCollapsed(
                        _session.CollapsedMaterialSections, materialKey, false);
                    MaterialEditorSessionState.SetCollapsed(
                        _session.CollapsedShaderSections, shaderKey, false);
                });
            presentation.MaterialSections.Add(section);

            var materialItem = new MaterialRowModel()
            {
                GameObject = gameObject,
                Data = data,
                Material = material,
                Projector = projector,
                MaterialName = materialName,
                Collapsed = materialCollapsed,
                CollapsedOnChange = value =>
                {
                    MaterialEditorSessionState.SetCollapsed(
                        _session.CollapsedMaterialSections, materialKey, value);
                    _actions.Refresh(gameObject, data, filter);
                },
                Copy = () => _editService.MaterialCopyEdits(data, material, gameObject),
                Paste = () =>
                {
                    _editService.MaterialPasteEdits(data, material, gameObject);
                    _actions.Refresh(gameObject, data, filter);
                },
                Rename = () => _actions.ShowRename(gameObject, material, data)
            };
            if (projector == null)
            {
                materialItem.CopyOrRemove = () =>
                {
                    _editService.MaterialCopyRemove(data, material, gameObject);
                    _actions.Refresh(gameObject, data, filter);
                    _actions.RefreshMaterialSelection(gameObject, data, allRenderers);
                };
            }
            rows.Add(materialItem);

            ShaderRowModel shaderItem = null;
            if (!materialCollapsed && projector != null)
                AddProjectorRows(rows, gameObject, data, propertyFilter, projector);

            if (!materialCollapsed)
            {
                shaderItem = AddShaderRows(
                    rows, gameObject, data, filter, materialName, material,
                    projector, shaderName, shaderKey, shaderCollapsed);
            }

            AddPropertyRows(
                rows, section, gameObject, data, filter, propertyFilter,
                materialName, material, projector, shaderName,
                !materialCollapsed && !shaderCollapsed);

            if (shaderItem != null)
            {
                shaderItem.HasCategories = section.Categories.Count > 0;
                shaderItem.AllCategoriesCollapsed = section.AllCategoriesCollapsed;
                shaderItem.CategoriesCollapsedOnChange = value =>
                {
                    section.SetAllCategoriesCollapsed(value);
                    _actions.Refresh(gameObject, data, filter);
                };
            }
            section.EndRowIndex = Math.Max(section.MaterialRowIndex, rows.Count - 1);
        }

        private ShaderRowModel AddShaderRows(
            ICollection<RowModel> rows,
            GameObject gameObject,
            object data,
            string filter,
            string materialName,
            Material material,
            Projector projector,
            string shaderName,
            string shaderKey,
            bool collapsed)
        {
            var originalShaderName = _editService.GetMaterialShaderNameOriginal(data, material, gameObject);
            if (originalShaderName.IsNullOrEmpty())
                originalShaderName = shaderName;
            var shaderItem = new ShaderRowModel()
            {
                GameObject = gameObject,
                Data = data,
                Material = material,
                Projector = projector,
                ShaderName = shaderName,
                OriginalShaderName = originalShaderName,
                TooltipText = ShaderUiMetadataRegistry.GetShaderTooltip(shaderName),
                Collapsed = collapsed,
                CollapsedOnChange = value =>
                {
                    MaterialEditorSessionState.SetCollapsed(
                        _session.CollapsedShaderSections, shaderKey, value);
                    _actions.Refresh(gameObject, data, filter);
                },
                ShaderNameOnChange = value =>
                {
                    _editService.SetMaterialShaderName(data, material, value, gameObject);
                    _actions.RefreshDeferred(gameObject, data, filter);
                },
                ShaderNameOnReset = () =>
                {
                    _editService.RemoveMaterialShaderName(data, material, gameObject);
                    _actions.RefreshDeferred(gameObject, data, filter);
                },
                SelectInterpolable = () =>
                    _actions.SelectInterpolable(
                        gameObject,
                        RowModel.RowItemType.Shader,
                        materialName,
                        string.Empty,
                        string.Empty)
            };
            rows.Add(shaderItem);

            if (collapsed)
                return shaderItem;

            var originalRenderQueue =
                _editService.GetMaterialShaderRenderQueueOriginal(data, material, gameObject)
                ?? material.renderQueue;
            rows.Add(new ShaderRenderQueueRowModel()
            {
                GameObject = gameObject,
                Data = data,
                Material = material,
                Projector = projector,
                Value = material.renderQueue,
                OriginalValue = originalRenderQueue,
                ValueOnChange = value =>
                    _editService.SetMaterialShaderRenderQueue(data, material, value, gameObject),
                ValueOnReset = () =>
                    _editService.RemoveMaterialShaderRenderQueue(data, material, gameObject)
            });
            return shaderItem;
        }

        private void AddPropertyRows(
            ICollection<RowModel> rows,
            MaterialSectionPresentation section,
            GameObject gameObject,
            object data,
            string filter,
            IList<string> propertyFilter,
            string materialName,
            Material material,
            Projector projector,
            string shaderName,
            bool includeRows)
        {
            var categories = PropertyOrganizer.PropertyOrganization[
                XMLShaderProperties.ContainsKey(shaderName) ? shaderName : "default"];

            foreach (var category in categories)
            {
                var definitions = category.Value
                    .Where(property =>
                        property.Type == ShaderPropertyType.Keyword
                        || material.HasProperty($"_{property.Name}"))
                    .Where(property =>
                        !_actions.IsPropertyBlacklisted(materialName, property.Name))
                    .Where(property =>
                        propertyFilter.Count == 0
                        || propertyFilter.Any(word =>
                            MaterialEditorFilter.Matches(property.Name, word)))
                    .ToList();
                if (definitions.Count == 0)
                    continue;

                var namedCategory =
                    categories.Count > 1
                    || category.Key != PropertyOrganizer.UncategorizedName;
                var showCategory =
                    namedCategory
                    && propertyFilter.Count == 0;
                var categoryKey = MaterialEditorSectionKeys.Category(
                    gameObject, material, shaderName, "manifest", category.Key);
                var storedCollapsed = MaterialEditorSessionState.IsCollapsed(
                    _session.CollapsedPropertyCategories,
                    categoryKey);
                CategoryNavigationTarget navigationTarget = null;
                if (namedCategory)
                {
                    navigationTarget = section.AddCategory(
                        category.Key,
                        -1,
                        categoryKey,
                        () => MaterialEditorSessionState.IsCollapsed(
                            _session.CollapsedPropertyCategories, categoryKey),
                        value => MaterialEditorSessionState.SetCollapsed(
                            _session.CollapsedPropertyCategories, categoryKey, value),
                        ShaderUiMetadataRegistry.GetCategoryTooltip(
                            shaderName,
                            category.Key));
                }

                if (includeRows && showCategory)
                {
                    navigationTarget?.RecordRowIndex(rows.Count);
                    rows.Add(new PropertyCategoryRowModel(category.Key)
                    {
                        Collapsed = storedCollapsed,
                        TooltipText = ShaderUiMetadataRegistry.GetCategoryTooltip(
                            shaderName,
                            category.Key),
                        CollapsedOnChange = value =>
                        {
                            MaterialEditorSessionState.SetCollapsed(
                                _session.CollapsedPropertyCategories, categoryKey, value);
                            _actions.Refresh(gameObject, data, filter);
                        }
                    });
                }

                if (!includeRows || (showCategory && storedCollapsed))
                    continue;

                navigationTarget?.RecordRowIndex(rows.Count);
                foreach (var definition in definitions)
                {
                    var descriptor = new PropertyDescriptor(
                        gameObject,
                        data,
                        material,
                        projector,
                        materialName,
                        definition,
                        category.Key);
                    foreach (var row in _propertyRows.Create(descriptor))
                        rows.Add(row);
                }
            }

            AddExtensionPropertyRows(
                rows,
                section,
                gameObject,
                data,
                filter,
                propertyFilter,
                materialName,
                material,
                projector,
                shaderName,
                includeRows);
        }

        private void AddExtensionPropertyRows(
            ICollection<RowModel> rows,
            MaterialSectionPresentation section,
            GameObject gameObject,
            object data,
            string filter,
            IList<string> propertyFilter,
            string materialName,
            Material material,
            Projector projector,
            string shaderName,
            bool includeRows)
        {
            var target = MaterialEditorExtensionRegistry.CreateTargetContext(
                _editService,
                gameObject,
                data,
                null,
                material,
                projector);
            var context = new MaterialEditorPropertyContext(
                target,
                materialName,
                shaderName);
            var descriptors = MaterialEditorExtensionRegistry
                .GetPropertyDescriptors(context)
                .Where(descriptor =>
                    descriptor != null
                    && MaterialEditorExtensionRegistry.HasPropertyEditor(descriptor.EditorId)
                    && !_actions.IsPropertyBlacklisted(
                        materialName,
                        string.IsNullOrEmpty(descriptor.PropertyName)
                            ? descriptor.Id
                            : descriptor.PropertyName))
                .Where(descriptor =>
                    propertyFilter.Count == 0
                    || propertyFilter.Any(word =>
                        MaterialEditorFilter.Matches(descriptor.DisplayName, word)
                        || MaterialEditorFilter.Matches(descriptor.PropertyName, word)))
                .ToList();

            foreach (var category in descriptors.GroupBy(
                         descriptor => descriptor.Category ?? string.Empty))
            {
                var categoryName = category.Key;
                var namedCategory = !string.IsNullOrEmpty(categoryName);
                var categoryKey = MaterialEditorSectionKeys.Category(
                    gameObject, material, shaderName, "extension", categoryName);
                var categoryCollapsed = namedCategory
                    && MaterialEditorSessionState.IsCollapsed(
                        _session.CollapsedPropertyCategories, categoryKey);
                CategoryNavigationTarget navigationTarget = null;
                if (namedCategory)
                {
                    navigationTarget = section.AddCategory(
                        categoryName,
                        -1,
                        categoryKey,
                        () => MaterialEditorSessionState.IsCollapsed(
                            _session.CollapsedPropertyCategories, categoryKey),
                        value => MaterialEditorSessionState.SetCollapsed(
                            _session.CollapsedPropertyCategories, categoryKey, value),
                        ShaderUiMetadataRegistry.GetCategoryTooltip(
                            shaderName,
                            categoryName));
                }

                if (includeRows && namedCategory)
                {
                    navigationTarget?.RecordRowIndex(rows.Count);
                    rows.Add(new PropertyCategoryRowModel(categoryName)
                    {
                        Collapsed = categoryCollapsed,
                        TooltipText = ShaderUiMetadataRegistry.GetCategoryTooltip(
                            shaderName,
                            categoryName),
                        CollapsedOnChange = value =>
                        {
                            MaterialEditorSessionState.SetCollapsed(
                                _session.CollapsedPropertyCategories, categoryKey, value);
                            _actions.Refresh(gameObject, data, filter);
                        }
                    });
                }
                if (!includeRows || categoryCollapsed)
                    continue;

                navigationTarget?.RecordRowIndex(rows.Count);
                foreach (var descriptor in category
                             .OrderBy(item => item.Order)
                             .ThenBy(item => item.DisplayName))
                {
                    ShaderPropertyType builtInType;
                    if (TryGetBuiltInPropertyType(descriptor.EditorId, out builtInType))
                    {
                        var propertyName = string.IsNullOrEmpty(descriptor.PropertyName)
                            ? descriptor.Id
                            : descriptor.PropertyName;
                        if (builtInType != ShaderPropertyType.Keyword
                            && !material.HasProperty($"_{propertyName}"))
                            continue;

                        var internalDescriptor = new PropertyDescriptor(
                            gameObject,
                            data,
                            material,
                            projector,
                            materialName,
                            descriptor,
                            builtInType);
                        foreach (var row in _propertyRows.Create(internalDescriptor))
                            rows.Add(row);
                        continue;
                    }

                    foreach (var row in _propertyRows.CreateExtension(context, descriptor))
                        rows.Add(row);
                }
            }
        }

        private static bool TryGetBuiltInPropertyType(
            string editorId,
            out ShaderPropertyType type)
        {
            if (editorId == MaterialEditorPropertyEditorIds.Texture)
            {
                type = ShaderPropertyType.Texture;
                return true;
            }
            if (editorId == MaterialEditorPropertyEditorIds.Color)
            {
                type = ShaderPropertyType.Color;
                return true;
            }
            if (editorId == MaterialEditorPropertyEditorIds.Float)
            {
                type = ShaderPropertyType.Float;
                return true;
            }
            if (editorId == MaterialEditorPropertyEditorIds.Boolean)
            {
                type = ShaderPropertyType.Keyword;
                return true;
            }

            type = default(ShaderPropertyType);
            return false;
        }

        private void AddProjectorRows(
            ICollection<RowModel> rows,
            GameObject gameObject,
            object data,
            IList<string> propertyFilter,
            Projector projector)
        {
            foreach (var property in Enum.GetValues(typeof(ProjectorProperties)).Cast<ProjectorProperties>())
            {
                string name;
                float value;
                float maxValue;
                GetProjectorPresentation(projector, property, out name, out value, out maxValue);

                if (propertyFilter.Count > 0
                    && !propertyFilter.Any(filterWord => MaterialEditorFilter.Matches(name, filterWord)))
                    continue;

                var original =
                    _editService.GetProjectorPropertyValueOriginal(data, projector, property, gameObject)
                    ?? value;
                rows.Add(CreateFloatRow(
                    gameObject,
                    data,
                    null,
                    projector,
                    name,
                    value,
                    original,
                    0f,
                    maxValue,
                    () => _actions.SelectProjectorInterpolable(
                        gameObject,
                        property,
                        projector.NameFormatted()),
                    newValue =>
                        _editService.SetProjectorProperty(
                            data,
                            projector,
                            property,
                            newValue,
                            projector.gameObject),
                    () =>
                        _editService.RemoveProjectorProperty(
                            data,
                            projector,
                            property,
                            projector.gameObject)));
            }
        }

        private static FloatPropertyRowModel CreateFloatRow(
            GameObject gameObject,
            object data,
            Material material,
            Projector projector,
            string propertyName,
            float value,
            float original,
            float? minValue,
            float? maxValue,
            Action selectInterpolable,
            Action<float> changeValue,
            Action resetValue)
        {
            var item = new FloatPropertyRowModel(propertyName)
            {
                GameObject = gameObject,
                Data = data,
                Material = material,
                Projector = projector,
                PropertyName = propertyName,
                Value = value,
                OriginalValue = original,
                SelectInterpolable = selectInterpolable,
                ValueOnChange = changeValue,
                ValueOnReset = resetValue
            };
            if (minValue != null)
                item.SliderMinimum = minValue.Value;
            if (maxValue != null)
                item.SliderMaximum = maxValue.Value;
            return item;
        }

        private static void GetProjectorPresentation(
            Projector projector,
            ProjectorProperties property,
            out string name,
            out float value,
            out float maxValue)
        {
            name = string.Empty;
            value = 0f;
            maxValue = 100f;
            switch (property)
            {
                case ProjectorProperties.Enabled:
                    name = "Enabled";
                    value = Convert.ToSingle(projector.enabled);
                    maxValue = 1f;
                    break;
                case ProjectorProperties.NearClipPlane:
                    name = "Near Clip Plane";
                    value = projector.nearClipPlane;
                    maxValue = ProjectorNearClipPlaneMax.Value;
                    break;
                case ProjectorProperties.FarClipPlane:
                    name = "Far Clip Plane";
                    value = projector.farClipPlane;
                    maxValue = ProjectorFarClipPlaneMax.Value;
                    break;
                case ProjectorProperties.FieldOfView:
                    name = "Field Of View";
                    value = projector.fieldOfView;
                    maxValue = ProjectorFieldOfViewMax.Value;
                    break;
                case ProjectorProperties.AspectRatio:
                    name = "Aspect Ratio";
                    value = projector.aspectRatio;
                    maxValue = ProjectorAspectRatioMax.Value;
                    break;
                case ProjectorProperties.Orthographic:
                    name = "Orthographic";
                    value = Convert.ToSingle(projector.orthographic);
                    maxValue = 1f;
                    break;
                case ProjectorProperties.OrthographicSize:
                    name = "Orthographic Size";
                    value = projector.orthographicSize;
                    maxValue = ProjectorOrthographicSizeMax.Value;
                    break;
                case ProjectorProperties.IgnoreMapLayer:
                    name = "Ignore Map layer";
                    value = Convert.ToSingle(
                        projector.ignoreLayers == (projector.ignoreLayers | (1 << 11)));
                    maxValue = 1f;
                    break;
                case ProjectorProperties.IgnoreCharaLayer:
                    name = "Ignore Chara Layer";
                    value = Convert.ToSingle(
                        projector.ignoreLayers == (projector.ignoreLayers | (1 << 10)));
                    maxValue = 1f;
                    break;
            }
        }
    }
}
