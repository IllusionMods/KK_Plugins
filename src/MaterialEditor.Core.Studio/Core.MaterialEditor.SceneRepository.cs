using System;
using System.Collections.Generic;
using MaterialEditorAPI;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;

namespace KK_Plugins.MaterialEditor
{
    internal sealed class SceneMaterialEditRepository : IMaterialEditRepository
    {
        private readonly Func<SceneController> _controllerResolver;

        internal SceneMaterialEditRepository(Func<SceneController> controllerResolver)
        {
            _controllerResolver = controllerResolver ?? throw new ArgumentNullException(nameof(controllerResolver));
        }

        private SceneController GetController()
        {
            var controller = _controllerResolver();
            if (controller == null)
                throw new InvalidOperationException("No scene Material Editor controller is available.");
            return controller;
        }

        private static int GetObjectId(object data) => (int)data;

        public string GetRendererPropertyValueOriginal(object data, Renderer renderer, RendererProperties property, GameObject gameObject) =>
            GetController().GetRendererPropertyValueOriginal(GetObjectId(data), renderer, property);

        public string GetRendererPropertyValue(object data, Renderer renderer, RendererProperties property, GameObject gameObject) =>
            GetController().GetRendererPropertyValue(GetObjectId(data), renderer, property);

        public void SetRendererProperty(object data, Renderer renderer, RendererProperties property, string value, GameObject gameObject) =>
            GetController().SetRendererProperty(GetObjectId(data), renderer, property, value);

        public void RemoveRendererProperty(object data, Renderer renderer, RendererProperties property, GameObject gameObject) =>
            GetController().RemoveRendererProperty(GetObjectId(data), renderer, property);

        public float? GetProjectorPropertyValueOriginal(object data, Projector projector, ProjectorProperties property, GameObject gameObject) =>
            GetController().GetProjectorPropertyValueOriginal(GetObjectId(data), projector, property);

        public float? GetProjectorPropertyValue(object data, Projector projector, ProjectorProperties property, GameObject gameObject) =>
            GetController().GetProjectorPropertyValue(GetObjectId(data), projector, property);

        public void SetProjectorProperty(object data, Projector projector, ProjectorProperties property, float value, GameObject gameObject) =>
            GetController().SetProjectorProperty(GetObjectId(data), projector, property, value);

        public void RemoveProjectorProperty(object data, Projector projector, ProjectorProperties property, GameObject gameObject) =>
            GetController().RemoveProjectorProperty(GetObjectId(data), projector, property);

        public IEnumerable<Projector> GetProjectorList(object data, GameObject gameObject) =>
            GetController().GetProjectorList(gameObject);

        public void MaterialCopyEdits(object data, Material material, GameObject gameObject) =>
            GetController().MaterialCopyEdits(GetObjectId(data), material);

        public void MaterialPasteEdits(object data, Material material, GameObject gameObject) =>
            GetController().MaterialPasteEdits(GetObjectId(data), material);

        public void MaterialCopyRemove(object data, Material material, GameObject gameObject) =>
            GetController().MaterialCopyRemove(GetObjectId(data), material, gameObject);

        public string GetMaterialNameOriginal(object data, Renderer renderer, Material material, GameObject gameObject) =>
            GetController().GetMaterialNamePropertyValueOriginal(GetObjectId(data), renderer, material);

        public void SetMaterialName(object data, Renderer renderer, Material material, string value, GameObject gameObject) =>
            GetController().SetMaterialNameProperty(GetObjectId(data), renderer, material, value);

        public void RemoveMaterialName(object data, Renderer renderer, Material material, GameObject gameObject) =>
            GetController().RemoveMaterialNameProperty(GetObjectId(data), renderer, material);

        public string GetMaterialShaderNameOriginal(object data, Material material, GameObject gameObject) =>
            GetController().GetMaterialShaderOriginal(GetObjectId(data), material);

        public void SetMaterialShaderName(object data, Material material, string value, GameObject gameObject) =>
            GetController().SetMaterialShader(GetObjectId(data), material, value);

        public void RemoveMaterialShaderName(object data, Material material, GameObject gameObject) =>
            GetController().RemoveMaterialShader(GetObjectId(data), material);

        public int? GetMaterialShaderRenderQueueOriginal(object data, Material material, GameObject gameObject) =>
            GetController().GetMaterialShaderRenderQueueOriginal(GetObjectId(data), material);

        public void SetMaterialShaderRenderQueue(object data, Material material, int value, GameObject gameObject) =>
            GetController().SetMaterialShaderRenderQueue(GetObjectId(data), material, value);

        public void RemoveMaterialShaderRenderQueue(object data, Material material, GameObject gameObject) =>
            GetController().RemoveMaterialShaderRenderQueue(GetObjectId(data), material);

        public bool GetMaterialTextureValueOriginal(object data, Material material, string propertyName, GameObject gameObject) =>
            GetController().GetMaterialTextureOriginal(GetObjectId(data), material, propertyName);

        public void SetMaterialTexture(object data, Material material, string propertyName, string filePath, GameObject gameObject) =>
            GetController().SetMaterialTextureFromFile(GetObjectId(data), material, propertyName, filePath, true);

        public void RemoveMaterialTexture(object data, Material material, string propertyName, GameObject gameObject) =>
            GetController().RemoveMaterialTexture(GetObjectId(data), material, propertyName);

        public Vector2? GetMaterialTextureOffsetOriginal(object data, Material material, string propertyName, GameObject gameObject) =>
            GetController().GetMaterialTextureOffsetOriginal(GetObjectId(data), material, propertyName);

        public void SetMaterialTextureOffset(object data, Material material, string propertyName, Vector2 value, GameObject gameObject) =>
            GetController().SetMaterialTextureOffset(GetObjectId(data), material, propertyName, value);

        public void RemoveMaterialTextureOffset(object data, Material material, string propertyName, GameObject gameObject) =>
            GetController().RemoveMaterialTextureOffset(GetObjectId(data), material, propertyName);

        public Vector2? GetMaterialTextureScaleOriginal(object data, Material material, string propertyName, GameObject gameObject) =>
            GetController().GetMaterialTextureScaleOriginal(GetObjectId(data), material, propertyName);

        public void SetMaterialTextureScale(object data, Material material, string propertyName, Vector2 value, GameObject gameObject) =>
            GetController().SetMaterialTextureScale(GetObjectId(data), material, propertyName, value);

        public void RemoveMaterialTextureScale(object data, Material material, string propertyName, GameObject gameObject) =>
            GetController().RemoveMaterialTextureScale(GetObjectId(data), material, propertyName);

        public Color? GetMaterialColorPropertyValueOriginal(object data, Material material, string propertyName, GameObject gameObject) =>
            GetController().GetMaterialColorPropertyValueOriginal(GetObjectId(data), material, propertyName);

        public void SetMaterialColorProperty(object data, Material material, string propertyName, Color value, GameObject gameObject) =>
            GetController().SetMaterialColorProperty(GetObjectId(data), material, propertyName, value);

        public void RemoveMaterialColorProperty(object data, Material material, string propertyName, GameObject gameObject) =>
            GetController().RemoveMaterialColorProperty(GetObjectId(data), material, propertyName);

        public float? GetMaterialFloatPropertyValueOriginal(object data, Material material, string propertyName, GameObject gameObject) =>
            GetController().GetMaterialFloatPropertyValueOriginal(GetObjectId(data), material, propertyName);

        public void SetMaterialFloatProperty(object data, Material material, string propertyName, float value, GameObject gameObject) =>
            GetController().SetMaterialFloatProperty(GetObjectId(data), material, propertyName, value);

        public void RemoveMaterialFloatProperty(object data, Material material, string propertyName, GameObject gameObject) =>
            GetController().RemoveMaterialFloatProperty(GetObjectId(data), material, propertyName);

        public bool? GetMaterialKeywordPropertyValueOriginal(object data, Material material, string propertyName, GameObject gameObject) =>
            GetController().GetMaterialKeywordPropertyValueOriginal(GetObjectId(data), material, propertyName);

        public void SetMaterialKeywordProperty(object data, Material material, string propertyName, bool value, GameObject gameObject) =>
            GetController().SetMaterialKeywordProperty(GetObjectId(data), material, propertyName, value);

        public void RemoveMaterialKeywordProperty(object data, Material material, string propertyName, GameObject gameObject) =>
            GetController().RemoveMaterialKeywordProperty(GetObjectId(data), material, propertyName);
    }
}
