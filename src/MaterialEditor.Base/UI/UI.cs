using BepInEx;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static MaterialEditorAPI.MaterialAPI;
using static MaterialEditorAPI.MaterialEditorPluginBase;

namespace MaterialEditorAPI
{
    /// <summary>
    /// Code for the MaterialEditor UI
    /// </summary>
#pragma warning disable BepInEx001 // Class inheriting from BaseUnityPlugin missing BepInPlugin attribute
    public abstract class MaterialEditorUI : BaseUnityPlugin
#pragma warning restore BepInEx001 // Class inheriting from BaseUnityPlugin missing BepInPlugin attribute
    {
        /// <summary>
        /// Element containing the entire UI
        /// </summary>
        public static Canvas MaterialEditorWindow;
        /// <summary>
        /// Main panel
        /// </summary>
        public static Image MaterialEditorMainPanel;
        /// <summary>
        /// Draggable header
        /// </summary>
        public static Image DragPanel;
        private static readonly MaterialEditorSessionState Session = new MaterialEditorSessionState();
        private static MaterialEditorWindowView ActiveView;

        private MaterialEditorWindowView _windowView;
        private MaterialEditorSelectionController _selectionController;
        private MaterialEditorPresenter _presenter;

        private static readonly List<Action<MaterialEditorLabelClickEventArgs>> LabelClickHandlers = new List<Action<MaterialEditorLabelClickEventArgs>>();

        internal static FileSystemWatcher TexChangeWatcher;
        private VirtualList VirtualList;

        internal const float MarginSize = MaterialEditorLayout.Margin;
        internal const float HeaderSize = MaterialEditorLayout.HeaderHeight;
        internal const float ScrollOffsetX = MaterialEditorLayout.ScrollbarOffset;
        internal const float PanelHeight = MaterialEditorLayout.RowHeight;

        #region Entry Item Width
        // General
        internal const float LabelWidth = MaterialEditorLayout.LabelWidth;
        internal const float ButtonWidth = MaterialEditorLayout.ButtonWidth;
        internal const float SmallButtonWidth = MaterialEditorLayout.SmallButtonWidth;
        internal const float ResetButtonWidth = MaterialEditorLayout.ResetButtonWidth;
        internal const float InterpolableButtonWidth = MaterialEditorLayout.InterpolableButtonWidth;
        internal const float ContentFullWidth = MaterialEditorLayout.ContentWidth;
        // Renderer (Enbale/ShadowCastingMode/ReceiveShadows/RendererUpdateWhenOffscreen/RecalulateNormals)
        internal const float RendererButtonWidth = MaterialEditorLayout.RendererButtonWidth;
        internal const float RendererToggleWidth = MaterialEditorLayout.RendererToggleWidth;
        internal const float RendererDropdownWidth = MaterialEditorLayout.RendererDropdownWidth;
        // Material
        internal const float MaterialButtonWidth = MaterialEditorLayout.MaterialButtonWidth;
        internal const float MaterialRenameButtonWidth = MaterialEditorLayout.MaterialRenameButtonWidth;
        // Shader
        internal const float ShaderDropdownWidth = MaterialEditorLayout.ShaderDropdownWidth;
        // RenderQueue
        internal const float RenderQueueInputFieldWidth = MaterialEditorLayout.RenderQueueInputWidth;
        // Texture
        internal const float TextureButtonWidth = ContentFullWidth / 2f;
        // Texture Offset and Scale
        internal const float OffsetScaleLabelXWidth = MaterialEditorLayout.OffsetScaleLabelXWidth;
        internal const float OffsetScaleLabelYWidth = MaterialEditorLayout.OffsetScaleLabelYWidth;
        internal const float OffsetScaleInputFieldWidth = MaterialEditorLayout.OffsetScaleInputWidth;
        // Color
        internal const float ColorLabelWidth = MaterialEditorLayout.ColorLabelWidth;
        internal const float ColorInputFieldWidth = MaterialEditorLayout.ColorInputWidth;
        internal const float ColorEditButtonWidth = MaterialEditorLayout.ColorEditButtonWidth;
        // Float
        internal const float FloatSliderWidth = MaterialEditorLayout.FloatSliderWidth;
        internal const float FloatInputFieldWidth = MaterialEditorLayout.FloatInputWidth;
        // Keyword
        internal const float KeywordToggleWidth = MaterialEditorLayout.KeywordToggleWidth;
        #endregion

        internal static RectOffset Padding => MaterialEditorLayout.RowPadding;

        #region Colors
        internal static readonly Color RowColor = MaterialEditorStyles.RowColor;
        internal static readonly Color RendererColor = MaterialEditorStyles.RendererColor;
        internal static readonly Color MaterialColor = MaterialEditorStyles.MaterialColor;
        internal static readonly Color CategoryColor = MaterialEditorStyles.CategoryColor;
        internal static readonly Color ItemColor = MaterialEditorStyles.TransparentRowColor;
        internal static readonly Color ItemColorChanged = MaterialEditorStyles.ChangedRowColor;
        #endregion

        private protected IMaterialEditorColorPalette ColorPalette;

        internal GameObject CurrentGameObject
        {
            get => Session.CurrentGameObject;
            set => Session.CurrentGameObject = value;
        }

        internal object CurrentData
        {
            get => Session.CurrentData;
            set => Session.CurrentData = value;
        }

        private MaterialEditService _materialEditService;
        private static string CurrentFilter
        {
            get => Session.Filter;
            set => Session.Filter = value;
        }

        internal static SelectedInterpolable selectedInterpolable;
        internal static SelectedProjectorInterpolable selectedProjectorInterpolable;

        private protected MaterialEditService EditService =>
            _materialEditService ?? (_materialEditService = CreateMaterialEditService());

        private protected virtual MaterialEditService CreateMaterialEditService() =>
            new MaterialEditService(new LegacyMaterialEditRepository(this));

        /// <summary>
        /// Register a callback for clicks on renderer, material, shader, and property labels.
        /// Registering the same callback more than once has no effect.
        /// </summary>
        /// <param name="handler">Callback invoked with the current Material Editor context.</param>
        public static void RegisterLabelClickHandler(Action<MaterialEditorLabelClickEventArgs> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            if (!LabelClickHandlers.Contains(handler))
                LabelClickHandlers.Add(handler);
        }

        /// <summary>
        /// Unregister a callback previously registered with <see cref="RegisterLabelClickHandler"/>.
        /// </summary>
        /// <param name="handler">Callback to remove.</param>
        public static void UnregisterLabelClickHandler(Action<MaterialEditorLabelClickEventArgs> handler)
        {
            if (handler == null)
                return;
            LabelClickHandlers.Remove(handler);
        }

        internal static void RaiseLabelClicked(MaterialEditorLabelClickEventArgs eventArgs)
        {
            foreach (var handler in LabelClickHandlers.ToArray())
            {
                try
                {
                    handler(eventArgs);
                }
                catch (Exception ex)
                {
                    MaterialEditorPluginBase.Logger?.LogError($"Exception in Material Editor label click handler: {ex}");
                }
            }
        }

        /// <summary>
        /// Initialize the MaterialEditor UI
        /// </summary>
        protected void InitUI()
        {
            MaterialEditorExtensionRegistry.SetActiveEditService(EditService);
            _windowView = new MaterialEditorWindowView(
                transform,
                CurrentFilter,
                RefreshUI,
                () => Visible = false,
                () => _selectionController.ToggleSidePanels());
            _selectionController = new MaterialEditorSelectionController(
                Session,
                _windowView,
                EditService,
                PopulateList);
            _presenter = new MaterialEditorPresenter(
                EditService,
                Session,
                new MaterialEditorPresentationActions
                {
                    Refresh = PopulateList,
                    RefreshDeferred = (go, data, filter) =>
                        StartCoroutine(PopulateListCoroutine(go, data, filter)),
                    RefreshMaterialSelection = PopulateMaterialList,
                    ShowRename = PopulateRenameList,
                    ExportUv = Export.ExportUVMaps,
                    RequestObjExport = Session.RequestObjExport,
                    ExportTexture = ExportTexture,
                    ImportTexture = ImportTexture,
                    SelectInterpolable = SelectInterpolableButtonOnClick,
                    SelectProjectorInterpolable = SelectProjectorInterpolableButtonOnClick,
                    EditColor = (data, material, title, value, onChanged) =>
                        SetupColorPalette(data, material, title, value, onChanged, true),
                    SetColorToPalette = SetColorToPalette,
                    IsPropertyBlacklisted = (materialName, propertyName) =>
                        Instance.CheckBlacklist(materialName, propertyName)
                });

            ActiveView = _windowView;
            MaterialEditorWindow = _windowView.Window;
            MaterialEditorMainPanel = _windowView.MainPanel;
            DragPanel = _windowView.HeaderPanel;
            VirtualList = _windowView.VirtualList;
            Visible = false;
        }

        /// <summary>
        /// Refresh the MaterialEditor UI
        /// </summary>
        public void RefreshUI() => RefreshUI(CurrentFilter);
        /// <summary>
        /// Refresh the MaterialEditor UI using the specified filter text
        /// </summary>
        public void RefreshUI(string filterText) => PopulateList(CurrentGameObject, CurrentData, filterText);

        /// <summary>
        /// Get or set the MaterialEditor UI visibility
        /// </summary>
        public static bool Visible
        {
            get
            {
                if (MaterialEditorWindow != null && MaterialEditorWindow.gameObject != null)
                    return MaterialEditorWindow.gameObject.activeInHierarchy;
                return false;
            }
            set
            {
                if (MaterialEditorWindow != null)
                    MaterialEditorWindow.gameObject.SetActive(value);
                if (!value)
                    TexChangeWatcher?.Dispose();
            }
        }

        internal static void UISettingChanged(object sender, EventArgs e)
        {
            ActiveView?.ApplySettings();
        }

        /// <summary>
        /// Search text using wildcards.
        /// </summary>
        /// <param name="text">Text to search in</param>
        /// <param name="filter">Filter with which to search the text</param>
        internal static bool WildCardSearch(string text, string filter)
        {
            return MaterialEditorFilter.Matches(text, filter);
        }

        /// <summary>
        /// Populate the renderer list
        /// </summary>
        /// <param name="go">GameObject for which to read the renderers</param>
        /// <param name="data">Object that will be passed through to the get/set/reset events</param>
        /// <param name="rendListFull">List of all renderers to display</param>
        private void PopulateRendererList(GameObject go, object data, IEnumerable<Renderer> rendListFull)
        {
            _selectionController.PopulateRendererList(go, data, rendListFull);
        }


        /// <summary>
        /// Populate the materials list
        /// </summary>
        /// <param name="go">GameObject for which to read the renderers</param>
        /// <param name="data">Object that will be passed through to the get/set/reset events</param>
        /// <param name="materials">List of all materials to display</param>
        private void PopulateMaterialList(GameObject go, object data, IEnumerable<Renderer> materials)
        {
            _selectionController.PopulateMaterialList(go, data, materials);
        }

        /// <summary>
        /// Populate the rename list
        /// </summary>
        /// <param name="go">GameObject for which to read the renderers</param>
        /// <param name="material">Material to be renamed</param>
        /// <param name="data">Object that will be passed through to the get/set/reset events</param>
        private void PopulateRenameList(GameObject go, Material material, object data)
        {
            _selectionController.ShowRenamePanel(go, material, data);
        }

        /// <summary>
        /// Populate the MaterialEditor UI
        /// </summary>
        /// <param name="go">GameObject for which to read the renderers and materials</param>
        /// <param name="data">Object that will be passed through to the get/set/reset events</param>
        /// <param name="filter">Comma separated list of text to filter the results</param>
        protected void PopulateList(GameObject go, object data, string filter = null)
        {
            _selectionController.CloseRenamePanel();

            if (filter == null)
                filter = PersistFilter.Value ? CurrentFilter : string.Empty;

            _windowView.PrepareForDisplay(filter);
            if (go == null)
                return;

            var renderers = GetRendererList(go).ToList();
            var projectors = EditService.GetProjectorList(data, go).ToList();
            PopulateRendererList(go, data, renderers);

            CurrentGameObject = go;
            CurrentData = data;
            CurrentFilter = filter;

            VirtualList.SetList(_presenter.BuildRows(go, data, filter, renderers, projectors));
        }

        /// <summary>
        /// Obj export should be done in OnGUI or something similarly late so that finger rotation is exported properly
        /// </summary>
        private void OnGUI()
        {
            if (Session.TryTakeObjExport(out var renderer))
                Export.ExportObj(renderer);
        }

        /// <summary>
        /// Hacky workaround to wait for the dropdown fade to complete before refreshing
        /// </summary>
        protected IEnumerator PopulateListCoroutine(GameObject go, object data, string filter = "")
        {
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            PopulateList(go, data, filter);
        }

        private void ImportTexture(
            TexturePropertyRowModel textureItem,
            GameObject gameObject,
            object data,
            Material material,
            string propertyName)
        {
#if !API
            string fileFilter = KK_Plugins.ImageHelper.FileFilter;
#else
            string fileFilter = "Images (*.png;.jpg)|*.png;*.jpg|All files|*.*";
#endif
            KKAPI.Utilities.OpenFileDialog.Show(OnFileAccept, "Open image", ExportPath, fileFilter, ".png");

            void OnFileAccept(string[] files)
            {
                if (files == null || files.Length == 0 || files[0].IsNullOrEmpty())
                {
                    textureItem.Changed =
                        !EditService.GetMaterialTextureValueOriginal(
                            data,
                            material,
                            propertyName,
                            gameObject);
                    textureItem.Exists = material.GetTexture($"_{propertyName}") != null;
                    return;
                }

                string filePath = files[0];
                EditService.SetMaterialTexture(data, material, propertyName, filePath, gameObject);

                TexChangeWatcher?.Dispose();
                if (!WatchTexChanges.Value)
                    return;

                var directory = Path.GetDirectoryName(filePath);
                if (directory == null)
                    return;

                TexChangeWatcher = new FileSystemWatcher(directory, Path.GetFileName(filePath));
                TexChangeWatcher.Changed += (sender, args) =>
                {
                    if (WatchTexChanges.Value && File.Exists(filePath))
                        EditService.SetMaterialTexture(data, material, propertyName, filePath, gameObject);
                };
                TexChangeWatcher.Deleted += (sender, args) => TexChangeWatcher?.Dispose();
                TexChangeWatcher.Error += (sender, args) => TexChangeWatcher?.Dispose();
                TexChangeWatcher.EnableRaisingEvents = true;
            }
        }

        internal virtual void ExportTexture(Material mat, string property)
        {
            var tex = mat.GetTexture($"_{property}");
            if (tex == null) return;
            var matName = mat.NameFormatted();
            matName = string.Concat(matName.Split(Path.GetInvalidFileNameChars())).Trim();
            string filename = Path.Combine(ExportPath, $"_Export_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}_{matName}_{property}.png");
            Instance.ConvertNormalMap(ref tex, property, ConvertNormalmapsOnExport.Value);
            SaveTex(tex, filename);
            MaterialEditorPluginBase.Logger.LogInfo($"Exported {filename}");
            Utilities.OpenFileInExplorer(filename);
        }

        internal void ExportTextureOriginal(Material mat, string property, string ext, byte[] texData)
        {
            var matName = mat.NameFormatted();
            matName = string.Concat(matName.Split(Path.GetInvalidFileNameChars())).Trim();
            string filename = Path.Combine(ExportPath, $"_Export_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}_{matName}_{property}.{ext}");
            System.IO.File.WriteAllBytes(filename, texData);
            MaterialEditorPluginBase.Logger.LogInfo($"Exported {filename}");
            Utilities.OpenFileInExplorer(filename);
        }

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

        private void SetupColorPalette(object data, Material material, string title, Color value, Action<Color> onChanged, bool useAlpha)
        {
            var name = material.name;
            if (ColorPalette.IsShowing(title, data, name))
            {
                ColorPalette.Close();
                return;
            }

            try
            {
                ColorPalette.Setup(title, data, name, value, onChanged, useAlpha);
            }
            catch (ArgumentException)
            {
                MaterialEditorPluginBase.Logger.LogError($"Color value is out of range. ({value})");
                ColorPalette.Close();
            }
        }
        private void SetColorToPalette(object data, Material material, string title, Color value)
        {
            if (ColorPalette.IsShowing(title, data, material.name))
            {
                try
                {
                    ColorPalette.SetColor(value);
                }
                catch (ArgumentException)
                {
                    MaterialEditorPluginBase.Logger.LogError($"Color value is out of range. ({value})");
                    ColorPalette.Close();
                }
            }
        }

        private void SelectInterpolableButtonOnClick(GameObject go, RowModel.RowItemType rowType, string materialName = "", string propertyName = "", string rendererName = "")
        {
            selectedInterpolable = new SelectedInterpolable(go, rowType, materialName, propertyName, rendererName);
            MaterialEditorPluginBase.Logger.LogMessage($"Activated interpolable(s), {selectedInterpolable}");
#if !API && !EC
            TimelineCompatibilityHelper.RefreshInterpolablesList();
#endif
        }

        private void SelectProjectorInterpolableButtonOnClick(GameObject go, ProjectorProperties property, string projectorName)
        {
            selectedProjectorInterpolable = new SelectedProjectorInterpolable(go, property, projectorName);
            MaterialEditorPluginBase.Logger.LogMessage($"Activated interpolable(s), {selectedProjectorInterpolable}");
#if !API && !EC
            TimelineCompatibilityHelper.RefreshInterpolablesList();
#endif
        }

        internal class SelectedInterpolable
        {
            public string MaterialName;
            public string PropertyName;
            public string RendererName;
            public GameObject GameObject;
            public RowModel.RowItemType RowType;

            public SelectedInterpolable(GameObject go, RowModel.RowItemType rowType, string materialName, string propertyName, string rendererName)
            {
                GameObject = go;
                RowType = rowType;
                MaterialName = materialName;
                PropertyName = propertyName;
                RendererName = rendererName;
            }

            public override string ToString()
            {
                return $"{RowType}: {string.Join(" - ", new string[] { PropertyName, MaterialName, RendererName, }.Where(x => !x.IsNullOrEmpty()).ToArray())}";
            }
        }

        internal class SelectedProjectorInterpolable
        {
            public string ProjectorName;
            public ProjectorProperties Property;
            public GameObject GameObject;

            public SelectedProjectorInterpolable(GameObject go, ProjectorProperties property, string projectorName)
            {
                GameObject = go;
                Property = property;
                ProjectorName = projectorName;
            }

            public override string ToString()
            {
                return $"Projector: {string.Join(" - ", new string[] { Property.ToString(), ProjectorName, }.Where(x => !x.IsNullOrEmpty()).ToArray())}";
            }
        }
    }
}
