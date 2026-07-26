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
        /// Add a float property to be saved and loaded with the card and optionally also update the materials.
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="value">Value</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void SetMaterialFloatProperty(int slot, ObjectType objectType, Material material, string propertyName, float value, GameObject go, bool setProperty = true)
        {
            var materialProperty = MaterialFloatPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted());
            if (materialProperty == null)
            {
                float valueOriginal = material.GetFloat($"_{propertyName}");
                MaterialFloatPropertyList.Add(new MaterialFloatProperty(objectType, GetCoordinateIndex(objectType), slot, material.NameFormatted(), propertyName, value.ToString(CultureInfo.InvariantCulture), valueOriginal.ToString(CultureInfo.InvariantCulture)));
            }
            else
            {
                if (value.ToString(CultureInfo.InvariantCulture) == materialProperty.ValueOriginal)
                    RemoveMaterialFloatProperty(slot, objectType, material, propertyName, go, false);
                else
                    materialProperty.Value = value.ToString(CultureInfo.InvariantCulture);
            }
            if (setProperty)
                SetFloat(go, material.NameFormatted(), propertyName, value);
        }
        /// <summary>
        /// Get the saved material property value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <returns>Saved material property value or null if none is saved</returns>
        public float? GetMaterialFloatPropertyValue(int slot, ObjectType objectType, Material material, string propertyName, GameObject go)
        {
            var value = MaterialFloatPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.Value;
            if (value.IsNullOrEmpty())
                return null;
            float parsedValue;
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedValue)
                ? parsedValue
                : (float?)null;
        }
        /// <summary>
        /// Get the saved material property's original value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <returns>Saved material property's original value or null if none is saved</returns>
        public float? GetMaterialFloatPropertyValueOriginal(int slot, ObjectType objectType, Material material, string propertyName, GameObject go)
        {
            var valueOriginal = MaterialFloatPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.ValueOriginal;
            if (valueOriginal.IsNullOrEmpty())
                return null;
            float parsedValue;
            return float.TryParse(valueOriginal, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedValue)
                ? parsedValue
                : (float?)null;
        }
        /// <summary>
        /// Remove the saved material property value if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void RemoveMaterialFloatProperty(int slot, ObjectType objectType, Material material, string propertyName, GameObject go, bool setProperty = true)
        {
            if (setProperty)
            {
                var original = GetMaterialFloatPropertyValueOriginal(slot, objectType, material, propertyName, go);
                if (original != null)
                    SetFloat(go, material.NameFormatted(), propertyName, (float)original);
            }
            MaterialFloatPropertyList.RemoveAll(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted());
        }

        /// <summary>
        /// Add a keyword property to be saved and loaded with the card and optionally also update the materials.
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="value">Value</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void SetMaterialKeywordProperty(int slot, ObjectType objectType, Material material, string propertyName, bool value, GameObject go, bool setProperty = true)
        {
            var materialProperty = MaterialKeywordPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted());
            if (materialProperty == null)
            {
                bool valueOriginal = material.IsKeywordEnabled($"_{propertyName}");
                MaterialKeywordPropertyList.Add(new MaterialKeywordProperty(objectType, GetCoordinateIndex(objectType), slot, material.NameFormatted(), propertyName, value, valueOriginal));
            }
            else
            {
                if (value == materialProperty.ValueOriginal)
                    RemoveMaterialKeywordProperty(slot, objectType, material, propertyName, go, false);
                else
                    materialProperty.Value = value;
            }
            if (setProperty)
                SetKeyword(go, material.NameFormatted(), propertyName, value);
        }
        /// <summary>
        /// Get the saved material property value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <returns>Saved material property value or null if none is saved</returns>
        public bool? GetMaterialKeywordPropertyValue(int slot, ObjectType objectType, Material material, string propertyName, GameObject go)
        {
            return MaterialKeywordPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.Value;
        }
        /// <summary>
        /// Get the saved material property's original value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <returns>Saved material property's original value or null if none is saved</returns>
        public bool? GetMaterialKeywordPropertyValueOriginal(int slot, ObjectType objectType, Material material, string propertyName, GameObject go)
        {
            return MaterialKeywordPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.ValueOriginal;
        }
        /// <summary>
        /// Remove the saved material property value if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void RemoveMaterialKeywordProperty(int slot, ObjectType objectType, Material material, string propertyName, GameObject go, bool setProperty = true)
        {
            if (setProperty)
            {
                var original = GetMaterialKeywordPropertyValueOriginal(slot, objectType, material, propertyName, go);
                if (original != null)
                    SetKeyword(go, material.NameFormatted(), propertyName, (bool)original);
            }
            MaterialKeywordPropertyList.RemoveAll(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted());
        }

        /// <summary>
        /// Add a color property to be saved and loaded with the card and optionally also update the materials.
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="value">Value</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void SetMaterialColorProperty(int slot, ObjectType objectType, Material material, string propertyName, Color value, GameObject go, bool setProperty = true)
        {
            var colorProperty = MaterialColorPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted());
            if (colorProperty == null)
            {
                Color valueOriginal = material.GetColor($"_{propertyName}");
                MaterialColorPropertyList.Add(new MaterialColorProperty(objectType, GetCoordinateIndex(objectType), slot, material.NameFormatted(), propertyName, value, valueOriginal));
            }
            else
            {
                if (value == colorProperty.ValueOriginal)
                    RemoveMaterialColorProperty(slot, objectType, material, propertyName, go, false);
                else
                    colorProperty.Value = value;
            }
            if (setProperty)
                SetColor(go, material.NameFormatted(), propertyName, value);
        }
        /// <summary>
        /// Get the saved material property value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <returns>Saved material property value or null if none is saved</returns>
        public Color? GetMaterialColorPropertyValue(int slot, ObjectType objectType, Material material, string propertyName, GameObject go)
        {
            return MaterialColorPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.Value;
        }
        /// <summary>
        /// Get the saved material property's original value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <returns>Saved material property's original value or null if none is saved</returns>
        public Color? GetMaterialColorPropertyValueOriginal(int slot, ObjectType objectType, Material material, string propertyName, GameObject go)
        {
            return MaterialColorPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.ValueOriginal;
        }
        /// <summary>
        /// Remove the saved material property value if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void RemoveMaterialColorProperty(int slot, ObjectType objectType, Material material, string propertyName, GameObject go, bool setProperty = true)
        {
            if (setProperty)
            {
                var original = GetMaterialColorPropertyValueOriginal(slot, objectType, material, propertyName, go);
                if (original != null)
                    SetColor(go, material.NameFormatted(), propertyName, (Color)original);
            }
            MaterialColorPropertyList.RemoveAll(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == propertyName && x.MaterialName == material.NameFormatted());
        }

    }
}
