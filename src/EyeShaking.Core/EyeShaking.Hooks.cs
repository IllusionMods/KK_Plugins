using HarmonyLib;
using UnityEngine;

namespace KK_Plugins
{
    public partial class EyeShaking
    {
        internal static class Hooks
        {
            /// <summary>
            /// Insert vaginal
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuKokanPlay))]
            private static void AddSonyuKokanPlay(HFlag __instance) => GetController(__instance.lstHeroine[0].chaCtrl).OnInsert();
            /// <summary>
            /// Insert anal
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuAnalPlay))]
            private static void AddSonyuAnalPlay(HFlag __instance) => GetController(__instance.lstHeroine[0].chaCtrl).OnInsert();

            [HarmonyPrefix, HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuOrg))]
            private static void AddSonyuOrg(HFlag __instance) => GetController(__instance.lstHeroine[0].chaCtrl).AddOrgasm();
            [HarmonyPrefix, HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuSame))]
            private static void AddSonyuSame(HFlag __instance) => GetController(__instance.lstHeroine[0].chaCtrl).AddOrgasm();
            [HarmonyPrefix, HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuAnalOrg))]
            private static void AddSonyuAnalOrg(HFlag __instance) => GetController(__instance.lstHeroine[0].chaCtrl).AddOrgasm();
            [HarmonyPrefix, HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuAnalSame))]
            private static void AddSonyuAnalSame(HFlag __instance) => GetController(__instance.lstHeroine[0].chaCtrl).AddOrgasm();

            /// <summary>
            /// Something that happens at the end of H scene loading, good enough place to hook
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(HSceneProc), nameof(HSceneProc.SetShortcutKey))]
            private static void SetShortcutKey(HSceneProc __instance)
            {
                SaveData.Heroine heroine = __instance.flags.lstHeroine[0];
                GetController(heroine.chaCtrl).HSceneStart(heroine.isVirgin && heroine.isAnalVirgin);
            }

            [HarmonyPrefix, HarmonyPatch(typeof(HSceneProc), nameof(HSceneProc.EndProc))]
            private static void EndProc(HSceneProc __instance)
            {
                GetController(__instance.flags.lstHeroine[0].chaCtrl).HSceneEnd();
            }

            internal static void MapSameObjectDisableVR(object __instance)
            {
                HFlag flags = (HFlag)Traverse.Create(__instance).Field("flags").GetValue();
                SaveData.Heroine heroine = flags.lstHeroine[0];
                GetController(heroine.chaCtrl).HSceneStart(heroine.isVirgin && heroine.isAnalVirgin);
            }

            internal static void EndProcVR(object __instance)
            {
                HFlag flags = (HFlag)Traverse.Create(__instance).Field("flags").GetValue();
                GetController(flags.lstHeroine[0].chaCtrl).HSceneEnd();
            }

#if KKS
            /// <summary>
            /// Eye shaking stuff was removed in KKS, patch it back in
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(EyeLookMaterialControll), nameof(EyeLookMaterialControll.Update))]
            private static bool EyeLookMaterialControll_Update(EyeLookMaterialControll __instance)
            {
                if (__instance.script == null)
                    return false;

                float x = __instance.script.GetAngleHRate(__instance.eyeLR) + __instance.offset.x;
                float y = __instance.script.GetAngleVRate() + __instance.offset.y;
                Vector2 vector = new Vector2(x, y);
                if (vector.magnitude > 1f)
                    vector = vector.normalized;
                float num = Mathf.Lerp(__instance.InsideWait, __instance.OutsideWait, Mathf.InverseLerp(-1f, 1f, vector.x));
                float num2 = Mathf.Lerp(__instance.DownWait, __instance.UpWait, Mathf.InverseLerp(-1f, 1f, vector.y));
                float num3 = Mathf.Lerp(1f, 5f, __instance.scale.x);
                float num4 = Mathf.Lerp(1f, 5f, __instance.scale.y);
                bool flag = false;
                if (__instance.YureTime < __instance.YureTimer)
                {
                    flag = true;
                    __instance.YureTimer = 0f;
                }
                __instance.YureTimer += Time.deltaTime;
                Vector2 a = __instance.scale;
                a.x *= __instance.YureAddScale.x;
                a.y *= __instance.YureAddScale.y;
                for (int i = 0; i < 3; i++)
                {
                    var texState = __instance.texStates[i];
                    Vector2 vector2 = new Vector2(__instance.power, __instance.power);
                    if (texState.isYure)
                    {
                        vector2.x *= 0.8f;
                        vector2.y *= 0.5f;
                    }
                    Vector2 vector3 = new Vector2(Mathf.Clamp(num * (vector2.x * num3), __instance.InsideLimit, __instance.OutsideLimit), Mathf.Clamp(num2 * (vector2.y * num4), __instance.UpLimit, __instance.DownLimit));
                    switch (i)
                    {
                        case 1:
                            vector3 = new Vector2(vector3.x + __instance.hlUpOffsetX, vector3.y + __instance.hlUpOffsetY);
                            break;
                        case 2:
                            vector3 = new Vector2(vector3.x + __instance.hlDownOffsetX, vector3.y + __instance.hlDownOffsetY);
                            break;
                    }
                    Vector2 value = vector3;
                    if (texState.isYure)
                    {
                        value += a * -0.5f;
                        if (flag)
                        {
                            __instance.YureAddScale.x = Random.Range(1f, 2f);
                            __instance.YureAddScale.y = Random.Range(1f, 1.5f);
                        }
                        value += __instance.YureAddVec;
                        __instance.YureTimer += Time.deltaTime;
                    }
                    else
                    {
                        value += __instance.scale * -0.5f;
                    }
                    if (texState.texID != -1)
                    {
                        __instance._material.SetTextureOffset(texState.texID, value);
                        if (texState.isYure)
                            __instance._material.SetTextureScale(texState.texID, new Vector2(1f + a.x, 1f + a.y));
                        else
                            __instance._material.SetTextureScale(texState.texID, new Vector2(1f + __instance.scale.x, 1f + __instance.scale.y));
                    }
                }
                return false;
            }
#endif
        }
    }
}
