using ExtensibleSaveFormat;
using KKAPI.Maker;
using KKAPI.Studio;
using KKAPI.Studio.SaveLoad;
using KKAPI.Utilities;
using MaterialEditorAPI;
using MessagePack;
using Studio;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;
using static MaterialEditorAPI.MaterialEditorPluginBase;

namespace KK_Plugins.MaterialEditor
{
    /// <summary>
    /// KKAPI scene controller which provides access for getting and setting properties to be saved and loaded with the scene data
    /// </summary>
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
        private static int IDToSet;

        /// <summary>
        /// Saves data
        /// </summary>
        protected override void OnSceneSave()
        {
            var data = new PluginData();

            List<int> IDsToPurge = new List<int>();
            foreach (int texID in TextureDictionary.Keys)
                if (MaterialTexturePropertyList.All(x => x.TexID != texID))
                    IDsToPurge.Add(texID);

            for (var i = 0; i < IDsToPurge.Count; i++)
                TextureDictionary.Remove(IDsToPurge[i]);

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

        /// <summary>
        /// Loads saved data
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="loadedItems"></param>
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
            {
                var properties = MessagePackSerializer.Deserialize<List<MaterialShader>>((byte[])shaderProperties);
                for (var i = 0; i < properties.Count; i++)
                {
                    var loadedProperty = properties[i];
                    if (loadedItems.TryGetValue(loadedProperty.ID, out ObjectCtrlInfo objectCtrlInfo) && objectCtrlInfo is OCIItem ociItem)
                    {
                        bool setShader = SetShader(ociItem.objectItem, loadedProperty.MaterialName, loadedProperty.ShaderName);
                        bool setRenderQueue = SetRenderQueue(ociItem.objectItem, loadedProperty.MaterialName, loadedProperty.RenderQueue);
                        if (setShader || setRenderQueue)
                            MaterialShaderList.Add(new MaterialShader(MEStudio.GetObjectID(objectCtrlInfo), loadedProperty.MaterialName, loadedProperty.ShaderName, loadedProperty.ShaderNameOriginal, loadedProperty.RenderQueue, loadedProperty.RenderQueueOriginal));
                    }
                }
            }

            if (data.data.TryGetValue(nameof(RendererPropertyList), out var rendererProperties) && rendererProperties != null)
            {
                var properties = MessagePackSerializer.Deserialize<List<RendererProperty>>((byte[])rendererProperties);
                for (var i = 0; i < properties.Count; i++)
                {
                    var loadedProperty = properties[i];
                    if (loadedItems.TryGetValue(loadedProperty.ID, out ObjectCtrlInfo objectCtrlInfo) && objectCtrlInfo is OCIItem ociItem)
                        if (MaterialAPI.SetRendererProperty(ociItem.objectItem, loadedProperty.RendererName, loadedProperty.Property, int.Parse(loadedProperty.Value)))
                            RendererPropertyList.Add(new RendererProperty(MEStudio.GetObjectID(objectCtrlInfo), loadedProperty.RendererName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));
                }
            }

            if (data.data.TryGetValue(nameof(MaterialFloatPropertyList), out var materialFloatProperties) && materialFloatProperties != null)
            {
                var properties = MessagePackSerializer.Deserialize<List<MaterialFloatProperty>>((byte[])materialFloatProperties);
                for (var i = 0; i < properties.Count; i++)
                {
                    var loadedProperty = properties[i];
                    if (loadedItems.TryGetValue(loadedProperty.ID, out ObjectCtrlInfo objectCtrlInfo) && objectCtrlInfo is OCIItem ociItem)
                        if (SetFloat(ociItem.objectItem, loadedProperty.MaterialName, loadedProperty.Property, float.Parse(loadedProperty.Value)))
                            MaterialFloatPropertyList.Add(new MaterialFloatProperty(MEStudio.GetObjectID(objectCtrlInfo), loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));
                }
            }

            if (data.data.TryGetValue(nameof(MaterialColorPropertyList), out var materialColorProperties) && materialColorProperties != null)
            {
                var properties = MessagePackSerializer.Deserialize<List<MaterialColorProperty>>((byte[])materialColorProperties);
                for (var i = 0; i < properties.Count; i++)
                {
                    var loadedProperty = properties[i];
                    if (loadedItems.TryGetValue(loadedProperty.ID, out ObjectCtrlInfo objectCtrlInfo) && objectCtrlInfo is OCIItem ociItem)
                        if (SetColor(ociItem.objectItem, loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value))
                            MaterialColorPropertyList.Add(new MaterialColorProperty(MEStudio.GetObjectID(objectCtrlInfo), loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));
                }
            }

            if (data.data.TryGetValue(nameof(MaterialTexturePropertyList), out var materialTextureProperties) && materialTextureProperties != null)
            {
                var properties = MessagePackSerializer.Deserialize<List<MaterialTextureProperty>>((byte[])materialTextureProperties);
                for (var i = 0; i < properties.Count; i++)
                {
                    var loadedProperty = properties[i];
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
                            setTex = SetTexture(ociItem.objectItem, newTextureProperty.MaterialName, newTextureProperty.Property, TextureDictionary[(int)newTextureProperty.TexID].Texture);

                        bool setOffset = SetTextureOffset(ociItem.objectItem, newTextureProperty.MaterialName, newTextureProperty.Property, newTextureProperty.Offset);
                        bool setScale = SetTextureScale(ociItem.objectItem, newTextureProperty.MaterialName, newTextureProperty.Property, newTextureProperty.Scale);

                        if (setTex || setOffset || setScale)
                            MaterialTexturePropertyList.Add(newTextureProperty);
                    }
                }
            }
        }

        /// <summary>
        /// Handles copying data when objects are copied
        /// </summary>
        /// <param name="copiedItems"></param>
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
                    for (var i = 0; i < MaterialShaderList.Count; i++)
                    {
                        var loadedProperty = MaterialShaderList[i];
                        if (loadedProperty.ID == copiedItem.Key)
                        {
                            bool setShader = SetShader(ociItem.objectItem, loadedProperty.MaterialName, loadedProperty.ShaderName);
                            bool setRenderQueue = SetRenderQueue(ociItem.objectItem, loadedProperty.MaterialName, loadedProperty.RenderQueue);
                            if (setShader || setRenderQueue) materialShaderListNew.Add(new MaterialShader(copiedItem.Value.GetSceneId(), loadedProperty.MaterialName, loadedProperty.ShaderName, loadedProperty.ShaderNameOriginal, loadedProperty.RenderQueue, loadedProperty.RenderQueueOriginal));
                        }
                    }

                    for (var i = 0; i < RendererPropertyList.Count; i++)
                    {
                        var loadedProperty = RendererPropertyList[i];
                        if (loadedProperty.ID == copiedItem.Key)
                            if (MaterialAPI.SetRendererProperty(ociItem.objectItem, loadedProperty.RendererName, loadedProperty.Property, loadedProperty.Value))
                                rendererPropertyListNew.Add(new RendererProperty(copiedItem.Value.GetSceneId(), loadedProperty.RendererName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));
                    }

                    for (var i = 0; i < MaterialFloatPropertyList.Count; i++)
                    {
                        var loadedProperty = MaterialFloatPropertyList[i];
                        if (loadedProperty.ID == copiedItem.Key)
                            if (SetFloat(ociItem.objectItem, loadedProperty.MaterialName, loadedProperty.Property, float.Parse(loadedProperty.Value)))
                                materialFloatPropertyListNew.Add(new MaterialFloatProperty(copiedItem.Value.GetSceneId(), loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));
                    }

                    for (var i = 0; i < MaterialColorPropertyList.Count; i++)
                    {
                        var loadedProperty = MaterialColorPropertyList[i];
                        if (loadedProperty.ID == copiedItem.Key)
                            if (SetColor(ociItem.objectItem, loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value))
                                materialColorPropertyListNew.Add(new MaterialColorProperty(copiedItem.Value.GetSceneId(), loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));
                    }

                    for (var i = 0; i < MaterialTexturePropertyList.Count; i++)
                    {
                        var loadedProperty = MaterialTexturePropertyList[i];
                        if (loadedProperty.ID == copiedItem.Key)
                        {
                            MaterialTextureProperty newTextureProperty = new MaterialTextureProperty(copiedItem.Value.GetSceneId(), loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.TexID, loadedProperty.Offset, loadedProperty.OffsetOriginal, loadedProperty.Scale, loadedProperty.ScaleOriginal);

                            bool setTex = false;
                            if (loadedProperty.TexID != null) setTex = SetTexture(ociItem.objectItem, newTextureProperty.MaterialName, newTextureProperty.Property, TextureDictionary[(int)newTextureProperty.TexID].Texture);

                            bool setOffset = SetTextureOffset(ociItem.objectItem, newTextureProperty.MaterialName, newTextureProperty.Property, newTextureProperty.Offset);
                            bool setScale = SetTextureScale(ociItem.objectItem, newTextureProperty.MaterialName, newTextureProperty.Property, newTextureProperty.Scale);

                            if (setTex || setOffset || setScale) materialTexturePropertyListNew.Add(newTextureProperty);
                        }
                    }
                }
            }

            RendererPropertyList.AddRange(rendererPropertyListNew);
            MaterialFloatPropertyList.AddRange(materialFloatPropertyListNew);
            MaterialColorPropertyList.AddRange(materialColorPropertyListNew);
            MaterialTexturePropertyList.AddRange(materialTexturePropertyListNew);
            MaterialShaderList.AddRange(materialShaderListNew);
        }

        private void Update()
        {
            if (MaterialEditorPlugin.DisableShadowCastingHotkey.Value.IsDown())
            {
                int count = 0;
                TreeNodeObject[] selectNodes = Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNodes;
                for (int i = 0; i < selectNodes.Length; i++)
                    SetRendererPropertyRecursive(selectNodes[i], RendererProperties.ShadowCastingMode, "0", ref count);
                if (count > 0)
                    MaterialEditorPlugin.Logger.LogMessage($"Disabled ShadowCasting for {count} items");
            }
            else if (MaterialEditorPlugin.EnableShadowCastingHotkey.Value.IsDown())
            {
                int count = 0;
                TreeNodeObject[] selectNodes = Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNodes;
                for (int i = 0; i < selectNodes.Length; i++)
                    SetRendererPropertyRecursive(selectNodes[i], RendererProperties.ShadowCastingMode, "1", ref count);
                if (count > 0)
                    MaterialEditorPlugin.Logger.LogMessage($"Enabled ShadowCasting for {count} items");
            }
            else if (MaterialEditorPlugin.ResetShadowCastingHotkey.Value.IsDown())
            {
                int count = 0;
                TreeNodeObject[] selectNodes = Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNodes;
                for (int i = 0; i < selectNodes.Length; i++)
                    SetRendererPropertyRecursive(selectNodes[i], RendererProperties.ShadowCastingMode, "-1", ref count);
                if (count > 0)
                    MaterialEditorPlugin.Logger.LogMessage($"Reset ShadowCasting for {count} items");
            }
            else if (MaterialEditorPlugin.DisableReceiveShadows.Value.IsDown())
            {
                int count = 0;
                TreeNodeObject[] selectNodes = Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNodes;
                for (int i = 0; i < selectNodes.Length; i++)
                    SetRendererPropertyRecursive(selectNodes[i], RendererProperties.ReceiveShadows, "0", ref count);
                if (count > 0)
                    MaterialEditorPlugin.Logger.LogMessage($"Disabled ReceiveShadows for {count} items");
            }
            else if (MaterialEditorPlugin.EnableReceiveShadows.Value.IsDown())
            {
                int count = 0;
                TreeNodeObject[] selectNodes = Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNodes;
                for (int i = 0; i < selectNodes.Length; i++)
                    SetRendererPropertyRecursive(selectNodes[i], RendererProperties.ReceiveShadows, "1", ref count);
                if (count > 0)
                    MaterialEditorPlugin.Logger.LogMessage($"Enabled ReceiveShadows for {count} items");
            }
            else if (MaterialEditorPlugin.ResetReceiveShadows.Value.IsDown())
            {
                int count = 0;
                TreeNodeObject[] selectNodes = Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNodes;
                for (int i = 0; i < selectNodes.Length; i++)
                    SetRendererPropertyRecursive(selectNodes[i], RendererProperties.ReceiveShadows, "-1", ref count);
                if (count > 0)
                    MaterialEditorPlugin.Logger.LogMessage($"Reset ReceiveShadows for {count} items");
            }
            try
            {
                if (!FileToSet.IsNullOrEmpty())
                    SetMaterialTextureFromFile(IDToSet, MatToSet, PropertyToSet, FileToSet);
            }
            catch
            {
                //MaterialEditorPlugin.Logger.Log(BepInEx.Logging.LogLevel.Error | BepInEx.Logging.LogLevel.Message, "Failed to load texture.");
            }
            finally
            {
                FileToSet = null;
                PropertyToSet = null;
                MatToSet = null;
            }
        }

        private void SetRendererPropertyRecursive(TreeNodeObject node, RendererProperties property, string value, ref int count)
        {
            if (Studio.Studio.Instance.dicInfo.TryGetValue(node, out ObjectCtrlInfo objectCtrlInfo))
                if (objectCtrlInfo is OCIItem ociItem)
                {
                    count++;
                    for (var index = 0; index < ociItem.arrayRender.Length; index++)
                    {
                        if (value == "-1")
                            RemoveRendererProperty(ociItem.objectInfo.dicKey, ociItem.arrayRender[index], property);
                        else
                            SetRendererProperty(ociItem.objectInfo.dicKey, ociItem.arrayRender[index], property, value);
                    }
                }
                else if (objectCtrlInfo is OCIChar ociChar)
                {
                    count++;
                    var chaControl = ociChar.GetChaControl();
                    var controller = MaterialEditorPlugin.GetCharaController(chaControl);
                    foreach (var rend in GetRendererList(chaControl.gameObject))
                    {
                        //Disable the shadowcaster renderer instead of changing the shadowcasting mode
                        if (property == RendererProperties.ShadowCastingMode && (rend.name == "o_shadowcaster" || rend.name == "o_shadowcaster_cm"))
                        {
                            if (value == "-1")
                                controller.RemoveRendererProperty(0, MaterialEditorCharaController.ObjectType.Character, rend, RendererProperties.Enabled, chaControl.gameObject);
                            else
                                controller.SetRendererProperty(0, MaterialEditorCharaController.ObjectType.Character, rend, RendererProperties.Enabled, value, chaControl.gameObject);
                        }
                        else
                        {
                            if (value == "-1")
                                controller.RemoveRendererProperty(0, MaterialEditorCharaController.ObjectType.Character, rend, property, chaControl.gameObject);
                            else
                                controller.SetRendererProperty(0, MaterialEditorCharaController.ObjectType.Character, rend, property, value, chaControl.gameObject);
                        }
                    }
                    var clothes = chaControl.GetClothes();
                    for (var i = 0; i < clothes.Length; i++)
                    {
                        var gameObj = clothes[i];
                        foreach (var renderer in GetRendererList(gameObj))
                            if (value == "-1")
                                controller.RemoveRendererProperty(i, MaterialEditorCharaController.ObjectType.Clothing, renderer, property, gameObj);
                            else
                                controller.SetRendererProperty(i, MaterialEditorCharaController.ObjectType.Clothing, renderer, property, value, gameObj);
                    }
                    var hair = chaControl.GetHair();
                    for (var i = 0; i < hair.Length; i++)
                    {
                        var gameObj = hair[i];
                        foreach (var renderer in GetRendererList(gameObj))
                            if (value == "-1")
                                controller.RemoveRendererProperty(i, MaterialEditorCharaController.ObjectType.Hair, renderer, property, gameObj);
                            else
                                controller.SetRendererProperty(i, MaterialEditorCharaController.ObjectType.Hair, renderer, property, value, gameObj);
                    }
                    var accessories = chaControl.GetAccessoryObjects();
                    for (var i = 0; i < accessories.Length; i++)
                    {
                        var gameObj = accessories[i];
                        if (gameObj != null)
                            foreach (var renderer in GetRendererList(gameObj))
                                if (value == "-1")
                                    controller.RemoveRendererProperty(i, MaterialEditorCharaController.ObjectType.Accessory, renderer, property, gameObj);
                                else
                                    controller.SetRendererProperty(i, MaterialEditorCharaController.ObjectType.Accessory, renderer, property, value, gameObj);
                    }
                }
            foreach (var child in node.child)
                SetRendererPropertyRecursive(child, property, value, ref count);
        }

        protected override void OnObjectDeleted(ObjectCtrlInfo objectCtrlInfo)
        {
            if (objectCtrlInfo is OCIItem item)
            {
                var id = item.GetSceneId();
                RendererPropertyList.RemoveAll(x => x.ID == id);
                MaterialFloatPropertyList.RemoveAll(x => x.ID == id);
                MaterialColorPropertyList.RemoveAll(x => x.ID == id);
                MaterialTexturePropertyList.RemoveAll(x => x.ID == id);
                MaterialShaderList.RemoveAll(x => x.ID == id);
                MaterialEditorUI.Visible = false;
            }
            else if (objectCtrlInfo is OCIChar)
                MaterialEditorUI.Visible = false;
            base.OnObjectDeleted(objectCtrlInfo);
        }

        protected override void OnObjectVisibilityToggled(ObjectCtrlInfo objectCtrlInfo, bool visible)
        {
            if (visible && objectCtrlInfo is OCIItem item)
            {
                var id = item.GetSceneId();
                var property = RendererPropertyList.FirstOrDefault(x => x.ID == id && x.Property == RendererProperties.Enabled);
                if (property != null)
                    MaterialAPI.SetRendererProperty(GetObjectByID(id), property.RendererName, property.Property, property.Value);
            }
            base.OnObjectVisibilityToggled(objectCtrlInfo, visible);
        }

        protected override void OnObjectsSelected(List<ObjectCtrlInfo> objectCtrlInfo)
        {
            if (MaterialEditorUI.Visible)
                MEStudio.Instance.UpdateUI();
            base.OnObjectsSelected(objectCtrlInfo);
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

        /// <summary>
        /// Copy any edits for the specified object
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        public void MaterialCopyEdits(int id, Material material)
        {
            CopyData.ClearAll();

            for (var i = 0; i < MaterialShaderList.Count; i++)
            {
                var materialShader = MaterialShaderList[i];
                if (materialShader.ID == id && materialShader.MaterialName == material.NameFormatted())
                    CopyData.MaterialShaderList.Add(new CopyContainer.MaterialShader(materialShader.ShaderName, materialShader.RenderQueue));
            }
            for (var i = 0; i < MaterialFloatPropertyList.Count; i++)
            {
                var materialFloatProperty = MaterialFloatPropertyList[i];
                if (materialFloatProperty.ID == id && materialFloatProperty.MaterialName == material.NameFormatted())
                    CopyData.MaterialFloatPropertyList.Add(new CopyContainer.MaterialFloatProperty(materialFloatProperty.Property, float.Parse(materialFloatProperty.Value)));
            }
            for (var i = 0; i < MaterialColorPropertyList.Count; i++)
            {
                var materialColorProperty = MaterialColorPropertyList[i];
                if (materialColorProperty.ID == id && materialColorProperty.MaterialName == material.NameFormatted())
                    CopyData.MaterialColorPropertyList.Add(new CopyContainer.MaterialColorProperty(materialColorProperty.Property, materialColorProperty.Value));
            }
            for (var i = 0; i < MaterialTexturePropertyList.Count; i++)
            {
                var materialTextureProperty = MaterialTexturePropertyList[i];
                if (materialTextureProperty.ID == id && materialTextureProperty.MaterialName == material.NameFormatted())
                {
                    if (materialTextureProperty.TexID != null)
                        CopyData.MaterialTexturePropertyList.Add(new CopyContainer.MaterialTextureProperty(materialTextureProperty.Property, TextureDictionary[(int)materialTextureProperty.TexID].Data, materialTextureProperty.Offset, materialTextureProperty.Scale));
                    else
                        CopyData.MaterialTexturePropertyList.Add(new CopyContainer.MaterialTextureProperty(materialTextureProperty.Property, null, materialTextureProperty.Offset, materialTextureProperty.Scale));
                }
            }
        }
        /// <summary>
        /// Paste any edits for the specified object
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void MaterialPasteEdits(int id, Material material, bool setProperty = true)
        {
            for (var i = 0; i < CopyData.MaterialShaderList.Count; i++)
            {
                var materialShader = CopyData.MaterialShaderList[i];
                if (materialShader.ShaderName != null)
                    SetMaterialShader(id, material, materialShader.ShaderName, setProperty);
                if (materialShader.RenderQueue != null)
                    SetMaterialShaderRenderQueue(id, material, (int)materialShader.RenderQueue, setProperty);
            }
            for (var i = 0; i < CopyData.MaterialFloatPropertyList.Count; i++)
            {
                var materialFloatProperty = CopyData.MaterialFloatPropertyList[i];
                if (material.HasProperty($"_{materialFloatProperty.Property}"))
                    SetMaterialFloatProperty(id, material, materialFloatProperty.Property, materialFloatProperty.Value, setProperty);
            }
            for (var i = 0; i < CopyData.MaterialColorPropertyList.Count; i++)
            {
                var materialColorProperty = CopyData.MaterialColorPropertyList[i];
                if (material.HasProperty($"_{materialColorProperty.Property}"))
                    SetMaterialColorProperty(id, material, materialColorProperty.Property, materialColorProperty.Value, setProperty);
            }
            for (var i = 0; i < CopyData.MaterialTexturePropertyList.Count; i++)
            {
                var materialTextureProperty = CopyData.MaterialTexturePropertyList[i];
                if (material.HasProperty($"_{materialTextureProperty.Property}"))
                    SetMaterialTexture(id, material, materialTextureProperty.Property, materialTextureProperty.Data);
                if (materialTextureProperty.Offset != null)
                    SetMaterialTextureOffset(id, material, materialTextureProperty.Property, (Vector2)materialTextureProperty.Offset, setProperty);
                if (materialTextureProperty.Scale != null)
                    SetMaterialTextureScale(id, material, materialTextureProperty.Property, (Vector2)materialTextureProperty.Scale, setProperty);
            }
        }

        #region Set, Get, Remove methods
        /// <summary>
        /// Add a renderer property to be saved and loaded with the scene and optionally also update the renderer.
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="renderer">Renderer being modified</param>
        /// <param name="property">Property of the renderer</param>
        /// <param name="value">Value</param>
        /// <param name="setProperty">Whether to also apply the value to the renderer</param>
        public void SetRendererProperty(int id, Renderer renderer, RendererProperties property, string value, bool setProperty = true)
        {
            GameObject go = GetObjectByID(id);
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
                    RemoveRendererProperty(id, renderer, property, false);
                else
                    rendererProperty.Value = value;
            }

            if (setProperty)
                MaterialAPI.SetRendererProperty(go, renderer.NameFormatted(), property, value);
        }

        /// <summary>
        /// Get the saved renderer property value or null if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="renderer">Renderer being modified</param>
        /// <param name="property">Property of the renderer</param>
        /// <returns>Saved renderer property value</returns>
        public string GetRendererPropertyValue(int id, Renderer renderer, RendererProperties property) =>
            RendererPropertyList.FirstOrDefault(x => x.ID == id && x.Property == property && x.RendererName == renderer.NameFormatted())?.Value;
        /// <summary>
        /// Get the saved renderer property's original value or null if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="renderer">Renderer being modified</param>
        /// <param name="property">Property of the renderer</param>
        /// <returns>Saved renderer property's original value</returns>
        public string GetRendererPropertyValueOriginal(int id, Renderer renderer, RendererProperties property) =>
            RendererPropertyList.FirstOrDefault(x => x.ID == id && x.Property == property && x.RendererName == renderer.NameFormatted())?.ValueOriginal;
        /// <summary>
        /// Remove the saved renderer property value if one is saved and optionally also update the renderer
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="renderer">Renderer being modified</param>
        /// <param name="property">Property of the renderer</param>
        /// <param name="setProperty">Whether to also apply the value to the renderer</param>
        public void RemoveRendererProperty(int id, Renderer renderer, RendererProperties property, bool setProperty = true)
        {
            GameObject go = GetObjectByID(id);
            if (setProperty)
            {
                var original = GetRendererPropertyValueOriginal(id, renderer, property);
                if (!original.IsNullOrEmpty())
                    MaterialAPI.SetRendererProperty(go, renderer.NameFormatted(), property, original);
            }

            RendererPropertyList.RemoveAll(x => x.ID == id && x.Property == property && x.RendererName == renderer.NameFormatted());
        }

        /// <summary>
        /// Add a float property to be saved and loaded with the scene and optionally also update the materials.
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="value">Value</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void SetMaterialFloatProperty(int id, Material material, string propertyName, float value, bool setProperty = true)
        {
            GameObject go = GetObjectByID(id);
            var materialProperty = MaterialFloatPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted());
            if (materialProperty == null)
            {
                float valueOriginal = material.GetFloat($"_{propertyName}");
                MaterialFloatPropertyList.Add(new MaterialFloatProperty(id, material.NameFormatted(), propertyName, value.ToString(CultureInfo.InvariantCulture), valueOriginal.ToString(CultureInfo.InvariantCulture)));
            }
            else
            {
                if (value.ToString(CultureInfo.InvariantCulture) == materialProperty.ValueOriginal)
                    RemoveMaterialFloatProperty(id, material, propertyName, false);
                else
                    materialProperty.Value = value.ToString(CultureInfo.InvariantCulture);
            }

            if (setProperty)
                SetFloat(go, material.NameFormatted(), propertyName, value);
        }
        /// <summary>
        /// Get the saved material property value or null if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <returns>Saved material property value or null if none is saved</returns>
        public float? GetMaterialFloatPropertyValue(int id, Material material, string propertyName)
        {
            var value = MaterialFloatPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.Value;
            if (value.IsNullOrEmpty())
                return null;
            return float.Parse(value);
        }
        /// <summary>
        /// Get the saved material property's original value or null if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <returns>Saved material property's original value or null if none is saved</returns>
        public float? GetMaterialFloatPropertyValueOriginal(int id, Material material, string propertyName)
        {
            var valueOriginal = MaterialFloatPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.ValueOriginal;
            if (valueOriginal.IsNullOrEmpty())
                return null;
            return float.Parse(valueOriginal);
        }
        /// <summary>
        /// Remove the saved material property value if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void RemoveMaterialFloatProperty(int id, Material material, string propertyName, bool setProperty = true)
        {
            GameObject go = GetObjectByID(id);
            if (setProperty)
            {
                var original = GetMaterialFloatPropertyValueOriginal(id, material, propertyName);
                if (original != null)
                    SetFloat(go, material.NameFormatted(), propertyName, (float)original);
            }

            MaterialFloatPropertyList.RemoveAll(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted());
        }
        /// <summary>
        /// Add a color property to be saved and loaded with the scene and optionally also update the materials.
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="value">Value</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void SetMaterialColorProperty(int id, Material material, string propertyName, Color value, bool setProperty = true)
        {
            GameObject go = GetObjectByID(id);
            var colorProperty = MaterialColorPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted());
            if (colorProperty == null)
            {
                Color valueOriginal = material.GetColor($"_{propertyName}");
                MaterialColorPropertyList.Add(new MaterialColorProperty(id, material.NameFormatted(), propertyName, value, valueOriginal));
            }
            else
            {
                if (value == colorProperty.ValueOriginal)
                    RemoveMaterialColorProperty(id, material, propertyName, false);
                else
                    colorProperty.Value = value;
            }

            if (setProperty)
                SetColor(go, material.NameFormatted(), propertyName, value);
        }
        /// <summary>
        /// Get the saved material property value or null if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <returns>Saved material property value or null if none is saved</returns>
        public Color? GetMaterialColorPropertyValue(int id, Material material, string propertyName) =>
            MaterialColorPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.Value;
        /// <summary>
        /// Get the saved material property's original value or null if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <returns>Saved material property's original value or null if none is saved</returns>
        public Color? GetMaterialColorPropertyValueOriginal(int id, Material material, string propertyName) =>
            MaterialColorPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.ValueOriginal;
        /// <summary>
        /// Remove the saved material property value if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void RemoveMaterialColorProperty(int id, Material material, string propertyName, bool setProperty = true)
        {
            GameObject go = GetObjectByID(id);
            if (setProperty)
            {
                var original = GetMaterialColorPropertyValueOriginal(id, material, propertyName);
                if (original != null)
                    SetColor(go, material.NameFormatted(), propertyName, (Color)original);
            }

            MaterialColorPropertyList.RemoveAll(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted());
        }

        /// <summary>
        /// Add a texture property to be saved and loaded with the card.
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="filePath">Path to the .png file on disk</param>
        /// <param name="setTexInUpdate">Whether to wait for the next Update</param>
        public void SetMaterialTextureFromFile(int id, Material material, string propertyName, string filePath, bool setTexInUpdate = false)
        {
            GameObject go = GetObjectByID(id);
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
                Texture2D tex = MaterialEditorPlugin.TextureFromBytes(texBytes);

                SetTexture(go, material.NameFormatted(), propertyName, tex);

                var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted());
                if (textureProperty == null)
                {
                    textureProperty = new MaterialTextureProperty(id, material.NameFormatted(), propertyName, SetAndGetTextureID(texBytes));
                    MaterialTexturePropertyList.Add(textureProperty);
                }
                else
                    textureProperty.TexID = SetAndGetTextureID(texBytes);
            }
        }
        /// <summary>
        /// Add a texture property to be saved and loaded with the card.
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="data">Byte array containing the texture data</param>
        public void SetMaterialTexture(int id, Material material, string propertyName, byte[] data)
        {
            GameObject go = GetObjectByID(id);
            if (data == null) return;

            Texture2D tex = MaterialEditorPlugin.TextureFromBytes(data);

            SetTexture(go, material.NameFormatted(), propertyName, tex);

            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted());
            if (textureProperty == null)
            {
                textureProperty = new MaterialTextureProperty(id, material.NameFormatted(), propertyName, SetAndGetTextureID(data));
                MaterialTexturePropertyList.Add(textureProperty);
            }
            else
                textureProperty.TexID = SetAndGetTextureID(data);
        }
        /// <summary>
        /// Get the saved material property value or null if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <returns>Saved material property value or null if none is saved</returns>
        public Texture2D GetMaterialTexture(int id, Material material, string propertyName)
        {
            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.MaterialName == material.NameFormatted() && x.Property == propertyName);
            if (textureProperty?.TexID != null)
                return TextureDictionary[(int)textureProperty.TexID].Texture;
            return null;
        }

        /// <summary>
        /// Get whether the texture has been changed
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <returns>True if the texture has not been modified, false if it has been.</returns>
        public bool GetMaterialTextureOriginal(int id, Material material, string propertyName) =>
            MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.MaterialName == material.NameFormatted() && x.Property == propertyName)?.TexID == null;
        /// <summary>
        /// Remove the saved material property value if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="displayMessage">Whether to display a message on screen telling the user to save and reload to refresh textures</param>
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

        /// <summary>
        /// Add a texture offset property to be saved and loaded with the scene and optionally also update the materials.
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="value">Value</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void SetMaterialTextureOffset(int id, Material material, string propertyName, Vector2 value, bool setProperty = true)
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
                    RemoveMaterialTextureOffset(id, material, propertyName, false);
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
        /// <summary>
        /// Get the saved material property value or null if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <returns>Saved material property value or null if none is saved</returns>
        public Vector2? GetMaterialTextureOffset(int id, Material material, string propertyName) =>
            MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.MaterialName == material.NameFormatted() && x.Property == propertyName)?.Offset;
        /// <summary>
        /// Get the saved material property's original value or null if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <returns>Saved material property's original value or null if none is saved</returns>
        public Vector2? GetMaterialTextureOffsetOriginal(int id, Material material, string propertyName) =>
            MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.MaterialName == material.NameFormatted() && x.Property == propertyName)?.OffsetOriginal;
        /// <summary>
        /// Remove the saved material property value if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
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

        /// <summary>
        /// Add a texture scale property to be saved and loaded with the scene and optionally also update the materials.
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="value">Value</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void SetMaterialTextureScale(int id, Material material, string propertyName, Vector2 value, bool setProperty = true)
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
                    RemoveMaterialFloatProperty(id, material, propertyName, false);
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

        /// <summary>
        /// Get the saved material property value or null if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <returns>Saved material property value or null if none is saved</returns>
        public Vector2? GetMaterialTextureScale(int id, Material material, string propertyName) =>
            MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.MaterialName == material.NameFormatted() && x.Property == propertyName)?.Scale;
        /// <summary>
        /// Get the saved material property's original value or null if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <returns>Saved material property's original value or null if none is saved</returns>
        public Vector2? GetMaterialTextureScaleOriginal(int id, Material material, string propertyName) =>
            MaterialTexturePropertyList.FirstOrDefault(x => x.ID == id && x.MaterialName == material.NameFormatted() && x.Property == propertyName)?.ScaleOriginal;
        /// <summary>
        /// Remove the saved material property value if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
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

        /// <summary>
        /// Add a shader to be saved and loaded with the scene and optionally also update the materials.
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="shaderName">Property of the material without the leading underscore</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void SetMaterialShader(int id, Material material, string shaderName, bool setProperty = true)
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
                    RemoveMaterialShader(id, material, false);
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
        /// <summary>
        /// Get the saved shader name or null if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <returns>Saved shader name or null if none is saved</returns>
        public string GetMaterialShader(int id, Material material) =>
            MaterialShaderList.FirstOrDefault(x => x.ID == id && x.MaterialName == material.NameFormatted())?.ShaderName;
        /// <summary>
        /// Get the saved shader name's original value or null if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <returns>Saved shader name's original value or null if none is saved</returns>
        public string GetMaterialShaderOriginal(int id, Material material) =>
            MaterialShaderList.FirstOrDefault(x => x.ID == id && x.MaterialName == material.NameFormatted())?.ShaderNameOriginal;
        /// <summary>
        /// Remove the saved shader if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
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

        /// <summary>
        /// Add a render queue to be saved and loaded with the scene and optionally also update the materials.
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="renderQueue">Value</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void SetMaterialShaderRenderQueue(int id, Material material, int renderQueue, bool setProperty = true)
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
                    RemoveMaterialShaderRenderQueue(id, material, false);
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
        /// <summary>
        /// Get the saved render queue value or null if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <returns>Saved render queue value or null if none is saved</returns>
        public int? GetMaterialShaderRenderQueue(int id, Material material) =>
            MaterialShaderList.FirstOrDefault(x => x.ID == id && x.MaterialName == material.NameFormatted())?.RenderQueue;
        /// <summary>
        /// Get the saved render queue's original value or null if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <returns>Saved render queue value's original or null if none is saved</returns>
        public int? GetMaterialShaderRenderQueueOriginal(int id, Material material) =>
            MaterialShaderList.FirstOrDefault(x => x.ID == id && x.MaterialName == material.NameFormatted())?.RenderQueueOriginal;
        /// <summary>
        /// Remove the saved render queue value if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void RemoveMaterialShaderRenderQueue(int id, Material material, bool setProperty = true)
        {
            GameObject go = GetObjectByID(id);
            if (setProperty)
            {
                var original = GetMaterialShaderRenderQueueOriginal(id, material);
                if (original != null)
                    SetRenderQueue(go, material.NameFormatted(), original);
            }

            for (var i = 0; i < MaterialShaderList.Count; i++)
            {
                var materialProperty = MaterialShaderList[i];
                if (materialProperty.ID == id && materialProperty.MaterialName == material.NameFormatted())
                {
                    materialProperty.RenderQueue = null;
                    materialProperty.RenderQueueOriginal = null;
                }
            }

            MaterialShaderList.RemoveAll(x => x.ID == id && x.MaterialName == material.NameFormatted() && x.NullCheck());
        }
        #endregion

        private static GameObject GetObjectByID(int id)
        {
            if (!Studio.Studio.Instance.dicObjectCtrl.TryGetValue(id, out var objectCtrlInfo)) return null;
            if (objectCtrlInfo is OCIItem ociItem)
                return ociItem.objectItem;
            else if (objectCtrlInfo is OCIChar ociChar)
                return ociChar.charInfo.gameObject;
            return null;
        }

        /// <summary>
        /// Data storage class for renderer properties
        /// </summary>
        [Serializable]
        [MessagePackObject]
        public class RendererProperty
        {
            /// <summary>
            /// ID of the item
            /// </summary>
            [Key("ID")]
            public int ID;
            /// <summary>
            /// Name of the renderer
            /// </summary>
            [Key("RendererName")]
            public string RendererName;
            /// <summary>
            /// Property type
            /// </summary>
            [Key("Property")]
            public RendererProperties Property;
            /// <summary>
            /// Value
            /// </summary>
            [Key("Value")]
            public string Value;
            /// <summary>
            /// Original value
            /// </summary>
            [Key("ValueOriginal")]
            public string ValueOriginal;

            /// <summary>
            /// Data storage class for renderer properties
            /// </summary>
            /// <param name="id">ID of the item</param>
            /// <param name="rendererName">Name of the renderer</param>
            /// <param name="property">Property type</param>
            /// <param name="value">Value</param>
            /// <param name="valueOriginal">Original</param>
            public RendererProperty(int id, string rendererName, RendererProperties property, string value, string valueOriginal)
            {
                ID = id;
                RendererName = rendererName.Replace("(Instance)", "").Trim();
                Property = property;
                Value = value;
                ValueOriginal = valueOriginal;
            }
        }

        /// <summary>
        /// Data storage class for float properties
        /// </summary>
        [Serializable]
        [MessagePackObject]
        public class MaterialFloatProperty
        {
            /// <summary>
            /// ID of the item
            /// </summary>
            [Key("ID")]
            public int ID;
            /// <summary>
            /// Name of the material
            /// </summary>
            [Key("MaterialName")]
            public string MaterialName;
            /// <summary>
            /// Name of the property
            /// </summary>
            [Key("Property")]
            public string Property;
            /// <summary>
            /// Value
            /// </summary>
            [Key("Value")]
            public string Value;
            /// <summary>
            /// Original value
            /// </summary>
            [Key("ValueOriginal")]
            public string ValueOriginal;

            /// <summary>
            /// Data storage class for float properties
            /// </summary>
            /// <param name="id">ID of the item</param>
            /// <param name="materialName">Name of the material</param>
            /// <param name="property">Name of the property</param>
            /// <param name="value">Value</param>
            /// <param name="valueOriginal">Original</param>
            public MaterialFloatProperty(int id, string materialName, string property, string value, string valueOriginal)
            {
                ID = id;
                MaterialName = materialName.Replace("(Instance)", "").Trim();
                Property = property;
                Value = value;
                ValueOriginal = valueOriginal;
            }
        }

        /// <summary>
        /// Data storage class for color properties
        /// </summary>
        [Serializable]
        [MessagePackObject]
        public class MaterialColorProperty
        {
            /// <summary>
            /// ID of the item
            /// </summary>
            [Key("ID")]
            public int ID;
            /// <summary>
            /// Name of the material
            /// </summary>
            [Key("MaterialName")]
            public string MaterialName;
            /// <summary>
            /// Name of the property
            /// </summary>
            [Key("Property")]
            public string Property;
            /// <summary>
            /// Value
            /// </summary>
            [Key("Value")]
            public Color Value;
            /// <summary>
            /// Original value
            /// </summary>
            [Key("ValueOriginal")]
            public Color ValueOriginal;

            /// <summary>
            /// Data storage class for float properties
            /// </summary>
            /// <param name="id">ID of the item</param>
            /// <param name="materialName">Name of the material</param>
            /// <param name="property">Name of the property</param>
            /// <param name="value">Value</param>
            /// <param name="valueOriginal">Original</param>
            public MaterialColorProperty(int id, string materialName, string property, Color value, Color valueOriginal)
            {
                ID = id;
                MaterialName = materialName.Replace("(Instance)", "").Trim();
                Property = property;
                Value = value;
                ValueOriginal = valueOriginal;
            }
        }

        /// <summary>
        /// Data storage class for texture properties
        /// </summary>
        [Serializable]
        [MessagePackObject]
        public class MaterialTextureProperty
        {
            /// <summary>
            /// ID of the item
            /// </summary>
            [Key("ID")]
            public int ID;
            /// <summary>
            /// Name of the material
            /// </summary>
            [Key("MaterialName")]
            public string MaterialName;
            /// <summary>
            /// Name of the property
            /// </summary>
            [Key("Property")]
            public string Property;
            /// <summary>
            /// ID of the texture from the texure dicionary
            /// </summary>
            [Key("TexID")]
            public int? TexID;
            /// <summary>
            /// Texture offset value
            /// </summary>
            [Key("Offset")]
            public Vector2? Offset;
            /// <summary>
            /// Texture offset original value
            /// </summary>
            [Key("OffsetOriginal")]
            public Vector2? OffsetOriginal;
            /// <summary>
            /// Texture scale value
            /// </summary>
            [Key("Scale")]
            public Vector2? Scale;
            /// <summary>
            /// Texture scale original value
            /// </summary>
            [Key("ScaleOriginal")]
            public Vector2? ScaleOriginal;

            /// <summary>
            /// Data storage class for texture properties
            /// </summary>
            /// <param name="id">ID of the item</param>
            /// <param name="materialName">Name of the material</param>
            /// <param name="property">Name of the property</param>
            /// <param name="texID">ID of the texture as stored in the texture dictionary</param>
            /// <param name="offset">Texture offset value</param>
            /// <param name="offsetOriginal">Texture offset original value</param>
            /// <param name="scale">Texture scale value</param>
            /// <param name="scaleOriginal">Texture scale original value</param>
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
            }

            /// <summary>
            /// Check if the TexID, Offset, and Scale are all null. Safe to remove this data if true.
            /// </summary>
            /// <returns></returns>
            public bool NullCheck() => TexID == null && Offset == null && Scale == null;
        }

        /// <summary>
        /// Data storage class for shaders
        /// </summary>
        [Serializable]
        [MessagePackObject]
        public class MaterialShader
        {
            /// <summary>
            /// ID of the item
            /// </summary>
            [Key("ID")]
            public int ID;
            /// <summary>
            /// Name of the material
            /// </summary>
            [Key("MaterialName")]
            public string MaterialName;
            /// <summary>
            /// Name of the shader
            /// </summary>
            [Key("ShaderName")]
            public string ShaderName;
            /// <summary>
            /// Name of the original shader
            /// </summary>
            [Key("ShaderNameOriginal")]
            public string ShaderNameOriginal;
            /// <summary>
            /// Render queue
            /// </summary>
            [Key("RenderQueue")]
            public int? RenderQueue;
            /// <summary>
            /// Original render queue
            /// </summary>
            [Key("RenderQueueOriginal")]
            public int? RenderQueueOriginal;

            /// <summary>
            /// Data storage class for shader data
            /// </summary>
            /// <param name="id">ID of the item</param>
            /// <param name="materialName">Name of the material</param>
            /// <param name="shaderName">Name of the shader</param>
            /// <param name="shaderNameOriginal">Name of the original shader</param>
            /// <param name="renderQueue">Render queue</param>
            /// <param name="renderQueueOriginal">Original render queue</param>
            public MaterialShader(int id, string materialName, string shaderName, string shaderNameOriginal, int? renderQueue, int? renderQueueOriginal)
            {
                ID = id;
                MaterialName = materialName.Replace("(Instance)", "").Trim();
                ShaderName = shaderName;
                ShaderNameOriginal = shaderNameOriginal;
                RenderQueue = renderQueue;
                RenderQueueOriginal = renderQueueOriginal;
            }
            /// <summary>
            /// Data storage class for shader data
            /// </summary>
            /// <param name="id">ID of the item</param>
            /// <param name="materialName">Name of the material</param>
            /// <param name="shaderName">Name of the shader</param>
            /// <param name="shaderNameOriginal">Name of the original shader</param>
            public MaterialShader(int id, string materialName, string shaderName, string shaderNameOriginal)
            {
                ID = id;
                MaterialName = materialName.Replace("(Instance)", "").Trim();
                ShaderName = shaderName;
                ShaderNameOriginal = shaderNameOriginal;
            }
            /// <summary>
            /// Data storage class for shader data
            /// </summary>
            /// <param name="id">ID of the item</param>
            /// <param name="materialName">Name of the material</param>
            /// <param name="renderQueue">Render queue</param>
            /// <param name="renderQueueOriginal">Original render queue</param>
            public MaterialShader(int id, string materialName, int renderQueue, int renderQueueOriginal)
            {
                ID = id;
                MaterialName = materialName.Replace("(Instance)", "").Trim();
                RenderQueue = renderQueue;
                RenderQueueOriginal = renderQueueOriginal;
            }

            /// <summary>
            /// Check if the shader name and render queue are both null. Safe to delete this data if true.
            /// </summary>
            /// <returns></returns>
            public bool NullCheck() => ShaderName.IsNullOrEmpty() && RenderQueue == null;
        }
    }
}
