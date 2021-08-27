using HarmonyLib;
using UnityEngine;

namespace KK_Plugins
{
    public partial class Subtitles
    {
        internal static class Hooks
        {
            [HarmonyPostfix, HarmonyPatch(typeof(Manager.Voice), nameof(Manager.Voice.Play_Standby), typeof(AudioSource), typeof(Manager.Voice.Loader))]
            private static void PlayVoice(AudioSource audioSource, Manager.Voice.Loader loader)
            {
                if (loader.asset.IsNullOrEmpty())
                    return;

                if (HSceneInstance != null)
                    Caption.DisplayHSubtitle(loader.asset, loader.bundle, audioSource.gameObject);
                else if (ActionGameInfoInstance != null && GameObject.Find("ActionScene/ADVScene") == null)
                    Caption.DisplayDialogueSubtitle(loader.asset, loader.bundle, audioSource.gameObject);
            }

            [HarmonyPostfix, HarmonyPatch(typeof(Manager.Sound), nameof(Manager.Sound.Play_Standby), typeof(AudioSource), typeof(Manager.Sound.Loader))]
            private static void PlaySound(AudioSource audioSource, Manager.Sound.Loader loader)
            {
                if (loader.asset.IsNullOrEmpty() || loader.asset.Contains("_bgm_") || loader.asset.StartsWith("kks_song"))
                    return;

                if (SubtitleDictionary.TryGetValue(loader.asset, out string text))
                    Caption.DisplaySubtitle(audioSource.gameObject, text);
            }

            [HarmonyPostfix, HarmonyPatch(typeof(ActionGame.Communication.Info), nameof(ActionGame.Communication.Info.Init))]
            private static void InfoInit(ActionGame.Communication.Info __instance)
            {
                ActionGameInfoInstance = __instance;
            }

            [HarmonyPostfix, HarmonyPatch(typeof(HVoiceCtrl), nameof(HVoiceCtrl.Init))]
            private static void HVoiceCtrlInit()
            {
                HSceneInstance = FindObjectOfType(typeof(HSceneProc));
            }
        }
    }
}