using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UniRx;

namespace KK_Plugins
{
    public partial class Subtitles
    {
        public partial class Caption
        {
            internal static void DisplayHSubtitle(LoadAudioBase voice)
            {
                List<HActionBase> lstProc = (List<HActionBase>)AccessTools.Field(typeof(VRHScene), "lstProc").GetValue(VRHSceneInstance);
                HActionBase mode = lstProc[(int)VRHSceneInstance.flags.mode];
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

                DisplayVRSubtitle(voice.gameObject, text);
            }
        }
    }
}