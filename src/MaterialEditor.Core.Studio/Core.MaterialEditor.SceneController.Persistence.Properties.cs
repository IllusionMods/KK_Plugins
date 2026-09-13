using System.Collections.Generic;
using KKAPI.Utilities;
using System.Linq;
using KKAPI.Studio.SaveLoad;
using MaterialEditorAPI;
using Studio;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;

namespace KK_Plugins.MaterialEditor
{
    public partial class SceneController
    {
        private sealed class SceneLoadContext
        {
            internal MaterialEditLoadContext Data;
            internal SceneOperationKind Operation;
            internal ReadOnlyDictionary<int, ObjectCtrlInfo> Items;
        }

        private void LoadSceneMaterialCopyList(SceneLoadContext context)
        {
            context.Data.Read<MaterialCopy>(nameof(MaterialCopyList), loadedProperty =>
            {
                GameObject go = ExtractGameObject(context.Items, loadedProperty.ID, out var objID);
                if (go != null)
                {
                    CopyMaterial(go, loadedProperty.MaterialName, loadedProperty.MaterialCopyName);
                    if (MaterialCopyList.Any(x => x.ID == objID && x.MaterialName == loadedProperty.MaterialName && x.MaterialCopyName == loadedProperty.MaterialCopyName))
                        return;
                    MaterialCopyList.Add(new MaterialCopy(objID, loadedProperty.MaterialName, loadedProperty.MaterialCopyName));
                }
            });
        }

        private void LoadSceneMaterialNamePropertyList(SceneLoadContext context)
        {
            context.Data.Read<MaterialNameProperty>(nameof(MaterialNamePropertyList), loadedProperty =>
            {
                GameObject go = ExtractGameObject(context.Items, loadedProperty.ID, out var objID);
                if (go != null)
                    if (MaterialAPI.SetName(go, loadedProperty.Renderer, loadedProperty.MaterialName, loadedProperty.Value))
                        MaterialNamePropertyList.Add(new MaterialNameProperty(objID, loadedProperty.Renderer, loadedProperty.MaterialName, loadedProperty.Value));
                    else
                        MaterialEditorPlugin.Logger.LogMessage($"Could not rename material ({loadedProperty.MaterialName}) of renderer ({loadedProperty.Renderer}) to ({loadedProperty.Value}) on load!");
            });
        }

        private void LoadSceneMaterialShaderList(SceneLoadContext context)
        {
            context.Data.Read<MaterialShader>(nameof(MaterialShaderList), loadedProperty =>
            {
                GameObject go = ExtractGameObject(context.Items, loadedProperty.ID, out var objID);
                if (go != null)
                {
                    bool setShader = SetShader(go, loadedProperty.MaterialName, loadedProperty.ShaderName);
                    bool setRenderQueue = SetRenderQueue(go, loadedProperty.MaterialName, loadedProperty.RenderQueue);
                    if (setShader || setRenderQueue)
                        MaterialShaderList.Add(new MaterialShader(objID, loadedProperty.MaterialName, loadedProperty.ShaderName, loadedProperty.ShaderNameOriginal, loadedProperty.RenderQueue, loadedProperty.RenderQueueOriginal));
                }
            });
        }

        private void LoadSceneRendererPropertyList(SceneLoadContext context)
        {
            context.Data.Read<RendererProperty>(nameof(RendererPropertyList), loadedProperty =>
            {
#if KK
                if (loadedProperty.Property == RendererProperties.UpdateWhenOffscreen) return;
#endif
                GameObject go = ExtractGameObject(context.Items, loadedProperty.ID, out var objID);
                if (go != null)
                    if (MaterialAPI.SetRendererProperty(go, loadedProperty.RendererName, loadedProperty.Property, int.Parse(loadedProperty.Value)))
                        RendererPropertyList.Add(new RendererProperty(objID, loadedProperty.RendererName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));
            });
        }

        private void LoadSceneProjectorPropertyList(SceneLoadContext context)
        {
            context.Data.Read<ProjectorProperty>(nameof(ProjectorPropertyList), loadedProperty =>
            {
                GameObject go = ExtractGameObject(context.Items, loadedProperty.ID, out var objID);
                if (go != null)
                    if (MaterialAPI.SetProjectorProperty(go, loadedProperty.ProjectorName, loadedProperty.Property, float.Parse(loadedProperty.Value)))
                        ProjectorPropertyList.Add(new ProjectorProperty(objID, loadedProperty.ProjectorName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));
            });
        }

        private void LoadSceneMaterialFloatPropertyList(SceneLoadContext context)
        {
            context.Data.Read<MaterialFloatProperty>(nameof(MaterialFloatPropertyList), loadedProperty =>
            {
                GameObject go = ExtractGameObject(context.Items, loadedProperty.ID, out var objID);
                if (go != null)
                    if (SetFloat(go, loadedProperty.MaterialName, loadedProperty.Property, float.Parse(loadedProperty.Value)))
                        MaterialFloatPropertyList.Add(new MaterialFloatProperty(objID, loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));
            });
        }

        private void LoadSceneMaterialKeywordPropertyList(SceneLoadContext context)
        {
            context.Data.Read<MaterialKeywordProperty>(nameof(MaterialKeywordPropertyList), loadedProperty =>
            {
                GameObject go = ExtractGameObject(context.Items, loadedProperty.ID, out var objID);
                if (go != null)
                    if (SetKeyword(go, loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value))
                        MaterialKeywordPropertyList.Add(new MaterialKeywordProperty(objID, loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));
            });
        }

        private void LoadSceneMaterialColorPropertyList(SceneLoadContext context)
        {
            context.Data.Read<MaterialColorProperty>(nameof(MaterialColorPropertyList), loadedProperty =>
            {
                GameObject go = ExtractGameObject(context.Items, loadedProperty.ID, out var objID);
                if (go != null)
                {
                    if (IsVectorProperty(go, loadedProperty.MaterialName, loadedProperty.Property))
                    {
                        var value = new Vector4(loadedProperty.Value.r, loadedProperty.Value.g, loadedProperty.Value.b, loadedProperty.Value.a);
                        var valueOriginal = new Vector4(loadedProperty.ValueOriginal.r, loadedProperty.ValueOriginal.g, loadedProperty.ValueOriginal.b, loadedProperty.ValueOriginal.a);
                        if (value != valueOriginal
                            && SetVector(go, loadedProperty.MaterialName, loadedProperty.Property, value)
                            && !MaterialVectorPropertyList.Any(x => x.ID == objID && x.MaterialName == loadedProperty.MaterialName && x.Property == loadedProperty.Property))
                            MaterialVectorPropertyList.Add(new MaterialVectorProperty(objID, loadedProperty.MaterialName, loadedProperty.Property, value, valueOriginal));
                    }
                    else if (SetColor(go, loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value))
                        MaterialColorPropertyList.Add(new MaterialColorProperty(objID, loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));
                }
            });
        }

        private void LoadSceneMaterialVectorPropertyList(SceneLoadContext context)
        {
            context.Data.Read<MaterialVectorProperty>(nameof(MaterialVectorPropertyList), loadedProperty =>
            {
                GameObject go = ExtractGameObject(context.Items, loadedProperty.ID, out var objID);
                if (go != null)
                {
                    // Native Vector data takes precedence when both representations exist.
                    MaterialColorPropertyList.RemoveAll(x => x.ID == objID && x.MaterialName == loadedProperty.MaterialName && x.Property == loadedProperty.Property);
                    MaterialVectorPropertyList.RemoveAll(x => x.ID == objID && x.MaterialName == loadedProperty.MaterialName && x.Property == loadedProperty.Property);
                    if (SetVector(go, loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value)
                        && loadedProperty.Value != loadedProperty.ValueOriginal)
                        MaterialVectorPropertyList.Add(new MaterialVectorProperty(objID, loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));
                }
            });
        }

        private void LoadSceneMaterialTexturePropertyList(SceneLoadContext context)
        {
            context.Data.Read<MaterialTextureProperty>(nameof(MaterialTexturePropertyList), loadedProperty =>
            {
                GameObject go = ExtractGameObject(context.Items, loadedProperty.ID, out var objID);
                if (go != null)
                {
                    int? texID = null;
                    if (context.Operation == SceneOperationKind.Import)
                    {
                        if (loadedProperty.TexID != null)
                            texID = context.Data.RemapTexture(loadedProperty.TexID);
                        MEAnimationUtil.RemapTexID(loadedProperty.TexAnimationDef, context.Data.TextureIds);
                    }
                    else
                        texID = loadedProperty.TexID;

                    MaterialTextureProperty newTextureProperty = new MaterialTextureProperty(objID, loadedProperty.MaterialName, loadedProperty.Property, texID, loadedProperty.Offset, loadedProperty.OffsetOriginal, loadedProperty.Scale, loadedProperty.ScaleOriginal, loadedProperty.TexAnimationDef);

                    bool setTex = false;
                    if (newTextureProperty.TexID != null)
                        setTex = SetTextureWithProperty(go, newTextureProperty);

                    bool setOffset = SetTextureOffset(go, newTextureProperty.MaterialName, newTextureProperty.Property, newTextureProperty.Offset);
                    bool setScale = SetTextureScale(go, newTextureProperty.MaterialName, newTextureProperty.Property, newTextureProperty.Scale);

                    if (setTex || setOffset || setScale)
                        MaterialTexturePropertyList.Add(newTextureProperty);
                }
            });
        }

        private void LoadSceneMaterialCubemapPropertyList(SceneLoadContext context)
        {
            context.Data.Read<MaterialCubemapProperty>(nameof(MaterialCubemapPropertyList), loadedProperty =>
            {
                GameObject go = ExtractGameObject(
                    context.Items,
                    loadedProperty.ID,
                    out var objID);
                if (go == null)
                    return;

                int? texID = loadedProperty.TexID;
                if (context.Operation == SceneOperationKind.Import
                    && loadedProperty.TexID.HasValue)
                    texID = context.Data.RemapTexture(loadedProperty.TexID);

                var newCubemapProperty = new MaterialCubemapProperty(
                    objID,
                    loadedProperty.MaterialName,
                    loadedProperty.Property,
                    texID);
                if (newCubemapProperty.TexID.HasValue
                    && SetCubemapWithProperty(go, newCubemapProperty))
                    MaterialCubemapPropertyList.Add(newCubemapProperty);
            });
        }
    }
}
