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

}
