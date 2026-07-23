using UnityEngine;

namespace MaterialEditorAPI
{
    // Owns the lifecycle of one pooled row instance; control wiring belongs to RowBinder.
    internal sealed class RowView : MonoBehaviour
    {
        private RowBinder _binder;

        internal RowModel CurrentModel => Binder.CurrentModel;

        private RowBinder Binder => _binder ?? (_binder = GetComponent<RowBinder>());

        internal void Initialize(RowBinder binder)
        {
            _binder = binder;
        }

        internal void Bind(RowModel model, bool force)
        {
            Binder.Bind(model, force);
        }

        internal void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible)
                gameObject.SetActive(visible);
        }
    }
}
