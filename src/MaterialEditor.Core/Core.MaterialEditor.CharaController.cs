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
#if AI
using AIChara;
#endif

namespace KK_Plugins.MaterialEditor
{
    public class MaterialEditorCharaController : CharaCustomFunctionController
    {
        private readonly List<RendererProperty> RendererPropertyList = new List<RendererProperty>();
        private readonly List<MaterialFloatProperty> MaterialFloatPropertyList = new List<MaterialFloatProperty>();
        private readonly List<MaterialColorProperty> MaterialColorPropertyList = new List<MaterialColorProperty>();
        private readonly List<MaterialTextureProperty> MaterialTexturePropertyList = new List<MaterialTextureProperty>();
        private readonly List<MaterialShader> MaterialShaderList = new List<MaterialShader>();

        private Dictionary<int, TextureContainer> TextureDictionary = new Dictionary<int, TextureContainer>();

#if KK
        public int CurrentCoordinateIndex => ChaControl.fileStatus.coordinateType;
#else
            public int CurrentCoordinateIndex => 0;
#endif
        private string FileToSet = null;
        private ObjectType ObjectTypeToSet;
        private string PropertyToSet;
        private string MatToSet;
        private int CoordinateIndexToSet;
        private int SlotToSet;
        private GameObject GameObjectToSet;

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

        protected override void OnReload(GameMode currentGameMode, bool maintainState)
        {
            if (!maintainState)
            {
                RendererPropertyList.Clear();
                MaterialFloatPropertyList.Clear();
                MaterialColorPropertyList.Clear();
                MaterialTexturePropertyList.Clear();
                MaterialShaderList.Clear();

                foreach (var textureHolder in TextureDictionary.Values) textureHolder.Dispose();
                TextureDictionary.Clear();

                var data = GetExtendedData();

                if (data == null) return;

                CharacterLoading = true;

                if (data.data.TryGetValue(nameof(TextureDictionary), out var texDic) && texDic != null)
                    TextureDictionary = MessagePackSerializer.Deserialize<Dictionary<int, byte[]>>((byte[])texDic).ToDictionary(pair => pair.Key, pair => new TextureContainer(pair.Value));

                //int counter = 0;
                //foreach (var tex in TextureDictionary.Values)
                //{
                //    string filename = Path.Combine(ExportPath, $"_Export_{ChaControl.chaFile.parameter.fullname.Trim()}_{counter}.png");
                //    SaveTex(TextureFromBytes(tex), filename);
                //    Logger.LogInfo($"Exported {filename}");
                //    counter++;
                //}

                if (data.data.TryGetValue(nameof(MaterialShaderList), out var shaderProperties) && shaderProperties != null)
                    foreach (var loadedShaderProperty in MessagePackSerializer.Deserialize<List<MaterialShader>>((byte[])shaderProperties))
                        MaterialShaderList.Add(new MaterialShader(loadedShaderProperty.ObjectType, loadedShaderProperty.CoordinateIndex, loadedShaderProperty.Slot, loadedShaderProperty.MaterialName, loadedShaderProperty.ShaderName, loadedShaderProperty.ShaderNameOriginal, loadedShaderProperty.RenderQueue, loadedShaderProperty.RenderQueueOriginal));

                if (data.data.TryGetValue(nameof(RendererPropertyList), out var rendererProperties) && rendererProperties != null)
                    foreach (var loadedRendererProperty in MessagePackSerializer.Deserialize<List<RendererProperty>>((byte[])rendererProperties))
                        RendererPropertyList.Add(new RendererProperty(loadedRendererProperty.ObjectType, loadedRendererProperty.CoordinateIndex, loadedRendererProperty.Slot, loadedRendererProperty.RendererName, loadedRendererProperty.Property, loadedRendererProperty.Value, loadedRendererProperty.ValueOriginal));

                if (data.data.TryGetValue(nameof(MaterialFloatPropertyList), out var materialFloatProperties) && materialFloatProperties != null)
                    foreach (var loadedMaterialFloatProperty in MessagePackSerializer.Deserialize<List<MaterialFloatProperty>>((byte[])materialFloatProperties))
                        MaterialFloatPropertyList.Add(new MaterialFloatProperty(loadedMaterialFloatProperty.ObjectType, loadedMaterialFloatProperty.CoordinateIndex, loadedMaterialFloatProperty.Slot, loadedMaterialFloatProperty.MaterialName, loadedMaterialFloatProperty.Property, loadedMaterialFloatProperty.Value, loadedMaterialFloatProperty.ValueOriginal));

                if (data.data.TryGetValue(nameof(MaterialColorPropertyList), out var materialColorProperties) && materialColorProperties != null)
                    foreach (var loadedMaterialColorProperty in MessagePackSerializer.Deserialize<List<MaterialColorProperty>>((byte[])materialColorProperties))
                        MaterialColorPropertyList.Add(new MaterialColorProperty(loadedMaterialColorProperty.ObjectType, loadedMaterialColorProperty.CoordinateIndex, loadedMaterialColorProperty.Slot, loadedMaterialColorProperty.MaterialName, loadedMaterialColorProperty.Property, loadedMaterialColorProperty.Value, loadedMaterialColorProperty.ValueOriginal));

                if (data.data.TryGetValue(nameof(MaterialTexturePropertyList), out var materialTextureProperties) && materialTextureProperties != null)
                    foreach (var loadedMaterialTextureProperty in MessagePackSerializer.Deserialize<List<MaterialTextureProperty>>((byte[])materialTextureProperties))
                        MaterialTexturePropertyList.Add(new MaterialTextureProperty(loadedMaterialTextureProperty.ObjectType, loadedMaterialTextureProperty.CoordinateIndex, loadedMaterialTextureProperty.Slot, loadedMaterialTextureProperty.MaterialName, loadedMaterialTextureProperty.Property, loadedMaterialTextureProperty.TexID, loadedMaterialTextureProperty.Offset, loadedMaterialTextureProperty.OffsetOriginal, loadedMaterialTextureProperty.Scale, loadedMaterialTextureProperty.ScaleOriginal));
            }

            ChaControl.StartCoroutine(LoadData(true, true, true));
        }

        internal new void Update()
        {
            try
            {
                if (FileToSet != null)
                {
                    AddMaterialTextureFromFile(ObjectTypeToSet, CoordinateIndexToSet, SlotToSet, MatToSet, PropertyToSet, FileToSet, GameObjectToSet);
                }
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

        protected override void OnCoordinateBeingLoaded(ChaFileCoordinate coordinate, bool maintainState)
        {
            var data = GetCoordinateExtendedData(coordinate);

            MaterialShaderList.RemoveAll(x => (x.ObjectType == ObjectType.Accessory || x.ObjectType == ObjectType.Clothing) && x.CoordinateIndex == CurrentCoordinateIndex);
            RendererPropertyList.RemoveAll(x => (x.ObjectType == ObjectType.Accessory || x.ObjectType == ObjectType.Clothing) && x.CoordinateIndex == CurrentCoordinateIndex);
            MaterialFloatPropertyList.RemoveAll(x => (x.ObjectType == ObjectType.Accessory || x.ObjectType == ObjectType.Clothing) && x.CoordinateIndex == CurrentCoordinateIndex);
            MaterialColorPropertyList.RemoveAll(x => (x.ObjectType == ObjectType.Accessory || x.ObjectType == ObjectType.Clothing) && x.CoordinateIndex == CurrentCoordinateIndex);
            MaterialTexturePropertyList.RemoveAll(x => (x.ObjectType == ObjectType.Accessory || x.ObjectType == ObjectType.Clothing) && x.CoordinateIndex == CurrentCoordinateIndex);

            if (data?.data == null) return;

            var importDictionary = new Dictionary<int, int>();

            if (data.data.TryGetValue(nameof(TextureDictionary), out var texDic) && texDic != null)
                foreach (var x in MessagePackSerializer.Deserialize<Dictionary<int, byte[]>>((byte[])texDic))
                    importDictionary[x.Key] = SetAndGetTextureID(x.Value);

            if (data.data.TryGetValue(nameof(MaterialShaderList), out var materialShaders) && materialShaders != null)
                foreach (var property in MessagePackSerializer.Deserialize<List<MaterialShader>>((byte[])materialShaders))
                    MaterialShaderList.Add(new MaterialShader(property.ObjectType, CurrentCoordinateIndex, property.Slot, property.MaterialName, property.ShaderName, property.ShaderNameOriginal, property.RenderQueue, property.RenderQueueOriginal));

            if (data.data.TryGetValue(nameof(RendererPropertyList), out var rendererProperties) && rendererProperties != null)
                foreach (var property in MessagePackSerializer.Deserialize<List<RendererProperty>>((byte[])rendererProperties))
                    RendererPropertyList.Add(new RendererProperty(property.ObjectType, CurrentCoordinateIndex, property.Slot, property.RendererName, property.Property, property.Value, property.ValueOriginal));

            if (data.data.TryGetValue(nameof(MaterialFloatPropertyList), out var materialFloatProperties) && materialFloatProperties != null)
                foreach (var property in MessagePackSerializer.Deserialize<List<MaterialFloatProperty>>((byte[])materialFloatProperties))
                    MaterialFloatPropertyList.Add(new MaterialFloatProperty(property.ObjectType, CurrentCoordinateIndex, property.Slot, property.MaterialName, property.Property, property.Value, property.ValueOriginal));

            if (data.data.TryGetValue(nameof(MaterialColorPropertyList), out var materialColorProperties) && materialColorProperties != null)
                foreach (var property in MessagePackSerializer.Deserialize<List<MaterialColorProperty>>((byte[])materialColorProperties))
                    MaterialColorPropertyList.Add(new MaterialColorProperty(property.ObjectType, CurrentCoordinateIndex, property.Slot, property.MaterialName, property.Property, property.Value, property.ValueOriginal));

            if (data.data.TryGetValue(nameof(MaterialTexturePropertyList), out var materialTextureProperties) && materialTextureProperties != null)
                foreach (var property in MessagePackSerializer.Deserialize<List<MaterialTextureProperty>>((byte[])materialTextureProperties))
                    MaterialTexturePropertyList.Add(new MaterialTextureProperty(property.ObjectType, CurrentCoordinateIndex, property.Slot, property.MaterialName, property.Property, property.TexID == null ? null : (int?)importDictionary[(int)property.TexID], property.Offset, property.OffsetOriginal, property.Scale, property.ScaleOriginal));

            CoordinateChanging = true;

            ChaControl.StartCoroutine(LoadData(true, true, false));
            base.OnCoordinateBeingLoaded(coordinate, maintainState);
        }

        private IEnumerator LoadData(bool clothes, bool accessories, bool hair)
        {
            yield return null;

            foreach (var property in MaterialShaderList)
            {
                if (property.ObjectType == ObjectType.Clothing && clothes && property.CoordinateIndex == CurrentCoordinateIndex)
                {
                    SetShader(ChaControl.objClothes[property.Slot], property.MaterialName, property.ShaderName);
                    SetRenderQueue(ChaControl.objClothes[property.Slot], property.MaterialName, property.RenderQueue);
                }
                else if (property.ObjectType == ObjectType.Accessory && accessories && property.CoordinateIndex == CurrentCoordinateIndex)
                {
                    SetShader(AccessoriesApi.GetAccessory(ChaControl, property.Slot)?.gameObject, property.MaterialName, property.ShaderName);
                    SetRenderQueue(AccessoriesApi.GetAccessory(ChaControl, property.Slot)?.gameObject, property.MaterialName, property.RenderQueue);
                }
                else if (property.ObjectType == ObjectType.Hair && hair)
                {
                    SetShader(ChaControl.objHair[property.Slot]?.gameObject, property.MaterialName, property.ShaderName);
                    SetRenderQueue(ChaControl.objHair[property.Slot]?.gameObject, property.MaterialName, property.RenderQueue);
                }
                else if (property.ObjectType == ObjectType.Character)
                {
                    SetShader(ChaControl, property.MaterialName, property.ShaderName);
                    SetRenderQueue(ChaControl, property.MaterialName, property.RenderQueue);
                }
            }
            foreach (var property in RendererPropertyList)
            {
                if (property.ObjectType == ObjectType.Clothing && clothes && property.CoordinateIndex == CurrentCoordinateIndex)
                    SetRendererProperty(ChaControl.objClothes[property.Slot], property.RendererName, property.Property, property.Value);
                else if (property.ObjectType == ObjectType.Accessory && accessories && property.CoordinateIndex == CurrentCoordinateIndex)
                    SetRendererProperty(AccessoriesApi.GetAccessory(ChaControl, property.Slot)?.gameObject, property.RendererName, property.Property, property.Value);
                else if (property.ObjectType == ObjectType.Hair && hair)
                    SetRendererProperty(ChaControl.objHair[property.Slot], property.RendererName, property.Property, property.Value);
                else if (property.ObjectType == ObjectType.Character)
                    SetRendererProperty(ChaControl.gameObject, property.RendererName, property.Property, property.Value);
            }
            foreach (var property in MaterialFloatPropertyList)
            {
                if (MaterialEditorPlugin.CheckBlacklist(property.ObjectType, property.Property)) continue;
                if (property.ObjectType == ObjectType.Clothing && clothes && property.CoordinateIndex == CurrentCoordinateIndex)
                    SetFloat(ChaControl.objClothes[property.Slot], property.MaterialName, property.Property, property.Value);
                else if (property.ObjectType == ObjectType.Accessory && accessories && property.CoordinateIndex == CurrentCoordinateIndex)
                    SetFloat(AccessoriesApi.GetAccessory(ChaControl, property.Slot)?.gameObject, property.MaterialName, property.Property, property.Value);
                else if (property.ObjectType == ObjectType.Hair && hair)
                    SetFloat(ChaControl.objHair[property.Slot]?.gameObject, property.MaterialName, property.Property, property.Value);
                else if (property.ObjectType == ObjectType.Character)
                    SetFloat(ChaControl, property.MaterialName, property.Property, property.Value);
            }
            foreach (var property in MaterialColorPropertyList)
            {
                if (MaterialEditorPlugin.CheckBlacklist(property.ObjectType, property.Property)) continue;
                if (property.ObjectType == ObjectType.Clothing && clothes && property.CoordinateIndex == CurrentCoordinateIndex)
                    SetColor(ChaControl.objClothes[property.Slot], property.MaterialName, property.Property, property.Value);
                else if (property.ObjectType == ObjectType.Accessory && accessories && property.CoordinateIndex == CurrentCoordinateIndex)
                    SetColor(AccessoriesApi.GetAccessory(ChaControl, property.Slot)?.gameObject, property.MaterialName, property.Property, property.Value);
                else if (property.ObjectType == ObjectType.Hair && hair)
                    SetColor(ChaControl.objHair[property.Slot]?.gameObject, property.MaterialName, property.Property, property.Value);
                else if (property.ObjectType == ObjectType.Character)
                    SetColor(ChaControl, property.MaterialName, property.Property, property.Value);
            }
            foreach (var property in MaterialTexturePropertyList)
            {
                if (MaterialEditorPlugin.CheckBlacklist(property.ObjectType, property.Property)) continue;
                {
                    if (property.ObjectType == ObjectType.Clothing && clothes && property.CoordinateIndex == CurrentCoordinateIndex)
                    {
                        if (property.TexID != null)
                            SetTexture(ChaControl.objClothes[property.Slot], property.MaterialName, property.Property, TextureDictionary[(int)property.TexID].Texture);
                        SetTextureOffset(ChaControl.objClothes[property.Slot], property.MaterialName, property.Property, property.Offset);
                        SetTextureScale(ChaControl.objClothes[property.Slot], property.MaterialName, property.Property, property.Scale);
                    }
                    else if (property.ObjectType == ObjectType.Accessory && accessories && property.CoordinateIndex == CurrentCoordinateIndex)
                    {
                        if (property.TexID != null)
                            SetTexture(AccessoriesApi.GetAccessory(ChaControl, property.Slot)?.gameObject, property.MaterialName, property.Property, TextureDictionary[(int)property.TexID].Texture);
                        SetTextureOffset(AccessoriesApi.GetAccessory(ChaControl, property.Slot)?.gameObject, property.MaterialName, property.Property, property.Offset);
                        SetTextureScale(AccessoriesApi.GetAccessory(ChaControl, property.Slot)?.gameObject, property.MaterialName, property.Property, property.Scale);
                    }
                    else if (property.ObjectType == ObjectType.Hair && hair)
                    {
                        if (property.TexID != null)
                            SetTexture(ChaControl.objHair[property.Slot]?.gameObject, property.MaterialName, property.Property, TextureDictionary[(int)property.TexID].Texture);
                        SetTextureOffset(ChaControl.objHair[property.Slot]?.gameObject, property.MaterialName, property.Property, property.Offset);
                        SetTextureScale(ChaControl.objHair[property.Slot]?.gameObject, property.MaterialName, property.Property, property.Scale);
                    }
                    else if (property.ObjectType == ObjectType.Character)
                    {
                        if (property.TexID != null)
                            SetTexture(ChaControl, property.MaterialName, property.Property, TextureDictionary[(int)property.TexID].Texture);
                        SetTextureOffset(ChaControl, property.MaterialName, property.Property, property.Offset);
                        SetTextureScale(ChaControl, property.MaterialName, property.Property, property.Scale);
                    }
                }
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
        }

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
        }

        internal void AccessorySelectedSlotChangeEvent(object sender, AccessorySlotEventArgs e)
        {
            if (!MakerAPI.InsideAndLoaded) return;

            AccessorySelectedSlotChanging = true;
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
            }
        }
#endif

        internal void ChangeAccessoryEvent(int slot, int type)
        {
#if AI
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
                if (MaterialEditorPlugin.CheckBlacklist(property.ObjectType, property.Property))
                    continue;

                if (property.ObjectType == ObjectType.Clothing && property.CoordinateIndex == CurrentCoordinateIndex && property.Property == "MainTex")
                    if (property.TexID != null)
                        SetTexture(ChaControl.objClothes[property.Slot], property.MaterialName, property.Property, TextureDictionary[(int)property.TexID].Texture);
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
                if (MaterialEditorPlugin.CheckBlacklist(property.ObjectType, property.Property))
                    continue;

                if (property.ObjectType == ObjectType.Character && property.Property == "MainTex")
                    if (property.TexID != null)
                        SetTexture(ChaControl, property.MaterialName, property.Property, TextureDictionary[(int)property.TexID].Texture);
            }
        }

        public void AddRendererProperty(ObjectType objectType, int coordinateIndex, int slot, string rendererName, RendererProperties property, string value, string valueOriginal)
        {
            RendererProperty rendererProperty = RendererPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == property && x.RendererName == rendererName);
            if (rendererProperty == null)
                RendererPropertyList.Add(new RendererProperty(objectType, coordinateIndex, slot, rendererName, property, value, valueOriginal));
            else
            {
                if (value == rendererProperty.ValueOriginal)
                    RendererPropertyList.Remove(rendererProperty);
                else
                    rendererProperty.Value = value;
            }
        }
        public void AddRendererProperty(ObjectType objectType, int coordinateIndex, int slot, string rendererName, RendererProperties property, string value, string valueOriginal, GameObject gameObject, bool setProperty = true)
        {
            AddRendererProperty(objectType, coordinateIndex, slot, rendererName, property, value, valueOriginal);
            if (setProperty)
                SetRendererProperty(gameObject, rendererName, property, value);
        }
        public string GetRendererPropertyValue(ObjectType objectType, int coordinateIndex, int slot, string rendererName, RendererProperties property) =>
            RendererPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == property && x.RendererName == rendererName)?.Value;
        public string GetRendererPropertyValueOriginal(ObjectType objectType, int coordinateIndex, int slot, string rendererName, RendererProperties property) =>
            RendererPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == property && x.RendererName == rendererName)?.ValueOriginal;
        public void RemoveRendererProperty(ObjectType objectType, int coordinateIndex, int slot, string rendererName, RendererProperties property) =>
            RendererPropertyList.RemoveAll(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == property && x.RendererName == rendererName);
        public void RemoveRendererProperty(ObjectType objectType, int coordinateIndex, int slot, string rendererName, RendererProperties property, GameObject gameObject, bool setProperty = true)
        {
            if (setProperty)
            {
                var original = GetRendererPropertyValueOriginal(objectType, coordinateIndex, slot, rendererName, property);
                if (!original.IsNullOrEmpty())
                    SetRendererProperty(gameObject, rendererName, RendererProperties.Enabled, original);
            }
            RemoveRendererProperty(objectType, coordinateIndex, slot, rendererName, property);
        }

        public void AddMaterialFloatProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, float value, float valueOriginal)
            => AddMaterialFloatProperty(objectType, coordinateIndex, slot, materialName, propertyName, value.ToString(), valueOriginal.ToString());
        public void AddMaterialFloatProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, string value, string valueOriginal)
        {
            var materialProperty = MaterialFloatPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == propertyName && x.MaterialName == materialName);
            if (materialProperty == null)
                MaterialFloatPropertyList.Add(new MaterialFloatProperty(objectType, coordinateIndex, slot, materialName, propertyName, value, valueOriginal));
            else
            {
                if (value == materialProperty.ValueOriginal)
                    MaterialFloatPropertyList.Remove(materialProperty);
                else
                    materialProperty.Value = value;
            }
        }

        public void AddMaterialFloatProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, float value, float valueOriginal, GameObject gameObject, bool setProperty = true)
            => AddMaterialFloatProperty(objectType, coordinateIndex, slot, materialName, propertyName, value.ToString(), valueOriginal.ToString(), gameObject, setProperty);
        public void AddMaterialFloatProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, string value, string valueOriginal, GameObject gameObject, bool setProperty = true)
        {
            AddMaterialFloatProperty(objectType, coordinateIndex, slot, materialName, propertyName, value, valueOriginal);
            if (setProperty)
                SetFloat(gameObject, materialName, propertyName, value);
        }

        public string GetMaterialFloatPropertyValue(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName) =>
            MaterialFloatPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == propertyName && x.MaterialName == materialName)?.Value;
        public string GetMaterialFloatPropertyValueOriginal(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName) =>
            MaterialFloatPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == propertyName && x.MaterialName == materialName)?.ValueOriginal;
        public void RemoveMaterialFloatProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName) =>
            MaterialFloatPropertyList.RemoveAll(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == propertyName && x.MaterialName == materialName);
        public void RemoveMaterialFloatProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject, bool setProperty = true)
        {
            if (setProperty)
            {
                var original = GetMaterialFloatPropertyValueOriginal(objectType, coordinateIndex, slot, materialName, propertyName);
                if (!original.IsNullOrEmpty())
                    SetFloat(gameObject, materialName, propertyName, original);
            }
            RemoveMaterialFloatProperty(objectType, coordinateIndex, slot, materialName, propertyName);
        }

        public void AddMaterialColorProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, Color value, Color valueOriginal)
        {
            var colorProperty = MaterialColorPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == propertyName && x.MaterialName == materialName);
            if (colorProperty == null)
                MaterialColorPropertyList.Add(new MaterialColorProperty(objectType, coordinateIndex, slot, materialName, propertyName, value, valueOriginal));
            else
            {
                if (value == colorProperty.ValueOriginal)
                    MaterialColorPropertyList.Remove(colorProperty);
                else
                    colorProperty.Value = value;
            }
        }
        public void AddMaterialColorProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, Color value, Color valueOriginal, GameObject gameObject, bool setProperty = true)
        {
            AddMaterialColorProperty(objectType, coordinateIndex, slot, materialName, propertyName, value, valueOriginal);
            if (setProperty)
                SetColor(gameObject, materialName, propertyName, value);
        }
        public Color? GetMaterialColorPropertyValue(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName) =>
            MaterialColorPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == propertyName && x.MaterialName == materialName)?.Value;
        public Color? GetMaterialColorPropertyValueOriginal(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName) =>
            MaterialColorPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == propertyName && x.MaterialName == materialName)?.ValueOriginal;
        public void RemoveMaterialColorProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName) =>
            MaterialColorPropertyList.RemoveAll(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == propertyName && x.MaterialName == materialName);
        public void RemoveMaterialColorProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject, bool setProperty = true)
        {
            if (setProperty)
            {
                var original = GetMaterialColorPropertyValueOriginal(objectType, coordinateIndex, slot, materialName, propertyName);
                if (original != null)
                    SetColor(gameObject, materialName, propertyName, (Color)original);
            }
            RemoveMaterialColorProperty(objectType, coordinateIndex, slot, materialName, propertyName);
        }

        public void AddMaterialTextureFromFile(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, string filePath, GameObject gameObject, bool setTexInUpdate = false)
        {
            if (!File.Exists(filePath)) return;

            if (setTexInUpdate)
            {
                FileToSet = filePath;
                PropertyToSet = propertyName;
                MatToSet = materialName;
                GameObjectToSet = gameObject;
                ObjectTypeToSet = objectType;
                CoordinateIndexToSet = coordinateIndex;
                SlotToSet = slot;
            }
            else
            {
                var texBytes = File.ReadAllBytes(filePath);
                Texture2D tex = MaterialEditorPlugin.TextureFromBytes(texBytes);

                if (objectType == ObjectType.Character)
                    SetTexture(ChaControl, materialName, propertyName, tex);
                else
                    SetTexture(GameObjectToSet, materialName, propertyName, tex);

                var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == propertyName && x.MaterialName == materialName);
                if (textureProperty == null)
                    MaterialTexturePropertyList.Add(new MaterialTextureProperty(objectType, coordinateIndex, slot, materialName, propertyName, SetAndGetTextureID(texBytes)));
                else
                    textureProperty.TexID = SetAndGetTextureID(texBytes);

            }
        }
        public Texture2D GetMaterialTexture(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName)
        {
            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == propertyName && x.MaterialName == materialName);
            if (textureProperty?.TexID != null)
                return TextureDictionary[(int)textureProperty.TexID].Texture;
            return null;
        }
        public bool GetMaterialTextureOriginal(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName) =>
            MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == propertyName && x.MaterialName == materialName)?.TexID == null ? true : false;
        public void RemoveMaterialTexture(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, bool displayMessage = true)
        {
            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == propertyName && x.MaterialName == materialName);
            if (textureProperty != null)
            {
                if (displayMessage)
                    MaterialEditorPlugin.Logger.LogMessage("Save and reload character or change outfits to refresh textures.");
                textureProperty.TexID = null;
                if (textureProperty.NullCheck())
                    MaterialTexturePropertyList.Remove(textureProperty);
            }
        }

        public void AddMaterialTextureOffset(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, Vector2 value, Vector2 valueOriginal)
        {
            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == propertyName && x.MaterialName == materialName);
            if (textureProperty == null)
                MaterialTexturePropertyList.Add(new MaterialTextureProperty(objectType, coordinateIndex, slot, materialName, propertyName, offset: value, offsetOriginal: valueOriginal));
            else
            {
                if (value == textureProperty.OffsetOriginal)
                {
                    textureProperty.Offset = null;
                    textureProperty.OffsetOriginal = null;
                    if (textureProperty.NullCheck())
                        MaterialTexturePropertyList.Remove(textureProperty);
                }
                else
                {
                    textureProperty.Offset = value;
                    textureProperty.OffsetOriginal = valueOriginal;
                }
            }
        }
        public void AddMaterialTextureOffset(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, Vector2 value, Vector2 valueOriginal, GameObject gameObject, bool setProperty = true)
        {
            AddMaterialTextureOffset(objectType, coordinateIndex, slot, materialName, propertyName, value, valueOriginal);
            if (setProperty)
                SetTextureOffset(gameObject, materialName, propertyName, value);
        }
        public Vector2? GetMaterialTextureOffset(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName) =>
            MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == propertyName && x.MaterialName == materialName)?.Offset;
        public Vector2? GetMaterialTextureOffsetOriginal(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName) =>
            MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == propertyName && x.MaterialName == materialName)?.OffsetOriginal;
        public void RemoveMaterialTextureOffset(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName)
        {
            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == propertyName && x.MaterialName == materialName);
            if (textureProperty != null)
            {
                textureProperty.Offset = null;
                textureProperty.OffsetOriginal = null;
                if (textureProperty.NullCheck())
                    MaterialTexturePropertyList.Remove(textureProperty);
            }
        }
        public void RemoveMaterialTextureOffset(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject, bool setProperty = true)
        {
            if (setProperty)
            {
                var original = GetMaterialTextureOffsetOriginal(objectType, coordinateIndex, slot, materialName, propertyName);
                if (original != null)
                    SetTextureOffset(gameObject, materialName, propertyName, (Vector2)original);
            }
            RemoveMaterialTextureOffset(objectType, coordinateIndex, slot, materialName, propertyName);
        }

        public void AddMaterialTextureScale(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, Vector2 value, Vector2 valueOriginal)
        {
            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == propertyName && x.MaterialName == materialName);
            if (textureProperty == null)
                MaterialTexturePropertyList.Add(new MaterialTextureProperty(objectType, coordinateIndex, slot, materialName, propertyName, scale: value, scaleOriginal: valueOriginal));
            else
            {
                if (value == textureProperty.ScaleOriginal)
                {
                    textureProperty.Scale = null;
                    textureProperty.ScaleOriginal = null;
                    if (textureProperty.NullCheck())
                        MaterialTexturePropertyList.Remove(textureProperty);
                }
                else
                {
                    textureProperty.Scale = value;
                    textureProperty.ScaleOriginal = valueOriginal;
                }
            }
        }
        public void AddMaterialTextureScale(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, Vector2 value, Vector2 valueOriginal, GameObject gameObject, bool setProperty = true)
        {
            AddMaterialTextureScale(objectType, coordinateIndex, slot, materialName, propertyName, value, valueOriginal);
            if (setProperty)
                SetTextureScale(gameObject, materialName, propertyName, value);
        }
        public Vector2? GetMaterialTextureScale(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName) =>
            MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == propertyName && x.MaterialName == materialName)?.Scale;
        public Vector2? GetMaterialTextureScaleOriginal(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName) =>
            MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == propertyName && x.MaterialName == materialName)?.ScaleOriginal;

        public void RemoveMaterialTextureScale(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName)
        {
            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == propertyName && x.MaterialName == materialName);
            if (textureProperty != null)
            {
                textureProperty.Scale = null;
                textureProperty.ScaleOriginal = null;
                if (textureProperty.NullCheck())
                    MaterialTexturePropertyList.Remove(textureProperty);
            }
        }
        public void RemoveMaterialTextureScale(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject, bool setProperty = true)
        {
            if (setProperty)
            {
                var original = GetMaterialTextureScaleOriginal(objectType, coordinateIndex, slot, materialName, propertyName);
                if (original != null)
                    SetTextureScale(gameObject, materialName, propertyName, (Vector2)original);
            }
            RemoveMaterialTextureScale(objectType, coordinateIndex, slot, materialName, propertyName);
        }

        public void AddMaterialShader(ObjectType objectType, int coordinateIndex, int slot, string materialName, string shaderName, string shaderNameOriginal)
        {
            var materialProperty = MaterialShaderList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.MaterialName == materialName);
            if (materialProperty == null)
                MaterialShaderList.Add(new MaterialShader(objectType, coordinateIndex, slot, materialName, shaderName, shaderNameOriginal));
            else
            {
                materialProperty.ShaderName = shaderName;
                materialProperty.ShaderNameOriginal = shaderNameOriginal;
            }
        }
        public void AddMaterialShader(ObjectType objectType, int coordinateIndex, int slot, string materialName, string shaderName, string shaderNameOriginal, GameObject gameObject, bool setProperty = true)
        {
            AddMaterialShader(objectType, coordinateIndex, slot, materialName, shaderName, shaderNameOriginal);
            if (setProperty)
            {
                RemoveMaterialShaderRenderQueue(objectType, coordinateIndex, slot, materialName);
                SetShader(gameObject, materialName, shaderName);
            }
        }

        public void AddMaterialShaderRenderQueue(ObjectType objectType, int coordinateIndex, int slot, string materialName, int renderQueue, int renderQueueOriginal)
        {
            var materialProperty = MaterialShaderList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.MaterialName == materialName);
            if (materialProperty == null)
                MaterialShaderList.Add(new MaterialShader(objectType, coordinateIndex, slot, materialName, renderQueue, renderQueueOriginal));
            else
            {
                if (renderQueue == materialProperty.RenderQueueOriginal)
                {
                    materialProperty.RenderQueue = null;
                    materialProperty.RenderQueueOriginal = null;
                    if (materialProperty.NullCheck())
                        MaterialShaderList.Remove(materialProperty);
                }
                else
                {
                    materialProperty.RenderQueue = renderQueue;
                    materialProperty.RenderQueueOriginal = renderQueueOriginal;
                }
            }
        }
        public void AddMaterialShaderRenderQueue(ObjectType objectType, int coordinateIndex, int slot, string materialName, int renderQueue, int renderQueueOriginal, GameObject gameObject, bool setProperty = true)
        {
            AddMaterialShaderRenderQueue(objectType, coordinateIndex, slot, materialName, renderQueue, renderQueueOriginal);
            if (setProperty)
                SetRenderQueue(gameObject, materialName, renderQueue);
        }
        public MaterialShader GetMaterialShaderValue(ObjectType objectType, int coordinateIndex, int slot, string materialName) =>
            MaterialShaderList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.MaterialName == materialName);
        public void RemoveMaterialShaderName(ObjectType objectType, int coordinateIndex, int slot, string materialName)
        {
#if EC
            //For EC don't remove shaders when reset, this helps users with fixing KK mods
            var materialProperty = MaterialShaderList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.MaterialName == materialName);
            if (materialProperty == null)
                return;
            else
                materialProperty.ShaderName = materialProperty.ShaderNameOriginal;
#else
            foreach (var materialProperty in MaterialShaderList.Where(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.MaterialName == materialName))
            {
                materialProperty.ShaderName = null;
                materialProperty.ShaderNameOriginal = null;
            }

            MaterialShaderList.RemoveAll(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.MaterialName == materialName && x.NullCheck());
#endif
        }
        public void RemoveMaterialShaderName(ObjectType objectType, int coordinateIndex, int slot, string materialName, GameObject gameObject, bool setProperty = true)
        {
            if (setProperty)
            {
                var original = GetMaterialShaderValue(objectType, coordinateIndex, slot, materialName);
                if (original != null && !original.ShaderNameOriginal.IsNullOrEmpty())
                    SetShader(gameObject, materialName, original.ShaderNameOriginal);
            }
            RemoveMaterialShaderName(objectType, coordinateIndex, slot, materialName);
        }
        public void RemoveMaterialShaderRenderQueue(ObjectType objectType, int coordinateIndex, int slot, string materialName)
        {
            foreach (var materialProperty in MaterialShaderList.Where(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.MaterialName == materialName))
            {
                materialProperty.RenderQueue = null;
                materialProperty.RenderQueueOriginal = null;
            }

            MaterialShaderList.RemoveAll(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.MaterialName == materialName && x.NullCheck());
        }
        public void RemoveMaterialShaderRenderQueue(ObjectType objectType, int coordinateIndex, int slot, string materialName, GameObject gameObject, bool setProperty = true)
        {
            if (setProperty)
            {
                var original = GetMaterialShaderValue(objectType, coordinateIndex, slot, materialName);
                if (original?.RenderQueueOriginal != null)
                    SetRenderQueue(gameObject, materialName, original.RenderQueueOriginal);
            }
            RemoveMaterialShaderRenderQueue(objectType, coordinateIndex, slot, materialName);
        }

        private bool coordinateChanging = false;
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

        [Serializable]
        [MessagePackObject]
        public class RendererProperty
        {
            [Key("ObjectType")]
            public ObjectType ObjectType;
            [Key("CoordinateIndex")]
            public int CoordinateIndex;
            [Key("Slot")]
            public int Slot;
            [Key("RendererName")]
            public string RendererName;
            [Key("Property")]
            public RendererProperties Property;
            [Key("Value")]
            public string Value;
            [Key("ValueOriginal")]
            public string ValueOriginal;

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

        [Serializable]
        [MessagePackObject]
        public class MaterialFloatProperty
        {
            [Key("ObjectType")]
            public ObjectType ObjectType;
            [Key("CoordinateIndex")]
            public int CoordinateIndex;
            [Key("Slot")]
            public int Slot;
            [Key("MaterialName")]
            public string MaterialName;
            [Key("Property")]
            public string Property;
            [Key("Value")]
            public string Value;
            [Key("ValueOriginal")]
            public string ValueOriginal;

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

        [Serializable]
        [MessagePackObject]
        public class MaterialColorProperty
        {
            [Key("ObjectType")]
            public ObjectType ObjectType;
            [Key("CoordinateIndex")]
            public int CoordinateIndex;
            [Key("Slot")]
            public int Slot;
            [Key("MaterialName")]
            public string MaterialName;
            [Key("Property")]
            public string Property;
            [Key("Value")]
            public Color Value;
            [Key("ValueOriginal")]
            public Color ValueOriginal;

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
        [Serializable]
        [MessagePackObject]
        public class MaterialTextureProperty
        {
            [Key("ObjectType")]
            public ObjectType ObjectType;
            [Key("CoordinateIndex")]
            public int CoordinateIndex;
            [Key("Slot")]
            public int Slot;
            [Key("MaterialName")]
            public string MaterialName;
            [Key("Property")]
            public string Property;
            [Key("TexID")]
            public int? TexID;
            [Key("Offset")]
            public Vector2? Offset;
            [Key("OffsetOriginal")]
            public Vector2? OffsetOriginal;
            [Key("Scale")]
            public Vector2? Scale;
            [Key("ScaleOriginal")]
            public Vector2? ScaleOriginal;

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

            public bool NullCheck() => TexID == null && Offset == null && Scale == null;
        }
        [Serializable]
        [MessagePackObject]
        public class MaterialShader
        {
            [Key("ObjectType")]
            public ObjectType ObjectType;
            [Key("CoordinateIndex")]
            public int CoordinateIndex;
            [Key("Slot")]
            public int Slot;
            [Key("MaterialName")]
            public string MaterialName;
            [Key("ShaderName")]
            public string ShaderName;
            [Key("ShaderNameOriginal")]
            public string ShaderNameOriginal;
            [Key("RenderQueue")]
            public int? RenderQueue;
            [Key("RenderQueueOriginal")]
            public int? RenderQueueOriginal;

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
            public MaterialShader(ObjectType objectType, int coordinateIndex, int slot, string materialName, string shaderName, string shaderNameOriginal)
            {
                ObjectType = objectType;
                CoordinateIndex = coordinateIndex;
                Slot = slot;
                MaterialName = materialName.Replace("(Instance)", "").Trim();
                ShaderName = shaderName;
                ShaderNameOriginal = shaderNameOriginal;
            }
            public MaterialShader(ObjectType objectType, int coordinateIndex, int slot, string materialName, int? renderQueue, int? renderQueueOriginal)
            {
                ObjectType = objectType;
                CoordinateIndex = coordinateIndex;
                Slot = slot;
                MaterialName = materialName.Replace("(Instance)", "").Trim();
                RenderQueue = renderQueue;
                RenderQueueOriginal = renderQueueOriginal;
            }
            public bool NullCheck() => ShaderName.IsNullOrEmpty() && RenderQueue == null;
        }
    }
}
