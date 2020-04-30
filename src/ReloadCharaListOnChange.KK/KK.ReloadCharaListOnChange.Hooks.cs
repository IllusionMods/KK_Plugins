using ChaCustom;
using HarmonyLib;
using System.IO;

namespace KK_Plugins
{
    public partial class ReloadCharaListOnChange
    {
        internal static class Hooks
        {
            /// <summary>
            /// When saving a new character card in game set a flag
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(ChaFileControl), "SaveCharaFile", typeof(BinaryWriter), typeof(bool))]
            internal static void SaveCharaFilePrefix()
            {
                if (InCharaMaker && Singleton<CustomBase>.Instance.customCtrl.saveNew == true)
                    EventFromCharaMaker = true;
            }
            /// <summary>
            /// When saving a new coordinate card in game set a flag
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(ChaFileCoordinate), nameof(ChaFileCoordinate.SaveFile), typeof(string))]
            internal static void SaveCoordinateFilePrefix(string path)
            {
                if (InCharaMaker && !File.Exists(path))
                    EventFromCharaMaker = true;
            }
            /// <summary>
            /// When deleting a chara card in game set a flag
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(CustomCharaFile), "DeleteCharaFile")]
            internal static void DeleteCharaFilePrefix()
            {
                if (InCharaMaker)
                    EventFromCharaMaker = true;
            }
            /// <summary>
            /// When deleting a coordinate card in game set a flag
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(CustomCoordinateFile), "DeleteCoordinateFile")]
            internal static void DeleteCoordinateFilePrefix()
            {
                if (InCharaMaker)
                    EventFromCharaMaker = true;
            }
            /// <summary>
            /// Initialize the character card file watcher when the chara maker starts
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(CustomCharaFile), "Initialize")]
            internal static void CustomCharaFileInitializePrefix(CustomCharaFile __instance)
            {
                if (CharacterCardWatcher == null)
                {
                    CustomCharaFileInstance = __instance;

                    CharacterCardWatcher = new FileSystemWatcher();
                    CharacterCardWatcher.Path = Singleton<CustomBase>.Instance.modeSex == 0 ? CC.Paths.MaleCardPath : CC.Paths.FemaleCardPath;
                    CharacterCardWatcher.NotifyFilter = NotifyFilters.FileName;
                    CharacterCardWatcher.Filter = "*.png";
                    CharacterCardWatcher.EnableRaisingEvents = true;
                    CharacterCardWatcher.Created += (o, ee) => CardEvent(CardEventType.CharaMakerCharacter);
                    CharacterCardWatcher.Deleted += (o, ee) => CardEvent(CardEventType.CharaMakerCharacter);
                    CharacterCardWatcher.IncludeSubdirectories = true;
                }

                InCharaMaker = true;
            }
            /// <summary>
            /// Initialize the coordinate card file watcher when the chara maker starts
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(CustomCoordinateFile), "Initialize")]
            internal static void CustomCoordinateFileInitializePrefix(CustomCoordinateFile __instance)
            {
                if (CoordinateCardWatcher == null)
                {
                    CustomCoordinateFileInstance = __instance;

                    CoordinateCardWatcher = new FileSystemWatcher();
                    CoordinateCardWatcher.Path = CC.Paths.CoordinateCardPath;
                    CoordinateCardWatcher.NotifyFilter = NotifyFilters.FileName;
                    CoordinateCardWatcher.Filter = "*.png";
                    CoordinateCardWatcher.EnableRaisingEvents = true;
                    CoordinateCardWatcher.Created += (o, ee) => CardEvent(CardEventType.CharaMakerCoordinate);
                    CoordinateCardWatcher.Deleted += (o, ee) => CardEvent(CardEventType.CharaMakerCoordinate);
                    CoordinateCardWatcher.IncludeSubdirectories = true;
                }
            }
            /// <summary>
            /// Initialize the file watcher once the list has been initiated
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(Studio.CharaList), "InitFemaleList")]
            internal static void StudioFemaleListPrefix(Studio.CharaList __instance)
            {
                if (StudioFemaleCardWatcher == null)
                {
                    StudioFemaleListInstance = __instance;

                    StudioFemaleCardWatcher = new FileSystemWatcher();
                    StudioFemaleCardWatcher.Path = CC.Paths.FemaleCardPath;
                    StudioFemaleCardWatcher.NotifyFilter = NotifyFilters.FileName;
                    StudioFemaleCardWatcher.Filter = "*.png";
                    StudioFemaleCardWatcher.EnableRaisingEvents = true;
                    StudioFemaleCardWatcher.Created += (o, ee) => CardEvent(CardEventType.StudioFemale);
                    StudioFemaleCardWatcher.Deleted += (o, ee) => CardEvent(CardEventType.StudioFemale);
                    StudioFemaleCardWatcher.IncludeSubdirectories = true;
                }
            }
            /// <summary>
            /// Initialize the file watcher once the list has been initiated
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(Studio.CharaList), "InitMaleList")]
            internal static void StudioMaleListPrefix(Studio.CharaList __instance)
            {
                if (StudioMaleCardWatcher == null)
                {
                    StudioMaleListInstance = __instance;

                    StudioMaleCardWatcher = new FileSystemWatcher();
                    StudioMaleCardWatcher.Path = CC.Paths.MaleCardPath;
                    StudioMaleCardWatcher.NotifyFilter = NotifyFilters.FileName;
                    StudioMaleCardWatcher.Filter = "*.png";
                    StudioMaleCardWatcher.EnableRaisingEvents = true;
                    StudioMaleCardWatcher.Created += (o, ee) => CardEvent(CardEventType.StudioMale);
                    StudioMaleCardWatcher.Deleted += (o, ee) => CardEvent(CardEventType.StudioMale);
                    StudioMaleCardWatcher.IncludeSubdirectories = true;
                }
            }
            /// <summary>
            /// Initialize the file watcher once the list has been initiated
            /// </summary>
            internal static void StudioCoordinateListPrefix(object __instance)
            {
                if (StudioCoordinateCardWatcher == null)
                {
                    StudioCoordinateListInstance = __instance;

                    StudioCoordinateCardWatcher = new FileSystemWatcher();
                    StudioCoordinateCardWatcher.Path = CC.Paths.CoordinateCardPath;
                    StudioCoordinateCardWatcher.NotifyFilter = NotifyFilters.FileName;
                    StudioCoordinateCardWatcher.Filter = "*.png";
                    StudioCoordinateCardWatcher.EnableRaisingEvents = true;
                    StudioCoordinateCardWatcher.Created += (o, ee) => CardEvent(CardEventType.StudioCoordinate);
                    StudioCoordinateCardWatcher.Deleted += (o, ee) => CardEvent(CardEventType.StudioCoordinate);
                    StudioCoordinateCardWatcher.IncludeSubdirectories = true;
                }
            }
        }
    }
}