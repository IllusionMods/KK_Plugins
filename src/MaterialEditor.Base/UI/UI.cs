using BepInEx;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UILib;
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
        private static ScrollRect MaterialEditorScrollableUI;
        private static InputField FilterInputField;

        internal static FileSystemWatcher TexChangeWatcher;
        private VirtualList VirtualList;

        internal const float MarginSize = 5f;
        internal const float HeaderSize = 20f;
        internal const float ScrollOffsetX = -15f;
        internal const float PanelHeight = 20f;
        internal const float LabelWidth = 50f;
        internal const float ButtonWidth = 100f;
        internal const float DropdownWidth = 100f;
        internal const float TextBoxWidth = 75f;
        internal const float ColorLabelWidth = 10f;
        internal const float ResetButtonWidth = 50f;
        internal const float SliderWidth = 150f;
        internal const float LabelXWidth = 60f;
        internal const float LabelYWidth = 10f;
        internal const float TextBoxXYWidth = 50f;
        internal static RectOffset Padding;
        internal static readonly Color RowColor = new Color(1f, 1f, 1f, 0.6f);
        internal static readonly Color ItemColor = new Color(1f, 1f, 1f, 0f);
        internal static readonly Color SeparatorItemColor = new Color(0.9f, 0.9f, 0.9f, 0.55f);

        private GameObject CurrentGameObject;
        private object CurrentData;
        private string CurrentFilter = "";

        /// <summary>
        /// Initialize the MaterialEditor UI
        /// </summary>
        protected void InitUI()
        {
            Padding = new RectOffset(3, 2, 0, 1);

            MaterialEditorWindow = UIUtility.CreateNewUISystem("MaterialEditorCanvas");
            MaterialEditorWindow.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f / UIScale.Value, 1080f / UIScale.Value);
            Visible = false;
            MaterialEditorWindow.gameObject.transform.SetParent(transform);
            MaterialEditorWindow.sortingOrder = 1000;

            MaterialEditorMainPanel = UIUtility.CreatePanel("Panel", MaterialEditorWindow.transform);
            MaterialEditorMainPanel.color = Color.white;
            MaterialEditorMainPanel.transform.SetRect(0.05f, 0.05f, UIWidth.Value * UIScale.Value, UIHeight.Value * UIScale.Value);

            UIUtility.AddOutlineToObject(MaterialEditorMainPanel.transform, Color.black);

            DragPanel = UIUtility.CreatePanel("Draggable", MaterialEditorMainPanel.transform);
            DragPanel.transform.SetRect(0f, 1f, 1f, 1f, 0f, -HeaderSize);
            DragPanel.color = Color.gray;
            UIUtility.MakeObjectDraggable(DragPanel.rectTransform, MaterialEditorMainPanel.rectTransform);

            var nametext = UIUtility.CreateText("Nametext", DragPanel.transform, "Material Editor");
            nametext.transform.SetRect();
            nametext.alignment = TextAnchor.MiddleCenter;

            FilterInputField = UIUtility.CreateInputField("Filter", DragPanel.transform, "Filter");
            FilterInputField.transform.SetRect(0f, 0f, 0f, 1f, 1f, 1f, 100f, -1f);
            FilterInputField.onValueChanged.AddListener(RefreshUI);

            var close = UIUtility.CreateButton("CloseButton", DragPanel.transform, "");
            close.transform.SetRect(1f, 0f, 1f, 1f, -20f, 1f, -1f, -1f);
            close.onClick.AddListener(() => Visible = false);

            //X button
            var x1 = UIUtility.CreatePanel("x1", close.transform);
            x1.transform.SetRect(0f, 0f, 1f, 1f, 8f, 0f, -8f);
            x1.rectTransform.eulerAngles = new Vector3(0f, 0f, 45f);
            x1.color = Color.black;
            var x2 = UIUtility.CreatePanel("x2", close.transform);
            x2.transform.SetRect(0f, 0f, 1f, 1f, 8f, 0f, -8f);
            x2.rectTransform.eulerAngles = new Vector3(0f, 0f, -45f);
            x2.color = Color.black;

            MaterialEditorScrollableUI = UIUtility.CreateScrollView("MaterialEditorWindow", MaterialEditorMainPanel.transform);
            MaterialEditorScrollableUI.transform.SetRect(0f, 0f, 1f, 1f, MarginSize, MarginSize, -MarginSize, -HeaderSize - MarginSize / 2f);
            MaterialEditorScrollableUI.gameObject.AddComponent<Mask>();
            MaterialEditorScrollableUI.content.gameObject.AddComponent<VerticalLayoutGroup>();
            MaterialEditorScrollableUI.content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            MaterialEditorScrollableUI.verticalScrollbar.GetComponent<RectTransform>().offsetMin = new Vector2(ScrollOffsetX, 0f);
            MaterialEditorScrollableUI.viewport.offsetMax = new Vector2(ScrollOffsetX, 0f);
            MaterialEditorScrollableUI.movementType = ScrollRect.MovementType.Clamped;
            MaterialEditorScrollableUI.verticalScrollbar.GetComponent<Image>().color = new Color(1, 1, 1, 0.6f);

            var template = ItemTemplate.CreateTemplate(MaterialEditorScrollableUI.content.transform);

            VirtualList = MaterialEditorScrollableUI.gameObject.AddComponent<VirtualList>();
            VirtualList.ScrollRect = MaterialEditorScrollableUI;
            VirtualList.EntryTemplate = template;
            VirtualList.Initialize();
        }

        /// <summary>
        /// Refresh the MaterialEditor UI
        /// </summary>
        public void RefreshUI() => RefreshUI(CurrentFilter);
        /// <summary>
        /// Refresh the MaterialEditor UI using the specified filter text
        /// </summary>
        public void RefreshUI(string filterText) => PopulateList(CurrentGameObject, CurrentData, filterText);

        private static void SetMainRectWithMemory(float anchorLeft, float anchorBottom, float anchorRight, float anchorTop)
        {
            Vector3 positionMemory = MaterialEditorMainPanel.transform.position;
            MaterialEditorMainPanel.transform.SetRect(anchorLeft, anchorBottom, anchorRight, anchorTop);
            if (!Input.GetKey(KeyCode.LeftControl))
                MaterialEditorMainPanel.transform.position = positionMemory;
        }

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
            if (MaterialEditorWindow != null)
                MaterialEditorWindow.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f / UIScale.Value, 1080f / UIScale.Value);
            if (MaterialEditorMainPanel != null)
                SetMainRectWithMemory(0.05f, 0.05f, UIWidth.Value * UIScale.Value, UIHeight.Value * UIScale.Value);
        }

        /// <summary>
        /// Populate the MaterialEditor UI
        /// </summary>
        /// <param name="go">GameObject for which to read the renderers and materials</param>
        /// <param name="data">Object that will be passed through to the get/set/reset events</param>
        /// <param name="filter">Comma separated list of text to filter the results</param>
        protected void PopulateList(GameObject go, object data, string filter = "")
        {
            MaterialEditorWindow.gameObject.SetActive(true);
            MaterialEditorWindow.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f / UIScale.Value, 1080f / UIScale.Value);
            SetMainRectWithMemory(0.05f, 0.05f, UIWidth.Value * UIScale.Value, UIHeight.Value * UIScale.Value);
            FilterInputField.Set(filter);

            CurrentGameObject = go;
            CurrentData = data;
            CurrentFilter = filter;

            if (go == null) return;

            List<Renderer> rendList = new List<Renderer>();
            IEnumerable<Renderer> rendListFull = GetRendererList(go);
            List<string> filterList = new List<string>();
            List<ItemInfo> items = new List<ItemInfo>();
            Dictionary<string, Material> matList = new Dictionary<string, Material>();

            if (!filter.IsNullOrEmpty())
                filterList = filter.Split(',').ToList();
            filterList.RemoveAll(x => x.IsNullOrWhiteSpace());

            //Get all renderers and materials matching the filter
            if (filterList.Count == 0)
                rendList = rendListFull.ToList();
            else
                foreach (var rend in rendListFull)
                {
                    for (var j = 0; j < filterList.Count; j++)
                    {
                        var filterWord = filterList[j];
                        if (rend.NameFormatted().ToLower().Contains(filterWord.Trim().ToLower()) && !rendList.Contains(rend))
                            rendList.Add(rend);
                    }

                    foreach (var mat in GetMaterials(go, rend))
                    {
                        for (var k = 0; k < filterList.Count; k++)
                        {
                            var filterWord = filterList[k];
                            if (mat.NameFormatted().ToLower().Contains(filterWord.Trim().ToLower()))
                                matList[mat.NameFormatted()] = mat;
                        }
                    }
                }

            for (var i = 0; i < rendList.Count; i++)
            {
                var rend = rendList[i];
                //Get materials if materials list wasn't previously built by the filter    
                if (filterList.Count == 0)
                    foreach (var mat in GetMaterials(go, rend))
                        matList[mat.NameFormatted()] = mat;

                var rendererItem = new ItemInfo(ItemInfo.RowItemType.Renderer, "Renderer")
                {
                    RendererName = rend.NameFormatted(),
                    ExportUVOnClick = () => Export.ExportUVMaps(rend),
                    ExportObjOnClick = () => Export.ExportObj(rend)
                };
                items.Add(rendererItem);

                //Renderer Enabled
                bool valueEnabledOriginal = rend.enabled;
                var temp = GetRendererPropertyValueOriginal(data, rend, RendererProperties.Enabled, go);
                if (!temp.IsNullOrEmpty())
                    valueEnabledOriginal = temp == "1";
                var rendererEnabledItem = new ItemInfo(ItemInfo.RowItemType.RendererEnabled, "Enabled")
                {
                    RendererEnabled = rend.enabled ? 1 : 0,
                    RendererEnabledOriginal = valueEnabledOriginal ? 1 : 0,
                    RendererEnabledOnChange = value => SetRendererProperty(data, rend, RendererProperties.Enabled, value.ToString(), go),
                    RendererEnabledOnReset = () => RemoveRendererProperty(data, rend, RendererProperties.Enabled, go)
                };
                items.Add(rendererEnabledItem);

                //Renderer ShadowCastingMode
                var valueShadowCastingModeOriginal = rend.shadowCastingMode;
                temp = GetRendererPropertyValueOriginal(data, rend, RendererProperties.ShadowCastingMode, go);
                if (!temp.IsNullOrEmpty())
                    valueShadowCastingModeOriginal = (UnityEngine.Rendering.ShadowCastingMode)int.Parse(temp);
                var rendererShadowCastingModeItem = new ItemInfo(ItemInfo.RowItemType.RendererShadowCastingMode, "Shadow Casting Mode")
                {
                    RendererShadowCastingMode = (int)rend.shadowCastingMode,
                    RendererShadowCastingModeOriginal = (int)valueShadowCastingModeOriginal,
                    RendererShadowCastingModeOnChange = value => SetRendererProperty(data, rend, RendererProperties.ShadowCastingMode, value.ToString(), go),
                    RendererShadowCastingModeOnReset = () => RemoveRendererProperty(data, rend, RendererProperties.ShadowCastingMode, go)
                };
                items.Add(rendererShadowCastingModeItem);

                //Renderer ReceiveShadows
                bool valueReceiveShadowsOriginal = rend.receiveShadows;
                temp = GetRendererPropertyValueOriginal(data, rend, RendererProperties.ShadowCastingMode, go);
                if (!temp.IsNullOrEmpty())
                    valueReceiveShadowsOriginal = temp == "1";
                var rendererReceiveShadowsItem = new ItemInfo(ItemInfo.RowItemType.RendererReceiveShadows, "Receive Shadows")
                {
                    RendererReceiveShadows = rend.receiveShadows ? 1 : 0,
                    RendererReceiveShadowsOriginal = valueReceiveShadowsOriginal ? 1 : 0,
                    RendererReceiveShadowsOnChange = value => SetRendererProperty(data, rend, RendererProperties.ReceiveShadows, value.ToString(), go),
                    RendererReceiveShadowsOnReset = () => RemoveRendererProperty(data, rend, RendererProperties.ReceiveShadows, go)
                };
                items.Add(rendererReceiveShadowsItem);
            }

            foreach (var mat in matList.Values)
            {
                string materialName = mat.NameFormatted();
                string shaderName = mat.shader.NameFormatted();

                var materialItem = new ItemInfo(ItemInfo.RowItemType.Material, "Material")
                {
                    MaterialName = materialName,
                    MaterialOnCopy = () => MaterialCopyEdits(data, mat, go),
                    MaterialOnPaste = () =>
                    {
                        MaterialPasteEdits(data, mat, go);
                        PopulateList(go, data, filter);
                    }
                };
                materialItem.MaterialOnCopyRemove = () =>
                {
                    CopyMaterial(go, materialName);
                    PopulateList(go, data, filter);
                };
                items.Add(materialItem);

                //Shader
                string shaderNameOriginal = shaderName;
                var temp = GetMaterialShaderNameOriginal(data, mat, go);
                if (!temp.IsNullOrEmpty())
                    shaderNameOriginal = temp;
                var shaderItem = new ItemInfo(ItemInfo.RowItemType.Shader, "Shader")
                {
                    ShaderName = shaderName,
                    ShaderNameOriginal = shaderNameOriginal,
                    ShaderNameOnChange = value =>
                    {
                        SetMaterialShaderName(data, mat, value, go);
                        StartCoroutine(PopulateListCoroutine(go, data, filter));
                    },
                    ShaderNameOnReset = () =>
                    {
                        RemoveMaterialShaderName(data, mat, go);
                        StartCoroutine(PopulateListCoroutine(go, data, filter));
                    }
                };
                items.Add(shaderItem);

                //Shader RenderQueue
                int renderQueueOriginal = mat.renderQueue;
                int? renderQueueOriginalTemp = GetMaterialShaderRenderQueueOriginal(data, mat, go);
                renderQueueOriginal = renderQueueOriginalTemp ?? renderQueueOriginal;
                var shaderRenderQueueItem = new ItemInfo(ItemInfo.RowItemType.ShaderRenderQueue, "Render Queue")
                {
                    ShaderRenderQueue = mat.renderQueue,
                    ShaderRenderQueueOriginal = renderQueueOriginal,
                    ShaderRenderQueueOnChange = value => SetMaterialShaderRenderQueue(data, mat, value, go),
                    ShaderRenderQueueOnReset = () => RemoveMaterialShaderRenderQueue(data, mat, go)
                };
                items.Add(shaderRenderQueueItem);

                foreach (var property in XMLShaderProperties[XMLShaderProperties.ContainsKey(shaderName) ? shaderName : "default"].OrderBy(x => x.Value.Type).ThenBy(x => x.Key))
                {
                    string propertyName = property.Key;
                    if (Instance.CheckBlacklist(materialName, propertyName)) continue;

                    if (property.Value.Type == ShaderPropertyType.Texture)
                    {
                        if (mat.HasProperty($"_{propertyName}"))
                        {
                            var textureItem = new ItemInfo(ItemInfo.RowItemType.TextureProperty, propertyName)
                            {
                                TextureChanged = !GetMaterialTextureValueOriginal(data, mat, propertyName, go),
                                TextureExists = mat.GetTexture($"_{propertyName}") != null,
                                TextureOnExport = () => ExportTexture(mat, propertyName)
                            };
                            textureItem.TextureOnImport = () =>
                            {
                                OpenFileDialog.Show(OnFileAccept, "Open image", Application.dataPath, FileFilter);

                                void OnFileAccept(string[] strings)
                                {
                                    if (strings == null || strings.Length == 0 || strings[0].IsNullOrEmpty())
                                    {
                                        textureItem.TextureChanged = !GetMaterialTextureValueOriginal(data, mat, propertyName, go);
                                        textureItem.TextureExists = mat.GetTexture($"_{propertyName}") != null;
                                        return;
                                    }
                                    string filePath = strings[0];

                                    SetMaterialTexture(data, mat, propertyName, filePath, go);

                                    TexChangeWatcher?.Dispose();
                                    if (WatchTexChanges.Value)
                                    {
                                        var directory = Path.GetDirectoryName(filePath);
                                        if (directory != null)
                                        {
                                            TexChangeWatcher = new FileSystemWatcher(directory, Path.GetFileName(filePath));
                                            TexChangeWatcher.Changed += (sender, args) =>
                                            {
                                                if (WatchTexChanges.Value && File.Exists(filePath))
                                                    SetMaterialTexture(data, mat, propertyName, filePath, go);
                                            };
                                            TexChangeWatcher.Deleted += (sender, args) => TexChangeWatcher?.Dispose();
                                            TexChangeWatcher.Error += (sender, args) => TexChangeWatcher?.Dispose();
                                            TexChangeWatcher.EnableRaisingEvents = true;
                                        }
                                    }
                                }
                            };
                            textureItem.TextureOnReset = () => RemoveMaterialTexture(data, mat, propertyName, go);
                            items.Add(textureItem);

                            Vector2 textureOffset = mat.GetTextureOffset($"_{propertyName}");
                            Vector2 textureOffsetOriginal = textureOffset;
                            Vector2? textureOffsetOriginalTemp = GetMaterialTextureOffsetOriginal(data, mat, propertyName, go);
                            if (textureOffsetOriginalTemp != null)
                                textureOffsetOriginal = (Vector2)textureOffsetOriginalTemp;

                            Vector2 textureScale = mat.GetTextureScale($"_{propertyName}");
                            Vector2 textureScaleOriginal = textureScale;
                            Vector2? textureScaleOriginalTemp = GetMaterialTextureScaleOriginal(data, mat, propertyName, go);
                            if (textureScaleOriginalTemp != null)
                                textureScaleOriginal = (Vector2)textureScaleOriginalTemp;

                            var textureItemOffsetScale = new ItemInfo(ItemInfo.RowItemType.TextureOffsetScale)
                            {
                                Offset = textureOffset,
                                OffsetOriginal = textureOffsetOriginal,
                                OffsetOnChange = value => SetMaterialTextureOffset(data, mat, propertyName, value, go),
                                OffsetOnReset = () => RemoveMaterialTextureOffset(data, mat, propertyName, go),
                                Scale = textureScale,
                                ScaleOriginal = textureScaleOriginal,
                                ScaleOnChange = value => SetMaterialTextureScale(data, mat, propertyName, value, go),
                                ScaleOnReset = () => RemoveMaterialTextureScale(data, mat, propertyName, go)
                            };
                            items.Add(textureItemOffsetScale);
                        }

                    }
                    else if (property.Value.Type == ShaderPropertyType.Color)
                    {
                        if (mat.HasProperty($"_{propertyName}"))
                        {
                            Color valueColor = mat.GetColor($"_{propertyName}");
                            Color valueColorOriginal = valueColor;
                            Color? c = GetMaterialColorPropertyValueOriginal(data, mat, propertyName, go);
                            if (c != null)
                                valueColorOriginal = (Color)c;
                            var contentItem = new ItemInfo(ItemInfo.RowItemType.ColorProperty, propertyName)
                            {
                                ColorValue = valueColor,
                                ColorValueOriginal = valueColorOriginal,
                                ColorValueOnChange = value => SetMaterialColorProperty(data, mat, propertyName, value, go),
                                ColorValueOnReset = () => RemoveMaterialColorProperty(data, mat, propertyName, go)
                            };
                            items.Add(contentItem);
                        }
                    }
                    else if (property.Value.Type == ShaderPropertyType.Float)
                    {
                        if (mat.HasProperty($"_{propertyName}"))
                        {
                            float valueFloat = mat.GetFloat($"_{propertyName}");
                            float valueFloatOriginal = valueFloat;
                            float? valueFloatOriginalTemp = GetMaterialFloatPropertyValueOriginal(data, mat, propertyName, go);
                            if (valueFloatOriginalTemp != null)
                                valueFloatOriginal = (float)valueFloatOriginalTemp;
                            var contentItem = new ItemInfo(ItemInfo.RowItemType.FloatProperty, propertyName)
                            {
                                FloatValue = valueFloat,
                                FloatValueOriginal = valueFloatOriginal
                            };
                            if (property.Value.MinValue != null)
                                contentItem.FloatValueSliderMin = (float)property.Value.MinValue;
                            if (property.Value.MaxValue != null)
                                contentItem.FloatValueSliderMax = (float)property.Value.MaxValue;
                            contentItem.FloatValueOnChange = value => SetMaterialFloatProperty(data, mat, propertyName, value, go);
                            contentItem.FloatValueOnReset = () => RemoveMaterialFloatProperty(data, mat, propertyName, go);
                            items.Add(contentItem);
                        }
                    }
                }
            }

            VirtualList.SetList(items);
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

        private static void ExportTexture(Material mat, string property)
        {
            var tex = mat.GetTexture($"_{property}");
            if (tex == null) return;
            string filename = Path.Combine(ExportPath, $"_Export_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}_{mat.NameFormatted()}_{property}.png");
            SaveTex(tex, filename);
            MaterialEditorPluginBase.Logger.LogInfo($"Exported {filename}");
            Utilities.OpenFileInExplorer(filename);
        }

        public abstract string GetRendererPropertyValueOriginal(object data, Renderer renderer, RendererProperties property, GameObject gameObject);
        public abstract void SetRendererProperty(object data, Renderer renderer, RendererProperties property, string value, GameObject gameObject);
        public abstract void RemoveRendererProperty(object data, Renderer renderer, RendererProperties property, GameObject gameObject);

        public abstract void MaterialCopyEdits(object data, Material material, GameObject gameObject);
        public abstract void MaterialPasteEdits(object data, Material material, GameObject gameObject);

        public abstract string GetMaterialShaderNameOriginal(object data, Material material, GameObject gameObject);
        public abstract void SetMaterialShaderName(object data, Material material, string value, GameObject gameObject);
        public abstract void RemoveMaterialShaderName(object data, Material material, GameObject gameObject);

        public abstract int? GetMaterialShaderRenderQueueOriginal(object data, Material material, GameObject gameObject);
        public abstract void SetMaterialShaderRenderQueue(object data, Material material, int value, GameObject gameObject);
        public abstract void RemoveMaterialShaderRenderQueue(object data, Material material, GameObject gameObject);

        public abstract bool GetMaterialTextureValueOriginal(object data, Material material, string propertyName, GameObject gameObject);
        public abstract void SetMaterialTexture(object data, Material material, string propertyName, string filePath, GameObject gameObject);
        public abstract void RemoveMaterialTexture(object data, Material material, string propertyName, GameObject gameObject);

        public abstract Vector2? GetMaterialTextureOffsetOriginal(object data, Material material, string propertyName, GameObject gameObject);
        public abstract void SetMaterialTextureOffset(object data, Material material, string propertyName, Vector2 value, GameObject gameObject);
        public abstract void RemoveMaterialTextureOffset(object data, Material material, string propertyName, GameObject gameObject);

        public abstract Vector2? GetMaterialTextureScaleOriginal(object data, Material material, string propertyName, GameObject gameObject);
        public abstract void SetMaterialTextureScale(object data, Material material, string propertyName, Vector2 value, GameObject gameObject);
        public abstract void RemoveMaterialTextureScale(object data, Material material, string propertyName, GameObject gameObject);

        public abstract Color? GetMaterialColorPropertyValueOriginal(object data, Material material, string propertyName, GameObject gameObject);
        public abstract void SetMaterialColorProperty(object data, Material material, string propertyName, Color value, GameObject gameObject);
        public abstract void RemoveMaterialColorProperty(object data, Material material, string propertyName, GameObject gameObject);

        public abstract float? GetMaterialFloatPropertyValueOriginal(object data, Material material, string propertyName, GameObject gameObject);
        public abstract void SetMaterialFloatProperty(object data, Material material, string propertyName, float value, GameObject gameObject);
        public abstract void RemoveMaterialFloatProperty(object data, Material material, string propertyName, GameObject gameObject);
    }
}
