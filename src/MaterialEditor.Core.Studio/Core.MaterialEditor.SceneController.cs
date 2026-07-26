using ExtensibleSaveFormat;
using KKAPI;
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
    public partial class SceneController : SceneCustomFunctionController
    {
        public const string TexDicSaveKey = nameof(TextureDictionary);

        private readonly List<RendererProperty> RendererPropertyList = new List<RendererProperty>();
        private readonly List<ProjectorProperty> ProjectorPropertyList = new List<ProjectorProperty>();
        private readonly List<MaterialNameProperty> MaterialNamePropertyList = new List<MaterialNameProperty>();
        private readonly List<MaterialFloatProperty> MaterialFloatPropertyList = new List<MaterialFloatProperty>();
        private readonly List<MaterialKeywordProperty> MaterialKeywordPropertyList = new List<MaterialKeywordProperty>();
        private readonly List<MaterialColorProperty> MaterialColorPropertyList = new List<MaterialColorProperty>();
        internal readonly List<MaterialTextureProperty> MaterialTexturePropertyList = new List<MaterialTextureProperty>();
        private readonly List<MaterialShader> MaterialShaderList = new List<MaterialShader>();
        private readonly List<MaterialCopy> MaterialCopyList = new List<MaterialCopy>();

        private readonly Dictionary<MaterialTextureProperty, MEAnimationController> AnimationControllerMap = new Dictionary<MaterialTextureProperty, MEAnimationController>();

        internal static Dictionary<int, TextureContainer> TextureDictionary = new Dictionary<int, TextureContainer>();

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

            if (TextureDictionary.Count > 0 || (
                SceneLocalTextures.SaveType == SceneTextureSaveType.Deduped
                && MaterialEditorCharaController.charaControllers.Any(x => x.TextureDictionary.Count > 0)
            ))
                TextureSaveHandler.Instance.Save(data, TexDicSaveKey, TextureDictionary, false);
            else
                data.data.Add(TexDicSaveKey, null);

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
            {
                TextureDictionary = TextureSaveHandler.Instance.Load<Dictionary<int, TextureContainer>>(data, TexDicSaveKey, false);
            }

            if (operation == SceneOperationKind.Import)
            {
                var importDictionaryTemp = TextureSaveHandler.Instance.Load<Dictionary<int, TextureContainer>>(data, TexDicSaveKey, false);
                foreach (var kvp in importDictionaryTemp)
                    importDictionary[kvp.Key] = SetAndGetTextureID(kvp.Value.Data);
            }

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
                if (copiedItem.Value is OCIChar ociChar)
                {
                    var chaCtrl = ociChar.charInfo
#if PH
                        .human
#endif
                        ;

                    MaterialEditorPlugin.GetCharaController(chaCtrl).DuplicatingFrom = copiedItem.Key;
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

    }
}
