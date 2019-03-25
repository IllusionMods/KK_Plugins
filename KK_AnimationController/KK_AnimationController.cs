using BepInEx;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using Harmony;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Studio;
using KKAPI.Studio.SaveLoad;
using KKAPI.Utilities;
using MessagePack;
using Studio;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
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
        public const string PluginName = "Animation Controller";
        public const string PluginNameInternal = nameof(KK_AnimationController);
        public const string GUID = "com.deathweasel.bepinex.animationcontroller";
        public const string Version = "2.0";
        public static SavedKeyboardShortcut AnimationControllerHotkey { get; private set; }
        private bool GUIVisible = false;
        int SelectedGuideObject = 0;
        static readonly string[] IKGuideObjectsPretty = new string[] { "Hips", "Left arm", "Left forearm", "Left hand", "Right arm", "Right forearm", "Right hand", "Left thigh", "Left knee", "Left foot", "Right thigh", "Right knee", "Right foot", "Eyes", "Neck" };
        static readonly string[] IKGuideObjects = new string[] { "cf_j_hips", "cf_j_arm00_L", "cf_j_forearm01_L", "cf_j_hand_L", "cf_j_arm00_R", "cf_j_forearm01_R", "cf_j_hand_R", "cf_j_thigh00_L", "cf_j_leg01_L", "cf_j_leg03_L", "cf_j_thigh00_R", "cf_j_leg01_R", "cf_j_leg03_R", "eyes", "neck" };

        void Main()
        {
            var harmony = HarmonyInstance.Create("com.deathweasel.bepinex.animationcontroller");
            harmony.PatchAll(typeof(KK_AnimationController));

            AnimationControllerHotkey = new SavedKeyboardShortcut(nameof(AnimationControllerHotkey), nameof(KK_AnimationController), new KeyboardShortcut(KeyCode.Minus));
            CharacterApi.RegisterExtraBehaviour<AnimationControllerCharaController>(GUID);
            StudioSaveLoadApi.RegisterExtraBehaviour<AnimationControllerSceneController>(GUID);
        }

        private void Update()
        {
            if (AnimationControllerHotkey.IsDown())
                GUIVisible = !GUIVisible;
        }

        private void LinkCharacterToObject()
        {
            ObjectCtrlInfo SelectedObject = null;
            ObjectCtrlInfo SelectedCharacter = null;
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
                            SelectedCharacter = objectCtrlInfo;
                            break;
                        case OCIItem Item:
                        case OCIFolder Folder:
                        case OCIRoute Route:
                            SelectedObject = objectCtrlInfo;
                            break;
                    }
                }
            }

            if (SelectedObject != null && SelectedCharacter != null)
                GetController(SelectedCharacter as OCIChar).AddLink(IKGuideObjects[SelectedGuideObject], SelectedObject);
            else
                Logger.Log(LogLevel.Info | LogLevel.Message, "Select both the character and object to link.");
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
                        GetController(objectCtrlInfo as OCIChar).RemoveLink(IKGuideObjects[SelectedGuideObject]);
                        DidUnlink = true;
                    }
                }
            }
            if (!DidUnlink)
                Logger.Log(LogLevel.Info | LogLevel.Message, "Select a character.");
        }

        private Rect AnimGUI = new Rect(70, 190, 200, 400);
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
                LinkCharacterToObject();
            if (GUILayout.Button("Unlink"))
                UnlinkCharacterToObject();
        }

        public static AnimationControllerCharaController GetController(ChaControl character) => character?.gameObject?.GetComponent<AnimationControllerCharaController>();
        public static AnimationControllerCharaController GetController(OCIChar character) => character?.charInfo?.gameObject?.GetComponent<AnimationControllerCharaController>();


        public class AnimationControllerCharaController : CharaCustomFunctionController
        {
            private Dictionary<OCIChar.IKInfo, ObjectCtrlInfo> GuideObjectLinksV1 = new Dictionary<OCIChar.IKInfo, ObjectCtrlInfo>();
            private Dictionary<OCIChar.IKInfo, ObjectCtrlInfo> GuideObjectLinks = new Dictionary<OCIChar.IKInfo, ObjectCtrlInfo>();
            private List<OCIChar.IKInfo> LinksToRemove = new List<OCIChar.IKInfo>();
            private ObjectCtrlInfo EyeLink;
            private bool EyeLinkEngaged = false;
            private ObjectCtrlInfo NeckLink;
            private bool NeckLinkEngaged = false;

            protected override void OnCardBeingSaved(GameMode currentGameMode)
            {
                try
                {
                    var data = new PluginData();
                    Dictionary<string, int> linksV1 = new Dictionary<string, int>();
                    Dictionary<string, int> links = new Dictionary<string, int>();

                    foreach (var link in GuideObjectLinksV1)
                        linksV1.Add(link.Key.boneObject.name, Studio.Studio.Instance.dicObjectCtrl.First(x => x.Value == link.Value).Key);
                    if (linksV1.Count != 0)
                        data.data.Add("LinksV1", linksV1);

                    foreach (var link in GuideObjectLinks)
                        links.Add(link.Key.boneObject.name, Studio.Studio.Instance.dicObjectCtrl.First(x => x.Value == link.Value).Key);
                    if (links.Count != 0)
                        data.data.Add("Links", links);

                    if (EyeLink != null)
                        data.data.Add("Eyes", Studio.Studio.Instance.dicObjectCtrl.First(x => x.Value == EyeLink).Key);
                    if (NeckLink != null)
                        data.data.Add("Neck", Studio.Studio.Instance.dicObjectCtrl.First(x => x.Value == NeckLink).Key);

                    if (data.data.Count == 0)
                        data.data = null;
                    data.version = 2;
                    SetExtendedData(data);

                    Logger.Log(LogLevel.Debug, $"Saved KK_AnimationController animations for character {ChaControl.chaFile.parameter.fullname.Trim()}");
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error | LogLevel.Message, "Could not save KK_AnimationController animations.");
                    Logger.Log(LogLevel.Error, ex.ToString());
                }
            }

            protected override void OnReload(GameMode currentGameMode) { }
            /// <summary>
            /// Every update, match the part to the object
            /// </summary>
            protected override void Update()
            {
                Transform eyeTransform = GetTransformFromObjectCtrl(EyeLink);
                if (eyeTransform == null)
                {
                    if (EyeLinkEngaged)
                        RemoveEyeLink();
                }
                else
                    ChaControl.eyeLookCtrl.target = eyeTransform;

                Transform neckTransform = GetTransformFromObjectCtrl(EyeLink);
                if (neckTransform == null)
                {
                    if (NeckLinkEngaged)
                        RemoveNeckLink();
                }
                else
                    ChaControl.neckLookCtrl.target = neckTransform;

                foreach (var ikInfo in LinksToRemove)
                {
                    GuideObjectLinksV1.Remove(ikInfo);
                    GuideObjectLinks.Remove(ikInfo);
                }

                foreach (var link in GuideObjectLinksV1)
                {
                    Transform objTransform = GetTransformFromObjectCtrl(link.Value);
                    if (objTransform == null)
                        RemoveLink(link.Key);
                    else
                    {
                        link.Key.guideObject.SetWorldPos(GetTransformFromObjectCtrl(link.Value).position);
                        //Original version used the wrong rotation, keep using it so that scenes load properly
                        link.Key.targetInfo.changeAmount.rot = GetTransformFromObjectCtrl(link.Value).localRotation.eulerAngles;
                    }
                }

                foreach (var link in GuideObjectLinks)
                {
                    Transform objTransform = GetTransformFromObjectCtrl(link.Value);
                    if (objTransform == null)
                        RemoveLink(link.Key);
                    else
                    {
                        link.Key.guideObject.SetWorldPos(GetTransformFromObjectCtrl(link.Value).position);
                        //Set the rotation of the linked body part to the rotation of the object
                        link.Key.targetObject.rotation = GetTransformFromObjectCtrl(link.Value).rotation;
                        //Update the guide object so that unlinking and then rotating manually doesn't cause strange behavior
                        link.Key.guideObject.changeAmount.rot = link.Key.targetObject.localRotation.eulerAngles;
                    }
                }
            }

            internal void LoadAnimations(int characterDicKey, ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems)
            {
                try
                {
                    PluginData ExtendedData = ExtendedSave.GetSceneExtendedDataById(PluginNameInternal);

                    //Version 1 save data
                    if (ExtendedData?.data != null && ExtendedData.data.ContainsKey("AnimationInfo"))
                    {
                        List<AnimationControllerInfo> AnimationControllerInfoList = ((object[])ExtendedData.data["AnimationInfo"]).Select(x => AnimationControllerInfo.Unserialize((byte[])x)).ToList();

                        foreach (var AnimInfo in AnimationControllerInfoList)
                        {
                            //See if this is the right character
                            if (AnimInfo.CharDicKey != characterDicKey)
                                continue;

                            ObjectCtrlInfo linkedItem = loadedItems[AnimInfo.ItemDicKey];

                            if (AnimInfo.Version.IsNullOrEmpty())
                                AddLinkV1(AnimInfo.IKPart, linkedItem);
                            else
                                AddLink(AnimInfo.IKPart, linkedItem);
                        }
                    }
                    //Version 2 save data
                    else
                    {
                        var data = GetExtendedData();
                        if (data?.data != null)
                        {
                            if (data.data.TryGetValue("LinksV1", out var loadedLinksV1) && loadedLinksV1 != null)
                                foreach (var link in (Dictionary<object, object>)loadedLinksV1)
                                    AddLinkV1((string)link.Key, loadedItems[(int)link.Value]);

                            if (data.data.TryGetValue("Links", out var loadedLinks) && loadedLinks != null)
                                foreach (var link in (Dictionary<object, object>)loadedLinks)
                                    AddLink((string)link.Key, loadedItems[(int)link.Value]);

                            if (data.data.TryGetValue("Eyes", out var loadedEyeLink) && loadedEyeLink != null)
                                AddEyeLink(loadedItems[(int)loadedEyeLink]);

                            if (data.data.TryGetValue("Neck", out var loadedNeckLink) && loadedNeckLink != null)
                                AddNeckLink(loadedItems[(int)loadedNeckLink]);
                        }
                    }
                    Logger.Log(LogLevel.Debug, $"Loaded KK_AnimationController animations for character {ChaControl.chaFile.parameter.fullname.Trim()}");
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error | LogLevel.Message, "Could not load KK_AnimationController animations.");
                    Logger.Log(LogLevel.Error, ex.ToString());
                }
            }

            internal void AddLinkV1(string selectedGuideObject, ObjectCtrlInfo selectedObject)
            {
                OCIChar.IKInfo ikInfo = StudioObjectExtensions.GetOCIChar(ChaControl).listIKTarget.First(x => x.boneObject.name == selectedGuideObject);
                GuideObjectLinksV1[ikInfo] = selectedObject;
            }

            public void AddLink(string selectedGuideObject, ObjectCtrlInfo selectedObject)
            {
                if (selectedGuideObject == "eyes")
                    AddEyeLink(selectedObject);
                else if (selectedGuideObject == "neck")
                    AddNeckLink(selectedObject);
                else
                {
                    OCIChar.IKInfo ikInfo = StudioObjectExtensions.GetOCIChar(ChaControl).listIKTarget.First(x => x.boneObject.name == selectedGuideObject);
                    GuideObjectLinks[ikInfo] = selectedObject;
                }
            }
            public void AddEyeLink(ObjectCtrlInfo selectedObject)
            {
                //Set the eye control to "Follow"
                SetEyeLook(1);
                EyeLink = selectedObject;
                EyeLinkEngaged = true;
            }
            public void AddNeckLink(ObjectCtrlInfo selectedObject)
            {
                //Set the neck control to "Follow"
                SetNeckLook(1);
                NeckLink = selectedObject;
                NeckLinkEngaged = true;
            }
            public void RemoveLink(string selectedGuideObject)
            {
                if (selectedGuideObject == "eyes")
                    RemoveEyeLink();
                else if (selectedGuideObject == "neck")
                    RemoveNeckLink();
                else
                {
                    OCIChar.IKInfo ikInfo = StudioObjectExtensions.GetOCIChar(ChaControl).listIKTarget.First(x => x.boneObject.name == selectedGuideObject);
                    LinksToRemove.Add(ikInfo);
                }
            }
            public void RemoveLink(OCIChar.IKInfo ikInfo) => LinksToRemove.Add(ikInfo);
            public void RemoveEyeLink()
            {
                ChaControl.eyeLookCtrl.target = Camera.main.transform;
                //Set the eye control to "Front"
                SetEyeLook(0);
                EyeLink = null;
                EyeLinkEngaged = false;
            }
            public void RemoveNeckLink()
            {
                ChaControl.neckLookCtrl.target = Camera.main.transform;
                //Set the neck control to "Anim"
                SetNeckLook(2);
                NeckLink = null;
                NeckLinkEngaged = false;
            }
            private void SetEyeLook(int no)
            {
                OCIChar ociChar = StudioObjectExtensions.GetOCIChar(ChaControl);
                foreach (MPCharCtrl mpCharCtrl in Resources.FindObjectsOfTypeAll<MPCharCtrl>())
                {
                    var temp = mpCharCtrl.ociChar;
                    mpCharCtrl.ociChar = ociChar;
                    var lookAtInfo = Traverse.Create(mpCharCtrl).Field("lookAtInfo").GetValue();
                    int eyesLookPtn = ociChar.charFileStatus.eyesLookPtn;
                    ociChar.ChangeLookEyesPtn(no, false);
                    Slider sliderSize = (Slider)Traverse.Create(lookAtInfo).Field("sliderSize").GetValue();
                    sliderSize.interactable = false;
                    Button[] buttonMode = (Button[])Traverse.Create(lookAtInfo).Field("buttonMode").GetValue();
                    buttonMode[eyesLookPtn].image.color = Color.white;
                    buttonMode[no].image.color = Color.green;
                    mpCharCtrl.ociChar = temp;
                }
            }
            private void SetNeckLook(int no)
            {
                OCIChar ociChar = StudioObjectExtensions.GetOCIChar(ChaControl);
                foreach (MPCharCtrl mpCharCtrl in Resources.FindObjectsOfTypeAll<MPCharCtrl>())
                {
                    ChaControl.neckLookCtrl.target = Camera.main.transform;
                    var temp = mpCharCtrl.ociChar;
                    mpCharCtrl.ociChar = ociChar;
                    var lookAtInfo = Traverse.Create(mpCharCtrl).Field("neckInfo").GetValue();
                    int neckLookPtn = ociChar.charFileStatus.neckLookPtn;
                    neckLookPtn = Array.FindIndex(patterns, (int v) => v == neckLookPtn);
                    ociChar.ChangeLookNeckPtn(patterns[no]);
                    Button[] buttonMode = (Button[])Traverse.Create(lookAtInfo).Field("buttonMode").GetValue();
                    buttonMode[neckLookPtn].image.color = Color.white;
                    buttonMode[no].image.color = Color.green;
                    mpCharCtrl.ociChar = temp;
                }
            }
            private readonly int[] patterns = new int[] { 0, 1, 3, 4 };
            private Transform GetTransformFromObjectCtrl(ObjectCtrlInfo objectCtrlInfo)
            {
                if (objectCtrlInfo == null) return null;

                switch (objectCtrlInfo)
                {
                    case OCIItem Item: return Item.childRoot;
                    case OCIFolder Folder: return Folder.childRoot;
                    case OCIRoute Route: return Route.childRoot;
                    default: return null;
                }
            }
            /// <summary>
            /// Used to load data from the original save format
            /// </summary>
            [Serializable]
            [MessagePackObject]
            private class AnimationControllerInfo
            {
                [Key("CharDicKey")]
                public int CharDicKey { get; set; }
                [Key("ItemDicKey")]
                public int ItemDicKey { get; set; }
                [Key("IKPart")]
                public string IKPart { get; set; }
                [Key("Version")]
                public string Version { get; set; }
                public static AnimationControllerInfo Unserialize(byte[] data) => MessagePackSerializer.Deserialize<AnimationControllerInfo>(data);
                public byte[] Serialize() => MessagePackSerializer.Serialize(this);
            }
        }

        public class AnimationControllerSceneController : SceneCustomFunctionController
        {
            protected override void OnSceneSave()
            {
                //Clear out the old style data
                PluginData OldData = ExtendedSave.GetSceneExtendedDataById(PluginNameInternal);
                if (OldData != null)
                    OldData = null;
                ExtendedSave.SetSceneExtendedDataById(PluginNameInternal, new PluginData { data = null });
            }

            protected override void OnSceneLoad(SceneOperationKind operation, ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems)
            {
                foreach (var kvp in loadedItems)
                    if (kvp.Value is OCIChar)
                        GetController(kvp.Value as OCIChar).LoadAnimations(kvp.Key, loadedItems);
            }
        }
    }
}
