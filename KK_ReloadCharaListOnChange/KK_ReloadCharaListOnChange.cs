using BepInEx;
using BepInEx.Logging;
using Harmony;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ChaCustom;
using UnityEngine.SceneManagement;
using System.Threading;
using Timer = System.Timers.Timer;

/// <summary>
/// Watches the character folders for changes and updates the character list in the chara maker.
/// Probably should be expanded to support coordinates. Maybe the studio lists too.
/// </summary>
namespace KK_ReloadCharaListOnChange
{
    [BepInDependency("com.bepis.bepinex.sideloader")]
    [BepInPlugin("com.deathweasel.bepinex.reloadcharalistonchange", "Reload Chara List On Change", "1.0")]
    public class KK_ReloadCharaListOnChange : BaseUnityPlugin
    {
        private static FileSystemWatcher CharaCardWatcher;
        private static string FemaleCardPath = Path.Combine(Paths.GameRootPath, "UserData\\chara\\female");
        private static string MaleCardPath = Path.Combine(Paths.GameRootPath, "UserData\\chara\\male");
        private static List<CardEventInfo> CardEventList = new List<CardEventInfo>();
        private static bool DoRefresh = false;
        private static bool EventFromCharaMaker = false;
        private static bool InCharaMaker = false;
        private static CustomFileListCtrl listCtrl;
        private static List<CustomFileInfo> lstFileInfo;
        private static Timer CardTimer;
        private static ReaderWriterLockSlim rwlock = new ReaderWriterLockSlim();

        public void Main()
        {
            SceneManager.sceneUnloaded += SceneUnloaded;

            var harmony = HarmonyInstance.Create("com.deathweasel.bepinex.reloadcharalistonchange");
            harmony.PatchAll(typeof(KK_ReloadCharaListOnChange));
        }
        /// <summary>
        /// When cards are added or removed from the folder create a list of them
        /// </summary>
        private static void CreateEventLists(object sender, FileSystemEventArgs args)
        {
            //Don't add cards for the wrong sex
            if (args.FullPath.Contains("\\female\\") && Singleton<CustomBase>.Instance.modeSex != 1)
                return;
            if (args.FullPath.Contains("\\male\\") && Singleton<CustomBase>.Instance.modeSex != 0)
                return;

            try
            {
                //Needs to be locked since dumping a bunch of cards in the folder will trigger this event a whole bunch of times that all run at once
                //which sometimes ends up with the list being modified while we're cycling through it later, which is very bad
                rwlock.EnterWriteLock();

                //Start a timer which will be reset every time a card is added/removed for when the user dumps in a whole bunch at once
                //Once the timer elapses, a flag will be set to do the refresh on all the cards that were add/remove at once
                if (CardTimer == null)
                {
                    //First file, start timer
                    CardTimer = new Timer(1000);
                    CardTimer.Elapsed += (o, ee) => DoRefresh = true;
                    CardTimer.Start();
                }
                else
                {
                    //Subsequent file, reset timer
                    CardTimer.Stop();
                    CardTimer.Start();
                }

                string CardPath = args.FullPath;
                string CardName = CardPath.Remove(0, CardPath.LastIndexOf('\\') + 1);
                CardName = CardName.Remove(CardName.IndexOf('.'));

                CardEventList.Add(new CardEventInfo(CardName, CardPath, args.ChangeType));
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, ex);
                CardTimer.Dispose();
                CardEventList.Clear();
            }
            finally
            {
                rwlock.ExitWriteLock();
            }
        }
        /// <summary>
        /// Add or remove the cards from the list, then refresh the list
        /// </summary>
        private static void RefreshCharaList()
        {
            //Turn off resolving to prevent spam since modded stuff isn't relevent for making this list.
            Sideloader.AutoResolver.Hooks.IsResolving = false;

            try
            {
                foreach (CardEventInfo CardEvent in CardEventList)
                {
                    if (CardEvent.EventType == WatcherChangeTypes.Deleted)
                    {
                        CustomFileInfo CardInfo = lstFileInfo.FirstOrDefault(x => x.FileName == CardEvent.CardName);
                        if (CardInfo == null)
                            Logger.Log(LogLevel.Error, "Card was removed from folder but could not be found on character list, skipping.");
                        else
                            listCtrl.RemoveList(CardInfo.index);
                    }
                    else if (CardEvent.EventType == WatcherChangeTypes.Created)
                    {
                        ChaFileControl AddedCharacter = new ChaFileControl();
                        AddedCharacter.LoadCharaFile(CardEvent.CardPath);

                        if (AddedCharacter == null)
                            Logger.Log(LogLevel.Error, "Card was added to the folder but could not be loaded.");
                        else
                        {
                            string CardClub = Manager.Game.ClubInfos.TryGetValue(AddedCharacter.parameter.clubActivities, out ClubInfo.Param ClubParam) ? ClubParam.Name : "不明";
                            string CardPersonality = Singleton<Manager.Voice>.Instance.voiceInfoDic.TryGetValue(AddedCharacter.parameter.personality, out VoiceInfo.Param PersonalityParam) ? PersonalityParam.Personality : "不明";
                            DateTime CardTime = File.GetLastWriteTime(CardEvent.CardPath);

                            //Find the highest index to use for our new index
                            int Index;
                            if (lstFileInfo.Count == 0)
                                Index = 0;
                            else
                                Index = lstFileInfo.Max(x => x.index) + 1;

                            listCtrl.AddList(Index, AddedCharacter.parameter.fullname, CardClub, CardPersonality, CardEvent.CardPath, CardEvent.CardName, CardTime);
                        }
                    }
                }
                listCtrl.ReCreate();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error | LogLevel.Message, "An error occured attempting to refresh the character list. Please restart the chara maker.");
                Logger.Log(LogLevel.Error, ex);
            }
            Sideloader.AutoResolver.Hooks.IsResolving = true;
            CardEventList.Clear();
        }
        /// <summary>
        /// On a game update run the actual refresh. It must be run from an update or it causes all sorts of errors for reasons I can't figure out.
        /// </summary>
        private void Update()
        {
            if (EventFromCharaMaker && DoRefresh)
            {
                //If we saved or deleted a card from the chara maker itself clear the events so cards don't get added twice
                CardEventList.Clear();
                CardTimer.Dispose();
                EventFromCharaMaker = false;
                DoRefresh = false;
            }
            else if (DoRefresh)
            {
                RefreshCharaList();
                CardTimer.Dispose();
                DoRefresh = false;
            }
        }
        /// <summary>
        /// Destroy the file watcher when the chara maker ends
        /// </summary>
        private void SceneUnloaded(Scene s)
        {
            if (s.name == "CustomScene")
            {
                InCharaMaker = false;
                CharaCardWatcher.Dispose();
                CardTimer.Dispose();
                CardEventList.Clear();
            }
        }

        private class CardEventInfo
        {
            public string CardName;
            public string CardPath;
            public WatcherChangeTypes EventType;

            public CardEventInfo(string cardName, string cardPath, WatcherChangeTypes eventType)
            {
                CardName = cardName;
                CardPath = cardPath;
                EventType = eventType;
            }
        }
        /// <summary>
        /// When saving the a card in game set a flag
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(ChaFileControl), "SaveCharaFile", new[] { typeof(BinaryWriter), typeof(bool) })]
        public static void SaveFilePrefix(bool savePng)
        {
            if (InCharaMaker)
                if (Singleton<CustomBase>.Instance.customCtrl.saveNew == true)
                    EventFromCharaMaker = true;
        }
        /// <summary>
        /// When deleting a card in game set a flag
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CustomCharaFile), "DeleteCharaFile")]
        public static void DeletePrefix()
        {
            if (InCharaMaker)
                EventFromCharaMaker = true;
        }
        /// <summary>
        /// Initialize the file watcher when the chara maker starts
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CustomCharaFile), "Initialize")]
        public static void InitializePrefix(CustomCharaFile __instance)
        {
            //Get some references to fields we'll be using later on
            listCtrl = Traverse.Create(__instance).Field("listCtrl").GetValue<CustomFileListCtrl>();
            lstFileInfo = Traverse.Create(listCtrl).Field("lstFileInfo").GetValue<List<CustomFileInfo>>();

            CharaCardWatcher = new FileSystemWatcher();
            if (Singleton<CustomBase>.Instance.modeSex == 0)
                CharaCardWatcher.Path = MaleCardPath;
            else
                CharaCardWatcher.Path = FemaleCardPath;
            CharaCardWatcher.NotifyFilter = NotifyFilters.FileName;
            CharaCardWatcher.Filter = "*.png";
            CharaCardWatcher.EnableRaisingEvents = true;
            CharaCardWatcher.Created += new FileSystemEventHandler(CreateEventLists);
            CharaCardWatcher.Deleted += new FileSystemEventHandler(CreateEventLists);

            InCharaMaker = true;
        }
    }
}
