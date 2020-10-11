using ActionGame.Communication;
using HarmonyLib;
using System;
using UnityEngine;

namespace KK_Plugins
{
    public partial class Subtitles
    {
        internal static class Hooks
        {
            [HarmonyPostfix, HarmonyPatch(typeof(LoadAudioBase), "Play")]
            private static void PlayVoice(LoadAudioBase __instance)
            {
                if (__instance.audioSource == null || __instance.audioSource.clip == null || __instance.audioSource.loop)
                    return;

                if (HSceneInstance != null)
                    Caption.DisplayHSubtitle(__instance);
                else if (ActionGameInfoInstance != null && GameObject.Find("ActionScene/ADVScene") == null)
                    Caption.DisplayDialogueSubtitle(__instance);
                else if (SubtitleDictionary.TryGetValue(__instance.assetName, out string text))
                    Caption.DisplaySubtitle(__instance.gameObject, text);
            }

            [HarmonyPostfix, HarmonyPatch(typeof(Info), "Init")]
            private static void InfoInit(Info __instance) => ActionGameInfoInstance = __instance;

            [HarmonyPostfix, HarmonyPatch(typeof(HVoiceCtrl), "Init")]
            private static void HVoiceCtrlInit()
            {
                //Get the H scene type used by VR, if possible
                HSceneType = Type.GetType("VRHScene, Assembly-CSharp");
                if (HSceneType == null)
                    HSceneType = typeof(HSceneProc);

                HSceneInstance = FindObjectOfType(HSceneType);
            }
        }
    }
}