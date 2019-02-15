using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx;
using Harmony;

namespace KK_ClothingUnlocker
{
    /// <summary>
    /// Removes gender restrictions for clothes
    /// </summary>
    [BepInPlugin(GUID, PluginName, Version)]
    public class KK_ClothingUnlocker : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.clothingunlocker";
        public const string PluginName = "Clothing Unlocker";
        public const string Version = "1.0";

        void Main()
        {
            var harmony = HarmonyInstance.Create(GUID);
            harmony.PatchAll(typeof(KK_ClothingUnlocker));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ListInfoBase), nameof(ListInfoBase.GetInfoInt))]
        public static void LoadAsyncPrefix(ChaListDefine.KeyType keyType, ref int __result)
        {
            if (keyType == ChaListDefine.KeyType.Sex)
                __result = 1;
        }
    }
}
