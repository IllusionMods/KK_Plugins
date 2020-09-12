using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace KK_Plugins
{
    public partial class ItemBlacklist
    {
        Canvas ContextMenu;
        Image ContextMenuPanel;
        private static Button BlacklistButton;
        private static Button BlacklistModButton;
        private static Button InfoButton;
        readonly float UIWidth = 0.175f;
        readonly float UIHeight = 0.105f;

        internal const float marginSize = 5f;
        internal const float panelHeight = 35f;
        internal const float scrollOffsetX = -15f;
        internal const float labelWidth = 50f;
        internal static readonly Color rowColor = new Color(1f, 1f, 1f, 1f);
        internal static readonly RectOffset padding = new RectOffset(3, 3, 0, 1);

        protected void InitUI()
        {
            if (ContextMenu != null) return;

            UIUtility.Init(nameof(KK_Plugins));

            ContextMenu = UIUtility.CreateNewUISystem("ContextMenu");
            ContextMenu.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f);
            ContextMenu.gameObject.transform.SetParent(transform);
            ContextMenu.sortingOrder = 900;
            ContextMenu.gameObject.SetActive(false);

            ContextMenuPanel = UIUtility.CreatePanel("Panel", ContextMenu.transform);
            ContextMenuPanel.color = Color.white;
            ContextMenuPanel.transform.SetRect(0.05f, 0.05f, UIWidth, UIHeight);

            UIUtility.AddOutlineToObject(ContextMenuPanel.transform, Color.black);

            var scrollRect = UIUtility.CreateScrollView("ContextMenuWindow", ContextMenuPanel.transform);
            scrollRect.transform.SetRect(0f, 0f, 1f, 1f, marginSize, marginSize, -marginSize, -marginSize / 2f);
            scrollRect.gameObject.AddComponent<Mask>();
            scrollRect.content.gameObject.AddComponent<VerticalLayoutGroup>();
            scrollRect.content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.verticalScrollbar.GetComponent<RectTransform>().offsetMin = new Vector2(scrollOffsetX, 0f);
            scrollRect.viewport.offsetMax = new Vector2(scrollOffsetX, 0f);
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            {
                var contentItem = UIUtility.CreatePanel("BlacklistContent", scrollRect.content.transform);
                contentItem.gameObject.AddComponent<LayoutElement>().preferredHeight = panelHeight;
                contentItem.gameObject.AddComponent<Mask>();
                contentItem.color = rowColor;
                var itemPanel = UIUtility.CreatePanel("BlacklistPanel", contentItem.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>().padding = padding;

                BlacklistButton = UIUtility.CreateButton($"BlacklistButton", itemPanel.transform, "Hide this item");
                var layoutElement = BlacklistButton.gameObject.AddComponent<LayoutElement>();
                layoutElement.preferredWidth = labelWidth;
                layoutElement.flexibleWidth = labelWidth;
            }
            {
                var contentItem = UIUtility.CreatePanel("BlacklistModContent", scrollRect.content.transform);
                contentItem.gameObject.AddComponent<LayoutElement>().preferredHeight = panelHeight;
                contentItem.gameObject.AddComponent<Mask>();
                contentItem.color = rowColor;
                var itemPanel = UIUtility.CreatePanel("BlacklistModPanel", contentItem.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>().padding = padding;

                BlacklistModButton = UIUtility.CreateButton($"BlacklistModButton", itemPanel.transform, "Hide all items from this mod");
                var layoutElement = BlacklistModButton.gameObject.AddComponent<LayoutElement>();
                layoutElement.preferredWidth = labelWidth;
                layoutElement.flexibleWidth = labelWidth;
            }
            {
                var contentItem = UIUtility.CreatePanel("InfoContent", scrollRect.content.transform);
                contentItem.gameObject.AddComponent<LayoutElement>().preferredHeight = panelHeight;
                contentItem.gameObject.AddComponent<Mask>();
                contentItem.color = rowColor;
                var itemPanel = UIUtility.CreatePanel("InfoPanel", contentItem.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>().padding = padding;

                InfoButton = UIUtility.CreateButton($"InfoButton", itemPanel.transform, "Print item info");
                var layoutElement = InfoButton.gameObject.AddComponent<LayoutElement>();
                layoutElement.preferredWidth = labelWidth;
                layoutElement.flexibleWidth = labelWidth;
            }
        }
    }
}