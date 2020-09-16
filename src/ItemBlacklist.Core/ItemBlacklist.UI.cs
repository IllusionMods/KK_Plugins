using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace KK_Plugins
{
    public partial class ItemBlacklist
    {
        private Canvas ContextMenu;
        private CanvasGroup ContextMenuCanvasGroup;
        private Image ContextMenuPanel;
        private static Button BlacklistButton;
        private static Button BlacklistModButton;
        private static Button InfoButton;
        private static Dropdown FilterDropdown;
        private readonly float UIWidth = 0.175f;
        private readonly float UIHeight = 0.1375f;

        internal const float marginSize = 4f;
        internal const float panelHeight = 35f;
        internal const float scrollOffsetX = -15f;
        internal static readonly Color rowColor = new Color(0, 0, 0, 0.01f);
        internal static readonly RectOffset padding = new RectOffset(3, 3, 0, 1);

        protected void InitUI()
        {
            if (ContextMenu != null) return;

            UIUtility.Init(nameof(KK_Plugins));

            ContextMenu = UIUtility.CreateNewUISystem("ContextMenu");
            ContextMenu.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f);
            ContextMenu.gameObject.transform.SetParent(transform);
            ContextMenu.sortingOrder = 900;
            ContextMenuCanvasGroup = ContextMenu.GetOrAddComponent<CanvasGroup>();
            SetMenuVisibility(false);

            ContextMenuPanel = UIUtility.CreatePanel("Panel", ContextMenu.transform);
            ContextMenuPanel.color = new Color(0.5f, 0.5f,0.5f,0.5f);
            ContextMenuPanel.transform.SetRect(0.05f, 0.05f, UIWidth, UIHeight);

            UIUtility.AddOutlineToObject(ContextMenuPanel.transform, Color.black);

            var scrollRect = UIUtility.CreateScrollView("ContextMenuWindow", ContextMenuPanel.transform);
            scrollRect.transform.SetRect(0f, 0f, 1f, 1f, marginSize, marginSize, 0.5f - marginSize, -marginSize);
            scrollRect.gameObject.AddComponent<Mask>();
            scrollRect.content.gameObject.AddComponent<VerticalLayoutGroup>();
            scrollRect.content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.verticalScrollbar.GetComponent<RectTransform>().offsetMin = new Vector2(scrollOffsetX, 0f);
            scrollRect.viewport.offsetMax = new Vector2(scrollOffsetX, 0f);
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.GetComponent<Image>().color = rowColor;

            {
                var contentItem = UIUtility.CreatePanel("BlacklistContent", scrollRect.content.transform);
                contentItem.gameObject.AddComponent<LayoutElement>().preferredHeight = panelHeight;
                contentItem.gameObject.AddComponent<Mask>();
                contentItem.color = rowColor;
                
                var itemPanel = UIUtility.CreatePanel("BlacklistPanel", contentItem.transform);
                itemPanel.color = rowColor;
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>().padding = padding;

                BlacklistButton = UIUtility.CreateButton($"BlacklistButton", itemPanel.transform, "Hide this item");
                var layoutElement = BlacklistButton.gameObject.AddComponent<LayoutElement>();
                
                var text = BlacklistButton.GetComponentInChildren<Text>();
                text.resizeTextForBestFit = false;
                text.fontSize = 26;
            }
            {
                var contentItem = UIUtility.CreatePanel("BlacklistModContent", scrollRect.content.transform);
                contentItem.gameObject.AddComponent<LayoutElement>().preferredHeight = panelHeight;
                contentItem.gameObject.AddComponent<Mask>();
                contentItem.color = rowColor;
                
                var itemPanel = UIUtility.CreatePanel("BlacklistModPanel", contentItem.transform);
                itemPanel.color = rowColor;
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>().padding = padding;

                BlacklistModButton = UIUtility.CreateButton($"BlacklistModButton", itemPanel.transform, "Hide all items from this mod");
                var layoutElement = BlacklistModButton.gameObject.AddComponent<LayoutElement>();
                
                var text = BlacklistModButton.GetComponentInChildren<Text>();
                text.resizeTextForBestFit = false;
                text.fontSize = 26;
            }
            {
                var contentItem = UIUtility.CreatePanel("InfoContent", scrollRect.content.transform);
                contentItem.gameObject.AddComponent<LayoutElement>().preferredHeight = panelHeight;
                contentItem.gameObject.AddComponent<Mask>();
                contentItem.color = rowColor;
                
                var itemPanel = UIUtility.CreatePanel("InfoPanel", contentItem.transform);
                itemPanel.color = rowColor;
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>().padding = padding;

                InfoButton = UIUtility.CreateButton($"InfoButton", itemPanel.transform, "Print item info");
                var layoutElement = InfoButton.gameObject.AddComponent<LayoutElement>();
                
                var text = InfoButton.GetComponentInChildren<Text>();
                text.resizeTextForBestFit = false;
                text.fontSize = 26;
            }

            {
                var contentItem = UIUtility.CreatePanel("FilterContent", scrollRect.content.transform);
                contentItem.gameObject.AddComponent<LayoutElement>().preferredHeight = panelHeight;
                contentItem.gameObject.AddComponent<Mask>();
                contentItem.color = rowColor;
                
                var itemPanel = UIUtility.CreatePanel("FilterPanel", contentItem.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>().padding = padding;
                itemPanel.color = rowColor;
        
                var label = UIUtility.CreateText("FilterText", itemPanel.transform, "Displaying:");
                label.color = Color.white;
                label.resizeTextForBestFit = false;
                label.fontSize = 26;
                label.alignment = TextAnchor.MiddleCenter;

                var labelLE = label.gameObject.AddComponent<LayoutElement>();
                labelLE.preferredWidth = 20f;
                labelLE.flexibleWidth = 20f;

                FilterDropdown = UIUtility.CreateDropdown("FilterDropdown", itemPanel.transform);
                FilterDropdown.transform.SetRect(0f, 0f, 0f, 1f, 0f, 0f, 100f);
                FilterDropdown.captionText.transform.SetRect(0f, 0f, 1f, 1f, 0f, 2f, -15f, -2f);
                FilterDropdown.captionText.resizeTextForBestFit = false;
                FilterDropdown.captionText.fontSize = 26;
                FilterDropdown.captionText.alignment = TextAnchor.MiddleCenter;
                FilterDropdown.itemText.fontStyle = FontStyle.Bold;
                
                FilterDropdown.options.Clear();
                FilterDropdown.options.Add(new Dropdown.OptionData("Filtered List"));
                FilterDropdown.options.Add(new Dropdown.OptionData("Hidden Items"));
                FilterDropdown.options.Add(new Dropdown.OptionData("All Items"));
                FilterDropdown.value = 0;
                FilterDropdown.captionText.text = "Filtered List";
                var dropdownEnabledLE = FilterDropdown.gameObject.AddComponent<LayoutElement>();
                dropdownEnabledLE.preferredWidth = 30;
                dropdownEnabledLE.flexibleWidth = 30;

                FilterDropdown.onValueChanged.AddListener(delegate (int value)
                {
                    ChangeListFilter((ListVisibilityType)value);
                    SetMenuVisibility(false);
                });
            }
        }
    }
}