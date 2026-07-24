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
        private readonly string name;
        private readonly ScrollRect scrollRect;

        private readonly InputField filterInputField;
        private readonly Dictionary<string, Image> listItems;

        public SelectListPanel(Transform parent, string name, string title)
        {
            listItems = new Dictionary<string, Image>();
            this.name = name;

            Panel = MaterialEditorControlFactory.CreatePanel($"{name}Panel", parent, MaterialEditorPanelRole.SidePanel);

            var nametext = MaterialEditorControlFactory.CreateText(
                $"{name}Title",
                Panel.transform,
                title,
                MaterialEditorTextRole.Label);
            nametext.transform.SetRect(0f, 1f, 0.4f, 1f, 5f, -MaterialEditorUI.HeaderSize, -2f, -2f);

            filterInputField = MaterialEditorControlFactory.CreateInputField($"{name}Filter", Panel.transform, "Filter");
            filterInputField.text = "";
            filterInputField.transform.SetRect(0.4f, 1f, 1f, 1f, 2f, -MaterialEditorUI.HeaderSize, -2f, -2f);
            filterInputField.onValueChanged.AddListener(FilterList);

            scrollRect = MaterialEditorControlFactory.CreateScrollView(name, Panel.transform);
            scrollRect.transform.SetRect(0f, 0f, 1f, 1f, 2f, 2f, -2f, -MaterialEditorUI.HeaderSize);
            scrollRect.gameObject.AddComponent<Mask>();
            scrollRect.content.gameObject.AddComponent<VerticalLayoutGroup>();
            scrollRect.content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.verticalScrollbar.GetComponent<RectTransform>().offsetMin = new Vector2(MaterialEditorUI.ScrollOffsetX, 0f);
            scrollRect.viewport.offsetMax = new Vector2(MaterialEditorUI.ScrollOffsetX, 0f);
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            MaterialEditorStyles.ApplyScrollView(scrollRect);

            MaterialEditorStyles.ApplyTypography(Panel.gameObject);
        }

        public void AddEntry(string name, Action<bool> onValueChanged)
        {
            if (listItems.ContainsKey(name))
                return;

            var contentList = MaterialEditorControlFactory.CreatePanel(
                $"{this.name}Entry",
                scrollRect.content.transform,
                MaterialEditorPanelRole.Row);
            contentList.gameObject.AddComponent<LayoutElement>().preferredHeight = MaterialEditorUI.PanelHeight;
            contentList.gameObject.AddComponent<Mask>();

            var itemPanel = MaterialEditorControlFactory.CreatePanel(
                $"{this.name}EntryPanel",
                contentList.transform,
                MaterialEditorPanelRole.TransparentRow);
            itemPanel.gameObject.AddComponent<CanvasGroup>();
            itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>().padding = MaterialEditorUI.Padding;

            Toggle toggle = MaterialEditorControlFactory.CreateToggle($"{this.name}Toggle", itemPanel.transform, name);
            var toggleLE = toggle.gameObject.AddComponent<LayoutElement>();
            toggle.gameObject.GetComponentInChildren<CanvasRenderer>(true).transform.SetRect(0f, 1f, 0f, 1f, 1f, -18f, 18f, -1f);
            toggle.isOn = false;
            toggle.onValueChanged.AddListener(value => onValueChanged(value));

            itemPanel.gameObject.AddComponent<Button>().onClick.AddListener(() => toggle.isOn = !toggle.isOn);

            MaterialEditorStyles.ApplyRow(contentList.gameObject);
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
