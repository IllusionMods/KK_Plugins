using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using UnityEngine;
using XUnity.ResourceRedirector;
using static MaterialEditorAPI.MaterialAPI;

namespace MaterialEditorAPI
{
    /// <summary>
    /// MaterialEditor plugin base
    /// </summary>
    [BepInDependency(XUnity.ResourceRedirector.Constants.PluginData.Identifier, XUnity.ResourceRedirector.Constants.PluginData.Version)]
    public partial class MaterialEditorPluginBase : BaseUnityPlugin
    {
        /// <summary>
        /// Logger instance for the plugin
        /// </summary>
        public static new ManualLogSource Logger;
        /// <summary>
        /// Singleton instance of the plugin
        /// </summary>
        public static MaterialEditorPluginBase Instance;

        /// <summary>
        /// Default path where textures will be exported
        /// </summary>
        public static string ExportPathDefault = Path.Combine(Paths.GameRootPath, @"UserData\MaterialEditor");
        /// <summary>
        /// Path where textures will be exported
        /// </summary>
        public static string ExportPath = ExportPathDefault;
        /// <summary>
        /// Default path where local textures will be exported to / imported from
        /// </summary>
        public static string LocalTexturePathDefault = Path.Combine(Paths.GameRootPath, @"UserData\MaterialEditor\_LocalTextures");
        /// <summary>
        /// Path where local textures will be exported to / imported from
        /// </summary>
        public static string LocalTexturePath = LocalTexturePathDefault;
        /// <summary>
        /// Saved material edits
        /// </summary>
        public static CopyContainer CopyData = new CopyContainer();

        /// <summary>
        /// Dictionary of loaded shaders
        /// </summary>
        public static Dictionary<string, ShaderData> LoadedShaders = new Dictionary<string, ShaderData>();
        /// <summary>
        /// Sorted dictionary of XML shader properties
        /// </summary>
        public static SortedDictionary<string, Dictionary<string, ShaderPropertyData>> XMLShaderProperties = new SortedDictionary<string, Dictionary<string, ShaderPropertyData>>();

        /// <summary>
        /// Configuration entry for ME window scale
        /// </summary>
        public static ConfigEntry<float> UIScale { get; set; }
        /// <summary>
        /// Configuration entry for ME window width
        /// </summary>
        public static ConfigEntry<float> UIWidth { get; set; }
        /// <summary>
        /// Configuration entry for ME window height
        /// </summary>
        public static ConfigEntry<float> UIHeight { get; set; }
        /// <summary>
        /// Configuration entry for width of the renderer/materials lists to the side of the window
        /// </summary>
        public static ConfigEntry<float> UIListWidth { get; set; }
        /// <summary>
        /// Configuration entry for sensitivity of dragging labels to edit float values
        /// </summary>
        public static ConfigEntry<float> DragSensitivity { get; set; }
        /// <summary>
        /// Configuration entry for watching for file changes and reloading textures on change
        /// </summary>
        public static ConfigEntry<bool> WatchTexChanges { get; set; }
        /// <summary>
        /// Replaces every loaded shader with the MaterialEditor copy of the shader
        /// </summary>
        public static ConfigEntry<bool> ShaderOptimization { get; set; }
        /// <summary>
        /// Skinned meshes will be exported in their current state with all customization applied as well as in the current pose
        /// </summary>
        public static ConfigEntry<bool> ExportBakedMesh { get; set; }
        /// <summary>
        /// When enabled, objects will be exported with their position changes intact so that, i.e. when exporting two objects they retain their position relative to each other
        /// </summary>
        public static ConfigEntry<bool> ExportBakedWorldPosition { get; set; }
        /// <summary>
        /// Textures and models will be exported to this folder. If empty, exports to {ExportPathDefault}
        /// </summary>
        internal static ConfigEntry<string> ConfigExportPath { get; private set; }
        /// <summary>
        /// Persist search filter across editor windows
        /// </summary>
        public static ConfigEntry<bool> PersistFilter { get; set; }
        /// <summary>
        /// Whether to show tooltips or not
        /// </summary>
        public static ConfigEntry<bool> Showtooltips { get; set; }
        /// <summary>
        /// Whether to sort shader properties by their types
        /// </summary>
        public static ConfigEntry<bool> SortPropertiesByType { get; set; }
        /// <summary>
        /// Whether to sort shader properties by their names
        /// </summary>
        public static ConfigEntry<bool> SortPropertiesByName { get; set; }
        /// <summary>
        /// Controls the max value of the slider for this projector property
        /// </summary>
        public static ConfigEntry<float> ProjectorNearClipPlaneMax { get; set; }
        /// <summary>
        /// Controls the max value of the slider for this projector property
        /// </summary>
        public static ConfigEntry<float> ProjectorFarClipPlaneMax { get; set; }
        /// <summary>
        /// Controls the max value of the slider for this projector property
        /// </summary>
        public static ConfigEntry<float> ProjectorFieldOfViewMax { get; set; }
        /// <summary>
        /// Controls the max value of the slider for this projector property
        /// </summary>
        public static ConfigEntry<float> ProjectorAspectRatioMax { get; set; }
        /// <summary>
        /// Controls the max value of the slider for this projector property
        /// </summary>
        public static ConfigEntry<float> ProjectorOrthographicSizeMax { get; set; }
        /// <summary>
        /// When enabled, normalmaps get converted from DXT5 compressed (red) normals back to normal OpenGL (blue/purple) normals
        /// </summary>
        public static ConfigEntry<bool> ConvertNormalmapsOnExport { get; set; }
        /// <summary>
        /// Local textures will be exported to / imported from this folder. If empty, defaults to {LocalTexturePathDefault}
        /// </summary>
        internal static ConfigEntry<string> ConfigLocalTexturePath { get; set; }

        /// <summary>
        /// Init logic, do not call
        /// </summary>
        public virtual void Awake()
        {
            Instance = this;
            Logger = base.Logger;
            Directory.CreateDirectory(ExportPath);

            UIScale = Config.Bind("Config", "UI Scale", 1.75f, new ConfigDescription("Controls the size of the window.", new AcceptableValueRange<float>(1f, 3f), new ConfigurationManagerAttributes { Order = 7 }));
            UIWidth = Config.Bind("Config", "UI Width", 0.33f, new ConfigDescription("Controls the size of the window.", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 6, ShowRangeAsPercent = false }));
            UIHeight = Config.Bind("Config", "UI Height", 0.3f, new ConfigDescription("Controls the size of the window.", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 5, ShowRangeAsPercent = false }));
            UIListWidth = Config.Bind("Config", "UI List Width", 180f, new ConfigDescription("Controls width of the renderer/materials lists to the side of the window", new AcceptableValueRange<float>(100f, 500f), new ConfigurationManagerAttributes { Order = 4, ShowRangeAsPercent = false }));
            DragSensitivity = Config.Bind("Config", "Drag Sensitivity", 30f, new ConfigDescription("Controls the sensitivity of dragging labels to edit float values", new AcceptableValueRange<float>(1f, 100f), new ConfigurationManagerAttributes { Order = 3, ShowRangeAsPercent = false }));
            WatchTexChanges = Config.Bind("Config", "Watch File Changes", true, new ConfigDescription("Watch for file changes and reload textures on change. Can be toggled in the UI.", null, new ConfigurationManagerAttributes { Order = 2 }));
            ShaderOptimization = Config.Bind("Config", "Shader Optimization", true, new ConfigDescription("Replaces every loaded shader with the MaterialEditor copy of the shader. Reduces the number of copies of shaders loaded which reduces RAM usage and improves performance.", null, new ConfigurationManagerAttributes { Order = 1 }));
            ExportBakedMesh = Config.Bind("Config", "Export Baked Mesh", false, new ConfigDescription("When enabled, skinned meshes will be exported in their current state with all customization applied as well as in the current pose.", null, new ConfigurationManagerAttributes { Order = 1 }));
            ExportBakedWorldPosition = Config.Bind("Config", "Export Baked World Position", false, new ConfigDescription("When enabled, objects will be exported with their position changes intact so that, i.e. when exporting two objects they retain their position relative to each other.\nOnly works when Export Baked Mesh is also enabled.", null, new ConfigurationManagerAttributes { Order = 1 }));
            ConfigExportPath = Config.Bind("Config", "Export Path Override", "", new ConfigDescription($"Textures and models will be exported to this folder. If empty, exports to {ExportPathDefault}", null, new ConfigurationManagerAttributes { Order = 1 }));
            PersistFilter = Config.Bind("Config", "Persist Filter", false, "Persist search filter across editor windows");
            Showtooltips = Config.Bind("Config", "Show Tooltips", true, "Whether to show tooltips or not");
            SortPropertiesByType = Config.Bind("Config", "Sort Properties by Type", true, "Whether to sort shader properties by their types.");
            SortPropertiesByName = Config.Bind("Config", "Sort Properties by Name", true, "Whether to sort shader properties by their names.");
            ConvertNormalmapsOnExport = Config.Bind("Config", "Convert Normalmaps On Export", true, new ConfigDescription("When enabled, normalmaps get converted from DXT5 compressed (red) normals back to normal OpenGL (blue/purple) normals"));

            // Everything in these games is 10x the size of KK/KKS
#if AI || HS2 || PH
            ProjectorNearClipPlaneMax = Config.Bind("Projector", "Max Near Clip Plane", 100f, new ConfigDescription("Controls the max value of the slider for this projector property", new AcceptableValueRange<float>(0.01f, 1000f), new ConfigurationManagerAttributes { Order = 5 }));
            ProjectorFarClipPlaneMax = Config.Bind("Projector", "Max Far Clip Plane", 1000f, new ConfigDescription("Controls the max value of the slider for this projector property", new AcceptableValueRange<float>(0.01f, 1000f), new ConfigurationManagerAttributes { Order = 4 }));
            ProjectorOrthographicSizeMax = Config.Bind("Projector", "Max Orthographic Size", 20f, new ConfigDescription("Controls the max value of the slider for this projector property", new AcceptableValueRange<float>(0.01f, 1000f), new ConfigurationManagerAttributes { Order = 1 }));
#else
            ProjectorNearClipPlaneMax = Config.Bind("Projector", "Max Near Clip Plane", 10f, new ConfigDescription("Controls the max value of the slider for this projector property", new AcceptableValueRange<float>(0.01f, 100f), new ConfigurationManagerAttributes { Order = 5 }));
            ProjectorFarClipPlaneMax = Config.Bind("Projector", "Max Far Clip Plane", 100f, new ConfigDescription("Controls the max value of the slider for this projector property", new AcceptableValueRange<float>(0.01f, 100f), new ConfigurationManagerAttributes { Order = 4 }));
            ProjectorOrthographicSizeMax = Config.Bind("Projector", "Max Orthographic Size", 2f, new ConfigDescription("Controls the max value of the slider for this projector property", new AcceptableValueRange<float>(0.01f, 100f), new ConfigurationManagerAttributes { Order = 1 }));
#endif
            ProjectorFieldOfViewMax = Config.Bind("Projector", "Max Field Of View", 180f, new ConfigDescription("Controls the max value of the slider for this projector property", new AcceptableValueRange<float>(0.01f, 180f), new ConfigurationManagerAttributes { Order = 3 }));
            ProjectorAspectRatioMax = Config.Bind("Projector", "Max Aspect Ratio", 2f, new ConfigDescription("Controls the max value of the slider for this projector property", new AcceptableValueRange<float>(0.01f, 100f), new ConfigurationManagerAttributes { Order = 2 }));

            UIScale.SettingChanged += MaterialEditorUI.UISettingChanged;
            UIWidth.SettingChanged += MaterialEditorUI.UISettingChanged;
            UIHeight.SettingChanged += MaterialEditorUI.UISettingChanged;
            UIListWidth.SettingChanged += MaterialEditorUI.UISettingChanged;
            WatchTexChanges.SettingChanged += WatchTexChanges_SettingChanged;
            ShaderOptimization.SettingChanged += ShaderOptimization_SettingChanged;
            ConfigExportPath.SettingChanged += ConfigExportPath_SettingChanged;
            SortPropertiesByType.SettingChanged += (object sender, EventArgs e) => PropertyOrganizer.Refresh();
            SortPropertiesByName.SettingChanged += (object sender, EventArgs e) => PropertyOrganizer.Refresh();
            SetExportPath();

            ResourceRedirection.RegisterAssetLoadedHook(HookBehaviour.OneCallbackPerResourceLoaded, AssetLoadedHook);
            LoadXML();
        }

        /// <summary>
        /// Every time an asset is loaded, swap its shader for the one loaded by MaterialEditor. This reduces the number of instances of a shader once they are cleaned up by garbage collection
        /// which reduce RAM usage, etc. Also fixes KK mods in EC by swapping them to the equivalent EC shader.
        /// </summary>
        protected virtual void AssetLoadedHook(AssetLoadedContext context)
        {
            if (!ShaderOptimization.Value) return;

            if (context.Asset is GameObject go)
            {
                var renderers = go.GetComponentsInChildren<Renderer>();
                for (var i = 0; i < renderers.Length; i++)
                {
                    var renderer = renderers[i];
                    for (var j = 0; j < renderer.materials.Length; j++)
                    {
                        var material = renderer.materials[j];
                        if (LoadedShaders.TryGetValue(material.shader.name, out var shaderData) && shaderData.Shader != null && shaderData.ShaderOptimization)
                        {
                            int renderQueue = material.renderQueue;
                            material.shader = shaderData.Shader;
                            material.renderQueue = renderQueue;
                        }
                    }
                }
            }
            else if (context.Asset is Material mat)
            {
                if (LoadedShaders.TryGetValue(mat.shader.name, out var shaderData) && shaderData.Shader != null && shaderData.ShaderOptimization)
                {
                    int renderQueue = mat.renderQueue;
                    mat.shader = shaderData.Shader;
                    mat.renderQueue = renderQueue;
                }
            }
            else if (context.Asset is Shader shader)
            {
                if (LoadedShaders.TryGetValue(shader.name, out var shaderData) && shaderData.Shader != null && shaderData.ShaderOptimization)
                    context.Asset = shaderData.Shader;
            }
        }

        private static void LoadXML()
        {
            XMLShaderProperties["default"] = new Dictionary<string, ShaderPropertyData>();

            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{nameof(MaterialEditorAPI)}.Resources.default.xml"))
                if (stream != null)
                    using (XmlReader reader = XmlReader.Create(stream))
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.Load(stream);
                        XmlElement materialEditorElement = doc.DocumentElement;

                        var shaderElements = materialEditorElement.GetElementsByTagName("Shader");
                        foreach (var shaderElementObj in shaderElements)
                        {
                            if (shaderElementObj != null)
                            {
                                var shaderElement = (XmlElement)shaderElementObj;
                                {
                                    string shaderName = shaderElement.GetAttribute("Name");

                                    XMLShaderProperties[shaderName] = new Dictionary<string, ShaderPropertyData>();

                                    var shaderPropertyElements = shaderElement.GetElementsByTagName("Property");
                                    foreach (var shaderPropertyElementObj in shaderPropertyElements)
                                    {
                                        if (shaderPropertyElementObj != null)
                                        {
                                            var shaderPropertyElement = (XmlElement)shaderPropertyElementObj;
                                            {
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
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
        }

        internal virtual void WatchTexChanges_SettingChanged(object sender, EventArgs e)
        {
            if (!WatchTexChanges.Value)
                MaterialEditorUI.TexChangeWatcher?.Dispose();
        }

        internal virtual void ShaderOptimization_SettingChanged(object sender, EventArgs e) { }

        internal virtual void ConfigExportPath_SettingChanged(object sender, EventArgs e)
        {
            SetExportPath();
        }

        private void SetExportPath()
        {
            if (ConfigExportPath.Value == "")
                ExportPath = ExportPathDefault;
            else
                ExportPath = ConfigExportPath.Value;
        }

        /// <summary>
        /// Always returns false, i.e. does nothing. Override to prevent certain materials from showing in the UI.
        /// </summary>
        /// <param name="materialName">Name of the material</param>
        /// <param name="propertyName">Name of the property</param>
        /// <returns></returns>
        public virtual bool CheckBlacklist(string materialName, string propertyName) => false;

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

        internal static bool IsAutoSave()
        {
            if (Chainloader.PluginInfos.TryGetValue("com.deathweasel.bepinex.autosave", out PluginInfo pluginInfo) && pluginInfo?.Instance != null)
                return (bool)(pluginInfo.Instance.GetType().GetField("Autosaving")?.GetValue(null) ?? false);
            return false;
        }

        /// <summary>
        /// Refreshes the property organization, which groups shader properties by their categories and sorts them based on the configuration settings.
        /// </summary>
        protected static void RefreshPropertyOrganization()
        {
            PropertyOrganizer.Refresh();
        }

        /// <summary>
        /// Represents data for a shader, including its name, shader object, render queue, and optimization flag.
        /// </summary>
        public class ShaderData
        {
            /// <summary>
            /// Name of the shader.
            /// </summary>
            public string ShaderName;
            /// <summary>
            /// Shader object.
            /// </summary>
            public Shader Shader;
            /// <summary>
            /// Render queue value for the shader. Null if not specified.
            /// </summary>
            public int? RenderQueue;
            /// <summary>
            /// Indicates whether shader optimization is enabled.
            /// </summary>
            public bool ShaderOptimization;

            /// <summary>
            /// Initializes a new instance of the <see cref="ShaderData"/> class.
            /// </summary>
            /// <param name="shader">The shader object.</param>
            /// <param name="shaderName">The name of the shader.</param>
            /// <param name="renderQueue">The render queue value as a string. Defaults to an empty string.</param>
            /// <param name="shaderOptimization">The shader optimization flag as a string. Defaults to null.</param>
            public ShaderData(Shader shader, string shaderName, string renderQueue = "", string shaderOptimization = null)
            {
                Shader = shader;
                ShaderName = shaderName;

                if (renderQueue.IsNullOrEmpty())
                    RenderQueue = null;
                else if (int.TryParse(renderQueue, out int result))
                    RenderQueue = result;
                else
                    RenderQueue = null;

                if (bool.TryParse(shaderOptimization, out bool shaderOptimizationBool))
                    ShaderOptimization = shaderOptimizationBool;
                else
                    ShaderOptimization = true;
            }
        }

        /// <summary>
        /// Represents data for a shader property, including its name, type, default values, visibility, range, and category.
        /// </summary>
        public class ShaderPropertyData
        {
            /// <summary>
            /// Name of the shader property.
            /// </summary>
            public string Name;
            /// <summary>
            /// Type of the shader property.
            /// </summary>
            public ShaderPropertyType Type;
            /// <summary>
            /// Default value of the shader property.
            /// </summary>
            public string DefaultValue;
            /// <summary>
            /// Default value of the shader property when loaded from an asset bundle, like a texture.
            /// </summary>
            public string DefaultValueAssetBundle;
            /// <summary>
            /// Should only be used with texture properties. The `anisoLevel` of the texture, 0-16.
            /// </summary>
            public int? AnisoLevel;
            /// <summary>
            /// Should only be used with texture properties. The `filterMode` of the texture.
            /// </summary>
            public FilterMode? FilterMode;
            /// <summary>
            /// Should only be used with texture properties. The `wrapMode` of the texture.
            /// </summary>
            public TextureWrapMode? WrapMode;
            /// <summary>
            /// Should only be used with float properties. Minimum value displayed on the slider, if applicable.
            /// </summary>
            public float? MinValue;
            /// <summary>
            /// Should only be used with float properties. Maximum value displayed on the slider, if applicable.
            /// </summary>
            public float? MaxValue;
            /// <summary>
            /// Indicates whether the shader property is hidden.
            /// </summary>
            public bool Hidden;
            /// <summary>
            /// Category of the shader property.
            /// </summary>
            public string Category;

            /// <summary>
            /// Initializes a new instance of the <see cref="ShaderPropertyData"/> class.
            /// </summary>
            /// <param name="name">Name of the shader property.</param>
            /// <param name="type">Type of the shader property.</param>
            /// <param name="defaultValue">Default value of the shader property.</param>
            /// <param name="defaultValueAB">Default value of the shader property when loaded from an asset bundle, like a texture.</param>
            /// <param name="anisoLevel">Should only be used with texture properties. The `anisoLevel` of the texture, 0-16.</param>
            /// <param name="filterMode">Should only be used with texture properties. The `filterMode` of the texture.</param>
            /// <param name="wrapMode">Should only be used with texture properties. The `wrapMode` of the texture.</param>
            /// <param name="minValue">Should only be used with float properties. Minimum value displayed on the slider, if applicable.</param>
            /// <param name="maxValue">Should only be used with float properties. Maximum value displayed on the slider, if applicable.</param>
            /// <param name="hidden">Indicates whether the shader property is hidden.</param>
            /// <param name="category">Category of the shader property.</param>
            public ShaderPropertyData(
                string name, ShaderPropertyType type,
                string defaultValue = null, string defaultValueAB = null,
                string anisoLevel = null, string filterMode = null, string wrapMode = null,
                string minValue = null, string maxValue = null,
                string hidden = null, string category = null
                )
            {
                Name = name;
                Type = type;
                DefaultValue = defaultValue.IsNullOrEmpty() ? null : defaultValue;
                DefaultValueAssetBundle = defaultValueAB.IsNullOrEmpty() ? null : defaultValueAB;

                if (!anisoLevel.IsNullOrWhiteSpace())
                {
                    int.TryParse(anisoLevel, out int outAnisoLevel);
                    AnisoLevel = Mathf.Clamp(outAnisoLevel, 0, 16);
                }

                if (!filterMode.IsNullOrWhiteSpace())
                {
                    int.TryParse(filterMode, out int outFilterMode);
                    if (Enum.IsDefined(typeof(FilterMode), outFilterMode)) FilterMode = (FilterMode)outFilterMode;
                }

                if (!wrapMode.IsNullOrWhiteSpace())
                {
                    int.TryParse(wrapMode, out int outWrapMode);
                    if (Enum.IsDefined(typeof(TextureWrapMode), outWrapMode)) WrapMode = (TextureWrapMode)outWrapMode;
                }

                if (!minValue.IsNullOrWhiteSpace() && !maxValue.IsNullOrWhiteSpace())
                {
                    if (float.TryParse(minValue, out float min) && float.TryParse(maxValue, out float max))
                    {
                        MinValue = min;
                        MaxValue = max;
                    }
                }

                Hidden = bool.TryParse(hidden, out bool result) && result;
                Category = category;
            }
        }
    }
}
