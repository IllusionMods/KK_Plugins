using IllusionUtility.GetUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static KK_Plugins.MaterialEditor.UI;

namespace KK_Plugins.MaterialEditor
{
    internal class VirtualList : MonoBehaviour
    {
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

            listEntry.RendererPanel = listEntry.transform.FindLoop("RendererPanel")?.GetComponent<CanvasGroup>() ?? throw new ArgumentException("Couldn't find RendererPanel");
            listEntry.RendererLabel = listEntry.transform.FindLoop("RendererLabel")?.GetComponent<Text>() ?? throw new ArgumentException("Couldn't find RendererLabel");
            listEntry.RendererText = listEntry.transform.FindLoop("RendererText")?.GetComponent<Text>() ?? throw new ArgumentException("Couldn't find RendererText");
            listEntry.ExportUVButton = listEntry.transform.FindLoop("ExportUVButton")?.GetComponent<Button>() ?? throw new ArgumentException("Couldn't find ExportUVButton");
            listEntry.ExportObjButton = listEntry.transform.FindLoop("ExportObjButton")?.GetComponent<Button>() ?? throw new ArgumentException("Couldn't find ExportObjButton");

            listEntry.RendererEnabledPanel = listEntry.transform.FindLoop("RendererEnabledPanel")?.GetComponent<CanvasGroup>() ?? throw new ArgumentException("Couldn't find RendererEnabledPanel");
            listEntry.RendererEnabledLabel = listEntry.transform.FindLoop("RendererEnabledLabel")?.GetComponent<Text>() ?? throw new ArgumentException("Couldn't find RendererEnabledLabel");
            listEntry.RendererEnabledDropdown = listEntry.transform.FindLoop("RendererEnabledDropdown")?.GetComponent<Dropdown>() ?? throw new ArgumentException("Couldn't find RendererEnabledDropdown");
            listEntry.RendererEnabledResetButton = listEntry.transform.FindLoop("RendererEnabledResetButton")?.GetComponent<Button>() ?? throw new ArgumentException("Couldn't find RendererEnabledResetButton");

            listEntry.RendererShadowCastingModePanel = listEntry.transform.FindLoop("RendererShadowCastingModePanel")?.GetComponent<CanvasGroup>() ?? throw new ArgumentException("Couldn't find RendererShadowCastingModePanel");
            listEntry.RendererShadowCastingModeLabel = listEntry.transform.FindLoop("RendererShadowCastingModeLabel")?.GetComponent<Text>() ?? throw new ArgumentException("Couldn't find RendererShadowCastingModeLabel");
            listEntry.RendererShadowCastingModeDropdown = listEntry.transform.FindLoop("RendererShadowCastingModeDropdown")?.GetComponent<Dropdown>() ?? throw new ArgumentException("Couldn't find RendererShadowCastingModeDropdown");
            listEntry.RendererShadowCastingModeResetButton = listEntry.transform.FindLoop("RendererShadowCastingModeResetButton")?.GetComponent<Button>() ?? throw new ArgumentException("Couldn't find RendererShadowCastingModeResetButton");

            listEntry.RendererReceiveShadowsPanel = listEntry.transform.FindLoop("RendererReceiveShadowsPanel")?.GetComponent<CanvasGroup>() ?? throw new ArgumentException("Couldn't find RendererReceiveShadowsPanel");
            listEntry.RendererReceiveShadowsLabel = listEntry.transform.FindLoop("RendererReceiveShadowsLabel")?.GetComponent<Text>() ?? throw new ArgumentException("Couldn't find RendererReceiveShadowsLabel");
            listEntry.RendererReceiveShadowsDropdown = listEntry.transform.FindLoop("RendererReceiveShadowsDropdown")?.GetComponent<Dropdown>() ?? throw new ArgumentException("Couldn't find RendererReceiveShadowsDropdown");
            listEntry.RendererReceiveShadowsResetButton = listEntry.transform.FindLoop("RendererReceiveShadowsResetButton")?.GetComponent<Button>() ?? throw new ArgumentException("Couldn't find RendererReceiveShadowsResetButton");

            listEntry.MaterialPanel = listEntry.transform.FindLoop("MaterialPanel")?.GetComponent<CanvasGroup>() ?? throw new ArgumentException("Couldn't find MaterialPanel");
            listEntry.MaterialLabel = listEntry.transform.FindLoop("MaterialLabel")?.GetComponent<Text>() ?? throw new ArgumentException("Couldn't find MaterialLabel");
            listEntry.MaterialText = listEntry.transform.FindLoop("MaterialText")?.GetComponent<Text>() ?? throw new ArgumentException("Couldn't find MaterialText");

            listEntry.ShaderPanel = listEntry.transform.FindLoop("ShaderPanel")?.GetComponent<CanvasGroup>() ?? throw new ArgumentException("Couldn't find ShaderPanel");
            listEntry.ShaderLabel = listEntry.transform.FindLoop("ShaderLabel")?.GetComponent<Text>() ?? throw new ArgumentException("Couldn't find ShaderLabel");
            listEntry.ShaderDropdown = listEntry.transform.FindLoop("ShaderDropdown")?.GetComponent<Dropdown>() ?? throw new ArgumentException("Couldn't find ShaderDropdown");
            listEntry.ShaderResetButton = listEntry.transform.FindLoop("ShaderResetButton")?.GetComponent<Button>() ?? throw new ArgumentException("Couldn't find ShaderResetButton");

            listEntry.ShaderRenderQueuePanel = listEntry.transform.FindLoop("ShaderRenderQueuePanel")?.GetComponent<CanvasGroup>() ?? throw new ArgumentException("Couldn't find ShaderRenderQueuePanel");
            listEntry.ShaderRenderQueueLabel = listEntry.transform.FindLoop("ShaderRenderQueueLabel")?.GetComponent<Text>() ?? throw new ArgumentException("Couldn't find ShaderRenderQueueLabel");
            listEntry.ShaderRenderQueueInput = listEntry.transform.FindLoop("ShaderRenderQueueInput")?.GetComponent<InputField>() ?? throw new ArgumentException("Couldn't find ShaderRenderQueueInput");
            listEntry.ShaderRenderQueueResetButton = listEntry.transform.FindLoop("ShaderRenderQueueResetButton")?.GetComponent<Button>() ?? throw new ArgumentException("Couldn't find ShaderRenderQueueResetButton");

            listEntry.TexturePanel = listEntry.transform.FindLoop("TexturePanel")?.GetComponent<CanvasGroup>() ?? throw new ArgumentException("Couldn't find TexturePanel");
            listEntry.TextureLabel = listEntry.transform.FindLoop("TextureLabel")?.GetComponent<Text>() ?? throw new ArgumentException("Couldn't find TextureLabel");
            listEntry.ExportTextureButton = listEntry.transform.FindLoop("TextureExportButton")?.GetComponent<Button>() ?? throw new ArgumentException("Couldn't find ExportTextureButton");
            listEntry.ImportTextureButton = listEntry.transform.FindLoop("TextureImportButton")?.GetComponent<Button>() ?? throw new ArgumentException("Couldn't find ImportTextureButton");
            listEntry.TextureResetButton = listEntry.transform.FindLoop("TextureResetButton")?.GetComponent<Button>() ?? throw new ArgumentException("Couldn't find TextureResetButton");

            listEntry.OffsetScalePanel = listEntry.transform.FindLoop("OffsetScalePanel")?.GetComponent<CanvasGroup>() ?? throw new ArgumentException("Couldn't find OffsetScalePanel");
            listEntry.OffsetScaleLabel = listEntry.transform.FindLoop("OffsetScaleLabel")?.GetComponent<Text>() ?? throw new ArgumentException("Couldn't find OffsetScaleLabel");
            listEntry.OffsetXText = listEntry.transform.FindLoop("OffsetXText")?.GetComponent<Text>() ?? throw new ArgumentException("Couldn't find OffsetXText");
            listEntry.OffsetXInput = listEntry.transform.FindLoop("OffsetXInput")?.GetComponent<InputField>() ?? throw new ArgumentException("Couldn't find OffsetXInput");
            listEntry.OffsetYText = listEntry.transform.FindLoop("OffsetYText")?.GetComponent<Text>() ?? throw new ArgumentException("Couldn't find OffsetYText");
            listEntry.OffsetYInput = listEntry.transform.FindLoop("OffsetYInput")?.GetComponent<InputField>() ?? throw new ArgumentException("Couldn't find OffsetYInput");
            listEntry.ScaleXText = listEntry.transform.FindLoop("ScaleXText")?.GetComponent<Text>() ?? throw new ArgumentException("Couldn't find ScaleXText");
            listEntry.ScaleXInput = listEntry.transform.FindLoop("ScaleXInput")?.GetComponent<InputField>() ?? throw new ArgumentException("Couldn't find ScaleXInput");
            listEntry.ScaleYText = listEntry.transform.FindLoop("ScaleYText")?.GetComponent<Text>() ?? throw new ArgumentException("Couldn't find ScaleYText");
            listEntry.ScaleYInput = listEntry.transform.FindLoop("ScaleYInput")?.GetComponent<InputField>() ?? throw new ArgumentException("Couldn't find ScaleYInput");
            listEntry.OffsetScaleResetButton = listEntry.transform.FindLoop("OffsetScaleResetButton")?.GetComponent<Button>() ?? throw new ArgumentException("Couldn't find OffsetScaleResetButton");

            listEntry.ColorPanel = listEntry.transform.FindLoop("ColorPanel")?.GetComponent<CanvasGroup>() ?? throw new ArgumentException("Couldn't find ColorPanel");
            listEntry.ColorLabel = listEntry.transform.FindLoop("ColorLabel")?.GetComponent<Text>() ?? throw new ArgumentException("Couldn't find ColorLabel");
            listEntry.ColorRText = listEntry.transform.FindLoop("ColorRText")?.GetComponent<Text>() ?? throw new ArgumentException("Couldn't find ColorRText");
            listEntry.ColorGText = listEntry.transform.FindLoop("ColorGText")?.GetComponent<Text>() ?? throw new ArgumentException("Couldn't find ColorGText");
            listEntry.ColorBText = listEntry.transform.FindLoop("ColorBText")?.GetComponent<Text>() ?? throw new ArgumentException("Couldn't find ColorBText");
            listEntry.ColorAText = listEntry.transform.FindLoop("ColorAText")?.GetComponent<Text>() ?? throw new ArgumentException("Couldn't find ColorAText");
            listEntry.ColorRInput = listEntry.transform.FindLoop("ColorRInput")?.GetComponent<InputField>() ?? throw new ArgumentException("Couldn't find ColorRInput");
            listEntry.ColorGInput = listEntry.transform.FindLoop("ColorGInput")?.GetComponent<InputField>() ?? throw new ArgumentException("Couldn't find ColorGInput");
            listEntry.ColorBInput = listEntry.transform.FindLoop("ColorBInput")?.GetComponent<InputField>() ?? throw new ArgumentException("Couldn't find ColorBInput");
            listEntry.ColorAInput = listEntry.transform.FindLoop("ColorAInput")?.GetComponent<InputField>() ?? throw new ArgumentException("Couldn't find ColorAInput");
            listEntry.ColorResetButton = listEntry.transform.FindLoop("ColorResetButton")?.GetComponent<Button>() ?? throw new ArgumentException("Couldn't find ColorResetButton");

            listEntry.FloatPanel = listEntry.transform.FindLoop("FloatPanel")?.GetComponent<CanvasGroup>() ?? throw new ArgumentException("Couldn't find FloatPanel");
            listEntry.FloatLabel = listEntry.transform.FindLoop("FloatLabel")?.GetComponent<Text>() ?? throw new ArgumentException("Couldn't find FloatLabel");
            listEntry.FloatSlider = listEntry.transform.FindLoop("FloatSlider")?.GetComponent<Slider>() ?? throw new ArgumentException("Couldn't find FloatSlider");
            listEntry.FloatInputField = listEntry.transform.FindLoop("FloatInputField")?.GetComponent<InputField>() ?? throw new ArgumentException("Couldn't find FloatInputField");
            listEntry.FloatResetButton = listEntry.transform.FindLoop("FloatResetButton")?.GetComponent<Button>() ?? throw new ArgumentException("Couldn't find FloatResetButton");

            listEntry.SetItem(null, true);
        }

        private void PopulateEntryCache()
        {
            var viewportHeight = ScrollRect.GetComponent<RectTransform>().rect.height;
            var visibleEntryCount = Mathf.CeilToInt(viewportHeight / panelHeight);

            for (var i = 0; i < visibleEntryCount; i++)
            {
                var copy = Instantiate(EntryTemplate, EntryTemplate.transform.parent);
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
            var itemsAboveViewRect = Mathf.FloorToInt(Mathf.Clamp(scrollPosition / panelHeight, 0, offscreenItemCount));

            if (_lastItemsAboveViewRect == itemsAboveViewRect && !_dirty)
                return;

            _lastItemsAboveViewRect = itemsAboveViewRect;
            _dirty = false;

            // Store selected item to preserve selection when moving the list with mouse
            var selectedItem = EventSystem.current != null
                ? _cachedEntries.Find(x => x.gameObject == EventSystem.current.currentSelectedGameObject)?.CurrentItem
                : null;

            var count = 0;
            foreach (var item in _items.Skip(itemsAboveViewRect))
            {
                if (_cachedEntries.Count <= count) break;

                var cachedEntry = _cachedEntries[count];

                count++;

                cachedEntry.SetItem(item, false);
                cachedEntry.SetVisible(true);

                if (ReferenceEquals(selectedItem, item))
                    EventSystem.current?.SetSelectedGameObject(cachedEntry.gameObject);
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
            var topOffset = Mathf.RoundToInt(itemsAboveViewRect * panelHeight);
            _verticalLayoutGroup.padding.top = _paddingTop + topOffset;

            var totalHeight = _items.Count * panelHeight;
            var cacheEntriesHeight = _cachedEntries.Count * panelHeight;
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
