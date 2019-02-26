using BepInEx;
using FreeH;
using Harmony;
using System;
using UniRx;
using UnityEngine.UI;
using static ExtensibleSaveFormat.ExtendedSave;
using ActionGame;
using Illusion.Game;

namespace KK_MiscFixes
{
    [BepInPlugin(GUID, PluginName, Version)]
    public class KK_MiscFixes : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.miscfixes";
        public const string PluginName = "Misc Fixes";
        public const string Version = "1.0";

        void Main()
        {
            var harmony = HarmonyInstance.Create(GUID);
            harmony.PatchAll(typeof(KK_MiscFixes));
        }
        /// <summary>
        /// Turn off ExtensibleSaveFormat events
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(FreeHClassRoomCharaFile), "Start")]
        public static void FreeHClassRoomCharaFileStartPrefix() => LoadEventsEnabled = false;
        /// <summary>
        /// Turn back on ExtensibleSaveFormat events, load a copy of the character with extended data on this time, and use that instead.
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(FreeHClassRoomCharaFile), "Start")]
        public static void FreeHClassRoomCharaFileStartPostfix(FreeHClassRoomCharaFile __instance)
        {
            LoadEventsEnabled = true;

            ReactiveProperty<ChaFileControl> info = Traverse.Create(__instance).Field("info").GetValue<ReactiveProperty<ChaFileControl>>();
            Button enterButton = Traverse.Create(__instance).Field("enterButton").GetValue<Button>();

            enterButton.onClick.RemoveAllListeners();
            enterButton.onClick.AddListener(() =>
            {
                var onEnter = (Action<ChaFileControl>)AccessTools.Field(typeof(FreeHClassRoomCharaFile), "onEnter").GetValue(__instance);
                ChaFileControl chaFileControl = new ChaFileControl();
                chaFileControl.LoadCharaFile(info.Value.charaFileName, info.Value.parameter.sex, false, true);

                onEnter(chaFileControl);
            });
        }

        /// <summary>
        /// Turn off ExtensibleSaveFormat events
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(ClassRoomCharaFile), "Start")]
        public static void ClassRoomCharaFileStartPrefix()
        {
            Logger.Log(BepInEx.Logging.LogLevel.Info, "ClassRoomCharaFileStartPrefix");
            LoadEventsEnabled = false;
        }
        /// <summary>
        /// Turn back on ExtensibleSaveFormat events, load a copy of the character with extended data on this time, and use that instead.
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(ClassRoomCharaFile), "Start")]
        public static void ClassRoomCharaFileStartPostfix(ClassRoomCharaFile __instance)
        {
            LoadEventsEnabled = true;

            ReactiveProperty<ChaFileControl> info = Traverse.Create(__instance).Field("info").GetValue<ReactiveProperty<ChaFileControl>>();
            Button enterButton = Traverse.Create(__instance).Field("enterButton").GetValue<Button>();

            enterButton.onClick.RemoveAllListeners();
            enterButton.onClick.AddListener(() =>
            {
                var onEnter = (Action<ChaFileControl>)AccessTools.Field(typeof(ClassRoomCharaFile), "onEnter").GetValue(__instance);
                ChaFileControl chaFileControl = new ChaFileControl();
                chaFileControl.LoadCharaFile(info.Value.charaFileName, info.Value.parameter.sex, false, true);

                onEnter(chaFileControl);
                Utils.Sound.Play(SystemSE.sel);
            });
        }
    }
}
