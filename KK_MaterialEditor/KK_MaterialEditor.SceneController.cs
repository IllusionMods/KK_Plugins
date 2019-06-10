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
            private readonly List<MeshProperty> StudioItemMeshPropertyList = new List<MeshProperty>();
            private readonly List<MaterialProperty> StudioItemMaterialPropertyList = new List<MaterialProperty>();

            protected override void OnSceneSave()
            {
                var data = new PluginData();

                if (StudioItemMeshPropertyList.Count > 0)
                    data.data.Add("MeshProperties", MessagePackSerializer.Serialize(StudioItemMeshPropertyList));
                else
                    data.data.Add("MeshProperties", null);

                if (StudioItemMaterialPropertyList.Count > 0)
                    data.data.Add("MaterialProperties", MessagePackSerializer.Serialize(StudioItemMaterialPropertyList));
                else
                    data.data.Add("MaterialProperties", null);

                SetExtendedData(data);
            }

            protected override void OnSceneLoad(SceneOperationKind operation, ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems)
            {
                var data = GetExtendedData();

                if (operation == SceneOperationKind.Clear || operation == SceneOperationKind.Load)
                {
                    StudioItemMeshPropertyList.Clear();
                    StudioItemMaterialPropertyList.Clear();
                }

                if (data.data.TryGetValue("MeshProperties", out var meshProperties) && meshProperties != null)
                {
                    var loadedMeshProperties = MessagePackSerializer.Deserialize<List<MeshProperty>>((byte[])meshProperties);

                    foreach (var loadedMeshProperty in loadedMeshProperties)
                    {
                        if (loadedItems.TryGetValue(loadedMeshProperty.ID, out ObjectCtrlInfo objectCtrlInfo) && objectCtrlInfo is OCIItem ociItem)
                        {
                            foreach (var mesh in ociItem.objectItem.GetComponentsInChildren<Renderer>())
                            {
                                if (FormatObjectName(mesh) == loadedMeshProperty.MeshName)
                                {
                                    string valueOriginal = "";
                                    if (loadedMeshProperty.Property == RendererProperties.ShadowCastingMode)
                                        valueOriginal = ((int)mesh.shadowCastingMode).ToString();
                                    else if (loadedMeshProperty.Property == RendererProperties.ReceiveShadows)
                                        valueOriginal = mesh.receiveShadows ? "1" : "0";

                                    StudioItemMeshPropertyList.Add(new MeshProperty(GetObjectID(objectCtrlInfo), loadedMeshProperty.MeshName, loadedMeshProperty.Property, loadedMeshProperty.Value, valueOriginal));
                                    SetMeshProperty(mesh, loadedMeshProperty.Property, int.Parse(loadedMeshProperty.Value));
                                }
                            }
                        }
                    }
                }

                if (data.data.TryGetValue("MaterialProperties", out var materialProperties) && materialProperties != null)
                {
                    var loadedMaterialProperties = MessagePackSerializer.Deserialize<List<MaterialProperty>>((byte[])materialProperties);

                    foreach (var loadedMaterialProperty in loadedMaterialProperties)
                    {
                        if (loadedItems.TryGetValue(loadedMaterialProperty.ID, out ObjectCtrlInfo objectCtrlInfo) && objectCtrlInfo is OCIItem ociItem)
                        {
                            foreach (var rend in ociItem.objectItem.GetComponentsInChildren<Renderer>())
                            {
                                foreach (var mat in rend.materials)
                                {
                                    if (mat.HasProperty($"_{loadedMaterialProperty.Property}"))
                                    {
                                        if (ColorProperties.Contains(loadedMaterialProperty.Property))
                                        { }
                                        else if (ImageProperties.Contains(loadedMaterialProperty.Property))
                                        { }
                                        else if (FloatProperties.Contains(loadedMaterialProperty.Property))
                                        {
                                            var valueOriginal = mat.GetFloat($"_{loadedMaterialProperty.Property}").ToString();
                                            StudioItemMaterialPropertyList.Add(new MaterialProperty(GetObjectID(objectCtrlInfo), loadedMaterialProperty.MaterialName, loadedMaterialProperty.Property, loadedMaterialProperty.Value, valueOriginal));
                                            SetFloatProperty(ociItem.objectItem, mat, loadedMaterialProperty.Property, loadedMaterialProperty.Value);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            public void AddMeshProperty(int id, string meshName, RendererProperties property, string value, string valueOriginal)
            {
                var meshProperty = StudioItemMeshPropertyList.FirstOrDefault(x => x.ID == id && x.Property == property);
                if (meshProperty == null)
                    StudioItemMeshPropertyList.Add(new MeshProperty(id, meshName, property, value, valueOriginal));
                else
                {
                    if (value == meshProperty.ValueOriginal)
                        StudioItemMeshPropertyList.Remove(meshProperty);
                    else
                        meshProperty.Value = value;
                }
            }

            public void AddMaterialProperty(int id, string materialName, string property, string value, string valueOriginal)
            {
                var materialProperty = StudioItemMaterialPropertyList.FirstOrDefault(x => x.ID == id && x.Property == property);
                if (materialProperty == null)
                    StudioItemMaterialPropertyList.Add(new MaterialProperty(id, materialName, property, value, valueOriginal));
                else
                {
                    if (value == materialProperty.ValueOriginal)
                        StudioItemMaterialPropertyList.Remove(materialProperty);
                    else
                        materialProperty.Value = value;
                }
            }

            [Serializable]
            [MessagePackObject]
            private class MeshProperty
            {
                [Key("ID")]
                public int ID;
                [Key("MeshName")]
                public string MeshName;
                [Key("Property")]
                public RendererProperties Property;
                [Key("Value")]
                public string Value;
                [Key("ValueOriginal")]
                public string ValueOriginal;

                public MeshProperty(int id, string meshName, RendererProperties property, string value, string valueOriginal)
                {
                    ID = id;
                    MeshName = meshName.Replace("(Instance)", "").Trim();
                    Property = property;
                    Value = value;
                    ValueOriginal = valueOriginal;
                }
            }

            [Serializable]
            [MessagePackObject]
            private class MaterialProperty
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

                public MaterialProperty(int id, string materialName, string property, string value, string valueOriginal)
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
