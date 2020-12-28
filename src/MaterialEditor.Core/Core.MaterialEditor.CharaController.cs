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
#if AI || HS2
using AIChara;
#endif
#if PH
using ChaFileCoordinate = Character.CustomParameter;
using ChaControl = Human;
#endif

namespace KK_Plugins.MaterialEditor
{
    /// <summary>
    /// KKAPI character controller that handles saving and loading character data as well as provides methods to get or set the saved data
    /// </summary>
    public class MaterialEditorCharaController : CharaCustomFunctionController
    {
        private readonly List<RendererProperty> RendererPropertyList = new List<RendererProperty>();
        private readonly List<MaterialFloatProperty> MaterialFloatPropertyList = new List<MaterialFloatProperty>();
        private readonly List<MaterialColorProperty> MaterialColorPropertyList = new List<MaterialColorProperty>();
        private readonly List<MaterialTextureProperty> MaterialTexturePropertyList = new List<MaterialTextureProperty>();
        private readonly List<MaterialShader> MaterialShaderList = new List<MaterialShader>();

        private readonly Dictionary<int, TextureContainer> TextureDictionary = new Dictionary<int, TextureContainer>();

        /// <summary>
        /// Index of the currently worn coordinate. Always 0 except for in Koikatsu
        /// </summary>
#if KK
        public int CurrentCoordinateIndex => ChaControl.fileStatus.coordinateType;
#else
        public int CurrentCoordinateIndex => 0;
#endif
        private string FileToSet;
        private string PropertyToSet;
        private Material MatToSet;
        private int SlotToSet;
        private ObjectType ObjectTypeToSet;
        private GameObject GameObjectToSet;

        /// <summary>
        /// Handles saving data to character cards
        /// </summary>
        /// <param name="currentGameMode"></param>
        protected override void OnCardBeingSaved(GameMode currentGameMode)
        {
            var data = new PluginData();

            List<int> IDsToPurge = new List<int>();
            foreach (int texID in TextureDictionary.Keys)
                if (MaterialTexturePropertyList.All(x => x.TexID != texID))
                    IDsToPurge.Add(texID);

            for (var i = 0; i < IDsToPurge.Count; i++)
            {
                int texID = IDsToPurge[i];
                if (TextureDictionary.TryGetValue(texID, out var val)) val.Dispose();
                TextureDictionary.Remove(texID);
            }

            if (TextureDictionary.Count > 0)
                data.data.Add(nameof(TextureDictionary), MessagePackSerializer.Serialize(TextureDictionary.ToDictionary(pair => pair.Key, pair => pair.Value.Data)));
            else
                data.data.Add(nameof(TextureDictionary), null);

            if (RendererPropertyList.Count > 0)
                data.data.Add(nameof(RendererPropertyList), MessagePackSerializer.Serialize(RendererPropertyList));
            else
                data.data.Add(nameof(RendererPropertyList), null);

            if (MaterialFloatPropertyList.Count > 0)
                data.data.Add(nameof(MaterialFloatPropertyList), MessagePackSerializer.Serialize(MaterialFloatPropertyList));
            else
                data.data.Add(nameof(MaterialFloatPropertyList), null);

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

            SetExtendedData(data);
        }

        /// <summary>
        /// Handles loading data from character cards
        /// </summary>
        /// <param name="currentGameMode"></param>
        /// <param name="maintainState"></param>
        protected override void OnReload(GameMode currentGameMode, bool maintainState)
        {
            if (!maintainState)
            {
                RemoveMaterialCopies(ChaControl.gameObject);

                List<ObjectType> objectTypesToLoad = new List<ObjectType>();

                var loadFlags = MakerAPI.GetCharacterLoadFlags();
                if (loadFlags == null)
                {
                    RendererPropertyList.Clear();
                    MaterialFloatPropertyList.Clear();
                    MaterialColorPropertyList.Clear();
                    MaterialTexturePropertyList.Clear();
                    MaterialShaderList.Clear();

                    objectTypesToLoad.Add(ObjectType.Accessory);
                    objectTypesToLoad.Add(ObjectType.Character);
                    objectTypesToLoad.Add(ObjectType.Clothing);
                    objectTypesToLoad.Add(ObjectType.Hair);
                }
                else
                {
                    if (loadFlags.Face || loadFlags.Body)
                    {
                        RendererPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Character);
                        MaterialFloatPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Character);
                        MaterialColorPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Character);
                        MaterialTexturePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Character);
                        MaterialShaderList.RemoveAll(x => x.ObjectType == ObjectType.Character);

                        objectTypesToLoad.Add(ObjectType.Character);
                    }
                    if (loadFlags.Clothes)
                    {
                        RendererPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing);
                        MaterialFloatPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing);
                        MaterialColorPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing);
                        MaterialTexturePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing);
                        MaterialShaderList.RemoveAll(x => x.ObjectType == ObjectType.Clothing);
                        objectTypesToLoad.Add(ObjectType.Clothing);

                        RendererPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory);
                        MaterialFloatPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory);
                        MaterialColorPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory);
                        MaterialTexturePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory);
                        MaterialShaderList.RemoveAll(x => x.ObjectType == ObjectType.Accessory);
                        objectTypesToLoad.Add(ObjectType.Accessory);
                    }
                    if (loadFlags.Hair)
                    {
                        RendererPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Hair);
                        MaterialFloatPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Hair);
                        MaterialColorPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Hair);
                        MaterialTexturePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Hair);
                        MaterialShaderList.RemoveAll(x => x.ObjectType == ObjectType.Hair);
                        objectTypesToLoad.Add(ObjectType.Hair);
                    }
                }

                List<int> IDsToPurge = new List<int>();
                foreach (int texID in TextureDictionary.Keys)
                    if (MaterialTexturePropertyList.All(x => x.TexID != texID))
                        IDsToPurge.Add(texID);

                for (var i = 0; i < IDsToPurge.Count; i++)
                {
                    int texID = IDsToPurge[i];
                    TextureDictionary[texID].Dispose();
                    TextureDictionary.Remove(texID);
                }

                CharacterLoading = true;

                var data = GetExtendedData();
                if (data != null)
                {
                    var importDictionary = new Dictionary<int, int>();

                    if (data.data.TryGetValue(nameof(TextureDictionary), out var texDic) && texDic != null)
                        foreach (var x in MessagePackSerializer.Deserialize<Dictionary<int, byte[]>>((byte[])texDic))
                            importDictionary[x.Key] = SetAndGetTextureID(x.Value);

                    //Debug for dumping all textures
                    //int counter = 0;
                    //foreach (var tex in TextureDictionary.Values)
                    //{
                    //    string filename = Path.Combine(MaterialEditorPlugin.ExportPath, $"_Export_{ChaControl.chaFile.parameter.fullname.Trim()}_{counter}.png");
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
                            if (objectTypesToLoad.Contains(loadedProperty.ObjectType))
                            {
                                int? texID = null;
                                if (loadedProperty.TexID != null)
                                    texID = importDictionary[(int)loadedProperty.TexID];
                                int coordinateIndex = loadedProperty.ObjectType == ObjectType.Character ? 0 : loadedProperty.CoordinateIndex;
                                MaterialTextureProperty newTextureProperty = new MaterialTextureProperty(loadedProperty.ObjectType, coordinateIndex, loadedProperty.Slot, loadedProperty.MaterialName, loadedProperty.Property, texID, loadedProperty.Offset, loadedProperty.OffsetOriginal, loadedProperty.Scale, loadedProperty.ScaleOriginal);
                                MaterialTexturePropertyList.Add(newTextureProperty);
                            }
                        }
                    }
                }
            }

            ChaControl.StartCoroutine(LoadData(true, true, true));
        }

        /// <summary>
        /// Used by SetMaterialTextureFromFile if setTexInUpdate is true, needed for loading files via file dialogue
        /// </summary>
        internal new void Update()
        {
            try
            {
                if (FileToSet != null)
                    SetMaterialTextureFromFile(SlotToSet, ObjectTypeToSet, MatToSet, PropertyToSet, FileToSet, GameObjectToSet);
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
                GameObjectToSet = null;
            }
            base.Update();
        }

        /// <summary>
        /// Handles saving data to coordinate cards
        /// </summary>
        /// <param name="coordinate"></param>
        protected override void OnCoordinateBeingSaved(ChaFileCoordinate coordinate)
        {
            var data = new PluginData();

            var coordinateRendererPropertyList = RendererPropertyList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex && x.ObjectType != ObjectType.Hair && x.ObjectType != ObjectType.Character).ToList();
            var coordinateMaterialFloatPropertyList = MaterialFloatPropertyList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex && x.ObjectType != ObjectType.Hair && x.ObjectType != ObjectType.Character).ToList();
            var coordinateMaterialColorPropertyList = MaterialColorPropertyList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex && x.ObjectType != ObjectType.Hair && x.ObjectType != ObjectType.Character).ToList();
            var coordinateMaterialTexturePropertyList = MaterialTexturePropertyList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex && x.ObjectType != ObjectType.Hair && x.ObjectType != ObjectType.Character).ToList();
            var coordinateMaterialShaderList = MaterialShaderList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex && x.ObjectType != ObjectType.Hair && x.ObjectType != ObjectType.Character).ToList();
            var coordinateTextureDictionary = new Dictionary<int, byte[]>();

            foreach (var tex in TextureDictionary)
                if (coordinateMaterialTexturePropertyList.Any(x => x.TexID == tex.Key))
                    coordinateTextureDictionary.Add(tex.Key, tex.Value.Data);

            if (coordinateTextureDictionary.Count > 0)
                data.data.Add(nameof(TextureDictionary), MessagePackSerializer.Serialize(coordinateTextureDictionary));
            else
                data.data.Add(nameof(TextureDictionary), null);

            if (coordinateRendererPropertyList.Count > 0)
                data.data.Add(nameof(RendererPropertyList), MessagePackSerializer.Serialize(coordinateRendererPropertyList));
            else
                data.data.Add(nameof(RendererPropertyList), null);

            if (coordinateMaterialFloatPropertyList.Count > 0)
                data.data.Add(nameof(MaterialFloatPropertyList), MessagePackSerializer.Serialize(coordinateMaterialFloatPropertyList));
            else
                data.data.Add(nameof(MaterialFloatPropertyList), null);

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

            SetCoordinateExtendedData(coordinate, data);

            base.OnCoordinateBeingSaved(coordinate);
        }

        /// <summary>
        /// Handles loading data from coordinate cards
        /// </summary>
        /// <param name="coordinate"></param>
        /// <param name="maintainState"></param>
        protected override void OnCoordinateBeingLoaded(ChaFileCoordinate coordinate, bool maintainState)
        {
            List<ObjectType> objectTypesToLoad = new List<ObjectType>();

            var loadFlags = MakerAPI.GetCoordinateLoadFlags();
            if (loadFlags == null)
            {
                RendererPropertyList.RemoveAll(x => (x.ObjectType == ObjectType.Clothing || x.ObjectType == ObjectType.Accessory) && x.CoordinateIndex == CurrentCoordinateIndex);
                MaterialFloatPropertyList.RemoveAll(x => (x.ObjectType == ObjectType.Clothing || x.ObjectType == ObjectType.Accessory) && x.CoordinateIndex == CurrentCoordinateIndex);
                MaterialColorPropertyList.RemoveAll(x => (x.ObjectType == ObjectType.Clothing || x.ObjectType == ObjectType.Accessory) && x.CoordinateIndex == CurrentCoordinateIndex);
                MaterialTexturePropertyList.RemoveAll(x => (x.ObjectType == ObjectType.Clothing || x.ObjectType == ObjectType.Accessory) && x.CoordinateIndex == CurrentCoordinateIndex);
                MaterialShaderList.RemoveAll(x => (x.ObjectType == ObjectType.Clothing || x.ObjectType == ObjectType.Accessory) && x.CoordinateIndex == CurrentCoordinateIndex);

                objectTypesToLoad.Add(ObjectType.Accessory);
                objectTypesToLoad.Add(ObjectType.Clothing);
            }
            else
            {
                if (loadFlags.Clothes)
                {
                    RendererPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == CurrentCoordinateIndex);
                    MaterialFloatPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == CurrentCoordinateIndex);
                    MaterialColorPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == CurrentCoordinateIndex);
                    MaterialTexturePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == CurrentCoordinateIndex);
                    MaterialShaderList.RemoveAll(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == CurrentCoordinateIndex);
                    objectTypesToLoad.Add(ObjectType.Clothing);
                }
                if (loadFlags.Accessories)
                {
                    RendererPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex);
                    MaterialFloatPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex);
                    MaterialColorPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex);
                    MaterialTexturePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex);
                    MaterialShaderList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex);
                    objectTypesToLoad.Add(ObjectType.Accessory);
                }
            }

            var data = GetCoordinateExtendedData(coordinate);
            if (data?.data != null)
            {
                var importDictionary = new Dictionary<int, int>();

                if (data.data.TryGetValue(nameof(TextureDictionary), out var texDic) && texDic != null)
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

                            MaterialTextureProperty newTextureProperty = new MaterialTextureProperty(loadedProperty.ObjectType, CurrentCoordinateIndex, loadedProperty.Slot, loadedProperty.MaterialName, loadedProperty.Property, texID, loadedProperty.Offset, loadedProperty.OffsetOriginal, loadedProperty.Scale, loadedProperty.ScaleOriginal);
                            MaterialTexturePropertyList.Add(newTextureProperty);
                        }
                    }
                }
            }

            CoordinateChanging = true;

            if (MakerAPI.InsideAndLoaded)
                MaterialEditorUI.Visible = false;

            ChaControl.StartCoroutine(LoadData(true, true, false));
            base.OnCoordinateBeingLoaded(coordinate, maintainState);
        }

        private IEnumerator LoadData(bool clothes, bool accessories, bool hair)
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

            CorrectTongue();

            for (var i = 0; i < MaterialShaderList.Count; i++)
            {
                var property = MaterialShaderList[i];
                if (property.ObjectType == ObjectType.Clothing && !clothes) continue;
                if (property.ObjectType == ObjectType.Accessory && !accessories) continue;
                if (property.ObjectType == ObjectType.Hair && !hair) continue;
                if ((property.ObjectType == ObjectType.Clothing || property.ObjectType == ObjectType.Accessory) && property.CoordinateIndex != CurrentCoordinateIndex) continue;

                SetShader(FindGameObject(property.ObjectType, property.Slot), property.MaterialName, property.ShaderName);
                SetRenderQueue(FindGameObject(property.ObjectType, property.Slot), property.MaterialName, property.RenderQueue);
            }
            for (var i = 0; i < RendererPropertyList.Count; i++)
            {
                var property = RendererPropertyList[i];
                if (property.ObjectType == ObjectType.Clothing && !clothes) continue;
                if (property.ObjectType == ObjectType.Accessory && !accessories) continue;
                if (property.ObjectType == ObjectType.Hair && !hair) continue;
                if ((property.ObjectType == ObjectType.Clothing || property.ObjectType == ObjectType.Accessory) && property.CoordinateIndex != CurrentCoordinateIndex) continue;

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

                SetFloat(go, property.MaterialName, property.Property, float.Parse(property.Value));
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

                SetColor(go, property.MaterialName, property.Property, property.Value);
            }
            for (var i = 0; i < MaterialTexturePropertyList.Count; i++)
            {
                var property = MaterialTexturePropertyList[i];
                if (property.ObjectType == ObjectType.Clothing && !clothes) continue;
                if (property.ObjectType == ObjectType.Accessory && !accessories) continue;
                if (property.ObjectType == ObjectType.Hair && !hair) continue;
                if ((property.ObjectType == ObjectType.Clothing || property.ObjectType == ObjectType.Accessory) && property.CoordinateIndex != CurrentCoordinateIndex) continue;
                var go = FindGameObject(property.ObjectType, property.Slot);
                if (Instance.CheckBlacklist(property.MaterialName, property.Property)) continue;

                if (property.TexID != null)
                    SetTexture(go, property.MaterialName, property.Property, TextureDictionary[(int)property.TexID].Texture);
                SetTextureOffset(go, property.MaterialName, property.Property, property.Offset);
                SetTextureScale(go, property.MaterialName, property.Property, property.Scale);
            }


#if KK || EC
            if (MaterialEditorPlugin.RimRemover.Value)
                RemoveRim();
#endif
        }
        /// <summary>
        /// Corrects the tongue materials since some of them are not properly refreshed on replacing a character
        /// </summary>
        private void CorrectTongue()
        {
#if KK
            if (!ChaControl.hiPoly) return;
#endif

#if KK || EC
            //Get the tongue material used by the head since this one is properly refreshed with every character reload
            Material tongueMat = null;
            foreach (var renderer in GetRendererList(ChaControl.objHead.gameObject))
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
                        SetTexture(ChaControl.gameObject, materialName, property.Key, (Texture2D)tongueMat.GetTexture("_" + property.Key));
                }
            }
#endif
        }

#if KK || EC
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
                if (tex.Value.Data.SequenceEqual(textureBytes))
                    return tex.Key;
                else if (tex.Key > highestID)
                    highestID = tex.Key;

            highestID++;
            TextureDictionary.Add(highestID, new TextureContainer(textureBytes));
            return highestID;
        }

        internal void ClothesStateChangeEvent()
        {
            if (CoordinateChanging) return;
            if (MakerAPI.InsideMaker) return;

            ChaControl.StartCoroutine(LoadData(true, false, false));
        }

        internal void CoordinateChangeEvent()
        {
            CoordinateChanging = true;

            ChaControl.StartCoroutine(LoadData(true, true, false));

            if (MakerAPI.InsideAndLoaded)
                MaterialEditorUI.Visible = false;
        }

#if KK
        internal void ClothingCopiedEvent(int copySource, int copyDestination, List<int> copySlots)
        {
            for (var i = 0; i < copySlots.Count; i++)
            {
                int slot = copySlots[i];
                MaterialShaderList.RemoveAll(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == copyDestination && x.Slot == slot);
                RendererPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == copyDestination && x.Slot == slot);
                MaterialFloatPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == copyDestination && x.Slot == slot);
                MaterialColorPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == copyDestination && x.Slot == slot);
                MaterialTexturePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == copyDestination && x.Slot == slot);

                List<MaterialShader> newAccessoryMaterialShaderList = new List<MaterialShader>();
                List<RendererProperty> newAccessoryRendererPropertyList = new List<RendererProperty>();
                List<MaterialFloatProperty> newAccessoryMaterialFloatPropertyList = new List<MaterialFloatProperty>();
                List<MaterialColorProperty> newAccessoryMaterialColorPropertyList = new List<MaterialColorProperty>();
                List<MaterialTextureProperty> newAccessoryMaterialTexturePropertyList = new List<MaterialTextureProperty>();

                foreach (var property in MaterialShaderList.Where(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == copySource && x.Slot == slot))
                    newAccessoryMaterialShaderList.Add(new MaterialShader(property.ObjectType, copyDestination, slot, property.MaterialName, property.ShaderName, property.ShaderNameOriginal, property.RenderQueue, property.RenderQueueOriginal));
                foreach (var property in RendererPropertyList.Where(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == copySource && x.Slot == slot))
                    newAccessoryRendererPropertyList.Add(new RendererProperty(property.ObjectType, copyDestination, slot, property.RendererName, property.Property, property.Value, property.ValueOriginal));
                foreach (var property in MaterialFloatPropertyList.Where(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == copySource && x.Slot == slot))
                    newAccessoryMaterialFloatPropertyList.Add(new MaterialFloatProperty(property.ObjectType, copyDestination, slot, property.MaterialName, property.Property, property.Value, property.ValueOriginal));
                foreach (var property in MaterialColorPropertyList.Where(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == copySource && x.Slot == slot))
                    newAccessoryMaterialColorPropertyList.Add(new MaterialColorProperty(property.ObjectType, copyDestination, slot, property.MaterialName, property.Property, property.Value, property.ValueOriginal));
                foreach (var property in MaterialTexturePropertyList.Where(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == copySource && x.Slot == slot))
                    newAccessoryMaterialTexturePropertyList.Add(new MaterialTextureProperty(property.ObjectType, copyDestination, slot, property.MaterialName, property.Property, property.TexID, property.Offset, property.OffsetOriginal, property.Scale, property.ScaleOriginal));

                MaterialShaderList.AddRange(newAccessoryMaterialShaderList);
                RendererPropertyList.AddRange(newAccessoryRendererPropertyList);
                MaterialFloatPropertyList.AddRange(newAccessoryMaterialFloatPropertyList);
                MaterialColorPropertyList.AddRange(newAccessoryMaterialColorPropertyList);
                MaterialTexturePropertyList.AddRange(newAccessoryMaterialTexturePropertyList);

                if (copyDestination == CurrentCoordinateIndex)
                    MaterialEditorUI.Visible = false;

                ChaControl.StartCoroutine(LoadData(true, true, false));
            }
        }
#endif

        internal void AccessoryKindChangeEvent(object sender, AccessorySlotEventArgs e)
        {
            if (AccessorySelectedSlotChanging) return;
            if (CoordinateChanging) return;

            //User switched accessories, remove all edited properties for this slot
            MaterialShaderList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SlotIndex);
            RendererPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SlotIndex);
            MaterialFloatPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SlotIndex);
            MaterialColorPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SlotIndex);
            MaterialTexturePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SlotIndex);

            if (MakerAPI.InsideAndLoaded)
                if (MaterialEditorUI.Visible && MEMaker.Instance != null)
                    MEMaker.Instance.UpdateUIAccessory();

#if KK || EC
            if (MaterialEditorPlugin.RimRemover.Value)
                RemoveRimAccessory(e.SlotIndex);
#endif
        }

        internal void AccessorySelectedSlotChangeEvent(object sender, AccessorySlotEventArgs e)
        {
            if (!MakerAPI.InsideAndLoaded) return;

            AccessorySelectedSlotChanging = true;

#if KK || EC
            if (MakerAPI.InsideAndLoaded)
                if (MaterialEditorUI.Visible && MEMaker.Instance != null)
                    MEMaker.Instance.UpdateUIAccessory();
#else
            ChaControl.StartCoroutine(LoadData(false, true, false));
            ChaControl.StartCoroutine(RefreshUI());
            IEnumerator RefreshUI()
            {
                yield return null;
                if (MakerAPI.InsideAndLoaded)
                    if (MaterialEditorUI.Visible && MEMaker.Instance != null)
                        MEMaker.Instance.UpdateUIAccessory();
            }
#endif
        }

        internal void AccessoryTransferredEvent(object sender, AccessoryTransferEventArgs e)
        {
            MaterialShaderList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.DestinationSlotIndex);
            RendererPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.DestinationSlotIndex);
            MaterialFloatPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.DestinationSlotIndex);
            MaterialColorPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.DestinationSlotIndex);
            MaterialTexturePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.DestinationSlotIndex);

            List<MaterialShader> newAccessoryMaterialShaderList = new List<MaterialShader>();
            List<RendererProperty> newAccessoryRendererPropertyList = new List<RendererProperty>();
            List<MaterialFloatProperty> newAccessoryMaterialFloatPropertyList = new List<MaterialFloatProperty>();
            List<MaterialColorProperty> newAccessoryMaterialColorPropertyList = new List<MaterialColorProperty>();
            List<MaterialTextureProperty> newAccessoryMaterialTexturePropertyList = new List<MaterialTextureProperty>();

            foreach (var property in MaterialShaderList.Where(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SourceSlotIndex))
                newAccessoryMaterialShaderList.Add(new MaterialShader(property.ObjectType, CurrentCoordinateIndex, e.DestinationSlotIndex, property.MaterialName, property.ShaderName, property.ShaderNameOriginal, property.RenderQueue, property.RenderQueueOriginal));
            foreach (var property in RendererPropertyList.Where(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SourceSlotIndex))
                newAccessoryRendererPropertyList.Add(new RendererProperty(property.ObjectType, CurrentCoordinateIndex, e.DestinationSlotIndex, property.RendererName, property.Property, property.Value, property.ValueOriginal));
            foreach (var property in MaterialFloatPropertyList.Where(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SourceSlotIndex))
                newAccessoryMaterialFloatPropertyList.Add(new MaterialFloatProperty(property.ObjectType, CurrentCoordinateIndex, e.DestinationSlotIndex, property.MaterialName, property.Property, property.Value, property.ValueOriginal));
            foreach (var property in MaterialColorPropertyList.Where(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SourceSlotIndex))
                newAccessoryMaterialColorPropertyList.Add(new MaterialColorProperty(property.ObjectType, CurrentCoordinateIndex, e.DestinationSlotIndex, property.MaterialName, property.Property, property.Value, property.ValueOriginal));
            foreach (var property in MaterialTexturePropertyList.Where(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SourceSlotIndex))
                newAccessoryMaterialTexturePropertyList.Add(new MaterialTextureProperty(property.ObjectType, CurrentCoordinateIndex, e.DestinationSlotIndex, property.MaterialName, property.Property, property.TexID, property.Offset, property.OffsetOriginal, property.Scale, property.ScaleOriginal));

            MaterialShaderList.AddRange(newAccessoryMaterialShaderList);
            RendererPropertyList.AddRange(newAccessoryRendererPropertyList);
            MaterialFloatPropertyList.AddRange(newAccessoryMaterialFloatPropertyList);
            MaterialColorPropertyList.AddRange(newAccessoryMaterialColorPropertyList);
            MaterialTexturePropertyList.AddRange(newAccessoryMaterialTexturePropertyList);

            if (MakerAPI.InsideAndLoaded)
                MaterialEditorUI.Visible = false;

            ChaControl.StartCoroutine(LoadData(true, true, false));
        }

#if KK
        internal void AccessoriesCopiedEvent(object sender, AccessoryCopyEventArgs e)
        {
            foreach (int slot in e.CopiedSlotIndexes)
            {
                MaterialShaderList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == (int)e.CopyDestination && x.Slot == slot);
                RendererPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == (int)e.CopyDestination && x.Slot == slot);
                MaterialFloatPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == (int)e.CopyDestination && x.Slot == slot);
                MaterialColorPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == (int)e.CopyDestination && x.Slot == slot);
                MaterialTexturePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == (int)e.CopyDestination && x.Slot == slot);

                List<MaterialShader> newAccessoryMaterialShaderList = new List<MaterialShader>();
                List<RendererProperty> newAccessoryRendererPropertyList = new List<RendererProperty>();
                List<MaterialFloatProperty> newAccessoryMaterialFloatPropertyList = new List<MaterialFloatProperty>();
                List<MaterialColorProperty> newAccessoryMaterialColorPropertyList = new List<MaterialColorProperty>();
                List<MaterialTextureProperty> newAccessoryMaterialTexturePropertyList = new List<MaterialTextureProperty>();

                foreach (var property in MaterialShaderList.Where(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == (int)e.CopySource && x.Slot == slot))
                    newAccessoryMaterialShaderList.Add(new MaterialShader(property.ObjectType, (int)e.CopyDestination, slot, property.MaterialName, property.ShaderName, property.ShaderNameOriginal, property.RenderQueue, property.RenderQueueOriginal));
                foreach (var property in RendererPropertyList.Where(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == (int)e.CopySource && x.Slot == slot))
                    newAccessoryRendererPropertyList.Add(new RendererProperty(property.ObjectType, (int)e.CopyDestination, slot, property.RendererName, property.Property, property.Value, property.ValueOriginal));
                foreach (var property in MaterialFloatPropertyList.Where(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == (int)e.CopySource && x.Slot == slot))
                    newAccessoryMaterialFloatPropertyList.Add(new MaterialFloatProperty(property.ObjectType, (int)e.CopyDestination, slot, property.MaterialName, property.Property, property.Value, property.ValueOriginal));
                foreach (var property in MaterialColorPropertyList.Where(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == (int)e.CopySource && x.Slot == slot))
                    newAccessoryMaterialColorPropertyList.Add(new MaterialColorProperty(property.ObjectType, (int)e.CopyDestination, slot, property.MaterialName, property.Property, property.Value, property.ValueOriginal));
                foreach (var property in MaterialTexturePropertyList.Where(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == (int)e.CopySource && x.Slot == slot))
                    newAccessoryMaterialTexturePropertyList.Add(new MaterialTextureProperty(property.ObjectType, (int)e.CopyDestination, slot, property.MaterialName, property.Property, property.TexID, property.Offset, property.OffsetOriginal, property.Scale, property.ScaleOriginal));

                MaterialShaderList.AddRange(newAccessoryMaterialShaderList);
                RendererPropertyList.AddRange(newAccessoryRendererPropertyList);
                MaterialFloatPropertyList.AddRange(newAccessoryMaterialFloatPropertyList);
                MaterialColorPropertyList.AddRange(newAccessoryMaterialColorPropertyList);
                MaterialTexturePropertyList.AddRange(newAccessoryMaterialTexturePropertyList);

                if (MakerAPI.InsideAndLoaded)
                    if ((int)e.CopyDestination == CurrentCoordinateIndex)
                        MaterialEditorUI.Visible = false;
            }
        }
#endif

        internal void ChangeAccessoryEvent(int slot, int type)
        {
#if AI || HS2
            if (type != 350) return; //type 350 = no category, accessory being removed
#elif KK || EC
            if (type != 120) //type 120 = no category, accessory being removed
            {
                if (MaterialEditorPlugin.RimRemover.Value)
                    RemoveRimAccessory(slot);
                return;
            }
#endif
            if (!MakerAPI.InsideAndLoaded) return;
            if (CoordinateChanging) return;

            MaterialShaderList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);
            RendererPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);
            MaterialFloatPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);
            MaterialColorPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);
            MaterialTexturePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);

            if (MakerAPI.InsideAndLoaded)
                MaterialEditorUI.Visible = false;
        }

        internal void ChangeCustomClothesEvent(int slot)
        {
            if (!MakerAPI.InsideAndLoaded) return;
            if (CoordinateChanging) return;
            if (ClothesChanging) return;
            if (CharacterLoading) return;
            if (RefreshingTextures) return;
            if (CustomClothesOverride) return;
            if (new System.Diagnostics.StackTrace().ToString().Contains("KoiClothesOverlayController"))
            {
                RefreshingTextures = true;
                return;
            }

            ClothesChanging = true;

            MaterialShaderList.RemoveAll(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);
            RendererPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);
            MaterialFloatPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);
            MaterialColorPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);
            MaterialTexturePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);

            if (MakerAPI.InsideAndLoaded)
                MaterialEditorUI.Visible = false;

#if KK || EC
            if (MaterialEditorPlugin.RimRemover.Value)
                RemoveRimClothes(slot);
#elif PH
            //Reapply edits for other clothes since they will have been undone
            ChaControl.StartCoroutine(LoadData(true, true, false));
#endif
        }

        internal void ChangeHairEvent(int slot)
        {
            if (!MakerAPI.InsideAndLoaded) return;
            if (CharacterLoading) return;

            MaterialShaderList.RemoveAll(x => x.ObjectType == ObjectType.Hair && x.Slot == slot);
            RendererPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Hair && x.Slot == slot);
            MaterialFloatPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Hair && x.Slot == slot);
            MaterialColorPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Hair && x.Slot == slot);
            MaterialTexturePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Hair && x.Slot == slot);

            if (MakerAPI.InsideAndLoaded)
                MaterialEditorUI.Visible = false;

#if KK || EC
            if (MaterialEditorPlugin.RimRemover.Value)
                StartCoroutine(RemoveRimHairCo(slot));
#elif PH
            //Reapply edits for other hairs since they will have been undone
            ChaControl.StartCoroutine(LoadData(false, false, true));
#endif
        }
        /// <summary>
        /// Refresh the clothes MainTex, typically called after editing colors in the character maker
        /// </summary>
        public void RefreshClothesMainTex() => StartCoroutine(RefreshClothesMainTexCoroutine());
        private IEnumerator RefreshClothesMainTexCoroutine()
        {
            yield return new WaitForEndOfFrame();
            for (var i = 0; i < MaterialTexturePropertyList.Count; i++)
            {
                var property = MaterialTexturePropertyList[i];
                if (Instance.CheckBlacklist(property.MaterialName, property.Property))
                    continue;

                if (property.ObjectType == ObjectType.Clothing && property.CoordinateIndex == CurrentCoordinateIndex && property.Property == "MainTex")
                    if (property.TexID != null)
                        SetTexture(FindGameObject(ObjectType.Clothing, property.Slot), property.MaterialName, property.Property, TextureDictionary[(int)property.TexID].Texture);
            }
        }
        /// <summary>
        /// Refresh the body MainTex, typically called after editing colors in the character maker
        /// </summary>
        public void RefreshBodyMainTex() => StartCoroutine(RefreshBodyMainTexCoroutine());
        private IEnumerator RefreshBodyMainTexCoroutine()
        {
            yield return new WaitForEndOfFrame();

            for (var i = 0; i < MaterialTexturePropertyList.Count; i++)
            {
                var property = MaterialTexturePropertyList[i];
                if (Instance.CheckBlacklist(property.MaterialName, property.Property))
                    continue;

                if (property.ObjectType == ObjectType.Character && property.Property == "MainTex")
                    if (property.TexID != null)
                        SetTexture(ChaControl.gameObject, property.MaterialName, property.Property, TextureDictionary[(int)property.TexID].Texture);
            }
        }
        /// <summary>
        /// Reapply all edits to the body and face
        /// </summary>
        public void RefreshBodyEdits()
        {
            if (CharacterLoading) return;
            StartCoroutine(LoadData(false, false, false));
        }
        /// <summary>
        /// Copy any edits for the specified object
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="go">GameObject the material belongs to</param>
        public void MaterialCopyEdits(int slot, ObjectType objectType, Material material, GameObject go)
        {
            CopyData.ClearAll();

            foreach (var materialShader in MaterialShaderList.Where(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.MaterialName == material.NameFormatted()))
                CopyData.MaterialShaderList.Add(new CopyContainer.MaterialShader(materialShader.ShaderName, materialShader.RenderQueue));
            foreach (var materialFloatProperty in MaterialFloatPropertyList.Where(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.MaterialName == material.NameFormatted()))
                CopyData.MaterialFloatPropertyList.Add(new CopyContainer.MaterialFloatProperty(materialFloatProperty.Property, float.Parse(materialFloatProperty.Value)));
            foreach (var materialColorProperty in MaterialColorPropertyList.Where(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.MaterialName == material.NameFormatted()))
                CopyData.MaterialColorPropertyList.Add(new CopyContainer.MaterialColorProperty(materialColorProperty.Property, materialColorProperty.Value));
            foreach (var materialTextureProperty in MaterialTexturePropertyList.Where(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.MaterialName == material.NameFormatted()))
            {
                if (materialTextureProperty.TexID != null)
                    CopyData.MaterialTexturePropertyList.Add(new CopyContainer.MaterialTextureProperty(materialTextureProperty.Property, TextureDictionary[(int)materialTextureProperty.TexID].Data, materialTextureProperty.Offset, materialTextureProperty.Scale));
                else
                    CopyData.MaterialTexturePropertyList.Add(new CopyContainer.MaterialTextureProperty(materialTextureProperty.Property, null, materialTextureProperty.Offset, materialTextureProperty.Scale));
            }
        }
        /// <summary>
        /// Paste any edits for the specified object
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void MaterialPasteEdits(int slot, ObjectType objectType, Material material, GameObject go, bool setProperty = true)
        {
            for (var i = 0; i < CopyData.MaterialShaderList.Count; i++)
            {
                var materialShader = CopyData.MaterialShaderList[i];
                if (materialShader.ShaderName != null)
                    SetMaterialShader(slot, objectType, material, materialShader.ShaderName, go, setProperty);
                if (materialShader.RenderQueue != null)
                    SetMaterialShaderRenderQueue(slot, objectType, material, (int)materialShader.RenderQueue, go, setProperty);
            }
            for (var i = 0; i < CopyData.MaterialFloatPropertyList.Count; i++)
            {
                var materialFloatProperty = CopyData.MaterialFloatPropertyList[i];
                if (material.HasProperty($"_{materialFloatProperty.Property}"))
                    SetMaterialFloatProperty(slot, objectType, material, materialFloatProperty.Property, materialFloatProperty.Value, go, setProperty);
            }
            for (var i = 0; i < CopyData.MaterialColorPropertyList.Count; i++)
            {
                var materialColorProperty = CopyData.MaterialColorPropertyList[i];
                if (material.HasProperty($"_{materialColorProperty.Property}"))
                    SetMaterialColorProperty(slot, objectType, material, materialColorProperty.Property, materialColorProperty.Value, go, setProperty);
            }
            for (var i = 0; i < CopyData.MaterialTexturePropertyList.Count; i++)
            {
                var materialTextureProperty = CopyData.MaterialTexturePropertyList[i];
                if (material.HasProperty($"_{materialTextureProperty.Property}"))
                    SetMaterialTexture(slot, objectType, material, materialTextureProperty.Property, materialTextureProperty.Data, go);
                if (materialTextureProperty.Offset != null)
                    SetMaterialTextureOffset(slot, objectType, material, materialTextureProperty.Property, (Vector2)materialTextureProperty.Offset, go, setProperty);
                if (materialTextureProperty.Scale != null)
                    SetMaterialTextureScale(slot, objectType, material, materialTextureProperty.Property, (Vector2)materialTextureProperty.Scale, go, setProperty);
            }
        }

        #region Set, Get, Remove methods
        /// <summary>
        /// Add a renderer property to be saved and loaded with the card and optionally also update the renderer.
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="renderer">Renderer being modified</param>
        /// <param name="property">Property of the renderer</param>
        /// <param name="value">Value</param>
        /// <param name="go">GameObject the renderer belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the renderer</param>
        public void SetRendererProperty(int slot, ObjectType objectType, Renderer renderer, RendererProperties property, string value, GameObject go, bool setProperty = true)
        {
            RendererProperty rendererProperty = RendererPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == property && x.RendererName == renderer.NameFormatted());
            if (rendererProperty == null)
            {
                string valueOriginal;
                if (property == RendererProperties.Enabled)
                    valueOriginal = renderer.enabled ? "1" : "0";
                else if (property == RendererProperties.ReceiveShadows)
                    valueOriginal = renderer.receiveShadows ? "1" : "0";
                else
                    valueOriginal = ((int)renderer.shadowCastingMode).ToString();

                RendererPropertyList.Add(new RendererProperty(objectType, GetCoordinateIndex(objectType), slot, renderer.NameFormatted(), property, value, valueOriginal));
            }
            else
            {
                if (value == rendererProperty.ValueOriginal)
                    RemoveRendererProperty(slot, objectType, renderer, property, go, false);
                else
                    rendererProperty.Value = value;
            }
            if (setProperty)
                MaterialAPI.SetRendererProperty(go, renderer.NameFormatted(), property, value);
        }
        /// <summary>
        /// Get the saved renderer property value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="renderer">Renderer being modified</param>
        /// <param name="property">Property of the renderer</param>
        /// <param name="go">GameObject the renderer belongs to</param>
        /// <returns>Saved renderer property value</returns>
        public string GetRendererPropertyValue(int slot, ObjectType objectType, Renderer renderer, RendererProperties property, GameObject go)
        {
            return RendererPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == property && x.RendererName == renderer.NameFormatted())?.Value;
        }
        /// <summary>
        /// Get the original value of the saved renderer property value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="renderer">Renderer being modified</param>
        /// <param name="property">Property of the renderer</param>
        /// <param name="go">GameObject the renderer belongs to</param>
        /// <returns>Saved renderer property value</returns>
        public string GetRendererPropertyValueOriginal(int slot, ObjectType objectType, Renderer renderer, RendererProperties property, GameObject go)
        {
            return RendererPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == property && x.RendererName == renderer.NameFormatted())?.ValueOriginal;
        }
        /// <summary>
        /// Remove the saved renderer property value if one is saved and optionally also update the renderer
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="renderer">Renderer being modified</param>
        /// <param name="property">Property of the renderer</param>
        /// <param name="go">GameObject the renderer belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the renderer</param>
        public void RemoveRendererProperty(int slot, ObjectType objectType, Renderer renderer, RendererProperties property, GameObject go, bool setProperty = true)
        {
            if (setProperty)
            {
                var original = GetRendererPropertyValueOriginal(slot, objectType, renderer, property, go);
                if (!original.IsNullOrEmpty())
                    MaterialAPI.SetRendererProperty(go, renderer.NameFormatted(), property, original);
            }
            RendererPropertyList.RemoveAll(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == property && x.RendererName == renderer.NameFormatted());
        }

        /// <summary>
        /// Add a float property to be saved and loaded with the card and optionally also update the materials.
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="value">Value</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void SetMaterialFloatProperty(int slot, ObjectType objectType, Material material, string propertyName, float value, GameObject go, bool setProperty = true)
        {
            var materialProperty = MaterialFloatPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted());
            if (materialProperty == null)
            {
                float valueOriginal = material.GetFloat($"_{propertyName}");
                MaterialFloatPropertyList.Add(new MaterialFloatProperty(objectType, GetCoordinateIndex(objectType), slot, material.NameFormatted(), propertyName, value.ToString(CultureInfo.InvariantCulture), valueOriginal.ToString(CultureInfo.InvariantCulture)));
            }
            else
            {
                if (value.ToString(CultureInfo.InvariantCulture) == materialProperty.ValueOriginal)
                    RemoveMaterialFloatProperty(slot, objectType, material, propertyName, go, false);
                else
                    materialProperty.Value = value.ToString(CultureInfo.InvariantCulture);
            }
            if (setProperty)
                SetFloat(go, material.NameFormatted(), propertyName, value);
        }
        /// <summary>
        /// Get the saved material property value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <returns>Saved material property value or null if none is saved</returns>
        public float? GetMaterialFloatPropertyValue(int slot, ObjectType objectType, Material material, string propertyName, GameObject go)
        {
            var value = MaterialFloatPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.Value;
            if (value.IsNullOrEmpty())
                return null;
            return float.Parse(value ?? "");
        }
        /// <summary>
        /// Get the saved material property's original value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <returns>Saved material property's original value or null if none is saved</returns>
        public float? GetMaterialFloatPropertyValueOriginal(int slot, ObjectType objectType, Material material, string propertyName, GameObject go)
        {
            var valueOriginal = MaterialFloatPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.ValueOriginal;
            if (valueOriginal.IsNullOrEmpty())
                return null;
            return float.Parse(valueOriginal ?? "");
        }
        /// <summary>
        /// Remove the saved material property value if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void RemoveMaterialFloatProperty(int slot, ObjectType objectType, Material material, string propertyName, GameObject go, bool setProperty = true)
        {
            if (setProperty)
            {
                var original = GetMaterialFloatPropertyValueOriginal(slot, objectType, material, propertyName, go);
                if (original != null)
                    SetFloat(go, material.NameFormatted(), propertyName, (float)original);
            }
            MaterialFloatPropertyList.RemoveAll(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted());
        }

        /// <summary>
        /// Add a color property to be saved and loaded with the card and optionally also update the materials.
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="value">Value</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void SetMaterialColorProperty(int slot, ObjectType objectType, Material material, string propertyName, Color value, GameObject go, bool setProperty = true)
        {
            var colorProperty = MaterialColorPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted());
            if (colorProperty == null)
            {
                Color valueOriginal = material.GetColor($"_{propertyName}");
                MaterialColorPropertyList.Add(new MaterialColorProperty(objectType, GetCoordinateIndex(objectType), slot, material.NameFormatted(), propertyName, value, valueOriginal));
            }
            else
            {
                if (value == colorProperty.ValueOriginal)
                    RemoveMaterialColorProperty(slot, objectType, material, propertyName, go, false);
                else
                    colorProperty.Value = value;
            }
            if (setProperty)
                SetColor(go, material.NameFormatted(), propertyName, value);
        }
        /// <summary>
        /// Get the saved material property value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <returns>Saved material property value or null if none is saved</returns>
        public Color? GetMaterialColorPropertyValue(int slot, ObjectType objectType, Material material, string propertyName, GameObject go)
        {
            return MaterialColorPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.Value;
        }
        /// <summary>
        /// Get the saved material property's original value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <returns>Saved material property's original value or null if none is saved</returns>
        public Color? GetMaterialColorPropertyValueOriginal(int slot, ObjectType objectType, Material material, string propertyName, GameObject go)
        {
            return MaterialColorPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.ValueOriginal;
        }
        /// <summary>
        /// Remove the saved material property value if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void RemoveMaterialColorProperty(int slot, ObjectType objectType, Material material, string propertyName, GameObject go, bool setProperty = true)
        {
            if (setProperty)
            {
                var original = GetMaterialColorPropertyValueOriginal(slot, objectType, material, propertyName, go);
                if (original != null)
                    SetColor(go, material.NameFormatted(), propertyName, (Color)original);
            }
            MaterialColorPropertyList.RemoveAll(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted());
        }

        /// <summary>
        /// Add a texture property to be saved and loaded with the card.
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="filePath">Path to the .png file on disk</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <param name="setTexInUpdate">Whether to wait for the next Update</param>
        public void SetMaterialTextureFromFile(int slot, ObjectType objectType, Material material, string propertyName, string filePath, GameObject go, bool setTexInUpdate = false)
        {
            if (!File.Exists(filePath)) return;

            if (setTexInUpdate)
            {
                FileToSet = filePath;
                PropertyToSet = propertyName;
                MatToSet = material;
                GameObjectToSet = go;
                SlotToSet = slot;
                ObjectTypeToSet = objectType;
            }
            else
            {
                var texBytes = File.ReadAllBytes(filePath);
                Texture2D tex = MaterialEditorPlugin.TextureFromBytes(texBytes);

                SetTexture(go, material.NameFormatted(), propertyName, tex);

                var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted());
                if (textureProperty == null)
                    MaterialTexturePropertyList.Add(new MaterialTextureProperty(objectType, GetCoordinateIndex(objectType), slot, material.NameFormatted(), propertyName, SetAndGetTextureID(texBytes)));
                else
                    textureProperty.TexID = SetAndGetTextureID(texBytes);
            }
        }
        /// <summary>
        /// Add a texture property to be saved and loaded with the card.
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="data">Byte array containing the texture data</param>
        /// <param name="go">GameObject the material belongs to</param>
        public void SetMaterialTexture(int slot, ObjectType objectType, Material material, string propertyName, byte[] data, GameObject go)
        {
            if (data == null) return;

            Texture2D tex = MaterialEditorPlugin.TextureFromBytes(data);

            SetTexture(go, material.NameFormatted(), propertyName, tex);

            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted());
            if (textureProperty == null)
                MaterialTexturePropertyList.Add(new MaterialTextureProperty(objectType, GetCoordinateIndex(objectType), slot, material.NameFormatted(), propertyName, SetAndGetTextureID(data)));
            else
                textureProperty.TexID = SetAndGetTextureID(data);
        }
        /// <summary>
        /// Get the saved material property value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <returns>Saved material property value or null if none is saved</returns>
        public Texture2D GetMaterialTexture(int slot, ObjectType objectType, Material material, string propertyName, GameObject go)
        {
            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted());
            if (textureProperty?.TexID != null)
                return TextureDictionary[(int)textureProperty.TexID].Texture;
            return null;
        }
        /// <summary>
        /// Get whether the texture has been changed
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <returns>True if the texture has been modified, false if not</returns>
        public bool GetMaterialTextureOriginal(int slot, ObjectType objectType, Material material, string propertyName, GameObject go)
        {
            return MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.TexID == null;
        }
        /// <summary>
        /// Remove the saved material property value if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <param name="displayMessage">Whether to display a message on screen telling the user to save and reload to refresh textures</param>
        public void RemoveMaterialTexture(int slot, ObjectType objectType, Material material, string propertyName, GameObject go, bool displayMessage = true)
        {
            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted());
            if (textureProperty != null)
            {
                if (displayMessage)
                    MaterialEditorPlugin.Logger.LogMessage("Save and reload character or change outfits to refresh textures.");
                textureProperty.TexID = null;
                if (textureProperty.NullCheck())
                    MaterialTexturePropertyList.Remove(textureProperty);
            }
        }

        /// <summary>
        /// Add a texture offset property to be saved and loaded with the card and optionally also update the materials.
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="value">Value</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void SetMaterialTextureOffset(int slot, ObjectType objectType, Material material, string propertyName, Vector2 value, GameObject go, bool setProperty = true)
        {
            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted());
            if (textureProperty == null)
            {
                Vector2 valueOriginal = material.GetTextureOffset($"_{propertyName}");
                MaterialTexturePropertyList.Add(new MaterialTextureProperty(objectType, GetCoordinateIndex(objectType), slot, material.NameFormatted(), propertyName, offset: value, offsetOriginal: valueOriginal));
            }
            else
            {
                if (value == textureProperty.OffsetOriginal)
                    RemoveMaterialTextureOffset(slot, objectType, material, propertyName, go, false);
                else
                {
                    textureProperty.Offset = value;
                    if (textureProperty.OffsetOriginal == null)
                        textureProperty.OffsetOriginal = material.GetTextureOffset($"_{propertyName}");
                }
            }
            if (setProperty)
                SetTextureOffset(go, material.NameFormatted(), propertyName, value);
        }
        /// <summary>
        /// Get the saved material property value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <returns>Saved material property value or null if none is saved</returns>
        public Vector2? GetMaterialTextureOffset(int slot, ObjectType objectType, Material material, string propertyName, GameObject go)
        {
            return MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.Offset;
        }
        /// <summary>
        /// Get the saved material property's original value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <returns>Saved material property's original value or null if none is saved</returns>
        public Vector2? GetMaterialTextureOffsetOriginal(int slot, ObjectType objectType, Material material, string propertyName, GameObject go)
        {
            return MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.OffsetOriginal;
        }
        /// <summary>
        /// Remove the saved material property value if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void RemoveMaterialTextureOffset(int slot, ObjectType objectType, Material material, string propertyName, GameObject go, bool setProperty = true)
        {
            if (setProperty)
            {
                var original = GetMaterialTextureOffsetOriginal(slot, objectType, material, propertyName, go);
                if (original != null)
                    SetTextureOffset(go, material.NameFormatted(), propertyName, (Vector2)original);
            }

            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted());
            if (textureProperty != null)
            {
                textureProperty.Offset = null;
                textureProperty.OffsetOriginal = null;
                if (textureProperty.NullCheck())
                    MaterialTexturePropertyList.Remove(textureProperty);
            }
        }

        /// <summary>
        /// Add a texture scale property to be saved and loaded with the card and optionally also update the materials.
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="value">Value</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void SetMaterialTextureScale(int slot, ObjectType objectType, Material material, string propertyName, Vector2 value, GameObject go, bool setProperty = true)
        {
            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted());
            if (textureProperty == null)
            {
                Vector2 valueOriginal = material.GetTextureScale($"_{propertyName}");
                MaterialTexturePropertyList.Add(new MaterialTextureProperty(objectType, GetCoordinateIndex(objectType), slot, material.NameFormatted(), propertyName, scale: value, scaleOriginal: valueOriginal));
            }
            else
            {
                if (value == textureProperty.ScaleOriginal)
                    RemoveMaterialTextureScale(slot, objectType, material, propertyName, go, false);
                else
                {
                    textureProperty.Scale = value;
                    if (textureProperty.ScaleOriginal == null)
                        textureProperty.ScaleOriginal = material.GetTextureScale($"_{propertyName}");
                }
            }

            if (setProperty)
                SetTextureScale(go, material.NameFormatted(), propertyName, value);
        }
        /// <summary>
        /// Get the saved material property value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <returns>Saved material property value or null if none is saved</returns>
        public Vector2? GetMaterialTextureScale(int slot, ObjectType objectType, Material material, string propertyName, GameObject go)
        {
            return MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.Scale;
        }
        /// <summary>
        /// Get the saved material property's original value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <returns>Saved material property's original value or null if none is saved</returns>
        public Vector2? GetMaterialTextureScaleOriginal(int slot, ObjectType objectType, Material material, string propertyName, GameObject go)
        {
            return MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.ScaleOriginal;
        }
        /// <summary>
        /// Remove the saved material property value if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void RemoveMaterialTextureScale(int slot, ObjectType objectType, Material material, string propertyName, GameObject go, bool setProperty = true)
        {
            if (setProperty)
            {
                var original = GetMaterialTextureScaleOriginal(slot, objectType, material, propertyName, go);
                if (original != null)
                    SetTextureScale(go, material.NameFormatted(), propertyName, (Vector2)original);
            }

            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted());
            if (textureProperty != null)
            {
                textureProperty.Scale = null;
                textureProperty.ScaleOriginal = null;
                if (textureProperty.NullCheck())
                    MaterialTexturePropertyList.Remove(textureProperty);
            }
        }

        /// <summary>
        /// Add a shader to be saved and loaded with the card and optionally also update the materials.
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="shaderName">Name of the shader to be saved, must be a shader that has been loaded by MaterialEditor</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void SetMaterialShader(int slot, ObjectType objectType, Material material, string shaderName, GameObject go, bool setProperty = true)
        {
            var materialProperty = MaterialShaderList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.MaterialName == material.NameFormatted());
            if (materialProperty == null)
            {
                string shaderNameOriginal = material.shader.NameFormatted();
                MaterialShaderList.Add(new MaterialShader(objectType, GetCoordinateIndex(objectType), slot, material.NameFormatted(), shaderName, shaderNameOriginal));
            }
            else
            {
                if (shaderName == materialProperty.ShaderNameOriginal)
                    RemoveMaterialShader(slot, objectType, material, go, false);
                else
                {
                    materialProperty.ShaderName = shaderName;
                    if (materialProperty.ShaderNameOriginal == null)
                        materialProperty.ShaderNameOriginal = material.shader.NameFormatted();
                }
            }

            if (setProperty)
            {
                RemoveMaterialShaderRenderQueue(slot, objectType, material, go, false);
                SetShader(go, material.NameFormatted(), shaderName);
            }
        }

        /// <summary>
        /// Get the saved shader name or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <returns>Saved shader name or null if none is saved</returns>
        public string GetMaterialShader(int slot, ObjectType objectType, Material material, GameObject go)
        {
            return MaterialShaderList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.MaterialName == material.NameFormatted())?.ShaderName;
        }
        /// <summary>
        /// Get the saved shader name's original value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <returns>Saved shader name's original value or null if none is saved</returns>
        public string GetMaterialShaderOriginal(int slot, ObjectType objectType, Material material, GameObject go)
        {
            return MaterialShaderList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.MaterialName == material.NameFormatted())?.ShaderNameOriginal;
        }

        /// <summary>
        /// Remove the saved shader if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void RemoveMaterialShader(int slot, ObjectType objectType, Material material, GameObject go, bool setProperty = true)
        {
            if (setProperty)
            {
                var original = GetMaterialShaderOriginal(slot, objectType, material, go);
                if (!original.IsNullOrEmpty())
                    SetShader(go, material.NameFormatted(), original);
            }

            foreach (var materialProperty in MaterialShaderList.Where(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.MaterialName == material.NameFormatted()))
            {
                materialProperty.ShaderName = null;
                materialProperty.ShaderNameOriginal = null;
            }

            MaterialShaderList.RemoveAll(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.MaterialName == material.NameFormatted() && x.NullCheck());
        }

        /// <summary>
        /// Add a shader render queue to be saved and loaded with the card and optionally also update the materials.
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="renderQueue">Value</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void SetMaterialShaderRenderQueue(int slot, ObjectType objectType, Material material, int renderQueue, GameObject go, bool setProperty = true)
        {
            var materialProperty = MaterialShaderList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.MaterialName == material.NameFormatted());
            if (materialProperty == null)
            {
                int renderQueueOriginal = material.renderQueue;
                MaterialShaderList.Add(new MaterialShader(objectType, GetCoordinateIndex(objectType), slot, material.NameFormatted(), renderQueue, renderQueueOriginal));
            }
            else
            {
                if (renderQueue == materialProperty.RenderQueueOriginal)
                    RemoveMaterialShaderRenderQueue(slot, objectType, material, go, false);
                else
                {
                    materialProperty.RenderQueue = renderQueue;
                    if (materialProperty.RenderQueueOriginal == null)
                        materialProperty.RenderQueueOriginal = material.renderQueue;
                }
            }

            if (setProperty)
                SetRenderQueue(go, material.NameFormatted(), renderQueue);
        }
        /// <summary>
        /// Get the saved render queue or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <returns>Saved render queue or null if none is saved</returns>
        public int? GetMaterialShaderRenderQueue(int slot, ObjectType objectType, Material material, GameObject go)
        {
            return MaterialShaderList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.MaterialName == material.NameFormatted())?.RenderQueue;
        }
        /// <summary>
        /// Get the saved render queue's original value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <returns>Saved render queue's original value or null if none is saved</returns>
        public int? GetMaterialShaderRenderQueueOriginal(int slot, ObjectType objectType, Material material, GameObject go)
        {
            return MaterialShaderList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.MaterialName == material.NameFormatted())?.RenderQueueOriginal;
        }
        /// <summary>
        /// Remove the saved render queue if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void RemoveMaterialShaderRenderQueue(int slot, ObjectType objectType, Material material, GameObject go, bool setProperty = true)
        {
            if (setProperty)
            {
                var original = GetMaterialShaderRenderQueueOriginal(slot, objectType, material, go);
                if (original != null)
                    SetRenderQueue(go, material.NameFormatted(), original);
            }

            foreach (var materialProperty in MaterialShaderList.Where(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.MaterialName == material.NameFormatted()))
            {
                materialProperty.RenderQueue = null;
                materialProperty.RenderQueueOriginal = null;
            }

            MaterialShaderList.RemoveAll(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.MaterialName == material.NameFormatted() && x.NullCheck());
        }

        /// <summary>
        /// Get the coordinate index based on object type, hair and character return 0, clothes and accessories return CurrentCoordinateIndex
        /// </summary>
        private int GetCoordinateIndex(ObjectType objectType)
        {
#if KK
            if (objectType == ObjectType.Accessory || objectType == ObjectType.Clothing)
                return CurrentCoordinateIndex;
#endif
            return 0;
        }
        #endregion

        private bool coordinateChanging;
        /// <summary>
        /// Whether the coordinate is being changed this Update. Used by methods that happen later in the update. If set, reverts to false on next Update.
        /// </summary>
        public bool CoordinateChanging
        {
            get => coordinateChanging;
            set
            {
                coordinateChanging = value;
                ChaControl.StartCoroutine(Reset());
                IEnumerator Reset()
                {
                    yield return null;
                    coordinateChanging = false;
                }
            }
        }

        private bool accessorySelectedSlotChanging;
        /// <summary>
        /// Whether the selected accessory slot is being changed this Update. Used by methods that happen later in the update. If set, reverts to false on next Update.
        /// </summary>
        public bool AccessorySelectedSlotChanging
        {
            get => accessorySelectedSlotChanging;
            set
            {
                accessorySelectedSlotChanging = value;
                ChaControl.StartCoroutine(Reset());
                IEnumerator Reset()
                {
                    yield return null;
                    accessorySelectedSlotChanging = false;
                }
            }
        }

        private bool clothesChanging;
        /// <summary>
        /// Whether the clothes are being changed this Update. Used by methods that happen later in the update. If set, reverts to false on next Update.
        /// </summary>
        public bool ClothesChanging
        {
            get => clothesChanging;
            set
            {
                clothesChanging = value;
                ChaControl.StartCoroutine(Reset());
                IEnumerator Reset()
                {
                    yield return null;
                    clothesChanging = false;
                }
            }
        }

        private bool characterLoading;
        /// <summary>
        /// Whether the character is being changed this Update. Used by methods that happen later in the update. If set, reverts to false on next Update.
        /// </summary>
        public bool CharacterLoading
        {
            get => characterLoading;
            set
            {
                characterLoading = value;
                ChaControl.StartCoroutine(Reset());
                IEnumerator Reset()
                {
                    yield return null;
                    characterLoading = false;
                }
            }
        }

        private bool refreshingTextures;
        /// <summary>
        /// Whether the overlay plugin is refreshing textures this Update. Used by methods that happen later in the update. If set, reverts to false on next Update.
        /// </summary>
        public bool RefreshingTextures
        {
            get => refreshingTextures;
            set
            {
                refreshingTextures = value;
                ChaControl.StartCoroutine(Reset());
                IEnumerator Reset()
                {
                    yield return null;
                    refreshingTextures = false;
                }
            }
        }

        private bool customClothesOverride;
        /// <summary>
        /// Override flag set to distinguish between clothes being changed via character maker and clothes changed by changing outfit slots, loading the character, or other methods.
        /// Used by methods that happen later in the update. If set, reverts to false on next Update.
        /// </summary>
        public bool CustomClothesOverride
        {
            get => customClothesOverride;
            set
            {
                customClothesOverride = value;
                ChaControl.StartCoroutine(Reset());
                IEnumerator Reset()
                {
                    yield return null;
                    customClothesOverride = false;
                }
            }
        }

        private GameObject FindGameObject(ObjectType objectType, int slot)
        {
            if (objectType == ObjectType.Clothing)
                return ChaControl.GetClothes(slot);
            if (objectType == ObjectType.Accessory)
            {
                var acc = ChaControl.GetAccessoryObject(slot);
                if (acc != null)
                    return acc;
            }
            if (objectType == ObjectType.Hair)
            {
                var hair = ChaControl.GetHair(slot);
                if (hair != null)
                    return hair.gameObject;
            }
            if (objectType == ObjectType.Character)
                return ChaControl.gameObject;
            return null;
        }

        /// <summary>
        /// Type of object, used for saving MaterialEditor data.
        /// </summary>
        public enum ObjectType
        {
            /// <summary>
            /// Unknown type, things should never be of this type
            /// </summary>
            Unknown,
            /// <summary>
            /// Clothing
            /// </summary>
            Clothing,
            /// <summary>
            /// Accessory
            /// </summary>
            Accessory,
            /// <summary>
            /// Hair
            /// </summary>
            Hair,
            /// <summary>
            /// Parts of a character
            /// </summary>
            Character
        };

        /// <summary>
        /// Data storage class for renderer properties
        /// </summary>
        [Serializable]
        [MessagePackObject]
        public class RendererProperty
        {
            /// <summary>
            /// Type of the object
            /// </summary>
            [Key("ObjectType")]
            public ObjectType ObjectType;
            /// <summary>
            /// Coordinate index, always 0 except in Koikatsu
            /// </summary>
            [Key("CoordinateIndex")]
            public int CoordinateIndex;
            /// <summary>
            /// Slot of the accessory, hair, or clothing
            /// </summary>
            [Key("Slot")]
            public int Slot;
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
            /// <param name="objectType">Type of the object</param>
            /// <param name="coordinateIndex">Coordinate index, always 0 except in Koikatsu</param>
            /// <param name="slot">Slot of the accessory, hair, or clothing</param>
            /// <param name="rendererName">Name of the renderer</param>
            /// <param name="property">Property type</param>
            /// <param name="value">Value</param>
            /// <param name="valueOriginal">Original</param>
            public RendererProperty(ObjectType objectType, int coordinateIndex, int slot, string rendererName, RendererProperties property, string value, string valueOriginal)
            {
                ObjectType = objectType;
                CoordinateIndex = coordinateIndex;
                Slot = slot;
                RendererName = rendererName.Replace("(Instance)", "").Trim();
                Property = property;
                Value = value;
                ValueOriginal = valueOriginal;
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
            /// Type of the object
            /// </summary>
            [Key("ObjectType")]
            public ObjectType ObjectType;
            /// <summary>
            /// Coordinate index, always 0 except in Koikatsu
            /// </summary>
            [Key("CoordinateIndex")]
            public int CoordinateIndex;
            /// <summary>
            /// Slot of the accessory, hair, or clothing
            /// </summary>
            [Key("Slot")]
            public int Slot;
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
            /// <param name="objectType">Type of the object</param>
            /// <param name="coordinateIndex">Coordinate index, always 0 except in Koikatsu</param>
            /// <param name="slot">Slot of the accessory, hair, or clothing</param>
            /// <param name="materialName">Name of the material</param>
            /// <param name="property">Name of the property</param>
            /// <param name="value">Value</param>
            /// <param name="valueOriginal">Original value</param>
            public MaterialFloatProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string property, string value, string valueOriginal)
            {
                ObjectType = objectType;
                CoordinateIndex = coordinateIndex;
                Slot = slot;
                MaterialName = materialName.Replace("(Instance)", "").Trim();
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
            /// Type of the object
            /// </summary>
            [Key("ObjectType")]
            public ObjectType ObjectType;
            /// <summary>
            /// Coordinate index, always 0 except in Koikatsu
            /// </summary>
            [Key("CoordinateIndex")]
            public int CoordinateIndex;
            /// <summary>
            /// Slot of the accessory, hair, or clothing
            /// </summary>
            [Key("Slot")]
            public int Slot;
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
            /// Data storage class for color properties
            /// </summary>
            /// <param name="objectType">Type of the object</param>
            /// <param name="coordinateIndex">Coordinate index, always 0 except in Koikatsu</param>
            /// <param name="slot">Slot of the accessory, hair, or clothing</param>
            /// <param name="materialName">Name of the material</param>
            /// <param name="property">Name of the property</param>
            /// <param name="value">Value</param>
            /// <param name="valueOriginal">Original value</param>
            public MaterialColorProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string property, Color value, Color valueOriginal)
            {
                ObjectType = objectType;
                CoordinateIndex = coordinateIndex;
                Slot = slot;
                MaterialName = materialName.Replace("(Instance)", "").Trim();
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
            /// Type of the object
            /// </summary>
            [Key("ObjectType")]
            public ObjectType ObjectType;
            /// <summary>
            /// Coordinate index, always 0 except in Koikatsu
            /// </summary>
            [Key("CoordinateIndex")]
            public int CoordinateIndex;
            /// <summary>
            /// Slot of the accessory, hair, or clothing
            /// </summary>
            [Key("Slot")]
            public int Slot;
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
            /// ID of the texture as stored in the texture dictionary
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
            /// Data storage class for texture properties
            /// </summary>
            /// <param name="objectType">Type of the object</param>
            /// <param name="coordinateIndex">Coordinate index, always 0 except in Koikatsu</param>
            /// <param name="slot">Slot of the accessory, hair, or clothing</param>
            /// <param name="materialName">Name of the material</param>
            /// <param name="property">Name of the property</param>
            /// <param name="texID">ID of the texture as stored in the texture dictionary</param>
            /// <param name="offset">Texture offset value</param>
            /// <param name="offsetOriginal">Texture offset original value</param>
            /// <param name="scale">Texture scale value</param>
            /// <param name="scaleOriginal">Texture scale original value</param>
            public MaterialTextureProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string property, int? texID = null, Vector2? offset = null, Vector2? offsetOriginal = null, Vector2? scale = null, Vector2? scaleOriginal = null)
            {
                ObjectType = objectType;
                CoordinateIndex = coordinateIndex;
                Slot = slot;
                MaterialName = materialName.Replace("(Instance)", "").Trim();
                Property = property;
                TexID = texID;
                Offset = offset;
                OffsetOriginal = offsetOriginal;
                Scale = scale;
                ScaleOriginal = scaleOriginal;
            }

            /// <summary>
            /// Check if the TexID, Offset, and Scale are all null. Safe to remove this data if true.
            /// </summary>
            /// <returns></returns>
            public bool NullCheck() => TexID == null && Offset == null && Scale == null;
        }

        /// <summary>
        /// Data storage class for shader data
        /// </summary>
        [Serializable]
        [MessagePackObject]
        public class MaterialShader
        {
            /// <summary>
            /// Type of the object
            /// </summary>
            [Key("ObjectType")]
            public ObjectType ObjectType;
            /// <summary>
            /// Coordinate index, always 0 except in Koikatsu
            /// </summary>
            [Key("CoordinateIndex")]
            public int CoordinateIndex;
            /// <summary>
            /// Slot of the accessory, hair, or clothing
            /// </summary>
            [Key("Slot")]
            public int Slot;
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
            /// <param name="objectType">Type of the object</param>
            /// <param name="coordinateIndex">Coordinate index, always 0 except in Koikatsu</param>
            /// <param name="slot">Slot of the accessory, hair, or clothing</param>
            /// <param name="materialName">Name of the material</param>
            /// <param name="shaderName">Name of the shader</param>
            /// <param name="shaderNameOriginal">Name of the original shader</param>
            /// <param name="renderQueue">Render queue</param>
            /// <param name="renderQueueOriginal">Original render queue</param>
            public MaterialShader(ObjectType objectType, int coordinateIndex, int slot, string materialName, string shaderName, string shaderNameOriginal, int? renderQueue, int? renderQueueOriginal)
            {
                ObjectType = objectType;
                CoordinateIndex = coordinateIndex;
                Slot = slot;
                MaterialName = materialName.Replace("(Instance)", "").Trim();
                ShaderName = shaderName;
                ShaderNameOriginal = shaderNameOriginal;
                RenderQueue = renderQueue;
                RenderQueueOriginal = renderQueueOriginal;
            }
            /// <summary>
            /// Data storage class for shader data
            /// </summary>
            /// <param name="objectType">Type of the object</param>
            /// <param name="coordinateIndex">Coordinate index, always 0 except in Koikatsu</param>
            /// <param name="slot">Slot of the accessory, hair, or clothing</param>
            /// <param name="materialName">Name of the material</param>
            /// <param name="shaderName">Name of the shader</param>
            /// <param name="shaderNameOriginal">Name of the original shader</param>
            public MaterialShader(ObjectType objectType, int coordinateIndex, int slot, string materialName, string shaderName, string shaderNameOriginal)
            {
                ObjectType = objectType;
                CoordinateIndex = coordinateIndex;
                Slot = slot;
                MaterialName = materialName.Replace("(Instance)", "").Trim();
                ShaderName = shaderName;
                ShaderNameOriginal = shaderNameOriginal;
            }
            /// <summary>
            /// Data storage class for shader data
            /// </summary>
            /// <param name="objectType">Type of the object</param>
            /// <param name="coordinateIndex">Coordinate index, always 0 except in Koikatsu</param>
            /// <param name="slot">Slot of the accessory, hair, or clothing</param>
            /// <param name="materialName">Name of the material</param>
            /// <param name="renderQueue">Render queue</param>
            /// <param name="renderQueueOriginal">Original render queue</param>
            public MaterialShader(ObjectType objectType, int coordinateIndex, int slot, string materialName, int? renderQueue, int? renderQueueOriginal)
            {
                ObjectType = objectType;
                CoordinateIndex = coordinateIndex;
                Slot = slot;
                MaterialName = materialName.Replace("(Instance)", "").Trim();
                RenderQueue = renderQueue;
                RenderQueueOriginal = renderQueueOriginal;
            }

            /// <summary>
            /// Check if the shader name and render queue are both null. Safe to delete this data if true.
            /// </summary>
            /// <returns></returns>
            public bool NullCheck() => ShaderName.IsNullOrEmpty() && RenderQueue == null;
        }
    }
}
