using System;
using System.Linq;
using UnityEngine;

namespace KK_Plugins
{
    public partial class Boop
    {
        public sealed class DbAdapter
        {
            public readonly MonoBehaviour BoneMb;
            public readonly Func<Transform> GetTransform;

            private readonly Func<Vector3> _getForce;
            private readonly Action<Vector3> _setForce;
            private Vector3? _originalForce;

            public Vector3 GetForce() => _getForce();

            public void SetForce(Vector3 f)
            {
                if (!_originalForce.HasValue) _originalForce = _getForce();
                _setForce(f);
            }

            public void ResetForce()
            {
                if (_originalForce.HasValue)
                {
                    SetForce(_originalForce.Value);
                    _originalForce = null;
                }
            }

            public void ApplyForce(Vector3 f)
            {
                SetForce((GetForce() + -f * ConfigScaling.Value) * ConfigDamping.Value);
            }

            private DbAdapter(MonoBehaviour boneMb, Func<Transform> getTransform, Func<Vector3> getForce, Action<Vector3> setForce)
            {
                BoneMb = boneMb;
                GetTransform = getTransform;
                _getForce = getForce;
                _setForce = setForce;
            }

            public static DbAdapter Create(DynamicBone bone)
            {
                return new DbAdapter(bone, () => bone.m_Particles.Last(t => t?.m_Transform != null).m_Transform, () => bone.m_Force, f => bone.m_Force = f);
            }
            public static DbAdapter Create(DynamicBone_Ver01 bone)
            {
                return new DbAdapter(bone, () => bone.m_Particles.Last(t => t?.m_Transform != null).m_Transform, () => bone.m_Force, f => bone.m_Force = f);
            }
            public static DbAdapter Create(DynamicBone_Ver02 bone)
            {
                return new DbAdapter(bone, () => bone.Bones.Last(t => t != null), () => bone.Force, f => bone.Force = f);
            }

            public static DbAdapter Create(MonoBehaviour bone)
            {
                if (bone == null) throw new ArgumentNullException(nameof(bone));
                switch (bone)
                {
                    case DynamicBone db: return Create(db);
                    case DynamicBone_Ver01 db1: return Create(db1);
                    case DynamicBone_Ver02 db2: return Create(db2);
                    default: throw new ArgumentException("unknown type " + bone.GetType());
                }
            }
        }
    }
}
