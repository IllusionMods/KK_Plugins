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
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using static KK_Plugins.PoseTools.PoseToolsConstants;

namespace KK_Plugins.PoseTools
{
    [BepInProcess(Constants.StudioProcessName)]
    [BepInDependency(ExtendedSave.GUID, ExtendedSave.Version)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.deathweasel.bepinex.posefolders";
        public const string PluginName = "Pose Tools";
        public const string PluginNameInternal = Constants.Prefix + "_PoseTools";
        public const string PluginVersion = "1.1.2";

        internal static readonly int UserdataRoot = new DirectoryInfo(UserdataFolder).FullName.Length + 1; //+1 for slash
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
            ConfigSavePng = Config.Bind("Config", "Save pose data as png", true, "When enabled, pose data will be saved as a small .png with embedded pose data.");
            ConfigPoseNamePrefix = Config.Bind("Config", "Save pose as filename prefix", true, "When enabled, the filename will be saved with the name of the pose followed by the date.");
            ConfigLoadExpression = Config.Bind("Config", "Load Expression", true, "When loading a pose, facial expression will be changed to the one saved with the pose data, if any.");
            ConfigLoadSkirtFK = Config.Bind("Config", "Load Skirt FK", true, "When loading a pose, skirt FK will be loaded from the pose data, if any.");

            if (!CurrentDirectory.Exists)
                CurrentDirectory.Create();
            Harmony.CreateAndPatchAll(typeof(Hooks));

            ExtendedSave.PoseBeingLoaded += ExtendedSave_PoseBeingLoaded;
            ExtendedSave.PoseBeingSaved += ExtendedSave_PoseBeingSaved;
        }

        private void ExtendedSave_PoseBeingLoaded(string poseName, PauseCtrl.FileInfo fileInfo, OCIChar ociChar, ExtendedSave.GameNames gameName)
        {
            bool loadSkirtFK = false;
            PluginData data = ExtendedSave.GetPoseExtendedDataById(PoseToolsData);
            if (data != null)
            {
                bool sameGame = false;

                //Only load expression data for the game this pose was created on, eye etc. patterns wouldn't match otherwise
#if KK || KKS
                if (gameName == ExtendedSave.GameNames.Koikatsu || gameName == ExtendedSave.GameNames.KoikatsuSunshine)
                    sameGame = true;
#elif AI || HS2
                if (gameName == ExtendedSave.GameNames.AIGirl || gameName == ExtendedSave.GameNames.HoneySelect2)
                    sameGame = true;
#elif PH
                if (gameName == ExtendedSave.GameNames.PlayHome)
                    sameGame = true;
#endif

                //Facial expression
                if (sameGame && ConfigLoadExpression.Value)
                {
#if !PH
                    if (data.data.TryGetValue(EyebrowPatternData, out var eyebrowPatternData))
                        ociChar.charInfo.ChangeEyebrowPtn((int)eyebrowPatternData);
#endif
                    if (data.data.TryGetValue(EyesPatternData, out var eyesPatternData))
                        ociChar.charInfo.ChangeEyesPtn((int)eyesPatternData);
                    if (data.data.TryGetValue(MouthPatternData, out var mouthPatternData))
                        ociChar.charInfo.ChangeMouthPtn((int)mouthPatternData);
                    if (data.data.TryGetValue(EyeOpenData, out var eyeOpenData))
                        ociChar.ChangeEyesOpen((float)eyeOpenData);
                    if (data.data.TryGetValue(MouthOpenData, out var mouthOpenData))
                        ociChar.ChangeMouthOpen((float)mouthOpenData);
                }

                //Skirt FK
                if (sameGame && ConfigLoadSkirtFK.Value && data.data.TryGetValue(SkirtFKData, out var skirtFKData) && skirtFKData != null)
                {
                    loadSkirtFK = true;
                    Dictionary<int, Vector3> skirtFK = MessagePackSerializer.Deserialize<Dictionary<int, Vector3>>((byte[])skirtFKData);
                    foreach (KeyValuePair<int, Vector3> item in skirtFK)
                    {
                        ociChar.oiCharInfo.bones[item.Key].changeAmount.rot = item.Value;
                    }
                }

                //Joint correction
                if (data.data.TryGetValue(JointCorrectionData, out var jointCorrectionData) && jointCorrectionData != null)
                {
                    bool[] expression = MessagePackSerializer.Deserialize<bool[]>((byte[])jointCorrectionData);
                    //Skip the first 4 since those are handled by vanilla code
                    for (int i = 4; i < expression.Length; i++)
                    {
                        ociChar.EnableExpressionCategory(i, expression[i]);
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

        private void ExtendedSave_PoseBeingSaved(string poseName, PauseCtrl.FileInfo fileInfo, OCIChar ociChar, ExtendedSave.GameNames gameName)
        {
            var data = new PluginData();

            //Facial expression
#if PH
            data.data.Add(EyesPatternData, ociChar.charStatus.eyesPtn);
            data.data.Add(MouthPatternData, ociChar.charStatus.mouthPtn);
            data.data.Add(EyeOpenData, ociChar.charStatus.eyesOpenMax);
            data.data.Add(MouthOpenData, ociChar.oiCharInfo.mouthOpen);
#else
            data.data.Add(EyebrowPatternData, ociChar.charFileStatus.eyebrowPtn);
            data.data.Add(EyesPatternData, ociChar.charFileStatus.eyesPtn);
            data.data.Add(MouthPatternData, ociChar.charFileStatus.mouthPtn);
            data.data.Add(EyeOpenData, ociChar.charFileStatus.eyesOpenMax);
            data.data.Add(MouthOpenData, ociChar.oiCharInfo.mouthOpen);
#endif

            //Only save skirt FK if enabled
            if (ociChar.oiCharInfo.activeFK[SkirtFKIndex])
            {
                Dictionary<int, Vector3> skirtFK = new Dictionary<int, Vector3>();
                foreach (KeyValuePair<int, OIBoneInfo> item2 in ociChar.oiCharInfo.bones.Where(b => (OIBoneInfo.BoneGroup.Skirt & b.Value.group) != 0))
                    skirtFK.Add(item2.Key, item2.Value.changeAmount.rot);
                data.data.Add(SkirtFKData, MessagePackSerializer.Serialize(skirtFK));
            }

            //Joint correction
            data.data.Add(JointCorrectionData, MessagePackSerializer.Serialize(ociChar.oiCharInfo.expression));

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

        public static Texture2D OverwriteTexture(Texture2D background, Texture2D watermark, int startX, int startY)
        {
            Texture2D newTex = new Texture2D(background.width, background.height, background.format, false);
            for (int x = 0; x < background.width; x++)
            {
                for (int y = 0; y < background.height; y++)
                {
                    if (x >= startX && y >= startY && x - startX < watermark.width && y - startY < watermark.height)
                    {
                        Color bgColor = background.GetPixel(x, y);
                        Color wmColor = watermark.GetPixel(x - startX, y - startY);

                        Color final_color = Color.Lerp(bgColor, wmColor, wmColor.a);
                        final_color.a = bgColor.a + wmColor.a;

                        newTex.SetPixel(x, y, final_color);
                    }
                    else
                    {
                        newTex.SetPixel(x, y, background.GetPixel(x, y));
                    }
                }
            }

            newTex.Apply();
            return newTex;
        }

        internal static Texture2D GetT2D(RenderTexture renderTexture)
        {
            var currentActiveRT = RenderTexture.active;
            RenderTexture.active = renderTexture;
            var tex = new Texture2D(renderTexture.width, renderTexture.height);
            tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
            RenderTexture.active = currentActiveRT;
            return tex;
        }

        internal static Texture2D TextureFromBytes(byte[] texBytes, TextureFormat format = TextureFormat.ARGB32, bool mipmaps = true)
        {
            if (texBytes == null || texBytes.Length == 0) return null;

            Texture2D tex = null;
            RenderTexture rt = null;

            try
            {
                //LoadImage automatically resizes the texture so the texture size doesn't matter here
                tex = new Texture2D(2, 2, format, mipmaps);
                tex.LoadImage(texBytes);

                rt = new RenderTexture(tex.width, tex.height, 0);
                rt.useMipMap = mipmaps;
                Graphics.Blit(tex, rt);

                return GetT2D(rt);
            }
            finally
            {
                if (rt != null)
                    UnityEngine.Object.Destroy(rt);

                if (tex != null)
                    UnityEngine.Object.Destroy(tex);
            }
        }

        internal static void LoadWatermark()
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{nameof(KK_Plugins)}.Resources.watermark.png"))
            {
                byte[] bytesInStream = new byte[stream.Length];
                stream.Read(bytesInStream, 0, bytesInStream.Length);
                Watermark = TextureFromBytes(bytesInStream, mipmaps: false);
            }
        }

        private static Texture2D _watermark;
        internal static Texture2D Watermark
        {
            get
            {
                if (_watermark == null)
                    LoadWatermark();
                return _watermark;
            }
            set
            {
                _watermark = value;
            }
        }

        public enum OrderBy { Date, Filename }
    }
}
