using BepInEx;
#if KK
using Harmony;
#else
using HarmonyLib;
#endif

namespace ClothingUnlocker
{
    public partial class ClothingUnlocker : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.clothingunlocker";
        public const string PluginName = "Clothing Unlocker";
        public const string Version = "1.1";

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