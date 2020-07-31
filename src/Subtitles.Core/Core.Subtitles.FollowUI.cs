using UnityEngine;

namespace KK_Plugins
{
    public class FollowUI : MonoBehaviour
    {
        private readonly float moveK = 2f;
        private readonly float rotK = 2f;
        private Vector3 pos = Vector3.zero;
        private Quaternion rot;

        internal void Start()
        {
            rot = transform.parent.rotation;
            pos = transform.parent.position;
        }

        internal void Update()
        {
            pos.x = Spring(pos.x, transform.parent.position.x, moveK, Time.deltaTime, 0f, 0f);
            pos.y = Spring(pos.y, transform.parent.position.y, moveK, Time.deltaTime, 0f, 0f);
            pos.z = Spring(pos.z, transform.parent.position.z, moveK, Time.deltaTime, 0f, 0f);
            rot = Quaternion.Slerp(rot, transform.parent.rotation, rotK * Time.deltaTime);
            transform.position = pos;
            transform.rotation = rot;
        }

        public static float Spring(float now, float goal, float K, float sec, float minSpeed = 0f, float maxSpeed = 0f)
        {
            float num = goal - now;
            float num2 = num * K * sec;
            if (num >= 0f)
                num2 = Mathf.Min(num2, num);
            else
                num2 = Mathf.Max(num2, num);
            if (maxSpeed > 0f)
            {
                float num3 = maxSpeed * sec;
                if (Mathf.Abs(num2) > num3)
                    num2 = (num2 < 0f) ? (-num3) : num3;
            }
            if (minSpeed > 0f)
            {
                float num4 = minSpeed * sec;
                if (Mathf.Abs(num2) < num4)
                    num2 = ((num2 < 0f) ? (-num4) : num4);
            }
            if (Mathf.Abs(goal - now) <= Mathf.Abs(num2))
                return goal;
            return now += num2;
        }
    }
}
