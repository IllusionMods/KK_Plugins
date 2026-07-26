using KKAPI.Studio.SaveLoad;
using MaterialEditorAPI;
using Studio;
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
    using MEAnimationController = MEAnimationController<SceneController, SceneController.MaterialTextureProperty>;

    public partial class SceneController
    {
        /// <summary>
        /// Copy any edits for the specified object
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="projector">Projector being modified</param>
        public void MaterialCopyEdits(int id, Material material)
        {
            CopyData.ClearAll();

            for (var i = 0; i < MaterialShaderList.Count; i++)
            {
                var materialShader = MaterialShaderList[i];
                if (materialShader.ID == id && materialShader.MaterialName == material.NameFormatted())
                    CopyData.MaterialShaderList.Add(new CopyContainer.MaterialShader(materialShader.ShaderName, materialShader.RenderQueue));
            }
            for (var i = 0; i < MaterialFloatPropertyList.Count; i++)
            {
                var materialFloatProperty = MaterialFloatPropertyList[i];
                if (materialFloatProperty.ID == id && materialFloatProperty.MaterialName == material.NameFormatted())
                    CopyData.MaterialFloatPropertyList.Add(new CopyContainer.MaterialFloatProperty(materialFloatProperty.Property, float.Parse(materialFloatProperty.Value)));
            }
            for (var i = 0; i < MaterialKeywordPropertyList.Count; i++)
            {
                var materialKeywordProperty = MaterialKeywordPropertyList[i];
                if (materialKeywordProperty.ID == id && materialKeywordProperty.MaterialName == material.NameFormatted())
                    CopyData.MaterialKeywordPropertyList.Add(new CopyContainer.MaterialKeywordProperty(materialKeywordProperty.Property, materialKeywordProperty.Value));
            }
            for (var i = 0; i < MaterialColorPropertyList.Count; i++)
            {
                var materialColorProperty = MaterialColorPropertyList[i];
                if (materialColorProperty.ID == id && materialColorProperty.MaterialName == material.NameFormatted())
                    CopyData.MaterialColorPropertyList.Add(new CopyContainer.MaterialColorProperty(materialColorProperty.Property, materialColorProperty.Value));
            }
            for (var i = 0; i < MaterialTexturePropertyList.Count; i++)
            {
                var materialTextureProperty = MaterialTexturePropertyList[i];
                if (materialTextureProperty.ID == id && materialTextureProperty.MaterialName == material.NameFormatted())
                {
                    if (materialTextureProperty.TexID != null)
                        CopyData.MaterialTexturePropertyList.Add(new CopyContainer.MaterialTextureProperty(materialTextureProperty.Property, TextureDictionary[(int)materialTextureProperty.TexID].Data, materialTextureProperty.Offset, materialTextureProperty.Scale));
                    else
                        CopyData.MaterialTexturePropertyList.Add(new CopyContainer.MaterialTextureProperty(materialTextureProperty.Property, null, materialTextureProperty.Offset, materialTextureProperty.Scale));
                }
            }

            if (GetProjectorList(GetObjectByID(id)).FirstOrDefault(x => x.material == material) != null)
                for (var i = 0; i < ProjectorPropertyList.Count; i++)
                {
                    var projectorProperty = ProjectorPropertyList[i];
                    if (projectorProperty.ID == id)
                        CopyData.ProjectorPropertyList.Add(new CopyContainer.ProjectorProperty(projectorProperty.Property, float.Parse(projectorProperty.Value)));
                }
        }
        /// <summary>
        /// Paste any edits for the specified object
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        /// <param name="projector">Projector being modified</param>
        public void MaterialPasteEdits(int id, Material material, bool setProperty = true)
        {
            for (var i = 0; i < CopyData.MaterialShaderList.Count; i++)
            {
                var materialShader = CopyData.MaterialShaderList[i];
                if (materialShader.ShaderName != null)
                    SetMaterialShader(id, material, materialShader.ShaderName, setProperty);
                if (materialShader.RenderQueue != null)
                    SetMaterialShaderRenderQueue(id, material, (int)materialShader.RenderQueue, setProperty);
            }
            for (var i = 0; i < CopyData.MaterialFloatPropertyList.Count; i++)
            {
                var materialFloatProperty = CopyData.MaterialFloatPropertyList[i];
                if (material.HasProperty($"_{materialFloatProperty.Property}"))
                    SetMaterialFloatProperty(id, material, materialFloatProperty.Property, materialFloatProperty.Value, setProperty);
            }
            for (var i = 0; i < CopyData.MaterialKeywordPropertyList.Count; i++)
            {
                var materialKeywordProperty = CopyData.MaterialKeywordPropertyList[i];
                SetMaterialKeywordProperty(id, material, materialKeywordProperty.Property, materialKeywordProperty.Value, setProperty);
            }
            for (var i = 0; i < CopyData.MaterialColorPropertyList.Count; i++)
            {
                var materialColorProperty = CopyData.MaterialColorPropertyList[i];
                if (material.HasProperty($"_{materialColorProperty.Property}"))
                    SetMaterialColorProperty(id, material, materialColorProperty.Property, materialColorProperty.Value, setProperty);
            }
            for (var i = 0; i < CopyData.MaterialTexturePropertyList.Count; i++)
            {
                var materialTextureProperty = CopyData.MaterialTexturePropertyList[i];
                if (material.HasProperty($"_{materialTextureProperty.Property}"))
                    SetMaterialTexture(id, material, materialTextureProperty.Property, materialTextureProperty.Data);
                if (materialTextureProperty.Offset != null)
                    SetMaterialTextureOffset(id, material, materialTextureProperty.Property, (Vector2)materialTextureProperty.Offset, setProperty);
                if (materialTextureProperty.Scale != null)
                    SetMaterialTextureScale(id, material, materialTextureProperty.Property, (Vector2)materialTextureProperty.Scale, setProperty);
            }

            var projector = GetProjectorList(GetObjectByID(id)).FirstOrDefault(x => x.material == material);
            if (projector != null)
                for (var i = 0; i < CopyData.ProjectorPropertyList.Count; i++)
                {
                    var projectorProperty = CopyData.ProjectorPropertyList[i];
                    SetProjectorProperty(id, projector, projectorProperty.Property, projectorProperty.Value, setProperty);
                }
        }
        public void MaterialCopyRemove(int id, Material material, GameObject go, bool setProperty = true)
        {
            string matName = material.NameFormatted();
            if (matName.Contains(MaterialCopyPostfix))
            {
                MaterialNamePropertyList.RemoveAll(x => x.ID == id && x.Value == material.name);

                RemoveMaterial(go, material);
                MaterialShaderList.RemoveAll(x => x.ID == id && x.MaterialName == matName);
                MaterialFloatPropertyList.RemoveAll(x => x.ID == id && x.MaterialName == matName);
                MaterialKeywordPropertyList.RemoveAll(x => x.ID == id && x.MaterialName == matName);
                MaterialColorPropertyList.RemoveAll(x => x.ID == id && x.MaterialName == matName);
                MaterialTexturePropertyList.RemoveAll(x => x.ID == id && x.MaterialName == matName);
                MaterialCopyList.RemoveAll(x => x.ID == id && x.MaterialCopyName == matName);
            }
            else if (GetMaterialNamePropertyValue(id, GetRendererList(go).FirstOrDefault(x => x.materials.Contains(material)), material) == string.Empty)
            {
                string newMatName = CopyMaterial(go, matName);
                MaterialCopyList.Add(new MaterialCopy(id, matName, newMatName));

                List<MaterialShader> newAccessoryMaterialShaderList = new List<MaterialShader>();
                List<MaterialFloatProperty> newAccessoryMaterialFloatPropertyList = new List<MaterialFloatProperty>();
                List<MaterialKeywordProperty> newAccessoryMaterialKeywordPropertyList = new List<MaterialKeywordProperty>();
                List<MaterialColorProperty> newAccessoryMaterialColorPropertyList = new List<MaterialColorProperty>();
                List<MaterialTextureProperty> newAccessoryMaterialTexturePropertyList = new List<MaterialTextureProperty>();

                foreach (var property in MaterialShaderList.Where(x => x.ID == id && x.MaterialName == matName))
                    newAccessoryMaterialShaderList.Add(new MaterialShader(id, newMatName, property.ShaderName, property.ShaderNameOriginal, property.RenderQueue, property.RenderQueueOriginal));
                foreach (var property in MaterialFloatPropertyList.Where(x => x.ID == id && x.MaterialName == matName))
                    newAccessoryMaterialFloatPropertyList.Add(new MaterialFloatProperty(id, newMatName, property.Property, property.Value, property.ValueOriginal));
                foreach (var property in MaterialKeywordPropertyList.Where(x => x.ID == id && x.MaterialName == matName))
                    newAccessoryMaterialKeywordPropertyList.Add(new MaterialKeywordProperty(id, newMatName, property.Property, property.Value, property.ValueOriginal));
                foreach (var property in MaterialColorPropertyList.Where(x => x.ID == id && x.MaterialName == matName))
                    newAccessoryMaterialColorPropertyList.Add(new MaterialColorProperty(id, newMatName, property.Property, property.Value, property.ValueOriginal));
                foreach (var property in MaterialTexturePropertyList.Where(x => x.ID == id && x.MaterialName == matName))
                    newAccessoryMaterialTexturePropertyList.Add(new MaterialTextureProperty(id, newMatName, property.Property, property.TexID, property.Offset, property.OffsetOriginal, property.Scale, property.ScaleOriginal, property.TexAnimationDef));

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
        /// Get the saved renderer property's original value or null if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="projector">Renderer being modified</param>
        /// <param name="property">Property of the renderer</param>
        /// <returns>Saved renderer property's original value</returns>
        public float? GetProjectorPropertyValueOriginal(int id, Projector projector, ProjectorProperties property)
        {
            var valueOriginal = ProjectorPropertyList.FirstOrDefault(x => x.ID == id && x.Property == property && x.ProjectorName == projector.NameFormatted())?.ValueOriginal;
            if (valueOriginal.IsNullOrEmpty())
                return null;
            return float.Parse(valueOriginal);
        }

        /// <summary>
        /// Get the saved projector property value or null if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="projector">Projector being modified</param>
        /// <param name="property">Property of the projector</param>
        /// <returns>Saved projector property value</returns>
        public float? GetProjectorPropertyValue(int id, Projector projector, ProjectorProperties property)
        {
            var valueOriginal = ProjectorPropertyList.FirstOrDefault(x => x.ID == id && x.Property == property && x.ProjectorName == projector.NameFormatted())?.Value;
            if (valueOriginal.IsNullOrEmpty())
                return null;
            return float.Parse(valueOriginal);
        }

        /// <summary>
        /// Remove the saved projector property value if one is saved and optionally also update the projector
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="projector">projector being modified</param>
        /// <param name="property">Property of the projector</param>
        /// <param name="setProperty">Whether to also apply the value to the projector</param>
        public void RemoveProjectorProperty(int id, Projector projector, ProjectorProperties property, bool setProperty = true)
        {
            GameObject go = GetObjectByID(id);
            if (setProperty)
            {
                var original = GetProjectorPropertyValueOriginal(id, projector, property);
                if (original != null)
                {
                    MaterialAPI.SetProjectorProperty(go, projector.NameFormatted(), property, (float)original);
                }
            }

            ProjectorPropertyList.RemoveAll(x => x.ID == id && x.Property == property && x.ProjectorName == projector.NameFormatted());
        }

        public void SetProjectorProperty(int id, Projector projector, ProjectorProperties property, float value, bool setProperty = true)
        {
            GameObject go = GetObjectByID(id);
            var projectorProperty = ProjectorPropertyList.FirstOrDefault(x => x.ID == id && x.Property == property && x.ProjectorName == projector.NameFormatted());
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
                    ProjectorPropertyList.Add(new ProjectorProperty(id, projector.NameFormatted(), property, value.ToString(CultureInfo.InvariantCulture), valueOriginal));
            }
            else
            {
                if (value.ToString(CultureInfo.InvariantCulture) == projectorProperty.ValueOriginal)
                    RemoveProjectorProperty(id, projector, property, false);
                else
                    projectorProperty.Value = value.ToString(CultureInfo.InvariantCulture);
            }

            if (setProperty)
                MaterialAPI.SetProjectorProperty(go, projector.NameFormatted(), property, value);
        }

        public IEnumerable<Projector> GetProjectorList(GameObject gameObject)
        {
            //Assume the projector component will always be attached to the root object
            //Otherwise no distinction can be made between projectors and editing them will not work properly
            return MaterialAPI.GetProjectorList(gameObject, false);
        }

        /// <summary>
        /// Add a renderer property to be saved and loaded with the scene and optionally also update the renderer.
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="renderer">Renderer being modified</param>
        /// <param name="property">Property of the renderer</param>
        /// <param name="value">Value</param>
        /// <param name="setProperty">Whether to also apply the value to the renderer</param>
        public void SetRendererProperty(int id, Renderer renderer, RendererProperties property, string value, bool setProperty = true)
        {
            GameObject go = GetObjectByID(id);
            var rendererProperty = RendererPropertyList.FirstOrDefault(x => x.ID == id && x.Property == property && x.RendererName == renderer.NameFormatted());
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
                    RendererPropertyList.Add(new RendererProperty(id, renderer.NameFormatted(), property, value, valueOriginal));
            }
            else
            {
                if (value == rendererProperty.ValueOriginal)
                    RemoveRendererProperty(id, renderer, property, false);
                else
                    rendererProperty.Value = value;
            }

            if (setProperty)
                MaterialAPI.SetRendererProperty(go, renderer.NameFormatted(), property, value);
        }

        /// <summary>
        /// Get the saved renderer property value or null if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="renderer">Renderer being modified</param>
        /// <param name="property">Property of the renderer</param>
        /// <returns>Saved renderer property value</returns>
        public string GetRendererPropertyValue(int id, Renderer renderer, RendererProperties property) =>
            RendererPropertyList.FirstOrDefault(x => x.ID == id && x.Property == property && x.RendererName == renderer.NameFormatted())?.Value;
        /// <summary>
        /// Get the saved renderer property's original value or null if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="renderer">Renderer being modified</param>
        /// <param name="property">Property of the renderer</param>
        /// <returns>Saved renderer property's original value</returns>
        public string GetRendererPropertyValueOriginal(int id, Renderer renderer, RendererProperties property) =>
            RendererPropertyList.FirstOrDefault(x => x.ID == id && x.Property == property && x.RendererName == renderer.NameFormatted())?.ValueOriginal;
        /// <summary>
        /// Remove the saved renderer property value if one is saved and optionally also update the renderer
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="renderer">Renderer being modified</param>
        /// <param name="property">Property of the renderer</param>
        /// <param name="setProperty">Whether to also apply the value to the renderer</param>
        public void RemoveRendererProperty(int id, Renderer renderer, RendererProperties property, bool setProperty = true)
        {
            GameObject go = GetObjectByID(id);
            if (setProperty)
            {
                var original = GetRendererPropertyValueOriginal(id, renderer, property);
                if (!original.IsNullOrEmpty())
                    MaterialAPI.SetRendererProperty(go, renderer.NameFormatted(), property, original);
                if (property == RendererProperties.RecalculateNormals)
                    MaterialEditorPlugin.Logger.LogMessage("Save and reload character or change outfits to reset normals.");
            }

            RendererPropertyList.RemoveAll(x => x.ID == id && x.Property == property && x.RendererName == renderer.NameFormatted());
        }

        /// <summary>
        /// Add a name property to be saved and loaded with the scene and optionally also update the materials.
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="renderer">Renderer being modified</param>
        /// <param name="material">Material being renamed</param>
        /// <param name="value">New name for the material</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void SetMaterialNameProperty(int id, Renderer renderer, Material material, string value, bool setProperty = true)
        {
            GameObject go = GetObjectByID(id);
            var materialProperty = MaterialNamePropertyList.FirstOrDefault(x => x.ID == id && x.Renderer == renderer.NameFormatted() && x.Value == material.name);
            if (materialProperty == null)
            {
                MaterialNamePropertyList.Add(new MaterialNameProperty(id, renderer, material, value));
                HandleMaterialNameChange(id, renderer, material, value, go);
            }
            else
            {
                if (value.FormatShadingObjectName() == materialProperty.MaterialName.FormatShadingObjectName())
                    RemoveMaterialNameProperty(id, renderer, material, false);
                else
                {
                    materialProperty.Value = value;
                    HandleMaterialNameChange(id, renderer, material, value, go);
                }
            }

            if (setProperty)
                MaterialAPI.SetName(go, renderer.NameFormatted(), material.name, value);
        }
        /// <summary>
        /// Get the saved material name or an empty string if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="renderer">Renderer that the material belongs to</param>
        /// <param name="material">Material to check for existing name property</param>
        /// <returns>Saved material name or empty string if none is saved</returns>
        public string GetMaterialNamePropertyValue(int id, Renderer renderer, Material material)
        {
            return MaterialNamePropertyList.FirstOrDefault(x => x.ID == id && x.Renderer == renderer?.NameFormatted() && x.Value == material?.name)?.Value ?? string.Empty;
        }
        /// <summary>
        /// Get the original material name or an empty string if the material isn't renamed
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="renderer">Renderer that the material belongs to</param>
        /// <param name="material">Material to check for an original name</param>
        /// <returns>Original material name or empty string if the material isn't renamed</returns>
        public string GetMaterialNamePropertyValueOriginal(int id, Renderer renderer, Material material)
        {
            return MaterialNamePropertyList.FirstOrDefault(x => x.ID == id && x.Renderer == renderer?.NameFormatted() && x.Value == material?.name)?.ValueOriginal ?? string.Empty;
        }
        /// <summary>
        /// Remove the saved material name property if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="renderer">Renderer that the material belongs to</param>
        /// <param name="material">Material to check for an original name</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void RemoveMaterialNameProperty(int id, Renderer renderer, Material material, bool setProperty = true)
        {
            GameObject go = GetObjectByID(id);
            if (setProperty)
            {
                var original = GetMaterialNamePropertyValueOriginal(id, renderer, material);
                if (original != string.Empty)
                {
                    MaterialAPI.SetName(go, renderer.NameFormatted(), material.name, original);
                }
            }

            MaterialNamePropertyList.RemoveAll(x => x.ID == id && x.Renderer == renderer.NameFormatted() && x.Value == material.name);
        }

    }
}
