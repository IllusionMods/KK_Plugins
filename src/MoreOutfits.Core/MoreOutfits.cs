using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using KKAPI.Maker;
using MessagePack;
using System;
using System.Collections.Generic;
using UnityEngine;

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

            Harmony.CreateAndPatchAll(typeof(Hooks));
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.M))
            {
                var chaControl = MakerAPI.GetCharacterControl();

                //Initialize a new bigger array, copy the contents of the old
                var newCoordinate = new ChaFileCoordinate[chaControl.chaFile.coordinate.Length + 1];
                for (int i = 0; i < chaControl.chaFile.coordinate.Length; i++)
                    newCoordinate[i] = chaControl.chaFile.coordinate[i];
                newCoordinate[newCoordinate.Length - 1] = new ChaFileCoordinate();
                chaControl.chaFile.coordinate = newCoordinate;

                //Add dropdown option for the additional coodinate
                var cc = FindObjectOfType<ChaCustom.CustomControl>();

                cc.ddCoordinate.m_Options.m_Options.Add(new TMPro.TMP_Dropdown.OptionData($"Extra {newCoordinate.Length - OriginalCoordinateLength}"));
            }
        }

        private void MakerAPI_ReloadCustomInterface(object sender, EventArgs e)
        {
            var chaControl = MakerAPI.GetCharacterControl();

            //Remove extras
            var cc = FindObjectOfType<ChaCustom.CustomControl>();
            cc.ddCoordinate.m_Options.m_Options.RemoveAll(x => x.text.StartsWith("Extra"));

            if (chaControl.chaFile.coordinate.Length <= OriginalCoordinateLength)
                return;

            //Add dropdown options for each additional coodinate
            for (int i = 0; i < (chaControl.chaFile.coordinate.Length - OriginalCoordinateLength); i++)
                cc.ddCoordinate.m_Options.m_Options.Add(new TMPro.TMP_Dropdown.OptionData($"Extra {i + 1}"));
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
    }
}
