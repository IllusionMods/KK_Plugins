using ExtensibleSaveFormat;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using MessagePack;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KK_Plugins
{
    public partial class HairAccessoryCustomizer
    {
        private static void AccessoriesApi_AccessoryTransferred(object sender, AccessoryTransferEventArgs e) => GetController(MakerAPI.GetCharacterControl()).TransferAccessoriesHandler(e);
#if KK || KKS
        private static void AccessoriesApi_AccessoriesCopied(object sender, AccessoryCopyEventArgs e) => GetController(MakerAPI.GetCharacterControl()).CopyAccessoriesHandler(e);
#endif

        private static void AccessoriesApi_AccessoryKindChanged(object sender, AccessorySlotEventArgs e)
        {
            if (ReloadingChara) return;

            var controller = GetController(MakerAPI.GetCharacterControl());
            bool hairAcc = controller.IsHairAccessory(e.SlotIndex);

            controller.InitHairAccessoryInfo(e.SlotIndex);
            if (hairAcc)
            {
                InitCurrentSlot(controller, true);
                controller.UpdateAccessory(e.SlotIndex);
            }
            else
                InitCurrentSlot(controller, false);
        }

        private static void AccessoriesApi_SelectedMakerAccSlotChanged(object sender, AccessorySlotEventArgs e)
        {
            if (ReloadingChara) return;
            if (!MakerAPI.InsideAndLoaded) return;

            var controller = GetController(MakerAPI.GetCharacterControl());
            bool hairAcc = controller.IsHairAccessory(e.SlotIndex);
            bool didInit = controller.InitHairAccessoryInfo(e.SlotIndex);

            if (hairAcc)
            {
                if (didInit)
                {
                    //switching to a hair accessory that previously had no data. Meaning this card was made before this plugin. ColorMatch and HairGloss should be off.
                    controller.SetColorMatch(false, e.SlotIndex);
                    controller.SetHairGloss(false, e.SlotIndex);
                }

                InitCurrentSlot(controller, true);
                controller.UpdateAccessory(e.SlotIndex);
            }
            else
                InitCurrentSlot(controller, false);
        }

        private void MakerAPI_MakerBaseLoaded(object s, RegisterCustomControlsEvent e)
        {
            var controller = GetController(MakerAPI.GetCharacterControl());

            ColorMatchToggle = new AccessoryControlWrapper<MakerToggle, bool>(MakerAPI.AddAccessoryWindowControl(new MakerToggle(null, "Match Color With Hair", ColorMatchDefault, this)));
            HairGlossToggle = new AccessoryControlWrapper<MakerToggle, bool>(MakerAPI.AddAccessoryWindowControl(new MakerToggle(null, "Use Hair Gloss", ColorMatchDefault, this)));
            HairLengthSlider = new AccessoryControlWrapper<MakerSlider, float>(MakerAPI.AddAccessoryWindowControl(new MakerSlider(null, "Length", 0, 1, HairLengthDefault, this)));
            OutlineColorPicker = new AccessoryControlWrapper<MakerColor, Color>(MakerAPI.AddAccessoryWindowControl(new MakerColor("Outline Color", false, null, OutlineColorDefault, this)));
            AccessoryColorPicker = new AccessoryControlWrapper<MakerColor, Color>(MakerAPI.AddAccessoryWindowControl(new MakerColor("Accessory Color", false, null, AccessoryColorDefault, this)));
#if KKS
            GlossColorPicker = new AccessoryControlWrapper<MakerColor, Color>(MakerAPI.AddAccessoryWindowControl(new MakerColor("Gloss Color", false, null, GlossColorDefault, this)));
#endif

            //Color Match
            ColorMatchToggle.Control.Visible.OnNext(false);
            ColorMatchToggle.ValueChanged += ColorMatchToggle_ValueChanged;
            void ColorMatchToggle_ValueChanged(object sender, AccessoryWindowControlValueChangedEventArgs<bool> eventArgs)
            {
                controller.SetColorMatch(eventArgs.NewValue, eventArgs.SlotIndex);
                OutlineColorPicker.Control.Visible.OnNext(!eventArgs.NewValue);
#if KKS
                GlossColorPicker.Control.Visible.OnNext(!eventArgs.NewValue);
#endif

                if (eventArgs.NewValue)
                    HideAccColors();
                else
                    ShowAccColors(true);

                controller.UpdateAccessory(eventArgs.SlotIndex);
            }

            //Hair Gloss
            HairGlossToggle.Control.Visible.OnNext(false);
            HairGlossToggle.ValueChanged += HairGlossToggle_ValueChanged;
            void HairGlossToggle_ValueChanged(object sender, AccessoryWindowControlValueChangedEventArgs<bool> eventArgs)
            {
                controller.SetHairGloss(eventArgs.NewValue, eventArgs.SlotIndex);
                controller.UpdateAccessory(eventArgs.SlotIndex);
            }

            //Outline Color
            OutlineColorPicker.Control.ColorBoxWidth = 230;
            OutlineColorPicker.Control.Visible.OnNext(false);
            OutlineColorPicker.ValueChanged += OutlineColorPicker_ValueChanged;
            void OutlineColorPicker_ValueChanged(object sender, AccessoryWindowControlValueChangedEventArgs<Color> eventArgs)
            {
                controller.SetOutlineColor(eventArgs.NewValue, eventArgs.SlotIndex);
                controller.UpdateAccessory(eventArgs.SlotIndex);
            }

            //AccessoryColor
            AccessoryColorPicker.Control.ColorBoxWidth = 230;
            AccessoryColorPicker.Control.Visible.OnNext(false);
            AccessoryColorPicker.ValueChanged += AccessoryColorPicker_ValueChanged;
            void AccessoryColorPicker_ValueChanged(object sender, AccessoryWindowControlValueChangedEventArgs<Color> eventArgs)
            {
                controller.SetAccessoryColor(eventArgs.NewValue, eventArgs.SlotIndex);
                controller.UpdateAccessory(eventArgs.SlotIndex);
            }

            //HairLength
            HairLengthSlider.Control.Visible.OnNext(false);
            HairLengthSlider.ValueChanged += HairLengthSlider_ValueChanged;
            void HairLengthSlider_ValueChanged(object sender, AccessoryWindowControlValueChangedEventArgs<float> eventArgs)
            {
                controller.SetHairLength(eventArgs.NewValue, eventArgs.SlotIndex);
                controller.UpdateAccessory(eventArgs.SlotIndex);
            }

#if KKS
            //GlossColor
            GlossColorPicker.Control.ColorBoxWidth = 230;
            GlossColorPicker.Control.Visible.OnNext(false);
            GlossColorPicker.ValueChanged += GlossColorPicker_ValueChanged;
            void GlossColorPicker_ValueChanged(object sender, AccessoryWindowControlValueChangedEventArgs<Color> eventArgs)
            {
                controller.SetGlossColor(eventArgs.NewValue, eventArgs.SlotIndex);
                controller.UpdateAccessory(eventArgs.SlotIndex);
            }
#endif
        }

        private void MakerAPI_MakerFinishedLoading(object sender, System.EventArgs e)
        {
            StartCoroutine(Wait());
            IEnumerator Wait()
            {
                yield return null;
                InitCurrentSlot();
            }
        }

#if EC
        private void ExtendedSave_CardBeingImported(Dictionary<string, PluginData> importedExtendedData)
        {
            if (importedExtendedData.TryGetValue(GUID, out var pluginData))
            {
                if (pluginData != null && pluginData.data.TryGetValue("HairAccessories", out var loadedHairAccessories) && loadedHairAccessories != null)
                {
                    var hairAccessories = MessagePackSerializer.Deserialize<Dictionary<int, Dictionary<int, HairAccessoryController.HairAccessoryInfo>>>((byte[])loadedHairAccessories);

                    //Remove all data except for the first outfit
                    List<int> keysToRemove = new List<int>();
                    foreach (var entry in hairAccessories)
                        if (entry.Key != 0)
                            keysToRemove.Add(entry.Key);
                    foreach (var key in keysToRemove)
                        hairAccessories.Remove(key);

                    if (hairAccessories.Count == 0)
                    {
                        importedExtendedData.Remove(GUID);
                    }
                    else
                    {
                        var data = new PluginData();
                        data.data.Add("HairAccessories", MessagePackSerializer.Serialize(hairAccessories));
                        importedExtendedData[GUID] = data;
                    }
                }
            }
        }
#elif  KKS
        private void ExtendedSave_CardBeingImported(Dictionary<string, PluginData> importedExtendedData, Dictionary<int, int?> coordinateMapping)
        {
            if (importedExtendedData.TryGetValue(GUID, out var pluginData))
            {
                if (pluginData != null && pluginData.data.TryGetValue("HairAccessories", out var loadedHairAccessories) && loadedHairAccessories != null)
                {
                    Dictionary<int, Dictionary<int, HairAccessoryController.HairAccessoryInfo>> hairAccessories = MessagePackSerializer.Deserialize<Dictionary<int, Dictionary<int, HairAccessoryController.HairAccessoryInfo>>>((byte[])loadedHairAccessories);
                    Dictionary<int, Dictionary<int, HairAccessoryController.HairAccessoryInfo>> hairAccessoriesNew = new Dictionary<int, Dictionary<int, HairAccessoryController.HairAccessoryInfo>>();

                    foreach (var entry in hairAccessories)
                    {
                        if (coordinateMapping.TryGetValue(entry.Key, out int? newIndex) && newIndex != null)
                        {
                            hairAccessoriesNew[(int)newIndex] = entry.Value;
                        }
                    }

                    if (hairAccessoriesNew.Count == 0)
                    {
                        importedExtendedData.Remove(GUID);
                    }
                    else
                    {
                        var data = new PluginData();
                        data.data.Add("HairAccessories", MessagePackSerializer.Serialize(hairAccessoriesNew));
                        importedExtendedData[GUID] = data;
                    }
                }
            }
        }
#endif
    }
}
