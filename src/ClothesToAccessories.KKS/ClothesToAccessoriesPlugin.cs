using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Logging;
using ChaCustom;
using HarmonyLib;
using Illusion.Extensions;
using Illusion.Game.Array;
using KKAPI.Maker;
using KKAPI.Utilities;
using Manager;
using StrayTech;
using TMPro;
using UniRx;
using UnityEngine;
using Character = Manager.Character;

namespace ClothesToAccessories
{
    //todo try catch all custom code with proper errors
    [BepInPlugin(GUID, PluginName, Version)]
    public class ClothesToAccessoriesPlugin : BaseUnityPlugin
    {
        public const string GUID = "ClothesToAccessories";
        public const string PluginName = "Clothes To Accessories";
        public const string Version = "0.1";

        internal static new ManualLogSource Logger;

        internal static List<IDisposable> CleanupList = new List<IDisposable>();

        private void Start()
        {
            Logger = base.Logger;

            var hi = new Harmony(Guid.NewGuid().ToString());
            CleanupList.Add(Disposable.Create(() => hi.UnpatchSelf()));

            hi.PatchAll(typeof(ClothesToAccessoriesPlugin));

            hi.PatchMoveNext(
                original: AccessTools.Method(typeof(ChaControl), nameof(ChaControl.ChangeAccessoryAsync), new[] { typeof(int), typeof(int), typeof(int), typeof(string), typeof(bool), typeof(bool) }),
                transpiler: new HarmonyMethod(typeof(ClothesToAccessoriesPlugin), nameof(UnlockAccessoryItemTypesTpl)));

            hi.Patch(
                original: typeof(CvsAccessory).GetMethods(AccessTools.allDeclared).Single(mi =>
                {
                    var p = mi.GetParameters();
                    return p.Length == 1 &&
                           p[0].ParameterType == typeof(int) &&
                           mi.Name.StartsWith("<Start>");
                }),
                prefix: new HarmonyMethod(typeof(ClothesToAccessoriesPlugin), nameof(ClothesToAccessoriesPlugin.ConvertDropdownIndexToRelativeTypeIndex)));
            MakeWindows();
        }

        private void OnDestroy()
        {
            foreach (var disposable in CleanupList)
            {
                try { disposable.Dispose(); }
                catch (Exception ex) { Console.WriteLine(ex); }
            }
        }

        #region add clothes to acc categories

        private static readonly ChaListDefine.CategoryNo[] _AcceptableCustomTypes =
        {
            ChaListDefine.CategoryNo.bo_hair_b,
            ChaListDefine.CategoryNo.bo_hair_f,
            ChaListDefine.CategoryNo.bo_hair_s,
            ChaListDefine.CategoryNo.bo_hair_o,
            ChaListDefine.CategoryNo.co_top,
            ChaListDefine.CategoryNo.co_bot,
            ChaListDefine.CategoryNo.co_bra,
            ChaListDefine.CategoryNo.co_shorts,
            ChaListDefine.CategoryNo.co_gloves,
            ChaListDefine.CategoryNo.co_panst,
            ChaListDefine.CategoryNo.co_socks,
            ChaListDefine.CategoryNo.co_shoes,
            ChaListDefine.CategoryNo.cpo_sailor_a,
            ChaListDefine.CategoryNo.cpo_sailor_b,
            ChaListDefine.CategoryNo.cpo_sailor_c,
            ChaListDefine.CategoryNo.cpo_jacket_a,
            ChaListDefine.CategoryNo.cpo_jacket_b,
            ChaListDefine.CategoryNo.cpo_jacket_c,
        };

        // list id equals dropdown id, stock list items are marked as ao_none
        private static List<ChaListDefine.CategoryNo> _typeIndexLookup;


        private void MakeWindows()
        {
            var sw = Stopwatch.StartNew();

            var slot = GameObject.FindObjectOfType<CustomAcsChangeSlot>();
            var donor = slot.customAcsSelectKind.First().gameObject;

            //cleanup
            var orig = slot.customAcsSelectKind;
            var cac = slot.cvsAccessory.First();
            var orig2 = cac.cgAccessoryWin;
            var orig3 = cac.customAccessory;
            CleanupList.Add(Disposable.Create(() =>
            {
                slot.customAcsSelectKind = orig;
                foreach (var cvsAccessory in slot.cvsAccessory)
                {
                    cvsAccessory.ddAcsType.options.RemoveAll(data => _AcceptableCustomTypes.Any(a => a.ToString() == data.text));
                    if (orig2 != null) cvsAccessory.cgAccessoryWin = orig2;
                    if (orig3 != null) cvsAccessory.customAccessory = orig3;
                }
            }));

            _typeIndexLookup = Enumerable.Repeat(ChaListDefine.CategoryNo.ao_none, cac.ddAcsType.options.Count).ToList();

            foreach (var typeToAdd in _AcceptableCustomTypes)
            {
                var copy = GameObject.Instantiate(donor, donor.transform.parent, false);
                copy.name = $"winAcsCustomKind_{typeToAdd}";

                //cleanup
                CleanupList.Add(Disposable.Create(() => GameObject.Destroy(copy)));

                var copyCmp = copy.GetComponent<CustomAcsSelectKind>();
                copyCmp.cate = typeToAdd;
                copyCmp.listCtrl.ClearList();
                copyCmp.Initialize();
                copyCmp.CloseWindow();
                copyCmp.enabled = true;

                // Remove the None items. For top, also remove the sailor and jacket tops since without the sub parts they don't actually exist
                if (typeToAdd == ChaListDefine.CategoryNo.co_top)
                    copyCmp.listCtrl.lstSelectInfo.RemoveAll(info => info.index == 0 || info.index == 1 || info.index == 2);
                else if (typeToAdd != ChaListDefine.CategoryNo.cpo_sailor_a && typeToAdd != ChaListDefine.CategoryNo.cpo_sailor_b && typeToAdd != ChaListDefine.CategoryNo.cpo_jacket_a)
                    copyCmp.listCtrl.lstSelectInfo.RemoveAll(info => info.index == 0);

                copyCmp.selWin.textTitle.text = $"Accessory ({copyCmp.cate})";

                slot.customAcsSelectKind = slot.customAcsSelectKind.AddToArray(copyCmp);

                foreach (var cvsAccessory in slot.cvsAccessory)
                {
                    cvsAccessory.ddAcsType.options.Add(new TMP_Dropdown.OptionData(copyCmp.cate.ToString()));
                    cvsAccessory.cgAccessoryWin = cvsAccessory.cgAccessoryWin.AddToArray(copyCmp.GetComponent<CanvasGroup>());
                    cvsAccessory.customAccessory = cvsAccessory.customAccessory.AddToArray(copyCmp);
                }

                _typeIndexLookup.Add(typeToAdd);
            }

            foreach (var cvsAccessory in slot.cvsAccessory)
            {
                var template = cvsAccessory.ddAcsType.template;
                var origSize = template.sizeDelta;
                CleanupList.Add(Disposable.Create(() => template.sizeDelta = origSize));
                template.sizeDelta = new Vector2(origSize.x, origSize.y + 480);
            }

            Console.WriteLine("MakeWindows finish in ms " + sw.ElapsedMilliseconds);
        }

        private static void ConvertDropdownIndexToRelativeTypeIndex(ref int idx)
        {
            if (idx == 0 || _typeIndexLookup == null) return;
            if (idx < 0 || idx >= _typeIndexLookup.Count)
            {
                Console.WriteLine($"oops idx {idx}\n{new StackTrace()}");
                idx = 0;
                return;
            }

            var customType = _typeIndexLookup[idx];
            // Handle default categories
            if (customType == ChaListDefine.CategoryNo.ao_none) return;

            // The dropdown index idx is later added 120 (ao_none) to convert it to CategoryNo, so 0 + 120 = CategoryNo.ao_none
            // Custom categories added by this plugin are added to the dropdown in sequendial indexes, but that does not work out when converting them by simply adding 120
            // instead their list index has to be adjusted so that when idx + 120 the result is equal to the correct CategoryNo
            var newIndex = customType - ChaListDefine.CategoryNo.ao_none;
            Console.WriteLine($"adjust {idx} into {newIndex}");
            idx = newIndex;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(CvsAccessory), nameof(CvsAccessory.UpdateCustomUI))]
        private static IEnumerable<CodeInstruction> ConvertAccTypeTpl(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .MatchForward(true,
                    new CodeMatch(OpCodes.Ldloc_1),
                    new CodeMatch(OpCodes.Callvirt, AccessTools.PropertySetter(typeof(TMP_Dropdown), nameof(TMP_Dropdown.value))))
                .ThrowIfInvalid("set_value not found")
                .Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ClothesToAccessoriesPlugin), nameof(ClothesToAccessoriesPlugin.ConvertTypeToDropdownIndex)) ?? throw new Exception("ConvertTypeToDropdownIndex")))
                .Instructions();
        }

        private static int ConvertTypeToDropdownIndex(int accTypeSubtracted)
        {
            if (_typeIndexLookup == null) return accTypeSubtracted;

            // accTypeSubtracted is a CategoryNo - 120
            var accType = accTypeSubtracted + 120;
            var customAccTypeIndex = _typeIndexLookup.IndexOf((ChaListDefine.CategoryNo)accType);
            return customAccTypeIndex >= 0 ? customAccTypeIndex : accTypeSubtracted;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(CvsAccessory), nameof(CvsAccessory.UpdateSelectAccessoryType))]
        private static IEnumerable<CodeInstruction> UnlockTypesTpl(IEnumerable<CodeInstruction> instructions)
        {
            var replacement = AccessTools.Method(typeof(ClothesToAccessoriesPlugin), nameof(ClothesToAccessoriesPlugin.CustomGetDefault)) ?? throw new Exception("CustomGetDefault");
            return new CodeMatcher(instructions)
                .MatchForward(true,
                    new CodeMatch(OpCodes.Ldfld),
                    new CodeMatch(OpCodes.Ldarg_1),
                    new CodeMatch(OpCodes.Ldelem_I4))
                .Repeat(matcher => matcher.Set(OpCodes.Call, replacement), s => throw new Exception("match fail - " + s))
                .Instructions();
        }

        private static int CustomGetDefault(int[] defaultAcsId, int index)
        {
            // index is type - 120
            if (index >= 0 && index < defaultAcsId.Length) return defaultAcsId[index];

            var isTop = index + 120 == (int)ChaListDefine.CategoryNo.co_top;
            return isTop ? 3 : 1;
        }

        #endregion

        #region allow loading clothes as accs

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.GetAccessoryDefaultParentStr))]
        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeAccessoryParent))]
        private static IEnumerable<CodeInstruction> GetAccessoryParentOverrideTpl(IEnumerable<CodeInstruction> instructions)
        {
            // Needed to
            return new CodeMatcher(instructions)
                .MatchForward(true,
                    new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)0x36),
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(ListInfoBase), nameof(ListInfoBase.GetInfo))))
                .ThrowIfInvalid("GetInfo not found")
                .Set(OpCodes.Call, AccessTools.Method(typeof(ClothesToAccessoriesPlugin), nameof(GetParentOverride)) ?? throw new Exception("GetParentOverride"))
                .Instructions();
        }

        [HarmonyTranspiler]
        //todo needs new harmony ver [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeAccessoryAsync), typeof(int), typeof(int), typeof(int), typeof(string), typeof(bool), typeof(bool))]
        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeAccessoryNoAsync))]
        private static IEnumerable<CodeInstruction> UnlockAccessoryItemTypesTpl(IEnumerable<CodeInstruction> instructions/*, MethodInfo __originalMethod*/)
        {

            var cm = new CodeMatcher(instructions);

            var replacement1 = AccessTools.Method(typeof(ClothesToAccessoriesPlugin), nameof(CustomAccessoryTypeCheck)) ?? throw new Exception("CustomAccessoryTypeCheck");
            cm.MatchForward(true,
                    // Filter by the max value. Only replace the two necessary calls to avoid interfering with moreaccs patches
                    new CodeMatch(ins => ins.opcode == OpCodes.Ldc_I4 && ((int)ins.operand == 129 || (int)ins.operand == 130)),
                    new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(MathfEx), nameof(MathfEx.RangeEqualOn))?.MakeGenericMethod(typeof(int)) ?? throw new Exception("RangeEqualOn")))
                .Repeat(matcher => matcher.Operand = replacement1, s => throw new InvalidOperationException("Replacement failed - " + s));

            cm.End()
               .MatchBack(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(ChaControl), nameof(ChaControl.LoadCharaFbxData)) ?? throw new Exception("LoadCharaFbxData")))
               .ThrowIfInvalid("LoadCharaFbxData not found").Set(OpCodes.Call, AccessTools.Method(typeof(ClothesToAccessoriesPlugin), nameof(LoadCharaFbxDataLeech)) ?? throw new Exception("LoadCharaFbxDataLeech"));

            //if (!__originalMethod.Name.EndsWith("NoAsync"))
            {
                cm.MatchBack(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(ChaControl), nameof(ChaControl.LoadCharaFbxDataAsync)) ?? throw new Exception("LoadCharaFbxDataAsync")));
                //.ThrowIfInvalid("LoadCharaFbxDataAsync not found")
                if (cm.IsValid) cm
                 .Set(OpCodes.Call, AccessTools.Method(typeof(ClothesToAccessoriesPlugin), nameof(LoadCharaFbxDataAsyncLeech)) ?? throw new Exception("LoadCharaFbxDataAsyncLeech"));
            }

            cm.End()
                .MatchBack(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(ChaControl), nameof(ChaControl.UpdateAccessoryMoveFromInfo)) ?? throw new Exception("UpdateAccessoryMoveFromInfo")))
                .ThrowIfInvalid("UpdateAccessoryMoveFromInfo not found")
                .Set(OpCodes.Call, AccessTools.Method(typeof(ClothesToAccessoriesPlugin), nameof(UpdateAccessoryMoveFromInfoAndStuff)));

            var replacement2 = AccessTools.Method(typeof(ClothesToAccessoriesPlugin), nameof(GetParentOverride)) ?? throw new Exception("GetParentOverride");
            cm.Start()
                .MatchForward(true,
                    new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)0x36),
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(ListInfoBase), nameof(ListInfoBase.GetInfo))))
                .Repeat(matcher => matcher.Set(OpCodes.Call, replacement2), s => throw new Exception("GetInfo not found - " + s));

            return cm.Instructions();
        }

        private static string GetParentOverride(ListInfoBase instance, ChaListDefine.KeyType type)
        {
            if (type == ChaListDefine.KeyType.Parent)
            {
                var category = (ChaListDefine.CategoryNo)instance.Category;
                if (category != ChaListDefine.CategoryNo.ao_none && _AcceptableCustomTypes.Contains(category))
                {
                    switch (category)
                    {
                        case ChaListDefine.CategoryNo.bo_hair_b:
                        //break;
                        case ChaListDefine.CategoryNo.bo_hair_f:
                        //break;
                        case ChaListDefine.CategoryNo.bo_hair_s:
                            Console.WriteLine("GetParentOverride 1");
                            // headside is center of head, all normal hair seem to be using this
                            return ChaAccessoryDefine.AccessoryParentKey.a_n_headside.ToString();
                        case ChaListDefine.CategoryNo.bo_hair_o:
                            Console.WriteLine("GetParentOverride 2");
                            // Ahoges act like hats
                            return ChaAccessoryDefine.AccessoryParentKey.a_n_headtop.ToString();

                        case ChaListDefine.CategoryNo.co_top:
                        case ChaListDefine.CategoryNo.co_bot:
                        case ChaListDefine.CategoryNo.co_bra:
                        case ChaListDefine.CategoryNo.co_shorts:
                        case ChaListDefine.CategoryNo.co_gloves:
                        case ChaListDefine.CategoryNo.co_panst:
                        case ChaListDefine.CategoryNo.co_socks:
                        case ChaListDefine.CategoryNo.co_shoes:
                        case ChaListDefine.CategoryNo.cpo_sailor_a:
                        case ChaListDefine.CategoryNo.cpo_sailor_b:
                        case ChaListDefine.CategoryNo.cpo_sailor_c:
                        case ChaListDefine.CategoryNo.cpo_jacket_a:
                        case ChaListDefine.CategoryNo.cpo_jacket_b:
                        case ChaListDefine.CategoryNo.cpo_jacket_c:
                            Console.WriteLine("GetParentOverride 3");
                            // this key doesn't actually matter since the bones get merged, but something is needed or the accessory won't be spawned properly
                            return ChaAccessoryDefine.AccessoryParentKey.a_n_waist.ToString();
                    }
                }
                Console.WriteLine($"GetParentOverride unhandled cat: {category}  value: {instance.GetInfo(type)}");
            }

            var origInfo = instance.GetInfo(type);
            return origInfo;
        }

        private static bool CustomAccessoryTypeCheck(int min, int n, int max)
        {
            if (min != 121 || n >= min && n <= max) return MathfEx.RangeEqualOn(min, n, max);

            return _AcceptableCustomTypes.Contains((ChaListDefine.CategoryNo)n);
        }

        private static GameObject LoadCharaFbxDataLeech(ChaControl instance, Action<ListInfoBase> actListInfo, bool _hiPoly, int category, int id,
            string createName, bool copyDynamicBone, byte copyWeights, Transform trfParent, int defaultId, bool worldPositionStays)
        {
            ListInfoBase spawnedLib = null;
            var newActList = new Action<ListInfoBase>(lib => spawnedLib = lib);
            var result = instance.LoadCharaFbxData(actListInfo + newActList, _hiPoly, category, id, createName, copyDynamicBone, copyWeights, trfParent, defaultId, worldPositionStays);
            CovertToChaAccessoryComponent(result, spawnedLib, instance, (ChaListDefine.CategoryNo)category);
            return result;
        }

        private static IEnumerator LoadCharaFbxDataAsyncLeech(ChaControl instance, Action<GameObject> actObj, Action<ListInfoBase> actListInfo, bool _hiPoly, int category,
            int id, string createName, bool copyDynamicBone, byte copyWeights, Transform trfParent, int defaultId, bool AsyncFlags = true, bool worldPositionStays = false)
        {
            GameObject spawnedObject = null;
            var newActObj = new Action<GameObject>(o => spawnedObject = o);
            ListInfoBase spawnedLib = null;
            var newActList = new Action<ListInfoBase>(lib => spawnedLib = lib);
            var result = instance.LoadCharaFbxDataAsync(actObj + newActObj, actListInfo + newActList, _hiPoly, category, id, createName, copyDynamicBone, copyWeights, trfParent, defaultId, AsyncFlags, worldPositionStays);
            return result.AppendCo(() => CovertToChaAccessoryComponent(spawnedObject, spawnedLib, instance, (ChaListDefine.CategoryNo)category));
        }

        private static void CovertToChaAccessoryComponent(GameObject instance, ListInfoBase listInfoBase, ChaControl chaControl, ChaListDefine.CategoryNo category)
        {
            Console.WriteLine("CovertToChaAccessoryComponent");
            var cac = instance.GetComponent<ChaAccessoryComponent>();
            if (cac != null) return;

            var ccc = instance.GetComponent<ChaClothesComponent>();
            if (ccc != null)
            {
                Console.WriteLine("CovertToChaAccessoryComponent cloth");
                cac = instance.AddComponent<ChaAccessoryComponent>();
                cac.defColor01 = ccc.defMainColor01;
                cac.defColor02 = ccc.defMainColor02;
                cac.defColor03 = ccc.defMainColor03;
                cac.defColor04 = ccc.defAccessoryColor;
                cac.useColor01 = ccc.useColorN01;
                cac.useColor02 = ccc.useColorN02;
                cac.useColor03 = ccc.useColorN03;

                cac.initialize = ccc.initialize;
                cac.setcolor = ccc.setcolor;

                cac.rendAlpha = ccc.rendAlpha01.Concat(ccc.rendAlpha02).Where(x => x != null).ToArray();
                cac.rendNormal = ccc.rendNormal01.Concat(ccc.rendNormal02).Concat(ccc.rendNormal03).AddItem(ccc.rendAccessory).Where(x => x != null).ToArray();
                cac.rendHair = Array.Empty<Renderer>();

                var adapter = instance.AddComponent<ClothesToAccessoriesAdapter>();
                adapter.Initialize(chaControl, ccc, cac, listInfoBase, category);

                return;
            }

            var chc = instance.GetComponent<ChaCustomHairComponent>();
            if (chc != null)
            {
                Console.WriteLine("CovertToChaAccessoryComponent hair");
                cac = instance.AddComponent<ChaAccessoryComponent>();

                cac.initialize = chc.initialize;
                cac.setcolor = chc.setcolor;
                cac.rendHair = Array.Empty<Renderer>();

                // Hair renderers need to be set as normal to work properly. rendHair on accessories doesn't apply color properly
                cac.rendNormal = chc.rendHair;
                cac.useColor01 = true;
                cac.useColor02 = true;
                cac.useColor03 = true;

                var currentHair = chaControl.fileHair.parts[0];
                cac.defColor01 = currentHair.baseColor;
                cac.defColor02 = currentHair.startColor;
                cac.defColor03 = currentHair.endColor;

                // Presence of any renderers inside rendAlpha enables defColor04. The color picker will let user change alpha on 04
                cac.rendAlpha = chc.rendAccessory;
                if (chc.acsDefColor.Length > 0) cac.defColor04 = chc.acsDefColor[0];

                // Make the accessory move gizmos work
                // Find the top of the bone tree used by the renderers instead of just using rootBone to avoid scaling and moving any bones directly
                // In theory there should only ever be 1, but since there can be 2 move gizmos this can handle 2 unique parents as well
                // This is not needed for clothes since they are glued to the body bones so they can't be moved anyways
                var positions = chc.rendHair.Concat(chc.rendAccessory).OfType<SkinnedMeshRenderer>().Select(x => x.rootBone).Distinct().Select(tr =>
                {
                    var parent = tr.parent;
                    while (parent && parent != instance.transform)
                    {
                        tr = parent;
                        parent = tr.parent;
                    }
                    return tr;
                }).Distinct().ToArray();
                if (positions.Length >= 1) positions[0].name = "N_move";
                if (positions.Length >= 2) positions[1].name = "N_move2";
                if (positions.Length >= 3) Console.WriteLine("more than 2 move transforms found! " + string.Join(", ", positions.Select(x => x.name)));

                return;
            }

            Console.WriteLine("CovertToChaAccessoryComponent failed");
        }

        private static bool UpdateAccessoryMoveFromInfoAndStuff(ChaControl instance, int slotNo)
        {
            var result = instance.UpdateAccessoryMoveFromInfo(slotNo);

            try
            {
                var accObj = instance.objAccessory[slotNo];

                if (accObj.GetComponent<ChaClothesComponent>())
                {
                    var chaAccessory = accObj.GetComponent<ChaAccessoryComponent>();

                    // Make dynamic bones on the instantiated clothing operate on the main body bones instead (since the renderers will be using these instead)
                    var componentsInChildren = accObj.GetComponentsInChildren<DynamicBone>(true);
                    if (!componentsInChildren.IsNullOrEmpty<DynamicBone>())
                    {
                        var componentsInChildren2 = instance.objBodyBone.GetComponentsInChildren<DynamicBoneCollider>(true);
                        var dictBone = instance.aaWeightsBody.dictBone;
                        foreach (var dynamicBone in componentsInChildren)
                        {
                            if (dynamicBone.m_Root)
                            {
                                foreach (var keyValuePair in dictBone)
                                {
                                    if (keyValuePair.Key == dynamicBone.m_Root.name)
                                    {
                                        dynamicBone.m_Root = keyValuePair.Value.transform;
                                        break;
                                    }
                                }
                            }

                            if (dynamicBone.m_Exclusions != null && dynamicBone.m_Exclusions.Count != 0)
                            {
                                for (var j = 0; j < dynamicBone.m_Exclusions.Count; j++)
                                {
                                    if (null != dynamicBone.m_Exclusions[j])
                                    {
                                        foreach (var keyValuePair2 in dictBone)
                                        {
                                            if (keyValuePair2.Key == dynamicBone.m_Exclusions[j].name)
                                            {
                                                dynamicBone.m_Exclusions[j] = keyValuePair2.Value.transform;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }

                            if (dynamicBone.m_notRolls != null && dynamicBone.m_notRolls.Count != 0)
                            {
                                for (var k = 0; k < dynamicBone.m_notRolls.Count; k++)
                                {
                                    if (null != dynamicBone.m_notRolls[k])
                                    {
                                        foreach (var keyValuePair3 in dictBone)
                                        {
                                            if (keyValuePair3.Key == dynamicBone.m_notRolls[k].name)
                                            {
                                                dynamicBone.m_notRolls[k] = keyValuePair3.Value.transform;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }

                            if (dynamicBone.m_Colliders != null)
                            {
                                dynamicBone.m_Colliders.Clear();
                                for (var l = 0; l < componentsInChildren2.Length; l++)
                                {
                                    dynamicBone.m_Colliders.Add(componentsInChildren2[l]);
                                }
                            }
                        }
                    }

                    //AssignedWeightsAndSetBounds replaces the bones of an object with the body bones
                    var objRootBone = instance.GetReferenceInfo(ChaReference.RefObjKey.A_ROOTBONE);
                    foreach (var rend in chaAccessory.rendNormal.Concat(chaAccessory.rendAlpha).Concat(chaAccessory.rendHair))
                    {
                        if (rend)
                            instance.aaWeightsBody.AssignedWeightsAndSetBounds(rend.gameObject, "cf_j_root", instance.bounds, objRootBone.transform);
                    }
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }

            return result;
        }

        #endregion

        #region handle clothes state and texture

        private static byte _lastTopState;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.UpdateVisible))]
        private static void Koroshitekure(ChaControl __instance)
        {
            ClothesToAccessoriesAdapter.AllInstances.TryGetValue(__instance, out var accClothes);
            if (accClothes == null) return;

            bool SetActiveControls(List<ClothesToAccessoriesAdapter> targets, ChaReference.RefObjKey key, bool enable)
            {
                var result = false;
                foreach (var target in targets)
                {
                    if (target == null) continue;
                    if (key == 0)
                    {
                        foreach (var go in target.AllObjects)
                        {
                            if (go != null && go.activeSelf != enable)
                            {
                                go.SetActive(enable);
                                result = true;
                            }
                        }
                    }
                    else
                    {
                        var go = target.Reference.GetReferenceInfo(key);
                        if (go != null && go.activeSelf != enable)
                        {
                            go.SetActive(enable);
                            result = true;
                        }
                    }
                }
                return result;
            }

            void DrawOption(List<ClothesToAccessoriesAdapter> targets, ChaFileDefine.ClothesKind kind)
            {
                var flag = !__instance.nowCoordinate.clothes.parts[(int)kind].hideOpt[0];
                var flag2 = !__instance.nowCoordinate.clothes.parts[(int)kind].hideOpt[1];
                if (ChaFileDefine.ClothesKind.bra == kind)
                {
                    flag = !__instance.nowCoordinate.clothes.hideBraOpt[0];
                    flag2 = !__instance.nowCoordinate.clothes.hideBraOpt[1];
                }
                if (ChaFileDefine.ClothesKind.shorts == kind)
                {
                    flag = !__instance.nowCoordinate.clothes.hideShortsOpt[0];
                    flag2 = !__instance.nowCoordinate.clothes.hideShortsOpt[1];
                }

                foreach (var target in targets)
                {
                    if (target == null) continue;

                    var chaClothesComponent = target.ClothesComponent;
                    if (chaClothesComponent.objOpt01 != null)
                    {
                        var array = chaClothesComponent.objOpt01;
                        for (var i = 0; i < array.Length; i++)
                        {
                            YS_Assist.SetActiveControl(array[i], flag);
                        }
                    }
                    if (chaClothesComponent.objOpt02 != null)
                    {
                        var array = chaClothesComponent.objOpt02;
                        for (var i = 0; i < array.Length; i++)
                        {
                            YS_Assist.SetActiveControl(array[i], flag2);
                        }
                    }
                }
            }

            var visibleSon = true;
            var visibleBody = true;
            var visibleSimple = false;
            if (Scene.NowSceneNames.Any((string s) => s == "H"))
            {
                visibleSon = __instance.sex != 0 || Manager.Config.HData.VisibleSon;
                visibleBody = __instance.sex != 0 || Manager.Config.HData.VisibleBody;
                visibleSimple = __instance.sex == 0 && __instance.fileStatus.visibleSimple;
            }
            var flags = default(Bool8);
            flags[0] = __instance.visibleAll;
            flags[1] = visibleBody;
            flags[2] = !visibleSimple;
            var topClothesState = __instance.fileStatus.clothesState[0];
            flags[3] = topClothesState != 3;
            flags[4] = __instance.fileStatus.visibleBodyAlways;
            var anyTopVisible = flags.Any(5);

            var flag7 = topClothesState == 0 && anyTopVisible;
            if (SetActiveControls(accClothes[0], ChaReference.RefObjKey.S_CTOP_T_DEF, flag7)) __instance.updateShape = true;

            var flag8 = topClothesState == 1 && anyTopVisible;
            if (SetActiveControls(accClothes[0], ChaReference.RefObjKey.S_CTOP_T_NUGE, flag8)) __instance.updateShape = true;

            flags[0] = __instance.fileStatus.clothesState[1] == 0 && anyTopVisible;
            flags[1] = topClothesState != 3;
            if (SetActiveControls(accClothes[0], ChaReference.RefObjKey.S_CTOP_B_DEF, flags.Any(2))) __instance.updateShape = true;

            flags[0] = __instance.fileStatus.clothesState[1] > 0 && anyTopVisible;
            flags[1] = topClothesState != 3;
            if (SetActiveControls(accClothes[0], ChaReference.RefObjKey.S_CTOP_B_NUGE, flags.Any(2))) __instance.updateShape = true;

            DrawOption(accClothes[0], ChaFileDefine.ClothesKind.top);

            //todo sleeves __instance.DrawSode();
            
            flags[0] = __instance.visibleAll;
            flags[1] = !__instance.notBot;
            flags[2] = visibleBody;
            flags[3] = !visibleSimple;
            flags[4] = 3 != __instance.fileStatus.clothesState[1];
            flags[5] = __instance.fileStatus.visibleBodyAlways;
            var anyBotVisible = flags.Any(6);

            var flag9 = __instance.fileStatus.clothesState[1] == 0 && anyBotVisible;
            if (SetActiveControls(accClothes[1], ChaReference.RefObjKey.S_CBOT_B_DEF, flag9)) __instance.updateShape = true;

            var flag10 = __instance.fileStatus.clothesState[1] == 1 && anyBotVisible;
            if (SetActiveControls(accClothes[1], ChaReference.RefObjKey.S_CBOT_B_NUGE, flag10)) __instance.updateShape = true;

            flags[0] = topClothesState == 0 && anyBotVisible;
            flags[1] = __instance.fileStatus.clothesState[1] != 2;
            if (SetActiveControls(accClothes[1], ChaReference.RefObjKey.S_CBOT_T_DEF, flags.Any(2))) __instance.updateShape = true;

            flags[0] = topClothesState > 0 && anyBotVisible;
            flags[1] = __instance.fileStatus.clothesState[1] != 2;
            if (SetActiveControls(accClothes[1], ChaReference.RefObjKey.S_CBOT_T_NUGE, flags.Any(2))) __instance.updateShape = true;

            DrawOption(accClothes[1], ChaFileDefine.ClothesKind.bot);

            flags[0] = __instance.visibleAll;
            flags[1] = visibleBody;
            flags[2] = !visibleSimple;
            flags[3] = !__instance.notBra;
            flags[4] = 3 != __instance.fileStatus.clothesState[2];
            flags[5] = __instance.fileStatus.visibleBodyAlways;
            var anyBraVisible = flags.Any(6);

            var flag11 = __instance.fileStatus.clothesState[2] == 0 && anyBraVisible;
            if (SetActiveControls(accClothes[2], ChaReference.RefObjKey.S_UWT_T_DEF, flag11)) __instance.updateShape = true;

            var flag12 = 1 == __instance.fileStatus.clothesState[2] && anyBraVisible;
            if (SetActiveControls(accClothes[2], ChaReference.RefObjKey.S_UWT_T_NUGE, flag12)) __instance.updateShape = true;

            flags[0] = __instance.fileStatus.clothesState[3] == 0 && anyBraVisible;
            flags[1] = 3 != __instance.fileStatus.clothesState[2];
            if (SetActiveControls(accClothes[2], ChaReference.RefObjKey.S_UWT_B_DEF, flags.Any(2))) __instance.updateShape = true;

            flags[0] = __instance.fileStatus.clothesState[3] > 0 && anyBraVisible;
            flags[1] = 3 != __instance.fileStatus.clothesState[2];
            if (SetActiveControls(accClothes[2], ChaReference.RefObjKey.S_UWT_B_NUGE, flags.Any(2))) __instance.updateShape = true;

            DrawOption(accClothes[2], ChaFileDefine.ClothesKind.bra);

            //todo unnecessary?
            //if (__instance.objHideNip != null)
            //    if (__instance.notBra && __instance.visibleAll && !visibleSimple && __instance.fileStatus.visibleBodyAlways && __instance.fileStatus.clothesState[0] != 0)
            //        __instance.objHideNip.SetActiveIfDifferent(true);

            flags[0] = __instance.visibleAll;
            flags[1] = !(__instance.hideShortsBot && __instance.objClothes[1] && __instance.objClothes[1].activeSelf && __instance.fileStatus.clothesState[1] == 0);
            flags[2] = !__instance.notShorts;
            flags[3] = visibleBody;
            flags[4] = !visibleSimple;
            flags[5] = 3 != __instance.fileStatus.clothesState[3];
            flags[6] = __instance.fileStatus.visibleBodyAlways;
            var anyPantieVisible = flags.Any(7);

            var flag13 = __instance.fileStatus.clothesState[3] == 0 && anyPantieVisible;
            if (SetActiveControls(accClothes[3], ChaReference.RefObjKey.S_UWB_B_DEF, flag13)) __instance.updateShape = true;

            var flag14 = 1 == __instance.fileStatus.clothesState[3] && anyPantieVisible;
            if (SetActiveControls(accClothes[3], ChaReference.RefObjKey.S_UWB_B_NUGE, flag14)) __instance.updateShape = true;

            var flag15 = 2 == __instance.fileStatus.clothesState[3] && anyPantieVisible;
            if (SetActiveControls(accClothes[3], ChaReference.RefObjKey.S_UWB_B_NUGE2, flag15)) __instance.updateShape = true;

            DrawOption(accClothes[3], ChaFileDefine.ClothesKind.shorts);

            //todo unnecessary?
            //if (__instance.objHideKokan != null)
            //    if (__instance.notShorts && __instance.notBra && __instance.visibleAll && !visibleSimple && __instance.fileStatus.visibleBodyAlways)
            //        __instance.objHideKokan.SetActiveIfDifferent(true);
            
            flags[0] = __instance.visibleAll;
            flags[1] = __instance.fileStatus.clothesState[4] == 0;
            flags[2] = visibleBody;
            flags[3] = !visibleSimple;
            flags[4] = __instance.fileStatus.visibleBodyAlways;
            if (SetActiveControls(accClothes[4], 0, flags.Any(5))) __instance.updateShape = true;

            DrawOption(accClothes[4], ChaFileDefine.ClothesKind.gloves);

            var b2 = __instance.fileStatus.clothesState[5];
            flags[0] = __instance.visibleAll;
            flags[1] = 3 != b2 && 2 != b2;
            flags[2] = visibleBody;
            flags[3] = !visibleSimple;
            flags[4] = __instance.fileStatus.visibleBodyAlways;
            var anyPanstVisible = flags.Any(5);

            if (SetActiveControls(accClothes[5], ChaReference.RefObjKey.S_PANST_DEF, __instance.fileStatus.clothesState[5] == 0 && anyPanstVisible)) __instance.updateShape = true;

            if (SetActiveControls(accClothes[5], ChaReference.RefObjKey.S_PANST_NUGE, 1 == __instance.fileStatus.clothesState[5] && anyPanstVisible)) __instance.updateShape = true;

            DrawOption(accClothes[5], ChaFileDefine.ClothesKind.panst);

            flags[0] = __instance.visibleAll;
            flags[1] = __instance.fileStatus.clothesState[6] == 0;
            flags[2] = visibleBody;
            flags[3] = !visibleSimple;
            flags[4] = __instance.fileStatus.visibleBodyAlways;
            if (SetActiveControls(accClothes[6], 0, flags.Any(5))) __instance.updateShape = true;

            DrawOption(accClothes[6], ChaFileDefine.ClothesKind.socks);

            flags[0] = __instance.visibleAll;
            flags[1] = __instance.fileStatus.shoesType == 0 || 1 == __instance.fileStatus.shoesType;
            flags[2] = __instance.fileStatus.clothesState[7] == 0;
            flags[3] = visibleBody;
            flags[4] = !visibleSimple;
            flags[5] = __instance.fileStatus.visibleBodyAlways;
            if (SetActiveControls(accClothes[7], 0, flags.Any(6))) __instance.updateShape = true;

            DrawOption(accClothes[7], ChaFileDefine.ClothesKind.shoes_inner);
            
            // If there are any top clothes in accessories and there are no normal clothes selected then pull masks from the accessory clothes
            if (accClothes[0].Count > 0)
            {
                if (__instance.nowCoordinate.clothes.parts[0].id == 0)
                {
                    if (__instance.texBodyAlphaMask == null || __instance.texBraAlphaMask == null)
                    {
                        Console.WriteLine("1");
                        foreach (var adapter in accClothes[0])
                        {
                            adapter.ApplyMasks();
                            if (__instance.texBodyAlphaMask != null && __instance.texBraAlphaMask != null)
                                break;
                        }
                    }
                    
                    if (_lastTopState != topClothesState)
                    {
                        __instance.ChangeAlphaMask(ChaFileDefine.alphaState[topClothesState, 0], ChaFileDefine.alphaState[topClothesState, 1]);
                        __instance.updateAlphaMask = false;
                    }
                }
            }
            _lastTopState = topClothesState;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeAccessoryColor))]
        private static void OnAccColorChanged(ChaControl __instance, bool __result, int slotNo)
        {
            if (__result)
            {
                var ctaa = __instance.cusAcsCmp[slotNo].GetComponent<ClothesToAccessoriesAdapter>();
                if (ctaa != null)
                {
                    ctaa.ChangeCustomClothes();
                }
            }
        }

        public class ClothesToAccessoriesAdapter : MonoBehaviour
        {
            public ChaClothesComponent ClothesComponent { get; private set; }
            public ChaAccessoryComponent AccessoryComponent { get; private set; }
            public ChaControl Owner { get; private set; }
            public ChaReference Reference { get; private set; }

            public static readonly Dictionary<ChaControl, List<ClothesToAccessoriesAdapter>[]> AllInstances;

            static ClothesToAccessoriesAdapter()
            {
                AllInstances = new Dictionary<ChaControl, List<ClothesToAccessoriesAdapter>[]>();
            }

            public void Initialize(ChaControl owner, ChaClothesComponent clothesComponent, ChaAccessoryComponent accessoryComponent, ListInfoBase listInfoBase, ChaListDefine.CategoryNo kind)
            {
                if (owner == null) throw new ArgumentNullException(nameof(owner));
                if (clothesComponent == null) throw new ArgumentNullException(nameof(clothesComponent));
                if (accessoryComponent == null) throw new ArgumentNullException(nameof(accessoryComponent));
                if (listInfoBase == null) throw new ArgumentNullException(nameof(listInfoBase));
                ClothesComponent = clothesComponent;
                AccessoryComponent = accessoryComponent;
                Owner = owner;
                _listInfoBase = listInfoBase;
                _kind = kind;

                // Treat top parts as normal tops
                if (kind >= ChaListDefine.CategoryNo.cpo_sailor_a)
                    _clothingKind = 0;
                else
                    _clothingKind = kind - ChaListDefine.CategoryNo.co_top;

                AllInstances.TryGetValue(owner, out var instances);
                if (instances == null)
                {
                    instances = Enumerable.Range(0, 8).Select(i => new List<ClothesToAccessoriesAdapter>()).ToArray();
                    AllInstances[Owner] = instances;
                }
                instances[_clothingKind].Add(this);

                Reference = gameObject.AddComponent<ChaReference>();
                Reference.CreateReferenceInfo((ulong)(_clothingKind + 5), gameObject);

                if (_kind == ChaListDefine.CategoryNo.co_gloves || _kind == ChaListDefine.CategoryNo.co_shoes || _kind == ChaListDefine.CategoryNo.co_socks)
                    AllObjects = AccessoryComponent.rendNormal.Select(x => x.transform.parent.gameObject).Distinct().ToArray();

                _colorRend = AccessoryComponent.rendNormal.FirstOrDefault(x => x != null) ?? AccessoryComponent.rendAlpha.FirstOrDefault(x => x != null) ?? AccessoryComponent.rendHair.FirstOrDefault(x => x != null);
                InitBaseCustomTextureClothes();
                
                // bug: if multipe clothes with skirt dynamic bones are spawned then their dynamic bone components all work at the same time on the same body bones, which can cause some weird physics effects
            }

            public GameObject[] AllObjects { get; private set; }

            private void OnDestroy()
            {
                var list = AllInstances[Owner];
                list[_clothingKind].Remove(this);
                if (list.Length == 0) AllInstances.Remove(Owner);

                if (Owner)
                {
                    // If character is using masks from this instance, force top clothes refresh to repopulate the masks (if they end up null it will be handled later)
                    if (_appliedBodyMask == Owner.texBodyAlphaMask || _appliedBraMask == Owner.texBraAlphaMask)
                    {
                        var fileClothes = Owner.nowCoordinate.clothes;
                        Owner.ChangeClothesTopNoAsync(fileClothes.parts[0].id, fileClothes.subPartsId[0], fileClothes.subPartsId[1], fileClothes.subPartsId[2], true, true);
                    }
                }

                // DO NOT DESTROY
                //Destroy(_appliedBodyMask);
                //Destroy(_appliedBraMask);
            }
            
            private ChaListDefine.CategoryNo _kind;
            private int _clothingKind;
            private ListInfoBase _listInfoBase;
            readonly CustomTextureCreate[] _ctcArr = new CustomTextureCreate[3];
            private Renderer _colorRend;
            private Texture _appliedBodyMask;
            private Texture _appliedBraMask;
            
            private bool InitBaseCustomTextureClothes()
            {
                var lib = _listInfoBase;
                var mainManifest = lib.GetInfo(ChaListDefine.KeyType.MainManifest);
                var mainTexAb = lib.GetInfo(ChaListDefine.KeyType.MainTexAB);
                if ("0" == mainTexAb) mainTexAb = lib.GetInfo(ChaListDefine.KeyType.MainAB);
                var mainTex = lib.GetInfo(ChaListDefine.KeyType.MainTex);
                var info3 = lib.GetInfo(ChaListDefine.KeyType.MainManifest);
                var info4 = lib.GetInfo(ChaListDefine.KeyType.MainTex02AB);
                if ("0" == info4) info4 = lib.GetInfo(ChaListDefine.KeyType.MainAB);
                var text2 = lib.GetInfo(ChaListDefine.KeyType.MainTex02);
                var info5 = lib.GetInfo(ChaListDefine.KeyType.MainManifest);
                var info6 = lib.GetInfo(ChaListDefine.KeyType.MainTex03AB);
                if ("0" == info6) info6 = lib.GetInfo(ChaListDefine.KeyType.MainAB);
                var info7 = lib.GetInfo(ChaListDefine.KeyType.MainTex03);
                var info8 = lib.GetInfo(ChaListDefine.KeyType.MainManifest);
                var info9 = lib.GetInfo(ChaListDefine.KeyType.ColorMaskAB);
                if ("0" == info9) info9 = lib.GetInfo(ChaListDefine.KeyType.MainAB);
                var text3 = lib.GetInfo(ChaListDefine.KeyType.ColorMaskTex);
                var info10 = lib.GetInfo(ChaListDefine.KeyType.MainManifest);
                var info11 = lib.GetInfo(ChaListDefine.KeyType.ColorMask02AB);
                if ("0" == info11) info11 = lib.GetInfo(ChaListDefine.KeyType.MainAB);
                var text4 = lib.GetInfo(ChaListDefine.KeyType.ColorMask02Tex);
                var info12 = lib.GetInfo(ChaListDefine.KeyType.MainManifest);
                var info13 = lib.GetInfo(ChaListDefine.KeyType.ColorMask03AB);
                if ("0" == info13) info13 = lib.GetInfo(ChaListDefine.KeyType.MainAB);
                var info14 = lib.GetInfo(ChaListDefine.KeyType.ColorMask03Tex);
                if ("0" == mainTexAb || "0" == mainTex) return false;
                // if (!base.hiPoly) text += "_low";
                var texture2D = CommonLib.LoadAsset<Texture2D>(mainTexAb, mainTex, false, mainManifest, true);
                if (null == texture2D) return false;
                if ("0" == info9 || "0" == text3)
                {
                    Resources.UnloadAsset(texture2D);
                    return false;
                }
                // if (!base.hiPoly) text3 += "_low";
                var texture2D2 = CommonLib.LoadAsset<Texture2D>(info9, text3, false, info8, true);
                if (null == texture2D2)
                {
                    Resources.UnloadAsset(texture2D);
                    return false;
                }
                Texture2D texture2D3 = null;
                if ("0" != info4 && "0" != text2)
                {
                    // if (!base.hiPoly) text2 += "_low";
                    texture2D3 = CommonLib.LoadAsset<Texture2D>(info4, text2, false, info3, true);
                }
                Texture2D texture2D4 = null;
                if ("0" != info11 && "0" != text4)
                {
                    // if (!base.hiPoly)text4 += "_low";
                    texture2D4 = CommonLib.LoadAsset<Texture2D>(info11, text4, false, info10, true);
                }
                Texture2D texture2D5 = null;
                if ("0" != info6 && "0" != info7)
                {
                    texture2D5 = CommonLib.LoadAsset<Texture2D>(info6, info7, false, info5, true);
                }
                Texture2D texture2D6 = null;
                if ("0" != info13 && "0" != info14)
                {
                    texture2D6 = CommonLib.LoadAsset<Texture2D>(info13, info14, false, info12, true);
                }
                const string createMatABName = "chara/mm_base.unity3d";
                const string createMatName = "cf_m_clothesN_create";
                for (var i = 0; i < 3; i++)
                {
                    CustomTextureCreate customTextureCreate = null;
                    Texture2D texture2D7;
                    Texture2D tex;
                    if (i == 0)
                    {
                        texture2D7 = texture2D;
                        tex = texture2D2;
                    }
                    else if (1 == i)
                    {
                        texture2D7 = texture2D3;
                        tex = texture2D4;
                    }
                    else
                    {
                        texture2D7 = texture2D5;
                        tex = texture2D6;
                    }
                    if (null != texture2D7)
                    {
                        customTextureCreate = new CustomTextureCreate(Owner.objRoot.transform);
                        var width = texture2D7.width;
                        var height = texture2D7.height;
                        customTextureCreate.Initialize(createMatABName, createMatName, "", width, height, RenderTextureFormat.ARGB32);
                        customTextureCreate.SetMainTexture(texture2D7);
                        customTextureCreate.SetTexture(ChaShader._ColorMask, tex);
                    }

                    _ctcArr[i] = customTextureCreate;
                }
                return true;
            }

            public bool ChangeCustomClothes(/*bool main, int kind, bool updateColor, bool updateTex01, bool updateTex02, bool updateTex03, bool updateTex04*/)
            {
                Console.WriteLine("ChangeCustomClothes triggered");
                bool main = true, updateColor = true, updateTex01 = true, updateTex02 = true, updateTex03 = true, updateTex04 = true;
                var kind = 0;
                //     CustomTextureCreate[] array = new CustomTextureCreate[]
                //     {
                // main ? base.ctCreateClothes[kind, 0] : base.ctCreateClothesSub[kind, 0],
                // main ? base.ctCreateClothes[kind, 1] : base.ctCreateClothesSub[kind, 1],
                // main ? base.ctCreateClothes[kind, 2] : base.ctCreateClothesSub[kind, 2]
                //     };
                var array = _ctcArr;
                if (array[0] == null)
                {
                    return false;
                }

                var chaClothesComponent = ClothesComponent; //(main ? base.GetCustomClothesComponent(kind) : base.GetCustomClothesSubComponent(kind));
                if (null == chaClothesComponent)
                {
                    return false;
                }

                //ChaFileClothes.PartsInfo partsInfo = (main ? this.nowCoordinate.clothes.parts[kind] : this.nowCoordinate.clothes.parts[0]);
                var partsInfo = new ChaFileClothes.PartsInfo();
                partsInfo.colorInfo[0].baseColor = _colorRend.material.GetColor(ChaShader._Color);   //colors[0];//
                partsInfo.colorInfo[1].baseColor = _colorRend.material.GetColor(ChaShader._Color2);  //colors[1];//
                partsInfo.colorInfo[2].baseColor = _colorRend.material.GetColor(ChaShader._Color3);  //colors[2];//
                //partsInfo.colorInfo[3].baseColor = _colorRend.material.GetColor(ChaShader._Color4);


                if (main)
                {
                    if (!updateColor && !updateTex01 && !updateTex02 && !updateTex03)
                    {
                        return false;
                    }
                }
                else if (!updateColor && !updateTex01 && !updateTex02 && !updateTex03 && !updateTex04)
                {
                    return false;
                }
                var result = true;
                var array2 = new int[]
                {
            ChaShader._PatternMask1,
            ChaShader._PatternMask2,
            ChaShader._PatternMask3,
            ChaShader._PatternMask1
                };
                var array3 = new bool[] { updateTex01, updateTex02, updateTex03, updateTex04 };
                var num = ((!main && 2 == kind) ? 4 : 3);
                for (var i = 0; i < num; i++)
                {
                    if (array3[i])
                    {
                        Texture2D tex = null;
                        string text;
                        string text2;
                        Owner.lstCtrl.GetFilePath(ChaListDefine.CategoryNo.mt_pattern, partsInfo.colorInfo[i].pattern, ChaListDefine.KeyType.MainTexAB, ChaListDefine.KeyType.MainTex, out text, out text2);
                        if ("0" != text && "0" != text2)
                        {
                            //if (!base.hiPoly)
                            //{
                            //    text2 += "_low";
                            //}
                            tex = CommonLib.LoadAsset<Texture2D>(text, text2, false, "", true);
                            Character.AddLoadAssetBundle(text, "");
                        }
                        foreach (var customTextureCreate in array)
                        {
                            if (customTextureCreate != null)
                            {
                                customTextureCreate.SetTexture(array2[i], tex);
                            }
                        }
                    }
                }
                if (updateColor)
                {
                    foreach (var customTextureCreate2 in array)
                    {
                        if (customTextureCreate2 != null)
                        {
                            if (!main && 2 == kind)
                            {
                                customTextureCreate2.SetColor(ChaShader._Color, partsInfo.colorInfo[3].baseColor);
                                customTextureCreate2.SetColor(ChaShader._Color1_2, partsInfo.colorInfo[3].patternColor);
                                customTextureCreate2.SetFloat(ChaShader._PatternScale1u, partsInfo.colorInfo[3].tiling.x);
                                customTextureCreate2.SetFloat(ChaShader._PatternScale1v, partsInfo.colorInfo[3].tiling.y);
                                customTextureCreate2.SetFloat(ChaShader._PatternOffset1u, partsInfo.colorInfo[3].offset.x);
                                customTextureCreate2.SetFloat(ChaShader._PatternOffset1v, partsInfo.colorInfo[3].offset.y);
                                customTextureCreate2.SetFloat(ChaShader._PatternRotator1, partsInfo.colorInfo[3].rotate);
                            }
                            else
                            {
                                customTextureCreate2.SetColor(ChaShader._Color, partsInfo.colorInfo[0].baseColor);
                                customTextureCreate2.SetColor(ChaShader._Color1_2, partsInfo.colorInfo[0].patternColor);
                                customTextureCreate2.SetFloat(ChaShader._PatternScale1u, partsInfo.colorInfo[0].tiling.x);
                                customTextureCreate2.SetFloat(ChaShader._PatternScale1v, partsInfo.colorInfo[0].tiling.y);
                                customTextureCreate2.SetFloat(ChaShader._PatternOffset1u, partsInfo.colorInfo[0].offset.x);
                                customTextureCreate2.SetFloat(ChaShader._PatternOffset1v, partsInfo.colorInfo[0].offset.y);
                                customTextureCreate2.SetFloat(ChaShader._PatternRotator1, partsInfo.colorInfo[0].rotate);
                            }
                            customTextureCreate2.SetColor(ChaShader._Color2, partsInfo.colorInfo[1].baseColor);
                            customTextureCreate2.SetColor(ChaShader._Color2_2, partsInfo.colorInfo[1].patternColor);
                            customTextureCreate2.SetFloat(ChaShader._PatternScale2u, partsInfo.colorInfo[1].tiling.x);
                            customTextureCreate2.SetFloat(ChaShader._PatternScale2v, partsInfo.colorInfo[1].tiling.y);
                            customTextureCreate2.SetColor(ChaShader._Color3, partsInfo.colorInfo[2].baseColor);
                            customTextureCreate2.SetColor(ChaShader._Color3_2, partsInfo.colorInfo[2].patternColor);
                            customTextureCreate2.SetFloat(ChaShader._PatternScale3u, partsInfo.colorInfo[2].tiling.x);
                            customTextureCreate2.SetFloat(ChaShader._PatternScale3v, partsInfo.colorInfo[2].tiling.y);
                            customTextureCreate2.SetFloat(ChaShader._PatternOffset2u, partsInfo.colorInfo[1].offset.x);
                            customTextureCreate2.SetFloat(ChaShader._PatternOffset2v, partsInfo.colorInfo[1].offset.y);
                            customTextureCreate2.SetFloat(ChaShader._PatternRotator2, partsInfo.colorInfo[1].rotate);
                            customTextureCreate2.SetFloat(ChaShader._PatternOffset3u, partsInfo.colorInfo[2].offset.x);
                            customTextureCreate2.SetFloat(ChaShader._PatternOffset3v, partsInfo.colorInfo[2].offset.y);
                            customTextureCreate2.SetFloat(ChaShader._PatternRotator3, partsInfo.colorInfo[2].rotate);
                        }
                    }
                }
                var flag = chaClothesComponent.rendNormal01 != null && chaClothesComponent.rendNormal01.Length != 0;
                var flag2 = chaClothesComponent.rendAlpha01 != null && chaClothesComponent.rendAlpha01.Length != 0;
                if (flag || flag2)
                {
                    var texture = array[0].RebuildTextureAndSetMaterial();
                    if (null != texture)
                    {
                        if (flag)
                        {
                            for (var k = 0; k < chaClothesComponent.rendNormal01.Length; k++)
                            {
                                if (chaClothesComponent.rendNormal01[k])
                                {
                                    chaClothesComponent.rendNormal01[k].material.SetTexture(ChaShader._MainTex, texture);
                                }
                                else
                                {
                                    result = false;
                                }
                            }
                        }
                        if (flag2)
                        {
                            for (var l = 0; l < chaClothesComponent.rendAlpha01.Length; l++)
                            {
                                if (chaClothesComponent.rendAlpha01[l])
                                {
                                    chaClothesComponent.rendAlpha01[l].material.SetTexture(ChaShader._MainTex, texture);
                                }
                                else
                                {
                                    result = false;
                                }
                            }
                        }
                    }
                    else
                    {
                        result = false;
                    }
                }
                else
                {
                    result = false;
                }
                if (chaClothesComponent.rendNormal02 != null && chaClothesComponent.rendNormal02.Length != 0 && array[1] != null)
                {
                    var texture2 = array[1].RebuildTextureAndSetMaterial();
                    if (null != texture2)
                    {
                        for (var m = 0; m < chaClothesComponent.rendNormal02.Length; m++)
                        {
                            if (chaClothesComponent.rendNormal02[m])
                            {
                                chaClothesComponent.rendNormal02[m].material.SetTexture(ChaShader._MainTex, texture2);
                            }
                            else
                            {
                                result = false;
                            }
                        }
                    }
                    else
                    {
                        result = false;
                    }
                }
                if (chaClothesComponent.rendNormal03 != null && chaClothesComponent.rendNormal03.Length != 0 && array[2] != null)
                {
                    var texture3 = array[2].RebuildTextureAndSetMaterial();
                    if (null != texture3)
                    {
                        for (var n = 0; n < chaClothesComponent.rendNormal03.Length; n++)
                        {
                            if (chaClothesComponent.rendNormal03[n])
                            {
                                chaClothesComponent.rendNormal03[n].material.SetTexture(ChaShader._MainTex, texture3);
                            }
                            else
                            {
                                result = false;
                            }
                        }
                    }
                    else
                    {
                        result = false;
                    }
                }
                if (null != chaClothesComponent.rendAccessory && array[0] != null)
                {
                    var texture4 = array[0].RebuildTextureAndSetMaterial();
                    if (null != texture4)
                    {
                        if (chaClothesComponent.rendAccessory)
                        {
                            chaClothesComponent.rendAccessory.material.SetTexture(ChaShader._MainTex, texture4);
                        }
                        else
                        {
                            result = false;
                        }
                    }
                    else
                    {
                        result = false;
                    }
                }
                return result;
            }



            public void ApplyMasks()
            {
                //Owner.AddClothesStateKind(kindNo, _listInfoBase.GetInfo(ChaListDefine.KeyType.StateType));
                Console.WriteLine("2");
                if (Owner.texBodyAlphaMask == null)
                {
                    if (_appliedBodyMask == null)
                    {
                        Console.WriteLine("2a");

                        Owner.LoadAlphaMaskTexture(_listInfoBase.GetInfo(ChaListDefine.KeyType.OverBodyMaskAB), _listInfoBase.GetInfo(ChaListDefine.KeyType.OverBodyMask), 0);
                        _appliedBodyMask = Owner.texBodyAlphaMask;
                    }
                    else
                    {
                        Console.WriteLine("2b");
                        Owner.texBodyAlphaMask = _appliedBodyMask;
                    }

                    if (Owner.customMatBody)
                    {
                        Owner.customMatBody.SetTexture(ChaShader._AlphaMask, Owner.texBodyAlphaMask);
                    }
                }

                if (Owner.texBraAlphaMask == null)
                {
                    Console.WriteLine("3");
                    if (_appliedBraMask == null)
                    {
                        Console.WriteLine("3a");
                        Owner.LoadAlphaMaskTexture(_listInfoBase.GetInfo(ChaListDefine.KeyType.OverBraMaskAB), _listInfoBase.GetInfo(ChaListDefine.KeyType.OverBraMask), 1);
                        _appliedBraMask = Owner.texBraAlphaMask;
                    }
                    else
                    {
                        Console.WriteLine("3b");
                        Owner.texBraAlphaMask = _appliedBraMask;
                    }

                    if (Owner.rendBra != null)
                    {
                        Console.WriteLine("3c");
                        var listInfoBase2 = Owner.infoClothes[2];
                        if (listInfoBase2 != null)
                        {
                            var num2 = ((2 == listInfoBase2.GetInfoInt(ChaListDefine.KeyType.Coordinate)) ? 1 : 0);
                            string b;
                            if (listInfoBase2.dictInfo.TryGetValue(105, out b))
                            {
                                num2 = (("1" == b) ? 1 : 0);
                            }

                            if (Owner.rendBra[0] != null)
                            {
                                Owner.rendBra[0].material.SetTexture(ChaShader._AlphaMask, Owner.texBraAlphaMask);
                                Owner.rendBra[0].material.SetTextureOffset(ChaShader._AlphaMask, ChaListDefine.braOffset[num2]);
                                Owner.rendBra[0].material.SetTextureScale(ChaShader._AlphaMask, ChaListDefine.braTiling[num2]);
                            }

                            if (Owner.rendBra[1] != null)
                            {
                                Owner.rendBra[1].material.SetTexture(ChaShader._AlphaMask, Owner.texBraAlphaMask);
                                Owner.rendBra[1].material.SetTextureOffset(ChaShader._AlphaMask, ChaListDefine.braOffset[num2]);
                                Owner.rendBra[1].material.SetTextureScale(ChaShader._AlphaMask, ChaListDefine.braTiling[num2]);
                            }
                        }
                    }
                }

                //todo
                //string info = _listInfoBase.GetInfo(ChaListDefine.KeyType.NormalData);
                //if ("0" != info)
                //{
                //	BustNormal bustNormal2 = null;
                //	if (!Owner.dictBustNormal.TryGetValue(ChaControl.BustNormalKind.NmlTop, out bustNormal2))
                //	{
                //		bustNormal2 = new BustNormal();
                //	}
                //	bustNormal2.Init(Owner.objClothes[_clothingKind], _listInfoBase.GetInfo(ChaListDefine.KeyType.MainAB), info, _listInfoBase.GetInfo(ChaListDefine.KeyType.MainManifest));
                //	Owner.dictBustNormal[ChaControl.BustNormalKind.NmlTop] = bustNormal2;
                //}
            }
        }

        #endregion
    }
}
