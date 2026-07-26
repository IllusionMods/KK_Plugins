using BepInEx.Configuration;
using ExtensibleSaveFormat;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using MaterialEditorAPI;
using MessagePack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using UnityEngine;
using XUnity.ResourceRedirector;
using static MaterialEditorAPI.MaterialAPI;
#if AI || HS2
using AIChara;
#endif
#if EC
using Map;
#else
using Studio;
using KKAPI.Utilities;
#endif
#if PH
using ChaControl = Human;
#endif
namespace KK_Plugins.MaterialEditor
{
    public partial class MaterialEditorPlugin
    {
        private static IEnumerator LoadXML()
        {
#if PH
            yield return null;
#else
            yield return new WaitUntil(() => AssetBundleManager.ManifestBundlePack.Count != 0);
#endif

            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{nameof(KK_Plugins)}.Resources.default.xml"))
                if (stream != null)
                    using (XmlReader reader = XmlReader.Create(stream))
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.Load(reader);
                        LoadXML(doc.DocumentElement, "MaterialEditor.default");
                    }

#if PH
            var di = new DirectoryInfo("abdata/MaterialEditor");
            if (di.Exists)
            {
                var files = di.GetFiles("*.xml", SearchOption.AllDirectories);
                for (var i = 0; i < files.Length; i++)
                {
                    var fileName = files[i].FullName;
                    try
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.Load(fileName);
                        LoadXML(doc.DocumentElement, fileName);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogLevel.Error, $"Failed to load {PluginNameInternal} xml file.");
                        Logger.Log(LogLevel.Error, ex);
                    }
                }
            }
#else
            var loadedManifests = Sideloader.Sideloader.Manifests;
            foreach (var manifest in loadedManifests.Values)
            {
                var element = manifest.ManifestDocument?.Root?.Element("MaterialEditor");
                if (element == null)
                    element = manifest.ManifestDocument?.Root?.Element(PluginNameInternal);
                if (element != null)
                {
                    //Convert XElement in to XmlElement
                    var doc = new XmlDocument();
                    doc.Load(element.CreateReader());
                    var sourceId =
                        manifest.ManifestDocument?.Root?.Element("guid")?.Value
                        ?? "unknown manifest";
                    LoadXML(doc.DocumentElement, sourceId);
                }
            }
#endif
            RefreshPropertyOrganization();
        }

        private static void LoadXML(
            XmlElement materialEditorElement,
            string sourceId)
        {
            if (materialEditorElement == null) return;
            var tooltipCatalog = LoadTooltipCatalogs(
                materialEditorElement,
                sourceId ?? "unknown source");
            var shaderElements = materialEditorElement.GetElementsByTagName("Shader");
            foreach (var shaderElementObj in shaderElements)
            {
                if (shaderElementObj != null)
                {
                    var shaderElement = (XmlElement)shaderElementObj;
                    string shaderName = shaderElement.GetAttribute("Name");
                    var shaderMetadata = tooltipCatalog.ResolveShader(shaderName);
                    ShaderUiMetadataRegistry.SetShader(shaderName, shaderMetadata);

                    if (LoadedShaders.ContainsKey(shaderName))
                    {
                        Destroy(LoadedShaders[shaderName].Shader);
                        LoadedShaders.Remove(shaderName);
                    }
                    var shader = LoadShader(shaderName, shaderElement.GetAttribute("AssetBundle"), shaderElement.GetAttribute("Asset"));
                    LoadedShaders[shaderName] = new ShaderData(shader, shaderName, shaderElement.GetAttribute("RenderQueue"), shaderElement.GetAttribute("ShaderOptimization"));

                    XMLShaderProperties[shaderName] = new Dictionary<string, ShaderPropertyData>();
                    if (shader != null && shader.name != shaderName)
                    {
                        XMLShaderProperties[shader.name] = new Dictionary<string, ShaderPropertyData>();
                        ShaderUiMetadataRegistry.SetShader(shader.name, shaderMetadata);
                    }

                    var shaderPropertyElements = shaderElement.GetElementsByTagName("Property");
                    foreach (var shaderPropertyElementObj in shaderPropertyElements)
                    {
                        if (shaderPropertyElementObj != null)
                        {
                            var shaderPropertyElement = (XmlElement)shaderPropertyElementObj;

                            string propertyName = shaderPropertyElement.GetAttribute("Name");
                            ShaderPropertyType propertyType = (ShaderPropertyType)Enum.Parse(typeof(ShaderPropertyType), shaderPropertyElement.GetAttribute("Type"));
                            string defaultValue = shaderPropertyElement.GetAttribute("DefaultValue");
                            string defaultValueAB = shaderPropertyElement.GetAttribute("DefaultValueAssetBundle");
                            string anisoLevel = shaderPropertyElement.GetAttribute("AnisoLevel");
                            string filterMode = shaderPropertyElement.GetAttribute("FilterMode");
                            string wrapMode = shaderPropertyElement.GetAttribute("WrapMode");
                            string range = shaderPropertyElement.GetAttribute("Range");
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
                            string hidden = shaderPropertyElement.GetAttribute("Hidden");
                            string category = shaderPropertyElement.GetAttribute("Category");

                            ShaderPropertyData shaderPropertyData = new ShaderPropertyData(
                                propertyName, propertyType,
                                defaultValue, defaultValueAB,
                                anisoLevel, filterMode, wrapMode,
                                min, max,
                                hidden, category
                            );

                            XMLShaderProperties["default"][propertyName] = shaderPropertyData;
                            XMLShaderProperties[shaderName][propertyName] = shaderPropertyData;
                            if (shader != null && shader.name != shaderName)
                                XMLShaderProperties[shader.name][propertyName] = shaderPropertyData;
                        }
                    }
                }
            }
        }

        private static Shader LoadShader(string shaderName, string assetBundlePath, string assetPath)
        {
            Shader shader = null;
            if (assetBundlePath.IsNullOrEmpty())
            {
                return shader;
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
                            shader = bundle.LoadAsset<Shader>(shaderName);
                            bundle.Unload(false);
                            return shader;
                        }
                        else
                            return CommonLib.LoadAsset<Shader>(assetBundlePath, $"{shaderName}");
                    }
                    catch
                    {
                        Logger.LogWarning($"Unable to load shader: {shaderName}");
                        return null;
                    }
                }
                else
                {
                    try
                    {
                        if (assetBundlePath.StartsWith("Resources."))
                        {
                            AssetBundle bundle = AssetBundle.LoadFromMemory(UILib.Resource.LoadEmbeddedResource($"{nameof(KK_Plugins)}.{assetBundlePath}"));
                            shader = bundle.LoadAsset<Shader>(shaderName);
                            var go = bundle.LoadAsset<GameObject>(assetPath);
                            bundle.Unload(false);

                            if (shader == null)
                            {
                                var renderers = go.GetComponentsInChildren<Renderer>();
                                for (var i = 0; i < renderers.Length; i++)
                                {
                                    var renderer = renderers[i];
                                    for (var j = 0; j < renderer.materials.Length; j++)
                                    {
                                        var material = renderer.materials[j];
                                        if (material.shader.NameFormatted() == shaderName)
                                            shader = material.shader;
                                    }
                                }
                            }
                            Destroy(go);

                            return shader;
                        }
                        else
                        {
#if PH
                            AssetBundle bundle = AssetBundle.LoadFromFile(assetBundlePath);
                            var go = bundle.LoadAsset<GameObject>(assetPath);
                            bundle.Unload(false);
#else
                            var go = CommonLib.LoadAsset<GameObject>(assetBundlePath, assetPath);
#endif
                            var renderers = go.GetComponentsInChildren<Renderer>();
                            for (var i = 0; i < renderers.Length; i++)
                            {
                                var renderer = renderers[i];
                                for (var j = 0; j < renderer.materials.Length; j++)
                                {
                                    var material = renderer.materials[j];
                                    if (material.shader.NameFormatted() == shaderName)
                                        shader = material.shader;
                                }
                            }
                            Destroy(go);
                            return shader;
                        }
                    }
                    catch
                    {
                        Logger.LogWarning($"Unable to load shader: {shaderName}");
                        return shader;
                    }
                }
            }
        }

    }
}
