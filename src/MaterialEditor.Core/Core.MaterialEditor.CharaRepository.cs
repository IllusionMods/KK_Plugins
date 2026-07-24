using System;
using System.Collections.Generic;
using MaterialEditorAPI;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;

namespace KK_Plugins.MaterialEditor
{
    internal sealed class CharaMaterialEditRepository : IMaterialEditRepository
    {
        private readonly Func<GameObject, MaterialEditorCharaController> _controllerResolver;

        internal CharaMaterialEditRepository(Func<GameObject, MaterialEditorCharaController> controllerResolver)
        {
            _controllerResolver = controllerResolver ?? throw new ArgumentNullException(nameof(controllerResolver));
        }

        private static ObjectData GetObjectData(object data) => (ObjectData)data;

        private MaterialEditorCharaController GetController(GameObject gameObject)
        {
            var controller = _controllerResolver(gameObject);
            if (controller == null)
                throw new InvalidOperationException("No character Material Editor controller is available for the current object.");
            return controller;
        }

        public string GetRendererPropertyValueOriginal(object data, Renderer renderer, RendererProperties property, GameObject gameObject)
        {
            var objectData = GetObjectData(data);
            return GetController(gameObject).GetRendererPropertyValueOriginal(objectData.Slot, objectData.ObjectType, renderer, property, gameObject);
        }

        public string GetRendererPropertyValue(object data, Renderer renderer, RendererProperties property, GameObject gameObject)
        {
            var objectData = GetObjectData(data);
            return GetController(gameObject).GetRendererPropertyValue(objectData.Slot, objectData.ObjectType, renderer, property, gameObject);
        }

        public void SetRendererProperty(object data, Renderer renderer, RendererProperties property, string value, GameObject gameObject)
        {
            var objectData = GetObjectData(data);
            GetController(gameObject).SetRendererProperty(objectData.Slot, objectData.ObjectType, renderer, property, value, gameObject);
        }

        public void RemoveRendererProperty(object data, Renderer renderer, RendererProperties property, GameObject gameObject)
        {
            var objectData = GetObjectData(data);
            GetController(gameObject).RemoveRendererProperty(objectData.Slot, objectData.ObjectType, renderer, property, gameObject);
        }

        public float? GetProjectorPropertyValueOriginal(object data, Projector projector, ProjectorProperties property, GameObject gameObject)
        {
            var objectData = GetObjectData(data);
            return GetController(gameObject).GetProjectorPropertyValueOriginal(objectData.Slot, objectData.ObjectType, projector, property, gameObject);
        }

        public float? GetProjectorPropertyValue(object data, Projector projector, ProjectorProperties property, GameObject gameObject)
        {
            var objectData = GetObjectData(data);
            return GetController(gameObject).GetProjectorPropertyValue(objectData.Slot, objectData.ObjectType, projector, property, gameObject);
        }

        public void SetProjectorProperty(object data, Projector projector, ProjectorProperties property, float value, GameObject gameObject)
        {
            var objectData = GetObjectData(data);
            GetController(gameObject).SetProjectorProperty(objectData.Slot, objectData.ObjectType, projector, property, value, gameObject);
        }

        public void RemoveProjectorProperty(object data, Projector projector, ProjectorProperties property, GameObject gameObject)
        {
            var objectData = GetObjectData(data);
            GetController(gameObject).RemoveProjectorProperty(objectData.Slot, objectData.ObjectType, projector, property, gameObject);
        }

        public IEnumerable<Projector> GetProjectorList(object data, GameObject gameObject)
        {
            var objectData = GetObjectData(data);
            return GetController(gameObject).GetProjectorList(objectData.ObjectType, gameObject);
        }

        public void MaterialCopyEdits(object data, Material material, GameObject gameObject)
        {
            var objectData = GetObjectData(data);
            GetController(gameObject).MaterialCopyEdits(objectData.Slot, objectData.ObjectType, material, gameObject);
        }

        public void MaterialPasteEdits(object data, Material material, GameObject gameObject)
        {
            var objectData = GetObjectData(data);
            GetController(gameObject).MaterialPasteEdits(objectData.Slot, objectData.ObjectType, material, gameObject);
        }

        public void MaterialCopyRemove(object data, Material material, GameObject gameObject)
        {
            var objectData = GetObjectData(data);
            GetController(gameObject).MaterialCopyRemove(objectData.Slot, objectData.ObjectType, material, gameObject);
        }

        public string GetMaterialNameOriginal(object data, Renderer renderer, Material material, GameObject gameObject)
        {
            var objectData = GetObjectData(data);
            return GetController(gameObject).GetMaterialNamePropertyValueOriginal(objectData.Slot, objectData.ObjectType, renderer, material, gameObject);
        }

        public void SetMaterialName(object data, Renderer renderer, Material material, string value, GameObject gameObject)
        {
            var objectData = GetObjectData(data);
            GetController(gameObject).SetMaterialNameProperty(objectData.Slot, objectData.ObjectType, renderer, material, value, gameObject);
        }

        public void RemoveMaterialName(object data, Renderer renderer, Material material, GameObject gameObject)
        {
            var objectData = GetObjectData(data);
            GetController(gameObject).RemoveMaterialNameProperty(objectData.Slot, objectData.ObjectType, renderer, material, gameObject);
        }

        public string GetMaterialShaderNameOriginal(object data, Material material, GameObject gameObject)
        {
            var objectData = GetObjectData(data);
            return GetController(gameObject).GetMaterialShaderOriginal(objectData.Slot, objectData.ObjectType, material, gameObject);
        }

        public void SetMaterialShaderName(object data, Material material, string value, GameObject gameObject)
        {
            var objectData = GetObjectData(data);
            GetController(gameObject).SetMaterialShader(objectData.Slot, objectData.ObjectType, material, value, gameObject);
        }

        public void RemoveMaterialShaderName(object data, Material material, GameObject gameObject)
        {
            var objectData = GetObjectData(data);
            GetController(gameObject).RemoveMaterialShader(objectData.Slot, objectData.ObjectType, material, gameObject);
        }

        public int? GetMaterialShaderRenderQueueOriginal(object data, Material material, GameObject gameObject)
        {
            var objectData = GetObjectData(data);
            return GetController(gameObject).GetMaterialShaderRenderQueueOriginal(objectData.Slot, objectData.ObjectType, material, gameObject);
        }

        public void SetMaterialShaderRenderQueue(object data, Material material, int value, GameObject gameObject)
        {
            var objectData = GetObjectData(data);
            GetController(gameObject).SetMaterialShaderRenderQueue(objectData.Slot, objectData.ObjectType, material, value, gameObject);
        }

        public void RemoveMaterialShaderRenderQueue(object data, Material material, GameObject gameObject)
        {
            var objectData = GetObjectData(data);
            GetController(gameObject).RemoveMaterialShaderRenderQueue(objectData.Slot, objectData.ObjectType, material, gameObject);
        }

        public bool GetMaterialTextureValueOriginal(object data, Material material, string propertyName, GameObject gameObject)
        {
            var objectData = GetObjectData(data);
            return GetController(gameObject).GetMaterialTextureOriginal(objectData.Slot, objectData.ObjectType, material, propertyName, gameObject);
        }

        public void SetMaterialTexture(object data, Material material, string propertyName, string filePath, GameObject gameObject)
        {
            var objectData = GetObjectData(data);
            GetController(gameObject).SetMaterialTextureFromFile(objectData.Slot, objectData.ObjectType, material, propertyName, filePath, gameObject, true);
        }

        public void RemoveMaterialTexture(object data, Material material, string propertyName, GameObject gameObject)
        {
            var objectData = GetObjectData(data);
            GetController(gameObject).RemoveMaterialTexture(objectData.Slot, objectData.ObjectType, material, propertyName, gameObject);
        }

        public Vector2? GetMaterialTextureOffsetOriginal(object data, Material material, string propertyName, GameObject gameObject)
        {
            var objectData = GetObjectData(data);
            return GetController(gameObject).GetMaterialTextureOffsetOriginal(objectData.Slot, objectData.ObjectType, material, propertyName, gameObject);
        }

        public void SetMaterialTextureOffset(object data, Material material, string propertyName, Vector2 value, GameObject gameObject)
        {
            var objectData = GetObjectData(data);
            GetController(gameObject).SetMaterialTextureOffset(objectData.Slot, objectData.ObjectType, material, propertyName, value, gameObject);
        }

        public void RemoveMaterialTextureOffset(object data, Material material, string propertyName, GameObject gameObject)
        {
            var objectData = GetObjectData(data);
            GetController(gameObject).RemoveMaterialTextureOffset(objectData.Slot, objectData.ObjectType, material, propertyName, gameObject);
        }

        public Vector2? GetMaterialTextureScaleOriginal(object data, Material material, string propertyName, GameObject gameObject)
        {
            var objectData = GetObjectData(data);
            return GetController(gameObject).GetMaterialTextureScaleOriginal(objectData.Slot, objectData.ObjectType, material, propertyName, gameObject);
        }

        public void SetMaterialTextureScale(object data, Material material, string propertyName, Vector2 value, GameObject gameObject)
        {
            var objectData = GetObjectData(data);
            GetController(gameObject).SetMaterialTextureScale(objectData.Slot, objectData.ObjectType, material, propertyName, value, gameObject);
        }

        public void RemoveMaterialTextureScale(object data, Material material, string propertyName, GameObject gameObject)
        {
            var objectData = GetObjectData(data);
            GetController(gameObject).RemoveMaterialTextureScale(objectData.Slot, objectData.ObjectType, material, propertyName, gameObject);
        }

        public Color? GetMaterialColorPropertyValueOriginal(object data, Material material, string propertyName, GameObject gameObject)
        {
            var objectData = GetObjectData(data);
            return GetController(gameObject).GetMaterialColorPropertyValueOriginal(objectData.Slot, objectData.ObjectType, material, propertyName, gameObject);
        }

        public void SetMaterialColorProperty(object data, Material material, string propertyName, Color value, GameObject gameObject)
        {
            var objectData = GetObjectData(data);
            GetController(gameObject).SetMaterialColorProperty(objectData.Slot, objectData.ObjectType, material, propertyName, value, gameObject);
        }

        public void RemoveMaterialColorProperty(object data, Material material, string propertyName, GameObject gameObject)
        {
            var objectData = GetObjectData(data);
            GetController(gameObject).RemoveMaterialColorProperty(objectData.Slot, objectData.ObjectType, material, propertyName, gameObject);
        }

        public float? GetMaterialFloatPropertyValueOriginal(object data, Material material, string propertyName, GameObject gameObject)
        {
            var objectData = GetObjectData(data);
            return GetController(gameObject).GetMaterialFloatPropertyValueOriginal(objectData.Slot, objectData.ObjectType, material, propertyName, gameObject);
        }

        public void SetMaterialFloatProperty(object data, Material material, string propertyName, float value, GameObject gameObject)
        {
            var objectData = GetObjectData(data);
            GetController(gameObject).SetMaterialFloatProperty(objectData.Slot, objectData.ObjectType, material, propertyName, value, gameObject);
        }

        public void RemoveMaterialFloatProperty(object data, Material material, string propertyName, GameObject gameObject)
        {
            var objectData = GetObjectData(data);
            GetController(gameObject).RemoveMaterialFloatProperty(objectData.Slot, objectData.ObjectType, material, propertyName, gameObject);
        }

        public bool? GetMaterialKeywordPropertyValueOriginal(object data, Material material, string propertyName, GameObject gameObject)
        {
            var objectData = GetObjectData(data);
            return GetController(gameObject).GetMaterialKeywordPropertyValueOriginal(objectData.Slot, objectData.ObjectType, material, propertyName, gameObject);
        }

        public void SetMaterialKeywordProperty(object data, Material material, string propertyName, bool value, GameObject gameObject)
        {
            var objectData = GetObjectData(data);
            GetController(gameObject).SetMaterialKeywordProperty(objectData.Slot, objectData.ObjectType, material, propertyName, value, gameObject);
        }

        public void RemoveMaterialKeywordProperty(object data, Material material, string propertyName, GameObject gameObject)
        {
            var objectData = GetObjectData(data);
            GetController(gameObject).RemoveMaterialKeywordProperty(objectData.Slot, objectData.ObjectType, material, propertyName, gameObject);
        }
    }
}
