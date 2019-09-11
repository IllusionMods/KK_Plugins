using HarmonyLib;
using Studio;

namespace KK_Plugins
{
    public partial class FKIK
    {
        internal static class Hooks
        {
            [HarmonyPostfix, HarmonyPatch(typeof(MPCharCtrl), "OnClickRoot")]
            internal static void OnClckRootPostfix() => InitUI();

            /// <summary>
            /// Enable aimultaneous kinematics on pose load
            /// </summary>
            [HarmonyPostfix, HarmonyPatch(typeof(PauseCtrl.FileInfo), nameof(PauseCtrl.FileInfo.Apply))]
            internal static void Apply(PauseCtrl.FileInfo __instance, OCIChar _char)
            {
                if (__instance.enableFK && __instance.enableIK)
                    EnableFKIK(_char);
            }

            /// <summary>
            /// Enable aimultaneous kinematics on character load. Pass the FK/IK state to the postfix
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
            /// Enable aimultaneous kinematics on character load. Pass the FK/IK state to the postfix
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
        }
    }
}
