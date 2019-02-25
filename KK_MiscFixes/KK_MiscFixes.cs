using ActionGame;
using BepInEx;
using BepInEx.Logging;
using ChaCustom;
using FreeH;
using Harmony;
using Illusion.Game;
using Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Logger = BepInEx.Logger;
using static ExtensibleSaveFormat.ExtendedSave;

namespace KK_MiscFixes
{
    [BepInPlugin(GUID, PluginName, Version)]
    public class KK_MiscFixes : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.miscfixes";
        public const string PluginName = "Misc Fixes";
        public const string PluginNameInternal = "KK_MiscFixes";
        public const string Version = "1.0";

        void Main()
        {
            var harmony = HarmonyInstance.Create(GUID);
            harmony.PatchAll(typeof(KK_MiscFixes));
        }

        /// <summary>
        /// Generates the list of characters for Free H but with LoadEventsEnabled disabled.
        /// Reloads the character with it enabled to ensure extended data does get loaded, but only for that one character and not all.
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(FreeHClassRoomCharaFile), "Start")]
        public static bool Start(FreeHClassRoomCharaFile __instance)
        {
            ClassRoomFileListCtrl listCtrl = Traverse.Create(__instance).Field("listCtrl").GetValue<ClassRoomFileListCtrl>();
            ReactiveProperty<ChaFileControl> info = Traverse.Create(__instance).Field("info").GetValue<ReactiveProperty<ChaFileControl>>();
            Button enterButton = Traverse.Create(__instance).Field("enterButton").GetValue<Button>();

            var _heroine = (ReactiveProperty<SaveData.Heroine>)Singleton<FreeHCharaSelect>.Instance.GetType().GetField("_heroine", AccessTools.all).GetValue(Singleton<FreeHCharaSelect>.Instance);
            var _player = (ReactiveProperty<SaveData.Player>)Singleton<FreeHCharaSelect>.Instance.GetType().GetField("_player", AccessTools.all).GetValue(Singleton<FreeHCharaSelect>.Instance);

            FolderAssist folderAssist = new FolderAssist();
            folderAssist.CreateFolderInfoEx(UserData.Path + (__instance.sex == 0 ? "chara/male/" : "chara/female/"), new string[] { "*.png" }, true);

            listCtrl.ClearList();
            int fileCount = folderAssist.GetFileCount();
            int num = 0;
            Dictionary<int, ChaFileControl> chaFileDic = new Dictionary<int, ChaFileControl>();

            //Disable extended save load events to speed up list creation
            LoadEventsEnabled = false;

            for (int i = 0; i < fileCount; i++)
            {
                FolderAssist.FileInfo fileInfo = folderAssist.lstFile[i];
                ChaFileControl chaFileControl = new ChaFileControl();
                if (chaFileControl.LoadCharaFile(fileInfo.FullPath, 255, false, true))
                {
                    if (chaFileControl.parameter.sex == __instance.sex)
                    {
                        string club = string.Empty;
                        string personality = string.Empty;
                        if (__instance.sex == 0)
                        {
                            listCtrl.DisableAddInfo();
                        }
                        else
                        {
                            personality = Singleton<Voice>.Instance.voiceInfoDic.TryGetValue(chaFileControl.parameter.personality, out VoiceInfo.Param param) ? param.Personality : "不明";
                            club = Game.ClubInfos.TryGetValue(chaFileControl.parameter.clubActivities, out ClubInfo.Param param2) ? param2.Name : "不明";
                        }
                        listCtrl.AddList(num, chaFileControl.parameter.fullname, club, personality, fileInfo.FullPath, fileInfo.FileName, fileInfo.time, false, false);
                        chaFileDic.Add(num, chaFileControl);
                        num++;
                    }
                }
            }

            //Re-enable events
            LoadEventsEnabled = true;

            listCtrl.OnPointerClick += delegate (CustomFileInfo customFileInfo)
            {
                info.Value = (customFileInfo != null) ? chaFileDic[customFileInfo.index] : null;
                Utils.Sound.Play(SystemSE.sel);
            };

            listCtrl.Create(delegate (CustomFileInfoComponent fic)
            {
                if (fic == null)
                {
                    return;
                }
                fic.transform.GetChild(0).GetOrAddComponent<PreviewDataComponent>().SetChaFile(chaFileDic[fic.info.index]);
            });
            (from p in info
             select p != null).SubscribeToInteractable(enterButton);

            enterButton.OnClickAsObservable().Subscribe(delegate (Unit _)
            {
                Logger.Log(LogLevel.Info, "Button Clicked");
                ChaFileControl chaFileControl = new ChaFileControl();

                chaFileControl.LoadCharaFile(info.Value.charaFileName, info.Value.parameter.sex, false, true);

                if (__instance.sex == 0)
                    _player.Value = new SaveData.Player(chaFileControl, false);
                else
                    _heroine.Value = new SaveData.Heroine(chaFileControl, false);

                Singleton<Scene>.Instance.UnLoad();
            });

            Button[] source = new Button[] { enterButton };

            source.ToList().ForEach(delegate (Button bt)
            {
                bt.OnClickAsObservable().Subscribe(delegate (Unit _)
                {
                    Utils.Sound.Play(SystemSE.ok_s);
                });
            });

            //Cancel the original method
            return false;
        }
    }
}
