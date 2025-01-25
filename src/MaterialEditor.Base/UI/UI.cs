using BepInEx;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
        internal static System.Timers.Timer filterTimer;

        internal static TooltipManager MaterialEditorTooltip;
        private static Button ViewListButton;
        private static SelectListPanel MaterialEditorRendererList;
        private static List<Renderer> SelectedRenderers = new List<Renderer>();
        private static SelectListPanel MaterialEditorMaterialList;
        private static List<Material> SelectedMaterials = new List<Material>();
        private static bool ListsVisible = false;

        private static SelectListPanel MaterialEditorRenameList;
        private static InputField MaterialEditorRenameField;
        private static Button MaterialEditorRenameButton;
        private static Text MaterialEditorRenameMaterial;
        private static List<Renderer> SelectedMaterialRenderers = new List<Renderer>();
        private static bool RenameListVisible = false;

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
        internal const float ResetButtonWidth = 30f;
        internal const float ColorEditButtonWidth = 20f;
        internal const float SliderWidth = 150f;
        internal const float LabelXWidth = 60f;
        internal const float LabelYWidth = 10f;
        internal const float TextBoxXYWidth = 50f;
        internal static RectOffset Padding;
        internal static readonly Color RowColor = new Color(1f, 1f, 1f, 0.6f);

        // https://simplified.com/blog/colors/triadic-colors
        internal static readonly Color RendererColor = new Color(0.984f, 0.600f, 0.008f, 0.5f);
        internal static readonly Color MaterialColor = new Color(0.400f, 0.690f, 0.196f, 0.5f);
        internal static readonly Color CategoryColor = new Color(0.627f, 0.004f, 0.812f, 0.5f);

        internal static readonly Color ItemColor = new Color(1f, 1f, 1f, 0f);
        internal static readonly Color ItemColorChanged = new Color(0f, 0f, 0f, 0.3f);

        private protected IMaterialEditorColorPalette ColorPalette;

        private GameObject CurrentGameObject;
        private object CurrentData;
        private static string CurrentFilter = "";
        private bool DoObjExport = false;
        private Renderer ObjRenderer;

        internal static SelectedInterpolable selectedInterpolable;
        internal static SelectedProjectorInterpolable selectedProjectorInterpolable;

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

            TooltipManager.Init(MaterialEditorWindow.transform);

            UIUtility.AddOutlineToObject(MaterialEditorMainPanel.transform, Color.black);

            DragPanel = UIUtility.CreatePanel("Draggable", MaterialEditorMainPanel.transform);
            DragPanel.transform.SetRect(0f, 1f, 1f, 1f, 0f, -HeaderSize);
            DragPanel.color = Color.gray;
            UIUtility.MakeObjectDraggable(DragPanel.rectTransform, MaterialEditorMainPanel.rectTransform);

            var nametext = UIUtility.CreateText("Nametext", DragPanel.transform, "Material Editor");
            nametext.transform.SetRect();
            nametext.alignment = TextAnchor.MiddleCenter;

            filterTimer = new System.Timers.Timer(FilterDelay.Value);
            filterTimer.AutoReset = false;
            filterTimer.Elapsed += (sender, e) =>
            {
                RefreshUI(FilterInputField.text);
            };
            filterTimer.Stop();

            FilterInputField = UIUtility.CreateInputField("Filter", DragPanel.transform, "Filter");
            FilterInputField.text = CurrentFilter;
            FilterInputField.transform.SetRect(0f, 0f, 0f, 1f, 1f, 1f, 100f, -1f);
            FilterInputField.onValueChanged.AddListener((e) => {
                filterTimer.Stop();
                filterTimer.Start();
            });
            FilterInputField.onEndEdit.AddListener((text) =>
            {
                filterTimer.Stop();
                RefreshUI(text);
            });
            TooltipManager.AddTooltip(FilterInputField.gameObject, @"Filter visible items in the window.

- Searches for renderers, materials and projectors
- Searches starting with '_' will search for material properties
- Combine multiple statements using a comma (an entry just has to match any of the search terms)
- Use a '*' as a wildcard for any amount of characters (e.g. ""_pattern*1"" will find the ""PatternMask1"" property)
- Use a '?' as a wildcard for a single character");

            var persistSearch = UIUtility.CreateToggle("PersistSearch", DragPanel.transform, "");
            persistSearch.transform.SetRect(0f, 1f, 1f, 0.5f, 100f, 0f, 0, 10f);
            persistSearch.Set(PersistFilter.Value);
            persistSearch.gameObject.GetComponentInChildren<CanvasRenderer>(true).transform.SetRect(0f, 1f, 0f, 0f, 0f, -19f, 19f, -1f);
            persistSearch.onValueChanged.AddListener((value) => PersistFilter.Value = value);
            TooltipManager.AddTooltip(persistSearch.gameObject, "Keeps the filter between instances of this window instead of resetting them");

            //Don't use text withing the toggle itself to prevent weird scaling issues
            var persistSearchText = UIUtility.CreateText("PersistSearchText", DragPanel.transform, "Persist search");
            persistSearchText.transform.SetRect(0f, 0.15f, 1f, 0.85f, 120f, 0f, 0, 0f);

            var close = UIUtility.CreateButton("CloseButton", DragPanel.transform, "");
            close.transform.SetRect(1f, 0f, 1f, 1f, -40f, 1f, -21f, -1f);
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

            ViewListButton = UIUtility.CreateButton("ViewListButton", DragPanel.transform, ">");
            ViewListButton.transform.SetRect(1f, 0f, 1f, 1f, -20f, 1f, -1f, -1f);
            ViewListButton.onClick.AddListener(() =>
            {
                if (RenameListVisible)
                {
                    MaterialEditorRenameList.ToggleVisibility(false);
                    ViewListButton.GetComponentInChildren<Text>().text = ">";
                    RenameListVisible = false;
                }
                else
                {
                    MaterialEditorRendererList.ToggleVisibility(!ListsVisible);
                    MaterialEditorMaterialList.ToggleVisibility(!ListsVisible);
                    ListsVisible = !ListsVisible;
                    if (ListsVisible)
                        ViewListButton.GetComponentInChildren<Text>().text = "<";
                    else
                        ViewListButton.GetComponentInChildren<Text>().text = ">";
                }
            });

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

            MaterialEditorRendererList = new SelectListPanel(MaterialEditorMainPanel.transform, "RendererList", "Renderers");
            MaterialEditorRendererList.Panel.transform.SetRect(1f, 0.5f, 1f, 1f, MarginSize, MarginSize / 2f, MarginSize + UIListWidth.Value);
            MaterialEditorRendererList.ToggleVisibility(false);
            MaterialEditorMaterialList = new SelectListPanel(MaterialEditorMainPanel.transform, "MaterialList", "Materials");
            MaterialEditorMaterialList.Panel.transform.SetRect(1f, 0f, 1f, 0.5f, MarginSize, 0f, MarginSize + UIListWidth.Value, -MarginSize);
            MaterialEditorMaterialList.ToggleVisibility(false);

            MaterialEditorRenameList = new SelectListPanel(MaterialEditorMainPanel.transform, "MaterialRenameList", "Mat. Renderers");
            MaterialEditorRenameList.Panel.transform.SetRect(1f, 0.5f, 1f, 1f, MarginSize, MarginSize / 2f, MarginSize + UIListWidth.Value);
            MaterialEditorRenameList.ToggleVisibility(false);
            MaterialEditorRenameField = UIUtility.CreateInputField("MaterialEditorRenameField", MaterialEditorRenameList.Panel.transform, "");
            MaterialEditorRenameField.transform.SetRect(0, 0, 1, 0, 0, -(PanelHeight + MarginSize / 2), 0, -(MarginSize / 2));
            MaterialEditorRenameButton = UIUtility.CreateButton("MaterialEditorRenameButton", MaterialEditorRenameList.Panel.transform, "Rename");
            MaterialEditorRenameButton.transform.SetRect(0, 0, 1, 0, 0, -(2 * PanelHeight + MarginSize), 0, -(PanelHeight + MarginSize));
            MaterialEditorRenameList.Panel.transform.GetChild(0).SetRect(0, 1, 0.4f, 1, 5, -40, -2, -27.5f);
            MaterialEditorRenameList.Panel.transform.GetChild(1).SetRect(0.4f, 1, 1, 1, 2, -42.5f, -2, -25);
            MaterialEditorRenameList.Panel.transform.GetChild(2).SetRect(0, 0, 1, 1, 2, 2, -2, -42.5f);
            MaterialEditorRenameMaterial = Instantiate(MaterialEditorRenameList.Panel.transform.GetChild(0), MaterialEditorRenameList.Panel.transform).GetComponent<Text>();
            MaterialEditorRenameMaterial.gameObject.name = nameof(MaterialEditorRenameMaterial);
            MaterialEditorRenameMaterial.transform.SetRect(0, 1, 1, 1, 5, -20, -2, -5);
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
            if (MaterialEditorRendererList != null)
                MaterialEditorRendererList.Panel.transform.SetRect(1f, 0.5f, 1f, 1f, MarginSize, MarginSize / 2f, MarginSize + UIListWidth.Value);
            if (MaterialEditorMaterialList != null)
                MaterialEditorMaterialList.Panel.transform.SetRect(1f, 0f, 1f, 0.5f, MarginSize, 0f, MarginSize + UIListWidth.Value, -MarginSize);
        }

        /// <summary>
        /// Search text using wildcards.
        /// </summary>
        /// <param name="text">Text to search in</param>
        /// <param name="filter">Filter with which to search the text</param>
        internal static bool WildCardSearch(string text, string filter)
        {
            string regex = "^.*" + Regex.Escape(filter).Replace("\\?", ".").Replace("\\*", ".*") + ".*$";
            return Regex.IsMatch(text, regex, RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Populate the renderer list
        /// </summary>
        /// <param name="go">GameObject for which to read the renderers</param>
        /// <param name="data">Object that will be passed through to the get/set/reset events</param>
        /// <param name="rendListFull">List of all renderers to display</param>
        private void PopulateRendererList(GameObject go, object data, IEnumerable<Renderer> rendListFull)
        {
            if (go != CurrentGameObject)
            {
                SelectedRenderers.Clear();
                MaterialEditorRendererList.ClearList();

                foreach (var rend in rendListFull)
                    MaterialEditorRendererList.AddEntry(rend.NameFormatted(), value =>
                    {
                        if (value)
                            SelectedRenderers.Add(rend);
                        else
                            SelectedRenderers.Remove(rend);
                        PopulateList(go, data, CurrentFilter);
                        PopulateMaterialList(go, data, rendListFull);
                    });
                PopulateMaterialList(go, data, rendListFull);
            }
        }


        /// <summary>
        /// Populate the materials list
        /// </summary>
        /// <param name="go">GameObject for which to read the renderers</param>
        /// <param name="data">Object that will be passed through to the get/set/reset events</param>
        /// <param name="materials">List of all materials to display</param>
        private void PopulateMaterialList(GameObject go, object data, IEnumerable<Renderer> materials)
        {
            SelectedMaterials.Clear();
            MaterialEditorMaterialList.ClearList();

            foreach (var rend in materials.Where(rend => SelectedRenderers.Count == 0 || SelectedRenderers.Contains(rend)))
            foreach (var mat in GetMaterials(go, rend))
                MaterialEditorMaterialList.AddEntry(mat.NameFormatted(), value =>
                {
                    if (value)
                        SelectedMaterials.Add(mat);
                    else
                        SelectedMaterials.Remove(mat);
                    PopulateList(go, data, CurrentFilter);
                });
        }

        /// <summary>
        /// Populate the rename list
        /// </summary>
        /// <param name="go">GameObject for which to read the renderers</param>
        /// <param name="material">Material to be renamed</param>
        private void PopulateRenameList(GameObject go, Material material, object data)
        {
            SelectedMaterialRenderers.Clear();
            MaterialEditorRenameList.ClearList();

            // Setup title
            MaterialEditorRenameMaterial.text = material.NameFormatted();

            // Setup text field
            string formattedName = material.NameFormatted().Split(new[] { MaterialCopyPostfix }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            MaterialEditorRenameField.text = formattedName;

            // Setup button
            string suffix = material.NameFormatted().Replace(formattedName, "");
            MaterialEditorRenameButton.interactable = false;
            MaterialEditorRenameButton.onClick.RemoveAllListeners();
            MaterialEditorRenameButton.onClick.AddListener(() =>
            {
                string safeNewName = MaterialEditorRenameField.text.Replace(MaterialCopyPostfix, "").Trim() + suffix;
                foreach (var renderer in SelectedMaterialRenderers)
                    SetMaterialName(data, renderer, material, safeNewName, go);
                RefreshUI();
            });

            // Setup renderer list
            foreach (var renderer in GetRendererList(go))
            {
                if (!renderer.materials.Any(mat => mat.NameFormatted() == material.NameFormatted())) continue;
                MaterialEditorRenameList.AddEntry(renderer.NameFormatted(), value =>
                {
                    if (value)
                        SelectedMaterialRenderers.Add(renderer);
                    else
                        SelectedMaterialRenderers.Remove(renderer);
                    MaterialEditorRenameButton.interactable = SelectedMaterialRenderers.Count > 0;
                });
            }
        }

        /// <summary>
        /// Populate the MaterialEditor UI
        /// </summary>
        /// <param name="go">GameObject for which to read the renderers and materials</param>
        /// <param name="data">Object that will be passed through to the get/set/reset events</param>
        /// <param name="filter">Comma separated list of text to filter the results</param>
        protected void PopulateList(GameObject go, object data, string filter = null)
        {
            if (RenameListVisible)
            {
                MaterialEditorRenameList.ToggleVisibility(false);
                ViewListButton.GetComponentInChildren<Text>().text = ">";
                RenameListVisible = false;
            }

            if (filter == null)
            {
                if (PersistFilter.Value) filter = CurrentFilter;
                else filter = "";
            }

            MaterialEditorWindow.gameObject.SetActive(true);
            MaterialEditorWindow.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f / UIScale.Value, 1080f / UIScale.Value);
            SetMainRectWithMemory(0.05f, 0.05f, UIWidth.Value * UIScale.Value, UIHeight.Value * UIScale.Value);
            FilterInputField.Set(filter);

            if (go == null) return;

            List<Renderer> rendList = new List<Renderer>();
            IEnumerable<Renderer> rendListFull = GetRendererList(go);
            List<Projector> projectorList = new List<Projector>();
            IEnumerable<Projector> projectorListFull = GetProjectorList(data, go);
            List<string> filterList = new List<string>();
            List<string> filterListProperties = new List<string>();
            List<ItemInfo> items = new List<ItemInfo>();
            Dictionary<string, Material> matList = new Dictionary<string, Material>();

            PopulateRendererList(go, data, rendListFull);

            CurrentGameObject = go;
            CurrentData = data;
            CurrentFilter = filter;

            if (!filter.IsNullOrEmpty())
            {
                filterList = filter.Split(',').Select(x => x.Trim()).ToList();
                filterList.RemoveAll(x => x.IsNullOrWhiteSpace());

                filterListProperties = new List<string>(filterList);
                filterListProperties = filterListProperties.Where(x => x.StartsWith("_")).Select(x => x.Trim('_')).ToList();
                filterList = filterList.Where(x => !x.StartsWith("_")).ToList();
            }

            //Get all renderers and materials matching the filter
            if (SelectedRenderers.Count > 0)
                rendList.AddRange(SelectedRenderers);
            else if (filterList.Count == 0)
                rendList = rendListFull.ToList();
            else
            {
                foreach (var rend in rendListFull)
                {
                    foreach (string filterWord in filterList)
                        if (WildCardSearch(rend.NameFormatted(), filterWord.Trim()) && !rendList.Contains(rend))
                            rendList.Add(rend);

                    foreach (var mat in SelectedMaterials.Count == 0 ? GetMaterials(go, rend) : GetMaterials(go, rend).Where(mat => SelectedMaterials.Contains(mat)))
                    foreach (string filterWord in filterList)
                        if (WildCardSearch(mat.NameFormatted(), filterWord.Trim()))
                            matList[mat.NameFormatted()] = mat;
                }
                foreach (var projector in projectorListFull)
                foreach (string filterWord in filterList)
                    if (WildCardSearch(projector.NameFormatted(), filterWord.Trim()))
                        projectorList.Add(projector);
            }

            for (var i = 0; i < rendList.Count; i++)
            {
                var rend = rendList[i];
                //Get materials if materials list wasn't previously built by the filter    
                if (filterList.Count == 0)
                    foreach (var mat in SelectedMaterials.Count == 0 ? GetMaterials(go, rend) : GetMaterials(go, rend).Where(mat => SelectedMaterials.Contains(mat)))
                        matList[mat.NameFormatted()] = mat;

                var rendererItem = new ItemInfo(ItemInfo.RowItemType.Renderer, "Renderer")
                {
                    RendererName = rend.NameFormatted(),
                    ExportUVOnClick = () => Export.ExportUVMaps(rend),
                    ExportObjOnClick = () =>
                    {
                        ObjRenderer = rend;
                        DoObjExport = true;
                    },
                    SelectInterpolableButtonRendererOnClick = () => SelectInterpolableButtonOnClick(go, ItemInfo.RowItemType.Renderer, rendererName: rend.NameFormatted())
                };
                items.Add(rendererItem);

                //Renderer Enabled
                bool valueEnabledOriginal = rend.enabled;
                var temp = GetRendererPropertyValueOriginal(data, rend, RendererProperties.Enabled, go);
                if (!temp.IsNullOrEmpty())
                    valueEnabledOriginal = temp == "1";
                var rendererEnabledItem = new ItemInfo(ItemInfo.RowItemType.RendererEnabled, "Enabled")
                {
                    RendererEnabled = rend.enabled,
                    RendererEnabledOriginal = valueEnabledOriginal,
                    RendererEnabledOnChange = value => SetRendererProperty(data, rend, RendererProperties.Enabled, (value ? 1 : 0).ToString(), go),
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
                temp = GetRendererPropertyValueOriginal(data, rend, RendererProperties.ReceiveShadows, go);
                if (!temp.IsNullOrEmpty())
                    valueReceiveShadowsOriginal = temp == "1";
                var rendererReceiveShadowsItem = new ItemInfo(ItemInfo.RowItemType.RendererReceiveShadows, "Receive Shadows")
                {
                    RendererReceiveShadows = rend.receiveShadows,
                    RendererReceiveShadowsOriginal = valueReceiveShadowsOriginal,
                    RendererReceiveShadowsOnChange = value => SetRendererProperty(data, rend, RendererProperties.ReceiveShadows, (value ? 1 : 0).ToString(), go),
                    RendererReceiveShadowsOnReset = () => RemoveRendererProperty(data, rend, RendererProperties.ReceiveShadows, go)
                };
                items.Add(rendererReceiveShadowsItem);

                if (rend is SkinnedMeshRenderer meshRenderer) // recalculate normals should only exist on skinned renderers
                {
                    //Renderer UpdateWhenOffscreen
                    bool valueUpdateWhenOffscreenOriginal = meshRenderer.updateWhenOffscreen;
                    temp = GetRendererPropertyValueOriginal(data, rend, RendererProperties.UpdateWhenOffscreen, go);
                    if (!temp.IsNullOrEmpty())
                        valueUpdateWhenOffscreenOriginal = temp == "1";
                    var rendererUpdateWhenOffscreenItem = new ItemInfo(ItemInfo.RowItemType.RendererUpdateWhenOffscreen, "Update When Off-Screen")
                    {
                        RendererUpdateWhenOffscreen = meshRenderer.updateWhenOffscreen,
                        RendererUpdateWhenOffscreenOriginal = valueUpdateWhenOffscreenOriginal,
                        RendererUpdateWhenOffscreenOnChange = value => SetRendererProperty(data, rend, RendererProperties.UpdateWhenOffscreen, (value ? 1 : 0).ToString(), go),
                        RendererUpdateWhenOffscreenOnReset = () => RemoveRendererProperty(data, rend, RendererProperties.UpdateWhenOffscreen, go)
                    };
                    items.Add(rendererUpdateWhenOffscreenItem);

                    //Renderer RecalculateNormals
                    bool valueRecalculateNormalsOriginal = false; // this is not a real renderproperty so I cannot be true by default
                    temp = GetRendererPropertyValueOriginal(data, rend, RendererProperties.RecalculateNormals, go);
                    if (!temp.IsNullOrEmpty())
                        valueRecalculateNormalsOriginal = temp == "1";
                    bool valueRecalculateNormals = false; // actual value not storable in renderer, grab renderProperty stored by ME instead
                    temp = GetRendererPropertyValue(data, rend, RendererProperties.RecalculateNormals, go);
                    if (!temp.IsNullOrEmpty())
                        valueRecalculateNormals = temp == "1";
                    var rendererRecalculateNormalsItem = new ItemInfo(ItemInfo.RowItemType.RendererRecalculateNormals, "Recalculate Normals")
                    {
                        RendererRecalculateNormals = valueRecalculateNormals,
                        RendererRecalculateNormalsOriginal = valueRecalculateNormalsOriginal,
                        RendererRecalculateNormalsOnChange = value => SetRendererProperty(data, rend, RendererProperties.RecalculateNormals, (value ? 1 : 0).ToString(), go),
                        RendererRecalculateNormalsOnReset = () => RemoveRendererProperty(data, rend, RendererProperties.RecalculateNormals, go)
                    };
                    items.Add(rendererRecalculateNormalsItem);
                }
            }

            foreach (var mat in matList.Values)
                PopulateListMaterial(mat);

            foreach (var projector in filterList.Count == 0 ? projectorListFull : projectorList)
                PopulateListMaterial(projector.material, projector);

            VirtualList.SetList(items);

            void PopulateListMaterial(Material mat, Projector projector = null)
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
                //Projectors only support 1 material. Copy button is hidden if the function is null
                if (projector == null)
                    materialItem.MaterialOnCopyRemove = () =>
                    {
                        MaterialCopyRemove(data, mat, go);
                        PopulateList(go, data, filter);
                        PopulateMaterialList(go, data, rendListFull);
                    };
                materialItem.MaterialOnRename = () =>
                {
                    if (ListsVisible)
                    {
                        MaterialEditorRendererList.ToggleVisibility(false);
                        MaterialEditorMaterialList.ToggleVisibility(false);
                        ListsVisible = false;
                    }
                    ViewListButton.GetComponentInChildren<Text>().text = "<";
                    MaterialEditorRenameList.ToggleVisibility(true);
                    PopulateRenameList(go, mat, data);
                    RenameListVisible = true;
                };
                items.Add(materialItem);

                if (projector != null)
                    PopulateProjectorSettings(projector);

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
                    },
                    SelectInterpolableButtonShaderOnClick = () => SelectInterpolableButtonOnClick(go, ItemInfo.RowItemType.Shader, materialName)
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

                // Shader property organizer
                var categories = PropertyOrganizer.PropertyOrganization[XMLShaderProperties.ContainsKey(shaderName) ? shaderName : "default"];

                foreach (var category in categories)
                {
                    // Blacklist and filter
                    if (!category.Value.Any(p =>
                        !Instance.CheckBlacklist(materialName, p.Name)
                        && (filterListProperties.Count == 0 || filterListProperties.Any(fw => WildCardSearch(p.Name, fw))))
                    ) continue;

                    if (categories.Count > 1 || category.Key != PropertyOrganizer.UncategorizedName) {
                        var categoryItem = new ItemInfo(ItemInfo.RowItemType.PropertyCategory, category.Key);
                        items.Add(categoryItem);
                    }

                foreach (var property in category.Value)
                {
                    string propertyName = property.Name;
                    // Blacklist
                    if (Instance.CheckBlacklist(materialName, propertyName)) continue;
                    // Filter
                    if (!(filterListProperties.Count == 0 || filterListProperties.Any(fw => WildCardSearch(propertyName, fw)))) continue;

                    if (property.Type == ShaderPropertyType.Texture)
                    {
                        if (mat.HasProperty($"_{propertyName}"))
                        {
                            var textureItem = new ItemInfo(ItemInfo.RowItemType.TextureProperty, propertyName)
                            {
                                TextureChanged = !GetMaterialTextureValueOriginal(data, mat, propertyName, go),
                                TextureExists = mat.GetTexture($"_{propertyName}") != null,
                                TextureOnExport = () => ExportTexture(mat, propertyName),
                                SelectInterpolableButtonTextureOnClick = () => SelectInterpolableButtonOnClick(go, ItemInfo.RowItemType.TextureProperty, materialName, propertyName)
                            };
                            textureItem.TextureOnImport = () =>
                            {
#if !API
                                string fileFilter = KK_Plugins.ImageHelper.FileFilter;
#else
                                string fileFilter = "Images (*.png;.jpg)|*.png;*.jpg|All files|*.*";
#endif
                                KKAPI.Utilities.OpenFileDialog.Show(OnFileAccept, "Open image", ExportPath, fileFilter, ".png");

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
                    else if (property.Type == ShaderPropertyType.Color)
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
                                ColorValueOnReset = () => RemoveMaterialColorProperty(data, mat, propertyName, go),
                                ColorValueOnEdit = (title, value, onChanged) => SetupColorPalette(data, mat, $"Material Editor - {title}", value, onChanged, true),
                                ColorValueSetToPalette = (title, value) => SetColorToPalette(data, mat, $"Material Editor - {title}", value),
                                SelectInterpolableButtonColorOnClick = () => SelectInterpolableButtonOnClick(go, ItemInfo.RowItemType.ColorProperty, materialName, propertyName)
                            };
                            items.Add(contentItem);
                        }
                    }
                    else if (property.Type == ShaderPropertyType.Float)
                    {
                        if (mat.HasProperty($"_{propertyName}"))
                        {
                            float valueFloatOriginal = mat.GetFloat($"_{propertyName}");
                            float? valueFloatOriginalTemp = GetMaterialFloatPropertyValueOriginal(data, mat, propertyName, go);
                            if (valueFloatOriginalTemp != null)
                                valueFloatOriginal = (float)valueFloatOriginalTemp;

                            AddFloatslider
                            (
                                valueFloat: mat.GetFloat($"_{propertyName}"),
                                propertyName: propertyName,
                                onInteroperableClick: () => SelectInterpolableButtonOnClick(go, ItemInfo.RowItemType.FloatProperty, materialName, propertyName),
                                changeValue: value => SetMaterialFloatProperty(data, mat, propertyName, value, go),
                                resetValue: () => RemoveMaterialFloatProperty(data, mat, propertyName, go),
                                valueFloatOriginal: valueFloatOriginal,
                                minValue: property.MinValue,
                                maxValue: property.MaxValue
                            );
                        }
                    }
                    else if (property.Type == ShaderPropertyType.Keyword)
                    {
                        // Since there's no way to check if a Keyword exists, we'll have to trust the XML.
                        bool valueKeyword = mat.IsKeywordEnabled($"_{propertyName}");
                        bool valueKeywordOriginal = valueKeyword;
                        bool? valueKeywordOriginalTemp = GetMaterialKeywordPropertyValueOriginal(data, mat, propertyName, go);

                        if (valueKeywordOriginalTemp != null)
                            valueKeywordOriginal = (bool)valueKeywordOriginalTemp;

                        var contentItem = new ItemInfo(ItemInfo.RowItemType.KeywordProperty, propertyName)
                        {
                            KeywordValue = valueKeyword,
                            KeywordValueOriginal = valueKeywordOriginal,
                            KeywordValueOnChange = value => SetMaterialKeywordProperty(data, mat, propertyName, value, go),
                            KeywordValueOnReset = () => RemoveMaterialKeywordProperty(data, mat, propertyName, go)
                        };

                        items.Add(contentItem);
                    }
                }
                }
            }

            void PopulateProjectorSettings(Projector projector)
            {
                foreach (var property in Enum.GetValues(typeof(ProjectorProperties)).Cast<ProjectorProperties>())
                {
                    float maxValue = 100f;
                    string name = "";
                    float valueFloat = 0f;
                    float? originalValueTemp = GetProjectorPropertyValueOriginal(data, projector, property, go);

                    switch (property)
                    {
                        case ProjectorProperties.Enabled:
                            name = "Enabled";
                            valueFloat = Convert.ToSingle(projector.enabled);
                            maxValue = 1f;
                            break;
                        case ProjectorProperties.NearClipPlane:
                            name = "Near Clip Plane";
                            valueFloat = projector.nearClipPlane;
                            maxValue = ProjectorNearClipPlaneMax.Value;
                            break;
                        case ProjectorProperties.FarClipPlane:
                            name = "Far Clip Plane";
                            valueFloat = projector.farClipPlane;
                            maxValue = ProjectorFarClipPlaneMax.Value;
                            break;
                        case ProjectorProperties.FieldOfView:
                            name = "Field Of View";
                            valueFloat = projector.fieldOfView;
                            maxValue = ProjectorFieldOfViewMax.Value;
                            break;
                        case ProjectorProperties.AspectRatio:
                            name = "Aspect Ratio";
                            valueFloat = projector.aspectRatio;
                            maxValue = ProjectorAspectRatioMax.Value;
                            break;
                        case ProjectorProperties.Orthographic:
                            name = "Orthographic";
                            valueFloat = Convert.ToSingle(projector.orthographic);
                            maxValue = 1f;
                            break;
                        case ProjectorProperties.OrthographicSize:
                            name = "Orthographic Size";
                            valueFloat = projector.orthographicSize;
                            maxValue = ProjectorOrthographicSizeMax.Value;
                            break;
                        case ProjectorProperties.IgnoreMapLayer:
                            name = "Ignore Map layer";
                            valueFloat = Convert.ToSingle(projector.ignoreLayers == (projector.ignoreLayers | (1 << 11)));
                            maxValue = 1f;
                            break;
                        case ProjectorProperties.IgnoreCharaLayer:
                            name = "Ignore Chara Layer";
                            valueFloat = Convert.ToSingle(projector.ignoreLayers == (projector.ignoreLayers | (1 << 10)));
                            maxValue = 1f;
                            break;
                    }

                    if (filterListProperties.Count == 0 || filterListProperties.Any(filterWord => WildCardSearch(name, filterWord)))
                        AddFloatslider
                        (
                            valueFloat: valueFloat,
                            propertyName: name,
                            onInteroperableClick: () => SelectProjectorInterpolableButtonOnClick(go, property, projector.NameFormatted()),
                            changeValue: value => SetProjectorProperty(data, projector, property, value, projector.gameObject),
                            resetValue: () => RemoveProjectorProperty(data, projector, property, projector.gameObject),
                            valueFloatOriginal: originalValueTemp != null ? (float)originalValueTemp : valueFloat,
                            minValue: 0f,
                            maxValue: maxValue
                        );
                }
            }

            void AddFloatslider(
                float valueFloat,
                string propertyName,
                Action onInteroperableClick,
                Action<float> changeValue,
                Action resetValue,
                float valueFloatOriginal,
                float? minValue = null,
                float? maxValue = null)
            {
                var contentItem = new ItemInfo(ItemInfo.RowItemType.FloatProperty, propertyName)
                {
                    FloatValue = valueFloat, FloatValueOriginal = valueFloatOriginal, SelectInterpolableButtonFloatOnClick = () => onInteroperableClick()
                };
                if (minValue != null)
                    contentItem.FloatValueSliderMin = (float)minValue;
                if (maxValue != null)
                    contentItem.FloatValueSliderMax = (float)maxValue;
                contentItem.FloatValueOnChange = value => changeValue(value);
                contentItem.FloatValueOnReset = () => resetValue();
                items.Add(contentItem);
            }
        }

        /// <summary>
        /// Obj export should be done in OnGUI or something similarly late so that finger rotation is exported properly
        /// </summary>
        private void OnGUI()
        {
            if (DoObjExport)
            {
                DoObjExport = false;
                Export.ExportObj(ObjRenderer);
                ObjRenderer = null;
            }
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
            var matName = mat.NameFormatted();
            matName = string.Concat(matName.Split(Path.GetInvalidFileNameChars())).Trim();
            string filename = Path.Combine(ExportPath, $"_Export_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}_{matName}_{property}.png");
            Instance.ConvertNormalMap(ref tex, property);
            SaveTex(tex, filename);
            MaterialEditorPluginBase.Logger.LogInfo($"Exported {filename}");
            Utilities.OpenFileInExplorer(filename);
        }

        public abstract string GetRendererPropertyValueOriginal(object data, Renderer renderer, RendererProperties property, GameObject gameObject);
        public abstract string GetRendererPropertyValue(object data, Renderer renderer, RendererProperties property, GameObject gameObject);
        public abstract void SetRendererProperty(object data, Renderer renderer, RendererProperties property, string value, GameObject gameObject);
        public abstract void RemoveRendererProperty(object data, Renderer renderer, RendererProperties property, GameObject gameObject);

        public abstract float? GetProjectorPropertyValueOriginal(object data, Projector renderer, ProjectorProperties property, GameObject gameObject);
        public abstract float? GetProjectorPropertyValue(object data, Projector renderer, ProjectorProperties property, GameObject gameObject);
        public abstract void SetProjectorProperty(object data, Projector projector, ProjectorProperties property, float value, GameObject gameObject);
        public abstract void RemoveProjectorProperty(object data, Projector projector, ProjectorProperties property, GameObject gameObject);
        public abstract IEnumerable<Projector> GetProjectorList(object data, GameObject gameObject);

        public abstract void MaterialCopyEdits(object data, Material material, GameObject gameObject);
        public abstract void MaterialPasteEdits(object data, Material material, GameObject gameObject);
        public abstract void MaterialCopyRemove(object data, Material material, GameObject gameObject);

        public abstract string GetMaterialNameOriginal(object data, Renderer renderer, Material material, GameObject gameObject);
        public abstract void SetMaterialName(object data, Renderer renderer, Material material, string value, GameObject gameObject);
        public abstract void RemoveMaterialName(object data, Renderer renderer, Material material, GameObject gameObject);

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

        public abstract bool? GetMaterialKeywordPropertyValueOriginal(object data, Material material, string propertyName, GameObject gameObject);
        public abstract void SetMaterialKeywordProperty(object data, Material material, string propertyName, bool value, GameObject gameObject);
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

        private void SelectInterpolableButtonOnClick(GameObject go, ItemInfo.RowItemType rowType, string materialName = "", string propertyName = "", string rendererName = "")
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
            public ItemInfo.RowItemType RowType;

            public SelectedInterpolable(GameObject go, ItemInfo.RowItemType rowType, string materialName, string propertyName, string rendererName)
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
