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
        private static FileSystemWatcher CharacterCardWatcher;
        private static FileSystemWatcher CoordinateCardWatcher;
        private static string FemaleCardPath = Path.Combine(Paths.GameRootPath, "UserData\\chara\\female");
        private static string MaleCardPath = Path.Combine(Paths.GameRootPath, "UserData\\chara\\male");
        private static string CoordinateCardPath = Path.Combine(Paths.GameRootPath, "UserData\\coordinate");
        private static List<CardEventInfo> CharacterCardEventList = new List<CardEventInfo>();
        private static List<CardEventInfo> CoordinateCardEventList = new List<CardEventInfo>();
        private static bool DoRefresh = false;
        private static bool EventFromCharaMaker = false;
        private static bool InCharaMaker = false;
        private static CustomFileListCtrl listCtrlCharacter;
        private static List<CustomFileInfo> lstFileInfoCharacter;
        private static CustomFileListCtrl listCtrlCoordinate;
        private static List<CustomFileInfo> lstFileInfoCoordinate;
        private static Timer CardTimer;
        private static ReaderWriterLockSlim rwlock = new ReaderWriterLockSlim();
        private static int ModeSex = 0;

        public void Main()
        {
            SceneManager.sceneUnloaded += SceneUnloaded;

            var harmony = HarmonyInstance.Create("com.deathweasel.bepinex.reloadcharalistonchange");
            harmony.PatchAll(typeof(KK_ReloadCharaListOnChange));
        }
        /// <summary>
        /// When cards are added or removed from the folder create a list of them
        /// </summary>
        private static void CreateCharacterEventLists(object sender, FileSystemEventArgs args)
        {
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

                CharacterCardEventList.Add(new CardEventInfo(CardName, CardPath, args.ChangeType));
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, ex);
                CardTimer.Dispose();
                CharacterCardEventList.Clear();
            }
            finally
            {
                rwlock.ExitWriteLock();
            }
        }
        /// <summary>
        /// When cards are added or removed from the folder create a list of them
        /// </summary>
        private static void CreateCoordinateEventLists(object sender, FileSystemEventArgs args)
        {
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

                CoordinateCardEventList.Add(new CardEventInfo(CardName, CardPath, args.ChangeType));
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, ex);
                CardTimer.Dispose();
                CharacterCardEventList.Clear();
            }
            finally
            {
                rwlock.ExitWriteLock();
            }
        }
        /// <summary>
        /// Add or remove the cards from the list, then refresh the list
        /// </summary>
        private static void RefreshCharacterList()
        {
            bool DidAddOrRemove = false;

            //Turn off resolving to prevent spam since modded stuff isn't relevent for making this list.
            Sideloader.AutoResolver.Hooks.IsResolving = false;

            try
            {
                foreach (CardEventInfo CardEvent in CharacterCardEventList)
                {
                    if (CardEvent.EventType == WatcherChangeTypes.Deleted)
                    {
                        CustomFileInfo CardInfo = lstFileInfoCharacter.FirstOrDefault(x => x.FileName == CardEvent.CardName);
                        if (CardInfo == null)
                            Logger.Log(LogLevel.Warning, $"{CardEvent.CardName}.png was removed from the folder but could not be found on character list, skipping.");
                        else
                        {
                            listCtrlCharacter.RemoveList(CardInfo.index);
                            DidAddOrRemove = true;
                        }
                    }
                    else if (CardEvent.EventType == WatcherChangeTypes.Created)
                    {
                        ChaFileControl AddedCharacter = new ChaFileControl();
                        if (!AddedCharacter.LoadCharaFile(CardEvent.CardPath))
                        {
                            Logger.Log(LogLevel.Warning | LogLevel.Message, $"{CardEvent.CardName}.png is not a character card.");
                        }
                        else
                        {
                            if (AddedCharacter.parameter.sex != ModeSex)
                            {
                                Logger.Log(LogLevel.Warning | LogLevel.Message, $"{CardEvent.CardName}.png is not a {((ModeSex == 0) ? "male" : "female")} card.");
                                continue;
                            }

                            string CardClub = Manager.Game.ClubInfos.TryGetValue(AddedCharacter.parameter.clubActivities, out ClubInfo.Param ClubParam) ? ClubParam.Name : "不明";
                            string CardPersonality = Singleton<Manager.Voice>.Instance.voiceInfoDic.TryGetValue(AddedCharacter.parameter.personality, out VoiceInfo.Param PersonalityParam) ? PersonalityParam.Personality : "不明";
                            DateTime CardTime = File.GetLastWriteTime(CardEvent.CardPath);

                            //Find the highest index to use for our new index
                            int Index;
                            if (lstFileInfoCharacter.Count == 0)
                                Index = 0;
                            else
                                Index = lstFileInfoCharacter.Max(x => x.index) + 1;

                            listCtrlCharacter.AddList(Index, AddedCharacter.parameter.fullname, CardClub, CardPersonality, CardEvent.CardPath, CardEvent.CardName, CardTime);
                            DidAddOrRemove = true;
                        }
                    }
                }
                if (DidAddOrRemove)
                    listCtrlCharacter.ReCreate();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error | LogLevel.Message, "An error occured attempting to refresh the character list. Please restart the chara maker.");
                Logger.Log(LogLevel.Error, $"KK_ReloadCharaListOnChange error: {ex.Message}");
                Logger.Log(LogLevel.Error, ex);
            }
            Sideloader.AutoResolver.Hooks.IsResolving = true;
        }
        /// <summary>
        /// Add or remove the cards from the list, then refresh the list
        /// </summary>
        private static void RefreshCoordinateList()
        {
            bool DidAddOrRemove = false;

            //Turn off resolving to prevent spam since modded stuff isn't relevent for making this list.
            Sideloader.AutoResolver.Hooks.IsResolving = false;

            try
            {
                foreach (CardEventInfo CardEvent in CoordinateCardEventList)
                {
                    if (CardEvent.EventType == WatcherChangeTypes.Deleted)
                    {
                        CustomFileInfo CardInfo = lstFileInfoCoordinate.FirstOrDefault(x => x.FileName == CardEvent.CardName);
                        if (CardInfo == null)
                            Logger.Log(LogLevel.Warning, $"{CardEvent.CardName}.png was removed from the folder but could not be found on character list, skipping.");
                        else
                        {
                            listCtrlCoordinate.RemoveList(CardInfo.index);
                            DidAddOrRemove = true;
                        }
                    }
                    else if (CardEvent.EventType == WatcherChangeTypes.Created)
                    {
                        ChaFileCoordinate AddedCoordinate = new ChaFileCoordinate();

                        if (!AddedCoordinate.LoadFile(CardEvent.CardPath))
                        {
                            Logger.Log(LogLevel.Warning | LogLevel.Message, $"{CardEvent.CardName}.png is not a coordinate card.");
                        }
                        else
                        {
                            DateTime CardTime = File.GetLastWriteTime(CardEvent.CardPath);

                            //Find the highest index to use for our new index
                            int Index;
                            if (lstFileInfoCoordinate.Count == 0)
                                Index = 0;
                            else
                                Index = lstFileInfoCoordinate.Max(x => x.index) + 1;

                            listCtrlCoordinate.AddList(Index, AddedCoordinate.coordinateName, string.Empty, string.Empty, CardEvent.CardPath, CardEvent.CardName, CardTime);
                            DidAddOrRemove = true;
                        }
                    }
                }
                if (DidAddOrRemove)
                    listCtrlCoordinate.ReCreate();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error | LogLevel.Message, "An error occured attempting to refresh the coordinate list. Please restart the chara maker.");
                Logger.Log(LogLevel.Error, $"KK_ReloadCharaListOnChange error: {ex.Message}");
                Logger.Log(LogLevel.Error, ex);
            }
            Sideloader.AutoResolver.Hooks.IsResolving = true;
        }
        /// <summary>
        /// On a game update run the actual refresh. It must be run from an update or it causes all sorts of errors for reasons I can't figure out.
        /// </summary>
        private void Update()
        {
            if (EventFromCharaMaker && DoRefresh)
            {
                //If we saved or deleted a card from the chara maker itself clear the events so cards don't get added twice
                CharacterCardEventList.Clear();
                CoordinateCardEventList.Clear();
                CardTimer.Dispose();
                EventFromCharaMaker = false;
                DoRefresh = false;
            }
            else if (DoRefresh)
            {
                if (CharacterCardEventList.Count > 0)
                    RefreshCharacterList();
                if (CoordinateCardEventList.Count > 0)
                    RefreshCoordinateList();
                CharacterCardEventList.Clear();
                CoordinateCardEventList.Clear();
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
                DoRefresh = false;
                EventFromCharaMaker = false;
                if (CardTimer != null)
                    CardTimer.Dispose();
                CharacterCardWatcher.Dispose();
                CharacterCardEventList.Clear();
                CoordinateCardWatcher.Dispose();
                CoordinateCardEventList.Clear();
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
        /// When saving the a new character card in game set a flag
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChaFileControl), "SaveCharaFile", new[] { typeof(BinaryWriter), typeof(bool) })]
        public static void SaveCharaFilePrefix()
        {
            if (InCharaMaker)
                if (Singleton<CustomBase>.Instance.customCtrl.saveNew == true)
                    EventFromCharaMaker = true;
        }
        /// <summary>
        /// When saving the a new coordinate card in game set a flag
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChaFileCoordinate), nameof(ChaFileCoordinate.SaveFile), new[] { typeof(string) })]
        public static void SaveCoordinateFilePrefix(string path)
        {
            if (InCharaMaker)
                if (!File.Exists(path)) //saving new
                    EventFromCharaMaker = true;
        }
        /// <summary>
        /// When deleting a chara card in game set a flag
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CustomCharaFile), "DeleteCharaFile")]
        public static void DeleteCharaFilePrefix()
        {
            if (InCharaMaker)
                EventFromCharaMaker = true;
        }
        /// <summary>
        /// When deleting a coordinate card in game set a flag
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CustomCoordinateFile), "DeleteCoordinateFile")]
        public static void DeleteCoordinateFilePrefix()
        {
            if (InCharaMaker)
                EventFromCharaMaker = true;
        }
        /// <summary>
        /// Initialize the character card file watcher when the chara maker starts
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CustomCharaFile), "Initialize")]
        public static void CustomCharaFileInitializePrefix(CustomCharaFile __instance)
        {
            ModeSex = Singleton<CustomBase>.Instance.modeSex;

            //Get some references to fields we'll be using later on
            listCtrlCharacter = Traverse.Create(__instance).Field("listCtrl").GetValue<CustomFileListCtrl>();
            lstFileInfoCharacter = Traverse.Create(listCtrlCharacter).Field("lstFileInfo").GetValue<List<CustomFileInfo>>();

            CharacterCardWatcher = new FileSystemWatcher();
            if (ModeSex == 0)
                CharacterCardWatcher.Path = MaleCardPath;
            else
                CharacterCardWatcher.Path = FemaleCardPath;
            CharacterCardWatcher.NotifyFilter = NotifyFilters.FileName;
            CharacterCardWatcher.Filter = "*.png";
            CharacterCardWatcher.EnableRaisingEvents = true;
            CharacterCardWatcher.Created += new FileSystemEventHandler(CreateCharacterEventLists);
            CharacterCardWatcher.Deleted += new FileSystemEventHandler(CreateCharacterEventLists);

            InCharaMaker = true;
        }
        /// <summary>
        /// Initialize the coordinate card file watcher when the chara maker starts
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CustomCoordinateFile), "Initialize")]
        public static void CustomCoordinateFileInitializePrefix(CustomCoordinateFile __instance)
        {
            //Get some references to fields we'll be using later on
            listCtrlCoordinate = Traverse.Create(__instance).Field("listCtrl").GetValue<CustomFileListCtrl>();
            lstFileInfoCoordinate = Traverse.Create(listCtrlCoordinate).Field("lstFileInfo").GetValue<List<CustomFileInfo>>();

            CoordinateCardWatcher = new FileSystemWatcher();
            CoordinateCardWatcher.Path = CoordinateCardPath;
            CoordinateCardWatcher.NotifyFilter = NotifyFilters.FileName;
            CoordinateCardWatcher.Filter = "*.png";
            CoordinateCardWatcher.EnableRaisingEvents = true;
            CoordinateCardWatcher.Created += new FileSystemEventHandler(CreateCoordinateEventLists);
            CoordinateCardWatcher.Deleted += new FileSystemEventHandler(CreateCoordinateEventLists);
        }
    }
}
