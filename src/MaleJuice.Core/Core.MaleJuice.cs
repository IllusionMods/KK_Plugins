using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using KKAPI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if AI || HS2
using AIChara;
#endif

namespace KK_Plugins
{
    /// <summary>
    /// Enables juice textures for male characters
    /// </summary>
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class MaleJuice : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.malejuice";
        public const string PluginName = "Male Juice";
        public const string PluginNameInternal = Constants.Prefix + "_MaleJuice";
        public const string Version = "1.2.2";
        public static MaleJuice Instance;

#if KK
        private static Texture LiquidMask;
#else
        private static Material LiquidMat;
#endif

        internal static new ManualLogSource Logger;

        internal void Main()
        {
            Logger = base.Logger;
            Instance = this;

            StartCoroutine(LoadJuice());
        }

        private static IEnumerator LoadJuice()
        {
            yield return new WaitUntil(() => AssetBundleManager.ManifestBundlePack.Count != 0);

            try
            {
#if KK
                //Get the juice texture used by females
                var mat = CommonLib.LoadAsset<Material>("chara/mm_base.unity3d", "cf_m_body");
                LiquidMask = mat.GetTexture("_liquidmask");
#else
                //Get the juice material used by females
                LiquidMat = CommonLib.LoadAsset<Material>("chara/oo_base.unity3d", "c_m_liquid_body");
                LiquidMat.SetFloat("_WeatheringRange1", 0);
                LiquidMat.SetFloat("_WeatheringRange2", 0);
                LiquidMat.SetFloat("_WeatheringRange3", 0);
                LiquidMat.SetFloat("_WeatheringRange4", 0);
                LiquidMat.SetFloat("_WeatheringRange5", 0);
                LiquidMat.SetFloat("_WeatheringRange6", 0);
#endif
            }
            catch
            {
                Logger.LogError("Could not load juice textures.");
            }

#if KK
            if (LiquidMask == null) yield break;
#else
            if (LiquidMat == null) yield break;
#endif

            Harmony.CreateAndPatchAll(typeof(Hooks));
        }

        /// <summary>
        /// Sets the juice textures for any character lacking them, typically males
        /// </summary>
        public static void SetJuice(ChaControl chaControl)
        {
#if KK
            if (chaControl.customMatBody.GetTexture("_liquidmask") == null)
                chaControl.customMatBody.SetTexture("_liquidmask", LiquidMask);
#else
            if (chaControl.cmpBody.targetCustom.rendBody.sharedMaterials.Length == 1)
            {
                List<Material> newMats = new List<Material>();
                newMats.Add(chaControl.cmpBody.targetCustom.rendBody.sharedMaterials[0]);
                newMats.Add(Instantiate(LiquidMat));
                chaControl.cmpBody.targetCustom.rendBody.sharedMaterials = newMats.ToArray();

                //Needed for loading scenes in Studio
                chaControl.cmpBody.targetCustom.rendBody.sharedMaterials[1].SetFloat(ChaShader.siruFrontTop, chaControl.GetSiruFlag(ChaFileDefine.SiruParts.SiruFrontTop) / 2);
                chaControl.cmpBody.targetCustom.rendBody.sharedMaterials[1].SetFloat(ChaShader.siruFrontBot, chaControl.GetSiruFlag(ChaFileDefine.SiruParts.SiruFrontBot) / 2);
                chaControl.cmpBody.targetCustom.rendBody.sharedMaterials[1].SetFloat(ChaShader.siruBackTop, chaControl.GetSiruFlag(ChaFileDefine.SiruParts.SiruBackTop) / 2);
                chaControl.cmpBody.targetCustom.rendBody.sharedMaterials[1].SetFloat(ChaShader.siruBackBot, chaControl.GetSiruFlag(ChaFileDefine.SiruParts.SiruBackBot) / 2);
            }
#endif
        }
    }
}
