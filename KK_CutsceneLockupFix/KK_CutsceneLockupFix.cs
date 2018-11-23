using BepInEx;
using Harmony;
using UnityEngine;

/// <summary>
/// Copy/pasted code from some functions from Assembly-CSharp.dll with added null checks so cutscenes don't lock up when using certain hair mods
/// Will probably need to get updated if these functions ever change in a future patch
/// </summary>
namespace KK_CutsceneLockupFix
{
    [BepInPlugin("com.deathweasel.bepinex.cutscenelockupfix", "Cutscene Lockup Fix", Version)]
    public class KK_CutsceneLockupFix : BaseUnityPlugin
    {
        public const string Version = "1.0";

        public void Main()
        {
            var harmony = HarmonyInstance.Create("com.deathweasel.bepinex.cutscenelockupfix");
            harmony.PatchAll(typeof(KK_CutsceneLockupFix));
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingHairColor))]
        public static bool ChangeSettingHairColor(int parts, bool c00, bool c01, bool c02, ChaControl __instance)
        {
            ChaCustomHairComponent customHairComponent = __instance.GetCustomHairComponent(parts);
            if (null == customHairComponent || customHairComponent.rendHair == null || customHairComponent.rendHair.Length == 0)
            {
                return false;
            }
            ChaFileHair hair = __instance.chaFile.custom.hair;
            for (int i = 0; i < customHairComponent.rendHair.Length; i++)
            {
                if (c00)
                {
                    if (1f > hair.parts[parts].baseColor.a)
                    {
                        hair.parts[parts].baseColor = new Color(hair.parts[parts].baseColor.r, hair.parts[parts].baseColor.g, hair.parts[parts].baseColor.b, 1f);
                    }
                    if (customHairComponent.rendHair[i] != null) //Added null check
                    {
                        customHairComponent.rendHair[i].material.SetColor(ChaShader._Color, hair.parts[parts].baseColor);
                    }
                }
                if (c01)
                {
                    if (1f > hair.parts[parts].startColor.a)
                    {
                        hair.parts[parts].startColor = new Color(hair.parts[parts].startColor.r, hair.parts[parts].startColor.g, hair.parts[parts].startColor.b, 1f);
                    }
                    if (customHairComponent.rendHair[i] != null) //Added null check
                    {
                        customHairComponent.rendHair[i].material.SetColor(ChaShader._Color2, hair.parts[parts].startColor);
                    }
                }
                if (c02)
                {
                    if (1f > hair.parts[parts].endColor.a)
                    {
                        hair.parts[parts].endColor = new Color(hair.parts[parts].endColor.r, hair.parts[parts].endColor.g, hair.parts[parts].endColor.b, 1f);
                    }
                    if (customHairComponent.rendHair[i] != null) //Added null check
                    {
                        customHairComponent.rendHair[i].material.SetColor(ChaShader._Color3, hair.parts[parts].endColor);
                    }
                }
            }
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingHairOutlineColor))]
        public static bool ChangeSettingHairOutlineColor(int parts, ChaControl __instance)
        {
            ChaCustomHairComponent customHairComponent = __instance.GetCustomHairComponent(parts);
            if (null == customHairComponent || customHairComponent.rendHair == null || customHairComponent.rendHair.Length == 0)
            {
                return false;
            }
            ChaFileHair hair = __instance.chaFile.custom.hair;
            for (int i = 0; i < customHairComponent.rendHair.Length; i++)
            {
                if (customHairComponent.rendHair[i] != null) //Added null check
                {
                    customHairComponent.rendHair[i].material.SetColor(ChaShader._LineColor, hair.parts[parts].outlineColor);
                }
            }
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingHairAcsColor))]
        public static bool ChangeSettingHairAcsColor(int parts, ChaControl __instance)
        {
            int hairAcsColorNum = __instance.GetHairAcsColorNum(parts);
            if (hairAcsColorNum == 0)
            {
                return false;
            }
            ChaCustomHairComponent customHairComponent = __instance.GetCustomHairComponent(parts);
            if (null == customHairComponent)
            {
                return false;
            }
            int[] array = new int[]
            {
                ChaShader._Color,
                ChaShader._Color2,
                ChaShader._Color3
            };
            ChaFileHair hair = __instance.chaFile.custom.hair;
            for (int i = 0; i < customHairComponent.rendAccessory.Length; i++)
            {
                for (int j = 0; j < hairAcsColorNum; j++)
                {
                    if (1f > hair.parts[parts].acsColor[j].a)
                    {
                        hair.parts[parts].acsColor[j] = new Color(hair.parts[parts].acsColor[j].r, hair.parts[parts].acsColor[j].g, hair.parts[parts].acsColor[j].b, 1f);
                    }
                    if (customHairComponent.rendAccessory[i] != null) //Added null check
                    {
                        customHairComponent.rendAccessory[i].material.SetColor(array[j], hair.parts[parts].acsColor[j]);
                    }
                }
            }
            return false;
        }
    }
}
