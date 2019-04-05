using BepInEx;
using Harmony;
using System.ComponentModel;

namespace KK_ClothingUnlocker
{
    /// <summary>
    /// Removes restrictions for clothes
    /// </summary>
    [BepInPlugin(GUID, PluginName, Version)]
    public class KK_ClothingUnlocker : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.clothingunlocker";
        public const string PluginName = "Clothing Unlocker";
        public const string Version = "1.1";

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

        private void Main()
        {
            var harmony = HarmonyInstance.Create(GUID);
            harmony.PatchAll(typeof(KK_ClothingUnlocker));

            EnableCrossdressing = new ConfigWrapper<bool>(nameof(EnableCrossdressing), nameof(KK_ClothingUnlocker), true);
            EnableBras = new ConfigWrapper<bool>(nameof(EnableBras), nameof(KK_ClothingUnlocker), false);
            EnableSkirts = new ConfigWrapper<bool>(nameof(EnableSkirts), nameof(KK_ClothingUnlocker), false);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ListInfoBase), nameof(ListInfoBase.GetInfoInt))]
        public static void GetInfoIntPostfix(ChaListDefine.KeyType keyType, ref int __result)
        {
            if (keyType == ChaListDefine.KeyType.Sex && EnableCrossdressing.Value)
                __result = 1;
            if (keyType == ChaListDefine.KeyType.NotBra && EnableBras.Value)
                __result = 0;
            if (keyType == ChaListDefine.KeyType.Coordinate && EnableSkirts.Value)
                __result = 0;
        }
    }
}
