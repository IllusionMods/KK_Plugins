using BepInEx;
using BepInEx.Harmony;
using HarmonyLib;
using KKAPI;
using KKAPI.Chara;
using Studio;
using System.Collections;
using UnityEngine;

namespace KK_Plugins
{
    /// <summary>
    /// Enables juice textures for male characters
    /// </summary>
    [BepInDependency(KoikatuAPI.GUID)]
    [BepInPlugin(GUID, PluginName, Version)]
    public class KK_MaleJuice : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.malejuice";
        public const string PluginName = "Male Juice";
        public const string Version = "1.1";
        private static Texture LiquidMask = null;

        private void Main()
        {
            //Get the juice texture used by females
            try
            {
                var mat = CommonLib.LoadAsset<Material>("chara/mm_base.unity3d", "cf_m_body");
                LiquidMask = mat.GetTexture("_liquidmask");
            }
            catch
            {
                Logger.LogError($"[{nameof(KK_MaleJuice)}] Could not load juice textures.");
            }

            if (LiquidMask == null)
                return;

            CharacterApi.RegisterExtraBehaviour<MaleJuiceCharaController>(GUID);
            HarmonyWrapper.PatchAll(typeof(KK_MaleJuice));
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
                yield return null;

                if (ChaControl.customMatBody.GetTexture("_liquidmask") == null)
                    ChaControl.customMatBody.SetTexture("_liquidmask", LiquidMask);
            }
        }
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
        /// Enable the juice section in Studio always, not just for females
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(MPCharCtrl.LiquidInfo), nameof(MPCharCtrl.LiquidInfo.UpdateInfo))]
        public static void LiquidInfoUpdateInfo(OCIChar _char, MPCharCtrl.LiquidInfo __instance)
        {
            __instance.active = true;
            __instance.face.select = _char.GetSiruFlags(ChaFileDefine.SiruParts.SiruKao);
            __instance.breast.select = _char.GetSiruFlags(ChaFileDefine.SiruParts.SiruFrontUp);
            __instance.back.select = _char.GetSiruFlags(ChaFileDefine.SiruParts.SiruBackUp);
            __instance.belly.select = _char.GetSiruFlags(ChaFileDefine.SiruParts.SiruFrontDown);
            __instance.hip.select = _char.GetSiruFlags(ChaFileDefine.SiruParts.SiruBackDown);
        }
    }
}
