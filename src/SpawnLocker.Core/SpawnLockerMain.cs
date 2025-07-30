// v1.0.0 code was provided by BitMagnet under GPL-3.0 license.

using BepInEx;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using Illusion.Extensions;
using KKAPI;
using System.Collections.Generic;
using System.Linq;

namespace SpawnLocker
{
    [BepInPlugin(GUID, GUID, Version)]
    [BepInProcess(KoikatuAPI.GameProcessName)]
#if KK
    [BepInProcess(KoikatuAPI.GameProcessNameSteam)]
#endif
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    public class SpawnLockMain : BaseUnityPlugin
    {
        public const string PluginName = "Koikatsu Spawn Locker";
        public const string GUID = "SpawnLocker";
        public const string Version = "1.0.0";

        internal static new ManualLogSource Logger;

        private void Main()
        {
            Logger = base.Logger;

            Hooks.Apply();
        }

        public static bool IsLocked(ChaFileControl heroine)
        {
            if (heroine == null)
                return false;

            var pluginData = ExtendedSave.GetExtendedDataById(heroine, GUID);
            return SpawnLockData.Load(pluginData)?.isLocked == true;
        }

        public static bool IsLocked(SaveData.Heroine heroine)
        {
            return IsLocked(heroine?.charFile);
        }

        public static bool ToggleLock(ChaFileControl heroine)
        {
            if (heroine == null)
                return false;

            var data = new SpawnLockData();
            data.isLocked = !IsLocked(heroine);
            ExtendedSave.SetExtendedDataById(heroine, GUID, data.Save());

            Logger.LogMessage("Number of locked heroines: " + _GetLockedHeroines());

            return data.isLocked;
        }

        public static bool ToggleLock(SaveData.Heroine heroine)
        {
            return ToggleLock(heroine?.charFile);
        }

        private static int _GetLockedHeroines()
        {
#if KK
            return Manager.Game.Instance.HeroineList.Count(IsLocked);
#elif KKS
            return Manager.Game.HeroineList.Count(IsLocked);
#endif
        }

        internal static IEnumerable<SaveData.Heroine> _SpawnLockTake(IEnumerable<SaveData.Heroine> heroines, int n)
        {
            var lockedHeroines = new List<SaveData.Heroine>();
            var notLockedHeroines = new List<SaveData.Heroine>();

            foreach (var heroine in heroines)
            {
                if (IsLocked(heroine))
                    lockedHeroines.Add(heroine);
                else
                    notLockedHeroines.Add(heroine);
            }

            Logger.LogInfo($"Number of locked heroines: {lockedHeroines.Count}, Number of unlocked heroines: {notLockedHeroines.Count}");

            if (lockedHeroines.Count < n)
            {
                lockedHeroines.AddRange(notLockedHeroines.Take(n - lockedHeroines.Count));
            }

            return lockedHeroines.Shuffle().Take(n);
        }
    }
}
