using System.Collections.Generic;
using UnityEngine;

namespace KK_Plugins
{
    internal static class ColliderConstants
    {
#if AI || HS2
        internal static ColliderData FloorColliderData = new ColliderData("cf_J_Root", 100f, 0f, new Vector3(0, -100.01f, 0f));
        internal static readonly List<ColliderData> ArmColliderDataList = new List<ColliderData>
        {
            new ColliderData("cf_J_Hand_s_L", 0.20f, 0.75f, new Vector3(-0.3f, -0.05f, 0f)),
            new ColliderData("cf_J_Hand_s_R", 0.20f, 0.75f, new Vector3(0.3f, -0.05f, 0f)),
            new ColliderData("cf_J_ArmLow02_s_L", 0.25f, 2.5f, new Vector3(0f, 0f, 0f)),
            new ColliderData("cf_J_ArmLow02_s_R", 0.25f, 2.5f, new Vector3(0f, 0f, 0f)),
            new ColliderData("cf_J_ArmUp02_s_L", 0.25f, 2.5f, new Vector3(0f, 0f, 0f)),
            new ColliderData("cf_J_ArmUp02_s_R", 0.25f, 2.5f, new Vector3(0f, 0f, 0f)),
        };
        internal static readonly HashSet<string> BreastDBComments = new HashSet<string> { "Mune_L", "Mune_R" };
#elif KK
        internal static ColliderData FloorColliderData = new ColliderData("cf_j_root", 100f, 0f, new Vector3(0, -100.01f, 0f));
        internal static readonly List<ColliderData> ArmColliderDataList = new List<ColliderData>
        {
            new ColliderData("cf_s_hand_L", 0.020f, 0.075f, new Vector3(-0.03f, -0.005f, 0f)),
            new ColliderData("cf_s_hand_R", 0.020f, 0.075f, new Vector3(0.03f, -0.005f, 0f)),
            new ColliderData("cf_s_forearm02_L", 0.025f, 0.25f, new Vector3(-0.03f, 0f, 0f)),
            new ColliderData("cf_s_forearm02_R", 0.025f, 0.25f, new Vector3(0.03f, 0f, 0f)),
            new ColliderData("cf_s_arm02_L", 0.025f, 0.25f, new Vector3(0f, 0f, 0f)),
            new ColliderData("cf_s_arm02_R", 0.025f, 0.25f, new Vector3(0f, 0f, 0f)),
        };
        internal static readonly List<ColliderData> LegColliderDataList = new List<ColliderData>
        {
            new ColliderData("cf_s_thigh01_L", 0.095f, 0.32f, new Vector3(0.05f, -0.1f, -0.015f), DynamicBoneCollider.Direction.Y),
            new ColliderData("cf_s_thigh01_R", 0.095f, 0.32f, new Vector3(-0.05f, -0.1f, -0.015f), DynamicBoneCollider.Direction.Y),
            new ColliderData("cf_s_thigh01_L", 0.095f, 0.32f, new Vector3(0.01f, -0.125f, -0.015f), DynamicBoneCollider.Direction.Y, "_2"),
            new ColliderData("cf_s_thigh01_R", 0.095f, 0.32f, new Vector3(-0.01f, -0.125f, -0.015f), DynamicBoneCollider.Direction.Y, "_2"),
            new ColliderData("cf_s_thigh02_L", 0.083f, 0.35f, new Vector3(-0.0065f, 0f, -0.012f), DynamicBoneCollider.Direction.Y),
            new ColliderData("cf_s_thigh02_R", 0.083f, 0.35f, new Vector3(0.0065f, 0f, -0.012f), DynamicBoneCollider.Direction.Y),
        };
        internal static readonly HashSet<string> BreastDBComments = new HashSet<string> { "右胸", "左胸" };
#endif
    }
}
