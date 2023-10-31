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

namespace MaterialEditor.API.UI
{
    /// <summary>
    /// Set the initial scroll position of the dropdown to the position of the selected item.
    /// </summary>
    class AutoScrollToSelectionWithDropdown : MonoBehaviour
    {
        public static void Setup( Dropdown dropdown )
        {
            var scrollbar = dropdown.GetComponentInChildren<Scrollbar>(true);

            if (scrollbar == null)
                return;

            scrollbar.GetOrAddComponent<AutoScrollToSelectionWithDropdown>().Target = dropdown;
        }

        [SerializeField]
        private Dropdown Target = null;

        private bool AutoScrolled = false;

        void OnEnable()
        {
            //No scrolling until LateUpdate when internal setup is complete
            AutoScrolled = false;
        }

        void LateUpdate()
        {
            if( !AutoScrolled )
            {
                AutoScrolled = true;
                AutoScroll();
            }            
        }

        void AutoScroll()
        {
            if (Target == null)
                return;

            int items = Target.options.Count;

            if (items <= 1)
                return;

            var scrollbar = GetComponent<Scrollbar>();

            if (scrollbar == null)
                return;

            //x = 0, y = 1
            int axis = (scrollbar.direction < Scrollbar.Direction.BottomToTop ? 0 : 1);
            
            var scrollRect = Target.template.GetComponent<ScrollRect>();

            float viewSize = scrollRect.viewport.rect.size[axis];
            float itemSize = 20f;

            if ( Target.itemText != null )
            {
                var itemRect = (RectTransform)Target.itemText.transform.parent;
                itemSize = itemRect.rect.size[axis];
            }
            else if( Target.itemImage != null )
            {
                var itemRect = (RectTransform)Target.itemImage.transform.parent;
                itemSize = itemRect.rect.size[axis];
            }

            float viewAreaRatio = (viewSize / itemSize) / items;

            float scroll = (float)Target.value / items - viewAreaRatio * 0.5f;
            scroll = Mathf.Clamp(scroll, 0f, 1f - viewAreaRatio);
            scroll = Mathf.InverseLerp(0, 1f - viewAreaRatio, scroll);

            scrollbar.value = Mathf.Clamp01(1.0f - scroll);
        }
    }
}
