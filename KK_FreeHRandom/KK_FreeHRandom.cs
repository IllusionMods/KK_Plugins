using BepInEx;
using BepInEx.Logging;
using Harmony;
using Illusion.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using UnityEngine;
using Logger = BepInEx.Logger;

namespace KK_FreeHRandom
{
    [BepInPlugin(GUID, PluginName, Version)]
    public class KK_FreeHRandom : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.freehrandom";
        public const string PluginName = "Free H Random";
        public const string PluginNameInternal = "KK_FreeHRandom";
        public const string Version = "1.0";

        [DisplayName("Random male")]
        [Category("Config")]
        [Description("Whether a random male will be selected as well")]
        public static ConfigWrapper<bool> RandomMale { get; private set; }
        [DisplayName("Random Hotkey")]
        [Description("The key that triggers the random selection of characters in the Free H select screen.")]
        public static SavedKeyboardShortcut RandomHotkey { get; private set; }

        private void Main()
        {
            RandomMale = new ConfigWrapper<bool>("RandomMale", PluginNameInternal, true);
            RandomHotkey = new SavedKeyboardShortcut("RandomHotkey", PluginNameInternal, new KeyboardShortcut(KeyCode.F5));
        }

        private void Update()
        {
            try
            {
                if (Singleton<Manager.Scene>.Instance.NowSceneNames.Any(sceneName => sceneName == "FreeH"))
                {
                    if (RandomHotkey.IsDown())
                    {
                        //Get some random female cards
                        FreeHScene instance = Singleton<FreeHScene>.Instance;
                        FreeHScene.Member member = (FreeHScene.Member)instance.GetType().GetField("member", AccessTools.all).GetValue(instance);
                        FolderAssist folderAssist = new FolderAssist();
                        folderAssist.CreateFolderInfoEx(Path.Combine(UserData.Path, "chara/female/"), new string[] { "*.png" }, true);
                        List<string> list = (from n in folderAssist.lstFile.Shuffle() select n.FullPath).ToList();

                        if (list.Count == 0)
                            return;

                        //Load the main female
                        ChaFileControl chaFileControl = new ChaFileControl();
                        if (chaFileControl.LoadCharaFile(list[0], 1, false, true))
                        {
                            member.resultHeroine.SetValueAndForceNotify(new SaveData.Heroine(chaFileControl, false));

                            //Load the second female
                            ChaFileControl chaFileControl2 = new ChaFileControl();
                            if (list.Count >= 2 && chaFileControl2.LoadCharaFile(list[1], 1, false, true))
                            {
                                member.resultPartner.SetValueAndForceNotify(new SaveData.Heroine(chaFileControl2, false));
                            }
                        }

                        //Load the male card, if allowed
                        if (RandomMale.Value)
                        {
                            folderAssist = new FolderAssist();
                            folderAssist.CreateFolderInfoEx(Path.Combine(UserData.Path, "chara/male/"), new string[] { "*.png" }, true);
                            list = (from n in folderAssist.lstFile.Shuffle() select n.FullPath).ToList();

                            if (list.Count == 0)
                                return;

                            ChaFileControl chaFileControlMale = new ChaFileControl();
                            if (chaFileControlMale.LoadCharaFile(list[1], 0, false, true))
                            {
                                member.resultPlayer.SetValueAndForceNotify(new SaveData.Player(chaFileControlMale, false));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "Error in KK_FreeHRandom");
                Logger.Log(LogLevel.Error, ex);
            }
        }
    }
}
