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
        private readonly PropertyCategorySectionBuilder _categories;

        internal MaterialSectionPresenter(
            MaterialEditService editService,
            MaterialEditorSessionState session,
            MaterialEditorPresentationActions actions)
        {
            _editService = editService;
            _session = session;
            _actions = actions;
            _propertyRows = new PropertyRowModelFactory(editService, actions);
            _categories = new PropertyCategorySectionBuilder(session, actions);
        }

        internal void AddRows(MaterialSectionContext context)
        {
            var materialCollapsed = MaterialEditorSessionState.IsCollapsed(
                _session.CollapsedMaterialSections,
                context.MaterialKey);
            var shaderCollapsed = MaterialEditorSessionState.IsCollapsed(
                _session.CollapsedShaderSections,
                context.ShaderKey);
            var section = new MaterialSectionPresentation(
                context.ShaderKey,
                context.MaterialName,
                context.ShaderName,
                context.Rows.Count,
                () =>
                {
                    MaterialEditorSessionState.SetCollapsed(
                        _session.CollapsedMaterialSections, context.MaterialKey, false);
                    MaterialEditorSessionState.SetCollapsed(
                        _session.CollapsedShaderSections, context.ShaderKey, false);
                });
            context.Presentation.MaterialSections.Add(section);

            var materialItem = new MaterialRowModel()
            {
                GameObject = context.GameObject,
                Data = context.Data,
                Material = context.Material,
                Projector = context.Projector,
                MaterialName = context.MaterialName,
                Collapsed = materialCollapsed,
                CollapsedOnChange = value =>
                {
                    MaterialEditorSessionState.SetCollapsed(
                        _session.CollapsedMaterialSections, context.MaterialKey, value);
                    _actions.Refresh(context.GameObject, context.Data, context.Filter);
                },
                Copy = () => context.Edits.CopyMaterialEdits(context.Material),
                Paste = () =>
                {
                    context.Edits.PasteMaterialEdits(context.Material);
                    _actions.Refresh(context.GameObject, context.Data, context.Filter);
                },
                Rename = () => _actions.ShowRename(
                    context.GameObject,
                    context.Material,
                    context.Data)
            };
            if (context.Projector == null)
            {
                materialItem.CopyOrRemove = () =>
                {
                    context.Edits.CopyOrRemoveMaterial(context.Material);
                    _actions.Refresh(context.GameObject, context.Data, context.Filter);
                    _actions.RefreshMaterialSelection(
                        context.GameObject,
                        context.Data,
                        context.AllRenderers);
                };
            }
            context.Rows.Add(materialItem);

            ShaderRowModel shaderItem = null;
            if (!materialCollapsed && context.Projector != null)
                AddProjectorRows(context);

            if (!materialCollapsed)
                shaderItem = AddShaderRows(context, shaderCollapsed);

            AddPropertyRows(
                context,
                section,
                !materialCollapsed && !shaderCollapsed);

            if (shaderItem != null)
            {
                shaderItem.HasCategories = section.Categories.Count > 0;
                shaderItem.AllCategoriesCollapsed = section.AllCategoriesCollapsed;
                shaderItem.CategoriesCollapsedOnChange = value =>
                {
                    section.SetAllCategoriesCollapsed(value);
                    _actions.Refresh(context.GameObject, context.Data, context.Filter);
                };
            }
            section.EndRowIndex = Math.Max(
                section.MaterialRowIndex,
                context.Rows.Count - 1);
        }

        private ShaderRowModel AddShaderRows(
            MaterialSectionContext context,
            bool collapsed)
        {
            var originalShaderName = context.Edits.GetOriginalShader(
                context.Material);
            if (originalShaderName.IsNullOrEmpty())
                originalShaderName = context.ShaderName;
            var shaderItem = new ShaderRowModel()
            {
                GameObject = context.GameObject,
                Data = context.Data,
                Material = context.Material,
                Projector = context.Projector,
                ShaderName = context.ShaderName,
                OriginalShaderName = originalShaderName,
                TooltipText = ShaderUiMetadataRegistry.GetShaderTooltip(
                    context.ShaderName),
                Collapsed = collapsed,
                CollapsedOnChange = value =>
                {
                    MaterialEditorSessionState.SetCollapsed(
                        _session.CollapsedShaderSections,
                        context.ShaderKey,
                        value);
                    _actions.Refresh(context.GameObject, context.Data, context.Filter);
                },
                ShaderNameOnChange = value =>
                {
                    context.Edits.SetShader(
                        context.Material,
                        value);
                    _actions.RefreshDeferred(
                        context.GameObject,
                        context.Data,
                        context.Filter);
                },
                ShaderNameOnReset = () =>
                {
                    context.Edits.ResetShader(context.Material);
                    _actions.RefreshDeferred(
                        context.GameObject,
                        context.Data,
                        context.Filter);
                },
                SelectInterpolable = () =>
                    _actions.SelectInterpolable(
                        context.GameObject,
                        RowModel.RowItemType.Shader,
                        context.MaterialName,
                        string.Empty,
                        string.Empty)
            };
            context.Rows.Add(shaderItem);

            if (collapsed)
                return shaderItem;

            var originalRenderQueue =
                context.Edits.GetOriginalRenderQueue(context.Material)
                ?? context.Material.renderQueue;
            context.Rows.Add(new ShaderRenderQueueRowModel()
            {
                GameObject = context.GameObject,
                Data = context.Data,
                Material = context.Material,
                Projector = context.Projector,
                Value = context.Material.renderQueue,
                OriginalValue = originalRenderQueue,
                ValueOnChange = value =>
                    context.Edits.SetRenderQueue(
                        context.Material,
                        value),
                ValueOnReset = () =>
                    context.Edits.ResetRenderQueue(context.Material)
            });
            return shaderItem;
        }

        private void AddPropertyRows(
            MaterialSectionContext context,
            MaterialSectionPresentation section,
            bool includeRows)
        {
            var categories = PropertyOrganizer.PropertyOrganization[
                XMLShaderProperties.ContainsKey(context.ShaderName)
                    ? context.ShaderName
                    : "default"];

            foreach (var category in categories)
            {
                var definitions = category.Value
                    .Where(property =>
                        property.Type == ShaderPropertyType.Keyword
                        || context.Material.HasProperty($"_{property.Name}"))
                    .Where(property =>
                        !_actions.IsPropertyBlacklisted(
                            context.MaterialName,
                            property.Name))
                    .Where(property =>
                        context.PropertyFilter.Count == 0
                        || context.PropertyFilter.Any(word =>
                            MaterialEditorFilter.Matches(property.Name, word)))
                    .ToList();
                if (definitions.Count == 0)
                    continue;

                var namedCategory =
                    categories.Count > 1
                    || category.Key != PropertyOrganizer.UncategorizedName;
                var categorySection = _categories.Add(
                    context,
                    section,
                    "manifest",
                    category.Key,
                    namedCategory,
                    includeRows);
                if (!includeRows || categorySection.RowsCollapsed)
                    continue;

                categorySection.RecordFirstRow(context.Rows.Count);
                foreach (var definition in definitions)
                {
                    var descriptor = new PropertyDescriptor(
                        context.GameObject,
                        context.Data,
                        context.Material,
                        context.Projector,
                        context.MaterialName,
                        definition,
                        category.Key);
                    foreach (var row in _propertyRows.Create(descriptor))
                        context.Rows.Add(row);
                }
            }

            AddExtensionPropertyRows(context, section, includeRows);
        }

        private void AddExtensionPropertyRows(
            MaterialSectionContext sectionContext,
            MaterialSectionPresentation section,
            bool includeRows)
        {
            var target = MaterialEditorExtensionRegistry.CreateTargetContext(
                _editService,
                sectionContext.GameObject,
                sectionContext.Data,
                null,
                sectionContext.Material,
                sectionContext.Projector);
            var propertyContext = new MaterialEditorPropertyContext(
                target,
                sectionContext.MaterialName,
                sectionContext.ShaderName);
            var descriptors = MaterialEditorExtensionRegistry
                .GetPropertyDescriptors(propertyContext)
                .Where(descriptor =>
                    descriptor != null
                    && MaterialEditorExtensionRegistry.HasPropertyEditor(descriptor.EditorId)
                    && !_actions.IsPropertyBlacklisted(
                        sectionContext.MaterialName,
                        string.IsNullOrEmpty(descriptor.PropertyName)
                            ? descriptor.Id
                            : descriptor.PropertyName))
                .Where(descriptor =>
                    sectionContext.PropertyFilter.Count == 0
                    || sectionContext.PropertyFilter.Any(word =>
                        MaterialEditorFilter.Matches(descriptor.DisplayName, word)
                        || MaterialEditorFilter.Matches(descriptor.PropertyName, word)))
                .ToList();

            foreach (var category in descriptors.GroupBy(
                         descriptor => descriptor.Category ?? string.Empty))
            {
                var categoryName = category.Key;
                var namedCategory = !string.IsNullOrEmpty(categoryName);
                var categorySection = _categories.Add(
                    sectionContext,
                    section,
                    "extension",
                    categoryName,
                    namedCategory,
                    includeRows);
                if (!includeRows || categorySection.RowsCollapsed)
                    continue;

                categorySection.RecordFirstRow(sectionContext.Rows.Count);
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
                            && !sectionContext.Material.HasProperty($"_{propertyName}"))
                            continue;

                        var internalDescriptor = new PropertyDescriptor(
                            sectionContext.GameObject,
                            sectionContext.Data,
                            sectionContext.Material,
                            sectionContext.Projector,
                            sectionContext.MaterialName,
                            descriptor,
                            builtInType);
                        foreach (var row in _propertyRows.Create(internalDescriptor))
                            sectionContext.Rows.Add(row);
                        continue;
                    }

                    foreach (var row in _propertyRows.CreateExtension(
                                 propertyContext,
                                 descriptor))
                        sectionContext.Rows.Add(row);
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

        private void AddProjectorRows(MaterialSectionContext context)
        {
            foreach (var property in Enum.GetValues(typeof(ProjectorProperties)).Cast<ProjectorProperties>())
            {
                string name;
                float value;
                float maxValue;
                GetProjectorPresentation(
                    context.Projector,
                    property,
                    out name,
                    out value,
                    out maxValue);

                if (context.PropertyFilter.Count > 0
                    && !context.PropertyFilter.Any(
                        filterWord => MaterialEditorFilter.Matches(name, filterWord)))
                    continue;

                var original =
                    context.Edits.GetOriginalProjectorProperty(
                        context.Projector,
                        property)
                    ?? value;
                context.Rows.Add(CreateFloatRow(
                    context.GameObject,
                    context.Data,
                    null,
                    context.Projector,
                    name,
                    value,
                    original,
                    0f,
                    maxValue,
                    () => _actions.SelectProjectorInterpolable(
                        context.GameObject,
                        property,
                        context.Projector.NameFormatted()),
                    newValue =>
                        _editService.SetProjectorProperty(
                            context.Data,
                            context.Projector,
                            property,
                            newValue,
                            context.Projector.gameObject),
                    () =>
                        _editService.RemoveProjectorProperty(
                            context.Data,
                            context.Projector,
                            property,
                            context.Projector.gameObject)));
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
