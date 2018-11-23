using BepInEx;
using Harmony;
using System.Collections.Generic;
/// <summary>
/// Changes any story character personalities to the "Pure" personality to prevent the game from breaking when adding them to the class
/// </summary>
namespace KK_PersonalityCorrector
{
    [BepInPlugin("com.deathweasel.bepinex.personalitycorrector", "Personality Corrector", Version)]
    public class KK_PersonalityCorrector : BaseUnityPlugin
    {
        public const string Version = "1.1";
        public static int DefaultPersonality = 8; //Pure

        void Main()
        {
            var harmony = HarmonyInstance.Create("com.deathweasel.bepinex.personalitycorrector");
            harmony.PatchAll(typeof(KK_PersonalityCorrector));
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionGame.ClassRoomCharaFile), nameof(ActionGame.ClassRoomCharaFile.InitializeList))]
        public static void InitializeList(ActionGame.ClassRoomCharaFile __instance)
        {
            Dictionary<int, ChaFileControl> chaFileDic = Traverse.Create(__instance).Field("chaFileDic").GetValue<Dictionary<int, ChaFileControl>>();

            foreach (var x in chaFileDic)
            {
                switch (x.Value.parameter.personality)
                {
                    case 30: //0727 DLC
                        if (!AssetBundleCheck.IsFile("etcetra/list/config/14.unity3d"))
                            x.Value.parameter.personality = DefaultPersonality;
                        break;
                    case 31: //0727 Paid DLC
                        if (!AssetBundleCheck.IsFile("etcetra/list/config/15.unity3d"))
                            x.Value.parameter.personality = DefaultPersonality;
                        break;
                    case 32: //0727 Paid DLC
                        if (!AssetBundleCheck.IsFile("etcetra/list/config/16.unity3d"))
                            x.Value.parameter.personality = DefaultPersonality;
                        break;
                    case 33: //0727 Paid DLC
                        if (!AssetBundleCheck.IsFile("etcetra/list/config/17.unity3d"))
                            x.Value.parameter.personality = DefaultPersonality;
                        break;
                    case 80:
                    case 81:
                    case 82:
                    case 83:
                    case 84:
                    case 85:
                    case 86: //Story character personalities added by a mod
                        x.Value.parameter.personality = DefaultPersonality;
                        break;
                }
            }
        }
    }
}