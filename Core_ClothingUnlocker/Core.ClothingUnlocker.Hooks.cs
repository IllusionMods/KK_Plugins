using BepInEx;
using HarmonyLib;

namespace KK_Plugins
{
    public partial class ClothingUnlocker : BaseUnityPlugin
    {
        internal static class Hooks
        {
            [HarmonyPostfix, HarmonyPatch(typeof(ListInfoBase), nameof(ListInfoBase.GetInfoInt))]
            internal static void GetInfoIntPostfix(ChaListDefine.KeyType keyType, ref int __result)
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
}