using BepInEx;
using BepInEx.Configuration;
using BepInEx.Harmony;
using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClothingUnlocker
{
    [BepInPlugin(GUID,PluginName,Version)]
    public partial class ClothingUnlocker : BaseUnityPlugin
    {
        public static ConfigWrapper<bool> EnableCrossdressing;
        public static ConfigWrapper<bool> EnableBras;
        public static ConfigWrapper<bool> EnableSkirts;

        private void Start() {
            HarmonyWrapper.PatchAll(typeof(ClothingUnlocker));
            EnableCrossdressing = Config.Wrap("Config", "Enable clothing for either gender", "Allows any clothing to be worn by either gender.", true);
            EnableBras = Config.Wrap("Config", "Enable bras for all tops", "Enable bras for all tops for all characters. May cause clipping or other undesired effects.", false);
            EnableSkirts = Config.Wrap("Config", "Enable skirts for all tops", "Enable skirts for all tops for all characters. May cause clipping or other undesired effects.", false);
        }
    }
}
