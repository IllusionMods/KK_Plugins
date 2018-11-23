using BepInEx;
using BepInEx.Logging;
using Logger = BepInEx.Logger;
using Harmony;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sideloader.AutoResolver;

namespace KK_GUIDMigration
{
    [BepInDependency("com.bepis.bepinex.sideloader")]
    [BepInPlugin("com.deathweasel.bepinex.guidmigration", "GUID Migration", Version)]
    public class KK_GUIDMigration : BaseUnityPlugin
    {
        public const string Version = "1.1";
        private static List<MigrationInfo> MigrationInfoList = new List<MigrationInfo>();
        private static string GUIDMigrationFilePath = Path.Combine(Paths.GameRootPath, "bepinex\\KK_GUIDMigration.csv");
        private static bool DoMigration = false;

        void Main()
        {
            //Don't even bother if there's no mods directory
            if (Directory.Exists(Path.Combine(Paths.GameRootPath, "mods")) && Directory.Exists(Path.Combine(Paths.GameRootPath, "bepinex")))
            {
                var harmony = HarmonyInstance.Create("com.deathweasel.bepinex.guidmigration");
                harmony.PatchAll(typeof(KK_GUIDMigration));

                //Only do migration if there's a .csv file and it has stuff in it
                if (File.Exists(GUIDMigrationFilePath))
                {
                    GenerateMigrationInfo();
                    if (MigrationInfoList.Count > 0)
                        DoMigration = true;
                }
            }
        }
        /// <summary>
        /// Look through all the GUIDs, compare it to the MigrationInfoList, and do migration when necessary
        /// </summary>
        private static IEnumerable<ResolveInfo> MigrateGUID(IEnumerable<ResolveInfo> extInfo, string characterName)
        {
            List<ResolveInfo> extInfoNew = new List<ResolveInfo>();
            bool DidBlankGUIDMessage = false;

            try
            {
                if (extInfo == null)
                    return extInfo;

                foreach (ResolveInfo a in extInfo)
                {
                    if (a.GUID.IsNullOrEmpty())
                    {
                        //Don't add empty GUID to the new list, this way CompatibilityResolve will treat it as a hard mod and attempt to find a match
                        if (!DidBlankGUIDMessage) //No need to spam it for every single thing
                        {
                            Logger.Log(LogLevel.Warning | LogLevel.Message, $"[{characterName}] Blank GUID detected, attempting Compatibility Resolve");
                            DidBlankGUIDMessage = true;
                        }
                    }
                    else if (DoMigration)
                    {
                        string propertyWithoutPrefix = a.Property;

                        //Remove outfit and accessory prefixes for searching purposes
                        if (propertyWithoutPrefix.StartsWith("outfit"))
                            propertyWithoutPrefix = propertyWithoutPrefix.Remove(0, propertyWithoutPrefix.IndexOf('.') + 1);
                        if (propertyWithoutPrefix.StartsWith("accessory"))
                            propertyWithoutPrefix = propertyWithoutPrefix.Remove(0, propertyWithoutPrefix.IndexOf('.') + 1);

                        MigrationInfo info = MigrationInfoList.Where(x => (x.Property == propertyWithoutPrefix && x.OldID == a.Slot && x.OldGUID == a.GUID)
                                                                       || (x.Property == "*" && x.OldGUID == a.GUID)).FirstOrDefault();
                        if (info == null)
                        {
                            //This item does not need to be migrated
                            extInfoNew.Add(a);
                        }
                        else if (info.Property == "*") //* assumes only the GUID changed while the IDs stayed the same
                        {
                            ResolveInfo GUIDCheckOld = UniversalAutoResolver.LoadedResolutionInfo.FirstOrDefault(x => x.GUID == a.GUID);

                            if (GUIDCheckOld == null)
                            {
                                //We do not have the old mod installed, do migration. Whether we have the new mod is irrelevant.
                                //If we don't have the new mod the user will get a missing mod warning for the new mod since they should be using that instead.
                                //If we do it will load correctly.
                                Logger.Log(LogLevel.Info, $"Migrating GUID {info.OldGUID} -> {info.NewGUID}");
                                ResolveInfo b = new ResolveInfo();
                                b = a;
                                b.GUID = info.NewGUID;
                                extInfoNew.Add(b);
                            }
                            else
                            {
                                ResolveInfo GUIDCheckNew = UniversalAutoResolver.LoadedResolutionInfo.FirstOrDefault(x => x.GUID == info.NewGUID);

                                if (GUIDCheckNew == null)
                                {
                                    //We have the old mod but not the new, do not do migration
                                    extInfoNew.Add(a);
                                }
                                else
                                {
                                    //We have the old mod and the new, do migration so characters save with the new stuff
                                    Logger.Log(LogLevel.Info, $"Migrating GUID {info.OldGUID} -> {info.NewGUID}");
                                    ResolveInfo b = new ResolveInfo();
                                    b = a;
                                    b.GUID = info.NewGUID;
                                    extInfoNew.Add(b);
                                }
                            }
                        }
                        else
                        {
                            ResolveInfo intResolveOld = UniversalAutoResolver.LoadedResolutionInfo.FirstOrDefault(x => x.Property == propertyWithoutPrefix && x.Slot == a.Slot && x.GUID == a.GUID);

                            if (intResolveOld == null)
                            {
                                //We do not have the old mod installed, do migration. Whether we have the new mod is irrelevant.
                                //If we don't have the new mod the user will get a missing mod warning for the new mod since they should be using that instead.
                                //If we do it will load correctly.
                                Logger.Log(LogLevel.Info, $"Migrating {info.OldGUID}:{info.OldID} -> {info.NewGUID}:{info.NewID}");
                                ResolveInfo b = new ResolveInfo();
                                b = a;
                                b.GUID = info.NewGUID;
                                b.Slot = info.NewID;
                                extInfoNew.Add(b);
                            }
                            else
                            {
                                ResolveInfo intResolveNew = UniversalAutoResolver.LoadedResolutionInfo.FirstOrDefault(x => x.Property == propertyWithoutPrefix && x.Slot == info.NewID && x.GUID == info.NewGUID);

                                if (intResolveNew == null)
                                {
                                    //We have the old mod but not the new, do not do migration
                                    extInfoNew.Add(a);
                                }
                                else
                                {
                                    //We have the old mod and the new, do migration so characters save with the new stuff
                                    Logger.Log(LogLevel.Warning, $"Migrating {info.OldGUID}:{info.OldID} -> {info.NewGUID}:{info.NewID}");
                                    ResolveInfo b = new ResolveInfo();
                                    b = a;
                                    b.GUID = info.NewGUID;
                                    b.Slot = info.NewID;
                                    extInfoNew.Add(b);
                                }
                            }
                        }
                    }
                }
                extInfo = extInfoNew;
            }
            catch (Exception ex)
            {
                //If something goes horribly wrong, return the original extInfo
                Logger.Log(LogLevel.Error, $"GUID migration cancelled due to error: {ex}");
                return extInfo;
            }

            return extInfoNew;
        }
        /// <summary>
        /// Read the KK_GUIDMigration.csv and generate a dictionary of MigrationInfo
        /// </summary>
        private static void GenerateMigrationInfo()
        {
            using (StreamReader reader = new StreamReader(GUIDMigrationFilePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    try
                    {
                        string[] parts = line.Split(',');
                        MigrationInfoList.Add(new MigrationInfo(parts[0], parts[1], int.Parse(parts[2]), parts[3], int.Parse(parts[4])));
                    }
                    catch
                    {
                        Logger.Log(LogLevel.Error, $"Error reading KK_GUIDMigration.csv line, skipping.");
                    }
                }
            }
        }

        private class MigrationInfo
        {
            public string Property;
            public string OldGUID;
            public int OldID;
            public string NewGUID;
            public int NewID;

            public MigrationInfo(string property, string oldGUID, int oldID, string newGUID, int newID)
            {
                Property = property;
                OldGUID = oldGUID;
                OldID = oldID;
                NewGUID = newGUID;
                NewID = newID;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Hooks), "IterateCardPrefixes")]
        public static void IterateCardPrefixesPrefix(ref IEnumerable<ResolveInfo> extInfo, ChaFile file)
        {
            extInfo = MigrateGUID(extInfo, file.parameter.fullname.Trim());
        }
    }
}