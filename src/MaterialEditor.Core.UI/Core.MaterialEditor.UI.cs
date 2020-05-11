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
#if AI
using AIChara;
#endif

namespace KK_Plugins.MaterialEditor
{
    public abstract class UI : BaseUnityPlugin
    {
        internal static Canvas MaterialEditorWindow;
        private static Image MaterialEditorMainPanel;
        private static ScrollRect MaterialEditorScrollableUI;

        private static FileSystemWatcher TexChangeWatcher;
        VirtualList virtualList;

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

        internal void Main()
        {
            UIScale.SettingChanged += UISettingChanged;
            UIWidth.SettingChanged += UISettingChanged;
            UIHeight.SettingChanged += UISettingChanged;
        }

        protected void InitUI()
        {
            UIUtility.Init(nameof(KK_Plugins));

            MaterialEditorWindow = UIUtility.CreateNewUISystem("MaterialEditorCanvas");
            MaterialEditorWindow.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f / UIScale.Value, 1080f / UIScale.Value);
            HideUI();
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
            nametext.transform.SetRect(0f, 0f, 1f, 1f, 0f, 0f, 0f);
            nametext.alignment = TextAnchor.MiddleCenter;

            var fileWatcher = UIUtility.CreateToggle("Filewatcher", drag.transform, "");
            fileWatcher.transform.SetRect(0f, 0f, 0f, 1f);
            fileWatcher.isOn = WatchTexChanges.Value;
            fileWatcher.onValueChanged.AddListener((value) =>
            {
                WatchTexChanges.Value = value;
                if (!value)
                    TexChangeWatcher?.Dispose();
            });

            var close = UIUtility.CreateButton("CloseButton", drag.transform, "");
            close.transform.SetRect(1f, 0f, 1f, 1f, -20f);
            close.onClick.AddListener(() => HideUI());

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

        public static void HideUI()
        {
            MaterialEditorWindow?.gameObject?.SetActive(false);
            TexChangeWatcher?.Dispose();
        }

        private void UISettingChanged(object sender, EventArgs e)
        {
            MaterialEditorWindow.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f / UIScale.Value, 1080f / UIScale.Value);
            MaterialEditorMainPanel?.transform?.SetRect(0.05f, 0.05f, UIWidth.Value * UIScale.Value, UIHeight.Value * UIScale.Value);
        }

        internal void PopulateListBody()
        {
            var chaControl = MakerAPI.GetCharacterControl();
            PopulateList(chaControl.gameObject, ObjectType.Character, filterType: FilterType.Body);
        }

        internal void PopulateListFace()
        {
            var chaControl = MakerAPI.GetCharacterControl();
            PopulateList(chaControl.gameObject, ObjectType.Character, filterType: FilterType.Face);
        }

        internal void PopulateListCharacter()
        {
            var chaControl = MakerAPI.GetCharacterControl();
            PopulateList(chaControl.gameObject, ObjectType.Character);
        }

        protected void PopulateList(GameObject gameObject, ObjectType objectType, int coordinateIndex = 0, int slot = 0, FilterType filterType = FilterType.All)
        {
            MaterialEditorWindow.gameObject.SetActive(true);
            MaterialEditorWindow.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f / UIScale.Value, 1080f / UIScale.Value);
            MaterialEditorMainPanel.transform.SetRect(0.05f, 0.05f, UIWidth.Value * UIScale.Value, UIHeight.Value * UIScale.Value);

            if (gameObject == null) return;
            if (objectType == ObjectType.Hair || objectType == ObjectType.Character)
                coordinateIndex = 0;

            List<Renderer> rendList = new List<Renderer>();
            List<string> mats = new List<string>();

            Dictionary<string, Material> matList = new Dictionary<string, Material>();

            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl == null)
            {
                rendList = GetRendererList(gameObject);
                filterType = FilterType.All;
            }
            else
            {
                if (filterType == FilterType.Body)
                {
                    matList[chaControl.customMatBody.NameFormatted()] = chaControl.customMatBody;
                    rendList.Add(chaControl.rendBody);
                }
                else if (filterType == FilterType.Face)
                {
                    matList[chaControl.customMatFace.NameFormatted()] = chaControl.customMatFace;
                    rendList.Add(chaControl.rendFace);
                }
                else
                    rendList = GetRendererList(gameObject);
            }
            List<ItemInfo> items = new List<ItemInfo>();

            foreach (var rend in rendList)
            {
                foreach (var mat in rend.sharedMaterials)
                    matList[mat.NameFormatted()] = mat;

                var rendererItem = new ItemInfo(ItemInfo.RowItemType.Renderer, "Renderer");
                rendererItem.RendererName = rend.NameFormatted();
                items.Add(rendererItem);

                //Renderer Enabled
                bool valueEnabledOriginal = rend.enabled;
                var temp = GetRendererPropertyValueOriginal(objectType, coordinateIndex, slot, rend.NameFormatted(), RendererProperties.Enabled);
                if (!temp.IsNullOrEmpty())
                    valueEnabledOriginal = temp == "1";
                var rendererEnabledItem = new ItemInfo(ItemInfo.RowItemType.RendererEnabled, "Enabled");
                rendererEnabledItem.RendererEnabled = rend.enabled ? 1 : 0;
                rendererEnabledItem.RendererEnabledOriginal = valueEnabledOriginal ? 1 : 0;
                rendererEnabledItem.RendererEnabledOnChange = delegate (int value) { AddRendererProperty(objectType, coordinateIndex, slot, rend.NameFormatted(), RendererProperties.Enabled, value.ToString(), valueEnabledOriginal ? "1" : "0", gameObject); };
                rendererEnabledItem.RendererEnabledOnReset = delegate { RemoveRendererProperty(objectType, coordinateIndex, slot, rend.NameFormatted(), RendererProperties.Enabled, gameObject); };
                items.Add(rendererEnabledItem);

                //Renderer ShadowCastingMode
                var valueShadowCastingModeOriginal = rend.shadowCastingMode;
                temp = GetRendererPropertyValueOriginal(objectType, coordinateIndex, slot, rend.NameFormatted(), RendererProperties.ShadowCastingMode);
                if (!temp.IsNullOrEmpty())
                    valueShadowCastingModeOriginal = (UnityEngine.Rendering.ShadowCastingMode)int.Parse(temp);
                var rendererShadowCastingModeItem = new ItemInfo(ItemInfo.RowItemType.RendererShadowCastingMode, "Shadow Casting Mode");
                rendererShadowCastingModeItem.RendererShadowCastingMode = (int)rend.shadowCastingMode;
                rendererShadowCastingModeItem.RendererShadowCastingModeOriginal = (int)valueShadowCastingModeOriginal;
                rendererShadowCastingModeItem.RendererShadowCastingModeOnChange = delegate (int value) { AddRendererProperty(objectType, coordinateIndex, slot, rend.NameFormatted(), RendererProperties.ShadowCastingMode, value.ToString(), ((int)valueShadowCastingModeOriginal).ToString(), gameObject); };
                rendererShadowCastingModeItem.RendererShadowCastingModeOnReset = delegate { RemoveRendererProperty(objectType, coordinateIndex, slot, rend.NameFormatted(), RendererProperties.ShadowCastingMode, gameObject); };
                items.Add(rendererShadowCastingModeItem);

                //Renderer ReceiveShadows
                bool valueReceiveShadowsOriginal = rend.receiveShadows;
                temp = GetRendererPropertyValueOriginal(objectType, coordinateIndex, slot, rend.NameFormatted(), RendererProperties.ShadowCastingMode);
                if (!temp.IsNullOrEmpty())
                    valueReceiveShadowsOriginal = temp == "1";
                var rendererReceiveShadowsItem = new ItemInfo(ItemInfo.RowItemType.RendererReceiveShadows, "Receive Shadows");
                rendererReceiveShadowsItem.RendererReceiveShadows = rend.receiveShadows ? 1 : 0;
                rendererReceiveShadowsItem.RendererReceiveShadowsOriginal = valueReceiveShadowsOriginal ? 1 : 0;
                rendererReceiveShadowsItem.RendererReceiveShadowsOnChange = delegate (int value) { AddRendererProperty(objectType, coordinateIndex, slot, rend.NameFormatted(), RendererProperties.ReceiveShadows, value.ToString(), valueReceiveShadowsOriginal ? "1" : "0", gameObject); };
                rendererReceiveShadowsItem.RendererReceiveShadowsOnReset = delegate { RemoveRendererProperty(objectType, coordinateIndex, slot, rend.NameFormatted(), RendererProperties.ReceiveShadows, gameObject); };
                items.Add(rendererReceiveShadowsItem);
            }

            foreach (var mat in matList.Values)
            {
                string materialName = mat.NameFormatted();
                string shaderName = mat.shader.NameFormatted();

                var materialItem = new ItemInfo(ItemInfo.RowItemType.Material, "Material");
                materialItem.MaterialName = materialName;
                items.Add(materialItem);

                //Shader
                string shaderNameOriginal = shaderName;
                var temp = GetMaterialShaderNameOriginal(objectType, coordinateIndex, slot, materialName);
                if (!temp.IsNullOrEmpty())
                    shaderNameOriginal = temp;
                var shaderItem = new ItemInfo(ItemInfo.RowItemType.Shader, "Shader");
                shaderItem.ShaderName = shaderName;
                shaderItem.ShaderNameOriginal = shaderNameOriginal;
                shaderItem.ShaderNameOnChange = delegate (string value)
                {
                    AddMaterialShaderName(objectType, coordinateIndex, slot, materialName, value, shaderNameOriginal, gameObject);
                    StartCoroutine(PopulateListCoroutine(gameObject, objectType, coordinateIndex, slot, filterType: filterType));
                };
                shaderItem.ShaderNameOnReset = delegate
                {
                    RemoveMaterialShaderName(objectType, coordinateIndex, slot, materialName, gameObject);
                    StartCoroutine(PopulateListCoroutine(gameObject, objectType, coordinateIndex, slot, filterType: filterType));
                };
                items.Add(shaderItem);

                //Shader RenderQueue
                int renderQueueOriginal = mat.renderQueue;
                int? renderQueueOriginalTemp = GetMaterialShaderRenderQueueOriginal(objectType, coordinateIndex, slot, materialName);
                renderQueueOriginal = renderQueueOriginalTemp == null ? mat.renderQueue : (int)renderQueueOriginalTemp;
                var shaderRenderQueueItem = new ItemInfo(ItemInfo.RowItemType.ShaderRenderQueue, "Render Queue");
                shaderRenderQueueItem.ShaderRenderQueue = mat.renderQueue;
                shaderRenderQueueItem.ShaderRenderQueueOriginal = renderQueueOriginal;
                shaderRenderQueueItem.ShaderRenderQueueOnChange = delegate (int value) { AddMaterialShaderRenderQueue(objectType, coordinateIndex, slot, materialName, mat.renderQueue, renderQueueOriginal, gameObject); };
                shaderRenderQueueItem.ShaderRenderQueueOnReset = delegate { RemoveMaterialShaderRenderQueue(objectType, coordinateIndex, slot, materialName, gameObject); };
                items.Add(shaderRenderQueueItem);

                foreach (var property in XMLShaderProperties[XMLShaderProperties.ContainsKey(shaderName) ? shaderName : "default"].OrderBy(x => x.Value.Type).ThenBy(x => x.Key))
                {
                    string propertyName = property.Key;
                    if (CheckBlacklist(objectType, propertyName)) continue;

                    if (property.Value.Type == ShaderPropertyType.Texture)
                    {
                        if (mat.HasProperty($"_{propertyName}"))
                        {
                            var textureItem = new ItemInfo(ItemInfo.RowItemType.TextureProperty, propertyName);
                            textureItem.TextureChanged = !GetMaterialTextureValueOriginal(objectType, coordinateIndex, slot, materialName, propertyName);
                            textureItem.TextureExists = mat.GetTexture($"_{propertyName}") != null;
                            textureItem.TextureOnExport = delegate { ExportTexture(mat, propertyName); };
                            textureItem.TextureOnImport = delegate
                            {
                                OpenFileDialog.Show(strings => OnFileAccept(strings), "Open image", Application.dataPath, FileFilter, FileExt);

                                void OnFileAccept(string[] strings)
                                {
                                    if (strings == null || strings.Length == 0 || strings[0].IsNullOrEmpty())
                                    {
                                        textureItem.TextureChanged = !GetMaterialTextureValueOriginal(objectType, coordinateIndex, slot, materialName, propertyName);
                                        textureItem.TextureExists = mat.GetTexture($"_{propertyName}") != null;
                                        return;
                                    }
                                    string filePath = strings[0];

                                    AddMaterialTexture(objectType, coordinateIndex, slot, materialName, propertyName, filePath, gameObject);

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
                                                    AddMaterialTexture(objectType, coordinateIndex, slot, materialName, propertyName, filePath, gameObject);
                                            };
                                            TexChangeWatcher.Deleted += (sender, args) => TexChangeWatcher?.Dispose();
                                            TexChangeWatcher.Error += (sender, args) => TexChangeWatcher?.Dispose();
                                            TexChangeWatcher.EnableRaisingEvents = true;
                                        }
                                    }
                                }
                            };
                            textureItem.TextureOnReset = delegate { RemoveMaterialTexture(objectType, coordinateIndex, slot, materialName, propertyName); };
                            items.Add(textureItem);

                            Vector2 textureOffset = mat.GetTextureOffset($"_{propertyName}");
                            Vector2 textureOffsetOriginal = textureOffset;
                            Vector2? textureOffsetOriginalTemp = GetMaterialTextureOffsetOriginal(objectType, coordinateIndex, slot, materialName, propertyName);
                            if (textureOffsetOriginalTemp != null)
                                textureOffsetOriginal = (Vector2)textureOffsetOriginalTemp;

                            Vector2 textureScale = mat.GetTextureScale($"_{propertyName}");
                            Vector2 textureScaleOriginal = textureScale;
                            Vector2? textureScaleOriginalTemp = GetMaterialTextureScaleOriginal(objectType, coordinateIndex, slot, materialName, propertyName);
                            if (textureScaleOriginalTemp != null)
                                textureScaleOriginal = (Vector2)textureScaleOriginalTemp;

                            var textureItemOffsetScale = new ItemInfo(ItemInfo.RowItemType.TextureOffsetScale);
                            textureItemOffsetScale.Offset = textureOffset;
                            textureItemOffsetScale.OffsetOriginal = textureOffsetOriginal;
                            textureItemOffsetScale.OffsetOnChange = delegate (Vector2 value) { AddMaterialTextureOffset(objectType, coordinateIndex, slot, materialName, propertyName, value, textureOffsetOriginal, gameObject); };
                            textureItemOffsetScale.OffsetOnReset = delegate { RemoveMaterialTextureOffset(objectType, coordinateIndex, slot, materialName, propertyName, gameObject); };
                            textureItemOffsetScale.Scale = textureScale;
                            textureItemOffsetScale.ScaleOriginal = textureScaleOriginal;
                            textureItemOffsetScale.ScaleOnChange = delegate (Vector2 value) { AddMaterialTextureScale(objectType, coordinateIndex, slot, materialName, propertyName, value, textureScaleOriginal, gameObject); };
                            textureItemOffsetScale.ScaleOnReset = delegate { RemoveMaterialTextureScale(objectType, coordinateIndex, slot, materialName, propertyName, gameObject); };
                            items.Add(textureItemOffsetScale);
                        }

                    }
                    else if (property.Value.Type == ShaderPropertyType.Color)
                    {
                        if (mat.HasProperty($"_{propertyName}"))
                        {
                            Color valueColor = mat.GetColor($"_{propertyName}");
                            Color valueColorOriginal = valueColor;
                            Color? c = GetMaterialColorPropertyValueOriginal(objectType, coordinateIndex, slot, materialName, propertyName);
                            if (c != null)
                                valueColorOriginal = (Color)c;
                            var contentItem = new ItemInfo(ItemInfo.RowItemType.ColorProperty, propertyName);
                            contentItem.ColorValue = valueColor;
                            contentItem.ColorValueOriginal = valueColorOriginal;
                            contentItem.ColorValueOnChange = delegate (Color value) { AddMaterialColorProperty(objectType, coordinateIndex, slot, materialName, propertyName, value, valueColorOriginal, gameObject); };
                            contentItem.ColorValueOnReset = delegate { RemoveMaterialColorProperty(objectType, coordinateIndex, slot, materialName, propertyName, gameObject); };
                            items.Add(contentItem);
                        }
                    }
                    else if (property.Value.Type == ShaderPropertyType.Float)
                    {
                        if (mat.HasProperty($"_{propertyName}"))
                        {
                            float valueFloat = mat.GetFloat($"_{propertyName}");
                            float valueFloatOriginal = valueFloat;
                            string valueFloatOriginalTemp = GetMaterialFloatPropertyValueOriginal(objectType, coordinateIndex, slot, materialName, propertyName);
                            if (!valueFloatOriginalTemp.IsNullOrEmpty() && float.TryParse(valueFloatOriginalTemp, out float valueFloatOriginalTempF))
                                valueFloatOriginal = valueFloatOriginalTempF;
                            var contentItem = new ItemInfo(ItemInfo.RowItemType.FloatProperty, propertyName);
                            contentItem.FloatValue = valueFloat;
                            contentItem.FloatValueOriginal = valueFloatOriginal;
                            if (property.Value.MinValue != null)
                                contentItem.FloatValueSliderMin = (float)property.Value.MinValue;
                            if (property.Value.MaxValue != null)
                                contentItem.FloatValueSliderMax = (float)property.Value.MaxValue;
                            contentItem.FloatValueOnChange = delegate (float value) { AddMaterialFloatProperty(objectType, coordinateIndex, slot, materialName, propertyName, value, valueFloatOriginal, gameObject); };
                            contentItem.FloatValueOnReset = delegate { RemoveMaterialFloatProperty(objectType, coordinateIndex, slot, materialName, propertyName, gameObject); };
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
        protected IEnumerator PopulateListCoroutine(GameObject gameObject, ObjectType objectType, int coordinateIndex = 0, int slot = 0, FilterType filterType = FilterType.All)
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
            PopulateList(gameObject, objectType, coordinateIndex, slot, filterType);
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

        protected enum FilterType { All, Body, Face }

        internal abstract string GetRendererPropertyValueOriginal(ObjectType objectType, int coordinateIndex, int slot, string rendererName, RendererProperties property);
        internal abstract void AddRendererProperty(ObjectType objectType, int coordinateIndex, int slot, string rendererName, RendererProperties property, string value, string valueOriginal, GameObject gameObject);
        internal abstract void RemoveRendererProperty(ObjectType objectType, int coordinateIndex, int slot, string rendererName, RendererProperties property, GameObject gameObject);

        internal abstract string GetMaterialShaderNameOriginal(ObjectType objectType, int coordinateIndex, int slot, string materialName);
        internal abstract void AddMaterialShaderName(ObjectType objectType, int coordinateIndex, int slot, string materialName, string value, string valueOriginal, GameObject gameObject);
        internal abstract void RemoveMaterialShaderName(ObjectType objectType, int coordinateIndex, int slot, string materialName, GameObject gameObject);

        internal abstract int? GetMaterialShaderRenderQueueOriginal(ObjectType objectType, int coordinateIndex, int slot, string materialName);
        internal abstract void AddMaterialShaderRenderQueue(ObjectType objectType, int coordinateIndex, int slot, string materialName, int value, int valueOriginal, GameObject gameObject);
        internal abstract void RemoveMaterialShaderRenderQueue(ObjectType objectType, int coordinateIndex, int slot, string materialName, GameObject gameObject);

        internal abstract bool GetMaterialTextureValueOriginal(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName);
        internal abstract void AddMaterialTexture(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, string filePath, GameObject gameObject);
        internal abstract void RemoveMaterialTexture(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName);

        internal abstract Vector2? GetMaterialTextureOffsetOriginal(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName);
        internal abstract void AddMaterialTextureOffset(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, Vector2 value, Vector2 valueOriginal, GameObject gameObject);
        internal abstract void RemoveMaterialTextureOffset(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject);

        internal abstract Vector2? GetMaterialTextureScaleOriginal(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName);
        internal abstract void AddMaterialTextureScale(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, Vector2 value, Vector2 valueOriginal, GameObject gameObject);
        internal abstract void RemoveMaterialTextureScale(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject);

        internal abstract Color? GetMaterialColorPropertyValueOriginal(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName);
        internal abstract void AddMaterialColorProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, Color value, Color valueOriginal, GameObject gameObject);
        internal abstract void RemoveMaterialColorProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject);

        internal abstract string GetMaterialFloatPropertyValueOriginal(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName);
        internal abstract void AddMaterialFloatProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, float value, float valueOriginal, GameObject gameObject);
        internal abstract void RemoveMaterialFloatProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject);
    }
}
