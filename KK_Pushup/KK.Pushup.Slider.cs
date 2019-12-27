using KKAPI.Maker.UI;
using System;

namespace KK_Plugins
{
    public partial class Pushup
    {
        public class PushupSlider
        {
            public MakerSlider MakerSlider;
            public Action<float> onUpdate;

            public void Update(float f) => onUpdate(f);
        }
    }
}