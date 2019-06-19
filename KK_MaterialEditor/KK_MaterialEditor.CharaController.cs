using CommonCode;
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

namespace KK_MaterialEditor
{
    public partial class KK_MaterialEditor
    {
        public class MaterialEditorCharaController : CharaCustomFunctionController
        {
            private readonly List<RendererProperty> RendererPropertyList = new List<RendererProperty>();
            private readonly List<MaterialFloatProperty> MaterialFloatPropertyList = new List<MaterialFloatProperty>();
            private readonly List<MaterialColorProperty> MaterialColorPropertyList = new List<MaterialColorProperty>();
            private readonly List<MaterialTextureProperty> MaterialTexturePropertyList = new List<MaterialTextureProperty>();

            private static Dictionary<int, byte[]> TextureDictionary = new Dictionary<int, byte[]>();

            public int CurrentCoordinateIndex => ChaControl.fileStatus.coordinateType;

            private bool CoordinateChanging = false;
            private bool AccessorySelectedSlotChanging = false;
            private bool ClothesChanging = false;
            private bool CharacterLoading = false;
            private bool RefreshingTextures = false;

            private static byte[] TexBytes = null;
            private static ObjectType ObjectTypeToSet;
            private static string PropertyToSet;
            private static string MatToSet;
            private static int CoordinateIndexToSet;
            private static int SlotToSet;
            private static GameObject GameObjectToSet;

            protected override void OnCardBeingSaved(GameMode currentGameMode)
            {
                var data = new PluginData();

                List<int> IDsToPurge = new List<int>();
                foreach (int texID in TextureDictionary.Keys)
                {
                    if (MaterialTexturePropertyList.Any(x => x.TexID == texID))
                        CC.Log("ID found");
                    else
                        IDsToPurge.Add(texID);
                }

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

                SetExtendedData(data);
            }

            protected override void OnReload(GameMode currentGameMode, bool maintainState)
            {
                if (MakerAPI.InsideAndLoaded && !MakerAPI.GetCharacterLoadFlags().Clothes)
                    return;

                RendererPropertyList.Clear();
                MaterialFloatPropertyList.Clear();
                MaterialColorPropertyList.Clear();
                MaterialTexturePropertyList.Clear();
                TextureDictionary.Clear();

                var data = GetExtendedData();

                if (data == null)
                    return;

                CharacterLoading = true;

                if (data.data.TryGetValue(nameof(TextureDictionary), out var texDic) && texDic != null)
                    TextureDictionary = MessagePackSerializer.Deserialize<Dictionary<int, byte[]>>((byte[])texDic);

                if (data.data.TryGetValue(nameof(RendererPropertyList), out var rendererProperties) && rendererProperties != null)
                {
                    var loadedRendererProperties = MessagePackSerializer.Deserialize<List<RendererProperty>>((byte[])rendererProperties);

                    foreach (var loadedRendererProperty in loadedRendererProperties)
                        RendererPropertyList.Add(new RendererProperty(loadedRendererProperty.ObjectType, loadedRendererProperty.CoordinateIndex, loadedRendererProperty.Slot, loadedRendererProperty.RendererName, loadedRendererProperty.Property, loadedRendererProperty.Value, loadedRendererProperty.ValueOriginal));
                }

                if (data.data.TryGetValue(nameof(MaterialFloatPropertyList), out var materialFloatProperties) && materialFloatProperties != null)
                {
                    var loadedMaterialFloatProperties = MessagePackSerializer.Deserialize<List<MaterialFloatProperty>>((byte[])materialFloatProperties);

                    foreach (var loadedMaterialFloatProperty in loadedMaterialFloatProperties)
                        MaterialFloatPropertyList.Add(new MaterialFloatProperty(loadedMaterialFloatProperty.ObjectType, loadedMaterialFloatProperty.CoordinateIndex, loadedMaterialFloatProperty.Slot, loadedMaterialFloatProperty.MaterialName, loadedMaterialFloatProperty.Property, loadedMaterialFloatProperty.Value, loadedMaterialFloatProperty.ValueOriginal));
                }

                if (data.data.TryGetValue(nameof(MaterialColorPropertyList), out var materialColorProperties) && materialColorProperties != null)
                {
                    var loadedColorProperties = MessagePackSerializer.Deserialize<List<MaterialColorProperty>>((byte[])materialColorProperties);

                    foreach (var loadedMaterialColorProperty in loadedColorProperties)
                        MaterialColorPropertyList.Add(new MaterialColorProperty(loadedMaterialColorProperty.ObjectType, loadedMaterialColorProperty.CoordinateIndex, loadedMaterialColorProperty.Slot, loadedMaterialColorProperty.MaterialName, loadedMaterialColorProperty.Property, loadedMaterialColorProperty.Value, loadedMaterialColorProperty.ValueOriginal));
                }

                if (data.data.TryGetValue(nameof(MaterialTexturePropertyList), out var materialTextureProperties) && materialTextureProperties != null)
                {
                    var loadedTextureProperties = MessagePackSerializer.Deserialize<List<MaterialTextureProperty>>((byte[])materialTextureProperties);

                    foreach (var loadedMaterialTextureProperty in loadedTextureProperties)
                        MaterialTexturePropertyList.Add(new MaterialTextureProperty(loadedMaterialTextureProperty.ObjectType, loadedMaterialTextureProperty.CoordinateIndex, loadedMaterialTextureProperty.Slot, loadedMaterialTextureProperty.MaterialName, loadedMaterialTextureProperty.Property, loadedMaterialTextureProperty.TexID));
                }

                ChaControl.StartCoroutine(LoadData(true, true, true));
                ChaControl.StartCoroutine(ResetEvents());
            }

            protected override void Update()
            {
                try
                {
                    if (TexBytes != null)
                    {
                        Texture2D tex = new Texture2D(2, 2);
                        tex.LoadImage(TexBytes);

                        foreach (var obj in GameObjectToSet.GetComponentsInChildren<Renderer>())
                            foreach (var objMat in obj.materials)
                                if (objMat.NameFormatted() == MatToSet)
                                    objMat.SetTexture($"_{PropertyToSet}", tex);

                        var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == ObjectTypeToSet && x.CoordinateIndex == CoordinateIndexToSet && x.Slot == SlotToSet && x.Property == PropertyToSet && x.MaterialName == MatToSet);
                        if (textureProperty == null)
                            MaterialTexturePropertyList.Add(new MaterialTextureProperty(ObjectTypeToSet, CoordinateIndexToSet, SlotToSet, MatToSet, PropertyToSet, GetTextureID(TexBytes)));
                        else
                            textureProperty.Data = TexBytes;
                    }
                }
                catch
                {
                    BepInEx.Logger.Log(BepInEx.Logging.LogLevel.Error | BepInEx.Logging.LogLevel.Message, "Failed to load texture.");
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

            private IEnumerator LoadData(bool clothes, bool accessories, bool hair)
            {
                yield return null;

                foreach (var property in RendererPropertyList)
                {
                    if (property.ObjectType == ObjectType.Clothing && clothes && property.CoordinateIndex == CurrentCoordinateIndex)
                    {
                        foreach (var rend in ChaControl.objClothes[property.Slot].GetComponentsInChildren<Renderer>())
                            if (rend.NameFormatted() == property.RendererName)
                                SetRendererProperty(rend, property.Property, int.Parse(property.Value));
                    }
                    else if (property.ObjectType == ObjectType.Accessory && accessories && property.CoordinateIndex == CurrentCoordinateIndex)
                    {
                        foreach (var rend in AccessoriesApi.GetAccessory(ChaControl, property.Slot)?.gameObject.GetComponentsInChildren<Renderer>())
                            if (rend.name == property.RendererName)
                                SetRendererProperty(rend, property.Property, int.Parse(property.Value));
                    }
                    else if (property.ObjectType == ObjectType.Hair && hair)
                    {
                        foreach (var rend in ChaControl.objHair[property.Slot]?.gameObject.GetComponentsInChildren<Renderer>())
                            if (rend.name == property.RendererName)
                                SetRendererProperty(rend, property.Property, int.Parse(property.Value));
                    }
                }
                foreach (var property in MaterialFloatPropertyList)
                {
                    if (property.ObjectType == ObjectType.Clothing && clothes && property.CoordinateIndex == CurrentCoordinateIndex)
                    {
                        foreach (var rend in ChaControl.objClothes[property.Slot].GetComponentsInChildren<Renderer>())
                            foreach (var mat in rend.materials)
                                if (mat.NameFormatted() == property.MaterialName)
                                    SetFloatProperty(ChaControl.objClothes[property.Slot], mat, property.Property, property.Value);
                    }
                    else if (property.ObjectType == ObjectType.Accessory && accessories && property.CoordinateIndex == CurrentCoordinateIndex)
                    {
                        foreach (var rend in AccessoriesApi.GetAccessory(ChaControl, property.Slot)?.gameObject.GetComponentsInChildren<Renderer>())
                            foreach (var mat in rend.materials)
                                if (mat.NameFormatted() == property.MaterialName)
                                    SetFloatProperty(AccessoriesApi.GetAccessory(ChaControl, property.Slot)?.gameObject, mat, property.Property, property.Value);
                    }
                    else if (property.ObjectType == ObjectType.Hair && hair)
                    {
                        foreach (var rend in ChaControl.objHair[property.Slot]?.gameObject.GetComponentsInChildren<Renderer>())
                            foreach (var mat in rend.materials)
                                if (mat.NameFormatted() == property.MaterialName)
                                    SetFloatProperty(ChaControl.objHair[property.Slot]?.gameObject, mat, property.Property, property.Value);
                    }
                }
                foreach (var property in MaterialColorPropertyList)
                {
                    if (property.ObjectType == ObjectType.Clothing && clothes && property.CoordinateIndex == CurrentCoordinateIndex)
                    {
                        foreach (var rend in ChaControl.objClothes[property.Slot].GetComponentsInChildren<Renderer>())
                            foreach (var mat in rend.materials)
                                if (mat.NameFormatted() == property.MaterialName)
                                    SetColorProperty(ChaControl.objClothes[property.Slot], mat, property.Property, property.Value);
                    }
                    else if (property.ObjectType == ObjectType.Accessory && accessories && property.CoordinateIndex == CurrentCoordinateIndex)
                    {
                        foreach (var rend in AccessoriesApi.GetAccessory(ChaControl, property.Slot)?.gameObject.GetComponentsInChildren<Renderer>())
                            foreach (var mat in rend.materials)
                                if (mat.NameFormatted() == property.MaterialName)
                                    SetColorProperty(AccessoriesApi.GetAccessory(ChaControl, property.Slot)?.gameObject, mat, property.Property, property.Value);
                    }
                    else if (property.ObjectType == ObjectType.Hair && hair)
                    {
                        foreach (var rend in ChaControl.objHair[property.Slot]?.gameObject.GetComponentsInChildren<Renderer>())
                            foreach (var mat in rend.materials)
                                if (mat.NameFormatted() == property.MaterialName)
                                    SetColorProperty(ChaControl.objHair[property.Slot]?.gameObject, mat, property.Property, property.Value);
                    }
                }
                foreach (var property in MaterialTexturePropertyList)
                {
                    if (property.ObjectType == ObjectType.Clothing && clothes && property.CoordinateIndex == CurrentCoordinateIndex)
                    {
                        foreach (var rend in ChaControl.objClothes[property.Slot].GetComponentsInChildren<Renderer>())
                            foreach (var mat in rend.materials)
                                if (mat.NameFormatted() == property.MaterialName)
                                    SetTextureProperty(ChaControl.objClothes[property.Slot], mat, property.Property, property.Texture);
                    }
                    else if (property.ObjectType == ObjectType.Accessory && accessories && property.CoordinateIndex == CurrentCoordinateIndex)
                    {
                        foreach (var rend in AccessoriesApi.GetAccessory(ChaControl, property.Slot)?.gameObject.GetComponentsInChildren<Renderer>())
                            foreach (var mat in rend.materials)
                                if (mat.NameFormatted() == property.MaterialName)
                                    SetTextureProperty(AccessoriesApi.GetAccessory(ChaControl, property.Slot)?.gameObject, mat, property.Property, property.Texture);
                    }
                    else if (property.ObjectType == ObjectType.Hair && hair)
                    {
                        foreach (var rend in ChaControl.objHair[property.Slot]?.gameObject.GetComponentsInChildren<Renderer>())
                            foreach (var mat in rend.materials)
                                if (mat.NameFormatted() == property.MaterialName)
                                    SetTextureProperty(ChaControl.objHair[property.Slot]?.gameObject, mat, property.Property, property.Texture);
                    }
                }
            }

            private static int GetTextureID(byte[] textureBytes)
            {
                int highestID = 0;
                foreach (var tex in TextureDictionary)
                    if (tex.Value.SequenceEqual(textureBytes))
                        return tex.Key;
                    else if (tex.Key > highestID)
                        highestID = tex.Key;

                highestID++;
                TextureDictionary.Add(highestID, textureBytes);
                CC.Log("GetTextureID adding new");
                return highestID;
            }

            internal void ClothesStateChangeEvent()
            {
                if (CoordinateChanging)
                    return;
                if (MakerAPI.InsideMaker)
                    return;

                ChaControl.StartCoroutine(LoadData(true, false, false));
            }

            internal void CoordinateChangeEvent()
            {
                CoordinateChanging = true;

                ChaControl.StartCoroutine(LoadData(true, true, false));
                ChaControl.StartCoroutine(ResetEvents());
            }

            internal void AccessoryKindChangeEvent(object sender, AccessorySlotEventArgs e)
            {
                if (AccessorySelectedSlotChanging)
                    return;
                if (CoordinateChanging)
                    return;

                //User switched accessories, remove all edited properties for this slot
                RendererPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SlotIndex);
                MaterialFloatPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SlotIndex);
                MaterialColorPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SlotIndex);
                MaterialTexturePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SlotIndex);

                if (UISystem.gameObject.activeInHierarchy)
                    PopulateListAccessory();
            }

            internal void AccessorySelectedSlotChangeEvent(object sender, AccessorySlotEventArgs e)
            {
                if (!MakerAPI.InsideAndLoaded)
                    return;

                AccessorySelectedSlotChanging = true;

                if (UISystem.gameObject.activeInHierarchy)
                    PopulateListAccessory();

                ChaControl.StartCoroutine(ResetEvents());
            }

            private IEnumerator ResetEvents()
            {
                yield return null;

                CoordinateChanging = false;
                AccessorySelectedSlotChanging = false;
                ClothesChanging = false;
                CharacterLoading = false;
                RefreshingTextures = false;
            }

            internal void AccessoryTransferredEvent(object sender, AccessoryTransferEventArgs e)
            {
                RendererPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.DestinationSlotIndex);
                MaterialFloatPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.DestinationSlotIndex);
                MaterialColorPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.DestinationSlotIndex);
                MaterialTexturePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.DestinationSlotIndex);
                
                List<RendererProperty> newAccessoryRendererPropertyList = new List<RendererProperty>();
                List<MaterialFloatProperty> newAccessoryMaterialFloatPropertyList = new List<MaterialFloatProperty>();
                List<MaterialColorProperty> newAccessoryMaterialColorPropertyList = new List<MaterialColorProperty>();

                foreach (var property in RendererPropertyList.Where(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SourceSlotIndex))
                    newAccessoryRendererPropertyList.Add(new RendererProperty(property.ObjectType, CurrentCoordinateIndex, e.DestinationSlotIndex, property.RendererName, property.Property, property.Value, property.ValueOriginal));
                foreach (var property in MaterialFloatPropertyList.Where(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SourceSlotIndex))
                    newAccessoryMaterialFloatPropertyList.Add(new MaterialFloatProperty(property.ObjectType, CurrentCoordinateIndex, e.DestinationSlotIndex, property.MaterialName, property.Property, property.Value, property.ValueOriginal));
                foreach (var property in MaterialColorPropertyList.Where(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SourceSlotIndex))
                    newAccessoryMaterialColorPropertyList.Add(new MaterialColorProperty(property.ObjectType, CurrentCoordinateIndex, e.DestinationSlotIndex, property.MaterialName, property.Property, property.Value, property.ValueOriginal));

                RendererPropertyList.AddRange(newAccessoryRendererPropertyList);
                MaterialFloatPropertyList.AddRange(newAccessoryMaterialFloatPropertyList);
                MaterialColorPropertyList.AddRange(newAccessoryMaterialColorPropertyList);

                UISystem.gameObject.SetActive(false);
                ChaControl.StartCoroutine(LoadData(false, true, false));
            }

            internal void AccessoriesCopiedEvent(object sender, AccessoryCopyEventArgs e)
            {
                foreach (int slot in e.CopiedSlotIndexes)
                {
                    RendererPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == (int)e.CopyDestination && x.Slot == slot);
                    MaterialFloatPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == (int)e.CopyDestination && x.Slot == slot);
                    MaterialColorPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == (int)e.CopyDestination && x.Slot == slot);
                    MaterialTexturePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == (int)e.CopyDestination && x.Slot == slot);
                    
                    List<RendererProperty> newAccessoryRendererPropertyList = new List<RendererProperty>();
                    List<MaterialFloatProperty> newAccessoryMaterialFloatPropertyList = new List<MaterialFloatProperty>();
                    List<MaterialColorProperty> newAccessoryMaterialColorPropertyList = new List<MaterialColorProperty>();

                    foreach (var property in RendererPropertyList.Where(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == (int)e.CopySource && x.Slot == slot))
                        newAccessoryRendererPropertyList.Add(new RendererProperty(property.ObjectType, (int)e.CopyDestination, slot, property.RendererName, property.Property, property.Value, property.ValueOriginal));
                    foreach (var property in MaterialFloatPropertyList.Where(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == (int)e.CopySource && x.Slot == slot))
                        newAccessoryMaterialFloatPropertyList.Add(new MaterialFloatProperty(property.ObjectType, (int)e.CopyDestination, slot, property.MaterialName, property.Property, property.Value, property.ValueOriginal));
                    foreach (var property in MaterialColorPropertyList.Where(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == (int)e.CopySource && x.Slot == slot))
                        newAccessoryMaterialColorPropertyList.Add(new MaterialColorProperty(property.ObjectType, (int)e.CopyDestination, slot, property.MaterialName, property.Property, property.Value, property.ValueOriginal));

                    RendererPropertyList.AddRange(newAccessoryRendererPropertyList);
                    MaterialFloatPropertyList.AddRange(newAccessoryMaterialFloatPropertyList);
                    MaterialColorPropertyList.AddRange(newAccessoryMaterialColorPropertyList);
                }

                if ((int)e.CopyDestination == CurrentCoordinateIndex)
                    UISystem.gameObject.SetActive(false);
            }

            internal void ChangeAccessoryEvent(int slot, int type)
            {
                if (type != 120) //type 120 = no category, accessory being removed
                    return;
                if (!MakerAPI.InsideAndLoaded)
                    return;
                if (CoordinateChanging)
                    return;

                RendererPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);
                MaterialFloatPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);
                MaterialColorPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);
                MaterialTexturePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);
                
                UISystem.gameObject.SetActive(false);
            }

            internal void ChangeCustomClothesEvent(int slot)
            {
                if (!MakerAPI.InsideAndLoaded)
                    return;
                if (CoordinateChanging)
                    return;
                if (ClothesChanging)
                    return;
                if (CharacterLoading)
                    return;
                if (RefreshingTextures)
                    return;
                if (new System.Diagnostics.StackTrace().ToString().Contains("KoiClothesOverlayController"))
                {
                    RefreshingTextures = true;
                    ChaControl.StartCoroutine(ResetEvents());
                    return;
                }

                ClothesChanging = true;

                RendererPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);
                MaterialFloatPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);
                MaterialColorPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);
                MaterialTexturePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);
                
                ChaControl.StartCoroutine(ResetEvents());

                UISystem.gameObject.SetActive(false);
            }

            internal void ChangeHairEvent(int slot)
            {
                if (!MakerAPI.InsideAndLoaded)
                    return;
                if (CharacterLoading)
                    return;

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
                    if (strings == null || strings.Length == 0)
                        return;

                    if (strings[0].IsNullOrEmpty())
                        return;

                    TexBytes = File.ReadAllBytes(strings[0]);
                    PropertyToSet = property;
                    MatToSet = materialName;
                    GameObjectToSet = go;
                    ObjectTypeToSet = objectType;
                    CoordinateIndexToSet = coordinateIndex;
                    SlotToSet = slot;
                }
            }
            [Serializable]
            [MessagePackObject]
            private class RendererProperty
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
            private class MaterialFloatProperty
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
            private class MaterialColorProperty
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
                public int TexID;

                [IgnoreMember]
                private byte[] _data;
                [IgnoreMember]
                public byte[] Data
                {
                    get => _data;
                    set
                    {
                        Dispose();
                        _data = value;
                        TexID = GetTextureID(value);
                    }
                }
                [IgnoreMember]
                private Texture2D _texture;
                [IgnoreMember]
                public Texture2D Texture
                {
                    get
                    {
                        if (_texture == null)
                        {
                            if (_data != null)
                                _texture = TextureFromBytes(_data, TextureFormat.ARGB32);
                        }
                        return _texture;
                    }
                }

                public MaterialTextureProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string property, int texID)
                {
                    ObjectType = objectType;
                    CoordinateIndex = coordinateIndex;
                    Slot = slot;
                    MaterialName = materialName.Replace("(Instance)", "").Trim();
                    Property = property;
                    TexID = texID;
                    Data = TextureDictionary[texID];
                }

                public void Dispose()
                {
                    if (_texture != null)
                    {
                        UnityEngine.Object.Destroy(_texture);
                        _texture = null;
                    }
                }

                public bool IsEmpty() => Data == null;
            }

        }
    }
}
