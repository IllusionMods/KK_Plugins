using BepInEx;
using Studio;
using UnityEngine;

namespace KK_Plugins
{
    [BepInPlugin(GUID, PluginName, Version)]
    public class KK_FKIK : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.fkik";
        public const string PluginName = "FK and IK";
        public const string Version = "0.1";

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                TreeNodeObject[] selectNodes = Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNodes;
                for (int i = 0; i < selectNodes.Length; i++)
                    if (Singleton<Studio.Studio>.Instance.dicInfo.TryGetValue(selectNodes[i], out ObjectCtrlInfo objectCtrlInfo))
                        if (objectCtrlInfo is OCIChar ociChar)
                        {
                            ociChar.oiCharInfo.enableIK = true;
                            ociChar.ActiveIK(OIBoneInfo.BoneGroup.Body, ociChar.oiCharInfo.activeIK[0], true);
                            ociChar.ActiveIK(OIBoneInfo.BoneGroup.RightLeg, ociChar.oiCharInfo.activeIK[1], true);
                            ociChar.ActiveIK(OIBoneInfo.BoneGroup.LeftLeg, ociChar.oiCharInfo.activeIK[2], true);
                            ociChar.ActiveIK(OIBoneInfo.BoneGroup.RightArm, ociChar.oiCharInfo.activeIK[3], true);
                            ociChar.ActiveIK(OIBoneInfo.BoneGroup.LeftArm, ociChar.oiCharInfo.activeIK[4], true);

                            ociChar.oiCharInfo.activeFK[3] = false;
                            ociChar.fkCtrl.enabled = true;
                            ociChar.oiCharInfo.enableFK = true;

                            for (int j = 0; j < FKCtrl.parts.Length; j++)
                                ociChar.ActiveFK(FKCtrl.parts[j], ociChar.oiCharInfo.activeFK[j], true);

                            ociChar.ActiveKinematicMode(OICharInfo.KinematicMode.IK, true, false);
                        }
            }
        }
    }
}
