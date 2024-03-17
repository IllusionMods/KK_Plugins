using System;
using UILib;
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

        internal SelectListEntryTemplate(Transform parent, string name)
        {
            this.Parent = parent;
            this.Name = name;

            ScrollRect = UIUtility.CreateScrollView(name, parent);
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
            toggleLE.preferredWidth = 12;
            toggleLE.flexibleWidth = 0;
            toggle.isOn = false;
            toggle.group = ToggleGroup;
            toggle.onValueChanged.AddListener(value => onValueChanged(value));

            itemPanel.gameObject.AddComponent<Button>().onClick.AddListener(() => toggle.isOn = !toggle.isOn);
        }

        internal void ClearList()
        {
            foreach (Transform child in ScrollRect.content.transform)
            {
                UnityEngine.Object.Destroy(child.gameObject);
            }
        }
    }
}
