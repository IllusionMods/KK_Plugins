using CommonCode;
using ExtensibleSaveFormat;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using MessagePack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;

namespace KK_MaterialEditor
{
    public partial class KK_MaterialEditor
    {

        public class MaterialEditorCharaController : CharaCustomFunctionController
        {
            private readonly List<RendererProperty> ClothingRendererPropertyList = new List<RendererProperty>();
            private readonly List<MaterialFloatProperty> ClothingMaterialFloatPropertyList = new List<MaterialFloatProperty>();
            private readonly List<MaterialColorProperty> ClothingMaterialColorPropertyList = new List<MaterialColorProperty>();
            private readonly List<RendererProperty> AccessoryRendererPropertyList = new List<RendererProperty>();
            private readonly List<MaterialFloatProperty> AccessoryMaterialFloatPropertyList = new List<MaterialFloatProperty>();
            private readonly List<MaterialColorProperty> AccessoryMaterialColorPropertyList = new List<MaterialColorProperty>();
            private readonly List<RendererProperty> HairRendererPropertyList = new List<RendererProperty>();
            private readonly List<MaterialFloatProperty> HairMaterialFloatPropertyList = new List<MaterialFloatProperty>();
            private readonly List<MaterialColorProperty> HairMaterialColorPropertyList = new List<MaterialColorProperty>();

            private bool CoordinateChanging = false;
            private bool AccessorySelectedSlotChanging = false;
            private bool ClothesChanging = false;
            private bool CharacterLoading = false;
            private bool RefreshingTextures = false;

            protected override void OnCardBeingSaved(GameMode currentGameMode)
            {
                var data = new PluginData();

                //Clothes
                if (ClothingRendererPropertyList.Count > 0)
                    data.data.Add("ClothingRendererProperties", MessagePackSerializer.Serialize(ClothingRendererPropertyList));
                else
                    data.data.Add("ClothingRendererProperties", null);

                if (ClothingMaterialFloatPropertyList.Count > 0)
                    data.data.Add("ClothingMaterialFloatProperties", MessagePackSerializer.Serialize(ClothingMaterialFloatPropertyList));
                else
                    data.data.Add("ClothingMaterialFloatProperties", null);

                if (ClothingMaterialColorPropertyList.Count > 0)
                    data.data.Add("ClothingMaterialColorProperties", MessagePackSerializer.Serialize(ClothingMaterialColorPropertyList));
                else
                    data.data.Add("ClothingMaterialColorProperties", null);

                //Accessories
                if (AccessoryRendererPropertyList.Count > 0)
                    data.data.Add("AccessoryRendererProperties", MessagePackSerializer.Serialize(AccessoryRendererPropertyList));
                else
                    data.data.Add("AccessoryRendererProperties", null);

                if (AccessoryMaterialFloatPropertyList.Count > 0)
                    data.data.Add("AccessoryMaterialFloatProperties", MessagePackSerializer.Serialize(AccessoryMaterialFloatPropertyList));
                else
                    data.data.Add("AccessoryMaterialFloatProperties", null);

                if (AccessoryMaterialColorPropertyList.Count > 0)
                    data.data.Add("AccessoryMaterialColorProperties", MessagePackSerializer.Serialize(AccessoryMaterialColorPropertyList));
                else
                    data.data.Add("AccessoryMaterialColorProperties", null);

                //Hair
                if (HairRendererPropertyList.Count > 0)
                    data.data.Add("HairRendererProperties", MessagePackSerializer.Serialize(HairRendererPropertyList));
                else
                    data.data.Add("HairRendererProperties", null);

                if (HairMaterialFloatPropertyList.Count > 0)
                    data.data.Add("HairMaterialFloatProperties", MessagePackSerializer.Serialize(HairMaterialFloatPropertyList));
                else
                    data.data.Add("HairMaterialFloatProperties", null);

                if (HairMaterialColorPropertyList.Count > 0)
                    data.data.Add("HairMaterialColorProperties", MessagePackSerializer.Serialize(HairMaterialColorPropertyList));
                else
                    data.data.Add("HairMaterialColorProperties", null);



                SetExtendedData(data);
            }

            protected override void OnReload(GameMode currentGameMode, bool maintainState)
            {
                if (MakerAPI.InsideAndLoaded && !MakerAPI.GetCharacterLoadFlags().Clothes)
                    return;

                ClothingRendererPropertyList.Clear();
                ClothingMaterialFloatPropertyList.Clear();
                ClothingMaterialColorPropertyList.Clear();
                AccessoryRendererPropertyList.Clear();
                AccessoryMaterialFloatPropertyList.Clear();
                AccessoryMaterialColorPropertyList.Clear();
                HairRendererPropertyList.Clear();
                HairMaterialFloatPropertyList.Clear();
                HairMaterialColorPropertyList.Clear();

                var data = GetExtendedData();

                if (data == null)
                    return;

                CharacterLoading = true;

                //Clothes
                if (data.data.TryGetValue("ClothingRendererProperties", out var clothingRendererProperties) && clothingRendererProperties != null)
                {
                    var loadedClothingRendererProperties = MessagePackSerializer.Deserialize<List<RendererProperty>>((byte[])clothingRendererProperties);

                    foreach (var loadedClothingRendererProperty in loadedClothingRendererProperties)
                        ClothingRendererPropertyList.Add(new RendererProperty(loadedClothingRendererProperty.CoordinateIndex, loadedClothingRendererProperty.Slot, loadedClothingRendererProperty.RendererName, loadedClothingRendererProperty.Property, loadedClothingRendererProperty.Value, loadedClothingRendererProperty.ValueOriginal));
                }

                if (data.data.TryGetValue("ClothingMaterialFloatProperties", out var clothingMaterialFloatProperties) && clothingMaterialFloatProperties != null)
                {
                    var loadedClothingMaterialFloatProperties = MessagePackSerializer.Deserialize<List<MaterialFloatProperty>>((byte[])clothingMaterialFloatProperties);

                    foreach (var loadedClothingMaterialFloatProperty in loadedClothingMaterialFloatProperties)
                        ClothingMaterialFloatPropertyList.Add(new MaterialFloatProperty(loadedClothingMaterialFloatProperty.CoordinateIndex, loadedClothingMaterialFloatProperty.Slot, loadedClothingMaterialFloatProperty.MaterialName, loadedClothingMaterialFloatProperty.Property, loadedClothingMaterialFloatProperty.Value, loadedClothingMaterialFloatProperty.ValueOriginal));
                }

                if (data.data.TryGetValue("ClothingMaterialColorProperties", out var clothingMaterialColorProperties) && clothingMaterialColorProperties != null)
                {
                    var loadedClothingColorProperties = MessagePackSerializer.Deserialize<List<MaterialColorProperty>>((byte[])clothingMaterialColorProperties);

                    foreach (var loadedClothingMaterialColorProperty in loadedClothingColorProperties)
                        ClothingMaterialColorPropertyList.Add(new MaterialColorProperty(loadedClothingMaterialColorProperty.CoordinateIndex, loadedClothingMaterialColorProperty.Slot, loadedClothingMaterialColorProperty.MaterialName, loadedClothingMaterialColorProperty.Property, loadedClothingMaterialColorProperty.Value, loadedClothingMaterialColorProperty.ValueOriginal));
                }

                //Accessories
                if (data.data.TryGetValue("AccessoryRendererProperties", out var accessoryRendererProperties) && accessoryRendererProperties != null)
                {
                    var loadedAccessoryRendererProperties = MessagePackSerializer.Deserialize<List<RendererProperty>>((byte[])accessoryRendererProperties);

                    foreach (var loadedAccessoryRendererProperty in loadedAccessoryRendererProperties)
                        AccessoryRendererPropertyList.Add(new RendererProperty(loadedAccessoryRendererProperty.CoordinateIndex, loadedAccessoryRendererProperty.Slot, loadedAccessoryRendererProperty.RendererName, loadedAccessoryRendererProperty.Property, loadedAccessoryRendererProperty.Value, loadedAccessoryRendererProperty.ValueOriginal));
                }

                if (data.data.TryGetValue("AccessoryMaterialFloatProperties", out var accessoryMaterialFloatProperties) && accessoryMaterialFloatProperties != null)
                {
                    var loadedAccessoryMaterialFloatProperties = MessagePackSerializer.Deserialize<List<MaterialFloatProperty>>((byte[])accessoryMaterialFloatProperties);

                    foreach (var loadedAccessoryMaterialFloatProperty in loadedAccessoryMaterialFloatProperties)
                        AccessoryMaterialFloatPropertyList.Add(new MaterialFloatProperty(loadedAccessoryMaterialFloatProperty.CoordinateIndex, loadedAccessoryMaterialFloatProperty.Slot, loadedAccessoryMaterialFloatProperty.MaterialName, loadedAccessoryMaterialFloatProperty.Property, loadedAccessoryMaterialFloatProperty.Value, loadedAccessoryMaterialFloatProperty.ValueOriginal));
                }

                if (data.data.TryGetValue("AccessoryColorFloatProperties", out var accessoryMaterialColorProperties) && accessoryMaterialColorProperties != null)
                {
                    var loadedAccessoryColorProperties = MessagePackSerializer.Deserialize<List<MaterialColorProperty>>((byte[])accessoryMaterialColorProperties);

                    foreach (var loadedAccessoryMaterialColorProperty in loadedAccessoryColorProperties)
                        AccessoryMaterialColorPropertyList.Add(new MaterialColorProperty(loadedAccessoryMaterialColorProperty.CoordinateIndex, loadedAccessoryMaterialColorProperty.Slot, loadedAccessoryMaterialColorProperty.MaterialName, loadedAccessoryMaterialColorProperty.Property, loadedAccessoryMaterialColorProperty.Value, loadedAccessoryMaterialColorProperty.ValueOriginal));
                }

                //Hair
                if (data.data.TryGetValue("HairRendererProperties", out var hairRendererProperties) && hairRendererProperties != null)
                {
                    var loadedHairRendererProperties = MessagePackSerializer.Deserialize<List<RendererProperty>>((byte[])hairRendererProperties);

                    foreach (var loadedHairRendererProperty in loadedHairRendererProperties)
                        HairRendererPropertyList.Add(new RendererProperty(loadedHairRendererProperty.CoordinateIndex, loadedHairRendererProperty.Slot, loadedHairRendererProperty.RendererName, loadedHairRendererProperty.Property, loadedHairRendererProperty.Value, loadedHairRendererProperty.ValueOriginal));
                }

                if (data.data.TryGetValue("HairMaterialFloatProperties", out var hairMaterialFloatProperties) && hairMaterialFloatProperties != null)
                {
                    var loadedHairMaterialFloatProperties = MessagePackSerializer.Deserialize<List<MaterialFloatProperty>>((byte[])hairMaterialFloatProperties);

                    foreach (var loadedHairMaterialFloatProperty in loadedHairMaterialFloatProperties)
                        HairMaterialFloatPropertyList.Add(new MaterialFloatProperty(loadedHairMaterialFloatProperty.CoordinateIndex, loadedHairMaterialFloatProperty.Slot, loadedHairMaterialFloatProperty.MaterialName, loadedHairMaterialFloatProperty.Property, loadedHairMaterialFloatProperty.Value, loadedHairMaterialFloatProperty.ValueOriginal));
                }

                if (data.data.TryGetValue("HairMaterialColorProperties", out var hairMaterialColorProperties) && hairMaterialColorProperties != null)
                {
                    var loadedHairColorProperties = MessagePackSerializer.Deserialize<List<MaterialColorProperty>>((byte[])hairMaterialColorProperties);

                    foreach (var loadedHairMaterialColorProperty in loadedHairColorProperties)
                        HairMaterialColorPropertyList.Add(new MaterialColorProperty(loadedHairMaterialColorProperty.CoordinateIndex, loadedHairMaterialColorProperty.Slot, loadedHairMaterialColorProperty.MaterialName, loadedHairMaterialColorProperty.Property, loadedHairMaterialColorProperty.Value, loadedHairMaterialColorProperty.ValueOriginal));
                }

                ChaControl.StartCoroutine(LoadData(true, true, true));
                ChaControl.StartCoroutine(ResetEvents());
            }

            private IEnumerator LoadData(bool clothes, bool accessories, bool hair)
            {
                yield return null;

                if (clothes)
                {
                    foreach (var clothingRendererProperty in ClothingRendererPropertyList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex))
                        foreach (var rend in ChaControl.objClothes[clothingRendererProperty.Slot].GetComponentsInChildren<Renderer>())
                            if (rend.NameFormatted() == clothingRendererProperty.RendererName)
                                SetRendererProperty(rend, clothingRendererProperty.Property, int.Parse(clothingRendererProperty.Value));

                    foreach (var clothingMaterialFloatProperty in ClothingMaterialFloatPropertyList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex))
                        foreach (var rend in ChaControl.objClothes[clothingMaterialFloatProperty.Slot].GetComponentsInChildren<Renderer>())
                            foreach (var mat in rend.materials)
                                if (mat.NameFormatted() == clothingMaterialFloatProperty.MaterialName)
                                    SetFloatProperty(ChaControl.objClothes[clothingMaterialFloatProperty.Slot], mat, clothingMaterialFloatProperty.Property, clothingMaterialFloatProperty.Value);

                    foreach (var clothingMaterialColorProperty in ClothingMaterialColorPropertyList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex))
                        foreach (var rend in ChaControl.objClothes[clothingMaterialColorProperty.Slot].GetComponentsInChildren<Renderer>())
                            foreach (var mat in rend.materials)
                                if (mat.NameFormatted() == clothingMaterialColorProperty.MaterialName)
                                    SetColorProperty(ChaControl.objClothes[clothingMaterialColorProperty.Slot], mat, clothingMaterialColorProperty.Property, clothingMaterialColorProperty.Value);
                }

                if (accessories)
                {
                    foreach (var accessoryRendererProperty in AccessoryRendererPropertyList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex))
                        foreach (var rend in AccessoriesApi.GetAccessory(ChaControl, accessoryRendererProperty.Slot)?.gameObject.GetComponentsInChildren<Renderer>())
                            if (rend.name == accessoryRendererProperty.RendererName)
                                SetRendererProperty(rend, accessoryRendererProperty.Property, int.Parse(accessoryRendererProperty.Value));

                    foreach (var accessoryMaterialFloatProperty in AccessoryMaterialFloatPropertyList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex))
                        foreach (var rend in AccessoriesApi.GetAccessory(ChaControl, accessoryMaterialFloatProperty.Slot)?.gameObject.GetComponentsInChildren<Renderer>())
                            foreach (var mat in rend.materials)
                                if (mat.NameFormatted() == accessoryMaterialFloatProperty.MaterialName)
                                    SetFloatProperty(AccessoriesApi.GetAccessory(ChaControl, accessoryMaterialFloatProperty.Slot)?.gameObject, mat, accessoryMaterialFloatProperty.Property, accessoryMaterialFloatProperty.Value);

                    foreach (var accessoryMaterialColorProperty in AccessoryMaterialColorPropertyList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex))
                        foreach (var rend in AccessoriesApi.GetAccessory(ChaControl, accessoryMaterialColorProperty.Slot)?.gameObject.GetComponentsInChildren<Renderer>())
                            foreach (var mat in rend.materials)
                                if (mat.NameFormatted() == accessoryMaterialColorProperty.MaterialName)
                                    SetColorProperty(AccessoriesApi.GetAccessory(ChaControl, accessoryMaterialColorProperty.Slot)?.gameObject, mat, accessoryMaterialColorProperty.Property, accessoryMaterialColorProperty.Value);
                }

                if (hair)
                {
                    foreach (var hairRendererProperty in HairRendererPropertyList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex))
                        foreach (var rend in ChaControl.objHair[hairRendererProperty.Slot]?.gameObject.GetComponentsInChildren<Renderer>())
                            if (rend.name == hairRendererProperty.RendererName)
                                SetRendererProperty(rend, hairRendererProperty.Property, int.Parse(hairRendererProperty.Value));

                    foreach (var hairMaterialFloatProperty in HairMaterialFloatPropertyList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex))
                        foreach (var rend in ChaControl.objHair[hairMaterialFloatProperty.Slot]?.gameObject.GetComponentsInChildren<Renderer>())
                            foreach (var mat in rend.materials)
                                if (mat.NameFormatted() == hairMaterialFloatProperty.MaterialName)
                                    SetFloatProperty(ChaControl.objHair[hairMaterialFloatProperty.Slot]?.gameObject, mat, hairMaterialFloatProperty.Property, hairMaterialFloatProperty.Value);

                    foreach (var hairMaterialColorProperty in HairMaterialColorPropertyList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex))
                        foreach (var rend in ChaControl.objHair[hairMaterialColorProperty.Slot]?.gameObject.GetComponentsInChildren<Renderer>())
                            foreach (var mat in rend.materials)
                                if (mat.NameFormatted() == hairMaterialColorProperty.MaterialName)
                                    SetColorProperty(ChaControl.objHair[hairMaterialColorProperty.Slot]?.gameObject, mat, hairMaterialColorProperty.Property, hairMaterialColorProperty.Value);
                }
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
                AccessoryRendererPropertyList.RemoveAll(x => x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SlotIndex);
                AccessoryMaterialFloatPropertyList.RemoveAll(x => x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SlotIndex);
                AccessoryMaterialColorPropertyList.RemoveAll(x => x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SlotIndex);

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
                AccessoryRendererPropertyList.RemoveAll(x => x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.DestinationSlotIndex);
                AccessoryMaterialFloatPropertyList.RemoveAll(x => x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.DestinationSlotIndex);
                AccessoryMaterialColorPropertyList.RemoveAll(x => x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.DestinationSlotIndex);

                List<RendererProperty> newAccessoryRendererPropertyList = new List<RendererProperty>();
                List<MaterialFloatProperty> newAccessoryMaterialFloatPropertyList = new List<MaterialFloatProperty>();
                List<MaterialColorProperty> newAccessoryMaterialColorPropertyList = new List<MaterialColorProperty>();

                foreach (var property in AccessoryRendererPropertyList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SourceSlotIndex))
                    newAccessoryRendererPropertyList.Add(new RendererProperty(CurrentCoordinateIndex, e.DestinationSlotIndex, property.RendererName, property.Property, property.Value, property.ValueOriginal));
                foreach (var property in AccessoryMaterialFloatPropertyList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SourceSlotIndex))
                    newAccessoryMaterialFloatPropertyList.Add(new MaterialFloatProperty(CurrentCoordinateIndex, e.DestinationSlotIndex, property.MaterialName, property.Property, property.Value, property.ValueOriginal));
                foreach (var property in AccessoryMaterialColorPropertyList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SourceSlotIndex))
                    newAccessoryMaterialColorPropertyList.Add(new MaterialColorProperty(CurrentCoordinateIndex, e.DestinationSlotIndex, property.MaterialName, property.Property, property.Value, property.ValueOriginal));

                AccessoryRendererPropertyList.AddRange(newAccessoryRendererPropertyList);
                AccessoryMaterialFloatPropertyList.AddRange(newAccessoryMaterialFloatPropertyList);
                AccessoryMaterialColorPropertyList.AddRange(newAccessoryMaterialColorPropertyList);

                UISystem.gameObject.SetActive(false);
                ChaControl.StartCoroutine(LoadData(false, true, false));
            }

            internal void AccessoriesCopiedEvent(object sender, AccessoryCopyEventArgs e)
            {
                foreach (int slot in e.CopiedSlotIndexes)
                {
                    AccessoryRendererPropertyList.RemoveAll(x => x.CoordinateIndex == (int)e.CopyDestination && x.Slot == slot);
                    AccessoryMaterialFloatPropertyList.RemoveAll(x => x.CoordinateIndex == (int)e.CopyDestination && x.Slot == slot);
                    AccessoryMaterialColorPropertyList.RemoveAll(x => x.CoordinateIndex == (int)e.CopyDestination && x.Slot == slot);

                    List<RendererProperty> newAccessoryRendererPropertyList = new List<RendererProperty>();
                    List<MaterialFloatProperty> newAccessoryMaterialFloatPropertyList = new List<MaterialFloatProperty>();
                    List<MaterialColorProperty> newAccessoryMaterialColorPropertyList = new List<MaterialColorProperty>();

                    foreach (var property in AccessoryRendererPropertyList.Where(x => x.CoordinateIndex == (int)e.CopySource && x.Slot == slot))
                        newAccessoryRendererPropertyList.Add(new RendererProperty((int)e.CopyDestination, slot, property.RendererName, property.Property, property.Value, property.ValueOriginal));
                    foreach (var property in AccessoryMaterialFloatPropertyList.Where(x => x.CoordinateIndex == (int)e.CopySource && x.Slot == slot))
                        newAccessoryMaterialFloatPropertyList.Add(new MaterialFloatProperty((int)e.CopyDestination, slot, property.MaterialName, property.Property, property.Value, property.ValueOriginal));
                    foreach (var property in AccessoryMaterialColorPropertyList.Where(x => x.CoordinateIndex == (int)e.CopySource && x.Slot == slot))
                        newAccessoryMaterialColorPropertyList.Add(new MaterialColorProperty((int)e.CopyDestination, slot, property.MaterialName, property.Property, property.Value, property.ValueOriginal));

                    AccessoryRendererPropertyList.AddRange(newAccessoryRendererPropertyList);
                    AccessoryMaterialFloatPropertyList.AddRange(newAccessoryMaterialFloatPropertyList);
                    AccessoryMaterialColorPropertyList.AddRange(newAccessoryMaterialColorPropertyList);
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

                AccessoryRendererPropertyList.RemoveAll(x => x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);
                AccessoryMaterialFloatPropertyList.RemoveAll(x => x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);
                AccessoryMaterialColorPropertyList.RemoveAll(x => x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);

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

                ClothingRendererPropertyList.RemoveAll(x => x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);
                ClothingMaterialFloatPropertyList.RemoveAll(x => x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);
                ClothingMaterialColorPropertyList.RemoveAll(x => x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);

                ChaControl.StartCoroutine(ResetEvents());

                UISystem.gameObject.SetActive(false);
            }

            internal void ChangeHairEvent(int slot)
            {
                if (!MakerAPI.InsideAndLoaded)
                    return;
                if (CharacterLoading)
                    return;

                HairRendererPropertyList.RemoveAll(x => x.Slot == slot);
                HairMaterialFloatPropertyList.RemoveAll(x => x.Slot == slot);
                HairMaterialColorPropertyList.RemoveAll(x => x.Slot == slot);

                UISystem.gameObject.SetActive(false);
            }

            public void AddClothingRendererProperty(int coordinateIndex, int slot, string rendererName, RendererProperties property, string value, string valueOriginal)
            {
                var rendererProperty = ClothingRendererPropertyList.FirstOrDefault(x => x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == property && x.RendererName == rendererName);
                if (rendererProperty == null)
                    ClothingRendererPropertyList.Add(new RendererProperty(coordinateIndex, slot, rendererName, property, value, valueOriginal));
                else
                {
                    if (value == rendererProperty.ValueOriginal)
                        ClothingRendererPropertyList.Remove(rendererProperty);
                    else
                        rendererProperty.Value = value;
                }
            }

            public void AddClothingMaterialFloatProperty(int coordinateIndex, int slot, string materialName, string property, string value, string valueOriginal)
            {
                var materialProperty = ClothingMaterialFloatPropertyList.FirstOrDefault(x => x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == property && x.MaterialName == materialName);
                if (materialProperty == null)
                    ClothingMaterialFloatPropertyList.Add(new MaterialFloatProperty(coordinateIndex, slot, materialName, property, value, valueOriginal));
                else
                {
                    if (value == materialProperty.ValueOriginal)
                        ClothingMaterialFloatPropertyList.Remove(materialProperty);
                    else
                        materialProperty.Value = value;
                }
            }

            public void AddClothingMaterialColorProperty(int coordinateIndex, int slot, string materialName, string property, Color value, Color valueOriginal)
            {
                var colorProperty = ClothingMaterialColorPropertyList.FirstOrDefault(x => x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == property && x.MaterialName == materialName);
                if (colorProperty == null)
                    ClothingMaterialColorPropertyList.Add(new MaterialColorProperty(coordinateIndex, slot, materialName, property, value, valueOriginal));
                else
                {
                    if (value == colorProperty.ValueOriginal)
                        ClothingMaterialColorPropertyList.Remove(colorProperty);
                    else
                        colorProperty.Value = value;
                }
            }

            public void AddAccessoryRendererProperty(int coordinateIndex, int slot, string rendererName, RendererProperties property, string value, string valueOriginal)
            {
                var rendererProperty = AccessoryRendererPropertyList.FirstOrDefault(x => x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == property && x.RendererName == rendererName);
                if (rendererProperty == null)
                    AccessoryRendererPropertyList.Add(new RendererProperty(coordinateIndex, slot, rendererName, property, value, valueOriginal));
                else
                {
                    if (value == rendererProperty.ValueOriginal)
                        AccessoryRendererPropertyList.Remove(rendererProperty);
                    else
                        rendererProperty.Value = value;
                }
            }

            public void AddAccessoryMaterialFloatProperty(int coordinateIndex, int slot, string materialName, string property, string value, string valueOriginal)
            {
                var materialProperty = AccessoryMaterialFloatPropertyList.FirstOrDefault(x => x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == property && x.MaterialName == materialName);
                if (materialProperty == null)
                    AccessoryMaterialFloatPropertyList.Add(new MaterialFloatProperty(coordinateIndex, slot, materialName, property, value, valueOriginal));
                else
                {
                    if (value == materialProperty.ValueOriginal)
                        AccessoryMaterialFloatPropertyList.Remove(materialProperty);
                    else
                        materialProperty.Value = value;
                }
            }

            public void AddAccessoryMaterialColorProperty(int coordinateIndex, int slot, string materialName, string property, Color value, Color valueOriginal)
            {
                var colorProperty = AccessoryMaterialColorPropertyList.FirstOrDefault(x => x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == property && x.MaterialName == materialName);
                if (colorProperty == null)
                    AccessoryMaterialColorPropertyList.Add(new MaterialColorProperty(coordinateIndex, slot, materialName, property, value, valueOriginal));
                else
                {
                    if (value == colorProperty.ValueOriginal)
                        AccessoryMaterialColorPropertyList.Remove(colorProperty);
                    else
                        colorProperty.Value = value;
                }
            }

            public void AddHairRendererProperty(int slot, string rendererName, RendererProperties property, string value, string valueOriginal)
            {
                var rendererProperty = HairRendererPropertyList.FirstOrDefault(x => x.Slot == slot && x.Property == property && x.RendererName == rendererName);
                if (rendererProperty == null)
                    HairRendererPropertyList.Add(new RendererProperty(0, slot, rendererName, property, value, valueOriginal));
                else
                {
                    if (value == rendererProperty.ValueOriginal)
                        HairRendererPropertyList.Remove(rendererProperty);
                    else
                        rendererProperty.Value = value;
                }
            }

            public void AddHairMaterialFloatProperty(int slot, string materialName, string property, string value, string valueOriginal)
            {
                var materialProperty = HairMaterialFloatPropertyList.FirstOrDefault(x => x.Slot == slot && x.Property == property && x.MaterialName == materialName);
                if (materialProperty == null)
                    HairMaterialFloatPropertyList.Add(new MaterialFloatProperty(0, slot, materialName, property, value, valueOriginal));
                else
                {
                    if (value == materialProperty.ValueOriginal)
                        HairMaterialFloatPropertyList.Remove(materialProperty);
                    else
                        materialProperty.Value = value;
                }
            }

            public void AddHairMaterialColorProperty(int slot, string materialName, string property, Color value, Color valueOriginal)
            {
                var colorProperty = HairMaterialColorPropertyList.FirstOrDefault(x => x.Slot == slot && x.Property == property && x.MaterialName == materialName);
                if (colorProperty == null)
                    HairMaterialColorPropertyList.Add(new MaterialColorProperty(0, slot, materialName, property, value, valueOriginal));
                else
                {
                    if (value == colorProperty.ValueOriginal)
                        HairMaterialColorPropertyList.Remove(colorProperty);
                    else
                        colorProperty.Value = value;
                }
            }

            public int CurrentCoordinateIndex => ChaControl.fileStatus.coordinateType;

            [Serializable]
            [MessagePackObject]
            private class RendererProperty
            {
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

                public RendererProperty(int coordinateIndex, int slot, string rendererName, RendererProperties property, string value, string valueOriginal)
                {
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

                public MaterialFloatProperty(int coordinateIndex, int slot, string materialName, string property, string value, string valueOriginal)
                {
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

                public MaterialColorProperty(int coordinateIndex, int slot, string materialName, string property, Color value, Color valueOriginal)
                {
                    CoordinateIndex = coordinateIndex;
                    Slot = slot;
                    MaterialName = materialName.Replace("(Instance)", "").Trim();
                    Property = property;
                    Value = value;
                    ValueOriginal = valueOriginal;
                }
            }
        }
    }
}
