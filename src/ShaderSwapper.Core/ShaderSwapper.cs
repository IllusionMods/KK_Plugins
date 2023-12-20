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

        internal static ConfigEntry<KeyboardShortcut> SwapShadersHotkey { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> ForceSwapShadersHotkey { get; private set; }
        internal static ConfigEntry<float> TesselationSlider { get; private set; }

        private readonly Dictionary<string, string> VanillaPlusShaders = new Dictionary<string, string>
        {
            {"Shader Forge/main_skin", "xukmi/SkinPlus" },
            {"Koikano/main_skin", "xukmi/SkinPlus" },
            {"Shader Forge/main_hair", "xukmi/HairPlus" },
            {"Koikano/hair_main_sun", "xukmi/HairPlus" },
            {"Shader Forge/main_hair_front", "xukmi/HairFrontPlus" },
            {"Koikano/hair_main_sun_front", "xukmi/HairFrontPlus" },
            {"Shader Forge/toon_eye_lod0", "xukmi/EyePlus" },
            {"Koikano/main_eye", "xukmi/EyePlus" },
            {"Shader Forge/toon_eyew_lod0", "xukmi/EyeWPlus" },
            {"Koikano/main_eyew", "xukmi/EyeWPlus" },
            {"Shader Forge/main_opaque", "xukmi/MainOpaquePlus" },
            {"Shader Forge/main_opaque2", "xukmi/MainOpaquePlus" },
            {"Koikano/main_clothes_opaque", "xukmi/MainOpaquePlus" },
            {"Shader Forge/main_alpha", "xukmi/MainAlphaPlus" },
            {"Koikano/main_clothes_alpha", "xukmi/MainAlphaPlus" },
            {"Shader Forge/main_item", "xukmi/MainItemPlus" },
            {"Koikano/main_clothes_item", "xukmi/MainItemPlus" },
            {"Shader Forge/main_item_studio", "xukmi/MainItemPlus" },
            {"Shader Forge/main_item_studio_alpha", "xukmi/MainItemAlphaPlus" },
            {"ShaderForge/main_StandardMDK_studio", "xukmi/MainItemPlus" },
            {"Standard", "xukmi/MainItemPlus" }
        };

        private readonly Dictionary<string, string> VanillaPlusTesselationShaders = new Dictionary<string, string>
        {
            {"Shader Forge/main_skin", "xukmi/SkinPlusTess" },
            {"Koikano/main_skin", "xukmi/SkinPlusTess" },
            {"Shader Forge/main_hair", "xukmi/HairPlus" },
            {"Koikano/hair_main_sun", "xukmi/HairPlus" },
            {"Shader Forge/main_hair_front", "xukmi/HairFrontPlus" },
            {"Koikano/hair_main_sun_front", "xukmi/HairFrontPlus" },
            {"Shader Forge/toon_eye_lod0", "xukmi/EyePlus" },
            {"Koikano/main_eye", "xukmi/EyePlus" },
            {"Shader Forge/toon_eyew_lod0", "xukmi/EyeWPlus" },
            {"Koikano/main_eyew", "xukmi/EyeWPlus" },
            {"Shader Forge/main_opaque", "xukmi/MainOpaquePlusTess" },
            {"Shader Forge/main_opaque2", "xukmi/MainOpaquePlusTess" },
            {"Koikano/main_clothes_opaque", "xukmi/MainOpaquePlusTess" },
            {"Shader Forge/main_alpha", "xukmi/MainAlphaPlusTess" },
            {"Koikano/main_clothes_alpha", "xukmi/MainAlphaPlusTess" },
            {"Shader Forge/main_item", "xukmi/MainItemPlus" },
            {"Koikano/main_clothes_item", "xukmi/MainItemPlus" },
            {"Shader Forge/main_item_studio", "xukmi/MainOpaquePlusTess" },
            {"Shader Forge/main_item_studio_alpha", "xkumi/MainAlphaPlusTess" },
            {"ShaderForge/main_StandardMDK_studio", "xukmi/MainOpaquePlusTess" },
            {"Standard", "xukmi/MainOpaquePlusTess" }
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

        private Dictionary<string, string> VanillaPlusShaderMapping { get => convertShaderMapping(false); set => setShaderMapping(false, value); }

        private Dictionary<string, string> VanillaPlusTessShaderMapping { get => convertShaderMapping(true); set => setShaderMapping(true, value); }

        private Dictionary<string, string> convertShaderMapping(bool tess)
        {
            if (tess) return XElement.Load(TessMapping.Value).Elements().ToDictionary(e => e.Attribute("From").Value, e => e.Attribute("To").Value);
            else return XElement.Load(NormalMapping.Value).Elements().ToDictionary(e => e.Attribute("From").Value, e => e.Attribute("To").Value);
        }
        private void setShaderMapping(bool tess, Dictionary<string, string> value)
        {
            string text = new XElement("ShaderSwapper", value.Select(e => new XElement("Mapping", new XAttribute[] { new XAttribute("From", e.Key), new XAttribute("To", e.Value) }))).ToString();
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
                if (TesselationSlider.Value > 0)
                {
                    if (VanillaPlusTessShaderMapping.TryGetValue(mat.shader.name, out var vanillaPlusTesShaderName))
                    {
                        if (DebugLogging.Value)
                            Logger.LogDebug($"Replacing shader [{mat.shader.name}] with [{vanillaPlusTesShaderName}] on [{(Studio.Studio.Instance.dicObjectCtrl.TryGetValue(id, out var value) ? value.treeNodeObject.textName : null)}]");

                        int renderQueue = mat.renderQueue;
                        controller.SetMaterialShader(id, mat, vanillaPlusTesShaderName);
                        controller.SetMaterialShaderRenderQueue(id, mat, renderQueue);
                        if (mat.shader.name == "xukmi/MainAlphaPlus")
                            controller.SetMaterialFloatProperty(id, mat, "Cutoff", 0.1f);
                        if (oldShader == "Standard")
                        {
                            controller.SetMaterialFloatProperty(id, mat, "Cutoff", 0f);
                        }

                        SetTesselationValue(mat);
                    }
                }
                else
                {
                    if (VanillaPlusShaderMapping.TryGetValue(mat.shader.name, out var vanillaPlusShaderName))
                    {
                        if (DebugLogging.Value)
                            Logger.LogDebug($"Replacing shader [{mat.shader.name}] with [{vanillaPlusShaderName}] on [{(Studio.Studio.Instance.dicObjectCtrl.TryGetValue(id, out var value) ? value.treeNodeObject.textName : null)}]");

                        int renderQueue = mat.renderQueue;
                        controller.SetMaterialShader(id, mat, vanillaPlusShaderName);
                        controller.SetMaterialShaderRenderQueue(id, mat, renderQueue);
                        if (mat.shader.name == "xukmi/MainAlphaPlus")
                            controller.SetMaterialFloatProperty(id, mat, "Cutoff", 0.1f);
                        if (oldShader == "Standard")
                        {
                            controller.SetMaterialFloatProperty(id, mat, "Cutoff", 0f);
                        }
                    }
                }

                if (AutoEnableOutline.Value && mat.HasProperty("_OutlineOn"))
                {
                    controller.SetMaterialFloatProperty(id, mat, "OutlineOn", 1f);
                }
            }
        }

        private void SwapToVanillaPlus(MaterialEditorCharaController controller, int slot, ObjectType objectType, Material mat, GameObject go, bool forceSwap)
        {
            if (controller.GetMaterialShader(slot, ObjectType.Clothing, mat, go) == null || forceSwap)
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

                if (TesselationSlider.Value > 0)
                {
                    if (VanillaPlusTessShaderMapping.TryGetValue(mat.shader.name, out var vanillaPlusTesShaderName))
                    {
                        if (DebugLogging.Value)
                            Logger.LogDebug($"Replacing shader [{mat.shader.name}] with [{vanillaPlusTesShaderName}] on [{controller.ChaControl.fileParam.fullname}]");

                        int renderQueue = mat.renderQueue;
                        controller.SetMaterialShader(slot, objectType, mat, vanillaPlusTesShaderName, go);
                        controller.SetMaterialShaderRenderQueue(slot, objectType, mat, renderQueue, go);
                        if (mat.shader.name == "xukmi/MainAlphaPlus")
                            controller.SetMaterialFloatProperty(slot, objectType, mat, "Cutoff", 0.1f, go);

                        SetTesselationValue(mat);
                    }
                }
                else
                {
                    if (VanillaPlusShaderMapping.TryGetValue(mat.shader.name, out var vanillaPlusShaderName))
                    {
                        if (DebugLogging.Value)
                            Logger.LogDebug($"Replacing shader [{mat.shader.name}] with [{vanillaPlusShaderName}] on [{controller.ChaControl.fileParam.fullname}]");

                        int renderQueue = mat.renderQueue;
                        controller.SetMaterialShader(slot, objectType, mat, vanillaPlusShaderName, go);
                        controller.SetMaterialShaderRenderQueue(slot, objectType, mat, renderQueue, go);
                        if (mat.shader.name == "xukmi/MainAlphaPlus")
                            controller.SetMaterialFloatProperty(slot, objectType, mat, "Cutoff", 0.1f, go);
                    }
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
