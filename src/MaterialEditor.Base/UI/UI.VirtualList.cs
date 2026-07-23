using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static MaterialEditorAPI.MaterialEditorUI;

namespace MaterialEditorAPI
{
    internal class VirtualList : MonoBehaviour
    {
        private static readonly bool instantiateOverloadExists = typeof(UnityEngine.Object).GetMethod("Instantiate", new[] { typeof(GameObject), typeof(Transform) }) != null;

        private readonly List<RowView> _cachedViews = new List<RowView>();
        private readonly List<RowModel> _models = new List<RowModel>();

        public GameObject EntryTemplate;
        public ScrollRect ScrollRect;

        private bool _dirty;
        private int _lastItemsAboveViewRect;

        private int _paddingBot;
        private int _paddingTop;

        private VerticalLayoutGroup _verticalLayoutGroup;

        public void Initialize()
        {
            if (ScrollRect == null) throw new ArgumentNullException(nameof(ScrollRect));

            _verticalLayoutGroup = ScrollRect.content.GetComponent<VerticalLayoutGroup>();
            if (_verticalLayoutGroup == null) throw new ArgumentNullException(nameof(_verticalLayoutGroup));

            _paddingTop = _verticalLayoutGroup.padding.top;
            _paddingBot = _verticalLayoutGroup.padding.bottom;

            SetupEntryTemplate();

            PopulateEntryCache();

            Destroy(EntryTemplate);

            Clear();
        }

        private void SetupEntryTemplate()
        {
            if (EntryTemplate == null) throw new ArgumentNullException(nameof(EntryTemplate));

            EntryTemplate.SetActive(false);

            var rowView = EntryTemplate.AddComponent<RowView>();
            var listEntry = EntryTemplate.AddComponent<RowBinder>();
            rowView.Initialize(listEntry);

            listEntry.RendererPanel = listEntry.GetUIComponent<CanvasGroup>("RendererPanel");
            listEntry.RendererLabel = listEntry.GetUIComponent<Text>("RendererLabel");
            listEntry.RendererText = listEntry.GetUIComponent<Text>("RendererText");
            listEntry.RendererTextClickTrigger = listEntry.GetUIComponent<LabelClickTrigger>("RendererText");
            listEntry.SelectInterpolableRendererButton = listEntry.GetUIComponent<Button>("SelectInterpolableRendererButton");
            listEntry.ExportUVButton = listEntry.GetUIComponent<Button>("ExportUVButton");
            listEntry.ExportObjButton = listEntry.GetUIComponent<Button>("ExportObjButton");

            listEntry.RendererEnabledPanel = listEntry.GetUIComponent<CanvasGroup>("RendererEnabledPanel");
            listEntry.RendererEnabledLabel = listEntry.GetUIComponent<Text>("RendererEnabledLabel");
            listEntry.RendererEnabledToggle = listEntry.GetUIComponent<Toggle>("RendererEnabledToggle");
            listEntry.RendererEnabledResetButton = listEntry.GetUIComponent<Button>("RendererEnabledResetButton");

            listEntry.RendererShadowCastingModePanel = listEntry.GetUIComponent<CanvasGroup>("RendererShadowCastingModePanel");
            listEntry.RendererShadowCastingModeLabel = listEntry.GetUIComponent<Text>("RendererShadowCastingModeLabel");
            listEntry.RendererShadowCastingModeDropdown = listEntry.GetUIComponent<Dropdown>("RendererShadowCastingModeDropdown");
            listEntry.RendererShadowCastingModeResetButton = listEntry.GetUIComponent<Button>("RendererShadowCastingModeResetButton");

            listEntry.RendererReceiveShadowsPanel = listEntry.GetUIComponent<CanvasGroup>("RendererReceiveShadowsPanel");
            listEntry.RendererReceiveShadowsLabel = listEntry.GetUIComponent<Text>("RendererReceiveShadowsLabel");
            listEntry.RendererReceiveShadowsToggle = listEntry.GetUIComponent<Toggle>("RendererReceiveShadowsToggle");
            listEntry.RendererReceiveShadowsResetButton = listEntry.GetUIComponent<Button>("RendererReceiveShadowsResetButton");

            listEntry.RendererUpdateWhenOffscreenPanel = listEntry.GetUIComponent<CanvasGroup>("RendererUpdateWhenOffscreenPanel");
            listEntry.RendererUpdateWhenOffscreenLabel = listEntry.GetUIComponent<Text>("RendererUpdateWhenOffscreenLabel");
            listEntry.RendererUpdateWhenOffscreenToggle = listEntry.GetUIComponent<Toggle>("RendererUpdateWhenOffscreenToggle");
            listEntry.RendererUpdateWhenOffscreenResetButton = listEntry.GetUIComponent<Button>("RendererUpdateWhenOffscreenResetButton");

            listEntry.RendererRecalculateNormalsPanel = listEntry.GetUIComponent<CanvasGroup>("RendererRecalculateNormalsPanel");
            listEntry.RendererRecalculateNormalsLabel = listEntry.GetUIComponent<Text>("RendererRecalculateNormalsLabel");
            listEntry.RendererRecalculateNormalsToggle = listEntry.GetUIComponent<Toggle>("RendererRecalculateNormalsToggle");
            listEntry.RendererRecalculateNormalsResetButton = listEntry.GetUIComponent<Button>("RendererRecalculateNormalsResetButton");

            listEntry.MaterialPanel = listEntry.GetUIComponent<CanvasGroup>("MaterialPanel");
            listEntry.MaterialLabel = listEntry.GetUIComponent<Text>("MaterialLabel");
            listEntry.MaterialText = listEntry.GetUIComponent<Text>("MaterialText");
            listEntry.MaterialTextClickTrigger = listEntry.GetUIComponent<LabelClickTrigger>("MaterialText");
            listEntry.MaterialCopyButton = listEntry.GetUIComponent<Button>("MaterialCopy");
            listEntry.MaterialPasteButton = listEntry.GetUIComponent<Button>("MaterialPaste");
            listEntry.MaterialCopyRemove = listEntry.GetUIComponent<Button>("MaterialCopyRemove");
            listEntry.MaterialRename = listEntry.GetUIComponent<Button>("MaterialRename");

            listEntry.ShaderPanel = listEntry.GetUIComponent<CanvasGroup>("ShaderPanel");
            listEntry.ShaderLabel = listEntry.GetUIComponent<Text>("ShaderLabel");
            listEntry.ShaderLabelClickTrigger = listEntry.GetUIComponent<LabelClickTrigger>("ShaderLabel");
            listEntry.SelectInterpolableShaderButton = listEntry.GetUIComponent<Button>("SelectInterpolableShaderButton");
            listEntry.ShaderDropdown = listEntry.GetUIComponent<Dropdown>("ShaderDropdown");
            listEntry.ShaderResetButton = listEntry.GetUIComponent<Button>("ShaderResetButton");

            listEntry.ShaderRenderQueuePanel = listEntry.GetUIComponent<CanvasGroup>("ShaderRenderQueuePanel");
            listEntry.ShaderRenderQueueLabel = listEntry.GetUIComponent<Text>("ShaderRenderQueueLabel");
            listEntry.ShaderRenderQueueLabelClickTrigger = listEntry.GetUIComponent<LabelClickTrigger>("ShaderRenderQueueLabel");
            listEntry.ShaderRenderQueueInput = listEntry.GetUIComponent<InputField>("ShaderRenderQueueInput");
            listEntry.ShaderRenderQueueResetButton = listEntry.GetUIComponent<Button>("ShaderRenderQueueResetButton");

            listEntry.PropertyCategoryPanel = listEntry.GetUIComponent<CanvasGroup>("PropertyCategoryPanel");
            listEntry.PropertyCategoryCollapseButton = listEntry.GetUIComponent<Button>("PropertyCategoryCollapseButton");
            listEntry.PropertyCategoryLabel = listEntry.GetUIComponent<Text>("PropertyCategoryLabel");

            listEntry.TexturePanel = listEntry.GetUIComponent<CanvasGroup>("TexturePanel");
            listEntry.TextureLabel = listEntry.GetUIComponent<Text>("TextureLabel");
            listEntry.TextureLabelClickTrigger = listEntry.GetUIComponent<LabelClickTrigger>("TextureLabel");
            listEntry.SelectInterpolableTextureButton = listEntry.GetUIComponent<Button>("SelectInterpolableTextureButton");
            listEntry.ExportTextureButton = listEntry.GetUIComponent<Button>("TextureExportButton");
            listEntry.ImportTextureButton = listEntry.GetUIComponent<Button>("TextureImportButton");
            listEntry.TextureResetButton = listEntry.GetUIComponent<Button>("TextureResetButton");

            listEntry.OffsetScalePanel = listEntry.GetUIComponent<CanvasGroup>("OffsetScalePanel");
            listEntry.OffsetScaleLabel = listEntry.GetUIComponent<Text>("OffsetScaleLabel");
            listEntry.OffsetScaleLabelClickTrigger = listEntry.GetUIComponent<LabelClickTrigger>("OffsetScaleLabel");
            listEntry.OffsetXText = listEntry.GetUIComponent<Text>("OffsetXText");
            listEntry.OffsetXTextClickTrigger = listEntry.GetUIComponent<LabelClickTrigger>("OffsetXText");
            listEntry.OffsetXInput = listEntry.GetUIComponent<InputField>("OffsetXInput");
            listEntry.OffsetYText = listEntry.GetUIComponent<Text>("OffsetYText");
            listEntry.OffsetYInput = listEntry.GetUIComponent<InputField>("OffsetYInput");
            listEntry.ScaleXText = listEntry.GetUIComponent<Text>("ScaleXText");
            listEntry.ScaleXInput = listEntry.GetUIComponent<InputField>("ScaleXInput");
            listEntry.ScaleYText = listEntry.GetUIComponent<Text>("ScaleYText");
            listEntry.ScaleYInput = listEntry.GetUIComponent<InputField>("ScaleYInput");
            listEntry.OffsetScaleResetButton = listEntry.GetUIComponent<Button>("OffsetScaleResetButton");

            listEntry.ColorPanel = listEntry.GetUIComponent<CanvasGroup>("ColorPanel");
            listEntry.SelectInterpolableColorButton = listEntry.GetUIComponent<Button>("SelectInterpolableColorButton");
            listEntry.ColorLabel = listEntry.GetUIComponent<Text>("ColorLabel");
            listEntry.ColorLabelClickTrigger = listEntry.GetUIComponent<LabelClickTrigger>("ColorLabel");
            listEntry.ColorRText = listEntry.GetUIComponent<Text>("ColorRText");
            listEntry.ColorGText = listEntry.GetUIComponent<Text>("ColorGText");
            listEntry.ColorBText = listEntry.GetUIComponent<Text>("ColorBText");
            listEntry.ColorAText = listEntry.GetUIComponent<Text>("ColorAText");
            listEntry.ColorRInput = listEntry.GetUIComponent<InputField>("ColorRInput");
            listEntry.ColorGInput = listEntry.GetUIComponent<InputField>("ColorGInput");
            listEntry.ColorBInput = listEntry.GetUIComponent<InputField>("ColorBInput");
            listEntry.ColorAInput = listEntry.GetUIComponent<InputField>("ColorAInput");
            listEntry.ColorResetButton = listEntry.GetUIComponent<Button>("ColorResetButton");
            listEntry.ColorEditButton = listEntry.GetUIComponent<Button>("ColorEditButton");

            listEntry.FloatPanel = listEntry.GetUIComponent<CanvasGroup>("FloatPanel");
            listEntry.FloatLabel = listEntry.GetUIComponent<Text>("FloatLabel");
            listEntry.FloatLabelClickTrigger = listEntry.GetUIComponent<LabelClickTrigger>("FloatLabel");
            listEntry.SelectInterpolableFloatButton = listEntry.GetUIComponent<Button>("SelectInterpolableFloatButton");
            listEntry.FloatSlider = listEntry.GetUIComponent<Slider>("FloatSlider");
            listEntry.FloatInputField = listEntry.GetUIComponent<InputField>("FloatInputField");
            listEntry.FloatResetButton = listEntry.GetUIComponent<Button>("FloatResetButton");

            listEntry.KeywordPanel = listEntry.GetUIComponent<CanvasGroup>("KeywordPanel");
            listEntry.KeywordLabel = listEntry.GetUIComponent<Text>("KeywordLabel");
            listEntry.KeywordLabelClickTrigger = listEntry.GetUIComponent<LabelClickTrigger>("KeywordLabel");
            listEntry.KeywordToggle = listEntry.GetUIComponent<Toggle>("KeywordToggle");
            listEntry.KeywordResetButton = listEntry.GetUIComponent<Button>("KeywordResetButton");

            rowView.Bind(null, true);
        }

        private void PopulateEntryCache()
        {
            var viewportHeight = ScrollRect.GetComponent<RectTransform>().rect.height;
            var visibleEntryCount = Mathf.CeilToInt(viewportHeight / PanelHeight);

            for (var i = 0; i < visibleEntryCount; i++)
            {
                GameObject copy;
                if (instantiateOverloadExists)
                {
                    copy = Instantiate(EntryTemplate, EntryTemplate.transform.parent);
                }
                else
                {
                    copy = Instantiate(EntryTemplate);
                    copy.transform.parent = EntryTemplate.transform.parent;
                }
                var entry = copy.GetComponent<RowView>();
                _cachedViews.Add(entry);
                entry.SetVisible(false);
            }
        }

        public void Clear()
        {
            SetList(null);
        }

        public void SetList(IEnumerable<RowModel> items)
        {
            _models.Clear();
            if (items != null)
                _models.AddRange(items);

            _dirty = true;
        }

        private void Update()
        {
            var scrollPosition = ScrollRect.content.localPosition.y;
            // How many items are not visible in current view
            var offscreenItemCount = Mathf.Max(0, _models.Count - _cachedViews.Count);
            // How many items are above current view rect and not visible
            var itemsAboveViewRect = Mathf.FloorToInt(Mathf.Clamp(scrollPosition / PanelHeight, 0, offscreenItemCount));

            if (_lastItemsAboveViewRect == itemsAboveViewRect && !_dirty)
                return;

            _lastItemsAboveViewRect = itemsAboveViewRect;
            _dirty = false;

            // Store selected item to preserve selection when moving the list with mouse
            RowModel selectedItem = null;
            if (EventSystem.current != null)
            {
                var cachedEntry = _cachedViews.Find(x => x.gameObject == EventSystem.current.currentSelectedGameObject);
                if (cachedEntry != null)
                    selectedItem = cachedEntry.CurrentModel;
            }

            var count = 0;
            bool eventSystem = EventSystem.current != null;
            foreach (var item in _models.Skip(itemsAboveViewRect))
            {
                if (_cachedViews.Count <= count) break;

                var cachedEntry = _cachedViews[count];

                count++;

                cachedEntry.Bind(item, false);
                cachedEntry.SetVisible(true);

                if (eventSystem && ReferenceEquals(selectedItem, item))
                    EventSystem.current.SetSelectedGameObject(cachedEntry.gameObject);
            }

            // If there are less items than cached list entries, disable unused cache entries
            if (_cachedViews.Count > _models.Count)
            {
                foreach (var cacheEntry in _cachedViews.Skip(_models.Count))
                    cacheEntry.SetVisible(false);
            }

            RecalculateOffsets(itemsAboveViewRect);

            // Needed after changing _verticalLayoutGroup.padding since it doesn't make the object dirty
            LayoutRebuilder.MarkLayoutForRebuild(_verticalLayoutGroup.GetComponent<RectTransform>());
        }

        private void RecalculateOffsets(int itemsAboveViewRect)
        {
            var topOffset = Mathf.RoundToInt(itemsAboveViewRect * PanelHeight);
            _verticalLayoutGroup.padding.top = _paddingTop + topOffset;

            var totalHeight = _models.Count * PanelHeight;
            var cacheEntriesHeight = _cachedViews.Count * PanelHeight;
            var trailingHeight = totalHeight - cacheEntriesHeight - topOffset;
            _verticalLayoutGroup.padding.bottom = Mathf.FloorToInt(Mathf.Max(0, trailingHeight) + _paddingBot);
        }

        public void SelectFirstItem()
        {
            var entry = _cachedViews.FirstOrDefault();
            if (entry != null) entry.GetComponent<Button>().Select();
        }
    }
}
