using ActionGame;
using HarmonyLib;
using Manager;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
namespace KK_Plugins
{
    public partial class ForceHighPoly
    {
        internal static class Hooks
        {
            /// <summary>
            /// List of controls that were affected by force high poly to stop weird stuff later
            /// Should be list of controls in overworld
            /// Have hiPoly set to True
            /// </summary>
            private static readonly HashSet<ChaControl> ForcedControls = new HashSet<ChaControl>();

            /// <summary>
            /// Test all coordiantes parts to check if a low poly doesn't exist.
            /// if low poly doesnt exist for an item set HiPoly to true and exit;
            /// </summary>
            /// <param name="__instance"></param>
            [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.Initialize))]
            internal static void CheckHiPoly(ChaControl __instance)
            {
                if (!__instance.hiPoly && PolySetting.Value == PolyMode.Full)
                {
                    __instance.hiPoly = true;
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
                        if (clothParts[j].id < 100000000) continue; //if ID is not considered unmodded (using  Sideloader.UAR.BaseSlotID minimum) skip.
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
                        Character.Instance.AddLoadAssetBundle(assetBundleName, manifestName);
#elif KKS
                        Character.AddLoadAssetBundle(assetBundleName, manifestName);
#endif
                        if (!CommonLib.LoadAsset<UnityEngine.GameObject>(assetBundleName, highAssetName + "_low", false, manifestName) && CommonLib.LoadAsset<UnityEngine.GameObject>(assetBundleName, highAssetName, false, manifestName))
                        {
                            __instance.hiPoly = true;
                            ForcedControls.Add(__instance);
                            return;
                        }
                    }
                }
            }

            #region stop hand twitching
            [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.LateUpdateForce)), HarmonyPriority(Priority.Last)]
            private static void LateUpdateForcePrefix(ChaControl __instance, out bool __state)
            {
                __state = __instance.hiPoly;
                if (ForcedControls.Contains(__instance))
                    __instance.hiPoly = false;
            }

            [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.LateUpdateForce)), HarmonyPriority(Priority.First)]
            private static void LateUpdateForcePostfix(ChaControl __instance, bool __state)
            {
                __instance.hiPoly = __state;
            }
            #endregion

            /// <summary>
            /// Clearlist since characters should be reloading
            /// </summary>
            /// <param name="targets"></param>
            [HarmonyPostfix, HarmonyPatch(typeof(Character), nameof(Character.DeleteCharaAll))]
            private static void DeleteCharaAllPostfix()
            {
                ForcedControls.Clear();
            }

            [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.OnDestroy))]
            private static void OnDestroyPrefix(ChaControl __instance)
            {
                ForcedControls.Remove(__instance);
            }

            #region Stop Game from deleting these ChaControls
            /// <summary>
            /// Stops the game from deleting all HiPoly Models by comparing the hashset of known controls that have had HiPoly forcefully applied
            /// Alternatively its possible to get a smaller arrays of ActionGame.Chara.Base[] targets from methods in ActionScene for the same effect, but requires additional patches
            /// Some stuff has while heroine.chactrl != null thus the need to reassign it since it attempts to delete hiPoly <see cref="ActionScene._SceneEventNPC"/>
            /// </summary>
            /// <param name="entryOnly">Only true so far when <see cref="ChaControl.OnDestroy"/> is called </param>
            [HarmonyPrefix, HarmonyPatch(typeof(Character), nameof(Character.DeleteChara)), HarmonyPriority(Priority.Last)]
            private static bool DeleteCharaPrefix(ChaControl cha, bool entryOnly, ref bool __result)
            {
#if KK
                if (Game.Instance == null || Game.Instance.actScene == null || entryOnly) return true;//use original function 
                var getNPC = Game.Instance.actScene.npcList.FirstOrDefault(x => x.heroine.chaCtrl == cha);
#elif KKS
                if (ActionScene.instance == null || entryOnly) return true;
                var getNPC = ActionScene.instance.npcList.FirstOrDefault(x => x.heroine.chaCtrl == cha);
#endif
                if (getNPC != null && ForcedControls.Contains(cha))
                {
                    getNPC.heroine.chaCtrl = null;
                    cha.StartCoroutine(ReassignHeroine(cha, getNPC.heroine));
                    __result = true;
                    return false;//skip original
                }
                return true;//original function
            }

            static IEnumerator ReassignHeroine(ChaControl cha, SaveData.Heroine hero)
            {
                yield return null;
                hero.chaCtrl = cha;
            }
            #endregion

            #region the game methods are bad and should feel bad
            [HarmonyPrefix, HarmonyPatch(typeof(EnvArea3D), nameof(EnvArea3D.Update)), HarmonyPriority(Priority.Last)]
            private static bool EnvArea3DUpdatePrefix(EnvArea3D __instance)
            {
                if (!Character.IsInstance())
                {
                    return false;
                }
#if KK
                var NatHiPoly = Character.Instance.dictEntryChara.Values.Any(x => x != null && !ForcedControls.Contains(x) && x.hiPoly);//only flag when a natural hiPoly ChaControl Exists
#elif KKS
                var NatHiPoly = Character.ChaControls.Any(x => x != null && !ForcedControls.Contains(x) && x.hiPoly);//only flag when a natural hiPoly ChaControl Exists
#endif

                if (__instance._playdataSolid != null)
                {
                    foreach (var item in __instance._playdataSolid)
                    {
                        item.Update(!NatHiPoly);
                    }
                }
                return false;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(ActionGame.MapSound.LoopSEVolume), nameof(ActionGame.MapSound.LoopSEVolume.Update)), HarmonyPriority(Priority.Last)]
            private static bool LoopSEVolumeUpdatePrefix(ActionGame.MapSound.LoopSEVolume __instance)
            {
                if (!Character.IsInstance())
                {
                    return false;
                }
#if KK
                if (!Game.IsInstance())
                {
                    return false;
                }
                var actionscene = Game.Instance.actScene;
#elif KKS
                var actionscene = ActionScene.instance;
#endif
                if (actionscene == null)
                {
                    return false;
                }
#if KK
                var NatHiPoly = Character.Instance.dictEntryChara.Values.Any(x => x != null && !ForcedControls.Contains(x) && x.hiPoly);//only flag when a natural hiPoly ChaControl Exists
#elif KKS
                var NatHiPoly = Character.ChaControls.Any(x => x != null && !ForcedControls.Contains(x) && x.hiPoly);//only flag when a natural hiPoly ChaControl Exists
#endif
                var runtimeDataList = __instance._runtimeDataList;

                if (!NatHiPoly && (__instance._ignoreCondition || actionscene.npcList.Any(x => x.isActive && x.mapNo == __instance.MapID && x.isArrival && x.AI.actionNo == __instance.ActionID)))
                {
                    foreach (var item in runtimeDataList)
                    {
                        item.Play(__instance._soundType);
                    }
                    return false;
                }
                foreach (var item in runtimeDataList)
                {
                    item.Stop();
                }
                return false;
            }
#if KKS
            [HarmonyPrefix, HarmonyPatch(typeof(EnvLineArea3D), nameof(EnvLineArea3D.Update)), HarmonyPriority(Priority.Last)]
            private static bool EnvLineArea3DUpdatePrefix(EnvLineArea3D __instance)
            {
                if (!Character.IsInstance())
                {
                    return false;
                }

                var NatHiPoly = Character.ChaControls.Any(x => !ForcedControls.Contains(x) && x.hiPoly);//only flag when a natural hiPoly ChaControl Exists

                if (__instance._playdataSolid != null)
                {
                    foreach (var item in __instance._playdataSolid)
                    {
                        item.Update(!NatHiPoly);
                    }
                }
                return false;
            }
#endif
            [HarmonyPrefix, HarmonyPatch(typeof(CorrectLightAngle), nameof(CorrectLightAngle.LateUpdate)), HarmonyPriority(Priority.Last)]
            private static bool CorrectLightAngleLateUpdate(CorrectLightAngle __instance)
            {
                ChaControl cha;
                if (!__instance.condition.IsNullOrEmpty())
                {
                    cha = __instance.condition();
                }
                else
                {
#if KK
                    cha = Character.Instance.dictEntryChara.Values.FirstOrDefault(x => x.hiPoly && !ForcedControls.Contains(x));
#elif KKS
                    cha = Character.ChaControls.FirstOrDefault(x => x.hiPoly && !ForcedControls.Contains(x));
#endif
                }
                var neck = __instance.GetNeck(cha);
                if (neck == null)
                {
                    __instance.lightTrans.localRotation = __instance.initRot;
                    return false;
                }
                __instance.lightTrans.rotation = neck.rotation;
                __instance.lightTrans.Rotate(__instance.correctRX + __instance.offset.x, __instance.correctRY + __instance.offset.y, 0f);
                return false;
            }
            #endregion
        }
    }
}