using BepInEx;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using Harmony;
using KKAPI;
using KKAPI.Chara;
using MessagePack;
using Studio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
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
        private static bool DoingImport = false;
        private static Dictionary<int, int> ImportDictionary = new Dictionary<int, int>();
        private static int NewIndex;
        int SelectedGuideObject = 0;
        static readonly string[] IKGuideObjectsPretty = new string[] { "Hips", "Left arm", "Left forearm", "Left hand", "Right arm", "Right forearm", "Right hand", "Left thigh", "Left knee", "Left foot", "Right thigh", "Right knee", "Right foot", "Eyes", "Neck" };
        static readonly string[] IKGuideObjects = new string[] { "cf_j_hips", "cf_j_arm00_L", "cf_j_forearm01_L", "cf_j_hand_L", "cf_j_arm00_R", "cf_j_forearm01_R", "cf_j_hand_R", "cf_j_thigh00_L", "cf_j_leg01_L", "cf_j_leg03_L", "cf_j_thigh00_R", "cf_j_leg01_R", "cf_j_leg03_R", "eyes", "neck" };

        void Main()
        {
            var harmony = HarmonyInstance.Create("com.deathweasel.bepinex.animationcontroller");
            harmony.PatchAll(typeof(KK_AnimationController));

            AnimationControllerHotkey = new SavedKeyboardShortcut(nameof(AnimationControllerHotkey), nameof(KK_AnimationController), new KeyboardShortcut(KeyCode.Minus));
            CharacterApi.RegisterExtraBehaviour<AnimationControllerController>(GUID);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(Studio.Studio), "ImportScene")]
        public static void ImportScenePrefix()
        {
            ImportDictionary.Clear();
            DoingImport = true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Studio.Studio), "ImportScene")]
        public static void ImportScenePostfix()
        {
            DoingImport = false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Studio.Studio), "GetNewIndex")]
        public static void GetNewIndex(int __result)
        {
            NewIndex = __result;
        }
        [HarmonyPrefix, HarmonyPatch(typeof(Studio.ObjectInfo), "Load")]
        public static bool ObjectInfoLoadPrefix(ObjectInfo __instance, BinaryReader _reader, Version _version, bool _import, bool _other = true)
        {
            int dicKey = _reader.ReadInt32();
            if (_import)
                ImportDictionary[dicKey] = NewIndex;
            else
                Traverse.Create(__instance).Property("dicKey").SetValue(Studio.Studio.SetNewIndex(dicKey));


            __instance.changeAmount.Load(_reader);
            if (__instance.dicKey != -1 && !_import)
                Studio.Studio.AddChangeAmount(__instance.dicKey, __instance.changeAmount);
            if (_other)
                __instance.treeState = (TreeNodeObject.TreeState)_reader.ReadInt32();
            if (_other)
                __instance.visible = _reader.ReadBoolean();
            return false;
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
                GetController(SelectedCharacter as OCIChar).AddLink(SelectedObject, IKGuideObjects[SelectedGuideObject]);
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

        public static AnimationControllerController GetController(ChaControl character) => character?.gameObject?.GetComponent<AnimationControllerController>();
        public static AnimationControllerController GetController(OCIChar character) => character?.charInfo?.gameObject?.GetComponent<AnimationControllerController>();

        public class AnimationControllerController : CharaCustomFunctionController
        {
            private Dictionary<OCIChar.IKInfo, ObjectCtrlInfo> GuideObjectLinksV1 = new Dictionary<OCIChar.IKInfo, ObjectCtrlInfo>();
            private Dictionary<OCIChar.IKInfo, ObjectCtrlInfo> GuideObjectLinks = new Dictionary<OCIChar.IKInfo, ObjectCtrlInfo>();
            private readonly List<OCIChar.IKInfo> LinksToRemove = new List<OCIChar.IKInfo>();
            private ObjectCtrlInfo EyeLink;
            private ObjectCtrlInfo NeckLink;
            private bool DoImportOnLoad = false;

            protected override void OnCardBeingSaved(GameMode currentGameMode)
            {
                try
                {
                    var data = new PluginData();
                    Dictionary<string, int> LinksV1 = new Dictionary<string, int>();
                    Dictionary<string, int> Links = new Dictionary<string, int>();
                    foreach (var link in GuideObjectLinksV1)
                        LinksV1.Add(link.Key.boneObject.name, GetObjectDicObjectCtrlKey(link.Value));
                    if (LinksV1.Count != 0)
                        data.data.Add("LinksV1", LinksV1);

                    foreach (var link in GuideObjectLinks)
                        Links.Add(link.Key.boneObject.name, GetObjectDicObjectCtrlKey(link.Value));
                    if (Links.Count != 0)
                        data.data.Add("Links", Links);

                    if (EyeLink != null)
                        data.data.Add("Eyes", GetObjectDicObjectCtrlKey(EyeLink));
                    if (NeckLink != null)
                        data.data.Add("Neck", GetObjectDicObjectCtrlKey(NeckLink));

                    if (data.data.Count == 0)
                        data.data = null;
                    data.version = 2;
                    SetExtendedData(data);

                    //Clear out the old style data
                    PluginData OldData = ExtendedSave.GetSceneExtendedDataById(PluginNameInternal);
                    if (OldData != null)
                        OldData = null;
                    ExtendedSave.SetSceneExtendedDataById(PluginNameInternal, new PluginData { data = null });

                    Logger.Log(LogLevel.Debug, $"Saved KK_AnimationController animations for character {ChaControl.chaFile.parameter.fullname.Trim()}");
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error | LogLevel.Message, "Could not save KK_AnimationController animations.");
                    Logger.Log(LogLevel.Error, ex.ToString());
                }
            }

            protected override void Awake()
            {
                base.Awake();
                if (DoingImport)
                    DoImportOnLoad = true;
            }

            protected override void OnReload(GameMode currentGameMode)
            {
                try
                {
                    PluginData ExtendedData = ExtendedSave.GetSceneExtendedDataById(PluginNameInternal);

                    //Version 1 save data
                    if (ExtendedData?.data != null && ExtendedData.data.ContainsKey("AnimationInfo"))
                    {
                        int key = GetCharacterDicObjectCtrlKey();
                        List<AnimationControllerInfo> AnimationControllerInfoList = ((object[])ExtendedData.data["AnimationInfo"]).Select(x => AnimationControllerInfo.Unserialize((byte[])x)).ToList();

                        foreach (var AnimInfo in AnimationControllerInfoList)
                        {
                            //See if this is the right character
                            if (DoImportOnLoad)
                            {
                                if (ImportDictionary[AnimInfo.CharDicKey] != key)
                                    continue;
                            }
                            else if (AnimInfo.CharDicKey != key)
                                continue;

                            int objectID = DoImportOnLoad ? ImportDictionary[AnimInfo.ItemDicKey] : AnimInfo.ItemDicKey;
                            ObjectCtrlInfo linkedItem = Studio.Studio.Instance.dicObjectCtrl.First(x => x.Key == objectID).Value;

                            if (AnimInfo.Version.IsNullOrEmpty())
                                AddLinkV1(linkedItem, AnimInfo.IKPart);
                            else
                                AddLink(linkedItem, AnimInfo.IKPart);
                        }
                    }
                    //Version 2 save data
                    else
                    {
                        var data = GetExtendedData();
                        if (data?.data != null)
                        {
                            if (data.data.TryGetValue("LinksV1", out var loadedLinksV1) && loadedLinksV1 != null)
                            {
                                Dictionary<object, object> LinksV1 = (Dictionary<object, object>)loadedLinksV1;
                                foreach (var link in LinksV1)
                                    if (DoImportOnLoad)
                                        GuideObjectLinksV1.Add(GetOCIChar().listIKTarget.Where(x => x.boneObject.name == link.Key.ToString()).First(), Studio.Studio.Instance.dicObjectCtrl[ImportDictionary[(int)link.Value]]);
                                    else
                                        GuideObjectLinksV1.Add(GetOCIChar().listIKTarget.Where(x => x.boneObject.name == link.Key.ToString()).First(), Studio.Studio.Instance.dicObjectCtrl[(int)link.Value]);
                            }
                            if (data.data.TryGetValue("Links", out var loadedLinks) && loadedLinks != null)
                            {
                                Dictionary<object, object> Links = (Dictionary<object, object>)loadedLinksV1;
                                foreach (var link in Links)
                                    if (DoImportOnLoad)
                                        GuideObjectLinks.Add(GetOCIChar().listIKTarget.Where(x => x.boneObject.name == link.Key.ToString()).First(), Studio.Studio.Instance.dicObjectCtrl[ImportDictionary[(int)link.Value]]);
                                    else
                                        GuideObjectLinks.Add(GetOCIChar().listIKTarget.Where(x => x.boneObject.name == link.Key.ToString()).First(), Studio.Studio.Instance.dicObjectCtrl[(int)link.Value]);
                            }
                            if (data.data.TryGetValue("Eyes", out var loadedEyeLink) && loadedEyeLink != null)
                            {
                                int key = (int)loadedEyeLink;
                                if (DoImportOnLoad)
                                    EyeLink = Studio.Studio.Instance.dicObjectCtrl[ImportDictionary[key]];
                                else
                                    EyeLink = Studio.Studio.Instance.dicObjectCtrl[key];
                            }
                            if (data.data.TryGetValue("Neck", out var loadedNeckLink) && loadedNeckLink != null)
                            {
                                int key = (int)loadedNeckLink;
                                if (DoImportOnLoad)
                                    NeckLink = Studio.Studio.Instance.dicObjectCtrl[ImportDictionary[key]];
                                else
                                    NeckLink = Studio.Studio.Instance.dicObjectCtrl[key];
                            }
                        }
                    }
                    Logger.Log(LogLevel.Debug, $"Loaded KK_AnimationController animations for character {ChaControl.chaFile.parameter.fullname.Trim()}");
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error | LogLevel.Message, "Could not load KK_AnimationController animations.");
                    Logger.Log(LogLevel.Error, ex.ToString());
                }
                DoImportOnLoad = false;
            }

            protected override void Update()
            {
                //Every update, match the part to the object
                if (EyeLink != null)
                    ChaControl.eyeLookCtrl.target = GetTransformFromObjectCtrl(EyeLink);
                if (NeckLink != null)
                    ChaControl.eyeLookCtrl.target = GetTransformFromObjectCtrl(NeckLink);

                foreach (var ikInfo in LinksToRemove)
                {
                    GuideObjectLinksV1.Remove(ikInfo);
                    GuideObjectLinks.Remove(ikInfo);
                }

                foreach (var link in GuideObjectLinksV1)
                {
                    if (link.Value == null)
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
                    if (link.Value == null)
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
            internal void AddLinkV1(ObjectCtrlInfo selectedObject, string selectedGuideObject)
            {
                OCIChar.IKInfo ikInfo = GetOCIChar().listIKTarget.Where(x => x.boneObject.name == selectedGuideObject).First();
                GuideObjectLinksV1[ikInfo] = selectedObject;
            }

            public void AddLink(ObjectCtrlInfo selectedObject, string selectedGuideObject)
            {
                if (selectedGuideObject == "eyes")
                    AddEyeLink(selectedObject);
                else if (selectedGuideObject == "neck")
                    AddNeckLink(selectedObject);
                else
                {
                    OCIChar.IKInfo ikInfo = GetOCIChar().listIKTarget.Where(x => x.boneObject.name == selectedGuideObject).First();
                    GuideObjectLinks[ikInfo] = selectedObject;
                }
            }
            public void AddEyeLink(ObjectCtrlInfo selectedObject) => EyeLink = selectedObject;
            public void AddNeckLink(ObjectCtrlInfo selectedObject) => NeckLink = selectedObject;
            public void RemoveLink(string selectedGuideObject)
            {
                if (selectedGuideObject == "eyes")
                    RemoveEyeLink();
                else if (selectedGuideObject == "neck")
                    RemoveNeckLink();
                else
                {
                    OCIChar.IKInfo ikInfo = GetOCIChar().listIKTarget.Where(x => x.boneObject.name == selectedGuideObject).First();
                    LinksToRemove.Add(ikInfo);
                }
            }
            public void RemoveLink(OCIChar.IKInfo ikInfo) => LinksToRemove.Add(ikInfo);
            public void RemoveEyeLink() => EyeLink = null;
            public void RemoveNeckLink() => NeckLink = null;
            public OCIChar GetOCIChar() => Studio.Studio.Instance.dicInfo.Values.OfType<OCIChar>().Single(x => x.charInfo == ChaControl);
            public int GetCharacterDicObjectCtrlKey() => Studio.Studio.Instance.dicObjectCtrl.FirstOrDefault(x => x.Value == GetOCIChar()).Key;
            public int GetObjectDicObjectCtrlKey(ObjectCtrlInfo objectCtrlInfo) => Studio.Studio.Instance.dicObjectCtrl.FirstOrDefault(x => x.Value == objectCtrlInfo).Key;
            private Transform GetTransformFromObjectCtrl(ObjectCtrlInfo objectCtrlInfo)
            {
                switch (objectCtrlInfo)
                {
                    case OCIItem Item:
                        return Item.childRoot;
                    case OCIFolder Folder:
                        return Folder.childRoot;
                    case OCIRoute Route:
                        return Route.childRoot;
                    default:
                        return null;
                }
            }
        }
    }
}
