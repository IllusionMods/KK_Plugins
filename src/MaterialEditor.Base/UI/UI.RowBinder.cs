using System;
using UnityEngine;

namespace MaterialEditorAPI
{
    // Routes a pooled row model to a plain C# family binder and owns its listener lifetime.
    internal sealed class RowBinder : MonoBehaviour
    {
        private RowModel _currentModel;
        private RowControlSet _controls;
        private RowHandlerRegistry _registry;
        private ListenerScope _listeners;

        internal RowModel CurrentModel
        {
            get => _currentModel;
            set => Bind(value, false);
        }

        internal void InitializeControls()
        {
            if (_controls != null)
                return;

            _controls = RowControlSet.Create(this);
            _registry = new RowHandlerRegistry();

            var renderer = new RendererRowTypeBinder(_controls);
            _registry.Register(
                renderer,
                RowModel.RowItemType.Renderer,
                RowModel.RowItemType.RendererEnabled,
                RowModel.RowItemType.RendererShadowCastingMode,
                RowModel.RowItemType.RendererReceiveShadows,
                RowModel.RowItemType.RendererUpdateWhenOffscreen,
                RowModel.RowItemType.RendererRecalculateNormals);

            var materialShader = new MaterialShaderRowTypeBinder(_controls);
            _registry.Register(
                materialShader,
                RowModel.RowItemType.Material,
                RowModel.RowItemType.Shader,
                RowModel.RowItemType.ShaderRenderQueue);

            var texture = new TextureRowTypeBinder(_controls);
            _registry.Register(
                texture,
                RowModel.RowItemType.PropertyCategory,
                RowModel.RowItemType.TextureProperty,
                RowModel.RowItemType.TextureOffsetScale);

            _registry.Register(
                new ColorRowTypeBinder(_controls),
                RowModel.RowItemType.ColorProperty);

            var floatKeyword = new FloatKeywordRowTypeBinder(_controls);
            _registry.Register(
                floatKeyword,
                RowModel.RowItemType.FloatProperty,
                RowModel.RowItemType.KeywordProperty);
        }

        internal void Bind(RowModel item, bool force)
        {
            if (!force && ReferenceEquals(item, _currentModel))
                return;

            InitializeControls();
            _currentModel = item;

            _listeners?.Dispose();
            _listeners = new ListenerScope();
            _controls.HideAll();

            if (item == null)
                return;

            IRowTypeBinder handler;
            if (_registry.TryGet(item.ItemType, out handler))
                handler.Bind(item, _listeners);
        }

        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible)
                gameObject.SetActive(visible);
        }

        public T GetUIComponent<T>(string gameObjectName) where T : Component
        {
            var uiTransform = transform.FindLoop(gameObjectName);
            if (uiTransform == null)
                throw new ArgumentException($"Couldn't find {gameObjectName}");

            var component = uiTransform.GetComponent<T>();
            if (component == null)
                throw new ArgumentException($"Couldn't find {gameObjectName}");
            return component;
        }

        private void OnDestroy()
        {
            _listeners?.Dispose();
        }
    }
}
