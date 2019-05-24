using BepInEx;
using Harmony;
using System.ComponentModel;

namespace ClothingUnlocker
{
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class ClothingUnlocker : BaseUnityPlugin {

        [DisplayName("Enable clothing for either gender")]
        [Category("Config")]
        [Advanced(true)]
        [Description("Allows any clothing to be worn by either gender.")]
        public static ConfigWrapper<bool> EnableCrossdressing { get; private set; }
        [DisplayName("Enable bras for all tops")]
        [Category("Config")]
        [Advanced(true)]
        [Description("Enable bras for all tops for all characters. May cause clipping or other undesired effects.")]
        public static ConfigWrapper<bool> EnableBras { get; private set; }
        [DisplayName("Enable skirts for all tops")]
        [Category("Config")]
        [Advanced(true)]
        [Description("Enable skirts for all tops for all characters. May cause clipping or other undesired effects.")]
        public static ConfigWrapper<bool> EnableSkirts { get; private set; }

        private void Start() {
            var harmony = HarmonyInstance.Create(GUID);
            harmony.PatchAll(typeof(ClothingUnlocker));
            
            EnableCrossdressing = new ConfigWrapper<bool>(nameof(EnableCrossdressing), nameof(ClothingUnlocker), true);
            EnableBras = new ConfigWrapper<bool>(nameof(EnableBras), nameof(ClothingUnlocker), false);
            EnableSkirts = new ConfigWrapper<bool>(nameof(EnableSkirts), nameof(ClothingUnlocker), false);
        }
    }
}
