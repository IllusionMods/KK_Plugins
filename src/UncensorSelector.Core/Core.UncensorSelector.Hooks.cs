using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UniRx;
#if AI || HS2
using AIChara;
#endif

namespace KK_Plugins
{
    internal partial class Hooks
    {
#if AI || HS2
        /// <summary>
        /// Do color matching whenever the body texture is changed
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.CreateBodyTexture))]
        private static void CreateBodyTexture(ChaControl __instance)
        {
            var controller = UncensorSelector.GetController(__instance);
            if (controller != null)
                controller.UpdateSkinColor();
        }

        /// <summary>
        /// Postfix patch to check underwear clothing state and hide objDanTop when the clothes are on. Would be better as a transpiler.
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), "UpdateVisible")]
        private static void UpdateVisible(ChaControl __instance, bool ___drawSimple, bool ___confSon, List<bool> ___lstActive)
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
        private static void SetBodyBaseMaterial(ChaControl __instance)
        {
            var controller = UncensorSelector.GetController(__instance);
            if (controller != null)
                controller.UpdateSkinColor();
        }
#endif


#if KK || EC
        /// <summary>
        /// LineMask texture assigned to the material, toggled on and off for any color matching parts along with the body
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.VisibleAddBodyLine))]
        private static void VisibleAddBodyLine(ChaControl __instance)
        {
            var controller = UncensorSelector.GetController(__instance);
            if (controller != null)
                controller.UpdateSkinLine();
        }

        /// <summary>
        /// Skin gloss slider level, as assigned in the character maker. This corresponds to the red coloring in the DetailMask texture.
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingSkinGlossPower))]
        private static void ChangeSettingSkinGlossPower(ChaControl __instance)
        {
            var controller = UncensorSelector.GetController(__instance);
            if (controller != null)
                controller.UpdateSkinGloss();
        }
#endif

        /// <summary>
        /// Demosaic
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), "LateUpdateForce")]
        private static void LateUpdateForce(ChaControl __instance) => __instance.hideMoz = true;

        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.CreateBodyTexture))]
        private static void CreateBodyTexturePrefix(ChaControl __instance)
        {
            var controller = UncensorSelector.GetController(__instance);
            if (controller != null && controller.BodyData != null)
                UncensorSelector.CurrentBodyGUID = controller.BodyData.BodyGUID;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), "InitBaseCustomTextureBody")]
        private static void InitBaseCustomTextureBodyPrefix(ChaControl __instance)
        {
            var controller = UncensorSelector.GetController(__instance);
            if (controller != null && controller.BodyData != null)
                UncensorSelector.CurrentBodyGUID = controller.BodyData.BodyGUID;
        }

        /// <summary>
        /// Modifies the code for string replacement of oo_base, etc.
        /// </summary>
        [HarmonyTranspiler, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.CreateBodyTexture))]
        private static IEnumerable<CodeInstruction> CreateBodyTextureTranspiler(IEnumerable<CodeInstruction> instructions)
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

            return instructionsList;
        }

        /// <summary>
        /// Modifies the code for string replacement of mm_base, etc.
        /// </summary>
        [HarmonyTranspiler, HarmonyPatch(typeof(ChaControl), "InitBaseCustomTextureBody")]
        private static IEnumerable<CodeInstruction> InitBaseCustomTextureBodyTranspiler(IEnumerable<CodeInstruction> instructions)
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

            return instructionsList;
        }

#if KK
        /// <summary>
        /// Change the male _low asset to the female _low asset. Female has more bones so trying to change male body to female doesn't work. Load as female and change to male as a workaround.
        /// </summary>
        internal static IEnumerable<CodeInstruction> LoadAsyncTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();

            foreach (var x in instructionsList)
            {
                switch (x.operand?.ToString())
                {
                    case "p_cm_body_00_low":
                        x.opcode = OpCodes.Call;
                        x.operand = typeof(UncensorSelector).GetMethod(nameof(UncensorSelector.SetMaleBodyLow), AccessTools.all);
                        break;
                }
            }

            return instructionsList;
        }
#endif
    }
}