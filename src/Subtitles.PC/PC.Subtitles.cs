using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KK_Plugins
{
    partial class Subtitles
    {
        private int previousLevel = -1;
        internal void OnLevelWasLoaded(int level)
        {
            if (level == previousLevel)
                return;
            previousLevel = level;
            Caption.UpdateScene();
        }
    }
}
