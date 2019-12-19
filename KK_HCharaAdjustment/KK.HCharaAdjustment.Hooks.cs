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

                if (AdjustmentXPlus.Value.IsDown())
                    AdjustmentX += 0.01f;
                if (AdjustmentXMinus.Value.IsDown())
                    AdjustmentX -= 0.01f;
                if (AdjustmentXReset.Value.IsDown())
                    AdjustmentX = 0f;
                if (AdjustmentYPlus.Value.IsDown())
                    AdjustmentY += 0.01f;
                if (AdjustmentYMinus.Value.IsDown())
                    AdjustmentY -= 0.01f;
                if (AdjustmentYReset.Value.IsDown())
                    AdjustmentY = 0f;
                if (AdjustmentZPlus.Value.IsDown())
                    AdjustmentZ += 0.01f;
                if (AdjustmentZMinus.Value.IsDown())
                    AdjustmentZ -= 0.01f;
                if (AdjustmentZReset.Value.IsDown())
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