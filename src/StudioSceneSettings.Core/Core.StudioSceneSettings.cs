using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using IllusionUtility.GetUtility;
using KKAPI;
using KKAPI.Studio.SaveLoad;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KK_Plugins.StudioSceneSettings
{
    [BepInProcess(Constants.StudioProcessName)]
    [BepInDependency(KoikatuAPI.GUID, "1.11")]
    [BepInPlugin(GUID, PluginName, Version)]
    public class StudioSceneSettings : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.studioscenesettings";
        public const string PluginName = "StudioSceneSettings";
        public const string PluginNameInternal = Constants.Prefix + "_StudioSceneSettings";
        public const string Version = "1.2";
        internal static new ManualLogSource Logger;

#if KK
        internal const int CameraMapMaskingLayer = 26;
#else
        internal const int CameraMapMaskingLayer = 22;
        private static bool DelayingLoadVanish = true;
#endif

        internal void Main()
        {
            Logger = base.Logger;
            StudioSaveLoadApi.RegisterExtraBehaviour<SceneController>(GUID);
            Harmony.CreateAndPatchAll(typeof(Hooks));
        }

        internal static class Hooks
        {
#if AI || HS2
            [HarmonyPostfix, HarmonyPatch(typeof(Studio.CameraControl), "OnTriggerEnter")]
            internal static void OnTriggerEnter(Collider other, ref List<Collider> ___listCollider)
            {
                if (other == null) return;

                if (___listCollider.Find((Collider x) => other.name == x.name) == null)
                    ___listCollider.Add(other);
            }

            [HarmonyPostfix, HarmonyPatch(typeof(Studio.CameraControl), "OnTriggerStay")]
            internal static void OnTriggerStay(Collider other, ref List<Collider> ___listCollider)
            {
                if (other == null) return;
                if (___listCollider.Find((Collider x) => other.name == x.name) == null)
                    ___listCollider.Add(other);
            }

            [HarmonyPostfix, HarmonyPatch(typeof(Studio.CameraControl), nameof(Studio.CameraControl.LoadVanish))]
            internal static void LoadVanish(string _assetbundle, string _file, GameObject _objMap, ref List<Studio.CameraControl.VisibleObject> ___lstMapVanish, ref bool __result)
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
                foreach (string ab in assetBundleList)
                {
                    var excelData = CommonLib.LoadAsset<ExcelData>(ab, _file);
                    if (excelData != null)
                        excelDataList.Add(excelData);
                }

                foreach (ExcelData excelData in excelDataList)
                {
                    foreach (ExcelData.Param param in excelData.list.Skip(2))
                    {
                        Studio.CameraControl.VisibleObject visibleObject = new Studio.CameraControl.VisibleObject();
                        visibleObject.nameCollider = param.list[0];
                        for (int l = 1; l < param.list.Count; l++)
                        {
                            if (param.list[l].IsNullOrEmpty())
                                break;

#if AI
                            GameObject go = _objMap.transform.FindLoop(param.list[l]);
#else
                            GameObject go = _objMap.transform.FindLoop(param.list[l])?.gameObject;
#endif
                            if (!(go == null))
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

#if AI || HS2
    public class StudioCameraColliderController : MonoBehaviour
    {
        protected void OnTriggerEnter(Collider other) => Traverse.Create(Studio.Studio.Instance.cameraCtrl).Method("OnTriggerEnter", other).GetValue();
        protected void OnTriggerStay(Collider other) => Traverse.Create(Studio.Studio.Instance.cameraCtrl).Method("OnTriggerStay", other).GetValue();
        protected void OnTriggerExit(Collider other) => Traverse.Create(Studio.Studio.Instance.cameraCtrl).Method("OnTriggerExit", other).GetValue();
    }
#endif
}