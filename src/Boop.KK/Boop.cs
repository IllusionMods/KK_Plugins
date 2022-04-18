using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using KKAPI;
using KKAPI.Chara;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KK_Plugins
{
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    public class Boop : BaseUnityPlugin
    {
        public const string GUID = "Boop";
        public const string PluginName = "Boop";
        internal const string PluginNameInternal = "Boop";
        public const string Version = "1.0.1";

        private static float _damping = 0.5f;
        private static int _distance = 100;
        private static float _scaling = 0.0002f;

        private static readonly List<ChaInfo> _ChaInfos = new List<ChaInfo>();
        private static IEnumerable<DynamicBone_Ver02> _dbv2S;

        private Camera _main;
        private Vector3 _mousePosPrev = Vector3.zero;
        private readonly CircularBuffer _mouseVelocity = new CircularBuffer(5);

        private void Awake()
        {
            CharacterApi.RegisterExtraBehaviour<BoopController>(GUID);
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }

        private static void SceneManager_sceneLoaded(Scene scene, LoadSceneMode lsm)
        {
            try
            {
                RefreshDynamicBones();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public static void RefreshDynamicBones()
        {
            for (var i = _ChaInfos.Count - 1; i >= 0; i--)
            {
                if (_ChaInfos[i] == null)
                    _ChaInfos.RemoveAt(i);
            }

            _dbv2S = _ChaInfos.SelectMany(x => x.GetComponentsInChildren<DynamicBone_Ver02>());
            foreach (var db in _dbv2S) db.Force = Vector3.zero;
        }

        public static void RegisterChar(ChaInfo ci)
        {
            _ChaInfos.Add(ci);
            RefreshDynamicBones();
        }

        public static void UnregisterChar(ChaInfo ci)
        {
            if (_ChaInfos.Remove(ci)) RefreshDynamicBones();
        }

        private static void ApplyForce(DynamicBone_Ver02 dbc, Vector3 f)
        {
            dbc.Force += -f * _scaling;
        }

        private void Update()
        {
            if (_main == null) _main = Camera.main;
            if (_main == null) return;

            var mousePosition = Input.mousePosition;
            var obj = _mousePosPrev - mousePosition;
            _mouseVelocity.Add(obj);

            if (_dbv2S != null)
            {
                _mousePosPrev = mousePosition;
                var point = _mouseVelocity.Average();
                var f = _main.transform.rotation * point;

                foreach (var db in _dbv2S)
                {
                    var boneDist = Vector3.Distance(_main.transform.position, db.Bones.Last().transform.position);
                    if (Vector3.Distance(_main.WorldToScreenPoint(db.Bones.Last().transform.position), mousePosition) < _distance / boneDist)
                        ApplyForce(db, f);

                    db.Force *= _damping;
                }
            }
        }
    }
}
