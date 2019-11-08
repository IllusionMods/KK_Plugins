using ExtensibleSaveFormat;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Utilities;
using MessagePack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniRx;
using UnityEngine;
#if AI
using AIChara;
#endif

namespace KK_Plugins
{
    public partial class MaterialEditor
    {
        public class MaterialEditorCharaController : CharaCustomFunctionController
        {
            private readonly List<RendererProperty> RendererPropertyList = new List<RendererProperty>();
            private readonly List<MaterialFloatProperty> MaterialFloatPropertyList = new List<MaterialFloatProperty>();
            private readonly List<MaterialColorProperty> MaterialColorPropertyList = new List<MaterialColorProperty>();
            private readonly List<MaterialTextureProperty> MaterialTexturePropertyList = new List<MaterialTextureProperty>();
            private readonly List<MaterialShader> MaterialShaderList = new List<MaterialShader>();

            private Dictionary<int, byte[]> TextureDictionary = new Dictionary<int, byte[]>();

#if KK
            public int CurrentCoordinateIndex => ChaControl.fileStatus.coordinateType;
#else
            public int CurrentCoordinateIndex => 0;
#endif
            private byte[] TexBytes = null;
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
                    TextureDictionary.Remove(texID);

                if (TextureDictionary.Count > 0)
                    data.data.Add(nameof(TextureDictionary), MessagePackSerializer.Serialize(TextureDictionary));
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
                RendererPropertyList.Clear();
                MaterialFloatPropertyList.Clear();
                MaterialColorPropertyList.Clear();
                MaterialTexturePropertyList.Clear();
                MaterialShaderList.Clear();
                TextureDictionary.Clear();

                var data = GetExtendedData();

                if (data == null) return;

                CharacterLoading = true;

                if (data.data.TryGetValue(nameof(TextureDictionary), out var texDic) && texDic != null)
                    TextureDictionary = MessagePackSerializer.Deserialize<Dictionary<int, byte[]>>((byte[])texDic);

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

                ChaControl.StartCoroutine(LoadData(true, true, true));
            }

            internal new void Update()
            {
                try
                {
                    if (TexBytes != null)
                    {
                        Texture2D tex = new Texture2D(2, 2);
                        tex.LoadImage(TexBytes);

                        if (ObjectTypeToSet == ObjectType.Character)
                            SetTextureProperty(ChaControl, MatToSet, PropertyToSet, tex);
                        else
                            SetTextureProperty(GameObjectToSet, MatToSet, PropertyToSet, tex, ObjectTypeToSet);

                        var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == ObjectTypeToSet && x.CoordinateIndex == CoordinateIndexToSet && x.Slot == SlotToSet && x.Property == PropertyToSet && x.MaterialName == MatToSet);
                        if (textureProperty == null)
                            MaterialTexturePropertyList.Add(new MaterialTextureProperty(ObjectTypeToSet, CoordinateIndexToSet, SlotToSet, MatToSet, PropertyToSet, SetAndGetTextureID(TexBytes)));
                        else
                            textureProperty.TexID = SetAndGetTextureID(TexBytes);
                    }
                }
                catch
                {
                    Logger.Log(BepInEx.Logging.LogLevel.Error | BepInEx.Logging.LogLevel.Message, "Failed to load texture.");
                }
                finally
                {
                    TexBytes = null;
                    PropertyToSet = "";
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
                        coordinateTextureDictionary.Add(tex.Key, tex.Value);

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
                        SetShader(ChaControl.objClothes[property.Slot], property.MaterialName, property.ShaderName, property.ObjectType);
                        SetRenderQueue(ChaControl.objClothes[property.Slot], property.MaterialName, property.RenderQueue, property.ObjectType);
                    }
                    else if (property.ObjectType == ObjectType.Accessory && accessories && property.CoordinateIndex == CurrentCoordinateIndex)
                    {
                        SetShader(AccessoriesApi.GetAccessory(ChaControl, property.Slot)?.gameObject, property.MaterialName, property.ShaderName, property.ObjectType);
                        SetRenderQueue(AccessoriesApi.GetAccessory(ChaControl, property.Slot)?.gameObject, property.MaterialName, property.RenderQueue, property.ObjectType);
                    }
                    else if (property.ObjectType == ObjectType.Hair && hair)
                    {
                        SetShader(ChaControl.objHair[property.Slot]?.gameObject, property.MaterialName, property.ShaderName, property.ObjectType);
                        SetRenderQueue(ChaControl.objHair[property.Slot]?.gameObject, property.MaterialName, property.RenderQueue, property.ObjectType);
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
                        SetRendererProperty(ChaControl.objClothes[property.Slot], property.RendererName, property.Property, property.Value, property.ObjectType);
                    else if (property.ObjectType == ObjectType.Accessory && accessories && property.CoordinateIndex == CurrentCoordinateIndex)
                        SetRendererProperty(AccessoriesApi.GetAccessory(ChaControl, property.Slot)?.gameObject, property.RendererName, property.Property, property.Value, property.ObjectType);
                    else if (property.ObjectType == ObjectType.Hair && hair)
                        SetRendererProperty(ChaControl.objHair[property.Slot], property.RendererName, property.Property, property.Value, property.ObjectType);
                    else if (property.ObjectType == ObjectType.Character)
                        SetRendererProperty(ChaControl.gameObject, property.RendererName, property.Property, property.Value, property.ObjectType);
                }
                foreach (var property in MaterialFloatPropertyList)
                {
                    if (CheckBlacklist(property.ObjectType, property.Property)) continue;
                    if (property.ObjectType == ObjectType.Clothing && clothes && property.CoordinateIndex == CurrentCoordinateIndex)
                        SetFloatProperty(ChaControl.objClothes[property.Slot], property.MaterialName, property.Property, property.Value, property.ObjectType);
                    else if (property.ObjectType == ObjectType.Accessory && accessories && property.CoordinateIndex == CurrentCoordinateIndex)
                        SetFloatProperty(AccessoriesApi.GetAccessory(ChaControl, property.Slot)?.gameObject, property.MaterialName, property.Property, property.Value, property.ObjectType);
                    else if (property.ObjectType == ObjectType.Hair && hair)
                        SetFloatProperty(ChaControl.objHair[property.Slot]?.gameObject, property.MaterialName, property.Property, property.Value, property.ObjectType);
                    else if (property.ObjectType == ObjectType.Character)
                        SetFloatProperty(ChaControl, property.MaterialName, property.Property, property.Value);
                }
                foreach (var property in MaterialColorPropertyList)
                {
                    if (CheckBlacklist(property.ObjectType, property.Property)) continue;
                    if (property.ObjectType == ObjectType.Clothing && clothes && property.CoordinateIndex == CurrentCoordinateIndex)
                        SetColorProperty(ChaControl.objClothes[property.Slot], property.MaterialName, property.Property, property.Value, property.ObjectType);
                    else if (property.ObjectType == ObjectType.Accessory && accessories && property.CoordinateIndex == CurrentCoordinateIndex)
                        SetColorProperty(AccessoriesApi.GetAccessory(ChaControl, property.Slot)?.gameObject, property.MaterialName, property.Property, property.Value, property.ObjectType);
                    else if (property.ObjectType == ObjectType.Hair && hair)
                        SetColorProperty(ChaControl.objHair[property.Slot]?.gameObject, property.MaterialName, property.Property, property.Value, property.ObjectType);
                    else if (property.ObjectType == ObjectType.Character)
                        SetColorProperty(ChaControl, property.MaterialName, property.Property, property.Value);
                }
                foreach (var property in MaterialTexturePropertyList)
                {
                    if (CheckBlacklist(property.ObjectType, property.Property)) continue;
                    {
                        if (property.ObjectType == ObjectType.Clothing && clothes && property.CoordinateIndex == CurrentCoordinateIndex)
                        {
                            if (property.TexID != null)
                                SetTextureProperty(ChaControl.objClothes[property.Slot], property.MaterialName, property.Property, TextureFromBytes(TextureDictionary[(int)property.TexID]), property.ObjectType);
                            SetTextureProperty(ChaControl.objClothes[property.Slot], property.MaterialName, property.Property, TexturePropertyType.Offset, property.Offset, property.ObjectType);
                            SetTextureProperty(ChaControl.objClothes[property.Slot], property.MaterialName, property.Property, TexturePropertyType.Scale, property.Scale, property.ObjectType);
                        }
                        else if (property.ObjectType == ObjectType.Accessory && accessories && property.CoordinateIndex == CurrentCoordinateIndex)
                        {
                            if (property.TexID != null)
                                SetTextureProperty(AccessoriesApi.GetAccessory(ChaControl, property.Slot)?.gameObject, property.MaterialName, property.Property, TextureFromBytes(TextureDictionary[(int)property.TexID]), property.ObjectType);
                            SetTextureProperty(AccessoriesApi.GetAccessory(ChaControl, property.Slot)?.gameObject, property.MaterialName, property.Property, TexturePropertyType.Offset, property.Offset, property.ObjectType);
                            SetTextureProperty(AccessoriesApi.GetAccessory(ChaControl, property.Slot)?.gameObject, property.MaterialName, property.Property, TexturePropertyType.Scale, property.Scale, property.ObjectType);
                        }
                        else if (property.ObjectType == ObjectType.Hair && hair)
                        {
                            if (property.TexID != null)
                                SetTextureProperty(ChaControl.objHair[property.Slot]?.gameObject, property.MaterialName, property.Property, TextureFromBytes(TextureDictionary[(int)property.TexID]), property.ObjectType);
                            SetTextureProperty(ChaControl.objHair[property.Slot]?.gameObject, property.MaterialName, property.Property, TexturePropertyType.Offset, property.Offset, property.ObjectType);
                            SetTextureProperty(ChaControl.objHair[property.Slot]?.gameObject, property.MaterialName, property.Property, TexturePropertyType.Scale, property.Scale, property.ObjectType);
                        }
                        else if (property.ObjectType == ObjectType.Character)
                        {
                            if (property.TexID != null)
                                SetTextureProperty(ChaControl, property.MaterialName, property.Property, TextureFromBytes(TextureDictionary[(int)property.TexID]));
                            SetTextureProperty(ChaControl, property.MaterialName, property.Property, TexturePropertyType.Offset, property.Offset);
                            SetTextureProperty(ChaControl, property.MaterialName, property.Property, TexturePropertyType.Scale, property.Scale);
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
                    if (tex.Value.SequenceEqual(textureBytes))
                        return tex.Key;
                    else if (tex.Key > highestID)
                        highestID = tex.Key;

                highestID++;
                TextureDictionary.Add(highestID, textureBytes);
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
                    UISystem.gameObject.SetActive(false);
            }

            internal void AccessoryKindChangeEvent(object sender, AccessorySlotEventArgs e)
            {
                if (AccessorySelectedSlotChanging)
                    return;
                if (CoordinateChanging)
                    return;

                //User switched accessories, remove all edited properties for this slot
                MaterialShaderList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SlotIndex);
                RendererPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SlotIndex);
                MaterialFloatPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SlotIndex);
                MaterialColorPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SlotIndex);
                MaterialTexturePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SlotIndex);

                if (UISystem.gameObject.activeInHierarchy)
                    PopulateListAccessory();
            }

            internal void AccessorySelectedSlotChangeEvent(object sender, AccessorySlotEventArgs e)
            {
                if (!MakerAPI.InsideAndLoaded) return;

                AccessorySelectedSlotChanging = true;

                if (UISystem.gameObject.activeInHierarchy)
                    PopulateListAccessory();
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

                UISystem.gameObject.SetActive(false);
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

                if ((int)e.CopyDestination == CurrentCoordinateIndex)
                    UISystem.gameObject.SetActive(false);
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

                UISystem.gameObject.SetActive(false);
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

                UISystem.gameObject.SetActive(false);
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

                UISystem.gameObject.SetActive(false);
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
            public string GetRendererPropertyValue(ObjectType objectType, int coordinateIndex, int slot, string rendererName, RendererProperties property) =>
                RendererPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == property && x.RendererName == rendererName)?.Value;
            public string GetRendererPropertyValueOriginal(ObjectType objectType, int coordinateIndex, int slot, string rendererName, RendererProperties property) =>
                RendererPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == property && x.RendererName == rendererName)?.ValueOriginal;
            public void RemoveRendererProperty(ObjectType objectType, int coordinateIndex, int slot, string rendererName, RendererProperties property) =>
                RendererPropertyList.RemoveAll(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == property && x.RendererName == rendererName);

            public void AddMaterialFloatProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string property, string value, string valueOriginal)
            {
                var materialProperty = MaterialFloatPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == property && x.MaterialName == materialName);
                if (materialProperty == null)
                    MaterialFloatPropertyList.Add(new MaterialFloatProperty(objectType, coordinateIndex, slot, materialName, property, value, valueOriginal));
                else
                {
                    if (value == materialProperty.ValueOriginal)
                        MaterialFloatPropertyList.Remove(materialProperty);
                    else
                        materialProperty.Value = value;
                }
            }
            public string GetMaterialFloatPropertyValue(ObjectType objectType, int coordinateIndex, int slot, string materialName, string property) =>
                MaterialFloatPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == property && x.MaterialName == materialName)?.Value;
            public string GetMaterialFloatPropertyValueOriginal(ObjectType objectType, int coordinateIndex, int slot, string materialName, string property) =>
                MaterialFloatPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == property && x.MaterialName == materialName)?.ValueOriginal;
            public void RemoveMaterialFloatProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string property) =>
                MaterialFloatPropertyList.RemoveAll(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == property && x.MaterialName == materialName);

            public void AddMaterialColorProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string property, Color value, Color valueOriginal)
            {
                var colorProperty = MaterialColorPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == property && x.MaterialName == materialName);
                if (colorProperty == null)
                    MaterialColorPropertyList.Add(new MaterialColorProperty(objectType, coordinateIndex, slot, materialName, property, value, valueOriginal));
                else
                {
                    if (value == colorProperty.ValueOriginal)
                        MaterialColorPropertyList.Remove(colorProperty);
                    else
                        colorProperty.Value = value;
                }
            }
            public Color GetMaterialColorPropertyValue(ObjectType objectType, int coordinateIndex, int slot, string materialName, string property)
            {
                if (MaterialColorPropertyList.Where(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == property && x.MaterialName == materialName).Count() == 0)
                    return new Color(-1, -1, -1, -1);
                return MaterialColorPropertyList.First(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == property && x.MaterialName == materialName).Value;
            }
            public Color GetMaterialColorPropertyValueOriginal(ObjectType objectType, int coordinateIndex, int slot, string materialName, string property)
            {
                if (MaterialColorPropertyList.Where(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == property && x.MaterialName == materialName).Count() == 0)
                    return new Color(-1, -1, -1, -1);
                return MaterialColorPropertyList.First(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == property && x.MaterialName == materialName).ValueOriginal;
            }
            public void RemoveMaterialColorProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string property) =>
                MaterialColorPropertyList.RemoveAll(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == property && x.MaterialName == materialName);

            public void AddMaterialTextureProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string property, GameObject go)
            {
                OpenFileDialog.Show(strings => OnFileAccept(strings), "Open image", Application.dataPath, FileFilter, FileExt);

                void OnFileAccept(string[] strings)
                {
                    if (strings == null || strings.Length == 0) return;
                    if (strings[0].IsNullOrEmpty()) return;

                    TexBytes = File.ReadAllBytes(strings[0]);
                    PropertyToSet = property;
                    MatToSet = materialName;
                    GameObjectToSet = go;
                    ObjectTypeToSet = objectType;
                    CoordinateIndexToSet = coordinateIndex;
                    SlotToSet = slot;
                }
            }
            public void AddMaterialTextureProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string property, TexturePropertyType propertyType, Vector2 value, Vector2 valueOriginal)
            {
                var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == property && x.MaterialName == materialName);
                if (textureProperty == null)
                {
                    if (propertyType == TexturePropertyType.Offset)
                        MaterialTexturePropertyList.Add(new MaterialTextureProperty(objectType, coordinateIndex, slot, materialName, property, offset: value, offsetOriginal: valueOriginal));
                    else if (propertyType == TexturePropertyType.Scale)
                        MaterialTexturePropertyList.Add(new MaterialTextureProperty(objectType, coordinateIndex, slot, materialName, property, scale: value, scaleOriginal: valueOriginal));
                }
                else
                {
                    if (propertyType == TexturePropertyType.Offset)
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
                    else if (propertyType == TexturePropertyType.Scale)
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
            }
            public Vector2? GetMaterialTexturePropertyValue(ObjectType objectType, int coordinateIndex, int slot, string materialName, string property, TexturePropertyType propertyType)
            {
                var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == property && x.MaterialName == materialName);
                if (propertyType == TexturePropertyType.Offset)
                    return textureProperty?.Offset;
                if (propertyType == TexturePropertyType.Scale)
                    return textureProperty?.Scale;
                if (propertyType == TexturePropertyType.Texture) 
                    return textureProperty?.TexID == null ? null : (Vector2?)new Vector2(-1, -1);
                return null;
            }
            public Vector2? GetMaterialTexturePropertyValueOriginal(ObjectType objectType, int coordinateIndex, int slot, string materialName, string property, TexturePropertyType propertyType)
            {
                var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == property && x.MaterialName == materialName);
                if (propertyType == TexturePropertyType.Offset)
                    return textureProperty?.OffsetOriginal;
                if (propertyType == TexturePropertyType.Scale)
                    return textureProperty?.ScaleOriginal;
                if (propertyType == TexturePropertyType.Texture)
                    return textureProperty?.TexID == null ? null : (Vector2?)new Vector2(-1, -1);
                return null;
            }
            public void RemoveMaterialTextureProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string property, TexturePropertyType propertyType)
            {
                var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == property && x.MaterialName == materialName);
                if (textureProperty != null)
                {
                    if (propertyType == TexturePropertyType.Texture)
                    {
                        Logger.LogMessage("Save and reload character or change outfits to refresh textures.");
                        textureProperty.TexID = null;
                        if (textureProperty.NullCheck())
                            MaterialTexturePropertyList.Remove(textureProperty);
                    }
                    else if (propertyType == TexturePropertyType.Offset)
                    {
                        textureProperty.Offset = null;
                        textureProperty.OffsetOriginal = null;
                        if (textureProperty.NullCheck())
                            MaterialTexturePropertyList.Remove(textureProperty);
                    }
                    else if (propertyType == TexturePropertyType.Scale)
                    {
                        textureProperty.Scale = null;
                        textureProperty.ScaleOriginal = null;
                        if (textureProperty.NullCheck())
                            MaterialTexturePropertyList.Remove(textureProperty);
                    }
                }
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
            public void AddMaterialShader(ObjectType objectType, int coordinateIndex, int slot, string materialName, int renderQueue, int renderQueueOriginal)
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
            public MaterialShader GetMaterialShaderValue(ObjectType objectType, int coordinateIndex, int slot, string materialName) =>
                MaterialShaderList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.MaterialName == materialName);
            public void RemoveMaterialShaderName(ObjectType objectType, int coordinateIndex, int slot, string materialName)
            {
                foreach (var materialProperty in MaterialShaderList.Where(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.MaterialName == materialName))
                {
                    materialProperty.ShaderName = null;
                    materialProperty.ShaderNameOriginal = null;
                }

                MaterialShaderList.RemoveAll(x => x.ObjectType == objectType && x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.MaterialName == materialName && x.NullCheck());
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
}
