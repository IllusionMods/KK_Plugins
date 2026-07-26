using System;
using System.Collections.Generic;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;

namespace MaterialEditorAPI
{
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

}
