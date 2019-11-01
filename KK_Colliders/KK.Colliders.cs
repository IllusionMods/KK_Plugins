using BepInEx;
using HarmonyLib;
using KKAPI;
using System.Collections.Generic;

namespace KK_Plugins
{
    [BepInDependency(KoikatuAPI.GUID)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class KK_Colliders : BaseUnityPlugin
    {
        private const string RootBoneName = "cf_j_root";
        private static readonly HashSet<string> ArmBoneNames = new HashSet<string>() { "cf_s_forearm02_L", "cf_s_forearm02_R", "cf_s_arm02_L", "cf_s_arm02_R", "cf_s_hand_L", "cf_s_hand_R" };
        private static readonly HashSet<string> TitComments = new HashSet<string>() { "右胸", "左胸" };

        private static readonly float[] SkirtRadius = new float[] { 0.03f, 0.045f, 0.02f, 0.045f, 0.03f, 0.045f, 0.02f, 0.045f };

        public partial class ColliderController
        {
            private void TweakSkirt()
            {
                if (!SkirtColliders.Value)
                    return;

                foreach (DynamicBone db in ChaControl?.gameObject?.GetComponentsInChildren<DynamicBone>(true))
                    if (db.name == "ct_clothesBot")
                    {
                        int idx = int.Parse(db.m_Root.name.Split('_')[3]);
                        db.m_Radius = SkirtRadius[idx];
                        db.m_FreezeAxis = DynamicBone.FreezeAxis.X;
                        if (idx == 0)
                        {
                            var keys = db.m_RadiusDistrib.keys;
                            keys[keys.Length - 1].value = 1.6f;
                            db.m_RadiusDistrib.keys = keys;
                        }
                        db.GetType().GetMethod("SetupParticles", AccessTools.all).Invoke(db, null);
                    }
            }
        }
    }
}