using ChaCustom;
using ExtensibleSaveFormat;
using HarmonyLib;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using MessagePack;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KK_Plugins
{
    public partial class HairAccessoryCustomizer
    {
        public class HairAccessoryController : CharaCustomFunctionController
        {
            private Dictionary<int, Dictionary<int, HairAccessoryInfo>> HairAccessories = new Dictionary<int, Dictionary<int, HairAccessoryInfo>>();
#if KK
            public int CurrentCoordinateIndex => ChaControl.fileStatus.coordinateType;
#elif EC
            public int CurrentCoordinateIndex => 0;
#endif

            protected override void OnCardBeingSaved(GameMode currentGameMode)
            {
                var data = new PluginData();
                data.data.Add("HairAccessories", MessagePackSerializer.Serialize(HairAccessories));
                SetExtendedData(data);
            }
            protected override void OnReload(GameMode currentGameMode, bool maintainState) => ChaControl.StartCoroutine(LoadData());
            protected override void OnCoordinateBeingSaved(ChaFileCoordinate coordinate)
            {
                var data = new PluginData();
                if (HairAccessories.TryGetValue(CurrentCoordinateIndex, out var hairAccessoryInfo))
                    if (hairAccessoryInfo.Count > 0)
                        data.data.Add("CoordinateHairAccessories", MessagePackSerializer.Serialize(hairAccessoryInfo));
                    else
                        data.data.Add("CoordinateHairAccessories", null);
                SetCoordinateExtendedData(coordinate, data);
            }
            protected override void OnCoordinateBeingLoaded(ChaFileCoordinate coordinate, bool maintainState) => ChaControl.StartCoroutine(LoadCoordinateData(coordinate));
            /// <summary>
            /// Wait one frame for accessories to load and then load the data.
            /// </summary>
            private IEnumerator LoadData()
            {
                ReloadingChara = true;
                yield return null;

                if (MakerAPI.InsideAndLoaded && !MakerAPI.GetCharacterLoadFlags().Clothes) yield break;

                HairAccessories.Clear();

                var data = GetExtendedData();
                if (data != null)
                    if (data.data.TryGetValue("HairAccessories", out var loadedHairAccessories) && loadedHairAccessories != null)
                        HairAccessories = MessagePackSerializer.Deserialize<Dictionary<int, Dictionary<int, HairAccessoryInfo>>>((byte[])loadedHairAccessories);

                if (MakerAPI.InsideAndLoaded)
                {
                    if (InitHairAccessoryInfo(AccessoriesApi.SelectedMakerAccSlot))
                    {
                        //switching to a hair accessory that previously had no data. Meaning this card was made before this plugin. ColorMatch and HairGloss should be off.
                        SetColorMatch(false);
                        SetHairGloss(false);
                    }

                    InitCurrentSlot(this);
                }

                UpdateAccessories();
                ReloadingChara = false;
            }
            /// <summary>
            /// Wait one frame for accessories to load and then load the data.
            /// </summary>
            private IEnumerator LoadCoordinateData(ChaFileCoordinate coordinate)
            {
                ReloadingChara = true;
                yield return null;

                var loadFlags = MakerAPI.GetCoordinateLoadFlags();
                if (loadFlags == null || loadFlags.Accessories)
                {
                    var data = GetCoordinateExtendedData(coordinate);
                    if (data != null && data.data.TryGetValue("CoordinateHairAccessories", out var loadedHairAccessories) && loadedHairAccessories != null)
                    {
                        if (HairAccessories.ContainsKey(CurrentCoordinateIndex))
                            HairAccessories[CurrentCoordinateIndex].Clear();
                        else
                            HairAccessories[CurrentCoordinateIndex] = new Dictionary<int, HairAccessoryInfo>();

                        HairAccessories[CurrentCoordinateIndex] = MessagePackSerializer.Deserialize<Dictionary<int, HairAccessoryInfo>>((byte[])loadedHairAccessories);
                    }

                    if (MakerAPI.InsideAndLoaded)
                    {
                        if (InitHairAccessoryInfo(AccessoriesApi.SelectedMakerAccSlot))
                        {
                            //switching to a hair accessory that previously had no data. Meaning this card was made before this plugin. ColorMatch and HairGloss should be off.
                            SetColorMatch(false);
                            SetHairGloss(false);
                        }

                        InitCurrentSlot(this);
                    }
                }

                UpdateAccessories();
                ReloadingChara = false;
            }
            /// <summary>
            /// Get color match data for the specified accessory or default if the accessory does not exist or is not a hair accessory
            /// </summary>
            public bool GetColorMatch(int slot)
            {
                if (HairAccessories.ContainsKey(CurrentCoordinateIndex) && HairAccessories[CurrentCoordinateIndex].TryGetValue(slot, out var hairAccessoryInfo))
                    return hairAccessoryInfo.ColorMatch;

                return ColorMatchDefault;
            }
            /// <summary>
            /// Get color match data for the current accessory or default if the accessory does not exist or is not a hair accessory
            /// </summary>
            public bool GetColorMatch() => GetColorMatch(AccessoriesApi.SelectedMakerAccSlot);
            /// <summary>
            /// Get hair gloss data for the specified accessory or default if the accessory does not exist or is not a hair accessory
            /// </summary>
            public bool GetHairGloss(int slot)
            {
                if (HairAccessories.ContainsKey(CurrentCoordinateIndex) && HairAccessories[CurrentCoordinateIndex].TryGetValue(slot, out var hairAccessoryInfo))
                    return hairAccessoryInfo.HairGloss;

                return HairGlossDefault;
            }
            /// <summary>
            /// Get hair gloss data for the current accessory or default if the accessory does not exist or is not a hair accessory
            /// </summary>
            public bool GetHairGloss() => GetHairGloss(AccessoriesApi.SelectedMakerAccSlot);
            /// <summary>
            /// Get outline color data for the specified accessory or default if the accessory does not exist or is not a hair accessory
            /// </summary>
            public Color GetOutlineColor(int slot)
            {
                if (HairAccessories.ContainsKey(CurrentCoordinateIndex) && HairAccessories[CurrentCoordinateIndex].TryGetValue(slot, out var hairAccessoryInfo))
                    return hairAccessoryInfo.OutlineColor;

                return OutlineColorDefault;
            }
            /// <summary>
            /// Get outline color data for the current accessory or default if the accessory does not exist or is not a hair accessory
            /// </summary>
            public Color GetOutlineColor() => GetOutlineColor(AccessoriesApi.SelectedMakerAccSlot);
            /// <summary>
            /// Get accessory color data for the specified accessory or default if the accessory does not exist or is not a hair accessory
            /// </summary>
            public Color GetAccessoryColor(int slot)
            {
                if (HairAccessories.ContainsKey(CurrentCoordinateIndex) && HairAccessories[CurrentCoordinateIndex].TryGetValue(slot, out var hairAccessoryInfo))
                    return hairAccessoryInfo.AccessoryColor;

                return AccessoryColorDefault;
            }
            /// <summary>
            /// Get accessory color data for the current accessory or default if the accessory does not exist or is not a hair accessory
            /// </summary>
            public Color GetAccessoryColor() => GetAccessoryColor(AccessoriesApi.SelectedMakerAccSlot);
            /// <summary>
            /// Get hair length data for the specified accessory or default if the accessory does not exist or is not a hair accessory
            /// </summary>
            public float GetHairLength(int slot)
            {
                if (HairAccessories.ContainsKey(CurrentCoordinateIndex) && HairAccessories[CurrentCoordinateIndex].TryGetValue(slot, out var hairAccessoryInfo))
                    return hairAccessoryInfo.HairLength;

                return HairLengthDefault;
            }
            /// <summary>
            /// Get hair length data for the current accessory or default if the accessory does not exist or is not a hair accessory
            /// </summary>
            public float GetHairLength() => GetHairLength(AccessoriesApi.SelectedMakerAccSlot);
            /// <summary>
            /// Initializes the HairAccessoryInfo for the slot if it is a hair accessory, or removes it if it is not.
            /// </summary>
            /// <returns>True if HairAccessoryInfo was initialized</returns>
            public bool InitHairAccessoryInfo(int slot)
            {
                if (IsHairAccessory(slot))
                {
                    if (!HairAccessories.ContainsKey(CurrentCoordinateIndex))
                        HairAccessories[CurrentCoordinateIndex] = new Dictionary<int, HairAccessoryInfo>();

                    if (!HairAccessories[CurrentCoordinateIndex].ContainsKey(slot))
                    {
                        HairAccessories[CurrentCoordinateIndex][slot] = new HairAccessoryInfo();
                        return true;
                    }
                    return false;
                }
                RemoveHairAccessoryInfo(slot);
                return false;
            }
            /// <summary>
            /// Removes the HairAccessoryInfo for the slot
            /// </summary>
            public void RemoveHairAccessoryInfo(int slot)
            {
                if (HairAccessories.ContainsKey(CurrentCoordinateIndex))
                    HairAccessories[CurrentCoordinateIndex].Remove(slot);
            }
            /// <summary>
            /// Set color match for the specified accessory
            /// </summary>
            public void SetColorMatch(bool value, int slot)
            {
                if (MakerAPI.InsideAndLoaded && HairAccessories.ContainsKey(CurrentCoordinateIndex) && IsHairAccessory(slot))
                    HairAccessories[CurrentCoordinateIndex][slot].ColorMatch = value;
            }
            /// <summary>
            /// Set color match for the current accessory
            /// </summary>
            public void SetColorMatch(bool value) => SetColorMatch(value, AccessoriesApi.SelectedMakerAccSlot);
            /// <summary>
            /// Set hair gloss for the specified accessory
            /// </summary>
            public void SetHairGloss(bool value, int slot)
            {
                if (MakerAPI.InsideAndLoaded && HairAccessories.ContainsKey(CurrentCoordinateIndex) && IsHairAccessory(slot))
                    HairAccessories[CurrentCoordinateIndex][slot].HairGloss = value;
            }
            /// <summary>
            /// Set hair gloss for the specified accessory
            /// </summary>
            public void SetHairGloss(bool value) => SetHairGloss(value, AccessoriesApi.SelectedMakerAccSlot);
            /// <summary>
            /// Set outline color for the specified accessory
            /// </summary>
            public void SetOutlineColor(Color value, int slot)
            {
                if (MakerAPI.InsideAndLoaded && HairAccessories.ContainsKey(CurrentCoordinateIndex) && IsHairAccessory(slot))
                    HairAccessories[CurrentCoordinateIndex][slot].OutlineColor = value;
            }
            /// <summary>
            /// Set outline color for the current accessory
            /// </summary>
            public void SetOutlineColor(Color value) => SetOutlineColor(value, AccessoriesApi.SelectedMakerAccSlot);
            /// <summary>
            /// Set accessory color for the specified accessory
            /// </summary>
            public void SetAccessoryColor(Color value, int slot)
            {
                if (MakerAPI.InsideAndLoaded && HairAccessories.ContainsKey(CurrentCoordinateIndex) && IsHairAccessory(slot))
                    HairAccessories[CurrentCoordinateIndex][slot].AccessoryColor = value;
            }
            /// <summary>
            /// Set accessory color for the current accessory
            /// </summary>
            public void SetHairLength(float value) => SetHairLength(value, AccessoriesApi.SelectedMakerAccSlot);
            /// <summary>
            /// Set hair length for the specified accessory
            /// </summary>
            public void SetHairLength(float value, int slot)
            {
                if (MakerAPI.InsideAndLoaded && HairAccessories.ContainsKey(CurrentCoordinateIndex) && IsHairAccessory(slot))
                    HairAccessories[CurrentCoordinateIndex][slot].HairLength = value;
            }
            /// <summary>
            /// Set hair length for the current accessory
            /// </summary>
            public void SetAccessoryColor(Color value) => SetAccessoryColor(value, AccessoriesApi.SelectedMakerAccSlot);
            /// <summary>
            /// Checks if the specified accessory is a hair accessory
            /// </summary>
            public bool IsHairAccessory(ChaAccessoryComponent chaAccessoryComponent) => chaAccessoryComponent != null && chaAccessoryComponent.gameObject.GetComponent<ChaCustomHairComponent>() != null;
            /// <summary>
            /// Checks if the specified accessory is a hair accessory
            /// </summary>
            public bool IsHairAccessory(int slot)
            {
                try
                {
                    var accessory = ChaControl.GetAccessoryObject(slot);
                    if (accessory == null)
                        return false;
                    return accessory.GetComponent<ChaCustomHairComponent>() != null;
                }
                catch
                {
                    return false;
                }
            }
            /// <summary>
            /// Checks if the specified accessory is a hair accessory and has accessory parts (rendAccessory in the ChaCustomHairComponent MonoBehavior)
            /// </summary>
            public bool HasAccessoryPart()
            {
                var accessory = ChaControl.GetAccessoryObject(AccessoriesApi.SelectedMakerAccSlot);
                if (accessory == null)
                    return false;
                var chaCustomHairComponent = accessory.GetComponent<ChaCustomHairComponent>();
                if (chaCustomHairComponent != null)
                    for (var i = 0; i < chaCustomHairComponent.rendAccessory.Length; i++)
                        if (chaCustomHairComponent.rendAccessory[i] != null)
                            return true;
                return false;
            }
            /// <summary>
            /// Checks if the specified accessory has length transforms (trfLength in the ChaCustomHairComponent MonoBehavior)
            /// </summary>
            public bool HasLengthTransforms()
            {
                var accessory = ChaControl.GetAccessoryObject(AccessoriesApi.SelectedMakerAccSlot);
                if (accessory == null)
                    return false;
                var chaCustomHairComponent = accessory.GetComponent<ChaCustomHairComponent>();
                if (chaCustomHairComponent != null)
                    for (var i = 0; i < chaCustomHairComponent.trfLength.Length; i++)
                        if (chaCustomHairComponent.trfLength[i] != null)
                            return true;
                return false;
            }
#if KK
            internal void CopyAccessoriesHandler(AccessoryCopyEventArgs e)
            {
                if (!HairAccessories.ContainsKey((int)e.CopySource))
                    HairAccessories[(int)e.CopySource] = new Dictionary<int, HairAccessoryInfo>();
                if (!HairAccessories.ContainsKey((int)e.CopyDestination))
                    HairAccessories[(int)e.CopyDestination] = new Dictionary<int, HairAccessoryInfo>();

                foreach (int x in e.CopiedSlotIndexes)
                {
                    if (HairAccessories[(int)e.CopySource].TryGetValue(x, out var hairAccessoryInfo))
                    {
                        //copy hair accessory info to the destination coordinate and slot
                        var newHairAccessoryInfo = new HairAccessoryInfo();
                        newHairAccessoryInfo.ColorMatch = hairAccessoryInfo.ColorMatch;
                        newHairAccessoryInfo.HairGloss = hairAccessoryInfo.HairGloss;
                        newHairAccessoryInfo.OutlineColor = hairAccessoryInfo.OutlineColor;
                        newHairAccessoryInfo.AccessoryColor = hairAccessoryInfo.AccessoryColor;
                        newHairAccessoryInfo.HairLength = hairAccessoryInfo.HairLength;
                        HairAccessories[(int)e.CopyDestination][x] = newHairAccessoryInfo;
                    }
                    else
                        //not a hair accessory, remove hair accessory info from the destination slot
                        HairAccessories[(int)e.CopyDestination].Remove(x);
                }
            }
#endif
            internal void TransferAccessoriesHandler(AccessoryTransferEventArgs e)
            {
                if (!HairAccessories.ContainsKey(CurrentCoordinateIndex)) return;

                if (HairAccessories[CurrentCoordinateIndex].TryGetValue(e.SourceSlotIndex, out var hairAccessoryInfo))
                {
                    //copy hair accessory info to the destination slot
                    var newHairAccessoryInfo = new HairAccessoryInfo();
                    newHairAccessoryInfo.ColorMatch = hairAccessoryInfo.ColorMatch;
                    newHairAccessoryInfo.HairGloss = hairAccessoryInfo.HairGloss;
                    newHairAccessoryInfo.OutlineColor = hairAccessoryInfo.OutlineColor;
                    newHairAccessoryInfo.AccessoryColor = hairAccessoryInfo.AccessoryColor;
                    newHairAccessoryInfo.HairLength = hairAccessoryInfo.HairLength;
                    HairAccessories[CurrentCoordinateIndex][e.DestinationSlotIndex] = newHairAccessoryInfo;

                    if (AccessoriesApi.SelectedMakerAccSlot == e.DestinationSlotIndex)
                        InitCurrentSlot(this, true);
                }
                else
                {
                    //not a hair accessory, remove hair accessory info from the destination slot
                    HairAccessories[CurrentCoordinateIndex].Remove(e.DestinationSlotIndex);
                    InitCurrentSlot(this, false);
                }

                UpdateAccessories();
            }
            /// <summary>
            /// Updates all the hair accessories
            /// </summary>
            public void UpdateAccessories(bool updateHairInfo = true)
            {
                if (HairAccessories.ContainsKey(CurrentCoordinateIndex))
                    foreach (var x in HairAccessories[CurrentCoordinateIndex])
                        UpdateAccessory(x.Key, updateHairInfo);
            }
            /// <summary>
            /// Updates the specified hair accessory
            /// </summary>
            public void UpdateAccessory(int slot, bool updateCharacter = true)
            {
                if (!IsHairAccessory(slot)) return;

                var acc = ChaControl.GetAccessoryObject(slot);
                if (acc == null) return;
                ChaAccessoryComponent chaAccessoryComponent = acc.GetComponent<ChaAccessoryComponent>();
                if (chaAccessoryComponent == null) return;
                ChaCustomHairComponent chaCustomHairComponent = chaAccessoryComponent.gameObject.GetComponent<ChaCustomHairComponent>();
                if (chaCustomHairComponent.rendHair == null) return;

                if (!HairAccessories.ContainsKey(CurrentCoordinateIndex)) return;
                if (!HairAccessories[CurrentCoordinateIndex].TryGetValue(slot, out var hairAccessoryInfo)) return;
                if (chaAccessoryComponent.rendNormal == null) return;
                if (chaCustomHairComponent.rendHair == null) return;

                if (updateCharacter && hairAccessoryInfo.ColorMatch)
                {
                    if (MakerAPI.InsideAndLoaded)
                    {
                        CvsAccessory cvsAccessory = AccessoriesApi.GetMakerAccessoryPageObject(slot).GetComponent<CvsAccessory>();
                        cvsAccessory.UpdateAcsColor01(ChaControl.chaFile.custom.hair.parts[0].baseColor);
                        cvsAccessory.UpdateAcsColor02(ChaControl.chaFile.custom.hair.parts[0].startColor);
                        cvsAccessory.UpdateAcsColor03(ChaControl.chaFile.custom.hair.parts[0].endColor);
                        OutlineColorPicker.SetValue(slot, ChaControl.chaFile.custom.hair.parts[0].outlineColor, false);
                        hairAccessoryInfo.OutlineColor = ChaControl.chaFile.custom.hair.parts[0].outlineColor;
                    }
                    else
                    {
                        for (var i = 0; i < chaCustomHairComponent.rendHair.Length; i++)
                        {
                            Renderer renderer = chaCustomHairComponent.rendHair[i];
                            if (renderer == null) continue;

                            if (renderer.sharedMaterial.HasProperty(ChaShader._Color))
                                renderer.sharedMaterial.SetColor(ChaShader._Color, ChaControl.chaFile.custom.hair.parts[0].baseColor);
                            if (renderer.sharedMaterial.HasProperty(ChaShader._Color2))
                                renderer.sharedMaterial.SetColor(ChaShader._Color2, ChaControl.chaFile.custom.hair.parts[0].startColor);
                            if (renderer.sharedMaterial.HasProperty(ChaShader._Color3))
                                renderer.sharedMaterial.SetColor(ChaShader._Color3, ChaControl.chaFile.custom.hair.parts[0].endColor);
                        }
                    }
                }

                Texture2D texHairGloss = (Texture2D)AccessTools.Property(typeof(ChaControl), "texHairGloss").GetValue(ChaControl, null);

                for (var i = 0; i < chaCustomHairComponent.rendHair.Length; i++)
                {
                    Renderer renderer = chaCustomHairComponent.rendHair[i];
                    if (renderer == null) continue;

                    if (renderer.sharedMaterial.HasProperty(ChaShader._HairGloss))
                    {
                        if (hairAccessoryInfo.HairGloss)
                            renderer.sharedMaterial.SetTexture(ChaShader._HairGloss, texHairGloss);
                        else
                            renderer.sharedMaterial.SetTexture(ChaShader._HairGloss, null);
                    }

                    if (renderer.sharedMaterial.HasProperty(ChaShader._LineColor))
                        if (hairAccessoryInfo.ColorMatch)
                            renderer.sharedMaterial.SetColor(ChaShader._LineColor, ChaControl.chaFile.custom.hair.parts[0].outlineColor);
                        else
                            renderer.sharedMaterial.SetColor(ChaShader._LineColor, hairAccessoryInfo.OutlineColor);
                }

                for (var i = 0; i < chaCustomHairComponent.rendAccessory.Length; i++)
                {
                    Renderer renderer = chaCustomHairComponent.rendAccessory[i];
                    if (renderer == null) continue;

                    if (renderer.sharedMaterial.HasProperty(ChaShader._Color))
                        renderer.sharedMaterial.SetColor(ChaShader._Color, hairAccessoryInfo.AccessoryColor);
                    if (renderer.sharedMaterial.HasProperty(ChaShader._Color2))
                        renderer.sharedMaterial.SetColor(ChaShader._Color2, hairAccessoryInfo.AccessoryColor);
                    if (renderer.sharedMaterial.HasProperty(ChaShader._Color3))
                        renderer.sharedMaterial.SetColor(ChaShader._Color3, hairAccessoryInfo.AccessoryColor);
                }

                chaCustomHairComponent.lengthRate = hairAccessoryInfo.HairLength;
            }

            [Serializable]
            [MessagePackObject]
            private class HairAccessoryInfo
            {
                [Key("HairGloss")]
                public bool HairGloss = ColorMatchDefault;
                [Key("ColorMatch")]
                public bool ColorMatch = HairGlossDefault;
                [Key("OutlineColor")]
                public Color OutlineColor = OutlineColorDefault;
                [Key("AccessoryColor")]
                public Color AccessoryColor = AccessoryColorDefault;
                [Key("HairLength")]
                public float HairLength = HairLengthDefault;
            }
        }
    }
}
