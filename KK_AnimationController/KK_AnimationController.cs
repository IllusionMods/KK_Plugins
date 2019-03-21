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
    /// <summary>
    /// Allows attaching IK nodes to objects to create custom animations
    /// </summary>
    [BepInProcess("CharaStudio")]
    [BepInPlugin(GUID, PluginName, Version)]
    public class KK_AnimationController : BaseUnityPlugin
    {
        public const string PluginName = "KK_AnimationController";
        public const string GUID = "com.deathweasel.bepinex.animationcontroller";
        public const string Version = "1.2";
        private static bool LoadClicked = false;
        public static SavedKeyboardShortcut AnimationControllerHotkey { get; private set; }
        private static List<IKObjectInfo> IKObjectInfoList = new List<IKObjectInfo>();
        private bool GUIVisible = false;

        int SelectedGuideObject = 0;
        static readonly string[] IKGuideObjectsPretty = new string[] { "Hips", "Left arm", "Left forearm", "Left hand", "Right arm", "Right forearm", "Right hand", "Left thigh", "Left knee", "Left foot", "Right thigh", "Right knee", "Right foot", "Eyes", "Neck" };
        static readonly string[] IKGuideObjects = new string[] { "cf_j_hips", "cf_j_arm00_L", "cf_j_forearm01_L", "cf_j_hand_L", "cf_j_arm00_R", "cf_j_forearm01_R", "cf_j_hand_R", "cf_j_thigh00_L", "cf_j_leg01_L", "cf_j_leg03_L", "cf_j_thigh00_R", "cf_j_leg01_R", "cf_j_leg03_R", "eyes", "neck" };

        void Main()
        {
            var harmony = HarmonyInstance.Create("com.deathweasel.bepinex.animationcontroller");
            harmony.PatchAll(typeof(KK_AnimationController));

            AnimationControllerHotkey = new SavedKeyboardShortcut(PluginName, PluginName, new KeyboardShortcut(KeyCode.Minus));
            ExtendedSave.SceneBeingSaved += ExtendedSceneSave;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        private void Update()
        {
            if (AnimationControllerHotkey.IsDown())
                GUIVisible = !GUIVisible;

            //Every update, match the part to the object
            for (int i = 0; i < IKObjectInfoList.Count;)
            {
                if (IKObjectInfoList[i].CheckNull())
                    //The character or the object it was attached to was deleted, remove it from the list
                    IKObjectInfoList.RemoveAt(i);
                else
                {
                    if (IKObjectInfoList[i].LinkType == LinkType.Eyes)
                        IKObjectInfoList[i].OCICharacter.charInfo.eyeLookCtrl.target = IKObjectInfoList[i].SelectedObject.transform;
                    else if (IKObjectInfoList[i].LinkType == LinkType.Neck)
                        IKObjectInfoList[i].OCICharacter.charInfo.neckLookCtrl.target = IKObjectInfoList[i].SelectedObject.transform;
                    else
                    {
                        IKObjectInfoList[i].IKTarget.guideObject.SetWorldPos(IKObjectInfoList[i].SelectedObject.transform.position);
                        if (IKObjectInfoList[i].Version == "1.0")
                            //Original version used the wrong rotation, keep using it so that scenes load properly
                            IKObjectInfoList[i].IKTarget.targetInfo.changeAmount.rot = IKObjectInfoList[i].SelectedObject.transform.localRotation.eulerAngles;
                        else
                        {
                            //Set the rotation of the linked body part to the rotation of the object
                            IKObjectInfoList[i].IKTarget.targetObject.rotation = IKObjectInfoList[i].SelectedObject.transform.rotation;
                            //Update the guide object cache so that unlinking and then rotating manually doesn't cause strange behavior
                            IKObjectInfoList[i].IKTarget.guideObject.changeAmount.rot = IKObjectInfoList[i].IKTarget.targetObject.localRotation.eulerAngles;
                        }
                    }

                    i++;
                }
            }
        }

        private void LinkCharacterToObject()
        {
            IKObjectInfo IKObject = new IKObjectInfo();
            TreeNodeObject[] selectNodes = Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNodes;

            if (selectNodes.Count() != 2)
            {
                Logger.Log(LogLevel.Info | LogLevel.Message, "Select both the character and object to link.");
                return;
            }

            for (int i = 0; i < selectNodes.Length; i++)
            {
                if (Singleton<Studio.Studio>.Instance.dicInfo.TryGetValue(selectNodes[i], out ObjectCtrlInfo objectCtrlInfo))
                {
                    switch (objectCtrlInfo)
                    {
                        case OCIChar Character:
                            IKObject.CharacterKey = objectCtrlInfo.objectInfo.dicKey;
                            IKObject.OCICharacter = Character;
                            if (IKGuideObjects[SelectedGuideObject] == "eyes")
                                IKObject.LinkType = LinkType.Eyes;
                            else if (IKGuideObjects[SelectedGuideObject] == "neck")
                                IKObject.LinkType = LinkType.Neck;
                            else
                            {
                                IKObject.LinkType = LinkType.GuideObject;
                                IKObject.IKTarget = Character.listIKTarget.Where(x => x.boneObject.name == IKGuideObjects[SelectedGuideObject]).First();
                                IKObject.IKPart = IKGuideObjects[SelectedGuideObject];
                            }
                            break;
                        case OCIItem Item:
                            IKObject.ObjectKey = objectCtrlInfo.objectInfo.dicKey;
                            IKObject.SelectedObject = Item.childRoot.gameObject;
                            break;
                        case OCIFolder Folder:
                            IKObject.ObjectKey = objectCtrlInfo.objectInfo.dicKey;
                            IKObject.SelectedObject = Folder.childRoot.gameObject;
                            break;
                        case OCIRoute Route:
                            IKObject.ObjectKey = objectCtrlInfo.objectInfo.dicKey;
                            IKObject.SelectedObject = Route.childRoot.gameObject;
                            break;
                    }
                }
            }

            if (IKObject.SelectedObject != null && IKObject.OCICharacter != null)
            {
                if (IKGuideObjects[SelectedGuideObject] == "eyes")
                    IKObjectInfoList.RemoveAll(x => x.OCICharacter == IKObject.OCICharacter && x.LinkType == LinkType.Eyes);
                else if (IKGuideObjects[SelectedGuideObject] == "neck")
                    IKObjectInfoList.RemoveAll(x => x.OCICharacter == IKObject.OCICharacter && x.LinkType == LinkType.Neck);
                else
                    IKObjectInfoList.RemoveAll(x => x.OCICharacter == IKObject.OCICharacter && x.LinkType == LinkType.GuideObject && x.IKTarget == IKObject.IKTarget);
                IKObject.Version = Version;
                IKObjectInfoList.Add(IKObject);
            }
            else
            {
                Logger.Log(LogLevel.Info | LogLevel.Message, "Select both the character and object to link.");
            }
        }

        private void UnlinkCharacterToObject()
        {
            TreeNodeObject[] selectNodes = Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNodes;
            bool DidUnlink = false;
            for (int i = 0; i < selectNodes.Length; i++)
            {
                if (Singleton<Studio.Studio>.Instance.dicInfo.TryGetValue(selectNodes[i], out ObjectCtrlInfo objectCtrlInfo))
                {
                    if (objectCtrlInfo is OCIChar ociChar)
                    {
                        if (IKGuideObjects[SelectedGuideObject] == "eyes")
                            IKObjectInfoList.RemoveAll(x => x.OCICharacter == ociChar && x.LinkType == LinkType.Eyes);
                        else if (IKGuideObjects[SelectedGuideObject] == "neck")
                            IKObjectInfoList.RemoveAll(x => x.OCICharacter == ociChar && x.LinkType == LinkType.Neck);
                        else
                            IKObjectInfoList.RemoveAll(x => x.OCICharacter == ociChar && x.LinkType == LinkType.GuideObject && x.IKTarget.boneObject.name == IKGuideObjects[SelectedGuideObject]);
                        DidUnlink = true;
                    }
                }
            }
            if (!DidUnlink)
                Logger.Log(LogLevel.Info | LogLevel.Message, "Select a character.");
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

                            var Character = Singleton<Studio.Studio>.Instance.dicObjectCtrl[AnimInfo.CharDicKey] as OCIChar;

                            LoadedAnimInfo.CharacterKey = AnimInfo.CharDicKey;
                            LoadedAnimInfo.OCICharacter = Character;
                            LoadedAnimInfo.IKPart = AnimInfo.IKPart;
                            LoadedAnimInfo.Version = AnimInfo.Version;
                            if (LoadedAnimInfo.Version.IsNullOrEmpty())
                                LoadedAnimInfo.Version = "1.0";
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
                    AnimationControllerInfoList.Add(new AnimationControllerInfo { CharDicKey = IKObj.CharacterKey, ItemDicKey = IKObj.ObjectKey, IKPart = IKObj.IKPart, Version = IKObj.Version });

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

        private enum LinkType { GuideObject, Eyes, Neck }
        private class IKObjectInfo
        {
            public GameObject SelectedObject;
            public OCIChar OCICharacter;
            public OCIChar.IKInfo IKTarget;
            public int CharacterKey;
            public int ObjectKey;
            public string IKPart;
            public string Version;
            public LinkType LinkType;

            public bool CheckNull() => SelectedObject == null || OCICharacter == null;
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
            [Key("Version")]
            public string Version { get; set; }

            public static AnimationControllerInfo Unserialize(byte[] data)
            {
                return MessagePackSerializer.Deserialize<AnimationControllerInfo>(data);
            }

            public byte[] Serialize()
            {
                return MessagePackSerializer.Serialize(this);
            }
        }

        private Rect AnimGUI = new Rect(70, 220, 200, 400);
        void OnGUI()
        {
            if (GUIVisible)
                AnimGUI = GUILayout.Window(23423475, AnimGUI, AnimWindow, PluginName);
        }

        private void AnimWindow(int id)
        {
            GUILayout.Label("Select an IK Guide Object");
            SelectedGuideObject = GUILayout.SelectionGrid(SelectedGuideObject, IKGuideObjectsPretty, 1, GUI.skin.toggle);
            GUILayout.Label("Select both the character and the object in Workspace");
            if (GUILayout.Button("Link"))
            {
                LinkCharacterToObject();
            }
            if (GUILayout.Button("Unlink"))
            {
                UnlinkCharacterToObject();
            }
        }
    }
}
