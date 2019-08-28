using ActionGame.Communication;
using HarmonyLib;
using UnityEngine;

namespace KK_Plugins
{
    public partial class Subtitles
    {
        internal static class Hooks
        {
            [HarmonyPostfix, HarmonyPatch(typeof(LoadAudioBase), "Play")]
            public static void PlayVoice(LoadAudioBase __instance)
            {
                if (__instance.audioSource == null || __instance.audioSource.clip == null || __instance.audioSource.loop)
                    return;

                if (HSceneProcInstance != null)
                    Caption.DisplayHSubtitle(__instance);
                else if (ActionGameInfoInstance != null && GameObject.Find("ActionScene/ADVScene") == null)
                    Caption.DisplayDialogueSubtitle(__instance);
                else if (SubtitleDictionary.TryGetValue(__instance.assetName, out string text))
                    Caption.DisplaySubtitle(__instance, text);
            }

            [HarmonyPostfix, HarmonyPatch(typeof(Info), "Init")]
            public static void InfoInit(Info __instance)
            {
                Caption.InitGUI();
                ActionGameInfoInstance = __instance;
            }
            [HarmonyPostfix, HarmonyPatch(typeof(HVoiceCtrl), "Init")]
            public static void HVoiceCtrlInit()
            {
                Caption.InitGUI();
                HSceneProcInstance = FindObjectOfType<HSceneProc>();
            }
        }
    }
}