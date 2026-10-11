using System;
using System.Collections.Generic;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;

namespace MaterialEditorAPI
{
    internal sealed class MaterialEditService
    {
        private static readonly ProjectorProperties[] ClipboardProjectorProperties =
            (ProjectorProperties[])Enum.GetValues(typeof(ProjectorProperties));
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

        internal void MaterialCopyEdits(object data, Material material, GameObject gameObject)
        {
            MaterialEditorClipboardState.EnsureClipboard();
            GetRepository(data).MaterialCopyEdits(data, material, gameObject);
        }

        internal void MaterialPasteEdits(object data, Material material, GameObject gameObject)
        {
            var clipboard = MaterialEditorPluginBase.CopyData;
            if (clipboard == null)
                return;
            using (new MaterialEditorClipboardPasteLease(
                       clipboard,
                       false))
            {
                GetRepository(data).MaterialPasteEdits(data, material, gameObject);
            }
        }

        internal void MaterialCopyEdits(
            object data,
            Material material,
            Projector projector,
            GameObject gameObject)
        {
            MaterialEditorClipboardState.EnsureClipboard();
            var repository = GetRepository(data);
            repository.MaterialCopyEdits(data, material, gameObject);

            var clipboard = MaterialEditorClipboardState.EnsureClipboard();
            if (clipboard.ProjectorPropertyList == null)
                clipboard.ProjectorPropertyList = new List<CopyContainer.ProjectorProperty>();
            else
                clipboard.ProjectorPropertyList.Clear();
            if (projector == null)
                return;

            for (var index = 0; index < ClipboardProjectorProperties.Length; index++)
            {
                var property = ClipboardProjectorProperties[index];
                var value = repository.GetProjectorPropertyValue(
                    data,
                    projector,
                    property,
                    gameObject);
                if (value.HasValue)
                {
                    clipboard.ProjectorPropertyList.Add(
                        new CopyContainer.ProjectorProperty(property, value.Value));
                }
            }
        }

        internal void MaterialPasteEdits(
            object data,
            Material material,
            Projector projector,
            GameObject gameObject)
        {
            var clipboard = MaterialEditorPluginBase.CopyData;
            if (clipboard == null)
                return;
            var repository = GetRepository(data);

            using (var paste = new MaterialEditorClipboardPasteLease(
                       clipboard,
                       true))
            {
                repository.MaterialPasteEdits(data, material, gameObject);

                if (projector == null)
                    return;
                for (var index = 0; index < paste.ProjectorEdits.Count; index++)
                {
                    var edit = paste.ProjectorEdits[index];
                    repository.SetProjectorProperty(
                        data,
                        projector,
                        edit.Property,
                        edit.Value,
                        gameObject);
                }
            }
        }

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

        internal bool GetMaterialCubemapValueOriginal(object data, Material material, string propertyName, GameObject gameObject) =>
            GetRepository(data).GetMaterialCubemapValueOriginal(data, material, propertyName, gameObject);

        internal void SetMaterialCubemap(object data, Material material, string propertyName, string filePath, GameObject gameObject) =>
            GetRepository(data).SetMaterialCubemap(data, material, propertyName, filePath, gameObject);

        internal bool SupportsMaterialCubemapDataImport(object data) =>
            GetRepository(data) is IMaterialCubemapDataImportRepository;

        internal bool SetMaterialCubemap(
            object data,
            Material material,
            string propertyName,
            byte[] encodedData,
            MaterialEditorCubemapContentKey contentKey,
            GameObject gameObject)
        {
            var repository = GetRepository(data);
            var dataRepository = repository as IMaterialCubemapDataImportRepository;
            if (dataRepository == null)
                return false;

            return dataRepository.SetMaterialCubemap(
                data,
                material,
                propertyName,
                encodedData,
                contentKey,
                gameObject);
        }

        internal void RemoveMaterialCubemap(object data, Material material, string propertyName, GameObject gameObject) =>
            GetRepository(data).RemoveMaterialCubemap(data, material, propertyName, gameObject);

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

        internal Vector4? GetMaterialVectorPropertyValueOriginal(object data, Material material, string propertyName, GameObject gameObject) =>
            GetRepository(data).GetMaterialVectorPropertyValueOriginal(data, material, propertyName, gameObject);

        internal void SetMaterialVectorProperty(object data, Material material, string propertyName, Vector4 value, GameObject gameObject) =>
            GetRepository(data).SetMaterialVectorProperty(data, material, propertyName, value, gameObject);

        internal void RemoveMaterialVectorProperty(object data, Material material, string propertyName, GameObject gameObject) =>
            GetRepository(data).RemoveMaterialVectorProperty(data, material, propertyName, gameObject);

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
    }}
