using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
#if AI || HS2
using AIChara;
#endif

namespace KK_Plugins.MaterialEditor
{
    /// <summary>
    /// API for safely setting the properties of materials
    /// </summary>
    public static class MaterialAPI
    {
        /// <summary>
        /// List of parts that comprise the body, used to distinguish between clothes, accessories, etc. attached to the body.
        /// </summary>
#if AI || HS2
        public static HashSet<string> BodyParts = new HashSet<string> {
            "o_eyebase_L", "o_eyebase_R", "o_eyelashes", "o_eyeshadow", "o_head", "o_namida", "o_tang", "o_tooth", "o_body_cf", "o_mnpa", "o_mnpb", "cm_o_dan00", "o_tang",
            "cm_o_dan00", "o_tang", "o_silhouette_cf", "o_body_cf", "o_head" };
#else
        public static HashSet<string> BodyParts = new HashSet<string> {
            "cf_O_tooth", "cf_O_canine", "cf_O_tang", "o_tang", "n_tang", "n_tang_silhouette",  "cf_O_eyeline", "cf_O_eyeline_low", "cf_O_mayuge", "cf_Ohitomi_L", "cf_Ohitomi_R",
            "cf_Ohitomi_L02", "cf_Ohitomi_R02", "cf_O_noseline", "cf_O_namida_L", "cf_O_namida_M", "o_dankon", "o_gomu", "o_dan_f", "cf_O_namida_S", "cf_O_gag_eye_00", "cf_O_gag_eye_01",
            "cf_O_gag_eye_02", "o_shadowcaster", "o_shadowcaster_cm", "o_mnpa", "o_mnpb", "n_body_silhouette", "o_body_a", "cf_O_face" };
#endif

        /// <summary>
        /// Get a list of all the renderers. If gameObject is a ChaControl, only gets renderers of the body and face (i.e. not clothes, accessories, etc.)
        /// </summary>
        public static List<Renderer> GetRendererList(GameObject gameObject)
        {
            List<Renderer> rendList = new List<Renderer>();
            if (gameObject == null) return rendList;
            var chaControl = gameObject.GetComponent<ChaControl>();

            if (chaControl != null)
                _GetRendererList(gameObject, rendList);
            else
                rendList = gameObject.GetComponentsInChildren<Renderer>(true).ToList();

            return rendList;
        }
        /// <summary>
        /// Recursively iterates over game objects to create the list. Use GetRendererList intead.
        /// </summary>
        private static void _GetRendererList(GameObject gameObject, List<Renderer> rendList)
        {
            if (gameObject == null) return;

            Renderer rend = gameObject.GetComponent<Renderer>();
            if (rend != null && BodyParts.Contains(rend.NameFormatted()))
                rendList.Add(rend);

            for (int i = 0; i < gameObject.transform.childCount; i++)
                _GetRendererList(gameObject.transform.GetChild(i).gameObject, rendList);
        }

        private static List<Material> GetMaterials(GameObject gameObject, string materialName)
        {
            List<Material> materials = new List<Material>();
            foreach (var renderer in GetRendererList(gameObject))
            {
                //Must use sharedMaterials for ChaControl and materials for other items or bad things happen
                Material[] materialsToSearch;
                if (gameObject.GetComponent<ChaControl>() == null)
                    materialsToSearch = renderer.materials;
                else
                    materialsToSearch = renderer.sharedMaterials;

                foreach (var material in materialsToSearch)
                    if (material.NameFormatted() == materialName)
                        materials.Add(material);
            }
            return materials;
        }

        /// <summary>
        /// Set the value of the specified material property
        /// </summary>
        /// <param name="chaControl">ChaControl to search for the material. Only parts comprising the body and face will be searched, not clothes, accessories, etc.</param>
        /// <param name="materialName">Name of the material being set</param>
        /// <param name="propertyName">Property of the material being set</param>
        /// <param name="value">Value to be set</param>
        /// <returns>True if the material was found and the value set</returns>
        public static bool SetFloat(ChaControl chaControl, string materialName, string propertyName, float value) => SetFloat(chaControl.gameObject, materialName, propertyName, value);
        /// <summary>
        /// Set the value of the specified material property
        /// </summary>
        /// <param name="gameObject">GameObject to search for the material. If this GameObject is a ChaControl, only parts comprising the body and face will be searched, not clothes, accessories, etc.</param>
        /// <param name="materialName">Name of the material being set</param>
        /// <param name="propertyName">Property of the material being set</param>
        /// <param name="value">Value to be set</param>
        /// <returns>True if the material was found and the value set</returns>
        public static bool SetFloat(GameObject gameObject, string materialName, string propertyName, float value)
        {
            bool didSet = false;

            foreach (var material in GetMaterials(gameObject, materialName))
                if (material.HasProperty($"_{propertyName}"))
                {
                    material.SetFloat($"_{propertyName}", value);
                    didSet = true;
                }
            return didSet;
        }

        /// <summary>
        /// Set the value of the specified material property
        /// </summary>
        /// <param name="chaControl">ChaControl to search for the material. Only parts comprising the body and face will be searched, not clothes, accessories, etc.</param>
        /// <param name="materialName">Name of the material being set</param>
        /// <param name="propertyName">Property of the material being set</param>
        /// <param name="value">Value to be set</param>
        /// <returns>True if the material was found and the value set</returns>
        public static bool SetColor(ChaControl chaControl, string materialName, string propertyName, string value) => SetColor(chaControl.gameObject, materialName, propertyName, value.ToColor());
        /// <summary>
        /// Set the value of the specified material property
        /// </summary>
        /// <param name="chaControl">ChaControl to search for the material. Only parts comprising the body and face will be searched, not clothes, accessories, etc.</param>
        /// <param name="materialName">Name of the material being set</param>
        /// <param name="propertyName">Property of the material being set</param>
        /// <param name="value">Value to be set</param>
        /// <returns>True if the material was found and the value set</returns>
        public static bool SetColor(ChaControl chaControl, string materialName, string propertyName, Color value) => SetColor(chaControl.gameObject, materialName, propertyName, value);
        /// <summary>
        /// Set the value of the specified material property
        /// </summary>
        /// <param name="gameObject">GameObject to search for the material</param>
        /// <param name="materialName">Name of the material being set</param>
        /// <param name="propertyName">Property of the material being set</param>
        /// <param name="value">Value to be set</param>
        /// <returns>True if the material was found and the value set</returns>
        public static bool SetColor(GameObject gameObject, string materialName, string propertyName, string value) => SetColor(gameObject, materialName, propertyName, value.ToColor());
        /// <summary>
        /// Set the value of the specified material property
        /// </summary>
        /// <param name="gameObject">GameObject to search for the material</param>
        /// <param name="materialName">Name of the material being set</param>
        /// <param name="propertyName">Property of the material being set</param>
        /// <param name="value">Value to be set</param>
        /// <returns>True if the material was found and the value set</returns>
        public static bool SetColor(GameObject gameObject, string materialName, string propertyName, Color value)
        {
            bool didSet = false;

            foreach (var material in GetMaterials(gameObject, materialName))
                if (material.HasProperty($"_{propertyName}"))
                {
                    material.SetColor($"_{propertyName}", value);
                    didSet = true;
                }
            return didSet;
        }

        /// <summary>
        /// Set the value of the specified renderer property
        /// </summary>
        /// <param name="chaControl">ChaControl to search for the renderer. Only parts comprising the body and face will be searched, not clothes, accessories, etc.</param>
        /// <param name="rendererName">Name of the renderer being modified</param>
        /// <param name="propertyName">Property of the renderer being modified</param>
        /// <param name="value">Value to be set</param>
        /// <returns>True if the value was set, false if it could not be set</returns>
        public static bool SetRendererProperty(ChaControl chaControl, string rendererName, RendererProperties propertyName, string value) => SetRendererProperty(chaControl.gameObject, rendererName, propertyName, int.Parse(value));
        /// <summary>
        /// Set the value of the specified renderer property
        /// </summary>
        /// <param name="chaControl">ChaControl to search for the renderer. Only parts comprising the body and face will be searched, not clothes, accessories, etc.</param>
        /// <param name="rendererName">Name of the renderer being modified</param>
        /// <param name="propertyName">Property of the renderer being modified</param>
        /// <param name="value">Value to be set</param>
        /// <returns>True if the value was set, false if it could not be set</returns>
        public static bool SetRendererProperty(ChaControl chaControl, string rendererName, RendererProperties propertyName, int value) => SetRendererProperty(chaControl.gameObject, rendererName, propertyName, value);
        /// <summary>
        /// Set the value of the specified renderer property
        /// </summary>
        /// <param name="gameObject">GameObject to search for the renderer</param>
        /// <param name="rendererName">Name of the renderer being modified</param>
        /// <param name="propertyName">Property of the renderer being modified</param>
        /// <param name="value">Value to be set</param>
        /// <returns>True if the value was set, false if it could not be set</returns>
        public static bool SetRendererProperty(GameObject gameObject, string rendererName, RendererProperties propertyName, string value) => SetRendererProperty(gameObject, rendererName, propertyName, int.Parse(value));
        /// <summary>
        /// Set the value of the specified renderer property
        /// </summary>
        /// <param name="gameObject">GameObject to search for the renderer</param>
        /// <param name="rendererName">Name of the renderer being modified</param>
        /// <param name="propertyName">Property of the renderer being modified</param>
        /// <param name="value">Value to be set</param>
        /// <returns>True if the value was set, false if it could not be set</returns>
        public static bool SetRendererProperty(GameObject gameObject, string rendererName, RendererProperties propertyName, int value)
        {
            if (propertyName == RendererProperties.Enabled)
                return SetRendererEnabled(gameObject, rendererName, value == 1);
            if (propertyName == RendererProperties.ShadowCastingMode)
                return SetRendererShadowCastingMode(gameObject, rendererName, (UnityEngine.Rendering.ShadowCastingMode)value);
            if (propertyName == RendererProperties.ReceiveShadows)
                return SetRendererReceiveShadows(gameObject, rendererName, value == 1);
            return false;
        }

        /// <summary>
        /// Set a renderer enabled or disabled
        /// </summary>
        /// <param name="chaControl">ChaControl to search for the renderer. Only parts comprising the body and face will be searched, not clothes, accessories, etc.</param>
        /// <param name="rendererName">Name of the renderer being modified</param>
        /// <param name="value">Value to be set</param>
        /// <returns>True if the value was set, false if it could not be set</returns>
        public static bool SetRendererEnabled(ChaControl chaControl, string rendererName, bool value) => SetRendererEnabled(chaControl.gameObject, rendererName, value);
        /// <summary>
        /// Set a renderer enabled or disabled
        /// </summary>
        /// <param name="gameObject">GameObject to search for the renderer</param>
        /// <param name="rendererName">Name of the renderer being modified</param>
        /// <param name="value">Value to be set</param>
        /// <returns>True if the value was set, false if it could not be set</returns>
        public static bool SetRendererEnabled(GameObject gameObject, string rendererName, bool value)
        {
            bool didSet = false;
            foreach (var rend in GetRendererList(gameObject))
                if (rend.NameFormatted() == rendererName)
                {
                    rend.enabled = value;
                    didSet = true;
                }
            return didSet;
        }

        /// <summary>
        /// Set the ShadowCastingMode of a renderer
        /// </summary>
        /// <param name="chaControl">ChaControl to search for the renderer. Only parts comprising the body and face will be searched, not clothes, accessories, etc.</param>
        /// <param name="rendererName">Name of the renderer being modified</param>
        /// <param name="value">Value to be set</param>
        /// <returns>True if the value was set, false if it could not be set</returns>
        public static bool SetRendererShadowCastingMode(ChaControl chaControl, string rendererName, UnityEngine.Rendering.ShadowCastingMode value) => SetRendererShadowCastingMode(chaControl.gameObject, rendererName, value);
        /// <summary>
        /// Set the ShadowCastingMode of a renderer
        /// </summary>
        /// <param name="gameObject">GameObject to search for the renderer</param>
        /// <param name="rendererName">Name of the renderer being modified</param>
        /// <param name="value">Value to be set</param>
        /// <returns>True if the value was set, false if it could not be set</returns>
        public static bool SetRendererShadowCastingMode(GameObject gameObject, string rendererName, UnityEngine.Rendering.ShadowCastingMode value)
        {
            bool didSet = false;
            foreach (var rend in GetRendererList(gameObject))
                if (rend.NameFormatted() == rendererName)
                {
                    rend.shadowCastingMode = value;
                    didSet = true;
                }
            return didSet;
        }

        /// <summary>
        /// Set the ReceiveShadows property of a renderer
        /// </summary>
        /// <param name="chaControl">ChaControl to search for the renderer. Only parts comprising the body and face will be searched, not clothes, accessories, etc.</param>
        /// <param name="rendererName">Name of the renderer being modified</param>
        /// <param name="value">Value to be set</param>
        /// <returns>True if the value was set, false if it could not be set</returns>
        public static bool SetRendererReceiveShadows(ChaControl chaControl, string rendererName, bool value) => SetRendererReceiveShadows(chaControl.gameObject, rendererName, value);
        /// <summary>
        /// Set the ReceiveShadows property of a renderer
        /// </summary>
        /// <param name="gameObject">GameObject to search for the renderer</param>
        /// <param name="rendererName">Name of the renderer being modified</param>
        /// <param name="value">Value to be set</param>
        /// <returns>True if the value was set, false if it could not be set</returns>
        public static bool SetRendererReceiveShadows(GameObject gameObject, string rendererName, bool value)
        {
            bool didSet = false;
            foreach (var rend in GetRendererList(gameObject))
                if (rend.NameFormatted() == rendererName)
                {
                    rend.receiveShadows = value;
                    didSet = true;
                }
            return didSet;
        }

        /// <summary>
        /// Set the texture property of a material
        /// </summary>
        /// <param name="chaControl">ChaControl to search for the material. Only parts comprising the body and face will be searched, not clothes, accessories, etc.</param>
        /// <param name="materialName">Name of the material being modified</param>
        /// <param name="propertyName">Property of the material being set</param>
        /// <param name="value">Value to be set</param>
        /// <returns>True if the value was set, false if it could not be set</returns>
        public static bool SetTexture(ChaControl chaControl, string materialName, string propertyName, Texture2D value) => SetTexture(chaControl.gameObject, materialName, propertyName, value);
        /// <summary>
        /// Set the texture property of a material
        /// </summary>
        /// <param name="gameObject">GameObject to search for the renderer</param>
        /// <param name="materialName">Name of the material being modified</param>
        /// <param name="propertyName">Property of the material being set</param>
        /// <param name="value">Value to be set</param>
        /// <returns>True if the value was set, false if it could not be set</returns>
        public static bool SetTexture(GameObject gameObject, string materialName, string propertyName, Texture2D value)
        {
            bool didSet = false;
            foreach (var material in GetMaterials(gameObject, materialName))
                if (material.HasProperty($"_{propertyName}"))
                {
                    var wrapMode = material.GetTexture($"_{propertyName}")?.wrapMode;
                    if (wrapMode != null)
                        value.wrapMode = (TextureWrapMode)wrapMode;
                    material.SetTexture($"_{propertyName}", value);
                    didSet = true;
                }
            return didSet;
        }

        /// <summary>
        /// Set the texture offset property of a material
        /// </summary>
        /// <param name="chaControl">ChaControl to search for the material. Only parts comprising the body and face will be searched, not clothes, accessories, etc.</param>
        /// <param name="materialName">Name of the material being modified</param>
        /// <param name="propertyName">Property of the material being set</param>
        /// <param name="value">Value to be set</param>
        /// <returns>True if the value was set, false if it could not be set</returns>
        public static bool SetTextureOffset(ChaControl chaControl, string materialName, string propertyName, Vector2? value) => value == null ? false : SetTextureOffset(chaControl.gameObject, materialName, propertyName, (Vector2)value);
        /// <summary>
        /// Set the texture offset property of a material
        /// </summary>
        /// <param name="gameObject">GameObject to search for the renderer</param>
        /// <param name="materialName">Name of the material being modified</param>
        /// <param name="propertyName">Property of the material being set</param>
        /// <param name="value">Value to be set</param>
        /// <returns>True if the value was set, false if it could not be set</returns>
        public static bool SetTextureOffset(GameObject gameObject, string materialName, string propertyName, Vector2? value)
        {
            if (value == null) return false;
            bool didSet = false;

            foreach (var material in GetMaterials(gameObject, materialName))
                if (material.HasProperty($"_{propertyName}"))
                {
                    material.SetTextureOffset($"_{propertyName}", (Vector2)value);
                    didSet = true;
                }
            return didSet;
        }

        /// <summary>
        /// Set the texture scale property of a material
        /// </summary>
        /// <param name="chaControl">ChaControl to search for the material. Only parts comprising the body and face will be searched, not clothes, accessories, etc.</param>
        /// <param name="materialName">Name of the material being modified</param>
        /// <param name="propertyName">Property of the material being set</param>
        /// <param name="value">Value to be set</param>
        /// <returns>True if the value was set, false if it could not be set</returns>
        public static bool SetTextureScale(ChaControl chaControl, string materialName, string propertyName, Vector2? value) => value == null ? false : SetTextureScale(chaControl.gameObject, materialName, propertyName, (Vector2)value);
        /// <summary>
        /// Set the texture scale property of a material
        /// </summary>
        /// <param name="gameObject">GameObject to search for the renderer</param>
        /// <param name="materialName">Name of the material being modified</param>
        /// <param name="propertyName">Property of the material being set</param>
        /// <param name="value">Value to be set</param>
        /// <returns>True if the value was set, false if it could not be set</returns>
        public static bool SetTextureScale(GameObject gameObject, string materialName, string propertyName, Vector2? value)
        {
            if (value == null) return false;
            bool didSet = false;

            foreach (var material in GetMaterials(gameObject, materialName))
                if (material.HasProperty($"_{propertyName}"))
                {
                    material.SetTextureScale($"_{propertyName}", (Vector2)value);
                    didSet = true;
                }
            return didSet;
        }

        /// <summary>
        /// Set the shader of a material. Can only be set to a shader that has been loaded by MaterialEditor
        /// </summary>
        /// <param name="chaControl">ChaControl to search for the material. Only parts comprising the body and face will be searched, not clothes, accessories, etc.</param>
        /// <param name="materialName">Name of the material being modified</param>
        /// <param name="shaderName">Name of the shader to be set</param>
        /// <returns>True if the value was set, false if it could not be set</returns>
        public static bool SetShader(ChaControl chaControl, string materialName, string shaderName) => SetShader(chaControl.gameObject, materialName, shaderName);
        /// <summary>
        /// Set the shader of a material. Can only be set to a shader that has been loaded by MaterialEditor
        /// </summary>
        /// <param name="gameObject">GameObject to search for the renderer</param>
        /// <param name="materialName">Name of the material being modified</param>
        /// <param name="shaderName">Name of the shader to be set</param>
        /// <returns>True if the value was set, false if it could not be set</returns>
        public static bool SetShader(GameObject gameObject, string materialName, string shaderName)
        {
            bool didSet = false;
            if (shaderName.IsNullOrEmpty()) return false;
            MaterialEditorPlugin.LoadedShaders.TryGetValue(shaderName, out var shaderData);

            if (shaderData.Shader == null)
            {
                MaterialEditorPlugin.Logger.Log(BepInEx.Logging.LogLevel.Warning | BepInEx.Logging.LogLevel.Message, $"[{MaterialEditorPlugin.PluginNameInternal}] Could not load shader:{shaderName}");
                return false;
            }
            if (!MaterialEditorPlugin.XMLShaderProperties.TryGetValue(shaderName, out var shaderPropertyDataList))
                shaderPropertyDataList = new Dictionary<string, MaterialEditorPlugin.ShaderPropertyData>();

            foreach (var material in GetMaterials(gameObject, materialName))
            {
                material.shader = shaderData.Shader;

                if (shaderData.RenderQueue != null)
                    material.renderQueue = (int)shaderData.RenderQueue;

                foreach (var shaderPropertyData in shaderPropertyDataList.Values)
                    if (!shaderPropertyData.DefaultValue.IsNullOrEmpty())
                    {
                        switch (shaderPropertyData.Type)
                        {
                            case ShaderPropertyType.Float:
                                SetFloat(gameObject, materialName, shaderPropertyData.Name, float.Parse(shaderPropertyData.DefaultValue));
                                break;
                            case ShaderPropertyType.Color:
                                SetColor(gameObject, materialName, shaderPropertyData.Name, shaderPropertyData.DefaultValue);
                                break;
                            case ShaderPropertyType.Texture:
                                if (shaderPropertyData.DefaultValue.IsNullOrEmpty()) continue;
                                try
                                {
                                    var tex = CommonLib.LoadAsset<Texture2D>(shaderPropertyData.DefaultValueAssetBundle, shaderPropertyData.DefaultValue);
                                    SetTexture(gameObject, materialName, shaderPropertyData.Name, tex);
                                }
                                catch
                                {
                                    MaterialEditorPlugin.Logger.LogWarning($"[{MaterialEditorPlugin.PluginNameInternal}] Could not load default texture:{shaderPropertyData.DefaultValueAssetBundle}:{shaderPropertyData.DefaultValue}");
                                }
                                break;
                        }
                    }
                didSet = true;
            }

            return didSet;
        }

        /// <summary>
        /// Set the render queue of a material
        /// </summary>
        /// <param name="chaControl">ChaControl to search for the material. Only parts comprising the body and face will be searched, not clothes, accessories, etc.</param>
        /// <param name="materialName">Name of the material being modified</param>
        /// <param name="value">Value to be set</param>
        /// <returns>True if the value was set, false if it could not be set</returns>
        public static bool SetRenderQueue(ChaControl chaControl, string materialName, int? value) => SetRenderQueue(chaControl.gameObject, materialName, value);
        /// <summary>
        /// Set the render queue of a material
        /// </summary>
        /// <param name="gameObject">GameObject to search for the renderer</param>
        /// <param name="materialName">Name of the material being modified</param>
        /// <param name="value">Value to be set</param>
        /// <returns>True if the value was set, false if it could not be set</returns>
        public static bool SetRenderQueue(GameObject gameObject, string materialName, int? value)
        {
            bool didSet = false;
            if (value == null) return false;

            foreach (var material in GetMaterials(gameObject, materialName))
            {
                material.renderQueue = (int)value;
                didSet = true;
            }
            return didSet;
        }

        /// <summary>
        /// Type of the shader property
        /// </summary>
        public enum ShaderPropertyType
        {
            /// <summary>
            /// Texture
            /// </summary>
            Texture,
            /// <summary>
            /// Color, Vector4, Vector3, Vector2
            /// </summary>
            Color,
            /// <summary>
            /// Float, Int, Bool
            /// </summary>
            Float
        }
        /// <summary>
        /// Properties of a renderer that can be set
        /// </summary>
        public enum RendererProperties
        {
            /// <summary>
            /// Whether the renderer is enabled
            /// </summary>
            Enabled,
            /// <summary>
            /// ShadowCastingMode of the renderer
            /// </summary>
            ShadowCastingMode,
            /// <summary>
            /// Whether the renderer will recieve shadows cast by other objects
            /// </summary>
            ReceiveShadows
        }
    }
}
