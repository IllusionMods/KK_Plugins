using BepInEx.Logging;
using Harmony;
using Sideloader.AutoResolver;
using System.Collections.Generic;
using Logger = BepInEx.Logger;

namespace KK_GUIDMigration
{
    internal class Hooks
    {
        [HarmonyPrefix, HarmonyPatch(typeof(Sideloader.AutoResolver.Hooks), "IterateCardPrefixes")]
        public static void IterateCardPrefixesPrefix(ref IEnumerable<ResolveInfo> extInfo, ChaFile file)
        {
            try
            {
                extInfo = Migrate.MigrateGUID(extInfo, file.parameter.fullname.Trim());
            }
            catch
            {
                Logger.Log(LogLevel.Error | LogLevel.Message, $"GUID migration failed. Please update KK_GUIDMigration and/or BepisPlugins.");
            }
        }
        [HarmonyPrefix, HarmonyPatch(typeof(Sideloader.AutoResolver.Hooks), "IterateCoordinatePrefixes")]
        public static void IterateCoordinatePrefixesPrefix(ref IEnumerable<ResolveInfo> extInfo, ChaFileCoordinate coordinate)
        {
            try
            {
                extInfo = Migrate.MigrateGUID(extInfo);
            }
            catch
            {
                Logger.Log(LogLevel.Error | LogLevel.Message, $"GUID migration failed. Please update KK_GUIDMigration and/or BepisPlugins.");
            }
        }
    }
}
