using System;
using System.Collections.Generic;
using UILib;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal class SelectListEntryTemplate
    {
        public string Name { get; }
        public Transform Parent { get; }
        public ToggleGroup ToggleGroup { get; }
        public ScrollRect ScrollRect { get; }
        public Image Panel { get; }

        private InputField filterInputField;
        private Dictionary<string, Image> listItems;

        internal SelectListEntryTemplate(Transform parent, string name, string title)
        {
            listItems = new Dictionary<string, Image>();
            this.Parent = parent;
            this.Name = name;

            Panel = UIUtility.CreatePanel($"{name}Panel", parent);
            Panel.color = new Color(0.42f, 0.42f, 0.42f);

            var nametext = UIUtility.CreateText($"{name}Title", Panel.transform, title);
            nametext.transform.SetRect(0f, 1f, 0.4f, 1f, 5f, -MaterialEditorUI.HeaderSize, -2f, -2f);
            nametext.alignment = TextAnchor.UpperLeft;

            filterInputField = UIUtility.CreateInputField($"{name}Filter", Panel.transform, "Filter");
            filterInputField.text = "";
            filterInputField.transform.SetRect(0.4f, 1f, 1f, 1f, 2f, -MaterialEditorUI.HeaderSize, -2f, -2f);
            filterInputField.onValueChanged.AddListener(FilterList);

            ScrollRect = UIUtility.CreateScrollView(name, Panel.transform);
            ScrollRect.transform.SetRect(0f, 0f, 1f, 1f, 2f, 2f, -2f, -MaterialEditorUI.HeaderSize);
            ScrollRect.gameObject.AddComponent<Mask>();
            ScrollRect.content.gameObject.AddComponent<VerticalLayoutGroup>();
            ScrollRect.content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ScrollRect.verticalScrollbar.GetComponent<RectTransform>().offsetMin = new Vector2(MaterialEditorUI.ScrollOffsetX, 0f);
            ScrollRect.viewport.offsetMax = new Vector2(MaterialEditorUI.ScrollOffsetX, 0f);
            ScrollRect.movementType = ScrollRect.MovementType.Clamped;
            ScrollRect.verticalScrollbar.GetComponent<Image>().color = new Color(1, 1, 1, 0.6f);

            ToggleGroup = ScrollRect.content.gameObject.AddComponent<ToggleGroup>();
            ToggleGroup.allowSwitchOff = true;
        }

        internal void AddEntry(string name, Action<bool> onValueChanged)
        {
            if (listItems.ContainsKey(name))
                return;

            var contentList = UIUtility.CreatePanel($"{this.Name}Entry", ScrollRect.content.transform);
            contentList.gameObject.AddComponent<LayoutElement>().preferredHeight = MaterialEditorUI.PanelHeight;
            contentList.gameObject.AddComponent<Mask>();
            contentList.color = MaterialEditorUI.RowColor;

            var itemPanel = UIUtility.CreatePanel($"{this.Name}EntryPanel", contentList.transform);
            itemPanel.gameObject.AddComponent<CanvasGroup>();
            itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>().padding = MaterialEditorUI.Padding;
            itemPanel.color = MaterialEditorUI.ItemColor;

            Toggle toggle = UIUtility.CreateToggle($"{this.Name}Toggle", itemPanel.transform, name);
            var toggleLE = toggle.gameObject.AddComponent<LayoutElement>();
            toggle.gameObject.GetComponentInChildren<CanvasRenderer>(true).transform.SetRect(0f, 1f, 0f, 1f, 1f, -18f, 18f, -1f);
            toggle.isOn = false;
            toggle.group = ToggleGroup;
            toggle.onValueChanged.AddListener(value => onValueChanged(value));

            itemPanel.gameObject.AddComponent<Button>().onClick.AddListener(() => toggle.isOn = !toggle.isOn);

            listItems[name] = contentList;
        }

        internal void ClearList()
        {
            filterInputField.Set("");
            listItems.Clear();
            foreach (Transform child in ScrollRect.content.transform)
                UnityEngine.Object.Destroy(child.gameObject);
        }

        internal void ToggleVisibility(bool visible)
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
