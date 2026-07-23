using System;
using System.Collections.Generic;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;

namespace MaterialEditorAPI
{
    internal interface IMaterialEditRepository
    {
        string GetRendererPropertyValueOriginal(object data, Renderer renderer, RendererProperties property, GameObject gameObject);
        string GetRendererPropertyValue(object data, Renderer renderer, RendererProperties property, GameObject gameObject);
        void SetRendererProperty(object data, Renderer renderer, RendererProperties property, string value, GameObject gameObject);
        void RemoveRendererProperty(object data, Renderer renderer, RendererProperties property, GameObject gameObject);

        float? GetProjectorPropertyValueOriginal(object data, Projector projector, ProjectorProperties property, GameObject gameObject);
        float? GetProjectorPropertyValue(object data, Projector projector, ProjectorProperties property, GameObject gameObject);
        void SetProjectorProperty(object data, Projector projector, ProjectorProperties property, float value, GameObject gameObject);
        void RemoveProjectorProperty(object data, Projector projector, ProjectorProperties property, GameObject gameObject);
        IEnumerable<Projector> GetProjectorList(object data, GameObject gameObject);

        void MaterialCopyEdits(object data, Material material, GameObject gameObject);
        void MaterialPasteEdits(object data, Material material, GameObject gameObject);
        void MaterialCopyRemove(object data, Material material, GameObject gameObject);

        string GetMaterialNameOriginal(object data, Renderer renderer, Material material, GameObject gameObject);
        void SetMaterialName(object data, Renderer renderer, Material material, string value, GameObject gameObject);
        void RemoveMaterialName(object data, Renderer renderer, Material material, GameObject gameObject);

        string GetMaterialShaderNameOriginal(object data, Material material, GameObject gameObject);
        void SetMaterialShaderName(object data, Material material, string value, GameObject gameObject);
        void RemoveMaterialShaderName(object data, Material material, GameObject gameObject);

        int? GetMaterialShaderRenderQueueOriginal(object data, Material material, GameObject gameObject);
        void SetMaterialShaderRenderQueue(object data, Material material, int value, GameObject gameObject);
        void RemoveMaterialShaderRenderQueue(object data, Material material, GameObject gameObject);

        bool GetMaterialTextureValueOriginal(object data, Material material, string propertyName, GameObject gameObject);
        void SetMaterialTexture(object data, Material material, string propertyName, string filePath, GameObject gameObject);
        void RemoveMaterialTexture(object data, Material material, string propertyName, GameObject gameObject);

        Vector2? GetMaterialTextureOffsetOriginal(object data, Material material, string propertyName, GameObject gameObject);
        void SetMaterialTextureOffset(object data, Material material, string propertyName, Vector2 value, GameObject gameObject);
        void RemoveMaterialTextureOffset(object data, Material material, string propertyName, GameObject gameObject);

        Vector2? GetMaterialTextureScaleOriginal(object data, Material material, string propertyName, GameObject gameObject);
        void SetMaterialTextureScale(object data, Material material, string propertyName, Vector2 value, GameObject gameObject);
        void RemoveMaterialTextureScale(object data, Material material, string propertyName, GameObject gameObject);

        Color? GetMaterialColorPropertyValueOriginal(object data, Material material, string propertyName, GameObject gameObject);
        void SetMaterialColorProperty(object data, Material material, string propertyName, Color value, GameObject gameObject);
        void RemoveMaterialColorProperty(object data, Material material, string propertyName, GameObject gameObject);

        float? GetMaterialFloatPropertyValueOriginal(object data, Material material, string propertyName, GameObject gameObject);
        void SetMaterialFloatProperty(object data, Material material, string propertyName, float value, GameObject gameObject);
        void RemoveMaterialFloatProperty(object data, Material material, string propertyName, GameObject gameObject);

        bool? GetMaterialKeywordPropertyValueOriginal(object data, Material material, string propertyName, GameObject gameObject);
        void SetMaterialKeywordProperty(object data, Material material, string propertyName, bool value, GameObject gameObject);
        void RemoveMaterialKeywordProperty(object data, Material material, string propertyName, GameObject gameObject);
    }

    internal sealed class LegacyMaterialEditRepository : IMaterialEditRepository
    {
        private readonly MaterialEditorUI _ui;

        internal LegacyMaterialEditRepository(MaterialEditorUI ui)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
        }

        public string GetRendererPropertyValueOriginal(object data, Renderer renderer, RendererProperties property, GameObject gameObject) =>
            _ui.GetRendererPropertyValueOriginal(data, renderer, property, gameObject);
        public string GetRendererPropertyValue(object data, Renderer renderer, RendererProperties property, GameObject gameObject) =>
            _ui.GetRendererPropertyValue(data, renderer, property, gameObject);
        public void SetRendererProperty(object data, Renderer renderer, RendererProperties property, string value, GameObject gameObject) =>
            _ui.SetRendererProperty(data, renderer, property, value, gameObject);
        public void RemoveRendererProperty(object data, Renderer renderer, RendererProperties property, GameObject gameObject) =>
            _ui.RemoveRendererProperty(data, renderer, property, gameObject);

        public float? GetProjectorPropertyValueOriginal(object data, Projector projector, ProjectorProperties property, GameObject gameObject) =>
            _ui.GetProjectorPropertyValueOriginal(data, projector, property, gameObject);
        public float? GetProjectorPropertyValue(object data, Projector projector, ProjectorProperties property, GameObject gameObject) =>
            _ui.GetProjectorPropertyValue(data, projector, property, gameObject);
        public void SetProjectorProperty(object data, Projector projector, ProjectorProperties property, float value, GameObject gameObject) =>
            _ui.SetProjectorProperty(data, projector, property, value, gameObject);
        public void RemoveProjectorProperty(object data, Projector projector, ProjectorProperties property, GameObject gameObject) =>
            _ui.RemoveProjectorProperty(data, projector, property, gameObject);
        public IEnumerable<Projector> GetProjectorList(object data, GameObject gameObject) =>
            _ui.GetProjectorList(data, gameObject);

        public void MaterialCopyEdits(object data, Material material, GameObject gameObject) =>
            _ui.MaterialCopyEdits(data, material, gameObject);
        public void MaterialPasteEdits(object data, Material material, GameObject gameObject) =>
            _ui.MaterialPasteEdits(data, material, gameObject);
        public void MaterialCopyRemove(object data, Material material, GameObject gameObject) =>
            _ui.MaterialCopyRemove(data, material, gameObject);

        public string GetMaterialNameOriginal(object data, Renderer renderer, Material material, GameObject gameObject) =>
            _ui.GetMaterialNameOriginal(data, renderer, material, gameObject);
        public void SetMaterialName(object data, Renderer renderer, Material material, string value, GameObject gameObject) =>
            _ui.SetMaterialName(data, renderer, material, value, gameObject);
        public void RemoveMaterialName(object data, Renderer renderer, Material material, GameObject gameObject) =>
            _ui.RemoveMaterialName(data, renderer, material, gameObject);

        public string GetMaterialShaderNameOriginal(object data, Material material, GameObject gameObject) =>
            _ui.GetMaterialShaderNameOriginal(data, material, gameObject);
        public void SetMaterialShaderName(object data, Material material, string value, GameObject gameObject) =>
            _ui.SetMaterialShaderName(data, material, value, gameObject);
        public void RemoveMaterialShaderName(object data, Material material, GameObject gameObject) =>
            _ui.RemoveMaterialShaderName(data, material, gameObject);

        public int? GetMaterialShaderRenderQueueOriginal(object data, Material material, GameObject gameObject) =>
            _ui.GetMaterialShaderRenderQueueOriginal(data, material, gameObject);
        public void SetMaterialShaderRenderQueue(object data, Material material, int value, GameObject gameObject) =>
            _ui.SetMaterialShaderRenderQueue(data, material, value, gameObject);
        public void RemoveMaterialShaderRenderQueue(object data, Material material, GameObject gameObject) =>
            _ui.RemoveMaterialShaderRenderQueue(data, material, gameObject);

        public bool GetMaterialTextureValueOriginal(object data, Material material, string propertyName, GameObject gameObject) =>
            _ui.GetMaterialTextureValueOriginal(data, material, propertyName, gameObject);
        public void SetMaterialTexture(object data, Material material, string propertyName, string filePath, GameObject gameObject) =>
            _ui.SetMaterialTexture(data, material, propertyName, filePath, gameObject);
        public void RemoveMaterialTexture(object data, Material material, string propertyName, GameObject gameObject) =>
            _ui.RemoveMaterialTexture(data, material, propertyName, gameObject);

        public Vector2? GetMaterialTextureOffsetOriginal(object data, Material material, string propertyName, GameObject gameObject) =>
            _ui.GetMaterialTextureOffsetOriginal(data, material, propertyName, gameObject);
        public void SetMaterialTextureOffset(object data, Material material, string propertyName, Vector2 value, GameObject gameObject) =>
            _ui.SetMaterialTextureOffset(data, material, propertyName, value, gameObject);
        public void RemoveMaterialTextureOffset(object data, Material material, string propertyName, GameObject gameObject) =>
            _ui.RemoveMaterialTextureOffset(data, material, propertyName, gameObject);

        public Vector2? GetMaterialTextureScaleOriginal(object data, Material material, string propertyName, GameObject gameObject) =>
            _ui.GetMaterialTextureScaleOriginal(data, material, propertyName, gameObject);
        public void SetMaterialTextureScale(object data, Material material, string propertyName, Vector2 value, GameObject gameObject) =>
            _ui.SetMaterialTextureScale(data, material, propertyName, value, gameObject);
        public void RemoveMaterialTextureScale(object data, Material material, string propertyName, GameObject gameObject) =>
            _ui.RemoveMaterialTextureScale(data, material, propertyName, gameObject);

        public Color? GetMaterialColorPropertyValueOriginal(object data, Material material, string propertyName, GameObject gameObject) =>
            _ui.GetMaterialColorPropertyValueOriginal(data, material, propertyName, gameObject);
        public void SetMaterialColorProperty(object data, Material material, string propertyName, Color value, GameObject gameObject) =>
            _ui.SetMaterialColorProperty(data, material, propertyName, value, gameObject);
        public void RemoveMaterialColorProperty(object data, Material material, string propertyName, GameObject gameObject) =>
            _ui.RemoveMaterialColorProperty(data, material, propertyName, gameObject);

        public float? GetMaterialFloatPropertyValueOriginal(object data, Material material, string propertyName, GameObject gameObject) =>
            _ui.GetMaterialFloatPropertyValueOriginal(data, material, propertyName, gameObject);
        public void SetMaterialFloatProperty(object data, Material material, string propertyName, float value, GameObject gameObject) =>
            _ui.SetMaterialFloatProperty(data, material, propertyName, value, gameObject);
        public void RemoveMaterialFloatProperty(object data, Material material, string propertyName, GameObject gameObject) =>
            _ui.RemoveMaterialFloatProperty(data, material, propertyName, gameObject);

        public bool? GetMaterialKeywordPropertyValueOriginal(object data, Material material, string propertyName, GameObject gameObject) =>
            _ui.GetMaterialKeywordPropertyValueOriginal(data, material, propertyName, gameObject);
        public void SetMaterialKeywordProperty(object data, Material material, string propertyName, bool value, GameObject gameObject) =>
            _ui.SetMaterialKeywordProperty(data, material, propertyName, value, gameObject);
        public void RemoveMaterialKeywordProperty(object data, Material material, string propertyName, GameObject gameObject) =>
            _ui.RemoveMaterialKeywordProperty(data, material, propertyName, gameObject);
    }

    internal sealed class MaterialEditService
    {
        private readonly Func<object, IMaterialEditRepository> _repositoryResolver;

        internal MaterialEditService(IMaterialEditRepository repository)
            : this(data => repository)
        {
        }

        internal MaterialEditService(Func<object, IMaterialEditRepository> repositoryResolver)
        {
            _repositoryResolver = repositoryResolver ?? throw new ArgumentNullException(nameof(repositoryResolver));
        }

        private IMaterialEditRepository GetRepository(object data)
        {
            var repository = _repositoryResolver(data);
            if (repository == null)
                throw new InvalidOperationException("No material edit repository is available for the current object.");
            return repository;
        }

        internal string GetRendererPropertyValueOriginal(object data, Renderer renderer, RendererProperties property, GameObject gameObject) =>
            GetRepository(data).GetRendererPropertyValueOriginal(data, renderer, property, gameObject);

        internal string GetRendererPropertyValue(object data, Renderer renderer, RendererProperties property, GameObject gameObject) =>
            GetRepository(data).GetRendererPropertyValue(data, renderer, property, gameObject);

        internal void SetRendererProperty(object data, Renderer renderer, RendererProperties property, string value, GameObject gameObject) =>
            GetRepository(data).SetRendererProperty(data, renderer, property, value, gameObject);

        internal void RemoveRendererProperty(object data, Renderer renderer, RendererProperties property, GameObject gameObject) =>
            GetRepository(data).RemoveRendererProperty(data, renderer, property, gameObject);

        internal float? GetProjectorPropertyValueOriginal(object data, Projector projector, ProjectorProperties property, GameObject gameObject) =>
            GetRepository(data).GetProjectorPropertyValueOriginal(data, projector, property, gameObject);

        internal float? GetProjectorPropertyValue(object data, Projector projector, ProjectorProperties property, GameObject gameObject) =>
            GetRepository(data).GetProjectorPropertyValue(data, projector, property, gameObject);

        internal void SetProjectorProperty(object data, Projector projector, ProjectorProperties property, float value, GameObject gameObject) =>
            GetRepository(data).SetProjectorProperty(data, projector, property, value, gameObject);

        internal void RemoveProjectorProperty(object data, Projector projector, ProjectorProperties property, GameObject gameObject) =>
            GetRepository(data).RemoveProjectorProperty(data, projector, property, gameObject);

        internal IEnumerable<Projector> GetProjectorList(object data, GameObject gameObject) =>
            GetRepository(data).GetProjectorList(data, gameObject);

        internal void MaterialCopyEdits(object data, Material material, GameObject gameObject) =>
            GetRepository(data).MaterialCopyEdits(data, material, gameObject);

        internal void MaterialPasteEdits(object data, Material material, GameObject gameObject) =>
            GetRepository(data).MaterialPasteEdits(data, material, gameObject);

        internal void MaterialCopyRemove(object data, Material material, GameObject gameObject) =>
            GetRepository(data).MaterialCopyRemove(data, material, gameObject);

        internal string GetMaterialNameOriginal(object data, Renderer renderer, Material material, GameObject gameObject) =>
            GetRepository(data).GetMaterialNameOriginal(data, renderer, material, gameObject);

        internal void SetMaterialName(object data, Renderer renderer, Material material, string value, GameObject gameObject) =>
            GetRepository(data).SetMaterialName(data, renderer, material, value, gameObject);

        internal void RemoveMaterialName(object data, Renderer renderer, Material material, GameObject gameObject) =>
            GetRepository(data).RemoveMaterialName(data, renderer, material, gameObject);

        internal string GetMaterialShaderNameOriginal(object data, Material material, GameObject gameObject) =>
            GetRepository(data).GetMaterialShaderNameOriginal(data, material, gameObject);

        internal void SetMaterialShaderName(object data, Material material, string value, GameObject gameObject) =>
            GetRepository(data).SetMaterialShaderName(data, material, value, gameObject);

        internal void RemoveMaterialShaderName(object data, Material material, GameObject gameObject) =>
            GetRepository(data).RemoveMaterialShaderName(data, material, gameObject);

        internal int? GetMaterialShaderRenderQueueOriginal(object data, Material material, GameObject gameObject) =>
            GetRepository(data).GetMaterialShaderRenderQueueOriginal(data, material, gameObject);

        internal void SetMaterialShaderRenderQueue(object data, Material material, int value, GameObject gameObject) =>
            GetRepository(data).SetMaterialShaderRenderQueue(data, material, value, gameObject);

        internal void RemoveMaterialShaderRenderQueue(object data, Material material, GameObject gameObject) =>
            GetRepository(data).RemoveMaterialShaderRenderQueue(data, material, gameObject);

        internal bool GetMaterialTextureValueOriginal(object data, Material material, string propertyName, GameObject gameObject) =>
            GetRepository(data).GetMaterialTextureValueOriginal(data, material, propertyName, gameObject);

        internal void SetMaterialTexture(object data, Material material, string propertyName, string filePath, GameObject gameObject) =>
            GetRepository(data).SetMaterialTexture(data, material, propertyName, filePath, gameObject);

        internal void RemoveMaterialTexture(object data, Material material, string propertyName, GameObject gameObject) =>
            GetRepository(data).RemoveMaterialTexture(data, material, propertyName, gameObject);

        internal Vector2? GetMaterialTextureOffsetOriginal(object data, Material material, string propertyName, GameObject gameObject) =>
            GetRepository(data).GetMaterialTextureOffsetOriginal(data, material, propertyName, gameObject);

        internal void SetMaterialTextureOffset(object data, Material material, string propertyName, Vector2 value, GameObject gameObject) =>
            GetRepository(data).SetMaterialTextureOffset(data, material, propertyName, value, gameObject);

        internal void RemoveMaterialTextureOffset(object data, Material material, string propertyName, GameObject gameObject) =>
            GetRepository(data).RemoveMaterialTextureOffset(data, material, propertyName, gameObject);

        internal Vector2? GetMaterialTextureScaleOriginal(object data, Material material, string propertyName, GameObject gameObject) =>
            GetRepository(data).GetMaterialTextureScaleOriginal(data, material, propertyName, gameObject);

        internal void SetMaterialTextureScale(object data, Material material, string propertyName, Vector2 value, GameObject gameObject) =>
            GetRepository(data).SetMaterialTextureScale(data, material, propertyName, value, gameObject);

        internal void RemoveMaterialTextureScale(object data, Material material, string propertyName, GameObject gameObject) =>
            GetRepository(data).RemoveMaterialTextureScale(data, material, propertyName, gameObject);

        internal Color? GetMaterialColorPropertyValueOriginal(object data, Material material, string propertyName, GameObject gameObject) =>
            GetRepository(data).GetMaterialColorPropertyValueOriginal(data, material, propertyName, gameObject);

        internal void SetMaterialColorProperty(object data, Material material, string propertyName, Color value, GameObject gameObject) =>
            GetRepository(data).SetMaterialColorProperty(data, material, propertyName, value, gameObject);

        internal void RemoveMaterialColorProperty(object data, Material material, string propertyName, GameObject gameObject) =>
            GetRepository(data).RemoveMaterialColorProperty(data, material, propertyName, gameObject);

        internal float? GetMaterialFloatPropertyValueOriginal(object data, Material material, string propertyName, GameObject gameObject) =>
            GetRepository(data).GetMaterialFloatPropertyValueOriginal(data, material, propertyName, gameObject);

        internal void SetMaterialFloatProperty(object data, Material material, string propertyName, float value, GameObject gameObject) =>
            GetRepository(data).SetMaterialFloatProperty(data, material, propertyName, value, gameObject);

        internal void RemoveMaterialFloatProperty(object data, Material material, string propertyName, GameObject gameObject) =>
            GetRepository(data).RemoveMaterialFloatProperty(data, material, propertyName, gameObject);

        internal bool? GetMaterialKeywordPropertyValueOriginal(object data, Material material, string propertyName, GameObject gameObject) =>
            GetRepository(data).GetMaterialKeywordPropertyValueOriginal(data, material, propertyName, gameObject);

        internal void SetMaterialKeywordProperty(object data, Material material, string propertyName, bool value, GameObject gameObject) =>
            GetRepository(data).SetMaterialKeywordProperty(data, material, propertyName, value, gameObject);

        internal void RemoveMaterialKeywordProperty(object data, Material material, string propertyName, GameObject gameObject) =>
            GetRepository(data).RemoveMaterialKeywordProperty(data, material, propertyName, gameObject);
    }
}
