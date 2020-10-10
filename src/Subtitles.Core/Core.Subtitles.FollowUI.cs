using UnityEngine;

namespace KK_Plugins
{
    public class FollowUI : MonoBehaviour
    {
        private const float moveK = 2f;
        private const float rotK = 2f;
        private Vector3 pos = Vector3.zero;
        private Quaternion rot;
        private new Transform transform;

        private void Start()
        {
            transform = base.transform;
            rot = transform.rotation;
            pos = transform.position;
        }

        private void Update()
        {
            var parent = transform.parent;
            var position = parent.position;
            pos.x = Spring(pos.x, position.x, moveK, Time.deltaTime);
            pos.y = Spring(pos.y, position.y, moveK, Time.deltaTime);
            pos.z = Spring(pos.z, position.z, moveK, Time.deltaTime);
            rot = Quaternion.Slerp(rot, parent.rotation, rotK * Time.deltaTime);
            transform.position = pos;
            transform.rotation = rot;
        }

        public static float Spring(float now, float goal, float K, float sec, float minSpeed = 0f, float maxSpeed = 0f)
        {
            float num = goal - now;
            float num2 = num * K * sec;
            num2 = num >= 0f ? Mathf.Min(num2, num) : Mathf.Max(num2, num);

            if (maxSpeed > 0f)
            {
                float num3 = maxSpeed * sec;
                if (Mathf.Abs(num2) > num3)
                    num2 = num2 < 0f ? -num3 : num3;
            }
            if (minSpeed > 0f)
            {
                float num4 = minSpeed * sec;
                if (Mathf.Abs(num2) < num4)
                    num2 = num2 < 0f ? -num4 : num4;
            }
            if (Mathf.Abs(goal - now) <= Mathf.Abs(num2))
                return goal;
            return now + num2;
        }
    }
}
