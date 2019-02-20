using Harmony;
using KKAPI.Maker;
using KKAPI.Studio;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UniRx;
using UnityEngine;

namespace KK_UncensorSelector
{
    class Hooks
    {
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.CreateBodyTexture))]
        public static void CreateBodyTexturePrefix(ChaControl __instance) => KK_UncensorSelector.CurrentCharacter = __instance;
        public static void InitializePrefix(ChaControl __instance, ChaFileControl _chaFile)
        {
            KK_UncensorSelector.CurrentCharacter = __instance;
            KK_UncensorSelector.CurrentChaFile = _chaFile;
        }
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.LoadAsync))]
        public static void LoadAsyncPrefix(ChaControl __instance) => KK_UncensorSelector.CurrentCharacter = __instance;
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), "InitBaseCustomTextureBody")]
        public static void InitBaseCustomTextureBodyPrefix(ChaControl __instance) => KK_UncensorSelector.CurrentCharacter = __instance;
        /// <summary>
        /// Modifies the code for string replacement of oo_base, etc.
        /// </summary>
        public static IEnumerable<CodeInstruction> LoadAsyncTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();

            foreach (var x in instructionsList)
            {
                switch (x.operand?.ToString())
                {
                    case "chara/oo_base.unity3d":
                        x.opcode = OpCodes.Call;
                        x.operand = typeof(KK_UncensorSelector).GetMethod(nameof(KK_UncensorSelector.SetOOBase), AccessTools.all);
                        break;
                    case "p_cm_body_00":
                        x.opcode = OpCodes.Call;
                        x.operand = typeof(KK_UncensorSelector).GetMethod(nameof(KK_UncensorSelector.SetMaleBodyHigh), AccessTools.all);
                        break;
                    case "p_cm_body_00_low":
                        x.opcode = OpCodes.Call;
                        x.operand = typeof(KK_UncensorSelector).GetMethod(nameof(KK_UncensorSelector.SetMaleBodyLow), AccessTools.all);
                        break;
                    case "p_cf_body_00":
                        x.opcode = OpCodes.Call;
                        x.operand = typeof(KK_UncensorSelector).GetMethod(nameof(KK_UncensorSelector.SetFemaleBodyHigh), AccessTools.all);
                        break;
                    case "p_cf_body_00_low":
                        x.opcode = OpCodes.Call;
                        x.operand = typeof(KK_UncensorSelector).GetMethod(nameof(KK_UncensorSelector.SetFemaleBodyLow), AccessTools.all);
                        break;
                    case "p_cf_body_00_Nml":
                        x.opcode = OpCodes.Call;
                        x.operand = typeof(KK_UncensorSelector).GetMethod(nameof(KK_UncensorSelector.SetNormals), AccessTools.all);
                        break;
                }
            }

            return instructions;
        }
        /// <summary>
        /// Modifies the code for string replacement of oo_base, etc.
        /// </summary>
        [HarmonyTranspiler, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.CreateBodyTexture))]
        public static IEnumerable<CodeInstruction> CreateBodyTextureTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();

            foreach (var x in instructionsList)
            {
                switch (x.operand?.ToString())
                {
                    case "chara/oo_base.unity3d":
                        x.opcode = OpCodes.Call;
                        x.operand = typeof(KK_UncensorSelector).GetMethod(nameof(KK_UncensorSelector.SetOOBase), AccessTools.all);
                        break;
                    case "cf_body_00_t":
                        x.opcode = OpCodes.Call;
                        x.operand = typeof(KK_UncensorSelector).GetMethod(nameof(KK_UncensorSelector.SetBodyMainTex), AccessTools.all);
                        break;
                    case "cm_body_00_mc":
                        x.opcode = OpCodes.Call;
                        x.operand = typeof(KK_UncensorSelector).GetMethod(nameof(KK_UncensorSelector.SetBodyColorMaskMale), AccessTools.all);
                        break;
                    case "cf_body_00_mc":
                        x.opcode = OpCodes.Call;
                        x.operand = typeof(KK_UncensorSelector).GetMethod(nameof(KK_UncensorSelector.SetBodyColorMaskFemale), AccessTools.all);
                        break;
                }
            }

            return instructions;
        }
        /// <summary>
        /// Modifies the code for string replacement of mm_base, etc.
        /// </summary>
        [HarmonyTranspiler, HarmonyPatch(typeof(ChaControl), "InitBaseCustomTextureBody")]
        public static IEnumerable<CodeInstruction> InitBaseCustomTextureBodyTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();

            foreach (var x in instructionsList)
            {
                switch (x.operand?.ToString())
                {
                    case "chara/mm_base.unity3d":
                        x.opcode = OpCodes.Call;
                        x.operand = typeof(KK_UncensorSelector).GetMethod(nameof(KK_UncensorSelector.SetMMBase), AccessTools.all);
                        break;
                    case "cm_m_body":
                        x.opcode = OpCodes.Call;
                        x.operand = typeof(KK_UncensorSelector).GetMethod(nameof(KK_UncensorSelector.SetBodyMaterialMale), AccessTools.all);
                        break;
                    case "cf_m_body":
                        x.opcode = OpCodes.Call;
                        x.operand = typeof(KK_UncensorSelector).GetMethod(nameof(KK_UncensorSelector.SetBodyMaterialFemale), AccessTools.all);
                        break;
                    case "cf_m_body_create":
                        x.opcode = OpCodes.Call;
                        x.operand = typeof(KK_UncensorSelector).GetMethod(nameof(KK_UncensorSelector.SetBodyMaterialCreate), AccessTools.all);
                        break;
                }
            }

            return instructions;
        }
        /// <summary>
        /// Do color matching whenever the body texture is changed
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetBodyBaseMaterial))]
        public static void SetBodyBaseMaterial(ChaControl __instance) => KK_UncensorSelector.ColorMatchMaterials(__instance, KK_UncensorSelector.GetUncensorData(__instance));
        /// <summary>
        /// When a character is reloaded, update the uncensor as well since it may be due to a character being replaced in studio, for example.
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.Reload))]
        public static void Reload(ChaControl __instance, bool noChangeBody)
        {
            if (noChangeBody)
                return;

            if (MakerAPI.InsideAndLoaded && !KK_UncensorSelector.DoingForcedReload)
                return;

            KK_UncensorSelector.ReloadCharacterUncensor(__instance);
        }
        /// <summary>
        /// LineMask texture assigned to the material, toggled on and off for any color matching parts along with the body
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.VisibleAddBodyLine))]
        public static void VisibleAddBodyLine(ChaControl __instance)
        {
            KK_UncensorSelector.UncensorData uncensor = KK_UncensorSelector.GetUncensorData(__instance);
            if (uncensor == null)
                return;

            foreach (var colorMatchPart in uncensor.ColorMatchList)
            {
                FindAssist findAssist = new FindAssist();
                findAssist.Initialize(__instance.objBody.transform);
                GameObject gameObject = findAssist.GetObjectFromName(colorMatchPart.Object);
                if (gameObject != null)
                    gameObject.GetComponent<Renderer>().material.SetFloat(ChaShader._linetexon, __instance.chaFile.custom.body.drawAddLine ? 1f : 0f);
            }
        }
        /// <summary>
        /// Skin gloss slider level, as assigned in the character maker.
        /// This corresponds to the red coloring in the DetailMask texture.
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingSkinGlossPower))]
        public static void ChangeSettingSkinGlossPower(ChaControl __instance)
        {
            KK_UncensorSelector.UncensorData uncensor = KK_UncensorSelector.GetUncensorData(__instance);
            if (uncensor == null)
                return;

            foreach (var colorMatchPart in uncensor.ColorMatchList)
            {
                FindAssist findAssist = new FindAssist();
                findAssist.Initialize(__instance.objBody.transform);
                GameObject gameObject = findAssist.GetObjectFromName(colorMatchPart.Object);
                if (gameObject != null)
                    gameObject.GetComponent<Renderer>().material.SetFloat(ChaShader._SpecularPower, Mathf.Lerp(__instance.chaFile.custom.body.skinGlossPower, 1f, __instance.chaFile.status.skinTuyaRate));
            }
        }
        /// <summary>
        /// For traps and futas, set the normals for the chest area. This prevents strange shadowing around flat-chested trap/futa characters
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCustomBodyWithoutCustomTexture))]
        public static void ChangeCustomBodyWithoutCustomTexture(ChaControl __instance)
        {
            KK_UncensorSelector.UncensorData uncensor = KK_UncensorSelector.GetUncensorData(__instance);

            KK_UncensorSelector.SetChestNormals(__instance, uncensor);

            if (!StudioAPI.InsideStudio && !MakerAPI.InsideMaker && uncensor != null)
                __instance.fileStatus.visibleSonAlways = uncensor.ShowPenis;
        }
        /// <summary>
        /// Demosaic
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), "LateUpdateForce")]
        public static void LateUpdateForce(ChaControl __instance) => __instance.hideMoz = true;
    }
}
