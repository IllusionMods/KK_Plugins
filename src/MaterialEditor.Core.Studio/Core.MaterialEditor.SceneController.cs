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
        private static Material MatToSet;
        private static int IDToSet = 0;

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
                    AddMaterialTextureFromFile(IDToSet, MatToSet, PropertyToSet, FileToSet);
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

        public void AddRendererProperty(int id, Renderer renderer, RendererProperties property, string value, bool setProperty = true)
        {
            GameObject gameObject = GetObjectByID(id);
            var rendererProperty = RendererPropertyList.FirstOrDefault(x => x.ID == id && x.Property == property && x.RendererName == renderer.NameFormatted());
            if (rendererProperty == null)
            {
                string valueOriginal;
                if (property == RendererProperties.Enabled)
                    valueOriginal = renderer.enabled ? "1" : "0";
                else if (property == RendererProperties.ReceiveShadows)
                    valueOriginal = renderer.receiveShadows ? "1" : "0";
                else
                    valueOriginal = ((int)renderer.shadowCastingMode).ToString();

                RendererPropertyList.Add(new RendererProperty(id, renderer.NameFormatted(), property, value, valueOriginal));
            }
            else
            {
                if (value == rendererProperty.ValueOriginal)
                    RendererPropertyList.Remove(rendererProperty);
                else
                    rendererProperty.Value = value;
            }

            if (setProperty)
                SetRendererProperty(gameObject, renderer.NameFormatted(), property, value);
        }
        public string GetRendererPropertyValue(int id, Renderer renderer, RendererProperties property) =>
            RendererPropertyList.FirstOrDefault(x => x.ID == id && x.Property == property && x.RendererName == renderer.NameFormatted())?.Value;
        public string GetRendererPropertyValueOriginal(int id, Renderer renderer, RendererProperties property) =>
            RendererPropertyList.FirstOrDefault(x => x.ID == id && x.Property == property && x.RendererName == renderer.NameFormatted())?.ValueOriginal;
        public void RemoveRendererProperty(int id, Renderer renderer, RendererProperties property, bool setProperty = true)
        {
            GameObject gameObject = GetObjectByID(id);
            if (setProperty)
            {
                var original = GetRendererPropertyValueOriginal(id, renderer, property);
                if (!original.IsNullOrEmpty())
                    SetRendererProperty(gameObject, renderer.NameFormatted(), property, original);
            }

            RendererPropertyList.RemoveAll(x => x.ID == id && x.Property == property && x.RendererName == renderer.NameFormatted());
        }

        public void AddMaterialFloatProperty(int id, Material material, string propertyName, float value, bool setProperty = true)
        {
            GameObject gameObject = GetObjectByID(id);
            var materialProperty = MaterialFloatPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted());
            if (materialProperty == null)
            {
                float valueOriginal = material.GetFloat($"_{propertyName}");
                MaterialFloatPropertyList.Add(new MaterialFloatProperty(id, material.NameFormatted(), propertyName, value.ToString(), valueOriginal.ToString()));
            }
            else
            {
                if (value.ToString() == materialProperty.ValueOriginal)
                    MaterialFloatPropertyList.Remove(materialProperty);
                else
                    materialProperty.Value = value.ToString();
            }

            if (setProperty)
                SetFloat(gameObject, material.NameFormatted(), propertyName, value);
        }
        public string GetMaterialFloatPropertyValue(int id, Material material, string propertyName) =>
            MaterialFloatPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.Value;
        public string GetMaterialFloatPropertyValueOriginal(int id, Material material, string propertyName) =>
            MaterialFloatPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.ValueOriginal;
        public void RemoveMaterialFloatProperty(int id, Material material, string propertyName, bool setProperty = true)
        {
            GameObject gameObject = GetObjectByID(id);
            if (setProperty)
            {
                var original = GetMaterialFloatPropertyValueOriginal(id, material, propertyName);
                if (!original.IsNullOrEmpty())
                    SetFloat(gameObject, material.NameFormatted(), propertyName, float.Parse(original));
            }

            MaterialFloatPropertyList.RemoveAll(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted());
        }
        public void AddMaterialColorProperty(int id, Material material, string propertyName, Color value, bool setProperty = true)
        {
            GameObject gameObject = GetObjectByID(id);
            var colorProperty = MaterialColorPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted());
            if (colorProperty == null)
            {
                Color valueOriginal = material.GetColor($"_{propertyName}");
                MaterialColorPropertyList.Add(new MaterialColorProperty(id, material.NameFormatted(), propertyName, value, valueOriginal));
            }
            else
            {
                if (value == colorProperty.ValueOriginal)
                    MaterialColorPropertyList.Remove(colorProperty);
                else
                    colorProperty.Value = value;
            }

            if (setProperty)
                SetColor(gameObject, material.NameFormatted(), propertyName, value);
        }
        public Color? GetMaterialColorPropertyValue(int id, Material material, string propertyName) =>
            MaterialColorPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.Value;
        public Color? GetMaterialColorPropertyValueOriginal(int id, Material material, string propertyName) =>
            MaterialColorPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.ValueOriginal;
        public void RemoveMaterialColorProperty(int id, Material material, string propertyName, bool setProperty = true)
        {
            GameObject gameObject = GetObjectByID(id);
            if (setProperty)
            {
                var original = GetMaterialColorPropertyValueOriginal(id, material, propertyName);
                if (original != null)
                    SetColor(gameObject, material.NameFormatted(), propertyName, (Color)original);
            }

            MaterialColorPropertyList.RemoveAll(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted());
        }

        public void AddMaterialTextureFromFile(int id, Material material, string propertyName, string filePath, bool setTexInUpdate = false)
        {
            GameObject gameObject = GetObjectByID(id);
            if (!File.Exists(filePath)) return;

            if (setTexInUpdate)
            {
                FileToSet = filePath;
                PropertyToSet = propertyName;
                MatToSet = material;
                IDToSet = id;
            }
            else
            {
                var texBytes = File.ReadAllBytes(filePath);

                var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted());
                if (textureProperty == null)
                {
                    textureProperty = new MaterialTextureProperty(id, material.NameFormatted(), propertyName, SetAndGetTextureID(texBytes));
                    MaterialTexturePropertyList.Add(textureProperty);
                }
                else
                    textureProperty.Data = texBytes;

                SetTexture(gameObject, material.NameFormatted(), propertyName, textureProperty.Texture);
            }
        }
        public Texture2D GetMaterialTexture(int id, Material material, string propertyName) =>
            MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.MaterialName == material.NameFormatted() && x.Property == propertyName)?.Texture;
        public bool GetMaterialTextureOriginal(int id, Material material, string propertyName) =>
            MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.MaterialName == material.NameFormatted() && x.Property == propertyName)?.TexID == null ? true : false;
        public void RemoveMaterialTexture(int id, Material material, string propertyName, bool displayMessage = true)
        {
            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.MaterialName == material.NameFormatted() && x.Property == propertyName);
            if (textureProperty != null)
            {
                if (displayMessage)
                    MaterialEditorPlugin.Logger.LogMessage("Save and reload scene to refresh textures.");
                textureProperty.TexID = null;
                if (textureProperty.NullCheck())
                    MaterialTexturePropertyList.Remove(textureProperty);
            }
        }

        public void AddMaterialTextureOffset(int id, Material material, string propertyName, Vector2 value, bool setProperty = true)
        {
            GameObject gameObject = GetObjectByID(id);
            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.MaterialName == material.NameFormatted() && x.Property == propertyName);
            if (textureProperty == null)
            {
                Vector2 valueOriginal = material.GetTextureOffset($"_{propertyName}");
                MaterialTexturePropertyList.Add(new MaterialTextureProperty(id, material.NameFormatted(), propertyName, offset: value, offsetOriginal: valueOriginal));
            }
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
                    if (textureProperty.OffsetOriginal == null)
                        textureProperty.OffsetOriginal = material.GetTextureOffset($"_{propertyName}");
                }
            }

            if (setProperty)
                SetTextureOffset(gameObject, material.NameFormatted(), propertyName, value);
        }
        public Vector2? GetMaterialTextureOffset(int id, Material material, string propertyName) =>
            MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.MaterialName == material.NameFormatted() && x.Property == propertyName)?.Offset;
        public Vector2? GetMaterialTextureOffsetOriginal(int id, Material material, string propertyName) =>
            MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.MaterialName == material.NameFormatted() && x.Property == propertyName)?.OffsetOriginal;
        public void RemoveMaterialTextureOffset(int id, Material material, string propertyName, bool setProperty = true)
        {
            GameObject gameObject = GetObjectByID(id);
            if (setProperty)
            {
                var original = GetMaterialTextureOffsetOriginal(id, material, propertyName);
                if (original != null)
                    SetTextureOffset(gameObject, material.NameFormatted(), propertyName, original);
            }

            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.MaterialName == material.NameFormatted() && x.Property == propertyName);
            if (textureProperty != null)
            {
                textureProperty.Offset = null;
                textureProperty.OffsetOriginal = null;
                if (textureProperty.NullCheck())
                    MaterialTexturePropertyList.Remove(textureProperty);
            }
        }

        public void AddMaterialTextureScale(int id, Material material, string propertyName, Vector2 value, bool setProperty = true)
        {
            GameObject gameObject = GetObjectByID(id);
            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.MaterialName == material.NameFormatted() && x.Property == propertyName);
            if (textureProperty == null)
            {
                Vector2 valueOriginal = material.GetTextureScale($"_{propertyName}");
                MaterialTexturePropertyList.Add(new MaterialTextureProperty(id, material.NameFormatted(), propertyName, scale: value, scaleOriginal: valueOriginal));
            }
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
                    if (textureProperty.ScaleOriginal == null)
                        textureProperty.ScaleOriginal = material.GetTextureScale($"_{propertyName}");
                }
            }

            if (setProperty)
                SetTextureScale(gameObject, material.NameFormatted(), propertyName, value);
        }

        public Vector2? GetMaterialTextureScale(int id, Material material, string propertyName) =>
            MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.MaterialName == material.NameFormatted() && x.Property == propertyName)?.Scale;
        public Vector2? GetMaterialTextureScaleOriginal(int id, Material material, string propertyName) =>
            MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.MaterialName == material.NameFormatted() && x.Property == propertyName)?.ScaleOriginal;

        public void RemoveMaterialTextureScale(int id, Material material, string propertyName, bool setProperty = true)
        {
            GameObject gameObject = GetObjectByID(id);
            if (setProperty)
            {
                var original = GetMaterialTextureScaleOriginal(id, material, propertyName);
                if (original != null)
                    SetTextureScale(gameObject, material.NameFormatted(), propertyName, original);
            }

            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.MaterialName == material.NameFormatted() && x.Property == propertyName);
            if (textureProperty != null)
            {
                textureProperty.Scale = null;
                textureProperty.ScaleOriginal = null;
                if (textureProperty.NullCheck())
                    MaterialTexturePropertyList.Remove(textureProperty);
            }
        }

        public void AddMaterialShader(int id, Material material, string shaderName, bool setProperty = true)
        {
            GameObject gameObject = GetObjectByID(id);
            var materialProperty = MaterialShaderList.FirstOrDefault(x => x.ID == id && x.MaterialName == material.NameFormatted());
            if (materialProperty == null)
            {
                string shaderNameOriginal = material.shader.NameFormatted();
                MaterialShaderList.Add(new MaterialShader(id, material.NameFormatted(), shaderName, shaderNameOriginal));
            }
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
                    if (materialProperty.ShaderNameOriginal == null)
                        materialProperty.ShaderNameOriginal = material.shader.NameFormatted();
                }
            }

            if (setProperty)
            {
                RemoveMaterialShaderRenderQueue(id, material, false);
                SetShader(gameObject, material.NameFormatted(), shaderName);
            }
        }
        public string GetMaterialShader(int id, Material material) =>
            MaterialShaderList.FirstOrDefault(x => x.ID == id && x.MaterialName == material.NameFormatted())?.ShaderName;
        public string GetMaterialShaderOriginal(int id, Material material) =>
            MaterialShaderList.FirstOrDefault(x => x.ID == id && x.MaterialName == material.NameFormatted())?.ShaderNameOriginal;
        public void RemoveMaterialShader(int id, Material material, bool setProperty = true)
        {
            GameObject gameObject = GetObjectByID(id);
            if (setProperty)
            {
                var original = GetMaterialShaderOriginal(id, material);
                if (!original.IsNullOrEmpty())
                    SetShader(gameObject, material.NameFormatted(), original);
            }

            foreach (var materialProperty in MaterialShaderList.Where(x => x.ID == id && x.MaterialName == material.NameFormatted()))
            {
                materialProperty.ShaderName = null;
                materialProperty.ShaderNameOriginal = null;
            }

            MaterialShaderList.RemoveAll(x => x.ID == id && x.MaterialName == material.NameFormatted() && x.NullCheck());
        }

        public void AddMaterialShaderRenderQueue(int id, Material material, int renderQueue, bool setProperty = true)
        {
            GameObject gameObject = GetObjectByID(id);
            var materialProperty = MaterialShaderList.FirstOrDefault(x => x.ID == id && x.MaterialName == material.NameFormatted());
            if (materialProperty == null)
            {
                int renderQueueOriginal = material.renderQueue;
                MaterialShaderList.Add(new MaterialShader(id, material.NameFormatted(), renderQueue, renderQueueOriginal));
            }
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
                    if (materialProperty.RenderQueueOriginal == null)
                        materialProperty.RenderQueueOriginal = material.renderQueue;
                }
            }

            if (setProperty)
                SetRenderQueue(gameObject, material.NameFormatted(), renderQueue);
        }
        public int? GetMaterialShaderRenderQueue(int id, Material material) =>
            MaterialShaderList.FirstOrDefault(x => x.ID == id && x.MaterialName == material.NameFormatted())?.RenderQueue;
        public int? GetMaterialShaderRenderQueueOriginal(int id, Material material) =>
            MaterialShaderList.FirstOrDefault(x => x.ID == id && x.MaterialName == material.NameFormatted())?.RenderQueueOriginal;
        public void RemoveMaterialShaderRenderQueue(int id, Material material, bool setProperty = true)
        {
            GameObject gameObject = GetObjectByID(id);
            if (setProperty)
            {
                var original = GetMaterialShaderRenderQueueOriginal(id, material);
                if (original != null)
                    SetRenderQueue(gameObject, material.NameFormatted(), original);
            }

            foreach (var materialProperty in MaterialShaderList.Where(x => x.ID == id && x.MaterialName == material.NameFormatted()))
            {
                materialProperty.RenderQueue = null;
                materialProperty.RenderQueueOriginal = null;
            }

            MaterialShaderList.RemoveAll(x => x.ID == id && x.MaterialName == material.NameFormatted() && x.NullCheck());
        }

        private GameObject GetObjectByID(int id)
        {
            if (!Studio.Studio.Instance.dicObjectCtrl.TryGetValue(id, out var objectCtrlInfo)) return null;
            if (objectCtrlInfo is OCIItem ociItem)
                return ociItem.objectItem;
            else if (objectCtrlInfo is OCIChar ociChar)
                return ociChar.charInfo.gameObject;
            return null;
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
