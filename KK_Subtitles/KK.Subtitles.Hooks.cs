using ActionGame.Communication;
using Harmony;
using UnityEngine;

namespace Subtitles
{
    internal static class Hooks
    {
        [HarmonyPostfix, HarmonyPatch(typeof(LoadVoice), "Play")]
        public static void PlayVoice(LoadVoice __instance)
        {
            if (__instance.audioSource == null || __instance.audioSource.clip == null || __instance.audioSource.loop)
                return;

            if (Subtitles.HSceneProcInstance != null)
                Caption.DisplayHSubtitle(__instance);
            else if (Subtitles.ActionGameInfoInstance != null && GameObject.Find("ActionScene/ADVScene") == null)
                Caption.DisplayDialogueSubtitle(__instance);
            else if (Subtitles.SubtitleDictionary.TryGetValue(__instance.assetName, out string text))
                Caption.DisplaySubtitle(__instance, text);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Info), "Init")]
        public static void InfoInit(Info __instance)
        {
            Caption.InitGUI();
            Subtitles.ActionGameInfoInstance = __instance;
        }
        [HarmonyPostfix, HarmonyPatch(typeof(HVoiceCtrl), "Init")]
        public static void HVoiceCtrlInit()
        {
            Caption.InitGUI();
            Subtitles.HSceneProcInstance = Object.FindObjectOfType<HSceneProc>();
        }
    }
}
