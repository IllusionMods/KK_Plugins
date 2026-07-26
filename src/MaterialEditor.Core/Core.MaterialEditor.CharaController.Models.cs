using MaterialEditorAPI;
using MessagePack;
using System;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;
using KKAPI.Chara;
namespace KK_Plugins.MaterialEditor
{
    public partial class MaterialEditorCharaController : CharaCustomFunctionController
    {
        /// <summary>
        /// Data storage class for renderer properties
        /// </summary>
        [Serializable]
        [MessagePackObject]
        public class RendererProperty
        {
            /// <summary>
            /// Type of the object
            /// </summary>
            [Key("ObjectType")]
            public ObjectType ObjectType;
            /// <summary>
            /// Coordinate index, always 0 except in Koikatsu
            /// </summary>
            [Key("CoordinateIndex")]
            public int CoordinateIndex;
            /// <summary>
            /// Slot of the accessory, hair, or clothing
            /// </summary>
            [Key("Slot")]
            public int Slot;
            /// <summary>
            /// Name of the renderer
            /// </summary>
            [Key("RendererName")]
            public string RendererName;
            /// <summary>
            /// Property type
            /// </summary>
            [Key("Property")]
            public RendererProperties Property;
            /// <summary>
            /// Value
            /// </summary>
            [Key("Value")]
            public string Value;
            /// <summary>
            /// Original value
            /// </summary>
            [Key("ValueOriginal")]
            public string ValueOriginal;

            /// <summary>
            /// Data storage class for renderer properties
            /// </summary>
            /// <param name="objectType">Type of the object</param>
            /// <param name="coordinateIndex">Coordinate index, always 0 except in Koikatsu</param>
            /// <param name="slot">Slot of the accessory, hair, or clothing</param>
            /// <param name="rendererName">Name of the renderer</param>
            /// <param name="property">Property type</param>
            /// <param name="value">Value</param>
            /// <param name="valueOriginal">Original</param>
            public RendererProperty(ObjectType objectType, int coordinateIndex, int slot, string rendererName, RendererProperties property, string value, string valueOriginal)
            {
                ObjectType = objectType;
                CoordinateIndex = coordinateIndex;
                Slot = slot;
                RendererName = rendererName.FormatShadingObjectName();
                Property = property;
                Value = value;
                ValueOriginal = valueOriginal;
            }
        }

        /// <summary>
        /// Data storage class for material renaming
        /// </summary>
        [Serializable]
        [MessagePackObject]
        public class MaterialNameProperty
        {
            /// <summary>
            /// Type of the object
            /// </summary>
            [Key("ObjectType")]
            public ObjectType ObjectType;
            /// <summary>
            /// Coordinate index, always 0 except in Koikatsu
            /// </summary>
            [Key("CoordinateIndex")]
            public int CoordinateIndex;
            /// <summary>
            /// Slot of the accessory, hair, or clothing
            /// </summary>
            [Key("Slot")]
            public int Slot;
            /// <summary>
            /// Name of the renderer
            /// </summary>
            [Key("Renderer")]
            public string Renderer;
            /// <summary>
            /// Name of the material
            /// </summary>
            [Key("MaterialName")]
            public string MaterialName;
            /// <summary>
            /// Value
            /// </summary>
            [Key("Value")]
            public string Value;
            /// <summary>
            /// Original value
            /// </summary>
            [Key("ValueOriginal")]
            public string ValueOriginal;

            /// <summary>
            /// Data storage class for name properties
            /// </summary>
            /// <param name="objectType">Type of the object</param>
            /// <param name="coordinateIndex">Coordinate index, always 0 except in Koikatsu</param>
            /// <param name="slot">Slot of the accessory, hair, or clothing</param>
            /// <param name="renderer">Renderer being modified</param>
            /// <param name="material">Material being renamed</param>
            /// <param name="value">New name for the material</param>
            public MaterialNameProperty(ObjectType objectType, int coordinateIndex, int slot, Renderer renderer, Material material, string value)
            {
                ObjectType = objectType;
                CoordinateIndex = coordinateIndex;
                Slot = slot;
                Renderer = renderer.NameFormatted();
                MaterialName = material.name;
                Value = value;
                ValueOriginal = material.name;
            }
            /// <summary>
            /// Data storage class for name properties
            /// </summary>
            /// <param name="objectType">Type of the object</param>
            /// <param name="coordinateIndex">Coordinate index, always 0 except in Koikatsu</param>
            /// <param name="slot">Slot of the accessory, hair, or clothing</param>
            /// <param name="renderer">NameFormatted() name of the Renderer being modified</param>
            /// <param name="materialName">Raw, unmodified name of the Material being renamed</param>
            /// <param name="value">New name for the material</param>
            [SerializationConstructor]
            public MaterialNameProperty(ObjectType objectType, int coordinateIndex, int slot, string renderer, string materialName, string value)
            {
                ObjectType = objectType;
                CoordinateIndex = coordinateIndex;
                Slot = slot;
                Renderer = renderer;
                MaterialName = materialName;
                Value = value;
                ValueOriginal = materialName;
            }
        }

        /// <summary>
        /// Data storage class for keyword properties
        /// </summary>
        [Serializable]
        [MessagePackObject]

        public class MaterialKeywordProperty
        {
            /// <summary>
            /// Type of the object
            /// </summary>
            [Key("ObjectType")]
            public ObjectType ObjectType;
            /// <summary>
            /// Coordinate index, always 0 except in Koikatsu
            /// </summary>
            [Key("CoordinateIndex")]
            public int CoordinateIndex;
            /// <summary>
            /// Slot of the accessory, hair, or clothing
            /// </summary>
            [Key("Slot")]
            public int Slot;
            /// <summary>
            /// Name of the material
            /// </summary>
            [Key("MaterialName")]
            public string MaterialName;
            /// <summary>
            /// Name of the property
            /// </summary>
            [Key("Property")]
            public string Property;
            /// <summary>
            /// Value
            /// </summary>
            [Key("Value")]
            public bool Value;
            /// <summary>
            /// Original value
            /// </summary>
            [Key("ValueOriginal")]
            public bool ValueOriginal;

            /// <summary>
            /// Data storage class for keyword properties
            /// </summary>
            /// <param name="objectType">Type of the object</param>
            /// <param name="coordinateIndex">Coordinate index, always 0 except in Koikatsu</param>
            /// <param name="slot">Slot of the accessory, hair, or clothing</param>
            /// <param name="materialName">Name of the material</param>
            /// <param name="property">Name of the property</param>
            /// <param name="value">Value</param>
            /// <param name="valueOriginal">Original value</param>
            public MaterialKeywordProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string property, bool value, bool valueOriginal)
            {
                ObjectType = objectType;
                CoordinateIndex = coordinateIndex;
                Slot = slot;
                MaterialName = materialName.FormatShadingObjectName();
                Property = property;
                Value = value;
                ValueOriginal = valueOriginal;
            }
        }

        /// <summary>
        /// Data storage class for float properties
        /// </summary>
        [Serializable]
        [MessagePackObject]
        public class MaterialFloatProperty
        {
            /// <summary>
            /// Type of the object
            /// </summary>
            [Key("ObjectType")]
            public ObjectType ObjectType;
            /// <summary>
            /// Coordinate index, always 0 except in Koikatsu
            /// </summary>
            [Key("CoordinateIndex")]
            public int CoordinateIndex;
            /// <summary>
            /// Slot of the accessory, hair, or clothing
            /// </summary>
            [Key("Slot")]
            public int Slot;
            /// <summary>
            /// Name of the material
            /// </summary>
            [Key("MaterialName")]
            public string MaterialName;
            /// <summary>
            /// Name of the property
            /// </summary>
            [Key("Property")]
            public string Property;
            /// <summary>
            /// Value
            /// </summary>
            [Key("Value")]
            public string Value;
            /// <summary>
            /// Original value
            /// </summary>
            [Key("ValueOriginal")]
            public string ValueOriginal;

            /// <summary>
            /// Data storage class for float properties
            /// </summary>
            /// <param name="objectType">Type of the object</param>
            /// <param name="coordinateIndex">Coordinate index, always 0 except in Koikatsu</param>
            /// <param name="slot">Slot of the accessory, hair, or clothing</param>
            /// <param name="materialName">Name of the material</param>
            /// <param name="property">Name of the property</param>
            /// <param name="value">Value</param>
            /// <param name="valueOriginal">Original value</param>
            public MaterialFloatProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string property, string value, string valueOriginal)
            {
                ObjectType = objectType;
                CoordinateIndex = coordinateIndex;
                Slot = slot;
                MaterialName = materialName.FormatShadingObjectName();
                Property = property;
                Value = value;
                ValueOriginal = valueOriginal;
            }
        }

        /// <summary>
        /// Data storage class for color properties
        /// </summary>
        [Serializable]
        [MessagePackObject]
        public class MaterialColorProperty
        {
            /// <summary>
            /// Type of the object
            /// </summary>
            [Key("ObjectType")]
            public ObjectType ObjectType;
            /// <summary>
            /// Coordinate index, always 0 except in Koikatsu
            /// </summary>
            [Key("CoordinateIndex")]
            public int CoordinateIndex;
            /// <summary>
            /// Slot of the accessory, hair, or clothing
            /// </summary>
            [Key("Slot")]
            public int Slot;
            /// <summary>
            /// Name of the material
            /// </summary>
            [Key("MaterialName")]
            public string MaterialName;
            /// <summary>
            /// Name of the property
            /// </summary>
            [Key("Property")]
            public string Property;
            /// <summary>
            /// Value
            /// </summary>
            [Key("Value")]
            public Color Value;
            /// <summary>
            /// Original value
            /// </summary>
            [Key("ValueOriginal")]
            public Color ValueOriginal;

            /// <summary>
            /// Data storage class for color properties
            /// </summary>
            /// <param name="objectType">Type of the object</param>
            /// <param name="coordinateIndex">Coordinate index, always 0 except in Koikatsu</param>
            /// <param name="slot">Slot of the accessory, hair, or clothing</param>
            /// <param name="materialName">Name of the material</param>
            /// <param name="property">Name of the property</param>
            /// <param name="value">Value</param>
            /// <param name="valueOriginal">Original value</param>
            public MaterialColorProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string property, Color value, Color valueOriginal)
            {
                ObjectType = objectType;
                CoordinateIndex = coordinateIndex;
                Slot = slot;
                MaterialName = materialName.FormatShadingObjectName();
                Property = property;
                Value = value;
                ValueOriginal = valueOriginal;
            }
        }

        /// <summary>
        /// Data storage class for texture properties
        /// </summary>
        [Serializable]
        [MessagePackObject]
        public class MaterialTextureProperty
        {
            /// <summary>
            /// Type of the object
            /// </summary>
            [Key("ObjectType")]
            public ObjectType ObjectType;
            /// <summary>
            /// Coordinate index, always 0 except in Koikatsu
            /// </summary>
            [Key("CoordinateIndex")]
            public int CoordinateIndex;
            /// <summary>
            /// Slot of the accessory, hair, or clothing
            /// </summary>
            [Key("Slot")]
            public int Slot;
            /// <summary>
            /// Name of the material
            /// </summary>
            [Key("MaterialName")]
            public string MaterialName;
            /// <summary>
            /// Name of the property
            /// </summary>
            [Key("Property")]
            public string Property;
            /// <summary>
            /// ID of the texture as stored in the texture dictionary
            /// </summary>
            [Key("TexID")]
            public int? TexID;
            /// <summary>
            /// Texture offset value
            /// </summary>
            [Key("Offset")]
            public Vector2? Offset;
            /// <summary>
            /// Texture offset original value
            /// </summary>
            [Key("OffsetOriginal")]
            public Vector2? OffsetOriginal;
            /// <summary>
            /// Texture scale value
            /// </summary>
            [Key("Scale")]
            public Vector2? Scale;
            /// <summary>
            /// Texture scale original value
            /// </summary>
            [Key("ScaleOriginal")]
            public Vector2? ScaleOriginal;
            /// <summary>
            /// Texture Animation Definition
            /// </summary>
            [Key("TexAnimationDef")]
            public MEAnimationDefine TexAnimationDef;

            /// <summary>
            /// Data storage class for texture properties
            /// </summary>
            /// <param name="objectType">Type of the object</param>
            /// <param name="coordinateIndex">Coordinate index, always 0 except in Koikatsu</param>
            /// <param name="slot">Slot of the accessory, hair, or clothing</param>
            /// <param name="materialName">Name of the material</param>
            /// <param name="property">Name of the property</param>
            /// <param name="texID">ID of the texture as stored in the texture dictionary</param>
            /// <param name="offset">Texture offset value</param>
            /// <param name="offsetOriginal">Texture offset original value</param>
            /// <param name="scale">Texture scale value</param>
            /// <param name="scaleOriginal">Texture scale original value</param>
            /// <param name="texAnimationDef">Texture animation define</param>
            public MaterialTextureProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string property, int? texID = null, Vector2? offset = null, Vector2? offsetOriginal = null, Vector2? scale = null, Vector2? scaleOriginal = null, MEAnimationDefine texAnimationDef = null)
            {
                ObjectType = objectType;
                CoordinateIndex = coordinateIndex;
                Slot = slot;
                MaterialName = materialName.FormatShadingObjectName();
                Property = property;
                TexID = texID;
                Offset = offset;
                OffsetOriginal = offsetOriginal;
                Scale = scale;
                ScaleOriginal = scaleOriginal;
                TexAnimationDef = texAnimationDef;
            }

            /// <summary>
            /// Check if any of a subset of properties is null. Which either make it safe to remove or a broken property.
            /// Both cases make the TextureProperty safe for removal
            /// </summary>
            /// <returns></returns>
            public bool NullCheck()
            {
                // These become null when an animated texture is removed
                var safeToRemove = TexID == null && Offset == null && Scale == null;
                // These should never be null, and the property will never work if they are (and can cause issues)
                var brokenProperty = Property == null || MaterialName == null;
                return safeToRemove || brokenProperty;
            }
        }

        /// <summary>
        /// Data storage class for shader data
        /// </summary>
        [Serializable]
        [MessagePackObject]
        public class MaterialShader
        {
            /// <summary>
            /// Type of the object
            /// </summary>
            [Key("ObjectType")]
            public ObjectType ObjectType;
            /// <summary>
            /// Coordinate index, always 0 except in Koikatsu
            /// </summary>
            [Key("CoordinateIndex")]
            public int CoordinateIndex;
            /// <summary>
            /// Slot of the accessory, hair, or clothing
            /// </summary>
            [Key("Slot")]
            public int Slot;
            /// <summary>
            /// Name of the material
            /// </summary>
            [Key("MaterialName")]
            public string MaterialName;
            /// <summary>
            /// Name of the shader
            /// </summary>
            [Key("ShaderName")]
            public string ShaderName;
            /// <summary>
            /// Name of the original shader
            /// </summary>
            [Key("ShaderNameOriginal")]
            public string ShaderNameOriginal;
            /// <summary>
            /// Render queue
            /// </summary>
            [Key("RenderQueue")]
            public int? RenderQueue;
            /// <summary>
            /// Original render queue
            /// </summary>
            [Key("RenderQueueOriginal")]
            public int? RenderQueueOriginal;

            /// <summary>
            /// Data storage class for shader data
            /// </summary>
            /// <param name="objectType">Type of the object</param>
            /// <param name="coordinateIndex">Coordinate index, always 0 except in Koikatsu</param>
            /// <param name="slot">Slot of the accessory, hair, or clothing</param>
            /// <param name="materialName">Name of the material</param>
            /// <param name="shaderName">Name of the shader</param>
            /// <param name="shaderNameOriginal">Name of the original shader</param>
            /// <param name="renderQueue">Render queue</param>
            /// <param name="renderQueueOriginal">Original render queue</param>
            public MaterialShader(ObjectType objectType, int coordinateIndex, int slot, string materialName, string shaderName, string shaderNameOriginal, int? renderQueue, int? renderQueueOriginal)
            {
                ObjectType = objectType;
                CoordinateIndex = coordinateIndex;
                Slot = slot;
                MaterialName = materialName.FormatShadingObjectName();
                ShaderName = shaderName;
                ShaderNameOriginal = shaderNameOriginal;
                RenderQueue = renderQueue;
                RenderQueueOriginal = renderQueueOriginal;
            }
            /// <summary>
            /// Data storage class for shader data
            /// </summary>
            /// <param name="objectType">Type of the object</param>
            /// <param name="coordinateIndex">Coordinate index, always 0 except in Koikatsu</param>
            /// <param name="slot">Slot of the accessory, hair, or clothing</param>
            /// <param name="materialName">Name of the material</param>
            /// <param name="shaderName">Name of the shader</param>
            /// <param name="shaderNameOriginal">Name of the original shader</param>
            public MaterialShader(ObjectType objectType, int coordinateIndex, int slot, string materialName, string shaderName, string shaderNameOriginal)
            {
                ObjectType = objectType;
                CoordinateIndex = coordinateIndex;
                Slot = slot;
                MaterialName = materialName.FormatShadingObjectName();
                ShaderName = shaderName;
                ShaderNameOriginal = shaderNameOriginal;
            }
            /// <summary>
            /// Data storage class for shader data
            /// </summary>
            /// <param name="objectType">Type of the object</param>
            /// <param name="coordinateIndex">Coordinate index, always 0 except in Koikatsu</param>
            /// <param name="slot">Slot of the accessory, hair, or clothing</param>
            /// <param name="materialName">Name of the material</param>
            /// <param name="renderQueue">Render queue</param>
            /// <param name="renderQueueOriginal">Original render queue</param>
            public MaterialShader(ObjectType objectType, int coordinateIndex, int slot, string materialName, int? renderQueue, int? renderQueueOriginal)
            {
                ObjectType = objectType;
                CoordinateIndex = coordinateIndex;
                Slot = slot;
                MaterialName = materialName.FormatShadingObjectName();
                RenderQueue = renderQueue;
                RenderQueueOriginal = renderQueueOriginal;
            }

            /// <summary>
            /// Check if the shader name and render queue are both null. Safe to delete this data if true.
            /// </summary>
            /// <returns></returns>
            public bool NullCheck() => ShaderName.IsNullOrEmpty() && RenderQueue == null;
        }

        /// <summary>
        /// Data storage class for material copy info
        /// </summary>
        [Serializable]
        [MessagePackObject]
        public class MaterialCopy
        {
            /// <summary>
            /// Type of the object
            /// </summary>
            [Key("ObjectType")]
            public ObjectType ObjectType;
            /// <summary>
            /// Coordinate index, always 0 except in Koikatsu
            /// </summary>
            [Key("CoordinateIndex")]
            public int CoordinateIndex;
            /// <summary>
            /// Slot of the accessory, hair, or clothing
            /// </summary>
            [Key("Slot")]
            public int Slot;
            /// <summary>
            /// Name of the material
            /// </summary>
            [Key("MaterialName")]
            public string MaterialName;
            /// <summary>
            /// Name of the copy
            /// </summary>
            [Key("MaterialCopyName")]
            public string MaterialCopyName;

            /// <summary>
            /// Constructor for material copy info objects
            /// </summary>
            /// <param name="objectType"></param>
            /// <param name="coordinateIndex"></param>
            /// <param name="slot"></param>
            /// <param name="materialName"></param>
            /// <param name="materialCopyName"></param>
            public MaterialCopy(ObjectType objectType, int coordinateIndex, int slot, string materialName, string materialCopyName)
            {
                ObjectType = objectType;
                CoordinateIndex = coordinateIndex;
                Slot = slot;
                MaterialName = materialName.FormatShadingObjectName();
                MaterialCopyName = materialCopyName;
            }
        }

        /// <summary>
        /// Data storage class for projector properties
        /// </summary>
        [Serializable]
        [MessagePackObject]
        public class ProjectorProperty
        {
            /// <summary>
            /// Type of the object
            /// </summary>
            [Key("ObjectType")]
            public ObjectType ObjectType;
            /// <summary>
            /// Coordinate index, always 0 except in Koikatsu
            /// </summary>
            [Key("CoordinateIndex")]
            public int CoordinateIndex;
            /// <summary>
            /// Slot of the accessory, hair, or clothing
            /// </summary>
            [Key("Slot")]
            public int Slot;
            /// <summary>
            /// Name of the projector
            /// </summary>
            [Key("ProjectorName")]
            public string ProjectorName;
            /// <summary>
            /// Property type
            /// </summary>
            [Key("Property")]
            public ProjectorProperties Property;
            /// <summary>
            /// Value
            /// </summary>
            [Key("Value")]
            public string Value;
            /// <summary>
            /// Original value
            /// </summary>
            [Key("ValueOriginal")]
            public string ValueOriginal;

            /// <summary>
            /// Data storage class for projector properties
            /// </summary>
            /// <param name="id">ID of the item</param>
            /// <param name="ProjectorName">Name of the projector</param>
            /// <param name="property">Property type</param>
            /// <param name="value">Value</param>
            /// <param name="valueOriginal">Original</param>
            public ProjectorProperty(ObjectType objectType, int coordinateIndex, int slot, string projectorName, ProjectorProperties property, string value, string valueOriginal)
            {
                ObjectType = objectType;
                CoordinateIndex = coordinateIndex;
                Slot = slot;
                ProjectorName = projectorName.FormatShadingObjectName();
                Property = property;
                Value = value;
                ValueOriginal = valueOriginal;
            }
        }    }
}
