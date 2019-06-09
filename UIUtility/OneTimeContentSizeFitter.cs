using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace UILib
{
    public class OneTimeContentSizeFitter : ContentSizeFitter
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            if (Application.isEditor == false || Application.isPlaying)
                this.ExecuteDelayed(() => this.enabled = false, 2);
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