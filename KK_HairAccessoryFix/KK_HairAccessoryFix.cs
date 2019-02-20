using BepInEx;
using Harmony;
using KKAPI.Maker;
using System.ComponentModel;
using UnityEngine;
/// <summary>
/// Match color, outline color, and hair gloss for hair accessories
/// </summary>
namespace KK_HairAccessoryFix
{
    [BepInPlugin(GUID, PluginName, Version)]
    public class KK_HairAccessoryFix : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.hairaccessoryfix";
        public const string PluginName = "Hair Accessory Fix";
        public const string PluginNameInternal = "KK_HairAccessoryFix";
        public const string Version = "0.1";

        [DisplayName("Color match")]
        [Category("Config")]
        [Description("Match hair accessory color to hair color when possible.")]
        public static ConfigWrapper<bool> MatchColor { get; private set; }
        [DisplayName("Outline color match")]
        [Category("Config")]
        [Description("Match hair accessory outline color to hair outline color when possible.")]
        public static ConfigWrapper<bool> MatchOutlineColor { get; private set; }
        [DisplayName("Hair gloss match")]
        [Category("Config")]
        [Description("Match hair accessory gloss when possible.")]
        public static ConfigWrapper<bool> MatchGloss { get; private set; }

        void Main()
        {
            var harmony = HarmonyInstance.Create(GUID);
            harmony.PatchAll(typeof(KK_HairAccessoryFix));

            MatchColor = new ConfigWrapper<bool>("MatchColor", PluginNameInternal, true);
            MatchOutlineColor = new ConfigWrapper<bool>("MatchOutlineColor", PluginNameInternal, true);
            MatchGloss = new ConfigWrapper<bool>("MatchGloss", PluginNameInternal, true);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha5) && MakerAPI.InsideAndLoaded)
                FixHairAccessories(MakerAPI.GetCharacterControl());
        }

        private static void FixHairAccessories(ChaControl chaControl)
        {
            try
            {
                if (chaControl?.chaFile?.custom?.hair.parts[0] == null)
                    return;

                Texture2D texHairGloss = (Texture2D)AccessTools.Property(typeof(ChaControl), "texHairGloss").GetValue(chaControl, null);
                foreach (ChaCustomHairComponent x in chaControl.GetComponentsInChildren<ChaCustomHairComponent>(true))
                {

                    Color baseColor = chaControl.chaFile.custom.hair.parts[0].baseColor;
                    Color endColor = chaControl.chaFile.custom.hair.parts[0].endColor;
                    Color startColor = chaControl.chaFile.custom.hair.parts[0].startColor;
                    Color outlineColor = chaControl.chaFile.custom.hair.parts[0].outlineColor;

                    foreach (Renderer y in x.rendHair)
                    {
                        Material material = y.material;

                        if (material.HasProperty("_Color") && MatchColor.Value)
                            material.SetColor("_Color", baseColor);

                        if (material.HasProperty("_Color2") && MatchColor.Value)
                            material.SetColor("_Color2", startColor);

                        if (material.HasProperty("_Color3") && MatchColor.Value)
                            material.SetColor("_Color3", endColor);

                        if (material.HasProperty(ChaShader._HairGloss) && MatchGloss.Value)
                            material.SetTexture(ChaShader._HairGloss, texHairGloss);

                        if (material.HasProperty(ChaShader._LineColor) && MatchOutlineColor.Value)
                            material.SetColor(ChaShader._LineColor, outlineColor);
                    }
                }
            }
            catch { }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingHairGlossMask))]
        public static void ChangeSettingHairGlossMask(ChaControl __instance) => FixHairAccessories(__instance);
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingHairColor))]
        public static void ChangeSettingHairColor(ChaControl __instance) => FixHairAccessories(__instance);
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingHairOutlineColor))]
        public static void ChangeSettingHairOutlineColor(ChaControl __instance) => FixHairAccessories(__instance);
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.Reload))]
        public static void Reload(ChaControl __instance) => FixHairAccessories(__instance);
    }
}
