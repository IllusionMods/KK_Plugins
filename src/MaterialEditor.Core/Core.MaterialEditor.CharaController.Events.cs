using ExtensibleSaveFormat;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using MaterialEditorAPI;
using MessagePack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UniRx;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;
using static MaterialEditorAPI.MaterialEditorPluginBase;
#if !EC
using KKAPI.Studio;
#endif
#if AI || HS2
using AIChara;
#endif
#if PH
using ChaFileCoordinate = Character.CustomParameter;
using ChaControl = Human;
#endif
namespace KK_Plugins.MaterialEditor
{
    using MEAnimationController = MEAnimationController<MaterialEditorCharaController, MaterialEditorCharaController.MaterialTextureProperty>;

    public partial class MaterialEditorCharaController
    {
        internal void ClothesStateChangeEvent()
        {
            if (CoordinateChanging) return;
            if (MakerAPI.InsideMaker) return;

            ChaControl.StartCoroutine(LoadData(true, false, false));
        }

#if KK || KKS
        internal void CoordinateChangedEvent()
        {
            //In H if a coordinate is loaded the data will be overwritten. When switching coordinates the ExtSave data must be reloaded to restore the original.
            if (KKAPI.MainGame.GameAPI.InsideHScene)
                LoadCharacterExtSaveData();

            ChaControl.StartCoroutine(LoadData(true, true, false));

            if (MakerAPI.InsideAndLoaded)
                MaterialEditorUI.Visible = false;
        }

        internal void ClothingCopiedEvent(int copySource, int copyDestination, List<int> copySlots)
        {
            for (var i = 0; i < copySlots.Count; i++)
            {
                int slot = copySlots[i];
                MaterialShaderList.RemoveAll(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == copyDestination && x.Slot == slot);
                RendererPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == copyDestination && x.Slot == slot);
                MaterialNamePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == copyDestination && x.Slot == slot);
                MaterialFloatPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == copyDestination && x.Slot == slot);
                MaterialColorPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == copyDestination && x.Slot == slot);
                MaterialTexturePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == copyDestination && x.Slot == slot);
                MaterialCopyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == copyDestination && x.Slot == slot);

                List<MaterialShader> newAccessoryMaterialShaderList = new List<MaterialShader>();
                List<RendererProperty> newAccessoryRendererPropertyList = new List<RendererProperty>();
                List<MaterialNameProperty> newAccessoryMaterialNamePropertyList = new List<MaterialNameProperty>();
                List<MaterialFloatProperty> newAccessoryMaterialFloatPropertyList = new List<MaterialFloatProperty>();
                List<MaterialColorProperty> newAccessoryMaterialColorPropertyList = new List<MaterialColorProperty>();
                List<MaterialTextureProperty> newAccessoryMaterialTexturePropertyList = new List<MaterialTextureProperty>();
                List<MaterialCopy> newMaterialCopyList = new List<MaterialCopy>();

                foreach (var property in MaterialShaderList.Where(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == copySource && x.Slot == slot))
                    newAccessoryMaterialShaderList.Add(new MaterialShader(property.ObjectType, copyDestination, slot, property.MaterialName, property.ShaderName, property.ShaderNameOriginal, property.RenderQueue, property.RenderQueueOriginal));
                foreach (var property in RendererPropertyList.Where(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == copySource && x.Slot == slot))
                    newAccessoryRendererPropertyList.Add(new RendererProperty(property.ObjectType, copyDestination, slot, property.RendererName, property.Property, property.Value, property.ValueOriginal));
                foreach (var property in MaterialNamePropertyList.Where(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == copySource && x.Slot == slot))
                    newAccessoryMaterialNamePropertyList.Add(new MaterialNameProperty(property.ObjectType, copyDestination, slot, property.Renderer, property.MaterialName, property.Value));
                foreach (var property in MaterialFloatPropertyList.Where(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == copySource && x.Slot == slot))
                    newAccessoryMaterialFloatPropertyList.Add(new MaterialFloatProperty(property.ObjectType, copyDestination, slot, property.MaterialName, property.Property, property.Value, property.ValueOriginal));
                foreach (var property in MaterialColorPropertyList.Where(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == copySource && x.Slot == slot))
                    newAccessoryMaterialColorPropertyList.Add(new MaterialColorProperty(property.ObjectType, copyDestination, slot, property.MaterialName, property.Property, property.Value, property.ValueOriginal));
                foreach (var property in MaterialTexturePropertyList.Where(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == copySource && x.Slot == slot))
                    newAccessoryMaterialTexturePropertyList.Add(new MaterialTextureProperty(property.ObjectType, copyDestination, slot, property.MaterialName, property.Property, property.TexID, property.Offset, property.OffsetOriginal, property.Scale, property.ScaleOriginal, property.TexAnimationDef));
                foreach (var property in MaterialCopyList.Where(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == copySource && x.Slot == slot))
                    newMaterialCopyList.Add(new MaterialCopy(property.ObjectType, copyDestination, slot, property.MaterialName, property.MaterialCopyName));

                MaterialShaderList.AddRange(newAccessoryMaterialShaderList);
                RendererPropertyList.AddRange(newAccessoryRendererPropertyList);
                MaterialNamePropertyList.AddRange(newAccessoryMaterialNamePropertyList);
                MaterialFloatPropertyList.AddRange(newAccessoryMaterialFloatPropertyList);
                MaterialColorPropertyList.AddRange(newAccessoryMaterialColorPropertyList);
                MaterialTexturePropertyList.AddRange(newAccessoryMaterialTexturePropertyList);
                MaterialCopyList.AddRange(newMaterialCopyList);

                if (copyDestination == CurrentCoordinateIndex)
                    MaterialEditorUI.Visible = false;

                ChaControl.StartCoroutine(LoadData(true, true, false));
            }

            PurgeUnusedAnimation();
        }
#endif

        internal void AccessoryKindChangeEvent(object sender, AccessorySlotEventArgs e)
        {
            if (AccessorySelectedSlotChanging) return;
            if (CoordinateChanging) return;

            //User switched accessories, remove all edited properties for this slot
            MaterialShaderList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SlotIndex);
            RendererPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SlotIndex);
            MaterialNamePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SlotIndex);
            MaterialFloatPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SlotIndex);
            MaterialColorPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SlotIndex);
            MaterialTexturePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SlotIndex);
            MaterialCopyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SlotIndex);

            if (MakerAPI.InsideAndLoaded)
                if (MaterialEditorUI.Visible && MEMaker.Instance != null)
                    MEMaker.Instance.UpdateUIAccessory();

#if KK || EC || KKS
            if (MaterialEditorPlugin.RimRemover.Value)
                RemoveRimAccessory(e.SlotIndex);
#endif

            PurgeUnusedAnimation();
        }

        internal void AccessorySelectedSlotChangeEvent(object sender, AccessorySlotEventArgs e)
        {
            if (!MakerAPI.InsideAndLoaded) return;

            AccessorySelectedSlotChanging = true;

#if KK || EC || KKS
            if (MakerAPI.InsideAndLoaded)
                if (MaterialEditorUI.Visible && MEMaker.Instance != null)
                    MEMaker.Instance.UpdateUIAccessory();
#else
            ChaControl.StartCoroutine(LoadData(false, true, false));
            ChaControl.StartCoroutine(RefreshUI());
            IEnumerator RefreshUI()
            {
                yield return null;
                if (MakerAPI.InsideAndLoaded)
                    if (MaterialEditorUI.Visible && MEMaker.Instance != null)
                        MEMaker.Instance.UpdateUIAccessory();
            }
#endif
        }

        internal void AccessoryTransferredEvent(object sender, AccessoryTransferEventArgs e)
        {
            MaterialShaderList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.DestinationSlotIndex);
            RendererPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.DestinationSlotIndex);
            MaterialNamePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.DestinationSlotIndex);
            MaterialFloatPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.DestinationSlotIndex);
            MaterialKeywordPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.DestinationSlotIndex);
            MaterialColorPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.DestinationSlotIndex);
            MaterialTexturePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.DestinationSlotIndex);
            MaterialCopyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.DestinationSlotIndex);

            List<MaterialShader> newAccessoryMaterialShaderList = new List<MaterialShader>();
            List<RendererProperty> newAccessoryRendererPropertyList = new List<RendererProperty>();
            List<MaterialNameProperty> newAccessoryMaterialNamePropertyList = new List<MaterialNameProperty>();
            List<MaterialFloatProperty> newAccessoryMaterialFloatPropertyList = new List<MaterialFloatProperty>();
            List<MaterialKeywordProperty> newAccessoryMaterialKeywordPropertyList = new List<MaterialKeywordProperty>();
            List<MaterialColorProperty> newAccessoryMaterialColorPropertyList = new List<MaterialColorProperty>();
            List<MaterialTextureProperty> newAccessoryMaterialTexturePropertyList = new List<MaterialTextureProperty>();
            List<MaterialCopy> newAccessoryMaterialCopyList = new List<MaterialCopy>();

            foreach (var property in MaterialShaderList.Where(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SourceSlotIndex))
                newAccessoryMaterialShaderList.Add(new MaterialShader(property.ObjectType, CurrentCoordinateIndex, e.DestinationSlotIndex, property.MaterialName, property.ShaderName, property.ShaderNameOriginal, property.RenderQueue, property.RenderQueueOriginal));
            foreach (var property in RendererPropertyList.Where(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SourceSlotIndex))
                newAccessoryRendererPropertyList.Add(new RendererProperty(property.ObjectType, CurrentCoordinateIndex, e.DestinationSlotIndex, property.RendererName, property.Property, property.Value, property.ValueOriginal));
            foreach (var property in MaterialNamePropertyList.Where(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SourceSlotIndex))
                newAccessoryMaterialNamePropertyList.Add(new MaterialNameProperty(property.ObjectType, CurrentCoordinateIndex, e.DestinationSlotIndex, property.Renderer, property.MaterialName, property.Value));
            foreach (var property in MaterialFloatPropertyList.Where(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SourceSlotIndex))
                newAccessoryMaterialFloatPropertyList.Add(new MaterialFloatProperty(property.ObjectType, CurrentCoordinateIndex, e.DestinationSlotIndex, property.MaterialName, property.Property, property.Value, property.ValueOriginal));
            foreach (var property in MaterialKeywordPropertyList.Where(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SourceSlotIndex))
                newAccessoryMaterialKeywordPropertyList.Add(new MaterialKeywordProperty(property.ObjectType, CurrentCoordinateIndex, e.DestinationSlotIndex, property.MaterialName, property.Property, property.Value, property.ValueOriginal));
            foreach (var property in MaterialColorPropertyList.Where(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SourceSlotIndex))
                newAccessoryMaterialColorPropertyList.Add(new MaterialColorProperty(property.ObjectType, CurrentCoordinateIndex, e.DestinationSlotIndex, property.MaterialName, property.Property, property.Value, property.ValueOriginal));
            foreach (var property in MaterialTexturePropertyList.Where(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SourceSlotIndex))
                newAccessoryMaterialTexturePropertyList.Add(new MaterialTextureProperty(property.ObjectType, CurrentCoordinateIndex, e.DestinationSlotIndex, property.MaterialName, property.Property, property.TexID, property.Offset, property.OffsetOriginal, property.Scale, property.ScaleOriginal, property.TexAnimationDef));
            foreach (var property in MaterialCopyList.Where(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SourceSlotIndex))
                newAccessoryMaterialCopyList.Add(new MaterialCopy(property.ObjectType, CurrentCoordinateIndex, e.DestinationSlotIndex, property.MaterialName, property.MaterialCopyName));

            MaterialShaderList.AddRange(newAccessoryMaterialShaderList);
            RendererPropertyList.AddRange(newAccessoryRendererPropertyList);
            MaterialNamePropertyList.AddRange(newAccessoryMaterialNamePropertyList);
            MaterialFloatPropertyList.AddRange(newAccessoryMaterialFloatPropertyList);
            MaterialKeywordPropertyList.AddRange(newAccessoryMaterialKeywordPropertyList);
            MaterialColorPropertyList.AddRange(newAccessoryMaterialColorPropertyList);
            MaterialTexturePropertyList.AddRange(newAccessoryMaterialTexturePropertyList);
            MaterialCopyList.AddRange(newAccessoryMaterialCopyList);

            if (MakerAPI.InsideAndLoaded)
                MaterialEditorUI.Visible = false;

            PurgeUnusedAnimation();

            ChaControl.StartCoroutine(LoadData(true, true, false));
        }

#if KK || KKS
        internal void AccessoriesCopiedEvent(object sender, AccessoryCopyEventArgs e)
        {
            foreach (int slot in e.CopiedSlotIndexes)
            {
                MaterialShaderList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == (int)e.CopyDestination && x.Slot == slot);
                RendererPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == (int)e.CopyDestination && x.Slot == slot);
                MaterialNamePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == (int)e.CopyDestination && x.Slot == slot);
                MaterialFloatPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == (int)e.CopyDestination && x.Slot == slot);
                MaterialColorPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == (int)e.CopyDestination && x.Slot == slot);
                MaterialTexturePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == (int)e.CopyDestination && x.Slot == slot);
                MaterialCopyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == (int)e.CopyDestination && x.Slot == slot);

                List<MaterialShader> newAccessoryMaterialShaderList = new List<MaterialShader>();
                List<RendererProperty> newAccessoryRendererPropertyList = new List<RendererProperty>();
                List<MaterialNameProperty> newAccessoryMaterialNamePropertyList = new List<MaterialNameProperty>();
                List<MaterialFloatProperty> newAccessoryMaterialFloatPropertyList = new List<MaterialFloatProperty>();
                List<MaterialColorProperty> newAccessoryMaterialColorPropertyList = new List<MaterialColorProperty>();
                List<MaterialTextureProperty> newAccessoryMaterialTexturePropertyList = new List<MaterialTextureProperty>();
                List<MaterialCopy> newAccessoryMaterialCopyList = new List<MaterialCopy>();

                foreach (var property in MaterialShaderList.Where(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == (int)e.CopySource && x.Slot == slot))
                    newAccessoryMaterialShaderList.Add(new MaterialShader(property.ObjectType, (int)e.CopyDestination, slot, property.MaterialName, property.ShaderName, property.ShaderNameOriginal, property.RenderQueue, property.RenderQueueOriginal));
                foreach (var property in RendererPropertyList.Where(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == (int)e.CopySource && x.Slot == slot))
                    newAccessoryRendererPropertyList.Add(new RendererProperty(property.ObjectType, (int)e.CopyDestination, slot, property.RendererName, property.Property, property.Value, property.ValueOriginal));
                foreach (var property in MaterialNamePropertyList.Where(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == (int)e.CopySource && x.Slot == slot))
                    newAccessoryMaterialNamePropertyList.Add(new MaterialNameProperty(property.ObjectType, (int)e.CopyDestination, slot, property.Renderer, property.MaterialName, property.Value));
                foreach (var property in MaterialFloatPropertyList.Where(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == (int)e.CopySource && x.Slot == slot))
                    newAccessoryMaterialFloatPropertyList.Add(new MaterialFloatProperty(property.ObjectType, (int)e.CopyDestination, slot, property.MaterialName, property.Property, property.Value, property.ValueOriginal));
                foreach (var property in MaterialColorPropertyList.Where(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == (int)e.CopySource && x.Slot == slot))
                    newAccessoryMaterialColorPropertyList.Add(new MaterialColorProperty(property.ObjectType, (int)e.CopyDestination, slot, property.MaterialName, property.Property, property.Value, property.ValueOriginal));
                foreach (var property in MaterialTexturePropertyList.Where(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == (int)e.CopySource && x.Slot == slot))
                    newAccessoryMaterialTexturePropertyList.Add(new MaterialTextureProperty(property.ObjectType, (int)e.CopyDestination, slot, property.MaterialName, property.Property, property.TexID, property.Offset, property.OffsetOriginal, property.Scale, property.ScaleOriginal, property.TexAnimationDef));
                foreach (var property in MaterialCopyList.Where(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == (int)e.CopySource && x.Slot == slot))
                    newAccessoryMaterialCopyList.Add(new MaterialCopy(property.ObjectType, (int)e.CopyDestination, slot, property.MaterialName, property.MaterialCopyName));

                MaterialShaderList.AddRange(newAccessoryMaterialShaderList);
                RendererPropertyList.AddRange(newAccessoryRendererPropertyList);
                MaterialNamePropertyList.AddRange(newAccessoryMaterialNamePropertyList);
                MaterialFloatPropertyList.AddRange(newAccessoryMaterialFloatPropertyList);
                MaterialColorPropertyList.AddRange(newAccessoryMaterialColorPropertyList);
                MaterialTexturePropertyList.AddRange(newAccessoryMaterialTexturePropertyList);
                MaterialCopyList.AddRange(newAccessoryMaterialCopyList);

                if (MakerAPI.InsideAndLoaded)
                    if ((int)e.CopyDestination == CurrentCoordinateIndex)
                        MaterialEditorUI.Visible = false;
            }

            PurgeUnusedAnimation();
        }
#endif

        internal void ChangeAccessoryEvent(int slot, int type)
        {
            if (MEMaker.Instance != null)
                MEMaker.ToggleButtonVisibility();

#if AI || HS2
            if (type != 350) return; //type 350 = no category, accessory being removed
#elif KK || EC || KKS
            if (type != 120) //type 120 = no category, accessory being removed
            {
                if (MaterialEditorPlugin.RimRemover.Value)
                    RemoveRimAccessory(slot);
                return;
            }
#endif
            if (!MakerAPI.InsideAndLoaded) return;
            if (CoordinateChanging) return;

            MaterialShaderList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);
            RendererPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);
            MaterialNamePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);
            MaterialFloatPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);
            MaterialKeywordPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);
            MaterialColorPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);
            MaterialTexturePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);
            MaterialCopyList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);

            if (MakerAPI.InsideAndLoaded)
                MaterialEditorUI.Visible = false;

            PurgeUnusedAnimation();
        }

        internal void ChangeCustomClothesEvent(int slot)
        {
            if (!MakerAPI.InsideAndLoaded) return;
            if (CoordinateChanging) return;
            if (ClothesChanging) return;
            if (CharacterLoading) return;
            if (RefreshingTextures) return;
            if (CustomClothesOverride) return;
            if (new System.Diagnostics.StackTrace().ToString().Contains("KoiClothesOverlayController"))
            {
                StartCoroutine(LoadData(true, false, false, false));
                RefreshingTextures = true;
                return;
            }

            ClothesChanging = true;

            MaterialShaderList.RemoveAll(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);
            RendererPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);
            MaterialNamePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);
            MaterialFloatPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);
            MaterialKeywordPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);
            MaterialColorPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);
            MaterialTexturePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);
            MaterialCopyList.RemoveAll(x => x.ObjectType == ObjectType.Clothing && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot);

            if (MakerAPI.InsideAndLoaded)
                MaterialEditorUI.Visible = false;

#if KK || EC || KKS
            if (MaterialEditorPlugin.RimRemover.Value)
                RemoveRimClothes(slot);
#elif PH
            //Reapply edits for other clothes since they will have been undone
            ChaControl.StartCoroutine(LoadData(true, true, false));
#endif

            PurgeUnusedAnimation();
        }

        internal void ChangeHairEvent(int slot)
        {
            if (!MakerAPI.InsideAndLoaded) return;
            if (CharacterLoading) return;

            MaterialShaderList.RemoveAll(x => x.ObjectType == ObjectType.Hair && x.Slot == slot);
            RendererPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Hair && x.Slot == slot);
            MaterialNamePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Hair && x.Slot == slot);
            MaterialFloatPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Hair && x.Slot == slot);
            MaterialKeywordPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Hair && x.Slot == slot);
            MaterialColorPropertyList.RemoveAll(x => x.ObjectType == ObjectType.Hair && x.Slot == slot);
            MaterialTexturePropertyList.RemoveAll(x => x.ObjectType == ObjectType.Hair && x.Slot == slot);
            MaterialCopyList.RemoveAll(x => x.ObjectType == ObjectType.Hair && x.Slot == slot);

            if (MakerAPI.InsideAndLoaded)
                MaterialEditorUI.Visible = false;

#if KK || EC || KKS
            if (MaterialEditorPlugin.RimRemover.Value)
                StartCoroutine(RemoveRimHairCo(slot));
#elif PH
            //Reapply edits for other hairs since they will have been undone
            ChaControl.StartCoroutine(LoadData(false, false, true));
#endif

            PurgeUnusedAnimation();
        }

        internal void HandleMaterialNameChange(int slot, ObjectType objectType, Renderer renderer, Material material, string value, GameObject go)
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
                int idx = GetCoordinateIndex(objectType);
                var shader = MaterialShaderList.Where(x => x.ObjectType == objectType && x.CoordinateIndex == idx && x.Slot == slot && x.MaterialName == material.NameFormatted()).ToList();
                var textures = MaterialTexturePropertyList.Where(x => x.ObjectType == objectType && x.CoordinateIndex == idx && x.Slot == slot && x.MaterialName == material.NameFormatted()).ToList();
                var colors = MaterialColorPropertyList.Where(x => x.ObjectType == objectType && x.CoordinateIndex == idx && x.Slot == slot && x.MaterialName == material.NameFormatted()).ToList();
                var floats = MaterialFloatPropertyList.Where(x => x.ObjectType == objectType && x.CoordinateIndex == idx && x.Slot == slot && x.MaterialName == material.NameFormatted()).ToList();
                var keywords = MaterialKeywordPropertyList.Where(x => x.ObjectType == objectType && x.CoordinateIndex == idx && x.Slot == slot && x.MaterialName == material.NameFormatted()).ToList();
                if (shader.Count == 1) MaterialShaderList.Add(new MaterialShader(objectType, idx, slot, value, shader[0].ShaderName, shader[0].ShaderNameOriginal, shader[0].RenderQueue, shader[0].RenderQueueOriginal));
                foreach (var tex in textures) MaterialTexturePropertyList.Add(new MaterialTextureProperty(objectType, idx, slot, value, tex.Property, tex.TexID, tex.Offset, tex.OffsetOriginal, tex.Scale, tex.ScaleOriginal, tex.TexAnimationDef));
                foreach (var col in colors) MaterialColorPropertyList.Add(new MaterialColorProperty(objectType, idx, slot, value, col.Property, col.Value, col.ValueOriginal));
                foreach (var _float in floats) MaterialFloatPropertyList.Add(new MaterialFloatProperty(objectType, idx, slot, value, _float.Property, _float.Value, _float.ValueOriginal));
                foreach (var kw in keywords) MaterialKeywordPropertyList.Add(new MaterialKeywordProperty(objectType, idx, slot, value, kw.Property, kw.Value, kw.ValueOriginal));
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
        /// Refresh the clothes MainTex, typically called after editing colors in the character maker
        /// </summary>
        public void RefreshClothesMainTex() => StartCoroutine(RefreshClothesMainTexCoroutine());
        private IEnumerator RefreshClothesMainTexCoroutine()
        {
            yield return new WaitForEndOfFrame();
            for (var i = 0; i < MaterialTexturePropertyList.Count; i++)
            {
                var property = MaterialTexturePropertyList[i];
                if (Instance.CheckBlacklist(property.MaterialName, property.Property))
                    continue;

                if (property.ObjectType != ObjectType.Clothing || property.CoordinateIndex != CurrentCoordinateIndex || property.Property != "MainTex")
                    continue;

                if (property.TexID != null)
                {
                    var tex = TextureDictionary[(int)property.TexID].Texture;
                    MaterialEditorPlugin.Instance.ConvertNormalMap(ref tex, property.Property);
                    SetTexture(FindGameObject(ObjectType.Clothing, property.Slot), property.MaterialName, property.Property, tex);
                }
            }
        }

        /// <summary>
        /// Sets the texture indicated by TexID to texture of Material indicated by TextureProperty
        /// </summary>
        /// <param name="go">GameObject to search for the renderer</param>
        /// <param name="textureProperty">TextureProperty with TexID to set for Material</param>
        /// <returns>True if the value was set, false if it could not be set</returns>
        private bool SetTextureWithProperty(GameObject go, MaterialTextureProperty textureProperty)
        {
            if (!textureProperty.TexID.HasValue || textureProperty.NullCheck())
                return false;

            int texID = textureProperty.TexID.Value;
            if (!TextureDictionary.TryGetValue(texID, out var container))
                return false;

            if (textureProperty.TexAnimationDef == null)
            {
                //Does not have animation

                AnimationControllerMap.Remove(textureProperty); //If have animation, delete it.

                var tex = container.Texture;
                MaterialEditorPlugin.Instance.ConvertNormalMap(ref tex, textureProperty.Property);
                return SetTexture(go, textureProperty.MaterialName, textureProperty.Property, tex);
            }
            else
            {
                if (AnimationControllerMap.TryGetValue(textureProperty, out var controller))
                {
                    controller.go = go;
                    if (textureProperty.TexAnimationDef != controller.def)
                        controller.Reset(textureProperty.TexAnimationDef);
                }
                else
                {
                    controller = new MEAnimationController(this, go, textureProperty.TexAnimationDef);
                    AnimationControllerMap[textureProperty] = controller;
                }

                controller.UpdateAnimation(textureProperty);
                return true;
            }
        }

        /// <summary>
        /// Refresh the body MainTex, typically called after editing colors in the character maker
        /// </summary>
        public void RefreshBodyMainTex() => StartCoroutine(RefreshBodyMainTexCoroutine());
        private IEnumerator RefreshBodyMainTexCoroutine()
        {
            yield return new WaitForEndOfFrame();

            for (var i = 0; i < MaterialTexturePropertyList.Count; i++)
            {
                var property = MaterialTexturePropertyList[i];
                if (Instance.CheckBlacklist(property.MaterialName, property.Property))
                    continue;

                if (property.ObjectType == ObjectType.Character && property.Property == "MainTex")
                    SetTextureWithProperty(ChaControl.gameObject, property);
            }
        }
        /// <summary>
        /// Reapply all edits to the body and face
        /// </summary>
        public void RefreshBodyEdits()
        {
            if (CharacterLoading) return;
            StartCoroutine(LoadData(false, false, false));
        }
    }
}
