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

        private readonly List<ListEntry> _cachedEntries = new List<ListEntry>();
        private readonly List<ItemInfo> _items = new List<ItemInfo>();

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

            var listEntry = EntryTemplate.AddComponent<ListEntry>();

            listEntry.RendererPanel = listEntry.GetUIComponent<CanvasGroup>("RendererPanel");
            listEntry.RendererLabel = listEntry.GetUIComponent<Text>("RendererLabel");
            listEntry.RendererText = listEntry.GetUIComponent<Text>("RendererText");
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
            listEntry.MaterialCopyButton = listEntry.GetUIComponent<Button>("MaterialCopy");
            listEntry.MaterialPasteButton = listEntry.GetUIComponent<Button>("MaterialPaste");
            listEntry.MaterialCopyRemove = listEntry.GetUIComponent<Button>("MaterialCopyRemove");
            listEntry.MaterialRename = listEntry.GetUIComponent<Button>("MaterialRename");

            listEntry.ShaderPanel = listEntry.GetUIComponent<CanvasGroup>("ShaderPanel");
            listEntry.ShaderLabel = listEntry.GetUIComponent<Text>("ShaderLabel");
            listEntry.SelectInterpolableShaderButton = listEntry.GetUIComponent<Button>("SelectInterpolableShaderButton");
            listEntry.ShaderDropdown = listEntry.GetUIComponent<Dropdown>("ShaderDropdown");
            listEntry.ShaderResetButton = listEntry.GetUIComponent<Button>("ShaderResetButton");

            listEntry.ShaderRenderQueuePanel = listEntry.GetUIComponent<CanvasGroup>("ShaderRenderQueuePanel");
            listEntry.ShaderRenderQueueLabel = listEntry.GetUIComponent<Text>("ShaderRenderQueueLabel");
            listEntry.ShaderRenderQueueInput = listEntry.GetUIComponent<InputField>("ShaderRenderQueueInput");
            listEntry.ShaderRenderQueueResetButton = listEntry.GetUIComponent<Button>("ShaderRenderQueueResetButton");

            listEntry.TexturePanel = listEntry.GetUIComponent<CanvasGroup>("TexturePanel");
            listEntry.TextureLabel = listEntry.GetUIComponent<Text>("TextureLabel");
            listEntry.SelectInterpolableTextureButton = listEntry.GetUIComponent<Button>("SelectInterpolableTextureButton");
            listEntry.ExportTextureButton = listEntry.GetUIComponent<Button>("TextureExportButton");
            listEntry.ImportTextureButton = listEntry.GetUIComponent<Button>("TextureImportButton");
            listEntry.TextureResetButton = listEntry.GetUIComponent<Button>("TextureResetButton");

            listEntry.OffsetScalePanel = listEntry.GetUIComponent<CanvasGroup>("OffsetScalePanel");
            listEntry.OffsetScaleLabel = listEntry.GetUIComponent<Text>("OffsetScaleLabel");
            listEntry.OffsetXText = listEntry.GetUIComponent<Text>("OffsetXText");
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
            listEntry.SelectInterpolableFloatButton = listEntry.GetUIComponent<Button>("SelectInterpolableFloatButton");
            listEntry.FloatSlider = listEntry.GetUIComponent<Slider>("FloatSlider");
            listEntry.FloatInputField = listEntry.GetUIComponent<InputField>("FloatInputField");
            listEntry.FloatResetButton = listEntry.GetUIComponent<Button>("FloatResetButton");

            listEntry.KeywordPanel = listEntry.GetUIComponent<CanvasGroup>("KeywordPanel");
            listEntry.KeywordLabel = listEntry.GetUIComponent<Text>("KeywordLabel");
            listEntry.KeywordToggle = listEntry.GetUIComponent<Toggle>("KeywordToggle");
            listEntry.KeywordResetButton = listEntry.GetUIComponent<Button>("KeywordResetButton");

            listEntry.SetItem(null, true);
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
                var entry = copy.GetComponent<ListEntry>();
                _cachedEntries.Add(entry);
                entry.SetVisible(false);
            }
        }

        public void Clear()
        {
            SetList(null);
        }

        public void SetList(IEnumerable<ItemInfo> items)
        {
            _items.Clear();
            if (items != null)
                _items.AddRange(items);

            _dirty = true;
        }

        private void Update()
        {
            var scrollPosition = ScrollRect.content.localPosition.y;
            // How many items are not visible in current view
            var offscreenItemCount = Mathf.Max(0, _items.Count - _cachedEntries.Count);
            // How many items are above current view rect and not visible
            var itemsAboveViewRect = Mathf.FloorToInt(Mathf.Clamp(scrollPosition / PanelHeight, 0, offscreenItemCount));

            if (_lastItemsAboveViewRect == itemsAboveViewRect && !_dirty)
                return;

            _lastItemsAboveViewRect = itemsAboveViewRect;
            _dirty = false;

            // Store selected item to preserve selection when moving the list with mouse
            ItemInfo selectedItem = null;
            if (EventSystem.current != null)
            {
                var cachedEntry = _cachedEntries.Find(x => x.gameObject == EventSystem.current.currentSelectedGameObject);
                if (cachedEntry != null)
                    selectedItem = cachedEntry.CurrentItem;
            }

            var count = 0;
            bool eventSystem = EventSystem.current != null;
            foreach (var item in _items.Skip(itemsAboveViewRect))
            {
                if (_cachedEntries.Count <= count) break;

                var cachedEntry = _cachedEntries[count];

                count++;

                cachedEntry.SetItem(item, false);
                cachedEntry.SetVisible(true);

                if (eventSystem && ReferenceEquals(selectedItem, item))
                    EventSystem.current.SetSelectedGameObject(cachedEntry.gameObject);
            }

            // If there are less items than cached list entries, disable unused cache entries
            if (_cachedEntries.Count > _items.Count)
            {
                foreach (var cacheEntry in _cachedEntries.Skip(_items.Count))
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

            var totalHeight = _items.Count * PanelHeight;
            var cacheEntriesHeight = _cachedEntries.Count * PanelHeight;
            var trailingHeight = totalHeight - cacheEntriesHeight - topOffset;
            _verticalLayoutGroup.padding.bottom = Mathf.FloorToInt(Mathf.Max(0, trailingHeight) + _paddingBot);
        }

        public void SelectFirstItem()
        {
            var entry = _cachedEntries.FirstOrDefault();
            if (entry != null) entry.GetComponent<Button>().Select();
        }
    }
}
