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

            protected override void OnCardBeingSaved(GameMode currentGameMode)
            {
                var data = new PluginData();

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

                SetExtendedData(data);

            }

            protected override void OnReload(GameMode currentGameMode, bool maintainState)
            {
                if (MakerAPI.InsideAndLoaded && !MakerAPI.GetCharacterLoadFlags().Clothes)
                    return;

                ClothingRendererPropertyList.Clear();
                ClothingMaterialFloatPropertyList.Clear();
                ClothingMaterialColorPropertyList.Clear();

                var data = GetExtendedData();

                if (data == null)
                    return;

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

                ChaControl.StartCoroutine(LoadData());
            }
            private IEnumerator LoadData()
            {
                yield return null;

                foreach (var clothingRendererProperty in ClothingRendererPropertyList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex))
                {
                    foreach (var rend in ChaControl.objClothes[clothingRendererProperty.Slot].GetComponentsInChildren<Renderer>())
                    {
                        if (rend.name == clothingRendererProperty.RendererName)
                        {
                            SetRendererProperty(rend, clothingRendererProperty.Property, int.Parse(clothingRendererProperty.Value));
                        }
                    }
                }

                foreach (var clothingMaterialFloatProperty in ClothingMaterialFloatPropertyList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex))
                {
                    foreach (var rend in ChaControl.objClothes[clothingMaterialFloatProperty.Slot].GetComponentsInChildren<Renderer>())
                    {
                        foreach (var mat in rend.materials)
                        {
                            if (mat.name == clothingMaterialFloatProperty.MaterialName)
                            {
                                SetFloatProperty(ChaControl.objClothes[clothingMaterialFloatProperty.Slot], mat, clothingMaterialFloatProperty.Property, clothingMaterialFloatProperty.Value);
                            }
                        }
                    }
                }

                foreach (var clothingMaterialColorProperty in ClothingMaterialColorPropertyList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex))
                {
                    foreach (var rend in ChaControl.objClothes[clothingMaterialColorProperty.Slot].GetComponentsInChildren<Renderer>())
                    {
                        foreach (var mat in rend.materials)
                        {
                            if (mat.name == clothingMaterialColorProperty.MaterialName)
                            {
                                SetColorProperty(ChaControl.objClothes[clothingMaterialColorProperty.Slot], mat, clothingMaterialColorProperty.Property, clothingMaterialColorProperty.Value);
                            }
                        }
                    }
                }
            }

            public void AddRendererProperty(int coordinateIndex, int slot, string rendererName, RendererProperties property, string value, string valueOriginal)
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

            public void AddMaterialFloatProperty(int coordinateIndex, int slot, string materialName, string property, string value, string valueOriginal)
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
