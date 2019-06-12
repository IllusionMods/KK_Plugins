using ExtensibleSaveFormat;
using KKAPI.Studio.SaveLoad;
using KKAPI.Utilities;
using MessagePack;
using Studio;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KK_MaterialEditor
{
    public partial class KK_MaterialEditor
    {

        public class MaterialEditorSceneController : SceneCustomFunctionController
        {
            private readonly List<RendererProperty> StudioItemRendererPropertyList = new List<RendererProperty>();
            private readonly List<MaterialFloatProperty> StudioItemMaterialFloatPropertyList = new List<MaterialFloatProperty>();
            private readonly List<MaterialColorProperty> StudioItemMaterialColorPropertyList = new List<MaterialColorProperty>();

            protected override void OnSceneSave()
            {
                var data = new PluginData();
                if (data == null)
                    return;

                if (StudioItemRendererPropertyList.Count > 0)
                    data.data.Add("RendererProperties", MessagePackSerializer.Serialize(StudioItemRendererPropertyList));
                else
                    data.data.Add("RendererProperties", null);

                if (StudioItemMaterialFloatPropertyList.Count > 0)
                    data.data.Add("MaterialFloatProperties", MessagePackSerializer.Serialize(StudioItemMaterialFloatPropertyList));
                else
                    data.data.Add("MaterialFloatProperties", null);

                if (StudioItemMaterialColorPropertyList.Count > 0)
                    data.data.Add("MaterialColorProperties", MessagePackSerializer.Serialize(StudioItemMaterialColorPropertyList));
                else
                    data.data.Add("MaterialColorProperties", null);

                SetExtendedData(data);
            }

            protected override void OnSceneLoad(SceneOperationKind operation, ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems)
            {
                var data = GetExtendedData();

                if (operation == SceneOperationKind.Clear || operation == SceneOperationKind.Load)
                {
                    StudioItemRendererPropertyList.Clear();
                    StudioItemMaterialFloatPropertyList.Clear();
                    StudioItemMaterialColorPropertyList.Clear();
                }

                if (data.data.TryGetValue("RendererProperties", out var rendererProperties) && rendererProperties != null)
                {
                    var loadedRendererProperties = MessagePackSerializer.Deserialize<List<RendererProperty>>((byte[])rendererProperties);

                    foreach (var loadedRendererProperty in loadedRendererProperties)
                    {
                        if (loadedItems.TryGetValue(loadedRendererProperty.ID, out ObjectCtrlInfo objectCtrlInfo) && objectCtrlInfo is OCIItem ociItem)
                        {
                            foreach (var renderer in ociItem.objectItem.GetComponentsInChildren<Renderer>())
                            {
                                if (FormatObjectName(renderer) == loadedRendererProperty.RendererName)
                                {
                                    string valueOriginal = "";
                                    if (loadedRendererProperty.Property == RendererProperties.ShadowCastingMode)
                                        valueOriginal = ((int)renderer.shadowCastingMode).ToString();
                                    else if (loadedRendererProperty.Property == RendererProperties.ReceiveShadows)
                                        valueOriginal = renderer.receiveShadows ? "1" : "0";

                                    StudioItemRendererPropertyList.Add(new RendererProperty(GetObjectID(objectCtrlInfo), loadedRendererProperty.RendererName, loadedRendererProperty.Property, loadedRendererProperty.Value, valueOriginal));
                                    SetRendererProperty(renderer, loadedRendererProperty.Property, int.Parse(loadedRendererProperty.Value));
                                }
                            }
                        }
                    }
                }

                if (data.data.TryGetValue("MaterialFloatProperties", out var materialFloatProperties) && materialFloatProperties != null)
                {
                    var loadedMaterialFloatProperties = MessagePackSerializer.Deserialize<List<MaterialFloatProperty>>((byte[])materialFloatProperties);

                    foreach (var loadedMaterialFloatProperty in loadedMaterialFloatProperties)
                    {
                        if (loadedItems.TryGetValue(loadedMaterialFloatProperty.ID, out ObjectCtrlInfo objectCtrlInfo) && objectCtrlInfo is OCIItem ociItem)
                        {
                            foreach (var rend in ociItem.objectItem.GetComponentsInChildren<Renderer>())
                            {
                                foreach (var mat in rend.materials)
                                {
                                    if (mat.HasProperty($"_{loadedMaterialFloatProperty.Property}") && FloatProperties.Contains(loadedMaterialFloatProperty.Property))
                                    {
                                        var valueOriginal = mat.GetFloat($"_{loadedMaterialFloatProperty.Property}").ToString();
                                        StudioItemMaterialFloatPropertyList.Add(new MaterialFloatProperty(GetObjectID(objectCtrlInfo), loadedMaterialFloatProperty.MaterialName, loadedMaterialFloatProperty.Property, loadedMaterialFloatProperty.Value, valueOriginal));
                                        SetFloatProperty(ociItem.objectItem, mat, loadedMaterialFloatProperty.Property, loadedMaterialFloatProperty.Value);
                                    }
                                }
                            }
                        }
                    }
                }

                if (data.data.TryGetValue("MaterialColorProperties", out var materialColorProperties) && materialColorProperties != null)
                {
                    var loadedMaterialColorProperties = MessagePackSerializer.Deserialize<List<MaterialColorProperty>>((byte[])materialColorProperties);

                    foreach (var loadedMaterialColorProperty in loadedMaterialColorProperties)
                    {
                        if (loadedItems.TryGetValue(loadedMaterialColorProperty.ID, out ObjectCtrlInfo objectCtrlInfo) && objectCtrlInfo is OCIItem ociItem)
                        {
                            foreach (var rend in ociItem.objectItem.GetComponentsInChildren<Renderer>())
                            {
                                foreach (var mat in rend.materials)
                                {
                                    if (mat.HasProperty($"_{loadedMaterialColorProperty.Property}") && ColorProperties.Contains(loadedMaterialColorProperty.Property))
                                    {
                                        var valueOriginal = mat.GetColor($"_{loadedMaterialColorProperty.Property}");
                                        StudioItemMaterialColorPropertyList.Add(new MaterialColorProperty(GetObjectID(objectCtrlInfo), loadedMaterialColorProperty.MaterialName, loadedMaterialColorProperty.Property, loadedMaterialColorProperty.Value, valueOriginal));
                                        SetColorProperty(ociItem.objectItem, mat, loadedMaterialColorProperty.Property, loadedMaterialColorProperty.Value);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            public void AddRendererProperty(int id, string rendererName, RendererProperties property, string value, string valueOriginal)
            {
                var rendererProperty = StudioItemRendererPropertyList.FirstOrDefault(x => x.ID == id && x.Property == property);
                if (rendererProperty == null)
                    StudioItemRendererPropertyList.Add(new RendererProperty(id, rendererName, property, value, valueOriginal));
                else
                {
                    if (value == rendererProperty.ValueOriginal)
                        StudioItemRendererPropertyList.Remove(rendererProperty);
                    else
                        rendererProperty.Value = value;
                }
            }

            public void AddMaterialFloatProperty(int id, string materialName, string property, string value, string valueOriginal)
            {
                var materialProperty = StudioItemMaterialFloatPropertyList.FirstOrDefault(x => x.ID == id && x.Property == property);
                if (materialProperty == null)
                    StudioItemMaterialFloatPropertyList.Add(new MaterialFloatProperty(id, materialName, property, value, valueOriginal));
                else
                {
                    if (value == materialProperty.ValueOriginal)
                        StudioItemMaterialFloatPropertyList.Remove(materialProperty);
                    else
                        materialProperty.Value = value;
                }
            }

            public void AddMaterialColorProperty(int id, string materialName, string property, Color value, Color valueOriginal)
            {
                var colorProperty = StudioItemMaterialColorPropertyList.FirstOrDefault(x => x.ID == id && x.Property == property);
                if (colorProperty == null)
                    StudioItemMaterialColorPropertyList.Add(new MaterialColorProperty(id, materialName, property, value, valueOriginal));
                else
                {
                    if (value == colorProperty.ValueOriginal)
                        StudioItemMaterialColorPropertyList.Remove(colorProperty);
                    else
                        colorProperty.Value = value;
                }
            }

            [Serializable]
            [MessagePackObject]
            private class RendererProperty
            {
                [Key("ID")]
                public int ID;
                [Key("RendererName")]
                public string RendererName;
                [Key("Property")]
                public RendererProperties Property;
                [Key("Value")]
                public string Value;
                [Key("ValueOriginal")]
                public string ValueOriginal;

                public RendererProperty(int id, string rendererName, RendererProperties property, string value, string valueOriginal)
                {
                    ID = id;
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
                [Key("ID")]
                public int ID;
                [Key("MaterialName")]
                public string MaterialName;
                [Key("Property")]
                public string Property;
                [Key("Value")]
                public string Value;
                [Key("ValueOriginal")]
                public string ValueOriginal;

                public MaterialFloatProperty(int id, string materialName, string property, string value, string valueOriginal)
                {
                    ID = id;
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
                [Key("ID")]
                public int ID;
                [Key("MaterialName")]
                public string MaterialName;
                [Key("Property")]
                public string Property;
                [Key("Value")]
                public Color Value;
                [Key("ValueOriginal")]
                public Color ValueOriginal;

                public MaterialColorProperty(int id, string materialName, string property, Color value, Color valueOriginal)
                {
                    ID = id;
                    MaterialName = materialName.Replace("(Instance)", "").Trim();
                    Property = property;
                    Value = value;
                    ValueOriginal = valueOriginal;
                }
            }

        }
    }
}
