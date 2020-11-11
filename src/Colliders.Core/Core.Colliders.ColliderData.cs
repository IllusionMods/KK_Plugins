using UnityEngine;

namespace KK_Plugins
{
    internal class ColliderData
    {
        public string BoneName;
        public float ColliderRadius;
        public float CollierHeight;
        public Vector3 ColliderCenter;
        public string ColliderNamePostfix;
        public DynamicBoneCollider.Direction ColliderDirection;

        public ColliderData(string boneName, float colliderRadius, float collierHeight, Vector3 colliderCenter, DynamicBoneCollider.Direction colliderDirection = default, string colliderNamePostfix = "")
        {
            BoneName = boneName;
            ColliderRadius = colliderRadius;
            CollierHeight = collierHeight;
            ColliderCenter = colliderCenter;
            ColliderDirection = colliderDirection;
            ColliderNamePostfix = colliderNamePostfix;
        }
    }
}
