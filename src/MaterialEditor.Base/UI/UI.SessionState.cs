using System.Collections.Generic;
using UnityEngine;

namespace MaterialEditorAPI
{
    internal sealed class MaterialEditorSessionState
    {
        internal GameObject CurrentGameObject;
        internal object CurrentData;
        internal string Filter = "";

        internal readonly List<Renderer> SelectedRenderers = new List<Renderer>();
        internal readonly List<Material> SelectedMaterials = new List<Material>();
        internal readonly List<Renderer> SelectedMaterialRenderers = new List<Renderer>();
        internal readonly Dictionary<string, bool> CollapsedPropertyCategories = new Dictionary<string, bool>();
        internal readonly Dictionary<string, bool> CollapsedMaterialSections = new Dictionary<string, bool>();
        internal readonly Dictionary<string, bool> CollapsedShaderSections = new Dictionary<string, bool>();

        internal bool ListsVisible;
        internal bool RenameListVisible;

        private bool _objExportPending;
        private Renderer _objRenderer;

        internal void RequestObjExport(Renderer renderer)
        {
            _objRenderer = renderer;
            _objExportPending = renderer != null;
        }

        internal bool TryTakeObjExport(out Renderer renderer)
        {
            renderer = _objRenderer;
            if (!_objExportPending)
                return false;

            _objExportPending = false;
            _objRenderer = null;
            return renderer != null;
        }

        internal static bool IsCollapsed(
            IDictionary<string, bool> states,
            string key)
        {
            bool collapsed;
            return states.TryGetValue(key, out collapsed) && collapsed;
        }

        internal static void SetCollapsed(
            IDictionary<string, bool> states,
            string key,
            bool collapsed)
        {
            if (collapsed)
                states[key] = true;
            else
                states.Remove(key);
        }
    }
}
