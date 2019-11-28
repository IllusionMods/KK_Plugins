using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UniRx;
#if AI
using AIChara;
#endif

namespace KK_Plugins
{
    internal partial class Hooks
    {
#if AI
        /// <summary>
        /// Do color matching whenever the body texture is changed
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.CreateBodyTexture))]
        internal static void CreateBodyTexture(ChaControl __instance) => UncensorSelector.GetController(__instance)?.UpdateSkinColor();

        /// <summary>
        /// Postfix patch to check underwear clothing state and hide objDanTop when the clothes are on. Would be better as a transpiler.
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), "UpdateVisible")]
        internal static void UpdateVisible(ChaControl __instance, bool ___drawSimple, bool ___confSon, List<bool> ___lstActive)
        {
            if (!___drawSimple && __instance.cmpBody && __instance.cmpBody.targetEtc.objDanTop)
            {
                bool pantsOff = !__instance.IsClothesStateKind(1) || __instance.fileStatus.clothesState[1] != 0;
                bool underwearOff = !__instance.IsClothesStateKind(3) || __instance.fileStatus.clothesState[3] != 0; //Added check

                ___lstActive.Clear();
                ___lstActive.Add(__instance.visibleAll);
                ___lstActive.Add(___drawSimple || (pantsOff && underwearOff) || __instance.fileStatus.visibleSon);
                ___lstActive.Add(___confSon);
                ___lstActive.Add(__instance.fileStatus.visibleSonAlways);
                YS_Assist.SetActiveControl(__instance.cmpBody.targetEtc.objDanTop, ___lstActive);
            }
        }
#else
        /// <summary>
        /// Do color matching whenever the body texture is changed
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetBodyBaseMaterial))]
        internal static void SetBodyBaseMaterial(ChaControl __instance) => UncensorSelector.GetController(__instance)?.UpdateSkinColor();
#endif


#if KK || EC
        /// <summary>
        /// LineMask texture assigned to the material, toggled on and off for any color matching parts along with the body
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.VisibleAddBodyLine))]
        internal static void VisibleAddBodyLine(ChaControl __instance) => UncensorSelector.GetController(__instance)?.UpdateSkinLine();

        /// <summary>
        /// Skin gloss slider level, as assigned in the character maker. This corresponds to the red coloring in the DetailMask texture.
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingSkinGlossPower))]
        internal static void ChangeSettingSkinGlossPower(ChaControl __instance) => UncensorSelector.GetController(__instance)?.UpdateSkinGloss();
#endif

        /// <summary>
        /// Demosaic
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), "LateUpdateForce")]
        internal static void LateUpdateForce(ChaControl __instance) => __instance.hideMoz = true;

        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.CreateBodyTexture))]
        internal static void CreateBodyTexturePrefix(ChaControl __instance) => UncensorSelector.CurrentBodyGUID = UncensorSelector.GetController(__instance)?.BodyData?.BodyGUID;

        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), "InitBaseCustomTextureBody")]
        internal static void InitBaseCustomTextureBodyPrefix(ChaControl __instance) => UncensorSelector.CurrentBodyGUID = UncensorSelector.GetController(__instance)?.BodyData?.BodyGUID;

        /// <summary>
        /// Modifies the code for string replacement of oo_base, etc.
        /// </summary>
        [HarmonyTranspiler, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.CreateBodyTexture))]
        internal static IEnumerable<CodeInstruction> CreateBodyTextureTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();

            foreach (var x in instructionsList)
            {
                switch (x.operand?.ToString())
                {
                    case "chara/oo_base.unity3d":
                        x.opcode = OpCodes.Call;
                        x.operand = typeof(UncensorSelector).GetMethod(nameof(UncensorSelector.SetOOBase), AccessTools.all);
                        break;
                    case "cf_body_00_t":
                        x.opcode = OpCodes.Call;
                        x.operand = typeof(UncensorSelector).GetMethod(nameof(UncensorSelector.SetBodyMainTex), AccessTools.all);
                        break;
                    case "cm_body_00_mc":
                        x.opcode = OpCodes.Call;
                        x.operand = typeof(UncensorSelector).GetMethod(nameof(UncensorSelector.SetBodyColorMaskMale), AccessTools.all);
                        break;
                    case "cf_body_00_mc":
                        x.opcode = OpCodes.Call;
                        x.operand = typeof(UncensorSelector).GetMethod(nameof(UncensorSelector.SetBodyColorMaskFemale), AccessTools.all);
                        break;
                }
            }

            return instructions;
        }

        /// <summary>
        /// Modifies the code for string replacement of mm_base, etc.
        /// </summary>
        [HarmonyTranspiler, HarmonyPatch(typeof(ChaControl), "InitBaseCustomTextureBody")]
        internal static IEnumerable<CodeInstruction> InitBaseCustomTextureBodyTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();

            foreach (var x in instructionsList)
            {
                switch (x.operand?.ToString())
                {
                    case "chara/mm_base.unity3d":
                        x.opcode = OpCodes.Call;
                        x.operand = typeof(UncensorSelector).GetMethod(nameof(UncensorSelector.SetMMBase), AccessTools.all);
                        break;
                    case "cm_m_body":
                        x.opcode = OpCodes.Call;
                        x.operand = typeof(UncensorSelector).GetMethod(nameof(UncensorSelector.SetBodyMaterialMale), AccessTools.all);
                        break;
                    case "cf_m_body":
                        x.opcode = OpCodes.Call;
                        x.operand = typeof(UncensorSelector).GetMethod(nameof(UncensorSelector.SetBodyMaterialFemale), AccessTools.all);
                        break;
                    case "cf_m_body_create":
                        x.opcode = OpCodes.Call;
                        x.operand = typeof(UncensorSelector).GetMethod(nameof(UncensorSelector.SetBodyMaterialCreate), AccessTools.all);
                        break;
                }
            }

            return instructions;
        }
    }
}