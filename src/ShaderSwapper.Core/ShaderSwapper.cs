using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using KK_Plugins.MaterialEditor;
using KKAPI.Maker;
using KKAPI.Studio;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using static KK_Plugins.MaterialEditor.MaterialEditorCharaController;
using static MaterialEditorAPI.MaterialAPI;
using Studio;
using MaterialEditorAPI;
using System.Linq;
using UnityEngine.UI;
using System.Xml.Linq;
using ADV.Commands.Base;
using ActionGame.Chara.Mover;
using System.IO;
using Screencap;

namespace KK_Plugins
{
    [BepInDependency(MaterialEditorPlugin.PluginGUID, MaterialEditorPlugin.PluginVersion)]
    [BepInDependency(ScreenshotManager.GUID, ScreenshotManager.Version)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class ShaderSwapper : BaseUnityPlugin
    {
        public const string PluginGUID = "com.deathweasel.bepinex.shaderswapper";
        public const string PluginName = "Shader Swapper";
        public const string PluginNameInternal = Constants.Prefix + "_ShaderSwapper";
        public const string PluginVersion = "1.6";
        internal static new ManualLogSource Logger;
        private static ShaderSwapper Instance;

        /// <summary>
        /// Represents a sequence of rules on what shader should be used as a replacement.
        /// <para/>
        /// Rules are evaluated in order; preceding rules will be evaluated first,
        /// and the first valid rule will be used.
        /// </summary>
        internal class SwapTargetList
        {
            /// <summary>
            /// Represents a rule on whether a shader should be used as a replacement.
            /// <para/>
            /// Each rule has an include and exclude list, defining the names of materials
            /// that should not have their shaders replaced.
            /// </summary>
            internal class Rule
            {
                /// <summary>
                /// Represents a list of string entries.
                /// <para/>
                /// Used for easier conversion between in memory List and serialized XElements.
                /// </summary>
                internal class EntryList
                {
                    public static string EntryElementName { get; } = "Entry";

                    public List<string> Entries { get; set; } = new List<string>();

                    public EntryList() { }

                    public EntryList(IEnumerable<string> entries)
                    {
                        Entries = new List<string>(entries);
                    }

                    public EntryList(IEnumerable<XElement> collection)
                    {
                        Entries = new List<string>(collection.Select(e => e.Value));
                    }

                    public IEnumerable<XElement> ToElements()
                    {
                        return Entries.Select(e => new XElement(EntryElementName, e));
                    }
                }

                public static string ElementName { get; } = "Rule";
                public static string ShaderAttributeName { get; } = "Name";
                public static string IncludeElementName { get; } = "Include";
                public static string ExcludeElementName { get; } = "Exclude";

                public string Shader { get; set; } = "MISSING SHADER NAME";
                public EntryList Include { get; set; } = new EntryList();
                public EntryList Exclude { get; set; } = new EntryList();

                public Rule(string shader)
                {
                    Shader = shader;
                }

                public Rule(string shader, IEnumerable<string> include, IEnumerable<string> exclude)
                {
                    Shader = shader;
                    Include = new EntryList(include);
                    Exclude = new EntryList(exclude);
                }

                public Rule(XElement element)
                {
                    if (element == null || element.Name != ElementName)
                        return;

                    if (element.Attribute(ShaderAttributeName) != null)
                        Shader = element.Attribute(ShaderAttributeName).Value;
                    if (element.Element(IncludeElementName) != null)
                        Include = new EntryList(element.Element(IncludeElementName).Elements());
                    if (element.Element(ExcludeElementName) != null)
                        Exclude = new EntryList(element.Element(ExcludeElementName).Elements());
                }

                public XElement ToElement()
                {
                    var element = new XElement(ElementName, new XAttribute(ShaderAttributeName, Shader));
                    if (Include.Entries.Count > 0)
                        element.Add(new XElement(IncludeElementName, Include.ToElements()));
                    if (Exclude.Entries.Count > 0)
                        element.Add(new XElement(ExcludeElementName, Exclude.ToElements()));
                    return element;
                }

                /// <summary>
                /// Gets the replacement shader for a material according to the predefined rules.
                /// </summary>
                /// <param name="material">The material whose shader to replace.</param>
                /// <param name="toShader">
                /// When this method returns, contains the name of the replacement shader,
                /// if the material's shader is to be replaced; otherwise, the value is undefined.
                /// </param>
                /// <returns><c>true</c> if the specified material should have its shader replaced; otherwise, <c>false</c>.</returns>
                public bool TryGetReplacementShader(Material material, out string toShader)
                {
                    toShader = "";
                    if (material == null)
                        return false;

                    string nonInstMatName = material.name.Replace(" (Instance)", "");
                    bool inIncludes = Include.Entries.Contains(nonInstMatName);
                    bool inExcludes = Exclude.Entries.Contains(nonInstMatName);

                    // filter exclusions
                    if (inExcludes && !inIncludes)
                        return false;

                    // filter inclusions without exclusions
                    if ((Exclude.Entries.Count == 0) && (Include.Entries.Count > 0) && !inIncludes)
                        return false;

                    toShader = Shader;
                    return true;
                }
            }

            public List<Rule> Rules { get; set; } = new List<Rule>();

            public SwapTargetList() { }

            public SwapTargetList(string shader) : this(new string[] { shader }) { }

            public SwapTargetList(IEnumerable<string> shaders)
            {
                Rules = shaders.Select(e => new Rule(e)).ToList();
            }

            public SwapTargetList(IEnumerable<XElement> collection)
            {
                Rules = new List<Rule>(collection.Select(e => new Rule(e)));
            }

            public IEnumerable<XElement> ToElements()
            {
                return Rules.Select(e => e.ToElement());
            }

            /// <summary>
            /// Gets the replacement shader for a material according to the predefined rules.
            /// </summary>
            /// <param name="material">The material whose shader to replace.</param>
            /// <param name="toShader">
            /// When this method returns, contains the name of the replacement shader,
            /// if the material's shader is to be replaced; otherwise, the value is undefined.
            /// </param>
            /// <returns><c>true</c> if the specified material should have its shader replaced; otherwise, <c>false</c>.</returns>
            public bool TryGetReplacementShader(Material material, out string toShader)
            {
                toShader = "";
                foreach (var rule in Rules)
                {
                    if (rule.TryGetReplacementShader(material, out toShader))
                        return true;
                }
                return false;
            }
        }

        internal static ConfigEntry<KeyboardShortcut> SwapShadersHotkey { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> ForceSwapShadersHotkey { get; private set; }
        internal static ConfigEntry<float> TesselationSlider { get; private set; }

        private readonly Dictionary<string, SwapTargetList> VanillaPlusShaders = new Dictionary<string, SwapTargetList>
        {
            {"Shader Forge/main_skin", new SwapTargetList("xukmi/SkinPlus") },
            {"Koikano/main_skin", new SwapTargetList("xukmi/SkinPlus") },
            {"Shader Forge/main_hair", new SwapTargetList("xukmi/HairPlus") },
            {"Koikano/hair_main_sun", new SwapTargetList("xukmi/HairPlus") },
            {"Shader Forge/main_hair_front", new SwapTargetList("xukmi/HairFrontPlus") },
            {"Koikano/hair_main_sun_front", new SwapTargetList("xukmi/HairFrontPlus") },
            {"Shader Forge/toon_eye_lod0", new SwapTargetList("xukmi/EyePlus") },
            {"Koikano/main_eye", new SwapTargetList("xukmi/EyePlus") },
            {"Shader Forge/toon_eyew_lod0", new SwapTargetList("xukmi/EyeWPlus") },
            {"Koikano/main_eyew", new SwapTargetList("xukmi/EyeWPlus") },
            {"Shader Forge/main_opaque", new SwapTargetList("xukmi/MainOpaquePlus") },
            {"Shader Forge/main_opaque2", new SwapTargetList("xukmi/MainOpaquePlus") },
            {"Koikano/main_clothes_opaque", new SwapTargetList("xukmi/MainOpaquePlus") },
            {"Shader Forge/main_alpha", new SwapTargetList("xukmi/MainAlphaPlus") },
            {"Koikano/main_clothes_alpha", new SwapTargetList("xukmi/MainAlphaPlus") },
            {"Shader Forge/main_item", new SwapTargetList("xukmi/MainItemPlus") },
            {"Koikano/main_clothes_item", new SwapTargetList("xukmi/MainItemPlus") },
            {"Shader Forge/main_item_studio", new SwapTargetList("xukmi/MainItemPlus") },
            {"Shader Forge/main_item_studio_alpha", new SwapTargetList("xukmi/MainItemAlphaPlus") },
            {"ShaderForge/main_StandardMDK_studio", new SwapTargetList("xukmi/MainItemPlus") },
            {"Standard", new SwapTargetList("xukmi/MainItemPlus") },
        };

        private readonly Dictionary<string, SwapTargetList> VanillaPlusTesselationShaders = new Dictionary<string, SwapTargetList>
        {
            {"Shader Forge/main_skin", new SwapTargetList("xukmi/SkinPlusTess") },
            {"Koikano/main_skin", new SwapTargetList("xukmi/SkinPlusTess") },
            {"Shader Forge/main_hair", new SwapTargetList("xukmi/HairPlus") },
            {"Koikano/hair_main_sun", new SwapTargetList("xukmi/HairPlus") },
            {"Shader Forge/main_hair_front", new SwapTargetList("xukmi/HairFrontPlus") },
            {"Koikano/hair_main_sun_front", new SwapTargetList("xukmi/HairFrontPlus") },
            {"Shader Forge/toon_eye_lod0", new SwapTargetList("xukmi/EyePlus") },
            {"Koikano/main_eye", new SwapTargetList("xukmi/EyePlus") },
            {"Shader Forge/toon_eyew_lod0", new SwapTargetList("xukmi/EyeWPlus") },
            {"Koikano/main_eyew", new SwapTargetList("xukmi/EyeWPlus") },
            {"Shader Forge/main_opaque", new SwapTargetList("xukmi/MainOpaquePlusTess") },
            {"Shader Forge/main_opaque2", new SwapTargetList("xukmi/MainOpaquePlusTess") },
            {"Koikano/main_clothes_opaque", new SwapTargetList("xukmi/MainOpaquePlusTess") },
            {"Shader Forge/main_alpha", new SwapTargetList("xukmi/MainAlphaPlusTess") },
            {"Koikano/main_clothes_alpha", new SwapTargetList("xukmi/MainAlphaPlusTess") },
            {"Shader Forge/main_item", new SwapTargetList("xukmi/MainItemPlus") },
            {"Koikano/main_clothes_item", new SwapTargetList("xukmi/MainItemPlus") },
            {"Shader Forge/main_item_studio", new SwapTargetList("xukmi/MainOpaquePlusTess") },
            {"Shader Forge/main_item_studio_alpha", new SwapTargetList("xkumi/MainAlphaPlusTess") },
            {"ShaderForge/main_StandardMDK_studio", new SwapTargetList("xukmi/MainOpaquePlusTess") },
            {"Standard", new SwapTargetList("xukmi/MainOpaquePlusTess") },
        };

        internal static ConfigEntry<bool> AutoReplace { get; private set; }
        internal static ConfigEntry<bool> DebugLogging { get; private set; }

        internal static ConfigEntry<bool> SwapStudioShadersOnCharacter { get; private set; }
        internal static ConfigEntry<bool> AutoEnableOutline { get; private set; }

        internal static ConfigEntry<string> NormalMapping { get; private set; }
        internal static ConfigEntry<string> TessMapping { get; private set; }

        internal static ConfigEntry<bool> TessMinOverrideScreenshots { get; private set; }
        internal static ConfigEntry<bool> TessMinOverride { get; private set; }

        private readonly Harmony _harmony = new Harmony(PluginGUID);

        private Dictionary<string, SwapTargetList> VanillaPlusShaderMapping { get => convertShaderMapping(false); set => setShaderMapping(false, value); }

        private Dictionary<string, SwapTargetList> VanillaPlusTessShaderMapping { get => convertShaderMapping(true); set => setShaderMapping(true, value); }

        private Dictionary<string, SwapTargetList> convertShaderMapping(bool tess)
        {
            return XElement.Load(tess ? TessMapping.Value : NormalMapping.Value).Elements().ToDictionary(
                e => e.Attribute("From").Value,
                e => e.HasElements ? new SwapTargetList(e.Elements()) : new SwapTargetList(e.Attribute("To").Value)); // support backwards compat
        }
        private void setShaderMapping(bool tess, Dictionary<string, SwapTargetList> value)
        {
            string text = new XElement("ShaderSwapper",
                value.Select(e => new XElement("Mapping", new XAttribute("From", e.Key), e.Value.ToElements()))
            ).ToString();
            using (StreamWriter outputFile = new StreamWriter(tess ? TessMapping.Value : NormalMapping.Value))
            {
                outputFile.Write(text);
            }
        }

        private void Awake()
        {
            Logger = base.Logger;
            Instance = this;

            SwapShadersHotkey = Config.Bind("Keyboard Shortcuts", "Swap Shaders", new KeyboardShortcut(KeyCode.P, KeyCode.RightControl), "Swap all shaders to the equivalent shader from the shadermapping, unless they are already changed in ME.");
            ForceSwapShadersHotkey = Config.Bind("Keyboard Shortcuts", "Force Swap Shaders", new KeyboardShortcut(KeyCode.P, KeyCode.RightControl, KeyCode.RightShift), "Swap all shaders to the equivalent shader from the shadermapping, regardless of they have been edited or not.");
            TesselationSlider = Config.Bind("Tesselation", "Tesselation", 0f,
                new ConfigDescription("The amount of tesselation to apply.  Leave at 0% to use the regular Vanilla+ shaders without tesselation.",
                    new AcceptableValueRange<float>(0f, 1f)
                )
            );

            DebugLogging = Config.Bind("General", "Verbose logging", true, "Write to log every time a shader is swapped.");

            AutoReplace = Config.Bind("General", "Auto swap to V+ shaders", false,
                "Automatically swap vanilla shaders to their Vanilla+ equivalents on ALL characters.\n" +
                "Changes take effect after character reload.\n" +
                "WARNING: Saving the game, cards, or scenes with this setting enabled can permanently apply the V+ shaders! You won't be able to go back to vanilla shaders without manually resetting MaterialEditor edits in the maker!");

            SwapStudioShadersOnCharacter = Config.Bind("General", "Studio-shaders on characters", false, "Toggles if the following shaders should be swapped on characters: [Shader Forge/main_item_studio],[Shader Forge/main_item_studio_alpha],[ShaderForge/main_StandartMDK_studio] and [Standart]");

            AutoEnableOutline = Config.Bind("General", "Auto enable outline", true, "Automatically sets the OutlineOn shaderproperty to 1");

            void ApplyPatches(bool enable)
            {
                var autoReplacePatchTargetM = AccessTools.Method(typeof(MaterialEditorCharaController), "LoadCharacterExtSaveData");
                var autoReplacePatchHookM = new HarmonyMethod(typeof(ShaderSwapper), nameof(ShaderSwapper.LoadHook));
                if (enable)
                    _harmony.Patch(autoReplacePatchTargetM, postfix: autoReplacePatchHookM);
                else
                    _harmony.Unpatch(autoReplacePatchTargetM, autoReplacePatchHookM.method);
            }
            AutoReplace.SettingChanged += (sender, args) => ApplyPatches(AutoReplace.Value);
            if (AutoReplace.Value) ApplyPatches(true);

            // XML stuff
            NormalMapping = Config.Bind("Mapping", "Normal Shader Mapping", "./UserData/config/shader_swapper_normal.xml", new ConfigDescription("XML file with mapping for shaders which is used when the tesselation setting = 0", null, new ConfigurationManagerAttributes { CustomDrawer = FileInputDrawer }));
            TessMapping = Config.Bind("Mapping", "Tess Shader Mapping", "./UserData/config/shader_swapper_tess.xml", new ConfigDescription("XML file with mapping for shaders which is used when the tesselation setting > 0", null, new ConfigurationManagerAttributes { CustomDrawer = FileInputDrawer }));
            if (!File.Exists(NormalMapping.Value)) setShaderMapping(false, VanillaPlusShaders);
            if (!File.Exists(TessMapping.Value)) setShaderMapping(true, VanillaPlusTesselationShaders);

            TessMinOverride = Config.Bind("Tesselation", "TessMin Clamping", true, "Clamp the TessMin value of all xukmi *Tess shaders to 1.0 - 1.5 range to improve performance with cards that have this set way too high.\nWarning: The limited value could be saved to the card/scene (but shouldn't).");
            TessMinOverrideScreenshots = Config.Bind("Tesselation", "TessMin Override on Screenshots", true, "Temporarily override the TessMin value of all xukmi *Tess shaders to 25 when taking screenshots to potentially improve quality (minimal speed hit but may have no perceptible effect).");

            _harmony.Patch(AccessTools.Method(typeof(MaterialAPI), nameof(MaterialAPI.SetFloat)), prefix: new HarmonyMethod(typeof(ShaderSwapper), nameof(ShaderSwapper.SetFloatHook)));
            ScreenshotManager.OnPreCapture += () => ScreenshotEvent(25);
            ScreenshotManager.OnPostCapture += () => ScreenshotEvent(TessMinOverride.Value ? 1 : -1);
        }

        internal static KKAPI.Utilities.OpenFileDialog.OpenSaveFileDialgueFlags SingleFileFlags =
                KKAPI.Utilities.OpenFileDialog.OpenSaveFileDialgueFlags.OFN_FILEMUSTEXIST |
                KKAPI.Utilities.OpenFileDialog.OpenSaveFileDialgueFlags.OFN_LONGNAMES |
                KKAPI.Utilities.OpenFileDialog.OpenSaveFileDialgueFlags.OFN_EXPLORER;

        static void FileInputDrawer(ConfigEntryBase entry)
        {
            GUILayout.BeginHorizontal();
            string value = entry.BoxedValue.ToString();
            value = GUILayout.TextField(value, GUILayout.MaxWidth(200), GUILayout.ExpandWidth(true));
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                string dir = Path.GetDirectoryName(value);
                string[] file = KKAPI.Utilities.OpenFileDialog.ShowDialog("Open 3D file", dir,
                        "XML (*.xml)|*.xml| All files (*.*)|*.*",
                        "xml", SingleFileFlags);
                if (file != null)
                {
                    value = file[0];
                }
            }
            entry.BoxedValue = value;
            GUILayout.EndHorizontal();
        }

        private static void LoadHook(MaterialEditorCharaController __instance)
        {
            Instance.UpdateCharShaders(__instance.ChaControl);
        }

        private void Update()
        {
            if (SwapShadersHotkey.Value.IsDown())
            {
                if (MakerAPI.InsideAndLoaded)
                {
                    var chaControl = MakerAPI.GetCharacterControl();
                    UpdateCharShaders(chaControl);
                }
                else if (StudioAPI.InsideStudio)
                {
                    foreach (var obj in StudioAPI.GetSelectedObjects())
                    {
                        if (obj is OCIChar cha) UpdateCharShaders(cha.GetChaControl());
                        if (obj is OCIItem item) UpdateItemShader(item);
                    }
                }
            }
            if (ForceSwapShadersHotkey.Value.IsDown())
            {
                if (MakerAPI.InsideAndLoaded)
                {
                    var chaControl = MakerAPI.GetCharacterControl();
                    UpdateCharShaders(chaControl, true);
                }
                else if (StudioAPI.InsideStudio)
                {
                    foreach (var obj in StudioAPI.GetSelectedObjects())
                    {
                        if (obj is OCIChar cha) UpdateCharShaders(cha.GetChaControl(), true);
                        if (obj is OCIItem item) UpdateItemShader(item, true);
                    }
                }
            }
        }

        private void UpdateItemShader(OCIItem item, bool forceUpdate = false)
        {
            GameObject go = item.objectItem;
            foreach (Renderer renderer in GetRendererList(go))
                foreach (Material material in GetMaterials(go, renderer))
                    SwapToVanillaPlus(GetSceneController(), item.objectInfo.dicKey, material, forceUpdate);
        }

        public void UpdateCharShaders(ChaControl chaControl, bool forceUpdate = false)
        {
            var controller = GetController(chaControl);
            for (var i = 0; i < controller.ChaControl.objClothes.Length; i++)
                SwapToVanillaPlusClothes(controller, i, forceUpdate);
            for (var i = 0; i < controller.ChaControl.objHair.Length; i++)
                SwapToVanillaPlusHair(controller, i, forceUpdate);
            for (var i = 0; i < controller.ChaControl.GetAccessoryObjects().Length; i++)
                SwapToVanillaPlusAccessory(controller, i, forceUpdate);
            SwapToVanillaPlusBody(controller, forceUpdate);
        }

        public static MaterialEditorCharaController GetController(ChaControl chaControl)
        {
            if (chaControl == null || chaControl.gameObject == null)
                return null;
            return chaControl.gameObject.GetComponent<MaterialEditorCharaController>();
        }

        public static SceneController GetSceneController()
        {
            return GameObject.Find("BepInEx_Manager/SceneCustomFunctionController Zoo")?.GetComponent<SceneController>();
        }

        private void SwapToVanillaPlus(SceneController controller, int id, Material mat, bool forceSwap)
        {
            if (controller.GetMaterialShader(id, mat) == null || forceSwap)
            {
                string oldShader = mat.shader.name;
                Dictionary<string, SwapTargetList> mapping = (TesselationSlider.Value > 0) ? VanillaPlusTessShaderMapping : VanillaPlusShaderMapping;
                if (mapping.TryGetValue(mat.shader.name, out var swapTargetList))
                {
                    string newShader;
                    if (!swapTargetList.TryGetReplacementShader(mat, out newShader))
                        return;

                    if (DebugLogging.Value)
                        Logger.LogDebug($"Replacing shader [{mat.shader.name}] with [{newShader}] on [{(Studio.Studio.Instance.dicObjectCtrl.TryGetValue(id, out var value) ? value.treeNodeObject.textName : null)}]");

                    int renderQueue = mat.renderQueue;
                    controller.SetMaterialShader(id, mat, newShader);
                    controller.SetMaterialShaderRenderQueue(id, mat, renderQueue);
                    if (mat.shader.name == "xukmi/MainAlphaPlus")
                        controller.SetMaterialFloatProperty(id, mat, "Cutoff", 0.1f);
                    if (oldShader == "Standard")
                        controller.SetMaterialFloatProperty(id, mat, "Cutoff", 0f);

                    if (TesselationSlider.Value > 0)
                        SetTesselationValue(mat);
                }

                if (AutoEnableOutline.Value && mat.HasProperty("_OutlineOn"))
                {
                    controller.SetMaterialFloatProperty(id, mat, "OutlineOn", 1f);
                }
            }
        }

        private void SwapToVanillaPlus(MaterialEditorCharaController controller, int slot, ObjectType objectType, Material mat, GameObject go, bool forceSwap)
        {
            if (controller.GetMaterialShader(slot, ObjectType.Clothing, mat, go) == null || forceSwap)
            {
                string oldShader = mat.shader.name;
                if (!SwapStudioShadersOnCharacter.Value)
                {
                    if (new List<string>() {
                        "Shader Forge/main_item_studio",
                        "Shader Forge/main_item_studio_alpha",
                        "ShaderForge/main_StandartMDK_studio",
                        "Standard" }
                    .Contains(mat.shader.name))
                    {
                        return;
                    }
                }

                Dictionary<string, SwapTargetList> mapping = (TesselationSlider.Value > 0) ? VanillaPlusTessShaderMapping : VanillaPlusShaderMapping;
                if (mapping.TryGetValue(mat.shader.name, out var swapTargetList))
                {
                    string newShader;
                    if (!swapTargetList.TryGetReplacementShader(mat, out newShader))
                        return;

                    if (DebugLogging.Value)
                        Logger.LogDebug($"Replacing shader [{mat.shader.name}] with [{newShader}] on [{controller.ChaControl.fileParam.fullname}]");

                    int renderQueue = mat.renderQueue;
                    controller.SetMaterialShader(slot, objectType, mat, newShader, go);
                    controller.SetMaterialShaderRenderQueue(slot, objectType, mat, renderQueue, go);
                    if (mat.shader.name == "xukmi/MainAlphaPlus")
                        controller.SetMaterialFloatProperty(slot, objectType, mat, "Cutoff", 0.1f, go);

                    if (TesselationSlider.Value > 0)
                        SetTesselationValue(mat);
                }

                if (AutoEnableOutline.Value && mat.HasProperty("_OutlineOn"))
                {
                    controller.SetMaterialFloatProperty(slot, objectType, mat, "OutlineOn", 1f, go);
                }
            }
        }

        private void SwapToVanillaPlusClothes(MaterialEditorCharaController controller, int slot, bool forceSwap)
        {
            var go = controller.ChaControl.objClothes[slot];
            foreach (var renderer in GetRendererList(go))
                foreach (var material in GetMaterials(go, renderer))
                    SwapToVanillaPlus(controller, slot, ObjectType.Clothing, material, go, forceSwap);
        }
        private void SwapToVanillaPlusHair(MaterialEditorCharaController controller, int slot, bool forceSwap)
        {
            var go = controller.ChaControl.objHair[slot];
            foreach (var renderer in GetRendererList(go))
                foreach (var material in GetMaterials(go, renderer))
                    SwapToVanillaPlus(controller, slot, ObjectType.Hair, material, go, forceSwap);
        }
        private void SwapToVanillaPlusAccessory(MaterialEditorCharaController controller, int slot, bool forceSwap)
        {
            var go = controller.ChaControl.GetAccessoryObject(slot);
            if (go != null)
                foreach (var renderer in GetRendererList(go))
                    foreach (var material in GetMaterials(go, renderer))
                        SwapToVanillaPlus(controller, slot, ObjectType.Accessory, material, go, forceSwap);
        }
        private void SwapToVanillaPlusBody(MaterialEditorCharaController controller, bool forceSwap)
        {
            foreach (var renderer in GetRendererList(controller.ChaControl.gameObject))
                foreach (var material in GetMaterials(controller.ChaControl.gameObject, renderer))
                    SwapToVanillaPlus(controller, 0, ObjectType.Character, material, controller.ChaControl.gameObject, forceSwap);
        }

        private void SetTesselationValue(Material mat)
        {
            if (mat == null || !mat.HasProperty("_TessSmooth"))
                return;

            //Adjust the weight of the tesselation
            mat.SetFloat("_TessSmooth", TesselationSlider.Value);
        }

        #region TessMin override

        private readonly struct SetFloatInfo : IEquatable<SetFloatInfo>
        {
            public readonly GameObject GameObject;
            public readonly string MaterialName;
            public readonly string PropertyName;

            public SetFloatInfo(GameObject gameObject, string materialName, string propertyName)
            {
                GameObject = gameObject;
                MaterialName = materialName;
                PropertyName = propertyName;
            }

            public bool Equals(SetFloatInfo other)
            {
                return GameObject == other.GameObject && MaterialName == other.MaterialName && PropertyName == other.PropertyName;
            }

            public override bool Equals(object obj)
            {
                return obj is SetFloatInfo other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = (GameObject != null ? GameObject.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (MaterialName != null ? MaterialName.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (PropertyName != null ? PropertyName.GetHashCode() : 0);
                    return hashCode;
                }
            }
        }

        private static readonly Dictionary<SetFloatInfo, float> _TessShaderList = new Dictionary<SetFloatInfo, float>();
        private static bool _isCapturingScreenshot;

        private static void ScreenshotEvent(float overrideValue)
        {
            if (!TessMinOverrideScreenshots.Value) return;

            _isCapturingScreenshot = true;
            foreach (var setFloatInfo in _TessShaderList.ToList())
            {
                if (setFloatInfo.Key.GameObject == null)
                    _TessShaderList.Remove(setFloatInfo.Key);
                else
                    MaterialAPI.SetFloat(setFloatInfo.Key.GameObject, setFloatInfo.Key.MaterialName, setFloatInfo.Key.PropertyName, overrideValue < 0 ? setFloatInfo.Value : overrideValue);
            }
            _isCapturingScreenshot = false;
        }

        private static void SetFloatHook(GameObject gameObject, string materialName, string propertyName, ref float value)
        {
            if (!_isCapturingScreenshot && propertyName == "TessMin")
            {
                var origValue = value;

                var floatInfo = new SetFloatInfo(gameObject, materialName, propertyName);

                const float valueCutoff = 1.5f;
                const float valueOverride = 1.5f;

                if (TessMinOverride.Value && value > valueCutoff)
                {
                    if (DebugLogging.Value && (!_TessShaderList.TryGetValue(floatInfo, out var storedValue) || storedValue <= valueCutoff))
                        Logger.LogDebug($"Overriding TessMin to {valueOverride} on [{gameObject.name}]");

                    value = valueOverride;
                }

                _TessShaderList[floatInfo] = origValue;
            }
        }

        #endregion
    }
}
