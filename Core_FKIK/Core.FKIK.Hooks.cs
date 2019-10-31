using HarmonyLib;
using Studio;
using System.Collections;
#if AI
using AIChara;
#endif

namespace KK_Plugins
{
    public partial class FKIK
    {
        internal static class Hooks
        {
            private static bool ChangingChara = false;

            /// <summary>
            /// Enable simultaneous kinematics on pose load
            /// </summary>
            [HarmonyPostfix, HarmonyPatch(typeof(PauseCtrl.FileInfo), nameof(PauseCtrl.FileInfo.Apply))]
            internal static void Apply(PauseCtrl.FileInfo __instance, OCIChar _char)
            {
                if (__instance.enableFK && __instance.enableIK)
                    EnableFKIK(_char);
            }

            /// <summary>
            /// Enable simultaneous kinematics on character load. Pass the FK/IK state to the postfix
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(AddObjectFemale), nameof(AddObjectFemale.Add), new[] { typeof(ChaControl), typeof(OICharInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int) })]
            internal static void AddObjectFemalePrefix(OICharInfo _info, ref bool __state) => __state = _info.enableFK && _info.enableIK;

            /// <summary>
            /// FK/IK state has been overwritten, check against the FK/IK state from prefix
            /// </summary>
            [HarmonyPostfix, HarmonyPatch(typeof(AddObjectFemale), nameof(AddObjectFemale.Add), new[] { typeof(ChaControl), typeof(OICharInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int) })]
            internal static void AddObjectFemalePostfix(ChaControl _female, ref bool __state)
            {
                if (__state)
                    EnableFKIK(_female);
            }

            /// <summary>
            /// Enable simultaneous kinematics on character load. Pass the FK/IK state to the postfix
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(AddObjectMale), nameof(AddObjectMale.Add), new[] { typeof(ChaControl), typeof(OICharInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int) })]
            internal static void AddObjectMalePrefix(OICharInfo _info, ref bool __state) => __state = _info.enableFK && _info.enableIK;

            /// <summary>
            /// FK/IK state has been overwritten, check against the FK/IK state from prefix
            /// </summary>
            [HarmonyPostfix, HarmonyPatch(typeof(AddObjectMale), nameof(AddObjectMale.Add), new[] { typeof(ChaControl), typeof(OICharInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int) })]
            internal static void AddObjectMalePostfix(ChaControl _male, ref bool __state)
            {
                if (__state)
                    EnableFKIK(_male);
            }

            /// <summary>
            /// Set a flag when changing characters in Studio
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(OCIChar), nameof(OCIChar.ChangeChara))]
            internal static void ActiveKinematicMode() => ChangingChara = true;

            /// <summary>
            /// Enable simultaneous kinematics on character change. Pass the FK/IK state to the postfix
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(OCIChar), nameof(OCIChar.ActiveKinematicMode))]
            internal static void ActiveKinematicModePrefix(OCIChar __instance, ref bool __state) => __state = __instance.oiCharInfo.enableFK && __instance.oiCharInfo.enableIK;

            /// <summary>
            /// FK/IK state has been overwritten, check against the FK/IK state from prefix
            /// </summary>
            [HarmonyPostfix, HarmonyPatch(typeof(OCIChar), nameof(OCIChar.ActiveKinematicMode))]
            internal static void ActiveKinematicModePostfix(OCIChar __instance, ref bool __state)
            {

                if (__state && ChangingChara)
                    Instance.StartCoroutine(EnableFKIKCoroutine(__instance));
            }

            private static IEnumerator EnableFKIKCoroutine(OCIChar ociChar)
            {
                yield return null;
                ChangingChara = false;
                EnableFKIK(ociChar);
            }
        }
    }
}
