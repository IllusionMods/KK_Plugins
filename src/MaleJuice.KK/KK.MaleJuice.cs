using BepInEx;
using BepInEx.Harmony;
using KKAPI;
using KKAPI.Chara;
using System.Collections;
using UnityEngine;

namespace KK_Plugins
{
    /// <summary>
    /// Enables juice textures for male characters
    /// </summary>
    [BepInDependency(KoikatuAPI.GUID)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class MaleJuice : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.malejuice";
        public const string PluginName = "Male Juice";
        public const string PluginNameInternal = "KK_MaleJuice";
        public const string Version = "1.1";
        private static Texture LiquidMask = null;

        internal void Main()
        {
            //Get the juice texture used by females
            try
            {
                var mat = CommonLib.LoadAsset<Material>("chara/mm_base.unity3d", "cf_m_body");
                LiquidMask = mat.GetTexture("_liquidmask");
            }
            catch
            {
                Logger.LogError($"Could not load juice textures.");
            }

            if (LiquidMask == null)
                return;

            CharacterApi.RegisterExtraBehaviour<MaleJuiceCharaController>(GUID);
            HarmonyWrapper.PatchAll(typeof(Hooks));
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
    }
}
