using KKAPI.Chara;
using MaterialEditorAPI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;
using static MaterialEditorAPI.MaterialEditorPluginBase;
namespace KK_Plugins.MaterialEditor
{
    using MEAnimationController = MEAnimationController<MaterialEditorCharaController, MaterialEditorCharaController.MaterialTextureProperty>;

    public partial class MaterialEditorCharaController
    {
        /// <summary>
        /// Add a texture property to be saved and loaded with the card.
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="filePath">Path to the .png file on disk</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <param name="setTexInUpdate">Whether to wait for the next Update</param>
        public void SetMaterialTextureFromFile(int slot, ObjectType objectType, Material material, string propertyName, string filePath, GameObject go, bool setTexInUpdate = false)
        {
            if (!File.Exists(filePath)) return;

            if (setTexInUpdate)
            {
                FileToSet = filePath;
                PropertyToSet = propertyName;
                MatToSet = material;
                GameObjectToSet = go;
                SlotToSet = slot;
                ObjectTypeToSet = objectType;
            }
            else
            {
                var texBytes = File.ReadAllBytes(filePath);
                SetMaterialTexture(slot, objectType, material, propertyName, texBytes, go);
            }
        }

        /// <summary>
        /// Add a texture property to be saved and loaded with the card.
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="data">Byte array containing the texture data</param>
        /// <param name="go">GameObject the material belongs to</param>
        public void SetMaterialTexture(int slot, ObjectType objectType, Material material, string propertyName, byte[] data, GameObject go)
        {
            if (data == null) return;

            var texID = SetAndGetTextureID(data);
            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted());
            if (textureProperty == null)
                MaterialTexturePropertyList.Add(textureProperty = new MaterialTextureProperty(objectType, GetCoordinateIndex(objectType), slot, material.NameFormatted(), propertyName, texID));
            else
                textureProperty.TexID = texID;

            textureProperty.TexAnimationDef = MEAnimationUtil.LoadAnimationDefFromBytes(texID, data, SetAndGetTextureID);
            SetTextureWithProperty(go, textureProperty);
        }
        /// <summary>
        /// Get the saved material property value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <returns>Saved material property value or null if none is saved</returns>
        public Texture GetMaterialTexture(int slot, ObjectType objectType, Material material, string propertyName, GameObject go)
        {
            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted());
            if (textureProperty?.TexID != null)
                return TextureDictionary[(int)textureProperty.TexID].Texture;
            return null;
        }
        /// <summary>
        /// Get whether the texture has been changed
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <returns>True if the texture has been modified, false if not</returns>
        public bool GetMaterialTextureOriginal(int slot, ObjectType objectType, Material material, string propertyName, GameObject go)
        {
            return MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.TexID == null;
        }
        /// <summary>
        /// Remove the saved material property value if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <param name="displayMessage">Whether to display a message on screen telling the user to save and reload to refresh textures</param>
        public void RemoveMaterialTexture(int slot, ObjectType objectType, Material material, string propertyName, GameObject go, bool displayMessage = true)
        {
            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted());
            if (textureProperty != null)
            {
                if (displayMessage)
                    MaterialEditorPlugin.Logger.LogMessage("Save and reload character or change outfits to refresh textures.");
                textureProperty.TexID = null;
                RemoveTexturePropertyIfNull(textureProperty);
            }
        }
        /// <summary>
        /// If TextureProperty is null, delete it.
        /// </summary>
        /// <param name="textureProperty"></param>
        private void RemoveTexturePropertyIfNull(MaterialTextureProperty textureProperty)
        {
            if (!textureProperty.NullCheck())
                return;
            MaterialTexturePropertyList.Remove(textureProperty);
            AnimationControllerMap.Remove(textureProperty);
        }

        /// <summary>
        /// Add a texture offset property to be saved and loaded with the card and optionally also update the materials.
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="value">Value</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void SetMaterialTextureOffset(int slot, ObjectType objectType, Material material, string propertyName, Vector2 value, GameObject go, bool setProperty = true)
        {
            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted());
            if (textureProperty == null)
            {
                Vector2 valueOriginal = material.GetTextureOffset($"_{propertyName}");
                MaterialTexturePropertyList.Add(new MaterialTextureProperty(objectType, GetCoordinateIndex(objectType), slot, material.NameFormatted(), propertyName, offset: value, offsetOriginal: valueOriginal));
            }
            else
            {
                if (value == textureProperty.OffsetOriginal)
                    RemoveMaterialTextureOffset(slot, objectType, material, propertyName, go, false);
                else
                {
                    textureProperty.Offset = value;
                    if (textureProperty.OffsetOriginal == null)
                        textureProperty.OffsetOriginal = material.GetTextureOffset($"_{propertyName}");
                }
            }
            if (setProperty)
                SetTextureOffset(go, material.NameFormatted(), propertyName, value);
        }
        /// <summary>
        /// Get the saved material property value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <returns>Saved material property value or null if none is saved</returns>
        public Vector2? GetMaterialTextureOffset(int slot, ObjectType objectType, Material material, string propertyName, GameObject go)
        {
            return MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.Offset;
        }
        /// <summary>
        /// Get the saved material property's original value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <returns>Saved material property's original value or null if none is saved</returns>
        public Vector2? GetMaterialTextureOffsetOriginal(int slot, ObjectType objectType, Material material, string propertyName, GameObject go)
        {
            return MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.OffsetOriginal;
        }
        /// <summary>
        /// Remove the saved material property value if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void RemoveMaterialTextureOffset(int slot, ObjectType objectType, Material material, string propertyName, GameObject go, bool setProperty = true)
        {
            if (setProperty)
            {
                var original = GetMaterialTextureOffsetOriginal(slot, objectType, material, propertyName, go);
                if (original != null)
                    SetTextureOffset(go, material.NameFormatted(), propertyName, (Vector2)original);
            }

            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted());
            if (textureProperty != null)
            {
                textureProperty.Offset = null;
                textureProperty.OffsetOriginal = null;
                RemoveTexturePropertyIfNull(textureProperty);
            }
        }

        /// <summary>
        /// Add a texture scale property to be saved and loaded with the card and optionally also update the materials.
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="value">Value</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void SetMaterialTextureScale(int slot, ObjectType objectType, Material material, string propertyName, Vector2 value, GameObject go, bool setProperty = true)
        {
            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted());
            if (textureProperty == null)
            {
                Vector2 valueOriginal = material.GetTextureScale($"_{propertyName}");
                MaterialTexturePropertyList.Add(new MaterialTextureProperty(objectType, GetCoordinateIndex(objectType), slot, material.NameFormatted(), propertyName, scale: value, scaleOriginal: valueOriginal));
            }
            else
            {
                if (value == textureProperty.ScaleOriginal)
                    RemoveMaterialTextureScale(slot, objectType, material, propertyName, go, false);
                else
                {
                    textureProperty.Scale = value;
                    if (textureProperty.ScaleOriginal == null)
                        textureProperty.ScaleOriginal = material.GetTextureScale($"_{propertyName}");
                }
            }

            if (setProperty)
                SetTextureScale(go, material.NameFormatted(), propertyName, value);
        }
        /// <summary>
        /// Get the saved material property value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <returns>Saved material property value or null if none is saved</returns>
        public Vector2? GetMaterialTextureScale(int slot, ObjectType objectType, Material material, string propertyName, GameObject go)
        {
            return MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.Scale;
        }
        /// <summary>
        /// Get the saved material property's original value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <returns>Saved material property's original value or null if none is saved</returns>
        public Vector2? GetMaterialTextureScaleOriginal(int slot, ObjectType objectType, Material material, string propertyName, GameObject go)
        {
            return MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.ScaleOriginal;
        }
        /// <summary>
        /// Remove the saved material property value if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void RemoveMaterialTextureScale(int slot, ObjectType objectType, Material material, string propertyName, GameObject go, bool setProperty = true)
        {
            if (setProperty)
            {
                var original = GetMaterialTextureScaleOriginal(slot, objectType, material, propertyName, go);
                if (original != null)
                    SetTextureScale(go, material.NameFormatted(), propertyName, (Vector2)original);
            }

            var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted());
            if (textureProperty != null)
            {
                textureProperty.Scale = null;
                textureProperty.ScaleOriginal = null;
                RemoveTexturePropertyIfNull(textureProperty);
            }
        }

        /// <summary>
        /// Add a shader to be saved and loaded with the card and optionally also update the materials.
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="shaderName">Name of the shader to be saved, must be a shader that has been loaded by MaterialEditor</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void SetMaterialShader(int slot, ObjectType objectType, Material material, string shaderName, GameObject go, bool setProperty = true)
        {
            var materialProperty = MaterialShaderList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.MaterialName == material.NameFormatted());
            if (materialProperty == null)
            {
                string shaderNameOriginal = material.shader.NameFormatted();
                MaterialShaderList.Add(new MaterialShader(objectType, GetCoordinateIndex(objectType), slot, material.NameFormatted(), shaderName, shaderNameOriginal));
            }
            else
            {
                if (shaderName == materialProperty.ShaderNameOriginal)
                    RemoveMaterialShader(slot, objectType, material, go, false);
                else
                {
                    materialProperty.ShaderName = shaderName;
                    if (materialProperty.ShaderNameOriginal == null)
                        materialProperty.ShaderNameOriginal = material.shader.NameFormatted();
                }
            }

            if (setProperty)
            {
#if KK || EC || KKS
                if (objectType == ObjectType.Character && MaterialEditorPlugin.EyeMaterials.Contains(material.NameFormatted()))
                {
                    SetShader(go, material.NameFormatted(), shaderName, true);
                }
                else
#endif
                {
                    RemoveMaterialShaderRenderQueue(slot, objectType, material, go, false);
                    SetShader(go, material.NameFormatted(), shaderName);
                }
            }
        }

        /// <summary>
        /// Get the saved shader name or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <returns>Saved shader name or null if none is saved</returns>
        public string GetMaterialShader(int slot, ObjectType objectType, Material material, GameObject go)
        {
            return MaterialShaderList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.MaterialName == material.NameFormatted())?.ShaderName;
        }
        /// <summary>
        /// Get the saved shader name's original value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <returns>Saved shader name's original value or null if none is saved</returns>
        public string GetMaterialShaderOriginal(int slot, ObjectType objectType, Material material, GameObject go)
        {
            return MaterialShaderList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.MaterialName == material.NameFormatted())?.ShaderNameOriginal;
        }

        /// <summary>
        /// Remove the saved shader if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void RemoveMaterialShader(int slot, ObjectType objectType, Material material, GameObject go, bool setProperty = true)
        {
            if (setProperty)
            {
                var original = GetMaterialShaderOriginal(slot, objectType, material, go);
                if (!original.IsNullOrEmpty())
                {
#if KK || EC || KKS
                    if (objectType == ObjectType.Character && MaterialEditorPlugin.EyeMaterials.Contains(material.NameFormatted()))
                    {
                        SetShader(go, material.NameFormatted(), original, true);

                    }
                    else
#endif
                    {
                        SetShader(go, material.NameFormatted(), original);
                    }
                }
            }

            foreach (var materialProperty in MaterialShaderList.Where(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.MaterialName == material.NameFormatted()))
            {
                materialProperty.ShaderName = null;
                materialProperty.ShaderNameOriginal = null;
            }

            MaterialShaderList.RemoveAll(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.MaterialName == material.NameFormatted() && x.NullCheck());
        }

        /// <summary>
        /// Add a shader render queue to be saved and loaded with the card and optionally also update the materials.
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="renderQueue">Value</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void SetMaterialShaderRenderQueue(int slot, ObjectType objectType, Material material, int renderQueue, GameObject go, bool setProperty = true)
        {
            var materialProperty = MaterialShaderList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.MaterialName == material.NameFormatted());
            if (materialProperty == null)
            {
                int renderQueueOriginal = material.renderQueue;
                MaterialShaderList.Add(new MaterialShader(objectType, GetCoordinateIndex(objectType), slot, material.NameFormatted(), renderQueue, renderQueueOriginal));
            }
            else
            {
                int renderQueueOriginal;
                if (materialProperty.RenderQueueOriginal == null)
                    renderQueueOriginal = material.renderQueue;
                else
                    renderQueueOriginal = (int)materialProperty.RenderQueueOriginal;

                if (renderQueue == renderQueueOriginal)
                    RemoveMaterialShaderRenderQueue(slot, objectType, material, go, false);
                else
                {
                    materialProperty.RenderQueue = renderQueue;
                    if (materialProperty.RenderQueueOriginal == null)
                        materialProperty.RenderQueueOriginal = material.renderQueue;
                }
            }

            if (setProperty)
                SetRenderQueue(go, material.NameFormatted(), renderQueue);
        }
        /// <summary>
        /// Get the saved render queue or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <returns>Saved render queue or null if none is saved</returns>
        public int? GetMaterialShaderRenderQueue(int slot, ObjectType objectType, Material material, GameObject go)
        {
            return MaterialShaderList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.MaterialName == material.NameFormatted())?.RenderQueue;
        }
        /// <summary>
        /// Get the saved render queue's original value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <returns>Saved render queue's original value or null if none is saved</returns>
        public int? GetMaterialShaderRenderQueueOriginal(int slot, ObjectType objectType, Material material, GameObject go)
        {
            return MaterialShaderList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.MaterialName == material.NameFormatted())?.RenderQueueOriginal;
        }
        /// <summary>
        /// Remove the saved render queue if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void RemoveMaterialShaderRenderQueue(int slot, ObjectType objectType, Material material, GameObject go, bool setProperty = true)
        {
            if (setProperty)
            {
                var original = GetMaterialShaderRenderQueueOriginal(slot, objectType, material, go);
                if (original != null)
                    SetRenderQueue(go, material.NameFormatted(), original);
            }

            foreach (var materialProperty in MaterialShaderList.Where(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.MaterialName == material.NameFormatted()))
            {
                materialProperty.RenderQueue = null;
                materialProperty.RenderQueueOriginal = null;
            }

            MaterialShaderList.RemoveAll(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.MaterialName == material.NameFormatted() && x.NullCheck());
        }

    }
}
