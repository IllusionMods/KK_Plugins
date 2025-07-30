// v1.0.0 code was provided by BitMagnet under GPL-3.0 license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using ExtensibleSaveFormat;
using HarmonyLib;
using KKAPI;
using KKAPI.Utilities;
using Manager;
using MessagePack;
using MessagePack.Formatters;
using UnityEngine;
using Illusion.Extensions;
using BepInEx.Logging;

namespace SpawnLocker
{
    [BepInPlugin(GUID, GUID, Version)]
    [BepInProcess(KoikatuAPI.GameProcessName)]
#if KK
    [BepInProcess(KoikatuAPI.GameProcessNameSteam)]
#endif
    public partial class SpawnLockMain : BaseUnityPlugin
    {
        public const string PluginName = "Koikatsu Spawn Locker";
        public const string GUID = "SpawnLocker";

        public const string Version = "1.0.0";

        static ManualLogSource s_Logger = null;

        internal void Main()
        {
            s_Logger = Logger;

            var harmony = HarmonyLib.Harmony.CreateAndPatchAll(typeof(SpawnLockMain));

            var transpiler = new HarmonyMethod(typeof(SpawnLockMain), nameof(NPCLoadAllTranspile));

            foreach (var targetMethod in typeof(ActionScene).GetMethods(AccessTools.all).Where(x => x.Name == nameof(ActionScene.NPCLoadAll)))
            {
                Logger.LogDebug("Patching: " + targetMethod.FullDescription());
                harmony.PatchMoveNext(targetMethod, transpiler:transpiler);
            }
        }

        private static IEnumerable<CodeInstruction> NPCLoadAllTranspile(IEnumerable<CodeInstruction> instructions)
        {
            var targetMethod = typeof(System.Linq.Enumerable).GetMethod(nameof(System.Linq.Enumerable.Take)).MakeGenericMethod(typeof(SaveData.Heroine));
            var newTakeMethod = AccessTools.Method(typeof(SpawnLockMain), nameof(_SpawnLockTake));

            foreach (var instruction in instructions)
            {
                if (instruction.opcode != OpCodes.Call || instruction.operand as MethodInfo != targetMethod)
                {
                    yield return instruction; 
                }
                else
                {
                    yield return new CodeInstruction(OpCodes.Call, newTakeMethod);
                }
            }
        }

        static public bool IsLocked(ChaFileControl heroine )
        {
            if (heroine == null)
                return false;

            PluginData pluginData = ExtendedSave.GetExtendedDataById(heroine, GUID);
            return SpawnLockData.Load(pluginData)?.isLocked == true;            
        }

        static public bool IsLocked(SaveData.Heroine heroine)
        {
            return IsLocked(heroine?.charFile);
        }

        static public bool ToggleLock(ChaFileControl heroine)
        {
            if (heroine == null)
                return false;

            SpawnLockData data = new SpawnLockData();
            data.isLocked = !IsLocked(heroine);
            ExtendedSave.SetExtendedDataById(heroine, GUID, data.Save());

            s_Logger.LogMessage("Number of locked heroines: " + _GetLockedHeroines());

            return data.isLocked;
        }

        static public bool ToggleLock(SaveData.Heroine heroine)
        {
            return ToggleLock(heroine?.charFile);
        }

        static private int _GetLockedHeroines()
        {
#if KK
            return Singleton<Manager.Game>.Instance.HeroineList.Count(heroine => IsLocked(heroine));
#elif KKS
            return Manager.Game.HeroineList.Count(heroine => IsLocked(heroine));
#endif
        }

        static System.Collections.Generic.IEnumerable<SaveData.Heroine> _SpawnLockTake(System.Collections.Generic.IEnumerable<SaveData.Heroine> heroines, int n )
        {
            List<SaveData.Heroine> lockedHeroines = new List<SaveData.Heroine>();
            List<SaveData.Heroine> notLockedHeroines = new List<SaveData.Heroine>();

            foreach( var heroine in heroines)
            {
                if (IsLocked(heroine))
                    lockedHeroines.Add(heroine);
                else
                    notLockedHeroines.Add(heroine);
            }

            s_Logger.LogInfo($"Number of locked heroines: {lockedHeroines.Count()}, Number of unlocked heroines: {notLockedHeroines.Count()}");

            if (lockedHeroines.Count() < n)
            {
                lockedHeroines.AddRange(notLockedHeroines.Take(n - lockedHeroines.Count()));
            }

            return lockedHeroines.Shuffle().Take(n);
        }
    }
}
