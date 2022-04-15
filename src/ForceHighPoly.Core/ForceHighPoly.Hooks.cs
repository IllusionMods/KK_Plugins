using HarmonyLib;

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
                var exType = Traverse.Create(__instance).Property("exType");
                var exTypeExists = exType.PropertyExists();
                if (Enabled.Value) __instance.hiPoly = true;
                if (__instance.hiPoly || !PartialPoly.Value) return;
                var coordinate = __instance.chaFile.coordinate;
                for (var i = 0; i < coordinate.Length; i++)
                {
                    var clothParts = coordinate[i].clothes.parts;
                    for (var j = 0; j < clothParts.Length; j++)
                    {
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
                        var currentcloth = clothParts[j];
                        var id = currentcloth.id;
                        var work = __instance.lstCtrl.GetCategoryInfo((ChaListDefine.CategoryNo)category);
                        if (!work.TryGetValue(id, out var lib))
                        {
                            continue;
                        }
                        else if (category == 105 || category == 107)
                        {
                            var infoInt = lib.GetInfoInt(ChaListDefine.KeyType.Sex);
                            if (__instance.sex == 0 && infoInt == 3 || __instance.sex == 1 && infoInt == 2)
                            {
                                if (id != 0)
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
                        if (!CommonLib.LoadAsset<UnityEngine.GameObject>(assetBundleName, lib.GetInfo(ChaListDefine.KeyType.MainData) + "_low", false, manifestName) && CommonLib.LoadAsset<UnityEngine.GameObject>(assetBundleName, highAssetName, false, manifestName))
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
            #region Transpile code that works, but animation locks
            //public static IEnumerable<CodeInstruction> TopAsyncTranspiler(IEnumerable<CodeInstruction> instructions)
            //{
            //    var loadObjThis = new CodeInstruction(OpCodes.Ldarg_0, null);
            //    var AnonType = AccessTools.TypeByName("ChaControl+<ChangeClothesTopAsync>c__Iterator8, Assembly-CSharp");
            //    var controlThis = new CodeInstruction(OpCodes.Ldfld, AnonType.GetField("$this", AccessTools.all));
            //    return AsyncTranspiler(instructions, new CodeInstruction[]
            //    {
            //        //load chacontrol
            //        loadObjThis,
            //        controlThis,
            //        //load Categories
            //        new CodeInstruction(OpCodes.Ldc_I4, 105),
            //        new CodeInstruction(OpCodes.Ldc_I4, 503),
            //        //load id
            //        loadObjThis,
            //        new CodeInstruction(OpCodes.Ldfld, AnonType.GetField("id", AccessTools.all)),
            //        //load objname
            //        loadObjThis,
            //        new CodeInstruction(OpCodes.Ldfld, AnonType.GetField("<objName>__0", AccessTools.all)),
            //        //load copyDynamicBones
            //        new CodeInstruction(OpCodes.Ldc_I4_1, null),
            //        //load copyWeights
            //        new CodeInstruction(OpCodes.Ldc_I4_1, null),
            //        //load trfParent
            //        loadObjThis,
            //        controlThis,
            //        new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(ChaInfo),nameof(ChaInfo.objTop))),
            //        new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(GameObject),nameof(GameObject.transform))),
            //        //load default
            //        new CodeInstruction(OpCodes.Ldc_I4_0, null),
            //        //load async
            //        loadObjThis,
            //        new CodeInstruction(OpCodes.Ldfld, AnonType.GetField("asyncFlags", AccessTools.all)),

            //        //load worldpos
            //        new CodeInstruction(OpCodes.Ldc_I4_0, null),
            //        //load kindno
            //        new CodeInstruction(OpCodes.Ldc_I4_0, null),
            //        //call custom method
            //        new CodeInstruction(OpCodes.Call, typeof(Hooks).GetMethod(nameof(LoadFbxDual), AccessTools.all)),
            //        //loadObjThis
            //        }, AnonType); ;
            //}

            //public static IEnumerable<CodeInstruction> AsyncTranspiler(IEnumerable<CodeInstruction> instructions, CodeInstruction[] insertInstructions, Type anonType)
            //{
            //    var thisclothesmatch = new CodeMatch[]
            //        {
            //            new CodeMatch(new OpCode?(OpCodes.Stelem_Ref)),
            //            new CodeMatch(new OpCode?(OpCodes.Ldarg_0)),
            //            new CodeMatch(new OpCode?(OpCodes.Ldfld), anonType.GetField("asyncFlags", AccessTools.all), null),
            //            new CodeMatch(new OpCode?(OpCodes.Brfalse)),
            //            new CodeMatch(new OpCode?(OpCodes.Ldarg_0)),
            //            new CodeMatch(new OpCode?(OpCodes.Ldnull)),
            //            new CodeMatch(new OpCode?(OpCodes.Stfld)),
            //        };
            //    var Matcher = new CodeMatcher(instructions, null).MatchForward(false, thisclothesmatch).Advance(1);//.MatchForward(false, thisclothesmatch).Advance(2);

            //    //var thisclothesmatch = new CodeMatch[]
            //    //    {
            //    //        new CodeMatch(new OpCode?(OpCodes.Br)),
            //    //        new CodeMatch(new OpCode?(OpCodes.Ldarg_0)),
            //    //        new CodeMatch(new OpCode?(OpCodes.Ldfld), anonType.GetField("$this",AccessTools.all), null),
            //    //        new CodeMatch(new OpCode?(OpCodes.Call), AccessTools.PropertyGetter(typeof(ChaInfo), nameof(ChaInfo.objClothes))),
            //    //        new CodeMatch(new OpCode?(OpCodes.Ldarg_0)),
            //    //    };
            //    //var Matcher = new CodeMatcher(instructions, null).MatchForward(true, thisclothesmatch).MatchForward(false, thisclothesmatch).Advance(2);


            //    logger.LogWarning($"{Matcher.Pos}:\t found match");

            //    var test = Matcher.InsertAndAdvance(insertInstructions).InstructionEnumeration();
            //    for (var i = 250; i < test.Count(); i++)
            //    {
            //        var instrct = test.ElementAt(i);
            //        logger.LogWarning($"{i}:\t{instrct.opcode} {instrct.operand}");
            //    }
            //    return test;
            //}

            //internal static void LoadFbx(ChaControl control, int category, int id, string createName, bool copyDynamicBone, byte copyWeights, Transform trfParent, int defaultId, bool asyncFlags, bool worldPositionStays, int kindNo)
            //{
            //    if (!control.objClothes[kindNo] && !control.hiPoly)
            //    {
            //        logger.LogWarning("attempting highpoly");
            //        if (false)
            //        {
            //            var cor = control.LoadCharaFbxDataAsync(delegate (GameObject o)
            //            {
            //                control.objClothes[kindNo] = o;
            //            }, true, category, id, createName, copyDynamicBone, copyWeights, trfParent, defaultId, worldPositionStays);
            //            //yield return control.StartCoroutine(cor);
            //        }
            //        else
            //        {
            //            control.objClothes[kindNo] = control.LoadCharaFbxData(true, category, id, createName, copyDynamicBone, copyWeights, trfParent, defaultId, worldPositionStays);
            //        }
            //        //if (control.objClothes[kindNo]) control.hiPoly = true;
            //        if (control.objClothes[kindNo] && !QueuedControls.Contains(control))
            //        {
            //            IEnumerator ExecuteDelayed_Routine()
            //            {
            //                for (var j = 0; j < 20; j++)
            //                    yield return null;
            //                logger.LogWarning("Attempting reload");
            //                control.loadEnd = false;
            //                control.hiPoly = true;
            //                //control.ReleaseAll();
            //                for (var j = 0; j < 20; j++)
            //                    yield return null;
            //                //control.StartCoroutine(control.ReloadAsync(false, false, false, false, true));
            //                control.StartCoroutine(control.LoadAsync(true, true));
            //                for (var j = 0; j < 20; j++)
            //                    yield return null;
            //                control.SetActiveTop(true);
            //                var test = Singleton<Manager.Game>.Instance;
            //                test.advAnimePack.SetDefalut(control);
            //                QueuedControls.Remove(control);
            //            }
            //            QueuedControls.Add(control);
            //            Instance.StartCoroutine(ExecuteDelayed_Routine());
            //        }
            //    }
            //}

            //internal static void LoadFbxDual(ChaControl control, int category1, int category2, int id, string createName, bool copyDynamicBone, byte copyWeights, Transform trfParent, int defaultId, bool asyncFlags, bool worldPositionStays, int kindNo) => LoadFbx(control, (control.sex != 0 || control.exType != 1) ? category1 : category2, id, createName, copyDynamicBone, copyWeights, trfParent, defaultId, asyncFlags, worldPositionStays, kindNo);
            #endregion
        }
    }
}