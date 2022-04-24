using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace KK_Plugins
{
    public partial class ForceHighPoly
    {
        internal static class Hooks
        {
            static HashSet<ChaControl> m_ChaControls = new HashSet<ChaControl>();
            /// <summary>
            /// Test all coordiantes parts to check if a low poly doesn't exist.
            /// if low poly doesnt exist for an item set HiPoly to true and exit;
            /// </summary>
            /// <param name="__instance"></param>
            [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.Initialize))]
            internal static void CheckHiPoly(ChaControl __instance)
            {
                if (PolySetting.Value == PolyMode.Full && !__instance.hiPoly)
                { m_ChaControls.Add(__instance); }

                if (__instance.hiPoly || PolySetting.Value != PolyMode.Partial) return;

                var exType = Traverse.Create(__instance).Property("exType");
                var exTypeExists = exType.PropertyExists();
                var coordinate = __instance.chaFile.coordinate;
                for (var i = 0; i < coordinate.Length; i++)
                {
                    var clothParts = coordinate[i].clothes.parts;
                    for (var j = 0; j < clothParts.Length; j++)
                    {
                        if (clothParts[j].id < 10000000) continue;
                        var category = 105;
                        switch (j)
                        {
                            case 0:
                                category = (__instance.sex != 0 || exTypeExists && exType.GetValue<int>() != 1) ? 105 : 503;
                                break;
                            case 7:
                            case 8:
                                category = ((__instance.sex != 0 || exTypeExists && exType.GetValue<int>() != 1) ? 112 : 504) - j;
                                break;
                            default:
                                break;
                        }
                        category += j;
                        var work = __instance.lstCtrl.GetCategoryInfo((ChaListDefine.CategoryNo)category);
                        if (!work.TryGetValue(clothParts[j].id, out var lib))
                        {
                            continue;
                        }
                        else if (category == 105 || category == 107)
                        {
                            var infoInt = lib.GetInfoInt(ChaListDefine.KeyType.Sex);
                            if (__instance.sex == 0 && infoInt == 3 || __instance.sex == 1 && infoInt == 2)
                            {
                                if (clothParts[j].id != 0)
                                {
                                    work.TryGetValue(0, out lib);
                                }
                                if (lib == null)
                                {
                                    continue;
                                }
                            }
                        }
                        var highAssetName = lib.GetInfo(ChaListDefine.KeyType.MainData);
                        if (string.Empty == highAssetName)
                        {
                            continue;
                        }
                        var manifestName = lib.GetInfo(ChaListDefine.KeyType.MainManifest);
                        var assetBundleName = lib.GetInfo(ChaListDefine.KeyType.MainAB);
#if KK
                        Singleton<Manager.Character>.Instance.AddLoadAssetBundle(assetBundleName, manifestName);
#elif KKS
                        Manager.Character.AddLoadAssetBundle(assetBundleName, manifestName);
#endif
                        if (!CommonLib.LoadAsset<UnityEngine.GameObject>(assetBundleName, highAssetName + "_low", false, manifestName) && CommonLib.LoadAsset<UnityEngine.GameObject>(assetBundleName, highAssetName, false, manifestName))
                        {
                            m_ChaControls.Add(__instance);
                            return;
                        }
                    }
                }
            }

            [HarmonyTranspiler]
            #region Append "_low"
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.LoadNoAsync))]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.LoadAsync), MethodType.Enumerator)]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.GetTexture))]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.CreateFaceTexture))]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.CreateBodyTexture))]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeEyesPtn))]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.InitBaseCustomTextureClothes))]
            #endregion

            #region DynamicBones
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeHairNoAsync))]
            #endregion

#if KKS

#endif
            //...

            private static IEnumerable<CodeInstruction> IsHiPolyRepeatTranspile(IEnumerable<CodeInstruction> instructions)
            {
                var orig = AccessTools.PropertyGetter(typeof(ChaInfo), nameof(ChaInfo.hiPoly)) ?? throw new Exception("ChaControl.hiPoly");
                var replacement = AccessTools.Method(typeof(Hooks), nameof(IsHiPoly));

                return new CodeMatcher(instructions)
                    .MatchForward(false, new CodeMatch(OpCodes.Call, orig))
                    .Repeat(matcher => matcher.Set(OpCodes.Call, replacement), s => throw new Exception("match fail - " + s))
                    .Instructions();
            }

            [HarmonyTranspiler]

            #region Append "_low"
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCustomClothes))]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCustomEmblem))]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetCreateTexture))]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.LoadAlphaMaskTexture))]
            #endregion

            #region Texture Resolution
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.InitBaseCustomTextureBody))]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.InitBaseCustomTextureFace))]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.InitBaseCustomTextureEtc))]
            #endregion

            #region Math?
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.InitShapeBody))]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetShapeBodyValue))]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.UpdateShapeBodyValueFromCustomInfo))]
            #endregion


            private static IEnumerable<CodeInstruction> IsHiPolyTranspile(IEnumerable<CodeInstruction> instructions)
            {
                var orig = AccessTools.PropertyGetter(typeof(ChaInfo), nameof(ChaInfo.hiPoly)) ?? throw new Exception("ChaControl.hiPoly");
                var replacement = AccessTools.Method(typeof(Hooks), nameof(IsHiPoly));

                return new CodeMatcher(instructions)
                    .MatchForward(false, new CodeMatch(OpCodes.Call, orig))
                    .Set(OpCodes.Call, replacement)
                    .Instructions();
            }

            private static bool IsHiPoly(ChaControl cha)
            {
                return cha.hiPoly || m_ChaControls.Contains(cha);
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.LoadCharaFbxDataNoAsync))]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.LoadCharaFbxDataAsync))]
            private static void FBXDataPrefix(ChaControl __instance, ref bool _hiPoly)
            {
                _hiPoly |= m_ChaControls.Contains(__instance);
            }
        }
    }
}