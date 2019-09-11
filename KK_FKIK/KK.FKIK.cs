using BepInEx;
using BepInEx.Harmony;
using KKAPI.Studio;
using Studio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Studio.OIBoneInfo;

namespace KK_Plugins
{
    [BepInProcess("CharaStudio")]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class FKIK : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.fkik";
        public const string PluginName = "FK and IK";
        public const string Version = "1.0";

        internal void Main() => HarmonyWrapper.PatchAll(typeof(Hooks));

        /// <summary>
        /// Enable aimultaneous kinematics for the specified ChaControl
        /// </summary>
        public static void EnableFKIK(ChaControl chaControl) => EnableFKIK(StudioObjectExtensions.GetOCIChar(chaControl));

        /// <summary>
        /// Enable aimultaneous kinematics for the specified OCIChar
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

        /// <summary>
        /// Enable aimultaneous kinematics for all characters selected in the workspace
        /// </summary>
        internal static void EnableFKIK()
        {
            foreach (ObjectCtrlInfo objectCtrlInfo in Studio.Studio.Instance.treeNodeCtrl.selectObjectCtrl)
                if (objectCtrlInfo is OCIChar)
                    EnableFKIK((OCIChar)objectCtrlInfo);
        }

        /// <summary>
        /// Add the UI button
        /// </summary>
        internal static void InitUI()
        {
            Transform transform = Studio.Studio.Instance.gameObject.transform.Find("Canvas Main Menu/02_Manipulate/00_Chara/02_Kinematic/Viewport/Content");
            if (transform.Find("FKIK_Button") == null)
            {
                Transform transform2 = transform.Find("Pose");
                GameObject gameObject = Instantiate(transform2.gameObject);
                gameObject.name = "FKIK_Button";
                foreach (var x in gameObject.GetComponentsInChildren<TextMeshProUGUI>())
                    x.text = "FK&IK";

                gameObject.transform.SetParent(transform2.transform.parent);
                Button component2 = gameObject.GetComponent<Button>();
                component2.onClick = new Button.ButtonClickedEvent();
                component2.onClick.AddListener(delegate { EnableFKIK(); });
                gameObject.transform.localPosition = transform2.transform.localPosition - new Vector3(0f, 30f, 0f);
                gameObject.transform.localScale = Vector3.one;
            }
        }
    }
}
