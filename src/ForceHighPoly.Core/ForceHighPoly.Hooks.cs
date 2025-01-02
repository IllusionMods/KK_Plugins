using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace KK_Plugins
{
    public partial class ForceHighPoly
    {
        internal static class Hooks
        {
            /// <summary>
            /// Test all coordiantes parts to check if a low poly doesn't exist.
            /// if low poly doesnt exist for an item set HiPoly to true and exit;
            /// </summary>
            /// <param name="__instance"></param>
            [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.Initialize))]
            internal static void CheckHiPoly(ChaControl __instance)
            {
                if (PolySetting.Value == PolyMode.Full && !__instance.hiPoly)
                {
                    ForcedControls.Add(__instance);
                }

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
                        if (!CommonLib.LoadAsset<UnityEngine.GameObject>(assetBundleName, highAssetName + "_low", false, manifestName))
                        {
                            ForcedControls.Add(__instance);
                            return;
                        }
                    }
                }
            }

            #region Multiple replacements
            [HarmonyTranspiler]
            #region Append "_low"
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.LoadAsync), MethodType.Enumerator)]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.GetTexture))]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.CreateFaceTexture))]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.CreateBodyTexture))]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeEyesPtn))]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.InitBaseCustomTextureClothes))]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeClothesTopAsync), MethodType.Enumerator)]
#if KKS
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.LoadNoAsync))]
#endif
            #endregion

            private static IEnumerable<CodeInstruction> IsHiPolyRepeatTranspile(IEnumerable<CodeInstruction> instructions)
            {
                var orig = AccessTools.PropertyGetter(typeof(ChaInfo), nameof(ChaInfo.hiPoly)) ?? throw new Exception("ChaControl.hiPoly");
                var replacement = AccessTools.Method(typeof(ForceHighPoly), nameof(IsHiPoly));

                return new CodeMatcher(instructions)
                    .MatchForward(false, new CodeMatch(OpCodes.Call, orig))
                    .Repeat(matcher => matcher.Set(OpCodes.Call, replacement), s => throw new Exception("match fail - " + s))
                    .Instructions();
            }
            #endregion

            #region Single replacements
            [HarmonyTranspiler]
            #region Append "_low"
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCustomClothes))]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCustomEmblem))]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetCreateTexture))]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.LoadAlphaMaskTexture))]
            #endregion
            private static IEnumerable<CodeInstruction> IsHiPolyTranspile(IEnumerable<CodeInstruction> instructions)
            {
                var orig = AccessTools.PropertyGetter(typeof(ChaInfo), nameof(ChaInfo.hiPoly)) ?? throw new Exception("ChaControl.hiPoly");
                var replacement = AccessTools.Method(typeof(ForceHighPoly), nameof(IsHiPoly));

                return new CodeMatcher(instructions)
                    .MatchForward(false, new CodeMatch(OpCodes.Call, orig))
                    .Set(OpCodes.Call, replacement)
                    .Instructions();
            }
            #endregion

            [HarmonyPrefix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.LoadCharaFbxDataAsync))]
#if KKS
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.LoadCharaFbxDataNoAsync))]
#endif
            private static void FBXDataPrefix(ChaControl __instance, ref bool _hiPoly)
            {
                _hiPoly |= PolySetting.Value != PolyMode.None && ForcedControls.Contains(__instance);
            }

            /// <summary>
            /// Uncensor appends _low if not hiPoly
            /// </summary>
            [HarmonyPatch]
            internal static class UncensorSelectorPatch
            {
                private static bool Prepare()
                {
                    var type = Type.GetType($"KK_Plugins.UncensorSelector+UncensorSelectorController, {Constants.Prefix}_UncensorSelector");//typed out for soft dependancy 
                    return type != null && Traverse.Create(type).Method("ReloadCharacterBody").MethodExists();
                }

                private static MethodInfo TargetMethod()
                {
                    return Type.GetType(typeof(UncensorSelector.UncensorSelectorController).AssemblyQualifiedName).GetMethod("ReloadCharacterBody", AccessTools.all);
                }

                //Transpile seems to occur twice if print statement is included
                private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
                {
                    var orig = AccessTools.PropertyGetter(typeof(ChaInfo), nameof(ChaInfo.hiPoly)) ?? throw new Exception("ChaControl.hiPoly");
                    var replacement = AccessTools.Method(typeof(ForceHighPoly), nameof(IsHiPoly));
                    return new CodeMatcher(instructions)
                        .MatchForward(false, new CodeMatch(OpCodes.Callvirt, orig))
                        .Set(OpCodes.Callvirt, replacement)
                        .Instructions();
                }
            }
        }
    }
}
