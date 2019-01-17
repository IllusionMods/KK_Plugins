using BepInEx;
using BepInEx.Logging;
using Harmony;
using Studio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Logger = BepInEx.Logger;

namespace KK_AnimationController
{
    [BepInPlugin(GUID, PluginName, Version)]
    public class KK_AnimationController : BaseUnityPlugin
    {
        public const string PluginName = "KK_AnimationController";
        public const string GUID = "com.deathweasel.bepinex.animationcontroller";
        public const string Version = "1.0";
        public static SavedKeyboardShortcut AnimationControllerHotkey { get; private set; }

        private static List<IKObjectInfo> IKObjectInfoList = new List<IKObjectInfo>();

        public static readonly string[] IKParts = new string[] { "cf_j_hips", "cf_j_arm00_L", "cf_j_forearm01_L", "cf_j_hand_L", "cf_j_arm00_R", "cf_j_forearm01_R", "cf_j_hand_R", "cf_j_thigh00_L", "cf_j_leg01_L", "cf_j_leg03_L", "cf_j_thigh00_R", "cf_j_leg01_R", "cf_j_leg03_R" };
        private int IKPart = IKParts.Count() - 1;

        void Main()
        {
            AnimationControllerHotkey = new SavedKeyboardShortcut(PluginName, PluginName, new KeyboardShortcut(KeyCode.Minus));
        }

        private void Update()
        {
            //Control + key to remove the link between body part and object
            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(AnimationControllerHotkey.Value.MainKey))
            {
                TreeNodeObject[] selectNodes = Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNodes;
                for (int i = 0; i < selectNodes.Length; i++)
                {
                    if (Singleton<Studio.Studio>.Instance.dicInfo.TryGetValue(selectNodes[i], out ObjectCtrlInfo objectCtrlInfo))
                    {
                        if (objectCtrlInfo is OCIChar ociChar)
                        {
                            IKObjectInfoList.RemoveAll(x => x.CharacterObject == GameObject.Find(ociChar.charInfo.name) &&
                                                            x.IKTarget.boneObject.name == IKParts[IKPart]);
                        }
                    }
                }
            }
            //Shift + key to change the IK Node
            else if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Input.GetKeyDown(AnimationControllerHotkey.Value.MainKey))
            {
                if (IKPart == IKParts.Count() - 1)
                    IKPart = 0;
                else
                    IKPart++;
                Logger.Log(LogLevel.Info | LogLevel.Message, $"Selected IK Node:{IKParts[IKPart]}");
            }
            //Press key to link the selected character with the selected object (select both in workspace first)
            else if (AnimationControllerHotkey.IsDown())
            {
                IKObjectInfo IKObject = new IKObjectInfo();
                TreeNodeObject[] selectNodes = Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNodes;

                for (int i = 0; i < selectNodes.Length; i++)
                {
                    if (Singleton<Studio.Studio>.Instance.dicInfo.TryGetValue(selectNodes[i], out ObjectCtrlInfo objectCtrlInfo))
                    {
                        if (objectCtrlInfo is OCIItem ociItem)
                        {
                            IKObject.SelectedObject = ociItem.objectItem;
                        }

                        if (objectCtrlInfo is OCIChar ociChar)
                        {
                            IKObject.CharacterObject = GameObject.Find(ociChar.charInfo.name);
                            IKObject.IKTarget = ociChar.listIKTarget.Where(x => x.boneObject.name == IKParts[IKPart]).First();
                        }
                    }
                }

                if (IKObject.SelectedObject != null && IKObject.CharacterObject != null && IKObject.IKTarget != null)
                {
                    IKObjectInfoList.RemoveAll(x => x.CharacterObject == IKObject.CharacterObject &&
                                                    x.IKTarget == IKObject.IKTarget);
                    IKObjectInfoList.Add(IKObject);
                }
            }

            //Every update, match the part to the object
            for (int i = 0; i < IKObjectInfoList.Count;)
            {
                if (IKObjectInfoList[i].CheckNull())
                    IKObjectInfoList.RemoveAt(i);
                else
                {
                    IKObjectInfoList[i].IKTarget.targetInfo.changeAmount.pos = IKObjectInfoList[i].SelectedObject.transform.position - IKObjectInfoList[i].CharacterObject.transform.position;
                    i++;
                }
            }
        }

        private class IKObjectInfo
        {
            public GameObject SelectedObject;
            public GameObject CharacterObject;
            public OCIChar.IKInfo IKTarget;

            public bool CheckNull() => SelectedObject == null || CharacterObject == null || IKTarget == null ? true : false;
        }
    }
}
