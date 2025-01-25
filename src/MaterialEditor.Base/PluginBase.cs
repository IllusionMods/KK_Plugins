using BepInEx;
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
        public static new ManualLogSource Logger;
        public static MaterialEditorPluginBase Instance;

        /// <summary>
        /// Path where textures will be exported
        /// </summary>
        public static string ExportPathDefault = Path.Combine(Paths.GameRootPath, @"UserData\MaterialEditor");
        /// <summary>
        /// Path where textures will be exported
        /// </summary>
        public static string ExportPath = ExportPathDefault;
        /// <summary>
        /// Saved material edits
        /// </summary>
        public static CopyContainer CopyData = new CopyContainer();

        public static Dictionary<string, ShaderData> LoadedShaders = new Dictionary<string, ShaderData>();
        public static SortedDictionary<string, Dictionary<string, ShaderPropertyData>> XMLShaderProperties = new SortedDictionary<string, Dictionary<string, ShaderPropertyData>>();

        public static ConfigEntry<float> UIScale { get; set; }
        public static ConfigEntry<float> UIWidth { get; set; }
        public static ConfigEntry<float> UIHeight { get; set; }
        public static ConfigEntry<float> UIListWidth { get; set; }
        public static ConfigEntry<float> DragSensitivity { get; set; }
        public static ConfigEntry<int> FilterDelay { get; set; }
        public static ConfigEntry<bool> WatchTexChanges { get; set; }
        public static ConfigEntry<bool> ShaderOptimization { get; set; }
        public static ConfigEntry<bool> ExportBakedMesh { get; set; }
        public static ConfigEntry<bool> ExportBakedWorldPosition { get; set; }
        internal static ConfigEntry<string> ConfigExportPath { get; private set; }
        public static ConfigEntry<bool> PersistFilter { get; set; }
        public static ConfigEntry<bool> Showtooltips { get; set; }
        public static ConfigEntry<bool> SortPropertiesByType { get; set; }
        public static ConfigEntry<bool> SortPropertiesByName { get; set; }
        public static ConfigEntry<float> ProjectorNearClipPlaneMax { get; set; }
        public static ConfigEntry<float> ProjectorFarClipPlaneMax { get; set; }
        public static ConfigEntry<float> ProjectorFieldOfViewMax { get; set; }
        public static ConfigEntry<float> ProjectorAspectRatioMax { get; set; }
        public static ConfigEntry<float> ProjectorOrthographicSizeMax { get; set; }

        public virtual void Awake()
        {
            Instance = this;
            Logger = base.Logger;
            Directory.CreateDirectory(ExportPath);

            UIScale = Config.Bind("Config", "UI Scale", 1.75f, new ConfigDescription("Controls the size of the window.", new AcceptableValueRange<float>(1f, 3f), new ConfigurationManagerAttributes { Order = 7 }));
            UIWidth = Config.Bind("Config", "UI Width", 0.3f, new ConfigDescription("Controls the size of the window.", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 6, ShowRangeAsPercent = false }));
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
            FilterDelay = Config.Bind("Config", "Filter search delay (in ms)", 200, new ConfigDescription("Time to wait until the filter actually refreshes the UI when stopped typing", new AcceptableValueRange<int>(1, 2000)));

            //Everything in these games is 10x the size of KK/KKS
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
            FilterDelay.SettingChanged += (sender, e) => MaterialEditorUI.filterTimer.Interval = FilterDelay.Value;
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
                                                string hidden = shaderPropertyElement.GetAttribute("Hidden");
                                                string range = shaderPropertyElement.GetAttribute("Range");
                                                string min = null;
                                                string max = null;
                                                string category = shaderPropertyElement.GetAttribute("Category");
                                                if (!range.IsNullOrWhiteSpace())
                                                {
                                                    var rangeSplit = range.Split(',');
                                                    if (rangeSplit.Length == 2)
                                                    {
                                                        min = rangeSplit[0];
                                                        max = rangeSplit[1];
                                                    }
                                                }
                                                ShaderPropertyData shaderPropertyData = new ShaderPropertyData(propertyName, propertyType, defaultValue, defaultValueAB, hidden, min, max, category);

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

        protected static void RefreshPropertyOrganization()
        {
            PropertyOrganizer.Refresh();
        }

        public class ShaderData
        {
            public string ShaderName;
            public Shader Shader;
            public int? RenderQueue;
            public bool ShaderOptimization;

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

        public class ShaderPropertyData
        {
            public string Name;
            public ShaderPropertyType Type;
            public string DefaultValue;
            public string DefaultValueAssetBundle;
            public bool Hidden;
            public float? MinValue;
            public float? MaxValue;
            public string Category;

            public ShaderPropertyData(string name, ShaderPropertyType type, string defaultValue = null, string defaultValueAB = null, string hidden = null, string minValue = null, string maxValue = null, string category = null)
            {
                Name = name;
                Type = type;
                DefaultValue = defaultValue.IsNullOrEmpty() ? null : defaultValue;
                DefaultValueAssetBundle = defaultValueAB.IsNullOrEmpty() ? null : defaultValueAB;
                Hidden = bool.TryParse(hidden, out bool result) && result;
                if (!minValue.IsNullOrWhiteSpace() && !maxValue.IsNullOrWhiteSpace())
                {
                    if (float.TryParse(minValue, out float min) && float.TryParse(maxValue, out float max))
                    {
                        MinValue = min;
                        MaxValue = max;
                    }
                }
                Category = category;
            }
        }
    }
}
