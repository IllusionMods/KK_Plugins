﻿using System;
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

namespace KK_Plugins
{
    [BepInDependency(MaterialEditorPlugin.PluginGUID, MaterialEditorPlugin.PluginVersion)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class ShaderSwapper : BaseUnityPlugin
    {
        public const string PluginGUID = "com.deathweasel.bepinex.shaderswapper";
        public const string PluginName = "Shader Swapper";
        public const string PluginNameInternal = Constants.Prefix + "_ShaderSwapper";
        public const string PluginVersion = "1.3";
        internal static new ManualLogSource Logger;
        private static ShaderSwapper Instance;

        internal static ConfigEntry<KeyboardShortcut> SwapShadersHotkey { get; private set; }
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
        };

        internal static ConfigEntry<bool> AutoReplace { get; private set; }
        internal static ConfigEntry<bool> DebugLogging { get; private set; }

        private readonly Harmony _harmony = new Harmony(PluginGUID);

        private void Awake()
        {
            Logger = base.Logger;
            Instance = this;

            SwapShadersHotkey = Config.Bind("Keyboard Shortcuts", "Swap Shaders", new KeyboardShortcut(KeyCode.P, KeyCode.RightControl), "Swap all shaders to the equivalent Vanilla+ shader.");

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
            void ApplyPatches(bool enable)
            {
                if (enable)
                    _harmony.Patch(AccessTools.Method(typeof(MaterialEditorCharaController), "LoadCharacterExtSaveData"), postfix: new HarmonyMethod(typeof(ShaderSwapper), nameof(ShaderSwapper.LoadHook)));
                else
                    _harmony.UnpatchSelf();
            }
            AutoReplace.SettingChanged += (sender, args) => ApplyPatches(AutoReplace.Value);
            if (AutoReplace.Value) ApplyPatches(true);
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
                    var ociChars = StudioAPI.GetSelectedCharacters();
                    foreach (var ociChar in ociChars)
                        UpdateCharShaders(ociChar.GetChaControl());
                }
            }
        }

        public void UpdateCharShaders(ChaControl chaControl)
        {
            var controller = GetController(chaControl);
            for (var i = 0; i < controller.ChaControl.objClothes.Length; i++)
                SwapToVanillaPlusClothes(controller, i);
            for (var i = 0; i < controller.ChaControl.objHair.Length; i++)
                SwapToVanillaPlusHair(controller, i);
            for (var i = 0; i < controller.ChaControl.GetAccessoryObjects().Length; i++)
                SwapToVanillaPlusAccessory(controller, i);
            SwapToVanillaPlusBody(controller);
        }

        public static MaterialEditorCharaController GetController(ChaControl chaControl)
        {
            if (chaControl == null || chaControl.gameObject == null)
                return null;
            return chaControl.gameObject.GetComponent<MaterialEditorCharaController>();
        }

        private void SwapToVanillaPlus(MaterialEditorCharaController controller, int slot, ObjectType objectType, Material mat, GameObject go)
        {
            if (controller.GetMaterialShader(slot, ObjectType.Clothing, mat, go) == null)
            {
                if (TesselationSlider.Value > 0)
                {
                    if (VanillaPlusTesselationShaders.TryGetValue(mat.shader.name, out var vanillaPlusTesShaderName))
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
                    if (VanillaPlusShaders.TryGetValue(mat.shader.name, out var vanillaPlusShaderName))
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
            }
        }

        private void SwapToVanillaPlusClothes(MaterialEditorCharaController controller, int slot)
        {
            var go = controller.ChaControl.objClothes[slot];
            foreach (var renderer in GetRendererList(go))
                foreach (var material in GetMaterials(go, renderer))
                    SwapToVanillaPlus(controller, slot, ObjectType.Clothing, material, go);
        }
        private void SwapToVanillaPlusHair(MaterialEditorCharaController controller, int slot)
        {
            var go = controller.ChaControl.objHair[slot];
            foreach (var renderer in GetRendererList(go))
                foreach (var material in GetMaterials(go, renderer))
                    SwapToVanillaPlus(controller, slot, ObjectType.Hair, material, go);
        }
        private void SwapToVanillaPlusAccessory(MaterialEditorCharaController controller, int slot)
        {
            var go = controller.ChaControl.GetAccessoryObject(slot);
            if (go != null)
                foreach (var renderer in GetRendererList(go))
                    foreach (var material in GetMaterials(go, renderer))
                        SwapToVanillaPlus(controller, slot, ObjectType.Accessory, material, go);
        }
        private void SwapToVanillaPlusBody(MaterialEditorCharaController controller)
        {
            foreach (var renderer in GetRendererList(controller.ChaControl.gameObject))
                foreach (var material in GetMaterials(controller.ChaControl.gameObject, renderer))
                    SwapToVanillaPlus(controller, 0, ObjectType.Character, material, controller.ChaControl.gameObject);
        }

        private void SetTesselationValue(Material mat)
        {
            if (mat == null || !mat.HasProperty("_TessSmooth"))
                return;

            //Adjust the weight of the tesselation
            mat.SetFloat("_TessSmooth", TesselationSlider.Value);
        }
    }
}
