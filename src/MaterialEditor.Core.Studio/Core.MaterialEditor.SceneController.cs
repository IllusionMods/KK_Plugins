using ExtensibleSaveFormat;
using KKAPI.Studio;
using KKAPI.Studio.SaveLoad;
using KKAPI.Utilities;
using MaterialEditorAPI;
using MessagePack;
using Studio;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;
using static MaterialEditorAPI.MaterialEditorPluginBase;

namespace KK_Plugins.MaterialEditor
{
    using MEAnimationController = MEAnimationController<SceneController, SceneController.MaterialTextureProperty>;

    /// <summary>
    /// KKAPI scene controller which provides access for getting and setting properties to be saved and loaded with the scene data
    /// </summary>
    public class SceneController : SceneCustomFunctionController
    {
        private readonly List<RendererProperty> RendererPropertyList = new List<RendererProperty>();
        private readonly List<ProjectorProperty> ProjectorPropertyList = new List<ProjectorProperty>();
        private readonly List<MaterialNameProperty> MaterialNamePropertyList = new List<MaterialNameProperty>();
        private readonly List<MaterialFloatProperty> MaterialFloatPropertyList = new List<MaterialFloatProperty>();
        private readonly List<MaterialKeywordProperty> MaterialKeywordPropertyList = new List<MaterialKeywordProperty>();
        private readonly List<MaterialColorProperty> MaterialColorPropertyList = new List<MaterialColorProperty>();
        private readonly List<MaterialTextureProperty> MaterialTexturePropertyList = new List<MaterialTextureProperty>();
        private readonly List<MaterialShader> MaterialShaderList = new List<MaterialShader>();
        private readonly List<MaterialCopy> MaterialCopyList = new List<MaterialCopy>();

        private readonly Dictionary<MaterialTextureProperty, MEAnimationController> AnimationControllerMap = new Dictionary<MaterialTextureProperty, MEAnimationController>();

        private static Dictionary<int, TextureContainer> TextureDictionary = new Dictionary<int, TextureContainer>();

        private static string FileToSet;
        private static string PropertyToSet;
        private static Material MatToSet;
        private static int IDToSet;

        private Dictionary<string, object> AAAAAA;
        private Dictionary<string, object> BBBBBB;

        static SceneController()
        {
            InitAnimationController();
        }

        /// <summary>
        /// Saves data
        /// </summary>
        protected override void OnSceneSave()
        {
            var data = new PluginData { version = 1 };

            PurgeUnusedTextures();

            if (TextureDictionary.Count > 0)
                if ((IsAutoSave() && AutosaveTexturesLocally.Value) || SaveSceneTexturesLocally.Value)
                    MaterialEditorPlugin.SaveLocally(data, nameof(TextureDictionary), TextureDictionary);
                else
                    data.data.Add(nameof(TextureDictionary), MessagePackSerializer.Serialize(TextureDictionary.ToDictionary(pair => pair.Key, pair => pair.Value.Data)));
            else
                data.data.Add(nameof(TextureDictionary), null);

            if (RendererPropertyList.Count > 0)
                data.data.Add(nameof(RendererPropertyList), MessagePackSerializer.Serialize(RendererPropertyList));
            else
                data.data.Add(nameof(RendererPropertyList), null);

            if (ProjectorPropertyList.Count > 0)
                data.data.Add(nameof(ProjectorPropertyList), MessagePackSerializer.Serialize(ProjectorPropertyList));
            else
                data.data.Add(nameof(ProjectorPropertyList), null);

            if (MaterialNamePropertyList.Count > 0)
                data.data.Add(nameof(MaterialNamePropertyList), MessagePackSerializer.Serialize(MaterialNamePropertyList));
            else
                data.data.Add(nameof(MaterialNamePropertyList), null);
            
            if (MaterialFloatPropertyList.Count > 0)
                data.data.Add(nameof(MaterialFloatPropertyList), MessagePackSerializer.Serialize(MaterialFloatPropertyList));
            else
                data.data.Add(nameof(MaterialFloatPropertyList), null);

            if (MaterialKeywordPropertyList.Count > 0)
                data.data.Add(nameof(MaterialKeywordPropertyList), MessagePackSerializer.Serialize(MaterialKeywordPropertyList));
            else
                data.data.Add(nameof(MaterialKeywordPropertyList), null);

            if (MaterialColorPropertyList.Count > 0)
                data.data.Add(nameof(MaterialColorPropertyList), MessagePackSerializer.Serialize(MaterialColorPropertyList));
            else
                data.data.Add(nameof(MaterialColorPropertyList), null);

            if (MaterialTexturePropertyList.Count > 0)
                data.data.Add(nameof(MaterialTexturePropertyList), MessagePackSerializer.Serialize(MaterialTexturePropertyList));
            else
                data.data.Add(nameof(MaterialTexturePropertyList), null);

            if (MaterialShaderList.Count > 0)
                data.data.Add(nameof(MaterialShaderList), MessagePackSerializer.Serialize(MaterialShaderList));
            else
                data.data.Add(nameof(MaterialShaderList), null);

            if (MaterialCopyList.Count > 0)
                data.data.Add(nameof(MaterialCopyList), MessagePackSerializer.Serialize(MaterialCopyList));
            else
                data.data.Add(nameof(MaterialCopyList), null);

            AAAAAA = data.data;

            SetExtendedData(data);
        }

        /// <summary>
        /// Purge unused textures from TextureDictionary
        /// </summary>
        protected void PurgeUnusedTextures()
        {
            if (TextureDictionary.Count <= 0)
                return;

            HashSet<int> unuseds = new HashSet<int>(TextureDictionary.Keys);

            //Remove textures in use
            for (int i = 0; i < MaterialTexturePropertyList.Count; ++i)
            {
                var prop = MaterialTexturePropertyList[i];
                var texID = prop.TexID;
                if (texID.HasValue)
                    unuseds.Remove(texID.Value);

                if (prop.TexAnimationDef != null)
                {
                    var frames = prop.TexAnimationDef.frames;
                    for (int j = 0; j < frames.Length; ++j)
                        unuseds.Remove(frames[j].texID);
                }
            }

            //Remove textures in use
            unuseds.RemoveWhere(texId => TimelineCompatibilityHelper.GetUsedTextureIds().Contains(texId));

            foreach (var texID in unuseds)
            {
                TextureDictionary[texID].Dispose();
                TextureDictionary.Remove(texID);
            }
        }

        /// <summary>
        /// Return GameObject from ObjectCtrlInfo ID
        /// </summary>
        /// <param name="items"></param>
        /// <param name="id"></param>
        /// <returns>GameObject with OCI</returns>
        protected static GameObject ExtractGameObject(ReadOnlyDictionary<int, ObjectCtrlInfo> items, int id, out int objectId)
        {
            if (!items.TryGetValue(id, out ObjectCtrlInfo objectCtrlInfo) || objectCtrlInfo == null || !(objectCtrlInfo is OCIItem ociItem))
            {
                objectId = -1;
                return null;
            }

            objectId = MEStudio.GetObjectID(ociItem);
            return ociItem.objectItem;
        }

        /// <summary>
        /// Loads saved data
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="loadedItems"></param>
        protected override void OnSceneLoad(SceneOperationKind operation, ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems)
        {
            var data = GetExtendedData();

            if (operation == SceneOperationKind.Clear || operation == SceneOperationKind.Load)
            {
                RendererPropertyList.Clear();
                MaterialNamePropertyList.Clear();
                MaterialFloatPropertyList.Clear();
                MaterialKeywordPropertyList.Clear();
                MaterialColorPropertyList.Clear();
                MaterialTexturePropertyList.Clear();
                MaterialShaderList.Clear();
                TextureDictionary.Clear();
                MaterialCopyList.Clear();
                AnimationControllerMap.Clear();
            }

            if (data == null) return;
            if (operation == SceneOperationKind.Clear) return;

            var importDictionary = new Dictionary<int, int>();

            if (operation == SceneOperationKind.Load)
                if (data.data.TryGetValue(nameof(TextureDictionary), out var texDic) && texDic != null)
                    TextureDictionary = MessagePackSerializer.Deserialize<Dictionary<int, byte[]>>((byte[])texDic).ToDictionary(pair => pair.Key, pair => new TextureContainer(pair.Value));

            if (operation == SceneOperationKind.Import)
                if (data.data.TryGetValue(nameof(TextureDictionary), out var texDic) && texDic != null)
                    foreach (var x in MessagePackSerializer.Deserialize<Dictionary<int, byte[]>>((byte[])texDic))
                        importDictionary[x.Key] = SetAndGetTextureID(x.Value);

            if (data.data.TryGetValue(nameof(MaterialCopyList), out var materialCopyData) && materialCopyData != null)
            {
                var properties = MessagePackSerializer.Deserialize<List<MaterialCopy>>((byte[])materialCopyData);
                for (var i = 0; i < properties.Count; i++)
                {
                    var loadedProperty = properties[i];
                    GameObject go = ExtractGameObject(loadedItems, loadedProperty.ID, out var objID);
                    if (go != null)
                    {
                        CopyMaterial(go, loadedProperty.MaterialName, loadedProperty.MaterialCopyName);
                        if (MaterialCopyList.Any(x => x.ID == objID && x.MaterialName == loadedProperty.MaterialName && x.MaterialCopyName == loadedProperty.MaterialCopyName))
                            continue;
                        MaterialCopyList.Add(new MaterialCopy(objID, loadedProperty.MaterialName, loadedProperty.MaterialCopyName));
                    }
                }
            }

            BBBBBB = data.data;

            if (data.data.TryGetValue(nameof(MaterialNamePropertyList), out var materialNameProperties) && materialNameProperties != null)
            {
                var properties = MessagePackSerializer.Deserialize<List<MaterialNameProperty>>((byte[])materialNameProperties);
                for (var i = 0; i < properties.Count; i++)
                {
                    var loadedProperty = properties[i];
                    GameObject go = ExtractGameObject(loadedItems, loadedProperty.ID, out var objID);
                    if (go != null)
                        if (MaterialAPI.SetName(go, loadedProperty.Renderer, loadedProperty.MaterialName, loadedProperty.Value))
                            MaterialNamePropertyList.Add(new MaterialNameProperty(objID, loadedProperty.Renderer, loadedProperty.MaterialName, loadedProperty.Value));
                        else
                            MaterialEditorPlugin.Logger.LogMessage($"Could not rename material ({loadedProperty.MaterialName}) of renderer ({loadedProperty.Renderer}) to ({loadedProperty.Value}) on load!");
                }
            }

            if (data.data.TryGetValue(nameof(MaterialShaderList), out var shaderProperties) && shaderProperties != null)
            {
                var properties = MessagePackSerializer.Deserialize<List<MaterialShader>>((byte[])shaderProperties);
                for (var i = 0; i < properties.Count; i++)
                {
                    var loadedProperty = properties[i];
                    GameObject go = ExtractGameObject(loadedItems, loadedProperty.ID, out var objID);
                    if (go != null)
                    {
                        bool setShader = SetShader(go, loadedProperty.MaterialName, loadedProperty.ShaderName);
                        bool setRenderQueue = SetRenderQueue(go, loadedProperty.MaterialName, loadedProperty.RenderQueue);
                        if (setShader || setRenderQueue)
                            MaterialShaderList.Add(new MaterialShader(objID, loadedProperty.MaterialName, loadedProperty.ShaderName, loadedProperty.ShaderNameOriginal, loadedProperty.RenderQueue, loadedProperty.RenderQueueOriginal));
                    }
                }
            }

            if (data.data.TryGetValue(nameof(RendererPropertyList), out var rendererProperties) && rendererProperties != null)
            {
                var properties = MessagePackSerializer.Deserialize<List<RendererProperty>>((byte[])rendererProperties);
                for (var i = 0; i < properties.Count; i++)
                {
                    var loadedProperty = properties[i];
#if KK
                    if (loadedProperty.Property == RendererProperties.UpdateWhenOffscreen) continue;
#endif
                    GameObject go = ExtractGameObject(loadedItems, loadedProperty.ID, out var objID);
                    if (go != null)
                        if (MaterialAPI.SetRendererProperty(go, loadedProperty.RendererName, loadedProperty.Property, int.Parse(loadedProperty.Value)))
                            RendererPropertyList.Add(new RendererProperty(objID, loadedProperty.RendererName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));
                }
            }

            if (data.data.TryGetValue(nameof(ProjectorPropertyList), out var projectorProperties) && projectorProperties != null)
            {
                var properties = MessagePackSerializer.Deserialize<List<ProjectorProperty>>((byte[])projectorProperties);
                for (var i = 0; i < properties.Count; i++)
                {
                    var loadedProperty = properties[i];
                    GameObject go = ExtractGameObject(loadedItems, loadedProperty.ID, out var objID);
                    if (go != null)
                        if (MaterialAPI.SetProjectorProperty(go, loadedProperty.ProjectorName, loadedProperty.Property, float.Parse(loadedProperty.Value)))
                            ProjectorPropertyList.Add(new ProjectorProperty(objID, loadedProperty.ProjectorName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));
                }
            }

            if (data.data.TryGetValue(nameof(MaterialFloatPropertyList), out var materialFloatProperties) && materialFloatProperties != null)
            {
                var properties = MessagePackSerializer.Deserialize<List<MaterialFloatProperty>>((byte[])materialFloatProperties);
                for (var i = 0; i < properties.Count; i++)
                {
                    var loadedProperty = properties[i];
                    GameObject go = ExtractGameObject(loadedItems, loadedProperty.ID, out var objID);
                    if (go != null)
                        if (SetFloat(go, loadedProperty.MaterialName, loadedProperty.Property, float.Parse(loadedProperty.Value)))
                            MaterialFloatPropertyList.Add(new MaterialFloatProperty(objID, loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));
                }
            }

            if (data.data.TryGetValue(nameof(MaterialKeywordPropertyList), out var materialKeywordProperties) && materialKeywordProperties != null)
            {
                var properties = MessagePackSerializer.Deserialize<List<MaterialKeywordProperty>>((byte[])materialKeywordProperties);
                for (var i = 0; i < properties.Count; i++)
                {
                    var loadedProperty = properties[i];
                    GameObject go = ExtractGameObject(loadedItems, loadedProperty.ID, out var objID);
                    if (go != null)
                        if (SetKeyword(go, loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value))
                            MaterialKeywordPropertyList.Add(new MaterialKeywordProperty(objID, loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));
                }
            }

            if (data.data.TryGetValue(nameof(MaterialColorPropertyList), out var materialColorProperties) && materialColorProperties != null)
            {
                var properties = MessagePackSerializer.Deserialize<List<MaterialColorProperty>>((byte[])materialColorProperties);
                for (var i = 0; i < properties.Count; i++)
                {
                    var loadedProperty = properties[i];
                    GameObject go = ExtractGameObject(loadedItems, loadedProperty.ID, out var objID);
                    if (go != null)
                        if (SetColor(go, loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value))
                            MaterialColorPropertyList.Add(new MaterialColorProperty(objID, loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));
                }
            }

            if (data.data.TryGetValue(nameof(MaterialTexturePropertyList), out var materialTextureProperties) && materialTextureProperties != null)
            {
                var properties = MessagePackSerializer.Deserialize<List<MaterialTextureProperty>>((byte[])materialTextureProperties);
                for (var i = 0; i < properties.Count; i++)
                {
                    var loadedProperty = properties[i];
                    GameObject go = ExtractGameObject(loadedItems, loadedProperty.ID, out var objID);
                    if (go != null)
                    {
                        int? texID = null;
                        if (operation == SceneOperationKind.Import)
                        {
                            if (loadedProperty.TexID != null)
                                texID = importDictionary[(int)loadedProperty.TexID];
                            MEAnimationUtil.RemapTexID(loadedProperty.TexAnimationDef, importDictionary);
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
                }
            }

            if (data.version < 1)
            {
                FixDuplicatesInKeywordList();
            }
        }
        private void FixDuplicatesInKeywordList()
        {
            // Clean up scenes saved in buggy versions of ME that duplicated keyword props on 
            // scene loads, causing massive file sizes. `.First()` should always keep the latest user edit.
            var fixedKeywordList = MaterialKeywordPropertyList
                .GroupBy(d => new { d.ID, d.MaterialName, d.Property })
                .Select(f => f.First())
                .ToArray();
            MaterialKeywordPropertyList.Clear();
            MaterialKeywordPropertyList.Capacity = 0;

            foreach (MaterialKeywordProperty materialKeywordProperty in fixedKeywordList)
            {
                MaterialKeywordPropertyList.Add(materialKeywordProperty);
            }
        }

        /// <summary>
        /// Handles copying data when objects are copied
        /// </summary>
        /// <param name="copiedItems"></param>
        protected override void OnObjectsCopied(ReadOnlyDictionary<int, ObjectCtrlInfo> copiedItems)
        {
            List<RendererProperty> rendererPropertyListNew = new List<RendererProperty>();
            List<ProjectorProperty> projectorPropertyListNew = new List<ProjectorProperty>();
            List<MaterialNameProperty> materialNamePropertyListNew = new List<MaterialNameProperty>();
            List<MaterialFloatProperty> materialFloatPropertyListNew = new List<MaterialFloatProperty>();
            List<MaterialKeywordProperty> materialKeywordPropertyListNew = new List<MaterialKeywordProperty>();
            List<MaterialColorProperty> materialColorPropertyListNew = new List<MaterialColorProperty>();
            List<MaterialTextureProperty> materialTexturePropertyListNew = new List<MaterialTextureProperty>();
            List<MaterialShader> materialShaderListNew = new List<MaterialShader>();
            List<MaterialCopy> materialCopyListNew = new List<MaterialCopy>();

            foreach (var copiedItem in copiedItems)
            {
                if (copiedItem.Value is OCIItem ociItem)
                {
                    for (var i = 0; i < MaterialCopyList.Count; i++)
                    {
                        var loadedProperty = MaterialCopyList[i];
                        if (loadedProperty.ID == copiedItem.Key)
                        {
                            CopyMaterial(ociItem.objectItem, loadedProperty.MaterialName, loadedProperty.MaterialCopyName);
                            materialCopyListNew.Add(new MaterialCopy(copiedItem.Value.GetSceneId(), loadedProperty.MaterialName, loadedProperty.MaterialCopyName));
                        }
                    }

                    for (var i = 0; i < MaterialNamePropertyList.Count; i++)
                    {
                        var loadedProperty = MaterialNamePropertyList[i];
                        if (loadedProperty.ID == copiedItem.Key)
                        {
                            MaterialAPI.SetName(ociItem.objectItem, loadedProperty.Renderer, loadedProperty.MaterialName, loadedProperty.Value);
                            materialNamePropertyListNew.Add(new MaterialNameProperty(copiedItem.Value.GetSceneId(), loadedProperty.Renderer, loadedProperty.MaterialName, loadedProperty.Value));
                        }
                    }

                    for (var i = 0; i < MaterialShaderList.Count; i++)
                    {
                        var loadedProperty = MaterialShaderList[i];
                        if (loadedProperty.ID == copiedItem.Key)
                        {
                            bool setShader = SetShader(ociItem.objectItem, loadedProperty.MaterialName, loadedProperty.ShaderName);
                            bool setRenderQueue = SetRenderQueue(ociItem.objectItem, loadedProperty.MaterialName, loadedProperty.RenderQueue);
                            if (setShader || setRenderQueue)
                                materialShaderListNew.Add(new MaterialShader(copiedItem.Value.GetSceneId(), loadedProperty.MaterialName, loadedProperty.ShaderName, loadedProperty.ShaderNameOriginal, loadedProperty.RenderQueue, loadedProperty.RenderQueueOriginal));
                        }
                    }

                    for (var i = 0; i < RendererPropertyList.Count; i++)
                    {
                        var loadedProperty = RendererPropertyList[i];
                        if (loadedProperty.ID == copiedItem.Key)
                            if (MaterialAPI.SetRendererProperty(ociItem.objectItem, loadedProperty.RendererName, loadedProperty.Property, loadedProperty.Value))
                                rendererPropertyListNew.Add(new RendererProperty(copiedItem.Value.GetSceneId(), loadedProperty.RendererName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));
                    }

                    for (var i = 0; i < ProjectorPropertyList.Count; i++)
                    {
                        var loadedProperty = ProjectorPropertyList[i];
                        if (loadedProperty.ID == copiedItem.Key)
                            if (MaterialAPI.SetProjectorProperty(ociItem.objectItem, loadedProperty.ProjectorName, loadedProperty.Property, float.Parse(loadedProperty.Value)))
                                projectorPropertyListNew.Add(new ProjectorProperty(copiedItem.Value.GetSceneId(), loadedProperty.ProjectorName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));
                    }

                    for (var i = 0; i < MaterialFloatPropertyList.Count; i++)
                    {
                        var loadedProperty = MaterialFloatPropertyList[i];
                        if (loadedProperty.ID == copiedItem.Key)
                            if (SetFloat(ociItem.objectItem, loadedProperty.MaterialName, loadedProperty.Property, float.Parse(loadedProperty.Value)))
                                materialFloatPropertyListNew.Add(new MaterialFloatProperty(copiedItem.Value.GetSceneId(), loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));
                    }

                    for (var i = 0; i < MaterialKeywordPropertyList.Count; i++)
                    {
                        var loadedProperty = MaterialKeywordPropertyList[i];
                        if (loadedProperty.ID == copiedItem.Key)
                            if (SetKeyword(ociItem.objectItem, loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value))
                                materialKeywordPropertyListNew.Add(new MaterialKeywordProperty(copiedItem.Value.GetSceneId(), loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));
                    }

                    for (var i = 0; i < MaterialColorPropertyList.Count; i++)
                    {
                        var loadedProperty = MaterialColorPropertyList[i];
                        if (loadedProperty.ID == copiedItem.Key)
                            if (SetColor(ociItem.objectItem, loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value))
                                materialColorPropertyListNew.Add(new MaterialColorProperty(copiedItem.Value.GetSceneId(), loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));
                    }

                    for (var i = 0; i < MaterialTexturePropertyList.Count; i++)
                    {
                        var loadedProperty = MaterialTexturePropertyList[i];
                        if (loadedProperty.ID == copiedItem.Key)
                        {
                            MaterialTextureProperty newTextureProperty = new MaterialTextureProperty(copiedItem.Value.GetSceneId(), loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.TexID, loadedProperty.Offset, loadedProperty.OffsetOriginal, loadedProperty.Scale, loadedProperty.ScaleOriginal, loadedProperty.TexAnimationDef);

                            bool setTex = false;
                            if (loadedProperty.TexID != null)
                                setTex = SetTextureWithProperty(ociItem.objectItem, newTextureProperty);

                            bool setOffset = SetTextureOffset(ociItem.objectItem, newTextureProperty.MaterialName, newTextureProperty.Property, newTextureProperty.Offset);
                            bool setScale = SetTextureScale(ociItem.objectItem, newTextureProperty.MaterialName, newTextureProperty.Property, newTextureProperty.Scale);

                            if (setTex || setOffset || setScale) materialTexturePropertyListNew.Add(newTextureProperty);
                        }
                    }
                }
            }

            RendererPropertyList.AddRange(rendererPropertyListNew);
            ProjectorPropertyList.AddRange(projectorPropertyListNew);
            MaterialNamePropertyList.AddRange(materialNamePropertyListNew);
            MaterialFloatPropertyList.AddRange(materialFloatPropertyListNew);
            MaterialKeywordPropertyList.AddRange(materialKeywordPropertyListNew);
            MaterialColorPropertyList.AddRange(materialColorPropertyListNew);
            MaterialTexturePropertyList.AddRange(materialTexturePropertyListNew);
            MaterialShaderList.AddRange(materialShaderListNew);
            MaterialCopyList.AddRange(materialCopyListNew);
        }

        private void Update()
        {
            if (MaterialEditorPlugin.PasteEditsHotkey.Value.IsDown())
            {
                if (!CopyData.IsEmpty)
                {
                    int count = 0;
                    TreeNodeObject[] selectNodes = Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNodes;
                    for (int i = 0; i < selectNodes.Length; i++)
                        PasteEditsRecursive(selectNodes[i], ref count);
                    if (count > 0)
                        MaterialEditorPlugin.Logger.LogMessage($"Pasted edits for {count} items");
                }
            }

            if (MaterialEditorPlugin.DisableShadowCastingHotkey.Value.IsDown())
            {
                int count = 0;
                TreeNodeObject[] selectNodes = Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNodes;
                for (int i = 0; i < selectNodes.Length; i++)
                    SetRendererPropertyRecursive(selectNodes[i], RendererProperties.ShadowCastingMode, "0", ref count);
                if (count > 0)
                    MaterialEditorPlugin.Logger.LogMessage($"Disabled ShadowCasting for {count} items");
            }
            else if (MaterialEditorPlugin.EnableShadowCastingHotkey.Value.IsDown())
            {
                int count = 0;
                TreeNodeObject[] selectNodes = Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNodes;
                for (int i = 0; i < selectNodes.Length; i++)
                    SetRendererPropertyRecursive(selectNodes[i], RendererProperties.ShadowCastingMode, "1", ref count);
                if (count > 0)
                    MaterialEditorPlugin.Logger.LogMessage($"Enabled ShadowCasting for {count} items");
            }
            else if (MaterialEditorPlugin.TwoSidedShadowCastingHotkey.Value.IsDown())
            {
                int count = 0;
                TreeNodeObject[] selectNodes = Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNodes;
                for (int i = 0; i < selectNodes.Length; i++)
                    SetRendererPropertyRecursive(selectNodes[i], RendererProperties.ShadowCastingMode, "2", ref count);
                if (count > 0)
                    MaterialEditorPlugin.Logger.LogMessage($"Two Sided ShadowCasting for {count} items");
            }
            else if (MaterialEditorPlugin.ShadowsOnlyShadowCastingHotkey.Value.IsDown())
            {
                int count = 0;
                TreeNodeObject[] selectNodes = Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNodes;
                for (int i = 0; i < selectNodes.Length; i++)
                    SetRendererPropertyRecursive(selectNodes[i], RendererProperties.ShadowCastingMode, "3", ref count);
                if (count > 0)
                    MaterialEditorPlugin.Logger.LogMessage($"Shadows Only ShadowCasting for {count} items");
            }
            else if (MaterialEditorPlugin.ResetShadowCastingHotkey.Value.IsDown())
            {
                int count = 0;
                TreeNodeObject[] selectNodes = Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNodes;
                for (int i = 0; i < selectNodes.Length; i++)
                    SetRendererPropertyRecursive(selectNodes[i], RendererProperties.ShadowCastingMode, "-1", ref count);
                if (count > 0)
                    MaterialEditorPlugin.Logger.LogMessage($"Reset ShadowCasting for {count} items");
            }
            else if (MaterialEditorPlugin.DisableReceiveShadows.Value.IsDown())
            {
                int count = 0;
                TreeNodeObject[] selectNodes = Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNodes;
                for (int i = 0; i < selectNodes.Length; i++)
                    SetRendererPropertyRecursive(selectNodes[i], RendererProperties.ReceiveShadows, "0", ref count);
                if (count > 0)
                    MaterialEditorPlugin.Logger.LogMessage($"Disabled ReceiveShadows for {count} items");
            }
            else if (MaterialEditorPlugin.EnableReceiveShadows.Value.IsDown())
            {
                int count = 0;
                TreeNodeObject[] selectNodes = Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNodes;
                for (int i = 0; i < selectNodes.Length; i++)
                    SetRendererPropertyRecursive(selectNodes[i], RendererProperties.ReceiveShadows, "1", ref count);
                if (count > 0)
                    MaterialEditorPlugin.Logger.LogMessage($"Enabled ReceiveShadows for {count} items");
            }
            else if (MaterialEditorPlugin.ResetReceiveShadows.Value.IsDown())
            {
                int count = 0;
                TreeNodeObject[] selectNodes = Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNodes;
                for (int i = 0; i < selectNodes.Length; i++)
                    SetRendererPropertyRecursive(selectNodes[i], RendererProperties.ReceiveShadows, "-1", ref count);
                if (count > 0)
                    MaterialEditorPlugin.Logger.LogMessage($"Reset ReceiveShadows for {count} items");
            }
            try
            {
                if (!FileToSet.IsNullOrEmpty())
                    SetMaterialTextureFromFile(IDToSet, MatToSet, PropertyToSet, FileToSet);
            }
            catch
            {
                //MaterialEditorPlugin.Logger.Log(BepInEx.Logging.LogLevel.Error | BepInEx.Logging.LogLevel.Message, "Failed to load texture.");
            }
            finally
            {
                FileToSet = null;
                PropertyToSet = null;
                MatToSet = null;
            }

            MEAnimationController.UpdateAnimations(AnimationControllerMap);
        }

        private void SetRendererPropertyRecursive(TreeNodeObject node, RendererProperties property, string value, ref int count)
        {
            if (Studio.Studio.Instance.dicInfo.TryGetValue(node, out ObjectCtrlInfo objectCtrlInfo))
                if (objectCtrlInfo is OCIItem ociItem)
                {
                    count++;
                    for (var index = 0; index < ociItem.arrayRender.Length; index++)
                    {
                        if (value == "-1")
                            RemoveRendererProperty(ociItem.objectInfo.dicKey, ociItem.arrayRender[index], property);
                        else
                            SetRendererProperty(ociItem.objectInfo.dicKey, ociItem.arrayRender[index], property, value);
                    }
                }
                else if (objectCtrlInfo is OCIChar ociChar)
                {
                    count++;
                    var chaControl = ociChar.GetChaControl();
                    var controller = MaterialEditorPlugin.GetCharaController(chaControl);
                    controller.SetRendererPropertyRecursive(property, value, true);
                }
            foreach (var child in node.child)
                SetRendererPropertyRecursive(child, property, value, ref count);
        }

        private void PasteEditsRecursive(TreeNodeObject node, ref int count)
        {
            if (Studio.Studio.Instance.dicInfo.TryGetValue(node, out ObjectCtrlInfo objectCtrlInfo))
                if (objectCtrlInfo is OCIItem ociItem)
                {
                    count++;
                    foreach (var rend in GetRendererList(ociItem.objectItem))
                    {
                        foreach (var mat in GetMaterials(ociItem.objectItem, rend))
                        {
                            MaterialPasteEdits(ociItem.objectInfo.dicKey, mat);
                        }
                    }
                    foreach (var projector in GetProjectorList(ociItem.objectItem))
                        MaterialPasteEdits(ociItem.objectInfo.dicKey, projector.material);
                }
            foreach (var child in node.child)
                PasteEditsRecursive(child, ref count);
        }

        protected override void OnObjectDeleted(ObjectCtrlInfo objectCtrlInfo)
        {
            if (objectCtrlInfo is OCIItem item)
            {
                var id = item.GetSceneId();
                RendererPropertyList.RemoveAll(x => x.ID == id);
                ProjectorPropertyList.RemoveAll(x => x.ID == id);
                MaterialNamePropertyList.RemoveAll(x => x.ID == id);
                MaterialFloatPropertyList.RemoveAll(x => x.ID == id);
                MaterialKeywordPropertyList.RemoveAll(x => x.ID == id);
                MaterialColorPropertyList.RemoveAll(x => x.ID == id);
                MaterialTexturePropertyList.RemoveAll(x => x.ID == id);
                MaterialShaderList.RemoveAll(x => x.ID == id);
                MaterialCopyList.RemoveAll(x => x.ID == id);
                MaterialEditorUI.Visible = false;
            }
            else if (objectCtrlInfo is OCIChar)
                MaterialEditorUI.Visible = false;
            base.OnObjectDeleted(objectCtrlInfo);
            PurgeUnusedAnimation();
        }

        protected override void OnObjectVisibilityToggled(ObjectCtrlInfo objectCtrlInfo, bool visible)
        {
            if (visible && objectCtrlInfo is OCIItem item)
            {
                var id = item.GetSceneId();
                foreach (var property in RendererPropertyList.Where(x => x.ID == id && x.Property == RendererProperties.Enabled))
                {
                    MaterialAPI.SetRendererProperty(GetObjectByID(id), property.RendererName, property.Property, property.Value);
                    // potential recalc of normals, have to test...
                }
            }
            base.OnObjectVisibilityToggled(objectCtrlInfo, visible);
        }

        protected override void OnObjectsSelected(List<ObjectCtrlInfo> objectCtrlInfo)
        {
            if (MaterialEditorUI.Visible)
                MEStudio.Instance.UpdateUI();
            base.OnObjectsSelected(objectCtrlInfo);
        }

        internal void HandleMaterialNameChange(int id, Renderer renderer, Material material, string value, GameObject go)
        {
            value = value.FormatShadingObjectName();

            // Check for an existing material on the renderer by the same name
            // Also check if we're renaming a copied material, and find the actual material being renamed
            Material existing = null;
            Material copiedOriginalMat = null;
            foreach (var rend in GetRendererList(go))
            {
                foreach (var mat in GetMaterials(go, rend))
                {
                    if (mat.NameFormatted() == value)
                    {
                        if (rend == renderer) return;
                        existing = mat;
                    }
                    else if (material.name.Contains(MaterialCopyPostfix) && rend == renderer && mat.NameFormatted() == material.NameFormatted())
                    {
                        copiedOriginalMat = mat;
                    }
                }
            }

            if (existing == null)
            {
                var shader = MaterialShaderList.Where(x => x.ID == id && x.MaterialName == material.NameFormatted()).ToList();
                var textures = MaterialTexturePropertyList.Where(x => x.ID == id && x.MaterialName == material.NameFormatted()).ToList();
                var colors = MaterialColorPropertyList.Where(x => x.ID == id && x.MaterialName == material.NameFormatted()).ToList();
                var floats = MaterialFloatPropertyList.Where(x => x.ID == id && x.MaterialName == material.NameFormatted()).ToList();
                var keywords = MaterialKeywordPropertyList.Where(x => x.ID == id && x.MaterialName == material.NameFormatted()).ToList();
                if (shader.Count == 1) MaterialShaderList.Add(new MaterialShader(id, value, shader[0].ShaderName, shader[0].ShaderNameOriginal, shader[0].RenderQueue, shader[0].RenderQueueOriginal));
                foreach (var tex in textures) MaterialTexturePropertyList.Add(new MaterialTextureProperty(id, value, tex.Property, tex.TexID, tex.Offset, tex.OffsetOriginal, tex.Scale, tex.ScaleOriginal, tex.TexAnimationDef));
                foreach (var col in colors) MaterialColorPropertyList.Add(new MaterialColorProperty(id, value, col.Property, col.Value, col.ValueOriginal));
                foreach (var _float in floats) MaterialFloatPropertyList.Add(new MaterialFloatProperty(id, value, _float.Property, _float.Value, _float.ValueOriginal));
                foreach (var kw in keywords) MaterialKeywordPropertyList.Add(new MaterialKeywordProperty(id, value, kw.Property, kw.Value, kw.ValueOriginal));
            }
            else if (!material.name.Contains(MaterialCopyPostfix))
            {
                material.shader = existing.shader;
                material.shaderKeywords = existing.shaderKeywords;
                material.color = existing.color;
                material.mainTexture = existing.mainTexture;
                material.mainTextureOffset = existing.mainTextureOffset;
                material.mainTextureScale = existing.mainTextureScale;
                material.renderQueue = existing.renderQueue;
            }
            else if (copiedOriginalMat != null)
            {
                copiedOriginalMat.shader = existing.shader;
                copiedOriginalMat.shaderKeywords = existing.shaderKeywords;
                copiedOriginalMat.color = existing.color;
                copiedOriginalMat.mainTexture = existing.mainTexture;
                copiedOriginalMat.mainTextureOffset = existing.mainTextureOffset;
                copiedOriginalMat.mainTextureScale = existing.mainTextureScale;
                copiedOriginalMat.renderQueue = existing.renderQueue;
            }
        }

        /// <summary>
        /// Finds the texture bytes in the dictionary of textures and returns its ID. If not found, adds the texture to the dictionary and returns the ID of the added texture.
        /// </summary>
        internal static int SetAndGetTextureID(byte[] textureBytes)
        {
            int highestID = 0;
            foreach (var tex in TextureDictionary)
                if (tex.Value.Data.SequenceEqualFast(textureBytes))
                    return tex.Key;
                else if (tex.Key > highestID)
                    highestID = tex.Key;

            highestID++;
            TextureDictionary.Add(highestID, new TextureContainer(textureBytes));
            return highestID;
        }


        /// <summary>
        /// Finds the texture in the dictionary of textures by its ID. Returns null if not found.
        /// </summary>
        internal static Texture GetTextureByDictionaryID(int id)
        {
            TextureDictionary.TryGetValue(id, out TextureContainer textureContainer);
            if (textureContainer != null) return textureContainer.Texture;
            return null;
        }

        /// <summary>
        /// Copy any edits for the specified object
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="projector">Projector being modified</param>
        public void MaterialCopyEdits(int id, Material material)
        {
            CopyData.ClearAll();

            for (var i = 0; i < MaterialShaderList.Count; i++)
            {
                var materialShader = MaterialShaderList[i];
                if (materialShader.ID == id && materialShader.MaterialName == material.NameFormatted())
                    CopyData.MaterialShaderList.Add(new CopyContainer.MaterialShader(materialShader.ShaderName, materialShader.RenderQueue));
            }
            for (var i = 0; i < MaterialFloatPropertyList.Count; i++)
            {
                var materialFloatProperty = MaterialFloatPropertyList[i];
                if (materialFloatProperty.ID == id && materialFloatProperty.MaterialName == material.NameFormatted())
                    CopyData.MaterialFloatPropertyList.Add(new CopyContainer.MaterialFloatProperty(materialFloatProperty.Property, float.Parse(materialFloatProperty.Value)));
            }
            for (var i = 0; i < MaterialKeywordPropertyList.Count; i++)
            {
                var materialKeywordProperty = MaterialKeywordPropertyList[i];
                if (materialKeywordProperty.ID == id && materialKeywordProperty.MaterialName == material.NameFormatted())
                    CopyData.MaterialKeywordPropertyList.Add(new CopyContainer.MaterialKeywordProperty(materialKeywordProperty.Property, materialKeywordProperty.Value));
            }
            for (var i = 0; i < MaterialColorPropertyList.Count; i++)
            {
                var materialColorProperty = MaterialColorPropertyList[i];
                if (materialColorProperty.ID == id && materialColorProperty.MaterialName == material.NameFormatted())
                    CopyData.MaterialColorPropertyList.Add(new CopyContainer.MaterialColorProperty(materialColorProperty.Property, materialColorProperty.Value));
            }
            for (var i = 0; i < MaterialTexturePropertyList.Count; i++)
            {
                var materialTextureProperty = MaterialTexturePropertyList[i];
                if (materialTextureProperty.ID == id && materialTextureProperty.MaterialName == material.NameFormatted())
                {
                    if (materialTextureProperty.TexID != null)
                        CopyData.MaterialTexturePropertyList.Add(new CopyContainer.MaterialTextureProperty(materialTextureProperty.Property, TextureDictionary[(int)materialTextureProperty.TexID].Data, materialTextureProperty.Offset, materialTextureProperty.Scale));
                    else
                        CopyData.MaterialTexturePropertyList.Add(new CopyContainer.MaterialTextureProperty(materialTextureProperty.Property, null, materialTextureProperty.Offset, materialTextureProperty.Scale));
                }
            }

            if (GetProjectorList(GetObjectByID(id)).FirstOrDefault(x => x.material == material) != null)
                for (var i = 0; i < ProjectorPropertyList.Count; i++)
                {
                    var projectorProperty = ProjectorPropertyList[i];
                    if (projectorProperty.ID == id)
                        CopyData.ProjectorPropertyList.Add(new CopyContainer.ProjectorProperty(projectorProperty.Property, float.Parse(projectorProperty.Value)));
                }
        }
        /// <summary>
        /// Paste any edits for the specified object
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        /// <param name="projector">Projector being modified</param>
        public void MaterialPasteEdits(int id, Material material, bool setProperty = true)
        {
            for (var i = 0; i < CopyData.MaterialShaderList.Count; i++)
            {
                var materialShader = CopyData.MaterialShaderList[i];
                if (materialShader.ShaderName != null)
                    SetMaterialShader(id, material, materialShader.ShaderName, setProperty);
                if (materialShader.RenderQueue != null)
                    SetMaterialShaderRenderQueue(id, material, (int)materialShader.RenderQueue, setProperty);
            }
            for (var i = 0; i < CopyData.MaterialFloatPropertyList.Count; i++)
            {
                var materialFloatProperty = CopyData.MaterialFloatPropertyList[i];
                if (material.HasProperty($"_{materialFloatProperty.Property}"))
                    SetMaterialFloatProperty(id, material, materialFloatProperty.Property, materialFloatProperty.Value, setProperty);
            }
            for (var i = 0; i < CopyData.MaterialKeywordPropertyList.Count; i++)
            {
                var materialKeywordProperty = CopyData.MaterialKeywordPropertyList[i];
                SetMaterialKeywordProperty(id, material, materialKeywordProperty.Property, materialKeywordProperty.Value, setProperty);
            }
            for (var i = 0; i < CopyData.MaterialColorPropertyList.Count; i++)
            {
                var materialColorProperty = CopyData.MaterialColorPropertyList[i];
                if (material.HasProperty($"_{materialColorProperty.Property}"))
                    SetMaterialColorProperty(id, material, materialColorProperty.Property, materialColorProperty.Value, setProperty);
            }
            for (var i = 0; i < CopyData.MaterialTexturePropertyList.Count; i++)
            {
                var materialTextureProperty = CopyData.MaterialTexturePropertyList[i];
                if (material.HasProperty($"_{materialTextureProperty.Property}"))
                    SetMaterialTexture(id, material, materialTextureProperty.Property, materialTextureProperty.Data);
                if (materialTextureProperty.Offset != null)
                    SetMaterialTextureOffset(id, material, materialTextureProperty.Property, (Vector2)materialTextureProperty.Offset, setProperty);
                if (materialTextureProperty.Scale != null)
                    SetMaterialTextureScale(id, material, materialTextureProperty.Property, (Vector2)materialTextureProperty.Scale, setProperty);
            }

            var projector = GetProjectorList(GetObjectByID(id)).FirstOrDefault(x => x.material == material);
            if (projector != null)
                for (var i = 0; i < CopyData.ProjectorPropertyList.Count; i++)
                {
                    var projectorProperty = CopyData.ProjectorPropertyList[i];
                    SetProjectorProperty(id, projector, projectorProperty.Property, projectorProperty.Value, setProperty);
                }
        }
        public void MaterialCopyRemove(int id, Material material, GameObject go, bool setProperty = true)
        {
            string matName = material.NameFormatted();
            if (matName.Contains(MaterialCopyPostfix))
            {
                MaterialNamePropertyList.RemoveAll(x => x.ID == id && x.Value == material.name);

                RemoveMaterial(go, material);
                MaterialShaderList.RemoveAll(x => x.ID == id && x.MaterialName == matName);
                MaterialFloatPropertyList.RemoveAll(x => x.ID == id && x.MaterialName == matName);
                MaterialKeywordPropertyList.RemoveAll(x => x.ID == id && x.MaterialName == matName);
                MaterialColorPropertyList.RemoveAll(x => x.ID == id && x.MaterialName == matName);
                MaterialTexturePropertyList.RemoveAll(x => x.ID == id && x.MaterialName == matName);
                MaterialCopyList.RemoveAll(x => x.ID == id && x.MaterialCopyName == matName);
            }
            else if (GetMaterialNamePropertyValue(id, GetRendererList(go).FirstOrDefault(x => x.materials.Contains(material)), material) == string.Empty)
            {
                string newMatName = CopyMaterial(go, matName);
                MaterialCopyList.Add(new MaterialCopy(id, matName, newMatName));

                List<MaterialShader> newAccessoryMaterialShaderList = new List<MaterialShader>();
                List<MaterialFloatProperty> newAccessoryMaterialFloatPropertyList = new List<MaterialFloatProperty>();
                List<MaterialKeywordProperty> newAccessoryMaterialKeywordPropertyList = new List<MaterialKeywordProperty>();
                List<MaterialColorProperty> newAccessoryMaterialColorPropertyList = new List<MaterialColorProperty>();
                List<MaterialTextureProperty> newAccessoryMaterialTexturePropertyList = new List<MaterialTextureProperty>();

                foreach (var property in MaterialShaderList.Where(x => x.ID == id && x.MaterialName == matName))
                    newAccessoryMaterialShaderList.Add(new MaterialShader(id, newMatName, property.ShaderName, property.ShaderNameOriginal, property.RenderQueue, property.RenderQueueOriginal));
                foreach (var property in MaterialFloatPropertyList.Where(x => x.ID == id && x.MaterialName == matName))
                    newAccessoryMaterialFloatPropertyList.Add(new MaterialFloatProperty(id, newMatName, property.Property, property.Value, property.ValueOriginal));
                foreach (var property in MaterialKeywordPropertyList.Where(x => x.ID == id && x.MaterialName == matName))
                    newAccessoryMaterialKeywordPropertyList.Add(new MaterialKeywordProperty(id, newMatName, property.Property, property.Value, property.ValueOriginal));
                foreach (var property in MaterialColorPropertyList.Where(x => x.ID == id && x.MaterialName == matName))
                    newAccessoryMaterialColorPropertyList.Add(new MaterialColorProperty(id, newMatName, property.Property, property.Value, property.ValueOriginal));
                foreach (var property in MaterialTexturePropertyList.Where(x => x.ID == id && x.MaterialName == matName))
                    newAccessoryMaterialTexturePropertyList.Add(new MaterialTextureProperty(id, newMatName, property.Property, property.TexID, property.Offset, property.OffsetOriginal, property.Scale, property.ScaleOriginal, property.TexAnimationDef));

                MaterialShaderList.AddRange(newAccessoryMaterialShaderList);
                MaterialFloatPropertyList.AddRange(newAccessoryMaterialFloatPropertyList);
                MaterialKeywordPropertyList.AddRange(newAccessoryMaterialKeywordPropertyList);
                MaterialColorPropertyList.AddRange(newAccessoryMaterialColorPropertyList);
                MaterialTexturePropertyList.AddRange(newAccessoryMaterialTexturePropertyList);
            }
            else
            {
                MaterialEditorPlugin.Logger.LogMessage("Cannot copy renamed materials!");
            }

            PurgeUnusedAnimation();
        }

        /// <summary>
        /// Get the saved renderer property's original value or null if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="projector">Renderer being modified</param>
        /// <param name="property">Property of the renderer</param>
        /// <returns>Saved renderer property's original value</returns>
        public float? GetProjectorPropertyValueOriginal(int id, Projector projector, ProjectorProperties property)
        {
            var valueOriginal = ProjectorPropertyList.FirstOrDefault(x => x.ID == id && x.Property == property && x.ProjectorName == projector.NameFormatted())?.ValueOriginal;
            if (valueOriginal.IsNullOrEmpty())
                return null;
            return float.Parse(valueOriginal);
        }

        /// <summary>
        /// Get the saved projector property value or null if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="projector">Projector being modified</param>
        /// <param name="property">Property of the projector</param>
        /// <returns>Saved projector property value</returns>
        public float? GetProjectorPropertyValue(int id, Projector projector, ProjectorProperties property)
        {
            var valueOriginal = ProjectorPropertyList.FirstOrDefault(x => x.ID == id && x.Property == property && x.ProjectorName == projector.NameFormatted())?.Value;
            if (valueOriginal.IsNullOrEmpty())
                return null;
            return float.Parse(valueOriginal);
        }

        /// <summary>
        /// Remove the saved projector property value if one is saved and optionally also update the projector
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="projector">projector being modified</param>
        /// <param name="property">Property of the projector</param>
        /// <param name="setProperty">Whether to also apply the value to the projector</param>
        public void RemoveProjectorProperty(int id, Projector projector, ProjectorProperties property, bool setProperty = true)
        {
            GameObject go = GetObjectByID(id);
            if (setProperty)
            {
                var original = GetProjectorPropertyValueOriginal(id, projector, property);
                if (original != null)
                {
                    MaterialAPI.SetProjectorProperty(go, projector.NameFormatted(), property, (float)original);
                }
            }

            ProjectorPropertyList.RemoveAll(x => x.ID == id && x.Property == property && x.ProjectorName == projector.NameFormatted());
        }

        public void SetProjectorProperty(int id, Projector projector, ProjectorProperties property, float value, bool setProperty = true)
        {
            GameObject go = GetObjectByID(id);
            var projectorProperty = ProjectorPropertyList.FirstOrDefault(x => x.ID == id && x.Property == property && x.ProjectorName == projector.NameFormatted());
            if (projectorProperty == null)
            {
                string valueOriginal = "";
                if (property == ProjectorProperties.FarClipPlane)
                    valueOriginal = projector.farClipPlane.ToString(CultureInfo.InvariantCulture);
                else if (property == ProjectorProperties.NearClipPlane)
                    valueOriginal = projector.nearClipPlane.ToString(CultureInfo.InvariantCulture);
                else if (property == ProjectorProperties.FieldOfView)
                    valueOriginal = projector.fieldOfView.ToString(CultureInfo.InvariantCulture);
                else if (property == ProjectorProperties.AspectRatio)
                    valueOriginal = projector.aspectRatio.ToString(CultureInfo.InvariantCulture);
                else if (property == ProjectorProperties.Orthographic)
                    valueOriginal = Convert.ToSingle(projector.orthographic).ToString(CultureInfo.InvariantCulture);
                else if (property == ProjectorProperties.OrthographicSize)
                    valueOriginal = projector.orthographicSize.ToString(CultureInfo.InvariantCulture);
                else if (property == ProjectorProperties.IgnoreCharaLayer)
                    valueOriginal = Convert.ToSingle(projector.ignoreLayers == (projector.ignoreLayers | (1 << 10))).ToString(CultureInfo.InvariantCulture);
                else if (property == ProjectorProperties.IgnoreMapLayer)
                    valueOriginal = Convert.ToSingle(projector.ignoreLayers == (projector.ignoreLayers | (1 << 11))).ToString(CultureInfo.InvariantCulture);

                if (valueOriginal != "")
                    ProjectorPropertyList.Add(new ProjectorProperty(id, projector.NameFormatted(), property, value.ToString(CultureInfo.InvariantCulture), valueOriginal));
            }
            else
            {
                if (value.ToString(CultureInfo.InvariantCulture) == projectorProperty.ValueOriginal)
                    RemoveProjectorProperty(id, projector, property, false);
                else
                    projectorProperty.Value = value.ToString(CultureInfo.InvariantCulture);
            }

            if (setProperty)
                MaterialAPI.SetProjectorProperty(go, projector.NameFormatted(), property, value);
        }

        public IEnumerable<Projector> GetProjectorList(GameObject gameObject)
        {
            //Assume the projector component will always be attached to the root object
            //Otherwise no distinction can be made between projectors and editing them will not work properly
            return MaterialAPI.GetProjectorList(gameObject, false);
        }

        #region Set, Get, Remove methods
        /// <summary>
        /// Add a renderer property to be saved and loaded with the scene and optionally also update the renderer.
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="renderer">Renderer being modified</param>
        /// <param name="property">Property of the renderer</param>
        /// <param name="value">Value</param>
        /// <param name="setProperty">Whether to also apply the value to the renderer</param>
        public void SetRendererProperty(int id, Renderer renderer, RendererProperties property, string value, bool setProperty = true)
        {
            GameObject go = GetObjectByID(id);
            var rendererProperty = RendererPropertyList.FirstOrDefault(x => x.ID == id && x.Property == property && x.RendererName == renderer.NameFormatted());
            if (rendererProperty == null)
            {
                string valueOriginal = "";
                if (property == RendererProperties.Enabled)
                    valueOriginal = renderer.enabled ? "1" : "0";
                else if (property == RendererProperties.ReceiveShadows)
                    valueOriginal = renderer.receiveShadows ? "1" : "0";
                else if (property == RendererProperties.ShadowCastingMode)
                    valueOriginal = ((int)renderer.shadowCastingMode).ToString();
                else if (property == RendererProperties.UpdateWhenOffscreen)
                    if (renderer is SkinnedMeshRenderer meshRenderer)
                        valueOriginal = meshRenderer.updateWhenOffscreen ? "1" : "0";
                    else valueOriginal = "0";
                else if (property == RendererProperties.RecalculateNormals)
                    valueOriginal = "0"; // this property cannot be set by default

                if (valueOriginal != "")
                    RendererPropertyList.Add(new RendererProperty(id, renderer.NameFormatted(), property, value, valueOriginal));
            }
            else
            {
                if (value == rendererProperty.ValueOriginal)
                    RemoveRendererProperty(id, renderer, property, false);
                else
                    rendererProperty.Value = value;
            }

            if (setProperty)
                MaterialAPI.SetRendererProperty(go, renderer.NameFormatted(), property, value);
        }

        /// <summary>
        /// Get the saved renderer property value or null if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="renderer">Renderer being modified</param>
        /// <param name="property">Property of the renderer</param>
        /// <returns>Saved renderer property value</returns>
        public string GetRendererPropertyValue(int id, Renderer renderer, RendererProperties property) =>
            RendererPropertyList.FirstOrDefault(x => x.ID == id && x.Property == property && x.RendererName == renderer.NameFormatted())?.Value;
        /// <summary>
        /// Get the saved renderer property's original value or null if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="renderer">Renderer being modified</param>
        /// <param name="property">Property of the renderer</param>
        /// <returns>Saved renderer property's original value</returns>
        public string GetRendererPropertyValueOriginal(int id, Renderer renderer, RendererProperties property) =>
            RendererPropertyList.FirstOrDefault(x => x.ID == id && x.Property == property && x.RendererName == renderer.NameFormatted())?.ValueOriginal;
        /// <summary>
        /// Remove the saved renderer property value if one is saved and optionally also update the renderer
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="renderer">Renderer being modified</param>
        /// <param name="property">Property of the renderer</param>
        /// <param name="setProperty">Whether to also apply the value to the renderer</param>
        public void RemoveRendererProperty(int id, Renderer renderer, RendererProperties property, bool setProperty = true)
        {
            GameObject go = GetObjectByID(id);
            if (setProperty)
            {
                var original = GetRendererPropertyValueOriginal(id, renderer, property);
                if (!original.IsNullOrEmpty())
                    MaterialAPI.SetRendererProperty(go, renderer.NameFormatted(), property, original);
                if (property == RendererProperties.RecalculateNormals)
                    MaterialEditorPlugin.Logger.LogMessage("Save and reload character or change outfits to reset normals.");
            }

            RendererPropertyList.RemoveAll(x => x.ID == id && x.Property == property && x.RendererName == renderer.NameFormatted());
        }

        /// <summary>
        /// Add a name property to be saved and loaded with the scene and optionally also update the materials.
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="renderer">Renderer being modified</param>
        /// <param name="material">Material being renamed</param>
        /// <param name="value">New name for the material</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void SetMaterialNameProperty(int id, Renderer renderer, Material material, string value, bool setProperty = true)
        {
            GameObject go = GetObjectByID(id);
            var materialProperty = MaterialNamePropertyList.FirstOrDefault(x => x.ID == id && x.Renderer == renderer.NameFormatted() && x.Value == material.name);
            if (materialProperty == null)
            {
                MaterialNamePropertyList.Add(new MaterialNameProperty(id, renderer, material, value));
                HandleMaterialNameChange(id, renderer, material, value, go);
            }
            else
            {
                if (value.FormatShadingObjectName() == materialProperty.MaterialName.FormatShadingObjectName())
                    RemoveMaterialNameProperty(id, renderer, material, false);
                else
                {
                    materialProperty.Value = value;
                    HandleMaterialNameChange(id, renderer, material, value, go);
                }
            }

            if (setProperty)
                MaterialAPI.SetName(go, renderer.NameFormatted(), material.name, value);
        }
        /// <summary>
        /// Get the saved material name or an empty string if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="renderer">Renderer that the material belongs to</param>
        /// <param name="material">Material to check for existing name property</param>
        /// <returns>Saved material name or empty string if none is saved</returns>
        public string GetMaterialNamePropertyValue(int id, Renderer renderer, Material material)
        {
            return MaterialNamePropertyList.FirstOrDefault(x => x.ID == id && x.Renderer == renderer?.NameFormatted() && x.Value == material?.name)?.Value ?? string.Empty;
        }
        /// <summary>
        /// Get the original material name or an empty string if the material isn't renamed
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="renderer">Renderer that the material belongs to</param>
        /// <param name="material">Material to check for an original name</param>
        /// <returns>Original material name or empty string if the material isn't renamed</returns>
        public string GetMaterialNamePropertyValueOriginal(int id, Renderer renderer, Material material)
        {
            return MaterialNamePropertyList.FirstOrDefault(x => x.ID == id && x.Renderer == renderer?.NameFormatted() && x.Value == material?.name)?.ValueOriginal ?? string.Empty;
        }
        /// <summary>
        /// Remove the saved material name property if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="renderer">Renderer that the material belongs to</param>
        /// <param name="material">Material to check for an original name</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void RemoveMaterialNameProperty(int id, Renderer renderer, Material material, bool setProperty = true)
        {
            GameObject go = GetObjectByID(id);
            if (setProperty)
            {
                var original = GetMaterialNamePropertyValueOriginal(id, renderer, material);
                if (original != string.Empty)
                {
                    MaterialAPI.SetName(go, renderer.NameFormatted(), material.name, original);
                }
            }

            MaterialNamePropertyList.RemoveAll(x => x.ID == id && x.Renderer == renderer.NameFormatted() && x.Value == material.name);
        }

        /// <summary>
        /// Add a float property to be saved and loaded with the scene and optionally also update the materials.
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="value">Value</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void SetMaterialFloatProperty(int id, Material material, string propertyName, float value, bool setProperty = true)
        {
            GameObject go = GetObjectByID(id);
            var materialProperty = MaterialFloatPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted());
            if (materialProperty == null)
            {
                float valueOriginal = material.GetFloat($"_{propertyName}");
                MaterialFloatPropertyList.Add(new MaterialFloatProperty(id, material.NameFormatted(), propertyName, value.ToString(CultureInfo.InvariantCulture), valueOriginal.ToString(CultureInfo.InvariantCulture)));
            }
            else
            {
                if (value.ToString(CultureInfo.InvariantCulture) == materialProperty.ValueOriginal)
                    RemoveMaterialFloatProperty(id, material, propertyName, false);
                else
                    materialProperty.Value = value.ToString(CultureInfo.InvariantCulture);
            }

            if (setProperty)
                SetFloat(go, material.NameFormatted(), propertyName, value);
        }
        /// <summary>
        /// Get the saved material property value or null if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <returns>Saved material property value or null if none is saved</returns>
        public float? GetMaterialFloatPropertyValue(int id, Material material, string propertyName)
        {
            var value = MaterialFloatPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.Value;
            if (value.IsNullOrEmpty())
                return null;
            return float.Parse(value);
        }
        /// <summary>
        /// Get the saved material property's original value or null if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <returns>Saved material property's original value or null if none is saved</returns>
        public float? GetMaterialFloatPropertyValueOriginal(int id, Material material, string propertyName)
        {
            var valueOriginal = MaterialFloatPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.ValueOriginal;
            if (valueOriginal.IsNullOrEmpty())
                return null;
            return float.Parse(valueOriginal);
        }
        /// <summary>
        /// Remove the saved material property value if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void RemoveMaterialFloatProperty(int id, Material material, string propertyName, bool setProperty = true)
        {
            GameObject go = GetObjectByID(id);
            if (setProperty)
            {
                var original = GetMaterialFloatPropertyValueOriginal(id, material, propertyName);
                if (original != null)
                    SetFloat(go, material.NameFormatted(), propertyName, (float)original);
            }

            MaterialFloatPropertyList.RemoveAll(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted());
        }
        /// <summary>
        /// Add a keyword property to be saved and loaded with the scene and optionally also update the materials.
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="value">Value</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void SetMaterialKeywordProperty(int id, Material material, string propertyName, bool value, bool setProperty = true)
        {
            GameObject go = GetObjectByID(id);
            var materialProperty = MaterialKeywordPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted());
            if (materialProperty == null)
            {
                bool valueOriginal = material.IsKeywordEnabled($"_{propertyName}");
                MaterialKeywordPropertyList.Add(new MaterialKeywordProperty(id, material.NameFormatted(), propertyName, value, valueOriginal));
            }
            else
            {
                if (value == materialProperty.ValueOriginal)
                    RemoveMaterialKeywordProperty(id, material, propertyName, false);
                else
                    materialProperty.Value = value;
            }

            if (setProperty)
                SetKeyword(go, material.NameFormatted(), propertyName, value);
        }
        /// <summary>
        /// Get the saved renderer property's original value or null if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="renderer">Renderer being modified</param>
        /// <param name="property">Property of the renderer</param>
        /// <returns>Saved renderer property's original value</returns>
        public bool? GetMaterialKeywordPropertyValue(int id, Material material, string propertyName)
        {
            return MaterialKeywordPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.Value;
        }
        /// <summary>
        /// Get the saved material property's original value or null if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <returns>Saved material property's original value or null if none is saved</returns>
        public bool? GetMaterialKeywordPropertyValueOriginal(int id, Material material, string propertyName)
        {
            return MaterialKeywordPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.ValueOriginal;
        }
        /// <summary>
        /// Remove the saved material property value if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void RemoveMaterialKeywordProperty(int id, Material material, string propertyName, bool setProperty = true)
        {
            GameObject go = GetObjectByID(id);
            if (setProperty)
            {
                var original = GetMaterialKeywordPropertyValueOriginal(id, material, propertyName);
                if (original != null)
                    SetKeyword(go, material.NameFormatted(), propertyName, (bool)original);
            }

            MaterialKeywordPropertyList.RemoveAll(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted());
        }
        /// <summary>
        /// Add a color property to be saved and loaded with the scene and optionally also update the materials.
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="value">Value</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void SetMaterialColorProperty(int id, Material material, string propertyName, Color value, bool setProperty = true)
        {
            GameObject go = GetObjectByID(id);
            var colorProperty = MaterialColorPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted());
            if (colorProperty == null)
            {
                Color valueOriginal = material.GetColor($"_{propertyName}");
                MaterialColorPropertyList.Add(new MaterialColorProperty(id, material.NameFormatted(), propertyName, value, valueOriginal));
            }
            else
            {
                if (value == colorProperty.ValueOriginal)
                    RemoveMaterialColorProperty(id, material, propertyName, false);
                else
                    colorProperty.Value = value;
            }

            if (setProperty)
                SetColor(go, material.NameFormatted(), propertyName, value);
        }
        /// <summary>
        /// Get the saved material property value or null if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <returns>Saved material property value or null if none is saved</returns>
        public Color? GetMaterialColorPropertyValue(int id, Material material, string propertyName) =>
            MaterialColorPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.Value;
        /// <summary>
        /// Get the saved material property's original value or null if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <returns>Saved material property's original value or null if none is saved</returns>
        public Color? GetMaterialColorPropertyValueOriginal(int id, Material material, string propertyName) =>
            MaterialColorPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.ValueOriginal;
        /// <summary>
        /// Remove the saved material property value if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void RemoveMaterialColorProperty(int id, Material material, string propertyName, bool setProperty = true)
        {
            GameObject go = GetObjectByID(id);
            if (setProperty)
            {
                var original = GetMaterialColorPropertyValueOriginal(id, material, propertyName);
                if (original != null)
                    SetColor(go, material.NameFormatted(), propertyName, (Color)original);
            }

            MaterialColorPropertyList.RemoveAll(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted());
        }

        /// <summary>
        /// Add a texture property to be saved and loaded with the card.
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="filePath">Path to the .png file on disk</param>
        /// <param name="setTexInUpdate">Whether to wait for the next Update</param>
        public void SetMaterialTextureFromFile(int id, Material material, string propertyName, string filePath, bool setTexInUpdate = false)
        {
            GameObject go = GetObjectByID(id);
            if (!File.Exists(filePath)) return;

            if (setTexInUpdate)
            {
                FileToSet = filePath;
                PropertyToSet = propertyName;
                MatToSet = material;
                IDToSet = id;
            }
            else
            {
                var texBytes = File.ReadAllBytes(filePath);
                var texID = SetAndGetTextureID(texBytes);

                var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted());
                if (textureProperty == null)
                {
                    textureProperty = new MaterialTextureProperty(id, material.NameFormatted(), propertyName, texID);
                    MaterialTexturePropertyList.Add(textureProperty);
                }
                else
                    textureProperty.TexID = texID;

                textureProperty.TexAnimationDef = MEAnimationUtil.LoadAnimationDefFromBytes(texID, texBytes, SetAndGetTextureID);
                SetTextureWithProperty(go, textureProperty);
            }
        }
        /// <summary>
        /// Sets the texture indicated by TexID to texture of Material indicated by TextureProperty
        /// </summary>
        /// <param name="go">GameObject to search for the renderer</param>
        /// <param name="textureProperty">TextureProperty with TexID to set for Material</param>
        /// <returns>True if the value was set, false if it could not be set</returns>
        private bool SetTextureWithProperty(GameObject go, MaterialTextureProperty textureProperty)
        {
            if (!textureProperty.TexID.HasValue || textureProperty.NullCheck())
                return false;

            int texID = textureProperty.TexID.Value;
            if (!TextureDictionary.TryGetValue(texID, out var container))
                return false;

            if (textureProperty.TexAnimationDef == null)
            {
                //Does not have animation

                AnimationControllerMap.Remove(textureProperty); //If have animation, delete it.

                var tex = container.Texture;
                MaterialEditorPlugin.Instance.ConvertNormalMap(ref tex, textureProperty.Property);
                return SetTexture(go, textureProperty.MaterialName, textureProperty.Property, tex);
            }
            else
            {
                if (AnimationControllerMap.TryGetValue(textureProperty, out var controller))
                {
                    controller.go = go;
                    if (textureProperty.TexAnimationDef != controller.def)
                        controller.Reset(textureProperty.TexAnimationDef);
                }
                else
                {
                    controller = new MEAnimationController(this, go, textureProperty.TexAnimationDef);
                    AnimationControllerMap[textureProperty] = controller;
                }

                controller.UpdateAnimation(textureProperty);
                return true;
            }
        }
        /// <summary>
        /// Add a texture property to be saved and loaded with the card.
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="data">Byte array containing the texture data</param>
        public void SetMaterialTexture(int id, Material material, string propertyName, byte[] data)
        {
            GameObject go = GetObjectByID(id);
            if (data == null) return;

            var texID = SetAndGetTextureID(data);

            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted());
            if (textureProperty == null)
            {
                textureProperty = new MaterialTextureProperty(id, material.NameFormatted(), propertyName, texID);
                MaterialTexturePropertyList.Add(textureProperty);
            }
            else
                textureProperty.TexID = texID;

            textureProperty.TexAnimationDef = MEAnimationUtil.LoadAnimationDefFromBytes(texID, data, SetAndGetTextureID);
            SetTextureWithProperty(go, textureProperty);
        }
        /// <summary>
        /// Get the saved material property value or null if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <returns>Saved material property value or null if none is saved</returns>
        public Texture GetMaterialTexture(int id, Material material, string propertyName)
        {
            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.MaterialName == material.NameFormatted() && x.Property == propertyName);
            if (textureProperty?.TexID != null)
                return TextureDictionary[(int)textureProperty.TexID].Texture;
            return null;
        }

        /// <summary>
        /// Get whether the texture has been changed
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <returns>True if the texture has not been modified, false if it has been.</returns>
        public bool GetMaterialTextureOriginal(int id, Material material, string propertyName) =>
            MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.MaterialName == material.NameFormatted() && x.Property == propertyName)?.TexID == null;
        /// <summary>
        /// Remove the saved material property value if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="displayMessage">Whether to display a message on screen telling the user to save and reload to refresh textures</param>
        public void RemoveMaterialTexture(int id, Material material, string propertyName, bool displayMessage = true)
        {
            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.MaterialName == material.NameFormatted() && x.Property == propertyName);
            if (textureProperty != null)
            {
                if (displayMessage)
                    MaterialEditorPlugin.Logger.LogMessage("Save and reload scene to refresh textures.");
                textureProperty.TexID = null;
                RemoveTexturePropertyIfNull(textureProperty);
            }
        }

        /// <summary>
        /// If TextureProperty is null, delete it.
        /// </summary>
        /// <param name="textureProperty"></param>
        void RemoveTexturePropertyIfNull(MaterialTextureProperty textureProperty)
        {
            if (!textureProperty.NullCheck())
                return;
            MaterialTexturePropertyList.Remove(textureProperty);
            AnimationControllerMap.Remove(textureProperty);
        }


        /// <summary>
        /// Add a texture offset property to be saved and loaded with the scene and optionally also update the materials.
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="value">Value</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void SetMaterialTextureOffset(int id, Material material, string propertyName, Vector2 value, bool setProperty = true)
        {
            GameObject gameObject = GetObjectByID(id);
            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.MaterialName == material.NameFormatted() && x.Property == propertyName);
            if (textureProperty == null)
            {
                Vector2 valueOriginal = material.GetTextureOffset($"_{propertyName}");
                MaterialTexturePropertyList.Add(new MaterialTextureProperty(id, material.NameFormatted(), propertyName, offset: value, offsetOriginal: valueOriginal));
            }
            else
            {
                if (value == textureProperty.OffsetOriginal)
                    RemoveMaterialTextureOffset(id, material, propertyName, false);
                else
                {
                    textureProperty.Offset = value;
                    if (textureProperty.OffsetOriginal == null)
                        textureProperty.OffsetOriginal = material.GetTextureOffset($"_{propertyName}");
                }
            }

            if (setProperty)
                SetTextureOffset(gameObject, material.NameFormatted(), propertyName, value);
        }
        /// <summary>
        /// Get the saved material property value or null if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <returns>Saved material property value or null if none is saved</returns>
        public Vector2? GetMaterialTextureOffset(int id, Material material, string propertyName) =>
            MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.MaterialName == material.NameFormatted() && x.Property == propertyName)?.Offset;
        /// <summary>
        /// Get the saved material property's original value or null if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <returns>Saved material property's original value or null if none is saved</returns>
        public Vector2? GetMaterialTextureOffsetOriginal(int id, Material material, string propertyName) =>
            MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.MaterialName == material.NameFormatted() && x.Property == propertyName)?.OffsetOriginal;
        /// <summary>
        /// Remove the saved material property value if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void RemoveMaterialTextureOffset(int id, Material material, string propertyName, bool setProperty = true)
        {
            GameObject gameObject = GetObjectByID(id);
            if (setProperty)
            {
                var original = GetMaterialTextureOffsetOriginal(id, material, propertyName);
                if (original != null)
                    SetTextureOffset(gameObject, material.NameFormatted(), propertyName, original);
            }

            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.MaterialName == material.NameFormatted() && x.Property == propertyName);
            if (textureProperty != null)
            {
                textureProperty.Offset = null;
                textureProperty.OffsetOriginal = null;
                RemoveTexturePropertyIfNull(textureProperty);
            }
        }

        /// <summary>
        /// Add a texture scale property to be saved and loaded with the scene and optionally also update the materials.
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="value">Value</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void SetMaterialTextureScale(int id, Material material, string propertyName, Vector2 value, bool setProperty = true)
        {
            GameObject gameObject = GetObjectByID(id);
            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.MaterialName == material.NameFormatted() && x.Property == propertyName);
            if (textureProperty == null)
            {
                Vector2 valueOriginal = material.GetTextureScale($"_{propertyName}");
                MaterialTexturePropertyList.Add(new MaterialTextureProperty(id, material.NameFormatted(), propertyName, scale: value, scaleOriginal: valueOriginal));
            }
            else
            {
                if (value == textureProperty.ScaleOriginal)
                    RemoveMaterialFloatProperty(id, material, propertyName, false);
                else
                {
                    textureProperty.Scale = value;
                    if (textureProperty.ScaleOriginal == null)
                        textureProperty.ScaleOriginal = material.GetTextureScale($"_{propertyName}");
                }
            }

            if (setProperty)
                SetTextureScale(gameObject, material.NameFormatted(), propertyName, value);
        }

        /// <summary>
        /// Get the saved material property value or null if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <returns>Saved material property value or null if none is saved</returns>
        public Vector2? GetMaterialTextureScale(int id, Material material, string propertyName) =>
            MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.MaterialName == material.NameFormatted() && x.Property == propertyName)?.Scale;
        /// <summary>
        /// Get the saved material property's original value or null if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <returns>Saved material property's original value or null if none is saved</returns>
        public Vector2? GetMaterialTextureScaleOriginal(int id, Material material, string propertyName) =>
            MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.MaterialName == material.NameFormatted() && x.Property == propertyName)?.ScaleOriginal;
        /// <summary>
        /// Remove the saved material property value if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void RemoveMaterialTextureScale(int id, Material material, string propertyName, bool setProperty = true)
        {
            GameObject gameObject = GetObjectByID(id);
            if (setProperty)
            {
                var original = GetMaterialTextureScaleOriginal(id, material, propertyName);
                if (original != null)
                    SetTextureScale(gameObject, material.NameFormatted(), propertyName, original);
            }

            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.MaterialName == material.NameFormatted() && x.Property == propertyName);
            if (textureProperty != null)
            {
                textureProperty.Scale = null;
                textureProperty.ScaleOriginal = null;
                RemoveTexturePropertyIfNull(textureProperty);
            }
        }

        /// <summary>
        /// Add a shader to be saved and loaded with the scene and optionally also update the materials.
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="shaderName">Property of the material without the leading underscore</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void SetMaterialShader(int id, Material material, string shaderName, bool setProperty = true)
        {
            GameObject gameObject = GetObjectByID(id);
            var materialProperty = MaterialShaderList.FirstOrDefault(x => x.ID == id && x.MaterialName == material.NameFormatted());
            if (materialProperty == null)
            {
                string shaderNameOriginal = material.shader.NameFormatted();
                MaterialShaderList.Add(new MaterialShader(id, material.NameFormatted(), shaderName, shaderNameOriginal));
            }
            else
            {
                if (shaderName == materialProperty.ShaderNameOriginal)
                    RemoveMaterialShader(id, material, false);
                else
                {
                    materialProperty.ShaderName = shaderName;
                    if (materialProperty.ShaderNameOriginal == null)
                        materialProperty.ShaderNameOriginal = material.shader.NameFormatted();
                }
            }

            if (setProperty)
            {
                RemoveMaterialShaderRenderQueue(id, material, false);
                SetShader(gameObject, material.NameFormatted(), shaderName);
            }
        }
        /// <summary>
        /// Get the saved shader name or null if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <returns>Saved shader name or null if none is saved</returns>
        public string GetMaterialShader(int id, Material material) =>
            MaterialShaderList.FirstOrDefault(x => x.ID == id && x.MaterialName == material.NameFormatted())?.ShaderName;
        /// <summary>
        /// Get the saved shader name's original value or null if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <returns>Saved shader name's original value or null if none is saved</returns>
        public string GetMaterialShaderOriginal(int id, Material material) =>
            MaterialShaderList.FirstOrDefault(x => x.ID == id && x.MaterialName == material.NameFormatted())?.ShaderNameOriginal;
        /// <summary>
        /// Remove the saved shader if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void RemoveMaterialShader(int id, Material material, bool setProperty = true)
        {
            GameObject gameObject = GetObjectByID(id);
            if (setProperty)
            {
                var original = GetMaterialShaderOriginal(id, material);
                if (!original.IsNullOrEmpty())
                    SetShader(gameObject, material.NameFormatted(), original);
            }

            foreach (var materialProperty in MaterialShaderList.Where(x => x.ID == id && x.MaterialName == material.NameFormatted()))
            {
                materialProperty.ShaderName = null;
                materialProperty.ShaderNameOriginal = null;
            }

            MaterialShaderList.RemoveAll(x => x.ID == id && x.MaterialName == material.NameFormatted() && x.NullCheck());
        }

        /// <summary>
        /// Add a render queue to be saved and loaded with the scene and optionally also update the materials.
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="renderQueue">Value</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void SetMaterialShaderRenderQueue(int id, Material material, int renderQueue, bool setProperty = true)
        {
            GameObject gameObject = GetObjectByID(id);
            var materialProperty = MaterialShaderList.FirstOrDefault(x => x.ID == id && x.MaterialName == material.NameFormatted());
            if (materialProperty == null)
            {
                int renderQueueOriginal = material.renderQueue;
                MaterialShaderList.Add(new MaterialShader(id, material.NameFormatted(), renderQueue, renderQueueOriginal));
            }
            else
            {
                if (renderQueue == materialProperty.RenderQueueOriginal)
                    RemoveMaterialShaderRenderQueue(id, material, false);
                else
                {
                    materialProperty.RenderQueue = renderQueue;
                    if (materialProperty.RenderQueueOriginal == null)
                        materialProperty.RenderQueueOriginal = material.renderQueue;
                }
            }

            if (setProperty)
                SetRenderQueue(gameObject, material.NameFormatted(), renderQueue);
        }
        /// <summary>
        /// Get the saved render queue value or null if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <returns>Saved render queue value or null if none is saved</returns>
        public int? GetMaterialShaderRenderQueue(int id, Material material) =>
            MaterialShaderList.FirstOrDefault(x => x.ID == id && x.MaterialName == material.NameFormatted())?.RenderQueue;
        /// <summary>
        /// Get the saved render queue's original value or null if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <returns>Saved render queue value's original or null if none is saved</returns>
        public int? GetMaterialShaderRenderQueueOriginal(int id, Material material) =>
            MaterialShaderList.FirstOrDefault(x => x.ID == id && x.MaterialName == material.NameFormatted())?.RenderQueueOriginal;
        /// <summary>
        /// Remove the saved render queue value if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void RemoveMaterialShaderRenderQueue(int id, Material material, bool setProperty = true)
        {
            GameObject go = GetObjectByID(id);
            if (setProperty)
            {
                var original = GetMaterialShaderRenderQueueOriginal(id, material);
                if (original != null)
                    SetRenderQueue(go, material.NameFormatted(), original);
            }

            for (var i = 0; i < MaterialShaderList.Count; i++)
            {
                var materialProperty = MaterialShaderList[i];
                if (materialProperty.ID == id && materialProperty.MaterialName == material.NameFormatted())
                {
                    materialProperty.RenderQueue = null;
                    materialProperty.RenderQueueOriginal = null;
                }
            }

            MaterialShaderList.RemoveAll(x => x.ID == id && x.MaterialName == material.NameFormatted() && x.NullCheck());
        }
        #endregion

        private static GameObject GetObjectByID(int id)
        {
            if (!Studio.Studio.Instance.dicObjectCtrl.TryGetValue(id, out var objectCtrlInfo)) return null;
            if (objectCtrlInfo is OCIItem ociItem)
                return ociItem.objectItem;
            else if (objectCtrlInfo is OCIChar ociChar)
                return ociChar.charInfo.gameObject;
            return null;
        }

        /// <summary>
        /// Purge unused animation
        /// </summary>
        private void PurgeUnusedAnimation()
        {
            MEAnimationUtil.PurgeUnusedAnimation(AnimationControllerMap, MaterialTexturePropertyList);
        }

        /// <summary>
        /// Initialization of animation controllers
        /// </summary>
        static void InitAnimationController()
        {
            MEAnimationController.UpdateTexture = SetTextureForAnimation;
            MEAnimationController.GetTexID = GetTexIDWithAnimation;
        }

        /// <summary>
        /// Get texture ID from MaterialTextureProperty
        /// </summary>
        static int? GetTexIDWithAnimation(MaterialTextureProperty property)
        {
            return property.TexID;
        }

        /// <summary>
        /// Set of textures for animation
        /// </summary>
        static void SetTextureForAnimation(SceneController controller, GameObject go, MaterialTextureProperty property, int texID)
        {
            if (!TextureDictionary.TryGetValue(texID, out var tex))
                return;

            SetTexture(go, property.MaterialName, property.Property, tex.Texture);
        }

        /// <summary>
        /// Data storage class for renderer properties
        /// </summary>
        [Serializable]
        [MessagePackObject]
        public class RendererProperty
        {
            /// <summary>
            /// ID of the item
            /// </summary>
            [Key("ID")]
            public int ID;
            /// <summary>
            /// Name of the renderer
            /// </summary>
            [Key("RendererName")]
            public string RendererName;
            /// <summary>
            /// Property type
            /// </summary>
            [Key("Property")]
            public RendererProperties Property;
            /// <summary>
            /// Value
            /// </summary>
            [Key("Value")]
            public string Value;
            /// <summary>
            /// Original value
            /// </summary>
            [Key("ValueOriginal")]
            public string ValueOriginal;

            /// <summary>
            /// Data storage class for renderer properties
            /// </summary>
            /// <param name="id">ID of the item</param>
            /// <param name="rendererName">Name of the renderer</param>
            /// <param name="property">Property type</param>
            /// <param name="value">Value</param>
            /// <param name="valueOriginal">Original</param>
            public RendererProperty(int id, string rendererName, RendererProperties property, string value, string valueOriginal)
            {
                ID = id;
                RendererName = rendererName.FormatShadingObjectName();
                Property = property;
                Value = value;
                ValueOriginal = valueOriginal;
            }
        }

        /// <summary>
        /// Data storage class for name properties
        /// </summary>
        [Serializable]
        [MessagePackObject]
        public class MaterialNameProperty
        {
            /// <summary>
            /// ID of the item
            /// </summary>
            [Key("ID")]
            public int ID;
            /// <summary>
            /// Name of the renderer
            /// </summary>
            [Key("Renderer")]
            public string Renderer;
            /// <summary>
            /// Name of the material
            /// </summary>
            [Key("MaterialName")]
            public string MaterialName;
            /// <summary>
            /// Value
            /// </summary>
            [Key("Value")]
            public string Value;
            /// <summary>
            /// Original value
            /// </summary>
            [Key("ValueOriginal")]
            public string ValueOriginal;

            /// <summary>
            /// Data storage class for name properties
            /// </summary>
            /// <param name="id">ID of the item</param>
            /// <param name="renderer">Renderer being modified</param>
            /// <param name="material">Material being renamed</param>
            /// <param name="value">New name for the material</param>
            public MaterialNameProperty(int id, Renderer renderer, Material material, string value)
            {
                ID = id;
                Renderer = renderer.NameFormatted();
                MaterialName = material.name;
                Value = value;
                ValueOriginal = material.name;
            }
            /// <summary>
            /// Data storage class for name properties
            /// </summary>
            /// <param name="id">ID of the item</param>
            /// <param name="renderer">NameFormatted() name of the Renderer being modified</param>
            /// <param name="materialName">Raw, unmodified name of the Material being renamed</param>
            /// <param name="value">New name for the material</param>
            [SerializationConstructor]
            public MaterialNameProperty(int id, string renderer, string materialName, string value)
            {
                ID = id;
                Renderer = renderer;
                MaterialName = materialName;
                Value = value;
                ValueOriginal = materialName;
            }
        }

        /// <summary>
        /// Data storage class for float properties
        /// </summary>
        [Serializable]
        [MessagePackObject]
        public class MaterialFloatProperty
        {
            /// <summary>
            /// ID of the item
            /// </summary>
            [Key("ID")]
            public int ID;
            /// <summary>
            /// Name of the material
            /// </summary>
            [Key("MaterialName")]
            public string MaterialName;
            /// <summary>
            /// Name of the property
            /// </summary>
            [Key("Property")]
            public string Property;
            /// <summary>
            /// Value
            /// </summary>
            [Key("Value")]
            public string Value;
            /// <summary>
            /// Original value
            /// </summary>
            [Key("ValueOriginal")]
            public string ValueOriginal;

            /// <summary>
            /// Data storage class for float properties
            /// </summary>
            /// <param name="id">ID of the item</param>
            /// <param name="materialName">Name of the material</param>
            /// <param name="property">Name of the property</param>
            /// <param name="value">Value</param>
            /// <param name="valueOriginal">Original</param>
            public MaterialFloatProperty(int id, string materialName, string property, string value, string valueOriginal)
            {
                ID = id;
                MaterialName = materialName.FormatShadingObjectName();
                Property = property;
                Value = value;
                ValueOriginal = valueOriginal;
            }
        }

        /// <summary>
        /// Data storage class for keyword properties
        /// </summary>
        [Serializable]
        [MessagePackObject]
        public class MaterialKeywordProperty
        {
            /// <summary>
            /// ID of the item
            /// </summary>
            [Key("ID")]
            public int ID;
            /// <summary>
            /// Name of the material
            /// </summary>
            [Key("MaterialName")]
            public string MaterialName;
            /// <summary>
            /// Name of the property
            /// </summary>
            [Key("Property")]
            public string Property;
            /// <summary>
            /// Value
            /// </summary>
            [Key("Value")]
            public bool Value;
            /// <summary>
            /// Original value
            /// </summary>
            [Key("ValueOriginal")]
            public bool ValueOriginal;

            /// <summary>
            /// Data storage class for keyword properties
            /// </summary>
            /// <param name="id">ID of the item</param>
            /// <param name="materialName">Name of the material</param>
            /// <param name="property">Name of the property</param>
            /// <param name="value">Value</param>
            /// <param name="valueOriginal">Original</param>
            public MaterialKeywordProperty(int id, string materialName, string property, bool value, bool valueOriginal)
            {
                ID = id;
                MaterialName = materialName.FormatShadingObjectName();
                Property = property;
                Value = value;
                ValueOriginal = valueOriginal;
            }
        }

        /// <summary>
        /// Data storage class for color properties
        /// </summary>
        [Serializable]
        [MessagePackObject]
        public class MaterialColorProperty
        {
            /// <summary>
            /// ID of the item
            /// </summary>
            [Key("ID")]
            public int ID;
            /// <summary>
            /// Name of the material
            /// </summary>
            [Key("MaterialName")]
            public string MaterialName;
            /// <summary>
            /// Name of the property
            /// </summary>
            [Key("Property")]
            public string Property;
            /// <summary>
            /// Value
            /// </summary>
            [Key("Value")]
            public Color Value;
            /// <summary>
            /// Original value
            /// </summary>
            [Key("ValueOriginal")]
            public Color ValueOriginal;

            /// <summary>
            /// Data storage class for float properties
            /// </summary>
            /// <param name="id">ID of the item</param>
            /// <param name="materialName">Name of the material</param>
            /// <param name="property">Name of the property</param>
            /// <param name="value">Value</param>
            /// <param name="valueOriginal">Original</param>
            public MaterialColorProperty(int id, string materialName, string property, Color value, Color valueOriginal)
            {
                ID = id;
                MaterialName = materialName.FormatShadingObjectName();
                Property = property;
                Value = value;
                ValueOriginal = valueOriginal;
            }
        }

        /// <summary>
        /// Data storage class for texture properties
        /// </summary>
        [Serializable]
        [MessagePackObject]
        public class MaterialTextureProperty
        {
            /// <summary>
            /// ID of the item
            /// </summary>
            [Key("ID")]
            public int ID;
            /// <summary>
            /// Name of the material
            /// </summary>
            [Key("MaterialName")]
            public string MaterialName;
            /// <summary>
            /// Name of the property
            /// </summary>
            [Key("Property")]
            public string Property;
            /// <summary>
            /// ID of the texture from the texure dicionary
            /// </summary>
            [Key("TexID")]
            public int? TexID;
            /// <summary>
            /// Texture offset value
            /// </summary>
            [Key("Offset")]
            public Vector2? Offset;
            /// <summary>
            /// Texture offset original value
            /// </summary>
            [Key("OffsetOriginal")]
            public Vector2? OffsetOriginal;
            /// <summary>
            /// Texture scale value
            /// </summary>
            [Key("Scale")]
            public Vector2? Scale;
            /// <summary>
            /// Texture scale original value
            /// </summary>
            [Key("ScaleOriginal")]
            public Vector2? ScaleOriginal;
            /// <summary>
            /// Texture Animation Definition
            /// </summary>
            [Key("TexAnimationDef")]
            public MEAnimationDefine TexAnimationDef;

            /// <summary>
            /// Data storage class for texture properties
            /// </summary>
            /// <param name="id">ID of the item</param>
            /// <param name="materialName">Name of the material</param>
            /// <param name="property">Name of the property</param>
            /// <param name="texID">ID of the texture as stored in the texture dictionary</param>
            /// <param name="offset">Texture offset value</param>
            /// <param name="offsetOriginal">Texture offset original value</param>
            /// <param name="scale">Texture scale value</param>
            /// <param name="scaleOriginal">Texture scale original value</param>
            public MaterialTextureProperty(int id, string materialName, string property, int? texID = null, Vector2? offset = null, Vector2? offsetOriginal = null, Vector2? scale = null, Vector2? scaleOriginal = null, MEAnimationDefine texAnimationDef = null)
            {
                ID = id;
                MaterialName = materialName.FormatShadingObjectName();
                Property = property;
                TexID = texID;
                Offset = offset;
                OffsetOriginal = offsetOriginal;
                Scale = scale;
                ScaleOriginal = scaleOriginal;
                TexAnimationDef = texAnimationDef;
            }

            /// <summary>
            /// Check if the TexID, Offset, and Scale are all null. Safe to remove this data if true.
            /// </summary>
            /// <returns></returns>
            public bool NullCheck() => TexID == null && Offset == null && Scale == null;
        }

        /// <summary>
        /// Data storage class for shaders
        /// </summary>
        [Serializable]
        [MessagePackObject]
        public class MaterialShader
        {
            /// <summary>
            /// ID of the item
            /// </summary>
            [Key("ID")]
            public int ID;
            /// <summary>
            /// Name of the material
            /// </summary>
            [Key("MaterialName")]
            public string MaterialName;
            /// <summary>
            /// Name of the shader
            /// </summary>
            [Key("ShaderName")]
            public string ShaderName;
            /// <summary>
            /// Name of the original shader
            /// </summary>
            [Key("ShaderNameOriginal")]
            public string ShaderNameOriginal;
            /// <summary>
            /// Render queue
            /// </summary>
            [Key("RenderQueue")]
            public int? RenderQueue;
            /// <summary>
            /// Original render queue
            /// </summary>
            [Key("RenderQueueOriginal")]
            public int? RenderQueueOriginal;

            /// <summary>
            /// Data storage class for shader data
            /// </summary>
            /// <param name="id">ID of the item</param>
            /// <param name="materialName">Name of the material</param>
            /// <param name="shaderName">Name of the shader</param>
            /// <param name="shaderNameOriginal">Name of the original shader</param>
            /// <param name="renderQueue">Render queue</param>
            /// <param name="renderQueueOriginal">Original render queue</param>
            public MaterialShader(int id, string materialName, string shaderName, string shaderNameOriginal, int? renderQueue, int? renderQueueOriginal)
            {
                ID = id;
                MaterialName = materialName.FormatShadingObjectName();
                ShaderName = shaderName;
                ShaderNameOriginal = shaderNameOriginal;
                RenderQueue = renderQueue;
                RenderQueueOriginal = renderQueueOriginal;
            }
            /// <summary>
            /// Data storage class for shader data
            /// </summary>
            /// <param name="id">ID of the item</param>
            /// <param name="materialName">Name of the material</param>
            /// <param name="shaderName">Name of the shader</param>
            /// <param name="shaderNameOriginal">Name of the original shader</param>
            public MaterialShader(int id, string materialName, string shaderName, string shaderNameOriginal)
            {
                ID = id;
                MaterialName = materialName.FormatShadingObjectName();
                ShaderName = shaderName;
                ShaderNameOriginal = shaderNameOriginal;
            }
            /// <summary>
            /// Data storage class for shader data
            /// </summary>
            /// <param name="id">ID of the item</param>
            /// <param name="materialName">Name of the material</param>
            /// <param name="renderQueue">Render queue</param>
            /// <param name="renderQueueOriginal">Original render queue</param>
            public MaterialShader(int id, string materialName, int renderQueue, int renderQueueOriginal)
            {
                ID = id;
                MaterialName = materialName.FormatShadingObjectName();
                RenderQueue = renderQueue;
                RenderQueueOriginal = renderQueueOriginal;
            }

            /// <summary>
            /// Check if the shader name and render queue are both null. Safe to delete this data if true.
            /// </summary>
            /// <returns></returns>
            public bool NullCheck() => ShaderName.IsNullOrEmpty() && RenderQueue == null;
        }

        /// <summary>
        /// Data storage class for material copy info
        /// </summary>
        [Serializable]
        [MessagePackObject]
        public class MaterialCopy
        {
            /// <summary>
            /// ID of the item
            /// </summary>
            [Key("ID")]
            public int ID;
            /// <summary>
            /// Name of the material
            /// </summary>
            [Key("MaterialName")]
            public string MaterialName;
            /// <summary>
            /// Name of the copy
            /// </summary>
            [Key("MaterialCopyName")]
            public string MaterialCopyName;

            public MaterialCopy(int id, string materialName, string materialCopyName)
            {
                ID = id;
                MaterialName = materialName.FormatShadingObjectName();
                MaterialCopyName = materialCopyName;
            }
        }

        /// <summary>
        /// Data storage class for projector properties
        /// </summary>
        [Serializable]
        [MessagePackObject]
        public class ProjectorProperty
        {
            /// <summary>
            /// ID of the item
            /// </summary>
            [Key("ID")]
            public int ID;
            /// <summary>
            /// Name of the projector
            /// </summary>
            [Key("ProjectorName")]
            public string ProjectorName;
            /// <summary>
            /// Property type
            /// </summary>
            [Key("Property")]
            public ProjectorProperties Property;
            /// <summary>
            /// Value
            /// </summary>
            [Key("Value")]
            public string Value;
            /// <summary>
            /// Original value
            /// </summary>
            [Key("ValueOriginal")]
            public string ValueOriginal;

            /// <summary>
            /// Data storage class for renderer properties
            /// </summary>
            /// <param name="id">ID of the item</param>
            /// <param name="ProjectorName">Name of the renderer</param>
            /// <param name="property">Property type</param>
            /// <param name="value">Value</param>
            /// <param name="valueOriginal">Original</param>
            public ProjectorProperty(int id, string projectorName, ProjectorProperties property, string value, string valueOriginal)
            {
                ID = id;
                ProjectorName = projectorName.FormatShadingObjectName();
                Property = property;
                Value = value;
                ValueOriginal = valueOriginal;
            }
        }
    }
}
