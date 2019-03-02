using BepInEx;
using Harmony;
using UnityEngine;
/// <summary>
/// Plugin for adjusting the female character in H scene independently of the male. Needs a UI.
/// </summary>
namespace KK_HCharaAdjustment
{
    [BepInPlugin(GUID, PluginName, Version)]
    public class KK_HCharaAdjustment : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.hcharaadjustment";
        public const string PluginName = "H Chara Adjustment";
        public const string PluginNameInternal = "KK_HCharaAdjustment";
        public const string Version = "1.0";

        void Main()
        {
            var harmony = HarmonyInstance.Create(GUID);
            harmony.PatchAll(typeof(KK_HCharaAdjustment));
        }

        private static float AdjustmentX = 0;
        private static float AdjustmentY = 0;
        private static float AdjustmentZ = 0;

        [HarmonyPrefix, HarmonyPatch(typeof(HSceneProc), "EndProc")]
        public static void EndProc()
        {
            AdjustmentX = 0f;
            AdjustmentY = 0f;
            AdjustmentZ = 0f;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HSceneProc), "LateShortCut")]
        public static void LateShortCut(HSceneProc __instance)
        {
            ChaControl heroine = __instance.flags.lstHeroine[0].chaCtrl;

            if (Input.GetKeyDown(KeyCode.P))
                AdjustmentX += 0.01f;
            if (Input.GetKeyDown(KeyCode.O))
                AdjustmentX -= 0.01f;
            if (Input.GetKeyDown(KeyCode.I))
                AdjustmentX = 0f;
            if (Input.GetKeyDown(KeyCode.L))
                AdjustmentY += 0.01f;
            if (Input.GetKeyDown(KeyCode.K))
                AdjustmentY -= 0.01f;
            if (Input.GetKeyDown(KeyCode.J))
                AdjustmentY = 0f;
            if (Input.GetKeyDown(KeyCode.M))
                AdjustmentZ += 0.01f;
            if (Input.GetKeyDown(KeyCode.N))
                AdjustmentZ -= 0.01f;
            if (Input.GetKeyDown(KeyCode.B))
                AdjustmentZ = 0f;

            Vector3 pos = heroine.GetPosition();
            pos.x += AdjustmentX;
            pos.y += AdjustmentY;
            pos.z += AdjustmentZ;
            heroine.SetPosition(pos);
        }
    }
}