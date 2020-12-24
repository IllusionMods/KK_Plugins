using System.Collections.Generic;
using UnityEngine;

namespace MaterialEditorAPI
{
    /// <summary>
    /// Class containing material data, used to for copy and paste of material edits
    /// </summary>
    public class CopyContainer
    {
        /// <summary>
        /// List of float property edits
        /// </summary>
        public List<MaterialFloatProperty> MaterialFloatPropertyList = new List<MaterialFloatProperty>();
        /// <summary>
        /// List of color property edits
        /// </summary>
        public List<MaterialColorProperty> MaterialColorPropertyList = new List<MaterialColorProperty>();
        /// <summary>
        /// List of texture property edits
        /// </summary>
        public List<MaterialTextureProperty> MaterialTexturePropertyList = new List<MaterialTextureProperty>();
        /// <summary>
        /// List of shader edits
        /// </summary>
        public List<MaterialShader> MaterialShaderList = new List<MaterialShader>();

        /// <summary>
        /// Whether there are any copied edits
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                if (MaterialFloatPropertyList.Count == 0 && MaterialColorPropertyList.Count == 0 && MaterialTexturePropertyList.Count == 0 && MaterialShaderList.Count == 0)
                    return true;
                return false;
            }
        }

        /// <summary>
        /// Clear any copied edits
        /// </summary>
        public void ClearAll()
        {
            MaterialFloatPropertyList = new List<MaterialFloatProperty>();
            MaterialColorPropertyList = new List<MaterialColorProperty>();
            MaterialTexturePropertyList = new List<MaterialTextureProperty>();
            MaterialShaderList = new List<MaterialShader>();
        }

        /// <summary>
        /// Data storage class for float properties
        /// </summary>
        public class MaterialFloatProperty
        {
            /// <summary>
            /// Name of the property
            /// </summary>
            public string Property;
            /// <summary>
            /// Value
            /// </summary>
            public float Value;

            /// <summary>
            /// Data storage class for float properties
            /// </summary>
            /// <param name="property">Name of the property</param>
            /// <param name="value">Value</param>
            public MaterialFloatProperty(string property, float value)
            {
                Property = property;
                Value = value;
            }
        }

        /// <summary>
        /// Data storage class for color properties
        /// </summary>
        public class MaterialColorProperty
        {
            /// <summary>
            /// Name of the property
            /// </summary>
            public string Property;
            /// <summary>
            /// Value
            /// </summary>
            public Color Value;

            /// <summary>
            /// Data storage class for color properties
            /// </summary>
            /// <param name="property">Name of the property</param>
            /// <param name="value">Value</param>
            public MaterialColorProperty(string property, Color value)
            {
                Property = property;
                Value = value;
            }
        }

        /// <summary>
        /// Data storage class for texture properties
        /// </summary>
        public class MaterialTextureProperty
        {
            /// <summary>
            /// Name of the property
            /// </summary>
            public string Property;
            /// <summary>
            /// ID of the texture as stored in the texture dictionary
            /// </summary>
            public byte[] Data;
            /// <summary>
            /// Texture offset value
            /// </summary>
            public Vector2? Offset;
            /// <summary>
            /// Texture scale value
            /// </summary>
            public Vector2? Scale;

            /// <summary>
            /// Data storage class for texture properties
            /// </summary>
            /// <param name="property">Name of the property</param>
            /// <param name="data">Byte array containing the texture</param>
            /// <param name="offset">Texture offset value</param>
            /// <param name="scale">Texture scale value</param>
            public MaterialTextureProperty(string property, byte[] data = null, Vector2? offset = null, Vector2? scale = null)
            {
                Property = property;
                Data = data;
                Offset = offset;
                Scale = scale;
            }
        }

        /// <summary>
        /// Data storage class for shader data
        /// </summary>
        public class MaterialShader
        {
            /// <summary>
            /// Name of the shader
            /// </summary>
            public string ShaderName;
            /// <summary>
            /// Render queue
            /// </summary>
            public int? RenderQueue;

            /// <summary>
            /// Data storage class for shader data
            /// </summary>
            /// <param name="shaderName">Name of the shader</param>
            /// <param name="renderQueue">Render queue</param>
            public MaterialShader(string shaderName, int? renderQueue)
            {
                ShaderName = shaderName;
                RenderQueue = renderQueue;
            }
            /// <summary>
            /// Data storage class for shader data
            /// </summary>
            /// <param name="shaderName">Name of the shader</param>
            public MaterialShader(string shaderName)
            {
                ShaderName = shaderName;
            }
            /// <summary>
            /// Data storage class for shader data
            /// </summary>
            /// <param name="renderQueue">Render queue</param>
            public MaterialShader(int? renderQueue)
            {
                RenderQueue = renderQueue;
            }

            /// <summary>
            /// Check if the shader name and render queue are both null. Safe to delete this data if true.
            /// </summary>
            /// <returns></returns>
            public bool NullCheck() => ShaderName.IsNullOrEmpty() && RenderQueue == null;
        }
    }
}
