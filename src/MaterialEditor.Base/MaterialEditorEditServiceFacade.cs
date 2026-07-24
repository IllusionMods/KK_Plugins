using System.Collections.Generic;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;

namespace MaterialEditorAPI
{
    /// <summary>
    /// Stable, UI-independent facade over Material Editor edit and persistence operations.
    /// Instances are bound to one root object and its opaque storage context.
    /// </summary>
    public sealed class MaterialEditorEditService
    {
        private readonly MaterialEditService _service;

        internal MaterialEditorEditService(
            MaterialEditService service,
            GameObject gameObject,
            object data)
        {
            _service = service;
            GameObject = gameObject;
            Data = data;
        }

        /// <summary>Root object to which operations are applied.</summary>
        public GameObject GameObject { get; }

        /// <summary>Opaque Material Editor persistence context.</summary>
        public object Data { get; }

        /// <summary>Get the current persisted renderer property value.</summary>
        public string GetRendererProperty(Renderer renderer, RendererProperties property) =>
            _service.GetRendererPropertyValue(Data, renderer, property, GameObject);

        /// <summary>Get the original renderer property value.</summary>
        public string GetOriginalRendererProperty(Renderer renderer, RendererProperties property) =>
            _service.GetRendererPropertyValueOriginal(Data, renderer, property, GameObject);

        /// <summary>Set and persist a renderer property.</summary>
        public void SetRendererProperty(Renderer renderer, RendererProperties property, string value) =>
            _service.SetRendererProperty(Data, renderer, property, value, GameObject);

        /// <summary>Remove the persisted renderer property override.</summary>
        public void ResetRendererProperty(Renderer renderer, RendererProperties property) =>
            _service.RemoveRendererProperty(Data, renderer, property, GameObject);

        /// <summary>Get projectors associated with the current target.</summary>
        public IEnumerable<Projector> GetProjectors() =>
            _service.GetProjectorList(Data, GameObject);

        /// <summary>Get the current persisted projector property value.</summary>
        public float? GetProjectorProperty(Projector projector, ProjectorProperties property) =>
            _service.GetProjectorPropertyValue(Data, projector, property, GameObject);

        /// <summary>Get the original projector property value.</summary>
        public float? GetOriginalProjectorProperty(Projector projector, ProjectorProperties property) =>
            _service.GetProjectorPropertyValueOriginal(Data, projector, property, GameObject);

        /// <summary>Set and persist a projector property.</summary>
        public void SetProjectorProperty(Projector projector, ProjectorProperties property, float value) =>
            _service.SetProjectorProperty(Data, projector, property, value, GameObject);

        /// <summary>Remove the persisted projector property override.</summary>
        public void ResetProjectorProperty(Projector projector, ProjectorProperties property) =>
            _service.RemoveProjectorProperty(Data, projector, property, GameObject);

        /// <summary>Copy all persisted edits from a material.</summary>
        public void CopyMaterialEdits(Material material) =>
            _service.MaterialCopyEdits(Data, material, GameObject);

        /// <summary>Paste copied edits onto a material.</summary>
        public void PasteMaterialEdits(Material material) =>
            _service.MaterialPasteEdits(Data, material, GameObject);

        /// <summary>Copy a material or remove a Material Editor copy.</summary>
        public void CopyOrRemoveMaterial(Material material) =>
            _service.MaterialCopyRemove(Data, material, GameObject);

        /// <summary>Get the original formatted material name.</summary>
        public string GetOriginalMaterialName(Renderer renderer, Material material) =>
            _service.GetMaterialNameOriginal(Data, renderer, material, GameObject);

        /// <summary>Rename and persist a material name.</summary>
        public void SetMaterialName(Renderer renderer, Material material, string value) =>
            _service.SetMaterialName(Data, renderer, material, value, GameObject);

        /// <summary>Remove the persisted material name override.</summary>
        public void ResetMaterialName(Renderer renderer, Material material) =>
            _service.RemoveMaterialName(Data, renderer, material, GameObject);

        /// <summary>Get the original shader name.</summary>
        public string GetOriginalShader(Material material) =>
            _service.GetMaterialShaderNameOriginal(Data, material, GameObject);

        /// <summary>Set and persist a material shader.</summary>
        public void SetShader(Material material, string shaderName) =>
            _service.SetMaterialShaderName(Data, material, shaderName, GameObject);

        /// <summary>Remove the persisted shader override.</summary>
        public void ResetShader(Material material) =>
            _service.RemoveMaterialShaderName(Data, material, GameObject);

        /// <summary>Get the original material render queue.</summary>
        public int? GetOriginalRenderQueue(Material material) =>
            _service.GetMaterialShaderRenderQueueOriginal(Data, material, GameObject);

        /// <summary>Set and persist a material render queue.</summary>
        public void SetRenderQueue(Material material, int value) =>
            _service.SetMaterialShaderRenderQueue(Data, material, value, GameObject);

        /// <summary>Remove the persisted render queue override.</summary>
        public void ResetRenderQueue(Material material) =>
            _service.RemoveMaterialShaderRenderQueue(Data, material, GameObject);

        /// <summary>Check whether a material texture is in its original state.</summary>
        public bool IsTextureOriginal(Material material, string propertyName) =>
            _service.GetMaterialTextureValueOriginal(Data, material, propertyName, GameObject);

        /// <summary>Import and persist a material texture from a file.</summary>
        public void SetTextureFromFile(Material material, string propertyName, string filePath) =>
            _service.SetMaterialTexture(Data, material, propertyName, filePath, GameObject);

        /// <summary>Remove the persisted material texture override.</summary>
        public void ResetTexture(Material material, string propertyName) =>
            _service.RemoveMaterialTexture(Data, material, propertyName, GameObject);

        /// <summary>Get the original material texture offset.</summary>
        public Vector2? GetOriginalTextureOffset(Material material, string propertyName) =>
            _service.GetMaterialTextureOffsetOriginal(Data, material, propertyName, GameObject);

        /// <summary>Set and persist a material texture offset.</summary>
        public void SetTextureOffset(Material material, string propertyName, Vector2 value) =>
            _service.SetMaterialTextureOffset(Data, material, propertyName, value, GameObject);

        /// <summary>Remove the persisted material texture offset override.</summary>
        public void ResetTextureOffset(Material material, string propertyName) =>
            _service.RemoveMaterialTextureOffset(Data, material, propertyName, GameObject);

        /// <summary>Get the original material texture scale.</summary>
        public Vector2? GetOriginalTextureScale(Material material, string propertyName) =>
            _service.GetMaterialTextureScaleOriginal(Data, material, propertyName, GameObject);

        /// <summary>Set and persist a material texture scale.</summary>
        public void SetTextureScale(Material material, string propertyName, Vector2 value) =>
            _service.SetMaterialTextureScale(Data, material, propertyName, value, GameObject);

        /// <summary>Remove the persisted material texture scale override.</summary>
        public void ResetTextureScale(Material material, string propertyName) =>
            _service.RemoveMaterialTextureScale(Data, material, propertyName, GameObject);

        /// <summary>Get the original material color property.</summary>
        public Color? GetOriginalColor(Material material, string propertyName) =>
            _service.GetMaterialColorPropertyValueOriginal(Data, material, propertyName, GameObject);

        /// <summary>Set and persist a material color property.</summary>
        public void SetColor(Material material, string propertyName, Color value) =>
            _service.SetMaterialColorProperty(Data, material, propertyName, value, GameObject);

        /// <summary>Remove the persisted material color override.</summary>
        public void ResetColor(Material material, string propertyName) =>
            _service.RemoveMaterialColorProperty(Data, material, propertyName, GameObject);

        /// <summary>Get the original material float property.</summary>
        public float? GetOriginalFloat(Material material, string propertyName) =>
            _service.GetMaterialFloatPropertyValueOriginal(Data, material, propertyName, GameObject);

        /// <summary>Set and persist a material float property.</summary>
        public void SetFloat(Material material, string propertyName, float value) =>
            _service.SetMaterialFloatProperty(Data, material, propertyName, value, GameObject);

        /// <summary>Remove the persisted material float override.</summary>
        public void ResetFloat(Material material, string propertyName) =>
            _service.RemoveMaterialFloatProperty(Data, material, propertyName, GameObject);

        /// <summary>Get the original material keyword state.</summary>
        public bool? GetOriginalBoolean(Material material, string propertyName) =>
            _service.GetMaterialKeywordPropertyValueOriginal(Data, material, propertyName, GameObject);

        /// <summary>Set and persist a material keyword state.</summary>
        public void SetBoolean(Material material, string propertyName, bool value) =>
            _service.SetMaterialKeywordProperty(Data, material, propertyName, value, GameObject);

        /// <summary>Remove the persisted material keyword override.</summary>
        public void ResetBoolean(Material material, string propertyName) =>
            _service.RemoveMaterialKeywordProperty(Data, material, propertyName, GameObject);
    }
}
