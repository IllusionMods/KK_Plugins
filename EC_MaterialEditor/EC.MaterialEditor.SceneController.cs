#pragma warning disable IDE0060 // Remove unused parameter
using UnityEngine;

namespace KK_Plugins
{
    public partial class MaterialEditor
    {
        /// <summary>
        /// Stubs for all public methods so I don't have to do conditional compilation. Don't actually try to use this class or you will be disapointed.
        /// </summary>
        public class MaterialEditorSceneController
        {
            public void AddRendererProperty(int id, string rendererName, RendererProperties property, string value, string valueOriginal) { }
            public string GetRendererPropertyValue(int id, string rendererName, RendererProperties property) => null;
            public string GetRendererPropertyValueOriginal(int id, string rendererName, RendererProperties property) => null;
            public void RemoveRendererProperty(int id, string rendererName, RendererProperties property) { }

            public void AddMaterialFloatProperty(int id, string materialName, string property, string value, string valueOriginal) { }
            public string GetMaterialFloatPropertyValue(int id, string materialName, string property) => null;
            public string GetMaterialFloatPropertyValueOriginal(int id, string materialName, string property) => null;
            public void RemoveMaterialFloatProperty(int id, string materialName, string property) { }

            public void AddMaterialColorProperty(int id, string materialName, string property, Color value, Color valueOriginal) { }
            public Color GetMaterialColorPropertyValue(int id, string materialName, string property) => Color.white;
            public Color GetMaterialColorPropertyValueOriginal(int id, string materialName, string property) => Color.white;
            public void RemoveMaterialColorProperty(int id, string materialName, string property) { }

            public void AddMaterialTextureProperty(int id, string materialName, string property, TexturePropertyType propertyType, Vector2 value, Vector2 valueOriginal) { }
            public void AddMaterialTextureProperty(int id, string materialName, string property, GameObject go) { }
            public Vector2? GetMaterialTexturePropertyValue(int id, string materialName, string property, TexturePropertyType propertyType) => null;
            public Vector2? GetMaterialTexturePropertyValueOriginal(int id, string materialName, string property, TexturePropertyType propertyType) => null;
            public void RemoveMaterialTextureProperty(int id, string materialName, string property, TexturePropertyType propertyType) { }
            public void RemoveMaterialTextureProperty(int id, string materialName, string property) { }

            public void AddMaterialShader(int id, string materialName, string shaderName, string shaderNameOriginal) { }
            public void AddMaterialShader(int id, string materialName, int renderQueue, int renderQueueOriginal) { }
            public MaterialShader GetMaterialShaderValue(int id, string materialName) => null;
            public void RemoveMaterialShaderName(int id, string materialName) { }
            public void RemoveMaterialShaderRenderQueue(int id, string materialName) { }

            private class RendererProperty { }
            private class MaterialFloatProperty { }
            private class MaterialColorProperty { }
            public class MaterialTextureProperty { }
            public class MaterialShader
            {
                public string ShaderNameOriginal = null;
                public int? RenderQueueOriginal = null;
            }
        }
    }
}
#pragma warning restore IDE0060 // Remove unused parameter
