using BepInEx;
using FreeH;
using Harmony;
using System;
using UniRx;
using UnityEngine.UI;
using static ExtensibleSaveFormat.ExtendedSave;

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
        public static void StartPrefix(FreeHClassRoomCharaFile __instance) => LoadEventsEnabled = false;
        /// <summary>
        /// Turn back on ExtensibleSaveFormat events, load a copy of the character with extended data on this time, and use that instead.
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(FreeHClassRoomCharaFile), "Start")]
        public static void StartPostfix(FreeHClassRoomCharaFile __instance)
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
    }
}
