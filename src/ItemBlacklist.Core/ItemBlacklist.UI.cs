using ChaCustom;
using HarmonyLib;
using Sideloader.AutoResolver;
using System.Collections.Generic;
using System.Linq;
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

        internal const float MarginSize = 4f;
        internal const float PanelHeight = 35f;
        internal const float ScrollOffsetX = -15f;
        internal static readonly Color RowColor = new Color(0, 0, 0, 0.01f);
        internal static RectOffset Padding;

        protected void InitUI()
        {
            if (ContextMenu != null) return;
            if (CustomBase.Instance == null) return;

            ContextMenu = UIUtility.CreateNewUISystem("ContextMenu");
            ContextMenu.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f);
            ContextMenu.transform.SetParent(CustomBase.Instance.transform);
            ContextMenu.sortingOrder = 900;
            ContextMenuCanvasGroup = ContextMenu.GetOrAddComponent<CanvasGroup>();
            SetMenuVisibility(false);

            ContextMenuPanel = UIUtility.CreatePanel("Panel", ContextMenu.transform);
            ContextMenuPanel.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            ContextMenuPanel.transform.SetRect(0.05f, 0.05f, UIWidth, UIHeight);

            UIUtility.AddOutlineToObject(ContextMenuPanel.transform, Color.black);

            var scrollRect = UIUtility.CreateScrollView("ContextMenuWindow", ContextMenuPanel.transform);
            scrollRect.transform.SetRect(0f, 0f, 1f, 1f, MarginSize, MarginSize, 0.5f - MarginSize, -MarginSize);
            scrollRect.gameObject.AddComponent<Mask>();
            scrollRect.content.gameObject.AddComponent<VerticalLayoutGroup>();
            scrollRect.content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.verticalScrollbar.GetComponent<RectTransform>().offsetMin = new Vector2(ScrollOffsetX, 0f);
            scrollRect.viewport.offsetMax = new Vector2(ScrollOffsetX, 0f);
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.GetComponent<Image>().color = RowColor;

            {
                var contentItem = UIUtility.CreatePanel("BlacklistContent", scrollRect.content.transform);
                contentItem.gameObject.AddComponent<LayoutElement>().preferredHeight = PanelHeight;
                contentItem.gameObject.AddComponent<Mask>();
                contentItem.color = RowColor;

                var itemPanel = UIUtility.CreatePanel("BlacklistPanel", contentItem.transform);
                itemPanel.color = RowColor;
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>().padding = Padding;

                BlacklistButton = UIUtility.CreateButton("BlacklistButton", itemPanel.transform, "Hide this item");
                BlacklistButton.gameObject.AddComponent<LayoutElement>();

                var text = BlacklistButton.GetComponentInChildren<Text>();
                text.resizeTextForBestFit = false;
                text.fontSize = 26;
            }
            {
                var contentItem = UIUtility.CreatePanel("BlacklistModContent", scrollRect.content.transform);
                contentItem.gameObject.AddComponent<LayoutElement>().preferredHeight = PanelHeight;
                contentItem.gameObject.AddComponent<Mask>();
                contentItem.color = RowColor;

                var itemPanel = UIUtility.CreatePanel("BlacklistModPanel", contentItem.transform);
                itemPanel.color = RowColor;
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>().padding = Padding;

                BlacklistModButton = UIUtility.CreateButton("BlacklistModButton", itemPanel.transform, "Hide all items from this mod");
                BlacklistModButton.gameObject.AddComponent<LayoutElement>();

                var text = BlacklistModButton.GetComponentInChildren<Text>();
                text.resizeTextForBestFit = false;
                text.fontSize = 26;
            }
            {
                var contentItem = UIUtility.CreatePanel("InfoContent", scrollRect.content.transform);
                contentItem.gameObject.AddComponent<LayoutElement>().preferredHeight = PanelHeight;
                contentItem.gameObject.AddComponent<Mask>();
                contentItem.color = RowColor;

                var itemPanel = UIUtility.CreatePanel("InfoPanel", contentItem.transform);
                itemPanel.color = RowColor;
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>().padding = Padding;

                InfoButton = UIUtility.CreateButton("InfoButton", itemPanel.transform, "Print item info");
                InfoButton.gameObject.AddComponent<LayoutElement>();

                var text = InfoButton.GetComponentInChildren<Text>();
                text.resizeTextForBestFit = false;
                text.fontSize = 26;
            }

            {
                var contentItem = UIUtility.CreatePanel("FilterContent", scrollRect.content.transform);
                contentItem.gameObject.AddComponent<LayoutElement>().preferredHeight = PanelHeight;
                contentItem.gameObject.AddComponent<Mask>();
                contentItem.color = RowColor;

                var itemPanel = UIUtility.CreatePanel("FilterPanel", contentItem.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>().padding = Padding;
                itemPanel.color = RowColor;

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
                Text captionText = FilterDropdown.captionText;
                captionText.transform.SetRect(0f, 0f, 1f, 1f, 0f, 2f, -15f, -2f);
                captionText.resizeTextForBestFit = false;
                captionText.fontSize = 26;
                captionText.alignment = TextAnchor.MiddleCenter;
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

                FilterDropdown.onValueChanged.AddListener(value =>
                {
                    ChangeListFilter((ListVisibilityType)value);
                    SetMenuVisibility(false);
                });
            }
        }

        private void ShowMenu()
        {
            if (CustomBase.Instance == null) return;
            InitUI();

            SetMenuVisibility(false);
            if (CurrentCustomSelectInfoComponent == null) return;
            if (!MouseIn) return;

            var xPosition = Input.mousePosition.x / Screen.width + 0.01f;
            var yPosition = Input.mousePosition.y / Screen.height - UIHeight - 0.01f;

            ContextMenuPanel.transform.SetRect(xPosition, yPosition, UIWidth + xPosition, UIHeight + yPosition);
            SetMenuVisibility(true);

            List<CustomSelectInfo> lstSelectInfo = (List<CustomSelectInfo>)Traverse.Create(CustomSelectListCtrlInstance).Field("lstSelectInfo").GetValue();
            int index = CurrentCustomSelectInfoComponent.info.index;
            var customSelectInfo = lstSelectInfo.First(x => x.index == index);
            string guid = null;
            int category = customSelectInfo.category;
            int id = index;

            if (index >= UniversalAutoResolver.BaseSlotID)
            {
                ResolveInfo info = UniversalAutoResolver.TryGetResolutionInfo((ChaListDefine.CategoryNo)customSelectInfo.category, customSelectInfo.index);
                if (info != null)
                {
                    guid = info.GUID;
                    id = info.Slot;
                }
            }

            if (ListVisibility.TryGetValue(CustomSelectListCtrlInstance, out var listVisibilityType))
                FilterDropdown.Set((int)listVisibilityType);

            BlacklistButton.onClick.RemoveAllListeners();
            BlacklistModButton.onClick.RemoveAllListeners();
            InfoButton.onClick.RemoveAllListeners();

            if (guid == null)
            {
                BlacklistButton.enabled = false;
                BlacklistModButton.enabled = false;
            }
            else
            {
                BlacklistButton.enabled = true;
                BlacklistModButton.enabled = true;
                if (CheckBlacklist(guid, category, id))
                {
                    BlacklistButton.GetComponentInChildren<Text>().text = "Unhide this item";
                    BlacklistButton.onClick.AddListener(() => UnblacklistItem(guid, category, id, index));
                    BlacklistModButton.GetComponentInChildren<Text>().text = "Unhide all items from this mod";
                    BlacklistModButton.onClick.AddListener(() => UnblacklistMod(guid));
                }
                else
                {
                    BlacklistButton.GetComponentInChildren<Text>().text = "Hide this item";
                    BlacklistButton.onClick.AddListener(() => BlacklistItem(guid, category, id, index));
                    BlacklistModButton.GetComponentInChildren<Text>().text = "Hide all items from this mod";
                    BlacklistModButton.onClick.AddListener(() => BlacklistMod(guid));
                }
            }

            InfoButton.onClick.AddListener(() => PrintInfo(index));

        }

        public void SetMenuVisibility(bool visible)
        {
            if (ContextMenuCanvasGroup == null) return;
            ContextMenuCanvasGroup.alpha = visible ? 1 : 0;
            ContextMenuCanvasGroup.blocksRaycasts = visible;
        }
    }
}