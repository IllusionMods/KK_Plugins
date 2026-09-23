using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        private const string LightThemeSetting = "Light";
        private const string DarkThemeSetting = "Dark";

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
        internal static readonly ShaderPropertyFallbackMergeState
            ShaderPropertyFallbacks = new ShaderPropertyFallbackMergeState();

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
        /// Configuration entry for width of the Categories list beside the window
        /// </summary>
        internal static ConfigEntry<float> UICategoriesWidth { get; private set; }
        internal static ConfigEntry<string> UITheme { get; private set; }
        internal static ConfigEntry<bool> CategoriesPanelOpen { get; private set; }
        internal static ConfigEntry<bool> RenderersPanelOpen { get; private set; }
        internal static ConfigEntry<bool> MaterialsPanelOpen { get; private set; }
        /// <summary>
        /// Configuration entry for sensitivity of dragging labels to edit float values
        /// </summary>
        public static ConfigEntry<float> DragSensitivity { get; set; }
        /// <summary>
        /// Prevent dragging the ME window outside of the game window
        /// </summary>
        public static ConfigEntry<bool> PreventDragout { get; set; }
        /// <summary>
        /// Defines which visible part of the Material Editor window remains
        /// recoverable while dragging. It is synchronized with the public
        /// <see cref="PreventDragout"/> compatibility entry.
        /// </summary>
        internal static ConfigEntry<MaterialEditorWindowDragMode> WindowDragMode
        {
            get;
            private set;
        }
        private static bool _synchronizingWindowDragSettings;
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
        /// Whether Shift-activated shader-authored hints are available
        /// </summary>
        internal static ConfigEntry<bool> EnableShaderHints { get; private set; }
        /// <summary>
        /// Whether to sort shader properties by their types
        /// </summary>
        public static ConfigEntry<bool> SortPropertiesByType { get; set; }
        /// <summary>
        /// Whether to sort shader properties by their names
        /// </summary>
        public static ConfigEntry<bool> SortPropertiesByName { get; set; }
        /// <summary>
        /// Whether to sort shader properties by their category
        /// </summary>
        public static ConfigEntry<bool> SortPropertiesByCategory { get; set; }
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
        /// Optional path for reading version-2 local texture data. Empty uses {LocalTexturePathDefault}.
        /// </summary>
        internal static ConfigEntry<string> ConfigLocalTexturePath { get; set; }

        private static void HandleLegacyWindowDragSettingChanged(
            object sender,
            EventArgs eventArgs)
        {
            if (_synchronizingWindowDragSettings || WindowDragMode == null)
                return;

            _synchronizingWindowDragSettings = true;
            try
            {
                WindowDragMode.Value =
                    MaterialEditorWindowBoundsPolicy.FromLegacy(
                        PreventDragout.Value);
            }
            finally
            {
                _synchronizingWindowDragSettings = false;
            }
        }

        private static void HandleWindowDragModeChanged(
            object sender,
            EventArgs eventArgs)
        {
            SynchronizeLegacyWindowDragSetting();
            MaterialEditorUI.UISettingChanged(sender, eventArgs);
        }

        private static void SynchronizeLegacyWindowDragSetting()
        {
            if (_synchronizingWindowDragSettings
                || PreventDragout == null
                || WindowDragMode == null)
                return;

            _synchronizingWindowDragSettings = true;
            try
            {
                PreventDragout.Value =
                    MaterialEditorWindowBoundsPolicy.ToLegacyBoolean(
                        WindowDragMode.Value);
            }
            finally
            {
                _synchronizingWindowDragSettings = false;
            }
        }

        /// <summary>
        /// Init logic, do not call
        /// </summary>
        public virtual void Awake()
        {
            Instance = this;
            Logger = base.Logger;
            Directory.CreateDirectory(ExportPath);

            UIScale = Config.Bind("Config", "UI Scale", MaterialEditorTheme.Metrics.UiScaleDefault, new ConfigDescription("Controls the size of the window.", new AcceptableValueRange<float>(MaterialEditorTheme.Metrics.UiScaleMinimum, MaterialEditorTheme.Metrics.UiScaleMaximum), new ConfigurationManagerAttributes { Order = 8 }));
            UIWidth = Config.Bind("Config", "UI Width", MaterialEditorTheme.Metrics.WindowWidthDefault, new ConfigDescription("Controls the size of the window.", new AcceptableValueRange<float>(MaterialEditorTheme.Metrics.WindowWidthMinimum, MaterialEditorTheme.Metrics.WindowWidthMaximum), new ConfigurationManagerAttributes { Order = 7, ShowRangeAsPercent = false }));
            UIHeight = Config.Bind("Config", "UI Height", MaterialEditorTheme.Metrics.WindowHeightDefault, new ConfigDescription("Controls the size of the window.", new AcceptableValueRange<float>(MaterialEditorTheme.Metrics.WindowHeightMinimum, MaterialEditorTheme.Metrics.WindowHeightMaximum), new ConfigurationManagerAttributes { Order = 6, ShowRangeAsPercent = false }));
            UICategoriesWidth = Config.Bind("Config", "UI Categories Width", MaterialEditorTheme.Metrics.CategoryPanelDefaultWidth, new ConfigDescription("Controls the width of the Categories list beside the window.", new AcceptableValueRange<float>(MaterialEditorTheme.Metrics.SidePanelMinimumWidth, MaterialEditorTheme.Metrics.SidePanelMaximumWidth), new ConfigurationManagerAttributes { Order = 5, ShowRangeAsPercent = false }));
            UIListWidth = Config.Bind("Config", "UI List Width", MaterialEditorTheme.Metrics.SidePanelDefaultWidth, new ConfigDescription("Controls the width of the renderer/material and Rename lists beside the window.", new AcceptableValueRange<float>(MaterialEditorTheme.Metrics.SidePanelMinimumWidth, MaterialEditorTheme.Metrics.SidePanelMaximumWidth), new ConfigurationManagerAttributes { Order = 4, ShowRangeAsPercent = false, DispName = "Renderer/Material List Width" }));
            UITheme = Config.Bind(
                "Config",
                "UI Theme",
                LightThemeSetting,
                new ConfigDescription(
                    "Selects the Light or Dark appearance. This changes visuals only.",
                    new AcceptableValueList<string>(
                        LightThemeSetting,
                        DarkThemeSetting)));
            MaterialEditorTheme.SetMode(GetConfiguredUITheme());
            CategoriesPanelOpen = Config.Bind(
                "UI State",
                "Categories Panel Open",
                false,
                new ConfigDescription(
                    "Remembers whether the Categories panel was left open.",
                    null,
                    new ConfigurationManagerAttributes { Browsable = false }));
            RenderersPanelOpen = Config.Bind(
                "UI State",
                "Renderers Panel Open",
                false,
                new ConfigDescription(
                    "Compatibility state for the jointly visible Renderers and Materials panels.",
                    null,
                    new ConfigurationManagerAttributes { Browsable = false }));
            MaterialsPanelOpen = Config.Bind(
                "UI State",
                "Materials Panel Open",
                false,
                new ConfigDescription(
                    "Compatibility state for the jointly visible Renderers and Materials panels.",
                    null,
                    new ConfigurationManagerAttributes { Browsable = false }));
            DragSensitivity = Config.Bind("Config", "Drag Sensitivity", 30f, new ConfigDescription("Controls the sensitivity of dragging labels to edit float values", new AcceptableValueRange<float>(1f, 100f), new ConfigurationManagerAttributes { Order = 3, ShowRangeAsPercent = false }));
            PreventDragout = Config.Bind(
                "Config",
                "Prevent Window Dragout",
                true,
                new ConfigDescription(
                    "Compatibility alias synchronized with Window Drag Limits: false selects NoLimits; true selects KeepHeaderInside.",
                    null,
                    new ConfigurationManagerAttributes { Browsable = false }));
            WindowDragMode = Config.Bind(
                "Config",
                "Window Drag Limits",
                MaterialEditorWindowBoundsPolicy.FromLegacy(
                    PreventDragout.Value),
                "Controls which visible part of the Material Editor window must remain inside the game window while dragging.");
            PreventDragout.SettingChanged += HandleLegacyWindowDragSettingChanged;
            WindowDragMode.SettingChanged += HandleWindowDragModeChanged;
            SynchronizeLegacyWindowDragSetting();
            WatchTexChanges = Config.Bind("Config", "Watch File Changes", true, new ConfigDescription("Watch for file changes and reload textures on change. Can be toggled in the UI.", null, new ConfigurationManagerAttributes { Order = 2 }));
            ShaderOptimization = Config.Bind("Config", "Shader Optimization", true, new ConfigDescription("Replaces every loaded shader with the MaterialEditor copy of the shader. Reduces the number of copies of shaders loaded which reduces RAM usage and improves performance.", null, new ConfigurationManagerAttributes { Order = 1 }));
            ExportBakedMesh = Config.Bind("Config", "Export Baked Mesh", false, new ConfigDescription("When enabled, skinned meshes will be exported in their current state with all customization applied as well as in the current pose.", null, new ConfigurationManagerAttributes { Order = 1 }));
            ExportBakedWorldPosition = Config.Bind("Config", "Export Baked World Position", false, new ConfigDescription("When enabled, objects will be exported with their position changes intact so that, i.e. when exporting two objects they retain their position relative to each other.\nOnly works when Export Baked Mesh is also enabled.", null, new ConfigurationManagerAttributes { Order = 1 }));
            ConfigExportPath = Config.Bind("Config", "Export Path Override", "", new ConfigDescription("Textures and models will be exported to this folder. If empty, exports to UserData\\MaterialEditor.", null, new ConfigurationManagerAttributes { Order = 1 }));
            PersistFilter = Config.Bind("Config", "Persist Filter", false, "Persist search filter across editor windows");
            Showtooltips = Config.Bind("Config", "Show Tooltips", true, "Whether to show tooltips or not");
            EnableShaderHints = Config.Bind(
                "Config",
                "Enable Shader Hints",
                true,
                "Show shader-authored hints while holding Shift. This is independent of the Show Tooltips setting.");
            SortPropertiesByType = Config.Bind("Config", "Sort Properties by Type", true, "Whether to sort shader properties by their types.");
            SortPropertiesByName = Config.Bind("Config", "Sort Properties by Name", true, "Whether to sort shader properties by their names.");
            SortPropertiesByCategory = Config.Bind("Config", "Sort Properties by Category", true, "Whether to sort shader properties by their category.");
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
            UICategoriesWidth.SettingChanged += MaterialEditorUI.UISettingChanged;
            UIListWidth.SettingChanged += MaterialEditorUI.UISettingChanged;
            UITheme.SettingChanged += MaterialEditorUI.UIThemeSettingChanged;
            WatchTexChanges.SettingChanged += WatchTexChanges_SettingChanged;
            ShaderOptimization.SettingChanged += ShaderOptimization_SettingChanged;
            ConfigExportPath.SettingChanged += ConfigExportPath_SettingChanged;
            SortPropertiesByType.SettingChanged += (object sender, EventArgs e) => PropertyOrganizer.Refresh();
            SortPropertiesByName.SettingChanged += (object sender, EventArgs e) => PropertyOrganizer.Refresh();
            SortPropertiesByCategory.SettingChanged += (object sender, EventArgs e) => PropertyOrganizer.Refresh();
            SetExportPath();

            ResourceRedirection.RegisterAssetLoadedHook(HookBehaviour.OneCallbackPerResourceLoaded, AssetLoadedHook);
            LoadXML();
        }

        internal static void ToggleUITheme()
        {
            if (UITheme == null)
                return;

            UITheme.Value = MaterialEditorTheme.ToggleMode
                            == MaterialEditorThemeMode.Dark
                ? DarkThemeSetting
                : LightThemeSetting;
        }

        internal static MaterialEditorThemeMode GetConfiguredUITheme()
        {
            return UITheme != null
                   && string.Equals(
                       UITheme.Value,
                       DarkThemeSetting,
                       StringComparison.OrdinalIgnoreCase)
                ? MaterialEditorThemeMode.Dark
                : MaterialEditorThemeMode.Legacy;
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
            ShaderPropertyFallbacks.Reset();

            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{nameof(MaterialEditorAPI)}.Resources.default.xml"))
                if (stream != null)
                    using (XmlReader reader = XmlReader.Create(stream))
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.Load(stream);
                        XmlElement materialEditorElement = doc.DocumentElement;
                        Action<string> metadataWarning = message =>
                            Logger?.LogWarning("Material Editor default metadata: " + message);
                        var schemaVersion = ShaderPropertyMetadataParser.ReadSchemaVersion(
                            materialEditorElement,
                            metadataWarning);

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
                                    var declarationOrder = 0;
                                    foreach (var shaderPropertyElementObj in shaderPropertyElements)
                                    {
                                        if (shaderPropertyElementObj != null)
                                        {
                                            var shaderPropertyElement = (XmlElement)shaderPropertyElementObj;
                                            {
                                                ShaderPropertyData shaderPropertyData;
                                                if (!ShaderPropertyData.TryParse(
                                                        shaderPropertyElement,
                                                        metadataWarning,
                                                        out shaderPropertyData,
                                                        schemaVersion))
                                                {
                                                    declarationOrder++;
                                                    continue;
                                                }

                                                shaderPropertyData.DeclarationOrder = declarationOrder++;
                                                ShaderPropertyFallbacks.MergeInto(
                                                    XMLShaderProperties["default"],
                                                    shaderPropertyData,
                                                    "MaterialEditor.API default",
                                                    message => Logger?.LogWarning(
                                                        "Material Editor fallback: " + message));
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
                MaterialEditorUI.DisposeTexChangeWatcher();
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
            Texture2D tex = null;
            try
            {
                RenderTexture.active = renderTexture;
                tex = new Texture2D(renderTexture.width, renderTexture.height);
                tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
                return tex;
            }
            catch
            {
                // Ownership transfers to the caller only after a successful read.
                if (tex != null)
                    DestroyImmediate(tex);
                throw;
            }
            finally
            {
                RenderTexture.active = currentActiveRT;
            }
        }

        internal static void SaveTexR(RenderTexture renderTexture, string path)
        {
            var tex = GetT2D(renderTexture);
            try
            {
                File.WriteAllBytes(path, EncodeTextureToPng(tex));
            }
            finally
            {
                DestroyImmediate(tex);
            }
        }

        internal static byte[] EncodeTextureToPng(Texture2D texture)
        {
            return texture.EncodeToPNG();
        }

        internal static void SaveTex(Texture tex, string path, RenderTextureFormat rtf = RenderTextureFormat.Default, RenderTextureReadWrite cs = RenderTextureReadWrite.Default)
        {
            var tmp = RenderTexture.GetTemporary(tex.width, tex.height, 0, rtf, cs);
            var currentActiveRT = RenderTexture.active;
            try
            {
                RenderTexture.active = tmp;
                GL.Clear(false, true, new Color(0, 0, 0, 0));
                Graphics.Blit(tex, tmp);
                SaveTexR(tmp, path);
            }
            finally
            {
                RenderTexture.active = currentActiveRT;
                RenderTexture.ReleaseTemporary(tmp);
            }
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
            // Preserve the flat Category attribute when a nested declaration
            // is cloned into the shared default-property catalog.
            internal string CategoryBeforeHierarchy;
            // Stable presentation hierarchy read from optional schema-2
            // Category/Subcategory parents. Category remains the flat
            // grouping/display value for consumers without hierarchy support.
            internal string CategoryId;
            internal string CategoryDisplayName;
            internal bool HasExplicitCategoryDisplayName;
            internal string SubcategoryId;
            internal string SubcategoryDisplayName;
            internal bool HasExplicitSubcategoryDisplayName;
            internal int? CategoryOrder;
            internal int DeclarationOrder;
            /// <summary>
            /// Optional label shown by Material Editor. Defaults to <see cref="Name"/>.
            /// </summary>
            public string DisplayName;
            internal bool HasExplicitDisplayName;
            /// <summary>
            /// Optional explicit ordering value from schema 2 metadata.
            /// </summary>
            public int? Order;
            /// <summary>
            /// Optional semantic editor identifier. A null value uses the editor implied by <see cref="Type"/>.
            /// </summary>
            public string EditorId;
            /// <summary>
            /// Optional inline English tooltip.
            /// </summary>
            public string TooltipText;
            /// <summary>
            /// Optional logical group identifier.
            /// </summary>
            public string Group;
            /// <summary>
            /// Optional condition controlling whether the property is shown.
            /// </summary>
            public MaterialEditorPropertyCondition ShowIf;
            /// <summary>
            /// Options used by an enum property editor.
            /// </summary>
            public List<MaterialEditorEnumOption> EnumOptions;
            /// <summary>
            /// Number of components shown by a vector editor, when specified.
            /// </summary>
            public int? VectorComponentCount;
            internal bool Invert;
            /// <summary>
            /// Numeric value written for the off state of a float-backed toggle.
            /// </summary>
            public float OffValue;
            /// <summary>
            /// Numeric value written for the on state of a float-backed toggle.
            /// </summary>
            public float OnValue;

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
                DisplayName = name;
                EnumOptions = new List<MaterialEditorEnumOption>();
                OffValue = 0f;
                OnValue = 1f;
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
                    if (TryParseManifestFloat(minValue, out float min)
                        && TryParseManifestFloat(maxValue, out float max))
                    {
                        MinValue = min;
                        MaxValue = max;
                    }
                }

                Hidden = bool.TryParse(hidden, out bool result) && result;
                Category = category;
            }

            internal static bool TryParse(
                XmlElement propertyElement,
                Action<string> warning,
                out ShaderPropertyData propertyData,
                int schemaVersion = 2)
            {
                propertyData = null;
                if (propertyElement == null)
                    return false;

                var propertyName = propertyElement.GetAttribute("Name").Trim();
                if (propertyName.Length == 0)
                {
                    warning?.Invoke("A shader Property without a Name was ignored.");
                    return false;
                }

                var declaredPropertyType = propertyElement.GetAttribute("Type");
                ShaderPropertyType propertyType;
                string aliasEditorId;
                if (!TryParsePropertyType(
                        declaredPropertyType,
                        schemaVersion,
                        out propertyType,
                        out aliasEditorId))
                {
                    warning?.Invoke(
                        "Shader property '" + propertyName + "' has unknown Type '"
                        + declaredPropertyType + "' and was ignored.");
                    return false;
                }
                if (schemaVersion >= 2
                    && string.Equals(
                        declaredPropertyType == null
                            ? string.Empty
                            : declaredPropertyType.Trim(),
                        "Dropdown",
                        StringComparison.OrdinalIgnoreCase))
                {
                    warning?.Invoke(
                        "Shader property '" + propertyName
                        + "' uses Type 'Dropdown'; use Type 'Enum' with the Enums attribute.");
                }

                string min = null;
                string max = null;
                var range = propertyElement.GetAttribute("Range");
                if (!range.IsNullOrWhiteSpace())
                {
                    var rangeSplit = range.Split(',');
                    float parsedMinimum;
                    float parsedMaximum;
                    if (rangeSplit.Length == 2
                        && TryParseManifestFloat(rangeSplit[0], out parsedMinimum)
                        && TryParseManifestFloat(rangeSplit[1], out parsedMaximum))
                    {
                        min = parsedMinimum.ToString(CultureInfo.InvariantCulture);
                        max = parsedMaximum.ToString(CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        warning?.Invoke(
                            "Shader property '" + propertyName + "' has invalid Range '"
                            + range + "'; the default slider range will be used.");
                    }
                }

                propertyData = new ShaderPropertyData(
                    propertyName,
                    propertyType,
                    propertyElement.GetAttribute("DefaultValue"),
                    propertyElement.GetAttribute("DefaultValueAssetBundle"),
                    propertyElement.GetAttribute("AnisoLevel"),
                    propertyElement.GetAttribute("FilterMode"),
                    propertyElement.GetAttribute("WrapMode"),
                    min,
                    max,
                    propertyElement.GetAttribute("Hidden"),
                    propertyElement.GetAttribute("Category"));

                var metadata = schemaVersion >= 2
                    ? ShaderPropertyMetadataParser.Parse(
                        propertyElement,
                        warning)
                    : new ShaderPropertyUiMetadata();
                var isCanonicalBoolean = schemaVersion >= 2
                    && string.Equals(
                        declaredPropertyType == null
                            ? string.Empty
                            : declaredPropertyType.Trim(),
                        "Boolean",
                        StringComparison.OrdinalIgnoreCase);
                if (isCanonicalBoolean)
                {
                    if (propertyElement.HasAttribute("OffValue")
                        || propertyElement.HasAttribute("OnValue"))
                    {
                        warning?.Invoke(
                            "Shader property '" + propertyName
                            + "' uses fixed Boolean values 0 and 1; OffValue/OnValue attributes are ignored. Use Invert=\"true\" to swap them.");
                    }
                    metadata.OffValue = metadata.Invert ? 1f : 0f;
                    metadata.OnValue = metadata.Invert ? 0f : 1f;
                }
                var hasExplicitEditor = schemaVersion >= 2
                    && !propertyElement.GetAttribute("Editor").IsNullOrWhiteSpace();
                if (!hasExplicitEditor
                    && metadata.EditorId.IsNullOrEmpty()
                    && !aliasEditorId.IsNullOrEmpty())
                {
                    metadata.EditorId = aliasEditorId;
                }
                if (!ShaderPropertyEditorPolicy.IsCompatible(
                        metadata.EditorId,
                        propertyType))
                {
                    warning?.Invoke(
                        "Shader property '" + propertyName + "' declares Editor '"
                        + propertyElement.GetAttribute("Editor") + "' for Type '"
                        + declaredPropertyType + "' (normalized backing Type '"
                        + propertyType + "'); its type editor will be used.");
                    metadata.EditorId = null;
                    metadata.VectorComponentCount = null;
                }
                if (metadata.EditorId == MaterialEditorPropertyEditorIds.Enum
                    && metadata.EnumOptions.Count == 0)
                {
                    warning?.Invoke(
                        "Shader property '" + propertyName + "' declares Type '"
                        + declaredPropertyType
                        + "' as an enum without a valid Enums attribute or Option elements; "
                        + "the Float editor will be used.");
                    metadata.EditorId = null;
                }
                propertyData.DisplayName = metadata.DisplayName.IsNullOrEmpty()
                    ? propertyName
                    : metadata.DisplayName;
                propertyData.HasExplicitDisplayName = schemaVersion >= 2
                    && !metadata.DisplayName.IsNullOrEmpty();
                propertyData.Order = metadata.Order;
                propertyData.CategoryOrder = metadata.CategoryOrder;
                propertyData.EditorId = metadata.EditorId;
                propertyData.TooltipText = metadata.TooltipText;
                propertyData.Group = metadata.Group;
                propertyData.ShowIf = metadata.ShowIf;
                propertyData.EnumOptions.AddRange(metadata.EnumOptions);
                propertyData.VectorComponentCount = metadata.VectorComponentCount;
                propertyData.Invert = metadata.Invert;
                propertyData.OffValue = metadata.OffValue;
                propertyData.OnValue = metadata.OnValue;
                if (schemaVersion >= 2)
                {
                    ApplyHierarchyMetadata(
                        propertyElement,
                        propertyData,
                        warning);
                }
                return true;
            }

            internal ShaderPropertyData WithoutHierarchyForDefaultFallback()
            {
                if (string.IsNullOrEmpty(CategoryId)
                    && string.IsNullOrEmpty(SubcategoryId))
                    return this;

                var fallback = (ShaderPropertyData)MemberwiseClone();
                fallback.CategoryId = null;
                fallback.CategoryDisplayName = null;
                fallback.HasExplicitCategoryDisplayName = false;
                fallback.SubcategoryId = null;
                fallback.SubcategoryDisplayName = null;
                fallback.HasExplicitSubcategoryDisplayName = false;
                fallback.Category = CategoryBeforeHierarchy;
                fallback.CategoryBeforeHierarchy = null;
                return fallback;
            }

            internal ShaderPropertyData WithoutConditionsForUiFallback()
            {
                if (ShowIf == null)
                    return this;

                var fallback = (ShaderPropertyData)MemberwiseClone();
                fallback.ShowIf = null;
                return fallback;
            }

            private static void ApplyHierarchyMetadata(
                XmlElement propertyElement,
                ShaderPropertyData propertyData,
                Action<string> warning)
            {
                var parent = propertyElement.ParentNode as XmlElement;
                XmlElement subcategoryElement = null;
                XmlElement categoryElement = null;

                if (HasElementName(parent, "Subcategory"))
                {
                    subcategoryElement = parent;
                    parent = parent.ParentNode as XmlElement;
                }

                if (HasElementName(parent, "Category"))
                    categoryElement = parent;

                if (subcategoryElement != null && categoryElement == null)
                {
                    warning?.Invoke(
                        "Shader property '" + propertyData.Name
                        + "' is inside a Subcategory without a parent Category; "
                        + "the Subcategory metadata was ignored.");
                    return;
                }

                if (categoryElement == null)
                    return;

                var categoryId = ReadHierarchyAttribute(categoryElement, "Id");
                if (categoryId == null)
                {
                    warning?.Invoke(
                        "Shader property '" + propertyData.Name
                        + "' is inside a Category without an Id; the nested "
                        + "Category metadata was ignored.");
                    return;
                }

                var declaredCategoryDisplayName =
                    ReadHierarchyAttribute(categoryElement, "DisplayName");
                var categoryDisplayName = declaredCategoryDisplayName
                                          ?? categoryId;
                propertyData.CategoryBeforeHierarchy = propertyData.Category;
                propertyData.CategoryId = categoryId;
                propertyData.CategoryDisplayName = categoryDisplayName;
                propertyData.HasExplicitCategoryDisplayName =
                    declaredCategoryDisplayName != null;
                propertyData.Category = categoryDisplayName;

                if (subcategoryElement == null)
                    return;

                var subcategoryId =
                    ReadHierarchyAttribute(subcategoryElement, "Id");
                if (subcategoryId == null)
                {
                    warning?.Invoke(
                        "Shader property '" + propertyData.Name
                        + "' is inside a Subcategory without an Id; the "
                        + "Subcategory metadata was ignored.");
                    return;
                }

                propertyData.SubcategoryId = subcategoryId;
                var declaredSubcategoryDisplayName =
                    ReadHierarchyAttribute(subcategoryElement, "DisplayName");
                propertyData.SubcategoryDisplayName =
                    declaredSubcategoryDisplayName ?? subcategoryId;
                propertyData.HasExplicitSubcategoryDisplayName =
                    declaredSubcategoryDisplayName != null;
            }

            private static bool HasElementName(
                XmlElement element,
                string expectedName)
            {
                return element != null
                       && string.Equals(
                           element.LocalName,
                           expectedName,
                           StringComparison.Ordinal);
            }

            private static string ReadHierarchyAttribute(
                XmlElement element,
                string attributeName)
            {
                if (element == null || !element.HasAttribute(attributeName))
                    return null;
                var value = element.GetAttribute(attributeName).Trim();
                return value.Length == 0 ? null : value;
            }

            private static bool TryParsePropertyType(
                string value,
                int schemaVersion,
                out ShaderPropertyType propertyType,
                out string aliasEditorId)
            {
                propertyType = default(ShaderPropertyType);
                aliasEditorId = null;
                if (value.IsNullOrWhiteSpace())
                    return false;

                var trimmedValue = value.Trim();
                if (schemaVersion >= 2)
                {
                    if (string.Equals(
                            trimmedValue,
                            "Boolean",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        propertyType = ShaderPropertyType.Float;
                        aliasEditorId = MaterialEditorPropertyEditorIds.Toggle;
                        return true;
                    }

                    if (string.Equals(
                            trimmedValue,
                            "Dropdown",
                            StringComparison.OrdinalIgnoreCase)
                        || string.Equals(
                            trimmedValue,
                            "Enum",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        propertyType = ShaderPropertyType.Float;
                        aliasEditorId = MaterialEditorPropertyEditorIds.Enum;
                        return true;
                    }
                }

                try
                {
                    var parsed = (ShaderPropertyType)Enum.Parse(
                        typeof(ShaderPropertyType),
                        trimmedValue,
                        true);
                    if (!Enum.IsDefined(typeof(ShaderPropertyType), parsed))
                        return false;
                    propertyType = parsed;
                    return true;
                }
                catch (ArgumentException)
                {
                    return false;
                }
                catch (OverflowException)
                {
                    return false;
                }
            }

            private static bool TryParseManifestFloat(string value, out float result)
            {
                if ((float.TryParse(
                         value,
                         NumberStyles.Float,
                         CultureInfo.InvariantCulture,
                         out result)
                     || float.TryParse(value, out result))
                    && !float.IsNaN(result)
                    && !float.IsInfinity(result))
                {
                    return true;
                }

                result = 0f;
                return false;
            }
        }
    }
}
