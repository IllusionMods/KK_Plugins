using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using System;

namespace KK_Plugins.MoreOutfits
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.deathweasel.bepinex.moreoutfits";
        public const string PluginName = "More Outfit Slots";
        public const string PluginNameInternal = Constants.Prefix + "_MoreOutfits";
        public const string PluginVersion = "1.1.1";

        internal static new ManualLogSource Logger;
        internal static Plugin Instance;
        public const string TextboxDefault = "Outfit #";
        public static readonly int OriginalCoordinateLength = Enum.GetNames(typeof(ChaFileDefine.CoordinateType)).Length;

        private void Awake()
        {
            Logger = base.Logger;
            Instance = this;

            MakerAPI.ReloadCustomInterface += MakerUI.MakerAPI_ReloadCustomInterface;
            MakerAPI.RegisterCustomSubCategories += MakerUI.MakerAPI_RegisterCustomSubCategories;
            CharacterApi.RegisterExtraBehaviour<MoreOutfitsController>(PluginGUID);
            StudioUI.RegisterStudioControls();

            Harmony.CreateAndPatchAll(typeof(Hooks));
        }

        /// <summary>
        /// Add another coordinate slot for the specified character
        /// </summary>
        /// <param name="chaControl">The character being modified</param>
        public static void AddCoordinateSlot(ChaControl chaControl)
        {
            //Initialize a new bigger array, copy the contents of the old
            var newCoordinate = new ChaFileCoordinate[chaControl.chaFile.coordinate.Length + 1];
            for (int i = 0; i < chaControl.chaFile.coordinate.Length; i++)
                newCoordinate[i] = chaControl.chaFile.coordinate[i];
            newCoordinate[newCoordinate.Length - 1] = new ChaFileCoordinate();
            chaControl.chaFile.coordinate = newCoordinate;

            MakerUI.UpdateMakerUI();
        }

        /// <summary>
        /// Remove the last added coordinate slot for the specified character
        /// </summary>
        /// <param name="chaControl">The character being modified</param>
        public static void RemoveCoordinateSlot(ChaControl chaControl)
        {
            //Initialize a new smaller array, copy the contents of the old
            if (chaControl.chaFile.coordinate.Length <= OriginalCoordinateLength)
                return;

            var newCoordinate = new ChaFileCoordinate[chaControl.chaFile.coordinate.Length - 1];
            for (int i = 0; i < newCoordinate.Length; i++)
                newCoordinate[i] = chaControl.chaFile.coordinate[i];
            chaControl.chaFile.coordinate = newCoordinate;

            MakerUI.UpdateMakerUI();
        }

        /// <summary>
        /// Set the name of a coordinate for a character
        /// </summary>
        /// <param name="chaControl">Character</param>
        /// <param name="index">Index of the coordinate</param>
        /// <param name="name">Name of the coordinate</param>
        public static void SetCoordinateName(ChaControl chaControl, int index, string name)
        {
            var controller = GetController(chaControl);
            if (chaControl != null)
                controller.SetCoordinateName(index, name);
        }

        /// <summary>
        /// Get the name of a coordinate for a character
        /// </summary>
        /// <param name="chaControl">Character</param>
        /// <param name="index">Index of the coordinate</param>
        /// <returns>Name of the coordinate</returns>
        public static string GetCoodinateName(ChaControl chaControl, int index)
        {
            var controller = GetController(chaControl);
            if (chaControl != null)
                return controller.GetCoodinateName(index);
            return $"Outfit {index + 1}";
        }

        /// <summary>
        /// Get the KKAPI CharaCustomFunctionController for the character
        /// </summary>
        /// <param name="chaControl">Character</param>
        /// <returns>KKAPI CharaCustomFunctionController</returns>
        public static MoreOutfitsController GetController(ChaControl chaControl) => chaControl == null || chaControl.gameObject == null ? null : chaControl.gameObject.GetComponent<MoreOutfitsController>();
    }
}
