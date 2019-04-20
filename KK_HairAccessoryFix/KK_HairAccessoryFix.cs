using BepInEx;
using ExtensibleSaveFormat;
using Harmony;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using MessagePack;
using System;
using System.Collections.Generic;
using UnityEngine;
using BepInEx.Logging;
using System.Collections;
using Logger = BepInEx.Logger;
/// <summary>
/// Match color, outline color, and hair gloss for hair accessories
/// </summary>
namespace KK_HairAccessoryFix
{
    [BepInPlugin(GUID, PluginName, Version)]
    public class KK_HairAccessoryFix : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.hairaccessoryfix";
        public const string PluginName = "Hair Accessory Fix";
        public const string PluginNameInternal = "KK_HairAccessoryFix";
        public const string Version = "1.1";

        private static bool DoEvents = true;
        private static AccessoryControlWrapper<MakerToggle, bool> ColorMatchToggle;
        private static AccessoryControlWrapper<MakerToggle, bool> HairGlossToggle;
        private static AccessoryControlWrapper<MakerColor, Color> OutlineColorPicker;
        private static AccessoryControlWrapper<MakerColor, Color> AccessoryColorPicker;

        private static readonly bool ColorMatchDefault = true;
        private static readonly bool HairGlossDefault = true;
        private static Color OutlineColorDefault = Color.black;
        private static Color AccessoryColorDefault = Color.red;

        private void Main()
        {
            var harmony = HarmonyInstance.Create(GUID);
            harmony.PatchAll(typeof(KK_HairAccessoryFix));

            MakerAPI.MakerBaseLoaded += MakerAPI_MakerBaseLoaded;
            CharacterApi.RegisterExtraBehaviour<HairAccessoryController>(GUID);
        }

        private void MakerAPI_MakerBaseLoaded(object s, RegisterCustomControlsEvent e)
        {
            var controller = GetController(MakerAPI.GetCharacterControl());

            ColorMatchToggle = new AccessoryControlWrapper<MakerToggle, bool>(MakerAPI.AddAccessoryWindowControl(new MakerToggle(null, "Color Match", ColorMatchDefault, this)));
            HairGlossToggle = new AccessoryControlWrapper<MakerToggle, bool>(MakerAPI.AddAccessoryWindowControl(new MakerToggle(null, "Hair Gloss", ColorMatchDefault, this)));
            OutlineColorPicker = new AccessoryControlWrapper<MakerColor, Color>(MakerAPI.AddAccessoryWindowControl(new MakerColor("Outline Color", false, null, OutlineColorDefault, this)));
            OutlineColorPicker.Control.ColorBoxWidth = 230;
            AccessoryColorPicker = new AccessoryControlWrapper<MakerColor, Color>(MakerAPI.AddAccessoryWindowControl(new MakerColor("Accessory Color", false, null, OutlineColorDefault, this)));
            AccessoryColorPicker.Control.ColorBoxWidth = 230;

            //Color Match
            ColorMatchToggle.ValueChanged += ColorMatchToggle_ValueChanged;
            void ColorMatchToggle_ValueChanged(object sender, AccessoryWindowControlValueChangedEventArgs<bool> eventArgs)
            {
                if (DoEvents)
                    controller.SetColorMatch(eventArgs.SlotIndex, eventArgs.NewValue);
                OutlineColorPicker.Control.Visible.OnNext(!eventArgs.NewValue);
            }

            ColorMatchToggle.VisibleIndexChanged += ColorMatchToggle_VisibleIndexChanged;
            void ColorMatchToggle_VisibleIndexChanged(object sender, AccessorySlotEventArgs eventArgs)
            {
                if (DoEvents)
                    ColorMatchToggle.SetSelectedValue(controller.GetColorMatch(eventArgs.SlotIndex));
                ColorMatchToggle.Control.Visible.OnNext(controller.IsHairAccessory(eventArgs.SlotIndex));
            }

            ColorMatchToggle.AccessoryKindChanged += ColorMatchToggle_AccessoryKindChanged;
            void ColorMatchToggle_AccessoryKindChanged(object sender, AccessorySlotEventArgs eventArgs)
            {
                ColorMatchToggle.Control.Visible.OnNext(controller.IsHairAccessory(eventArgs.SlotIndex));
                if (controller.InitHairAccessoryInfo(eventArgs.SlotIndex))
                {
                    if (DoEvents)
                        controller.UpdateAccessory(eventArgs.SlotIndex);
                }
                else
                    SetDefaults();
            }
            ColorMatchToggle.Control.Visible.OnNext(false);

            void SetDefaults()
            {
                DoEvents = false;
                ColorMatchToggle.Control.Value = ColorMatchDefault;
                HairGlossToggle.Control.Value = HairGlossDefault;
                OutlineColorPicker.Control.Value = OutlineColorDefault;
                AccessoryColorPicker.Control.Value = AccessoryColorDefault;
                DoEvents = true;
            }

            //Hair Gloss
            HairGlossToggle.ValueChanged += HairGlossToggle_ValueChanged;
            void HairGlossToggle_ValueChanged(object sender, AccessoryWindowControlValueChangedEventArgs<bool> eventArgs)
            {
                if (DoEvents)
                    controller.SetHairGloss(eventArgs.SlotIndex, eventArgs.NewValue);
            }

            HairGlossToggle.VisibleIndexChanged += HairGlossToggle_VisibleIndexChanged;
            void HairGlossToggle_VisibleIndexChanged(object sender, AccessorySlotEventArgs eventArgs)
            {
                if (DoEvents)
                    HairGlossToggle.SetSelectedValue(controller.GetHairGloss(eventArgs.SlotIndex));
                HairGlossToggle.Control.Visible.OnNext(controller.IsHairAccessory(eventArgs.SlotIndex));
            }

            HairGlossToggle.AccessoryKindChanged += HairGlossToggle_AccessoryKindChanged;
            void HairGlossToggle_AccessoryKindChanged(object sender, AccessorySlotEventArgs eventArgs)
            {
                HairGlossToggle.Control.Visible.OnNext(controller.IsHairAccessory(eventArgs.SlotIndex));
            }
            HairGlossToggle.Control.Visible.OnNext(false);

            //Outline Color
            OutlineColorPicker.ValueChanged += OutlineColorPicker_ValueChanged;
            void OutlineColorPicker_ValueChanged(object sender, AccessoryWindowControlValueChangedEventArgs<Color> eventArgs)
            {
                if (DoEvents)
                    controller.SetOutlineColor(eventArgs.SlotIndex, eventArgs.NewValue);
            }

            OutlineColorPicker.VisibleIndexChanged += OutlineColorPicker_VisibleIndexChanged;
            void OutlineColorPicker_VisibleIndexChanged(object sender, AccessorySlotEventArgs eventArgs)
            {
                if (DoEvents)
                    OutlineColorPicker.SetSelectedValue(controller.GetOutlineColor(eventArgs.SlotIndex));
                if (controller.IsHairAccessory(eventArgs.SlotIndex))
                    OutlineColorPicker.Control.Visible.OnNext(!ColorMatchToggle.Control.Value);
                else
                    OutlineColorPicker.Control.Visible.OnNext(false);
            }

            OutlineColorPicker.AccessoryKindChanged += OutlineColorPicker_AccessoryKindChanged;
            void OutlineColorPicker_AccessoryKindChanged(object sender, AccessorySlotEventArgs eventArgs)
            {
                if (controller.IsHairAccessory(eventArgs.SlotIndex))
                    OutlineColorPicker.Control.Visible.OnNext(!ColorMatchToggle.Control.Value);
                else
                    OutlineColorPicker.Control.Visible.OnNext(false);
            }
            OutlineColorPicker.Control.Visible.OnNext(false);

            //AccessoryColor
            AccessoryColorPicker.ValueChanged += AccessoryColorPicker_ValueChanged;
            void AccessoryColorPicker_ValueChanged(object sender, AccessoryWindowControlValueChangedEventArgs<Color> eventArgs)
            {
                controller.SetAccessoryColor(eventArgs.SlotIndex, eventArgs.NewValue);
            }

            AccessoryColorPicker.VisibleIndexChanged += AccessoryColorPicker_VisibleIndexChanged;
            void AccessoryColorPicker_VisibleIndexChanged(object sender, AccessorySlotEventArgs eventArgs)
            {
                if (DoEvents)
                    AccessoryColorPicker.SetSelectedValue(controller.GetAccessoryColor(eventArgs.SlotIndex));
                AccessoryColorPicker.Control.Visible.OnNext(controller.HasAccessoryPart(eventArgs.SlotIndex));
            }

            AccessoryColorPicker.AccessoryKindChanged += AccessoryColorPicker_AccessoryKindChanged;
            void AccessoryColorPicker_AccessoryKindChanged(object sender, AccessorySlotEventArgs eventArgs)
            {
                AccessoryColorPicker.Control.Visible.OnNext(controller.HasAccessoryPart(eventArgs.SlotIndex));
            }
            AccessoryColorPicker.Control.Visible.OnNext(false);
        }

        private void ColorMatchToggle_VisibleIndexChanged1(object sender, AccessorySlotEventArgs e) => throw new System.NotImplementedException();
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingHairGlossMask))]
        public static void ChangeSettingHairGlossMask(ChaControl __instance) => GetController(__instance).UpdateAccessories();
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingHairColor))]
        public static void ChangeSettingHairColor(ChaControl __instance) => GetController(__instance).UpdateAccessories();
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingHairOutlineColor))]
        public static void ChangeSettingHairOutlineColor(ChaControl __instance) => GetController(__instance).UpdateAccessories();
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingHairAcsColor))]
        public static void ChangeSettingHairAcsColor(ChaControl __instance) => GetController(__instance).UpdateAccessories();

        private static HairAccessoryController GetController(ChaControl character) => character?.gameObject?.GetComponent<HairAccessoryController>();

        public class HairAccessoryController : CharaCustomFunctionController
        {
            private Dictionary<int, HairAccessoryInfo> HairAccessories = new Dictionary<int, HairAccessoryInfo>();

            protected override void OnCardBeingSaved(GameMode currentGameMode)
            {
                var data = new PluginData();
                data.data.Add("HairAccessories", MessagePackSerializer.Serialize(HairAccessories));
                SetExtendedData(data);
            }
            protected override void OnReload(GameMode currentGameMode, bool maintainState)
            {
                base.OnReload(currentGameMode, maintainState);
                ChaControl.StartCoroutine(LoadData());
            }
            /// <summary>
            /// Wait one frame and then load the data. Accessories do not yet exist at the time OnReload triggers.
            /// </summary>
            /// <returns></returns>
            private IEnumerator LoadData()
            {
                DoEvents = false;
                yield return null;

                HairAccessories.Clear();

                var data = GetExtendedData();
                if (data == null)
                {
                    for (int i = 0; i < AccessoriesApi.GetCvsAccessoryCount(); i++)
                    {
                        if (InitHairAccessoryInfo(i))
                        {
                            HairAccessories[i].ColorMatch = false;
                            HairAccessories[i].HairGloss = false;
                            var chaCustomHairComponent = AccessoriesApi.GetAccessory(ChaControl, i)?.gameObject.GetComponent<ChaCustomHairComponent>();
                            if (chaCustomHairComponent != null)
                                foreach (Color color in chaCustomHairComponent.acsDefColor)
                                    HairAccessories[i].AccessoryColor = color;
                        }
                    }
                }
                else
                {
                    if (data.data.TryGetValue("HairAccessories", out var loadedHairAccessories) && loadedHairAccessories != null)
                        HairAccessories = MessagePackSerializer.Deserialize<Dictionary<int, HairAccessoryInfo>>((byte[])loadedHairAccessories);
                }

                if (MakerAPI.InsideAndLoaded)
                {
                    foreach (var acc in HairAccessories)
                    {
                        DoEvents = false;
                        ColorMatchToggle.SetValue(acc.Key, acc.Value.ColorMatch);
                        HairGlossToggle.SetValue(acc.Key, acc.Value.HairGloss);
                        OutlineColorPicker.SetValue(acc.Key, acc.Value.OutlineColor);
                        AccessoryColorPicker.SetValue(acc.Key, acc.Value.AccessoryColor);
                        DoEvents = true;
                    }
                }

                DoEvents = true;
                UpdateAccessories();
            }
            /// <summary>
            /// Get color match data for the specified accessory or default if the accessory does not exist or is not a hair accessory
            /// </summary>
            public bool GetColorMatch(int slot) => HairAccessories.TryGetValue(slot, out var hairAccessoryInfo) ? hairAccessoryInfo.ColorMatch : ColorMatchDefault;
            /// <summary>
            /// Get hair gloss data for the specified accessory or default if the accessory does not exist or is not a hair accessory
            /// </summary>
            public bool GetHairGloss(int slot) => HairAccessories.TryGetValue(slot, out var hairAccessoryInfo) ? hairAccessoryInfo.HairGloss : HairGlossDefault;
            /// <summary>
            /// Get outline color data for the specified accessory or default if the accessory does not exist or is not a hair accessory
            /// </summary>
            public Color GetOutlineColor(int slot) => HairAccessories.TryGetValue(slot, out var hairAccessoryInfo) ? hairAccessoryInfo.OutlineColor : OutlineColorDefault;
            /// <summary>
            /// Get accessory color data for the specified accessory or default if the accessory does not exist or is not a hair accessory
            /// </summary>
            public Color GetAccessoryColor(int slot) => HairAccessories.TryGetValue(slot, out var hairAccessoryInfo) ? hairAccessoryInfo.AccessoryColor : AccessoryColorDefault;
            /// <summary>
            /// Initializes the HairAccessoryInfo for the slot if it is a hair accessory, or removes it if it is not.
            /// Returns true if the accessory is a hair accessory so this method check, initialize, and remove HairAccessoryInfo. Use before getting or setting.
            /// </summary>
            /// <returns>True if the accessory is a hair accessory</returns>
            public bool InitHairAccessoryInfo(int slot)
            {
                if (IsHairAccessory(slot))
                {
                    if (!HairAccessories.TryGetValue(slot, out var hairAccessoryInfo))
                        HairAccessories[slot] = new HairAccessoryInfo();
                    return true;
                }
                else
                {
                    RemoveHairAccessoryInfo(slot);
                    return false;
                }
            }
            /// <summary>
            /// Removes the HairAccessoryInfo for the slot
            /// </summary>
            public void RemoveHairAccessoryInfo(int slot) => HairAccessories.Remove(slot);
            /// <summary>
            /// Set color match for the specified accessory
            /// </summary>
            public void SetColorMatch(int slot, bool value)
            {
                if (MakerAPI.InsideAndLoaded && IsHairAccessory(slot))
                {
                    HairAccessories[slot].ColorMatch = value;
                    UpdateAccessory(slot);
                }
            }
            /// <summary>
            /// Set hair gloss for the specified accessory
            /// </summary>
            public void SetHairGloss(int slot, bool value)
            {
                if (MakerAPI.InsideAndLoaded && IsHairAccessory(slot))
                {
                    HairAccessories[slot].HairGloss = value;
                    UpdateAccessory(slot);
                }
            }
            /// <summary>
            /// Set outline color for the specified accessory
            /// </summary>
            public void SetOutlineColor(int slot, Color value)
            {
                if (MakerAPI.InsideAndLoaded && IsHairAccessory(slot))
                {
                    HairAccessories[slot].OutlineColor = value;
                    UpdateAccessory(slot);
                }
            }
            /// <summary>
            /// Set accessory color for the specified accessory
            /// </summary>
            public void SetAccessoryColor(int slot, Color value)
            {
                if (MakerAPI.InsideAndLoaded && IsHairAccessory(slot))
                {
                    HairAccessories[slot].AccessoryColor = value;
                    UpdateAccessory(slot);
                }
            }
            /// <summary>
            /// Checks if the specified accessory is a hair accessory
            /// </summary>
            public bool IsHairAccessory(ChaAccessoryComponent chaAccessoryComponent) => chaAccessoryComponent?.gameObject.GetComponent<ChaCustomHairComponent>() != null;
            /// <summary>
            /// Checks if the specified accessory is a hair accessory
            /// </summary>
            public bool IsHairAccessory(int slot) => AccessoriesApi.GetAccessory(ChaControl, slot)?.gameObject.GetComponent<ChaCustomHairComponent>() != null;
            /// <summary>
            /// Checks if the specified accessory is a hair accessory and has accessory parts (rendAccessory in the ChaCustomHairComponent MonoBehavior)
            /// </summary>
            public bool HasAccessoryPart(int slot)
            {
                var chaCustomHairComponent = AccessoriesApi.GetAccessory(ChaControl, slot)?.gameObject.GetComponent<ChaCustomHairComponent>();
                if (chaCustomHairComponent != null)
                    foreach (Renderer renderer in chaCustomHairComponent.rendAccessory)
                        if (renderer != null) return true;
                return false;
            }
            /// <summary>
            /// Updates all the hair accessories
            /// </summary>
            public void UpdateAccessories()
            {
                foreach (var x in HairAccessories)
                    UpdateAccessory(x.Key);
            }
            /// <summary>
            /// Updates the specified hair accessory
            /// </summary>
            public void UpdateAccessory(int slot)
            {
                if (!IsHairAccessory(slot)) return;

                ChaAccessoryComponent chaAccessoryComponent = AccessoriesApi.GetAccessory(ChaControl, slot);
                ChaCustomHairComponent chaCustomHairComponent = chaAccessoryComponent?.gameObject.GetComponent<ChaCustomHairComponent>();

                if (!HairAccessories.TryGetValue(slot, out var hairAccessoryInfo)) return;
                if (chaAccessoryComponent?.rendNormal == null) return;
                if (chaCustomHairComponent?.rendHair == null) return;

                if (MakerAPI.InsideAndLoaded && hairAccessoryInfo.ColorMatch)
                {
                    DoEvents = false;
                    ChaCustom.CvsAccessory cvsAccessory = AccessoriesApi.GetCvsAccessory(slot);
                    cvsAccessory.UpdateAcsColor01(ChaControl.chaFile.custom.hair.parts[0].baseColor);
                    cvsAccessory.UpdateAcsColor02(ChaControl.chaFile.custom.hair.parts[0].startColor);
                    cvsAccessory.UpdateAcsColor03(ChaControl.chaFile.custom.hair.parts[0].endColor);
                    OutlineColorPicker.SetValue(slot, ChaControl.chaFile.custom.hair.parts[0].outlineColor);
                    hairAccessoryInfo.OutlineColor = ChaControl.chaFile.custom.hair.parts[0].outlineColor;
                    DoEvents = true;
                }

                Texture2D texHairGloss = (Texture2D)AccessTools.Property(typeof(ChaControl), "texHairGloss").GetValue(ChaControl, null);

                foreach (Renderer renderer in chaCustomHairComponent.rendHair)
                {
                    if (renderer == null) continue;

                    if (renderer.material.HasProperty(ChaShader._HairGloss))
                    {
                        var mt = renderer.material.GetTexture(ChaShader._MainTex);
                        if (hairAccessoryInfo.HairGloss)
                            renderer.material.SetTexture(ChaShader._HairGloss, texHairGloss);
                        else
                            renderer.material.SetTexture(ChaShader._HairGloss, null);
                        Destroy(mt);
                    }

                    if (renderer.material.HasProperty(ChaShader._LineColor))
                        if (hairAccessoryInfo.ColorMatch)
                            renderer.material.SetColor(ChaShader._LineColor, ChaControl.chaFile.custom.hair.parts[0].outlineColor);
                        else
                            renderer.material.SetColor(ChaShader._LineColor, hairAccessoryInfo.OutlineColor);
                }

                foreach (Renderer renderer in chaCustomHairComponent.rendAccessory)
                {
                    if (renderer == null) continue;

                    if (renderer.material.HasProperty("_Color"))
                        renderer.material.SetColor("_Color", hairAccessoryInfo.AccessoryColor);
                }
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
            }
        }
    }
}
