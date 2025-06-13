using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MaterialEditorAPI
{
    /// <summary>
    /// API for safely setting the properties of materials
    /// </summary>
    public static class MaterialAPI
    {
        /// <summary>
        /// Projector materials share an instance by default, unlike renderers which get a unique one by default.
        /// This list keeps track of every projector and it's new unique material instance
        /// </summary>
        internal static readonly Dictionary<Projector, Material> ProjectorMaterialInstances = new Dictionary<Projector, Material>();

        /// <summary>
        /// Postfix added to the name of a material when copied
        /// </summary>
        public const string MaterialCopyPostfix = ".MECopy";

        /// <summary>
        /// Get a list of all the renderers of a GameObject
        /// </summary>
        public static IEnumerable<Renderer> GetRendererList(GameObject gameObject)
        {
            if (gameObject == null)
                return new List<Renderer>();

            return gameObject.GetComponentsInChildren<Renderer>(true);
        }

        /// <summary>
        /// Get a list of all the projectors of a GameObject
        /// </summary>
        public static IEnumerable<Projector> GetProjectorList(GameObject gameObject, bool inChildren = false)
        {
            if (gameObject == null)
                return new List<Projector>();

            IEnumerable<Projector> projectors;
            if (inChildren)
                projectors = gameObject.GetComponentsInChildren<Projector>(true);
            else
                projectors = gameObject.GetComponents<Projector>();

            //Assign a unique copy of the material if the projector didn't have one already
            foreach (var projector in projectors)
                if (!ProjectorMaterialInstances.ContainsKey(projector))
                {
                    projector.material = new Material(projector.material);
                    ProjectorMaterialInstances[projector] = projector.material;
                }
            return projectors;
        }

        /// <summary>
        /// Get a list of materials for the renderer
        /// </summary>
        /// <param name="gameObject">GameObject to which the renderer belongs</param>
        /// <param name="renderer">Renderer containing the materials</param>
        /// <returns>Array of materials</returns>
#pragma warning disable IDE0060 // Remove unused parameter
        //gameObject may be needed by Harmony patches
        public static IEnumerable<Material> GetMaterials(GameObject gameObject, Renderer renderer) => renderer.materials.Where(x => x != null);
#pragma warning restore IDE0060 // Remove unused parameter

        /// <summary>
        /// Set the material array for a renderer
        /// </summary>
        /// <param name="gameObject">GameObject to which the renderer belongs</param>
        /// <param name="renderer">Renderer containing the materials</param>
        /// <param name="materials">Materials to set</param>
#pragma warning disable IDE0060 // Remove unused parameter
        public static void SetMaterials(GameObject gameObject, Renderer renderer, Material[] materials) => renderer.materials = materials;
#pragma warning restore IDE0060 // Remove unused parameter

        private static List<Material> GetObjectMaterials(GameObject gameObject, string materialName)
        {
            if (gameObject == null)
                return new List<Material>();

            List<Material> materials = new List<Material>();
            foreach (var renderer in GetRendererList(gameObject))
                foreach (var material in GetMaterials(gameObject, renderer))
                    if (material.NameFormatted() == materialName)
                        materials.Add(material);

            foreach (var projector in GetProjectorList(gameObject))
                materials.Add(projector.material);

            return materials;
        }

        /// <summary>
        /// Create a copy of the materials with the specified name for the GameObject
        /// </summary>
        /// <param name="gameObject">GameObject for which to copy materials</param>
        /// <param name="materialName">Name of the material</param>
        /// <param name="materialCopyName">Name for the new copy, leave blank to auto generate a name</param>
        /// <returns>Name of the new copy</returns>
        public static string CopyMaterial(GameObject gameObject, string materialName, string materialCopyName = "")
        {
            string newMatName = materialCopyName;
            foreach (var renderer in GetRendererList(gameObject))
            {
                var materials = GetMaterials(gameObject, renderer);

                if (materials.Any(x => x.NameFormatted() == materialName))
                {
                    List<Material> newMats = new List<Material>();
                    Material originalMat = null;
                    bool copyAdded = false;
                    int copyCount = 0;
                    foreach (var mat in materials)
                    {
                        if (mat.NameFormatted() == materialName)
                        {
                            originalMat = mat;
                            newMats.Add(mat);
                        }
                        else if (mat.NameFormatted().StartsWith(materialName + MaterialCopyPostfix))
                        {
                            if (newMatName != "" && mat.NameFormatted() == newMatName)
                            {
                                //Copy already exists
                                newMats.Add(mat);
                                copyAdded = true;
                            }
                            else
                            {
                                //Find the number of the other copies to avoid ID conflict
                                string copyNumber = mat.NameFormatted().Replace(materialName + MaterialCopyPostfix, "");

                                if (int.TryParse(copyNumber, out int copyNumberInt))
                                    copyCount = copyNumberInt;
                                else
                                    copyCount++;
                                newMats.Add(mat);
                            }
                        }
                        else
                        {
                            newMats.Add(mat);
                        }
                    }

                    if (!copyAdded)
                    {
                        Material newMat = Object.Instantiate(originalMat);
                        if (newMatName == "")
                            newMatName = $"{materialName}{MaterialCopyPostfix}{copyCount + 1}";
                        newMat.name = newMatName;
                        newMats.Add(newMat);
                    }

                    SetMaterials(gameObject, renderer, newMats.ToArray());
                }
            }
            return newMatName;
        }

        /// <summary>
        /// Remove any material copies added by MaterialEditor for the specified GameObject
        /// </summary>
        /// <param name="gameObject">GameObject for which to remove materials</param>
        public static void RemoveMaterialCopies(GameObject gameObject)
        {
            foreach (var renderer in GetRendererList(gameObject))
            {
                var materials = GetMaterials(gameObject, renderer);

                if (materials.Any(x => x != null && x.NameFormatted().Contains(MaterialCopyPostfix)))
                {
                    List<Material> newMats = new List<Material>();
                    foreach (var mat in materials)
                        if (!mat.NameFormatted().Contains(MaterialCopyPostfix))
                            newMats.Add(mat);

                    SetMaterials(gameObject, renderer, newMats.ToArray());
                }
            }
        }

        /// <summary>
        /// Removes a specific material from the specified GameObject's renderers.
        /// </summary>
        /// <param name="gameObject">The GameObject whose renderers will be searched for the material.</param>
        /// <param name="material">The material to be removed.</param>
        public static void RemoveMaterial(GameObject gameObject, Material material)
        {
            foreach (var renderer in GetRendererList(gameObject))
            {
                var materials = GetMaterials(gameObject, renderer);

                if (materials.Any(x => x != null && x.NameFormatted() == material.NameFormatted()))
                {
                    List<Material> newMats = new List<Material>();
                    foreach (var mat in materials)
                        if (mat.NameFormatted() != material.NameFormatted())
                            newMats.Add(mat);

                    SetMaterials(gameObject, renderer, newMats.ToArray());
                }
            }
        }

        /// <summary>
        /// Rename the material specified by GameObject, Renderer name, and Material name
        /// </summary>
        /// <param name="gameObject">GameObject to which the material belongs</param>
        /// <param name="rendererName">Name of the renderer</param>
        /// <param name="materialName">Name of the material</param>
        /// <param name="value">New name of the material</param>
        public static bool SetName(GameObject gameObject, string rendererName, string materialName, string value)
        {
            bool didSet = false;

            foreach (var renderer in GetRendererList(gameObject))
            {
                if (renderer.NameFormatted() == rendererName.FormatShadingObjectName())
                {
                    var materials = GetMaterials(gameObject, renderer).Where(mat => mat.NameFormatted() == materialName.FormatShadingObjectName());
                    foreach (var mat in materials)
                    {
                        mat.name = value;
                        didSet = true;
                    }
                }
            }
            return didSet;
        }

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

            var list = GetObjectMaterials(gameObject, materialName);
            for (var i = 0; i < list.Count; i++)
            {
                var material = list[i];
                if (material.HasProperty($"_{propertyName}"))
                {
                    material.SetFloat($"_{propertyName}", value);
                    didSet = true;
                }
            }
            return didSet;
        }

        /// <summary>
        /// Set the value of the specified material property
        /// </summary>
        /// <param name="gameObject">GameObject to search for the material</param>
        /// <param name="materialName">Name of the material being set</param>
        /// <param name="propertyName">Property of the material being set</param>
        /// <param name="value">Value to be set</param>
        /// <returns>True if the material was found and the value set</returns>
        public static bool SetKeyword(GameObject gameObject, string materialName, string propertyName, bool value)
        {
            bool didSet = false;

            var list = GetObjectMaterials(gameObject, materialName);
            for (var i = 0; i < list.Count; i++)
            {
                var material = list[i];
                if (value == true)
                {
                    material.EnableKeyword($"_{propertyName}");
                }
                else
                {
                    material.DisableKeyword($"_{propertyName}");
                }
                didSet = true; // There's no way to tell if the keyword actually exists, so we'll just have to trust the XML.
            }
            return didSet;
        }

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

            var materials = GetObjectMaterials(gameObject, materialName);
            for (var i = 0; i < materials.Count; i++)
            {
                var material = materials[i];
                if (material.HasProperty($"_{propertyName}"))
                {
                    material.SetColor($"_{propertyName}", value);
                    didSet = true;
                }
            }
            return didSet;
        }

        /// <summary>
        /// Set the value of the specified renderer property
        /// </summary>
        /// <param name="gameObject">GameObject to search for the renderer</param>
        /// <param name="rendererName">Name of the renderer being modified</param>
        /// <param name="propertyName">Property of the renderer being modified</param>
        /// <param name="value">Value to be set</param>
        /// <returns>True if the value was set, false if it could not be set</returns>
        public static bool SetRendererProperty(GameObject gameObject, string rendererName, RendererProperties propertyName, string value) =>
            SetRendererProperty(gameObject, rendererName, propertyName, int.Parse(value));
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
            if (propertyName == RendererProperties.UpdateWhenOffscreen)
                return SetRendererUpdateWhenOffscreen(gameObject, rendererName, value == 1);
            if (propertyName == RendererProperties.RecalculateNormals)
                return SetRendererRecalculateNormals(gameObject, rendererName, value == 1);
            return false;
        }

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
            foreach (var renderer in GetRendererList(gameObject))
            {
                if (renderer.NameFormatted() == rendererName)
                {
                    renderer.enabled = value;
                    didSet = true;
                }
            }
            return didSet;
        }

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
            foreach (var renderer in GetRendererList(gameObject))
            {
                if (renderer.NameFormatted() == rendererName)
                {
                    renderer.shadowCastingMode = value;
                    didSet = true;
                }
            }
            return didSet;
        }

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
            foreach (var renderer in GetRendererList(gameObject))
            {
                if (renderer.NameFormatted() == rendererName)
                {
                    renderer.receiveShadows = value;
                    didSet = true;
                }
            }
            return didSet;
        }

        /// <summary>
        /// Set the ReceiveShadows property of a renderer
        /// </summary>
        /// <param name="gameObject">GameObject to search for the renderer</param>
        /// <param name="rendererName">Name of the renderer being modified</param>
        /// <param name="value">Value to be set</param>
        /// <returns>True if the value was set, false if it could not be set</returns>
        public static bool SetRendererUpdateWhenOffscreen(GameObject gameObject, string rendererName, bool value)
        {
#if !KK
            bool didSet = false;
            foreach (var renderer in GetRendererList(gameObject))
            {
                if (renderer is SkinnedMeshRenderer meshRenderer && renderer.NameFormatted() == rendererName)
                {
                    meshRenderer.updateWhenOffscreen = value;
                    didSet = true;
                }
            }
            return didSet;
#else
            return false;
#endif
        }

        /// <summary>
        /// Recalculates the normals of a SkinnedMeshRenderer's mesh.
        /// </summary>
        /// <param name="gameObject">The GameObject containing the renderer.</param>
        /// <param name="rendererName">The name of the renderer to modify.</param>
        /// <param name="value">If true, recalculates the normals; otherwise, does nothing.</param>
        /// <returns>True if the normals were recalculated; otherwise, false.</returns>
        public static bool SetRendererRecalculateNormals(GameObject gameObject, string rendererName, bool value)
        {
            bool didSet = false;
            foreach (var renderer in GetRendererList(gameObject))
            {
                if (renderer is SkinnedMeshRenderer && renderer.NameFormatted() == rendererName)
                {
                    if (value)
                    {
                        SkinnedMeshRenderer rend = (SkinnedMeshRenderer)renderer;
                        Mesh temp = new Mesh();
                        rend.BakeMesh(temp);
                        temp.RecalculateNormals();
                        Mesh original = Object.Instantiate(rend.sharedMesh);
                        original.normals = temp.normals;
                        rend.sharedMesh = original;
                    }
                    didSet = true;
                }
            }
            return didSet;
        }

        /// <summary>
        /// Set the value of the specified projector property
        /// </summary>
        /// <param name="gameObject">GameObject to search for the projector</param>
        /// <param name="projectorName">Name of the projector being modified</param>
        /// <param name="propertyName">Property of the projector being modified</param>
        /// <param name="value">Value to be set</param>
        /// <returns>True if the value was set, false if it could not be set</returns>
        public static bool SetProjectorProperty(GameObject gameObject, string projectorName, ProjectorProperties propertyName, float value)
        {
            if (propertyName == ProjectorProperties.Enabled)
                return SetProjectorEnabled(gameObject, projectorName, System.Convert.ToBoolean(value));
            else if (propertyName == ProjectorProperties.FarClipPlane)
                return SetProjectorFarClipPlane(gameObject, projectorName, value);
            else if (propertyName == ProjectorProperties.NearClipPlane)
                return SetProjectorNearClipPlane(gameObject, projectorName, value);
            else if (propertyName == ProjectorProperties.FieldOfView)
                return SetProjectorFieldOfView(gameObject, projectorName, value);
            else if (propertyName == ProjectorProperties.AspectRatio)
                return SetProjectorAspectRatio(gameObject, projectorName, value);
            else if (propertyName == ProjectorProperties.Orthographic)
                return SetProjectorOrthographic(gameObject, projectorName, System.Convert.ToBoolean(value));
            else if (propertyName == ProjectorProperties.OrthographicSize)
                return SetProjectorOrthographicSize(gameObject, projectorName, value);
            else if (propertyName == ProjectorProperties.IgnoreCharaLayer)
                return SetProjectorIgnoreLayers(gameObject, projectorName, System.Convert.ToBoolean(value), 10);
            else if (propertyName == ProjectorProperties.IgnoreMapLayer)
                return SetProjectorIgnoreLayers(gameObject, projectorName, System.Convert.ToBoolean(value), 11);
            return false;
        }

        /// <summary>
        /// Set the enabled property of a projector
        /// </summary>
        /// <param name="gameObject">GameObject to search for the renderer</param>
        /// <param name="projectorName">Name of the renderer being modified</param>
        /// <param name="value">Value to be set</param>
        /// <returns>True if the value was set, false if it could not be set</returns>
        public static bool SetProjectorEnabled(GameObject gameObject, string projectorName, bool value)
        {
            foreach (var projector in GetProjectorList(gameObject))
            {
                if (projector.NameFormatted() == projectorName)
                {
                    projector.enabled = value;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Set the farclipPlane property of a projector
        /// </summary>
        /// <param name="gameObject">GameObject to search for the projector</param>
        /// <param name="projectorName">Name of the projector being modified</param>
        /// <param name="value">Value to be set</param>
        /// <returns>True if the value was set, false if it could not be set</returns>
        public static bool SetProjectorFarClipPlane(GameObject gameObject, string projectorName, float value)
        {
            foreach (var projector in GetProjectorList(gameObject))
            {
                if (projector.NameFormatted() == projectorName)
                {
                    projector.farClipPlane = value;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Set the nearClipPlane property of a projector
        /// </summary>
        /// <param name="gameObject">GameObject to search for the projector</param>
        /// <param name="projectorName">Name of the projector being modified</param>
        /// <param name="value">Value to be set</param>
        /// <returns>True if the value was set, false if it could not be set</returns>
        public static bool SetProjectorNearClipPlane(GameObject gameObject, string projectorName, float value)
        {
            foreach (var projector in GetProjectorList(gameObject))
            {
                if (projector.NameFormatted() == projectorName)
                {
                    projector.nearClipPlane = value;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Set the fieldOfView property of a projector
        /// </summary>
        /// <param name="gameObject">GameObject to search for the projector</param>
        /// <param name="projectorName">Name of the projector being modified</param>
        /// <param name="value">Value to be set</param>
        /// <returns>True if the value was set, false if it could not be set</returns>
        public static bool SetProjectorFieldOfView(GameObject gameObject, string projectorName, float value)
        {
            foreach (var projector in GetProjectorList(gameObject))
            {
                if (projector.NameFormatted() == projectorName)
                {
                    projector.fieldOfView = value;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Set the aspectRatio property of a projector
        /// </summary>
        /// <param name="gameObject">GameObject to search for the projector</param>
        /// <param name="projectorName">Name of the projector being modified</param>
        /// <param name="value">Value to be set</param>
        /// <returns>True if the value was set, false if it could not be set</returns>
        public static bool SetProjectorAspectRatio(GameObject gameObject, string projectorName, float value)
        {
            foreach (var projector in GetProjectorList(gameObject))
            {
                if (projector.NameFormatted() == projectorName)
                {
                    projector.aspectRatio = value;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Set the orthographic property of a projector
        /// </summary>
        /// <param name="gameObject">GameObject to search for the projector</param>
        /// <param name="projectorName">Name of the projector being modified</param>
        /// <param name="value">Value to be set</param>
        /// <returns>True if the value was set, false if it could not be set</returns>
        public static bool SetProjectorOrthographic(GameObject gameObject, string projectorName, bool value)
        {
            foreach (var projector in GetProjectorList(gameObject))
            {
                if (projector.NameFormatted() == projectorName)
                {
                    projector.orthographic = value;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Set the orthoGraphicSize property of a projector
        /// </summary>
        /// <param name="gameObject">GameObject to search for the projector</param>
        /// <param name="projectorName">Name of the projector being modified</param>
        /// <param name="value">Value to be set</param>
        /// <returns>True if the value was set, false if it could not be set</returns>
        public static bool SetProjectorOrthographicSize(GameObject gameObject, string projectorName, float value)
        {
            foreach (var projector in GetProjectorList(gameObject))
            {
                if (projector.NameFormatted() == projectorName)
                {
                    projector.orthographicSize = value;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Add or remove the given layer to/from the ignored layers property.
        /// Ensures it's not removed/added that was already the case
        /// </summary>
        /// <param name="gameObject">GameObject to search for the projector</param>
        /// <param name="projectorName">Name of the projector being modified</param>
        /// <param name="ignore">If the given layer should be ignored or not</param>
        /// <param name="layer">The index of the layer to be added/removed</param>
        /// <returns>True if the value was set, false if it could not be set</returns>
        public static bool SetProjectorIgnoreLayers(GameObject gameObject, string projectorName, bool ignore, int layer)
        {
            foreach (var projector in GetProjectorList(gameObject))
            {
                if (projector.NameFormatted() == projectorName)
                {
                    bool inMask = projector.ignoreLayers == (projector.ignoreLayers | (1 << layer));
                    //Remove layer from mask
                    if (inMask && !ignore)
                        projector.ignoreLayers &= ~(1 << layer);
                    //Add layer to mask
                    else if (!inMask && ignore)
                        projector.ignoreLayers |= (1 << layer);
                }
            }
            return false;
        }

        /// <summary>
        /// Set the texture property of a material
        /// </summary>
        /// <param name="gameObject">GameObject to search for the renderer</param>
        /// <param name="materialName">Name of the material being modified</param>
        /// <param name="propertyName">Property of the material being set</param>
        /// <param name="value">Value to be set</param>
        /// <returns>True if the value was set, false if it could not be set</returns>
        public static bool SetTexture(GameObject gameObject, string materialName, string propertyName, Texture value)
        {
            bool didSet = false;
            var materials = GetObjectMaterials(gameObject, materialName);
            for (var i = 0; i < materials.Count; i++)
            {
                var material = materials[i];
                if (material.HasProperty($"_{propertyName}"))
                {
                    if (value != null)
                    {
                        var tex = material.GetTexture($"_{propertyName}");
                        if (tex == null)
                            value.wrapMode = TextureWrapMode.Repeat;
                        else
                            value.wrapMode = tex.wrapMode;
                    }
                    material.SetTexture($"_{propertyName}", value);
                    didSet = true;
                }
            }
            return didSet;
        }

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

            var materials = GetObjectMaterials(gameObject, materialName);
            for (var i = 0; i < materials.Count; i++)
            {
                var material = materials[i];
                if (material.HasProperty($"_{propertyName}"))
                {
                    material.SetTextureOffset($"_{propertyName}", (Vector2)value);
                    didSet = true;
                }
            }
            return didSet;
        }

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

            var materials = GetObjectMaterials(gameObject, materialName);
            for (var i = 0; i < materials.Count; i++)
            {
                var material = materials[i];
                if (material.HasProperty($"_{propertyName}"))
                {
                    material.SetTextureScale($"_{propertyName}", (Vector2)value);
                    didSet = true;
                }
            }
            return didSet;
        }

        /// <summary>
        /// Set the shader of a material. Can only be set to a shader that has been loaded by MaterialEditor.
        /// </summary>
        /// <param name="gameObject">The GameObject to search for the renderer.</param>
        /// <param name="materialName">The name of the material being modified.</param>
        /// <param name="shaderName">The name of the shader to be set.</param>
        /// <param name="preserveRenderQueue">Indicates whether to preserve the render queue of the material (if it's not specified in shaderData).</param>
        /// <returns>True if the shader was successfully set; otherwise, false.</returns>
        public static bool SetShader(GameObject gameObject, string materialName, string shaderName, bool preserveRenderQueue = false)
        {
            bool didSet = false;
            if (shaderName.IsNullOrEmpty()) return false;
            MaterialEditorPluginBase.LoadedShaders.TryGetValue(shaderName, out var shaderData);

            if (shaderData?.Shader == null)
            {
                MaterialEditorPluginBase.Logger.Log(BepInEx.Logging.LogLevel.Warning | BepInEx.Logging.LogLevel.Message, $"Could not load shader:{shaderName}");
                return false;
            }
            if (!MaterialEditorPluginBase.XMLShaderProperties.TryGetValue(shaderName, out var shaderPropertyDataList))
                shaderPropertyDataList = new Dictionary<string, MaterialEditorPluginBase.ShaderPropertyData>();

            var materials = GetObjectMaterials(gameObject, materialName);
            for (var i = 0; i < materials.Count; i++)
            {
                var material = materials[i];
                int renderQueue = material.renderQueue;
                material.shader = shaderData.Shader;

                if (shaderData.RenderQueue != null)
                    material.renderQueue = (int)shaderData.RenderQueue;
                else if (preserveRenderQueue)
                    material.renderQueue = renderQueue;

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
                                    var tex = LoadShaderDefaultTexture(shaderPropertyData.DefaultValueAssetBundle, shaderPropertyData.DefaultValue);
                                    SetTexture(gameObject, materialName, shaderPropertyData.Name, tex);
                                }
                                catch
                                {
                                    MaterialEditorPluginBase.Logger.LogWarning($"Could not load default texture:{shaderPropertyData.DefaultValueAssetBundle}:{shaderPropertyData.DefaultValue}");
                                }
                                break;
                            case ShaderPropertyType.Keyword:
                                SetKeyword(gameObject, materialName, shaderPropertyData.Name, bool.Parse(shaderPropertyData.DefaultValue));
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
        /// <param name="gameObject">GameObject to search for the renderer</param>
        /// <param name="materialName">Name of the material being modified</param>
        /// <param name="value">Value to be set</param>
        /// <returns>True if the value was set, false if it could not be set</returns>
        public static bool SetRenderQueue(GameObject gameObject, string materialName, int? value)
        {
            bool didSet = false;
            if (value == null) return false;

            var list = GetObjectMaterials(gameObject, materialName);
            for (var i = 0; i < list.Count; i++)
            {
                var material = list[i];
                material.renderQueue = (int)value;
                didSet = true;
            }
            return didSet;
        }

        private static Texture2D LoadShaderDefaultTexture(string assetBundlePath, string assetPath)
        {
            AssetBundle bundle = AssetBundle.LoadFromFile(assetBundlePath);
            Texture2D tex = bundle.LoadAsset<Texture2D>(assetPath);
            bundle.Unload(false);
            return tex;
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
            Float,
            /// <summary>
            /// Bool
            /// </summary>
            Keyword
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
            /// Whether the renderer will receive shadows cast by other objects
            /// </summary>
            ReceiveShadows,
            /// <summary>
            /// Whether the renderer should recalculate the normals on itself
            /// </summary>
            RecalculateNormals,
            /// <summary>
            /// Whether the renderer updates while off-screen
            /// </summary>
            UpdateWhenOffscreen
        }

        /// <summary>
        /// Properties of a projector that can be set
        /// </summary>
        public enum ProjectorProperties
        {
            /// <summary>
            /// Whether the projector is enabled
            /// </summary>
            Enabled,
            /// <summary>
            /// Near clip plane to start projecting
            /// </summary>
            NearClipPlane,
            /// <summary>
            /// Far clip plane to stop projecting
            /// </summary>
            FarClipPlane,
            /// <summary>
            /// Field of View of projection
            /// </summary>
            FieldOfView,
            /// <summary>
            /// Aspect ratio of the projection
            /// </summary>
            AspectRatio,
            /// <summary>
            /// Whether the projector should project in orthographic mode
            /// </summary>
            Orthographic,
            /// <summary>
            /// The size of the orthographic projection
            /// </summary>
            OrthographicSize,
            /// <summary>
            /// If the projector should ignore objects on the chara layer
            /// </summary>
            IgnoreCharaLayer,
            /// <summary>
            /// If the projector should ignore objects on the map layer
            /// </summary>
            IgnoreMapLayer
        }
    }
}
