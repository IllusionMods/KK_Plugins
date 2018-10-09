using BepInEx;
using Harmony;

namespace KK_ForceHighPoly
{
    [BepInPlugin("com.deathweasel.bepinex.forcehighpoly", "Force High Poly", "1.0")]
    public class KK_ForceHighPoly : BaseUnityPlugin
    {
        public KK_ForceHighPoly()
        {
            var harmony = HarmonyInstance.Create("com.deathweasel.bepinex.forcehighpoly");
            harmony.PatchAll(typeof(KK_ForceHighPoly));
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Manager.Character), nameof(Manager.Character.CreateChara))]
        public static void CreateCharaPreHook(ref bool hiPoly)
        {
            hiPoly = true;
        }
    }
}
