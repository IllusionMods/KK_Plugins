using System;
using System.Collections.Generic;
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
                prefix: new HarmonyMethod(typeof(ClothesToAccessoriesPlugin), nameof(ClothesToAccessoriesPlugin.TypeDropboxIndexToTypeIndex)));
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

        private void MakeWindows()
        {
            var slot = GameObject.FindObjectOfType<CustomAcsChangeSlot>();
            var donor = slot.customAcsSelectKind.First().gameObject;
            var copy = GameObject.Instantiate(donor, donor.transform.parent, false);
            CleanupList.Add(Disposable.Create(() => GameObject.Destroy(copy)));
            var copyCmp = copy.GetComponent<CustomAcsSelectKind>();
            //todo all of them
            copyCmp.cate = ChaListDefine.CategoryNo.co_top;

            copy.name = $"winAcsCustomKind_{copyCmp.cate}";
            copyCmp.listCtrl.ClearList();
            copyCmp.Initialize();
            copyCmp.CloseWindow();


            var orig = slot.customAcsSelectKind;
            slot.customAcsSelectKind = slot.customAcsSelectKind.AddToArray(copyCmp);
            CleanupList.Add(Disposable.Create(() => slot.customAcsSelectKind = orig));

            CanvasGroup[] orig2 = null;
                CustomAcsSelectKind[] orig3 = null;
            foreach (var cvsAccessory in slot.cvsAccessory)
            {
                cvsAccessory.ddAcsType.AddOptions(new List<string>() { copyCmp.cate.ToString() });
                orig2 = cvsAccessory.cgAccessoryWin;
                cvsAccessory.cgAccessoryWin = cvsAccessory.cgAccessoryWin.AddToArray(copyCmp.GetComponent<CanvasGroup>());
                orig3 = cvsAccessory.customAccessory;
                cvsAccessory.customAccessory = cvsAccessory.customAccessory.AddToArray(copyCmp);
            }
            CleanupList.Add(Disposable.Create(() =>
            {
                foreach (var cvsAccessory in slot.cvsAccessory)
                {
                    cvsAccessory.ddAcsType.options.RemoveAll(data => data.text == copyCmp.cate.ToString());
                    if (orig2 != null) cvsAccessory.cgAccessoryWin = orig2;
                    if (orig3 != null) cvsAccessory.customAccessory = orig3;
                }
            }));
        }

        private static void TypeDropboxIndexToTypeIndex(ref int idx)
        {
            var accCount = 10;//todo get and store dropbox item count and use that since sfw version has one less
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

            Console.WriteLine(adjustedIndex + " uh");
        }

        [HarmonyTranspiler]
        [HarmonyDebug]
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
            switch ((ChaListDefine.CategoryNo)(index + 120))
            {
                case ChaListDefine.CategoryNo.bo_hair_b:
                case ChaListDefine.CategoryNo.bo_hair_f:
                case ChaListDefine.CategoryNo.bo_hair_s:
                case ChaListDefine.CategoryNo.bo_hair_o:

                case ChaListDefine.CategoryNo.co_top:
                case ChaListDefine.CategoryNo.co_bot:
                case ChaListDefine.CategoryNo.co_bra:
                case ChaListDefine.CategoryNo.co_shorts:
                case ChaListDefine.CategoryNo.co_gloves:
                case ChaListDefine.CategoryNo.co_panst:
                case ChaListDefine.CategoryNo.co_socks:
                case ChaListDefine.CategoryNo.co_shoes:
                //todo
                case ChaListDefine.CategoryNo.cpo_sailor_a:
                case ChaListDefine.CategoryNo.cpo_sailor_b:
                case ChaListDefine.CategoryNo.cpo_sailor_c:
                case ChaListDefine.CategoryNo.cpo_jacket_a:
                case ChaListDefine.CategoryNo.cpo_jacket_b:
                case ChaListDefine.CategoryNo.cpo_jacket_c:
                //todo

                default:
                    return 1;
            }
        }

        #endregion

        #region allow loading clothes as accs

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
                // fin
                .Instructions();
        }

        private static bool CustomAccessoryTypeCheck(int min, int n, int max)
        {
            if (min != 121 || n >= min && n <= max) return MathfEx.RangeEqualOn(min, n, max);

            switch ((ChaListDefine.CategoryNo)n)
            {
                //case ChaListDefine.CategoryNo.bo_head:
                case ChaListDefine.CategoryNo.bo_hair_b:
                case ChaListDefine.CategoryNo.bo_hair_f:
                case ChaListDefine.CategoryNo.bo_hair_s:
                case ChaListDefine.CategoryNo.bo_hair_o:
                    return true;
                case ChaListDefine.CategoryNo.co_top:
                case ChaListDefine.CategoryNo.co_bot:
                case ChaListDefine.CategoryNo.co_bra:
                case ChaListDefine.CategoryNo.co_shorts:
                case ChaListDefine.CategoryNo.co_gloves:
                case ChaListDefine.CategoryNo.co_panst:
                case ChaListDefine.CategoryNo.co_socks:
                case ChaListDefine.CategoryNo.co_shoes:
                    return true;
                case ChaListDefine.CategoryNo.cpo_sailor_a:
                case ChaListDefine.CategoryNo.cpo_sailor_b:
                case ChaListDefine.CategoryNo.cpo_sailor_c:
                case ChaListDefine.CategoryNo.cpo_jacket_a:
                case ChaListDefine.CategoryNo.cpo_jacket_b:
                case ChaListDefine.CategoryNo.cpo_jacket_c:
                    return true;
                //case ChaListDefine.CategoryNo.ex_bo_hair:
                //case ChaListDefine.CategoryNo.ex_co_clothes:
                //case ChaListDefine.CategoryNo.ex_co_shoes:
                default:
                    return false;
            }
        }

        private static ChaAccessoryComponent CovertToChaAccessoryComponent(GameObject instance)
        {
            var cac = instance.GetComponent<ChaAccessoryComponent>();
            if (cac != null) return cac;

            var ccc = instance.GetComponent<ChaClothesComponent>();
            if (ccc != null)
            {
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

                var chaAccessory = accObj.GetComponent<ChaAccessoryComponent>();
                var objRootBone = instance.GetReferenceInfo(ChaReference.RefObjKey.A_ROOTBONE);

                //AssignedWeightsAndSetBounds replaces the bones of an object with the body bones
                foreach (var rend in chaAccessory.rendNormal.Concat(chaAccessory.rendAlpha).Concat(chaAccessory.rendHair))
                {
                    if (rend)
                        instance.aaWeightsBody.AssignedWeightsAndSetBounds(rend.gameObject, "cf_j_root", instance.bounds, objRootBone.transform);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return result;
        }

        #endregion
    }
}
