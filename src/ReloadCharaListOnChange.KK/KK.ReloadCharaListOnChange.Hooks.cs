using ChaCustom;
using HarmonyLib;
using KKAPI.Maker;
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
            private static void SaveCharaFilePrefix()
            {
                if (MakerAPI.InsideAndLoaded && Singleton<CustomBase>.Instance.customCtrl.saveNew)
                    EventFromCharaMaker = true;
            }
            /// <summary>
            /// When saving a new coordinate card in game set a flag
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(ChaFileCoordinate), nameof(ChaFileCoordinate.SaveFile), typeof(string))]
            private static void SaveCoordinateFilePrefix(string path)
            {
                if (MakerAPI.InsideAndLoaded && !File.Exists(path))
                    EventFromCharaMaker = true;
            }
            /// <summary>
            /// When deleting a chara card in game set a flag
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(CustomCharaFile), "DeleteCharaFile")]
            private static void DeleteCharaFilePrefix()
            {
                if (MakerAPI.InsideAndLoaded)
                    EventFromCharaMaker = true;
            }
            /// <summary>
            /// When deleting a coordinate card in game set a flag
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(CustomCoordinateFile), "DeleteCoordinateFile")]
            private static void DeleteCoordinateFilePrefix()
            {
                if (MakerAPI.InsideAndLoaded)
                    EventFromCharaMaker = true;
            }
            /// <summary>
            /// Initialize the file watcher once the list has been initiated
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(Studio.CharaList), "InitFemaleList")]
            private static void StudioFemaleListPrefix(Studio.CharaList __instance)
            {
                if (StudioFemaleCardWatcher == null)
                {
                    StudioFemaleListInstance = __instance;

                    StudioFemaleCardWatcher = new FileSystemWatcher();
                    StudioFemaleCardWatcher.Path = CC.Paths.FemaleCardPath;
                    StudioFemaleCardWatcher.NotifyFilter = NotifyFilters.FileName;
                    StudioFemaleCardWatcher.Filter = "*.png";
                    StudioFemaleCardWatcher.EnableRaisingEvents = true;
                    StudioFemaleCardWatcher.Created += (o, ee) => CardEvent(ee.FullPath, CardEventType.StudioFemale);
                    StudioFemaleCardWatcher.Deleted += (o, ee) => CardEvent(ee.FullPath, CardEventType.StudioFemale);
                    StudioFemaleCardWatcher.IncludeSubdirectories = true;
                }
            }
            /// <summary>
            /// Initialize the file watcher once the list has been initiated
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(Studio.CharaList), "InitMaleList")]
            private static void StudioMaleListPrefix(Studio.CharaList __instance)
            {
                if (StudioMaleCardWatcher == null)
                {
                    StudioMaleListInstance = __instance;

                    StudioMaleCardWatcher = new FileSystemWatcher();
                    StudioMaleCardWatcher.Path = CC.Paths.MaleCardPath;
                    StudioMaleCardWatcher.NotifyFilter = NotifyFilters.FileName;
                    StudioMaleCardWatcher.Filter = "*.png";
                    StudioMaleCardWatcher.EnableRaisingEvents = true;
                    StudioMaleCardWatcher.Created += (o, ee) => CardEvent(ee.FullPath, CardEventType.StudioMale);
                    StudioMaleCardWatcher.Deleted += (o, ee) => CardEvent(ee.FullPath, CardEventType.StudioMale);
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
                    StudioCoordinateCardWatcher.Created += (o, ee) => CardEvent(ee.FullPath, CardEventType.StudioCoordinate);
                    StudioCoordinateCardWatcher.Deleted += (o, ee) => CardEvent(ee.FullPath, CardEventType.StudioCoordinate);
                    StudioCoordinateCardWatcher.IncludeSubdirectories = true;
                }
            }
        }
    }
}