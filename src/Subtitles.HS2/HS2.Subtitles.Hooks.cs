using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace KK_Plugins
{
    public partial class Subtitles
    {
        internal static class Hooks
        {
            [HarmonyPostfix, HarmonyPatch(typeof(HVoiceCtrl), "Init")]
            private static void HVoiceCtrlInit() => HSceneInstance = FindObjectOfType<HScene>();

            [HarmonyPostfix, HarmonyPatch(typeof(Manager.Sound), "Play", typeof(Manager.Sound.Loader))]
            private static void PlayPostfix(Manager.Sound.Loader loader, AudioSource __result)
            {
                if (loader.asset.IsNullOrEmpty() || loader.asset.Contains("_bgm_"))
                    return;

                if (SubtitleDictionary.TryGetValue(loader.asset, out string text))
                    Caption.DisplaySubtitle(__result.gameObject, text);
            }

            [HarmonyPostfix, HarmonyPatch(typeof(Manager.Voice), "Play_Standby", typeof(AudioSource), typeof(Manager.Voice.Loader))]
            private static void PlayStandbyPostfix(AudioSource audioSource, Manager.Voice.Loader loader)
            {
                if (loader.asset.IsNullOrEmpty() || loader.asset.Contains("_bgm_"))
                    return;

                if (HSceneInstance != null && HSceneInstance.ctrlVoice != null)
                    DisplayHSubtitle(loader, audioSource);
                else if (SubtitleDictionary.TryGetValue(loader.asset, out string text))
                    Caption.DisplaySubtitle(audioSource.gameObject, text);
            }

            private static void DisplayHSubtitle(Manager.Voice.Loader loader, AudioSource audioSource)
            {
                Dictionary<int, Dictionary<int, HVoiceCtrl.VoiceList>>[] dicdiclstVoiceList = (Dictionary<int, Dictionary<int, HVoiceCtrl.VoiceList>>[])Traverse.Create(HSceneInstance.ctrlVoice).Field("dicdiclstVoiceList").GetValue();

                foreach (Dictionary<int, Dictionary<int, HVoiceCtrl.VoiceList>> a in dicdiclstVoiceList)
                    foreach (Dictionary<int, HVoiceCtrl.VoiceList> b in a.Values)
                        foreach (HVoiceCtrl.VoiceList c in b.Values)
                            foreach (Dictionary<int, HVoiceCtrl.VoiceListInfo> d in c.dicdicVoiceList)
                                foreach (var e in d.Values)
                                    if (e.nameFile == loader.asset && e.pathAsset == loader.bundle)
                                    {
                                        if (Application.productName == Constants.VRProcessName)
                                            Caption.DisplayVRSubtitle(audioSource.gameObject, e.word);
                                        else
                                            Caption.DisplaySubtitle(audioSource.gameObject, e.word);
                                        return;
                                    }
            }
        }
    }
}
