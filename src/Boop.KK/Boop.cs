using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using KKAPI;
using KKAPI.Chara;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Boop
{
    [BepInPlugin("Boop", "Boop", "1.0")]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    public class Boop : BaseUnityPlugin
    {
        static Boop()
        {
        }

        public Boop()
        {
        }

        private void ApplyForce(DynamicBone_Ver02 dbc, Vector3 f)
        {
            dbc.Force += -f * 0.0002f;
        }

        private void Awake()
        {
            CharacterApi.RegisterExtraBehaviour<BoopController>("Boop");
            SceneManager.sceneLoaded += this.SceneManager_sceneLoaded;
        }

        public static void RefreshDynamicBones()
        {
            for (int i = Boop.chaInfos.Count - 1; i >= 0; i--)
            {
                if (Boop.chaInfos[i] == null)
                {
                    Boop.chaInfos.RemoveAt(i);
                }
            }
            Boop.dbv2s = Boop.chaInfos.SelectMany((ChaInfo x) => x.GetComponentsInChildren<DynamicBone_Ver02>());
            foreach (DynamicBone_Ver02 dynamicBone_Ver in Boop.dbv2s)
            {
                dynamicBone_Ver.Force = Vector3.zero;
            }
        }

        public static void RegisterChar(ChaInfo ci)
        {
            Boop.chaInfos.Add(ci);
            Boop.RefreshDynamicBones();
        }

        private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode lsm)
        {
            Boop.RefreshDynamicBones();
        }

        public static void UnregisterChar(ChaInfo ci)
        {
            if (Boop.chaInfos.Remove(ci))
            {
                Boop.RefreshDynamicBones();
            }
        }

        private void Update()
        {
            if (this.main == null)
            {
                this.main = Camera.main;
            }
            if (!(this.main == null))
            {
                Vector3 mousePosition = Input.mousePosition;
                Vector3 obj = this.mousePos_Prev - mousePosition;
                this.mouseVelocity.Add(obj);
                this.mousePos_Prev = mousePosition;
                Vector3 point = this.mouseVelocity.Average();
                Vector3 f = this.main.transform.rotation * point;
                if (Boop.dbv2s != null)
                {
                    foreach (DynamicBone_Ver02 dynamicBone_Ver in Boop.dbv2s)
                    {
                        float num = Vector3.Distance(this.main.transform.position, dynamicBone_Ver.Bones.Last<Transform>().transform.position);
                        if (Vector3.Distance(this.main.WorldToScreenPoint(dynamicBone_Ver.Bones.Last<Transform>().transform.position), mousePosition) < 100f / num)
                        {
                            this.ApplyForce(dynamicBone_Ver, f);
                        }
                        dynamicBone_Ver.Force *= 0.5f;
                    }
                }
            }
        }

        private static List<ChaInfo> chaInfos = new List<ChaInfo>();

        private const float damping = 0.5f;

        private static IEnumerable<DynamicBone_Ver02> dbv2s;

        private const int distance = 100;

        private Camera main;

        private Vector3 mousePos_Prev = Vector3.zero;

        private CircularBuffer mouseVelocity = new CircularBuffer(5);

        private const float scaling = 0.0002f;
    }
}
