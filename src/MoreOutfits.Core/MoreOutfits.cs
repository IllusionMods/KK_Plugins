using BepInEx;
using BepInEx.Logging;
using ChaCustom;
using HarmonyLib;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using MessagePack;
using System;
using System.Collections.Generic;

namespace KK_Plugins
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class MoreOutfits : BaseUnityPlugin
    {
        public const string PluginGUID = "com.deathweasel.bepinex.moreoutfits";
        public const string PluginName = "More Outfit Slots";
        public const string PluginNameInternal = Constants.Prefix + "_MoreOutfits";
        public const string PluginVersion = "1.0";
        internal static new ManualLogSource Logger;

        private readonly int OriginalCoordinateLength = Enum.GetNames(typeof(ChaFileDefine.CoordinateType)).Length;

        private void Awake()
        {
            Logger = base.Logger;
            MakerAPI.ReloadCustomInterface += MakerAPI_ReloadCustomInterface;
            MakerAPI.RegisterCustomSubCategories += MakerAPI_RegisterCustomSubCategories;

            Harmony.CreateAndPatchAll(typeof(Hooks));
        }

        private void MakerAPI_ReloadCustomInterface(object sender, EventArgs e)
        {
            SetUpDropDowns();
        }

        private void MakerAPI_RegisterCustomSubCategories(object sender, RegisterSubCategoriesEvent ev)
        {
            MakerCategory category = new MakerCategory("03_ClothesTop", "tglSettings", MakerConstants.Clothes.Copy.Position + 1, "Settings");

            var addCoordinateButton = new MakerButton("Add additional clothing slot", category, this);
            ev.AddControl(addCoordinateButton);
            addCoordinateButton.OnClick.AddListener(() => { AddCoordinateSlot(MakerAPI.GetCharacterControl()); });

            ev.AddSubCategory(category);
        }

        public void AddCoordinateSlot(ChaControl chaControl)
        {
            //Initialize a new bigger array, copy the contents of the old
            var newCoordinate = new ChaFileCoordinate[chaControl.chaFile.coordinate.Length + 1];
            for (int i = 0; i < chaControl.chaFile.coordinate.Length; i++)
                newCoordinate[i] = chaControl.chaFile.coordinate[i];
            newCoordinate[newCoordinate.Length - 1] = new ChaFileCoordinate();
            chaControl.chaFile.coordinate = newCoordinate;

            SetUpDropDowns();
        }

        private void SetUpDropDowns()
        {
            if (!MakerAPI.InsideMaker)
                return;

            var chaControl = MakerAPI.GetCharacterControl();

            //Remove extras
            var customControl = FindObjectOfType<CustomControl>();
            customControl.ddCoordinate.m_Options.m_Options.RemoveAll(x => x.text.StartsWith("Extra"));

            var cvsCopy = CustomBase.Instance.GetComponentInChildren<CvsClothesCopy>(true);
            cvsCopy.ddCoordeType[0].m_Options.m_Options.RemoveAll(x => x.text.StartsWith("Extra"));
            cvsCopy.ddCoordeType[1].m_Options.m_Options.RemoveAll(x => x.text.StartsWith("Extra"));

            var cvsAccessoryCopy = CustomBase.Instance.GetComponentInChildren<CvsAccessoryCopy>(true);
            cvsAccessoryCopy.ddCoordeType[0].m_Options.m_Options.RemoveAll(x => x.text.StartsWith("Extra"));
            cvsAccessoryCopy.ddCoordeType[1].m_Options.m_Options.RemoveAll(x => x.text.StartsWith("Extra"));

            if (chaControl.chaFile.coordinate.Length <= OriginalCoordinateLength)
                return;

            //Add dropdown options for each additional coodinate
            for (int i = 0; i < (chaControl.chaFile.coordinate.Length - OriginalCoordinateLength); i++)
            {
                customControl.ddCoordinate.m_Options.m_Options.Add(new TMPro.TMP_Dropdown.OptionData($"Extra {i + 1}"));
                cvsCopy.ddCoordeType[0].m_Options.m_Options.Add(new TMPro.TMP_Dropdown.OptionData($"Extra {i + 1}"));
                cvsCopy.ddCoordeType[1].m_Options.m_Options.Add(new TMPro.TMP_Dropdown.OptionData($"Extra {i + 1}"));
                cvsAccessoryCopy.ddCoordeType[0].m_Options.m_Options.Add(new TMPro.TMP_Dropdown.OptionData($"Extra {i + 1}"));
                cvsAccessoryCopy.ddCoordeType[1].m_Options.m_Options.Add(new TMPro.TMP_Dropdown.OptionData($"Extra {i + 1}"));
            }
        }
    }

    public class Hooks
    {
        /// <summary>
        /// Ensure extra coordinates are loaded
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(ChaFile), nameof(ChaFile.SetCoordinateBytes))]
        private static bool SetCoordinateBytes(ChaFile __instance, byte[] data, Version ver)
        {
            List<byte[]> list = MessagePackSerializer.Deserialize<List<byte[]>>(data);

            //Reinitialize the array with the new length
            __instance.coordinate = new ChaFileCoordinate[list.Count];
            for (int i = 0; i < list.Count; i++)
                __instance.coordinate[i] = new ChaFileCoordinate();

            //Load all the coordinates
            for (int i = 0; i < __instance.coordinate.Length; i++)
                __instance.coordinate[i].LoadBytes(list[i], ver);

            return false;
        }

        /// <summary>
        /// Prevent index out of range exceptions
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPrefix, HarmonyPatch(typeof(CvsAccessoryCopy), nameof(CvsAccessoryCopy.ChangeDstDD))]
        private static void CvsAccessoryCopy_ChangeDstDD(CvsAccessoryCopy __instance)
        {
            if (__instance.ddCoordeType[0].value >= __instance.chaCtrl.chaFile.coordinate.Length)
                __instance.ddCoordeType[0].value = 0;
        }
        [HarmonyPrefix, HarmonyPatch(typeof(CvsAccessoryCopy), nameof(CvsAccessoryCopy.ChangeSrcDD))]
        private static void CvsAccessoryCopy_ChangeSrcDD(CvsAccessoryCopy __instance)
        {
            if (__instance.ddCoordeType[1].value >= __instance.chaCtrl.chaFile.coordinate.Length)
                __instance.ddCoordeType[1].value = 0;
        }
        [HarmonyPrefix, HarmonyPatch(typeof(CvsClothesCopy), nameof(CvsClothesCopy.ChangeDstDD))]
        private static void CvsClothesCopy_ChangeDstDD(CvsAccessoryCopy __instance)
        {
            if (__instance.ddCoordeType[0].value >= __instance.chaCtrl.chaFile.coordinate.Length)
                __instance.ddCoordeType[0].value = 0;
        }
        [HarmonyPrefix, HarmonyPatch(typeof(CvsClothesCopy), nameof(CvsClothesCopy.ChangeSrcDD))]
        private static void CvsClothesCopy_ChangeSrcDD(CvsAccessoryCopy __instance)
        {
            if (__instance.ddCoordeType[1].value >= __instance.chaCtrl.chaFile.coordinate.Length)
                __instance.ddCoordeType[1].value = 0;
        }
    }
}
