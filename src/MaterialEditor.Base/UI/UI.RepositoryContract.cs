using System.Collections.Generic;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;
namespace MaterialEditorAPI
{
#pragma warning disable BepInEx001 // Attribute is declared on concrete plugin subclasses.
    public abstract partial class MaterialEditorUI
#pragma warning restore BepInEx001
    {
        /// <summary>
        /// Gets the original value of a renderer property.
        /// </summary>
        /// <param name="data">The data object associated with the renderer.</param>
        /// <param name="renderer">The renderer to retrieve the property value from.</param>
        /// <param name="property">The property to retrieve.</param>
        /// <param name="gameObject">The game object associated with the renderer.</param>
        /// <returns>The original value of the renderer property.</returns>
        public abstract string GetRendererPropertyValueOriginal(object data, Renderer renderer, RendererProperties property, GameObject gameObject);
        /// <summary>
        /// Gets the current value of a renderer property.
        /// </summary>
        /// <param name="data">The data object associated with the renderer.</param>
        /// <param name="renderer">The renderer to retrieve the property value from.</param>
        /// <param name="property">The property to retrieve.</param>
        /// <param name="gameObject">The game object associated with the renderer.</param>
        /// <returns>The current value of the renderer property.</returns>
        public abstract string GetRendererPropertyValue(object data, Renderer renderer, RendererProperties property, GameObject gameObject);
        /// <summary>
        /// Sets the value of a renderer property.
        /// </summary>
        /// <param name="data">The data object associated with the renderer.</param>
        /// <param name="renderer">The renderer to modify.</param>
        /// <param name="property">The property to set.</param>
        /// <param name="value">The value to set for the property.</param>
        /// <param name="gameObject">The game object associated with the renderer.</param>
        public abstract void SetRendererProperty(object data, Renderer renderer, RendererProperties property, string value, GameObject gameObject);
        /// <summary>
        /// Removes a renderer property.
        /// </summary>
        /// <param name="data">The data object associated with the renderer.</param>
        /// <param name="renderer">The renderer to modify.</param>
        /// <param name="property">The property to remove.</param>
        /// <param name="gameObject">The game object associated with the renderer.</param>
        public abstract void RemoveRendererProperty(object data, Renderer renderer, RendererProperties property, GameObject gameObject);

        /// <summary>
        /// Gets the original value of a projector property.
        /// </summary>
        /// <param name="data">The data object associated with the projector.</param>
        /// <param name="renderer">The projector to retrieve the property value from.</param>
        /// <param name="property">The property to retrieve.</param>
        /// <param name="gameObject">The game object associated with the projector.</param>
        /// <returns>The original value of the projector property.</returns>
        public abstract float? GetProjectorPropertyValueOriginal(object data, Projector renderer, ProjectorProperties property, GameObject gameObject);
        /// <summary>
        /// Gets the current value of a projector property.
        /// </summary>
        /// <param name="data">The data object associated with the projector.</param>
        /// <param name="renderer">The projector to retrieve the property value from.</param>
        /// <param name="property">The property to retrieve.</param>
        /// <param name="gameObject">The game object associated with the projector.</param>
        /// <returns>The current value of the projector property.</returns>
        public abstract float? GetProjectorPropertyValue(object data, Projector renderer, ProjectorProperties property, GameObject gameObject);
        /// <summary>
        /// Sets the value of a projector property.
        /// </summary>
        /// <param name="data">The data object associated with the projector.</param>
        /// <param name="projector">The projector to modify.</param>
        /// <param name="property">The property to set.</param>
        /// <param name="value">The value to set for the property.</param>
        /// <param name="gameObject">The game object associated with the projector.</param>
        public abstract void SetProjectorProperty(object data, Projector projector, ProjectorProperties property, float value, GameObject gameObject);
        /// <summary>
        /// Removes a projector property.
        /// </summary>
        /// <param name="data">The data object associated with the projector.</param>
        /// <param name="projector">The projector to modify.</param>
        /// <param name="property">The property to remove.</param>
        /// <param name="gameObject">The game object associated with the projector.</param>
        public abstract void RemoveProjectorProperty(object data, Projector projector, ProjectorProperties property, GameObject gameObject);
        /// <summary>
        /// Gets the list of projectors associated with a game object.
        /// </summary>
        /// <param name="data">The data object associated with the game object.</param>
        /// <param name="gameObject">The game object to retrieve projectors from.</param>
        /// <returns>An enumerable list of projectors.</returns>
        public abstract IEnumerable<Projector> GetProjectorList(object data, GameObject gameObject);

        /// <summary>
        /// Copies edits made to a material.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to copy edits from.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        public abstract void MaterialCopyEdits(object data, Material material, GameObject gameObject);
        /// <summary>
        /// Pastes edits to a material.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to paste edits to.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        public abstract void MaterialPasteEdits(object data, Material material, GameObject gameObject);
        /// <summary>
        /// Removes copied edits from a material.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to remove copied edits from.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        public abstract void MaterialCopyRemove(object data, Material material, GameObject gameObject);

        /// <summary>
        /// Gets the original name of a material.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="renderer">The renderer associated with the material.</param>
        /// <param name="material">The material to retrieve the name from.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        /// <returns>The original name of the material.</returns>
        public abstract string GetMaterialNameOriginal(object data, Renderer renderer, Material material, GameObject gameObject);
        /// <summary>
        /// Sets the name of a material.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="renderer">The renderer associated with the material.</param>
        /// <param name="material">The material to set the name for.</param>
        /// <param name="value">The new name for the material.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        public abstract void SetMaterialName(object data, Renderer renderer, Material material, string value, GameObject gameObject);
        /// <summary>
        /// Removes the name of a material.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="renderer">The renderer associated with the material.</param>
        /// <param name="material">The material to remove the name from.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        public abstract void RemoveMaterialName(object data, Renderer renderer, Material material, GameObject gameObject);

        /// <summary>
        /// Gets the original shader name of a material.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to retrieve the shader name from.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        /// <returns>The original shader name of the material.</returns>
        public abstract string GetMaterialShaderNameOriginal(object data, Material material, GameObject gameObject);
        /// <summary>
        /// Sets the shader name of a material.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to set the shader name for.</param>
        /// <param name="value">The new shader name for the material.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        public abstract void SetMaterialShaderName(object data, Material material, string value, GameObject gameObject);
        /// <summary>
        /// Removes the shader name of a material.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to remove the shader name from.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        public abstract void RemoveMaterialShaderName(object data, Material material, GameObject gameObject);

        /// <summary>
        /// Gets the original render queue value of a material's shader.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to retrieve the render queue value from.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        /// <returns>The original render queue value of the material's shader.</returns>
        public abstract int? GetMaterialShaderRenderQueueOriginal(object data, Material material, GameObject gameObject);
        /// <summary>
        /// Sets the render queue value of a material's shader.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to set the render queue value for.</param>
        /// <param name="value">The new render queue value for the material's shader.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        public abstract void SetMaterialShaderRenderQueue(object data, Material material, int value, GameObject gameObject);
        /// <summary>
        /// Removes the render queue value of a material's shader.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to remove the render queue value from.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        public abstract void RemoveMaterialShaderRenderQueue(object data, Material material, GameObject gameObject);

        /// <summary>
        /// Gets the original texture value of a material property.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to retrieve the texture value from.</param>
        /// <param name="propertyName">The name of the texture property.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        /// <returns>True if the texture value has changed; otherwise, false.</returns>
        public abstract bool GetMaterialTextureValueOriginal(object data, Material material, string propertyName, GameObject gameObject);
        /// <summary>
        /// Sets the texture value of a material property.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to set the texture value for.</param>
        /// <param name="propertyName">The name of the texture property.</param>
        /// <param name="filePath">The file path of the texture to set.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        public abstract void SetMaterialTexture(object data, Material material, string propertyName, string filePath, GameObject gameObject);
        /// <summary>
        /// Removes the texture value of a material property.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to remove the texture value from.</param>
        /// <param name="propertyName">The name of the texture property.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        public abstract void RemoveMaterialTexture(object data, Material material, string propertyName, GameObject gameObject);

        /// <summary>
        /// Gets the original texture offset value of a material property.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to retrieve the texture offset value from.</param>
        /// <param name="propertyName">The name of the texture property.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        /// <returns>The original texture offset value of the material property.</returns>
        public abstract Vector2? GetMaterialTextureOffsetOriginal(object data, Material material, string propertyName, GameObject gameObject);
        /// <summary>
        /// Sets the texture offset value of a material property.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to set the texture offset value for.</param>
        /// <param name="propertyName">The name of the texture property.</param>
        /// <param name="value">The new texture offset value.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        public abstract void SetMaterialTextureOffset(object data, Material material, string propertyName, Vector2 value, GameObject gameObject);
        /// <summary>
        /// Removes the texture offset value of a material property.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to remove the texture offset value from.</param>
        /// <param name="propertyName">The name of the texture property.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        public abstract void RemoveMaterialTextureOffset(object data, Material material, string propertyName, GameObject gameObject);

        /// <summary>
        /// Gets the original texture scale value of a material property.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to retrieve the texture scale value from.</param>
        /// <param name="propertyName">The name of the texture property.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        /// <returns>The original texture scale value of the material property.</returns>
        public abstract Vector2? GetMaterialTextureScaleOriginal(object data, Material material, string propertyName, GameObject gameObject);
        /// <summary>
        /// Sets the texture scale value of a material property.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to set the texture scale value for.</param>
        /// <param name="propertyName">The name of the texture property.</param>
        /// <param name="value">The new texture scale value.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        public abstract void SetMaterialTextureScale(object data, Material material, string propertyName, Vector2 value, GameObject gameObject);
        /// <summary>
        /// Removes the texture scale value of a material property.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to remove the texture scale value from.</param>
        /// <param name="propertyName">The name of the texture property.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        public abstract void RemoveMaterialTextureScale(object data, Material material, string propertyName, GameObject gameObject);

        /// <summary>
        /// Gets the original color value of a material property.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to retrieve the color value from.</param>
        /// <param name="propertyName">The name of the color property.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        /// <returns>The original color value of the material property.</returns>
        public abstract Color? GetMaterialColorPropertyValueOriginal(object data, Material material, string propertyName, GameObject gameObject);
        /// <summary>
        /// Sets the color value of a material property.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to set the color value for.</param>
        /// <param name="propertyName">The name of the color property.</param>
        /// <param name="value">The new color value.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        public abstract void SetMaterialColorProperty(object data, Material material, string propertyName, Color value, GameObject gameObject);
        /// <summary>
        /// Removes the color value of a material property.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to remove the color value from.</param>
        /// <param name="propertyName">The name of the color property.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        public abstract void RemoveMaterialColorProperty(object data, Material material, string propertyName, GameObject gameObject);

        /// <summary>
        /// Gets the original float value of a material property.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to retrieve the float value from.</param>
        /// <param name="propertyName">The name of the float property.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        /// <returns>The original float value of the material property.</returns>
        public abstract float? GetMaterialFloatPropertyValueOriginal(object data, Material material, string propertyName, GameObject gameObject);
        /// <summary>
        /// Sets the float value of a material property.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to set the float value for.</param>
        /// <param name="propertyName">The name of the float property.</param>
        /// <param name="value">The new float value.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        public abstract void SetMaterialFloatProperty(object data, Material material, string propertyName, float value, GameObject gameObject);
        /// <summary>
        /// Removes the float value of a material property.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to remove the float value from.</param>
        /// <param name="propertyName">The name of the float property.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        public abstract void RemoveMaterialFloatProperty(object data, Material material, string propertyName, GameObject gameObject);

        /// <summary>
        /// Gets the original keyword value of a material property.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to retrieve the keyword value from.</param>
        /// <param name="propertyName">The name of the keyword property.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        /// <returns>The original keyword value of the material property.</returns>
        public abstract bool? GetMaterialKeywordPropertyValueOriginal(object data, Material material, string propertyName, GameObject gameObject);
        /// <summary>
        /// Sets the keyword value of a material property.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to set the keyword value for.</param>
        /// <param name="propertyName">The name of the keyword property.</param>
        /// <param name="value">The new keyword value.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        public abstract void SetMaterialKeywordProperty(object data, Material material, string propertyName, bool value, GameObject gameObject);
        /// <summary>
        /// Removes the keyword value of a material property.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to remove the keyword value from.</param>
        /// <param name="propertyName">The name of the keyword property.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        public abstract void RemoveMaterialKeywordProperty(object data, Material material, string propertyName, GameObject gameObject);

    }
}
