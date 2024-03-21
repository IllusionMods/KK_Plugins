using System;
using System.Collections.Generic;
using UILib;
using UnityEngine;
using UnityEngine.UI;
using static MaterialEditorAPI.MaterialEditorPluginBase;

namespace MaterialEditorAPI
{
    internal class SelectListPanel
    {
        public Image Panel { get; }
        private string name;
        private Transform parent;
        private ScrollRect scrollRect;

        private InputField filterInputField;
        private Dictionary<string, Image> listItems;

        public SelectListPanel(Transform parent, string name, string title)
        {
            listItems = new Dictionary<string, Image>();
            this.parent = parent;
            this.name = name;

            Panel = UIUtility.CreatePanel($"{name}Panel", parent);
            Panel.color = new Color(0.42f, 0.42f, 0.42f);

            var nametext = UIUtility.CreateText($"{name}Title", Panel.transform, title);
            nametext.transform.SetRect(0f, 1f, 0.4f, 1f, 5f, -MaterialEditorUI.HeaderSize, -2f, -2f);
            nametext.alignment = TextAnchor.UpperLeft;

            filterInputField = UIUtility.CreateInputField($"{name}Filter", Panel.transform, "Filter");
            filterInputField.text = "";
            filterInputField.transform.SetRect(0.4f, 1f, 1f, 1f, 2f, -MaterialEditorUI.HeaderSize, -2f, -2f);
            filterInputField.onValueChanged.AddListener(FilterList);

            scrollRect = UIUtility.CreateScrollView(name, Panel.transform);
            scrollRect.transform.SetRect(0f, 0f, 1f, 1f, 2f, 2f, -2f, -MaterialEditorUI.HeaderSize);
            scrollRect.gameObject.AddComponent<Mask>();
            scrollRect.content.gameObject.AddComponent<VerticalLayoutGroup>();
            scrollRect.content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.verticalScrollbar.GetComponent<RectTransform>().offsetMin = new Vector2(MaterialEditorUI.ScrollOffsetX, 0f);
            scrollRect.viewport.offsetMax = new Vector2(MaterialEditorUI.ScrollOffsetX, 0f);
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.verticalScrollbar.GetComponent<Image>().color = new Color(1, 1, 1, 0.6f);
        }

        public void AddEntry(string name, Action<bool> onValueChanged)
        {
            if (listItems.ContainsKey(name))
                return;

            var contentList = UIUtility.CreatePanel($"{this.name}Entry", scrollRect.content.transform);
            contentList.gameObject.AddComponent<LayoutElement>().preferredHeight = MaterialEditorUI.PanelHeight;
            contentList.gameObject.AddComponent<Mask>();
            contentList.color = MaterialEditorUI.RowColor;

            var itemPanel = UIUtility.CreatePanel($"{this.name}EntryPanel", contentList.transform);
            itemPanel.gameObject.AddComponent<CanvasGroup>();
            itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>().padding = MaterialEditorUI.Padding;
            itemPanel.color = MaterialEditorUI.ItemColor;

            Toggle toggle = UIUtility.CreateToggle($"{this.name}Toggle", itemPanel.transform, name);
            var toggleLE = toggle.gameObject.AddComponent<LayoutElement>();
            toggle.gameObject.GetComponentInChildren<CanvasRenderer>(true).transform.SetRect(0f, 1f, 0f, 1f, 1f, -18f, 18f, -1f);
            toggle.isOn = false;
            toggle.onValueChanged.AddListener(value => onValueChanged(value));

            itemPanel.gameObject.AddComponent<Button>().onClick.AddListener(() => toggle.isOn = !toggle.isOn);

            listItems[name] = contentList;
            FilterList(filterInputField.text);
        }

        public void ClearList()
        {
            if (!PersistFilter.Value)
                filterInputField.Set("");
            listItems.Clear();
            foreach (Transform child in scrollRect.content.transform)
                UnityEngine.Object.Destroy(child.gameObject);
        }

        public void ToggleVisibility(bool visible)
        {
            Panel.gameObject.SetActive(visible);
        }

        private void FilterList(string filter)
        {
            foreach (var name in listItems.Keys)
                listItems[name].gameObject.SetActive(MaterialEditorUI.WildCardSearch(name, filter));
        }
    }
}
