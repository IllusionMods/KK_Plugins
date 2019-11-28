using HarmonyLib;
using UnityEngine;

namespace KK_Plugins
{
    public partial class HCharaAdjustment
    {
        internal static class Hooks
        {
            [HarmonyPrefix, HarmonyPatch(typeof(HSceneProc), "EndProc")]
            internal static void EndProc()
            {
                AdjustmentX = 0f;
                AdjustmentY = 0f;
                AdjustmentZ = 0f;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(HSceneProc), "LateShortCut")]
            internal static void LateShortCut(HSceneProc __instance)
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
}