using ExtensibleSaveFormat;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using MessagePack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniRx;
using UnityEngine;
using static KK_Plugins.MaterialEditor.MaterialAPI;
#if AI || HS2
using AIChara;
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
        private string FileToSet = null;
        private string PropertyToSet;
        private Material MatToSet;
        private int SlotToSet;
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
                if (!MaterialTexturePropertyList.Any(x => x.TexID == texID))
                    IDsToPurge.Add(texID);

            foreach (int texID in IDsToPurge)
            {
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
                    if (!MaterialTexturePropertyList.Any(x => x.TexID == texID))
                        IDsToPurge.Add(texID);

                foreach (int texID in IDsToPurge)
                {
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
                    //    string filename = Path.Combine(ExportPath, $"_Export_{ChaControl.chaFile.parameter.fullname.Trim()}_{counter}.png");
                    //    SaveTex(TextureFromBytes(tex), filename);
                    //    Logger.LogInfo($"Exported {filename}");
                    //    counter++;
                    //}

                    if (data.data.TryGetValue(nameof(MaterialShaderList), out var shaderProperties) && shaderProperties != null)
                        foreach (var loadedProperty in MessagePackSerializer.Deserialize<List<MaterialShader>>((byte[])shaderProperties))
                            if (objectTypesToLoad.Contains(loadedProperty.ObjectType))
                                MaterialShaderList.Add(new MaterialShader(loadedProperty.ObjectType, loadedProperty.CoordinateIndex, loadedProperty.Slot, loadedProperty.MaterialName, loadedProperty.ShaderName, loadedProperty.ShaderNameOriginal, loadedProperty.RenderQueue, loadedProperty.RenderQueueOriginal));

                    if (data.data.TryGetValue(nameof(RendererPropertyList), out var rendererProperties) && rendererProperties != null)
                        foreach (var loadedProperty in MessagePackSerializer.Deserialize<List<RendererProperty>>((byte[])rendererProperties))
                            if (objectTypesToLoad.Contains(loadedProperty.ObjectType))
                                RendererPropertyList.Add(new RendererProperty(loadedProperty.ObjectType, loadedProperty.CoordinateIndex, loadedProperty.Slot, loadedProperty.RendererName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));

                    if (data.data.TryGetValue(nameof(MaterialFloatPropertyList), out var materialFloatProperties) && materialFloatProperties != null)
                        foreach (var loadedProperty in MessagePackSerializer.Deserialize<List<MaterialFloatProperty>>((byte[])materialFloatProperties))
                            if (objectTypesToLoad.Contains(loadedProperty.ObjectType))
                                MaterialFloatPropertyList.Add(new MaterialFloatProperty(loadedProperty.ObjectType, loadedProperty.CoordinateIndex, loadedProperty.Slot, loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));

                    if (data.data.TryGetValue(nameof(MaterialColorPropertyList), out var materialColorProperties) && materialColorProperties != null)
                        foreach (var loadedProperty in MessagePackSerializer.Deserialize<List<MaterialColorProperty>>((byte[])materialColorProperties))
                            if (objectTypesToLoad.Contains(loadedProperty.ObjectType))
                                MaterialColorPropertyList.Add(new MaterialColorProperty(loadedProperty.ObjectType, loadedProperty.CoordinateIndex, loadedProperty.Slot, loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));

                    if (data.data.TryGetValue(nameof(MaterialTexturePropertyList), out var materialTextureProperties) && materialTextureProperties != null)
                        foreach (var loadedProperty in MessagePackSerializer.Deserialize<List<MaterialTextureProperty>>((byte[])materialTextureProperties))
                            if (objectTypesToLoad.Contains(loadedProperty.ObjectType))
                            {
                                int? texID = null;
                                if (loadedProperty.TexID != null)
                                    texID = importDictionary[(int)loadedProperty.TexID];

                                MaterialTextureProperty newTextureProperty = new MaterialTextureProperty(loadedProperty.ObjectType, loadedProperty.CoordinateIndex, loadedProperty.Slot, loadedProperty.MaterialName, loadedProperty.Property, texID, loadedProperty.Offset, loadedProperty.OffsetOriginal, loadedProperty.Scale, loadedProperty.ScaleOriginal);
                                MaterialTexturePropertyList.Add(newTextureProperty);
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
                    SetMaterialTextureFromFile(SlotToSet, MatToSet, PropertyToSet, FileToSet, GameObjectToSet);
            }
            catch
            {
                MaterialEditorPlugin.Logger.Log(BepInEx.Logging.LogLevel.Error | BepInEx.Logging.LogLevel.Message, "Failed to load texture.");
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
                    foreach (var loadedProperty in MessagePackSerializer.Deserialize<List<MaterialShader>>((byte[])materialShaders))
                        if (objectTypesToLoad.Contains(loadedProperty.ObjectType))
                            MaterialShaderList.Add(new MaterialShader(loadedProperty.ObjectType, CurrentCoordinateIndex, loadedProperty.Slot, loadedProperty.MaterialName, loadedProperty.ShaderName, loadedProperty.ShaderNameOriginal, loadedProperty.RenderQueue, loadedProperty.RenderQueueOriginal));

                if (data.data.TryGetValue(nameof(RendererPropertyList), out var rendererProperties) && rendererProperties != null)
                    foreach (var loadedProperty in MessagePackSerializer.Deserialize<List<RendererProperty>>((byte[])rendererProperties))
                        if (objectTypesToLoad.Contains(loadedProperty.ObjectType))
                            RendererPropertyList.Add(new RendererProperty(loadedProperty.ObjectType, CurrentCoordinateIndex, loadedProperty.Slot, loadedProperty.RendererName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));

                if (data.data.TryGetValue(nameof(MaterialFloatPropertyList), out var materialFloatProperties) && materialFloatProperties != null)
                    foreach (var loadedProperty in MessagePackSerializer.Deserialize<List<MaterialFloatProperty>>((byte[])materialFloatProperties))
                        if (objectTypesToLoad.Contains(loadedProperty.ObjectType))
                            MaterialFloatPropertyList.Add(new MaterialFloatProperty(loadedProperty.ObjectType, CurrentCoordinateIndex, loadedProperty.Slot, loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));

                if (data.data.TryGetValue(nameof(MaterialColorPropertyList), out var materialColorProperties) && materialColorProperties != null)
                    foreach (var loadedProperty in MessagePackSerializer.Deserialize<List<MaterialColorProperty>>((byte[])materialColorProperties))
                        if (objectTypesToLoad.Contains(loadedProperty.ObjectType))
                            MaterialColorPropertyList.Add(new MaterialColorProperty(loadedProperty.ObjectType, CurrentCoordinateIndex, loadedProperty.Slot, loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));

                if (data.data.TryGetValue(nameof(MaterialTexturePropertyList), out var materialTextureProperties) && materialTextureProperties != null)
                    foreach (var loadedProperty in MessagePackSerializer.Deserialize<List<MaterialTextureProperty>>((byte[])materialTextureProperties))
                        if (objectTypesToLoad.Contains(loadedProperty.ObjectType))
                        {
                            int? texID = null;
                            if (loadedProperty.TexID != null)
                                texID = importDictionary[(int)loadedProperty.TexID];

                            MaterialTextureProperty newTextureProperty = new MaterialTextureProperty(loadedProperty.ObjectType, CurrentCoordinateIndex, loadedProperty.Slot, loadedProperty.MaterialName, loadedProperty.Property, texID, loadedProperty.Offset, loadedProperty.OffsetOriginal, loadedProperty.Scale, loadedProperty.ScaleOriginal);
                            MaterialTexturePropertyList.Add(newTextureProperty);
                        }
            }

            CoordinateChanging = true;

            ChaControl.StartCoroutine(LoadData(true, true, false));
            base.OnCoordinateBeingLoaded(coordinate, maintainState);
        }

        private IEnumerator LoadData(bool clothes, bool accessories, bool hair)
        {
            yield return null;

            foreach (var property in MaterialShaderList)
            {
                if (property.ObjectType == ObjectType.Clothing && !clothes) continue;
                if (property.ObjectType == ObjectType.Accessory && !accessories) continue;
                if (property.ObjectType == ObjectType.Hair && !hair) continue;
                if ((property.ObjectType == ObjectType.Clothing || property.ObjectType == ObjectType.Accessory) && property.CoordinateIndex != CurrentCoordinateIndex) continue;

                SetShader(FindGameObject(property.ObjectType, property.Slot), property.MaterialName, property.ShaderName);
                SetRenderQueue(FindGameObject(property.ObjectType, property.Slot), property.MaterialName, property.RenderQueue);
            }
            foreach (var property in RendererPropertyList)
            {
                if (property.ObjectType == ObjectType.Clothing && !clothes) continue;
                if (property.ObjectType == ObjectType.Accessory && !accessories) continue;
                if (property.ObjectType == ObjectType.Hair && !hair) continue;
                if ((property.ObjectType == ObjectType.Clothing || property.ObjectType == ObjectType.Accessory) && property.CoordinateIndex != CurrentCoordinateIndex) continue;

                MaterialAPI.SetRendererProperty(FindGameObject(property.ObjectType, property.Slot), property.RendererName, property.Property, property.Value);
            }
            foreach (var property in MaterialFloatPropertyList)
            {
                if (MaterialEditorPlugin.CheckBlacklist(property.MaterialName, property.Property)) continue;
                if (property.ObjectType == ObjectType.Clothing && !clothes) continue;
                if (property.ObjectType == ObjectType.Accessory && !accessories) continue;
                if (property.ObjectType == ObjectType.Hair && !hair) continue;
                if ((property.ObjectType == ObjectType.Clothing || property.ObjectType == ObjectType.Accessory) && property.CoordinateIndex != CurrentCoordinateIndex) continue;

                SetFloat(FindGameObject(property.ObjectType, property.Slot), property.MaterialName, property.Property, float.Parse(property.Value));
            }
            foreach (var property in MaterialColorPropertyList)
            {
                if (MaterialEditorPlugin.CheckBlacklist(property.MaterialName, property.Property)) continue;
                if (property.ObjectType == ObjectType.Clothing && !clothes) continue;
                if (property.ObjectType == ObjectType.Accessory && !accessories) continue;
                if (property.ObjectType == ObjectType.Hair && !hair) continue;
                if ((property.ObjectType == ObjectType.Clothing || property.ObjectType == ObjectType.Accessory) && property.CoordinateIndex != CurrentCoordinateIndex) continue;

                SetColor(FindGameObject(property.ObjectType, property.Slot), property.MaterialName, property.Property, property.Value);
            }
            foreach (var property in MaterialTexturePropertyList)
            {
                if (MaterialEditorPlugin.CheckBlacklist(property.MaterialName, property.Property)) continue;
                if (property.ObjectType == ObjectType.Clothing && !clothes) continue;
                if (property.ObjectType == ObjectType.Accessory && !accessories) continue;
                if (property.ObjectType == ObjectType.Hair && !hair) continue;
                if ((property.ObjectType == ObjectType.Clothing || property.ObjectType == ObjectType.Accessory) && property.CoordinateIndex != CurrentCoordinateIndex) continue;

                if (property.TexID != null)
                    SetTexture(FindGameObject(property.ObjectType, property.Slot), property.MaterialName, property.Property, TextureDictionary[(int)property.TexID].Texture);
                SetTextureOffset(FindGameObject(property.ObjectType, property.Slot), property.MaterialName, property.Property, property.Offset);
                SetTextureScale(FindGameObject(property.ObjectType, property.Slot), property.MaterialName, property.Property, property.Scale);
            }
        }
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
                UI.Visible = false;
        }

#if KK
        internal void ClothingCopiedEvent(int copySource, int copyDestination, List<int> copySlots)
        {
            foreach (int slot in copySlots)
            {
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
                    UI.Visible = false;

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
                if (UI.Visible)
                    MEMaker.Instance?.PopulateListAccessory();
        }

        internal void AccessorySelectedSlotChangeEvent(object sender, AccessorySlotEventArgs e)
        {
            if (!MakerAPI.InsideAndLoaded) return;

            AccessorySelectedSlotChanging = true;

            if (MakerAPI.InsideAndLoaded)
                if (UI.Visible)
                    MEMaker.Instance?.PopulateListAccessory();
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
                UI.Visible = false;

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
                        UI.Visible = false;
            }
        }
#endif

        internal void ChangeAccessoryEvent(int slot, int type)
        {
#if AI || HS2
            if (type != 350) return; //type 350 = no category, accessory being removed
#else
            if (type != 120) return; //type 120 = no category, accessory being removed
#endif
            if (!MakerAPI.InsideAndLoaded) return;
            if (CoordinateChanging) return;

            MaterialShaderList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);
            RendererPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);
            MaterialFloatPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);
            MaterialColorPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);
            MaterialTexturePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);

            if (MakerAPI.InsideAndLoaded)
                UI.Visible = false;
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
                UI.Visible = false;
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
                UI.Visible = false;
        }
        /// <summary>
        /// Refresh the clothes MainTex, typically called after editing colors in the character maker
        /// </summary>
        public void RefreshClothesMainTex() => StartCoroutine(RefreshClothesMainTexCoroutine());
        private IEnumerator RefreshClothesMainTexCoroutine()
        {
            yield return new WaitForEndOfFrame();
            foreach (var property in MaterialTexturePropertyList)
            {
                if (MaterialEditorPlugin.CheckBlacklist(property.MaterialName, property.Property))
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

            foreach (var property in MaterialTexturePropertyList)
            {
                if (MaterialEditorPlugin.CheckBlacklist(property.MaterialName, property.Property))
                    continue;

                if (property.ObjectType == ObjectType.Character && property.Property == "MainTex")
                    if (property.TexID != null)
                        SetTexture(ChaControl, property.MaterialName, property.Property, TextureDictionary[(int)property.TexID].Texture);
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
        /// <param name="gameObject">GameObject the renderer belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the renderer</param>
        public void SetRendererProperty(int slot, Renderer renderer, RendererProperties property, string value, GameObject gameObject, bool setProperty = true)
        {
            ObjectType objectType = FindGameObjectType(gameObject);
            RendererProperty rendererProperty = RendererPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot && x.Property == property && x.RendererName == renderer.NameFormatted());
            if (rendererProperty == null)
            {
                string valueOriginal;
                if (property == RendererProperties.Enabled)
                    valueOriginal = renderer.enabled ? "1" : "0";
                else if (property == RendererProperties.ReceiveShadows)
                    valueOriginal = renderer.receiveShadows ? "1" : "0";
                else
                    valueOriginal = ((int)renderer.shadowCastingMode).ToString();

                RendererPropertyList.Add(new RendererProperty(objectType, CurrentCoordinateIndex, slot, renderer.NameFormatted(), property, value, valueOriginal));
            }
            else
            {
                if (value == rendererProperty.ValueOriginal)
                    RemoveRendererProperty(slot, renderer, property, gameObject, false);
                else
                    rendererProperty.Value = value;
            }
            if (setProperty)
                MaterialAPI.SetRendererProperty(gameObject, renderer.NameFormatted(), property, value);
        }
        /// <summary>
        /// Get the saved renderer property value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="renderer">Renderer being modified</param>
        /// <param name="property">Property of the renderer</param>
        /// <param name="gameObject">GameObject the renderer belongs to</param>
        /// <returns>Saved renderer property value</returns>
        public string GetRendererPropertyValue(int slot, Renderer renderer, RendererProperties property, GameObject gameObject)
        {
            ObjectType objectType = FindGameObjectType(gameObject);
            return RendererPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot && x.Property == property && x.RendererName == renderer.NameFormatted())?.Value;
        }
        /// <summary>
        /// Get the original value of the saved renderer property value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="renderer">Renderer being modified</param>
        /// <param name="property">Property of the renderer</param>
        /// <param name="gameObject">GameObject the renderer belongs to</param>
        /// <returns>Saved renderer property value</returns>
        public string GetRendererPropertyValueOriginal(int slot, Renderer renderer, RendererProperties property, GameObject gameObject)
        {
            ObjectType objectType = FindGameObjectType(gameObject);
            return RendererPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot && x.Property == property && x.RendererName == renderer.NameFormatted())?.ValueOriginal;
        }
        /// <summary>
        /// Remove the saved renderer property value if one is saved and optionally also update the renderer
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="renderer">Renderer being modified</param>
        /// <param name="property">Property of the renderer</param>
        /// <param name="gameObject">GameObject the renderer belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the renderer</param>
        public void RemoveRendererProperty(int slot, Renderer renderer, RendererProperties property, GameObject gameObject, bool setProperty = true)
        {
            ObjectType objectType = FindGameObjectType(gameObject);
            if (setProperty)
            {
                var original = GetRendererPropertyValueOriginal(slot, renderer, property, gameObject);
                if (!original.IsNullOrEmpty())
                    MaterialAPI.SetRendererProperty(gameObject, renderer.NameFormatted(), property, original);
            }
            RendererPropertyList.RemoveAll(x => x.ObjectType == objectType && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot && x.Property == property && x.RendererName == renderer.NameFormatted());
        }

        /// <summary>
        /// Add a float property to be saved and loaded with the card and optionally also update the materials.
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="value">Value</param>
        /// <param name="gameObject">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void SetMaterialFloatProperty(int slot, Material material, string propertyName, float value, GameObject gameObject, bool setProperty = true)
        {
            ObjectType objectType = FindGameObjectType(gameObject);
            var materialProperty = MaterialFloatPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted());
            if (materialProperty == null)
            {
                float valueOriginal = material.GetFloat($"_{propertyName}");
                MaterialFloatPropertyList.Add(new MaterialFloatProperty(objectType, CurrentCoordinateIndex, slot, material.NameFormatted(), propertyName, value.ToString(), valueOriginal.ToString()));
            }
            else
            {
                if (value.ToString() == materialProperty.ValueOriginal)
                    RemoveMaterialFloatProperty(slot, material, propertyName, gameObject, false);
                else
                    materialProperty.Value = value.ToString();
            }
            if (setProperty)
                SetFloat(gameObject, material.NameFormatted(), propertyName, value);
        }
        /// <summary>
        /// Get the saved material property value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="gameObject">GameObject the material belongs to</param>
        /// <returns>Saved material property value or null if none is saved</returns>
        public float? GetMaterialFloatPropertyValue(int slot, Material material, string propertyName, GameObject gameObject)
        {
            ObjectType objectType = FindGameObjectType(gameObject);
            var value = MaterialFloatPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.Value;
            if (value.IsNullOrEmpty())
                return null;
            return float.Parse(value);
        }
        /// <summary>
        /// Get the saved material property's original value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="gameObject">GameObject the material belongs to</param>
        /// <returns>Saved material property's original value or null if none is saved</returns>
        public float? GetMaterialFloatPropertyValueOriginal(int slot, Material material, string propertyName, GameObject gameObject)
        {
            ObjectType objectType = FindGameObjectType(gameObject);
            var valueOriginal = MaterialFloatPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.ValueOriginal;
            if (valueOriginal.IsNullOrEmpty())
                return null;
            return float.Parse(valueOriginal);
        }
        /// <summary>
        /// Remove the saved material property value if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="gameObject">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void RemoveMaterialFloatProperty(int slot, Material material, string propertyName, GameObject gameObject, bool setProperty = true)
        {
            ObjectType objectType = FindGameObjectType(gameObject);
            if (setProperty)
            {
                var original = GetMaterialFloatPropertyValueOriginal(slot, material, propertyName, gameObject);
                if (original != null)
                    SetFloat(gameObject, material.NameFormatted(), propertyName, (float)original);
            }
            MaterialFloatPropertyList.RemoveAll(x => x.ObjectType == objectType && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted());
        }

        /// <summary>
        /// Add a color property to be saved and loaded with the card and optionally also update the materials.
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="value">Value</param>
        /// <param name="gameObject">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void SetMaterialColorProperty(int slot, Material material, string propertyName, Color value, GameObject gameObject, bool setProperty = true)
        {
            ObjectType objectType = FindGameObjectType(gameObject);
            var colorProperty = MaterialColorPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted());
            if (colorProperty == null)
            {
                Color valueOriginal = material.GetColor($"_{propertyName}");
                MaterialColorPropertyList.Add(new MaterialColorProperty(objectType, CurrentCoordinateIndex, slot, material.NameFormatted(), propertyName, value, valueOriginal));
            }
            else
            {
                if (value == colorProperty.ValueOriginal)
                    RemoveMaterialColorProperty(slot, material, propertyName, gameObject, false);
                else
                    colorProperty.Value = value;
            }
            if (setProperty)
                SetColor(gameObject, material.NameFormatted(), propertyName, value);
        }
        /// <summary>
        /// Get the saved material property value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="gameObject">GameObject the material belongs to</param>
        /// <returns>Saved material property value or null if none is saved</returns>
        public Color? GetMaterialColorPropertyValue(int slot, Material material, string propertyName, GameObject gameObject)
        {
            ObjectType objectType = FindGameObjectType(gameObject);
            return MaterialColorPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.Value;
        }
        /// <summary>
        /// Get the saved material property's original value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="gameObject">GameObject the material belongs to</param>
        /// <returns>Saved material property's original value or null if none is saved</returns>
        public Color? GetMaterialColorPropertyValueOriginal(int slot, Material material, string propertyName, GameObject gameObject)
        {
            ObjectType objectType = FindGameObjectType(gameObject);
            return MaterialColorPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.ValueOriginal;
        }
        /// <summary>
        /// Remove the saved material property value if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="gameObject">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void RemoveMaterialColorProperty(int slot, Material material, string propertyName, GameObject gameObject, bool setProperty = true)
        {
            ObjectType objectType = FindGameObjectType(gameObject);
            if (setProperty)
            {
                var original = GetMaterialColorPropertyValueOriginal(slot, material, propertyName, gameObject);
                if (original != null)
                    SetColor(gameObject, material.NameFormatted(), propertyName, (Color)original);
            }
            MaterialColorPropertyList.RemoveAll(x => x.ObjectType == objectType && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted());
        }

        /// <summary>
        /// Add a texture property to be saved and loaded with the card.
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="filePath">Path to the .png file on disk</param>
        /// <param name="gameObject">GameObject the material belongs to</param>
        /// <param name="setTexInUpdate">Whether to wait for the next Update</param>
        public void SetMaterialTextureFromFile(int slot, Material material, string propertyName, string filePath, GameObject gameObject, bool setTexInUpdate = false)
        {
            if (!File.Exists(filePath)) return;

            ObjectType objectType = FindGameObjectType(gameObject);
            if (setTexInUpdate)
            {
                FileToSet = filePath;
                PropertyToSet = propertyName;
                MatToSet = material;
                GameObjectToSet = gameObject;
                SlotToSet = slot;
            }
            else
            {
                var texBytes = File.ReadAllBytes(filePath);
                Texture2D tex = MaterialEditorPlugin.TextureFromBytes(texBytes);

                SetTexture(gameObject, material.NameFormatted(), propertyName, tex);

                var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted());
                if (textureProperty == null)
                    MaterialTexturePropertyList.Add(new MaterialTextureProperty(objectType, CurrentCoordinateIndex, slot, material.NameFormatted(), propertyName, SetAndGetTextureID(texBytes)));
                else
                    textureProperty.TexID = SetAndGetTextureID(texBytes);
            }
        }
        /// <summary>
        /// Get the saved material property value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="gameObject">GameObject the material belongs to</param>
        /// <returns>Saved material property value or null if none is saved</returns>
        public Texture2D GetMaterialTexture(int slot, Material material, string propertyName, GameObject gameObject)
        {
            ObjectType objectType = FindGameObjectType(gameObject);
            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted());
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
        /// <param name="gameObject">GameObject the material belongs to</param>
        /// <returns>True if the texture has been modified, false if not</returns>
        public bool GetMaterialTextureOriginal(int slot, Material material, string propertyName, GameObject gameObject)
        {
            ObjectType objectType = FindGameObjectType(gameObject);
            return MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.TexID == null ? true : false;
        }
        /// <summary>
        /// Remove the saved material property value if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="gameObject">GameObject the material belongs to</param>
        /// <param name="displayMessage">Whether to display a message on screen telling the user to save and reload to refresh textures</param>
        public void RemoveMaterialTexture(int slot, Material material, string propertyName, GameObject gameObject, bool displayMessage = true)
        {
            ObjectType objectType = FindGameObjectType(gameObject);
            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted());
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
        /// <param name="gameObject">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void SetMaterialTextureOffset(int slot, Material material, string propertyName, Vector2 value, GameObject gameObject, bool setProperty = true)
        {
            ObjectType objectType = FindGameObjectType(gameObject);
            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted());
            if (textureProperty == null)
            {
                Vector2 valueOriginal = material.GetTextureOffset($"_{propertyName}");
                MaterialTexturePropertyList.Add(new MaterialTextureProperty(objectType, CurrentCoordinateIndex, slot, material.NameFormatted(), propertyName, offset: value, offsetOriginal: valueOriginal));
            }
            else
            {
                if (value == textureProperty.OffsetOriginal)
                    RemoveMaterialTextureOffset(slot, material, propertyName, gameObject, false);
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
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="gameObject">GameObject the material belongs to</param>
        /// <returns>Saved material property value or null if none is saved</returns>
        public Vector2? GetMaterialTextureOffset(int slot, Material material, string propertyName, GameObject gameObject)
        {
            ObjectType objectType = FindGameObjectType(gameObject);
            return MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.Offset;
        }
        /// <summary>
        /// Get the saved material property's original value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="gameObject">GameObject the material belongs to</param>
        /// <returns>Saved material property's original value or null if none is saved</returns>
        public Vector2? GetMaterialTextureOffsetOriginal(int slot, Material material, string propertyName, GameObject gameObject)
        {
            ObjectType objectType = FindGameObjectType(gameObject);
            return MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.OffsetOriginal;
        }
        /// <summary>
        /// Remove the saved material property value if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="gameObject">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void RemoveMaterialTextureOffset(int slot, Material material, string propertyName, GameObject gameObject, bool setProperty = true)
        {
            ObjectType objectType = FindGameObjectType(gameObject);
            if (setProperty)
            {
                var original = GetMaterialTextureOffsetOriginal(slot, material, propertyName, gameObject);
                if (original != null)
                    SetTextureOffset(gameObject, material.NameFormatted(), propertyName, (Vector2)original);
            }

            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted());
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
        /// <param name="gameObject">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void SetMaterialTextureScale(int slot, Material material, string propertyName, Vector2 value, GameObject gameObject, bool setProperty = true)
        {
            ObjectType objectType = FindGameObjectType(gameObject);
            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted());
            if (textureProperty == null)
            {
                Vector2 valueOriginal = material.GetTextureScale($"_{propertyName}");
                MaterialTexturePropertyList.Add(new MaterialTextureProperty(objectType, CurrentCoordinateIndex, slot, material.NameFormatted(), propertyName, scale: value, scaleOriginal: valueOriginal));
            }
            else
            {
                if (value == textureProperty.ScaleOriginal)
                    RemoveMaterialTextureScale(slot, material, propertyName, gameObject, false);
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
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="gameObject">GameObject the material belongs to</param>
        /// <returns>Saved material property value or null if none is saved</returns>
        public Vector2? GetMaterialTextureScale(int slot, Material material, string propertyName, GameObject gameObject)
        {
            ObjectType objectType = FindGameObjectType(gameObject);
            return MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.Scale;
        }
        /// <summary>
        /// Get the saved material property's original value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="gameObject">GameObject the material belongs to</param>
        /// <returns>Saved material property's original value or null if none is saved</returns>
        public Vector2? GetMaterialTextureScaleOriginal(int slot, Material material, string propertyName, GameObject gameObject)
        {
            ObjectType objectType = FindGameObjectType(gameObject);
            return MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.ScaleOriginal;
        }
        /// <summary>
        /// Remove the saved material property value if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="gameObject">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void RemoveMaterialTextureScale(int slot, Material material, string propertyName, GameObject gameObject, bool setProperty = true)
        {
            ObjectType objectType = FindGameObjectType(gameObject);
            if (setProperty)
            {
                var original = GetMaterialTextureScaleOriginal(slot, material, propertyName, gameObject);
                if (original != null)
                    SetTextureScale(gameObject, material.NameFormatted(), propertyName, (Vector2)original);
            }

            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted());
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
        /// <param name="gameObject">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void SetMaterialShader(int slot, Material material, string shaderName, GameObject gameObject, bool setProperty = true)
        {
            ObjectType objectType = FindGameObjectType(gameObject);
            var materialProperty = MaterialShaderList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot && x.MaterialName == material.NameFormatted());
            if (materialProperty == null)
            {
                string shaderNameOriginal = material.shader.NameFormatted();
                MaterialShaderList.Add(new MaterialShader(objectType, CurrentCoordinateIndex, slot, material.NameFormatted(), shaderName, shaderNameOriginal));
            }
            else
            {
                if (shaderName == materialProperty.ShaderNameOriginal)
                    RemoveMaterialShader(slot, material, gameObject, false);
                else
                {
                    materialProperty.ShaderName = shaderName;
                    if (materialProperty.ShaderNameOriginal == null)
                        materialProperty.ShaderNameOriginal = material.shader.NameFormatted();
                }
            }

            if (setProperty)
            {
                RemoveMaterialShaderRenderQueue(slot, material, gameObject, false);
                SetShader(gameObject, material.NameFormatted(), shaderName);
            }
        }

        /// <summary>
        /// Get the saved shader name or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="gameObject">GameObject the material belongs to</param>
        /// <returns>Saved shader name or null if none is saved</returns>
        public string GetMaterialShader(int slot, Material material, GameObject gameObject)
        {
            ObjectType objectType = FindGameObjectType(gameObject);
            return MaterialShaderList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot && x.MaterialName == material.NameFormatted())?.ShaderName;
        }
        /// <summary>
        /// Get the saved shader name's original value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="gameObject">GameObject the material belongs to</param>
        /// <returns>Saved shader name's original value or null if none is saved</returns>
        public string GetMaterialShaderOriginal(int slot, Material material, GameObject gameObject)
        {
            ObjectType objectType = FindGameObjectType(gameObject);
            return MaterialShaderList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot && x.MaterialName == material.NameFormatted())?.ShaderNameOriginal;
        }

        /// <summary>
        /// Remove the saved shader if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="gameObject">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void RemoveMaterialShader(int slot, Material material, GameObject gameObject, bool setProperty = true)
        {
            ObjectType objectType = FindGameObjectType(gameObject);
            if (setProperty)
            {
                var original = GetMaterialShaderOriginal(slot, material, gameObject);
                if (!original.IsNullOrEmpty())
                    SetShader(gameObject, material.NameFormatted(), original);
            }

#if EC
            //For EC don't remove shaders when reset, this helps users with fixing KK mods
            var materialProperty = MaterialShaderList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot && x.MaterialName == material.NameFormatted());
            if (materialProperty == null)
                return;
            else
                materialProperty.ShaderName = materialProperty.ShaderNameOriginal;
#else
            foreach (var materialProperty in MaterialShaderList.Where(x => x.ObjectType == objectType && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot && x.MaterialName == material.NameFormatted()))
            {
                materialProperty.ShaderName = null;
                materialProperty.ShaderNameOriginal = null;
            }

            MaterialShaderList.RemoveAll(x => x.ObjectType == objectType && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot && x.MaterialName == material.NameFormatted() && x.NullCheck());
#endif
        }

        /// <summary>
        /// Add a shader render queue to be saved and loaded with the card and optionally also update the materials.
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="renderQueue">Value</param>
        /// <param name="gameObject">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void SetMaterialShaderRenderQueue(int slot, Material material, int renderQueue, GameObject gameObject, bool setProperty = true)
        {
            ObjectType objectType = FindGameObjectType(gameObject);
            var materialProperty = MaterialShaderList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot && x.MaterialName == material.NameFormatted());
            if (materialProperty == null)
            {
                int renderQueueOriginal = material.renderQueue;
                MaterialShaderList.Add(new MaterialShader(objectType, CurrentCoordinateIndex, slot, material.NameFormatted(), renderQueue, renderQueueOriginal));
            }
            else
            {
                if (renderQueue == materialProperty.RenderQueueOriginal)
                    RemoveMaterialShaderRenderQueue(slot, material, gameObject, false);
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
        /// Get the saved render queue or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="gameObject">GameObject the material belongs to</param>
        /// <returns>Saved render queue or null if none is saved</returns>
        public int? GetMaterialShaderRenderQueue(int slot, Material material, GameObject gameObject)
        {
            ObjectType objectType = FindGameObjectType(gameObject);
            return MaterialShaderList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot && x.MaterialName == material.NameFormatted())?.RenderQueue;
        }
        /// <summary>
        /// Get the saved render queue's original value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="gameObject">GameObject the material belongs to</param>
        /// <returns>Saved render queue's original value or null if none is saved</returns>
        public int? GetMaterialShaderRenderQueueOriginal(int slot, Material material, GameObject gameObject)
        {
            ObjectType objectType = FindGameObjectType(gameObject);
            return MaterialShaderList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot && x.MaterialName == material.NameFormatted())?.RenderQueueOriginal;
        }
        /// <summary>
        /// Remove the saved render queue if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="gameObject">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void RemoveMaterialShaderRenderQueue(int slot, Material material, GameObject gameObject, bool setProperty = true)
        {
            ObjectType objectType = FindGameObjectType(gameObject);
            if (setProperty)
            {
                var original = GetMaterialShaderRenderQueueOriginal(slot, material, gameObject);
                if (original != null)
                    SetRenderQueue(gameObject, material.NameFormatted(), original);
            }

            foreach (var materialProperty in MaterialShaderList.Where(x => x.ObjectType == objectType && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot && x.MaterialName == material.NameFormatted()))
            {
                materialProperty.RenderQueue = null;
                materialProperty.RenderQueueOriginal = null;
            }

            MaterialShaderList.RemoveAll(x => x.ObjectType == objectType && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot && x.MaterialName == material.NameFormatted() && x.NullCheck());
        }
        #endregion

        private bool coordinateChanging = false;
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

        private bool accessorySelectedSlotChanging = false;
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

        private bool clothesChanging = false;
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

        private bool characterLoading = false;
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

        private bool refreshingTextures = false;
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

        private bool customClothesOverride = false;
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
                return ChaControl.objClothes[slot];
            else if (objectType == ObjectType.Accessory)
                return AccessoriesApi.GetAccessory(ChaControl, slot)?.gameObject;
            else if (objectType == ObjectType.Hair)
                return ChaControl.objHair[slot]?.gameObject;
            else if (objectType == ObjectType.Character)
                return ChaControl.gameObject;
            return null;
        }

        private ObjectType FindGameObjectType(GameObject gameObject)
        {
            if (gameObject.GetComponent<ChaControl>())
                return ObjectType.Character;
#if KK || EC
            if (gameObject.GetComponentInChildren<ChaClothesComponent>())
                return ObjectType.Clothing;
            if (gameObject.GetComponent<ChaAccessoryComponent>())
                return ObjectType.Accessory;
            if (gameObject.GetComponent<ChaCustomHairComponent>())
                return ObjectType.Hair;
#elif AI || HS2
            if (gameObject.GetComponent<CmpClothes>())
                return ObjectType.Clothing;
            if (gameObject.GetComponent<CmpAccessory>())
                return ObjectType.Accessory;
            if (gameObject.GetComponent<CmpHair>())
                return ObjectType.Hair;
#endif

            throw new Exception("Could not determine object's type. Object may have a missing or misconfigured MonoBehavior.");
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
