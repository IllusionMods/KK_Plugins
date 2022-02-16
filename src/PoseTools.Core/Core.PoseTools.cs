using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using HarmonyLib;
using MessagePack;
using Studio;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using static KK_Plugins.PoseTools.PoseToolsConstants;

namespace KK_Plugins.PoseTools
{
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.deathweasel.bepinex.posefolders";
        public const string PluginName = "Pose Tools";
        public const string PluginNameInternal = Constants.Prefix + "_PoseTools";
        public const string PluginVersion = "1.0";

        internal static readonly int UserdataRoot = new DirectoryInfo(UserdataFolder).FullName.Length + 1;  //+1 for slash
        internal static DirectoryInfo CurrentDirectory = new DirectoryInfo(UserdataFolder + "/" + PoseFolder);
        internal static readonly int DefaultRootLength = CurrentDirectory.FullName.Length;
        internal static GameObject v_prefabNode;
        internal static Transform v_transformRoot;

        internal static new ManualLogSource Logger;

        internal static ConfigEntry<OrderBy> ConfigOrderBy { get; private set; }
        internal static ConfigEntry<bool> ConfigSavePng { get; private set; }
        internal static ConfigEntry<bool> ConfigPoseNamePrefix { get; private set; }
        internal static ConfigEntry<bool> ConfigLoadExpression { get; private set; }
        internal static ConfigEntry<bool> ConfigLoadSkirtFK { get; private set; }

        internal void Main()
        {
            Logger = base.Logger;

            ConfigOrderBy = Config.Bind("Config", "Order by", OrderBy.Filename, "How to order the pose list.");
            ConfigSavePng = Config.Bind("Config", "Save pose data as png", false, "When enabled, pose data will be saved as a small .png with embedded pose data.");
            ConfigPoseNamePrefix = Config.Bind("Config", "Save pose as filename prefix", true, "When enabled, the filename will be saved with the name of the pose followed by the date.");
            ConfigLoadExpression = Config.Bind("Config", "Load Expression", true, "When loading a pose, facial expression will be changed to the one saved with the pose data, if any.");
            ConfigLoadSkirtFK = Config.Bind("Config", "Load Skirt FK", true, "When loading a pose, skirt FK will be loaded from the pose data, if any.");

            if (!CurrentDirectory.Exists)
                CurrentDirectory.Create();
            Harmony.CreateAndPatchAll(typeof(Hooks));

            ExtendedSave.PoseBeingLoaded += ExtendedSave_PoseBeingLoaded;
            ExtendedSave.PoseBeingSaved += ExtendedSave_PoseBeingSaved;
        }

        private void ExtendedSave_PoseBeingLoaded(string poseName, PauseCtrl.FileInfo fileInfo, OCIChar ociChar)
        {
            bool loadSkirtFK = false;
            PluginData data = ExtendedSave.GetPoseExtendedDataById(PoseToolsData);
            if (data != null)
            {
                bool loadExpression = false;

                //Only load expression data for the game this pose was created on, eye etc. patterns wouldn't match otherwise
#if KK || KKS
                if (data.data.TryGetValue(GameData, out var gameData))
                    if ((string)gameData == "KK" || (string)gameData == "KKS")
                        loadExpression = true;
#elif AI || HS2
                if (data.data.TryGetValue(GameData, out var gameData))
                    if ((string)gameData == "AI" || (string)gameData == "HS2")
                        loadExpression = true;
#endif

                if (loadExpression && ConfigLoadExpression.Value)
                {
                    if (data.data.TryGetValue(EyebrowPatternData, out var eyebrowPatternData))
                        ociChar.charInfo.ChangeEyebrowPtn((int)eyebrowPatternData);
                    if (data.data.TryGetValue(EyesPatternData, out var eyesPatternData))
                        ociChar.charInfo.ChangeEyesPtn((int)eyesPatternData);
                    if (data.data.TryGetValue(MouthPatternData, out var mouthPatternData))
                        ociChar.charInfo.ChangeMouthPtn((int)mouthPatternData);
                    if (data.data.TryGetValue(EyeOpenData, out var eyeOpenData))
                        ociChar.ChangeEyesOpen((float)eyeOpenData);
                    if (data.data.TryGetValue(MouthOpenData, out var mouthOpenData))
                        ociChar.ChangeMouthOpen((float)mouthOpenData);
                }

                if (ConfigLoadSkirtFK.Value && data.data.TryGetValue(SkirtFKData, out var skirtFKData) && skirtFKData != null)
                {
                    loadSkirtFK = true;
                    Dictionary<int, Vector3> skirtFK = MessagePackSerializer.Deserialize<Dictionary<int, Vector3>>((byte[])skirtFKData);
                    foreach (KeyValuePair<int, Vector3> item in skirtFK)
                    {
                        ociChar.oiCharInfo.bones[item.Key].changeAmount.rot = item.Value;
                    }
                }
            }

            if (!loadSkirtFK)
            {
                //Disable skirt FK if there was none to load since it's worthless to have enabled
                StartCoroutine(DisableSkirtFK(ociChar));
                ociChar.ActiveFK(OIBoneInfo.BoneGroup.Skirt, false);
            }
        }

        private IEnumerator DisableSkirtFK(OCIChar ociChar)
        {
            yield return null;
            ociChar.ActiveFK(OIBoneInfo.BoneGroup.Skirt, false);
        }

        private void ExtendedSave_PoseBeingSaved(string poseName, PauseCtrl.FileInfo fileInfo, OCIChar ociChar)
        {
            var data = new PluginData();
            data.data.Add(GameData, Constants.Prefix);
            data.data.Add(EyebrowPatternData, ociChar.charFileStatus.eyebrowPtn);
            data.data.Add(EyesPatternData, ociChar.charFileStatus.eyesPtn);
            data.data.Add(MouthPatternData, ociChar.charFileStatus.mouthPtn);
            data.data.Add(EyeOpenData, ociChar.charFileStatus.eyesOpenMax);
            data.data.Add(MouthOpenData, ociChar.oiCharInfo.mouthOpen);

            if (ociChar.oiCharInfo.activeFK[SkirtFKIndex])
            {
                Dictionary<int, Vector3> skirtFK = new Dictionary<int, Vector3>();
                foreach (KeyValuePair<int, OIBoneInfo> item2 in ociChar.oiCharInfo.bones.Where(b => (OIBoneInfo.BoneGroup.Skirt & b.Value.group) != 0))
                    skirtFK.Add(item2.Key, item2.Value.changeAmount.rot);
                data.data.Add(SkirtFKData, MessagePackSerializer.Serialize(skirtFK));
            }

            ExtendedSave.SetPoseExtendedDataById(PoseToolsData, data);
        }

        internal static string GetFolder()
        {
            if (!CurrentDirectory.Exists)
                CurrentDirectory = new DirectoryInfo(UserdataFolder + "/" + PoseFolder);
            return CurrentDirectory.FullName.Substring(UserdataRoot);
        }

        internal static Transform AddListButton(string text, UnityAction callback)
        {
            var prefabNode = Instantiate(v_prefabNode, v_transformRoot, false);
            var component = prefabNode.GetComponent<StudioNode>();
            component.active = true;
            component.addOnClick = callback;
            component.text = text;

            return prefabNode.transform;
        }

        public enum OrderBy { Date, Filename }
    }
}
