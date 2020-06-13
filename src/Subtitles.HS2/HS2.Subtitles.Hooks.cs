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
            internal static void HVoiceCtrlInit()
            {
                Caption.InitGUI();
                HSceneInstance = FindObjectOfType<HScene>();
            }

            [HarmonyPostfix, HarmonyPatch(typeof(Manager.Voice), nameof(Manager.Voice.OncePlayChara), typeof(Manager.Voice.Loader))]
            internal static void OncePlayCharaPostfix(Manager.Voice.Loader loader, AudioSource __result)
            {
                Dictionary<int, Dictionary<int, HVoiceCtrl.VoiceList>>[] dicdiclstVoiceList = (Dictionary<int, Dictionary<int, HVoiceCtrl.VoiceList>>[])Traverse.Create(HSceneInstance.ctrlVoice).Field("dicdiclstVoiceList").GetValue();

                foreach (Dictionary<int, Dictionary<int, HVoiceCtrl.VoiceList>> a in dicdiclstVoiceList)
                    foreach (Dictionary<int, HVoiceCtrl.VoiceList> b in a.Values)
                        foreach (HVoiceCtrl.VoiceList c in b.Values)
                            foreach (Dictionary<int, HVoiceCtrl.VoiceListInfo> d in c.dicdicVoiceList)
                                foreach (var e in d.Values)
                                    if (e.nameFile == loader.asset && e.pathAsset == loader.bundle)
                                    {
                                        Caption.DisplaySubtitle(__result, e.nameFile, e.word);
                                        return;
                                    }
            }
        }
    }
}
