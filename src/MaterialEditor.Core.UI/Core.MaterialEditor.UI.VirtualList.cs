using IllusionUtility.GetUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
        private float _singleItemHeight;

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
            listEntry.LabelText = listEntry.transform.FindLoop("Label")?.GetComponent<Text>() ?? throw new ArgumentException("Couldn't find TextGroup");

            listEntry.RendererText = listEntry.transform.FindLoop("RendererText")?.GetComponent<Text>() ?? throw new ArgumentException("Couldn't find RendererText");
            listEntry.ExportUVButton = listEntry.transform.FindLoop("ExportUVButton")?.GetComponent<Button>() ?? throw new ArgumentException("Couldn't find ExportUVButton");
            listEntry.ExportObjButton = listEntry.transform.FindLoop("ExportObjButton")?.GetComponent<Button>() ?? throw new ArgumentException("Couldn't find ExportObjButton");

            listEntry.RendererEnabledDropdown = listEntry.transform.FindLoop("RendererEnabledDropdown")?.GetComponent<Dropdown>() ?? throw new ArgumentException("Couldn't find RendererEnabledDropdown");
            listEntry.RendererShadowCastingModeDropdown = listEntry.transform.FindLoop("RendererShadowCastingModeDropdown")?.GetComponent<Dropdown>() ?? throw new ArgumentException("Couldn't find RendererShadowCastingModeDropdown");
            listEntry.RendererReceiveShadowsDropdown = listEntry.transform.FindLoop("RendererReceiveShadowsDropdown")?.GetComponent<Dropdown>() ?? throw new ArgumentException("Couldn't find RendererReceiveShadowsDropdown");

            listEntry.MaterialText = listEntry.transform.FindLoop("MaterialText")?.GetComponent<Text>() ?? throw new ArgumentException("Couldn't find MaterialText");
            listEntry.ShaderDropdown = listEntry.transform.FindLoop("ShaderDropdown")?.GetComponent<Dropdown>() ?? throw new ArgumentException("Couldn't find ShaderDropdown");
            listEntry.ShaderRenderQueueInput = listEntry.transform.FindLoop("ShaderRenderQueueInput")?.GetComponent<InputField>() ?? throw new ArgumentException("Couldn't find ShaderRenderQueueInput");

            listEntry.ExportTextureButton = listEntry.transform.FindLoop("ExportTextureButton")?.GetComponent<Button>() ?? throw new ArgumentException("Couldn't find ExportTextureButton");
            listEntry.ImportTextureButton = listEntry.transform.FindLoop("ImportTextureButton")?.GetComponent<Button>() ?? throw new ArgumentException("Couldn't find ImportTextureButton");

            listEntry.OffsetXText = listEntry.transform.FindLoop("OffsetXText")?.GetComponent<Text>() ?? throw new ArgumentException("Couldn't find OffsetXText");
            listEntry.OffsetXInput = listEntry.transform.FindLoop("OffsetXInput")?.GetComponent<InputField>() ?? throw new ArgumentException("Couldn't find OffsetXInput");
            listEntry.OffsetYText = listEntry.transform.FindLoop("OffsetYText")?.GetComponent<Text>() ?? throw new ArgumentException("Couldn't find OffsetYText");
            listEntry.OffsetYInput = listEntry.transform.FindLoop("OffsetYInput")?.GetComponent<InputField>() ?? throw new ArgumentException("Couldn't find OffsetYInput");
            listEntry.ScaleXText = listEntry.transform.FindLoop("ScaleXText")?.GetComponent<Text>() ?? throw new ArgumentException("Couldn't find ScaleXText");
            listEntry.ScaleXInput = listEntry.transform.FindLoop("ScaleXInput")?.GetComponent<InputField>() ?? throw new ArgumentException("Couldn't find ScaleXInput");
            listEntry.ScaleYText = listEntry.transform.FindLoop("ScaleYText")?.GetComponent<Text>() ?? throw new ArgumentException("Couldn't find ScaleYText");
            listEntry.ScaleYInput = listEntry.transform.FindLoop("ScaleYInput")?.GetComponent<InputField>() ?? throw new ArgumentException("Couldn't find ScaleYInput");

            listEntry.ColorRText = listEntry.transform.FindLoop("ColorRText")?.GetComponent<Text>() ?? throw new ArgumentException("Couldn't find ColorRText");
            listEntry.ColorGText = listEntry.transform.FindLoop("ColorGText")?.GetComponent<Text>() ?? throw new ArgumentException("Couldn't find ColorGText");
            listEntry.ColorBText = listEntry.transform.FindLoop("ColorBText")?.GetComponent<Text>() ?? throw new ArgumentException("Couldn't find ColorBText");
            listEntry.ColorAText = listEntry.transform.FindLoop("ColorAText")?.GetComponent<Text>() ?? throw new ArgumentException("Couldn't find ColorAText");
            listEntry.ColorRInput = listEntry.transform.FindLoop("ColorRInput")?.GetComponent<InputField>() ?? throw new ArgumentException("Couldn't find ColorRInput");
            listEntry.ColorGInput = listEntry.transform.FindLoop("ColorGInput")?.GetComponent<InputField>() ?? throw new ArgumentException("Couldn't find ColorGInput");
            listEntry.ColorBInput = listEntry.transform.FindLoop("ColorBInput")?.GetComponent<InputField>() ?? throw new ArgumentException("Couldn't find ColorBInput");
            listEntry.ColorAInput = listEntry.transform.FindLoop("ColorAInput")?.GetComponent<InputField>() ?? throw new ArgumentException("Couldn't find ColorAInput");

            listEntry.FloatSlider = listEntry.transform.FindLoop("FloatSlider")?.GetComponent<Slider>() ?? throw new ArgumentException("Couldn't find FloatSlider");
            listEntry.FloatInputField = listEntry.transform.FindLoop("FloatInputField")?.GetComponent<InputField>() ?? throw new ArgumentException("Couldn't find FloatInputField");

            listEntry.ResetButton = listEntry.transform.FindLoop("ResetButton")?.GetComponent<Button>() ?? throw new ArgumentException("Couldn't find ResetButton");
            listEntry.SetItem(null, true);

            //var templateRectTransform = EntryTemplate.GetComponent<RectTransform>();
            //_singleItemHeight = templateRectTransform.rect.height + _verticalLayoutGroup.spacing;
            _singleItemHeight = 20f;
        }

        private void PopulateEntryCache()
        {
            var viewportHeight = ScrollRect.GetComponent<RectTransform>().rect.height;
            var visibleEntryCount = Mathf.CeilToInt(viewportHeight / _singleItemHeight) + 1;

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
            var itemsAboveViewRect = Mathf.FloorToInt(Mathf.Clamp(scrollPosition / _singleItemHeight, 0, offscreenItemCount));

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
            var topOffset = Mathf.RoundToInt(itemsAboveViewRect * _singleItemHeight);
            _verticalLayoutGroup.padding.top = _paddingTop + topOffset;

            var totalHeight = _items.Count * _singleItemHeight;
            var cacheEntriesHeight = _cachedEntries.Count * _singleItemHeight;
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
