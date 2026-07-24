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
                entry.Initialize(copy.GetComponent<RowBinder>());
                _cachedViews.Add(entry);
                entry.SetVisible(false);
            }

            if (_cachedViews.Count > 0)
                RowLayoutRuntimeAssertions.Validate(_cachedViews[0]);
            if (_cachedViews.Count > 1)
                RowLayoutRuntimeAssertions.ValidateClones(_cachedViews[0], _cachedViews[1]);
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
