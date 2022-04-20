using HarmonyLib;
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
            /// Test all coordiantes parts to check if a low poly doesn't exist.
            /// if low poly doesnt exist for an item set HiPoly to true and exit;
            /// </summary>
            /// <param name="__instance"></param>
            [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.Initialize))]
            internal static void CheckHiPoly(ChaControl __instance)
            {
                if (PolySetting.Value == PolyMode.Full) __instance.hiPoly = true;

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
                            __instance.hiPoly = true;
                            return;
                        }
                    }
                }
            }

            /// <summary>
            /// Might not be neccessary because of the first line of the above method, put probably also cheaper than the previous method
            /// Equivilant to last method results of patching AssetBundleManager and trimming _low
            /// </summary>
            /// <param name="__result"></param>
            [HarmonyPostfix, HarmonyPatch(typeof(ChaInfo), nameof(ChaInfo.hiPoly), MethodType.Getter)]
            private static void HiPolyPostfix(ref bool __result)
            {
                __result = __result || Enabled.Value;
            }

            #region Stop Game from deleting these ChaControls
            static ActionGame.Chara.Base[] DontDelete = new ActionGame.Chara.Base[0];

            /// <summary>
            /// Prefix the following methods to take the Base[] targets parameter.
            /// parameters 
            /// </summary>
            /// <param name="targets"></param>
            [HarmonyPrefix]
            [HarmonyPatch(typeof(ActionScene), nameof(ActionScene.ChangeH))]
            [HarmonyPatch(typeof(ActionScene), nameof(ActionScene.ChangeHEventWithActionPoint))]
#if KK
            [HarmonyPatch(typeof(ActionScene), nameof(ActionScene.ChangeHSpecial), new[] { typeof(List<int>), typeof(bool), typeof(UnityEngine.Vector3), typeof(UnityEngine.Quaternion), typeof(System.Action<ActionGame.H.OpenHData.Data>), typeof(ActionGame.Chara.Base[]) })]
#elif KKS
            [HarmonyPatch(typeof(ActionScene), nameof(ActionScene.ChangeHSpecial), new[] { typeof(List<int>), typeof(bool), typeof(UnityEngine.Vector3), typeof(UnityEngine.Quaternion), typeof(System.Action<ActionGame.H.OpenHData.Data>), typeof(System.Threading.CancellationToken), typeof(ActionGame.Chara.Base[]) })]
            [HarmonyPatch(typeof(ActionScene), nameof(ActionScene.ChangeEsthetic))]
#endif
            public static void GetTargets(ActionGame.Chara.Base[] targets)
            {
                DontDelete = targets;
            }

            [HarmonyPostfix, HarmonyPatch(typeof(Manager.Character), nameof(Manager.Character.DeleteCharaAll))]
            private static void DeleteCharaAllPostfix()
            {
                DontDelete = new ActionGame.Chara.Base[0];
            }
            #endregion

            /// <summary>
            /// Stops the game from deleting all HiPoly Models by comparing it with an array of targets from Overworld
            /// Some stuff has while heroine.chactrl != null thus the need to reassign it since it attempts to delete hiPoly <see cref="ActionScene._SceneEventNPC"/>
            /// </summary>
            /// <param name="entryOnly"></param>
            [HarmonyPrefix, HarmonyPatch(typeof(Manager.Character), nameof(Manager.Character.DeleteChara)), HarmonyPriority(Priority.Last)]
            private static bool DeleteCharaPrefix(ChaControl cha, bool entryOnly, ref bool __result)
            {
#if DEBUG
#if KK
                foreach (var item in Manager.Character.Instance.dictEntryChara)
                {
                    logSource.Log(cha == item.Value ? BepInEx.Logging.LogLevel.Fatal : BepInEx.Logging.LogLevel.Warning, $"{item.Key} {item.Value.fileParam.fullname} MainGame heroine? {Manager.Game.Instance.actScene.npcList.FirstOrDefault(x => x.heroine.chaCtrl == item.Value) != null}\n\tDontDelete {DontDelete.Any(x => x.chaCtrl == item.Value)} count {DontDelete.Length}\n\thiPoly: {item.Value.hiPoly}");
                }
#elif KKS
                foreach (var item in Manager.Character.dictEntryChara)
                {
                    logSource.Log(cha == item.Value ? BepInEx.Logging.LogLevel.Fatal : BepInEx.Logging.LogLevel.Warning, $"{item.Key} {item.Value.fileParam.fullname} MainGame heroine? {ActionScene.instance.npcList.FirstOrDefault(x => x.heroine.chaCtrl == item.Value) != null}\n\tDontDelete {DontDelete.Any(x => x.chaCtrl == item.Value)} count {DontDelete.Length}\n\thiPoly: {item.Value.hiPoly}");
                }
#endif
#endif
#if KK
                var getNPC = Manager.Game.Instance.actScene.npcList.FirstOrDefault(x => x.heroine.chaCtrl == cha);
#elif KKS
                var getNPC = ActionScene.instance.npcList.FirstOrDefault(x => x.heroine.chaCtrl == cha);
#endif
                if (getNPC != null && DontDelete.Any(x => x.chaCtrl == cha))
                {
                    getNPC.heroine.chaCtrl = null;
                    cha.StartCoroutine(ReassignHeroine(cha, getNPC.heroine));
                    __result = true;
                    return false;
                }
                //original function below
#if KK
                foreach (var keyValuePair in Manager.Character.Instance.dictEntryChara)
#elif KKS
                foreach (var keyValuePair in Manager.Character.dictEntryChara)
#endif
                {
                    if (keyValuePair.Value == cha)
                    {
                        if (!entryOnly)
                        {
                            keyValuePair.Value.name = "Delete_Reserve";
                            keyValuePair.Value.transform.SetParent(null);
                            Destroy(keyValuePair.Value.gameObject);
                        }
#if KK
                        Manager.Character.Instance.dictEntryChara.Remove(keyValuePair.Key);
#elif KKS
                        Manager.Character.dictEntryChara.Remove(keyValuePair.Key);
                        Manager.Character.chaControls.Remove(cha);
#endif
                        __result = true;
                        return false;
                    }
                }

                __result = false;
                return false;
            }

            static IEnumerator ReassignHeroine(ChaControl cha, SaveData.Heroine hero)
            {
                yield return null;
                hero.chaCtrl = cha;
            }
        }
    }
}