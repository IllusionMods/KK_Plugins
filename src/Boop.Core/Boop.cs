using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using KKAPI;
using KKAPI.Studio;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KK_Plugins
{
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    public partial class Boop : BaseUnityPlugin
    {
        public const string GUID = "Boop";
        public const string PluginName = "Boop";
        internal const string PluginNameInternal = "Boop";
        public const string Version = "2.0";

        private static ConfigEntry<float> ConfigDamping;
        private static ConfigEntry<int> ConfigDistance;
        private static ConfigEntry<float> ConfigScaling;
        private static ConfigEntry<bool> ConfigEnabledStudio;
        private static ConfigEntry<bool> ConfigEnabledMainGame;

        private Camera _mainCam;
        private Vector3 _mousePosPrev = Vector3.zero;
        private readonly CircularBuffer _mouseVelocity = new CircularBuffer(5);
        private static readonly List<DbAdapter> _DBs = new List<DbAdapter>();

        private void Awake()
        {
            ConfigDistance = Config.Bind("Mouse booping", "Distance", 100);
            ConfigDamping = Config.Bind("Mouse booping", "Damping", 0.5f);
            ConfigScaling = Config.Bind("Mouse booping", "Scaling", 0.0002f);
            ConfigEnabledStudio = Config.Bind("General", "Run in Studio", true);
            ConfigEnabledMainGame = Config.Bind("General", "Run in Main Game", true);

            if (!ConfigEnabledStudio.Value && StudioAPI.InsideStudio)
            {
                enabled = false;
                return;
            }
            if (!ConfigEnabledMainGame.Value && !StudioAPI.InsideStudio)
            {
                enabled = false;
                return;
            }

            Harmony.CreateAndPatchAll(typeof(Boop), GUID);

#if DEBUG // for hot reloading
            foreach (var db in FindObjectsOfType<DynamicBone>()) OnDynamicBoneInit(db);
            foreach (var db in FindObjectsOfType<DynamicBone_Ver01>()) OnDynamicBoneInit(db);
            foreach (var db in FindObjectsOfType<DynamicBone_Ver02>()) OnDynamicBoneInit(db);
#endif
        }

        [HarmonyPostfix]
        [HarmonyWrapSafe]
        [HarmonyPatch(typeof(DynamicBone), nameof(DynamicBone.SetupParticles))]
        [HarmonyPatch(typeof(DynamicBone_Ver01), nameof(DynamicBone_Ver01.SetupParticles))]
        [HarmonyPatch(typeof(DynamicBone_Ver02), nameof(DynamicBone_Ver02.SetupParticles))]
        private static void OnDynamicBoneInit(MonoBehaviour __instance)
        {
            // Prevent having multiple copies, maybe use a dictionary and check for key?
            if (_DBs.All(x => x.BoneMb != __instance))
                _DBs.Add(DbAdapter.Create(__instance));
        }

        private void Update()
        {
            if (_mainCam == null) _mainCam = Camera.main;
            if (_mainCam == null) return;

            var mousePosition = Input.mousePosition;
            var obj = _mousePosPrev - mousePosition;
            _mouseVelocity.Add(obj);

            _mousePosPrev = mousePosition;
            var point = _mouseVelocity.Average();
            var f = _mainCam.transform.rotation * point;

            for (var index = 0; index < _DBs.Count; index++)
            {
                var db = _DBs[index];
                if (db.BoneMb == null)
                {
                    _DBs.RemoveAt(index);
                    index--;
                    continue;
                }

                var dbtr = db.GetTransform();
                if (dbtr == null) continue;
                var bonePos = dbtr.position;
                var boneDist = Vector3.Distance(_mainCam.transform.position, bonePos);
                if (Vector3.Distance(_mainCam.WorldToScreenPoint(bonePos), mousePosition) < ConfigDistance.Value / boneDist)
                    db.ApplyForce(f);
                else
                    db.ResetForce();
            }
        }
    }
}
