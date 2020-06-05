using ExtensibleSaveFormat;
using KKAPI.Studio;
using KKAPI.Studio.SaveLoad;
using KKAPI.Utilities;
using MessagePack;
using Studio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static KK_Plugins.MaterialEditor.MaterialAPI;

namespace KK_Plugins.MaterialEditor
{
    public class SceneController : SceneCustomFunctionController
    {
        private readonly List<RendererProperty> RendererPropertyList = new List<RendererProperty>();
        private readonly List<MaterialFloatProperty> MaterialFloatPropertyList = new List<MaterialFloatProperty>();
        private readonly List<MaterialColorProperty> MaterialColorPropertyList = new List<MaterialColorProperty>();
        private readonly List<MaterialTextureProperty> MaterialTexturePropertyList = new List<MaterialTextureProperty>();
        private readonly List<MaterialShader> MaterialShaderList = new List<MaterialShader>();

        private static Dictionary<int, TextureContainer> TextureDictionary = new Dictionary<int, TextureContainer>();

        private static string FileToSet;
        private static string PropertyToSet;
        private static string MatToSet;
        private static int IDToSet = 0;
        private static GameObject GameObjectToSet;

        protected override void OnSceneSave()
        {
            var data = new PluginData();

            List<int> IDsToPurge = new List<int>();
            foreach (int texID in TextureDictionary.Keys)
                if (!MaterialTexturePropertyList.Any(x => x.TexID == texID))
                    IDsToPurge.Add(texID);

            foreach (int texID in IDsToPurge)
                TextureDictionary.Remove(texID);

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

        protected override void OnSceneLoad(SceneOperationKind operation, ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems)
        {
            var data = GetExtendedData();

            if (operation == SceneOperationKind.Clear || operation == SceneOperationKind.Load)
            {
                RendererPropertyList.Clear();
                MaterialFloatPropertyList.Clear();
                MaterialColorPropertyList.Clear();
                MaterialTexturePropertyList.Clear();
                MaterialShaderList.Clear();
                TextureDictionary.Clear();
            }

            if (data == null) return;
            if (operation == SceneOperationKind.Clear) return;

            var importDictionary = new Dictionary<int, int>();

            if (operation == SceneOperationKind.Load)
                if (data.data.TryGetValue(nameof(TextureDictionary), out var texDic) && texDic != null)
                    TextureDictionary = MessagePackSerializer.Deserialize<Dictionary<int, byte[]>>((byte[])texDic).ToDictionary(pair => pair.Key, pair => new TextureContainer(pair.Value));

            if (operation == SceneOperationKind.Import)
                if (data.data.TryGetValue(nameof(TextureDictionary), out var texDic) && texDic != null)
                    foreach (var x in MessagePackSerializer.Deserialize<Dictionary<int, byte[]>>((byte[])texDic))
                        importDictionary[x.Key] = SetAndGetTextureID(x.Value);

            if (data.data.TryGetValue(nameof(MaterialShaderList), out var shaderProperties) && shaderProperties != null)
                foreach (var loadedProperty in MessagePackSerializer.Deserialize<List<MaterialShader>>((byte[])shaderProperties))
                    if (loadedItems.TryGetValue(loadedProperty.ID, out ObjectCtrlInfo objectCtrlInfo) && objectCtrlInfo is OCIItem ociItem)
                    {
                        bool setShader = SetShader(ociItem.objectItem, loadedProperty.MaterialName, loadedProperty.ShaderName);
                        bool setRenderQueue = SetRenderQueue(ociItem.objectItem, loadedProperty.MaterialName, loadedProperty.RenderQueue);
                        if (setShader || setRenderQueue)
                            MaterialShaderList.Add(new MaterialShader(MEStudio.GetObjectID(objectCtrlInfo), loadedProperty.MaterialName, loadedProperty.ShaderName, loadedProperty.ShaderNameOriginal, loadedProperty.RenderQueue, loadedProperty.RenderQueueOriginal));
                    }

            if (data.data.TryGetValue(nameof(RendererPropertyList), out var rendererProperties) && rendererProperties != null)
                foreach (var loadedProperty in MessagePackSerializer.Deserialize<List<RendererProperty>>((byte[])rendererProperties))
                    if (loadedItems.TryGetValue(loadedProperty.ID, out ObjectCtrlInfo objectCtrlInfo) && objectCtrlInfo is OCIItem ociItem)
                        if (SetRendererProperty(ociItem.objectItem, loadedProperty.RendererName, loadedProperty.Property, int.Parse(loadedProperty.Value)))
                            RendererPropertyList.Add(new RendererProperty(MEStudio.GetObjectID(objectCtrlInfo), loadedProperty.RendererName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));

            if (data.data.TryGetValue(nameof(MaterialFloatPropertyList), out var materialFloatProperties) && materialFloatProperties != null)
                foreach (var loadedProperty in MessagePackSerializer.Deserialize<List<MaterialFloatProperty>>((byte[])materialFloatProperties))
                    if (loadedItems.TryGetValue(loadedProperty.ID, out ObjectCtrlInfo objectCtrlInfo) && objectCtrlInfo is OCIItem ociItem)
                        if (SetFloat(ociItem.objectItem, loadedProperty.MaterialName, loadedProperty.Property, float.Parse(loadedProperty.Value)))
                            MaterialFloatPropertyList.Add(new MaterialFloatProperty(MEStudio.GetObjectID(objectCtrlInfo), loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));

            if (data.data.TryGetValue(nameof(MaterialColorPropertyList), out var materialColorProperties) && materialColorProperties != null)
                foreach (var loadedProperty in MessagePackSerializer.Deserialize<List<MaterialColorProperty>>((byte[])materialColorProperties))
                    if (loadedItems.TryGetValue(loadedProperty.ID, out ObjectCtrlInfo objectCtrlInfo) && objectCtrlInfo is OCIItem ociItem)
                        if (SetColor(ociItem.objectItem, loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value))
                            MaterialColorPropertyList.Add(new MaterialColorProperty(MEStudio.GetObjectID(objectCtrlInfo), loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));

            if (data.data.TryGetValue(nameof(MaterialTexturePropertyList), out var materialTextureProperties) && materialTextureProperties != null)
                foreach (var loadedProperty in MessagePackSerializer.Deserialize<List<MaterialTextureProperty>>((byte[])materialTextureProperties))
                    if (loadedItems.TryGetValue(loadedProperty.ID, out ObjectCtrlInfo objectCtrlInfo) && objectCtrlInfo is OCIItem ociItem)
                    {
                        int? texID = null;
                        if (operation == SceneOperationKind.Import)
                        {
                            if (loadedProperty.TexID != null)
                                texID = importDictionary[(int)loadedProperty.TexID];
                        }
                        else
                            texID = loadedProperty.TexID;

                        MaterialTextureProperty newTextureProperty = new MaterialTextureProperty(MEStudio.GetObjectID(objectCtrlInfo), loadedProperty.MaterialName, loadedProperty.Property, texID, loadedProperty.Offset, loadedProperty.OffsetOriginal, loadedProperty.Scale, loadedProperty.ScaleOriginal);

                        bool setTex = false;
                        if (newTextureProperty.TexID != null)
                            setTex = SetTexture(ociItem.objectItem, newTextureProperty.MaterialName, newTextureProperty.Property, newTextureProperty.Texture);

                        bool setOffset = SetTextureOffset(ociItem.objectItem, newTextureProperty.MaterialName, newTextureProperty.Property, newTextureProperty.Offset);
                        bool setScale = SetTextureScale(ociItem.objectItem, newTextureProperty.MaterialName, newTextureProperty.Property, newTextureProperty.Scale);

                        if (setTex || setOffset || setScale)
                            MaterialTexturePropertyList.Add(newTextureProperty);
                    }
        }

        protected override void OnObjectsCopied(ReadOnlyDictionary<int, ObjectCtrlInfo> copiedItems)
        {
            List<RendererProperty> rendererPropertyListNew = new List<RendererProperty>();
            List<MaterialFloatProperty> materialFloatPropertyListNew = new List<MaterialFloatProperty>();
            List<MaterialColorProperty> materialColorPropertyListNew = new List<MaterialColorProperty>();
            List<MaterialTextureProperty> materialTexturePropertyListNew = new List<MaterialTextureProperty>();
            List<MaterialShader> materialShaderListNew = new List<MaterialShader>();

            foreach (var copiedItem in copiedItems)
            {
                if (copiedItem.Value is OCIItem ociItem)
                {
                    foreach (var loadedProperty in MaterialShaderList.Where(x => x.ID == copiedItem.Key))
                    {
                        bool setShader = SetShader(ociItem.objectItem, loadedProperty.MaterialName, loadedProperty.ShaderName);
                        bool setRenderQueue = SetRenderQueue(ociItem.objectItem, loadedProperty.MaterialName, loadedProperty.RenderQueue);
                        if (setShader || setRenderQueue)
                            materialShaderListNew.Add(new MaterialShader(copiedItem.Value.GetSceneId(), loadedProperty.MaterialName, loadedProperty.ShaderName, loadedProperty.ShaderNameOriginal, loadedProperty.RenderQueue, loadedProperty.RenderQueueOriginal));
                    }

                    foreach (var loadedProperty in RendererPropertyList.Where(x => x.ID == copiedItem.Key))
                        if (SetRendererProperty(ociItem.objectItem, loadedProperty.RendererName, loadedProperty.Property, loadedProperty.Value))
                            rendererPropertyListNew.Add(new RendererProperty(copiedItem.Value.GetSceneId(), loadedProperty.RendererName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));

                    foreach (var loadedProperty in MaterialFloatPropertyList.Where(x => x.ID == copiedItem.Key))
                        if (SetFloat(ociItem.objectItem, loadedProperty.MaterialName, loadedProperty.Property, float.Parse(loadedProperty.Value)))
                            materialFloatPropertyListNew.Add(new MaterialFloatProperty(copiedItem.Value.GetSceneId(), loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));

                    foreach (var loadedProperty in MaterialColorPropertyList.Where(x => x.ID == copiedItem.Key))
                        if (SetColor(ociItem.objectItem, loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value))
                            materialColorPropertyListNew.Add(new MaterialColorProperty(copiedItem.Value.GetSceneId(), loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));

                    foreach (var loadedProperty in MaterialTexturePropertyList.Where(x => x.ID == copiedItem.Key))
                    {
                        MaterialTextureProperty newTextureProperty = new MaterialTextureProperty(copiedItem.Value.GetSceneId(), loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.TexID, loadedProperty.Offset, loadedProperty.OffsetOriginal, loadedProperty.Scale, loadedProperty.ScaleOriginal);

                        bool setTex = false;
                        if (loadedProperty.TexID != null)
                            setTex = SetTexture(ociItem.objectItem, newTextureProperty.MaterialName, newTextureProperty.Property, newTextureProperty.Texture);

                        bool setOffset = SetTextureOffset(ociItem.objectItem, newTextureProperty.MaterialName, newTextureProperty.Property, newTextureProperty.Offset);
                        bool setScale = SetTextureScale(ociItem.objectItem, newTextureProperty.MaterialName, newTextureProperty.Property, newTextureProperty.Scale);

                        if (setTex || setOffset || setScale)
                            materialTexturePropertyListNew.Add(newTextureProperty);
                    }
                }
            }

            RendererPropertyList.AddRange(rendererPropertyListNew);
            MaterialFloatPropertyList.AddRange(materialFloatPropertyListNew);
            MaterialColorPropertyList.AddRange(materialColorPropertyListNew);
            MaterialTexturePropertyList.AddRange(materialTexturePropertyListNew);
            MaterialShaderList.AddRange(materialShaderListNew);
        }

        internal void Update()
        {
            try
            {
                if (!FileToSet.IsNullOrEmpty())
                    AddMaterialTextureFromFile(IDToSet, MatToSet, PropertyToSet, FileToSet, GameObjectToSet);
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
        }

        internal void ItemDeleteEvent(int ID)
        {
            RendererPropertyList.RemoveAll(x => x.ID == ID);
            MaterialFloatPropertyList.RemoveAll(x => x.ID == ID);
            MaterialColorPropertyList.RemoveAll(x => x.ID == ID);
            MaterialTexturePropertyList.RemoveAll(x => x.ID == ID);
            MaterialShaderList.RemoveAll(x => x.ID == ID);
        }
        /// <summary>
        /// Finds the texture bytes in the dictionary of textures and returns its ID. If not found, adds the texture to the dictionary and returns the ID of the added texture.
        /// </summary>
        private static int SetAndGetTextureID(byte[] textureBytes)
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

        public void AddRendererProperty(int id, string rendererName, RendererProperties property, string value, string valueOriginal, GameObject gameObject, bool setProperty = true)
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

            if (setProperty)
                SetRendererProperty(gameObject, rendererName, property, value);
        }
        public string GetRendererPropertyValue(int id, string rendererName, RendererProperties property) =>
            RendererPropertyList.FirstOrDefault(x => x.ID == id && x.Property == property && x.RendererName == rendererName)?.Value;
        public string GetRendererPropertyValueOriginal(int id, string rendererName, RendererProperties property) =>
            RendererPropertyList.FirstOrDefault(x => x.ID == id && x.Property == property && x.RendererName == rendererName)?.ValueOriginal;
        public void RemoveRendererProperty(int id, string rendererName, RendererProperties property, GameObject gameObject, bool setProperty = true)
        {
            if (setProperty)
            {
                var original = GetRendererPropertyValueOriginal(id, rendererName, property);
                if (!original.IsNullOrEmpty())
                    SetRendererProperty(gameObject, rendererName, property, original);
            }

            RendererPropertyList.RemoveAll(x => x.ID == id && x.Property == property && x.RendererName == rendererName);
        }
        public void AddMaterialFloatProperty(int id, string materialName, string propertyName, float value, float valueOriginal, GameObject gameObject, bool setProperty = true)
        {
            var materialProperty = MaterialFloatPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == materialName);
            if (materialProperty == null)
                MaterialFloatPropertyList.Add(new MaterialFloatProperty(id, materialName, propertyName, value.ToString(), valueOriginal.ToString()));
            else
            {
                if (value.ToString() == materialProperty.ValueOriginal)
                    MaterialFloatPropertyList.Remove(materialProperty);
                else
                    materialProperty.Value = value.ToString();
            }

            if (setProperty)
                SetFloat(gameObject, materialName, propertyName, value);
        }
        public string GetMaterialFloatPropertyValue(int id, string materialName, string propertyName) =>
            MaterialFloatPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == materialName)?.Value;
        public string GetMaterialFloatPropertyValueOriginal(int id, string materialName, string propertyName) =>
            MaterialFloatPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == materialName)?.ValueOriginal;
        public void RemoveMaterialFloatProperty(int id, string materialName, string propertyName, GameObject gameObject, bool setProperty = true)
        {
            if (setProperty)
            {
                var original = GetMaterialFloatPropertyValueOriginal(id, materialName, propertyName);
                if (!original.IsNullOrEmpty())
                    SetFloat(gameObject, materialName, propertyName, float.Parse(original));
            }

            MaterialFloatPropertyList.RemoveAll(x => x.ID == id && x.Property == propertyName && x.MaterialName == materialName);
        }
        public void AddMaterialColorProperty(int id, string materialName, string propertyName, Color value, Color valueOriginal, GameObject gameObject, bool setProperty = true)
        {
            var colorProperty = MaterialColorPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == materialName);
            if (colorProperty == null)
                MaterialColorPropertyList.Add(new MaterialColorProperty(id, materialName, propertyName, value, valueOriginal));
            else
            {
                if (value == colorProperty.ValueOriginal)
                    MaterialColorPropertyList.Remove(colorProperty);
                else
                    colorProperty.Value = value;
            }

            if (setProperty)
                SetColor(gameObject, materialName, propertyName, value);
        }
        public Color? GetMaterialColorPropertyValue(int id, string materialName, string propertyName) =>
            MaterialColorPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == materialName)?.Value;
        public Color? GetMaterialColorPropertyValueOriginal(int id, string materialName, string propertyName) =>
            MaterialColorPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == materialName)?.ValueOriginal;
        public void RemoveMaterialColorProperty(int id, string materialName, string propertyName, GameObject gameObject, bool setProperty = true)
        {
            if (setProperty)
            {
                var original = GetMaterialColorPropertyValueOriginal(id, materialName, propertyName);
                if (original != null)
                    SetColor(gameObject, materialName, propertyName, (Color)original);
            }

            MaterialColorPropertyList.RemoveAll(x => x.ID == id && x.Property == propertyName && x.MaterialName == materialName);
        }

        public void AddMaterialTextureFromFile(int id, string materialName, string propertyName, string filePath, GameObject gameObject, bool setTexInUpdate = false)
        {
            if (!File.Exists(filePath)) return;

            if (setTexInUpdate)
            {
                FileToSet = filePath;
                PropertyToSet = propertyName;
                MatToSet = materialName;
                GameObjectToSet = gameObject;
                IDToSet = id;
            }
            else
            {
                var texBytes = File.ReadAllBytes(filePath);

                var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == materialName);
                if (textureProperty == null)
                {
                    textureProperty = new MaterialTextureProperty(id, materialName, propertyName, SetAndGetTextureID(texBytes));
                    MaterialTexturePropertyList.Add(textureProperty);
                }
                else
                    textureProperty.Data = texBytes;

                SetTexture(gameObject, materialName, propertyName, textureProperty.Texture);
            }
        }
        public Texture2D GetMaterialTexture(int id, string materialName, string propertyName) =>
            MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.MaterialName == materialName && x.Property == propertyName)?.Texture;
        public bool GetMaterialTextureOriginal(int id, string materialName, string propertyName) =>
            MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.MaterialName == materialName && x.Property == propertyName)?.TexID == null ? true : false;
        public void RemoveMaterialTexture(int id, string materialName, string propertyName, bool displayMessage = true)
        {
            MaterialEditorPlugin.Logger.LogInfo($"RemoveMaterialTexture id:{id} materialName:{materialName} propertyName:{propertyName}");
            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.MaterialName == materialName && x.Property == propertyName);
            if (textureProperty != null)
            {
                if (displayMessage)
                    MaterialEditorPlugin.Logger.LogMessage("Save and reload scene to refresh textures.");
                textureProperty.TexID = null;
                if (textureProperty.NullCheck())
                    MaterialTexturePropertyList.Remove(textureProperty);
            }
        }

        public void AddMaterialTextureOffset(int id, string materialName, string propertyName, Vector2 value, Vector2 valueOriginal, GameObject gameObject, bool setProperty = true)
        {
            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.MaterialName == materialName && x.Property == propertyName);
            if (textureProperty == null)
                MaterialTexturePropertyList.Add(new MaterialTextureProperty(id, materialName, propertyName, offset: value, offsetOriginal: valueOriginal));
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

            if (setProperty)
                SetTextureOffset(gameObject, materialName, propertyName, value);
        }
        public Vector2? GetMaterialTextureOffset(int id, string materialName, string propertyName) =>
            MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.MaterialName == materialName && x.Property == propertyName)?.Offset;
        public Vector2? GetMaterialTextureOffsetOriginal(int id, string materialName, string propertyName) =>
            MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.MaterialName == materialName && x.Property == propertyName)?.OffsetOriginal;
        public void RemoveMaterialTextureOffset(int id, string materialName, string propertyName, GameObject gameObject, bool setProperty = true)
        {
            if (setProperty)
            {
                var original = GetMaterialTextureOffsetOriginal(id, materialName, propertyName);
                if (original != null)
                    SetTextureOffset(gameObject, materialName, propertyName, original);
            }

            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.MaterialName == materialName && x.Property == propertyName);
            if (textureProperty != null)
            {
                textureProperty.Offset = null;
                textureProperty.OffsetOriginal = null;
                if (textureProperty.NullCheck())
                    MaterialTexturePropertyList.Remove(textureProperty);
            }
        }

        public void AddMaterialTextureScale(int id, string materialName, string propertyName, Vector2 value, Vector2 valueOriginal, GameObject gameObject, bool setProperty = true)
        {
            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.MaterialName == materialName && x.Property == propertyName);
            if (textureProperty == null)
                MaterialTexturePropertyList.Add(new MaterialTextureProperty(id, materialName, propertyName, scale: value, scaleOriginal: valueOriginal));
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

            if (setProperty)
                SetTextureScale(gameObject, materialName, propertyName, value);
        }

        public Vector2? GetMaterialTextureScale(int id, string materialName, string propertyName) =>
            MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.MaterialName == materialName && x.Property == propertyName)?.Scale;
        public Vector2? GetMaterialTextureScaleOriginal(int id, string materialName, string propertyName) =>
            MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.MaterialName == materialName && x.Property == propertyName)?.ScaleOriginal;

        public void RemoveMaterialTextureScale(int id, string materialName, string propertyName, GameObject gameObject, bool setProperty = true)
        {
            if (setProperty)
            {
                var original = GetMaterialTextureScaleOriginal(id, materialName, propertyName);
                if (original != null)
                    SetTextureScale(gameObject, materialName, propertyName, original);
            }

            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.MaterialName == materialName && x.Property == propertyName);
            if (textureProperty != null)
            {
                textureProperty.Scale = null;
                textureProperty.ScaleOriginal = null;
                if (textureProperty.NullCheck())
                    MaterialTexturePropertyList.Remove(textureProperty);
            }
        }

        public void AddMaterialShader(int id, string materialName, string shaderName, string shaderNameOriginal, GameObject gameObject, bool setProperty = true)
        {
            var materialProperty = MaterialShaderList.FirstOrDefault(x => x.ID == id && x.MaterialName == materialName);
            if (materialProperty == null)
                MaterialShaderList.Add(new MaterialShader(id, materialName, shaderName, shaderNameOriginal));
            else
            {
                if (shaderName == materialProperty.ShaderNameOriginal)
                {
                    materialProperty.ShaderName = null;
                    materialProperty.ShaderNameOriginal = null;
                    if (materialProperty.NullCheck())
                        MaterialShaderList.Remove(materialProperty);
                }
                else
                {
                    materialProperty.ShaderName = shaderName;
                    materialProperty.ShaderNameOriginal = shaderNameOriginal;
                }
            }

            if (setProperty)
            {
                RemoveMaterialShaderRenderQueue(id, materialName, gameObject, false);
                SetShader(gameObject, materialName, shaderName);
            }
        }
        public string GetMaterialShader(int id, string materialName) =>
            MaterialShaderList.FirstOrDefault(x => x.ID == id && x.MaterialName == materialName)?.ShaderName;
        public string GetMaterialShaderOriginal(int id, string materialName) =>
            MaterialShaderList.FirstOrDefault(x => x.ID == id && x.MaterialName == materialName)?.ShaderNameOriginal;
        public void RemoveMaterialShader(int id, string materialName, GameObject gameObject, bool setProperty = true)
        {
            if (setProperty)
            {
                var original = GetMaterialShaderOriginal(id, materialName);
                if (!original.IsNullOrEmpty())
                    SetShader(gameObject, materialName, original);
            }

            foreach (var materialProperty in MaterialShaderList.Where(x => x.ID == id && x.MaterialName == materialName))
            {
                materialProperty.ShaderName = null;
                materialProperty.ShaderNameOriginal = null;
            }

            MaterialShaderList.RemoveAll(x => x.ID == id && x.MaterialName == materialName && x.NullCheck());
        }

        public void AddMaterialShaderRenderQueue(int id, string materialName, int renderQueue, int renderQueueOriginal, GameObject gameObject, bool setProperty = true)
        {
            var materialProperty = MaterialShaderList.FirstOrDefault(x => x.ID == id && x.MaterialName == materialName);
            if (materialProperty == null)
                MaterialShaderList.Add(new MaterialShader(id, materialName, renderQueue, renderQueueOriginal));
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

            if (setProperty)
                SetRenderQueue(gameObject, materialName, renderQueue);
        }
        public int? GetMaterialShaderRenderQueue(int id, string materialName) =>
            MaterialShaderList.FirstOrDefault(x => x.ID == id && x.MaterialName == materialName)?.RenderQueue;
        public int? GetMaterialShaderRenderQueueOriginal(int id, string materialName) =>
            MaterialShaderList.FirstOrDefault(x => x.ID == id && x.MaterialName == materialName)?.RenderQueueOriginal;
        public void RemoveMaterialShaderRenderQueue(int id, string materialName, GameObject gameObject, bool setProperty = true)
        {
            if (setProperty)
            {
                var original = GetMaterialShaderRenderQueueOriginal(id, materialName);
                if (original != null)
                    SetRenderQueue(gameObject, materialName, original);
            }

            foreach (var materialProperty in MaterialShaderList.Where(x => x.ID == id && x.MaterialName == materialName))
            {
                materialProperty.RenderQueue = null;
                materialProperty.RenderQueueOriginal = null;
            }

            MaterialShaderList.RemoveAll(x => x.ID == id && x.MaterialName == materialName && x.NullCheck());
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
        private class MaterialTextureProperty
        {
            [Key("ID")]
            public int ID;
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
                    TexID = SetAndGetTextureID(value);
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
                            _texture = MaterialEditorPlugin.TextureFromBytes(_data);
                    }
                    return _texture;
                }
            }

            public MaterialTextureProperty(int id, string materialName, string property, int? texID = null, Vector2? offset = null, Vector2? offsetOriginal = null, Vector2? scale = null, Vector2? scaleOriginal = null)
            {
                ID = id;
                MaterialName = materialName.Replace("(Instance)", "").Trim();
                Property = property;
                TexID = texID;
                Offset = offset;
                OffsetOriginal = offsetOriginal;
                Scale = scale;
                ScaleOriginal = scaleOriginal;
                if (texID != null && TextureDictionary.TryGetValue((int)texID, out var tex))
                    Data = tex.Data;
            }

            public void Dispose()
            {
                if (_texture != null)
                {
                    Destroy(_texture);
                    _texture = null;
                }
            }

            public bool IsEmpty() => Data == null;
            public bool NullCheck() => TexID == null && Offset == null && Scale == null;

        }

        [Serializable]
        [MessagePackObject]
        private class MaterialShader
        {
            [Key("ID")]
            public int ID;
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

            public MaterialShader(int id, string materialName, string shaderName, string shaderNameOriginal, int? renderQueue, int? renderQueueOriginal)
            {
                ID = id;
                MaterialName = materialName.Replace("(Instance)", "").Trim();
                ShaderName = shaderName;
                ShaderNameOriginal = shaderNameOriginal;
                RenderQueue = renderQueue;
                RenderQueueOriginal = renderQueueOriginal;
            }
            public MaterialShader(int id, string materialName, string shaderName, string shaderNameOriginal)
            {
                ID = id;
                MaterialName = materialName.Replace("(Instance)", "").Trim();
                ShaderName = shaderName;
                ShaderNameOriginal = shaderNameOriginal;
            }
            public MaterialShader(int id, string materialName, int renderQueue, int renderQueueOriginal)
            {
                ID = id;
                MaterialName = materialName.Replace("(Instance)", "").Trim();
                RenderQueue = renderQueue;
                RenderQueueOriginal = renderQueueOriginal;
            }

            public bool NullCheck() => ShaderName.IsNullOrEmpty() && RenderQueue == null;
        }
    }
}
