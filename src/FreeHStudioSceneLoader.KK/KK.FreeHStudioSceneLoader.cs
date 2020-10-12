using ActionGame;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Illusion.Elements.Xml;
using Illusion.Game;
using Studio;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UniRx;
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
        private static bool LoadingScene;
        private static string StudioSceneFile;
        private static int StudioMapNo;

        private void Awake()
        {
            Logger = base.Logger;
            IsStudio = Application.productName == "CharaStudio";

            var harmony = Harmony.CreateAndPatchAll(typeof(Hooks));
            harmony.Patch(typeof(CameraLightCtrl).GetNestedType("LightCalc", AccessTools.all).GetMethod("Init"), new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.CameraLightCtrl_LightCalc_Init), AccessTools.all)));
            harmony.Patch(typeof(CameraLightCtrl).GetNestedType("LightCalc", AccessTools.all).GetMethod("UpdateUI"), new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.CameraLightCtrl_LightCalc_UpdateUI), AccessTools.all)));
            harmony.Patch(typeof(CameraLightCtrl).GetNestedType("LightCalc", AccessTools.all).GetMethod("Reflect"), new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.CameraLightCtrl_LightCalc_Reflect), AccessTools.all)));
        }

        private static class Hooks
        {
            [HarmonyPrefix, HarmonyPatch(typeof(HSceneProc), "OnDestroy")]
            private static void HSceneProc_OnDestroy()
            {
                if (IsStudio) return;

                Singleton<Studio.Studio>.Instance.InitScene();
            }

            [HarmonyPrefix, HarmonyPatch(typeof(HSceneProc), "Start")]
            private static void HSceneProc_Start(HSceneProc __instance)
            {
                if (IsStudio) return;

                if (StudioSceneFile != null)
                    __instance.dataH.mapNoFreeH = -1;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(ActionMap), nameof(ActionMap.Change))]
            private static bool ActionMap_Change(ActionMap __instance, ref int no)
            {
                if (IsStudio) return true;
                if (StudioSceneFile == null) return true;

                Singleton<Studio.Studio>.Instance.LoadScene(StudioSceneFile);
                StudioMapNo = Singleton<Studio.Studio>.Instance.sceneInfo.map;

                //Because the map IDs in the Studio list files don't match the Map list files
                //Todo: Sideloader map support
                if (Studio.Info.Instance.dicMapLoadInfo.TryGetValue(StudioMapNo, out var mapLoadInfo))
                    foreach (var x in __instance.infoDic)
                        if (x.Value.AssetBundleName == mapLoadInfo.bundlePath && x.Value.AssetName == mapLoadInfo.fileName)
                            StudioMapNo = x.Key;

                no = StudioMapNo;
                Traverse.Create(__instance).Property("no").SetValue(StudioMapNo);
                return false;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(AssetBundleManager), nameof(AssetBundleManager.LoadAsset), typeof(string), typeof(string), typeof(Type), typeof(string))]
            private static void LoadAssetPrefix(ref string assetBundleName, ref string assetName)
            {
                //Redirect to the Studio asset bundle when loading scenes
                if (!IsStudio && LoadingScene && (assetName == "p_cf_body_bone" || assetName == "p_cf_head_bone"))
                    assetBundleName = "studio/base/00.unity3d";
            }

            [HarmonyPostfix, HarmonyPatch(typeof(Manager.Config), "Start")]
            private static void Config_Start()
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

            [HarmonyPostfix, HarmonyPatch(typeof(MapSelectMenuScene), "Start")]
            private static void MapSelectMenuScene_Start(ref IEnumerator __result, GameObject ___nodeFrame, ReactiveProperty<MapInfo.Param> ____mapInfo)
            {
                if (IsStudio) return;

                var original = __result;
                __result = new[] { original, MapSelectMenuScene_Start_Postfix(___nodeFrame, ____mapInfo) }.GetEnumerator();
            }
            private static IEnumerator MapSelectMenuScene_Start_Postfix(GameObject nodeFrame, ReactiveProperty<MapInfo.Param> _mapInfo)
            {
                StudioSceneFile = null;

                int i = 0;
                int childCount = nodeFrame.transform.childCount;
                GameObject currentFrame = null;

                foreach (FileInfo fn in from p in new DirectoryInfo(UserData.Create("studio/scene")).GetFiles("*.png")
                                        orderby p.CreationTime descending
                                        select p)
                {
                    if (i % childCount == 0)
                    {
                        currentFrame = Instantiate(nodeFrame, nodeFrame.transform.parent, false);
                        currentFrame.SetActive(true);
                    }
                    GameObject gameObject = currentFrame.transform.GetChild(i % childCount).gameObject;
                    i++;
                    gameObject.SetActive(true);
                    Button button = gameObject.GetComponent<Button>();
                    button.GetComponent<Image>().sprite = PngAssist.LoadSpriteFromFile(fn.FullName);
                    button.onClick.AddListener(() =>
                    {
                        var param = new MapInfo.Param();
                        _mapInfo.Value = param;
                        StudioSceneFile = fn.FullName;

                        //enterMapColor.Value = button;
                        Utils.Sound.Play(SystemSE.sel);
                    });
                }
                yield return null;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(BackgroundCtrl), nameof(BackgroundCtrl.isVisible), MethodType.Setter)]
            private static bool BackgroundCtrl_IsVisible(ref MeshRenderer ___meshRenderer, ref bool ___m_IsVisible, bool value)
            {
                if (IsStudio) return true;

                ___m_IsVisible = value;
                //Added null check
                if (___meshRenderer != null)
                    ___meshRenderer.enabled = value;
                return false;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(Studio.CameraControl), nameof(Studio.CameraControl.ReflectOption))]
            private static bool CameraControl_ReflectOption(Studio.CameraControl __instance, ref float ___rateAddSpeed, ref Camera ___m_SubCamera)
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
            private static bool GuideObject_guideMove(ref GuideMove[] __result)
            {
                if (IsStudio) return true;

                __result = new GuideMove[0];
                return false;
            }
            [HarmonyPrefix, HarmonyPatch(typeof(GuideObject), nameof(GuideObject.guideSelect), MethodType.Getter)]
            private static bool GuideObject_guideSelect(ref GuideSelect __result)
            {
                if (IsStudio) return true;

                __result = new GuideSelect();
                return false;
            }
            [HarmonyPrefix, HarmonyPatch(typeof(GuideObject), nameof(GuideObject.enablePos), MethodType.Setter)]
            private static bool GuideObject_enablePos() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(GuideObject), nameof(GuideObject.enableRot), MethodType.Setter)]
            private static bool GuideObject_enableRot() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(GuideObject), nameof(GuideObject.enableScale), MethodType.Setter)]
            private static bool GuideObject_enableScale() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(GuideObject), nameof(GuideObject.SetMode))]
            private static bool GuideObject_SetMode() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(GuideObject), nameof(GuideObject.SetScale))]
            private static bool GuideObject_SetScale() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(GuideObject), nameof(GuideObject.SetVisibleCenter))]
            private static bool GuideObject_SetVisibleCenter() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(GuideObject), "Awake")]
            private static bool GuideObject_Awake(GuideObject __instance, ref int ___m_DicKey)
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
            private static bool GuideObject_LateUpdate(GuideObject __instance, ref GameObject[] ___roots)
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
            private static bool OCILight_SetDrawTarget() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(MapCtrl), nameof(MapCtrl.UpdateUI))]
            private static bool MapCtrl_UpdateUI() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(MapCtrl), "Awake")]
            private static bool MapCtrl_Awake() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(PatternSelectListCtrl), nameof(PatternSelectListCtrl.Create))]
            private static bool PatternSelectListCtrl_Create() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(PatternSelectListCtrl), "Start")]
            private static bool PatternSelectListCtrl_Start() => IsStudio;

            [HarmonyPrefix, HarmonyPatch(typeof(GuideObjectManager), nameof(GuideObjectManager.DeleteAll))]
            private static bool GuideObjectManager_DeleteAll(GuideObjectManager __instance, ref HashSet<GuideObject> ___hashSelectObject, ref Dictionary<Transform, GuideObject> ___dicGuideObject, ref Dictionary<Transform, Light> ___dicTransLight, ref Dictionary<GuideObject, Light> ___dicGuideLight)
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
                if (__instance.drawLightLine != null)
                    __instance.drawLightLine.Clear();
                //Added null check
                if (__instance.guideInput != null)
                    __instance.guideInput.Stop();

                return false;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(GuideObjectManager), "Awake")]
            private static void GuideObjectManager_Awake(ref GameObject ___objectOriginal)
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
            private static bool TreeNodeCtrl_AddNode(TreeNodeCtrl __instance, TreeNodeObject _parent, ref TreeNodeObject __result, ref List<TreeNodeObject> ___m_TreeNodeObject)
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
            private static bool TreeNodeCtrl_DeleteNode(TreeNodeCtrl __instance, TreeNodeObject _node, ref List<TreeNodeObject> ___m_TreeNodeObject, ref ScrollRect ___scrollRect)
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
            private static bool TreeNodeCtrl_SelectMultiple() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(TreeNodeCtrl), "RefreshHierachyLoop")]
            private static bool TreeNodeCtrl_RefreshHierachyLoop() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(TreeNodeCtrl), "RefreshVisibleLoop")]
            private static bool TreeNodeCtrl_RefreshVisibleLoop() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(TreeNodeCtrl), "SetSelectNode")]
            private static bool TreeNodeCtrl_SetSelectNode() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(TreeNodeCtrl), nameof(TreeNodeCtrl.OnPointerDown))]
            private static bool TreeNodeCtrl_OnPointerDown() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(TreeNodeCtrl), "Start")]
            private static bool TreeNodeCtrl_Start() => IsStudio;

            [HarmonyPrefix, HarmonyPatch(typeof(TreeNodeCtrl), nameof(TreeNodeCtrl.DeleteAllNode))]
            private static bool TreeNodeCtrl_DeleteAllNode(TreeNodeCtrl __instance, ref List<TreeNodeObject> ___m_TreeNodeObject, ref ScrollRect ___scrollRect, ref HashSet<TreeNodeObject> ___hashSelectNode)
            {
                if (IsStudio) return true;

                for (int i = 0; i < ___m_TreeNodeObject.Count; i++)
                    Traverse.Create(__instance).Method("DeleteNodeLoop", ___m_TreeNodeObject[i]).GetValue();
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
            private static bool TreeNodeCtrl_SetParent(TreeNodeObject _node, TreeNodeObject _parent, TreeNodeCtrl __instance, ref GUITree.TreeRoot ___m_TreeRoot, ref Action<TreeNodeObject, TreeNodeObject> ___onParentage)
            {
                if (IsStudio) return true;

                if (_node != null && _node.enableChangeParent && (!(bool)Traverse.Create(__instance).Method("RefreshHierachy").GetValue(_node) || !(_parent == null)) && _node.SetParent(_parent))
                {
                    Traverse.Create(__instance).Method("RefreshHierachy").GetValue();
                    //Added null check
                    if (___m_TreeRoot != null)
                        ___m_TreeRoot.Invoke("SetDirty", 0f);
                    ___onParentage?.Invoke(_parent, _node);
                }
                return false;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(TreeNodeObject), nameof(TreeNodeObject.colorSelect), MethodType.Setter)]
            private static bool TreeNodeObject_colorSelect() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(TreeNodeObject), nameof(TreeNodeObject.textName), MethodType.Getter)]
            private static bool TreeNodeObject_guideMove(ref string __result)
            {
                if (IsStudio) return true;

                __result = "";
                return false;
            }
            [HarmonyPrefix, HarmonyPatch(typeof(TreeNodeObject), nameof(TreeNodeObject.SetVisible))]
            private static bool TreeNodeObject_SetVisible(TreeNodeObject __instance, bool _visible, ref bool ___m_Visible, ref List<TreeNodeObject> ___m_child)
            {
                if (IsStudio) return true;

                ___m_Visible = _visible;
                if (__instance != null)
                    __instance.onVisible(_visible);
                for (var i = 0; i < ___m_child.Count; i++)
                    Traverse.Create(__instance).Method("SetVisibleChild").GetValue(___m_child[i], _visible);
                return false;
            }
            [HarmonyPrefix, HarmonyPatch(typeof(TreeNodeObject), nameof(TreeNodeObject.ResetVisible))]
            private static bool TreeNodeObject_ResetVisible(TreeNodeObject __instance, ref bool ___m_Visible)
            {
                if (IsStudio) return true;

                if (__instance != null)
                {
                    __instance.onVisible(___m_Visible);
                    __instance.buttonVisible.interactable = true;
                }
                foreach (TreeNodeObject item in __instance.child)
                    Traverse.Create(__instance).Method("SetVisibleChild").GetValue(item, ___m_Visible);
                return false;
            }
            [HarmonyPrefix, HarmonyPatch(typeof(TreeNodeObject), nameof(TreeNodeObject.textName), MethodType.Setter)]
            private static bool TreeNodeObject_textName() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(TreeNodeObject), nameof(TreeNodeObject.enableVisible), MethodType.Setter)]
            private static bool TreeNodeObject_enableVisible() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(TreeNodeObject), nameof(TreeNodeObject.SetTreeState))]
            private static bool TreeNodeObject_SetTreeState() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(TreeNodeObject), nameof(TreeNodeObject.Select))]
            private static bool TreeNodeObject_Select() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(TreeNodeObject), "SetStateVisible")]
            private static bool TreeNodeObject_SetStateVisible() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(TreeNodeObject), "SetVisibleLoop")]
            private static bool TreeNodeObject_SetVisibleLoop() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(TreeNodeObject), "SetVisibleChild")]
            private static bool TreeNodeObject_SetVisibleChild() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(TreeNodeObject), "RecalcSelectButtonPos")]
            private static bool TreeNodeObject_RecalcSelectButtonPos() => IsStudio;

            [HarmonyPrefix, HarmonyPatch(typeof(Map), nameof(Map.sunType), MethodType.Setter)]
            private static bool Map_sunType(Map __instance, SunLightInfo.Info.Type value)
            {
                if (IsStudio) return true;

                Singleton<Studio.Studio>.Instance.sceneInfo.sunLightType = (int)value;
                if (__instance.sunLightInfo != null)
                    __instance.sunLightInfo.Set(value, Singleton<Studio.Studio>.Instance.cameraCtrl.mainCmaera);
                //Added null check
                if (Singleton<Studio.Studio>.Instance.systemButtonCtrl != null)
                    Singleton<Studio.Studio>.Instance.systemButtonCtrl.MapDependent();

                return false;
            }
            [HarmonyPrefix, HarmonyPatch(typeof(Map), nameof(Map.ReleaseMap))]
            private static bool Map_ReleaseMap(Map __instance)
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

            [HarmonyPrefix, HarmonyPatch(typeof(Studio.Studio), nameof(Studio.Studio.AddFemale))]
            private static bool Studio_AddFemale(string _path)
            {
                if (IsStudio) return true;

                AddObjectFemale.Add(_path);
                Singleton<UndoRedoManager>.Instance.Clear();

                return false;
            }
            [HarmonyPrefix, HarmonyPatch(typeof(Studio.Studio), nameof(Studio.Studio.AddMale))]
            private static bool Studio_AddMale(string _path)
            {
                if (IsStudio) return true;

                AddObjectMale.Add(_path);
                Singleton<UndoRedoManager>.Instance.Clear();

                return false;
            }
            [HarmonyPrefix, HarmonyPatch(typeof(Studio.Studio), nameof(Studio.Studio.AddItem))]
            private static bool Studio_AddItem(int _group, int _category, int _no)
            {
                if (IsStudio) return true;

                AddObjectItem.Add(_group, _category, _no);
                Singleton<UndoRedoManager>.Instance.Clear();

                return false;
            }
            [HarmonyPrefix, HarmonyPatch(typeof(Studio.Studio), nameof(Studio.Studio.AddLight), typeof(int))]
            private static bool Studio_AddLight(Studio.Studio __instance, int _no)
            {
                if (IsStudio) return true;
                if (!__instance.sceneInfo.isLightCheck) return true;

                AddObjectLight.Add(_no);
                Singleton<UndoRedoManager>.Instance.Clear();

                return false;
            }
            [HarmonyPrefix, HarmonyPatch(typeof(Studio.Studio), nameof(Studio.Studio.LoadScene))]
            private static bool Studio_LoadScene(Studio.Studio __instance, string _path, ref bool __result, ref BackgroundCtrl ___m_BackgroundCtrl, ref Studio.CameraControl ___m_CameraCtrl)
            {
                if (IsStudio) return true;

                if (!File.Exists(_path))
                {
                    __result = false;
                    return false;
                }
                __instance.InitScene(false);
                var sceneInfo = new SceneInfo();
                Traverse.Create(__instance).Property("sceneInfo").SetValue(sceneInfo);
                if (!sceneInfo.Load(_path))
                {
                    __result = false;
                    return false;
                }
                LoadingScene = true;
                AddObjectAssist.LoadChild(sceneInfo.dicObject);
                ChangeAmount source = sceneInfo.caMap.Clone();
                __instance.AddMap(sceneInfo.map, false, false);
                sceneInfo.caMap.Copy(source);
                Singleton<MapCtrl>.Instance.Reflect();
                __instance.bgmCtrl.Play(__instance.bgmCtrl.no);
                __instance.envCtrl.Play(__instance.envCtrl.no);
                __instance.outsideSoundCtrl.Play(__instance.outsideSoundCtrl.fileName);
                //Add the component if it doesn't exist
                if (___m_BackgroundCtrl == null)
                    ___m_BackgroundCtrl = Camera.main.gameObject.AddComponent<BackgroundCtrl>();
                __instance.treeNodeCtrl.RefreshHierachy();
                if (sceneInfo.cameraSaveData != null)
                    ___m_CameraCtrl.Import(sceneInfo.cameraSaveData);
                __instance.cameraLightCtrl.Reflect();
                sceneInfo.dataVersion = sceneInfo.version;
                LoadingScene = false;

                __result = true;
                return false;
            }
            [HarmonyPrefix, HarmonyPatch(typeof(Studio.Studio), nameof(Studio.Studio.InitScene))]
            private static bool Studio_InitScene(Studio.Studio __instance, bool _close, ref BackgroundCtrl ___m_BackgroundCtrl)
            {
                if (IsStudio) return true;

                __instance.ChangeCamera(null, false, true);
                Traverse.Create(__instance).Property("cameraCount").SetValue(0);
                __instance.treeNodeCtrl.DeleteAllNode();
                foreach (KeyValuePair<TreeNodeObject, ObjectCtrlInfo> item in __instance.dicInfo)
                {
                    switch (item.Value.kind)
                    {
                        case 0:
                            {
                                OCIChar oCIChar = item.Value as OCIChar;
                                oCIChar?.StopVoice();
                                break;
                            }
                        case 4:
                            {
                                OCIRoute oCIRoute = item.Value as OCIRoute;
                                oCIRoute?.DeleteLine();
                                break;
                            }
                    }
                    Destroy(item.Value.guideObject.transformTarget.gameObject);
                }
                __instance.dicInfo.Clear();
                __instance.dicChangeAmount.Clear();
                __instance.dicObjectCtrl.Clear();
                Singleton<Map>.Instance.ReleaseMap();
                __instance.cameraCtrl.CloerListCollider();
                __instance.bgmCtrl.Stop();
                __instance.envCtrl.Stop();
                __instance.outsideSoundCtrl.Stop();
                __instance.sceneInfo.Init();
                __instance.cameraCtrl.Reset(0);
                __instance.cameraLightCtrl.Reflect();
                __instance.onChangeMap?.Invoke();
                if (_close)
                {
                    Destroy(___m_BackgroundCtrl);
                    ___m_BackgroundCtrl = null;
                }
                return false;
            }
            [HarmonyPrefix, HarmonyPatch(typeof(Studio.Studio), nameof(Studio.Studio.Init))]
            private static bool Studio_Init(Studio.Studio __instance, ref InputField ____inputFieldNow, ref TMP_InputField ____inputFieldTMPNow)
            {
                if (IsStudio) return true;

                Traverse.Create(__instance).Property("sceneInfo").SetValue(new SceneInfo());
                __instance.cameraLightCtrl.Init();
                ____inputFieldNow = null;
                ____inputFieldTMPNow = null;
                TreeNodeCtrl treeNodeCtrl = __instance.treeNodeCtrl;
                treeNodeCtrl.onDelete = (Action<TreeNodeObject>)Delegate.Combine(treeNodeCtrl.onDelete, new Action<TreeNodeObject>(__instance.OnDeleteNode));
                TreeNodeCtrl treeNodeCtrl2 = __instance.treeNodeCtrl;
                treeNodeCtrl2.onParentage = (Action<TreeNodeObject, TreeNodeObject>)Delegate.Combine(treeNodeCtrl2.onParentage, new Action<TreeNodeObject, TreeNodeObject>(__instance.OnParentage));

                return false;
            }
            [HarmonyPrefix, HarmonyPatch(typeof(Studio.Studio), "Awake")]
            private static bool Studio_Awake(Studio.Studio __instance, ref Control ___xmlCtrl)
            {
                if (IsStudio) return true;

                //if (CheckInstance())
                DontDestroyOnLoad(__instance.gameObject);
                Traverse.Create(__instance).Property("optionSystem").SetValue(new OptionSystem("Option"));
                ___xmlCtrl = new Control("studio", "option.xml", "Option", Studio.Studio.optionSystem);
                __instance.LoadOption();

                return false;
            }
            [HarmonyPrefix, HarmonyPatch(typeof(Studio.Studio), nameof(Studio.Studio.ChangeCamera), typeof(OCICamera), typeof(bool), typeof(bool))]
            private static bool Studio_ChangeCamera() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(Studio.Studio), nameof(Studio.Studio.SetSunCaster))]
            private static bool Studio_SetSunCaster() => IsStudio;
            [HarmonyPrefix, HarmonyPatch(typeof(Studio.Studio), "OnApplicationQuit")]
            private static bool Studio_OnApplicationQuit() => IsStudio;

            [HarmonyPrefix, HarmonyPatch(typeof(FreeHScene), "SetMapSprite")]
            private static bool FreeHScene_SetMapSprite(ref Image ___mapImageNormal, ref Image ___mapImageMasturbation, ref Image ___mapImageLesbian)
            {
                if (IsStudio) return true;
                if (StudioSceneFile == null) return true;

                ___mapImageNormal.sprite = PngAssist.LoadSpriteFromFile(StudioSceneFile);
                ___mapImageMasturbation.sprite = PngAssist.LoadSpriteFromFile(StudioSceneFile);
                ___mapImageLesbian.sprite = PngAssist.LoadSpriteFromFile(StudioSceneFile);
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
        }
    }
}