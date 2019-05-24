using BepInEx;
using ExtensibleSaveFormat;
using Harmony;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

namespace KK_BodyShaders
{
    [BepInPlugin(GUID, PluginName, Version)]
    public class KK_BodyShaders : BaseUnityPlugin
    {
        public const string PluginName = "Body Shaders";
        public const string GUID = "com.deathweasel.bepinex.bodyshaders";
        public const string Version = "1.0";
        private static Shader sha;
        private static readonly HashSet<string> excluded = new HashSet<string> { "cf_O_eyeline", "cf_O_eyeline_low", "cf_O_mayuge", "cf_Ohitomi_L", "cf_Ohitomi_R", "cf_Ohitomi_L02", "cf_Ohitomi_R02", "cf_O_noseline", "cf_O_namida_L", "cf_O_namida_M", "cf_O_namida_S", "cf_O_gag_eye_00", "cf_O_gag_eye_01", "cf_O_gag_eye_02", "o_shadowcaster", "o_mnpa", "o_mnpb", "o_body_a" };
        private static MakerToggle GooToggle;
        private static MakerSlider AlphaSlider;
        private static bool DoEvents = true;

        private void Main()
        {
            sha = CommonLib.LoadAsset<Shader>("chara/goo.unity3d", "goo.shader");
            var harmony = HarmonyInstance.Create(GUID);
            harmony.PatchAll(typeof(KK_BodyShaders));

            CharacterApi.RegisterExtraBehaviour<BodyShadersCharaController>(GUID);
            MakerAPI.RegisterCustomSubCategories += MakerAPI_RegisterCustomSubCategories;
        }

        private void MakerAPI_RegisterCustomSubCategories(object sender, RegisterSubCategoriesEvent e)
        {
            GooToggle = e.AddControl(new MakerToggle(MakerConstants.Body.All, "Goo", this));
            GooToggle.ValueChanged.Subscribe(Observer.Create<bool>(delegate { if (DoEvents) GetController(MakerAPI.GetCharacterControl()).SetEnabled(GooToggle.Value); }));
            AlphaSlider = e.AddControl(new MakerSlider(MakerConstants.Body.All, "Alpha", 0.5f, 1f, 0.741f, this));
            AlphaSlider.ValueChanged.Subscribe(Observer.Create<float>(delegate { if (DoEvents) GetController(MakerAPI.GetCharacterControl()).SetAlpha(AlphaSlider.Value); }));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetBodyBaseMaterial))]
        public static void SetBodyBaseMaterial(ChaControl __instance) => GetController(__instance).SetAllMaterial();
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetFaceBaseMaterial))]
        public static void SetFaceBaseMaterial(ChaControl __instance) => GetController(__instance).SetAllMaterial();

        public static BodyShadersCharaController GetController(ChaControl character) => character?.gameObject?.GetComponent<BodyShadersCharaController>();

        public class BodyShadersCharaController : CharaCustomFunctionController
        {
            private readonly bool DoClothes = false;
            private readonly bool DoHair = true;
            private string ShaderName = "Goo";
            private float Alpha = 0.741f;
            private bool Enabled = false;

            protected override void OnCardBeingSaved(GameMode currentGameMode)
            {
                var data = new PluginData();
                data.data.Add("ShaderName", ShaderName);
                data.data.Add("Alpha", Alpha);
                data.data.Add("Enabled", Enabled);
                data.version = 2;
                SetExtendedData(data);
            }
            protected override void OnReload(GameMode currentGameMode, bool maintainState)
            {
                ShaderName = "Goo";
                Alpha = 0.741f;
                Enabled = false;
                DoEvents = false;

                var data = GetExtendedData();
                if (data != null)
                {
                    if (data.data.TryGetValue("ShaderName", out var loadedShaderName))
                        ShaderName = (string)loadedShaderName;
                    if (data.data.TryGetValue("Alpha", out var loadedAlpha) && loadedAlpha != null)
                        Alpha = (float)loadedAlpha;
                    if (data.data.TryGetValue("Enabled", out var loadedEnabled) && loadedEnabled != null)
                        Enabled = (bool)loadedEnabled;
                }

                if (MakerAPI.InsideAndLoaded)
                {
                    GooToggle.Value = Enabled;
                    AlphaSlider.Value = Alpha;
                }

                SetAllMaterial();
                DoEvents = true;

            }

            public void SetAlpha(float alpha)
            {
                Alpha = alpha;
                SetAllMaterial();
            }
            public void SetEnabled(bool enabled)
            {
                Enabled = enabled;
                ChaControl.Reload();
                SetAllMaterial();
            }
            public void SetAllMaterial()
            {
                if (Enabled)
                {
                    IterateMaterials(ChaControl.objTop);
                    SetMaterial(ChaControl.customTexCtrlBody.matDraw);
                }
            }

            private void IterateMaterials(GameObject go)
            {
                if (go == null) return;
                if (excluded.Contains(go.name)) return;
                if (go.GetComponent<ChaClothesComponent>() && DoClothes == false) return; //clothes
                if (go.GetComponent<ChaCustomHairComponent>() && go.GetComponent<ChaAccessoryComponent>() && DoHair == false) return; //hair accessory
                if (go.GetComponent<ChaCustomHairComponent>() && !go.GetComponent<ChaAccessoryComponent>() && DoHair == false) return; //hair
                if (!go.GetComponent<ChaCustomHairComponent>() && go.GetComponent<ChaAccessoryComponent>()) return; //accessory

                if (go.GetComponent<Renderer>())
                    SetMaterial(go.GetComponent<Renderer>().material);

                for (int i = 0; i < go.transform.childCount; i++)
                    IterateMaterials(go.transform.GetChild(i).gameObject);
            }
            private void SetMaterial(Material mat)
            {
                mat.shader = sha;
                mat.SetFloat("_FresnelScale", 1.2f);
                mat.SetFloat("_FresnelPower", 2.2f);
                mat.SetFloat("_FresnelAlpha", 0.385f);
                mat.SetFloat("_Sh", 0.456f);
                mat.SetFloat("_FresnelBias", 0.01f);
                mat.SetFloat("_Alpha", Alpha);
                mat.SetColor("_Albedo", ChaControl.chaFile.custom.body.skinMainColor);
                mat.renderQueue = 2500;
            }
        }
    }
}
