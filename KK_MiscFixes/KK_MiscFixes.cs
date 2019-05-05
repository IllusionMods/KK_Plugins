using ActionGame;
using BepInEx;
using ChaCustom;
using ExtensibleSaveFormat;
using FreeH;
using Harmony;
using Illusion.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine.UI;
/// <summary>
/// Miscellaneous fixes aimed at improving the performance of the game
/// </summary>
namespace KK_MiscFixes
{
    [BepInPlugin(GUID, PluginName, Version)]
    public class KK_MiscFixes : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.miscfixes";
        public const string PluginName = "Misc Fixes";
        public const string Version = "1.1";

        private void Main()
        {
            var harmony = HarmonyInstance.Create(GUID);
            harmony.PatchAll(typeof(KK_MiscFixes));
        }
        #region Free H List
        /// <summary>
        /// Turn off ExtensibleSaveFormat events
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(FreeHClassRoomCharaFile), "Start")]
        public static void FreeHClassRoomCharaFileStartPrefix() => ExtendedSave.LoadEventsEnabled = false;
        /// <summary>
        /// Turn back on ExtensibleSaveFormat events, load a copy of the character with extended data on this time, and use that instead.
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(FreeHClassRoomCharaFile), "Start")]
        public static void FreeHClassRoomCharaFileStartPostfix(FreeHClassRoomCharaFile __instance)
        {
            ExtendedSave.LoadEventsEnabled = true;

            ReactiveProperty<ChaFileControl> info = Traverse.Create(__instance).Field("info").GetValue<ReactiveProperty<ChaFileControl>>();
            ClassRoomFileListCtrl listCtrl = Traverse.Create(__instance).Field("listCtrl").GetValue<ClassRoomFileListCtrl>();
            List<CustomFileInfo> lstFileInfo = Traverse.Create(listCtrl).Field("lstFileInfo").GetValue<List<CustomFileInfo>>();
            Button enterButton = Traverse.Create(__instance).Field("enterButton").GetValue<Button>();

            enterButton.onClick.RemoveAllListeners();
            enterButton.onClick.AddListener(() =>
            {
                var onEnter = (Action<ChaFileControl>)AccessTools.Field(typeof(FreeHClassRoomCharaFile), "onEnter").GetValue(__instance);
                string fullPath = lstFileInfo.First(x => x.FileName == info.Value.charaFileName.Remove(info.Value.charaFileName.Length - 4)).FullPath;

                ChaFileControl chaFileControl = new ChaFileControl();
                chaFileControl.LoadCharaFile(fullPath, info.Value.parameter.sex, false, true);

                onEnter(chaFileControl);
            });
        }
        #endregion

        #region Classroom list
        /// <summary>
        /// Turn off ExtensibleSaveFormat events
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(ClassRoomCharaFile), "InitializeList")]
        public static void ClassRoomCharaFileInitializeListPrefix() => ExtendedSave.LoadEventsEnabled = false;
        /// <summary>
        /// Turn back on ExtensibleSaveFormat events
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(ClassRoomCharaFile), "InitializeList")]
        public static void ClassRoomCharaFileInitializeListPostfix() => ExtendedSave.LoadEventsEnabled = true;
        /// <summary>
        /// Load a copy of the character with extended data on this time, and use that instead.
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(ClassRoomCharaFile), "Start")]
        public static void ClassRoomCharaFileStartPostfix(ClassRoomCharaFile __instance)
        {
            ReactiveProperty<ChaFileControl> info = Traverse.Create(__instance).Field("info").GetValue<ReactiveProperty<ChaFileControl>>();
            ClassRoomFileListCtrl listCtrl = Traverse.Create(__instance).Field("listCtrl").GetValue<ClassRoomFileListCtrl>();
            List<CustomFileInfo> lstFileInfo = Traverse.Create(listCtrl).Field("lstFileInfo").GetValue<List<CustomFileInfo>>();
            Button enterButton = Traverse.Create(__instance).Field("enterButton").GetValue<Button>();

            enterButton.onClick.RemoveAllListeners();
            enterButton.onClick.AddListener(() =>
            {
                var onEnter = (Action<ChaFileControl>)AccessTools.Field(typeof(ClassRoomCharaFile), "onEnter").GetValue(__instance);
                string fullPath = lstFileInfo.First(x => x.FileName == info.Value.charaFileName.Remove(info.Value.charaFileName.Length - 4)).FullPath;

                ChaFileControl chaFileControl = new ChaFileControl();
                chaFileControl.LoadCharaFile(fullPath, info.Value.parameter.sex, false, true);

                onEnter(chaFileControl);
                Utils.Sound.Play(SystemSE.sel);
            });
        }
        #endregion
    }
}
