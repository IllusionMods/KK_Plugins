using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using ExtensionMethods;
using HarmonyLib;
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
#if AI || HS2
using AIChara;
#endif

namespace KK_Plugins
{
    /// <summary>
    /// Allows attaching IK nodes to objects to create custom animations
    /// </summary>
    [BepInProcess(Constants.StudioProcessName)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class AnimationController : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.animationcontroller";
        public const string PluginName = "Animation Controller";
        public const string PluginNameInternal = Constants.Prefix + "_AnimationController";
        public const string Version = "2.2";
        internal static new ManualLogSource Logger;

        private bool GUIVisible;
        private int SelectedGuideObject;
        private Rect AnimGUI = new Rect(70, 190, 200, 400);

        private static readonly string[] IKGuideObjectsPretty = { "Hips", "Left arm", "Left forearm", "Left hand", "Right arm", "Right forearm", "Right hand", "Left thigh", "Left knee", "Left foot", "Right thigh", "Right knee", "Right foot", "Eyes", "Neck" };
#if KK
        private static readonly string[] IKGuideObjects = { "cf_j_hips", "cf_j_arm00_L", "cf_j_forearm01_L", "cf_j_hand_L", "cf_j_arm00_R", "cf_j_forearm01_R", "cf_j_hand_R", "cf_j_thigh00_L", "cf_j_leg01_L", "cf_j_leg03_L", "cf_j_thigh00_R", "cf_j_leg01_R", "cf_j_leg03_R", "eyes", "neck" };
#else
        private static readonly string[] IKGuideObjects = { "cf_J_Hips", "cf_J_ArmUp00_L", "cf_J_ArmLow01_L", "cf_J_Hand_L", "cf_J_ArmUp00_R", "cf_J_ArmLow01_R", "cf_J_Hand_R", "cf_J_LegUp00_L", "cf_J_LegLow01_L", "cf_J_Foot01_L", "cf_J_LegUp00_R", "cf_J_LegLow01_R", "cf_J_Foot01_R", "eyes", "neck" };
#endif

        public static ConfigEntry<KeyboardShortcut> AnimationControllerHotkey { get; private set; }

        internal void Main()
        {
            Logger = base.Logger;
            AnimationControllerHotkey = Config.Bind("Keyboard Shortcuts", "Toggle Animation Controller Window", new KeyboardShortcut(KeyCode.Minus), "Show or hide the Animation Controller window in Studio");
            CharacterApi.RegisterExtraBehaviour<AnimationControllerCharaController>(GUID);
            StudioSaveLoadApi.RegisterExtraBehaviour<AnimationControllerSceneController>(GUID);

            //Change window location for different resolutions. Probably a better way to do this, but I don't care.
            if (Screen.height == 900)
                AnimGUI = new Rect(90, 350, 200, 400);
            else if (Screen.height == 1080)
                AnimGUI = new Rect(110, 510, 200, 400);
        }

        private void Update()
        {
            if (AnimationControllerHotkey.Value.IsDown())
                GUIVisible = !GUIVisible;
        }
        /// <summary>
        /// Called by GUI button click. Links the selected node to the selected object.
        /// </summary>
        private void LinkCharacterToObject()
        {
            ObjectCtrlInfo SelectedObject = null;
            ObjectCtrlInfo SelectedCharacter = null;
            TreeNodeObject[] selectNodes = Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNodes;

            if (selectNodes.Length != 2)
            {
                Logger.Log(BepInEx.Logging.LogLevel.Info | BepInEx.Logging.LogLevel.Message, "Select both the character and object to link.");
                return;
            }

            for (int i = 0; i < selectNodes.Length; i++)
            {
                if (Singleton<Studio.Studio>.Instance.dicInfo.TryGetValue(selectNodes[i], out ObjectCtrlInfo objectCtrlInfo))
                {
                    switch (objectCtrlInfo)
                    {
                        case OCIChar _:
                            SelectedCharacter = objectCtrlInfo;
                            break;
                        case OCIItem _:
                        case OCIFolder _:
                        case OCIRoute _:
                            SelectedObject = objectCtrlInfo;
                            break;
                    }
                }
            }

            if (SelectedObject != null && SelectedCharacter != null)
                GetController(SelectedCharacter as OCIChar).AddLink(IKGuideObjects[SelectedGuideObject], SelectedObject);
            else
                Logger.Log(BepInEx.Logging.LogLevel.Info | BepInEx.Logging.LogLevel.Message, "Select both the character and object to link.");
        }
        /// <summary>
        /// Called by GUI button click. Unlinks the selected node.
        /// </summary>
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
                        GetController(ociChar).RemoveLink(IKGuideObjects[SelectedGuideObject]);
                        DidUnlink = true;
                    }
                }
            }
            if (!DidUnlink)
                Logger.Log(BepInEx.Logging.LogLevel.Info | BepInEx.Logging.LogLevel.Message, "Select a character.");
        }

        /// <summary>
        /// Draws the GUI
        /// </summary>
        internal void OnGUI()
        {
            if (GUIVisible)
                GUILayout.Window(23423475, AnimGUI, AnimWindow, PluginName);
        }
        /// <summary>
        /// The AnimationController GUI
        /// </summary>
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
        /// <summary>
        /// Get the controller for the character
        /// </summary>
        public static AnimationControllerCharaController GetController(ChaControl character) => character == null ? null : character.gameObject.GetComponent<AnimationControllerCharaController>();
        /// <summary>
        /// Get the controller for the character
        /// </summary>
        public static AnimationControllerCharaController GetController(OCIChar character) => character == null || character.charInfo == null ? null : character.charInfo.gameObject.GetComponent<AnimationControllerCharaController>();

        public class AnimationControllerCharaController : CharaCustomFunctionController
        {
            private readonly Dictionary<OCIChar.IKInfo, ObjectCtrlInfo> GuideObjectLinksV1 = new Dictionary<OCIChar.IKInfo, ObjectCtrlInfo>();
            private readonly Dictionary<OCIChar.IKInfo, ObjectCtrlInfo> GuideObjectLinks = new Dictionary<OCIChar.IKInfo, ObjectCtrlInfo>();
            private readonly List<OCIChar.IKInfo> LinksToRemove = new List<OCIChar.IKInfo>();
            private ObjectCtrlInfo EyeLink;
            private bool EyeLinkEngaged;
            private ObjectCtrlInfo NeckLink;
            private bool NeckLinkEngaged;

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

                    Logger.LogDebug($"Saved KK_AnimationController animations for character {ChaControl.chaFile.parameter.fullname.Trim()}");
                }
                catch (Exception ex)
                {
                    Logger.Log(BepInEx.Logging.LogLevel.Error | BepInEx.Logging.LogLevel.Message, "Could not save KK_AnimationController animations.");
                    Logger.LogError(ex.ToString());
                }
            }

            protected override void OnReload(GameMode currentGameMode, bool maintainState) { }
            /// <summary>
            /// Every update, match the part to the object
            /// </summary>
            protected override void Update()
            {
                Transform eyeTransform = GetChildRootFromObjectCtrl(EyeLink);
                if (eyeTransform == null)
                {
                    if (EyeLinkEngaged)
                        RemoveEyeLink();
                }
                else
                    ChaControl.eyeLookCtrl.target = eyeTransform;

                Transform neckTransform = GetChildRootFromObjectCtrl(NeckLink);
                if (neckTransform == null)
                {
                    if (NeckLinkEngaged)
                        RemoveNeckLink();
                }
                else
                    ChaControl.neckLookCtrl.target = neckTransform;

                foreach (var link in GuideObjectLinksV1)
                {
                    Transform objTransform = GetChildRootFromObjectCtrl(link.Value);
                    if (objTransform == null)
                        LinksToRemove.Add(link.Key);
                    else
                    {
                        link.Key.guideObject.SetWorldPos(GetChildRootFromObjectCtrl(link.Value).position);
                        //Original version used the wrong rotation, keep using it so that scenes load properly
                        link.Key.targetInfo.changeAmount.rot = GetChildRootFromObjectCtrl(link.Value).localRotation.eulerAngles;
                    }
                }

                foreach (var link in GuideObjectLinks)
                {
                    Transform objTransform = GetChildRootFromObjectCtrl(link.Value);
                    if (objTransform == null)
                        LinksToRemove.Add(link.Key);
                    else
                    {
                        link.Key.guideObject.SetWorldPos(GetChildRootFromObjectCtrl(link.Value).position);
                        //Set the rotation of the linked body part to the rotation of the object
                        link.Key.targetObject.rotation = GetChildRootFromObjectCtrl(link.Value).rotation;
                        //Update the guide object so that unlinking and then rotating manually doesn't cause strange behavior
                        link.Key.guideObject.changeAmount.rot = link.Key.targetObject.localRotation.eulerAngles;
                    }
                }

                if (LinksToRemove.Count > 0)
                {
                    for (var i = 0; i < LinksToRemove.Count; i++)
                    {
                        var ikInfo = LinksToRemove[i];
                        RemoveLink(ikInfo);
                        RemoveLink(ikInfo);
                    }
                    LinksToRemove.Clear();
                }
            }
            /// <summary>
            /// Called by the scene controller, loads animations from the loaded or imported scene
            /// </summary>
            internal void LoadAnimations(int characterDicKey, ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems)
            {
                try
                {
                    PluginData ExtendedData = ExtendedSave.GetSceneExtendedDataById(PluginNameInternal);

                    //Version 1 save data
                    if (ExtendedData?.data != null && ExtendedData.data.ContainsKey("AnimationInfo"))
                    {
                        List<AnimationControllerInfo> AnimationControllerInfoList = ((object[])ExtendedData.data["AnimationInfo"]).Select(x => AnimationControllerInfo.Unserialize((byte[])x)).ToList();

                        for (var i = 0; i < AnimationControllerInfoList.Count; i++)
                        {
                            var AnimInfo = AnimationControllerInfoList[i];
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
                    Logger.LogDebug($"Loaded KK_AnimationController animations for character {ChaControl.chaFile.parameter.fullname.Trim()}");
                }
                catch (Exception ex)
                {
                    Logger.Log(BepInEx.Logging.LogLevel.Debug | BepInEx.Logging.LogLevel.Message, "Could not load KK_AnimationController animations.");
                    Logger.LogError(ex.ToString());
                }
            }
            /// <summary>
            /// Add a v1 link. Only ever added by loading card data, new links will never by of this type.
            /// </summary>
            private void AddLinkV1(string selectedGuideObject, ObjectCtrlInfo selectedObject)
            {
                OCIChar.IKInfo ikInfo = OCIChar.listIKTarget.First(x => x.boneObject.name == selectedGuideObject);
                GuideObjectLinksV1[ikInfo] = selectedObject;
            }
            /// <summary>
            /// Add a link between the selected guide object and the selected object.
            /// </summary>
            /// <param name="selectedGuideObject">Name of the bone associated with the guide object.</param>
            /// <param name="selectedObject">ObjectCtrlInfo of the object. Can be an item, a folder, or a route.</param>
            public void AddLink(string selectedGuideObject, ObjectCtrlInfo selectedObject)
            {
                if (selectedGuideObject == "eyes")
                    AddEyeLink(selectedObject);
                else if (selectedGuideObject == "neck")
                    AddNeckLink(selectedObject);
                else
                {
                    OCIChar.IKInfo ikInfo = OCIChar.listIKTarget.First(x => x.boneObject.name == selectedGuideObject);
                    GuideObjectLinks[ikInfo] = selectedObject;
                }
            }
            /// <summary>
            /// Links the character's eyes to the selected object.
            /// </summary>
            /// <param name="selectedObject">ObjectCtrlInfo of the object. Can be an item, a folder, or a route.</param>
            public void AddEyeLink(ObjectCtrlInfo selectedObject)
            {
                //Set the eye control to "Follow"
                SetEyeLook(1);
                EyeLink = selectedObject;
                EyeLinkEngaged = true;
            }
            /// <summary>
            /// Links the character's neck to the selected object.
            /// </summary>
            /// <param name="selectedObject">ObjectCtrlInfo of the object. Can be an item, a folder, or a route.</param>
            public void AddNeckLink(ObjectCtrlInfo selectedObject)
            {
                //Set the neck control to "Follow"
                SetNeckLook(1);
                NeckLink = selectedObject;
                NeckLinkEngaged = true;
            }
            /// <summary>
            /// Remove a link between the selected guide object and any object it is currently linked to.
            /// </summary>
            /// <param name="selectedGuideObject">Name of the bone associated with the guide object.</param>
            public void RemoveLink(string selectedGuideObject)
            {
                if (selectedGuideObject == "eyes")
                    RemoveEyeLink();
                else if (selectedGuideObject == "neck")
                    RemoveNeckLink();
                else
                {
                    OCIChar.IKInfo ikInfo = OCIChar.listIKTarget.First(x => x.boneObject.name == selectedGuideObject);
                    GuideObjectLinksV1.Remove(ikInfo);
                    GuideObjectLinks.Remove(ikInfo);
                }
            }
            /// <summary>
            /// Remove a link between the selected guide object and any object it is currently linked to.
            /// </summary>
            public void RemoveLink(OCIChar.IKInfo ikInfo)
            {
                GuideObjectLinksV1.Remove(ikInfo);
                GuideObjectLinks.Remove(ikInfo);
            }
            /// <summary>
            /// Remove a link between the eyes and any object it is currently linked to.
            /// </summary>
            public void RemoveEyeLink()
            {
                ChaControl.eyeLookCtrl.target = Camera.main.transform;
                //Set the eye control to "Front"
                SetEyeLook(0);
                EyeLink = null;
                EyeLinkEngaged = false;
            }
            /// <summary>
            /// Remove a link between the neck and any object it is currently linked to.
            /// </summary>
            public void RemoveNeckLink()
            {
                ChaControl.neckLookCtrl.target = Camera.main.transform;
                //Set the neck control to "Anim"
                SetNeckLook(2);
                NeckLink = null;
                NeckLinkEngaged = false;
            }
            /// <summary>
            /// Set the eye look stuff. Most of the code comes from Studio.MPCharCtrl.LookAtInfo.OnClick.
            /// This sets the selected button, changes the color to green, etc. as though the button were clicked manually.
            /// This is done because the eyes will only track the target object when the eyes are set to "Follow"
            /// </summary>
            private void SetEyeLook(int no)
            {
                var temp = Studio.Studio.Instance.manipulatePanelCtrl.MPCharCtrl().ociChar;
                Studio.Studio.Instance.manipulatePanelCtrl.MPCharCtrl().ociChar = OCIChar;
                int eyesLookPtn = OCIChar.charFileStatus.eyesLookPtn;
                OCIChar.ChangeLookEyesPtn(no);
                Studio.Studio.Instance.manipulatePanelCtrl.MPCharCtrl().LookAtInfo_SliderSize().interactable = false;
                Studio.Studio.Instance.manipulatePanelCtrl.MPCharCtrl().LookAtInfo_ButtonMode()[eyesLookPtn].image.color = Color.white;
                Studio.Studio.Instance.manipulatePanelCtrl.MPCharCtrl().LookAtInfo_ButtonMode()[no].image.color = Color.green;
                Studio.Studio.Instance.manipulatePanelCtrl.MPCharCtrl().ociChar = temp;
            }
            /// <summary>
            /// Set the neck look stuff. Most of the code comes from Studio.MPCharCtrl.NeckInfo.OnClick.
            /// This sets the selected button, changes the color to green, etc. as though the button were clicked manually.
            /// This is done because the neck will only track the target object when the neck is set to "Follow"
            /// </summary>
            private void SetNeckLook(int no)
            {
                var temp = Studio.Studio.Instance.manipulatePanelCtrl.MPCharCtrl().ociChar;
                Studio.Studio.Instance.manipulatePanelCtrl.MPCharCtrl().ociChar = OCIChar;
                int neckLookPtn = OCIChar.charFileStatus.neckLookPtn;
                neckLookPtn = Array.FindIndex(patterns, v => v == neckLookPtn);
                OCIChar.ChangeLookNeckPtn(patterns[no]);
                Studio.Studio.Instance.manipulatePanelCtrl.MPCharCtrl().NeckInfo_ButtonMode()[neckLookPtn].image.color = Color.white;
                Studio.Studio.Instance.manipulatePanelCtrl.MPCharCtrl().NeckInfo_ButtonMode()[no].image.color = Color.green;
                Studio.Studio.Instance.manipulatePanelCtrl.MPCharCtrl().ociChar = temp;
            }
            /// <summary>
            /// Used by SetNeckLook
            /// </summary>
            private readonly int[] patterns = { 0, 1, 3, 4 };
            /// <summary>
            /// Because ObjectCtrlInfo does not have a childRoot, cast it as the appropriate type and get it from that.
            /// </summary>
            private static Transform GetChildRootFromObjectCtrl(ObjectCtrlInfo objectCtrlInfo)
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

            private OCIChar _OCIChar;
            /// <summary>
            /// The character's OCIChar object
            /// </summary>
            public OCIChar OCIChar
            {
                get
                {
                    if (_OCIChar == null)
                        _OCIChar = ChaControl.GetOCIChar();
                    return _OCIChar;
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
            //Clear out the old style data
            protected override void OnSceneSave() => ExtendedSave.SetSceneExtendedDataById(PluginNameInternal, new PluginData { data = null });

            protected override void OnSceneLoad(SceneOperationKind operation, ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems)
            {
                foreach (var kvp in loadedItems)
                    if (kvp.Value is OCIChar chara)
                        GetController(chara).LoadAnimations(kvp.Key, loadedItems);
            }
        }
    }
}

namespace ExtensionMethods
{
    public static class MPCharCtrlExtensions
    {
        private static MPCharCtrl _MPCharCtrl;
        public static MPCharCtrl MPCharCtrl(this ManipulatePanelCtrl _)
        {
            if (_MPCharCtrl == null)
            {
                var charaPanelInfo = Traverse.Create(Studio.Studio.Instance.manipulatePanelCtrl).Field("charaPanelInfo").GetValue();
                _MPCharCtrl = (MPCharCtrl)Traverse.Create(charaPanelInfo).Property("mpCharCtrl").GetValue();
            }
            return _MPCharCtrl;
        }

        private static object _LookAtInfo;
        private static object LookAtInfo
        {
            get
            {
                if (_LookAtInfo == null)
                    _LookAtInfo = Traverse.Create(Studio.Studio.Instance.manipulatePanelCtrl.MPCharCtrl()).Field("lookAtInfo").GetValue();
                return _LookAtInfo;
            }
        }

        private static Slider _LookAtInfo_SliderSize;
        public static Slider LookAtInfo_SliderSize(this MPCharCtrl _)
        {
            if (_LookAtInfo_SliderSize == null)
                _LookAtInfo_SliderSize = (Slider)Traverse.Create(LookAtInfo).Field("sliderSize").GetValue();
            return _LookAtInfo_SliderSize;
        }

        private static Button[] _LookAtInfo_ButtonMode;
        public static Button[] LookAtInfo_ButtonMode(this MPCharCtrl _)
        {
            if (_LookAtInfo_ButtonMode == null)
                _LookAtInfo_ButtonMode = (Button[])Traverse.Create(LookAtInfo).Field("buttonMode").GetValue();
            return _LookAtInfo_ButtonMode;
        }

        private static object _MPCharCtrl_NeckInfo;
        private static object MPCharCtrl_NeckInfo
        {
            get
            {
                if (_MPCharCtrl_NeckInfo == null)
                    _MPCharCtrl_NeckInfo = Traverse.Create(Studio.Studio.Instance.manipulatePanelCtrl.MPCharCtrl()).Field("neckInfo").GetValue();
                return _MPCharCtrl_NeckInfo;
            }
        }

        private static Button[] _NeckInfo_ButtonMode;
        public static Button[] NeckInfo_ButtonMode(this MPCharCtrl _)
        {
            if (_NeckInfo_ButtonMode == null)
                _NeckInfo_ButtonMode = (Button[])Traverse.Create(MPCharCtrl_NeckInfo).Field("buttonMode").GetValue();
            return _NeckInfo_ButtonMode;
        }
    }
}