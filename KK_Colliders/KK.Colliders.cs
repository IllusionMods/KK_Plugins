using BepInEx;
using HarmonyLib;
using KKAPI;
using System.Collections.Generic;
using UnityEngine;

namespace KK_Plugins
{
    [BepInDependency(KoikatuAPI.GUID)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class KK_Colliders : BaseUnityPlugin
    {
        private static readonly List<ColliderData> ColliderList = new List<ColliderData>() {
            { new ColliderData("cf_j_root", 10f, 0f, new Vector3(0, -10.01f, 0f)) },
            { new ColliderData("cf_s_hand_L", 0.020f, 0.075f, new Vector3(-0.03f, -0.005f, 0f)) },
            { new ColliderData("cf_s_hand_R", 0.020f, 0.075f, new Vector3(0.03f, -0.005f, 0f)) },
            { new ColliderData("cf_s_forearm02_L", 0.025f, 0.25f, new Vector3(-0.03f, 0f, 0f)) },
            { new ColliderData("cf_s_forearm02_R", 0.025f, 0.25f, new Vector3(0.03f, 0f, 0f)) },
            { new ColliderData("cf_s_arm02_L", 0.025f, 0.25f, new Vector3(0.0f, 0f, 0f)) },
            { new ColliderData("cf_s_arm02_R", 0.025f, 0.25f, new Vector3(0.0f, 0f, 0f)) },
            };
        private static readonly HashSet<string> TitComments = new HashSet<string>() { "右胸", "左胸" };
        private static readonly float[] SkirtRadius = new float[] { 0.04f, 0.045f, 0.035f, 0.045f, 0.03f, 0.045f, 0.035f, 0.045f };

        public partial class ColliderController
        {
            private void TweakSkirt()
            {
                if (!SkirtColliders.Value) return;

                foreach (DynamicBone db in ChaControl?.gameObject?.GetComponentsInChildren<DynamicBone>(true))
                {
                    if (db.name == "ct_clothesBot")
                    {
                        int idx = int.Parse(db.m_Root.name.Split('_')[3]);
                        db.m_Radius = SkirtRadius[idx];
                        db.m_FreezeAxis = DynamicBone.FreezeAxis.X;
                        if (idx == 0)
                        {
                            var keys = db.m_RadiusDistrib.keys;
                            keys[keys.Length - 1].value = 1.6f;
                            keys[keys.Length - 2].value = 1.25f;
                            db.m_RadiusDistrib.keys = keys;
                        }
                        db.GetType().GetMethod("SetupParticles", AccessTools.all).Invoke(db, null);
                    }
                }
            }
        }
    }
}