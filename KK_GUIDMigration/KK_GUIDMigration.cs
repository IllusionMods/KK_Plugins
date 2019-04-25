using BepInEx;
using BepInEx.Logging;
using Harmony;
using Sideloader.AutoResolver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Logger = BepInEx.Logger;

namespace KK_GUIDMigration
{
    /// <summary>
    /// Modifies the GUID or ID of items saved to a card
    /// </summary>
    [BepInDependency(Sideloader.Sideloader.GUID)]
    [BepInPlugin(GUID, PluginName, Version)]
    public class KK_GUIDMigration : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.guidmigration";
        public const string PluginName = "GUID Migration";
        public const string Version = "1.5";
        private static readonly List<MigrationInfo> MigrationInfoList = new List<MigrationInfo>();
        private static readonly string GUIDMigrationFilePath = Path.Combine(Paths.PluginPath, "KK_GUIDMigration.csv");

        private void Main()
        {
            //Don't even bother if there's no mods directory
            if (!Directory.Exists(Path.Combine(Paths.GameRootPath, "mods")) || !Directory.Exists(Paths.PluginPath))
            {
                Logger.Log(LogLevel.Warning, "KK_GUIDMigration was not loaded due to missing mods folder.");
                return;
            }

            //Only do migration if there's a .csv file and it has stuff in it
            if (!File.Exists(GUIDMigrationFilePath))
            {
                Logger.Log(LogLevel.Error | LogLevel.Message, "KK_GUIDMigration was not loaded due to missing KK_GUIDMigration.csv file.");
                return;
            }

            GenerateMigrationInfo();
            if (MigrationInfoList.Count == 0)
            {
                Logger.Log(LogLevel.Error | LogLevel.Message, "KK_GUIDMigration was not loaded due to empty KK_GUIDMigration.csv file.");
                return;
            }

            var harmony = HarmonyInstance.Create(GUID);
            harmony.PatchAll(typeof(KK_GUIDMigration));
        }
        /// <summary>
        /// Look through all the GUIDs, compare it to the MigrationInfoList, and do migration when necessary
        /// </summary>
        private static IEnumerable<ResolveInfo> MigrateGUID(IEnumerable<ResolveInfo> extInfo, string characterName = "")
        {
            List<ResolveInfo> extInfoNew = new List<ResolveInfo>();
            bool DidBlankGUIDMessage = false;

            try
            {
                if (extInfo == null)
                    return extInfo;

                foreach (ResolveInfo resolveInfo in extInfo)
                {
                    if (resolveInfo.GUID.IsNullOrEmpty())
                    {
                        //Don't add empty GUID to the new list, this way CompatibilityResolve will treat it as a hard mod and attempt to find a match
                        if (!DidBlankGUIDMessage) //No need to spam it for every single thing
                        {
                            if (characterName == "")
                                Logger.Log(LogLevel.Warning | LogLevel.Message, $"Blank GUID detected, attempting Compatibility Resolve");
                            else
                                Logger.Log(LogLevel.Warning | LogLevel.Message, $"[{characterName}] Blank GUID detected, attempting Compatibility Resolve");
                            DidBlankGUIDMessage = true;
                        }
                    }
                    else
                    {
                        string propertyWithoutPrefix = resolveInfo.Property;

                        //Remove outfit and accessory prefixes for searching purposes
                        if (propertyWithoutPrefix.StartsWith("outfit"))
                            propertyWithoutPrefix = propertyWithoutPrefix.Remove(0, propertyWithoutPrefix.IndexOf('.') + 1);
                        if (propertyWithoutPrefix.Remove(propertyWithoutPrefix.IndexOf('.')).Contains("accessory"))
                            propertyWithoutPrefix = propertyWithoutPrefix.Remove(0, propertyWithoutPrefix.IndexOf('.') + 1);

                        MigrationInfo info = MigrationInfoList.Where(x => (x.Property == propertyWithoutPrefix && x.OldID == resolveInfo.Slot && x.OldGUID == resolveInfo.GUID)
                                                                       || (x.Property == "*" && x.OldGUID == resolveInfo.GUID)
                                                                       || (x.Property == "-" && x.OldGUID == resolveInfo.GUID)).FirstOrDefault();
                        if (info == null)
                        {
                            //This item does not need to be migrated
                            extInfoNew.Add(resolveInfo);
                        }
                        else if (info.Property == "*") //* assumes only the GUID changed while the IDs stayed the same
                        {
                            ResolveInfo GUIDCheckOld = UniversalAutoResolver.LoadedResolutionInfo.FirstOrDefault(x => x.GUID == resolveInfo.GUID);

                            if (GUIDCheckOld == null)
                            {
                                //We do not have the old mod installed, do migration. Whether we have the new mod is irrelevant.
                                //If we don't have the new mod the user will get a missing mod warning for the new mod since they should be using that instead.
                                //If we do it will load correctly.
                                Logger.Log(LogLevel.Info, $"Migrating GUID {info.OldGUID} -> {info.NewGUID}");
                                ResolveInfo resolveInfoNew = new ResolveInfo();
                                resolveInfoNew = resolveInfo;
                                resolveInfoNew.GUID = info.NewGUID;
                                extInfoNew.Add(resolveInfoNew);
                            }
                            else
                            {
                                ResolveInfo GUIDCheckNew = UniversalAutoResolver.LoadedResolutionInfo.FirstOrDefault(x => x.GUID == info.NewGUID);

                                if (GUIDCheckNew == null)
                                {
                                    //We have the old mod but not the new, do not do migration
                                    extInfoNew.Add(resolveInfo);
                                }
                                else
                                {
                                    //We have the old mod and the new, do migration so characters save with the new stuff
                                    Logger.Log(LogLevel.Info, $"Migrating GUID {info.OldGUID} -> {info.NewGUID}");
                                    ResolveInfo resolveInfoNew = new ResolveInfo();
                                    resolveInfoNew = resolveInfo;
                                    resolveInfoNew.GUID = info.NewGUID;
                                    extInfoNew.Add(resolveInfoNew);
                                }
                            }
                        }
                        else if (info.Property == "-") //- indicates the entry needs to be stripped of its extended data and loaded as a hard mod
                        {
                            continue;
                        }
                        else
                        {
                            ResolveInfo intResolveOld = UniversalAutoResolver.TryGetResolutionInfo(resolveInfo.Slot, propertyWithoutPrefix, resolveInfo.GUID);

                            if (intResolveOld == null)
                            {
                                //We do not have the old mod installed, do migration. Whether we have the new mod is irrelevant.
                                //If we don't have the new mod the user will get a missing mod warning for the new mod since they should be using that instead.
                                //If we do it will load correctly.
                                Logger.Log(LogLevel.Info, $"Migrating {info.OldGUID}:{info.OldID} -> {info.NewGUID}:{info.NewID}");
                                ResolveInfo resolveInfoNew = new ResolveInfo();
                                resolveInfoNew = resolveInfo;
                                resolveInfoNew.GUID = info.NewGUID;
                                resolveInfoNew.Slot = info.NewID;
                                extInfoNew.Add(resolveInfoNew);
                            }
                            else
                            {
                                ResolveInfo intResolveNew = UniversalAutoResolver.TryGetResolutionInfo(info.NewID, propertyWithoutPrefix, info.NewGUID);

                                if (intResolveNew == null)
                                {
                                    //We have the old mod but not the new, do not do migration
                                    extInfoNew.Add(resolveInfo);
                                }
                                else
                                {
                                    //We have the old mod and the new, do migration so characters save with the new stuff
                                    Logger.Log(LogLevel.Info, $"Migrating {info.OldGUID}:{info.OldID} -> {info.NewGUID}:{info.NewID}");
                                    ResolveInfo b = new ResolveInfo();
                                    b = resolveInfo;
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

        [HarmonyPrefix, HarmonyPatch(typeof(Hooks), "IterateCardPrefixes")]
        public static void IterateCardPrefixesPrefix(ref IEnumerable<ResolveInfo> extInfo, ChaFile file)
        {
            try
            {
                extInfo = MigrateGUID(extInfo, file.parameter.fullname.Trim());
            }
            catch
            {
                Logger.Log(LogLevel.Error | LogLevel.Message, $"GUID migration failed. Please update KK_GUIDMigration and/or BepisPlugins.");
            }
        }
        [HarmonyPrefix, HarmonyPatch(typeof(Hooks), "IterateCoordinatePrefixes")]
        public static void IterateCoordinatePrefixesPrefix(ref IEnumerable<ResolveInfo> extInfo, ChaFileCoordinate coordinate)
        {
            try
            {
                extInfo = MigrateGUID(extInfo);
            }
            catch
            {
                Logger.Log(LogLevel.Error | LogLevel.Message, $"GUID migration failed. Please update KK_GUIDMigration and/or BepisPlugins.");
            }
        }
    }
}
