using System.Collections.Generic;

namespace KK_Plugins
{
    public partial class Colliders
    {
        public partial class ColliderController
        {
            private readonly List<DynamicBoneCollider> LegColliders = new List<DynamicBoneCollider>();

            /// <summary>
            /// Update the skirt dynamic bones. Locks the skirt's front dynamic bone on the X axis to reduce clipping.
            /// </summary>
            private void UpdateSkirtDB(DynamicBone dynamicBone)
            {
                if (dynamicBone.name != "ct_clothesBot") return;

                int idx = int.Parse(dynamicBone.m_Root.name.Split('_')[3]);
                if (idx == 0)
                    if (SkirtCollidersEnabled)
                        dynamicBone.m_FreezeAxis = DynamicBone.FreezeAxis.X;
                    else
                        dynamicBone.m_FreezeAxis = DynamicBone.FreezeAxis.None;
            }

            /// <summary>
            /// Prevent arm colliders from affecting the skirt dynamic bones
            /// </summary>
            private void UpdateArmCollidersSkirtDB(DynamicBone dynamicBone)
            {
                if (dynamicBone.name != "ct_clothesBot") return;
                if (ArmColliders == null) return;

                foreach (var armCollider in ArmColliders)
                    dynamicBone.m_Colliders.Remove(armCollider);
            }

            /// <summary>
            /// Apply leg colliders to skirt dynamic bones
            /// </summary>
            private void UpdateLegCollidersSkirtDB(DynamicBone dynamicBone)
            {
                if (dynamicBone.name != "ct_clothesBot") return;
                if (LegColliders == null) return;

                foreach (var legCollider in LegColliders)
                    if (!SkirtCollidersEnabled)
                        dynamicBone.m_Colliders.Remove(legCollider);
                    else if (legCollider != null && !dynamicBone.m_Colliders.Contains(legCollider))
                        dynamicBone.m_Colliders.Add(legCollider);
            }
        }
    }
}
