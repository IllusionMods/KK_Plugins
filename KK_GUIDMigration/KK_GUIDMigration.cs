using BepInEx;
using BepInEx.Logging;
using Harmony;
using System.Collections.Generic;
using System.IO;
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
        internal static readonly List<MigrationInfo> MigrationInfoList = new List<MigrationInfo>();
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
            harmony.PatchAll(typeof(Hooks));
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

        internal class MigrationInfo
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
    }
}
