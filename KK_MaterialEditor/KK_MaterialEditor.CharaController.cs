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

                var data = GetExtendedData();

                if (data == null)
                    return;

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

                if (data.data.TryGetValue("ClothingMaterialFloatProperties", out var clothingMaterialColorProperties) && clothingMaterialColorProperties != null)
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

                if (data.data.TryGetValue("AccessoryMaterialFloatProperties", out var accessoryMaterialColorProperties) && accessoryMaterialColorProperties != null)
                {
                    var loadedAccessoryColorProperties = MessagePackSerializer.Deserialize<List<MaterialColorProperty>>((byte[])accessoryMaterialColorProperties);

                    foreach (var loadedAccessoryMaterialColorProperty in loadedAccessoryColorProperties)
                        AccessoryMaterialColorPropertyList.Add(new MaterialColorProperty(loadedAccessoryMaterialColorProperty.CoordinateIndex, loadedAccessoryMaterialColorProperty.Slot, loadedAccessoryMaterialColorProperty.MaterialName, loadedAccessoryMaterialColorProperty.Property, loadedAccessoryMaterialColorProperty.Value, loadedAccessoryMaterialColorProperty.ValueOriginal));
                }

                ChaControl.StartCoroutine(LoadData());
            }
            private IEnumerator LoadData()
            {
                yield return null;

                //Clothes
                foreach (var clothingRendererProperty in ClothingRendererPropertyList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex))
                    foreach (var rend in ChaControl.objClothes[clothingRendererProperty.Slot].GetComponentsInChildren<Renderer>())
                        if (rend.name == clothingRendererProperty.RendererName)
                            SetRendererProperty(rend, clothingRendererProperty.Property, int.Parse(clothingRendererProperty.Value));

                foreach (var clothingMaterialFloatProperty in ClothingMaterialFloatPropertyList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex))
                    foreach (var rend in ChaControl.objClothes[clothingMaterialFloatProperty.Slot].GetComponentsInChildren<Renderer>())
                        foreach (var mat in rend.materials)
                            if (mat.name == clothingMaterialFloatProperty.MaterialName)
                                SetFloatProperty(ChaControl.objClothes[clothingMaterialFloatProperty.Slot], mat, clothingMaterialFloatProperty.Property, clothingMaterialFloatProperty.Value);

                foreach (var clothingMaterialColorProperty in ClothingMaterialColorPropertyList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex))
                    foreach (var rend in ChaControl.objClothes[clothingMaterialColorProperty.Slot].GetComponentsInChildren<Renderer>())
                        foreach (var mat in rend.materials)
                            if (mat.name == clothingMaterialColorProperty.MaterialName)
                                SetColorProperty(ChaControl.objClothes[clothingMaterialColorProperty.Slot], mat, clothingMaterialColorProperty.Property, clothingMaterialColorProperty.Value);

                //Accessories
                foreach (var AccessoryRendererProperty in AccessoryRendererPropertyList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex))
                    foreach (var rend in AccessoriesApi.GetAccessory(ChaControl, AccessoryRendererProperty.Slot)?.gameObject.GetComponentsInChildren<Renderer>())
                        if (rend.name == AccessoryRendererProperty.RendererName)
                            SetRendererProperty(rend, AccessoryRendererProperty.Property, int.Parse(AccessoryRendererProperty.Value));

                foreach (var AccessoryMaterialFloatProperty in AccessoryMaterialFloatPropertyList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex))
                    foreach (var rend in AccessoriesApi.GetAccessory(ChaControl, AccessoryMaterialFloatProperty.Slot)?.gameObject.GetComponentsInChildren<Renderer>())
                        foreach (var mat in rend.materials)
                            if (mat.name == AccessoryMaterialFloatProperty.MaterialName)
                                SetFloatProperty(AccessoriesApi.GetAccessory(ChaControl, AccessoryMaterialFloatProperty.Slot)?.gameObject, mat, AccessoryMaterialFloatProperty.Property, AccessoryMaterialFloatProperty.Value);

                foreach (var AccessoryMaterialColorProperty in AccessoryMaterialColorPropertyList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex))
                    foreach (var rend in AccessoriesApi.GetAccessory(ChaControl, AccessoryMaterialColorProperty.Slot)?.gameObject.GetComponentsInChildren<Renderer>())
                        foreach (var mat in rend.materials)
                            if (mat.name == AccessoryMaterialColorProperty.MaterialName)
                                SetColorProperty(AccessoriesApi.GetAccessory(ChaControl, AccessoryMaterialColorProperty.Slot)?.gameObject, mat, AccessoryMaterialColorProperty.Property, AccessoryMaterialColorProperty.Value);

            }

            public void AddClothingRendererProperty(int coordinateIndex, int slot, string rendererName, RendererProperties property, string value, string valueOriginal)
            {
                var rendererProperty = ClothingRendererPropertyList.FirstOrDefault(x => x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == property);
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
                var materialProperty = ClothingMaterialFloatPropertyList.FirstOrDefault(x => x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == property);
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
                var colorProperty = ClothingMaterialColorPropertyList.FirstOrDefault(x => x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == property);
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
                var rendererProperty = AccessoryRendererPropertyList.FirstOrDefault(x => x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == property);
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
                var materialProperty = AccessoryMaterialFloatPropertyList.FirstOrDefault(x => x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == property);
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
                var colorProperty = AccessoryMaterialColorPropertyList.FirstOrDefault(x => x.CoordinateIndex == coordinateIndex && x.Slot == slot && x.Property == property);
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
