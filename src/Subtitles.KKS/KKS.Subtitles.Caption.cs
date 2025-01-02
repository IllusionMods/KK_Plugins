using ActionGame.Communication;
using HarmonyLib;
using Manager;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;

namespace KK_Plugins
{
    public partial class Subtitles
    {
        public partial class Caption
        {
            private static string PrepareSubtitle(string text)
            {
                if (ADV.SceneParameter.advScene == null || ADV.SceneParameter.advScene.Scenario == null) return text;
                try
                {
                    return ADV.SceneParameter.advScene.Scenario.ReplaceText(text, true);
                }
                catch
                {
                    return text;
                }
            }

            internal static void DisplayDialogueSubtitle(string asset, string bundle, GameObject voiceObject)
            {
                foreach (var a in ActionGameInfoInstance.dicTalkInfo)
                foreach (var b in a.Value)
                foreach (var c in b.Value)
                foreach (var d in c.Value.OfType<Info.GenericInfo>())
                {
                    var text = d.talk.Where(x => x.assetbundle == bundle && x.file == asset).Select(x => x.text).FirstOrDefault();
                    if (!text.IsNullOrEmpty())
                    {
                        DisplaySubtitle(voiceObject, text);
                        return;
                    }
                }
            }

            internal static void DisplayHSubtitle(string asset, string bundle, GameObject voiceObject)
            {
                List<HActionBase> lstProc = (List<HActionBase>)AccessTools.Field(typeof(HSceneProc), "lstProc").GetValue(HSceneInstance);
                HFlag flags = (HFlag)Traverse.Create(HSceneInstance).Field("flags").GetValue();
                HActionBase mode = lstProc[(int)flags.mode];
                HVoiceCtrl voicectrl = (HVoiceCtrl)AccessTools.Field(typeof(HActionBase), "voice").GetValue(mode);

                //At the start of the H scene, all the text was loaded. Look through the loaded stuff and find the text for the current spoken voice.
                foreach (var a in voicectrl.dicVoiceIntos)
                foreach (var b in a)
                foreach (var c in b.Value)
                {
                    var text = c.Value.Where(x => x.Value.pathAsset == bundle && x.Value.nameFile == asset).Select(x => x.Value.word).FirstOrDefault();
                    if (!text.IsNullOrEmpty())
                    {
                        // try to replace name tags in subtitle ([P], etc.) before displaying
                        DisplaySubtitle(voiceObject, PrepareSubtitle(text));
                        return;
                    }
                }
            }

            internal static void DisplayEstheticSubtitle(string asset, string bundle, GameObject voiceObject)
            {
                var mgr = Singleton<EstheticManager>.Instance;
                if (mgr == null) return;
                foreach (var a in mgr.VoiceCtrl.infoVoiceTable)
                foreach (var b in a.Value.infos)
                {
                    var text = b.Value.voiceAssets.Where(x => x.asset == asset && x.bundle == bundle).Select(x => x.voice).FirstOrDefault();
                    if (!text.IsNullOrEmpty())
                    {
                        // try to replace name tags in subtitle ([P], etc.) before displaying
                        DisplaySubtitle(voiceObject, PrepareSubtitle(text));
                        return;
                    }
                }
            }
        }
    }
}
