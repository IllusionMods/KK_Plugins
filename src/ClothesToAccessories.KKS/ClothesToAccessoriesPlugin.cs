using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Logging;
using ChaCustom;
using HarmonyLib;
using Illusion.Game.Array;
using KKAPI;
using KKAPI.Maker;
using KKAPI.Utilities;
using Manager;
using TMPro;
using UniRx;
using UnityEngine;

namespace KK_Plugins
{
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    public class ClothesToAccessoriesPlugin : BaseUnityPlugin
    {
        public const string GUID = "ClothesToAccessories";
        public const string PluginName = "Clothes To Accessories";
        public const string PluginNameInternal = "KKS_ClothesToAccessories";
        public const string Version = "1.0";

        internal static new ManualLogSource Logger;

#if DEBUG // reload cleanup
        internal static List<IDisposable> CleanupList = new List<IDisposable>();
#endif

        private void Start()
        {
            Logger = base.Logger;

            ApplyHooks();

#if DEBUG // apply instantly since we should be running inside maker already
            InitializeMakerWindows();
#else
            MakerAPI.MakerFinishedLoading += (s, e) => InitializeMakerWindows();
#endif
        }

        private static void ApplyHooks()
        {
            var hi = new Harmony(GUID);
#if DEBUG // reload cleanup
            CleanupList.Add(Disposable.Create(() => hi.UnpatchSelf()));
#endif
            try
            {
                hi.PatchAll(typeof(ClothesToAccessoriesPlugin));

                hi.PatchMoveNext(
                    original: AccessTools.Method(typeof(ChaControl), nameof(ChaControl.ChangeAccessoryAsync),
                        new[] { typeof(int), typeof(int), typeof(int), typeof(string), typeof(bool), typeof(bool) }),
                    transpiler: new HarmonyMethod(typeof(ClothesToAccessoriesPlugin), nameof(UnlockAccessoryItemTypesTpl)));

                hi.Patch(
                    original: typeof(CvsAccessory).GetMethods(AccessTools.allDeclared).Single(mi =>
                    {
                        // find the dropdown value changed subscribtion lambda
                        var p = mi.GetParameters();
                        return p.Length == 1 &&
                               p[0].ParameterType == typeof(int) &&
                               mi.Name.StartsWith("<Start>");
                    }),
                    prefix: new HarmonyMethod(typeof(ClothesToAccessoriesPlugin), nameof(ConvertDropdownIndexToRelativeTypeIndex)));
            }
            catch
            {
                Logger.LogError("Failed to apply hooks");
                hi.UnpatchSelf();
                throw;
            }
        }

#if DEBUG // reload cleanup
        private void OnDestroy()
        {
            foreach (var disposable in CleanupList)
            {
                try { disposable.Dispose(); }
                catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
            }
        }
#endif

        #region add custom accessory categories

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

        private static void InitializeMakerWindows()
        {
            var sw = Stopwatch.StartNew();

            var slot = FindObjectOfType<CustomAcsChangeSlot>();
            var donor = slot.customAcsSelectKind.First().gameObject;

            var acc01 = slot.cvsAccessory.First();

#if DEBUG // reload cleanup
            var orig = slot.customAcsSelectKind;
            var orig2 = acc01.cgAccessoryWin;
            var orig3 = acc01.customAccessory;
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
#endif

            var originalOptionCount = acc01.GetComponentInParent<ActivateHDropDown>().targetList.Count; //cac.ddAcsType.options.Count;
            _typeIndexLookup = Enumerable.Repeat(ChaListDefine.CategoryNo.ao_none, originalOptionCount).ToList();

            var customWindows = new List<CustomAcsSelectKind>();

            foreach (var typeToAdd in _AcceptableCustomTypes)
            {
                var copy = Instantiate(donor, donor.transform.parent, false);
                copy.name = $"winAcsCustomKind_{typeToAdd}";

#if DEBUG // reload cleanup
                CleanupList.Add(Disposable.Create(() => Destroy(copy)));
#endif

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

                customWindows.Add(copyCmp);

                _typeIndexLookup.Add(typeToAdd);
            }

            void AddNewAccKindsToSlot(CvsAccessory cvsAcc)
            {
                var hCheck = cvsAcc.GetComponentInParent<ActivateHDropDown>();
                // This overwrites the options so need to do this before we modify it
                hCheck.Set(cvsAcc.ddAcsType);
                // Disable the component in case it didn't apply by itself yet
                hCheck._dropdownTMP = null;
                hCheck._dropdown = null;

                cvsAcc.ddAcsType.options.AddRange(customWindows.Select(x => new TMP_Dropdown.OptionData(x.cate.ToString())));

                //cvsAcc.ddAcsType.AddOptions(customWindows.Select(x => new TMP_Dropdown.OptionData(x.cate.ToString())).ToList());
                // Take(originalOptionCount - 1) because copied slots will most likely already have customWindows added, so they would get duplicated. -1 to account for ao_none
                cvsAcc.cgAccessoryWin = cvsAcc.cgAccessoryWin.Take(originalOptionCount - 1).Concat(customWindows.Select(x => x.GetComponent<CanvasGroup>())).ToArray();
                cvsAcc.customAccessory = cvsAcc.customAccessory.Take(originalOptionCount - 1).Concat(customWindows).ToArray();

                var template = cvsAcc.ddAcsType.template;
                //var origSize = template.sizeDelta;
#if DEBUG // reload cleanup
                CleanupList.Add(Disposable.Create(() =>
                {
                    //template.sizeDelta = origSize;
                    hCheck._dropdownTMP = cvsAcc.ddAcsType;
                }));
#endif
                // Need to hardcode the offsets instead of changing sizeDelta because they get messed up on slots added by moreaccs
                template.anchorMin = Vector2.zero;
                template.anchorMax = new Vector2(1, 0);
                template.offsetMin = new Vector2(0, -678);
                template.offsetMax = new Vector2(0, 2);
                //template.sizeDelta = new Vector2(origSize.x, origSize.y + 480);
            }

            foreach (var cvsAccessory in slot.cvsAccessory) AddNewAccKindsToSlot(cvsAccessory);

            void OnAccSlotAdded(object sender, AccessorySlotEventArgs args)
            {
                //Logger.LogDebug("Adding clothing accessory kinds to dropdown in new acc slot id=" + args.SlotIndex);
                var cvsAccessory = slot.cvsAccessory[args.SlotIndex];
                // Necessary in case the dropdown items added to an existing slot got copied
                // todo a more reliable method?
                //cvsAccessory.ddAcsType.options.RemoveAll(data => _AcceptableCustomTypes.Any(a => a.ToString() == data.text));
                AddNewAccKindsToSlot(cvsAccessory);
            }
            AccessoriesApi.MakerAccSlotAdded += OnAccSlotAdded;
#if DEBUG // reload cleanup
            CleanupList.Add(Disposable.Create(() => AccessoriesApi.MakerAccSlotAdded -= OnAccSlotAdded));
#endif

            // Needed for class maker when editing a character that has 1st accessory using an acc type this plugin adds (UI is in broken state since its updated before hooks are applied)
            if (_AcceptableCustomTypes.Contains((ChaListDefine.CategoryNo)acc01.accessory.parts[0].type))
                acc01.UpdateCustomUI();

            Logger.LogDebug($"InitializeMakerWindows finish in {sw.ElapsedMilliseconds}ms");
        }

        private static void ConvertDropdownIndexToRelativeTypeIndex(ref int idx)
        {
            if (idx == 0 || _typeIndexLookup == null) return;
            if (idx < 0 || idx >= _typeIndexLookup.Count)
            {
                //Console.WriteLine($"oops idx {idx}\n{new StackTrace()}");
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
            //Console.WriteLine($"adjust {idx} into {newIndex}");
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
                .Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ClothesToAccessoriesPlugin), nameof(ConvertTypeToDropdownIndex)) ?? throw new Exception("ConvertTypeToDropdownIndex")))
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
            var replacement = AccessTools.Method(typeof(ClothesToAccessoriesPlugin), nameof(CustomGetDefault)) ?? throw new Exception("CustomGetDefault");
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

            // Top clothes index 1 and 2 are not usable
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
        private static IEnumerable<CodeInstruction> UnlockAccessoryItemTypesTpl(IEnumerable<CodeInstruction> instructions)
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
                .Set(OpCodes.Call, AccessTools.Method(typeof(ClothesToAccessoriesPlugin), nameof(UpdateAccessoryMoveFromInfoAndReassignBones)));

            var replacement2 = AccessTools.Method(typeof(ClothesToAccessoriesPlugin), nameof(GetParentOverride)) ?? throw new Exception("GetParentOverride");
            cm.Start()
                .MatchForward(true,
                    new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)0x36),
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(ListInfoBase), nameof(ListInfoBase.GetInfo))))
                .Repeat(matcher => matcher.Set(OpCodes.Call, replacement2), s => throw new Exception("GetInfo not found - " + s));

            return cm.Instructions();
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
            var spawnedObject = instance.LoadCharaFbxData(actListInfo + newActList, _hiPoly, category, id, createName, copyDynamicBone, copyWeights, trfParent, defaultId, worldPositionStays);
            CovertToChaAccessoryComponent(spawnedObject, spawnedLib, instance, (ChaListDefine.CategoryNo)category);
            return spawnedObject;
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
            var cac = instance.GetComponent<ChaAccessoryComponent>();
            if (cac != null) return;

            var ccc = instance.GetComponent<ChaClothesComponent>();
            if (ccc != null)
            {
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
                if (positions.Length >= 3) Logger.LogWarning($"More than 2 move transforms found! id={listInfoBase.Id} kind={listInfoBase.Kind} category={listInfoBase.Category} transforms={string.Join(", ", positions.Select(x => x.name))}");

                return;
            }

            Logger.LogWarning($"CovertToChaAccessoryComponent failed for id={listInfoBase.Id} kind={listInfoBase.Kind} category={listInfoBase.Category}");
        }

        private static bool UpdateAccessoryMoveFromInfoAndReassignBones(ChaControl instance, int slotNo)
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
                    if (!componentsInChildren.IsNullOrEmpty())
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

                    FixConflictingBoneNames(accObj);
                }
                else if (accObj.GetComponent<ChaCustomHairComponent>())
                {
                    FixConflictingBoneNames(accObj);
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }

            return result;

            // If bones are left as they are, there's a good chance they will cause issues because of having same names as character body bones, so
            // game/plugin code can find these spawned bones instead of the body bones it expects
            // TODO Remove the unused bones instead? Still need them for hair?UpdateCustomUI
            void FixConflictingBoneNames(GameObject accObj)
            {
                foreach (var child in accObj.GetComponentsInChildren<Transform>(true))
                {
                    if (child != accObj.transform && !child.name.StartsWith("N_move"))
                        child.gameObject.name = "CTA_" + child.gameObject.name + "_"; // guard against StartsWith and EndsWith
                }
            }
        }

        /// <summary>
        /// Get accessory attach point for custom accessory types, otherwise call the original method
        /// </summary>
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
                            // headside is center of head, all normal hair seem to be using this
                            return ChaAccessoryDefine.AccessoryParentKey.a_n_headside.ToString();
                        case ChaListDefine.CategoryNo.bo_hair_o:
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
                            // this key doesn't actually matter since the bones get merged, but something is needed or the accessory won't be spawned properly
                            return ChaAccessoryDefine.AccessoryParentKey.a_n_waist.ToString();

                        default:
                            Logger.LogWarning($"GetParentOverride unhandled category={category}  value={instance.GetInfo(type)}");
                            break;
                    }
                }
            }

            var origInfo = instance.GetInfo(type);
            return origInfo;
        }

        #endregion

        #region handle clothes state and texture

        internal static Dictionary<ChaControl, byte> LastTopState = new Dictionary<ChaControl, byte>();

        [HarmonyPostfix]
        [HarmonyWrapSafe]
        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.UpdateVisible))]
        private static void UpdateVisibleAccessoryClothes(ChaControl __instance)
        {
            // Don't run on disabled characters
            if (!__instance.objTop || !__instance.objTop.activeSelf) return;

            // Only run this if any acc clothes are used
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
            if (Scene.NowSceneNames.Any(s => s == "H"))
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

            var topstate = anyTopVisible && accClothes[0].Any(x => x.gameObject.activeSelf) ? topClothesState : (byte)3;
            // If there are any top clothes in accessories and there are no normal clothes selected then pull masks from the accessory clothes
            if (accClothes[0].Count > 0)
            {
                if (__instance.nowCoordinate.clothes.parts[0].id == 0)
                {
                    if (__instance.texBodyAlphaMask == null || __instance.texBraAlphaMask == null)
                    {
                        foreach (var adapter in accClothes[0])
                        {
                            adapter.ApplyMasksToChaControl();
                            if (__instance.texBodyAlphaMask != null && __instance.texBraAlphaMask != null)
                            {
                                // Force ChangeAlphaMask to run
                                LastTopState[__instance] = 255;
                                break;
                            }
                        }
                    }

                    if (LastTopState.TryGetValue(__instance, out var lts) && lts != topstate)
                    {
                        __instance.ChangeAlphaMask(ChaFileDefine.alphaState[topstate, 0], ChaFileDefine.alphaState[topstate, 1]);
                        __instance.updateAlphaMask = false;
                    }
                }
            }

            for (int i = 0; i < accClothes.Length; i++)
            {
                var list = accClothes[i];
                var any = list.Count > 0;
                if (any != __instance.dictStateType.ContainsKey(i))
                {
                    if (any)
                    {
                        foreach (var adapter in list)
                        {
                            var stateTypeStr = adapter.InfoBase.GetInfo(ChaListDefine.KeyType.StateType);
                            if (byte.TryParse(stateTypeStr, out var stateTypeB))
                            {
                                __instance.AddClothesStateKindSub(i, stateTypeB);
                                break;
                            }
                        }
                    }
                    else
                    {
                        if (__instance.objClothes[i] == null)
                            __instance.RemoveClothesStateKind(i);
                    }
                }
            }

            LastTopState[__instance] = topstate;
        }

        [HarmonyPostfix]
        [HarmonyWrapSafe]
        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeAccessoryColor))]
        private static void OnAccColorChanged(ChaControl __instance, bool __result, int slotNo)
        {
            if (__result)
            {
                var ctaa = __instance.cusAcsCmp[slotNo].GetComponent<ClothesToAccessoriesAdapter>();
                if (ctaa != null)
                {
                    ctaa.UpdateClothesColorAndStuff();
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyWrapSafe]
        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeAlphaMask))]
        private static void ChangeAlphaMaskPost(ChaControl __instance, byte state0, byte state1)
        {
            ClothesToAccessoriesAdapter.AllInstances.TryGetValue(__instance, out var accClothes);
            if (accClothes == null) return;

            foreach (var adapter in accClothes[2])
            {
                adapter.ChangeAlphaMask(state0, state1);
            }
        }

        // todo unnecessary patch?
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.IsClothes))]
        private static void IsClothesPost(ChaControl __instance, int clothesKind, ref bool __result)
        {
            if (__result) return;

            ClothesToAccessoriesAdapter.AllInstances.TryGetValue(__instance, out var accClothes);
            if (accClothes == null) return;

            clothesKind = Mathf.Clamp(clothesKind, 0, accClothes.Length - 1);
            __result = accClothes[clothesKind].Count > 0;
        }

        // todo unnecessary patch?
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.IsKokanHide))]
        private static void IsKokanHidePost(ChaControl __instance, ref bool __result)
        {
            if (__result) return;

            ClothesToAccessoriesAdapter.AllInstances.TryGetValue(__instance, out var accClothes);
            if (accClothes == null) return;

            int[] clothId = { 0, 1, 2, 3 };
            int[] stateId = { 1, 1, 3, 3 };
            for (int i = 0; i < clothId.Length; i++)
            {
                if (accClothes[clothId[i]].Any(x => (i != 0 && i != 2 || x.InfoBase.GetInfo(ChaListDefine.KeyType.Coordinate) == "2") && "1" == x.InfoBase.GetInfo(ChaListDefine.KeyType.KokanHide))
                    && __instance.fileStatus.clothesState[stateId[i]] == 0)
                {
                    __result = true;
                    break;
                }
            }
        }
        #endregion
    }
}
