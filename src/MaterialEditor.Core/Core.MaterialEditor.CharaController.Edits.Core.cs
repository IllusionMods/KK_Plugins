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
        /// Copy any edits for the specified object
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="go">GameObject the material belongs to</param>
        public void MaterialCopyEdits(int slot, ObjectType objectType, Material material, GameObject go)
        {
            CopyData.ClearAll();

            foreach (var materialShader in MaterialShaderList.Where(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.MaterialName == material.NameFormatted()))
                CopyData.MaterialShaderList.Add(new CopyContainer.MaterialShader(materialShader.ShaderName, materialShader.RenderQueue));
            foreach (var materialFloatProperty in MaterialFloatPropertyList.Where(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.MaterialName == material.NameFormatted()))
                CopyData.MaterialFloatPropertyList.Add(new CopyContainer.MaterialFloatProperty(materialFloatProperty.Property, float.Parse(materialFloatProperty.Value)));
            foreach (var materialKeywordProperty in MaterialKeywordPropertyList.Where(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.MaterialName == material.NameFormatted()))
                CopyData.MaterialKeywordPropertyList.Add(new CopyContainer.MaterialKeywordProperty(materialKeywordProperty.Property, materialKeywordProperty.Value));
            foreach (var materialColorProperty in MaterialColorPropertyList.Where(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.MaterialName == material.NameFormatted()))
                CopyData.MaterialColorPropertyList.Add(new CopyContainer.MaterialColorProperty(materialColorProperty.Property, materialColorProperty.Value));
            foreach (var materialTextureProperty in MaterialTexturePropertyList.Where(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.MaterialName == material.NameFormatted()))
            {
                if (materialTextureProperty.TexID != null)
                    CopyData.MaterialTexturePropertyList.Add(new CopyContainer.MaterialTextureProperty(materialTextureProperty.Property, TextureDictionary[(int)materialTextureProperty.TexID].Data, materialTextureProperty.Offset, materialTextureProperty.Scale));
                else
                    CopyData.MaterialTexturePropertyList.Add(new CopyContainer.MaterialTextureProperty(materialTextureProperty.Property, null, materialTextureProperty.Offset, materialTextureProperty.Scale));
            }
            if (GetProjectorList(objectType, go).FirstOrDefault(x => x.material == material) != null)
                foreach (var projectorProperty in ProjectorPropertyList.Where(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.ProjectorName == material.NameFormatted()))
                    CopyData.ProjectorPropertyList.Add(new CopyContainer.ProjectorProperty(projectorProperty.Property, float.Parse(projectorProperty.Value)));

        }
        /// <summary>
        /// Paste any edits for the specified object
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void MaterialPasteEdits(int slot, ObjectType objectType, Material material, GameObject go, bool setProperty = true)
        {
            for (var i = 0; i < CopyData.MaterialShaderList.Count; i++)
            {
                var materialShader = CopyData.MaterialShaderList[i];
                if (materialShader.ShaderName != null)
                    SetMaterialShader(slot, objectType, material, materialShader.ShaderName, go, setProperty);
                if (materialShader.RenderQueue != null)
                    SetMaterialShaderRenderQueue(slot, objectType, material, (int)materialShader.RenderQueue, go, setProperty);
            }
            for (var i = 0; i < CopyData.MaterialFloatPropertyList.Count; i++)
            {
                var materialFloatProperty = CopyData.MaterialFloatPropertyList[i];
                if (material.HasProperty($"_{materialFloatProperty.Property}"))
                    SetMaterialFloatProperty(slot, objectType, material, materialFloatProperty.Property, materialFloatProperty.Value, go, setProperty);
            }
            for (var i = 0; i < CopyData.MaterialKeywordPropertyList.Count; i++)
            {
                var materialKeywordProperty = CopyData.MaterialKeywordPropertyList[i];
                SetMaterialKeywordProperty(slot, objectType, material, materialKeywordProperty.Property, materialKeywordProperty.Value, go, setProperty);
            }
            for (var i = 0; i < CopyData.MaterialColorPropertyList.Count; i++)
            {
                var materialColorProperty = CopyData.MaterialColorPropertyList[i];
                if (material.HasProperty($"_{materialColorProperty.Property}"))
                    SetMaterialColorProperty(slot, objectType, material, materialColorProperty.Property, materialColorProperty.Value, go, setProperty);
            }
            for (var i = 0; i < CopyData.MaterialTexturePropertyList.Count; i++)
            {
                var materialTextureProperty = CopyData.MaterialTexturePropertyList[i];
                if (material.HasProperty($"_{materialTextureProperty.Property}"))
                    SetMaterialTexture(slot, objectType, material, materialTextureProperty.Property, materialTextureProperty.Data, go);
                if (materialTextureProperty.Offset != null)
                    SetMaterialTextureOffset(slot, objectType, material, materialTextureProperty.Property, (Vector2)materialTextureProperty.Offset, go, setProperty);
                if (materialTextureProperty.Scale != null)
                    SetMaterialTextureScale(slot, objectType, material, materialTextureProperty.Property, (Vector2)materialTextureProperty.Scale, go, setProperty);
            }

            var projector = GetProjectorList(objectType, go).FirstOrDefault(x => x.material == material);
            if (projector != null)
                for (var i = 0; i < CopyData.MaterialTexturePropertyList.Count; i++)
                {
                    var projectorProperty = CopyData.ProjectorPropertyList[i];
                    SetProjectorProperty(slot, objectType, projector, projectorProperty.Property, projectorProperty.Value, go, setProperty);
                }
        }

        /// <summary>
        /// Duplicate a material / remove a duplicate material from the specified object
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="objectType"></param>
        /// <param name="material"></param>
        /// <param name="go"></param>
        public void MaterialCopyRemove(int slot, ObjectType objectType, Material material, GameObject go)
        {
            string matName = material.NameFormatted();
            if (matName.Contains(MaterialCopyPostfix))
            {
                MaterialNamePropertyList.RemoveAll(x => x.CoordinateIndex == CurrentCoordinateIndex && x.ObjectType == objectType && x.Slot == slot && x.Value == material.name);

                RemoveMaterial(go, material);
                MaterialShaderList.RemoveAll(x => x.CoordinateIndex == CurrentCoordinateIndex && x.ObjectType == objectType && x.Slot == slot && x.MaterialName == matName);
                MaterialFloatPropertyList.RemoveAll(x => x.CoordinateIndex == CurrentCoordinateIndex && x.ObjectType == objectType && x.Slot == slot && x.MaterialName == matName);
                MaterialKeywordPropertyList.RemoveAll(x => x.CoordinateIndex == CurrentCoordinateIndex && x.ObjectType == objectType && x.Slot == slot && x.MaterialName == matName);
                MaterialColorPropertyList.RemoveAll(x => x.CoordinateIndex == CurrentCoordinateIndex && x.ObjectType == objectType && x.Slot == slot && x.MaterialName == matName);
                MaterialTexturePropertyList.RemoveAll(x => x.CoordinateIndex == CurrentCoordinateIndex && x.ObjectType == objectType && x.Slot == slot && x.MaterialName == matName);
                MaterialCopyList.RemoveAll(x => x.CoordinateIndex == CurrentCoordinateIndex && x.ObjectType == objectType && x.Slot == slot && x.MaterialCopyName == matName);
            }
            else if (GetMaterialNamePropertyValue(slot, objectType, GetRendererList(go).FirstOrDefault(x => x.materials.Contains(material)), material, go) == string.Empty)
            {
                string newMatName = CopyMaterial(go, matName);
                MaterialCopyList.Add(new MaterialCopy(objectType, CurrentCoordinateIndex, slot, matName, newMatName));

                List<MaterialShader> newAccessoryMaterialShaderList = new List<MaterialShader>();
                List<MaterialFloatProperty> newAccessoryMaterialFloatPropertyList = new List<MaterialFloatProperty>();
                List<MaterialKeywordProperty> newAccessoryMaterialKeywordPropertyList = new List<MaterialKeywordProperty>();
                List<MaterialColorProperty> newAccessoryMaterialColorPropertyList = new List<MaterialColorProperty>();
                List<MaterialTextureProperty> newAccessoryMaterialTexturePropertyList = new List<MaterialTextureProperty>();

                foreach (var property in MaterialShaderList.Where(x => x.ObjectType == objectType && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot && x.MaterialName == matName))
                    newAccessoryMaterialShaderList.Add(new MaterialShader(property.ObjectType, property.CoordinateIndex, slot, newMatName, property.ShaderName, property.ShaderNameOriginal, property.RenderQueue, property.RenderQueueOriginal));
                foreach (var property in MaterialFloatPropertyList.Where(x => x.ObjectType == objectType && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot && x.MaterialName == matName))
                    newAccessoryMaterialFloatPropertyList.Add(new MaterialFloatProperty(property.ObjectType, property.CoordinateIndex, slot, newMatName, property.Property, property.Value, property.ValueOriginal));
                foreach (var property in MaterialKeywordPropertyList.Where(x => x.ObjectType == objectType && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot && x.MaterialName == matName))
                    newAccessoryMaterialKeywordPropertyList.Add(new MaterialKeywordProperty(property.ObjectType, property.CoordinateIndex, slot, newMatName, property.Property, property.Value, property.ValueOriginal));
                foreach (var property in MaterialColorPropertyList.Where(x => x.ObjectType == objectType && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot && x.MaterialName == matName))
                    newAccessoryMaterialColorPropertyList.Add(new MaterialColorProperty(property.ObjectType, property.CoordinateIndex, slot, newMatName, property.Property, property.Value, property.ValueOriginal));
                foreach (var property in MaterialTexturePropertyList.Where(x => x.ObjectType == objectType && x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot && x.MaterialName == matName))
                    newAccessoryMaterialTexturePropertyList.Add(new MaterialTextureProperty(property.ObjectType, property.CoordinateIndex, slot, newMatName, property.Property, property.TexID, property.Offset, property.OffsetOriginal, property.Scale, property.ScaleOriginal, property.TexAnimationDef));

                MaterialShaderList.AddRange(newAccessoryMaterialShaderList);
                MaterialFloatPropertyList.AddRange(newAccessoryMaterialFloatPropertyList);
                MaterialKeywordPropertyList.AddRange(newAccessoryMaterialKeywordPropertyList);
                MaterialColorPropertyList.AddRange(newAccessoryMaterialColorPropertyList);
                MaterialTexturePropertyList.AddRange(newAccessoryMaterialTexturePropertyList);
            }
            else
            {
                MaterialEditorPlugin.Logger.LogMessage("Cannot copy renamed materials!");
            }

            PurgeUnusedAnimation();
        }

        /// <summary>
        /// Get the original value of the saved projector property value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="projector">Projector being modified</param>
        /// <param name="property">Property of the projector</param>
        /// <param name="go">GameObject the projector belongs to</param>
        /// <returns>Saved projector property value</returns>
        public float? GetProjectorPropertyValueOriginal(int slot, ObjectType objectType, Projector projector, ProjectorProperties property, GameObject go)
        {
            var valueOriginal = ProjectorPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == property && x.ProjectorName == projector.NameFormatted())?.ValueOriginal;
            if (valueOriginal.IsNullOrEmpty())
                return null;
            return float.Parse(valueOriginal);
        }
        /// <summary>
        /// Get the value of the saved projector property value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="projector">Projector being modified</param>
        /// <param name="property">Property of the projector</param>
        /// <param name="go">GameObject the projector belongs to</param>
        /// <returns>Saved projector property value</returns>
        public float? GetProjectorPropertyValue(int slot, ObjectType objectType, Projector projector, ProjectorProperties property, GameObject go)
        {
            var valueOriginal = ProjectorPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == property && x.ProjectorName == projector.NameFormatted())?.Value;
            if (valueOriginal.IsNullOrEmpty())
                return null;
            return float.Parse(valueOriginal);
        }
        /// <summary>
        /// Remove the saved projector property value if one is saved and optionally also update the projector
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="projector">Projector being modified</param>
        /// <param name="property">Property of the projector</param>
        /// <param name="go">GameObject the projector belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the projector</param>
        public void RemoveProjectorProperty(int slot, ObjectType objectType, Projector projector, ProjectorProperties property, GameObject go, bool setProperty = true)
        {
            if (setProperty)
            {
                var original = GetProjectorPropertyValueOriginal(slot, objectType, projector, property, go);
                if (original != null)
                    MaterialAPI.SetProjectorProperty(go, projector.NameFormatted(), property, (float)original);
            }
            ProjectorPropertyList.RemoveAll(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == property && x.ProjectorName == projector.NameFormatted());
        }
        /// <summary>
        /// Add a renderer property to be saved and loaded with the card and optionally also update the renderer.
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="renderer">Renderer being modified</param>
        /// <param name="property">Property of the renderer</param>
        /// <param name="value">Value</param>
        /// <param name="go">GameObject the renderer belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the renderer</param>
        public void SetProjectorProperty(int slot, ObjectType objectType, Projector projector, ProjectorProperties property, float value, GameObject go, bool setProperty = true)
        {
            ProjectorProperty projectorProperty = ProjectorPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == property && x.ProjectorName == projector.NameFormatted());
            if (projectorProperty == null)
            {
                string valueOriginal = "";
                if (property == ProjectorProperties.FarClipPlane)
                    valueOriginal = projector.farClipPlane.ToString(CultureInfo.InvariantCulture);
                else if (property == ProjectorProperties.NearClipPlane)
                    valueOriginal = projector.nearClipPlane.ToString(CultureInfo.InvariantCulture);
                else if (property == ProjectorProperties.FieldOfView)
                    valueOriginal = projector.fieldOfView.ToString(CultureInfo.InvariantCulture);
                else if (property == ProjectorProperties.AspectRatio)
                    valueOriginal = projector.aspectRatio.ToString(CultureInfo.InvariantCulture);
                else if (property == ProjectorProperties.Orthographic)
                    valueOriginal = Convert.ToSingle(projector.orthographic).ToString(CultureInfo.InvariantCulture);
                else if (property == ProjectorProperties.OrthographicSize)
                    valueOriginal = projector.orthographicSize.ToString(CultureInfo.InvariantCulture);
                else if (property == ProjectorProperties.IgnoreCharaLayer)
                    valueOriginal = Convert.ToSingle(projector.ignoreLayers == (projector.ignoreLayers | (1 << 10))).ToString(CultureInfo.InvariantCulture);
                else if (property == ProjectorProperties.IgnoreMapLayer)
                    valueOriginal = Convert.ToSingle(projector.ignoreLayers == (projector.ignoreLayers | (1 << 11))).ToString(CultureInfo.InvariantCulture);

                if (valueOriginal != "")
                    ProjectorPropertyList.Add(new ProjectorProperty(objectType, GetCoordinateIndex(objectType), slot, projector.NameFormatted(), property, value.ToString(CultureInfo.InvariantCulture), valueOriginal));
            }
            else
            {
                if (value.ToString(CultureInfo.InvariantCulture) == projectorProperty.ValueOriginal)
                    RemoveProjectorProperty(slot, objectType, projector, property, go, false);
                else
                    projectorProperty.Value = value.ToString(CultureInfo.InvariantCulture);
            }

            if (setProperty)
                MaterialAPI.SetProjectorProperty(go, projector.NameFormatted(), property, value);
        }
        /// <summary>
        /// Return a list of projectors attached to the specified object
        /// </summary>
        /// <param name="objectType"></param>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        public IEnumerable<Projector> GetProjectorList(ObjectType objectType, GameObject gameObject)
        {
            //The body will never have a projector component attached
            //And returning all components from children will return every projector on a character
            if (objectType == ObjectType.Character)
                return new List<Projector>();
            return MaterialAPI.GetProjectorList(gameObject, true);
        }

        /// <summary>
        /// Add a renderer property to be saved and loaded with the card and optionally also update the renderer.
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="renderer">Renderer being modified</param>
        /// <param name="property">Property of the renderer</param>
        /// <param name="value">Value</param>
        /// <param name="go">GameObject the renderer belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the renderer</param>
        public void SetRendererProperty(int slot, ObjectType objectType, Renderer renderer, RendererProperties property, string value, GameObject go, bool setProperty = true)
        {
            RendererProperty rendererProperty = RendererPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == property && x.RendererName == renderer.NameFormatted());
            if (rendererProperty == null)
            {
                string valueOriginal = "";
                if (property == RendererProperties.Enabled)
                    valueOriginal = renderer.enabled ? "1" : "0";
                else if (property == RendererProperties.ReceiveShadows)
                    valueOriginal = renderer.receiveShadows ? "1" : "0";
                else if (property == RendererProperties.ShadowCastingMode)
                    valueOriginal = ((int)renderer.shadowCastingMode).ToString();
                else if (property == RendererProperties.UpdateWhenOffscreen)
                    if (renderer is SkinnedMeshRenderer meshRenderer)
                        valueOriginal = meshRenderer.updateWhenOffscreen ? "1" : "0";
                    else valueOriginal = "0";
                else if (property == RendererProperties.RecalculateNormals)
                    valueOriginal = "0"; // this property cannot be set by default

                if (valueOriginal != "")
                    RendererPropertyList.Add(new RendererProperty(objectType, GetCoordinateIndex(objectType), slot, renderer.NameFormatted(), property, value, valueOriginal));
            }
            else
            {
                if (value == rendererProperty.ValueOriginal)
                    RemoveRendererProperty(slot, objectType, renderer, property, go, false);
                else
                    rendererProperty.Value = value;
            }
            if (setProperty)
                MaterialAPI.SetRendererProperty(go, renderer.NameFormatted(), property, value);
        }
        /// <summary>
        /// Get the saved renderer property value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="renderer">Renderer being modified</param>
        /// <param name="property">Property of the renderer</param>
        /// <param name="go">GameObject the renderer belongs to</param>
        /// <returns>Saved renderer property value</returns>
        public string GetRendererPropertyValue(int slot, ObjectType objectType, Renderer renderer, RendererProperties property, GameObject go)
        {
            return RendererPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == property && x.RendererName == renderer.NameFormatted())?.Value;
        }
        /// <summary>
        /// Get the original value of the saved renderer property value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="renderer">Renderer being modified</param>
        /// <param name="property">Property of the renderer</param>
        /// <param name="go">GameObject the renderer belongs to</param>
        /// <returns>Saved renderer property value</returns>
        public string GetRendererPropertyValueOriginal(int slot, ObjectType objectType, Renderer renderer, RendererProperties property, GameObject go)
        {
            return RendererPropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == property && x.RendererName == renderer.NameFormatted())?.ValueOriginal;
        }
        /// <summary>
        /// Remove the saved renderer property value if one is saved and optionally also update the renderer
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="renderer">Renderer being modified</param>
        /// <param name="property">Property of the renderer</param>
        /// <param name="go">GameObject the renderer belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the renderer</param>
        public void RemoveRendererProperty(int slot, ObjectType objectType, Renderer renderer, RendererProperties property, GameObject go, bool setProperty = true)
        {
            if (setProperty)
            {
                var original = GetRendererPropertyValueOriginal(slot, objectType, renderer, property, go);
                if (!original.IsNullOrEmpty())
                    MaterialAPI.SetRendererProperty(go, renderer.NameFormatted(), property, original);
                if (property == RendererProperties.RecalculateNormals)
                    MaterialEditorPlugin.Logger.LogMessage("Save and reload character or change outfits to reset normals.");
            }
            RendererPropertyList.RemoveAll(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Property == property && x.RendererName == renderer.NameFormatted());
        }

        /// <summary>
        /// Add a rename entry to be saved and loaded with the card and optionally also update the materials.
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="renderer">Renderer for which to rename the material</param>
        /// <param name="material">Material being modified</param>
        /// <param name="value">New name for the material</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void SetMaterialNameProperty(int slot, ObjectType objectType, Renderer renderer, Material material, string value, GameObject go, bool setProperty = true)
        {
            var materialProperty = MaterialNamePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Renderer == renderer.NameFormatted() && x.Value == material.name);
            if (materialProperty == null)
            {
                MaterialNamePropertyList.Add(new MaterialNameProperty(objectType, GetCoordinateIndex(objectType), slot, renderer, material, value));
                HandleMaterialNameChange(slot, objectType, renderer, material, value, go);
            }
            else
            {
                if (value.FormatShadingObjectName() == materialProperty.ValueOriginal.FormatShadingObjectName())
                    RemoveMaterialNameProperty(slot, objectType, renderer, material, go, false);
                else
                {
                    materialProperty.Value = value;
                    HandleMaterialNameChange(slot, objectType, renderer, material, value, go);
                }
            }
            if (setProperty)
                MaterialAPI.SetName(go, renderer.NameFormatted(), material.name, value);
        }
        /// <summary>
        /// Remove the saved material property value if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="renderer">Renderer for which to restore the original material name</param>
        /// <param name="material">Material to restore the name for</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void RemoveMaterialNameProperty(int slot, ObjectType objectType, Renderer renderer, Material material, GameObject go, bool setProperty = true)
        {
            if (setProperty)
            {
                var original = GetMaterialNamePropertyValueOriginal(slot, objectType, renderer, material, go);
                if (original != string.Empty)
                    MaterialAPI.SetName(go, renderer.NameFormatted(), material.name, original);
            }
            MaterialNamePropertyList.RemoveAll(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Renderer == renderer.NameFormatted() && x.Value == material.name);
        }
        /// <summary>
        /// Get the saved material property's current name or empty string if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="renderer">Renderer for which to check existence of modified material name</param>
        /// <param name="material">Material to check if it's been renamed</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <returns>Saved material property's current name or empty string if none is saved</returns>
        public string GetMaterialNamePropertyValue(int slot, ObjectType objectType, Renderer renderer, Material material, GameObject go)
        {
            return MaterialNamePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Renderer == (renderer == null ? null : renderer.NameFormatted()) && x.Value == (material == null ? null : material.name))?.Value ?? string.Empty;
        }
        /// <summary>
        /// Get the saved material property's original name or empty string if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="renderer">Renderer for which to check existence of modified material name</param>
        /// <param name="material">Material to check if it's been renamed</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <returns>Saved material property's original value or null if none is saved</returns>
        public string GetMaterialNamePropertyValueOriginal(int slot, ObjectType objectType, Renderer renderer, Material material, GameObject go)
        {
            return MaterialNamePropertyList.FirstOrDefault(x => x.ObjectType == objectType && x.CoordinateIndex == GetCoordinateIndex(objectType) && x.Slot == slot && x.Renderer == (renderer == null ? null : renderer.NameFormatted()) && x.Value == (material == null ? null : material.name))?.ValueOriginal ?? string.Empty;
        }

    }
}
