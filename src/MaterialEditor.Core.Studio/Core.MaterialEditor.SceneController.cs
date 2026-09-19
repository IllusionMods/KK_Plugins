using ExtensibleSaveFormat;
using KKAPI;
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
#if AI || HS2
using AIChara;
#endif
#if PH
using ChaControl = Human;
#endif

namespace KK_Plugins.MaterialEditor
{
    using MEAnimationController = MEAnimationController<SceneController, SceneController.MaterialTextureProperty>;

    /// <summary>
    /// KKAPI scene controller which provides access for getting and setting properties to be saved and loaded with the scene data
    /// </summary>
    public partial class SceneController : SceneCustomFunctionController
    {
        public const string TexDicSaveKey = nameof(TextureDictionary);

        private readonly List<RendererProperty> RendererPropertyList = new List<RendererProperty>();
        private readonly List<ProjectorProperty> ProjectorPropertyList = new List<ProjectorProperty>();
        private readonly List<MaterialNameProperty> MaterialNamePropertyList = new List<MaterialNameProperty>();
        private readonly List<MaterialFloatProperty> MaterialFloatPropertyList = new List<MaterialFloatProperty>();
        private readonly List<MaterialKeywordProperty> MaterialKeywordPropertyList = new List<MaterialKeywordProperty>();
        private readonly List<MaterialColorProperty> MaterialColorPropertyList = new List<MaterialColorProperty>();
        private readonly List<MaterialVectorProperty> MaterialVectorPropertyList = new List<MaterialVectorProperty>();
        internal readonly List<MaterialTextureProperty> MaterialTexturePropertyList = new List<MaterialTextureProperty>();
        internal readonly List<MaterialCubemapProperty> MaterialCubemapPropertyList = new List<MaterialCubemapProperty>();
        private readonly List<MaterialShader> MaterialShaderList = new List<MaterialShader>();
        private readonly List<MaterialCopy> MaterialCopyList = new List<MaterialCopy>();

        private readonly Dictionary<MaterialTextureProperty, MEAnimationController> AnimationControllerMap = new Dictionary<MaterialTextureProperty, MEAnimationController>();

        internal static Dictionary<int, TextureContainer> TextureDictionary = new Dictionary<int, TextureContainer>();

        private static readonly MaterialEditorCubemapLeaseStore CubemapLeases =
            new MaterialEditorCubemapLeaseStore();

        private readonly MaterialEditRequestQueue _textureImports = new MaterialEditRequestQueue();


        static SceneController()
        {
            InitAnimationController();
        }

        private void OnDisable() => _textureImports.CancelAll();

        /// <summary>
        /// Saves data
        /// </summary>
        protected override void OnSceneSave()
        {
            RemoveLegacyMaterialVectorDuplicates();
            var data = new PluginData { version = 1 };

            PurgeUnusedTextures();

            TextureSaveHandler.Instance.Save(data, TexDicSaveKey, TextureDictionary, false);

            WriteRecords(nameof(RendererPropertyList), RendererPropertyList);
            WriteRecords(nameof(ProjectorPropertyList), ProjectorPropertyList);
            WriteRecords(nameof(MaterialNamePropertyList), MaterialNamePropertyList);
            WriteRecords(nameof(MaterialFloatPropertyList), MaterialFloatPropertyList);
            WriteRecords(nameof(MaterialKeywordPropertyList), MaterialKeywordPropertyList);
            WriteRecords(nameof(MaterialColorPropertyList), MaterialColorPropertyList);
            WriteRecords(nameof(MaterialVectorPropertyList), MaterialVectorPropertyList);
            WriteRecords(nameof(MaterialTexturePropertyList), MaterialTexturePropertyList);
            WriteRecords(nameof(MaterialCubemapPropertyList), MaterialCubemapPropertyList);
            WriteRecords(nameof(MaterialShaderList), MaterialShaderList);
            WriteRecords(nameof(MaterialCopyList), MaterialCopyList);

            SetExtendedData(data);

            void WriteRecords<T>(string key, List<T> records)
            {
                // Keep empty fields present as null in the saved data.
                data.data.Add(key,
                    records.Count > 0 ? MessagePackSerializer.Serialize(records) : null);
            }
        }

        /// <summary>
        /// Purge unused textures from TextureDictionary
        /// </summary>
        protected void PurgeUnusedTextures()
        {
            PurgeUnusedCubemapLeases();
            if (TextureDictionary.Count <= 0)
                return;

            HashSet<int> unuseds = new HashSet<int>(TextureDictionary.Keys);

            //Remove textures in use
            for (int i = 0; i < MaterialTexturePropertyList.Count; ++i)
            {
                var prop = MaterialTexturePropertyList[i];
                var texID = prop.TexID;
                if (texID.HasValue)
                    unuseds.Remove(texID.Value);

                if (prop.TexAnimationDef != null)
                {
                    var frames = prop.TexAnimationDef.frames;
                    for (int j = 0; j < frames.Length; ++j)
                        unuseds.Remove(frames[j].texID);
                }
            }

            for (var i = 0; i < MaterialCubemapPropertyList.Count; i++)
            {
                var texID = MaterialCubemapPropertyList[i].TexID;
                if (texID.HasValue)
                    unuseds.Remove(texID.Value);
            }

            //Remove textures in use
            unuseds.RemoveWhere(texId => TimelineCompatibilityHelper.GetUsedTextureIds().Contains(texId));

            foreach (var texID in unuseds)
            {
                CubemapLeases.Release(texID);
                TextureDictionary[texID].Dispose();
                TextureDictionary.Remove(texID);
            }
        }

        private static bool TryGetCubemap(int texID, out Cubemap cubemap, out string error)
        {
            TextureContainer container;
            if (!TextureDictionary.TryGetValue(texID, out container))
            {
                cubemap = null;
                error = "The Cubemap texture data is missing.";
                return false;
            }
            return CubemapLeases.TryAcquire(texID, container.Data, out cubemap, out error);
        }

        private void PurgeUnusedCubemapLeases()
        {
            CubemapLeases.Purge(MaterialCubemapPropertyList
                .Where(x => x.TexID.HasValue)
                .Select(x => x.TexID.Value));
        }

        private static void DisposeTextureDictionary()
        {
            TextureSaveHandler.DisposeTextureContainers(TextureDictionary);
        }

        /// <summary>
        /// Return GameObject from ObjectCtrlInfo ID
        /// </summary>
        /// <param name="items"></param>
        /// <param name="id"></param>
        /// <returns>GameObject with OCI</returns>
        protected static GameObject ExtractGameObject(ReadOnlyDictionary<int, ObjectCtrlInfo> items, int id, out int objectId)
        {
            if (!items.TryGetValue(id, out ObjectCtrlInfo objectCtrlInfo) || objectCtrlInfo == null || !(objectCtrlInfo is OCIItem ociItem))
            {
                objectId = -1;
                return null;
            }

            objectId = MEStudio.GetObjectID(ociItem);
            return ociItem.objectItem;
        }

        /// <summary>
        /// Loads saved data
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="loadedItems"></param>
        protected override void OnSceneLoad(SceneOperationKind operation, ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems)
        {
            _textureImports.CancelAll();
            if (operation == SceneOperationKind.Clear
                || operation == SceneOperationKind.Load)
            {
                MEStudio.Instance?.ReleaseItemTypeDropdownTarget();
                MaterialEditorUI.InvalidateAllTargetState();
            }
            var data = GetExtendedData();

            if (operation == SceneOperationKind.Clear || operation == SceneOperationKind.Load)
            {
                RendererPropertyList.Clear();
                MaterialNamePropertyList.Clear();
                MaterialFloatPropertyList.Clear();
                MaterialKeywordPropertyList.Clear();
                MaterialColorPropertyList.Clear();
                MaterialVectorPropertyList.Clear();
                MaterialTexturePropertyList.Clear();
                MaterialCubemapPropertyList.Clear();
                MaterialShaderList.Clear();
                CubemapLeases.DisposeAll();
                DisposeTextureDictionary();
                MaterialCopyList.Clear();
                AnimationControllerMap.Clear();
            }

            if (data == null) return;
            if (operation == SceneOperationKind.Clear) return;

            var importDictionary = new Dictionary<int, int>();

            if (operation == SceneOperationKind.Load)
            {
                TextureDictionary = TextureSaveHandler.Instance.Load<Dictionary<int, TextureContainer>>(data, TexDicSaveKey, false);
            }

            if (operation == SceneOperationKind.Import)
            {
                var importDictionaryTemp = TextureSaveHandler.Instance.Load<Dictionary<int, TextureContainer>>(data, TexDicSaveKey, false);
                try
                {
                    importDictionary = MaterialEditLoadContext.ImportTextures(importDictionaryTemp, x => x.Data, SetAndGetTextureID);
                }
                finally
                {
                    TextureSaveHandler.DisposeTextureContainers(importDictionaryTemp);
                }
            }

            var context = new SceneLoadContext
            {
                Data = new MaterialEditLoadContext(data, importDictionary),
                Operation = operation,
                Items = loadedItems
            };
            LoadSceneMaterialCopyList(context);


            LoadSceneMaterialNamePropertyList(context);

            LoadSceneMaterialShaderList(context);

            LoadSceneRendererPropertyList(context);

            LoadSceneProjectorPropertyList(context);

            LoadSceneMaterialFloatPropertyList(context);

            LoadSceneMaterialKeywordPropertyList(context);

            LoadSceneMaterialColorPropertyList(context);

            LoadSceneMaterialVectorPropertyList(context);

            LoadSceneMaterialTexturePropertyList(context);

            LoadSceneMaterialCubemapPropertyList(context);

            if (data.version < 1)
            {
                FixDuplicatesInKeywordList();
            }
        }
        private void FixDuplicatesInKeywordList()
        {
            // Clean up scenes saved in buggy versions of ME that duplicated keyword props on 
            // scene loads, causing massive file sizes. `.First()` should always keep the latest user edit.
            var fixedKeywordList = MaterialKeywordPropertyList
                .GroupBy(d => new { d.ID, d.MaterialName, d.Property })
                .Select(f => f.First())
                .ToArray();
            MaterialKeywordPropertyList.Clear();
            MaterialKeywordPropertyList.Capacity = 0;

            foreach (MaterialKeywordProperty materialKeywordProperty in fixedKeywordList)
            {
                MaterialKeywordPropertyList.Add(materialKeywordProperty);
            }
        }

        /// <summary>
        /// Handles copying data when objects are copied
        /// </summary>
        /// <param name="copiedItems"></param>
        protected override void OnObjectsCopied(ReadOnlyDictionary<int, ObjectCtrlInfo> copiedItems)
        {

            var batch = new SceneCopyBatch();
            foreach (var copiedItem in copiedItems)
            {
                if (copiedItem.Value is OCIItem ociItem)
                {
                    var context = new SceneCopyContext
                    {
                        SourceId = copiedItem.Key,
                        DestinationId = copiedItem.Value.GetSceneId(),
                        Root = ociItem.objectItem,
                        SourceRoot = GetObjectByID(copiedItem.Key),
                        Output = batch
                    };
                    CopyMaterialCopyList(context);

                    CopyMaterialNamePropertyList(context);

                    CopyMaterialShaderList(context);

                    CopyRendererPropertyList(context);

                    CopyProjectorPropertyList(context);

                    CopyMaterialFloatPropertyList(context);

                    CopyMaterialKeywordPropertyList(context);

                    CopyMaterialColorPropertyList(context);

                    CopyMaterialVectorPropertyList(context);

                    CopyMaterialTexturePropertyList(context);

                    CopyMaterialCubemapPropertyList(context);
                }
                if (copiedItem.Value is OCIChar ociChar)
                {
                    var chaCtrl = ociChar.charInfo
#if PH
                        .human
#endif
                        ;

                    MaterialEditorPlugin.GetCharaController(chaCtrl).DuplicatingFrom = copiedItem.Key;
                }
            }

            CommitCopiedProperties(batch);
        }

        private void Update()
        {
            if (MaterialEditorPlugin.PasteEditsHotkey.Value.IsDown())
            {
                if (!CopyData.IsEmpty)
                {
                    int count = 0;
                    TreeNodeObject[] selectNodes = Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNodes;
                    for (int i = 0; i < selectNodes.Length; i++)
                        PasteEditsRecursive(selectNodes[i], ref count);
                    if (count > 0)
                        MaterialEditorPlugin.Logger.LogMessage($"Pasted edits for {count} items");
                }
            }

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
            else if (MaterialEditorPlugin.TwoSidedShadowCastingHotkey.Value.IsDown())
            {
                int count = 0;
                TreeNodeObject[] selectNodes = Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNodes;
                for (int i = 0; i < selectNodes.Length; i++)
                    SetRendererPropertyRecursive(selectNodes[i], RendererProperties.ShadowCastingMode, "2", ref count);
                if (count > 0)
                    MaterialEditorPlugin.Logger.LogMessage($"Two Sided ShadowCasting for {count} items");
            }
            else if (MaterialEditorPlugin.ShadowsOnlyShadowCastingHotkey.Value.IsDown())
            {
                int count = 0;
                TreeNodeObject[] selectNodes = Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNodes;
                for (int i = 0; i < selectNodes.Length; i++)
                    SetRendererPropertyRecursive(selectNodes[i], RendererProperties.ShadowCastingMode, "3", ref count);
                if (count > 0)
                    MaterialEditorPlugin.Logger.LogMessage($"Shadows Only ShadowCasting for {count} items");
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
            _textureImports.Pump();

            MEAnimationController.UpdateAnimations(AnimationControllerMap);
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
                    controller.SetRendererPropertyRecursive(property, value, true);
                }
            foreach (var child in node.child)
                SetRendererPropertyRecursive(child, property, value, ref count);
        }

        private void PasteEditsRecursive(TreeNodeObject node, ref int count)
        {
            if (Studio.Studio.Instance.dicInfo.TryGetValue(node, out ObjectCtrlInfo objectCtrlInfo))
                if (objectCtrlInfo is OCIItem ociItem)
                {
                    count++;
                    foreach (var rend in GetRendererList(ociItem.objectItem))
                    {
                        foreach (var mat in GetMaterials(ociItem.objectItem, rend))
                        {
                            MaterialPasteEdits(ociItem.objectInfo.dicKey, mat);
                        }
                    }
                    foreach (var projector in GetProjectorList(ociItem.objectItem))
                        MaterialPasteEdits(ociItem.objectInfo.dicKey, projector.material);
                }
            foreach (var child in node.child)
                PasteEditsRecursive(child, ref count);
        }

        protected override void OnObjectDeleted(ObjectCtrlInfo objectCtrlInfo)
        {
            if (objectCtrlInfo is OCIItem item)
            {
                var id = item.GetSceneId();
                if (MaterialEditorUI.RetainedTargetData is int currentId
                    && currentId == id)
                    MaterialEditorUI.InvalidateCurrentTarget();
                else
                    MaterialEditorUI.NotifyTargetDestroyed(item.objectItem);
                RendererPropertyList.RemoveAll(x => x.ID == id);
                ProjectorPropertyList.RemoveAll(x => x.ID == id);
                MaterialNamePropertyList.RemoveAll(x => x.ID == id);
                MaterialFloatPropertyList.RemoveAll(x => x.ID == id);
                MaterialKeywordPropertyList.RemoveAll(x => x.ID == id);
                MaterialColorPropertyList.RemoveAll(x => x.ID == id);
                MaterialVectorPropertyList.RemoveAll(x => x.ID == id);
                MaterialTexturePropertyList.RemoveAll(x => x.ID == id);
                MaterialCubemapPropertyList.RemoveAll(x => x.ID == id);
                MaterialShaderList.RemoveAll(x => x.ID == id);
                MaterialCopyList.RemoveAll(x => x.ID == id);
                MaterialEditorUI.Visible = false;
            }
            else if (objectCtrlInfo is OCIChar character)
            {
                ChaControl targetControl = null;
                GameObject targetRoot = null;
                try
                {
                    targetControl = character.GetChaControl();
                    if (!ReferenceEquals(targetControl, null))
                        targetRoot = targetControl.gameObject;
                    else if (!ReferenceEquals(character.charInfo, null))
                        targetRoot = character.charInfo.gameObject;
                }
                catch (MissingReferenceException)
                {
                    // Exact control identity remains usable by the dropdown.
                }

                MEStudio.Instance?.ReleaseItemTypeDropdownTarget(targetControl);
                var targetInvalidated =
                    MaterialEditorUI.NotifyTargetDestroyed(targetRoot);
                var retainedTarget =
                    MaterialEditorUI.RetainedTargetGameObject;
                if (!targetInvalidated
                    && !ReferenceEquals(retainedTarget, null)
                    && retainedTarget == null)
                    MaterialEditorUI.InvalidateCurrentTarget();
                MaterialEditorUI.Visible = false;
            }
            base.OnObjectDeleted(objectCtrlInfo);
            PurgeUnusedAnimation();
        }

        protected override void OnObjectVisibilityToggled(ObjectCtrlInfo objectCtrlInfo, bool visible)
        {
            if (visible && objectCtrlInfo is OCIItem item)
            {
                var id = item.GetSceneId();
                foreach (var property in RendererPropertyList.Where(x => x.ID == id && x.Property == RendererProperties.Enabled))
                {
                    MaterialAPI.SetRendererProperty(GetObjectByID(id), property.RendererName, property.Property, property.Value);
                    // potential recalc of normals, have to test...
                }
            }
            base.OnObjectVisibilityToggled(objectCtrlInfo, visible);
        }

        protected override void OnObjectsSelected(List<ObjectCtrlInfo> objectCtrlInfo)
        {
            if (MaterialEditorUI.Visible)
                MEStudio.Instance.UpdateUI();
            base.OnObjectsSelected(objectCtrlInfo);
        }

        internal void HandleMaterialNameChange(int id, Renderer renderer, Material material, string value, GameObject go)
        {
            value = value.FormatShadingObjectName();

            // Check for an existing material on the renderer by the same name
            // Also check if we're renaming a copied material, and find the actual material being renamed
            Material existing = null;
            Material copiedOriginalMat = null;
            foreach (var rend in GetRendererList(go))
            {
                foreach (var mat in GetMaterials(go, rend))
                {
                    if (mat.NameFormatted() == value)
                    {
                        if (rend == renderer) return;
                        existing = mat;
                    }
                    else if (material.name.Contains(MaterialCopyPostfix) && rend == renderer && mat.NameFormatted() == material.NameFormatted())
                    {
                        copiedOriginalMat = mat;
                    }
                }
            }

            if (existing == null)
            {
                foreach (var legacyProperty in MaterialColorPropertyList.Where(x => x.ID == id && x.MaterialName == material.NameFormatted()).ToList())
                    MigrateLegacyMaterialVectorProperty(id, legacyProperty.MaterialName, legacyProperty.Property, go);

                var shader = MaterialShaderList.Where(x => x.ID == id && x.MaterialName == material.NameFormatted()).ToList();
                var textures = MaterialTexturePropertyList.Where(x => x.ID == id && x.MaterialName == material.NameFormatted()).ToList();
                var cubemaps = MaterialCubemapPropertyList.Where(x => x.ID == id && x.MaterialName == material.NameFormatted()).ToList();
                var colors = MaterialColorPropertyList.Where(x => x.ID == id && x.MaterialName == material.NameFormatted()).ToList();
                var vectors = MaterialVectorPropertyList.Where(x => x.ID == id && x.MaterialName == material.NameFormatted()).ToList();
                var floats = MaterialFloatPropertyList.Where(x => x.ID == id && x.MaterialName == material.NameFormatted()).ToList();
                var keywords = MaterialKeywordPropertyList.Where(x => x.ID == id && x.MaterialName == material.NameFormatted()).ToList();
                if (shader.Count == 1) MaterialShaderList.Add(new MaterialShader(id, value, shader[0].ShaderName, shader[0].ShaderNameOriginal, shader[0].RenderQueue, shader[0].RenderQueueOriginal));
                foreach (var tex in textures)
                    MaterialTexturePropertyList.Add(new MaterialTextureProperty(id, value, tex.Property, tex.TexID, tex.Offset, tex.OffsetOriginal, tex.Scale, tex.ScaleOriginal, tex.TexAnimationDef));
                foreach (var cubemap in cubemaps)
                {
                    var renamedProperty = new MaterialCubemapProperty(
                        id,
                        value,
                        cubemap.Property,
                        cubemap.TexID);
                    renamedProperty.InheritCubemapOriginalSnapshotSameMaterials(
                        cubemap,
                        go);
                    MaterialCubemapPropertyList.Add(renamedProperty);
                }
                foreach (var col in colors) MaterialColorPropertyList.Add(new MaterialColorProperty(id, value, col.Property, col.Value, col.ValueOriginal));
                foreach (var vector in vectors) MaterialVectorPropertyList.Add(new MaterialVectorProperty(id, value, vector.Property, vector.Value, vector.ValueOriginal));
                foreach (var _float in floats) MaterialFloatPropertyList.Add(new MaterialFloatProperty(id, value, _float.Property, _float.Value, _float.ValueOriginal));
                foreach (var kw in keywords) MaterialKeywordPropertyList.Add(new MaterialKeywordProperty(id, value, kw.Property, kw.Value, kw.ValueOriginal));
            }
            else if (!material.name.Contains(MaterialCopyPostfix))
            {
                material.shader = existing.shader;
                material.shaderKeywords = existing.shaderKeywords;
                material.color = existing.color;
                material.mainTexture = existing.mainTexture;
                material.mainTextureOffset = existing.mainTextureOffset;
                material.mainTextureScale = existing.mainTextureScale;
                material.renderQueue = existing.renderQueue;
            }
            else if (copiedOriginalMat != null)
            {
                copiedOriginalMat.shader = existing.shader;
                copiedOriginalMat.shaderKeywords = existing.shaderKeywords;
                copiedOriginalMat.color = existing.color;
                copiedOriginalMat.mainTexture = existing.mainTexture;
                copiedOriginalMat.mainTextureOffset = existing.mainTextureOffset;
                copiedOriginalMat.mainTextureScale = existing.mainTextureScale;
                copiedOriginalMat.renderQueue = existing.renderQueue;
            }
        }

        /// <summary>
        /// Finds the texture bytes in the dictionary of textures and returns its ID. If not found, adds the texture to the dictionary and returns the ID of the added texture.
        /// </summary>
        internal static int SetAndGetTextureID(byte[] textureBytes)
        {
            return TextureSaveHandler.GetOrAddTexture(TextureDictionary, textureBytes);
        }


        /// <summary>
        /// Finds the texture in the dictionary of textures by its ID. Returns null if not found.
        /// </summary>
        internal static Texture GetTextureByDictionaryID(int id)
        {
            TextureDictionary.TryGetValue(id, out TextureContainer textureContainer);
            if (textureContainer != null) return textureContainer.Texture;
            return null;
        }

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
        /// Purge unused animation
        /// </summary>
        private void PurgeUnusedAnimation()
        {
            PurgeUnusedCubemapLeases();
            MEAnimationUtil.PurgeUnusedAnimation(AnimationControllerMap, MaterialTexturePropertyList);
        }

        /// <summary>
        /// Initialization of animation controllers
        /// </summary>
        static void InitAnimationController()
        {
            MEAnimationController.TryUpdateTexture = SetTextureForAnimation;
            MEAnimationController.GetTexID = GetTexIDWithAnimation;
        }

        /// <summary>
        /// Get texture ID from MaterialTextureProperty
        /// </summary>
        static int? GetTexIDWithAnimation(MaterialTextureProperty property)
        {
            // This delegate also supplies the used-texture set for scene
            // persistence. Animation remains disabled by SetTextureForAnimation.
            return property.TexID;
        }

        /// <summary>
        /// Set of textures for animation
        /// </summary>
        static bool SetTextureForAnimation(SceneController controller, GameObject go, MaterialTextureProperty property, int texID)
        {
            if (!TextureDictionary.TryGetValue(texID, out var tex))
                return false;

            return SetTexture(go, property.MaterialName, property.Property, tex.Texture);
        }

    }
}
