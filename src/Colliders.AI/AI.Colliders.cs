using BepInEx;
using KKAPI;
using System.Collections.Generic;
using UnityEngine;

namespace KK_Plugins
{
    [BepInDependency(KoikatuAPI.GUID)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class Colliders : BaseUnityPlugin
    {
        internal static ColliderData FloorColliderData = new ColliderData("cf_J_Root", 100f, 0f, new Vector3(0, -100.01f, 0f));
        private static readonly List<ColliderData> ArmColliderDataList = new List<ColliderData>() {
            { new ColliderData("cf_J_Hand_s_L", 0.20f, 0.75f, new Vector3(-0.3f, -0.05f, 0f)) },
            { new ColliderData("cf_J_Hand_s_R", 0.20f, 0.75f, new Vector3(0.3f, -0.05f, 0f)) },
            { new ColliderData("cf_J_ArmLow02_s_L", 0.25f, 2.5f, new Vector3(0f, 0f, 0f)) },
            { new ColliderData("cf_J_ArmLow02_s_R", 0.25f, 2.5f, new Vector3(0f, 0f, 0f)) },
            { new ColliderData("cf_J_ArmUp02_s_L", 0.25f, 2.5f, new Vector3(0f, 0f, 0f)) },
            { new ColliderData("cf_J_ArmUp02_s_R", 0.25f, 2.5f, new Vector3(0f, 0f, 0f)) },
            };
        private static readonly HashSet<string> BreastDBComments = new HashSet<string>() { "Mune_L", "Mune_R" };
    }
}