using BepInEx;
using BepInEx.Harmony;
using BepInEx.Logging;
using HarmonyLib;
using KKAPI.Studio;
using Studio;
using static Studio.OIBoneInfo;

namespace KK_Plugins
{
    [BepInProcess("CharaStudio")]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class FKIK : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.fkik";
        public const string PluginName = "FK and IK";
        public const string Version = "1.1";
        internal static new ManualLogSource Logger;
        internal static FKIK Instance;

        internal void Main()
        {
            Instance = this;
            Logger = base.Logger;
            var harmony = HarmonyWrapper.PatchAll(typeof(Hooks));
            harmony.Patch(typeof(MPCharCtrl).GetNestedType("IKInfo", AccessTools.all).GetMethod("Init"), null, new HarmonyMethod(typeof(UI).GetMethod(nameof(UI.InitUI), AccessTools.all)));
            harmony.Patch(typeof(MPCharCtrl).GetNestedType("IKInfo", AccessTools.all).GetMethod("UpdateInfo"), null, new HarmonyMethod(typeof(UI).GetMethod(nameof(UI.UpdateUI), AccessTools.all)));
        }

        /// <summary>
        /// Enable simultaneous kinematics for the specified ChaControl
        /// </summary>
        public static void EnableFKIK(ChaControl chaControl) => EnableFKIK(StudioObjectExtensions.GetOCIChar(chaControl));

        /// <summary>
        /// Enable simultaneous kinematics for the specified OCIChar
        /// </summary>
        public static void EnableFKIK(OCIChar ociChar)
        {
            ociChar.oiCharInfo.enableIK = true;
            ociChar.ActiveIK(BoneGroup.Body, ociChar.oiCharInfo.activeIK[0], true);
            ociChar.ActiveIK(BoneGroup.RightLeg, ociChar.oiCharInfo.activeIK[1], true);
            ociChar.ActiveIK(BoneGroup.LeftLeg, ociChar.oiCharInfo.activeIK[2], true);
            ociChar.ActiveIK(BoneGroup.RightArm, ociChar.oiCharInfo.activeIK[3], true);
            ociChar.ActiveIK(BoneGroup.LeftArm, ociChar.oiCharInfo.activeIK[4], true);
            ociChar.ActiveKinematicMode(OICharInfo.KinematicMode.IK, true, true);

            ociChar.oiCharInfo.activeFK[3] = false;
            ociChar.fkCtrl.enabled = true;
            ociChar.oiCharInfo.enableFK = true;

            for (int j = 0; j < FKCtrl.parts.Length; j++)
                ociChar.ActiveFK(FKCtrl.parts[j], ociChar.oiCharInfo.activeFK[j], true);
        }

        internal static void DisableFKIK(OCIChar ociChar)
        {
            ociChar.oiCharInfo.enableIK = false;
            ociChar.oiCharInfo.enableFK = false;
            ociChar.finalIK.enabled = true;
            ociChar.ActiveKinematicMode(OICharInfo.KinematicMode.IK, false, true);
            ociChar.ActiveKinematicMode(OICharInfo.KinematicMode.FK, false, true);
        }

        internal static void ToggleFKIK(bool toggle)
        {
            if (Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNodes.Length != 1)
                return;

            TreeNodeObject[] selectNodes = Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNodes;

            for (int i = 0; i < selectNodes.Length; i++)
                if (Singleton<Studio.Studio>.Instance.dicInfo.TryGetValue(selectNodes[i], out ObjectCtrlInfo objectCtrlInfo))
                    if (objectCtrlInfo is OCIChar ociChar)
                        if (toggle)
                            EnableFKIK(ociChar);
                        else
                            DisableFKIK(ociChar);
        }
    }
}
