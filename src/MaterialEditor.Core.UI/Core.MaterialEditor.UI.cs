using BepInEx;
using KKAPI.Maker;
using KKAPI.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UILib;
using UnityEngine;
using UnityEngine.UI;
using static KK_Plugins.MaterialEditor.MaterialAPI;
using static KK_Plugins.MaterialEditor.MaterialEditorPlugin;
#if AI || HS2
using AIChara;
using ChaClothesComponent = AIChara.CmpClothes;
using ChaCustomHairComponent = AIChara.CmpHair;
using ChaAccessoryComponent = AIChara.CmpAccessory;
#endif

namespace KK_Plugins.MaterialEditor
{
    /// <summary>
    /// Code for the MaterialEditor UI
    /// </summary>
    public abstract class UI : BaseUnityPlugin
    {
        internal static Canvas MaterialEditorWindow;
        private static Image MaterialEditorMainPanel;
        private static ScrollRect MaterialEditorScrollableUI;
        private static InputField FilterInputField;
        internal static Dropdown ItemTypeDropDown;

        internal static FileSystemWatcher TexChangeWatcher;
        private VirtualList virtualList;

        internal const float marginSize = 5f;
        internal const float headerSize = 20f;
        internal const float scrollOffsetX = -15f;
        internal const float panelHeight = 20f;
        internal const float labelWidth = 50f;
        internal const float buttonWidth = 100f;
        internal const float dropdownWidth = 100f;
        internal const float textBoxWidth = 75f;
        internal const float colorLabelWidth = 10f;
        internal const float resetButtonWidth = 50f;
        internal const float sliderWidth = 150f;
        internal const float labelXWidth = 60f;
        internal const float labelYWidth = 10f;
        internal const float textBoxXYWidth = 50f;
        internal static readonly RectOffset padding = new RectOffset(3, 3, 0, 1);
        internal static readonly Color rowColor = new Color(1f, 1f, 1f, 1f);

        private GameObject CurrentGameObject;
        private int CurrentSlot;
        private string CurrentFilter = "";

        internal void Main()
        {
            UIScale.SettingChanged += UISettingChanged;
            UIWidth.SettingChanged += UISettingChanged;
            UIHeight.SettingChanged += UISettingChanged;
        }

        /// <summary>
        /// Initialize the MaterialEditor UI
        /// </summary>
        protected void InitUI()
        {
            UIUtility.Init(nameof(KK_Plugins));

            MaterialEditorWindow = UIUtility.CreateNewUISystem("MaterialEditorCanvas");
            MaterialEditorWindow.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f / UIScale.Value, 1080f / UIScale.Value);
            Visible = false;
            MaterialEditorWindow.gameObject.transform.SetParent(transform);
            MaterialEditorWindow.sortingOrder = 1000;

            MaterialEditorMainPanel = UIUtility.CreatePanel("Panel", MaterialEditorWindow.transform);
            MaterialEditorMainPanel.color = Color.white;
            MaterialEditorMainPanel.transform.SetRect(0.05f, 0.05f, UIWidth.Value * UIScale.Value, UIHeight.Value * UIScale.Value);

            UIUtility.AddOutlineToObject(MaterialEditorMainPanel.transform, Color.black);

            var drag = UIUtility.CreatePanel("Draggable", MaterialEditorMainPanel.transform);
            drag.transform.SetRect(0f, 1f, 1f, 1f, 0f, -headerSize);
            drag.color = Color.gray;
            UIUtility.MakeObjectDraggable(drag.rectTransform, MaterialEditorMainPanel.rectTransform);

            var nametext = UIUtility.CreateText("Nametext", drag.transform, "Material Editor");
            nametext.transform.SetRect();
            nametext.alignment = TextAnchor.MiddleCenter;

            FilterInputField = UIUtility.CreateInputField("Filter", drag.transform, "Filter");
            FilterInputField.transform.SetRect(0f, 0f, 0f, 1f, 0f, 0f, 100f);
            FilterInputField.onValueChanged.AddListener(RefreshUI);

            ItemTypeDropDown = UIUtility.CreateDropdown("ItemType", drag.transform);
            ItemTypeDropDown.transform.SetRect(1f, 0f, 1f, 1f, -200f, 0f, -20f);
            ItemTypeDropDown.captionText.transform.SetRect(0.05f, 0f, 1f, 1f, 5f, 2f, -15f, -2f);
            ItemTypeDropDown.captionText.alignment = TextAnchor.MiddleLeft;
            ItemTypeDropDown.gameObject.SetActive(false);

            var close = UIUtility.CreateButton("CloseButton", drag.transform, "");
            close.transform.SetRect(1f, 0f, 1f, 1f, -20f);
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
            MaterialEditorScrollableUI.transform.SetRect(0f, 0f, 1f, 1f, marginSize, marginSize, -marginSize, -headerSize - marginSize / 2f);
            MaterialEditorScrollableUI.gameObject.AddComponent<Mask>();
            MaterialEditorScrollableUI.content.gameObject.AddComponent<VerticalLayoutGroup>();
            MaterialEditorScrollableUI.content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            MaterialEditorScrollableUI.verticalScrollbar.GetComponent<RectTransform>().offsetMin = new Vector2(scrollOffsetX, 0f);
            MaterialEditorScrollableUI.viewport.offsetMax = new Vector2(scrollOffsetX, 0f);
            MaterialEditorScrollableUI.movementType = ScrollRect.MovementType.Clamped;

            var template = ItemTemplate.CreateTemplate(MaterialEditorScrollableUI.content.transform);

            virtualList = MaterialEditorScrollableUI.gameObject.AddComponent<VirtualList>();
            virtualList.ScrollRect = MaterialEditorScrollableUI;
            virtualList.EntryTemplate = template;
            virtualList.Initialize();
        }

        /// <summary>
        /// Refresh the MaterialEditor UI
        /// </summary>
        public void RefreshUI() => RefreshUI(CurrentFilter);
        /// <summary>
        /// Refresh the MaterialEditor UI using the specified filter text
        /// </summary>
        public void RefreshUI(string filterText) => PopulateList(CurrentGameObject, CurrentSlot, filterText);

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

        private static void UISettingChanged(object sender, EventArgs e)
        {
            if (MaterialEditorWindow != null)
                MaterialEditorWindow.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f / UIScale.Value, 1080f / UIScale.Value);
            if (MaterialEditorMainPanel != null)
                MaterialEditorMainPanel.transform.SetRect(0.05f, 0.05f, UIWidth.Value * UIScale.Value, UIHeight.Value * UIScale.Value);
        }

        /// <summary>
        /// Populate the MaterialEditor UI
        /// </summary>
        /// <param name="go">GameObject for which to read the renderers and materials</param>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory, or ID of the Studio item. Ignored for other object types.</param>
        /// <param name="filter">Comma separated list of text to filter the results</param>
        protected void PopulateList(GameObject go, int slot = 0, string filter = "")
        {
            MaterialEditorWindow.gameObject.SetActive(true);
            MaterialEditorWindow.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f / UIScale.Value, 1080f / UIScale.Value);
            MaterialEditorMainPanel.transform.SetRect(0.05f, 0.05f, UIWidth.Value * UIScale.Value, UIHeight.Value * UIScale.Value);
            FilterInputField.Set(filter);

            CurrentGameObject = go;
            CurrentSlot = slot;
            CurrentFilter = filter;

            if (go == null) return;

            List<Renderer> rendList = new List<Renderer>();
            List<Renderer> rendListFull = GetRendererList(go);
            List<string> filterList = new List<string>();
            List<ItemInfo> items = new List<ItemInfo>();
            Dictionary<string, Material> matList = new Dictionary<string, Material>();

            if (!filter.IsNullOrEmpty())
                filterList = filter.Split(',').ToList();
            filterList.RemoveAll(x => x.IsNullOrWhiteSpace());

            //Get all renderers and materials matching the filter
            if (filterList.Count == 0)
                rendList = rendListFull;
            else
                for (var i = 0; i < rendListFull.Count; i++)
                {
                    var rend = rendListFull[i];
                    for (var j = 0; j < filterList.Count; j++)
                    {
                        var filterWord = filterList[j];
                        if (rend.NameFormatted().ToLower().Contains(filterWord.Trim().ToLower()) && !rendList.Contains(rend))
                            rendList.Add(rend);
                    }

                    var mats = GetMaterials(rend);
                    for (var j = 0; j < mats.Count; j++)
                    {
                        var mat = mats[j];
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
                    foreach (var mat in GetMaterials(rend))
                        matList[mat.NameFormatted()] = mat;

                var rendererItem = new ItemInfo(ItemInfo.RowItemType.Renderer, "Renderer");
                rendererItem.RendererName = rend.NameFormatted();
                rendererItem.ExportUVOnClick = () => Export.ExportUVMaps(rend);
                rendererItem.ExportObjOnClick = () => Export.ExportObj(rend);
                items.Add(rendererItem);

                //Renderer Enabled
                bool valueEnabledOriginal = rend.enabled;
                var temp = GetRendererPropertyValueOriginal(slot, rend, RendererProperties.Enabled, go);
                if (!temp.IsNullOrEmpty())
                    valueEnabledOriginal = temp == "1";
                var rendererEnabledItem = new ItemInfo(ItemInfo.RowItemType.RendererEnabled, "Enabled");
                rendererEnabledItem.RendererEnabled = rend.enabled ? 1 : 0;
                rendererEnabledItem.RendererEnabledOriginal = valueEnabledOriginal ? 1 : 0;
                rendererEnabledItem.RendererEnabledOnChange = value => SetRendererProperty(slot, rend, RendererProperties.Enabled, value.ToString(), go);
                rendererEnabledItem.RendererEnabledOnReset = () => RemoveRendererProperty(slot, rend, RendererProperties.Enabled, go);
                items.Add(rendererEnabledItem);

                //Renderer ShadowCastingMode
                var valueShadowCastingModeOriginal = rend.shadowCastingMode;
                temp = GetRendererPropertyValueOriginal(slot, rend, RendererProperties.ShadowCastingMode, go);
                if (!temp.IsNullOrEmpty())
                    valueShadowCastingModeOriginal = (UnityEngine.Rendering.ShadowCastingMode)int.Parse(temp);
                var rendererShadowCastingModeItem = new ItemInfo(ItemInfo.RowItemType.RendererShadowCastingMode, "Shadow Casting Mode");
                rendererShadowCastingModeItem.RendererShadowCastingMode = (int)rend.shadowCastingMode;
                rendererShadowCastingModeItem.RendererShadowCastingModeOriginal = (int)valueShadowCastingModeOriginal;
                rendererShadowCastingModeItem.RendererShadowCastingModeOnChange = value => SetRendererProperty(slot, rend, RendererProperties.ShadowCastingMode, value.ToString(), go);
                rendererShadowCastingModeItem.RendererShadowCastingModeOnReset = () => RemoveRendererProperty(slot, rend, RendererProperties.ShadowCastingMode, go);
                items.Add(rendererShadowCastingModeItem);

                //Renderer ReceiveShadows
                bool valueReceiveShadowsOriginal = rend.receiveShadows;
                temp = GetRendererPropertyValueOriginal(slot, rend, RendererProperties.ShadowCastingMode, go);
                if (!temp.IsNullOrEmpty())
                    valueReceiveShadowsOriginal = temp == "1";
                var rendererReceiveShadowsItem = new ItemInfo(ItemInfo.RowItemType.RendererReceiveShadows, "Receive Shadows");
                rendererReceiveShadowsItem.RendererReceiveShadows = rend.receiveShadows ? 1 : 0;
                rendererReceiveShadowsItem.RendererReceiveShadowsOriginal = valueReceiveShadowsOriginal ? 1 : 0;
                rendererReceiveShadowsItem.RendererReceiveShadowsOnChange = value => SetRendererProperty(slot, rend, RendererProperties.ReceiveShadows, value.ToString(), go);
                rendererReceiveShadowsItem.RendererReceiveShadowsOnReset = () => RemoveRendererProperty(slot, rend, RendererProperties.ReceiveShadows, go);
                items.Add(rendererReceiveShadowsItem);
            }

            foreach (var mat in matList.Values)
            {
                string materialName = mat.NameFormatted();
                string shaderName = mat.shader.NameFormatted();

                var materialItem = new ItemInfo(ItemInfo.RowItemType.Material, "Material");
                materialItem.MaterialName = materialName;
                materialItem.MaterialOnCopy = () => MaterialCopyEdits(slot, mat, go);
                materialItem.MaterialOnPaste = () =>
                {
                    MaterialPasteEdits(slot, mat, go);
                    PopulateList(go, slot, filter);
                };
                //materialItem.MaterialOnCopyRemove = () =>
                //{
                //    CopyMaterial(gameObject, materialName);
                //    PopulateList(gameObject, slot, filter);
                //};
                items.Add(materialItem);

                //Shader
                string shaderNameOriginal = shaderName;
                var temp = GetMaterialShaderNameOriginal(slot, mat, go);
                if (!temp.IsNullOrEmpty())
                    shaderNameOriginal = temp;
                var shaderItem = new ItemInfo(ItemInfo.RowItemType.Shader, "Shader");
                shaderItem.ShaderName = shaderName;
                shaderItem.ShaderNameOriginal = shaderNameOriginal;
                shaderItem.ShaderNameOnChange = value =>
                {
                    SetMaterialShaderName(slot, mat, value, go);
                    StartCoroutine(PopulateListCoroutine(go, slot, filter));
                };
                shaderItem.ShaderNameOnReset = () =>
                {
                    RemoveMaterialShaderName(slot, mat, go);
                    StartCoroutine(PopulateListCoroutine(go, slot, filter));
                };
                items.Add(shaderItem);

                //Shader RenderQueue
                int renderQueueOriginal = mat.renderQueue;
                int? renderQueueOriginalTemp = GetMaterialShaderRenderQueueOriginal(slot, mat, go);
                renderQueueOriginal = renderQueueOriginalTemp ?? renderQueueOriginal;
                var shaderRenderQueueItem = new ItemInfo(ItemInfo.RowItemType.ShaderRenderQueue, "Render Queue");
                shaderRenderQueueItem.ShaderRenderQueue = mat.renderQueue;
                shaderRenderQueueItem.ShaderRenderQueueOriginal = renderQueueOriginal;
                shaderRenderQueueItem.ShaderRenderQueueOnChange = value => SetMaterialShaderRenderQueue(slot, mat, value, go);
                shaderRenderQueueItem.ShaderRenderQueueOnReset = () => RemoveMaterialShaderRenderQueue(slot, mat, go);
                items.Add(shaderRenderQueueItem);

                foreach (var property in XMLShaderProperties[XMLShaderProperties.ContainsKey(shaderName) ? shaderName : "default"].OrderBy(x => x.Value.Type).ThenBy(x => x.Key))
                {
                    string propertyName = property.Key;
                    if (CheckBlacklist(materialName, propertyName)) continue;

                    if (property.Value.Type == ShaderPropertyType.Texture)
                    {
                        if (mat.HasProperty($"_{propertyName}"))
                        {
                            var textureItem = new ItemInfo(ItemInfo.RowItemType.TextureProperty, propertyName);
                            textureItem.TextureChanged = !GetMaterialTextureValueOriginal(slot, mat, propertyName, go);
                            textureItem.TextureExists = mat.GetTexture($"_{propertyName}") != null;
                            textureItem.TextureOnExport = () => ExportTexture(mat, propertyName);
                            textureItem.TextureOnImport = () =>
                            {
                                OpenFileDialog.Show(OnFileAccept, "Open image", Application.dataPath, FileFilter, FileExt);

                                void OnFileAccept(string[] strings)
                                {
                                    if (strings == null || strings.Length == 0 || strings[0].IsNullOrEmpty())
                                    {
                                        textureItem.TextureChanged = !GetMaterialTextureValueOriginal(slot, mat, propertyName, go);
                                        textureItem.TextureExists = mat.GetTexture($"_{propertyName}") != null;
                                        return;
                                    }
                                    string filePath = strings[0];

                                    SetMaterialTexture(slot, mat, propertyName, filePath, go);

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
                                                    SetMaterialTexture(slot, mat, propertyName, filePath, go);
                                            };
                                            TexChangeWatcher.Deleted += (sender, args) => TexChangeWatcher?.Dispose();
                                            TexChangeWatcher.Error += (sender, args) => TexChangeWatcher?.Dispose();
                                            TexChangeWatcher.EnableRaisingEvents = true;
                                        }
                                    }
                                }
                            };
                            textureItem.TextureOnReset = () => RemoveMaterialTexture(slot, mat, propertyName, go);
                            items.Add(textureItem);

                            Vector2 textureOffset = mat.GetTextureOffset($"_{propertyName}");
                            Vector2 textureOffsetOriginal = textureOffset;
                            Vector2? textureOffsetOriginalTemp = GetMaterialTextureOffsetOriginal(slot, mat, propertyName, go);
                            if (textureOffsetOriginalTemp != null)
                                textureOffsetOriginal = (Vector2)textureOffsetOriginalTemp;

                            Vector2 textureScale = mat.GetTextureScale($"_{propertyName}");
                            Vector2 textureScaleOriginal = textureScale;
                            Vector2? textureScaleOriginalTemp = GetMaterialTextureScaleOriginal(slot, mat, propertyName, go);
                            if (textureScaleOriginalTemp != null)
                                textureScaleOriginal = (Vector2)textureScaleOriginalTemp;

                            var textureItemOffsetScale = new ItemInfo(ItemInfo.RowItemType.TextureOffsetScale);
                            textureItemOffsetScale.Offset = textureOffset;
                            textureItemOffsetScale.OffsetOriginal = textureOffsetOriginal;
                            textureItemOffsetScale.OffsetOnChange = value => SetMaterialTextureOffset(slot, mat, propertyName, value, go);
                            textureItemOffsetScale.OffsetOnReset = () => RemoveMaterialTextureOffset(slot, mat, propertyName, go);
                            textureItemOffsetScale.Scale = textureScale;
                            textureItemOffsetScale.ScaleOriginal = textureScaleOriginal;
                            textureItemOffsetScale.ScaleOnChange = value => SetMaterialTextureScale(slot, mat, propertyName, value, go);
                            textureItemOffsetScale.ScaleOnReset = () => RemoveMaterialTextureScale(slot, mat, propertyName, go);
                            items.Add(textureItemOffsetScale);
                        }

                    }
                    else if (property.Value.Type == ShaderPropertyType.Color)
                    {
                        if (mat.HasProperty($"_{propertyName}"))
                        {
                            Color valueColor = mat.GetColor($"_{propertyName}");
                            Color valueColorOriginal = valueColor;
                            Color? c = GetMaterialColorPropertyValueOriginal(slot, mat, propertyName, go);
                            if (c != null)
                                valueColorOriginal = (Color)c;
                            var contentItem = new ItemInfo(ItemInfo.RowItemType.ColorProperty, propertyName);
                            contentItem.ColorValue = valueColor;
                            contentItem.ColorValueOriginal = valueColorOriginal;
                            contentItem.ColorValueOnChange = value => SetMaterialColorProperty(slot, mat, propertyName, value, go);
                            contentItem.ColorValueOnReset = () => RemoveMaterialColorProperty(slot, mat, propertyName, go);
                            items.Add(contentItem);
                        }
                    }
                    else if (property.Value.Type == ShaderPropertyType.Float)
                    {
                        if (mat.HasProperty($"_{propertyName}"))
                        {
                            float valueFloat = mat.GetFloat($"_{propertyName}");
                            float valueFloatOriginal = valueFloat;
                            float? valueFloatOriginalTemp = GetMaterialFloatPropertyValueOriginal(slot, mat, propertyName, go);
                            if (valueFloatOriginalTemp != null)
                                valueFloatOriginal = (float)valueFloatOriginalTemp;
                            var contentItem = new ItemInfo(ItemInfo.RowItemType.FloatProperty, propertyName);
                            contentItem.FloatValue = valueFloat;
                            contentItem.FloatValueOriginal = valueFloatOriginal;
                            if (property.Value.MinValue != null)
                                contentItem.FloatValueSliderMin = (float)property.Value.MinValue;
                            if (property.Value.MaxValue != null)
                                contentItem.FloatValueSliderMax = (float)property.Value.MaxValue;
                            contentItem.FloatValueOnChange = value => SetMaterialFloatProperty(slot, mat, propertyName, value, go);
                            contentItem.FloatValueOnReset = () => RemoveMaterialFloatProperty(slot, mat, propertyName, go);
                            items.Add(contentItem);
                        }
                    }
                }
            }

            virtualList.SetList(items);
        }
        /// <summary>
        /// Hacky workaround to wait for the dropdown fade to complete before refreshing
        /// </summary>
        protected IEnumerator PopulateListCoroutine(GameObject go, int slot = 0, string filter = "")
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
            PopulateList(go, slot, filter);
        }

        /// <summary>
        /// Populate the ItemType dropdown for switching between displaying various types of items on a character
        /// </summary>
        protected void PopulateItemTypeDropdown(ChaControl chaControl)
        {
            ItemTypeDropDown.onValueChanged.RemoveAllListeners();
            ItemTypeDropDown.onValueChanged.AddListener(value => ChangeItemType(value, chaControl));
            ItemTypeDropDown.options.Clear();
            ItemTypeDropDown.options.Add(new Dropdown.OptionData("Body"));
            ItemTypeDropDown.captionText.text = "Body";

            for (var i = 0; i < chaControl.objClothes.Length; i++)
                if (chaControl.objClothes[i] != null && chaControl.objClothes[i].GetComponentInChildren<ChaClothesComponent>() != null)
                    ItemTypeDropDown.options.Add(new Dropdown.OptionData($"Clothes {ClothesIndexToString(i)}"));
            for (var i = 0; i < chaControl.objHair.Length; i++)
                if (chaControl.objHair[i] != null && chaControl.objHair[i].GetComponent<ChaCustomHairComponent>() != null)
                    ItemTypeDropDown.options.Add(new Dropdown.OptionData($"Hair {HairIndexToString(i)}"));
            for (var i = 0; i < chaControl.objAccessory.Length; i++)
                if (chaControl.objAccessory[i] != null && chaControl.GetAccessory(i) != null)
                    ItemTypeDropDown.options.Add(new Dropdown.OptionData($"Accessory {AccessoryIndexToString(i)}"));
        }

        private void ChangeItemType(int selectedItem, ChaControl chaControl)
        {
            var option = ItemTypeDropDown.OptionText(selectedItem).Split(' ');
            int index = 0;

            if (chaControl == null)
                PopulateList(null);

            switch (option[0])
            {
                case "Body":
                    PopulateList(chaControl.gameObject);
                    break;
                case "Clothes":
                    if (option.Length > 1)
                        index = ClothesStringToIndex(option[1]);

                    if (index == -1 || chaControl.objClothes[index] == null || chaControl.objClothes[index].GetComponentInChildren<ChaClothesComponent>() == null)
                        PopulateList(chaControl.gameObject);
                    else
                        PopulateList(chaControl.objClothes[index], index);
                    break;
                case "Hair":
                    if (option.Length > 1)
                        index = HairStringToIndex(option[1]);

                    if (index == -1 || chaControl.objHair[index] == null || chaControl.objHair[index].GetComponent<ChaCustomHairComponent>() == null)
                        PopulateList(chaControl.gameObject);
                    else
                        PopulateList(chaControl.objHair[index], index);
                    break;
                case "Accessory":
                    if (option.Length > 1)
                        index = AccessoryStringToIndex(option[1]);

                    if (index == -1 || chaControl.objAccessory[index] == null || chaControl.GetAccessory(index) == null)
                        PopulateList(chaControl.gameObject);
                    else
                        PopulateList(chaControl.GetAccessory(index).gameObject, index);
                    break;
            }
        }

        private string ClothesIndexToString(int index)
        {
            switch (index)
            {
                case 0:
                    return "Top";
                case 1:
                    return "Bottom";
                case 2:
                    return "Bra";
                case 3:
                    return "Underwear";
                case 4:
                    return "Gloves";
                case 5:
                    return "Pantyhose";
                case 6:
                    return "Legwear";
                case 7:
                    return "Indoor Shoes";
                case 8:
                    return "Outdoor Shoes";
                default:
                    return "";
            }
        }
        private int ClothesStringToIndex(string s)
        {
            switch (s)
            {
                case "Top":
                    return 0;
                case "Bottom":
                    return 1;
                case "Bra":
                    return 2;
                case "Underwear":
                    return 3;
                case "Gloves":
                    return 4;
                case "Pantyhose":
                    return 5;
                case "Legwear":
                    return 6;
                case "Indoor Shoes":
                case "Indoor":
                    return 7;
                case "Outdoor Shoes":
                case "Outdoor":
                    return 8;
                default:
                    return -1;
            }
        }

        private string HairIndexToString(int index)
        {
            switch (index)
            {
                case 0:
                    return "Back";
                case 1:
                    return "Front";
                case 2:
                    return "Side";
                case 3:
                    return "Extension";
                default:
                    return "";
            }
        }
        private int HairStringToIndex(string s)
        {
            switch (s)
            {
                case "Back":
                    return 0;
                case "Front":
                    return 1;
                case "Side":
                    return 2;
                case "Extension":
                    return 3;
                default:
                    return -1;
            }
        }
        private string AccessoryIndexToString(int index) => $"{index + 1:00}";
        private int AccessoryStringToIndex(string s)
        {
            if (int.TryParse(s, out int index))
                return index - 1;
            return -1;
        }

        private static void ExportTexture(Material mat, string property)
        {
            var tex = mat.GetTexture($"_{property}");
            if (tex == null) return;
            string filename = Path.Combine(ExportPath, $"_Export_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}_{mat.NameFormatted()}_{property}.png");
            SaveTex(tex, filename);
            MaterialEditorPlugin.Logger.LogInfo($"Exported {filename}");
            CC.OpenFileInExplorer(filename);
        }

        internal byte[] LoadIcon()
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{nameof(KK_Plugins)}.Resources.MaterialEditorIcon.png"))
            {
                byte[] bytesInStream = new byte[stream.Length];
                stream.Read(bytesInStream, 0, bytesInStream.Length);
                return bytesInStream;
            }
        }

        internal abstract string GetRendererPropertyValueOriginal(int slot, Renderer renderer, RendererProperties property, GameObject gameObject);
        internal abstract void SetRendererProperty(int slot, Renderer renderer, RendererProperties property, string value, GameObject gameObject);
        internal abstract void RemoveRendererProperty(int slot, Renderer renderer, RendererProperties property, GameObject gameObject);
        internal abstract void MaterialCopyEdits(int slot, Material material, GameObject gameObject);
        internal abstract void MaterialPasteEdits(int slot, Material material, GameObject gameObject);

        internal abstract string GetMaterialShaderNameOriginal(int slot, Material material, GameObject gameObject);
        internal abstract void SetMaterialShaderName(int slot, Material material, string value, GameObject gameObject);
        internal abstract void RemoveMaterialShaderName(int slot, Material material, GameObject gameObject);

        internal abstract int? GetMaterialShaderRenderQueueOriginal(int slot, Material material, GameObject gameObject);
        internal abstract void SetMaterialShaderRenderQueue(int slot, Material material, int value, GameObject gameObject);
        internal abstract void RemoveMaterialShaderRenderQueue(int slot, Material material, GameObject gameObject);

        internal abstract bool GetMaterialTextureValueOriginal(int slot, Material material, string propertyName, GameObject gameObject);
        internal abstract void SetMaterialTexture(int slot, Material material, string propertyName, string filePath, GameObject gameObject);
        internal abstract void RemoveMaterialTexture(int slot, Material material, string propertyName, GameObject gameObject);

        internal abstract Vector2? GetMaterialTextureOffsetOriginal(int slot, Material material, string propertyName, GameObject gameObject);
        internal abstract void SetMaterialTextureOffset(int slot, Material material, string propertyName, Vector2 value, GameObject gameObject);
        internal abstract void RemoveMaterialTextureOffset(int slot, Material material, string propertyName, GameObject gameObject);

        internal abstract Vector2? GetMaterialTextureScaleOriginal(int slot, Material material, string propertyName, GameObject gameObject);
        internal abstract void SetMaterialTextureScale(int slot, Material material, string propertyName, Vector2 value, GameObject gameObject);
        internal abstract void RemoveMaterialTextureScale(int slot, Material material, string propertyName, GameObject gameObject);

        internal abstract Color? GetMaterialColorPropertyValueOriginal(int slot, Material material, string propertyName, GameObject gameObject);
        internal abstract void SetMaterialColorProperty(int slot, Material material, string propertyName, Color value, GameObject gameObject);
        internal abstract void RemoveMaterialColorProperty(int slot, Material material, string propertyName, GameObject gameObject);

        internal abstract float? GetMaterialFloatPropertyValueOriginal(int slot, Material material, string propertyName, GameObject gameObject);
        internal abstract void SetMaterialFloatProperty(int slot, Material material, string propertyName, float value, GameObject gameObject);
        internal abstract void RemoveMaterialFloatProperty(int slot, Material material, string propertyName, GameObject gameObject);
    }
}
