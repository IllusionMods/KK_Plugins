using BepInEx;
using BepInEx.Logging;
using ChaCustom;
using ExtensibleSaveFormat;
using HarmonyLib;
using KKAPI.Maker;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using Timer = System.Timers.Timer;

namespace KK_Plugins
{
    /// <summary>
    /// Watches the character folders for changes and updates the character/coordinate list in the chara maker and studio.
    /// </summary>
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    [BepInDependency(ExtendedSave.GUID)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class ReloadCharaListOnChange : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.reloadcharalistonchange";
        public const string PluginName = "Reload Character List On Change";
        public const string PluginNameInternal = Constants.Prefix + "_ReloadCharaListOnChange";
        public const string Version = "1.5.1";
        internal static new ManualLogSource Logger;
        private static FileSystemWatcher CharacterCardWatcher;
        private static FileSystemWatcher CoordinateCardWatcher;
        private static FileSystemWatcher StudioFemaleCardWatcher;
        private static FileSystemWatcher StudioMaleCardWatcher;
        private static FileSystemWatcher StudioCoordinateCardWatcher;
        private static bool DoRefresh;
        private static bool EventFromCharaMaker;
        private static bool InCharaMaker;
        private static CustomCharaFile CustomCharaFileInstance;
        private static CustomCoordinateFile CustomCoordinateFileInstance;
        private static Studio.CharaList StudioFemaleListInstance;
        private static Studio.CharaList StudioMaleListInstance;
        private static object StudioCoordinateListInstance;
        private static Timer CardTimer;
        private static CardEventType EventType;
        private static readonly ReaderWriterLockSlim rwlock = new ReaderWriterLockSlim();

        internal void Main()
        {
            Logger = base.Logger;

            //KK Party may not have these directories when first run, create them to avoid errors
            Directory.CreateDirectory(CC.Paths.FemaleCardPath);
            Directory.CreateDirectory(CC.Paths.MaleCardPath);
            Directory.CreateDirectory(CC.Paths.CoordinateCardPath);

            SceneManager.sceneUnloaded += SceneUnloaded;

            var harmony = Harmony.CreateAndPatchAll(typeof(Hooks));
            harmony.Patch(typeof(Studio.MPCharCtrl).GetNestedType("CostumeInfo", BindingFlags.NonPublic).GetMethod("InitFileList", AccessTools.all),
                          new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.StudioCoordinateListPrefix), AccessTools.all)));
        }
        /// <summary>
        /// On a game update run the actual refresh. It must be run from an update or it causes all sorts of errors.
        /// </summary>
        internal void Update()
        {
            if (EventFromCharaMaker && DoRefresh)
            {
                //If we saved or deleted a card from the chara maker itself there's no need to refresh, the game will handle that.
                CardTimer.Dispose();
                EventFromCharaMaker = false;
                DoRefresh = false;
            }
            else if (DoRefresh)
            {
                RefreshList();
                CardTimer.Dispose();
                DoRefresh = false;
            }
            if (Input.GetKeyDown(KeyCode.F5) && MakerAPI.InsideAndLoaded)
            {
                EventType = CardEventType.CharaMakerCharacter;
                RefreshList();
            }
        }
        /// <summary>
        /// When cards are added or removed from the folder set a flag
        /// </summary>
        private static void CardEvent(CardEventType eventType)
        {
            //Needs to be locked since dumping a bunch of cards in the folder will trigger this event a whole bunch of times that all run at once
            rwlock.EnterWriteLock();

            try
            {
                EventType = eventType;

                //Start a timer which will be reset every time a card is added/removed for when the user dumps in a whole bunch at once
                //Once the timer elapses, a flag will be set to do the refresh, which will then happen on the next Update.
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
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, ex);
                CardTimer?.Dispose();
            }

            rwlock.ExitWriteLock();
        }
        /// <summary>
        /// Refresh the list
        /// </summary>
        private static void RefreshList()
        {
            try
            {
                //Turn off resolving to prevent spam since modded stuff isn't relevant for making this list.
                ExtendedSave.LoadEventsEnabled = false;
                switch (EventType)
                {
                    case CardEventType.CharaMakerCharacter:
                        var initializeChara = typeof(CustomCharaFile).GetMethod("Initialize", AccessTools.all);
                        if (initializeChara != null)
                            if (initializeChara.GetParameters().Length == 0)
                                initializeChara.Invoke(CustomCharaFileInstance, null);
                            else
                                initializeChara.Invoke(CustomCharaFileInstance, new object[] { true, false });
                        break;
                    case CardEventType.CharaMakerCoordinate:
                        var initializeCoordinate = typeof(CustomCoordinateFile).GetMethod("Initialize", AccessTools.all);
                        if (initializeCoordinate != null)
                            if (initializeCoordinate.GetParameters().Length == 0)
                                initializeCoordinate.Invoke(CustomCoordinateFileInstance, null);
                            else
                                initializeCoordinate.Invoke(CustomCoordinateFileInstance, new object[] { true, false });
                        break;
                    case CardEventType.StudioFemale:
                        StudioFemaleListInstance.InitCharaList(true);
                        break;
                    case CardEventType.StudioMale:
                        StudioMaleListInstance.InitCharaList(true);
                        break;
                    case CardEventType.StudioCoordinate:
                        var sex = Traverse.Create(StudioCoordinateListInstance).Field("sex").GetValue();
                        typeof(Studio.MPCharCtrl).GetNestedType("CostumeInfo", BindingFlags.NonPublic).GetMethod("InitList", AccessTools.all)?.Invoke(StudioCoordinateListInstance, new object[] { 100 });
                        Traverse.Create(StudioCoordinateListInstance).Field("sex").SetValue(sex);
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error | LogLevel.Message, "An error occured attempting to refresh the list.");
                Logger.Log(LogLevel.Error, $"KK_ReloadCharaListOnChange error: {ex.Message}");
                Logger.Log(LogLevel.Error, ex);
            }
            finally
            {
                ExtendedSave.LoadEventsEnabled = true;
            }
        }
        /// <summary>
        /// End the file watcher and set variables back to default for next time the chara maker is started
        /// </summary>
        private static void SceneUnloaded(Scene s)
        {
            if (s.name == "CustomScene")
            {
                InCharaMaker = false;
                DoRefresh = false;
                EventFromCharaMaker = false;
                CardTimer?.Dispose();
                CardTimer = null;
                CharacterCardWatcher?.Dispose();
                CharacterCardWatcher = null;
                CoordinateCardWatcher?.Dispose();
                CoordinateCardWatcher = null;
            }
        }

        public enum CardEventType { CharaMakerCharacter, CharaMakerCoordinate, StudioMale, StudioFemale, StudioCoordinate }
    }
}
