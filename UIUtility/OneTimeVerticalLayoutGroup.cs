using UnityEngine;
using UnityEngine.UI;

namespace UILib
{
    public class OneTimeVerticalLayoutGroup : VerticalLayoutGroup
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            if (Application.isEditor == false || Application.isPlaying)
                this.ExecuteDelayed(() => this.enabled = false, 3);
        }

        protected override void OnDisable()
        {
        }

        public void UpdateLayout()
        {
            this.enabled = true;
        }
    }
}
