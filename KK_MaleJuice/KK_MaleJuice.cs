using BepInEx;
using Harmony;
using KKAPI;
using KKAPI.Chara;
using Studio;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

namespace KK_MaleJuice
{
    /// <summary>
    /// Enables juice textures for male characters
    /// </summary>
    [BepInPlugin(GUID, PluginName, Version)]
    public class KK_MaleJuice : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.malejuice";
        public const string PluginName = "Male Juice";
        public const string Version = "1.0";
        private static Texture LiquidMask = null;

        private void Main()
        {
            CharacterApi.RegisterExtraBehaviour<MaleJuiceCharaController>(GUID);
            HarmonyInstance.Create(GUID).PatchAll(typeof(KK_MaleJuice));

            //Get the juice texture used by females
            var mat = CommonLib.LoadAsset<Material>("chara/mm_base.unity3d", "cf_m_body");
            LiquidMask = mat.GetTexture("_liquidmask");
        }

        public class MaleJuiceCharaController : CharaCustomFunctionController
        {
            protected override void OnReload(GameMode currentGameMode, bool maintainState)
            {
                SetJuice();

                base.OnReload(currentGameMode, maintainState);
            }
            protected override void OnCardBeingSaved(GameMode currentGameMode) { }
            /// <summary>
            /// Sets the juice textures for any character lacking them, typically males
            /// </summary>
            public void SetJuice() => ChaControl.StartCoroutine(_SetJuice());
            private IEnumerator _SetJuice()
            {
                if (LiquidMask == null) yield break;
                if (ChaControl.customMatBody.GetTexture("_liquidmask") != null) yield break;

                yield return null;
                ChaControl.customMatBody.SetTexture("_liquidmask", LiquidMask);
            }
        }
        /// <summary>
        /// Get the MaleJuiceCharaController for a ChaControl
        /// </summary>
        private static MaleJuiceCharaController GetController(ChaControl character) => character?.gameObject?.GetComponent<MaleJuiceCharaController>();
        /// <summary>
        /// Set juice flags for males in Studio
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(OCIChar), nameof(OCIChar.SetSiruFlags))]
        public static void SetSiruFlags(ChaFileDefine.SiruParts _parts, byte _state, OCIChar __instance)
        {
            if (__instance is OCICharMale charMale)
                charMale.male.SetSiruFlags(_parts, _state);
        }
        /// <summary>
        /// Get juice flags for males in Studio
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(OCIChar), nameof(OCIChar.GetSiruFlags))]
        public static void GetSiruFlags(ChaFileDefine.SiruParts _parts, OCIChar __instance, ref byte __result)
        {
            if (__instance is OCICharMale charMale)
                __result = charMale.male.GetSiruFlags(_parts);
        }
        /// <summary>
        /// Remove the If and Else stuff so this method always enables juice texture selection in Studio
        /// </summary>
        [HarmonyTranspiler, HarmonyPatch(typeof(MPCharCtrl.LiquidInfo), nameof(MPCharCtrl.LiquidInfo.UpdateInfo))]
        public static IEnumerable<CodeInstruction> LiquidInfoUpdateInfoTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();

            int counter = 0;
            int ifStart = 0;
            int elseStart = 0;
            foreach (var x in instructionsList)
            {
                if (x.opcode == OpCodes.Bne_Un)
                    ifStart = counter - 4;
                if (x.opcode == OpCodes.Br)
                    elseStart = counter;

                counter++;
            }

            instructionsList.RemoveRange(elseStart, 4);
            instructionsList.RemoveRange(ifStart, 5);

            return instructionsList;
        }
        /// <summary>
        /// On H scene start, set juice textures
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(HSceneProc), "MapSameObjectDisable")]
        public static void MapSameObjectDisable(HSceneProc __instance)
        {
            foreach (var heroine in __instance.flags.lstHeroine)
                GetController(heroine.chaCtrl)?.SetJuice();
            GetController(__instance.flags.player.chaCtrl)?.SetJuice();
        }
    }
}
