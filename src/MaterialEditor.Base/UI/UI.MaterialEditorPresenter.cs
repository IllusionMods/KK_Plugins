using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;
using static MaterialEditorAPI.MaterialEditorPluginBase;

namespace MaterialEditorAPI
{
    internal delegate void ImportTextureAction(
        RowModel row,
        GameObject gameObject,
        object data,
        Material material,
        string propertyName);

    internal delegate void SelectInterpolableAction(
        GameObject gameObject,
        RowModel.RowItemType itemType,
        string materialName,
        string propertyName,
        string rendererName);

    internal delegate void EditColorAction(
        object data,
        Material material,
        string title,
        Color value,
        Action<Color> onChanged);

    internal static class MaterialEditorFilter
    {
        internal static bool Matches(string text, string filter)
        {
            string regex =
                "^.*"
                + Regex.Escape(filter).Replace("\\?", ".").Replace("\\*", ".*")
                + ".*$";
            return Regex.IsMatch(text, regex, RegexOptions.IgnoreCase);
        }
    }

    internal sealed class MaterialEditorPresentationActions
    {
        internal Action<GameObject, object, string> Refresh { get; set; }
        internal Action<GameObject, object, string> RefreshDeferred { get; set; }
        internal Action<GameObject, object, IEnumerable<Renderer>> RefreshMaterialSelection { get; set; }
        internal Action<GameObject, Material, object> ShowRename { get; set; }
        internal Action<Renderer> ExportUv { get; set; }
        internal Action<Renderer> RequestObjExport { get; set; }
        internal Action<Material, string> ExportTexture { get; set; }
        internal ImportTextureAction ImportTexture { get; set; }
        internal SelectInterpolableAction SelectInterpolable { get; set; }
        internal Action<GameObject, ProjectorProperties, string> SelectProjectorInterpolable { get; set; }
        internal EditColorAction EditColor { get; set; }
        internal Action<object, Material, string, Color> SetColorToPalette { get; set; }
        internal Func<string, string, bool> IsPropertyBlacklisted { get; set; }
    }

    internal sealed class MaterialEditorPresenter
    {
        private readonly MaterialEditService _editService;
        private readonly MaterialEditorSessionState _session;
        private readonly MaterialEditorPresentationActions _actions;
        private readonly PropertyRowModelFactory _propertyRows;

        internal MaterialEditorPresenter(
            MaterialEditService editService,
            MaterialEditorSessionState session,
            MaterialEditorPresentationActions actions)
        {
            _editService = editService;
            _session = session;
            _actions = actions;
            _propertyRows = new PropertyRowModelFactory(editService, actions);
        }

        internal List<RowModel> BuildRows(
            GameObject gameObject,
            object data,
            string filter,
            IEnumerable<Renderer> rendererSource,
            IEnumerable<Projector> projectorSource)
        {
            var allRenderers = rendererSource.ToList();
            var allProjectors = projectorSource.ToList();
            var rendererFilter = new List<string>();
            var propertyFilter = new List<string>();
            ParseFilter(filter, rendererFilter, propertyFilter);

            var renderers = SelectRenderers(allRenderers, rendererFilter);
            var projectors = SelectProjectors(allProjectors, rendererFilter);
            var materials = SelectMaterials(gameObject, allRenderers, renderers, rendererFilter);
            var rows = new List<RowModel>();

            foreach (var renderer in renderers)
                AddRendererRows(rows, gameObject, data, renderer);

            foreach (var material in materials.Values)
                AddMaterialRows(rows, gameObject, data, filter, allRenderers, propertyFilter, material, null);

            foreach (var projector in rendererFilter.Count == 0 ? allProjectors : projectors)
                AddMaterialRows(rows, gameObject, data, filter, allRenderers, propertyFilter, projector.material, projector);

            return rows;
        }

        private static void ParseFilter(
            string filter,
            ICollection<string> rendererFilter,
            ICollection<string> propertyFilter)
        {
            if (filter.IsNullOrEmpty())
                return;

            var parts = filter
                .Split(',')
                .Select(value => value.Trim())
                .Where(value => !string.IsNullOrEmpty(value))
                .ToList();

            foreach (var part in parts)
            {
                if (part.StartsWith("_"))
                {
                    var property = part.Trim('_');
                    if (!property.IsNullOrEmpty())
                        propertyFilter.Add(property);
                }
                else
                {
                    rendererFilter.Add(part);
                }
            }
        }

        private List<Renderer> SelectRenderers(
            IList<Renderer> allRenderers,
            IList<string> filter)
        {
            if (_session.SelectedRenderers.Count > 0)
                return new List<Renderer>(_session.SelectedRenderers);
            if (filter.Count == 0)
                return new List<Renderer>(allRenderers);

            var renderers = new List<Renderer>();
            foreach (var renderer in allRenderers)
            foreach (var filterWord in filter)
                if (MaterialEditorFilter.Matches(renderer.NameFormatted(), filterWord.Trim())
                    && !renderers.Contains(renderer))
                    renderers.Add(renderer);

            return renderers;
        }

        private List<Projector> SelectProjectors(
            IEnumerable<Projector> allProjectors,
            IList<string> filter)
        {
            var projectors = new List<Projector>();
            if (filter.Count == 0)
                return projectors;

            foreach (var projector in allProjectors)
            foreach (var filterWord in filter)
                if (MaterialEditorFilter.Matches(projector.NameFormatted(), filterWord.Trim()))
                    projectors.Add(projector);

            return projectors;
        }

        private Dictionary<string, Material> SelectMaterials(
            GameObject gameObject,
            IEnumerable<Renderer> allRenderers,
            IEnumerable<Renderer> selectedRenderers,
            IList<string> filter)
        {
            var materials = new Dictionary<string, Material>();
            if (filter.Count == 0)
            {
                foreach (var renderer in selectedRenderers)
                foreach (var material in GetSelectedMaterials(gameObject, renderer))
                    materials[material.NameFormatted()] = material;
                return materials;
            }

            foreach (var renderer in allRenderers)
            foreach (var material in GetSelectedMaterials(gameObject, renderer))
            foreach (var filterWord in filter)
                if (MaterialEditorFilter.Matches(material.NameFormatted(), filterWord.Trim()))
                    materials[material.NameFormatted()] = material;

            return materials;
        }

        private IEnumerable<Material> GetSelectedMaterials(GameObject gameObject, Renderer renderer)
        {
            var materials = GetMaterials(gameObject, renderer);
            return _session.SelectedMaterials.Count == 0
                ? materials
                : materials.Where(material => _session.SelectedMaterials.Contains(material));
        }

        private void AddRendererRows(
            ICollection<RowModel> rows,
            GameObject gameObject,
            object data,
            Renderer renderer)
        {
            var rendererName = renderer.NameFormatted();
            rows.Add(new RendererRowModel()
            {
                GameObject = gameObject,
                Data = data,
                Renderer = renderer,
                RendererName = rendererName,
                ExportUVOnClick = () => _actions.ExportUv(renderer),
                ExportObjOnClick = () => _actions.RequestObjExport(renderer),
                SelectInterpolableButtonRendererOnClick = () =>
                    _actions.SelectInterpolable(
                        gameObject,
                        RowModel.RowItemType.Renderer,
                        string.Empty,
                        string.Empty,
                        rendererName)
            });

            var originalValue = _editService.GetRendererPropertyValueOriginal(
                data,
                renderer,
                RendererProperties.Enabled,
                gameObject);
            var originalEnabled = originalValue.IsNullOrEmpty()
                ? renderer.enabled
                : originalValue == "1";
            rows.Add(new RowModel(RowModel.RowItemType.RendererEnabled, "Enabled")
            {
                RendererEnabled = renderer.enabled,
                RendererEnabledOriginal = originalEnabled,
                RendererEnabledOnChange = value =>
                    _editService.SetRendererProperty(
                        data,
                        renderer,
                        RendererProperties.Enabled,
                        (value ? 1 : 0).ToString(),
                        gameObject),
                RendererEnabledOnReset = () =>
                    _editService.RemoveRendererProperty(data, renderer, RendererProperties.Enabled, gameObject)
            });

            originalValue = _editService.GetRendererPropertyValueOriginal(
                data,
                renderer,
                RendererProperties.ShadowCastingMode,
                gameObject);
            var originalShadowCastingMode = originalValue.IsNullOrEmpty()
                ? renderer.shadowCastingMode
                : (UnityEngine.Rendering.ShadowCastingMode)int.Parse(originalValue);
            rows.Add(new RowModel(RowModel.RowItemType.RendererShadowCastingMode, "Shadow Casting Mode")
            {
                RendererShadowCastingMode = (int)renderer.shadowCastingMode,
                RendererShadowCastingModeOriginal = (int)originalShadowCastingMode,
                RendererShadowCastingModeOnChange = value =>
                    _editService.SetRendererProperty(
                        data,
                        renderer,
                        RendererProperties.ShadowCastingMode,
                        value.ToString(),
                        gameObject),
                RendererShadowCastingModeOnReset = () =>
                    _editService.RemoveRendererProperty(
                        data,
                        renderer,
                        RendererProperties.ShadowCastingMode,
                        gameObject)
            });

            originalValue = _editService.GetRendererPropertyValueOriginal(
                data,
                renderer,
                RendererProperties.ReceiveShadows,
                gameObject);
            var originalReceiveShadows = originalValue.IsNullOrEmpty()
                ? renderer.receiveShadows
                : originalValue == "1";
            rows.Add(new RowModel(RowModel.RowItemType.RendererReceiveShadows, "Receive Shadows")
            {
                RendererReceiveShadows = renderer.receiveShadows,
                RendererReceiveShadowsOriginal = originalReceiveShadows,
                RendererReceiveShadowsOnChange = value =>
                    _editService.SetRendererProperty(
                        data,
                        renderer,
                        RendererProperties.ReceiveShadows,
                        (value ? 1 : 0).ToString(),
                        gameObject),
                RendererReceiveShadowsOnReset = () =>
                    _editService.RemoveRendererProperty(
                        data,
                        renderer,
                        RendererProperties.ReceiveShadows,
                        gameObject)
            });

            var meshRenderer = renderer as SkinnedMeshRenderer;
            if (meshRenderer == null)
                return;

#if !KK
            originalValue = _editService.GetRendererPropertyValueOriginal(
                data,
                renderer,
                RendererProperties.UpdateWhenOffscreen,
                gameObject);
            var originalUpdateWhenOffscreen = originalValue.IsNullOrEmpty()
                ? meshRenderer.updateWhenOffscreen
                : originalValue == "1";
            rows.Add(new RowModel(RowModel.RowItemType.RendererUpdateWhenOffscreen, "Update When Off-Screen")
            {
                RendererUpdateWhenOffscreen = meshRenderer.updateWhenOffscreen,
                RendererUpdateWhenOffscreenOriginal = originalUpdateWhenOffscreen,
                RendererUpdateWhenOffscreenOnChange = value =>
                    _editService.SetRendererProperty(
                        data,
                        renderer,
                        RendererProperties.UpdateWhenOffscreen,
                        (value ? 1 : 0).ToString(),
                        gameObject),
                RendererUpdateWhenOffscreenOnReset = () =>
                    _editService.RemoveRendererProperty(
                        data,
                        renderer,
                        RendererProperties.UpdateWhenOffscreen,
                        gameObject)
            });
#endif

            originalValue = _editService.GetRendererPropertyValueOriginal(
                data,
                renderer,
                RendererProperties.RecalculateNormals,
                gameObject);
            var originalRecalculateNormals = !originalValue.IsNullOrEmpty() && originalValue == "1";
            var currentValue = _editService.GetRendererPropertyValue(
                data,
                renderer,
                RendererProperties.RecalculateNormals,
                gameObject);
            var recalculateNormals = !currentValue.IsNullOrEmpty() && currentValue == "1";
            rows.Add(new RowModel(RowModel.RowItemType.RendererRecalculateNormals, "Recalculate Normals")
            {
                RendererRecalculateNormals = recalculateNormals,
                RendererRecalculateNormalsOriginal = originalRecalculateNormals,
                RendererRecalculateNormalsOnChange = value =>
                    _editService.SetRendererProperty(
                        data,
                        renderer,
                        RendererProperties.RecalculateNormals,
                        (value ? 1 : 0).ToString(),
                        gameObject),
                RendererRecalculateNormalsOnReset = () =>
                    _editService.RemoveRendererProperty(
                        data,
                        renderer,
                        RendererProperties.RecalculateNormals,
                        gameObject)
            });
        }

        private void AddMaterialRows(
            ICollection<RowModel> rows,
            GameObject gameObject,
            object data,
            string filter,
            IEnumerable<Renderer> allRenderers,
            IList<string> propertyFilter,
            Material material,
            Projector projector)
        {
            var materialName = material.NameFormatted();
            var shaderName = material.shader.NameFormatted();
            var materialItem = new RowModel(RowModel.RowItemType.Material, "Material")
            {
                GameObject = gameObject,
                Data = data,
                Material = material,
                Projector = projector,
                MaterialName = materialName,
                MaterialOnCopy = () => _editService.MaterialCopyEdits(data, material, gameObject),
                MaterialOnPaste = () =>
                {
                    _editService.MaterialPasteEdits(data, material, gameObject);
                    _actions.Refresh(gameObject, data, filter);
                },
                MaterialOnRename = () => _actions.ShowRename(gameObject, material, data)
            };
            if (projector == null)
            {
                materialItem.MaterialOnCopyRemove = () =>
                {
                    _editService.MaterialCopyRemove(data, material, gameObject);
                    _actions.Refresh(gameObject, data, filter);
                    _actions.RefreshMaterialSelection(gameObject, data, allRenderers);
                };
            }
            rows.Add(materialItem);

            if (projector != null)
                AddProjectorRows(rows, gameObject, data, propertyFilter, projector);

            AddShaderRows(rows, gameObject, data, filter, materialName, material, projector, shaderName);
            AddPropertyRows(rows, gameObject, data, filter, propertyFilter, materialName, material, projector, shaderName);
        }

        private void AddShaderRows(
            ICollection<RowModel> rows,
            GameObject gameObject,
            object data,
            string filter,
            string materialName,
            Material material,
            Projector projector,
            string shaderName)
        {
            var originalShaderName = _editService.GetMaterialShaderNameOriginal(data, material, gameObject);
            if (originalShaderName.IsNullOrEmpty())
                originalShaderName = shaderName;
            rows.Add(new RowModel(RowModel.RowItemType.Shader, "Shader")
            {
                GameObject = gameObject,
                Data = data,
                Material = material,
                Projector = projector,
                ShaderName = shaderName,
                ShaderNameOriginal = originalShaderName,
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
                SelectInterpolableButtonShaderOnClick = () =>
                    _actions.SelectInterpolable(
                        gameObject,
                        RowModel.RowItemType.Shader,
                        materialName,
                        string.Empty,
                        string.Empty)
            });

            var originalRenderQueue =
                _editService.GetMaterialShaderRenderQueueOriginal(data, material, gameObject)
                ?? material.renderQueue;
            rows.Add(new RowModel(RowModel.RowItemType.ShaderRenderQueue, "Render Queue")
            {
                GameObject = gameObject,
                Data = data,
                Material = material,
                Projector = projector,
                ShaderRenderQueue = material.renderQueue,
                ShaderRenderQueueOriginal = originalRenderQueue,
                ShaderRenderQueueOnChange = value =>
                    _editService.SetMaterialShaderRenderQueue(data, material, value, gameObject),
                ShaderRenderQueueOnReset = () =>
                    _editService.RemoveMaterialShaderRenderQueue(data, material, gameObject)
            });
        }

        private void AddPropertyRows(
            ICollection<RowModel> rows,
            GameObject gameObject,
            object data,
            string filter,
            IList<string> propertyFilter,
            string materialName,
            Material material,
            Projector projector,
            string shaderName)
        {
            var categories = PropertyOrganizer.PropertyOrganization[
                XMLShaderProperties.ContainsKey(shaderName) ? shaderName : "default"];

            foreach (var category in categories)
            {
                var definitions = category.Value
                    .Where(property =>
                        property.Type == ShaderPropertyType.Keyword
                        || material.HasProperty($"_{property.Name}"))
                    .ToList();
                var showCategory =
                    propertyFilter.Count == 0
                    && (categories.Count > 1 || category.Key != PropertyOrganizer.UncategorizedName)
                    && definitions.Any();
                var categoryKey = $"{material.GetInstanceID()}:{category.Key}";
                bool collapsed;
                var categoryCollapsed =
                    showCategory
                    && _session.CollapsedPropertyCategories.TryGetValue(categoryKey, out collapsed)
                    && collapsed;

                if (showCategory)
                {
                    rows.Add(new RowModel(RowModel.RowItemType.PropertyCategory, category.Key)
                    {
                        CategoryCollapsed = categoryCollapsed,
                        CategoryCollapsedOnChange = value =>
                        {
                            if (value)
                                _session.CollapsedPropertyCategories[categoryKey] = true;
                            else
                                _session.CollapsedPropertyCategories.Remove(categoryKey);
                            _actions.Refresh(gameObject, data, filter);
                        }
                    });
                }

                if (categoryCollapsed)
                    continue;

                foreach (var definition in definitions)
                {
                    var propertyName = definition.Name;
                    if (_actions.IsPropertyBlacklisted(materialName, propertyName))
                        continue;
                    if (propertyFilter.Count > 0
                        && !propertyFilter.Any(word => MaterialEditorFilter.Matches(propertyName, word)))
                        continue;

                    var descriptor = new PropertyDescriptor(
                        gameObject,
                        data,
                        material,
                        projector,
                        materialName,
                        definition);
                    foreach (var row in _propertyRows.Create(descriptor))
                        rows.Add(row);
                }
            }
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

        private static RowModel CreateFloatRow(
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
                FloatValue = value,
                FloatValueOriginal = original,
                SelectInterpolableButtonFloatOnClick = selectInterpolable,
                FloatValueOnChange = changeValue,
                FloatValueOnReset = resetValue
            };
            if (minValue != null)
                item.FloatValueSliderMin = minValue.Value;
            if (maxValue != null)
                item.FloatValueSliderMax = maxValue.Value;
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
