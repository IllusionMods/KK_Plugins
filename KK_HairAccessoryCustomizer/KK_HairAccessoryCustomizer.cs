using BepInEx;
using Harmony;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// Individual customization of hair accessories for adding hair gloss, color matching, etc.
/// </summary>
namespace KK_HairAccessoryCustomizer
{
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class KK_HairAccessoryCustomizer : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.hairaccessorycustomizer";
        public const string PluginName = "Hair Accessory Customizer";
        public const string PluginNameInternal = nameof(KK_HairAccessoryCustomizer);
        public const string Version = "1.0";
        internal static bool ReloadingChara = false;
        internal static AccessoryControlWrapper<MakerToggle, bool> ColorMatchToggle;
        internal static AccessoryControlWrapper<MakerToggle, bool> HairGlossToggle;
        internal static AccessoryControlWrapper<MakerColor, Color> OutlineColorPicker;
        internal static AccessoryControlWrapper<MakerColor, Color> AccessoryColorPicker;
        private static readonly bool ColorMatchDefault = true;
        private static readonly bool HairGlossDefault = true;
        private static Color OutlineColorDefault = Color.black;
        private static Color AccessoryColorDefault = Color.red;

        private void Main()
        {
            var harmony = HarmonyInstance.Create(GUID);
            harmony.PatchAll(typeof(KK_HairAccessoryCustomizer_hooks));

            CharacterApi.RegisterExtraBehaviour<HairAccessoryController>(GUID);

            MakerAPI.MakerBaseLoaded += MakerAPI_MakerBaseLoaded;
            AccessoriesApi.SelectedMakerAccSlotChanged += AccessoriesApi_SelectedMakerAccSlotChanged;
            AccessoriesApi.AccessoryKindChanged += AccessoriesApi_AccessoryKindChanged;
            AccessoriesApi.AccessoriesCopied += AccessoriesApi_AccessoriesCopied;
            AccessoriesApi.AccessoryTransferred += AccessoriesApi_AccessoryTransferred;
        }
        /// <summary>
        /// Hides the accessory color controls for the slot
        /// </summary>
        internal static void HideAccColors(int slot)
        {
            if (!MakerAPI.InsideAndLoaded) return;

            Traverse.Create(AccessoriesApi.GetCvsAccessory(slot)).Field("separateColor").GetValue<GameObject>().SetActive(false);
            Traverse.Create(AccessoriesApi.GetCvsAccessory(slot)).Field("btnAcsColor01").GetValue<Button>().transform.parent.gameObject.SetActive(false);
            Traverse.Create(AccessoriesApi.GetCvsAccessory(slot)).Field("btnAcsColor02").GetValue<Button>().transform.parent.gameObject.SetActive(false);
            Traverse.Create(AccessoriesApi.GetCvsAccessory(slot)).Field("btnAcsColor03").GetValue<Button>().transform.parent.gameObject.SetActive(false);
            Traverse.Create(AccessoriesApi.GetCvsAccessory(slot)).Field("btnAcsColor04").GetValue<Button>().transform.parent.gameObject.SetActive(false);
            Traverse.Create(AccessoriesApi.GetCvsAccessory(slot)).Field("btnInitColor").GetValue<Button>().transform.parent.gameObject.SetActive(false);
        }
        /// <summary>
        /// Shows the accessory color controls for the slot
        /// </summary>
        internal static void ShowAccColors(int slot)
        {
            if (!MakerAPI.InsideAndLoaded) return;

            AccessoriesApi.GetCvsAccessory(slot).ChangeUseColorVisible();
            Traverse.Create(AccessoriesApi.GetCvsAccessory(slot)).Field("btnInitColor").GetValue<Button>().transform.parent.gameObject.SetActive(true);
        }
        /// <summary>
        /// Sets up the visibility and values for the current slot
        /// </summary>
        internal static void InitCurrentSlot(HairAccessoryController controller)
        {
            if (!MakerAPI.InsideAndLoaded) return;

            ColorMatchToggle.SetSelectedValue(controller.GetColorMatch(), false);
            HairGlossToggle.SetSelectedValue(controller.GetHairGloss(), false);
            OutlineColorPicker.SetSelectedValue(controller.GetOutlineColor(), false);
            AccessoryColorPicker.SetSelectedValue(controller.GetAccessoryColor(), false);

            ColorMatchToggle.Control.Visible.OnNext(true);
            HairGlossToggle.Control.Visible.OnNext(true);
            OutlineColorPicker.Control.Visible.OnNext(!controller.GetColorMatch());
            AccessoryColorPicker.Control.Visible.OnNext(controller.HasAccessoryPart(AccessoriesApi.SelectedMakerAccSlot));

            if (controller.GetColorMatch(AccessoriesApi.SelectedMakerAccSlot))
                HideAccColors(AccessoriesApi.SelectedMakerAccSlot);
            else
                ShowAccColors(AccessoriesApi.SelectedMakerAccSlot);
        }
        /// <summary>
        /// Sets default values for the current slot
        /// </summary>
        internal void SetDefaults()
        {
            ColorMatchToggle.SetSelectedValue(ColorMatchDefault, false);
            HairGlossToggle.SetSelectedValue(HairGlossDefault, false);
            OutlineColorPicker.SetSelectedValue(OutlineColorDefault, false);
            AccessoryColorPicker.SetSelectedValue(AccessoryColorDefault, false);
        }

        internal static HairAccessoryController GetController(ChaControl character) => character?.gameObject?.GetComponent<HairAccessoryController>();
    }
}
