using UnityEngine;
using UnityEngine.UI;

namespace UILib
{
    internal class OneTimeVerticalLayoutGroup : VerticalLayoutGroup
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            if (Application.isEditor == false || Application.isPlaying)
                this.ExecuteDelayed(() => enabled = false, 3);
        }

        public void UpdateLayout() => enabled = true;
    }
}
