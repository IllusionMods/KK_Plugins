using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ExtensibleSaveFormat;
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
using KKAPI.Studio.UI;
using MessagePack.Formatters;
using UnityEngine;
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
        public const string Version = "2.3";
        internal static new ManualLogSource Logger;

        private bool GUIVisible
        {
            get => _guiVisible;
            set
            {
                _guiVisible = value;
                _toolbarToggle.Value = value;
            }
        }
        private bool _guiVisible;
        private ToolbarToggle _toolbarToggle;
        private Texture2D _windowBackground; // This needs to be in a field or it will get destroyed on scene load
        private GUIStyle _bgStyle;
        private int SelectedGuideObject;
        private Rect AnimGUI = new Rect(70, 190, 200, 400);

        private static readonly string[] IKGuideObjectsPretty = { "Hips", "Left arm", "Left forearm", "Left hand", "Right arm", "Right forearm", "Right hand", "Left thigh", "Left knee", "Left foot", "Right thigh", "Right knee", "Right foot", "Eyes", "Neck" };
#if KK || KKS
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

            var buttonTex = ResourceUtils.GetEmbeddedResource("studio_icon.png", typeof(AnimationController).Assembly).LoadTexture();
            _toolbarToggle = CustomToolbarButtons.AddLeftToolbarToggle(buttonTex, false, b => GUIVisible = b);

            // todo get rid of this and use the kkapi version after it's fixed in studio
            _windowBackground = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            _windowBackground.SetPixel(0, 0, new Color(0.4f, 0.4f, 0.4f));
            _windowBackground.Apply();
            DontDestroyOnLoad(_windowBackground);
            _bgStyle = new GUIStyle { normal = new GUIStyleState { background = _windowBackground } };

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
            var gr = StudioAPI.GetSelectedObjects().GroupBy(x => x is OCIChar).ToList();

            var selectedCharacters = (gr.SingleOrDefault(x => x.Key) ?? Enumerable.Empty<ObjectCtrlInfo>()).Cast<OCIChar>().ToList();
            var selectedObjects = (gr.SingleOrDefault(x => !x.Key) ?? Enumerable.Empty<ObjectCtrlInfo>()).Where(x => x is OCIItem || x is OCIFolder || x is OCIRoute).ToList();

            if (selectedCharacters.Count == 0 || selectedObjects.Count == 0)
            {
                Logger.Log(BepInEx.Logging.LogLevel.Info | BepInEx.Logging.LogLevel.Message, "Select both the characters and the object to link.");
            }
            else if (selectedObjects.Count > 1)
            {
                Logger.Log(BepInEx.Logging.LogLevel.Info | BepInEx.Logging.LogLevel.Message, "You can only select one object.");
            }
            else
            {
                var selectedGuideObject = IKGuideObjects[SelectedGuideObject];
                var selectedObject = selectedObjects.Single();
                foreach (var selectedCharacter in selectedCharacters)
                    GetController(selectedCharacter).AddLink(selectedGuideObject, selectedObject);
            }
        }
        /// <summary>
        /// Called by GUI button click. Unlinks the selected node.
        /// </summary>
        private void UnlinkCharacterToObject()
        {
            bool DidUnlink = false;

            foreach (var ociChar in StudioAPI.GetSelectedCharacters())
            {
                var success = GetController(ociChar).RemoveLink(IKGuideObjects[SelectedGuideObject]);
                if (success) Logger.Log(BepInEx.Logging.LogLevel.Info | BepInEx.Logging.LogLevel.Message, "Link removed successfully.");
                else Logger.Log(BepInEx.Logging.LogLevel.Info | BepInEx.Logging.LogLevel.Message, "No links to remove.");
                DidUnlink = true;
            }

            if (!DidUnlink)
                Logger.Log(BepInEx.Logging.LogLevel.Info | BepInEx.Logging.LogLevel.Message, "No characters selected. Select which characters to unlink.");
        }

        /// <summary>
        /// Draws the GUI
        /// </summary>
        internal void OnGUI()
        {
            if (GUIVisible)
            {
                AnimGUI = GUILayout.Window(23423475, AnimGUI, AnimWindow, PluginName);
                GUI.Box(AnimGUI, GUIContent.none, _bgStyle);
            }
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
                    if (!OCIChar.finalIK.enabled)
                        Logger.Log(BepInEx.Logging.LogLevel.Info | BepInEx.Logging.LogLevel.Message, "IK is disabled on this character! You have to enable anim\\Kinematics\\IK for the link to work.");
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
            public bool RemoveLink(string selectedGuideObject)
            {
                if (selectedGuideObject == "eyes")
                    return RemoveEyeLink();
                if (selectedGuideObject == "neck")
                    return RemoveNeckLink();
                OCIChar.IKInfo ikInfo = OCIChar.listIKTarget.First(x => x.boneObject.name == selectedGuideObject);
                var a = GuideObjectLinksV1.Remove(ikInfo);
                var b = GuideObjectLinks.Remove(ikInfo);
                return a || b;
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
            public bool RemoveEyeLink()
            {
                var wasEngaged = EyeLinkEngaged;
                ChaControl.eyeLookCtrl.target = Camera.main.transform;
                //Set the eye control to "Front"
                SetEyeLook(0);
                EyeLink = null;
                EyeLinkEngaged = false;
                return wasEngaged;
            }
            /// <summary>
            /// Remove a link between the neck and any object it is currently linked to.
            /// </summary>
            public bool RemoveNeckLink()
            {
                var wasEngaged = NeckLinkEngaged;
                ChaControl.neckLookCtrl.target = Camera.main.transform;
                //Set the neck control to "Anim"
                SetNeckLook(2);
                NeckLink = null;
                NeckLinkEngaged = false;
                return wasEngaged;
            }
            /// <summary>
            /// Set the eye look stuff. Most of the code comes from Studio.MPCharCtrl.LookAtInfo.OnClick.
            /// This sets the selected button, changes the color to green, etc. as though the button were clicked manually.
            /// This is done because the eyes will only track the target object when the eyes are set to "Follow"
            /// </summary>
            private void SetEyeLook(int no)
            {
                var temp = Studio.Studio.Instance.manipulatePanelCtrl.charaPanelInfo.mpCharCtrl.ociChar;
                Studio.Studio.Instance.manipulatePanelCtrl.charaPanelInfo.mpCharCtrl.ociChar = OCIChar;
                int eyesLookPtn = OCIChar.charFileStatus.eyesLookPtn;
                OCIChar.ChangeLookEyesPtn(no);
                Studio.Studio.Instance.manipulatePanelCtrl.charaPanelInfo.mpCharCtrl.lookAtInfo.sliderSize.interactable = false;
                Studio.Studio.Instance.manipulatePanelCtrl.charaPanelInfo.mpCharCtrl.lookAtInfo.buttonMode[eyesLookPtn].image.color = Color.white;
                Studio.Studio.Instance.manipulatePanelCtrl.charaPanelInfo.mpCharCtrl.lookAtInfo.buttonMode[no].image.color = Color.green;
                Studio.Studio.Instance.manipulatePanelCtrl.charaPanelInfo.mpCharCtrl.ociChar = temp;
            }
            /// <summary>
            /// Set the neck look stuff. Most of the code comes from Studio.MPCharCtrl.NeckInfo.OnClick.
            /// This sets the selected button, changes the color to green, etc. as though the button were clicked manually.
            /// This is done because the neck will only track the target object when the neck is set to "Follow"
            /// </summary>
            private void SetNeckLook(int no)
            {
                var temp = Studio.Studio.Instance.manipulatePanelCtrl.charaPanelInfo.mpCharCtrl.ociChar;
                Studio.Studio.Instance.manipulatePanelCtrl.charaPanelInfo.mpCharCtrl.ociChar = OCIChar;
                int neckLookPtn = OCIChar.charFileStatus.neckLookPtn;
                neckLookPtn = Array.FindIndex(patterns, v => v == neckLookPtn);
                OCIChar.ChangeLookNeckPtn(patterns[no]);
                Studio.Studio.Instance.manipulatePanelCtrl.charaPanelInfo.mpCharCtrl.neckInfo.buttonMode[neckLookPtn].image.color = Color.white;
                Studio.Studio.Instance.manipulatePanelCtrl.charaPanelInfo.mpCharCtrl.neckInfo.buttonMode[no].image.color = Color.green;
                Studio.Studio.Instance.manipulatePanelCtrl.charaPanelInfo.mpCharCtrl.ociChar = temp;
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
