using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Studio;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace KK_Plugins
{
    [BepInPlugin(GUID, PluginName, Version)]
    public class FreeHStudioSceneLoader : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.freehstudiosceneloader";
        public const string PluginName = "Free H Studio Scene Loader";
        public const string PluginNameInternal = "KK_FreeHStudioSceneLoader";
        public const string Version = "1.0";
        internal static new ManualLogSource Logger;

        private static bool IsStudio;

        internal void Awake()
        {
            Logger = base.Logger;
            IsStudio = Application.productName == "CharaStudio";

            var harmony = Harmony.CreateAndPatchAll(typeof(Hooks));
            harmony.Patch(typeof(CameraLightCtrl).GetNestedType("LightCalc", AccessTools.all).GetMethod("Init"), new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.CameraLightCtrl_LightCalc_Init), AccessTools.all)), null);
            harmony.Patch(typeof(CameraLightCtrl).GetNestedType("LightCalc", AccessTools.all).GetMethod("UpdateUI"), new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.CameraLightCtrl_LightCalc_UpdateUI), AccessTools.all)), null);
            harmony.Patch(typeof(CameraLightCtrl).GetNestedType("LightCalc", AccessTools.all).GetMethod("Reflect"), new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.CameraLightCtrl_LightCalc_Reflect), AccessTools.all)), null);
        }

        private static class Hooks
        {
            [HarmonyPostfix, HarmonyPatch(typeof(Manager.Config), "Start")]
            internal static void Config_Start()
            {
                if (IsStudio) return;

                GameObject go = new GameObject("Studio");
                DontDestroyOnLoad(go);
                go.AddComponent<Studio.Studio>();
                go.AddComponent<MapCtrl>();
                go.AddComponent<Map>();
                go.AddComponent<Info>();
                go.AddComponent<GuideObjectManager>();
                Singleton<Info>.Instance.StartCoroutine(Singleton<Info>.Instance.LoadExcelDataCoroutine());
                Traverse.Create(Singleton<Studio.Studio>.Instance).Field("m_CameraCtrl").SetValue(go.AddComponent<Studio.CameraControl>());
                Traverse.Create(Singleton<Studio.Studio>.Instance).Field("m_CameraLightCtrl").SetValue(go.AddComponent<CameraLightCtrl>());
                Traverse.Create(Singleton<Studio.Studio>.Instance).Field("m_TreeNodeCtrl").SetValue(go.AddComponent<TreeNodeCtrl>());
                Traverse.Create(Singleton<Studio.Studio>.Instance).Field("_patternSelectListCtrl").SetValue(go.AddComponent<PatternSelectListCtrl>());
                Singleton<Studio.Studio>.Instance.Init();

                //StudioScene.CreatePatternList();
            }

            #region NullRef Fixes
            [HarmonyPrefix, HarmonyPatch(typeof(BackgroundCtrl), nameof(BackgroundCtrl.isVisible), MethodType.Setter)]
            internal static bool BackgroundCtrl_IsVisible(ref MeshRenderer ___meshRenderer, ref bool ___m_IsVisible, bool value)
            {
                if (IsStudio) return true;

                ___m_IsVisible = value;
                //Added null check
                if (___meshRenderer != null)
                    ___meshRenderer.enabled = value;
                return false;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(Studio.CameraControl), nameof(Studio.CameraControl.ReflectOption))]
            internal static bool CameraControl_ReflectOption(Studio.CameraControl __instance, ref float ___rateAddSpeed, ref Camera ___m_SubCamera)
            {
                if (IsStudio) return true;

                ___rateAddSpeed = Studio.Studio.optionSystem.cameraSpeed;
                __instance.xRotSpeed = Studio.Studio.optionSystem.cameraSpeedX;
                __instance.yRotSpeed = Studio.Studio.optionSystem.cameraSpeedY;
                List<string> list = new List<string>();
                if (Singleton<Studio.Studio>.Instance.workInfo.visibleAxis)
                {
                    if (Studio.Studio.optionSystem.selectedState == 0)
                        list.Add("Studio/Col");
                    list.Add("Studio/Select");
                }
                list.Add("Studio/Route");

                //Added null check
                if (___m_SubCamera != null)
                    ___m_SubCamera.cullingMask = LayerMask.GetMask(list.ToArray());
                return false;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(GuideObject), nameof(GuideObject.guideMove), MethodType.Getter)]
            internal static bool GuideObject_guideMove(ref GuideMove[] __result)
            {
                if (IsStudio) return true;

                __result = new GuideMove[0];
                return false;
            }
            [HarmonyPrefix, HarmonyPatch(typeof(GuideObject), nameof(GuideObject.guideSelect), MethodType.Getter)]
            internal static bool GuideObject_guideSelect(ref GuideSelect __result)
            {
                if (IsStudio) return true;

                __result = new GuideSelect();
                return false;
            }
            [HarmonyPrefix, HarmonyPatch(typeof(GuideObject), nameof(GuideObject.enablePos), MethodType.Setter)]
            internal static bool GuideObject_enablePos() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(GuideObject), nameof(GuideObject.enableRot), MethodType.Setter)]
            internal static bool GuideObject_enableRot() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(GuideObject), nameof(GuideObject.enableScale), MethodType.Setter)]
            internal static bool GuideObject_enableScale() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(GuideObject), nameof(GuideObject.SetMode))]
            internal static bool GuideObject_SetMode() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(GuideObject), nameof(GuideObject.SetScale))]
            internal static bool GuideObject_SetScale() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(GuideObject), nameof(GuideObject.SetVisibleCenter))]
            internal static bool GuideObject_SetVisibleCenter() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(GuideObject), "Awake")]
            internal static bool GuideObject_Awake(GuideObject __instance, ref int ___m_DicKey)
            {
                if (IsStudio) return true;

                //Only run part of the Awake code
                ___m_DicKey = -1;
                __instance.isActiveFunc = null;
                __instance.parentGuide = null;
                __instance.enableMaluti = true;
                __instance.calcScale = true;

                return false;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(GuideObject), "LateUpdate")]
            internal static bool GuideObject_LateUpdate(GuideObject __instance, ref int ___m_DicKey, ref GameObject[] ___roots)
            {
                if (IsStudio) return true;

                if (__instance.parent && __instance.nonconnect)
                {
                    Traverse.Create(__instance).Method("CalcPosition").GetValue();
                    Traverse.Create(__instance).Method("CalcRotation").GetValue();
                }
                __instance.transform.position = __instance.transformTarget.position;
                __instance.transform.rotation = __instance.transformTarget.rotation;
                switch (__instance.mode)
                {
                    case GuideObject.Mode.Local:
                        //Added null check
                        if (___roots[0] != null)
                            ___roots[0].transform.eulerAngles = __instance.parent ? __instance.parent.eulerAngles : Vector3.zero;
                        break;
                    case GuideObject.Mode.LocalIK:
                        //Added null check
                        if (___roots[0] != null)
                            ___roots[0].transform.localEulerAngles = Vector3.zero;
                        break;
                    case GuideObject.Mode.World:
                        //Added null check
                        if (___roots[0] != null)
                            ___roots[0].transform.eulerAngles = Vector3.zero;
                        break;
                }
                if (__instance.calcScale)
                {
                    Vector3 localScale = __instance.transformTarget.localScale;
                    Vector3 lossyScale = __instance.transformTarget.lossyScale;
                    Vector3 vector = __instance.enableScale ? __instance.changeAmount.scale : Vector3.one;
                    __instance.transformTarget.localScale = new Vector3(localScale.x / lossyScale.x * vector.x, localScale.y / lossyScale.y * vector.y, localScale.z / lossyScale.z * vector.z);
                }

                return false;
            }
            [HarmonyPrefix, HarmonyPatch(typeof(OCILight), nameof(OCILight.SetDrawTarget))]
            internal static bool OCILight_SetDrawTarget() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(MapCtrl), nameof(MapCtrl.UpdateUI))]
            internal static bool MapCtrl_UpdateUI() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(MapCtrl), "Awake")]
            internal static bool MapCtrl_Awake() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(PatternSelectListCtrl), nameof(PatternSelectListCtrl.Create))]
            internal static bool PatternSelectListCtrl_Create() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(PatternSelectListCtrl), "Start")]
            internal static bool PatternSelectListCtrl_Start() => IsStudio;

            [HarmonyPrefix, HarmonyPatch(typeof(GuideObjectManager), nameof(GuideObjectManager.DeleteAll))]
            internal static bool GuideObjectManager_DeleteAll(GuideObjectManager __instance, ref HashSet<GuideObject> ___hashSelectObject, ref Dictionary<Transform, GuideObject> ___dicGuideObject, ref Dictionary<Transform, Light> ___dicTransLight, ref Dictionary<GuideObject, Light> ___dicGuideLight)
            {
                if (IsStudio) return true;

                ___hashSelectObject.Clear();
                __instance.operationTarget = null;
                GameObject[] array = (from v in ___dicGuideObject
                                      where v.Value != null
                                      select v.Value.gameObject).ToArray();
                for (int i = 0; i < array.Length; i++)
                    if (array[i] != null)
                        DestroyImmediate(array[i]);
                ___dicGuideObject.Clear();
                ___dicTransLight.Clear();
                ___dicGuideLight.Clear();
                //Added null check
                __instance.drawLightLine?.Clear();
                //Added null check
                __instance.guideInput?.Stop();

                return false;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(GuideObjectManager), "Awake")]
            internal static void GuideObjectManager_Awake(ref GameObject ___objectOriginal)
            {
                if (IsStudio) return;

                if (___objectOriginal == null)
                {
                    ___objectOriginal = new GameObject();
                    ___objectOriginal.SetActive(false);
                    ___objectOriginal.AddComponent<GuideObject>();
                    DontDestroyOnLoad(___objectOriginal);
                }
            }

            [HarmonyPrefix, HarmonyPatch(typeof(TreeNodeCtrl), nameof(TreeNodeCtrl.AddNode), typeof(string), typeof(TreeNodeObject))]
            internal static bool TreeNodeCtrl_AddNode(TreeNodeCtrl __instance, TreeNodeObject _parent, ref TreeNodeObject __result, ref List<TreeNodeObject> ___m_TreeNodeObject)
            {
                if (IsStudio) return true;

                GameObject gob = new GameObject();
                TreeNodeObject com = gob.AddComponent<TreeNodeObject>();
                Traverse.Create(com).Field("m_TreeNodeCtrl").SetValue(__instance);
                if (_parent != null)
                    com.SetParent(_parent);
                else
                    ___m_TreeNodeObject.Add(com);

                __result = com;
                return false;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(TreeNodeCtrl), nameof(TreeNodeCtrl.DeleteNode), typeof(TreeNodeObject))]
            internal static bool TreeNodeCtrl_DeleteNode(TreeNodeCtrl __instance, TreeNodeObject _node, ref List<TreeNodeObject> ___m_TreeNodeObject, ref ScrollRect ___scrollRect)
            {
                if (IsStudio) return true;

                if (_node.enableDelete)
                {
                    _node.SetParent(null);
                    ___m_TreeNodeObject.Remove(_node);
                    _node.onDelete?.Invoke();
                    Traverse.Create(__instance).Method("DeleteNodeLoop").GetValue(_node);
                    if (___m_TreeNodeObject.Count == 0)
                        //Added null check
                        if (___scrollRect != null)
                            ___scrollRect.verticalNormalizedPosition = 1f;
                }
                return false;
            }
            [HarmonyPrefix, HarmonyPatch(typeof(TreeNodeCtrl), nameof(TreeNodeCtrl.SelectMultiple))]
            internal static bool TreeNodeCtrl_SelectMultiple() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(TreeNodeCtrl), "RefreshHierachyLoop")]
            internal static bool TreeNodeCtrl_RefreshHierachyLoop() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(TreeNodeCtrl), "RefreshVisibleLoop")]
            internal static bool TreeNodeCtrl_RefreshVisibleLoop() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(TreeNodeCtrl), "SetSelectNode")]
            internal static bool TreeNodeCtrl_SetSelectNode() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(TreeNodeCtrl), nameof(TreeNodeCtrl.OnPointerDown))]
            internal static bool TreeNodeCtrl_OnPointerDown() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(TreeNodeCtrl), "Start")]
            internal static bool TreeNodeCtrl_Start() => IsStudio;


            [HarmonyPrefix, HarmonyPatch(typeof(TreeNodeCtrl), nameof(TreeNodeCtrl.DeleteAllNode))]
            internal static bool TreeNodeCtrl_DeleteAllNode(TreeNodeCtrl __instance, ref List<TreeNodeObject> ___m_TreeNodeObject, ref ScrollRect ___scrollRect, ref HashSet<TreeNodeObject> ___hashSelectNode)
            {
                if (IsStudio) return true;

                for (int i = 0; i < ___m_TreeNodeObject.Count; i++)
                    Traverse.Create(__instance).Method("DeleteNodeLoop").GetValue(___m_TreeNodeObject[i]);
                ___m_TreeNodeObject.Clear();
                ___hashSelectNode.Clear();
                //Added null check
                if (___scrollRect != null)
                {
                    ___scrollRect.verticalNormalizedPosition = 1f;
                    ___scrollRect.horizontalNormalizedPosition = 0f;
                }
                return false;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(TreeNodeCtrl), nameof(TreeNodeCtrl.SetParent), typeof(TreeNodeObject), typeof(TreeNodeObject))]
            internal static bool TreeNodeCtrl_SetParent(TreeNodeObject _node, TreeNodeObject _parent, TreeNodeCtrl __instance, ref GUITree.TreeRoot ___m_TreeRoot, ref Action<TreeNodeObject, TreeNodeObject> ___onParentage)
            {
                if (IsStudio) return true;

                if ((_node != null) && _node.enableChangeParent && (!(bool)Traverse.Create(__instance).Method("RefreshHierachy").GetValue(_node) || !(_parent == null)) && _node.SetParent(_parent))
                {
                    Traverse.Create(__instance).Method("RefreshHierachy").GetValue();
                    //Added null check
                    ___m_TreeRoot?.Invoke("SetDirty", 0f);
                    ___onParentage?.Invoke(_parent, _node);
                }
                return false;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(TreeNodeObject), nameof(TreeNodeObject.colorSelect), MethodType.Setter)]
            internal static bool TreeNodeObject_colorSelect() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(TreeNodeObject), nameof(TreeNodeObject.textName), MethodType.Getter)]
            internal static bool TreeNodeObject_guideMove(ref string __result)
            {
                if (IsStudio) return true;

                __result = "";
                return false;
            }
            [HarmonyPrefix, HarmonyPatch(typeof(TreeNodeObject), nameof(TreeNodeObject.SetVisible))]
            internal static bool TreeNodeObject_SetVisible(TreeNodeObject __instance, bool _visible, ref bool ___m_Visible, ref List<TreeNodeObject> ___m_child)
            {
                if (IsStudio) return true;

                ___m_Visible = _visible;
                __instance?.onVisible(_visible);
                //Don't do this outside of Studio
                //imageVisible.sprite = m_SpriteVisible[_visible ? 1 : 0];
                foreach (TreeNodeObject item in ___m_child)
                    Traverse.Create(__instance).Method("SetVisibleChild").GetValue(item, _visible);
                return false;
            }
            [HarmonyPrefix, HarmonyPatch(typeof(TreeNodeObject), nameof(TreeNodeObject.ResetVisible))]
            internal static bool TreeNodeObject_ResetVisible(TreeNodeObject __instance, ref bool ___m_Visible)
            {
                if (IsStudio) return true;

                __instance?.onVisible(___m_Visible);
                //Don't do this outside of Studio
                //imageVisible.sprite = m_SpriteVisible[m_Visible ? 1 : 0];
                __instance.buttonVisible.interactable = true;
                foreach (TreeNodeObject item in __instance.child)
                    Traverse.Create(__instance).Method("SetVisibleChild").GetValue(item, ___m_Visible);
                return false;
            }
            [HarmonyPrefix, HarmonyPatch(typeof(TreeNodeObject), nameof(TreeNodeObject.textName), MethodType.Setter)]
            internal static bool TreeNodeObject_textName() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(TreeNodeObject), nameof(TreeNodeObject.enableVisible), MethodType.Setter)]
            internal static bool TreeNodeObject_enableVisible() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(TreeNodeObject), nameof(TreeNodeObject.SetTreeState))]
            internal static bool TreeNodeObject_SetTreeState() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(TreeNodeObject), nameof(TreeNodeObject.Select))]
            internal static bool TreeNodeObject_Select() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(TreeNodeObject), "SetStateVisible")]
            internal static bool TreeNodeObject_SetStateVisible() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(TreeNodeObject), "SetVisibleLoop")]
            internal static bool TreeNodeObject_SetVisibleLoop() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(TreeNodeObject), "SetVisibleChild")]
            internal static bool TreeNodeObject_SetVisibleChild() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(TreeNodeObject), "RecalcSelectButtonPos")]
            internal static bool TreeNodeObject_RecalcSelectButtonPos() => IsStudio;

            [HarmonyPrefix, HarmonyPatch(typeof(Map), nameof(Map.sunType), MethodType.Setter)]
            internal static bool Map_sunType(Map __instance, SunLightInfo.Info.Type value)
            {
                if (IsStudio) return true;

                Singleton<Studio.Studio>.Instance.sceneInfo.sunLightType = (int)value;
                __instance.sunLightInfo?.Set(value, Singleton<Studio.Studio>.Instance.cameraCtrl.mainCmaera);
                //Added null check
                Singleton<Studio.Studio>.Instance.systemButtonCtrl?.MapDependent();

                return false;
            }
            [HarmonyPrefix, HarmonyPatch(typeof(Map), nameof(Map.ReleaseMap))]
            internal static bool Map_ReleaseMap(Map __instance)
            {
                if (IsStudio) return true;

                if (Singleton<Map>.IsInstance())
                {
                    Traverse.Create(__instance).Property("mapRoot").SetValue(null);
                    Traverse.Create(__instance).Property("sunLightInfo").SetValue(null);
                    Traverse.Create(__instance).Property("no").SetValue(-1);
                    //Don't do this outside of Studio
                    //Singleton<Manager.Scene>.Instance.UnloadBaseScene();
                    Singleton<Studio.Studio>.Instance.SetSunCaster(Singleton<Studio.Studio>.Instance.sceneInfo.sunCaster);
                }

                return false;
            }


            internal static void CameraLightCtrl_LightCalc_Init(object __instance)
            {
                if (IsStudio) return;

                //Initialize some stuff
                Traverse.Create(__instance).Method("Reflect").GetValue();
                Traverse.Create(__instance).Property("isInit").SetValue(true);
            }

            internal static bool CameraLightCtrl_LightCalc_UpdateUI() => IsStudio;

            internal static bool CameraLightCtrl_LightCalc_Reflect(Light ___light)
            {
                if (IsStudio) return true;
                //Added null check
                if (___light != null) return true;
                return false;
            }
            #endregion
        }
    }
}