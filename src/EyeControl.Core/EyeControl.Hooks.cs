using HarmonyLib;
#if AI || HS2
using AIChara;
#endif
#if !EC
using Studio;
#endif

namespace KK_Plugins
{
    public static class Hooks
    {
        /// <summary>
        /// Apply hooks based on whether Studio is running. MainGameHooks would override Studio controls and so should not be patched in Studio
        /// </summary>
        internal static void ApplyHooks()
        {
            if (EyeControl.InsideStudio)
                Harmony.CreateAndPatchAll(typeof(StudioHooks));
            else
                Harmony.CreateAndPatchAll(typeof(MainGameHooks));
        }

        public static class MainGameHooks
        {
            [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeEyesOpenMax))]
            private static void ChaControl_ChangeEyesOpenMax(ChaControl __instance, ref float maxValue)
            {
                if (EyeControl.InsideStudio)
                    return;

                float eyeOpenMax = EyeControl.GetCharaController(__instance).EyeOpenMax;
                if (maxValue > eyeOpenMax)
                    maxValue = eyeOpenMax;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeEyesBlinkFlag))]
            private static void ChaControl_ChangeEyesBlinkFlag(ChaControl __instance, ref bool blink)
            {
                if (EyeControl.InsideStudio)
                    return;

                if (blink)
                    blink = !EyeControl.GetCharaController(__instance).DisableBlinking;
            }
        }

        public static class StudioHooks
        {
#if !EC
            [HarmonyPostfix, HarmonyPatch(typeof(AddObjectFemale), nameof(AddObjectFemale.Add), typeof(ChaControl), typeof(OICharInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int))]
            private static void AddObjectFemale_Add(ChaControl _female, bool _addInfo)
            {
                if (_addInfo)
                    EyeControl.GetCharaController(_female).OnCharacterAddedToScene();
            }

            [HarmonyPostfix, HarmonyPatch(typeof(AddObjectMale), nameof(AddObjectMale.Add), typeof(ChaControl), typeof(OICharInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int))]
            private static void AddObjectMale_Add(ChaControl _male, bool _addInfo)
            {
                if (_addInfo)
                    EyeControl.GetCharaController(_male).OnCharacterAddedToScene();
            }
#endif
        }
    }
}
