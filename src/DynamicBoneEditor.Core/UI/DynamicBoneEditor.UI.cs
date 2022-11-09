using KKAPI.Maker;
using KKAPI.Maker.UI;
using System.Linq;
using UILib;
using UnityEngine;
using UnityEngine.UI;
using static KK_Plugins.DynamicBoneEditor.Plugin;

namespace KK_Plugins.DynamicBoneEditor
{
    public static class UI
    {
        public static Canvas EditorWindow;
        public static Image EditorMainPanel;
        public static Image DragPanel;
        public static MakerButton DynamicBoneEditorButton;
        private static ScrollRect EditorScrollableUI;
        internal static RectOffset Padding;

        internal const float UIScale = 1.75f;
        internal const float UIWidth = 0.3f;
        internal const float UIHeight = 0.3f;
        internal const float MarginSize = 5f;
        internal const float HeaderSize = 20f;
        internal const float ScrollOffsetX = -15f;
        internal const float PanelHeight = 20f;
        internal const float LabelWidth = 50f;
        internal const float ButtonWidth = 100f;
        internal const float DropdownWidth = 250f;
        internal const float TextBoxWidth = 75f;
        internal const float ResetButtonWidth = 50f;
        internal const float SliderWidth = 150f;
        internal const float ToggleWidth = 75f;
        internal static readonly Color RowColor = new Color(1f, 1f, 1f, 0.6f);
        internal static readonly Color ItemColor = new Color(1f, 1f, 1f, 0f);

        internal static Dropdown DynamicBoneDropdown;
        internal static ToggleSet FreezeAxis = new ToggleSet("Freeze Axis", new[] { "None", "X", "Y", "Z" });
        internal static SliderSet Weight = new SliderSet("Weight");
        internal static SliderSet Damping = new SliderSet("Damping");
        internal static SliderSet Elasticity = new SliderSet("Elasticity");
        internal static SliderSet Stiffness = new SliderSet("Stiffness");
        internal static SliderSet Inertia = new SliderSet("Inertia");
        internal static SliderSet Radius = new SliderSet("Radius");
        internal static Texture2D WindowBackground { get; set; }

        public static bool Visible
        {
            get
            {
                if (DynamicBoneEditorButton != null && EditorWindow != null && EditorWindow.gameObject != null)
                    return EditorWindow.gameObject.activeInHierarchy;
                return false;
            }
            set
            {
                if (EditorWindow != null)
                {
                    EditorWindow.gameObject.SetActive(value);
                }
            }
        }

        public static void InitUI()
        {
            DynamicBoneEditorButton = MakerAPI.AddAccessoryWindowControl(new MakerButton("Dynamic Bone Editor", null, PluginInstance));
#if !PH
            DynamicBoneEditorButton.GroupingID = "Buttons";
#endif
            DynamicBoneEditorButton.OnClick.AddListener(() => ShowUI(0));

            var windowBackground = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            windowBackground.SetPixel(0, 0, new Color(0f, 0f, 0f, 0f));
            windowBackground.Apply();
            WindowBackground = windowBackground;

            Padding = new RectOffset(3, 2, 0, 1);

            EditorWindow = UIUtility.CreateNewUISystem("DynamicBoneEditorCanvas");
            EditorWindow.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f / UIScale, 1080f / UIScale);
            Visible = false;
            EditorWindow.sortingOrder = 1000;

            EditorMainPanel = UIUtility.CreatePanel("Panel", EditorWindow.transform);
            EditorMainPanel.color = Color.white;
            EditorMainPanel.transform.SetRect(0.05f, 0.05f, UIWidth * UIScale, UIHeight * UIScale);

            UIUtility.AddOutlineToObject(EditorMainPanel.transform, Color.black);

            DragPanel = UIUtility.CreatePanel("Draggable", EditorMainPanel.transform);
            DragPanel.transform.SetRect(0f, 1f, 1f, 1f, 0f, -HeaderSize);
            DragPanel.color = Color.gray;
            UIUtility.MakeObjectDraggable(DragPanel.rectTransform, EditorMainPanel.rectTransform);

            var nametext = UIUtility.CreateText("Nametext", DragPanel.transform, "Dynamic Bone Editor");
            nametext.transform.SetRect();
            nametext.alignment = TextAnchor.MiddleCenter;

            var close = UIUtility.CreateButton("CloseButton", DragPanel.transform, "");
            close.transform.SetRect(1f, 0f, 1f, 1f, -20f, 1f, -1f, -1f);
            close.onClick.AddListener(() => Visible = false);

            //X button
            var x1 = UIUtility.CreatePanel("x1", close.transform);
            x1.transform.SetRect(0f, 0f, 1f, 1f, 8f, 0f, -8f);
            x1.rectTransform.eulerAngles = new Vector3(0f, 0f, 45f);
            x1.color = Color.black;
            var x2 = UIUtility.CreatePanel("x2", close.transform);
            x2.transform.SetRect(0f, 0f, 1f, 1f, 8f, 0f, -8f);
            x2.rectTransform.eulerAngles = new Vector3(0f, 0f, -45f);
            x2.color = Color.black;

            EditorScrollableUI = UIUtility.CreateScrollView("DynamicBoneEditorWindow", EditorMainPanel.transform);
            EditorScrollableUI.transform.SetRect(0f, 0f, 1f, 1f, MarginSize, MarginSize, -MarginSize, -HeaderSize - MarginSize / 2f);
            EditorScrollableUI.gameObject.AddComponent<Mask>();
            EditorScrollableUI.content.gameObject.AddComponent<VerticalLayoutGroup>();
            EditorScrollableUI.content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            EditorScrollableUI.verticalScrollbar.GetComponent<RectTransform>().offsetMin = new Vector2(ScrollOffsetX, 0f);
            EditorScrollableUI.viewport.offsetMax = new Vector2(ScrollOffsetX, 0f);
            EditorScrollableUI.movementType = ScrollRect.MovementType.Clamped;
            EditorScrollableUI.verticalScrollbar.GetComponent<Image>().color = new Color(1, 1, 1, 0.6f);

            {
                var contentList = UIUtility.CreatePanel("ListEntry", EditorScrollableUI.content.transform);
                contentList.gameObject.AddComponent<LayoutElement>().preferredHeight = PanelHeight;
                contentList.gameObject.AddComponent<Mask>();
                contentList.color = RowColor;

                var itemPanel = UIUtility.CreatePanel("DynamicBonePanel", contentList.transform);
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>().padding = Padding;
                itemPanel.color = ItemColor;

                var label = UIUtility.CreateText("DynamicBoneLabel", itemPanel.transform, "Dynamic Bone");
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.black;
                var labelLE = label.gameObject.AddComponent<LayoutElement>();
                labelLE.preferredWidth = LabelWidth;
                labelLE.flexibleWidth = LabelWidth;

                DynamicBoneDropdown = UIUtility.CreateDropdown("DynamicBoneDropdown", itemPanel.transform);
                DynamicBoneDropdown.transform.SetRect(0f, 0f, 0f, 1f, 0f, 0f, 100f);
                DynamicBoneDropdown.captionText.transform.SetRect(0f, 0f, 1f, 1f, 5f, 2f, -15f, -2f);
                DynamicBoneDropdown.captionText.alignment = TextAnchor.MiddleLeft;
                var dropdownEnabledLE = DynamicBoneDropdown.gameObject.AddComponent<LayoutElement>();
                dropdownEnabledLE.preferredWidth = DropdownWidth;
                dropdownEnabledLE.flexibleWidth = 0;
            }

            FreezeAxis.CreateUI(EditorScrollableUI.content.transform);
            Weight.CreateUI(EditorScrollableUI.content.transform);
            Damping.CreateUI(EditorScrollableUI.content.transform);
            Elasticity.CreateUI(EditorScrollableUI.content.transform);
            Stiffness.CreateUI(EditorScrollableUI.content.transform);
            Inertia.CreateUI(EditorScrollableUI.content.transform);
            Radius.CreateUI(EditorScrollableUI.content.transform);
        }

        public static void DestroyUI()
        {
            DynamicBoneEditorButton = null;
        }

        public static void ShowUI(int dynamicBoneIndex)
        {
            if (!MakerAPI.InsideAndLoaded)
                return;
            if (DynamicBoneEditorButton == null)
                return;

            int slot = AccessoriesApi.SelectedMakerAccSlot;

            var accessory = MakerAPI.GetCharacterControl().GetAccessoryObject(slot);
            if (accessory == null)
            {
                Visible = false;
                return;
            }
            else
            {
                var dynamicBones = accessory.GetComponentsInChildren<DynamicBone>().Where(x => x.m_Root != null).ToList();

                if (dynamicBones.Count == 0)
                {
                    Visible = false;
                    return;
                }

                Visible = true;

                var dynamicBone = dynamicBones[dynamicBoneIndex];
                var controller = GetMakerCharaController();

                DynamicBoneDropdown.onValueChanged.RemoveAllListeners();
                DynamicBoneDropdown.options.Clear();
                foreach (var bone in dynamicBones)
                    DynamicBoneDropdown.options.Add(new Dropdown.OptionData(bone.m_Root.name));
                DynamicBoneDropdown.value = dynamicBoneIndex;
                DynamicBoneDropdown.captionText.text = dynamicBones[dynamicBoneIndex].m_Root.name;
                DynamicBoneDropdown.onValueChanged.AddListener(value => { ShowUI(value); });

                FreezeAxis.OnChange = null;
                FreezeAxis.ValueOriginal = (int)controller.GetFreezeAxisOriginal(slot, dynamicBone);
                var freezeAxis = controller.GetFreezeAxis(slot, dynamicBone);
                FreezeAxis.Value = freezeAxis == null ? FreezeAxis.ValueOriginal : (int)freezeAxis;
                FreezeAxis.OnChange = (value) => { GetMakerCharaController().SetFreezeAxis(slot, dynamicBone, (DynamicBone.FreezeAxis)value); };

                Weight.OnChange = null;
                Weight.ValueOriginal = controller.GetWeightOriginal(slot, dynamicBone);
                var weight = controller.GetWeight(slot, dynamicBone);
                Weight.Value = weight == null ? Weight.ValueOriginal : (float)weight;
                Weight.OnChange = (value) => { GetMakerCharaController().SetWeight(slot, dynamicBone, value); };

                Damping.OnChange = null;
                Damping.ValueOriginal = controller.GetDampingOriginal(slot, dynamicBone);
                var damping = controller.GetDamping(slot, dynamicBone);
                Damping.Value = damping == null ? Damping.ValueOriginal : (float)damping;
                Damping.OnChange = (value) => { GetMakerCharaController().SetDamping(slot, dynamicBone, value); };

                Elasticity.OnChange = null;
                Elasticity.ValueOriginal = controller.GetElasticityOriginal(slot, dynamicBone);
                var elasticity = controller.GetElasticity(slot, dynamicBone);
                Elasticity.Value = elasticity == null ? Elasticity.ValueOriginal : (float)elasticity;
                Elasticity.OnChange = (value) => { GetMakerCharaController().SetElasticity(slot, dynamicBone, value); };

                Stiffness.OnChange = null;
                Stiffness.ValueOriginal = controller.GetStiffnessOriginal(slot, dynamicBone);
                var stiffness = controller.GetStiffness(slot, dynamicBone);
                Stiffness.Value = stiffness == null ? Stiffness.ValueOriginal : (float)stiffness;
                Stiffness.OnChange = (value) => { GetMakerCharaController().SetStiffness(slot, dynamicBone, value); };

                Inertia.OnChange = null;
                Inertia.ValueOriginal = controller.GetInertiaOriginal(slot, dynamicBone);
                var inertia = controller.GetInertia(slot, dynamicBone);
                Inertia.Value = inertia == null ? Inertia.ValueOriginal : (float)inertia;
                Inertia.OnChange = (value) => { GetMakerCharaController().SetInertia(slot, dynamicBone, value); };

                Radius.OnChange = null;
                Radius.ValueOriginal = controller.GetRadiusOriginal(slot, dynamicBone);
                var radius = controller.GetRadius(slot, dynamicBone);
                Radius.Value = radius == null ? Radius.ValueOriginal : (float)radius;
                Radius.OnChange = (value) => { GetMakerCharaController().SetRadius(slot, dynamicBone, value); };
            }
        }

        public static void ToggleButtonVisibility()
        {
            if (!MakerAPI.InsideMaker)
                return;
            if (DynamicBoneEditorButton == null)
                return;

            var accessory = MakerAPI.GetCharacterControl().GetAccessoryObject(AccessoriesApi.SelectedMakerAccSlot);
            if (accessory == null)
            {
                DynamicBoneEditorButton.Visible.OnNext(false);
                return;
            }
            else
            {
                var dynamicBones = accessory.GetComponentsInChildren<DynamicBone>().Where(x => x.m_Root != null);
                if (dynamicBones.Count() == 0)
                    DynamicBoneEditorButton.Visible.OnNext(false);
                else
                    DynamicBoneEditorButton.Visible.OnNext(true);
            }
        }
    }
}
