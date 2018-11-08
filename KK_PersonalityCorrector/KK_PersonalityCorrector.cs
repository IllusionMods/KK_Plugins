using BepInEx;
using BepInEx.Logging;
using Harmony;
using System.Collections.Generic;
/// <summary>
/// Changes any story character personalities to the "Pure" personality to prevent the game from breaking when adding them to the class
/// </summary>
namespace KK_PersonalityCorrector
{
    [BepInPlugin("com.deathweasel.bepinex.personalitycorrector", "Personality Corrector", "1.0")]
    public class KK_PersonalityCorrector : BaseUnityPlugin
    {
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
                if (x.Value.parameter.personality == 80 ||
                    x.Value.parameter.personality == 81 ||
                    x.Value.parameter.personality == 82 ||
                    x.Value.parameter.personality == 83 ||
                    x.Value.parameter.personality == 84 ||
                    x.Value.parameter.personality == 85 ||
                    x.Value.parameter.personality == 86)
                    x.Value.parameter.personality = 8; //Pure
            }
        }
    }
}