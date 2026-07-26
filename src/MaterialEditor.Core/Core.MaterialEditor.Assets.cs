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
        private void LoadNormalMapConverter()
        {
            AssetBundle bundle = AssetBundle.LoadFromMemory(UILib.Resource.LoadEmbeddedResource($"{nameof(KK_Plugins)}.Resources.normal_convert.unity3d"));
            try
            {
                var shader = bundle.LoadAsset<Shader>("normal_convert");
                var shader_opengl = bundle.LoadAsset<Shader>("normal_convert_opengl");
                var unpack_shader = bundle.LoadAsset<Shader>("unpack_normal");
                NormalMapConvertMaterial = new Material(shader);
                NormalMapOpenGLConvertMaterial = new Material(shader_opengl);
                NormalMapUnpackDXT5Material = new Material(unpack_shader);
            }
            finally
            {
                if (bundle != null)
                    bundle.Unload(false);
            }
        }

#if EC || KKS
        protected override void AssetLoadedHook(AssetLoadedContext context)
        {
            if (!ShaderOptimization.Value && !ConfigConvertNormalMaps.Value)
                return;

            if (context.Asset is GameObject go)
            {
                var renderers = go.GetComponentsInChildren<Renderer>();
                for (var i = 0; i < renderers.Length; i++)
                {
                    var renderer = renderers[i];
                    for (var j = 0; j < renderer.materials.Length; j++)
                    {
                        var material = renderer.materials[j];

                        ReplaceShaders(material);
                        ConvertNormalMaps(material);
                    }
                }
                var projectors = go.GetComponentsInChildren<Projector>();
                foreach (var projector in projectors)
                    ReplaceShaders(projector.material);
            }
            else if (context.Asset is Material material)
            {
                ReplaceShaders(material);
                ConvertNormalMaps(material);
            }
            else if (context.Asset is Shader shader)
            {
                if (ShaderOptimization.Value)
                {
                    string shaderName = shader.name;

                    if (LoadedShaders.TryGetValue(shaderName, out var shaderData) && shaderData.Shader != null && shaderData.ShaderOptimization)
                        context.Asset = shaderData.Shader;
                }
            }
        }

        private static void ReplaceShaders(Material material)
        {
            if (!ShaderOptimization.Value)
                return;

            string shaderName = material.shader.name;

            if (LoadedShaders.TryGetValue(shaderName, out var shaderData) && shaderData.Shader != null && shaderData.ShaderOptimization)
            {
                int renderQueue = material.renderQueue;
                material.shader = shaderData.Shader;
                material.renderQueue = renderQueue;
            }
        }

        /// <summary>
        /// Convert normal maps from grey to red for all normal maps on the material
        /// </summary>
        private static void ConvertNormalMaps(Material material)
        {
            if (!ConfigConvertNormalMaps.Value)
                return;

            for (int i = 0; i < NormalMapProperties.Count; i++)
                if (material.HasProperty($"_{NormalMapProperties[i]}"))
                    ConvertNormalMap(material, NormalMapProperties[i]);
        }

        /// <summary>
        /// Convert a normal map texture from grey to red by setting the entire red color channel to white
        /// </summary>
        internal static void ConvertNormalMap(Material material, string propertyName)
        {
            if (!NormalMapProperties.Contains(propertyName))
                return;

            if (material.HasProperty($"_{propertyName}"))
            {
                var tex = material.GetTexture($"_{propertyName}");
                if (tex != null)
                    if (Instance.ConvertNormalMap(ref tex, propertyName))
                        material.SetTexture($"_{propertyName}", tex);
            }
        }
#endif

        protected override Texture ConvertNormalMap(Texture tex, bool unpack = false)
        {
            var material = NormalMapConvertMaterial;
            if (unpack)
            {
                MaterialEditorPluginBase.Logger.LogInfo("Unpacking Normal");
                material = NormalMapUnpackDXT5Material;
            }
            else if (IsUncompressedNormalMap(tex))
                material = NormalMapOpenGLConvertMaterial;
            RenderTexture rt = new RenderTexture(tex.width, tex.height, 0);
            rt.useMipMap = true;
            rt.autoGenerateMips = true;
            Graphics.Blit(tex, rt, material);
            rt.wrapMode = tex.wrapMode;
            rt.anisoLevel = tex.anisoLevel;
            rt.filterMode = tex.filterMode;

            return rt;
        }

    }
}
