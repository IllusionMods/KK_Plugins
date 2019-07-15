using BepInEx;
using BepInEx.Bootstrap;
using CommonCode;
using Harmony;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Studio.SaveLoad;
using Studio;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KK_MaterialEditor
{
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class KK_MaterialEditor : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.materialeditor";
        public const string PluginName = "Material Editor";
        public const string Version = "1.2";

        public static readonly string ExportPath = Path.Combine(Paths.GameRootPath, @"UserData\MaterialEditor");
        public static readonly string XMLPath = Path.Combine(Paths.PluginPath, nameof(KK_MaterialEditor));

        internal static Dictionary<string, Dictionary<string, string>> XMLShaderProperties = new Dictionary<string, Dictionary<string, string>>();
        internal static Dictionary<string, string> XMLShaderPropertiesAll = new Dictionary<string, string>();
        internal static Dictionary<string, Shader> LoadedShaders = new Dictionary<string, Shader>();

        [DisplayName("Enable advanced editing")]
        [Category("Config")]
        [Description("Enables advanced editing of characters in the character maker. Note: Some textures and colors will override chracter maker selections but will not always appear to do so, especially after changing them from the in game color pickers. Save and reload to see the real effects.\nUse at your own risk.")]
        public static ConfigWrapper<bool> AdvancedMode { get; private set; }

        private void Main()
        {
            Directory.CreateDirectory(ExportPath);

            SceneManager.sceneLoaded += (s, lsm) => InitStudioUI(s.name);
            MakerAPI.MakerBaseLoaded += MakerAPI_MakerBaseLoaded;
            AccessoriesApi.SelectedMakerAccSlotChanged += AccessoriesApi_SelectedMakerAccSlotChanged;
            AccessoriesApi.AccessoryKindChanged += AccessoriesApi_AccessoryKindChanged;
            AccessoriesApi.AccessoriesCopied += AccessoriesApi_AccessoriesCopied;
            AccessoriesApi.AccessoryTransferred += AccessoriesApi_AccessoryTransferred;

            CharacterApi.RegisterExtraBehaviour<MaterialEditorCharaController>(GUID);
            StudioSaveLoadApi.RegisterExtraBehaviour<MaterialEditorSceneController>(GUID);

            HarmonyInstance.Create(GUID).PatchAll(typeof(Hooks));

            AdvancedMode = new ConfigWrapper<bool>(nameof(AdvancedMode), PluginName, false);
        }

        private void AccessoriesApi_AccessoryTransferred(object sender, AccessoryTransferEventArgs e) => GetCharaController(MakerAPI.GetCharacterControl())?.AccessoryTransferredEvent(sender, e);
        private void AccessoriesApi_AccessoriesCopied(object sender, AccessoryCopyEventArgs e) => GetCharaController(MakerAPI.GetCharacterControl())?.AccessoriesCopiedEvent(sender, e);
        private void AccessoriesApi_AccessoryKindChanged(object sender, AccessorySlotEventArgs e) => GetCharaController(MakerAPI.GetCharacterControl())?.AccessoryKindChangeEvent(sender, e);
        private void AccessoriesApi_SelectedMakerAccSlotChanged(object sender, AccessorySlotEventArgs e) => GetCharaController(MakerAPI.GetCharacterControl())?.AccessorySelectedSlotChangeEvent(sender, e);

        private static bool SetFloatProperty(GameObject go, string materialName, string property, string value, ObjectType objectType)
        {
            float floatValue = float.Parse(value);
            bool didSet = false;

            foreach (var obj in GetRendererList(go, objectType))
                foreach (var objMat in obj.materials)
                    if (objMat.NameFormatted() == materialName)
                    {
                        objMat.SetFloat($"_{property}", floatValue);
                        didSet = true;
                    }
            return didSet;
        }

        private static bool SetColorProperty(GameObject go, string materialName, string property, Color value, ObjectType objectType)
        {
            bool didSet = false;

            foreach (var obj in GetRendererList(go, objectType))
                foreach (var objMat in obj.materials)
                    if (objMat.NameFormatted() == materialName)
                    {
                        objMat.SetColor($"_{property}", value);
                        didSet = true;
                    }
            return didSet;
        }

        private static void SetColorRProperty(GameObject go, Material mat, string property, string value, ObjectType objectType)
        {
            float floatValue = float.Parse(value);
            Color colorOrig = mat.GetColor($"_{property}");

            foreach (var obj in GetRendererList(go, objectType))
                foreach (var objMat in obj.materials)
                    if (objMat.NameFormatted() == mat.NameFormatted())
                        objMat.SetColor($"_{property}", new Color(floatValue, colorOrig.g, colorOrig.b, colorOrig.a));
        }

        private static void SetColorGProperty(GameObject go, Material mat, string property, string value, ObjectType objectType)
        {
            float floatValue = float.Parse(value);
            Color colorOrig = mat.GetColor($"_{property}");

            foreach (var obj in GetRendererList(go, objectType))
                foreach (var objMat in obj.materials)
                    if (objMat.NameFormatted() == mat.NameFormatted())
                        objMat.SetColor($"_{property}", new Color(colorOrig.r, floatValue, colorOrig.b, colorOrig.a));
        }

        private static void SetColorBProperty(GameObject go, Material mat, string property, string value, ObjectType objectType)
        {
            float floatValue = float.Parse(value);
            Color colorOrig = mat.GetColor($"_{property}");

            foreach (var obj in GetRendererList(go, objectType))
                foreach (var objMat in obj.materials)
                    if (objMat.NameFormatted() == mat.NameFormatted())
                        objMat.SetColor($"_{property}", new Color(colorOrig.r, colorOrig.g, floatValue, colorOrig.a));
        }

        private static void SetColorAProperty(GameObject go, Material mat, string property, string value, ObjectType objectType)
        {
            float floatValue = float.Parse(value);
            Color colorOrig = mat.GetColor($"_{property}");

            foreach (var obj in GetRendererList(go, objectType))
                foreach (var objMat in obj.materials)
                    if (objMat.NameFormatted() == mat.NameFormatted())
                        objMat.SetColor($"_{property}", new Color(colorOrig.r, colorOrig.g, colorOrig.b, floatValue));
        }

        private static bool SetRendererProperty(GameObject go, string rendererName, RendererProperties property, string value, ObjectType objectType) => SetRendererProperty(go, rendererName, property, int.Parse(value), objectType);
        private static bool SetRendererProperty(GameObject go, string rendererName, RendererProperties property, int value, ObjectType objectType)
        {
            bool didSet = false;
            foreach (var rend in GetRendererList(go, objectType))
            {
                if (rend.NameFormatted() == rendererName)
                {
                    if (property == RendererProperties.ShadowCastingMode)
                    {
                        rend.shadowCastingMode = (UnityEngine.Rendering.ShadowCastingMode)value;
                        didSet = true;
                    }
                    else if (property == RendererProperties.ReceiveShadows)
                    {
                        rend.receiveShadows = value == 1;
                        didSet = true;
                    }
                    else if (property == RendererProperties.Enabled)
                    {
                        rend.enabled = value == 1;
                        didSet = true;
                    }
                }
            }
            return didSet;
        }

        private static bool SetTextureProperty(GameObject go, string materialName, string property, Texture2D value, ObjectType objectType)
        {
            bool didSet = false;
            foreach (var rend in GetRendererList(go, objectType))
                foreach (var mat in rend.materials)
                    if (mat.NameFormatted() == materialName)
                    {
                        mat.SetTexture($"_{property}", value);
                        didSet = true;
                    }
            return didSet;
        }

        public static Texture2D TextureFromBytes(byte[] texBytes, TextureFormat format = TextureFormat.ARGB32)
        {
            if (texBytes == null || texBytes.Length == 0) return null;

            var tex = new Texture2D(2, 2, format, false);
            tex.LoadImage(texBytes);
            return tex;
        }
        /// <summary>
        /// When geting the renderers, GetComponentsInChildren cannot be used on the body or it causes problems. This method constructs a list without using GetComponentsInChildren.
        /// </summary>
        private static List<Renderer> GetRendererList(GameObject go, ObjectType objectType = ObjectType.Other)
        {
            List<Renderer> rendList = new List<Renderer>();

            if (objectType == ObjectType.Character)
                _GetRendererList(go, rendList);
            else
                rendList = go.GetComponentsInChildren<Renderer>(true).ToList();

            return rendList;
        }
        /// <summary>
        /// Recursively iterates over game objects to create the list. Use GetRendererList intead.
        /// </summary>
        private static void _GetRendererList(GameObject go, List<Renderer> rendList)
        {
            if (go == null) return;

            //Iterating over o_body_a destroys the body mask for some reason so we skip it. Iterating over cf_O_face destroys juice textures.
            if (go.name != "o_body_a" && go.name != "cf_O_face")
            {
                Renderer rend = go.GetComponent<Renderer>();
                if (rend != null && BodyParts.Contains(rend.NameFormatted()))
                    rendList.Add(rend);
            }

            for (int i = 0; i < go.transform.childCount; i++)
                _GetRendererList(go.transform.GetChild(i).gameObject, rendList);
        }

        public enum ObjectType { StudioItem, Clothing, Accessory, Hair, Character, Other };
        private static int GetObjectID(ObjectCtrlInfo oci) => Studio.Studio.Instance.dicObjectCtrl.First(x => x.Value == oci).Key;
        public static MaterialEditorSceneController GetSceneController() => Chainloader.ManagerObject.transform.GetComponentInChildren<MaterialEditorSceneController>();
        public static MaterialEditorCharaController GetCharaController(ChaControl character) => character?.gameObject?.GetComponent<MaterialEditorCharaController>();
    }
}