using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using KKAPI.Chara;
using KKAPI.Maker;
using System;
using System.Collections.Generic;

namespace KK_Plugins.MoreOutfits
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.deathweasel.bepinex.moreoutfits";
        public const string PluginName = "More Outfit Slots";
        public const string PluginNameInternal = Constants.Prefix + "_MoreOutfits";
        public const string PluginVersion = "1.0";

        internal static new ManualLogSource Logger;
        internal static Plugin Instance;
        public const string TextboxDefault = "Outfit #";

        public static readonly int OriginalCoordinateLength = Enum.GetNames(typeof(ChaFileDefine.CoordinateType)).Length;
        public static readonly List<string> CoordinateNames = new List<string> { "学生服（校内）", "学生服（下校）", "体操着", "水着", "部活", "私服", "お泊り" };

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

        public static MoreOutfitsController GetController(ChaControl character) => character == null || character.gameObject == null ? null : character.gameObject.GetComponent<MoreOutfitsController>();
    }
}
