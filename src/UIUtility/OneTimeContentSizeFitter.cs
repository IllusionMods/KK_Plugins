using UnityEngine;
using UnityEngine.UI;

namespace UILib
{
    internal class OneTimeContentSizeFitter : ContentSizeFitter
    {
        public override void OnEnable()
        {
            base.OnEnable();
            if (Application.isEditor == false || Application.isPlaying)
                this.ExecuteDelayed(() => enabled = false, 2);
        }

        public void UpdateLayout() => enabled = true;
    }
}