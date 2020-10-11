using HarmonyLib;
using IllusionUtility.GetUtility;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KK_Plugins.StudioSceneSettings
{
    public partial class StudioSceneSettings
    {
        internal static class Hooks
        {
#if AI || HS2
            [HarmonyPostfix, HarmonyPatch(typeof(Studio.CameraControl), "OnTriggerEnter")]
            private static void OnTriggerEnter(Collider other, ref List<Collider> ___listCollider)
            {
                if (other == null) return;
                if (!___listCollider.Contains(other))
                    ___listCollider.Add(other);
            }

            [HarmonyPostfix, HarmonyPatch(typeof(Studio.CameraControl), "OnTriggerStay")]
            private static void OnTriggerStay(Collider other, ref List<Collider> ___listCollider)
            {
                if (other == null) return;
                if (!___listCollider.Contains(other))
                    ___listCollider.Add(other);
            }

            [HarmonyPostfix, HarmonyPatch(typeof(Studio.CameraControl), nameof(Studio.CameraControl.LoadVanish))]
            private static void LoadVanish(string _assetbundle, string _file, GameObject _objMap, ref List<Studio.CameraControl.VisibleObject> ___lstMapVanish, ref bool __result)
            {
                ___lstMapVanish.Clear();

                if (_assetbundle.IsNullOrEmpty() || _file.IsNullOrEmpty())
                    return;

                //Delay loading because game code calls this too early
                if (DelayingLoadVanish)
                {
                    DelayingLoadVanish = false;
                    Studio.Studio.Instance.StartCoroutine(LoadVanishDelay(_assetbundle, _file));
                    __result = false;
                    return;
                }
                DelayingLoadVanish = true;

                if (_objMap == null)
                    return;

                List<ExcelData> excelDataList = new List<ExcelData>();
                List<string> assetBundleList = CommonLib.GetAssetBundleNameListFromPath(_assetbundle);
                assetBundleList.Sort();
                for (var i = 0; i < assetBundleList.Count; i++)
                {
                    string ab = assetBundleList[i];
                    var excelData = CommonLib.LoadAsset<ExcelData>(ab, _file);
                    if (excelData != null)
                        excelDataList.Add(excelData);
                }

                for (var i = 0; i < excelDataList.Count; i++)
                {
                    foreach (ExcelData.Param param in excelDataList[i].list.Skip(2))
                    {
                        Studio.CameraControl.VisibleObject visibleObject = new Studio.CameraControl.VisibleObject();
                        visibleObject.nameCollider = param.list[0];
                        for (int l = 1; l < param.list.Count; l++)
                        {
                            if (param.list[l].IsNullOrEmpty())
                                break;

                            var go = _objMap.transform.FindLoop(param.list[l]);
                            if (go != null)
                            {
                                MeshRenderer[] componentsInChildren = go.GetComponentsInChildren<MeshRenderer>(true);
                                visibleObject.listRender.AddRange(componentsInChildren);
                            }
                        }
                        ___lstMapVanish.Add(visibleObject);
                    }
                }
                __result = true;
            }

            private static IEnumerator LoadVanishDelay(string _assetbundle, string _file)
            {
                yield return null;
                yield return null;
                yield return null;
                yield return null;
                yield return null;
                Studio.Studio.Instance.cameraCtrl.LoadVanish(_assetbundle, _file, GameObject.Find("Map"));
            }
#endif
        }
    }
}