using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;

namespace MaterialEditorAPI
{
    internal sealed class MaterialEditorSelectionController
    {
        private readonly MaterialEditorSessionState _session;
        private readonly MaterialEditorWindowView _view;
        private readonly MaterialEditService _editService;
        private readonly Action<GameObject, object, string> _refresh;

        internal MaterialEditorSelectionController(
            MaterialEditorSessionState session,
            MaterialEditorWindowView view,
            MaterialEditService editService,
            Action<GameObject, object, string> refresh)
        {
            _session = session;
            _view = view;
            _editService = editService;
            _refresh = refresh;
        }

        internal void ToggleSidePanels()
        {
            if (_session.RenameListVisible)
            {
                CloseRenamePanel();
                return;
            }

            _session.ListsVisible = !_session.ListsVisible;
            _view.SetSelectionListsVisible(_session.ListsVisible);
            _view.SetViewListGlyph(_session.ListsVisible ? "<" : ">");
        }

        internal void CloseRenamePanel()
        {
            if (!_session.RenameListVisible)
                return;

            _view.SetRenameListVisible(false);
            _view.SetViewListGlyph(">");
            _session.RenameListVisible = false;
        }

        internal void ShowRenamePanel(GameObject gameObject, Material material, object data)
        {
            if (_session.ListsVisible)
            {
                _view.SetSelectionListsVisible(false);
                _session.ListsVisible = false;
            }

            _view.SetViewListGlyph("<");
            _view.SetRenameListVisible(true);
            PopulateRenameList(gameObject, material, data);
            _session.RenameListVisible = true;
        }

        internal void PopulateRendererList(GameObject gameObject, object data, IEnumerable<Renderer> renderers)
        {
            if (gameObject == _session.CurrentGameObject)
                return;

            _session.SelectedRenderers.Clear();
            _view.RendererList.ClearList();

            foreach (var renderer in renderers)
            {
                var capturedRenderer = renderer;
                _view.RendererList.AddEntry(capturedRenderer.NameFormatted(), selected =>
                {
                    UpdateSelection(_session.SelectedRenderers, capturedRenderer, selected);
                    _refresh(gameObject, data, _session.Filter);
                    PopulateMaterialList(gameObject, data, renderers);
                });
            }

            PopulateMaterialList(gameObject, data, renderers);
        }

        internal void PopulateMaterialList(GameObject gameObject, object data, IEnumerable<Renderer> renderers)
        {
            _session.SelectedMaterials.Clear();
            _view.MaterialList.ClearList();

            foreach (var renderer in renderers.Where(renderer =>
                         _session.SelectedRenderers.Count == 0
                         || _session.SelectedRenderers.Contains(renderer)))
            {
                foreach (var material in GetMaterials(gameObject, renderer))
                {
                    var capturedMaterial = material;
                    _view.MaterialList.AddEntry(capturedMaterial.NameFormatted(), selected =>
                    {
                        UpdateSelection(_session.SelectedMaterials, capturedMaterial, selected);
                        _refresh(gameObject, data, _session.Filter);
                    });
                }
            }
        }

        private void PopulateRenameList(GameObject gameObject, Material material, object data)
        {
            _session.SelectedMaterialRenderers.Clear();
            _view.RenameList.ClearList();
            _view.RenameMaterial.text = material.NameFormatted();

            var formattedName = material.NameFormatted()
                .Split(new[] { MaterialCopyPostfix }, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();
            if (string.IsNullOrEmpty(formattedName))
            {
                MaterialEditorPluginBase.Logger.LogWarning("Material name is empty or failed to be extracted from: " + material.name);
                formattedName = "";
            }
            _view.RenameField.text = formattedName;

            var suffix = material.NameFormatted().Replace(formattedName, "");
            _view.RenameButton.interactable = false;
            _view.RenameButton.onClick.RemoveAllListeners();
            _view.RenameButton.onClick.AddListener(() =>
            {
                var safeNewName = _view.RenameField.text.Replace(MaterialCopyPostfix, "").Trim() + suffix;
                foreach (var renderer in _session.SelectedMaterialRenderers)
                    _editService.SetMaterialName(data, renderer, material, safeNewName, gameObject);
                _refresh(gameObject, data, _session.Filter);
            });

            foreach (var renderer in GetRendererList(gameObject))
            {
                if (!renderer.materials.Any(candidate => candidate.NameFormatted() == material.NameFormatted()))
                    continue;

                var capturedRenderer = renderer;
                _view.RenameList.AddEntry(capturedRenderer.NameFormatted(), selected =>
                {
                    UpdateSelection(_session.SelectedMaterialRenderers, capturedRenderer, selected);
                    _view.RenameButton.interactable = _session.SelectedMaterialRenderers.Count > 0;
                });
            }
        }

        private static void UpdateSelection<T>(ICollection<T> selection, T item, bool selected)
        {
            if (selected)
            {
                if (!selection.Contains(item))
                    selection.Add(item);
            }
            else
            {
                selection.Remove(item);
            }
        }
    }
}
