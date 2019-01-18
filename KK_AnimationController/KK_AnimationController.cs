using BepInEx;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using Harmony;
using MessagePack;
using Studio;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Logger = BepInEx.Logger;

namespace KK_AnimationController
{
    [BepInPlugin(GUID, PluginName, Version)]
    public class KK_AnimationController : BaseUnityPlugin
    {
        public const string PluginName = "KK_AnimationController";
        public const string GUID = "com.deathweasel.bepinex.animationcontroller";
        public const string Version = "1.0";
        private static bool LoadClicked = false;
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
            var harmony = HarmonyInstance.Create("com.deathweasel.bepinex.invisiblebody");
            harmony.PatchAll(typeof(KK_AnimationController));

            AnimationControllerHotkey = new SavedKeyboardShortcut(PluginName, PluginName, new KeyboardShortcut(KeyCode.Minus));
            ExtendedSave.SceneBeingSaved += ExtendedSceneSave;
            SceneManager.sceneLoaded += SceneLoaded;
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
                                IKObject.ObjectKey = objectCtrlInfo.objectInfo.dicKey;
                                IKObject.SelectedObject = Item.childRoot.gameObject;
                                break;
                            case OCIFolder Folder:
                                IKObject.ObjectKey = objectCtrlInfo.objectInfo.dicKey;
                                IKObject.SelectedObject = Folder.childRoot.gameObject;
                                break;
                            case OCIChar Character:
                                IKObject.CharacterKey = objectCtrlInfo.objectInfo.dicKey;
                                IKObject.CharacterObject = GameObject.Find(Character.charInfo.name);
                                IKObject.IKTarget = Character.listIKTarget.Where(x => x.boneObject.name == IKPart).First();
                                IKObject.IKPart = IKPart;
                                break;
                            case OCIRoute Route:
                                IKObject.ObjectKey = objectCtrlInfo.objectInfo.dicKey;
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

        [HarmonyPrefix, HarmonyPatch(typeof(SceneLoadScene), "OnClickLoad")]
        public static void OnClickLoadPrefix() => LoadClicked = true;
        /// <summary>
        /// Scene has to be fully loaded for all characters and objects to exist in the game
        /// </summary>
        private void SceneLoaded(Scene s, LoadSceneMode lsm)
        {
            if (s.name == "StudioNotification" && LoadClicked)
            {
                LoadClicked = false;

                try
                {
                    IKObjectInfoList.Clear();
                    PluginData ExtendedData = ExtendedSave.GetSceneExtendedDataById(PluginName);

                    if (ExtendedData != null && ExtendedData.data.ContainsKey("AnimationInfo"))
                    {
                        List<AnimationControllerInfo> AnimationControllerInfoList;

                        AnimationControllerInfoList = ((object[])ExtendedData.data["AnimationInfo"]).Select(x => AnimationControllerInfo.Unserialize((byte[])x)).ToList();

                        foreach (var AnimInfo in AnimationControllerInfoList)
                        {
                            IKObjectInfo LoadedAnimInfo = new IKObjectInfo();

                            var Character = Singleton<Studio.Studio>.Instance.dicObjectCtrl.Where(x => x.Key == AnimInfo.CharDicKey).Select(x => x.Value as OCIChar).First();

                            LoadedAnimInfo.CharacterKey = AnimInfo.CharDicKey;
                            LoadedAnimInfo.CharacterObject = GameObject.Find(Character.charInfo.name);
                            LoadedAnimInfo.IKPart = AnimInfo.IKPart;
                            LoadedAnimInfo.IKTarget = Character.listIKTarget.Where(x => x.boneObject.name == AnimInfo.IKPart).First();

                            var LinkedItem = Singleton<Studio.Studio>.Instance.dicObjectCtrl.Where(x => x.Key == AnimInfo.ItemDicKey).Select(x => x.Value).First();

                            switch (LinkedItem)
                            {
                                case OCIItem Item:
                                    LoadedAnimInfo.ObjectKey = Item.objectInfo.dicKey;
                                    LoadedAnimInfo.SelectedObject = Item.childRoot.gameObject;
                                    break;
                                case OCIFolder Folder:
                                    LoadedAnimInfo.ObjectKey = Folder.objectInfo.dicKey;
                                    LoadedAnimInfo.SelectedObject = Folder.childRoot.gameObject;
                                    break;
                                case OCIRoute Route:
                                    LoadedAnimInfo.ObjectKey = Route.objectInfo.dicKey;
                                    LoadedAnimInfo.SelectedObject = Route.childRoot.gameObject;
                                    break;
                            }

                            IKObjectInfoList.Add(LoadedAnimInfo);
                        }
                    }
                    Logger.Log(LogLevel.Debug, "Loaded KK_AnimationController animations");
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error | LogLevel.Message, "Could not load KK_AnimationController animations.");
                    Logger.Log(LogLevel.Error, ex.ToString());
                }
            }
        }

        private static void ExtendedSceneSave(string path)
        {
            try
            {
                Dictionary<string, object> ExtendedData = new Dictionary<string, object>();
                List<AnimationControllerInfo> AnimationControllerInfoList = new List<AnimationControllerInfo>();

                foreach (var IKObj in IKObjectInfoList)
                    AnimationControllerInfoList.Add(new AnimationControllerInfo { CharDicKey = IKObj.CharacterKey, ItemDicKey = IKObj.ObjectKey, IKPart = IKObj.IKPart });

                if (AnimationControllerInfoList.Count == 0)
                    ExtendedSave.SetSceneExtendedDataById(PluginName, null);
                else
                {
                    ExtendedData.Add("AnimationInfo", AnimationControllerInfoList.Select(x => x.Serialize()).ToList());
                    ExtendedSave.SetSceneExtendedDataById(PluginName, new PluginData { data = ExtendedData });
                }
                Logger.Log(LogLevel.Debug, "Saved KK_AnimationController animations");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error | LogLevel.Message, "Could not save KK_AnimationController animations.");
                Logger.Log(LogLevel.Error, ex.ToString());
            }
        }

        private class IKObjectInfo
        {
            public GameObject SelectedObject;
            public GameObject CharacterObject;
            public OCIChar.IKInfo IKTarget;
            public int CharacterKey;
            public int ObjectKey;
            public string IKPart;

            public bool CheckNull() => SelectedObject == null || CharacterObject == null || IKTarget == null ? true : false;
        }

        [Serializable]
        [MessagePackObject]
        public class AnimationControllerInfo
        {
            [Key("CharDicKey")]
            public int CharDicKey { get; set; }
            [Key("ItemDicKey")]
            public int ItemDicKey { get; set; }
            [Key("IKPart")]
            public string IKPart { get; set; }

            public static AnimationControllerInfo Unserialize(byte[] data)
            {
                return MessagePackSerializer.Deserialize<AnimationControllerInfo>(data);
            }

            public byte[] Serialize()
            {
                return MessagePackSerializer.Serialize(this);
            }
        }
    }
}
