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
        TexturePropertyRowModel row,
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

    /// <summary>
    /// Coordinates filtering and renderer presentation, then delegates each material
    /// section to <see cref="MaterialSectionPresenter"/>.
    /// </summary>
    internal sealed class MaterialEditorPresenter
    {
        private readonly MaterialEditService _editService;
        private readonly MaterialEditorSessionState _session;
        private readonly MaterialEditorPresentationActions _actions;
        private readonly MaterialSectionPresenter _materialSections;

        internal MaterialEditorPresenter(
            MaterialEditService editService,
            MaterialEditorSessionState session,
            MaterialEditorPresentationActions actions)
        {
            _editService = editService;
            _session = session;
            _actions = actions;
            _materialSections = new MaterialSectionPresenter(editService, session, actions);
        }

        internal MaterialEditorPresentation BuildRows(
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
            var presentation = new MaterialEditorPresentation();
            var rows = presentation.Rows;

            foreach (var renderer in renderers)
                AddRendererRows(rows, gameObject, data, renderer);

            foreach (var material in materials.Values)
            {
                _materialSections.AddRows(new MaterialSectionContext(
                    _editService,
                    presentation,
                    gameObject,
                    data,
                    filter,
                    allRenderers,
                    propertyFilter,
                    material,
                    null));
            }

            foreach (var projector in rendererFilter.Count == 0 ? allProjectors : projectors)
            {
                _materialSections.AddRows(new MaterialSectionContext(
                    _editService,
                    presentation,
                    gameObject,
                    data,
                    filter,
                    allRenderers,
                    propertyFilter,
                    projector.material,
                    projector));
            }

            return presentation;
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

        private static List<Projector> SelectProjectors(
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
            var edits = new MaterialEditorEditService(
                _editService,
                gameObject,
                data);
            rows.Add(new RendererRowModel()
            {
                GameObject = gameObject,
                Data = data,
                Renderer = renderer,
                RendererName = rendererName,
                ExportUv = () => _actions.ExportUv(renderer),
                ExportObj = () => _actions.RequestObjExport(renderer),
                SelectInterpolable = () =>
                    _actions.SelectInterpolable(
                        gameObject,
                        RowModel.RowItemType.Renderer,
                        string.Empty,
                        string.Empty,
                        rendererName)
            });

            var originalValue = edits.GetOriginalRendererProperty(
                renderer,
                RendererProperties.Enabled);
            var originalEnabled = originalValue.IsNullOrEmpty()
                ? renderer.enabled
                : originalValue == "1";
            rows.Add(new RendererEnabledRowModel()
            {
                Value = renderer.enabled,
                OriginalValue = originalEnabled,
                ValueOnChange = value =>
                    edits.SetRendererProperty(
                        renderer,
                        RendererProperties.Enabled,
                        (value ? 1 : 0).ToString()),
                ValueOnReset = () =>
                    edits.ResetRendererProperty(
                        renderer,
                        RendererProperties.Enabled)
            });

            originalValue = edits.GetOriginalRendererProperty(
                renderer,
                RendererProperties.ShadowCastingMode);
            var originalShadowCastingMode = originalValue.IsNullOrEmpty()
                ? renderer.shadowCastingMode
                : (UnityEngine.Rendering.ShadowCastingMode)int.Parse(originalValue);
            rows.Add(new RendererShadowCastingModeRowModel()
            {
                Value = (int)renderer.shadowCastingMode,
                OriginalValue = (int)originalShadowCastingMode,
                ValueOnChange = value =>
                    edits.SetRendererProperty(
                        renderer,
                        RendererProperties.ShadowCastingMode,
                        value.ToString()),
                ValueOnReset = () =>
                    edits.ResetRendererProperty(
                        renderer,
                        RendererProperties.ShadowCastingMode)
            });

            originalValue = edits.GetOriginalRendererProperty(
                renderer,
                RendererProperties.ReceiveShadows);
            var originalReceiveShadows = originalValue.IsNullOrEmpty()
                ? renderer.receiveShadows
                : originalValue == "1";
            rows.Add(new RendererReceiveShadowsRowModel()
            {
                Value = renderer.receiveShadows,
                OriginalValue = originalReceiveShadows,
                ValueOnChange = value =>
                    edits.SetRendererProperty(
                        renderer,
                        RendererProperties.ReceiveShadows,
                        (value ? 1 : 0).ToString()),
                ValueOnReset = () =>
                    edits.ResetRendererProperty(
                        renderer,
                        RendererProperties.ReceiveShadows)
            });

            var meshRenderer = renderer as SkinnedMeshRenderer;
            if (meshRenderer == null)
                return;

#if !KK
            originalValue = edits.GetOriginalRendererProperty(
                renderer,
                RendererProperties.UpdateWhenOffscreen);
            var originalUpdateWhenOffscreen = originalValue.IsNullOrEmpty()
                ? meshRenderer.updateWhenOffscreen
                : originalValue == "1";
            rows.Add(new RendererUpdateWhenOffscreenRowModel()
            {
                Value = meshRenderer.updateWhenOffscreen,
                OriginalValue = originalUpdateWhenOffscreen,
                ValueOnChange = value =>
                    edits.SetRendererProperty(
                        renderer,
                        RendererProperties.UpdateWhenOffscreen,
                        (value ? 1 : 0).ToString()),
                ValueOnReset = () =>
                    edits.ResetRendererProperty(
                        renderer,
                        RendererProperties.UpdateWhenOffscreen)
            });
#endif

            originalValue = edits.GetOriginalRendererProperty(
                renderer,
                RendererProperties.RecalculateNormals);
            var originalRecalculateNormals = !originalValue.IsNullOrEmpty() && originalValue == "1";
            var currentValue = edits.GetRendererProperty(
                renderer,
                RendererProperties.RecalculateNormals);
            var recalculateNormals = !currentValue.IsNullOrEmpty() && currentValue == "1";
            rows.Add(new RendererRecalculateNormalsRowModel()
            {
                Value = recalculateNormals,
                OriginalValue = originalRecalculateNormals,
                ValueOnChange = value =>
                    edits.SetRendererProperty(
                        renderer,
                        RendererProperties.RecalculateNormals,
                        (value ? 1 : 0).ToString()),
                ValueOnReset = () =>
                    edits.ResetRendererProperty(
                        renderer,
                        RendererProperties.RecalculateNormals)
            });
        }
    }
}
