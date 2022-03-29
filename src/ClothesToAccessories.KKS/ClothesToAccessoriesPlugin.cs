using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Logging;
using ChaCustom;
using HarmonyLib;
using KKAPI.Utilities;
using StrayTech;
using TMPro;
using UniRx;
using UnityEngine;

namespace ClothesToAccessories
{
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

        private static readonly ChaListDefine.CategoryNo[] acceptableCustomTypes =
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
        private static List<ChaListDefine.CategoryNo> typeIndexLookup = null;


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
                    cvsAccessory.ddAcsType.options.RemoveAll(data => acceptableCustomTypes.Any(a => a.ToString() == data.text));
                    if (orig2 != null) cvsAccessory.cgAccessoryWin = orig2;
                    if (orig3 != null) cvsAccessory.customAccessory = orig3;
                }
            }));

            typeIndexLookup = Enumerable.Repeat(ChaListDefine.CategoryNo.ao_none, cac.ddAcsType.options.Count).ToList();

            foreach (var typeToAdd in acceptableCustomTypes)
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

                copyCmp.selWin.textTitle.text = $"Accessory ({copyCmp.cate})";

                slot.customAcsSelectKind = slot.customAcsSelectKind.AddToArray(copyCmp);

                foreach (var cvsAccessory in slot.cvsAccessory)
                {
                    cvsAccessory.ddAcsType.options.Add(new TMP_Dropdown.OptionData(copyCmp.cate.ToString()));
                    cvsAccessory.cgAccessoryWin = cvsAccessory.cgAccessoryWin.AddToArray(copyCmp.GetComponent<CanvasGroup>());
                    cvsAccessory.customAccessory = cvsAccessory.customAccessory.AddToArray(copyCmp);
                }

                typeIndexLookup.Add(typeToAdd);
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
            if (idx == 0 || typeIndexLookup == null) return;
            if (idx < 0 || idx >= typeIndexLookup.Count)
            {
                Console.WriteLine($"oops idx {idx}\n{new StackTrace()}");
                idx = 0;
                return;
            }

            var customType = typeIndexLookup[idx];
            // Handle default categories
            if (customType == ChaListDefine.CategoryNo.ao_none) return;

            // The dropdown index idx is later added 120 (ao_none) to convert it to CategoryNo, so 0 + 120 = CategoryNo.ao_none
            // Custom categories added by this plugin are added to the dropdown in sequendial indexes, but that does not work out when converting them by simply adding 120
            // instead their list index has to be adjusted so that when idx + 120 the result is equal to the correct CategoryNo
            var newIndex = customType - ChaListDefine.CategoryNo.ao_none;
            Console.WriteLine($"adjust {idx} into {newIndex}");
            idx = newIndex;
            return;

            /*var accCount = 10;//todo get and store dropbox item count and use that since sfw version has one less
            if (idx <= accCount) return;

            var clothesCount = ChaListDefine.CategoryNo.co_shoes - ChaListDefine.CategoryNo.co_top;

            var adjustedIndex = idx - accCount - 1;
            if (adjustedIndex <= clothesCount)
            {
                var newIndex = (adjustedIndex + ChaListDefine.CategoryNo.co_top) - ChaListDefine.CategoryNo.ao_none;
                Console.WriteLine($"adjust {idx} into {newIndex}");
                idx = newIndex;
                return;
            }

            Console.WriteLine(adjustedIndex + " uh");*/
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

        //todo !!! rewrite all conversions with a lookup table that gets populated when adding dropdown items, do an array for index with corresponding type numbers
        private static int ConvertTypeToDropdownIndex(int accTypeSubtracted)
        {
            if (typeIndexLookup == null) return accTypeSubtracted;

            // accTypeSubtracted is a CategoryNo - 120
            var accType = accTypeSubtracted + 120;
            var customAccTypeIndex = typeIndexLookup.IndexOf((ChaListDefine.CategoryNo)accType);
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
            if (index >= 0 && index < defaultAcsId.Length) return defaultAcsId[index];

            return 1;
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
        private static IEnumerable<CodeInstruction> UnlockAccessoryItemTypesTpl(IEnumerable<CodeInstruction> instructions)
        {
            var rangeReplacementMethod = AccessTools.Method(typeof(ClothesToAccessoriesPlugin), nameof(CustomAccessoryTypeCheck)) ?? throw new Exception("CustomAccessoryTypeCheck");

            return new CodeMatcher(instructions)
                // 1
                .MatchForward(true,
                    // Filter by the max value. Only replace the two necessary calls to avoid interfering with moreaccs patches
                    new CodeMatch(ins => ins.opcode == OpCodes.Ldc_I4 && ((int)ins.operand == 129 || (int)ins.operand == 130)),
                    new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(MathfEx), nameof(MathfEx.RangeEqualOn))?.MakeGenericMethod(typeof(int)) ?? throw new Exception("RangeEqualOn")))
                .Repeat(matcher => matcher.Operand = rangeReplacementMethod, s => throw new InvalidOperationException("Replacement failed - " + s))
                // 2
                .Start()
                .MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(GameObject), nameof(GameObject.GetComponent), Type.EmptyTypes, new[] { typeof(ChaAccessoryComponent) }) ?? throw new Exception("GetComponent")))
                .ThrowIfInvalid("GetComponent not found")
                .Set(OpCodes.Call, AccessTools.Method(typeof(ClothesToAccessoriesPlugin), nameof(CovertToChaAccessoryComponent)) ?? throw new Exception("CovertToChaAccessoryComponent"))
                // 3
                .End()
                .MatchBack(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(ChaControl), nameof(ChaControl.UpdateAccessoryMoveFromInfo)) ?? throw new Exception("UpdateAccessoryMoveFromInfo")))
                .ThrowIfInvalid("UpdateAccessoryMoveFromInfo not found")
                .Set(OpCodes.Call, AccessTools.Method(typeof(ClothesToAccessoriesPlugin), nameof(UpdateAccessoryMoveFromInfoAndStuff)))
                // 4
                .Start()
                .MatchForward(true,
                    new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)0x36),
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(ListInfoBase), nameof(ListInfoBase.GetInfo))))
                .Repeat(matcher => matcher.Set(OpCodes.Call, AccessTools.Method(typeof(ClothesToAccessoriesPlugin), nameof(GetParentOverride)) ?? throw new Exception("GetParentOverride")), s => throw new Exception("GetInfo not found - " + s))
                // fin
                .Instructions();
        }

        private static string GetParentOverride(ListInfoBase instance, ChaListDefine.KeyType type)
        {
            if (type == ChaListDefine.KeyType.Parent)
            {
                var category = (ChaListDefine.CategoryNo)instance.Category;
                if (category != ChaListDefine.CategoryNo.ao_none && acceptableCustomTypes.Contains(category))
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

            return acceptableCustomTypes.Contains((ChaListDefine.CategoryNo)n);
        }

        private static ChaAccessoryComponent CovertToChaAccessoryComponent(GameObject instance)
        {
            Console.WriteLine("CovertToChaAccessoryComponent");
            var cac = instance.GetComponent<ChaAccessoryComponent>();
            if (cac != null) return cac;

            var ccc = instance.GetComponent<ChaClothesComponent>();
            if (ccc != null)
            {
                Console.WriteLine("CovertToChaAccessoryComponent cloth");
                cac = instance.AddComponent<ChaAccessoryComponent>();
                cac.defColor01 = ccc.defMainColor01;
                cac.defColor02 = ccc.defMainColor02;
                cac.defColor03 = ccc.defMainColor03;
                cac.defColor04 = ccc.defAccessoryColor;
                // todo alpha?
                cac.useColor01 = ccc.useColorN01;
                cac.useColor02 = ccc.useColorN02;
                cac.useColor03 = ccc.useColorN03;

                cac.initialize = ccc.initialize;
                cac.setcolor = ccc.setcolor;

                //todo likely to be wrong
                cac.rendAlpha = ccc.rendAlpha01.Concat(ccc.rendAlpha02).ToArray();
                cac.rendNormal = ccc.rendNormal01.Concat(ccc.rendNormal02).Concat(ccc.rendNormal03).ToArray();
                //todo cac.rendHair = ccc.rend
                cac.rendHair = Array.Empty<Renderer>();

                return cac;
            }

            var chc = instance.GetComponent<ChaCustomHairComponent>();
            if (chc != null)
            {
                Console.WriteLine("CovertToChaAccessoryComponent hair");
                cac = instance.AddComponent<ChaAccessoryComponent>();
                cac.useColor01 = chc.acsDefColor.Length >= 1;
                cac.useColor02 = chc.acsDefColor.Length >= 2;
                cac.useColor03 = chc.acsDefColor.Length >= 3;
                if (cac.useColor01) cac.defColor01 = chc.acsDefColor.SafeGet(0);
                if (cac.useColor02) cac.defColor02 = chc.acsDefColor.SafeGet(1);
                if (cac.useColor03) cac.defColor03 = chc.acsDefColor.SafeGet(2);
                //cac.defColor04 = chc.acsDefColor.SafeGet(3);

                cac.initialize = chc.initialize;
                cac.setcolor = chc.setcolor;

                cac.rendHair = chc.rendHair;
                cac.rendNormal = chc.rendAccessory;
                // todo accessory as alpha?
                cac.rendAlpha = Array.Empty<Renderer>();

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

                return cac;
            }

            Console.WriteLine("CovertToChaAccessoryComponent failed");
            return null;
        }

        private static bool UpdateAccessoryMoveFromInfoAndStuff(ChaControl instance, int slotNo)
        {
            var result = instance.UpdateAccessoryMoveFromInfo(slotNo);

            try //todo try catch all custom code with proper errors
            {
                var accObj = instance.objAccessory[slotNo];

                if (accObj.GetComponent<ChaClothesComponent>())
                {
                    var chaAccessory = accObj.GetComponent<ChaAccessoryComponent>();
                    var objRootBone = instance.GetReferenceInfo(ChaReference.RefObjKey.A_ROOTBONE);

                    //AssignedWeightsAndSetBounds replaces the bones of an object with the body bones
                    // todo handle dynamic bones
                    // todo doesn't work for hair, make hair be normal accs
                    foreach (var rend in chaAccessory.rendNormal.Concat(chaAccessory.rendAlpha).Concat(chaAccessory.rendHair))
                    {
                        if (rend)
                            instance.aaWeightsBody.AssignedWeightsAndSetBounds(rend.gameObject, "cf_j_root", instance.bounds, objRootBone.transform);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return result;
        }

        #endregion

        #region fix default acc parent

        // todo parents are in AccessoryParentKey
        // breakpoints show where patching is needed
        // or add the keys to lists permanenly (side effects?)

        #endregion
    }
}
