using BepInEx;
using BepInEx.Configuration;
using BepInEx.Harmony;
using HarmonyLib;

namespace KK_Plugins
{
    public partial class ClothingUnlocker : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.clothingunlocker";
        public const string PluginName = "Clothing Unlocker";
        public const string Version = "1.1";

        public static ConfigEntry<bool> EnableCrossdressing;
        public static ConfigEntry<bool> EnableBras;
        public static ConfigEntry<bool> EnableSkirts;

        internal void Start()
        {
            EnableCrossdressing = Config.Bind("Config", "Enable clothing for either gender", true, "Allows any clothing to be worn by either gender.");
            EnableBras = Config.Bind("Config", "Enable bras for all tops", false, "Enable bras for all tops for all characters. May cause clipping or other undesired effects.");
            EnableSkirts = Config.Bind("Config", "Enable skirts for all tops", false, "Enable skirts for all tops for all characters. May cause clipping or other undesired effects.");

            HarmonyWrapper.PatchAll(typeof(ClothingUnlocker));
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