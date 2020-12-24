using HarmonyLib;
using MaterialEditorAPI;
#if KK || EC
using ChaCustom;
#endif

namespace KK_Plugins.MaterialEditor
{
    internal static class MakerHooks
    {
#if KK || EC
        /// <summary>
        /// Add some events to hide or refresh the ME UI when changing between hair types
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(CustomChangeHairMenu), nameof(CustomChangeHairMenu.Start))]
        private static void CustomChangeHairMenu_Start(CustomChangeHairMenu __instance)
        {
            for (int i = 0; i < __instance.items.Length; i++)
            {
                int index = i;
                var tglItem = __instance.items[i].tglItem;
                if (tglItem != null)
                {
                    tglItem.onValueChanged.AddListener(value =>
                    {
                        if (value)
                        {
                            MEMaker.currentHairIndex = index;
                            if (MaterialEditorUI.Visible)
                                MEMaker.Instance.UpdateUIHair(index);
                        }
                    });
                }
            }
        }

        /// <summary>
        /// Add some events to hide or refresh the ME UI when changing between clothes types
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(CustomChangeClothesMenu), nameof(CustomChangeClothesMenu.Start))]
        private static void CustomChangeClothesMenu_Start(CustomChangeClothesMenu __instance)
        {
            for (int i = 0; i < __instance.items.Length; i++)
            {
                int index = i;
                var tglItem = __instance.items[i].tglItem;
                if (tglItem != null)
                {
                    tglItem.onValueChanged.AddListener(value =>
                    {
                        if (value)
                        {
                            MEMaker.currentClothesIndex = index;
                            if (MaterialEditorUI.Visible)
                                MEMaker.Instance.UpdateUIClothes(index);
                        }
                    });
                }
            }
        }

        /// <summary>
        /// Add some events to hide or refresh the ME UI when changing between item types
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(CustomChangeMainMenu), nameof(CustomChangeMainMenu.Start))]
        private static void CustomChangeMainMenu_Start(CustomChangeMainMenu __instance)
        {
            for (int i = 0; i < __instance.items.Length; i++)
            {
                int index = i;
                var tglItem = __instance.items[i].tglItem;
                if (tglItem != null)
                {
                    tglItem.onValueChanged.AddListener(value =>
                    {
                        if (value && MaterialEditorUI.Visible)
                        {
                            if (index == 2)
                                MEMaker.Instance.UpdateUIHair(MEMaker.currentHairIndex);
                            else if (index == 3)
                                MEMaker.Instance.UpdateUIClothes(MEMaker.currentClothesIndex);
                            else if (index == 4)
                                MEMaker.Instance.UpdateUIAccessory();
                            else
                                MaterialEditorUI.Visible = false;
                        }
                    });
                }
            }
        }
#endif
    }
}
