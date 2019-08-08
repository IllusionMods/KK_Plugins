using Harmony;
using System.Collections.Generic;
using System.Linq;
using UniRx;

namespace Subtitles
{
    public partial class Caption
    {
        internal static void DisplayDialogueSubtitle(LoadVoice voice)
        {
            string text = "";
            FindText();
            void FindText()
            {
                foreach (var a in Subtitles.ActionGameInfoInstance.dicTalkInfo)
                {
                    foreach (var b in a.Value)
                    {
                        foreach (var c in b.Value)
                        {
                            foreach (var d in c.Value.Where(x => x is ActionGame.Communication.Info.GenericInfo).Select(x => x as ActionGame.Communication.Info.GenericInfo))
                            {
                                text = d.talk.Where(x => x.assetbundle == voice.assetBundleName && x.file == voice.assetName).Select(x => x.text).FirstOrDefault();
                                if (!text.IsNullOrEmpty())
                                    return;
                            }
                        }
                    }
                }
            }
            if (text.IsNullOrEmpty())
                return;

            DisplaySubtitle(voice, text);
        }

        internal static void DisplayHSubtitle(LoadVoice voice)
        {
            List<HActionBase> lstProc = (List<HActionBase>)AccessTools.Field(typeof(HSceneProc), "lstProc").GetValue(Subtitles.HSceneProcInstance);
            HActionBase mode = lstProc[(int)Subtitles.HSceneProcInstance.flags.mode];
            HVoiceCtrl voicectrl = (HVoiceCtrl)AccessTools.Field(typeof(HActionBase), "voice").GetValue(mode);

            //At the start of the H scene, all the text was loaded. Look through the loaded stuff and find the text for the current spoken voice.
            string text = "";
            FindText();
            void FindText()
            {
                foreach (var a in voicectrl.dicVoiceIntos)
                {
                    foreach (var b in a)
                    {
                        foreach (var c in b.Value)
                        {
                            text = c.Value.Where(x => x.Value.pathAsset == voice.assetBundleName && x.Value.nameFile == voice.assetName).Select(x => x.Value.word).FirstOrDefault();
                            if (!text.IsNullOrEmpty())
                                return;
                        }
                    }
                }
            }
            if (text.IsNullOrEmpty())
                return;

            DisplaySubtitle(voice, text, init:false);
        }
    }
}