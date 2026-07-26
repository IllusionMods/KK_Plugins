using ExtensibleSaveFormat;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using MaterialEditorAPI;
using MessagePack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UniRx;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;
using static MaterialEditorAPI.MaterialEditorPluginBase;
#if !EC
using KKAPI.Studio;
#endif
#if AI || HS2
using AIChara;
#endif
#if PH
using ChaFileCoordinate = Character.CustomParameter;
using ChaControl = Human;
#endif
namespace KK_Plugins.MaterialEditor
{
    using MEAnimationController = MEAnimationController<MaterialEditorCharaController, MaterialEditorCharaController.MaterialTextureProperty>;

    public partial class MaterialEditorCharaController
    {
        /// <summary>
        /// Handles saving data to coordinate cards
        /// </summary>
        /// <param name="coordinate"></param>
        protected override void OnCoordinateBeingSaved(ChaFileCoordinate coordinate)
        {

            var coordinateRendererPropertyList = RendererPropertyList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex && x.ObjectType != ObjectType.Hair && x.ObjectType != ObjectType.Character).ToList();
            var coordinateProjectorPropertyList = ProjectorPropertyList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex && x.ObjectType != ObjectType.Hair && x.ObjectType != ObjectType.Character).ToList();
            var coordinateMaterialNamePropertyList = MaterialNamePropertyList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex && x.ObjectType != ObjectType.Hair && x.ObjectType != ObjectType.Character).ToList();
            var coordinateMaterialFloatPropertyList = MaterialFloatPropertyList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex && x.ObjectType != ObjectType.Hair && x.ObjectType != ObjectType.Character).ToList();
            var coordinateMaterialKeywordPropertyList = MaterialKeywordPropertyList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex && x.ObjectType != ObjectType.Hair && x.ObjectType != ObjectType.Character).ToList();
            var coordinateMaterialColorPropertyList = MaterialColorPropertyList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex && x.ObjectType != ObjectType.Hair && x.ObjectType != ObjectType.Character).ToList();
            var coordinateMaterialTexturePropertyList = MaterialTexturePropertyList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex && x.ObjectType != ObjectType.Hair && x.ObjectType != ObjectType.Character).ToList();
            var coordinateMaterialShaderList = MaterialShaderList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex && x.ObjectType != ObjectType.Hair && x.ObjectType != ObjectType.Character).ToList();
            var coordinateMaterialCopyList = MaterialCopyList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex && x.ObjectType != ObjectType.Hair && x.ObjectType != ObjectType.Character).ToList();
            var coordinateTextureDictionary = new Dictionary<int, byte[]>();

            var usedTexIDMap = MEAnimationController.GetUsedTexIDSet(AnimationControllerMap, coordinateMaterialTexturePropertyList);

            foreach (var tex in TextureDictionary)
            {
                if (usedTexIDMap.Contains(tex.Key))
                    coordinateTextureDictionary.Add(tex.Key, tex.Value.Data);
            }

            if (coordinateRendererPropertyList.Count == 0 && coordinateMaterialNamePropertyList.Count == 0 && coordinateMaterialFloatPropertyList.Count == 0 && coordinateMaterialKeywordPropertyList.Count == 0 && coordinateMaterialColorPropertyList.Count == 0 && coordinateMaterialTexturePropertyList.Count == 0 && coordinateMaterialShaderList.Count == 0 && coordinateMaterialCopyList.Count == 0)
            {
                SetCoordinateExtendedData(coordinate, null);
            }
            else
            {
                var data = new PluginData();
                if (coordinateTextureDictionary.Count > 0)
                    data.data.Add(TexDicSaveKey, MessagePackSerializer.Serialize(coordinateTextureDictionary));
                else
                    data.data.Add(TexDicSaveKey, null);

                if (coordinateRendererPropertyList.Count > 0)
                    data.data.Add(nameof(RendererPropertyList), MessagePackSerializer.Serialize(coordinateRendererPropertyList));
                else
                    data.data.Add(nameof(RendererPropertyList), null);

                if (coordinateProjectorPropertyList.Count > 0)
                    data.data.Add(nameof(ProjectorPropertyList), MessagePackSerializer.Serialize(coordinateProjectorPropertyList));
                else
                    data.data.Add(nameof(ProjectorPropertyList), null);

                if (coordinateMaterialNamePropertyList.Count > 0)
                    data.data.Add(nameof(MaterialNamePropertyList), MessagePackSerializer.Serialize(coordinateMaterialNamePropertyList));
                else
                    data.data.Add(nameof(MaterialNamePropertyList), null);

                if (coordinateMaterialFloatPropertyList.Count > 0)
                    data.data.Add(nameof(MaterialFloatPropertyList), MessagePackSerializer.Serialize(coordinateMaterialFloatPropertyList));
                else
                    data.data.Add(nameof(MaterialFloatPropertyList), null);

                if (coordinateMaterialKeywordPropertyList.Count > 0)
                    data.data.Add(nameof(MaterialKeywordPropertyList), MessagePackSerializer.Serialize(coordinateMaterialKeywordPropertyList));
                else
                    data.data.Add(nameof(MaterialKeywordPropertyList), null);

                if (coordinateMaterialColorPropertyList.Count > 0)
                    data.data.Add(nameof(MaterialColorPropertyList), MessagePackSerializer.Serialize(coordinateMaterialColorPropertyList));
                else
                    data.data.Add(nameof(MaterialColorPropertyList), null);

                if (coordinateMaterialTexturePropertyList.Count > 0)
                    data.data.Add(nameof(MaterialTexturePropertyList), MessagePackSerializer.Serialize(coordinateMaterialTexturePropertyList));
                else
                    data.data.Add(nameof(MaterialTexturePropertyList), null);

                if (coordinateMaterialShaderList.Count > 0)
                    data.data.Add(nameof(MaterialShaderList), MessagePackSerializer.Serialize(coordinateMaterialShaderList));
                else
                    data.data.Add(nameof(MaterialShaderList), null);

                if (coordinateMaterialCopyList.Count > 0)
                    data.data.Add(nameof(MaterialCopyList), MessagePackSerializer.Serialize(coordinateMaterialCopyList));
                else
                    data.data.Add(nameof(MaterialCopyList), null);

                SetCoordinateExtendedData(coordinate, data);
            }

            base.OnCoordinateBeingSaved(coordinate);
        }

        /// <summary>
        /// Handles loading data from coordinate cards
        /// </summary>
        /// <param name="coordinate"></param>
        /// <param name="maintainState"></param>
        protected override void OnCoordinateBeingLoaded(ChaFileCoordinate coordinate, bool maintainState)
        {
            LoadCoordinateExtSaveData(coordinate);

            CoordinateChanging = true;

            if (MakerAPI.InsideAndLoaded)
                MaterialEditorUI.Visible = false;

            ChaControl.StartCoroutine(LoadData(true, true, false));
            base.OnCoordinateBeingLoaded(coordinate, maintainState);
        }

        private void LoadCharacterExtSaveData()
        {
            RemoveMaterialCopies(ChaControl.gameObject);

            List<ObjectType> objectTypesToLoad = new List<ObjectType>();

            var loadFlags = MakerAPI.GetCharacterLoadFlags();
            if (loadFlags == null)
            {
                RendererPropertyList.Clear();
                ProjectorPropertyList.Clear();
                MaterialNamePropertyList.Clear();
                MaterialFloatPropertyList.Clear();
                MaterialKeywordPropertyList.Clear();
                MaterialColorPropertyList.Clear();
                MaterialTexturePropertyList.Clear();
                MaterialShaderList.Clear();
                MaterialCopyList.Clear();
                AnimationControllerMap.Clear();

                objectTypesToLoad.Add(ObjectType.Accessory);
                objectTypesToLoad.Add(ObjectType.Character);
                objectTypesToLoad.Add(ObjectType.Clothing);
                objectTypesToLoad.Add(ObjectType.Hair);
            }
            else
            {
                bool changed = false;

                if (loadFlags.Face || loadFlags.Body)
                {
                    RendererPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Character);
                    ProjectorPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Character);
                    MaterialNamePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Character);
                    MaterialFloatPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Character);
                    MaterialKeywordPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Character);
                    MaterialColorPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Character);
                    MaterialTexturePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Character);
                    MaterialShaderList.RemoveAll(x => x.ObjectType == ObjectType.Character);
                    MaterialCopyList.RemoveAll(x => x.ObjectType == ObjectType.Character);

                    objectTypesToLoad.Add(ObjectType.Character);

                    changed = true;
                }
                if (loadFlags.Clothes)
                {
                    RendererPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing);
                    ProjectorPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing);
                    MaterialNamePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing);
                    MaterialFloatPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing);
                    MaterialKeywordPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing);
                    MaterialColorPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing);
                    MaterialTexturePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing);
                    MaterialShaderList.RemoveAll(x => x.ObjectType == ObjectType.Clothing);
                    MaterialCopyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing);
                    objectTypesToLoad.Add(ObjectType.Clothing);

                    RendererPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory);
                    ProjectorPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory);
                    MaterialNamePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory);
                    MaterialFloatPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory);
                    MaterialKeywordPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory);
                    MaterialColorPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory);
                    MaterialTexturePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory);
                    MaterialShaderList.RemoveAll(x => x.ObjectType == ObjectType.Accessory);
                    MaterialCopyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory);
                    objectTypesToLoad.Add(ObjectType.Accessory);

                    changed = true;
                }
                if (loadFlags.Hair)
                {
                    RendererPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Hair);
                    ProjectorPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Hair);
                    MaterialNamePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Hair);
                    MaterialFloatPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Hair);
                    MaterialKeywordPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Hair);
                    MaterialColorPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Hair);
                    MaterialTexturePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Hair);
                    MaterialShaderList.RemoveAll(x => x.ObjectType == ObjectType.Hair);
                    MaterialCopyList.RemoveAll(x => x.ObjectType == ObjectType.Hair);
                    objectTypesToLoad.Add(ObjectType.Hair);

                    changed = true;
                }

                if (changed)
                {
                    PurgeUnusedAnimation();
                }
            }

            //Don't destroy the textures in H mode because they will still be needed
            if (KoikatuAPI.GetCurrentGameMode() != GameMode.MainGame)
            {
                PurgeUnusedTextures();
            }

            CharacterLoading = true;

            var data = GetExtendedData();
            if (data != null)
            {
                var importDictionary = new Dictionary<int, int>();

#if !EC
                if (DuplicatingFrom.HasValue)
                {
                    var chaCtrl = (Studio.Studio.Instance.dicObjectCtrl[DuplicatingFrom.Value] as Studio.OCIChar).charInfo
#if PH
                        .human
#endif
                        ;
                    foreach (var kvp in MaterialEditorPlugin.GetCharaController(chaCtrl).TextureDictionary)
                        importDictionary[kvp.Key] = SetAndGetTextureID(kvp.Value.Data);
                    DuplicatingFrom = null;
                }
                else
#endif
                {
                    var importDictionaryTemp = TextureSaveHandler.Instance.Load<Dictionary<int, TextureContainer>>(data, TexDicSaveKey, true);
                    foreach (var kvp in importDictionaryTemp)
                        importDictionary[kvp.Key] = SetAndGetTextureID(kvp.Value.Data);
                }

                //Debug for dumping all textures
                //int counter = 1;
                //foreach (var tex in TextureDictionary.Values)
                //{
                //    string filename = Path.Combine(MaterialEditorPlugin.ExportPath, $"_Export_{ChaControl.GetCharacterName()}_{counter}.png");
                //    MaterialEditorPlugin.SaveTex(tex.Texture, filename);
                //    MaterialEditorPlugin.Logger.LogInfo($"Exported {filename}");
                //    counter++;
                //}

                if (data.data.TryGetValue(nameof(MaterialShaderList), out var shaderProperties) && shaderProperties != null)
                {
                    var properties = MessagePackSerializer.Deserialize<List<MaterialShader>>((byte[])shaderProperties);
                    for (var i = 0; i < properties.Count; i++)
                    {
                        var loadedProperty = properties[i];
                        int coordinateIndex = loadedProperty.ObjectType == ObjectType.Character ? 0 : loadedProperty.CoordinateIndex;
                        if (objectTypesToLoad.Contains(loadedProperty.ObjectType))
                            MaterialShaderList.Add(new MaterialShader(loadedProperty.ObjectType, coordinateIndex, loadedProperty.Slot, loadedProperty.MaterialName, loadedProperty.ShaderName, loadedProperty.ShaderNameOriginal, loadedProperty.RenderQueue, loadedProperty.RenderQueueOriginal));
                    }
                }

                if (data.data.TryGetValue(nameof(RendererPropertyList), out var rendererProperties) && rendererProperties != null)
                {
                    var properties = MessagePackSerializer.Deserialize<List<RendererProperty>>((byte[])rendererProperties);
                    for (var i = 0; i < properties.Count; i++)
                    {
                        var loadedProperty = properties[i];
                        int coordinateIndex = loadedProperty.ObjectType == ObjectType.Character ? 0 : loadedProperty.CoordinateIndex;
                        if (objectTypesToLoad.Contains(loadedProperty.ObjectType))
                            RendererPropertyList.Add(new RendererProperty(loadedProperty.ObjectType, coordinateIndex, loadedProperty.Slot, loadedProperty.RendererName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));
                    }
                }

                if (data.data.TryGetValue(nameof(ProjectorPropertyList), out var projectorProperties) && projectorProperties != null)
                {
                    var properties = MessagePackSerializer.Deserialize<List<ProjectorProperty>>((byte[])projectorProperties);
                    for (var i = 0; i < properties.Count; i++)
                    {
                        var loadedProperty = properties[i];
                        int coordinateIndex = loadedProperty.ObjectType == ObjectType.Character ? 0 : loadedProperty.CoordinateIndex;
                        if (objectTypesToLoad.Contains(loadedProperty.ObjectType))
                            ProjectorPropertyList.Add(new ProjectorProperty(loadedProperty.ObjectType, coordinateIndex, loadedProperty.Slot, loadedProperty.ProjectorName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));
                    }
                }

                if (data.data.TryGetValue(nameof(MaterialNamePropertyList), out var materialNameProperties) && materialNameProperties != null)
                {
                    var properties = MessagePackSerializer.Deserialize<List<MaterialNameProperty>>((byte[])materialNameProperties);
                    for (var i = 0; i < properties.Count; i++)
                    {
                        var loadedProperty = properties[i];
                        int coordinateIndex = loadedProperty.ObjectType == ObjectType.Character ? 0 : loadedProperty.CoordinateIndex;
                        if (objectTypesToLoad.Contains(loadedProperty.ObjectType))
                            MaterialNamePropertyList.Add(new MaterialNameProperty(loadedProperty.ObjectType, coordinateIndex, loadedProperty.Slot, loadedProperty.Renderer, loadedProperty.MaterialName, loadedProperty.Value));
                    }
                }

                if (data.data.TryGetValue(nameof(MaterialFloatPropertyList), out var materialFloatProperties) && materialFloatProperties != null)
                {
                    var properties = MessagePackSerializer.Deserialize<List<MaterialFloatProperty>>((byte[])materialFloatProperties);
                    for (var i = 0; i < properties.Count; i++)
                    {
                        var loadedProperty = properties[i];
                        int coordinateIndex = loadedProperty.ObjectType == ObjectType.Character ? 0 : loadedProperty.CoordinateIndex;
                        if (objectTypesToLoad.Contains(loadedProperty.ObjectType))
                            MaterialFloatPropertyList.Add(new MaterialFloatProperty(loadedProperty.ObjectType, coordinateIndex, loadedProperty.Slot, loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));
                    }
                }

                if (data.data.TryGetValue(nameof(MaterialKeywordPropertyList), out var materialKeywordProperties) && materialKeywordProperties != null)
                {
                    var properties = MessagePackSerializer.Deserialize<List<MaterialKeywordProperty>>((byte[])materialKeywordProperties);
                    for (var i = 0; i < properties.Count; i++)
                    {
                        var loadedProperty = properties[i];
                        int coordinateIndex = loadedProperty.ObjectType == ObjectType.Character ? 0 : loadedProperty.CoordinateIndex;
                        if (objectTypesToLoad.Contains(loadedProperty.ObjectType))
                            MaterialKeywordPropertyList.Add(new MaterialKeywordProperty(loadedProperty.ObjectType, coordinateIndex, loadedProperty.Slot, loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));
                    }
                }

                if (data.data.TryGetValue(nameof(MaterialColorPropertyList), out var materialColorProperties) && materialColorProperties != null)
                {
                    var properties = MessagePackSerializer.Deserialize<List<MaterialColorProperty>>((byte[])materialColorProperties);
                    for (var i = 0; i < properties.Count; i++)
                    {
                        var loadedProperty = properties[i];
                        int coordinateIndex = loadedProperty.ObjectType == ObjectType.Character ? 0 : loadedProperty.CoordinateIndex;
                        if (objectTypesToLoad.Contains(loadedProperty.ObjectType))
                            MaterialColorPropertyList.Add(new MaterialColorProperty(loadedProperty.ObjectType, coordinateIndex, loadedProperty.Slot, loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));
                    }
                }

                if (data.data.TryGetValue(nameof(MaterialTexturePropertyList), out var materialTextureProperties) && materialTextureProperties != null)
                {
                    var properties = MessagePackSerializer.Deserialize<List<MaterialTextureProperty>>((byte[])materialTextureProperties);
                    for (var i = 0; i < properties.Count; i++)
                    {
                        var loadedProperty = properties[i];
                        if (objectTypesToLoad.Contains(loadedProperty.ObjectType) && !loadedProperty.NullCheck())
                        {
                            int? texID = null;
                            if (loadedProperty.TexID != null && importDictionary.TryGetValue((int)loadedProperty.TexID, out var importTextID))
                                texID = importTextID;
                            MEAnimationUtil.RemapTexID(loadedProperty.TexAnimationDef, importDictionary);
                            int coordinateIndex = loadedProperty.ObjectType == ObjectType.Character ? 0 : loadedProperty.CoordinateIndex;
                            MaterialTextureProperty newTextureProperty = new MaterialTextureProperty(loadedProperty.ObjectType, coordinateIndex, loadedProperty.Slot, loadedProperty.MaterialName, loadedProperty.Property, texID, loadedProperty.Offset, loadedProperty.OffsetOriginal, loadedProperty.Scale, loadedProperty.ScaleOriginal, loadedProperty.TexAnimationDef);
                            MaterialTexturePropertyList.Add(newTextureProperty);
                        }
                    }
                }

                if (data.data.TryGetValue(nameof(MaterialCopyList), out var materialCopyData) && materialCopyData != null)
                {
                    var properties = MessagePackSerializer.Deserialize<List<MaterialCopy>>((byte[])materialCopyData);
                    for (var i = 0; i < properties.Count; i++)
                    {
                        var loadedProperty = properties[i];
                        int coordinateIndex = loadedProperty.ObjectType == ObjectType.Character ? 0 : loadedProperty.CoordinateIndex;
                        if (objectTypesToLoad.Contains(loadedProperty.ObjectType))
                            MaterialCopyList.Add(new MaterialCopy(loadedProperty.ObjectType, coordinateIndex, loadedProperty.Slot, loadedProperty.MaterialName, loadedProperty.MaterialCopyName));
                    }
                }
            }
        }

        private void LoadCoordinateExtSaveData(ChaFileCoordinate coordinate)
        {
            List<ObjectType> objectTypesToLoad = new List<ObjectType>();

            var loadFlags = MakerAPI.GetCoordinateLoadFlags();
            if (loadFlags == null)
            {
                RendererPropertyList.RemoveAll(x => (x.ObjectType == ObjectType.Clothing || x.ObjectType == ObjectType.Accessory) && x.CoordinateIndex == CurrentCoordinateIndex);
                MaterialNamePropertyList.RemoveAll(x => (x.ObjectType == ObjectType.Clothing || x.ObjectType == ObjectType.Accessory) && x.CoordinateIndex == CurrentCoordinateIndex);
                MaterialFloatPropertyList.RemoveAll(x => (x.ObjectType == ObjectType.Clothing || x.ObjectType == ObjectType.Accessory) && x.CoordinateIndex == CurrentCoordinateIndex);
                MaterialKeywordPropertyList.RemoveAll(x => (x.ObjectType == ObjectType.Clothing || x.ObjectType == ObjectType.Accessory) && x.CoordinateIndex == CurrentCoordinateIndex);
                MaterialColorPropertyList.RemoveAll(x => (x.ObjectType == ObjectType.Clothing || x.ObjectType == ObjectType.Accessory) && x.CoordinateIndex == CurrentCoordinateIndex);
                MaterialTexturePropertyList.RemoveAll(x => (x.ObjectType == ObjectType.Clothing || x.ObjectType == ObjectType.Accessory) && x.CoordinateIndex == CurrentCoordinateIndex);
                MaterialShaderList.RemoveAll(x => (x.ObjectType == ObjectType.Clothing || x.ObjectType == ObjectType.Accessory) && x.CoordinateIndex == CurrentCoordinateIndex);
                MaterialCopyList.RemoveAll(x => (x.ObjectType == ObjectType.Clothing || x.ObjectType == ObjectType.Accessory) && x.CoordinateIndex == CurrentCoordinateIndex);

                objectTypesToLoad.Add(ObjectType.Accessory);
                objectTypesToLoad.Add(ObjectType.Clothing);

                PurgeUnusedAnimation();
            }
            else
            {
                if (loadFlags.Clothes)
                {
                    RendererPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == CurrentCoordinateIndex);
                    MaterialNamePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == CurrentCoordinateIndex);
                    MaterialFloatPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == CurrentCoordinateIndex);
                    MaterialKeywordPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == CurrentCoordinateIndex);
                    MaterialColorPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == CurrentCoordinateIndex);
                    MaterialTexturePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == CurrentCoordinateIndex);
                    MaterialShaderList.RemoveAll(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == CurrentCoordinateIndex);
                    MaterialCopyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == CurrentCoordinateIndex);
                    objectTypesToLoad.Add(ObjectType.Clothing);
                }
                if (loadFlags.Accessories)
                {
                    RendererPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex);
                    MaterialNamePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex);
                    MaterialFloatPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex);
                    MaterialKeywordPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex);
                    MaterialColorPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex);
                    MaterialTexturePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex);
                    MaterialShaderList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex);
                    MaterialCopyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex);
                    objectTypesToLoad.Add(ObjectType.Accessory);
                }

                if (loadFlags.Clothes || loadFlags.Accessories)
                {
                    PurgeUnusedAnimation();
                }
            }

            var data = GetCoordinateExtendedData(coordinate);
            if (data?.data != null)
            {
                var importDictionary = new Dictionary<int, int>();

                if (data.data.TryGetValue(TexDicSaveKey, out var texDic) && texDic != null)
                    foreach (var x in MessagePackSerializer.Deserialize<Dictionary<int, byte[]>>((byte[])texDic))
                        importDictionary[x.Key] = SetAndGetTextureID(x.Value);

                if (data.data.TryGetValue(nameof(MaterialShaderList), out var materialShaders) && materialShaders != null)
                {
                    var properties = MessagePackSerializer.Deserialize<List<MaterialShader>>((byte[])materialShaders);
                    for (var i = 0; i < properties.Count; i++)
                    {
                        var loadedProperty = properties[i];
                        if (objectTypesToLoad.Contains(loadedProperty.ObjectType))
                            MaterialShaderList.Add(new MaterialShader(loadedProperty.ObjectType, CurrentCoordinateIndex, loadedProperty.Slot, loadedProperty.MaterialName, loadedProperty.ShaderName, loadedProperty.ShaderNameOriginal, loadedProperty.RenderQueue, loadedProperty.RenderQueueOriginal));
                    }
                }

                if (data.data.TryGetValue(nameof(RendererPropertyList), out var rendererProperties) && rendererProperties != null)
                {
                    var properties = MessagePackSerializer.Deserialize<List<RendererProperty>>((byte[])rendererProperties);
                    for (var i = 0; i < properties.Count; i++)
                    {
                        var loadedProperty = properties[i];
                        if (objectTypesToLoad.Contains(loadedProperty.ObjectType))
                            RendererPropertyList.Add(new RendererProperty(loadedProperty.ObjectType, CurrentCoordinateIndex, loadedProperty.Slot, loadedProperty.RendererName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));
                    }
                }

                if (data.data.TryGetValue(nameof(MaterialNamePropertyList), out var materialNameProperties) && materialNameProperties != null)
                {
                    var properties = MessagePackSerializer.Deserialize<List<MaterialNameProperty>>((byte[])materialNameProperties);
                    for (var i = 0; i < properties.Count; i++)
                    {
                        var loadedProperty = properties[i];
                        if (objectTypesToLoad.Contains(loadedProperty.ObjectType))
                            MaterialNamePropertyList.Add(new MaterialNameProperty(loadedProperty.ObjectType, CurrentCoordinateIndex, loadedProperty.Slot, loadedProperty.Renderer, loadedProperty.MaterialName, loadedProperty.Value));
                    }
                }

                if (data.data.TryGetValue(nameof(MaterialFloatPropertyList), out var materialFloatProperties) && materialFloatProperties != null)
                {
                    var properties = MessagePackSerializer.Deserialize<List<MaterialFloatProperty>>((byte[])materialFloatProperties);
                    for (var i = 0; i < properties.Count; i++)
                    {
                        var loadedProperty = properties[i];
                        if (objectTypesToLoad.Contains(loadedProperty.ObjectType))
                            MaterialFloatPropertyList.Add(new MaterialFloatProperty(loadedProperty.ObjectType, CurrentCoordinateIndex, loadedProperty.Slot, loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));
                    }
                }

                if (data.data.TryGetValue(nameof(MaterialKeywordPropertyList), out var materialKeywordProperties) && materialKeywordProperties != null)
                {
                    var properties = MessagePackSerializer.Deserialize<List<MaterialKeywordProperty>>((byte[])materialKeywordProperties);
                    for (var i = 0; i < properties.Count; i++)
                    {
                        var loadedProperty = properties[i];
                        if (objectTypesToLoad.Contains(loadedProperty.ObjectType))
                            MaterialKeywordPropertyList.Add(new MaterialKeywordProperty(loadedProperty.ObjectType, CurrentCoordinateIndex, loadedProperty.Slot, loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));
                    }
                }

                if (data.data.TryGetValue(nameof(MaterialColorPropertyList), out var materialColorProperties) && materialColorProperties != null)
                {
                    var properties = MessagePackSerializer.Deserialize<List<MaterialColorProperty>>((byte[])materialColorProperties);
                    for (var i = 0; i < properties.Count; i++)
                    {
                        var loadedProperty = properties[i];
                        if (objectTypesToLoad.Contains(loadedProperty.ObjectType))
                            MaterialColorPropertyList.Add(new MaterialColorProperty(loadedProperty.ObjectType, CurrentCoordinateIndex, loadedProperty.Slot, loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));
                    }
                }

                if (data.data.TryGetValue(nameof(MaterialTexturePropertyList), out var materialTextureProperties) && materialTextureProperties != null)
                {
                    var properties = MessagePackSerializer.Deserialize<List<MaterialTextureProperty>>((byte[])materialTextureProperties);
                    for (var i = 0; i < properties.Count; i++)
                    {
                        var loadedProperty = properties[i];
                        if (objectTypesToLoad.Contains(loadedProperty.ObjectType))
                        {
                            int? texID = null;
                            if (loadedProperty.TexID != null)
                                texID = importDictionary[(int)loadedProperty.TexID];
                            MEAnimationUtil.RemapTexID(loadedProperty.TexAnimationDef, importDictionary);
                            MaterialTextureProperty newTextureProperty = new MaterialTextureProperty(loadedProperty.ObjectType, CurrentCoordinateIndex, loadedProperty.Slot, loadedProperty.MaterialName, loadedProperty.Property, texID, loadedProperty.Offset, loadedProperty.OffsetOriginal, loadedProperty.Scale, loadedProperty.ScaleOriginal, loadedProperty.TexAnimationDef);
                            MaterialTexturePropertyList.Add(newTextureProperty);
                        }
                    }
                }

                if (data.data.TryGetValue(nameof(MaterialCopyList), out var materialCopyData) && materialCopyData != null)
                {
                    var properties = MessagePackSerializer.Deserialize<List<MaterialCopy>>((byte[])materialCopyData);
                    for (var i = 0; i < properties.Count; i++)
                    {
                        var loadedProperty = properties[i];
                        if (objectTypesToLoad.Contains(loadedProperty.ObjectType))
                            MaterialCopyList.Add(new MaterialCopy(loadedProperty.ObjectType, CurrentCoordinateIndex, loadedProperty.Slot, loadedProperty.MaterialName, loadedProperty.MaterialCopyName));
                    }
                }
            }
        }

        /// <summary></summary>
        public IEnumerator LoadData(bool clothes, bool accessories, bool hair)
        {
            return LoadData(clothes, accessories, hair, true);
        }

        /// <summary></summary>
        public IEnumerator LoadData(bool clothes, bool accessories, bool hair, bool body)
        {
            yield return null;
#if !EC
            if (KKAPI.Studio.StudioAPI.InsideStudio)
            {
                yield return null;
                yield return null;
            }
#endif
            while (ChaControl == null || ChaControl.GetHead() == null)
                yield return null;

            if (body)
                CorrectTongue();
#if KK || KKS
            if (KKAPI.Studio.StudioAPI.InsideStudio && body)
                CorrectFace();
#endif

            //Instantiate all material copies before applying any edits to ensure edits are applied to copies
            for (var i = 0; i < MaterialCopyList.Count; i++)
            {
                var property = MaterialCopyList[i];
                if (property.ObjectType == ObjectType.Clothing && !clothes) continue;
                if (property.ObjectType == ObjectType.Accessory && !accessories) continue;
                if (property.ObjectType == ObjectType.Hair && !hair) continue;
                if ((property.ObjectType == ObjectType.Clothing || property.ObjectType == ObjectType.Accessory) && property.CoordinateIndex != CurrentCoordinateIndex) continue;
                if (property.ObjectType == ObjectType.Character && !body) continue;

                CopyMaterial(FindGameObject(property.ObjectType, property.Slot), property.MaterialName, property.MaterialCopyName);
            }

            // Rename materials before applying edits, but after copying materials, to ensure no missing material mishaps occur
            // Do not move this anywhere else
            for (var i = 0; i < MaterialNamePropertyList.Count; i++)
            {
                var property = MaterialNamePropertyList[i];
                if (property.ObjectType == ObjectType.Clothing && !clothes) continue;
                if (property.ObjectType == ObjectType.Accessory && !accessories) continue;
                if (property.ObjectType == ObjectType.Hair && !hair) continue;
                if ((property.ObjectType == ObjectType.Clothing || property.ObjectType == ObjectType.Accessory) && property.CoordinateIndex != CurrentCoordinateIndex) continue;
                if (property.ObjectType == ObjectType.Character && !body) continue;

                MaterialAPI.SetName(FindGameObject(property.ObjectType, property.Slot), property.Renderer, property.MaterialName, property.Value);
            }

            for (var i = 0; i < MaterialShaderList.Count; i++)
            {
                var property = MaterialShaderList[i];
                if (property.ObjectType == ObjectType.Clothing && !clothes) continue;
                if (property.ObjectType == ObjectType.Accessory && !accessories) continue;
                if (property.ObjectType == ObjectType.Hair && !hair) continue;
                if ((property.ObjectType == ObjectType.Clothing || property.ObjectType == ObjectType.Accessory) && property.CoordinateIndex != CurrentCoordinateIndex) continue;
                if (property.ObjectType == ObjectType.Character && !body) continue;

#if KK || EC || KKS
                if (property.ObjectType == ObjectType.Character && MaterialEditorPlugin.EyeMaterials.Contains(property.MaterialName))
                {
                    SetShader(FindGameObject(property.ObjectType, property.Slot), property.MaterialName, property.ShaderName, true);
                }
                else
#endif
                {
                    SetShader(FindGameObject(property.ObjectType, property.Slot), property.MaterialName, property.ShaderName);
                }
                SetRenderQueue(FindGameObject(property.ObjectType, property.Slot), property.MaterialName, property.RenderQueue);
            }
            for (var i = 0; i < RendererPropertyList.Count; i++)
            {
                var property = RendererPropertyList[i];
#if KK
                if (property.Property == RendererProperties.UpdateWhenOffscreen) continue;
#endif
                if (property.ObjectType == ObjectType.Clothing && !clothes) continue;
                if (property.ObjectType == ObjectType.Accessory && !accessories) continue;
                if (property.ObjectType == ObjectType.Hair && !hair) continue;
                if ((property.ObjectType == ObjectType.Clothing || property.ObjectType == ObjectType.Accessory) && property.CoordinateIndex != CurrentCoordinateIndex) continue;
                if (property.ObjectType == ObjectType.Character && !body) continue;

                MaterialAPI.SetRendererProperty(FindGameObject(property.ObjectType, property.Slot), property.RendererName, property.Property, property.Value);
            }
            for (var i = 0; i < MaterialFloatPropertyList.Count; i++)
            {
                var property = MaterialFloatPropertyList[i];
                if (property.ObjectType == ObjectType.Clothing && !clothes) continue;
                if (property.ObjectType == ObjectType.Accessory && !accessories) continue;
                if (property.ObjectType == ObjectType.Hair && !hair) continue;
                if ((property.ObjectType == ObjectType.Clothing || property.ObjectType == ObjectType.Accessory) && property.CoordinateIndex != CurrentCoordinateIndex) continue;
                var go = FindGameObject(property.ObjectType, property.Slot);
                if (Instance.CheckBlacklist(property.MaterialName, property.Property)) continue;
                if (property.ObjectType == ObjectType.Character && !body) continue;

                SetFloat(go, property.MaterialName, property.Property, float.Parse(property.Value));
            }
            for (var i = 0; i < MaterialKeywordPropertyList.Count; i++)
            {
                var property = MaterialKeywordPropertyList[i];
                if (property.ObjectType == ObjectType.Clothing && !clothes) continue;
                if (property.ObjectType == ObjectType.Accessory && !accessories) continue;
                if (property.ObjectType == ObjectType.Hair && !hair) continue;
                if ((property.ObjectType == ObjectType.Clothing || property.ObjectType == ObjectType.Accessory) && property.CoordinateIndex != CurrentCoordinateIndex) continue;
                var go = FindGameObject(property.ObjectType, property.Slot);
                if (Instance.CheckBlacklist(property.MaterialName, property.Property)) continue;
                if (property.ObjectType == ObjectType.Character && !body) continue;

                SetKeyword(go, property.MaterialName, property.Property, property.Value);
            }
            for (var i = 0; i < MaterialColorPropertyList.Count; i++)
            {
                var property = MaterialColorPropertyList[i];
                if (property.ObjectType == ObjectType.Clothing && !clothes) continue;
                if (property.ObjectType == ObjectType.Accessory && !accessories) continue;
                if (property.ObjectType == ObjectType.Hair && !hair) continue;
                if ((property.ObjectType == ObjectType.Clothing || property.ObjectType == ObjectType.Accessory) && property.CoordinateIndex != CurrentCoordinateIndex) continue;
                var go = FindGameObject(property.ObjectType, property.Slot);
                if (Instance.CheckBlacklist(property.MaterialName, property.Property)) continue;
                if (property.ObjectType == ObjectType.Character && !body) continue;

                SetColor(go, property.MaterialName, property.Property, property.Value);
            }
            for (var i = 0; i < MaterialTexturePropertyList.Count; i++)
            {
                var property = MaterialTexturePropertyList[i];
                if (property.ObjectType == ObjectType.Clothing && !clothes) continue;
                if (property.ObjectType == ObjectType.Accessory && !accessories) continue;
                if (property.ObjectType == ObjectType.Hair && !hair) continue;
                if ((property.ObjectType == ObjectType.Clothing || property.ObjectType == ObjectType.Accessory) && property.CoordinateIndex != CurrentCoordinateIndex) continue;
                if (property.ObjectType == ObjectType.Character && !body) continue;
                var go = FindGameObject(property.ObjectType, property.Slot);
                if (Instance.CheckBlacklist(property.MaterialName, property.Property)) continue;

                SetTextureWithProperty(go, property);
                SetTextureOffset(go, property.MaterialName, property.Property, property.Offset);
                SetTextureScale(go, property.MaterialName, property.Property, property.Scale);
            }
            for (var i = 0; i < ProjectorPropertyList.Count; i++)
            {
                var property = ProjectorPropertyList[i];
                if (property.ObjectType == ObjectType.Clothing && !clothes) continue;
                if (property.ObjectType == ObjectType.Accessory && !accessories) continue;
                if (property.ObjectType == ObjectType.Hair && !hair) continue;
                if ((property.ObjectType == ObjectType.Clothing || property.ObjectType == ObjectType.Accessory) && property.CoordinateIndex != CurrentCoordinateIndex) continue;
                if (property.ObjectType == ObjectType.Character && !body) continue;

                MaterialAPI.SetProjectorProperty(FindGameObject(property.ObjectType, property.Slot), property.ProjectorName, property.Property, float.Parse(property.Value));
            }


#if KK || EC || KKS
            if (MaterialEditorPlugin.RimRemover.Value)
                RemoveRim();
#endif
        }
        /// <summary>
        /// Corrects the tongue materials since some of them are not properly refreshed on replacing a character
        /// </summary>
        private void CorrectTongue()
        {
#if KK || KKS
            if (!ChaControl.hiPoly) return;
#endif

#if KK || EC || KKS || AI || HS2
            //Get the tongue material used by the head since this one is properly refreshed with every character reload
            Material tongueMat = null;
            foreach (var renderer in GetRendererList(ChaControl.objHead))
            {
                var mat = GetMaterials(ChaControl.gameObject, renderer).FirstOrDefault(x => x.name.Contains("tang"));
                if (mat != null)
                    tongueMat = mat;
            }

            //Set the materials of the other tongues to the one from the head
            if (tongueMat != null)
            {
                string shaderName = tongueMat.shader.NameFormatted();
                string materialName = tongueMat.NameFormatted();

                SetShader(ChaControl.gameObject, materialName, shaderName);

                foreach (var property in XMLShaderProperties[XMLShaderProperties.ContainsKey(shaderName) ? shaderName : "default"])
                {
                    if (property.Value.Type == ShaderPropertyType.Color)
                        SetColor(ChaControl.gameObject, materialName, property.Key, tongueMat.GetColor("_" + property.Key));
                    else if (property.Value.Type == ShaderPropertyType.Float)
                        SetFloat(ChaControl.gameObject, materialName, property.Key, tongueMat.GetFloat("_" + property.Key));
                    else if (property.Value.Type == ShaderPropertyType.Texture)
                        SetTexture(ChaControl.gameObject, materialName, property.Key, tongueMat.GetTexture("_" + property.Key));
                    else if (property.Value.Type == ShaderPropertyType.Keyword)
                        SetKeyword(ChaControl.gameObject, materialName, property.Key, tongueMat.IsKeywordEnabled("_" + property.Key));
                }
            }
#endif
        }

#if KK || KKS
        /// <summary>
        /// Force reload face textures
        /// </summary>
        private void CorrectFace()
        {
            ChaControl.ChangeSettingEyebrow();
            ChaControl.ChangeSettingEye(true, true, true);
            ChaControl.ChangeSettingEyeHiUp();
            ChaControl.ChangeSettingEyeHiDown();
            ChaControl.ChangeSettingEyelineUp();
            ChaControl.ChangeSettingEyelineDown();
            ChaControl.ChangeSettingWhiteOfEye(true, true);
            ChaControl.ChangeSettingNose();
        }
#endif

#if KK || EC || KKS
        private void RemoveRim()
        {
            for (var i = 0; i < ChaControl.objClothes.Length; i++)
                RemoveRimClothes(i);
            for (var i = 0; i < ChaControl.objHair.Length; i++)
                RemoveRimHair(i);
            for (var i = 0; i < ChaControl.GetAccessoryObjects().Length; i++)
                RemoveRimAccessory(i);
        }
        private void RemoveRimClothes(int slot)
        {
            var go = ChaControl.objClothes[slot];
            foreach (var renderer in GetRendererList(go))
                foreach (var material in GetMaterials(go, renderer))
                    if (material.HasProperty("_rimV") && GetMaterialFloatPropertyValue(slot, ObjectType.Clothing, material, "rimV", go) == null)
                        SetMaterialFloatProperty(slot, ObjectType.Clothing, material, "rimV", 0, go);
        }
        private IEnumerator RemoveRimHairCo(int slot)
        {
            yield return null;
            RemoveRimHair(slot);
        }
        private void RemoveRimHair(int slot)
        {
            var go = ChaControl.objHair[slot];
            foreach (var renderer in GetRendererList(go))
                foreach (var material in GetMaterials(go, renderer))
                    if (material.HasProperty("_rimV") && GetMaterialFloatPropertyValue(slot, ObjectType.Hair, material, "rimV", go) == null)
                        SetMaterialFloatProperty(slot, ObjectType.Hair, material, "rimV", 0, go);
        }
        private void RemoveRimAccessory(int slot)
        {
            var go = ChaControl.GetAccessoryObject(slot);
            if (go != null)
                foreach (var renderer in GetRendererList(go))
                    foreach (var material in GetMaterials(go, renderer))
                        if (material.HasProperty("_rimV") && GetMaterialFloatPropertyValue(slot, ObjectType.Accessory, material, "rimV", go) == null)
                            SetMaterialFloatProperty(slot, ObjectType.Accessory, material, "rimV", 0, go);
        }
#endif

        /// <summary>
        /// Finds the texture bytes in the dictionary of textures and returns its ID. If not found, adds the texture to the dictionary and returns the ID of the added texture.
        /// </summary>
        private int SetAndGetTextureID(byte[] textureBytes)
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

    }
}
