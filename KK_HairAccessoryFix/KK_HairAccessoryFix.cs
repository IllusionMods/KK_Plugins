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
        public const string Version = "1.0.3";

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

            MatchColor = new ConfigWrapper<bool>("MatchColor", PluginNameInternal, false);
            MatchOutlineColor = new ConfigWrapper<bool>("MatchOutlineColor", PluginNameInternal, true);
            MatchGloss = new ConfigWrapper<bool>("MatchGloss", PluginNameInternal, true);
        }

        private static void FixHairAccessory(ChaControl chaControl, int slotNo)
        {
            if (!MathfEx.RangeEqualOn<int>(0, slotNo, 19)) return;

            ChaAccessoryComponent chaAccessoryComponent = chaControl.cusAcsCmp[slotNo];
            if (chaAccessoryComponent?.rendNormal == null) return;

            ChaCustomHairComponent chaCustomHairComponent = chaAccessoryComponent.gameObject.GetComponent<ChaCustomHairComponent>();
            if (chaCustomHairComponent?.rendHair == null) return;

            Texture2D texHairGloss = (Texture2D)AccessTools.Property(typeof(ChaControl), "texHairGloss").GetValue(chaControl, null);
            Color baseColor = chaControl.chaFile.custom.hair.parts[0].baseColor;
            Color endColor = chaControl.chaFile.custom.hair.parts[0].endColor;
            Color startColor = chaControl.chaFile.custom.hair.parts[0].startColor;
            Color outlineColor = chaControl.chaFile.custom.hair.parts[0].outlineColor;
            Color acsColor = chaControl.chaFile.custom.hair.parts[0].acsColor[0];

            foreach (Renderer renderer in chaCustomHairComponent.rendHair)
            {
                if (renderer == null) continue;

                if (renderer.material.HasProperty("_Color") && MatchColor.Value)
                    renderer.material.SetColor("_Color", baseColor);

                if (renderer.material.HasProperty("_Color2") && MatchColor.Value)
                    renderer.material.SetColor("_Color2", startColor);

                if (renderer.material.HasProperty("_Color3") && MatchColor.Value)
                    renderer.material.SetColor("_Color3", endColor);

                if (renderer.material.HasProperty(ChaShader._HairGloss) && MatchGloss.Value)
                    renderer.material.SetTexture(ChaShader._HairGloss, texHairGloss);

                if (renderer.material.HasProperty(ChaShader._LineColor) && MatchOutlineColor.Value)
                    renderer.material.SetColor(ChaShader._LineColor, outlineColor);
            }

            foreach (Renderer renderer in chaCustomHairComponent.rendAccessory)
            {
                if (renderer == null) continue;

                if (renderer.material.HasProperty("_Color") && MatchColor.Value)
                    renderer.material.SetColor("_Color", acsColor);
            }
        }

        private static void FixHairAccessories(ChaControl chaControl)
        {
            for (int i = 0; i < 20; i++)
                FixHairAccessory(chaControl, i);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingHairGlossMask))]
        public static void ChangeSettingHairGlossMask(ChaControl __instance) => FixHairAccessories(__instance);
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingHairColor))]
        public static void ChangeSettingHairColor(ChaControl __instance) => FixHairAccessories(__instance);
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingHairOutlineColor))]
        public static void ChangeSettingHairOutlineColor(ChaControl __instance) => FixHairAccessories(__instance);
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingHairAcsColor))]
        public static void ChangeSettingHairAcsColor(ChaControl __instance) => FixHairAccessories(__instance);
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeAccessoryColor))]
        public static void ChangeAccessoryColor(ChaControl __instance, int slotNo) => FixHairAccessory(__instance, slotNo);
    }
}
