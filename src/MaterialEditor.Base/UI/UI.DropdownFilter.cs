using System.Collections.Generic;
using System.Text.RegularExpressions;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal class DropdownFilter : MonoBehaviour
    {
        [SerializeField] private Dropdown _parent;
        [SerializeField] private RectTransform _filterRect;
        [SerializeField] private InputField _filterField;
        [SerializeField] private RectTransform _content;
        [SerializeField] private RectTransform _clearButtonRect;
        [SerializeField] private string _persistentKeyword;

        static private Dictionary<string, string> _persistentFilterMap = new Dictionary<string, string>();
        static readonly float _clearButtonAnchorX = 0.16f;

        private float _itemStride = 20f;

        /// <summary>
        /// Add filter UI to dropdown
        /// </summary>
        /// <param name="target">Target dropdown</param>
        /// <param name="persistentKeyword">Name for persistent filter. A persisted filter will continue to hold the filter after it is closed.</param>
        public static void AddFilterUI( Dropdown target, string persistentKeyword = null )
        {
            var menuTemplate = target.transform.Find("Template");

            if (menuTemplate == null)
                return;

            var filter = menuTemplate.GetComponent<DropdownFilter>();

            if (filter != null)
                return;

            filter = menuTemplate.gameObject.AddComponent<DropdownFilter>();
            filter._parent = target;
            filter._persistentKeyword = persistentKeyword;

            var filterField = UIUtility.CreateInputField("Filter", menuTemplate.transform, "Filter");
            filter._filterField = filterField;

            float uiMargin = 2f;
            float filterUIHeight = GetItemSize(target) + uiMargin;

            var templateRect = (RectTransform)menuTemplate.transform;
            templateRect.anchoredPosition = new Vector2(0, -filterUIHeight + uiMargin + uiMargin);

            var filterRect = (RectTransform)filterField.transform;
            filter._filterRect = filterRect;

            filterRect.anchorMin = new Vector2(_clearButtonAnchorX, 0f);
            filterRect.anchorMax = new Vector2(1f, 0f);
            filterRect.offsetMin = new Vector2(0, -filterUIHeight);
            filterRect.offsetMax = new Vector2(0, 0);
            filterRect.sizeDelta = new Vector2(0, filterUIHeight);

            var clearButton = UIUtility.CreateButton("ClearFilter", menuTemplate.transform, "Clear");
            var clearButtonRect = (RectTransform)clearButton.transform;
            filter._clearButtonRect = clearButtonRect;
            clearButtonRect.anchorMin = new Vector2(0f, 0);
            clearButtonRect.anchorMax = new Vector2(_clearButtonAnchorX, 0);
            clearButtonRect.offsetMin = new Vector2(0, -filterUIHeight);
            clearButtonRect.offsetMax = new Vector2(0, 0);
            clearButtonRect.sizeDelta = new Vector2(0, filterUIHeight);

            var content = (RectTransform)target.transform.Find("Template/Viewport/Content");
            filter._content = content;

            //Add layout groups/element so that items are automatically aligned
            var contentLayoutGroup = content.gameObject.AddComponent<VerticalLayoutGroup>();

            contentLayoutGroup.childForceExpandWidth = true;
            contentLayoutGroup.childForceExpandHeight = false;
            contentLayoutGroup.childControlWidth = true;
            contentLayoutGroup.childControlHeight = false;

            content.Find("Item").gameObject.AddComponent<LayoutElement>();
        }

        private void Start()
        {
            if (_filterRect == null)
                return; //Not Instantiate Component

            SetUIPosition();

            _filterField.onValueChanged.AddListener( OnChangeFilter );
            _filterField.onEndEdit.AddListener(OnEndEditFilter);

            if( _content.childCount >= 3 )
            {
                var item1 = (RectTransform)_content.GetChild(1);
                var item2 = (RectTransform)_content.GetChild(2);
                _itemStride = Mathf.Abs(item1.offsetMin.y - item2.offsetMin.y);
            }
            else
            {
                _itemStride = 20f;
            }

            _clearButtonRect.GetComponent<Button>().onClick.AddListener(ClearFilter);
            RestoreFilter();
        }

        private void SaveFilter()
        {
            if (string.IsNullOrEmpty(_persistentKeyword))
                return;

            _persistentFilterMap[_persistentKeyword] = _filterField.text;
        }

        private void RestoreFilter()
        {
            if (string.IsNullOrEmpty(_persistentKeyword) || !_persistentFilterMap.ContainsKey(_persistentKeyword))
                return;

            var filter = _persistentFilterMap[_persistentKeyword];
            _filterField.text = filter;
        }

        private void ClearFilter()
        {
            _filterField.text = "";
            SaveFilter();
        }

        private void OnEndEditFilter( string filter )
        {
            SaveFilter();
        }

        private void OnChangeFilter( string filter )
        {
            if (string.IsNullOrEmpty(filter))
                filter = "*";

            var regex = new Regex(Regex.Escape(filter.ToUpper()).Replace("\\*", ".*").Replace("\\?", "?"), RegexOptions.IgnoreCase);
            int activeItems = 0;

            foreach( Transform item in _content )
            {
                var name = item.name;

                if (name.EndsWith(": Reset"))
                {
                    ++activeItems;
                    continue;
                }                    

                int colon = name.IndexOf(":");
                if (colon < 0)
                    continue;                    

                bool isShown = regex.IsMatch(name.Substring(colon));
                item.gameObject.SetActive(isShown);
                if (isShown)
                    ++activeItems;
            }

            var sizeDelta = _content.sizeDelta;
            sizeDelta.y = _itemStride * activeItems + 8f;
            _content.sizeDelta = sizeDelta;

            var scrollbar = GetComponentInChildren<Scrollbar>();
            if( scrollbar != null )
            {
                scrollbar.value = 1f;
                scrollbar.Rebuild(CanvasUpdate.Prelayout);
            }   
        }

        /// <summary>
        /// Set the position of the filter UI.
        /// The drop-down UI is displayed either up or down depending on position. 
        /// Change the position of the filter UI accordingly.
        /// </summary>
        private void SetUIPosition()
        {
            float height = _filterRect.sizeDelta.y;
            float offset = 2f;
            var dropdownRect = (RectTransform)transform;

            if (dropdownRect.offsetMax.y > 0 )
            {
                _filterRect.anchorMin = new Vector2(_clearButtonAnchorX, 0f);
                _filterRect.anchorMax = new Vector2(1f, 0f);
                _filterRect.offsetMin = new Vector2(0, -height + offset);
                _filterRect.offsetMax = new Vector2(0, offset);

                _clearButtonRect.anchorMin = new Vector2(0f, 0f);
                _clearButtonRect.anchorMax = new Vector2(_clearButtonAnchorX, 0f);
                _clearButtonRect.offsetMin = new Vector2(0, -height + offset);
                _clearButtonRect.offsetMax = new Vector2(0, offset);
            }
            else
            {
                _filterRect.anchorMin = new Vector2(_clearButtonAnchorX, 1f);
                _filterRect.anchorMax = new Vector2(1f, 1f);
                _filterRect.offsetMin = new Vector2(0, -offset);
                _filterRect.offsetMax = new Vector2(0, height - offset);

                _clearButtonRect.anchorMin = new Vector2(0, 1f);
                _clearButtonRect.anchorMax = new Vector2(_clearButtonAnchorX, 1f);
                _clearButtonRect.offsetMin = new Vector2(0, -offset);
                _clearButtonRect.offsetMax = new Vector2(0, height - offset);
            }
        }

        private static float GetItemSize( Dropdown dropdown )
        {
            if( dropdown.itemText != null )
            {
                return dropdown.itemText.rectTransform.rect.height;
            }
            else if(dropdown.itemImage != null)
            {
                return dropdown.itemImage.rectTransform.rect.height;
            }

            return 20f;
        }
    }
}
