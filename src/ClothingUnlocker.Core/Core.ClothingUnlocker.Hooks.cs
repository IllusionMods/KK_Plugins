using HarmonyLib;

namespace KK_Plugins
{
    public partial class ClothingUnlocker
    {
        internal static class Hooks
        {
            private static ChaControl chaControl;

            [HarmonyPostfix, HarmonyPatch(typeof(ListInfoBase), nameof(ListInfoBase.GetInfoInt))]
            private static void GetInfoIntPostfix(ChaListDefine.KeyType keyType, ref int __result)
            {
                if (keyType == ChaListDefine.KeyType.Sex && EnableCrossdressing.Value)
                    __result = 1;
                else
                {
                    var value = CheckOverride(keyType);
                    if (value != null)
                        __result = int.Parse(value);
                }
            }
            [HarmonyPostfix, HarmonyPatch(typeof(ListInfoBase), nameof(ListInfoBase.GetInfo))]
            private static void GetInfoPostfix(ChaListDefine.KeyType keyType, ref string __result)
            {
                var value = CheckOverride(keyType);
                if (value != null)
                    __result = value;
            }

            /// <summary>
            /// Return 0 to override, null if not
            /// </summary>
            private static string CheckOverride(ChaListDefine.KeyType keyType)
            {
                if (keyType == ChaListDefine.KeyType.NotBra || keyType == ChaListDefine.KeyType.Coordinate || keyType == ChaListDefine.KeyType.HideShorts)
                {
                    if (chaControl == null) return null;
                    var controller = GetController(chaControl);
                    if (controller == null) return null;
                    if (controller.GetClothingUnlocked())
                        return "0";
                }
                return null;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeClothesTop))]
            private static void ChangeClothesTop(ChaControl __instance) => chaControl = __instance;
            [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeClothesBot))]
            private static void ChangeClothesBot(ChaControl __instance) => chaControl = __instance;
            [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeClothesBra))]
            private static void ChangeClothesBra(ChaControl __instance) => chaControl = __instance;
            [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeClothesShorts))]
            private static void ChangeClothesShorts(ChaControl __instance) => chaControl = __instance;
        }
    }
}