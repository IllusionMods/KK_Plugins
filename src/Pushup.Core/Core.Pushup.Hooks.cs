using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ChaCustom;
using HarmonyLib;
using KKAPI.Maker;
using KKAPI.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace KK_Plugins
{
    public partial class Pushup
    {
        internal partial class Hooks
        {
            private static Harmony _harmony;
            private static HashSet<string> _sldLookup;

            /// <summary>
            /// Trigger the ClothesStateChangeEvent for tops and bras
            /// </summary>
            [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetClothesState))]
            private static void SetClothesStatePostfix(ChaControl __instance, int clothesKind)
            {
                if (clothesKind == 0 || clothesKind == 2) //tops and bras
                {
                    var controller = GetCharaController(__instance);
                    if (controller != null)
                        controller.ClothesStateChangeEvent();
                }
            }

            /// <summary>
            /// Set the CharacterLoading flag
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.CreateBodyTexture))]
            private static void CreateBodyTextureHook(ChaControl __instance)
            {
                var controller = GetCharaController(__instance);
                if (controller != null)
                    controller.CharacterLoading = true;
            }

            /// <summary>
            /// When the Breast tab of the character maker is set active, disable Pushup because the game will try to set the sliders to the current body values.
            /// </summary>
            [HarmonyPrefix]
            [HarmonyPatch(typeof(ChaCustom.CustomBase), nameof(ChaCustom.CustomBase.updateCvsBreast), MethodType.Setter)]
            [HarmonyPatch(typeof(ChaCustom.CvsBodyShapeAll), nameof(ChaCustom.CustomBase.updateCvsBodyShapeAll), MethodType.Setter)]
            private static void UpdateCvsBreastPrefix()
            {
                var controller = GetCharaController(MakerAPI.GetCharacterControl());
                if (controller != null)
                    controller.MapBodyInfoToChaFile(controller.BaseData);
            }

            /// <summary>
            /// Re-enable Pushup
            /// </summary>
            [HarmonyPostfix]
            [HarmonyPatch(typeof(ChaCustom.CustomBase), nameof(ChaCustom.CustomBase.updateCvsBreast), MethodType.Setter)]
            [HarmonyPatch(typeof(ChaCustom.CvsBodyShapeAll), nameof(ChaCustom.CustomBase.updateCvsBodyShapeAll), MethodType.Setter)]
            private static void UpdateCvsBreastPostfix()
            {
                var controller = GetCharaController(MakerAPI.GetCharacterControl());
                if (controller != null)
                    controller.RecalculateBody();
            }

            [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCustomClothes))]
            private static void ChangeCustomClothes(ChaControl __instance, int kind)
            {
                if (MakerAPI.InsideAndLoaded)
                    if (kind == 0 || kind == 2) //Tops and bras
                    {
                        var controller = GetCharaController(__instance);
                        if (controller != null)
                            controller.ClothesChangeEvent();
                    }
            }

//todo kks different m name
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(CvsBreast), nameof(CvsBreast.Start))]
            [HarmonyPatch(typeof(CvsBodyShapeAll), nameof(CvsBodyShapeAll.Start))]
            private static IEnumerable<CodeInstruction>  asd(IEnumerable<CodeInstruction> instructions)
            {
               return new CodeMatcher(instructions)
                    .MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Slider), nameof(Slider.onValueChanged))))
                    .Repeat(m =>
                    {
                        m.Advance(-1);
                        var mi = (FieldInfo)m.Operand;
                        m.Advance(4);
                        if (_sldLookup.Contains(mi.Name))
                        {
                            Console.WriteLine($"hit {mi.FieldType.FullName}.{mi.Name}");
                            if (m.Opcode == OpCodes.Ldftn)
                            {
                                var topatch = (MethodInfo)m.Operand;
                                Console.WriteLine("patch " + topatch.FullDescription());
                                //todo patch it
                                _harmony.Patch(topatch, new HarmonyMethod(typeof(Hooks), nameof(Hooks.SliderHook)));
                            }
                        }
                    }).Instructions();
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(CvsBreast), nameof(CvsBreast.Start))]
            [HarmonyPatch(typeof(CvsBodyShapeAll), nameof(CvsBodyShapeAll.Start))]
            private static void dsds(MonoBehaviour __instance, ref Slider[] ___sliders, out Slider[] __state)
            {
                __state = ___sliders;
                ___sliders = ___sliders.Except(_sldLookup.Select(x => __instance.GetFieldValue(x, out var val) ? (Slider)val : null)).ToArray();
                Console.WriteLine($"{__state.Length} -> {___sliders.Length}");
            }
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CvsBreast), nameof(CvsBreast.Start))]
            [HarmonyPatch(typeof(CvsBodyShapeAll), nameof(CvsBodyShapeAll.Start))]
            private static void dsds(ref Slider[] ___sliders, Slider[] __state)
            {
                ___sliders = __state;
            }

            /// <summary>
            /// Cancel the original slider onValueChanged event
            /// </summary>
            internal static bool SliderHook() => false;

            public static void ApplyHooks(Harmony harmony)
            {
                _sldLookup = new HashSet<string>
                {
                    "sldBustSize",
                    "sldBustY",
                    "sldBustRotX",
                    "sldBustX",
                    "sldBustRotY",
                    "sldBustSharp",
                    "sldBustForm",
                    "sldBustSoftness",
                    "sldBustWeight",
                    "sldAreolaBulge",
                    "sldNipWeight",
                    "sldNipStand",
                };
                _harmony = harmony;

                harmony.PatchAll(typeof(Hooks));
            }
        }
    }
}
