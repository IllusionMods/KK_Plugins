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

        private string IKPart = "";
        public static readonly Dictionary<string, string> IKParts = new Dictionary<string, string>
        {
            ["cf_j_arm00_L"] = "Left arm",
            ["cf_j_forearm01_L"] = "Left forearm",
            ["cf_j_hand_L"] = "Left hand",
            ["cf_j_arm00_R"] = "Right arm",
            ["cf_j_forearm01_R"] = "Right forearm",
            ["cf_j_hand_R"] = "Right hand",
            ["cf_j_hips"] = "Hips",
            ["cf_j_thigh00_L"] = "Left thigh",
            ["cf_j_leg01_L"] = "Left knee",
            ["cf_j_leg03_L"] = "Left foot",
            ["cf_j_thigh00_R"] = "Right thigh",
            ["cf_j_leg01_R"] = "Right knee",
            ["cf_j_leg03_R"] = "Right foot"
        };

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
                                                            x.IKTarget.boneObject.name == IKPart);
                        }
                    }
                }
            }
            //Shift + key to change the IK Node
            else if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Input.GetKeyDown(AnimationControllerHotkey.Value.MainKey))
            {
                if (IKPart == "")
                    IKPart = IKParts.ElementAt(0).Key;
                else
                {
                    int index = Array.IndexOf(IKParts.Keys.ToArray(), IKPart);
                    if (index == IKParts.Count - 1)
                        IKPart = IKParts.ElementAt(0).Key;
                    else
                        IKPart = IKParts.ElementAt(index + 1).Key;
                }
                Logger.Log(LogLevel.Info | LogLevel.Message, $"Selected IK Node:{IKParts[IKPart]}");
            }
            //Press key to link the selected character with the selected object (select both in workspace first)
            else if (AnimationControllerHotkey.IsDown())
            {
                if (IKPart == "")
                    Logger.Log(LogLevel.Info | LogLevel.Message, "Set an IK part first (shift+hotkey)");

                IKObjectInfo IKObject = new IKObjectInfo();
                TreeNodeObject[] selectNodes = Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNodes;

                for (int i = 0; i < selectNodes.Length; i++)
                {
                    if (Singleton<Studio.Studio>.Instance.dicInfo.TryGetValue(selectNodes[i], out ObjectCtrlInfo objectCtrlInfo))
                    {
                        switch (objectCtrlInfo)
                        {
                            case OCIItem Item:
                                IKObject.SelectedObject = Item.childRoot.gameObject;
                                break;
                            case OCIFolder Folder:
                                IKObject.SelectedObject = Folder.childRoot.gameObject;
                                break;
                            case OCIChar Char:
                                IKObject.CharacterObject = GameObject.Find(Char.charInfo.name);
                                IKObject.IKTarget = Char.listIKTarget.Where(x => x.boneObject.name == IKPart).First();
                                break;
                            case OCIRoute Route:
                                IKObject.SelectedObject = Route.childRoot.gameObject;
                                break;
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
                    //The character or the object it was attached to was deleted, remove it from the list
                    IKObjectInfoList.RemoveAt(i);
                else
                {
                    IKObjectInfoList[i].IKTarget.guideObject.SetWorldPos(IKObjectInfoList[i].SelectedObject.transform.position);
                    IKObjectInfoList[i].IKTarget.targetInfo.changeAmount.rot = IKObjectInfoList[i].SelectedObject.transform.localRotation.eulerAngles;
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
