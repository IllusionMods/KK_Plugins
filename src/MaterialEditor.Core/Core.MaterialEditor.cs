using BepInEx;
using BepInEx.Harmony;
using BepInEx.Logging;
using HarmonyLib;
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
using static KK_Plugins.MaterialEditor.MaterialAPI;
#if AI
using AIChara;
#endif

namespace KK_Plugins.MaterialEditor
{
    public partial class MaterialEditorPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.materialeditor";
        public const string PluginName = "Material Editor";
        public const string Version = "1.10";
        internal static new ManualLogSource Logger;

        public const string FileExt = ".png";
        public const string FileFilter = "Images (*.png;.jpg)|*.png;*.jpg|All files|*.*";
        public static readonly string ExportPath = Path.Combine(Paths.GameRootPath, @"UserData\MaterialEditor");

        internal static Dictionary<string, ShaderData> LoadedShaders = new Dictionary<string, ShaderData>();
        internal static SortedDictionary<string, Dictionary<string, ShaderPropertyData>> XMLShaderProperties = new SortedDictionary<string, Dictionary<string, ShaderPropertyData>>();

        internal void Main()
        {
            Logger = base.Logger;
            Directory.CreateDirectory(ExportPath);

            CharacterApi.RegisterExtraBehaviour<MaterialEditorCharaController>(GUID);
            AccessoriesApi.SelectedMakerAccSlotChanged += AccessoriesApi_SelectedMakerAccSlotChanged;
            AccessoriesApi.AccessoryKindChanged += AccessoriesApi_AccessoryKindChanged;
            AccessoriesApi.AccessoryTransferred += AccessoriesApi_AccessoryTransferred;
#if KK
            AccessoriesApi.AccessoriesCopied += AccessoriesApi_AccessoriesCopied;
#endif

            LoadXML();
            var harmony = HarmonyWrapper.PatchAll(typeof(Hooks));

#if KK || EC
            //Hooks for transfering accessories (MoreAccessories compatibility)
            foreach (var method in typeof(ChaCustom.CvsAccessoryChange).GetMethods(AccessTools.all).Where(x => x.Name.Contains("<Start>m__4")))
                harmony.Patch(method, new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.AccessoryTransferHook), AccessTools.all)));
#elif AI
            //Hooks for changing clothing pattern
            foreach (var method in typeof(CharaCustom.CustomClothesPatternSelect).GetMethods(AccessTools.all).Where(x => x.Name.Contains("<ChangeLink>")))
                harmony.Patch(method, new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.ClothesColorChangeHook), AccessTools.all)));

            //hooks for changing clothing color
            foreach (var method in typeof(CharaCustom.CustomClothesColorSet).GetMethods(AccessTools.all).Where(x => x.Name.StartsWith("<Initialize>")))
                harmony.Patch(method, new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.ClothesColorChangeHook), AccessTools.all)));
#endif
        }

        private void AccessoriesApi_AccessoryTransferred(object sender, AccessoryTransferEventArgs e) => MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl())?.AccessoryTransferredEvent(sender, e);
        private void AccessoriesApi_AccessoryKindChanged(object sender, AccessorySlotEventArgs e) => MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl())?.AccessoryKindChangeEvent(sender, e);
        private void AccessoriesApi_SelectedMakerAccSlotChanged(object sender, AccessorySlotEventArgs e) => MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl())?.AccessorySelectedSlotChangeEvent(sender, e);
#if KK
        private void AccessoriesApi_AccessoriesCopied(object sender, AccessoryCopyEventArgs e) => MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl())?.AccessoriesCopiedEvent(sender, e);
#endif

        private void LoadXML()
        {
            var loadedManifests = Sideloader.Sideloader.Manifests;
            XMLShaderProperties["default"] = new Dictionary<string, ShaderPropertyData>();

            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{nameof(KK_Plugins)}.Resources.default.xml"))
            using (XmlReader reader = XmlReader.Create(stream))
                LoadXML(XDocument.Load(reader).Element(PluginNameInternal));

            foreach (var manifest in loadedManifests.Values)
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

        public static Texture2D TextureFromBytes(byte[] texBytes, TextureFormat format = TextureFormat.ARGB32, bool mipmaps = true)
        {
            if (texBytes == null || texBytes.Length == 0) return null;

            var tex = new Texture2D(2, 2, format, mipmaps);
            tex.LoadImage(texBytes);
            return tex;
        }

        public static bool CheckBlacklist(ObjectType objectType, string propertyName)
        {
            if (objectType == ObjectType.Character && CharacterBlacklist.Contains(propertyName))
                return true;
            return false;
        }

        public static MaterialEditorCharaController GetCharaController(ChaControl character) => character?.gameObject?.GetComponent<MaterialEditorCharaController>();

        public static HashSet<string> CharacterBlacklist = new HashSet<string>() { "alpha_a", "alpha_b" };
        internal static Texture2D GetT2D(RenderTexture renderTexture)
        {
            var currentActiveRT = RenderTexture.active;
            RenderTexture.active = renderTexture;
            var tex = new Texture2D(renderTexture.width, renderTexture.height);
            tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
            RenderTexture.active = currentActiveRT;
            return tex;
        }

        internal static void SaveTexR(RenderTexture renderTexture, string path)
        {
            var tex = GetT2D(renderTexture);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            DestroyImmediate(tex);
        }

        internal static void SaveTex(Texture tex, string path, RenderTextureFormat rtf = RenderTextureFormat.Default, RenderTextureReadWrite cs = RenderTextureReadWrite.Default)
        {
            var tmp = RenderTexture.GetTemporary(tex.width, tex.height, 0, rtf, cs);
            var currentActiveRT = RenderTexture.active;
            RenderTexture.active = tmp;
            GL.Clear(false, true, new Color(0, 0, 0, 0));
            Graphics.Blit(tex, tmp);
            SaveTexR(tmp, path);
            RenderTexture.active = currentActiveRT;
            RenderTexture.ReleaseTemporary(tmp);
        }

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
            public float? MinValue = null;
            public float? MaxValue = null;

            public ShaderPropertyData(string name, ShaderPropertyType type, string defaultValue = null, string defaultValueAB = null, string minValue = null, string maxValue = null)
            {
                Name = name;
                Type = type;
                DefaultValue = defaultValue.IsNullOrEmpty() ? null : defaultValue;
                DefaultValueAssetBundle = defaultValueAB.IsNullOrEmpty() ? null : defaultValueAB;
                if (!minValue.IsNullOrWhiteSpace() && !maxValue.IsNullOrWhiteSpace())
                {
                    if (float.TryParse(minValue, out float min) && float.TryParse(maxValue, out float max))
                    {
                        MinValue = min;
                        MaxValue = max;
                    }
                }
            }
        }
    }
}
