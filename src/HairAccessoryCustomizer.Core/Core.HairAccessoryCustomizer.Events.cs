using KKAPI.Maker;
using KKAPI.Maker.UI;
using UnityEngine;

namespace KK_Plugins
{
    public partial class HairAccessoryCustomizer
    {
        private static void AccessoriesApi_AccessoryTransferred(object sender, AccessoryTransferEventArgs e) => GetController(MakerAPI.GetCharacterControl()).TransferAccessoriesHandler(e);
#if KK
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
            OutlineColorPicker = new AccessoryControlWrapper<MakerColor, Color>(MakerAPI.AddAccessoryWindowControl(new MakerColor("Outline Color", false, null, OutlineColorDefault, this)));
            AccessoryColorPicker = new AccessoryControlWrapper<MakerColor, Color>(MakerAPI.AddAccessoryWindowControl(new MakerColor("Accessory Color", false, null, OutlineColorDefault, this)));
            HairLengthSlider = new AccessoryControlWrapper<MakerSlider, float>(MakerAPI.AddAccessoryWindowControl(new MakerSlider(null, "Length", 0, 1, HairLengthDefault, this)));

            //Color Match
            ColorMatchToggle.Control.Visible.OnNext(false);
            ColorMatchToggle.ValueChanged += ColorMatchToggle_ValueChanged;
            void ColorMatchToggle_ValueChanged(object sender, AccessoryWindowControlValueChangedEventArgs<bool> eventArgs)
            {
                controller.SetColorMatch(eventArgs.NewValue, eventArgs.SlotIndex);
                OutlineColorPicker.Control.Visible.OnNext(!eventArgs.NewValue);

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
        }
    }
}
