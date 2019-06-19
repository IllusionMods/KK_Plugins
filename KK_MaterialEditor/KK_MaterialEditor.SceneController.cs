using CommonCode;
using ExtensibleSaveFormat;
using KKAPI.Studio.SaveLoad;
using KKAPI.Utilities;
using MessagePack;
using Studio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace KK_MaterialEditor
{
    public partial class KK_MaterialEditor
    {
        public class MaterialEditorSceneController : SceneCustomFunctionController
        {
            private readonly List<RendererProperty> RendererPropertyList = new List<RendererProperty>();
            private readonly List<MaterialFloatProperty> MaterialFloatPropertyList = new List<MaterialFloatProperty>();
            private readonly List<MaterialColorProperty> MaterialColorPropertyList = new List<MaterialColorProperty>();
            private readonly List<MaterialTextureProperty> MaterialTexturePropertyList = new List<MaterialTextureProperty>();

            private static Dictionary<int, byte[]> TextureDictionary = new Dictionary<int, byte[]>();

            private static byte[] TexBytes = null;
            private static string PropertyToSet = "";
            private static string MatToSet;
            private static int IDToSet = 0;
            private static GameObject GameObjectToSet;

            protected override void OnSceneSave()
            {
                var data = new PluginData();
                if (data == null)
                    return;

                List<int> IDsToPurge = new List<int>();
                foreach (int texID in TextureDictionary.Keys)
                    if (MaterialTexturePropertyList.Any(x => x.TexID == texID))
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

                SetExtendedData(data);
            }

            protected override void OnSceneLoad(SceneOperationKind operation, ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems)
            {
                var data = GetExtendedData();

                if (operation == SceneOperationKind.Clear || operation == SceneOperationKind.Load)
                {
                    RendererPropertyList.Clear();
                    MaterialFloatPropertyList.Clear();
                    MaterialColorPropertyList.Clear();
                    MaterialTexturePropertyList.Clear();
                    TextureDictionary.Clear();
                }

                if (data == null)
                    return;

                if (data.data.TryGetValue(nameof(TextureDictionary), out var texDic) && texDic != null)
                    TextureDictionary = MessagePackSerializer.Deserialize<Dictionary<int, byte[]>>((byte[])texDic);

                if (data.data.TryGetValue(nameof(RendererPropertyList), out var rendererProperties) && rendererProperties != null)
                {
                    var loadedRendererProperties = MessagePackSerializer.Deserialize<List<RendererProperty>>((byte[])rendererProperties);

                    foreach (var loadedRendererProperty in loadedRendererProperties)
                    {
                        if (loadedItems.TryGetValue(loadedRendererProperty.ID, out ObjectCtrlInfo objectCtrlInfo) && objectCtrlInfo is OCIItem ociItem)
                        {
                            foreach (var renderer in ociItem.objectItem.GetComponentsInChildren<Renderer>())
                            {
                                if (renderer.NameFormatted() == loadedRendererProperty.RendererName)
                                {
                                    RendererPropertyList.Add(new RendererProperty(GetObjectID(objectCtrlInfo), loadedRendererProperty.RendererName, loadedRendererProperty.Property, loadedRendererProperty.Value, loadedRendererProperty.ValueOriginal));
                                    SetRendererProperty(renderer, loadedRendererProperty.Property, int.Parse(loadedRendererProperty.Value));
                                }
                            }
                        }
                    }
                }

                if (data.data.TryGetValue(nameof(MaterialFloatPropertyList), out var materialFloatProperties) && materialFloatProperties != null)
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
                                    if (mat.NameFormatted() == loadedMaterialFloatProperty.MaterialName && mat.HasProperty($"_{loadedMaterialFloatProperty.Property}") && FloatProperties.Contains(loadedMaterialFloatProperty.Property))
                                    {
                                        var valueOriginal = mat.GetFloat($"_{loadedMaterialFloatProperty.Property}").ToString();
                                        MaterialFloatPropertyList.Add(new MaterialFloatProperty(GetObjectID(objectCtrlInfo), loadedMaterialFloatProperty.MaterialName, loadedMaterialFloatProperty.Property, loadedMaterialFloatProperty.Value, valueOriginal));
                                        SetFloatProperty(ociItem.objectItem, mat, loadedMaterialFloatProperty.Property, loadedMaterialFloatProperty.Value);
                                    }
                                }
                            }
                        }
                    }
                }

                if (data.data.TryGetValue(nameof(MaterialColorPropertyList), out var materialColorProperties) && materialColorProperties != null)
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
                                    if (mat.NameFormatted() == loadedMaterialColorProperty.MaterialName && mat.HasProperty($"_{loadedMaterialColorProperty.Property}") && ColorProperties.Contains(loadedMaterialColorProperty.Property))
                                    {
                                        var valueOriginal = mat.GetColor($"_{loadedMaterialColorProperty.Property}");
                                        MaterialColorPropertyList.Add(new MaterialColorProperty(GetObjectID(objectCtrlInfo), loadedMaterialColorProperty.MaterialName, loadedMaterialColorProperty.Property, loadedMaterialColorProperty.Value, valueOriginal));
                                        SetColorProperty(ociItem.objectItem, mat, loadedMaterialColorProperty.Property, loadedMaterialColorProperty.Value);
                                    }
                                }
                            }
                        }
                    }
                }

                if (data.data.TryGetValue(nameof(MaterialTexturePropertyList), out var materialTextureProperties) && materialTextureProperties != null)
                {
                    var loadedMaterialTextureProperties = MessagePackSerializer.Deserialize<List<MaterialTextureProperty>>((byte[])materialTextureProperties);

                    foreach (var loadedMaterialTextureProperty in loadedMaterialTextureProperties)
                    {
                        if (loadedItems.TryGetValue(loadedMaterialTextureProperty.ID, out ObjectCtrlInfo objectCtrlInfo) && objectCtrlInfo is OCIItem ociItem)
                        {
                            foreach (var rend in ociItem.objectItem.GetComponentsInChildren<Renderer>())
                            {
                                foreach (var mat in rend.materials)
                                {
                                    if (mat.NameFormatted() == loadedMaterialTextureProperty.MaterialName && mat.HasProperty($"_{loadedMaterialTextureProperty.Property}") && TextureProperties.Contains(loadedMaterialTextureProperty.Property))
                                    {
                                        MaterialTexturePropertyList.Add(new MaterialTextureProperty(GetObjectID(objectCtrlInfo), loadedMaterialTextureProperty.MaterialName, loadedMaterialTextureProperty.Property, loadedMaterialTextureProperty.TexID));
                                        SetTextureProperty(ociItem.objectItem, mat, loadedMaterialTextureProperty.Property, loadedMaterialTextureProperty.Texture);
                                    }
                                }
                            }
                        }
                    }
                }
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

                        var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ID == IDToSet && x.Property == PropertyToSet && x.MaterialName == MatToSet);
                        if (textureProperty == null)
                            MaterialTexturePropertyList.Add(new MaterialTextureProperty(IDToSet, MatToSet, PropertyToSet, GetTextureID(TexBytes)));
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
                return highestID;
            }

            public void AddRendererProperty(int id, string rendererName, RendererProperties property, string value, string valueOriginal)
            {
                var rendererProperty = RendererPropertyList.FirstOrDefault(x => x.ID == id && x.Property == property && x.RendererName == rendererName);
                if (rendererProperty == null)
                    RendererPropertyList.Add(new RendererProperty(id, rendererName, property, value, valueOriginal));
                else
                {
                    if (value == rendererProperty.ValueOriginal)
                        RendererPropertyList.Remove(rendererProperty);
                    else
                        rendererProperty.Value = value;
                }
            }
            public string GetRendererPropertyValue(int id, string rendererName, RendererProperties property) =>
                RendererPropertyList.FirstOrDefault(x => x.ID == id && x.Property == property && x.RendererName == rendererName)?.Value;
            public string GetRendererPropertyValueOriginal(int id, string rendererName, RendererProperties property) =>
                RendererPropertyList.FirstOrDefault(x => x.ID == id && x.Property == property && x.RendererName == rendererName)?.ValueOriginal;
            public void RemoveRendererProperty(int id, string rendererName, RendererProperties property) =>
                RendererPropertyList.RemoveAll(x => x.ID == id && x.Property == property && x.RendererName == rendererName);

            public void AddMaterialFloatProperty(int id, string materialName, string property, string value, string valueOriginal)
            {
                var materialProperty = MaterialFloatPropertyList.FirstOrDefault(x => x.ID == id && x.Property == property && x.MaterialName == materialName);
                if (materialProperty == null)
                    MaterialFloatPropertyList.Add(new MaterialFloatProperty(id, materialName, property, value, valueOriginal));
                else
                {
                    if (value == materialProperty.ValueOriginal)
                        MaterialFloatPropertyList.Remove(materialProperty);
                    else
                        materialProperty.Value = value;
                }
            }
            public string GetMaterialFloatPropertyValue(int id, string materialName, string property) =>
                MaterialFloatPropertyList.FirstOrDefault(x => x.ID == id && x.Property == property && x.MaterialName == materialName)?.Value;
            public string GetMaterialFloatPropertyValueOriginal(int id, string materialName, string property) =>
                MaterialFloatPropertyList.FirstOrDefault(x => x.ID == id && x.Property == property && x.MaterialName == materialName)?.ValueOriginal;
            public void RemoveMaterialFloatProperty(int id, string materialName, string property) =>
                MaterialFloatPropertyList.RemoveAll(x => x.ID == id && x.Property == property && x.MaterialName == materialName);

            public void AddMaterialColorProperty(int id, string materialName, string property, Color value, Color valueOriginal)
            {
                var colorProperty = MaterialColorPropertyList.FirstOrDefault(x => x.ID == id && x.Property == property && x.MaterialName == materialName);
                if (colorProperty == null)
                    MaterialColorPropertyList.Add(new MaterialColorProperty(id, materialName, property, value, valueOriginal));
                else
                {
                    if (value == colorProperty.ValueOriginal)
                        MaterialColorPropertyList.Remove(colorProperty);
                    else
                        colorProperty.Value = value;
                }
            }
            public Color GetMaterialColorPropertyValue(int id, string materialName, string property)
            {
                if (MaterialColorPropertyList.Where(x => x.ID == id && x.Property == property && x.MaterialName == materialName).Count() == 0)
                    return new Color(-1, -1, -1, -1);
                return MaterialColorPropertyList.First(x => x.ID == id && x.Property == property && x.MaterialName == materialName).Value;
            }
            public Color GetMaterialColorPropertyValueOriginal(int id, string materialName, string property)
            {
                if (MaterialColorPropertyList.Where(x => x.ID == id && x.Property == property && x.MaterialName == materialName).Count() == 0)
                    return new Color(-1, -1, -1, -1);
                return MaterialColorPropertyList.First(x => x.ID == id && x.Property == property && x.MaterialName == materialName).ValueOriginal;
            }
            public void RemoveMaterialColorProperty(int id, string materialName, string property) =>
                MaterialColorPropertyList.RemoveAll(x => x.ID == id && x.Property == property && x.MaterialName == materialName);

            public void AddMaterialTextureProperty(int id, string materialName, string property, GameObject go)
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
                    IDToSet = id;
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
            [Serializable]
            [MessagePackObject]
            public class MaterialTextureProperty
            {
                [Key("ID")]
                public int ID;
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

                public MaterialTextureProperty(int id, string materialName, string property, int texID)
                {
                    ID = id;
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
