using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Harmony;
using BepInEx.Logging;
using KK_Plugins.CommonCode;
using KKAPI.Chara;
using KKAPI.Maker;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using HarmonyLib;
#if KK
using KKAPI.Studio.SaveLoad;
using Studio;
#endif
#if AI
using AIChara;
#endif

namespace KK_Plugins
{
    public partial class MaterialEditor
    {
        public const string GUID = "com.deathweasel.bepinex.materialeditor";
        public const string PluginName = "Material Editor";
        public const string Version = "1.6";
        internal static new ManualLogSource Logger;

        public static readonly string ExportPath = Path.Combine(Paths.GameRootPath, @"UserData\MaterialEditor");
        public static readonly string XMLPath = Path.Combine(Paths.PluginPath, PluginNameInternal);

        internal static Dictionary<string, ShaderData> LoadedShaders = new Dictionary<string, ShaderData>();
        internal static SortedDictionary<string, Dictionary<string, ShaderPropertyData>> XMLShaderProperties = new SortedDictionary<string, Dictionary<string, ShaderPropertyData>>();

        public static ConfigEntry<bool> AdvancedMode { get; private set; }

        internal void Main()
        {
            Logger = base.Logger;
            Directory.CreateDirectory(ExportPath);

            MakerAPI.MakerBaseLoaded += MakerAPI_MakerBaseLoaded;
            MakerAPI.RegisterCustomSubCategories += MakerAPI_RegisterCustomSubCategories;
            AccessoriesApi.SelectedMakerAccSlotChanged += AccessoriesApi_SelectedMakerAccSlotChanged;
            AccessoriesApi.AccessoryKindChanged += AccessoriesApi_AccessoryKindChanged;
            AccessoriesApi.AccessoryTransferred += AccessoriesApi_AccessoryTransferred;

            CharacterApi.RegisterExtraBehaviour<MaterialEditorCharaController>(GUID);

#if KK
            SceneManager.sceneLoaded += (s, lsm) => InitStudioUI(s.name);
            AccessoriesApi.AccessoriesCopied += AccessoriesApi_AccessoriesCopied;
            StudioSaveLoadApi.RegisterExtraBehaviour<MaterialEditorSceneController>(GUID);
#endif

            AdvancedMode = Config.AddSetting("Config", "Enable advanced editing", false, "Enables advanced editing of characters in the character maker. Note: Some textures and colors will override chracter maker selections but will not always appear to do so, especially after changing them from the in game color pickers. Save and reload to see the real effects.\nUse at your own risk.");

            LoadXML();
            var harmony = HarmonyWrapper.PatchAll(typeof(Hooks));

#if AI
            foreach (var method in typeof(CharaCustom.CustomClothesPatternSelect).GetMethods().Where(x => x.Name.Contains("<ChangeLink>")))
                harmony.Patch(method, new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.OverrideHook), AccessTools.all)));

            foreach (var method in typeof(CharaCustom.CustomClothesColorSet).GetMethods().Where(x => x.Name.Contains("<Initialize>")))
                harmony.Patch(method, new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.OverrideHook), AccessTools.all)));
#endif
        }

        private void LoadXML()
        {
            var loadedManifests = Sideloader.Sideloader.LoadedManifests;
            XMLShaderProperties["default"] = new Dictionary<string, ShaderPropertyData>();

            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{nameof(KK_Plugins)}.Resources.default.xml"))
            using (XmlReader reader = XmlReader.Create(stream))
                LoadXML(XDocument.Load(reader).Element(PluginNameInternal));

            foreach (var manifest in loadedManifests)
                LoadXML(manifest.manifestDocument?.Root?.Element(PluginNameInternal));
        }

        private void LoadXML(XElement materialEditorElement)
        {
            if (materialEditorElement == null) return;

            foreach (var shaderElement in materialEditorElement.Elements("Shader"))
            {
                string shaderName = shaderElement.Attribute("Name").Value;

                LoadedShaders[shaderName] = new ShaderData(shaderName, shaderElement.Attribute("AssetBundle")?.Value, shaderElement.Attribute("RenderQueue")?.Value, shaderElement.Attribute("Asset")?.Value);

                XMLShaderProperties[shaderName] = new Dictionary<string, ShaderPropertyData>();

                foreach (XElement element in shaderElement.Elements("Property"))
                {
                    string propertyName = element.Attribute("Name").Value;
                    ShaderPropertyType propertyType = (ShaderPropertyType)Enum.Parse(typeof(ShaderPropertyType), element.Attribute("Type").Value);
                    string defaultValue = element.Attribute("DefaultValue")?.Value;
                    string defaultValueAB = element.Attribute("DefaultValueAssetBundle")?.Value;
                    string range = element.Attribute("Range")?.Value;
                    string min = null;
                    string max = null;
                    if (!range.IsNullOrWhiteSpace())
                    {
                        var rangeSplit = range.Split(',');
                        if (rangeSplit.Length == 2)
                        {
                            min = rangeSplit[0];
                            max = rangeSplit[1];
                        }
                    }
                    ShaderPropertyData shaderPropertyData = new ShaderPropertyData(propertyName, propertyType, defaultValue, defaultValueAB, min, max);

                    XMLShaderProperties["default"][propertyName] = shaderPropertyData;
                    XMLShaderProperties[shaderName][propertyName] = shaderPropertyData;
                }
            }
        }

        private void AccessoriesApi_AccessoryTransferred(object sender, AccessoryTransferEventArgs e) => GetCharaController(MakerAPI.GetCharacterControl())?.AccessoryTransferredEvent(sender, e);
        private void AccessoriesApi_AccessoryKindChanged(object sender, AccessorySlotEventArgs e) => GetCharaController(MakerAPI.GetCharacterControl())?.AccessoryKindChangeEvent(sender, e);
        private void AccessoriesApi_SelectedMakerAccSlotChanged(object sender, AccessorySlotEventArgs e) => GetCharaController(MakerAPI.GetCharacterControl())?.AccessorySelectedSlotChangeEvent(sender, e);
#if KK
        private void AccessoriesApi_AccessoriesCopied(object sender, AccessoryCopyEventArgs e) => GetCharaController(MakerAPI.GetCharacterControl())?.AccessoriesCopiedEvent(sender, e);
#endif

        private static bool SetFloatProperty(ChaControl chaControl, string materialName, string property, string value)
        {
            if (value == null) return false;

            float floatValue = float.Parse(value);
            bool didSet = false;
            Material mat = null;

            if (materialName == chaControl.customMatBody.NameFormatted())
                mat = chaControl.customMatBody;
            else if (materialName == chaControl.customMatFace.NameFormatted())
                mat = chaControl.customMatFace;
            if (mat != null)
            {
                mat.SetFloat($"_{property}", floatValue);
                didSet = true;
            }

            return didSet ? didSet : SetFloatProperty(chaControl.gameObject, materialName, property, value, ObjectType.Character);
        }
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

        private static bool SetColorProperty(ChaControl chaControl, string materialName, string property, string value) => SetColorProperty(chaControl, materialName, property, value.ToColor());
        private static bool SetColorProperty(ChaControl chaControl, string materialName, string property, Color value)
        {
            if (value == null) return false;

            bool didSet = false;
            Material mat = null;

            if (materialName == chaControl.customMatBody.NameFormatted())
                mat = chaControl.customMatBody;
            else if (materialName == chaControl.customMatFace.NameFormatted())
                mat = chaControl.customMatFace;
            if (mat != null)
            {
                mat.SetColor($"_{property}", value);
                didSet = true;
            }

            return didSet ? didSet : SetColorProperty(chaControl.gameObject, materialName, property, value, ObjectType.Character);
        }

        private static bool SetColorProperty(GameObject go, string materialName, string property, string value, ObjectType objectType) => SetColorProperty(go, materialName, property, value.ToColor(), objectType);
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

        private static bool SetTextureProperty(ChaControl chaControl, string materialName, string property, Texture2D value)
        {
            if (value == null) return false;

            bool didSet = false;
            Material mat = null;

            if (materialName == chaControl.customMatBody.NameFormatted())
                mat = chaControl.customMatBody;
            else if (materialName == chaControl.customMatFace.NameFormatted())
                mat = chaControl.customMatFace;
            if (mat != null)
            {
                mat.SetTexture($"_{property}", value);
                didSet = true;
            }

            return didSet ? didSet : SetTextureProperty(chaControl.gameObject, materialName, property, value, ObjectType.Character);
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

        private static bool SetShader(ChaControl chaControl, string materialName, string shaderName)
        {
            bool didSet = false;
            if (shaderName.IsNullOrEmpty()) return false;
            Material mat = null;

            if (materialName == chaControl.customMatBody.NameFormatted())
                mat = chaControl.customMatBody;
            else if (materialName == chaControl.customMatFace.NameFormatted())
                mat = chaControl.customMatFace;

            if (mat != null)
            {
                if (LoadedShaders.TryGetValue(shaderName, out var shaderData) && shaderData.Shader != null)
                {
                    mat.shader = shaderData.Shader;
                    if (shaderData.RenderQueue != null)
                        mat.renderQueue = (int)shaderData.RenderQueue;

                    if (XMLShaderProperties.TryGetValue(shaderName, out var shaderPropertyDataList))
                        foreach (var shaderPropertyData in shaderPropertyDataList.Values)
                            if (!shaderPropertyData.DefaultValue.IsNullOrEmpty())
                            {
                                switch (shaderPropertyData.Type)
                                {
                                    case ShaderPropertyType.Float:
                                        SetFloatProperty(chaControl, materialName, shaderPropertyData.Name, shaderPropertyData.DefaultValue);
                                        break;
                                    case ShaderPropertyType.Color:
                                        SetColorProperty(chaControl, materialName, shaderPropertyData.Name, shaderPropertyData.DefaultValue);
                                        break;
                                    case ShaderPropertyType.Texture:
                                        if (shaderPropertyData.DefaultValue.IsNullOrEmpty()) continue;
                                        try
                                        {
                                            var tex = CommonLib.LoadAsset<Texture2D>(shaderPropertyData.DefaultValueAssetBundle, shaderPropertyData.DefaultValue);
                                            SetTextureProperty(chaControl, materialName, shaderPropertyData.Name, tex);
                                        }
                                        catch
                                        {
                                            Logger.LogWarning($"[{PluginNameInternal}] Could not load default texture:{shaderPropertyData.DefaultValueAssetBundle}:{shaderPropertyData.DefaultValue}");
                                        }
                                        break;
                                }
                            }

                    didSet = true;
                }
                else
                    Logger.Log(BepInEx.Logging.LogLevel.Warning | BepInEx.Logging.LogLevel.Message, $"[{PluginNameInternal}] Could not load shader:{shaderName}");
            }

            return didSet ? didSet : SetShader(chaControl.gameObject, materialName, shaderName, ObjectType.Character);

        }
        private static bool SetShader(GameObject go, string materialName, string shaderName, ObjectType objectType)
        {
            bool didSet = false;
            if (shaderName.IsNullOrEmpty()) return false;

            foreach (var rend in GetRendererList(go, objectType))
                foreach (var mat in rend.materials)
                    if (mat.NameFormatted() == materialName)
                    {
                        if (LoadedShaders.TryGetValue(shaderName, out var shaderData) && shaderData.Shader != null)
                        {
                            mat.shader = shaderData.Shader;

                            if (shaderData.RenderQueue != null)
                                mat.renderQueue = (int)shaderData.RenderQueue;

                            if (XMLShaderProperties.TryGetValue(shaderName, out var shaderPropertyDataList))
                                foreach (var shaderPropertyData in shaderPropertyDataList.Values)
                                    if (!shaderPropertyData.DefaultValue.IsNullOrEmpty())
                                    {
                                        switch (shaderPropertyData.Type)
                                        {
                                            case ShaderPropertyType.Float:
                                                SetFloatProperty(go, materialName, shaderPropertyData.Name, shaderPropertyData.DefaultValue, objectType);
                                                break;
                                            case ShaderPropertyType.Color:
                                                SetColorProperty(go, materialName, shaderPropertyData.Name, shaderPropertyData.DefaultValue, objectType);
                                                break;
                                            case ShaderPropertyType.Texture:
                                                if (shaderPropertyData.DefaultValue.IsNullOrEmpty()) continue;
                                                try
                                                {
                                                    var tex = CommonLib.LoadAsset<Texture2D>(shaderPropertyData.DefaultValueAssetBundle, shaderPropertyData.DefaultValue);
                                                    SetTextureProperty(go, materialName, shaderPropertyData.Name, tex, objectType);
                                                }
                                                catch
                                                {
                                                    Logger.LogWarning($"[{PluginNameInternal}] Could not load default texture:{shaderPropertyData.DefaultValueAssetBundle}:{shaderPropertyData.DefaultValue}");
                                                }
                                                break;
                                        }
                                    }
                            didSet = true;
                        }
                        else
                            Logger.Log(BepInEx.Logging.LogLevel.Warning | BepInEx.Logging.LogLevel.Message, $"[{PluginNameInternal}] Could not load shader:{shaderName}");
                    }

            return didSet;
        }

        private static bool SetRenderQueue(ChaControl chaControl, string materialName, int? value)
        {
            bool didSet = false;
            if (value == null) return false;
            Material mat = null;

            if (materialName == chaControl.customMatBody.NameFormatted())
                mat = chaControl.customMatBody;
            else if (materialName == chaControl.customMatFace.NameFormatted())
                mat = chaControl.customMatFace;

            if (mat != null)
            {
                mat.renderQueue = (int)value;
                didSet = true;
            }

            return didSet ? didSet : SetRenderQueue(chaControl.gameObject, materialName, value, ObjectType.Character);
        }

        private static bool SetRenderQueue(GameObject go, string materialName, int? value, ObjectType objectType)
        {
            bool didSet = false;
            if (value == null) return false;

            foreach (var obj in GetRendererList(go, objectType))
                foreach (var objMat in obj.materials)
                    if (objMat.NameFormatted() == materialName)
                    {
                        objMat.renderQueue = (int)value;
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
            if (go == null) return rendList;

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
            if (go.name != "o_body_a" && go.name != "cf_O_face" && go.name != "o_body_cf")
            {
                Renderer rend = go.GetComponent<Renderer>();
                if (rend != null && BodyParts.Contains(rend.NameFormatted()))
                    rendList.Add(rend);
            }

            for (int i = 0; i < go.transform.childCount; i++)
                _GetRendererList(go.transform.GetChild(i).gameObject, rendList);
        }

        public enum ObjectType { StudioItem, Clothing, Accessory, Hair, Character, Other };
        public enum ShaderPropertyType { Texture, Color, Float }
        public static MaterialEditorCharaController GetCharaController(ChaControl character) => character?.gameObject?.GetComponent<MaterialEditorCharaController>();
        public static MaterialEditorSceneController GetSceneController() => Chainloader.ManagerObject.transform.GetComponentInChildren<MaterialEditorSceneController>();

#if KK
        private static int GetObjectID(ObjectCtrlInfo oci) => Studio.Studio.Instance.dicObjectCtrl.First(x => x.Value == oci).Key;
#endif

        public class ShaderData
        {
            public string ShaderName;
            public Shader Shader;
            public int? RenderQueue = null;

            public ShaderData(string shaderName, string assetBundlePath = "", string renderQueue = "", string assetPath = "")
            {
                ShaderName = shaderName;
                if (renderQueue.IsNullOrEmpty())
                    RenderQueue = null;
                else if (int.TryParse(renderQueue, out int result))
                    RenderQueue = result;
                else
                    RenderQueue = null;

                if (assetBundlePath.IsNullOrEmpty())
                {
                    Shader = null;
                }
                else
                {
                    if (assetPath.IsNullOrEmpty())
                    {
                        try
                        {
                            if (assetBundlePath.StartsWith("Resources."))
                            {
                                AssetBundle bundle = AssetBundle.LoadFromMemory(UILib.Resource.LoadEmbeddedResource($"{nameof(KK_Plugins)}.{assetBundlePath}"));
                                Shader = bundle.LoadAsset<Shader>(shaderName);
                                bundle.Unload(false);
                            }
                            else
                                Shader = CommonLib.LoadAsset<Shader>(assetBundlePath, $"{shaderName}");
                        }
                        catch
                        {
                            Logger.LogWarning($"Unable to load shader: {shaderName}");
                            Shader = null;
                        }
                    }
                    else
                    {
                        try
                        {
                            if (assetBundlePath.StartsWith("Resources."))
                            {
                                AssetBundle bundle = AssetBundle.LoadFromMemory(UILib.Resource.LoadEmbeddedResource($"{nameof(KK_Plugins)}.{assetBundlePath}"));
                                Shader = bundle.LoadAsset<Shader>(shaderName);

                                var go = bundle.LoadAsset<GameObject>(assetPath);
                                foreach (var x in go.GetComponentsInChildren<Renderer>())
                                    foreach (var y in x.materials)
                                        if (y.shader.NameFormatted() == ShaderName)
                                            Shader = y.shader;
                                Destroy(go);

                                bundle.Unload(false);
                            }
                            else
                            {
                                var go = CommonLib.LoadAsset<GameObject>(assetBundlePath, assetPath);
                                foreach (var x in go.GetComponentsInChildren<Renderer>())
                                    foreach (var y in x.materials)
                                        if (y.shader.NameFormatted() == ShaderName)
                                            Shader = y.shader;
                                Destroy(go);
                            }
                        }
                        catch
                        {
                            Logger.LogWarning($"Unable to load shader: {shaderName}");
                            Shader = null;
                        }
                    }
                }
            }
        }

        public class ShaderPropertyData
        {
            public string Name;
            public ShaderPropertyType Type;
            public string DefaultValue = null;
            public string DefaultValueAssetBundle = null;
            public int? MinValue = null;
            public int? MaxValue = null;

            public ShaderPropertyData(string name, ShaderPropertyType type, string defaultValue = null, string defaultValueAB = null, string minValue = null, string maxValue = null)
            {
                Name = name;
                Type = type;
                DefaultValue = defaultValue.IsNullOrEmpty() ? null : defaultValue;
                DefaultValueAssetBundle = defaultValueAB.IsNullOrEmpty() ? null : defaultValueAB;
                if (!minValue.IsNullOrWhiteSpace() && !maxValue.IsNullOrWhiteSpace())
                {
                    if (int.TryParse(minValue, out int min) && int.TryParse(maxValue, out int max))
                    {
                        MinValue = min;
                        MaxValue = max;
                    }
                }
            }
        }
    }
}
