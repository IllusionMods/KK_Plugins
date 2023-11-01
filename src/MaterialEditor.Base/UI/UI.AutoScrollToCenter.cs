using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    /// <summary>
    /// Set the initial scroll position of the dropdown to the position of the selected item.
    /// </summary>
    internal class AutoScrollToSelectionWithDropdown : MonoBehaviour
    {
        public static void Setup(Dropdown dropdown)
        {
            var scrollbar = dropdown.GetComponentInChildren<Scrollbar>(true);

            if (scrollbar == null)
                return;

            var assComp = scrollbar.GetComponent<AutoScrollToSelectionWithDropdown>();
            if (assComp == null)
                assComp = scrollbar.gameObject.AddComponent<AutoScrollToSelectionWithDropdown>();
            assComp._target = dropdown;
        }

        [SerializeField]
        private Dropdown _target;

        private bool _autoScrolled;

        private void OnEnable()
        {
            //No scrolling until LateUpdate when internal setup is complete
            _autoScrolled = false;
        }

        private void LateUpdate()
        {
            if (!_autoScrolled)
            {
                _autoScrolled = true;
                AutoScroll();
            }
        }

        private void AutoScroll()
        {
            if (_target == null)
                return;

            int items = _target.options.Count;

            if (items <= 1)
                return;

            var scrollbar = GetComponent<Scrollbar>();

            if (scrollbar == null)
                return;

            //x = 0, y = 1
            int axis = (scrollbar.direction < Scrollbar.Direction.BottomToTop ? 0 : 1);

            var scrollRect = _target.template.GetComponent<ScrollRect>();

            float viewSize = scrollRect.viewport.rect.size[axis];
            float itemSize = 20f;

            if (_target.itemText != null)
            {
                var itemRect = (RectTransform)_target.itemText.transform.parent;
                itemSize = itemRect.rect.size[axis];
            }
            else if (_target.itemImage != null)
            {
                var itemRect = (RectTransform)_target.itemImage.transform.parent;
                itemSize = itemRect.rect.size[axis];
            }

            float viewAreaRatio = (viewSize / itemSize) / items;

            float scroll = (float)_target.value / items - viewAreaRatio * 0.5f;
            scroll = Mathf.Clamp(scroll, 0f, 1f - viewAreaRatio);
            scroll = Mathf.InverseLerp(0, 1f - viewAreaRatio, scroll);

            scrollbar.value = Mathf.Clamp01(1.0f - scroll);
        }
    }
}
