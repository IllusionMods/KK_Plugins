using BepInEx;
using BepInEx.Logging;
using ChaCustom;
using Harmony;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEngine.SceneManagement;
using Timer = System.Timers.Timer;

namespace KK_ReloadCharaListOnChange
{
    /// <summary>
    /// Watches the character folders for changes and updates the character/coordinate list in the chara maker.
    /// Probably should be expanded to support studio lists too.
    /// </summary>
    [BepInDependency("com.bepis.bepinex.sideloader")]
    [BepInPlugin("com.deathweasel.bepinex.reloadcharalistonchange", "Reload Chara List On Change", Version)]
    public class KK_ReloadCharaListOnChange : BaseUnityPlugin
    {
        public const string Version = "1.3";
        private static FileSystemWatcher CharacterCardWatcher;
        private static FileSystemWatcher CoordinateCardWatcher;
        private static readonly string FemaleCardPath = Path.Combine(Paths.GameRootPath, "UserData\\chara\\female");
        private static readonly string MaleCardPath = Path.Combine(Paths.GameRootPath, "UserData\\chara\\male");
        private static readonly string CoordinateCardPath = Path.Combine(Paths.GameRootPath, "UserData\\coordinate");
        private static List<CardEventInfo> CharacterCardEventList = new List<CardEventInfo>();
        private static List<CardEventInfo> CoordinateCardEventList = new List<CardEventInfo>();
        private static bool DoRefresh = false;
        private static bool EventFromCharaMaker = false;
        private static bool InCharaMaker = false;
        private static CustomCharaFile listCtrlCharacter;
        private static CustomFileListCtrl listCtrlCoordinate;
        private static List<CustomFileInfo> lstFileInfoCoordinate;
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
                CardTimer?.Dispose();
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
                CardTimer?.Dispose();
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
            try
            {
                //Turn off resolving to prevent spam since modded stuff isn't relevent for making this list.
                Sideloader.AutoResolver.Hooks.IsResolving = false;

                typeof(CustomCharaFile).GetMethod("Initialize", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(listCtrlCharacter, null);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error | LogLevel.Message, "An error occured attempting to refresh the character list. Please restart the chara maker.");
                Logger.Log(LogLevel.Error, $"KK_ReloadCharaListOnChange error: {ex.Message}");
                Logger.Log(LogLevel.Error, ex);
            }
            finally
            {
                Sideloader.AutoResolver.Hooks.IsResolving = true;
            }
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
                            int Index = lstFileInfoCoordinate.Count == 0 ? 0 : lstFileInfoCoordinate.Max(x => x.index) + 1;

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
                CardTimer?.Dispose();
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
            //Get some references to fields we'll be using later on
            listCtrlCharacter = __instance;

            CharacterCardWatcher = new FileSystemWatcher();
            CharacterCardWatcher.Path = Singleton<CustomBase>.Instance.modeSex == 0 ? MaleCardPath : FemaleCardPath;
            CharacterCardWatcher.NotifyFilter = NotifyFilters.FileName;
            CharacterCardWatcher.Filter = "*.png";
            CharacterCardWatcher.EnableRaisingEvents = true;
            CharacterCardWatcher.Created += new FileSystemEventHandler(CreateCharacterEventLists);
            CharacterCardWatcher.Deleted += new FileSystemEventHandler(CreateCharacterEventLists);
            CharacterCardWatcher.IncludeSubdirectories = true;

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
