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

namespace MaterialEditorAPI
{
    /// <summary>
    /// Code for the MaterialEditor UI
    /// </summary>
#pragma warning disable BepInEx001 // Class inheriting from BaseUnityPlugin missing BepInPlugin attribute
    public abstract class MaterialEditorUI : BaseUnityPlugin
#pragma warning restore BepInEx001 // Class inheriting from BaseUnityPlugin missing BepInPlugin attribute
    {
        /// <summary>
        /// Element containing the entire UI
        /// </summary>
        public static Canvas MaterialEditorWindow;
        /// <summary>
        /// Main panel
        /// </summary>
        public static Image MaterialEditorMainPanel;
        /// <summary>
        /// Draggable header
        /// </summary>
        public static Image DragPanel;
        private static ScrollRect MaterialEditorScrollableUI;
        private static InputField FilterInputField;

        private static Button ViewListButton;
        private static SelectListPanel MaterialEditorRendererList;
        private static List<Renderer> SelectedRenderers = new List<Renderer>();
        private static SelectListPanel MaterialEditorMaterialList;
        private static List<Material> SelectedMaterials = new List<Material>();
        private static bool ListsVisible = false;

        private static SelectListPanel MaterialEditorRenameList;
        private static InputField MaterialEditorRenameField;
        private static Button MaterialEditorRenameButton;
        private static Text MaterialEditorRenameMaterial;
        private static List<Renderer> SelectedMaterialRenderers = new List<Renderer>();
        private static bool RenameListVisible = false;

        internal static readonly HashSet<string> CollapsedMaterials = new HashSet<string>();
        internal static readonly HashSet<int> CollapsedRenderers = new HashSet<int>();
        internal static bool RendererSectionCollapsed = false;
        internal static bool MaterialSectionCollapsed = false;
        internal static bool HackerMode = false;

        /// <summary>Singleton instance of the MaterialEditorUI</summary>
        public static MaterialEditorUI UIInstance { get; private set; }

        internal static FileSystemWatcher TexChangeWatcher;
        private VirtualList VirtualList;

        internal const float MarginSize = 5f;
        internal const float HeaderSize = 20f;
        internal const float ScrollOffsetX = -10f; // scrollbar width
        internal const float PanelHeight = 22f;

        #region Entry Item Width
        // General
        internal const float LabelWidth = 0f;
        internal const float ButtonWidth = 100f;
        internal const float SmallButtonWidth = 20f;
        internal const float ResetButtonWidth = SmallButtonWidth * 2f;
        internal const float InterpolableButtonWidth = SmallButtonWidth;
        internal const float ContentFullWidth = 316f;
        // Renderer (Enbale/ShadowCastingMode/ReceiveShadows/RendererUpdateWhenOffscreen/RecalulateNormals)
        internal const float RendererButtonWidth = ButtonWidth * 0.65f;
        internal const float RendererToggleWidth = 20f;
        internal const float RendererDropdownWidth = 94f;
        // Material
        internal const float MaterialButtonWidth = ButtonWidth * 0.55f;
        internal const float MaterialRenameButtonWidth = SmallButtonWidth;
        // Shader
        internal const float ShaderDropdownWidth = ContentFullWidth;
        // RenderQueue
        internal const float RenderQueueInputFieldWidth = 94f;
        // Texture
        internal const float TextureButtonWidth = ContentFullWidth / 2f;
        // Texture Offset and Scale
        internal const float OffsetScaleLabelXWidth = 48f;
        internal const float OffsetScaleLabelYWidth = 10f;
        internal const float OffsetScaleInputFieldWidth = 50f;
        // Color
        internal const float ColorLabelWidth = 10f;
        internal const float ColorInputFieldWidth = 64f;
        internal const float ColorEditButtonWidth = 20f;
        // Float
        internal const float FloatSliderWidth = ContentFullWidth - 94f;
        internal const float FloatInputFieldWidth = 94f;
        // Keyword
        internal const float KeywordToggleWidth = ContentFullWidth;
        #endregion

        internal static RectOffset Padding;
        internal static RectOffset SubRowPadding;

        #region Colors
        //Light mode base colours
        internal static Color RowColor     = new Color(0.94f, 0.94f, 0.95f, 1f);
        internal static Color RowColorAlt  = new Color(1.00f, 1.00f, 1.00f, 1f);
        // https://simplified.com/blog/colors/triadic-colors
        internal static Color RendererColor  = new Color(1.00f, 0.96f, 0.90f, 1f);
        internal static Color MaterialColor  = new Color(0.93f, 0.98f, 0.93f, 1f);
        internal static Color CategoryColor  = new Color(0.91f, 0.86f, 0.98f, 1f);

        internal static Color RendererSectionColor = new Color(0.98f, 0.88f, 0.70f, 1f);
        internal static Color MaterialSectionColor = new Color(0.78f, 0.93f, 0.80f, 1f);
        internal static Color RendererSectionAccent = new Color(0.72f, 0.42f, 0.02f, 1f);
        internal static Color MaterialSectionAccent = new Color(0.14f, 0.50f, 0.20f, 1f);
        internal static Color RendererSectionText = new Color(0.35f, 0.18f, 0.00f, 1f);
        internal static Color MaterialSectionText = new Color(0.04f, 0.24f, 0.06f, 1f);

        internal static Color ItemColor        = new Color(0f, 0f, 0f, 0f);
        internal static Color ItemColorChanged = new Color(0f, 0f, 0f, 0.08f);
        internal static Color ItemTextColor    = Color.black;
        #endregion

        private static Font _arialFont;
        private static Font _interRegular;
        private static Font _interBold;
        private static Font _cascadiaFont;
        private static Font _notoRegular;
        private static Font _notoBold;
        private static Sprite _flatSprite;

        /// <summary>1x1 white sprite for a flat squared look instead of UILib's rounded sprites.</summary>
        internal static Sprite FlatSprite
        {
            get
            {
                if (_flatSprite == null)
                {
                    var tex = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                    tex.SetPixel(0, 0, Color.white);
                    tex.Apply();
                    _flatSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
                }
                return _flatSprite;
            }
        }

        private static void LoadFonts()
        {
            if (_interRegular == null)
            {
                try
                {
                    var asm = System.Reflection.Assembly.GetExecutingAssembly();
                    foreach (var name in asm.GetManifestResourceNames())
                    {
                        if (!name.EndsWith("mefonts.unity3d")) continue;
                        using (var stream = asm.GetManifestResourceStream(name))
                        {
                            byte[] buf = new byte[stream.Length];
                            stream.Read(buf, 0, buf.Length);
                            var bundle = AssetBundle.LoadFromMemory(buf);
                            if (bundle == null) return;
                            _interRegular = bundle.LoadAsset<Font>("Assets/Fonts/JetBrainsMonoNL-Regular.ttf");
                            _interBold    = bundle.LoadAsset<Font>("Assets/Fonts/JetBrainsMonoNL-Bold.ttf");
                            _cascadiaFont = bundle.LoadAsset<Font>("Assets/Fonts/CascadiaMono.ttf");
                            _notoRegular  = bundle.LoadAsset<Font>("Assets/Fonts/NotoSans-Regular.ttf");
                            _notoBold     = bundle.LoadAsset<Font>("Assets/Fonts/NotoSans-Bold.ttf");
                            bundle.Unload(false);
                        }
                        break;
                    }
                }
                catch (System.Exception e)
                {
                    MaterialEditorPluginBase.Logger.LogWarning($"[ME] Could not load font bundle: {e.Message}");
                }
            }
            ApplyFontChoice();
        }

        internal static void ApplyFontChoice()
        {
            Font chosen;
            switch (MaterialEditorPluginBase.UIFont?.Value)
            {
                case MaterialEditorPluginBase.MEFont.JetBrainsMono when _interRegular != null:
                    chosen = _interRegular; break;
                case MaterialEditorPluginBase.MEFont.CascadiaMono when _cascadiaFont != null:
                    chosen = _cascadiaFont; break;
                case MaterialEditorPluginBase.MEFont.NotoSans when _notoRegular != null:
                    chosen = _notoRegular; break;
                default:
                    chosen = _arialFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf"); break;
            }
            UILib.UIUtility.defaultFont = chosen;
            //Update all existing text elements in the ME window
            if (MaterialEditorWindow != null)
                foreach (var txt in MaterialEditorWindow.GetComponentsInChildren<Text>(true))
                    txt.font = chosen;
        }

        private static Image FooterPanel;
        private static Button FooterRendererSectionButton;
        private static Button FooterMaterialSectionButton;
        private static Button CollapseRenderersButton;
        private static Button CollapseMaterialsButton;

        /// <summary>Show or hide the R and M header buttons based on config</summary>
        public static void UpdateHeaderButtons()
        {
            CollapseRenderersButton?.gameObject.SetActive(MaterialEditorPluginBase.ShowCollapseRenderersButton.Value);
            CollapseMaterialsButton?.gameObject.SetActive(MaterialEditorPluginBase.ShowCollapseMaterialsButton.Value);
        }

        private static RawImage TexturePreviewImage;
        private static Text TexturePreviewNameText;
        private static GameObject TexturePreviewPanel;
        private static bool TexturePreviewVisible = false;
        private static Vector2 TexturePreviewDefaultSize;
        private static Vector2 TexturePreviewDefaultPosition;

        /// <summary>
        /// Update the texture preview panel with the given texture and name
        /// </summary>
        public static void SetTexturePreview(Texture texture, string textureName)
        {
            if (TexturePreviewPanel == null || !TexturePreviewVisible) return;
            TexturePreviewImage.texture = texture;
            TexturePreviewImage.gameObject.SetActive(texture != null);
            TexturePreviewNameText.text = texture != null ? textureName : "No Texture";
            TexturePreviewImage.uvRect = new Rect(0, 0, 1, 1);
            // Update the aspect ratio fitter to match the actual texture
            var arf = TexturePreviewImage.GetComponent<AspectRatioFitter>();
            if (arf != null)
                arf.aspectRatio = (texture != null && texture.width > 0 && texture.height > 0)
                    ? (float)texture.width / texture.height
                    : 1f;
        }

        /// <summary>
        /// Apply light or dark theme to the Material Editor UI
        /// </summary>
        public static void ApplyTheme()
        {
            if (MaterialEditorMainPanel == null) return;
            bool dark = MaterialEditorPluginBase.DarkMode.Value;
            float op = MaterialEditorPluginBase.WindowOpacity.Value;

            //Hacker mode: black bg, green text, Cascadia font
            if (HackerMode)
            {
                var hackerBg     = new Color(0f, 0f, 0f, op);
                var hackerGreen  = new Color(0.00f, 0.90f, 0.20f, 1f);
                var hackerDimGreen = new Color(0.00f, 0.55f, 0.12f, 1f);
                var hackerPanel  = new Color(0f, 0f, 0f, op);
                var hackerRowA   = new Color(0.04f, 0.04f, 0.04f, op);
                var hackerRowB   = new Color(0.07f, 0.07f, 0.07f, op);

                //Override font to Cascadia for terminal feel
                if (_cascadiaFont != null)
                {
                    UILib.UIUtility.defaultFont = _cascadiaFont;
                    if (MaterialEditorWindow != null)
                        foreach (var txt in MaterialEditorWindow.GetComponentsInChildren<Text>(true))
                            txt.font = _cascadiaFont;
                }

                MaterialEditorMainPanel.color = hackerBg;
                var scrollBgImgH = MaterialEditorScrollableUI?.GetComponent<Image>();
                if (scrollBgImgH != null) scrollBgImgH.color = hackerBg;
                var cgH = MaterialEditorWindow?.GetComponent<CanvasGroup>();
                if (cgH != null) { cgH.alpha = 1f; cgH.interactable = true; cgH.blocksRaycasts = true; }

                RowColor         = hackerRowA;
                RowColorAlt      = hackerRowB;
                ItemColor        = new Color(0f, 0f, 0f, 0f);
                ItemColorChanged = new Color(0f, 0.9f, 0.2f, 0.10f);
                ItemTextColor    = hackerGreen;
                RendererColor    = new Color(0.02f, 0.06f, 0.02f, op);
                MaterialColor    = new Color(0.02f, 0.06f, 0.02f, op);
                CategoryColor    = new Color(0.02f, 0.04f, 0.06f, op);
                RendererSectionColor  = new Color(0f, 0.12f, 0.02f, op);
                MaterialSectionColor  = new Color(0f, 0.12f, 0.02f, op);
                RendererSectionAccent = hackerGreen;
                MaterialSectionAccent = hackerGreen;
                RendererSectionText   = hackerGreen;
                MaterialSectionText   = hackerGreen;

                if (MaterialEditorScrollableUI != null)
                {
                    var content = MaterialEditorScrollableUI.content;
                    if (content != null)
                    {
                        foreach (var panel in content.GetComponentsInChildren<Image>(true))
                        {
                            if (panel.gameObject.name == "RendererSectionPanel") panel.color = RendererSectionColor;
                            else if (panel.gameObject.name == "MaterialSectionPanel") panel.color = MaterialSectionColor;
                            else if (panel.gameObject.name == "RendererPanel") panel.color = RendererColor;
                            else if (panel.gameObject.name == "MaterialPanel") panel.color = MaterialColor;
                        }
                        foreach (var img in content.GetComponentsInChildren<Image>(true))
                        {
                            if (img.gameObject.name == "RendererSectionAccentBar") img.color = RendererSectionAccent;
                            else if (img.gameObject.name == "MaterialSectionAccentBar") img.color = MaterialSectionAccent;
                            else if (img.gameObject.name == "RendererAccentBar") img.color = RendererSectionAccent;
                            else if (img.gameObject.name == "MaterialAccentBar") img.color = MaterialSectionAccent;
                        }
                        foreach (var txt in content.GetComponentsInChildren<Text>(true))
                        {
                            if (txt.gameObject.name == "RendererSectionText" || txt.gameObject.name == "MaterialSectionText")
                                txt.color = hackerGreen;
                        }
                    }
                }

                var footerHackerColor = new Color(0f, 0f, 0f, op);
                if (FooterPanel != null) FooterPanel.color = footerHackerColor;
                DragPanel.color = footerHackerColor;

                foreach (var panel in new[] { DragPanel?.transform, FooterPanel?.transform })
                {
                    if (panel == null) continue;
                    foreach (var btn in panel.GetComponentsInChildren<Button>(true))
                    {
                        if (btn.name == "CloseButton") continue;
                        var img = btn.GetComponent<Image>();
                        if (img != null) img.color = new Color(0f, 0.15f, 0.03f, 1f);
                        var txt = btn.GetComponentInChildren<Text>();
                        if (txt != null) txt.color = hackerGreen;
                    }
                }

                var nameTextH = DragPanel.transform.Find("Nametext")?.GetComponent<Text>();
                if (nameTextH != null) { nameTextH.text = "HACKER MODE"; nameTextH.color = hackerGreen; }
                var x1H = DragPanel.transform.Find("CloseButton/x1")?.GetComponent<Image>();
                var x2H = DragPanel.transform.Find("CloseButton/x2")?.GetComponent<Image>();
                var closeBgH = DragPanel.transform.Find("CloseButton")?.GetComponent<Image>();
                if (closeBgH != null) closeBgH.color = new Color(0f, 0.15f, 0.03f, 1f);
                if (x1H != null) x1H.color = hackerGreen;
                if (x2H != null) x2H.color = hackerGreen;

                var scrollbarH = MaterialEditorScrollableUI?.verticalScrollbar?.GetComponent<UnityEngine.UI.Image>();
                if (scrollbarH != null) scrollbarH.color = new Color(0f, 0.12f, 0.03f, 0.6f); // dim green track
                var scrollbarHandleH = MaterialEditorScrollableUI?.verticalScrollbar?.handleRect?.GetComponent<UnityEngine.UI.Image>();
                if (scrollbarHandleH != null) scrollbarHandleH.color = hackerGreen;

                foreach (var bar in new[] { "StickyRendererBar", "StickyMaterialBar" })
                {
                    var barImg = MaterialEditorMainPanel?.transform.Find(bar)?.GetComponent<Image>();
                    if (barImg != null) barImg.color = new Color(0f, 0.12f, 0.02f, op);
                    var accentName = bar == "StickyRendererBar" ? "StickyRendererAccentBar" : "StickyMaterialAccentBar";
                    var accentImg = MaterialEditorMainPanel?.transform.Find($"{bar}/{accentName}")?.GetComponent<Image>();
                    if (accentImg != null) accentImg.color = hackerGreen;
                    var barTextName = bar == "StickyRendererBar" ? "StickyRendererBarText" : "StickyMaterialBarText";
                    var barTxt = MaterialEditorMainPanel?.transform.Find($"{bar}/{barTextName}")?.GetComponent<Text>();
                    if (barTxt != null) barTxt.color = hackerGreen;
                }

                if (UIInstance != null && UIInstance.CurrentGameObject != null)
                    UIInstance.RefreshUI();

                if (MaterialEditorScrollableUI != null)
                {
                    var labelNames = new System.Collections.Generic.HashSet<string>
                        { "OffsetXText", "OffsetYText", "ScaleXText", "ScaleYText",
                          "ColorRText", "ColorGText", "ColorBText", "ColorAText" };
                    foreach (var txt in MaterialEditorScrollableUI.content.GetComponentsInChildren<Text>(true))
                        if (labelNames.Contains(txt.gameObject.name))
                            txt.color = hackerGreen;
                    foreach (var btn in MaterialEditorScrollableUI.content.GetComponentsInChildren<Button>(true))
                    {
                        var btnImg = btn.GetComponent<Image>();
                        if (btnImg != null) btnImg.color = new Color(0f, 0.15f, 0.03f, 1f);
                        var btnTxt = btn.GetComponentInChildren<Text>();
                        if (btnTxt != null) btnTxt.color = hackerGreen;
                    }
                    foreach (var slider in MaterialEditorScrollableUI.content.GetComponentsInChildren<Slider>(true))
                        ItemTemplate.StyleSlider(slider);
                }
                //Side lists (Renderer/Material/Rename panels)
                foreach (var listPanel in new[] { MaterialEditorRendererList, MaterialEditorMaterialList, MaterialEditorRenameList })
                {
                    if (listPanel == null) continue;
                    listPanel.Panel.color = new Color(0f, 0.08f, 0.01f, 1f);
                    listPanel.TextColor = hackerGreen;
                    listPanel.RowColor = new Color(0.02f, 0.06f, 0.02f, 1f);
                    foreach (var t in listPanel.Panel.GetComponentsInChildren<Text>(true))
                        t.color = hackerGreen;
                    foreach (var img in listPanel.Panel.GetComponentsInChildren<Image>(true))
                        if (img.gameObject.name.EndsWith("Entry"))
                            img.color = new Color(0.02f, 0.06f, 0.02f, 1f);
                    foreach (var sb in listPanel.Panel.GetComponentsInChildren<Scrollbar>(true))
                    {
                        var sbTrack = sb.GetComponent<Image>();
                        if (sbTrack != null) sbTrack.color = new Color(0f, 0.12f, 0.03f, 0.6f);
                        var sbHandle = sb.handleRect?.GetComponent<Image>();
                        if (sbHandle != null) sbHandle.color = hackerGreen;
                    }
                }

                //Texture preview panel
                if (TexturePreviewPanel != null)
                {
                    TexturePreviewPanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 1f);
                    var prevHeader = TexturePreviewPanel.transform.Find("TexturePreviewHeader")?.GetComponent<Image>();
                    if (prevHeader != null) prevHeader.color = new Color(0f, 0.10f, 0.02f, 1f);
                    var prevHeaderTxt = TexturePreviewPanel.transform.Find("TexturePreviewHeader/TexturePreviewHeaderText")?.GetComponent<Text>();
                    if (prevHeaderTxt != null) prevHeaderTxt.color = hackerGreen;
                    var prevName = TexturePreviewPanel.transform.Find("TexturePreviewName")?.GetComponent<Text>();
                    if (prevName != null) prevName.color = hackerGreen;
                    foreach (var btn in TexturePreviewPanel.GetComponentsInChildren<Button>(true))
                    {
                        var btnImg = btn.GetComponent<Image>();
                        if (btnImg != null) btnImg.color = new Color(0f, 0.15f, 0.03f, 1f);
                        var btnTxt = btn.GetComponentInChildren<Text>();
                        if (btnTxt != null) btnTxt.color = hackerGreen;
                    }
                    var resizeImg = TexturePreviewPanel.transform.Find("ResizeHandle")?.GetComponent<Image>();
                    if (resizeImg != null) resizeImg.color = new Color(0f, 0.30f, 0.07f, 0.8f);
                }

                return;
            }

            //Restore font and title when hacker mode is off
            ApplyFontChoice();
            var nameTextRestore = DragPanel.transform.Find("Nametext")?.GetComponent<Text>();
            if (nameTextRestore != null && nameTextRestore.text == "HACKER MODE")
                nameTextRestore.text = "Material Editor";

            var bgColor        = dark ? new Color(0.13f, 0.13f, 0.15f, 1f) : new Color(0.96f, 0.96f, 0.97f, 1f);
            var headerColor    = dark ? new Color(0.10f, 0.10f, 0.12f, 1f) : new Color(0.88f, 0.88f, 0.90f, 1f);
            var textColor      = dark ? new Color(0.90f, 0.90f, 0.90f, 1f) : Color.black;
            var scrollbarColor = dark ? new Color(0.35f, 0.35f, 0.35f, 0.8f) : new Color(0.6f, 0.6f, 0.6f, 0.6f);

            //Bake opacity into row colours so rows fade with the window
            RowColor          = dark ? new Color(0.16f, 0.16f, 0.18f, op) : new Color(0.94f, 0.94f, 0.95f, op);
            RowColorAlt       = dark ? new Color(0.19f, 0.19f, 0.21f, op) : new Color(1.00f, 1.00f, 1.00f, op);
            ItemColor         = new Color(0f, 0f, 0f, 0f);
            ItemColorChanged  = dark ? new Color(1f, 1f, 1f, 0.06f) : new Color(0f, 0f, 0f, 0.08f);
            ItemTextColor     = textColor;
            RendererColor     = dark ? new Color(0.20f, 0.17f, 0.13f, op) : new Color(1.00f, 0.96f, 0.90f, op);
            MaterialColor     = dark ? new Color(0.13f, 0.18f, 0.14f, op) : new Color(0.93f, 0.98f, 0.93f, op);
            CategoryColor     = dark ? new Color(0.16f, 0.12f, 0.22f, op) : new Color(0.91f, 0.86f, 0.98f, op);

            RendererSectionColor  = dark ? new Color(0.22f, 0.14f, 0.04f, op) : new Color(0.98f, 0.88f, 0.70f, op);
            MaterialSectionColor  = dark ? new Color(0.06f, 0.15f, 0.07f, op) : new Color(0.78f, 0.93f, 0.80f, op);
            RendererSectionAccent = dark ? new Color(0.80f, 0.52f, 0.10f, 1f) : new Color(0.72f, 0.42f, 0.02f, 1f);
            MaterialSectionAccent = dark ? new Color(0.24f, 0.65f, 0.30f, 1f) : new Color(0.14f, 0.50f, 0.20f, 1f);
            RendererSectionText   = dark ? new Color(0.95f, 0.72f, 0.38f, 1f) : new Color(0.35f, 0.18f, 0.00f, 1f);
            MaterialSectionText   = dark ? new Color(0.48f, 0.88f, 0.54f, 1f) : new Color(0.04f, 0.24f, 0.06f, 1f);

            //Section bar colours
            if (MaterialEditorScrollableUI != null)
            {
                var content = MaterialEditorScrollableUI.content;
                if (content != null)
                {
                    foreach (var panel in content.GetComponentsInChildren<Image>(true))
                    {
                        if (panel.gameObject.name == "RendererSectionPanel") panel.color = RendererSectionColor;
                        else if (panel.gameObject.name == "MaterialSectionPanel") panel.color = MaterialSectionColor;
                        else if (panel.gameObject.name == "RendererPanel") panel.color = RendererColor;
                        else if (panel.gameObject.name == "MaterialPanel") panel.color = MaterialColor;
                    }
                    foreach (var img in content.GetComponentsInChildren<Image>(true))
                    {
                        if (img.gameObject.name == "RendererSectionAccentBar") img.color = RendererSectionAccent;
                        else if (img.gameObject.name == "MaterialSectionAccentBar") img.color = MaterialSectionAccent;
                        else if (img.gameObject.name == "RendererAccentBar") img.color = RendererSectionAccent;
                        else if (img.gameObject.name == "MaterialAccentBar") img.color = MaterialSectionAccent;
                    }
                    foreach (var txt in content.GetComponentsInChildren<Text>(true))
                    {
                        if (txt.gameObject.name == "RendererSectionText") txt.color = RendererSectionText;
                        else if (txt.gameObject.name == "MaterialSectionText") txt.color = MaterialSectionText;
                    }
                }
            }

            //Main panel background and scroll view background
            MaterialEditorMainPanel.color = new Color(bgColor.r, bgColor.g, bgColor.b, op);
            var scrollBgImg = MaterialEditorScrollableUI?.GetComponent<Image>();
            if (scrollBgImg != null) scrollBgImg.color = new Color(bgColor.r, bgColor.g, bgColor.b, op);

            //Drive opacity per background panel, not CanvasGroup alpha
            var cg = MaterialEditorWindow?.GetComponent<CanvasGroup>();
            if (cg != null) { cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true; }

            //Header text
            var nameText = DragPanel.transform.Find("Nametext")?.GetComponent<Text>();
            if (nameText != null) nameText.color = dark ? Color.white : Color.black;

            //Close button: bg matches header, X gets contrast colour
            var closeBtnImg = DragPanel.transform.Find("CloseButton")?.GetComponent<Image>();
            if (closeBtnImg != null) closeBtnImg.color = dark ? new Color(0.06f, 0.06f, 0.08f, op) : new Color(0.70f, 0.70f, 0.72f, op);
            var x1 = DragPanel.transform.Find("CloseButton/x1")?.GetComponent<Image>();
            var x2 = DragPanel.transform.Find("CloseButton/x2")?.GetComponent<Image>();
            var xColor = dark ? Color.white : Color.black;
            if (x1 != null) x1.color = xColor;
            if (x2 != null) x2.color = xColor;

            //Main window scrollbar: subtle track + darker pill handle
            var scrollbar = MaterialEditorScrollableUI?.verticalScrollbar?.GetComponent<UnityEngine.UI.Image>();
            if (scrollbar != null) scrollbar.color = dark ? new Color(0.25f, 0.25f, 0.28f, 0.5f) : new Color(0.70f, 0.70f, 0.72f, 0.4f);
            var scrollbarHandle = MaterialEditorScrollableUI?.verticalScrollbar?.handleRect?.GetComponent<UnityEngine.UI.Image>();
            if (scrollbarHandle != null) scrollbarHandle.color = dark ? new Color(0.84f, 0.84f, 0.87f, 1f) : new Color(0.40f, 0.40f, 0.45f, 0.9f);

            //Texture preview panel
            if (TexturePreviewPanel != null)
            {
                TexturePreviewPanel.GetComponent<Image>().color = bgColor;
                var previewHeader = TexturePreviewPanel.transform.Find("TexturePreviewHeader")?.GetComponent<Image>();
                if (previewHeader != null) previewHeader.color = headerColor;
                var previewHeaderTxt = TexturePreviewPanel.transform.Find("TexturePreviewHeader/TexturePreviewHeaderText")?.GetComponent<Text>();
                if (previewHeaderTxt != null) previewHeaderTxt.color = dark ? Color.white : Color.black;
                var previewName = TexturePreviewPanel.transform.Find("TexturePreviewName")?.GetComponent<Text>();
                if (previewName != null) previewName.color = textColor;
                //Restore buttons in case hacker mode left them green
                var previewBtnBg = dark ? new Color(0.18f, 0.18f, 0.22f, 1f) : new Color(0.84f, 0.84f, 0.87f, 1f);
                foreach (var btn in TexturePreviewPanel.GetComponentsInChildren<Button>(true))
                {
                    var btnImg = btn.GetComponent<Image>();
                    if (btnImg != null) btnImg.color = previewBtnBg;
                    var btnTxt = btn.GetComponentInChildren<Text>();
                    if (btnTxt != null) btnTxt.color = dark ? Color.white : Color.black;
                }
                var resizeImg = TexturePreviewPanel.transform.Find("ResizeHandle")?.GetComponent<Image>();
                if (resizeImg != null) resizeImg.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
            }

            //Sticky section bars
            var stickyRendBarImg = MaterialEditorMainPanel?.transform.Find("StickyRendererBar")?.GetComponent<Image>();
            if (stickyRendBarImg != null) stickyRendBarImg.color = RendererSectionColor;
            var stickyRendAccentImg = MaterialEditorMainPanel?.transform.Find("StickyRendererBar/StickyRendererAccentBar")?.GetComponent<Image>();
            if (stickyRendAccentImg != null) stickyRendAccentImg.color = RendererSectionAccent;
            var stickyRendTxt = MaterialEditorMainPanel?.transform.Find("StickyRendererBar/StickyRendererBarText")?.GetComponent<Text>();
            if (stickyRendTxt != null) stickyRendTxt.color = RendererSectionText;
            var stickyMatBarImg = MaterialEditorMainPanel?.transform.Find("StickyMaterialBar")?.GetComponent<Image>();
            if (stickyMatBarImg != null) stickyMatBarImg.color = MaterialSectionColor;
            var stickyMatAccentImg = MaterialEditorMainPanel?.transform.Find("StickyMaterialBar/StickyMaterialAccentBar")?.GetComponent<Image>();
            if (stickyMatAccentImg != null) stickyMatAccentImg.color = MaterialSectionAccent;
            var stickyMatTxt = MaterialEditorMainPanel?.transform.Find("StickyMaterialBar/StickyMaterialBarText")?.GetComponent<Text>();
            if (stickyMatTxt != null) stickyMatTxt.color = MaterialSectionText;

            //Footer darker than buttons so 2px gaps read as dividers
            var footerPanelColor = dark ? new Color(0.06f, 0.06f, 0.08f, op) : new Color(0.70f, 0.70f, 0.72f, op);
            if (FooterPanel != null) FooterPanel.color = footerPanelColor;
            //Header panel also uses the same gap-background trick
            DragPanel.color = footerPanelColor;

            //Header/footer button colours, gaps show between them
            var headerBtnBg   = dark ? new Color(0.18f, 0.18f, 0.22f, 1f) : new Color(0.84f, 0.84f, 0.87f, 1f);
            var headerBtnText = dark ? Color.white : Color.black;
            foreach (var panel in new[] { DragPanel?.transform, FooterPanel?.transform })
            {
                if (panel == null) continue;
                foreach (var btn in panel.GetComponentsInChildren<Button>(true))
                {
                    var txt = btn.GetComponentInChildren<Text>();
                    //Skip the close button (no text), found by name
                    if (btn.name == "CloseButton") continue;
                    var img = btn.GetComponent<Image>();
                    if (img != null) img.color = headerBtnBg;
                    if (txt != null) txt.color = headerBtnText;
                }
            }

            //Repaint all visible rows so they pick up the new colours immediately
            if (UIInstance != null && UIInstance.CurrentGameObject != null)
                UIInstance.RefreshUI();
            //Recolour static labels after RefreshUI so rows exist
            //Only targets label Text elements, not button texts or section header texts
            if (MaterialEditorScrollableUI != null)
            {
                var labelNames = new System.Collections.Generic.HashSet<string>
                    { "OffsetXText", "OffsetYText", "ScaleXText", "ScaleYText",
                      "ColorRText", "ColorGText", "ColorBText", "ColorAText" };
                foreach (var txt in MaterialEditorScrollableUI.content.GetComponentsInChildren<Text>(true))
                    if (labelNames.Contains(txt.gameObject.name))
                        txt.color = textColor;
                //Update all content button colours
                var btnBgColor = dark ? new Color(0.25f, 0.25f, 0.28f, 1f) : new Color(0.85f, 0.85f, 0.85f, 1f);
                foreach (var btn in MaterialEditorScrollableUI.content.GetComponentsInChildren<Button>(true))
                {
                    var btnImg = btn.GetComponent<Image>();
                    if (btnImg != null) btnImg.color = btnBgColor;
                    var btnTxt = btn.GetComponentInChildren<Text>();
                    if (btnTxt != null) btnTxt.color = textColor;
                }
                //Restyle sliders for current theme
                foreach (var slider in MaterialEditorScrollableUI.content.GetComponentsInChildren<Slider>(true))
                    ItemTemplate.StyleSlider(slider);
            }

            // Theme the SelectList side panels
            var selectPanelColor = dark ? new Color(0.20f, 0.20f, 0.20f, 1f) : new Color(0.76f, 0.76f, 0.76f, 1f);
            var selectPanelTextColor = dark ? new Color(0.90f, 0.90f, 0.90f, 1f) : Color.black;
            var selectPanelRowColor = dark ? new Color(0.30f, 0.30f, 0.30f, 1f) : new Color(1f, 1f, 1f, 0.6f);
            var selectSbTrack  = dark ? new Color(0.25f, 0.25f, 0.28f, 0.5f) : new Color(0.70f, 0.70f, 0.72f, 0.4f);
            var selectSbHandle = dark ? new Color(0.84f, 0.84f, 0.87f, 1f)   : new Color(0.40f, 0.40f, 0.45f, 0.9f);
            foreach (var panel in new[] { MaterialEditorRendererList, MaterialEditorMaterialList, MaterialEditorRenameList })
            {
                if (panel == null) continue;
                panel.Panel.color = selectPanelColor;
                panel.TextColor = selectPanelTextColor;
                panel.RowColor = selectPanelRowColor;
                foreach (var t in panel.Panel.GetComponentsInChildren<Text>(true))
                    t.color = selectPanelTextColor;
                foreach (var img in panel.Panel.GetComponentsInChildren<Image>(true))
                    if (img.gameObject.name.EndsWith("Entry"))
                        img.color = selectPanelRowColor;
                // Restyle scrollbar to match main ME scrollbar
                foreach (var sb in panel.Panel.GetComponentsInChildren<Scrollbar>(true))
                {
                    var sbTrack = sb.GetComponent<Image>();
                    if (sbTrack != null) sbTrack.color = selectSbTrack;
                    var sbHandle = sb.handleRect?.GetComponent<Image>();
                    if (sbHandle != null) sbHandle.color = selectSbHandle;
                }
            }
        }

        private static string _previewedMaterialName = null;
        private static bool _previewInitialized = false;
        private static bool _previewDocked = false;
        private static Vector3 _meDragLastPos;

        /// <summary>
        /// Toggle the texture preview for a material row. Right-click same to close, different to switch.
        /// </summary>
        public static void ToggleMaterialPreview(string materialName, Texture texture)
        {
            if (!TexturePreviewVisible || _previewedMaterialName != materialName)
            {
                if (!TexturePreviewVisible)
                    ToggleTexturePreviewPanel();
                _previewedMaterialName = materialName;
                SetTexturePreview(texture, materialName);
            }
            else
            {
                ToggleTexturePreviewPanel();
                _previewedMaterialName = null;
            }
        }

        private static void ToggleTexturePreviewPanel()
        {
            TexturePreviewVisible = !TexturePreviewVisible;
            TexturePreviewPanel.SetActive(TexturePreviewVisible);
            if (!TexturePreviewVisible)
            {
                TexturePreviewImage.texture = null;
                TexturePreviewNameText.text = "";
                return;
            }
            //Position only on the first open; afterwards keep the last position
            if (!_previewInitialized)
            {
                UpdateTexturePreviewPanelPosition();
                _previewInitialized = true;
            }
        }

        /// <summary>Snap the preview panel to the right of the ME window.</summary>
        internal static void UpdateTexturePreviewPanelPosition()
        {
            if (TexturePreviewPanel == null) return;
            var mainRT = MaterialEditorMainPanel.GetComponent<RectTransform>();
            var previewRT = TexturePreviewPanel.GetComponent<RectTransform>();
            previewRT.anchorMin = new Vector2(0f, 0f);
            previewRT.anchorMax = new Vector2(0f, 0f);
            previewRT.pivot = new Vector2(0f, 1f);
            var corners = new Vector3[4];
            mainRT.GetWorldCorners(corners);
            var canvasRT = MaterialEditorWindow.GetComponent<RectTransform>();
            Vector2 topRight = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, topRight, null, out Vector2 localTopRight);
            previewRT.anchoredPosition = new Vector2(localTopRight.x + canvasRT.rect.width / 2f + MarginSize, localTopRight.y + canvasRT.rect.height / 2f);
        }

        private protected IMaterialEditorColorPalette ColorPalette;

        internal GameObject CurrentGameObject;
        internal object CurrentData;
        private static string CurrentFilter = "";
        private bool DoObjExport = false;
        private Renderer ObjRenderer;

        internal static SelectedInterpolable selectedInterpolable;
        internal static SelectedProjectorInterpolable selectedProjectorInterpolable;

        /// <summary>
        /// Initialize the MaterialEditor UI
        /// </summary>
        protected void InitUI()
        {
            UIInstance = this;
            Padding = new RectOffset(1, 1, 1, 1);
            SubRowPadding = new RectOffset(18, 1, 1, 1);
            //Store original font before LoadFonts potentially changes it
            _arialFont = UILib.UIUtility.defaultFont;
            LoadFonts();
            //Replace UILib rounded sprites with flat 1x1 sprite for squared look
            var origBackground = UILib.UIUtility.backgroundSprite;
            var origStandard   = UILib.UIUtility.standardSprite;
            UILib.UIUtility.backgroundSprite     = FlatSprite;
            UILib.UIUtility.standardSprite       = FlatSprite;
            UILib.UIUtility.resources.background = FlatSprite;
            UILib.UIUtility.resources.standard   = FlatSprite;
            //Set theme colours before creating the template so static labels pick up the correct colour
            ItemTextColor = MaterialEditorPluginBase.DarkMode.Value ? new Color(0.90f, 0.90f, 0.90f, 1f) : Color.black;

            MaterialEditorWindow = UIUtility.CreateNewUISystem("MaterialEditorCanvas");
            MaterialEditorWindow.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f / UIScale.Value, 1080f / UIScale.Value);
            Visible = false;
            MaterialEditorWindow.gameObject.transform.SetParent(transform);
            MaterialEditorWindow.sortingOrder = 1000;

            MaterialEditorMainPanel = UIUtility.CreatePanel("Panel", MaterialEditorWindow.transform);
            MaterialEditorMainPanel.color = Color.white;
            MaterialEditorMainPanel.transform.SetRect(0.15f, 0.05f, UIWidth.Value * UIScale.Value, UIHeight.Value * UIScale.Value);

            TooltipManager.Init(MaterialEditorWindow.transform);

            UIUtility.AddOutlineToObject(MaterialEditorMainPanel.transform, Color.black);

            DragPanel = UIUtility.CreatePanel("Draggable", MaterialEditorMainPanel.transform);
            DragPanel.transform.SetRect(0f, 1f, 1f, 1f, 0f, -HeaderSize);
            DragPanel.color = Color.gray;
            UIUtility.MakeObjectDraggable(DragPanel.rectTransform, MaterialEditorMainPanel.rectTransform, PreventDragout.Value);
            //When the ME window is dragged and preview is docked, keep preview snapped
            var meDragTrigger = DragPanel.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            var meDownEntry = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown };
            meDownEntry.callback.AddListener(_ => { _meDragLastPos = MaterialEditorMainPanel.transform.position; });
            meDragTrigger.triggers.Add(meDownEntry);
            //When docked the preview follows the ME window by the same delta, keeping its relative position
            var meDragEntry = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.Drag };
            meDragEntry.callback.AddListener(_ =>
            {
                if (!_previewDocked || !TexturePreviewVisible) return;
                var mePos = MaterialEditorMainPanel.transform.position;
                TexturePreviewPanel.transform.position += mePos - _meDragLastPos;
                _meDragLastPos = mePos;
            });
            meDragTrigger.triggers.Add(meDragEntry);

            var nametext = UIUtility.CreateText("Nametext", DragPanel.transform, "Material Editor");
            nametext.transform.SetRect();
            nametext.alignment = TextAnchor.MiddleCenter;

            FilterInputField = UIUtility.CreateInputField("Filter", DragPanel.transform, "Filter");
            FilterInputField.text = CurrentFilter;
            FilterInputField.transform.SetRect(0f, 0f, 0f, 1f, 1f, 1f, 100f, -1f);
            FilterInputField.onValueChanged.AddListener(RefreshUI);
            TooltipManager.AddTooltip(FilterInputField.gameObject, @"Filter visible items in the window.

- Searches for renderers, materials and projectors
- Searches starting with '_' will search for material properties
- Combine multiple statements using a comma (an entry just has to match any of the search terms)
- Use a '*' as a wildcard for any amount of characters (e.g. ""_pattern*1"" will find the ""PatternMask1"" property)
- Use a '?' as a wildcard for a single character");

            var persistBtn = UIUtility.CreateButton("PersistSearchButton", DragPanel.transform, PersistFilter.Value ? "[P]" : "P");
            persistBtn.transform.SetRect(0f, 0f, 0f, 1f, 101f, 1f, 120f, -1f);
            TooltipManager.AddTooltip(persistBtn.gameObject, "Persist filter between objects");
            persistBtn.onClick.AddListener(() =>
            {
                PersistFilter.Value = !PersistFilter.Value;
                persistBtn.GetComponentInChildren<Text>().text = PersistFilter.Value ? "[P]" : "P";
            });

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

            ViewListButton = UIUtility.CreateButton("ViewListButton", DragPanel.transform, ">");
            ViewListButton.transform.SetRect(1f, 0f, 1f, 1f, -40f, 1f, -21f, -1f);
            ViewListButton.onClick.AddListener(() =>
            {
                if (RenameListVisible)
                {
                    MaterialEditorRenameList.ToggleVisibility(false);
                    ViewListButton.GetComponentInChildren<Text>().text = ">";
                    RenameListVisible = false;
                }
                else
                {
                    MaterialEditorRendererList.ToggleVisibility(!ListsVisible);
                    MaterialEditorMaterialList.ToggleVisibility(!ListsVisible);
                    ListsVisible = !ListsVisible;
                    if (ListsVisible)
                        ViewListButton.GetComponentInChildren<Text>().text = "<";
                    else
                        ViewListButton.GetComponentInChildren<Text>().text = ">";
                }
            });

            UpdateHeaderButtons();

            MaterialEditorScrollableUI = UIUtility.CreateScrollView("MaterialEditorWindow", MaterialEditorMainPanel.transform);
            MaterialEditorScrollableUI.transform.SetRect(0f, 0f, 1f, 1f, MarginSize, MarginSize + HeaderSize, -MarginSize, -HeaderSize - MarginSize / 2f);
            MaterialEditorScrollableUI.gameObject.AddComponent<Mask>();
            MaterialEditorScrollableUI.content.gameObject.AddComponent<VerticalLayoutGroup>();
            MaterialEditorScrollableUI.content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            MaterialEditorScrollableUI.verticalScrollbar.GetComponent<RectTransform>().offsetMin = new Vector2(ScrollOffsetX, 0f);
            MaterialEditorScrollableUI.viewport.offsetMax = new Vector2(ScrollOffsetX, 0f);
            MaterialEditorScrollableUI.movementType = ScrollRect.MovementType.Clamped;
            // Scrollbar: semi-transparent track, pill handle inset 2px each side
            MaterialEditorScrollableUI.verticalScrollbar.GetComponent<Image>().color = new Color(0.70f, 0.70f, 0.72f, 0.4f);
            //Match scroll view background to main panel colour (updated by ApplyTheme on theme change)
            var scrollBg = MaterialEditorScrollableUI.GetComponent<Image>();
            if (scrollBg != null) scrollBg.color = new Color(0.96f, 0.96f, 0.97f, 1f);

            var template = ItemTemplate.CreateTemplate(MaterialEditorScrollableUI.content.transform);

            VirtualList = MaterialEditorScrollableUI.gameObject.AddComponent<VirtualList>();
            VirtualList.ScrollRect = MaterialEditorScrollableUI;
            VirtualList.EntryTemplate = template;
            VirtualList.Initialize();


            //Sticky section bars overlay the top of the scroll area and float in place as the user scrolls
            var stickyComp = MaterialEditorScrollableUI.gameObject.AddComponent<StickySectionBar>();
            stickyComp.ScrollRect = MaterialEditorScrollableUI;

            // Renderer sticky bar
            var stickyRendBar = UIUtility.CreatePanel("StickyRendererBar", MaterialEditorMainPanel.transform);
            stickyRendBar.color = RendererSectionColor;
            stickyRendBar.transform.SetRect(0f, 1f, 1f, 1f,
                MarginSize, -(HeaderSize + MarginSize / 2f + PanelHeight), ScrollOffsetX - MarginSize, -(HeaderSize + MarginSize / 2f));
            UIUtility.AddOutlineToObject(stickyRendBar.transform, new Color(0f, 0f, 0f, 0.15f));
            var stickyRendAccent = UIUtility.CreatePanel("StickyRendererAccentBar", stickyRendBar.transform);
            stickyRendAccent.color = RendererSectionAccent;
            stickyRendAccent.transform.SetRect(0f, 0f, 0f, 1f, 0f, 0f, 3f, 0f);
            var stickyRendCollapse = UIUtility.CreateButton("StickyRendererCollapseButton", stickyRendBar.transform, "-");
            stickyRendCollapse.transform.SetRect(0f, 0f, 0f, 1f, 7f, 1f, 7f + SmallButtonWidth, -1f);
            stickyRendCollapse.onClick.AddListener(() =>
            {
                RendererSectionCollapsed = !RendererSectionCollapsed;
                UIInstance.RefreshUI();
            });
            var stickyRendText = UIUtility.CreateText("StickyRendererBarText", stickyRendBar.transform, "Renderers");
            stickyRendText.transform.SetRect(0f, 0f, 1f, 1f, 7f + SmallButtonWidth + 4f, 0f, 0f, 0f);
            stickyRendText.alignment = TextAnchor.MiddleLeft;
            stickyRendText.fontStyle = FontStyle.Bold;
            stickyRendText.fontSize = 14;
            stickyRendText.color = RendererSectionText;
            stickyRendBar.gameObject.SetActive(false);
            stickyComp.RendererBar = stickyRendBar;
            stickyComp.RendererBarText = stickyRendText;
            stickyComp.RendererBarCollapseButton = stickyRendCollapse;

            // Material sticky bar
            var stickyMatBar = UIUtility.CreatePanel("StickyMaterialBar", MaterialEditorMainPanel.transform);
            stickyMatBar.color = MaterialSectionColor;
            stickyMatBar.transform.SetRect(0f, 1f, 1f, 1f,
                MarginSize, -(HeaderSize + MarginSize / 2f + PanelHeight), ScrollOffsetX - MarginSize, -(HeaderSize + MarginSize / 2f));
            UIUtility.AddOutlineToObject(stickyMatBar.transform, new Color(0f, 0f, 0f, 0.15f));
            var stickyMatAccent = UIUtility.CreatePanel("StickyMaterialAccentBar", stickyMatBar.transform);
            stickyMatAccent.color = MaterialSectionAccent;
            stickyMatAccent.transform.SetRect(0f, 0f, 0f, 1f, 0f, 0f, 3f, 0f);
            var stickyMatCollapse = UIUtility.CreateButton("StickyMaterialCollapseButton", stickyMatBar.transform, "-");
            stickyMatCollapse.transform.SetRect(0f, 0f, 0f, 1f, 7f, 1f, 7f + SmallButtonWidth, -1f);
            stickyMatCollapse.onClick.AddListener(() =>
            {
                MaterialSectionCollapsed = !MaterialSectionCollapsed;
                UIInstance.RefreshUI();
            });
            var stickyMatText = UIUtility.CreateText("StickyMaterialBarText", stickyMatBar.transform, "Materials");
            stickyMatText.transform.SetRect(0f, 0f, 1f, 1f, 7f + SmallButtonWidth + 4f, 0f, 0f, 0f);
            stickyMatText.alignment = TextAnchor.MiddleLeft;
            stickyMatText.fontStyle = FontStyle.Bold;
            stickyMatText.fontSize = 14;
            stickyMatText.color = MaterialSectionText;
            stickyMatBar.gameObject.SetActive(false);
            stickyComp.MaterialBar = stickyMatBar;
            stickyComp.MaterialBarText = stickyMatText;
            stickyComp.MaterialBarCollapseButton = stickyMatCollapse;

            // Footer panel with buttons
            FooterPanel = UIUtility.CreatePanel("FooterPanel", MaterialEditorMainPanel.transform);
            FooterPanel.transform.SetRect(0f, 0f, 1f, 0f, 0f, MarginSize / 2f, 0f, MarginSize / 2f + HeaderSize);
            FooterPanel.color = Color.gray;

            //Footer order: Renderers-, R, Materials-, M, collapse-all, T, P
            //1px gap between buttons achieved by inset right edge by 1px

            //Footer: Renderers section toggle
            var footerRendererBtn = UIUtility.CreateButton("FooterRendererSectionButton", FooterPanel.transform, "Renderers -");
            footerRendererBtn.transform.SetRect(0f, 0f, 0f, 1f, 1f, 1f, 80f, -1f);
            TooltipManager.AddTooltip(footerRendererBtn.gameObject, "Collapse / expand the Renderers section");
            footerRendererBtn.onClick.AddListener(() =>
            {
                RendererSectionCollapsed = !RendererSectionCollapsed;
                UIInstance.RefreshUI();
            });
            FooterRendererSectionButton = footerRendererBtn;

            //Footer: Collapse renderers (R)
            var footerCollapseRBtn = UIUtility.CreateButton("FooterCollapseRenderersButton", FooterPanel.transform, "R");
            footerCollapseRBtn.transform.SetRect(0f, 0f, 0f, 1f, 82f, 1f, 101f, -1f);
            TooltipManager.AddTooltip(footerCollapseRBtn.gameObject, "Collapse / expand all renderer sub-rows");
            footerCollapseRBtn.onClick.AddListener(() =>
            {
                if (CurrentGameObject == null) return;
                if (CollapsedRenderers.Count == 0)
                    foreach (var rend in GetRendererList(CurrentGameObject))
                        CollapsedRenderers.Add(rend.GetInstanceID());
                else
                    CollapsedRenderers.Clear();
                UIInstance.RefreshUI();
            });
            CollapseRenderersButton = footerCollapseRBtn;

            //Footer: Materials section toggle
            var footerMaterialBtn = UIUtility.CreateButton("FooterMaterialSectionButton", FooterPanel.transform, "Materials -");
            footerMaterialBtn.transform.SetRect(0f, 0f, 0f, 1f, 103f, 1f, 182f, -1f);
            TooltipManager.AddTooltip(footerMaterialBtn.gameObject, "Collapse / expand the Materials section");
            footerMaterialBtn.onClick.AddListener(() =>
            {
                MaterialSectionCollapsed = !MaterialSectionCollapsed;
                UIInstance.RefreshUI();
            });
            FooterMaterialSectionButton = footerMaterialBtn;

            //Footer: Collapse materials (M)
            var footerCollapseMBtn = UIUtility.CreateButton("FooterCollapseMaterialsButton", FooterPanel.transform, "M");
            footerCollapseMBtn.transform.SetRect(0f, 0f, 0f, 1f, 184f, 1f, 203f, -1f);
            TooltipManager.AddTooltip(footerCollapseMBtn.gameObject, "Collapse / expand all material sub-rows");
            footerCollapseMBtn.onClick.AddListener(() =>
            {
                if (CurrentGameObject == null) return;
                if (CollapsedMaterials.Count == 0)
                    foreach (var rend in GetRendererList(CurrentGameObject))
                        foreach (var mat in GetMaterials(CurrentGameObject, rend))
                            CollapsedMaterials.Add(mat.NameFormatted());
                else
                    CollapsedMaterials.Clear();
                UIInstance.RefreshUI();
            });
            CollapseMaterialsButton = footerCollapseMBtn;

            //Footer: Collapse all
            var footerCollapseAllBtn = UIUtility.CreateButton("FooterCollapseAllButton", FooterPanel.transform, "--");
            footerCollapseAllBtn.transform.SetRect(0f, 0f, 0f, 1f, 205f, 1f, 224f, -1f);
            TooltipManager.AddTooltip(footerCollapseAllBtn.gameObject, "Collapse all / expand all");
            footerCollapseAllBtn.onClick.AddListener(() =>
            {
                if (CurrentGameObject == null) return;
                bool anyExpanded = CollapsedRenderers.Count == 0 && CollapsedMaterials.Count == 0 && !RendererSectionCollapsed && !MaterialSectionCollapsed;
                if (anyExpanded)
                {
                    foreach (var rend in GetRendererList(CurrentGameObject))
                    {
                        CollapsedRenderers.Add(rend.GetInstanceID());
                        foreach (var mat in GetMaterials(CurrentGameObject, rend))
                            CollapsedMaterials.Add(mat.NameFormatted());
                    }
                    RendererSectionCollapsed = true;
                    MaterialSectionCollapsed = true;
                }
                else
                {
                    CollapsedRenderers.Clear();
                    CollapsedMaterials.Clear();
                    RendererSectionCollapsed = false;
                    MaterialSectionCollapsed = false;
                }
                UIInstance.RefreshUI();
            });

            //Footer: Texture preview toggle (T)
            var footerTexPreviewBtn = UIUtility.CreateButton("FooterTexPreviewButton", FooterPanel.transform, "T");
            footerTexPreviewBtn.transform.SetRect(0f, 0f, 0f, 1f, 226f, 1f, 245f, -1f);
            TooltipManager.AddTooltip(footerTexPreviewBtn.gameObject, "Toggle texture preview panel");
            footerTexPreviewBtn.onClick.AddListener(() => ToggleTexturePreviewPanel());

            //Footer: Hacker mode toggle
            var footerHackerBtn = UIUtility.CreateButton("FooterHackerButton", FooterPanel.transform, ">_");
            footerHackerBtn.transform.SetRect(1f, 0f, 1f, 1f, -24f, 1f, -1f, -1f);
            TooltipManager.AddTooltip(footerHackerBtn.gameObject, "Toggle hacker terminal mode");
            footerHackerBtn.onClick.AddListener(() =>
            {
                HackerMode = !HackerMode;
                footerHackerBtn.GetComponentInChildren<Text>().text = HackerMode ? "[>_]" : ">_";
                ApplyTheme();
            });

            UpdateHeaderButtons();

            MaterialEditorRendererList = new SelectListPanel(MaterialEditorMainPanel.transform, "RendererList", "Renderers");
            MaterialEditorRendererList.Panel.transform.SetRect(1f, 0.5f, 1f, 1f, MarginSize, MarginSize / 2f, MarginSize + UIListWidth.Value);
            MaterialEditorRendererList.ToggleVisibility(false);
            MaterialEditorMaterialList = new SelectListPanel(MaterialEditorMainPanel.transform, "MaterialList", "Materials");
            MaterialEditorMaterialList.Panel.transform.SetRect(1f, 0f, 1f, 0.5f, MarginSize, 0f, MarginSize + UIListWidth.Value, -MarginSize);
            MaterialEditorMaterialList.ToggleVisibility(false);

            MaterialEditorRenameList = new SelectListPanel(MaterialEditorMainPanel.transform, "MaterialRenameList", "Mat. Renderers");
            MaterialEditorRenameList.Panel.transform.SetRect(1f, 0.5f, 1f, 1f, MarginSize, MarginSize / 2f, MarginSize + UIListWidth.Value);
            MaterialEditorRenameList.ToggleVisibility(false);
            MaterialEditorRenameField = UIUtility.CreateInputField("MaterialEditorRenameField", MaterialEditorRenameList.Panel.transform, "");
            MaterialEditorRenameField.transform.SetRect(0, 0, 1, 0, 0, -(PanelHeight + MarginSize / 2), 0, -(MarginSize / 2));
            MaterialEditorRenameButton = UIUtility.CreateButton("MaterialEditorRenameButton", MaterialEditorRenameList.Panel.transform, "Rename");
            MaterialEditorRenameButton.transform.SetRect(0, 0, 1, 0, 0, -(2 * PanelHeight + MarginSize), 0, -(PanelHeight + MarginSize));
            MaterialEditorRenameList.Panel.transform.GetChild(0).SetRect(0, 1, 0.4f, 1, 5, -40, -2, -27.5f);
            MaterialEditorRenameList.Panel.transform.GetChild(1).SetRect(0.4f, 1, 1, 1, 2, -42.5f, -2, -25);
            MaterialEditorRenameList.Panel.transform.GetChild(2).SetRect(0, 0, 1, 1, 2, 2, -2, -42.5f);
            MaterialEditorRenameMaterial = Instantiate(MaterialEditorRenameList.Panel.transform.GetChild(0), MaterialEditorRenameList.Panel.transform).GetComponent<Text>();
            MaterialEditorRenameMaterial.gameObject.name = nameof(MaterialEditorRenameMaterial);
            MaterialEditorRenameMaterial.transform.SetRect(0, 1, 1, 1, 5, -20, -2, -5);

            // Texture preview side panel - parented to canvas, positioned to the right of the main panel
            var previewPanelGO = new GameObject("TexturePreviewPanel");
            previewPanelGO.transform.SetParent(MaterialEditorWindow.transform, false);
            var previewPanelRT = previewPanelGO.AddComponent<RectTransform>();
            previewPanelRT.anchorMin = new Vector2(0f, 0f);
            previewPanelRT.anchorMax = new Vector2(0f, 0f);
            previewPanelRT.pivot = new Vector2(0f, 1f);
            // Default size: half ME window width x half ME window height
            float previewSize = (UIWidth.Value - 0.05f) * 1920f / UIScale.Value * 0.5f;
            previewPanelRT.sizeDelta = new Vector2(previewSize, previewSize);
            var previewPanelBG = previewPanelGO.AddComponent<Image>();
            previewPanelBG.color = Color.white;
            UIUtility.AddOutlineToObject(previewPanelRT, Color.black);

            // Header bar
            var previewHeader = UIUtility.CreatePanel("TexturePreviewHeader", previewPanelGO.transform);
            previewHeader.color = Color.gray;
            previewHeader.transform.SetRect(0f, 1f, 1f, 1f, 0f, -HeaderSize);
            var previewHeaderText = UIUtility.CreateText("TexturePreviewHeaderText", previewHeader.transform, "Texture Preview");
            previewHeaderText.transform.SetRect();
            previewHeaderText.alignment = TextAnchor.MiddleCenter;
            UIUtility.MakeObjectDraggable(previewHeader.rectTransform, previewPanelRT, false);

            //Lock button, top-left, toggles docking
            var previewLockButton = UIUtility.CreateButton("TexturePreviewLockButton", previewHeader.transform, "\u25a1");
            previewLockButton.transform.SetRect(0f, 0f, 0f, 1f, 0f, 0f, HeaderSize, 0f);
            TooltipManager.AddTooltip(previewLockButton.gameObject, "Lock/unlock: when locked the preview panel moves with the ME window");
            previewLockButton.onClick.AddListener(() =>
            {
                _previewDocked = !_previewDocked;
                previewLockButton.GetComponentInChildren<Text>().text = _previewDocked ? "\u25a0" : "\u25a1";
                //Anchor at the current relative position instead of snapping to default
                if (_previewDocked) _meDragLastPos = MaterialEditorMainPanel.transform.position;
            });

            //Close button, upper-right of preview header
            var previewCloseButton = UIUtility.CreateButton("TexturePreviewCloseButton", previewHeader.transform, "X");
            previewCloseButton.transform.SetRect(1f, 0f, 1f, 1f, -HeaderSize, 0f, 0f, 0f);
            TooltipManager.AddTooltip(previewCloseButton.gameObject, "Close preview");
            previewCloseButton.onClick.AddListener(() =>
            {
                if (TexturePreviewVisible)
                {
                    ToggleTexturePreviewPanel();
                    _previewedMaterialName = null;
                }
            });

            // Texture image container - fixed area between header and name label
            var imageContainerGO = new GameObject("TexturePreviewImageContainer");
            imageContainerGO.transform.SetParent(previewPanelGO.transform, false);
            var imageContainerRT = imageContainerGO.AddComponent<RectTransform>();
            imageContainerRT.anchorMin = new Vector2(0f, 0f);
            imageContainerRT.anchorMax = new Vector2(1f, 1f);
            imageContainerRT.offsetMin = new Vector2(MarginSize, HeaderSize + MarginSize * 2f + PanelHeight);
            imageContainerRT.offsetMax = new Vector2(-MarginSize, -HeaderSize - MarginSize);
            imageContainerGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);

            // Texture image - sized by AspectRatioFitter inside the container
            var imageGO = new GameObject("TexturePreviewImage");
            imageGO.transform.SetParent(imageContainerGO.transform, false);
            var imageRT = imageGO.AddComponent<RectTransform>();
            imageRT.anchorMin = new Vector2(0.5f, 0.5f);
            imageRT.anchorMax = new Vector2(0.5f, 0.5f);
            imageRT.pivot = new Vector2(0.5f, 0.5f);
            imageRT.anchoredPosition = Vector2.zero;
            imageRT.sizeDelta = Vector2.zero;
            TexturePreviewImage = imageGO.AddComponent<RawImage>();
            var imageARF = imageGO.AddComponent<AspectRatioFitter>();
            imageARF.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            imageARF.aspectRatio = 1f;

            // Texture name label
            TexturePreviewNameText = UIUtility.CreateText("TexturePreviewName", previewPanelGO.transform, "");
            TexturePreviewNameText.alignment = TextAnchor.MiddleCenter;
            TexturePreviewNameText.color = Color.black;
            TexturePreviewNameText.transform.SetRect(0f, 0f, 1f, 0f, MarginSize, MarginSize, -MarginSize, MarginSize + PanelHeight);

            TexturePreviewPanel = previewPanelGO;
            TexturePreviewPanel.SetActive(false);
            UpdateTexturePreviewPanelPosition();

            //Store defaults for reset
            TexturePreviewDefaultSize = previewPanelRT.sizeDelta;
            TexturePreviewDefaultPosition = previewPanelRT.anchoredPosition;

            //Reset button, lower-left corner of the panel
            var previewResetButton = UIUtility.CreateButton("TexturePreviewResetButton", previewPanelGO.transform, "R");
            previewResetButton.transform.SetRect(0f, 0f, 0f, 0f, 0f, 0f, HeaderSize, HeaderSize);
            TooltipManager.AddTooltip(previewResetButton.gameObject, "Reset position and size");
            previewResetButton.onClick.AddListener(() =>
            {
                previewPanelRT.sizeDelta = TexturePreviewDefaultSize;
                _previewDocked = false;
                var lockBtn = previewHeader.transform.Find("TexturePreviewLockButton");
                if (lockBtn != null) lockBtn.GetComponentInChildren<Text>().text = "\u25a1";
                UpdateTexturePreviewPanelPosition();
            });

            //Resize handle, bottom-right corner
            var resizeHandleGO = new GameObject("ResizeHandle");
            resizeHandleGO.transform.SetParent(previewPanelGO.transform, false);
            var resizeHandleRT = resizeHandleGO.AddComponent<RectTransform>();
            resizeHandleRT.anchorMin = new Vector2(1f, 0f);
            resizeHandleRT.anchorMax = new Vector2(1f, 0f);
            resizeHandleRT.pivot = new Vector2(1f, 0f);
            resizeHandleRT.sizeDelta = new Vector2(16f, 16f);
            resizeHandleRT.anchoredPosition = Vector2.zero;
            var resizeHandleImg = resizeHandleGO.AddComponent<UnityEngine.UI.Image>();
            resizeHandleImg.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
            var resizeHandle = resizeHandleGO.AddComponent<ResizeHandle>();
            resizeHandle.TargetRT = previewPanelRT;
            resizeHandle.MinSize = new Vector2(100f, 100f);
            TooltipManager.AddTooltip(resizeHandleGO, "Drag to resize");

            //Restore UILib sprites so other plugins aren't affected
            UILib.UIUtility.backgroundSprite     = origBackground;
            UILib.UIUtility.standardSprite       = origStandard;
            UILib.UIUtility.resources.background = origBackground;
            UILib.UIUtility.resources.standard   = origStandard;

            ApplyTheme();
        }

        /// <summary>
        /// Refresh the MaterialEditor UI
        /// </summary>
        public void RefreshUI() => RefreshUI(CurrentFilter);
        /// <summary>
        /// Refresh the MaterialEditor UI using the specified filter text
        /// </summary>
        public void RefreshUI(string filterText) => PopulateList(CurrentGameObject, CurrentData, filterText);

        private static void SetMainRectWithMemory(float anchorLeft, float anchorBottom, float anchorRight, float anchorTop)
        {
            Vector3 positionMemory = MaterialEditorMainPanel.transform.position;
            MaterialEditorMainPanel.transform.SetRect(anchorLeft, anchorBottom, anchorRight, anchorTop);
            if (!Input.GetKey(KeyCode.LeftControl))
                MaterialEditorMainPanel.transform.position = positionMemory;
        }

        /// <summary>
        /// Get or set the MaterialEditor UI visibility
        /// </summary>
        public static bool Visible
        {
            get
            {
                if (MaterialEditorWindow != null && MaterialEditorWindow.gameObject != null)
                    return MaterialEditorWindow.gameObject.activeInHierarchy;
                return false;
            }
            set
            {
                if (MaterialEditorWindow != null)
                    MaterialEditorWindow.gameObject.SetActive(value);
                if (!value)
                    TexChangeWatcher?.Dispose();
            }
        }

        internal static void UISettingChanged(object sender, EventArgs e)
        {
            if (MaterialEditorWindow != null)
                MaterialEditorWindow.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f / UIScale.Value, 1080f / UIScale.Value);
            if (MaterialEditorMainPanel != null)
                SetMainRectWithMemory(0.15f, 0.05f, UIWidth.Value * UIScale.Value, UIHeight.Value * UIScale.Value);
            if (MaterialEditorScrollableUI != null)
                MaterialEditorScrollableUI.transform.SetRect(0f, 0f, 1f, 1f, MarginSize, MarginSize + HeaderSize, -MarginSize, -HeaderSize - MarginSize / 2f);
            if (FooterPanel != null)
                FooterPanel.transform.SetRect(0f, 0f, 1f, 0f, 0f, MarginSize / 2f, 0f, MarginSize / 2f + HeaderSize);
            if (MaterialEditorRendererList != null)
                MaterialEditorRendererList.Panel.transform.SetRect(1f, 0.5f, 1f, 1f, MarginSize, MarginSize / 2f, MarginSize + UIListWidth.Value);
            if (MaterialEditorMaterialList != null)
                MaterialEditorMaterialList.Panel.transform.SetRect(1f, 0f, 1f, 0.5f, MarginSize, 0f, MarginSize + UIListWidth.Value, -MarginSize);
            UpdateTexturePreviewPanelPosition();
        }

        /// <summary>
        /// Search text using wildcards.
        /// </summary>
        /// <param name="text">Text to search in</param>
        /// <param name="filter">Filter with which to search the text</param>
        internal static bool WildCardSearch(string text, string filter)
        {
            string regex = "^.*" + Regex.Escape(filter).Replace("\\?", ".").Replace("\\*", ".*") + ".*$";
            return Regex.IsMatch(text, regex, RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Populate the renderer list
        /// </summary>
        /// <param name="go">GameObject for which to read the renderers</param>
        /// <param name="data">Object that will be passed through to the get/set/reset events</param>
        /// <param name="rendListFull">List of all renderers to display</param>
        private void PopulateRendererList(GameObject go, object data, IEnumerable<Renderer> rendListFull)
        {
            if (go != CurrentGameObject)
            {
                SelectedRenderers.Clear();
                MaterialEditorRendererList.ClearList();

                foreach (var rend in rendListFull)
                    MaterialEditorRendererList.AddEntry(rend.NameFormatted(), value =>
                    {
                        if (value)
                            SelectedRenderers.Add(rend);
                        else
                            SelectedRenderers.Remove(rend);
                        PopulateList(go, data, CurrentFilter);
                        PopulateMaterialList(go, data, rendListFull);
                    });
                PopulateMaterialList(go, data, rendListFull);
            }
        }


        /// <summary>
        /// Populate the materials list
        /// </summary>
        /// <param name="go">GameObject for which to read the renderers</param>
        /// <param name="data">Object that will be passed through to the get/set/reset events</param>
        /// <param name="materials">List of all materials to display</param>
        private void PopulateMaterialList(GameObject go, object data, IEnumerable<Renderer> materials)
        {
            SelectedMaterials.Clear();
            MaterialEditorMaterialList.ClearList();

            foreach (var rend in materials.Where(rend => SelectedRenderers.Count == 0 || SelectedRenderers.Contains(rend)))
            foreach (var mat in GetMaterials(go, rend))
                MaterialEditorMaterialList.AddEntry(mat.NameFormatted(), value =>
                {
                    if (value)
                        SelectedMaterials.Add(mat);
                    else
                        SelectedMaterials.Remove(mat);
                    PopulateList(go, data, CurrentFilter);
                });
        }

        /// <summary>
        /// Populate the rename list
        /// </summary>
        /// <param name="go">GameObject for which to read the renderers</param>
        /// <param name="material">Material to be renamed</param>
        /// <param name="data">Object that will be passed through to the get/set/reset events</param>
        private void PopulateRenameList(GameObject go, Material material, object data)
        {
            SelectedMaterialRenderers.Clear();
            MaterialEditorRenameList.ClearList();

            // Setup title
            MaterialEditorRenameMaterial.text = material.NameFormatted();

            // Setup text field
            string formattedName = material.NameFormatted().Split(new[] { MaterialCopyPostfix }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (string.IsNullOrEmpty(formattedName))
            {
                MaterialEditorPluginBase.Logger.LogWarning("Material name is empty or failed to be extracted from: " + material.name);
                formattedName = "";
            }
            MaterialEditorRenameField.text = formattedName;

            // Setup button
            string suffix = material.NameFormatted().Replace(formattedName, "");
            MaterialEditorRenameButton.interactable = false;
            MaterialEditorRenameButton.onClick.RemoveAllListeners();
            MaterialEditorRenameButton.onClick.AddListener(() =>
            {
                string safeNewName = MaterialEditorRenameField.text.Replace(MaterialCopyPostfix, "").Trim() + suffix;
                foreach (var renderer in SelectedMaterialRenderers)
                    SetMaterialName(data, renderer, material, safeNewName, go);
                RefreshUI();
            });

            // Setup renderer list
            foreach (var renderer in GetRendererList(go))
            {
                if (!renderer.materials.Any(mat => mat.NameFormatted() == material.NameFormatted())) continue;
                MaterialEditorRenameList.AddEntry(renderer.NameFormatted(), value =>
                {
                    if (value)
                        SelectedMaterialRenderers.Add(renderer);
                    else
                        SelectedMaterialRenderers.Remove(renderer);
                    MaterialEditorRenameButton.interactable = SelectedMaterialRenderers.Count > 0;
                });
            }
        }

        /// <summary>
        /// Populate the MaterialEditor UI
        /// </summary>
        /// <param name="go">GameObject for which to read the renderers and materials</param>
        /// <param name="data">Object that will be passed through to the get/set/reset events</param>
        /// <param name="filter">Comma separated list of text to filter the results</param>
        protected void PopulateList(GameObject go, object data, string filter = null)
        {
            if (RenameListVisible)
            {
                MaterialEditorRenameList.ToggleVisibility(false);
                ViewListButton.GetComponentInChildren<Text>().text = ">";
                RenameListVisible = false;
            }

            if (filter == null)
            {
                if (PersistFilter.Value) filter = CurrentFilter;
                else filter = "";
            }

            MaterialEditorWindow.gameObject.SetActive(true);
            MaterialEditorWindow.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f / UIScale.Value, 1080f / UIScale.Value);
            SetMainRectWithMemory(0.15f, 0.05f, UIWidth.Value * UIScale.Value, UIHeight.Value * UIScale.Value);
            FilterInputField.Set(filter);

            if (go == null) return;

            List<Renderer> rendList = new List<Renderer>();
            IEnumerable<Renderer> rendListFull = GetRendererList(go);
            List<Projector> projectorList = new List<Projector>();
            IEnumerable<Projector> projectorListFull = GetProjectorList(data, go);
            List<string> filterList = new List<string>();
            List<string> filterListProperties = new List<string>();
            List<ItemInfo> items = new List<ItemInfo>();
            Dictionary<string, Material> matList = new Dictionary<string, Material>();

            PopulateRendererList(go, data, rendListFull);

            //Clear collapsed state when switching to a different object
            if (go != CurrentGameObject)
            {
                CollapsedMaterials.Clear();
                CollapsedRenderers.Clear();

                //Pre-collapse based on config options
                if (MaterialEditorPluginBase.CollapseRenderersByDefault.Value)
                    foreach (var rend in GetRendererList(go))
                        CollapsedRenderers.Add(rend.GetInstanceID());
                if (MaterialEditorPluginBase.CollapseMaterialsByDefault.Value)
                    foreach (var rend in GetRendererList(go))
                        foreach (var mat in GetMaterials(go, rend))
                            CollapsedMaterials.Add(mat.NameFormatted());
            }

            CurrentGameObject = go;
            CurrentData = data;
            CurrentFilter = filter;

            if (!filter.IsNullOrEmpty())
            {
                filterList = filter.Split(',').Select(x => x.Trim()).ToList();
                filterList.RemoveAll(x => x.IsNullOrWhiteSpace());

                filterListProperties = new List<string>(filterList);
                filterListProperties = filterListProperties
                    .Where(x => x.StartsWith("_"))
                    .Select(x => x.Trim('_'))
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToList();
                filterList = filterList.Where(x => !x.StartsWith("_")).ToList();
            }

            //Get all renderers and materials matching the filter
            if (SelectedRenderers.Count > 0)
                rendList.AddRange(SelectedRenderers);
            else if (filterList.Count == 0)
                rendList = rendListFull.ToList();
            else
            {
                foreach (var rend in rendListFull)
                {
                    foreach (string filterWord in filterList)
                        if (WildCardSearch(rend.NameFormatted(), filterWord.Trim()) && !rendList.Contains(rend))
                            rendList.Add(rend);

                    foreach (var mat in SelectedMaterials.Count == 0 ? GetMaterials(go, rend) : GetMaterials(go, rend).Where(mat => SelectedMaterials.Contains(mat)))
                    foreach (string filterWord in filterList)
                        if (WildCardSearch(mat.NameFormatted(), filterWord.Trim()))
                            matList[mat.NameFormatted()] = mat;
                }
                foreach (var projector in projectorListFull)
                foreach (string filterWord in filterList)
                    if (WildCardSearch(projector.NameFormatted(), filterWord.Trim()))
                        projectorList.Add(projector);
            }

            //Build matList regardless of collapse state so materials always show
            if (filterList.Count == 0)
                foreach (var rend in rendList)
                    foreach (var mat in SelectedMaterials.Count == 0 ? GetMaterials(go, rend) : GetMaterials(go, rend).Where(mat => SelectedMaterials.Contains(mat)))
                        matList[mat.NameFormatted()] = mat;

            //Renderer section header row
            var rendererSectionItem = new ItemInfo(ItemInfo.RowItemType.RendererSection, "Renderers")
            {
                RendererCount = rendList.Count
            };
            items.Add(rendererSectionItem);

            if (!RendererSectionCollapsed)
            {
            for (var i = 0; i < rendList.Count; i++)
            {
                var rend = rendList[i];

                bool valueEnabledOriginal = rend.enabled;
                var temp = GetRendererPropertyValueOriginal(data, rend, RendererProperties.Enabled, go);
                if (!temp.IsNullOrEmpty())
                    valueEnabledOriginal = temp == "1";

                var rendererItem = new ItemInfo(ItemInfo.RowItemType.Renderer, "Renderer")
                {
                    RendererName = rend.NameFormatted(),
                    RendererInstanceID = rend.GetInstanceID(),
                    RendererHasChanges =
                        !GetRendererPropertyValueOriginal(data, rend, RendererProperties.Enabled, go).IsNullOrEmpty() ||
                        !GetRendererPropertyValueOriginal(data, rend, RendererProperties.ShadowCastingMode, go).IsNullOrEmpty() ||
                        !GetRendererPropertyValueOriginal(data, rend, RendererProperties.ReceiveShadows, go).IsNullOrEmpty() ||
                        !GetRendererPropertyValueOriginal(data, rend, RendererProperties.UpdateWhenOffscreen, go).IsNullOrEmpty() ||
                        !GetRendererPropertyValueOriginal(data, rend, RendererProperties.RecalculateNormals, go).IsNullOrEmpty(),
                    ExportUVOnClick = () => Export.ExportUVMaps(rend),
                    ExportObjOnClick = () =>
                    {
                        ObjRenderer = rend;
                        DoObjExport = true;
                    },
                    SelectInterpolableButtonRendererOnClick = () => SelectInterpolableButtonOnClick(go, ItemInfo.RowItemType.Renderer, rendererName: rend.NameFormatted()),
                    RendererEnabled = rend.enabled,
                    RendererEnabledOriginal = valueEnabledOriginal,
                    RendererEnabledOnChange = value => SetRendererProperty(data, rend, RendererProperties.Enabled, (value ? 1 : 0).ToString(), go),
                    RendererEnabledOnReset = () => RemoveRendererProperty(data, rend, RendererProperties.Enabled, go)
                };
                items.Add(rendererItem);

                // Skip all properties for this renderer if collapsed
                if (CollapsedRenderers.Contains(rend.GetInstanceID()))
                    continue;

                //Renderer ShadowCastingMode
                var valueShadowCastingModeOriginal = rend.shadowCastingMode;
                temp = GetRendererPropertyValueOriginal(data, rend, RendererProperties.ShadowCastingMode, go);
                if (!temp.IsNullOrEmpty())
                    valueShadowCastingModeOriginal = (UnityEngine.Rendering.ShadowCastingMode)int.Parse(temp);
                var rendererShadowCastingModeItem = new ItemInfo(ItemInfo.RowItemType.RendererShadowCastingMode, "Shadow Casting Mode")
                {
                    RendererShadowCastingMode = (int)rend.shadowCastingMode,
                    RendererShadowCastingModeOriginal = (int)valueShadowCastingModeOriginal,
                    RendererShadowCastingModeOnChange = value => SetRendererProperty(data, rend, RendererProperties.ShadowCastingMode, value.ToString(), go),
                    RendererShadowCastingModeOnReset = () => RemoveRendererProperty(data, rend, RendererProperties.ShadowCastingMode, go)
                };
                items.Add(rendererShadowCastingModeItem);

                //Renderer ReceiveShadows
                bool valueReceiveShadowsOriginal = rend.receiveShadows;
                temp = GetRendererPropertyValueOriginal(data, rend, RendererProperties.ReceiveShadows, go);
                if (!temp.IsNullOrEmpty())
                    valueReceiveShadowsOriginal = temp == "1";
                var rendererReceiveShadowsItem = new ItemInfo(ItemInfo.RowItemType.RendererReceiveShadows, "Receive Shadows")
                {
                    RendererReceiveShadows = rend.receiveShadows,
                    RendererReceiveShadowsOriginal = valueReceiveShadowsOriginal,
                    RendererReceiveShadowsOnChange = value => SetRendererProperty(data, rend, RendererProperties.ReceiveShadows, (value ? 1 : 0).ToString(), go),
                    RendererReceiveShadowsOnReset = () => RemoveRendererProperty(data, rend, RendererProperties.ReceiveShadows, go)
                };
                items.Add(rendererReceiveShadowsItem);

                if (rend is SkinnedMeshRenderer meshRenderer) // recalculate normals should only exist on skinned renderers
                {
                    //Renderer UpdateWhenOffscreen
#if !KK
                    bool valueUpdateWhenOffscreenOriginal = meshRenderer.updateWhenOffscreen;
                    temp = GetRendererPropertyValueOriginal(data, rend, RendererProperties.UpdateWhenOffscreen, go);
                    if (!temp.IsNullOrEmpty())
                        valueUpdateWhenOffscreenOriginal = temp == "1";
                    var rendererUpdateWhenOffscreenItem = new ItemInfo(ItemInfo.RowItemType.RendererUpdateWhenOffscreen, "Update When Off-Screen")
                    {
                        RendererUpdateWhenOffscreen = meshRenderer.updateWhenOffscreen,
                        RendererUpdateWhenOffscreenOriginal = valueUpdateWhenOffscreenOriginal,
                        RendererUpdateWhenOffscreenOnChange = value => SetRendererProperty(data, rend, RendererProperties.UpdateWhenOffscreen, (value ? 1 : 0).ToString(), go),
                        RendererUpdateWhenOffscreenOnReset = () => RemoveRendererProperty(data, rend, RendererProperties.UpdateWhenOffscreen, go)
                    };
                    items.Add(rendererUpdateWhenOffscreenItem);
#endif

                    //Renderer RecalculateNormals
                    bool valueRecalculateNormalsOriginal = false; // this is not a real renderproperty so I cannot be true by default
                    temp = GetRendererPropertyValueOriginal(data, rend, RendererProperties.RecalculateNormals, go);
                    if (!temp.IsNullOrEmpty())
                        valueRecalculateNormalsOriginal = temp == "1";
                    bool valueRecalculateNormals = false; // actual value not storable in renderer, grab renderProperty stored by ME instead
                    temp = GetRendererPropertyValue(data, rend, RendererProperties.RecalculateNormals, go);
                    if (!temp.IsNullOrEmpty())
                        valueRecalculateNormals = temp == "1";
                    var rendererRecalculateNormalsItem = new ItemInfo(ItemInfo.RowItemType.RendererRecalculateNormals, "Recalculate Normals")
                    {
                        RendererRecalculateNormals = valueRecalculateNormals,
                        RendererRecalculateNormalsOriginal = valueRecalculateNormalsOriginal,
                        RendererRecalculateNormalsOnChange = value => SetRendererProperty(data, rend, RendererProperties.RecalculateNormals, (value ? 1 : 0).ToString(), go),
                        RendererRecalculateNormalsOnReset = () => RemoveRendererProperty(data, rend, RendererProperties.RecalculateNormals, go)
                    };
                    items.Add(rendererRecalculateNormalsItem);
                }
            }

            } // end if (!RendererSectionCollapsed)

            //Material section header row
            var materialSectionItem = new ItemInfo(ItemInfo.RowItemType.MaterialSection, "Materials")
            {
                MaterialCount = matList.Count
            };
            items.Add(materialSectionItem);

            if (!MaterialSectionCollapsed)
            {
                foreach (var mat in matList.Values)
                    PopulateListMaterial(mat);

                foreach (var projector in filterList.Count == 0 ? projectorListFull : projectorList)
                    PopulateListMaterial(projector.material, projector);
            }

            VirtualList.SetList(items);

            //Ensure button colours are correct after population (covers first-open in dark mode)
            var btnBgColor = MaterialEditorPluginBase.DarkMode.Value ? new Color(0.25f, 0.25f, 0.28f, 1f) : new Color(0.85f, 0.85f, 0.85f, 1f);
            foreach (var btn in MaterialEditorScrollableUI.content.GetComponentsInChildren<Button>(true))
            {
                var btnImg = btn.GetComponent<Image>();
                if (btnImg != null) btnImg.color = btnBgColor;
                var btnTxt = btn.GetComponentInChildren<Text>();
                if (btnTxt != null && btn.gameObject.name != "RendererSectionCollapseButton" && btn.gameObject.name != "MaterialSectionCollapseButton"
                    && btn.gameObject.name != "RendererCollapseButton" && btn.gameObject.name != "MaterialCollapseButton")
                    btnTxt.color = ItemTextColor;
            }

            //Update sticky section bar with layout indices and current state
            var sticky = MaterialEditorScrollableUI?.GetComponent<StickySectionBar>();
            if (sticky != null)
            {
                //MaterialSection item sits after the RendererSection header and all renderer rows
                int matSectionIdx = items.IndexOf(materialSectionItem);
                sticky.UpdateLayout(
                    matSectionIdx,
                    items.Count,
                    RendererSectionCollapsed ? $"Renderers ({rendList.Count}) +" : $"Renderers ({rendList.Count})",
                    MaterialSectionCollapsed  ? $"Materials ({matList.Count}) +"  : $"Materials ({matList.Count})",
                    RendererSectionCollapsed,
                    MaterialSectionCollapsed);
            }

            //Update footer section button labels to reflect current state
            if (FooterRendererSectionButton != null)
                FooterRendererSectionButton.GetComponentInChildren<Text>().text = RendererSectionCollapsed ? "Renderers +" : "Renderers -";
            if (FooterMaterialSectionButton != null)
                FooterMaterialSectionButton.GetComponentInChildren<Text>().text = MaterialSectionCollapsed ? "Materials +" : "Materials -";

            void PopulateListMaterial(Material mat, Projector projector = null)
            {
                string materialName = mat.NameFormatted();
                string shaderName = mat.shader.NameFormatted();

                // Check if this material has any ME-tracked changes
                string origShaderName = GetMaterialShaderNameOriginal(data, mat, go);
                int? origRenderQueue = GetMaterialShaderRenderQueueOriginal(data, mat, go);
                bool matHasChanges = (!origShaderName.IsNullOrEmpty() && origShaderName != shaderName)
                    || (origRenderQueue.HasValue && origRenderQueue.Value != mat.renderQueue);

                // Also check texture, color, and float properties if not already flagged
                if (!matHasChanges)
                {
                    var checkCats = PropertyOrganizer.PropertyOrganization[XMLShaderProperties.ContainsKey(shaderName) ? shaderName : "default"];
                    foreach (var cat in checkCats)
                    {
                        foreach (var prop in cat.Value)
                        {
                            if (matHasChanges) break;
                            if (prop.Type == ShaderPropertyType.Texture)
                            {
                                if (!GetMaterialTextureValueOriginal(data, mat, prop.Name, go))
                                    matHasChanges = true;
                                var offOrig = GetMaterialTextureOffsetOriginal(data, mat, prop.Name, go);
                                if (offOrig.HasValue && offOrig.Value != mat.GetTextureOffset($"_{prop.Name}"))
                                    matHasChanges = true;
                                var scaleOrig = GetMaterialTextureScaleOriginal(data, mat, prop.Name, go);
                                if (scaleOrig.HasValue && scaleOrig.Value != mat.GetTextureScale($"_{prop.Name}"))
                                    matHasChanges = true;
                            }
                            else if (prop.Type == ShaderPropertyType.Color && mat.HasProperty($"_{prop.Name}"))
                            {
                                var colOrig = GetMaterialColorPropertyValueOriginal(data, mat, prop.Name, go);
                                if (colOrig.HasValue && colOrig.Value != mat.GetColor($"_{prop.Name}"))
                                    matHasChanges = true;
                            }
                            else if (prop.Type == ShaderPropertyType.Float && mat.HasProperty($"_{prop.Name}"))
                            {
                                var floatOrig = GetMaterialFloatPropertyValueOriginal(data, mat, prop.Name, go);
                                if (floatOrig.HasValue && floatOrig.Value != mat.GetFloat($"_{prop.Name}"))
                                    matHasChanges = true;
                            }
                        }
                        if (matHasChanges) break;
                    }
                }

                var materialItem = new ItemInfo(ItemInfo.RowItemType.Material, "Material")
                {
                    MaterialName = materialName,
                    MaterialCollapseKey = materialName,
                    MaterialHasChanges = matHasChanges,
                    MaterialOnCopy = () => MaterialCopyEdits(data, mat, go),
                    MaterialOnPaste = () =>
                    {
                        MaterialPasteEdits(data, mat, go);
                        PopulateList(go, data, filter);
                    }
                };
                //Projectors only support 1 material. Copy button is hidden if the function is null
                if (projector == null)
                    materialItem.MaterialOnCopyRemove = () =>
                    {
                        MaterialCopyRemove(data, mat, go);
                        PopulateList(go, data, filter);
                        PopulateMaterialList(go, data, rendListFull);
                    };
                materialItem.MaterialOnRename = () =>
                {
                    if (ListsVisible)
                    {
                        MaterialEditorRendererList.ToggleVisibility(false);
                        MaterialEditorMaterialList.ToggleVisibility(false);
                        ListsVisible = false;
                    }
                    ViewListButton.GetComponentInChildren<Text>().text = "<";
                    MaterialEditorRenameList.ToggleVisibility(true);
                    PopulateRenameList(go, mat, data);
                    RenameListVisible = true;
                };
                items.Add(materialItem);

                if (projector != null)
                    PopulateProjectorSettings(projector);

                // Skip all properties for this material if collapsed
                if (CollapsedMaterials.Contains(materialName))
                    return;

                //Shader
                string shaderNameOriginal = shaderName;
                var temp = GetMaterialShaderNameOriginal(data, mat, go);
                if (!temp.IsNullOrEmpty())
                    shaderNameOriginal = temp;
                var shaderItem = new ItemInfo(ItemInfo.RowItemType.Shader, "Shader")
                {
                    ShaderName = shaderName,
                    ShaderNameOriginal = shaderNameOriginal,
                    ShaderNameOnChange = value =>
                    {
                        SetMaterialShaderName(data, mat, value, go);
                        StartCoroutine(PopulateListCoroutine(go, data, filter));
                    },
                    ShaderNameOnReset = () =>
                    {
                        RemoveMaterialShaderName(data, mat, go);
                        StartCoroutine(PopulateListCoroutine(go, data, filter));
                    },
                    SelectInterpolableButtonShaderOnClick = () => SelectInterpolableButtonOnClick(go, ItemInfo.RowItemType.Shader, materialName)
                };
                items.Add(shaderItem);

                //Shader RenderQueue
                int renderQueueOriginal = mat.renderQueue;
                int? renderQueueOriginalTemp = GetMaterialShaderRenderQueueOriginal(data, mat, go);
                renderQueueOriginal = renderQueueOriginalTemp ?? renderQueueOriginal;
                var shaderRenderQueueItem = new ItemInfo(ItemInfo.RowItemType.ShaderRenderQueue, "Render Queue")
                {
                    ShaderRenderQueue = mat.renderQueue,
                    ShaderRenderQueueOriginal = renderQueueOriginal,
                    ShaderRenderQueueOnChange = value => SetMaterialShaderRenderQueue(data, mat, value, go),
                    ShaderRenderQueueOnReset = () => RemoveMaterialShaderRenderQueue(data, mat, go)
                };
                items.Add(shaderRenderQueueItem);

                // Shader property organizer
                var categories = PropertyOrganizer.PropertyOrganization[XMLShaderProperties.ContainsKey(shaderName) ? shaderName : "default"];

                foreach (var category in categories)
                {
                    // There is no API to determine whether the shader contains a certain keyword, we can't rely
                    // on a toggle property with the same name to determine whether the keyword is present either.
                    var properties = category.Value.Where(x => x.Type == ShaderPropertyType.Keyword || mat.HasProperty($"_{x.Name}"));
                    if (
                        filterListProperties.Count == 0
                        && (categories.Count > 1 || category.Key != PropertyOrganizer.UncategorizedName)
                        && properties.Any()

                    )
                    {
                        var categoryItem = new ItemInfo(ItemInfo.RowItemType.PropertyCategory, category.Key);
                        items.Add(categoryItem);
                    }

                    foreach (var property in properties)
                    {
                        string propertyName = property.Name;
                        // Blacklist
                        if (MaterialEditorPluginBase.Instance.CheckBlacklist(materialName, propertyName)) continue;
                        // Filter
                        if (!(filterListProperties.Count == 0 || filterListProperties.Any(fw => WildCardSearch(propertyName, fw)))) continue;

                        if (property.Type == ShaderPropertyType.Texture)
                        {
                            var textureItem = new ItemInfo(ItemInfo.RowItemType.TextureProperty, propertyName)
                            {
                                TextureChanged = !GetMaterialTextureValueOriginal(data, mat, propertyName, go),
                                TextureExists = mat.GetTexture($"_{propertyName}") != null,
                                TexturePreview = mat.GetTexture($"_{propertyName}"),
                                TexturePreviewFileName = mat.GetTexture($"_{propertyName}")?.name ?? "",
                                TextureOnExport = () => ExportTexture(mat, propertyName),
                                SelectInterpolableButtonTextureOnClick = () => SelectInterpolableButtonOnClick(go, ItemInfo.RowItemType.TextureProperty, materialName, propertyName)
                            };
                            textureItem.TextureOnImport = () =>
                            {
#if !API
                                string fileFilter = KK_Plugins.ImageHelper.FileFilter;
#else
                                string fileFilter = "Images (*.png;.jpg)|*.png;*.jpg|All files|*.*";
#endif
                                KKAPI.Utilities.OpenFileDialog.Show(OnFileAccept, "Open image", ExportPath, fileFilter, ".png");

                                void OnFileAccept(string[] strings)
                                {
                                    if (strings == null || strings.Length == 0 || strings[0].IsNullOrEmpty())
                                    {
                                        textureItem.TextureChanged = !GetMaterialTextureValueOriginal(data, mat, propertyName, go);
                                        textureItem.TextureExists = mat.GetTexture($"_{propertyName}") != null;
                                        return;
                                    }
                                    string filePath = strings[0];

                                    SetMaterialTexture(data, mat, propertyName, filePath, go);
                                    textureItem.TexturePreviewFileName = System.IO.Path.GetFileName(filePath);
                                    // Wait a frame for the texture to be applied before reading it back
                                    StartCoroutine(UpdateTexturePreview(textureItem, mat, propertyName));

                                    TexChangeWatcher?.Dispose();
                                    if (WatchTexChanges.Value)
                                    {
                                        var directory = Path.GetDirectoryName(filePath);
                                        if (directory != null)
                                        {
                                            TexChangeWatcher = new FileSystemWatcher(directory, Path.GetFileName(filePath));
                                            TexChangeWatcher.Changed += (sender, args) =>
                                            {
                                                if (WatchTexChanges.Value && File.Exists(filePath))
                                                    SetMaterialTexture(data, mat, propertyName, filePath, go);
                                            };
                                            TexChangeWatcher.Deleted += (sender, args) => TexChangeWatcher?.Dispose();
                                            TexChangeWatcher.Error += (sender, args) => TexChangeWatcher?.Dispose();
                                            TexChangeWatcher.EnableRaisingEvents = true;
                                        }
                                    }
                                }
                            };
                            textureItem.TextureOnReset = () => { RemoveMaterialTexture(data, mat, propertyName, go); textureItem.TexturePreview = mat.GetTexture($"_{propertyName}"); };
                            items.Add(textureItem);

                            Vector2 textureOffset = mat.GetTextureOffset($"_{propertyName}");
                            Vector2 textureOffsetOriginal = textureOffset;
                            Vector2? textureOffsetOriginalTemp = GetMaterialTextureOffsetOriginal(data, mat, propertyName, go);
                            if (textureOffsetOriginalTemp != null)
                                textureOffsetOriginal = (Vector2)textureOffsetOriginalTemp;

                            Vector2 textureScale = mat.GetTextureScale($"_{propertyName}");
                            Vector2 textureScaleOriginal = textureScale;
                            Vector2? textureScaleOriginalTemp = GetMaterialTextureScaleOriginal(data, mat, propertyName, go);
                            if (textureScaleOriginalTemp != null)
                                textureScaleOriginal = (Vector2)textureScaleOriginalTemp;

                            var textureItemOffsetScale = new ItemInfo(ItemInfo.RowItemType.TextureOffsetScale)
                            {
                                Offset = textureOffset,
                                OffsetOriginal = textureOffsetOriginal,
                                OffsetOnChange = value => SetMaterialTextureOffset(data, mat, propertyName, value, go),
                                OffsetOnReset = () => RemoveMaterialTextureOffset(data, mat, propertyName, go),
                                Scale = textureScale,
                                ScaleOriginal = textureScaleOriginal,
                                ScaleOnChange = value => SetMaterialTextureScale(data, mat, propertyName, value, go),
                                ScaleOnReset = () => RemoveMaterialTextureScale(data, mat, propertyName, go)
                            };
                            items.Add(textureItemOffsetScale);
                        }
                        else if (property.Type == ShaderPropertyType.Color)
                        {
                            Color valueColor = mat.GetColor($"_{propertyName}");
                            Color valueColorOriginal = valueColor;
                            Color? c = GetMaterialColorPropertyValueOriginal(data, mat, propertyName, go);
                            if (c != null)
                                valueColorOriginal = (Color)c;
                            var contentItem = new ItemInfo(ItemInfo.RowItemType.ColorProperty, propertyName)
                            {
                                ColorValue = valueColor,
                                ColorValueOriginal = valueColorOriginal,
                                ColorValueOnChange = value => SetMaterialColorProperty(data, mat, propertyName, value, go),
                                ColorValueOnReset = () => RemoveMaterialColorProperty(data, mat, propertyName, go),
                                ColorValueOnEdit = (title, value, onChanged) => SetupColorPalette(data, mat, $"Material Editor - {title}", value, onChanged, true),
                                ColorValueSetToPalette = (title, value) => SetColorToPalette(data, mat, $"Material Editor - {title}", value),
                                SelectInterpolableButtonColorOnClick = () => SelectInterpolableButtonOnClick(go, ItemInfo.RowItemType.ColorProperty, materialName, propertyName)
                            };
                            items.Add(contentItem);
                        }
                        else if (property.Type == ShaderPropertyType.Float)
                        {
                            float valueFloatOriginal = mat.GetFloat($"_{propertyName}");
                            float? valueFloatOriginalTemp = GetMaterialFloatPropertyValueOriginal(data, mat, propertyName, go);
                            if (valueFloatOriginalTemp != null)
                                valueFloatOriginal = (float)valueFloatOriginalTemp;

                            AddFloatslider
                            (
                                valueFloat: mat.GetFloat($"_{propertyName}"),
                                propertyName: propertyName,
                                onInteroperableClick: () => SelectInterpolableButtonOnClick(go, ItemInfo.RowItemType.FloatProperty, materialName, propertyName),
                                changeValue: value => SetMaterialFloatProperty(data, mat, propertyName, value, go),
                                resetValue: () => RemoveMaterialFloatProperty(data, mat, propertyName, go),
                                valueFloatOriginal: valueFloatOriginal,
                                minValue: property.MinValue,
                                maxValue: property.MaxValue
                            );
                        }
                        else if (property.Type == ShaderPropertyType.Keyword)
                        {
                            // Since there's no way to check if a Keyword exists, we'll have to trust the XML.
                            bool valueKeyword = mat.IsKeywordEnabled($"_{propertyName}");
                            bool valueKeywordOriginal = valueKeyword;
                            bool? valueKeywordOriginalTemp = GetMaterialKeywordPropertyValueOriginal(data, mat, propertyName, go);

                            if (valueKeywordOriginalTemp != null)
                                valueKeywordOriginal = (bool)valueKeywordOriginalTemp;

                            var contentItem = new ItemInfo(ItemInfo.RowItemType.KeywordProperty, propertyName)
                            {
                                KeywordValue = valueKeyword,
                                KeywordValueOriginal = valueKeywordOriginal,
                                KeywordValueOnChange = value => SetMaterialKeywordProperty(data, mat, propertyName, value, go),
                                KeywordValueOnReset = () => RemoveMaterialKeywordProperty(data, mat, propertyName, go)
                            };

                            items.Add(contentItem);
                        }
                    }
                }
            }

            void PopulateProjectorSettings(Projector projector)
            {
                foreach (var property in Enum.GetValues(typeof(ProjectorProperties)).Cast<ProjectorProperties>())
                {
                    float maxValue = 100f;
                    string name = "";
                    float valueFloat = 0f;
                    float? originalValueTemp = GetProjectorPropertyValueOriginal(data, projector, property, go);

                    switch (property)
                    {
                        case ProjectorProperties.Enabled:
                            name = "Enabled";
                            valueFloat = Convert.ToSingle(projector.enabled);
                            maxValue = 1f;
                            break;
                        case ProjectorProperties.NearClipPlane:
                            name = "Near Clip Plane";
                            valueFloat = projector.nearClipPlane;
                            maxValue = ProjectorNearClipPlaneMax.Value;
                            break;
                        case ProjectorProperties.FarClipPlane:
                            name = "Far Clip Plane";
                            valueFloat = projector.farClipPlane;
                            maxValue = ProjectorFarClipPlaneMax.Value;
                            break;
                        case ProjectorProperties.FieldOfView:
                            name = "Field Of View";
                            valueFloat = projector.fieldOfView;
                            maxValue = ProjectorFieldOfViewMax.Value;
                            break;
                        case ProjectorProperties.AspectRatio:
                            name = "Aspect Ratio";
                            valueFloat = projector.aspectRatio;
                            maxValue = ProjectorAspectRatioMax.Value;
                            break;
                        case ProjectorProperties.Orthographic:
                            name = "Orthographic";
                            valueFloat = Convert.ToSingle(projector.orthographic);
                            maxValue = 1f;
                            break;
                        case ProjectorProperties.OrthographicSize:
                            name = "Orthographic Size";
                            valueFloat = projector.orthographicSize;
                            maxValue = ProjectorOrthographicSizeMax.Value;
                            break;
                        case ProjectorProperties.IgnoreMapLayer:
                            name = "Ignore Map layer";
                            valueFloat = Convert.ToSingle(projector.ignoreLayers == (projector.ignoreLayers | (1 << 11)));
                            maxValue = 1f;
                            break;
                        case ProjectorProperties.IgnoreCharaLayer:
                            name = "Ignore Chara Layer";
                            valueFloat = Convert.ToSingle(projector.ignoreLayers == (projector.ignoreLayers | (1 << 10)));
                            maxValue = 1f;
                            break;
                    }

                    if (filterListProperties.Count == 0 || filterListProperties.Any(filterWord => WildCardSearch(name, filterWord)))
                        AddFloatslider
                        (
                            valueFloat: valueFloat,
                            propertyName: name,
                            onInteroperableClick: () => SelectProjectorInterpolableButtonOnClick(go, property, projector.NameFormatted()),
                            changeValue: value => SetProjectorProperty(data, projector, property, value, projector.gameObject),
                            resetValue: () => RemoveProjectorProperty(data, projector, property, projector.gameObject),
                            valueFloatOriginal: originalValueTemp != null ? (float)originalValueTemp : valueFloat,
                            minValue: 0f,
                            maxValue: maxValue
                        );
                }
            }

            void AddFloatslider(
                float valueFloat,
                string propertyName,
                Action onInteroperableClick,
                Action<float> changeValue,
                Action resetValue,
                float valueFloatOriginal,
                float? minValue = null,
                float? maxValue = null)
            {
                var contentItem = new ItemInfo(ItemInfo.RowItemType.FloatProperty, propertyName)
                {
                    FloatValue = valueFloat, FloatValueOriginal = valueFloatOriginal, SelectInterpolableButtonFloatOnClick = () => onInteroperableClick()
                };
                if (minValue != null)
                    contentItem.FloatValueSliderMin = (float)minValue;
                if (maxValue != null)
                    contentItem.FloatValueSliderMax = (float)maxValue;
                contentItem.FloatValueOnChange = value => changeValue(value);
                contentItem.FloatValueOnReset = () => resetValue();
                items.Add(contentItem);
            }
        }

        /// <summary>
        /// Obj export should be done in OnGUI or something similarly late so that finger rotation is exported properly
        /// </summary>
        private void OnGUI()
        {
            if (DoObjExport)
            {
                DoObjExport = false;
                Export.ExportObj(ObjRenderer);
                ObjRenderer = null;
            }
        }

        /// <summary>
        /// Hacky workaround to wait for the dropdown fade to complete before refreshing
        /// </summary>
        protected IEnumerator PopulateListCoroutine(GameObject go, object data, string filter = "")
        {
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            PopulateList(go, data, filter);
        }

        private IEnumerator UpdateTexturePreview(ItemInfo item, Material mat, string propertyName)
        {
            yield return null;
            item.TexturePreview = mat.GetTexture($"_{propertyName}");
        }

        internal virtual void ExportTexture(Material mat, string property)
        {
            var tex = mat.GetTexture($"_{property}");
            if (tex == null) return;
            var matName = mat.NameFormatted();
            matName = string.Concat(matName.Split(Path.GetInvalidFileNameChars())).Trim();
            string filename = Path.Combine(ExportPath, $"_Export_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}_{matName}_{property}.png");
            MaterialEditorPluginBase.Instance.ConvertNormalMap(ref tex, property, ConvertNormalmapsOnExport.Value);
            SaveTex(tex, filename);
            MaterialEditorPluginBase.Logger.LogInfo($"Exported {filename}");
            Utilities.OpenFileInExplorer(filename);
        }

        internal void ExportTextureOriginal(Material mat, string property, string ext, byte[] texData)
        {
            var matName = mat.NameFormatted();
            matName = string.Concat(matName.Split(Path.GetInvalidFileNameChars())).Trim();
            string filename = Path.Combine(ExportPath, $"_Export_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}_{matName}_{property}.{ext}");
            System.IO.File.WriteAllBytes(filename, texData);
            MaterialEditorPluginBase.Logger.LogInfo($"Exported {filename}");
            Utilities.OpenFileInExplorer(filename);
        }

        /// <summary>
        /// Gets the original value of a renderer property.
        /// </summary>
        /// <param name="data">The data object associated with the renderer.</param>
        /// <param name="renderer">The renderer to retrieve the property value from.</param>
        /// <param name="property">The property to retrieve.</param>
        /// <param name="gameObject">The game object associated with the renderer.</param>
        /// <returns>The original value of the renderer property.</returns>
        public abstract string GetRendererPropertyValueOriginal(object data, Renderer renderer, RendererProperties property, GameObject gameObject);
        /// <summary>
        /// Gets the current value of a renderer property.
        /// </summary>
        /// <param name="data">The data object associated with the renderer.</param>
        /// <param name="renderer">The renderer to retrieve the property value from.</param>
        /// <param name="property">The property to retrieve.</param>
        /// <param name="gameObject">The game object associated with the renderer.</param>
        /// <returns>The current value of the renderer property.</returns>
        public abstract string GetRendererPropertyValue(object data, Renderer renderer, RendererProperties property, GameObject gameObject);
        /// <summary>
        /// Sets the value of a renderer property.
        /// </summary>
        /// <param name="data">The data object associated with the renderer.</param>
        /// <param name="renderer">The renderer to modify.</param>
        /// <param name="property">The property to set.</param>
        /// <param name="value">The value to set for the property.</param>
        /// <param name="gameObject">The game object associated with the renderer.</param>
        public abstract void SetRendererProperty(object data, Renderer renderer, RendererProperties property, string value, GameObject gameObject);
        /// <summary>
        /// Removes a renderer property.
        /// </summary>
        /// <param name="data">The data object associated with the renderer.</param>
        /// <param name="renderer">The renderer to modify.</param>
        /// <param name="property">The property to remove.</param>
        /// <param name="gameObject">The game object associated with the renderer.</param>
        public abstract void RemoveRendererProperty(object data, Renderer renderer, RendererProperties property, GameObject gameObject);

        /// <summary>
        /// Gets the original value of a projector property.
        /// </summary>
        /// <param name="data">The data object associated with the projector.</param>
        /// <param name="renderer">The projector to retrieve the property value from.</param>
        /// <param name="property">The property to retrieve.</param>
        /// <param name="gameObject">The game object associated with the projector.</param>
        /// <returns>The original value of the projector property.</returns>
        public abstract float? GetProjectorPropertyValueOriginal(object data, Projector renderer, ProjectorProperties property, GameObject gameObject);
        /// <summary>
        /// Gets the current value of a projector property.
        /// </summary>
        /// <param name="data">The data object associated with the projector.</param>
        /// <param name="renderer">The projector to retrieve the property value from.</param>
        /// <param name="property">The property to retrieve.</param>
        /// <param name="gameObject">The game object associated with the projector.</param>
        /// <returns>The current value of the projector property.</returns>
        public abstract float? GetProjectorPropertyValue(object data, Projector renderer, ProjectorProperties property, GameObject gameObject);
        /// <summary>
        /// Sets the value of a projector property.
        /// </summary>
        /// <param name="data">The data object associated with the projector.</param>
        /// <param name="projector">The projector to modify.</param>
        /// <param name="property">The property to set.</param>
        /// <param name="value">The value to set for the property.</param>
        /// <param name="gameObject">The game object associated with the projector.</param>
        public abstract void SetProjectorProperty(object data, Projector projector, ProjectorProperties property, float value, GameObject gameObject);
        /// <summary>
        /// Removes a projector property.
        /// </summary>
        /// <param name="data">The data object associated with the projector.</param>
        /// <param name="projector">The projector to modify.</param>
        /// <param name="property">The property to remove.</param>
        /// <param name="gameObject">The game object associated with the projector.</param>
        public abstract void RemoveProjectorProperty(object data, Projector projector, ProjectorProperties property, GameObject gameObject);
        /// <summary>
        /// Gets the list of projectors associated with a game object.
        /// </summary>
        /// <param name="data">The data object associated with the game object.</param>
        /// <param name="gameObject">The game object to retrieve projectors from.</param>
        /// <returns>An enumerable list of projectors.</returns>
        public abstract IEnumerable<Projector> GetProjectorList(object data, GameObject gameObject);

        /// <summary>
        /// Copies edits made to a material.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to copy edits from.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        public abstract void MaterialCopyEdits(object data, Material material, GameObject gameObject);
        /// <summary>
        /// Pastes edits to a material.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to paste edits to.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        public abstract void MaterialPasteEdits(object data, Material material, GameObject gameObject);
        /// <summary>
        /// Removes copied edits from a material.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to remove copied edits from.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        public abstract void MaterialCopyRemove(object data, Material material, GameObject gameObject);

        /// <summary>
        /// Gets the original name of a material.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="renderer">The renderer associated with the material.</param>
        /// <param name="material">The material to retrieve the name from.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        /// <returns>The original name of the material.</returns>
        public abstract string GetMaterialNameOriginal(object data, Renderer renderer, Material material, GameObject gameObject);
        /// <summary>
        /// Sets the name of a material.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="renderer">The renderer associated with the material.</param>
        /// <param name="material">The material to set the name for.</param>
        /// <param name="value">The new name for the material.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        public abstract void SetMaterialName(object data, Renderer renderer, Material material, string value, GameObject gameObject);
        /// <summary>
        /// Removes the name of a material.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="renderer">The renderer associated with the material.</param>
        /// <param name="material">The material to remove the name from.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        public abstract void RemoveMaterialName(object data, Renderer renderer, Material material, GameObject gameObject);

        /// <summary>
        /// Gets the original shader name of a material.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to retrieve the shader name from.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        /// <returns>The original shader name of the material.</returns>
        public abstract string GetMaterialShaderNameOriginal(object data, Material material, GameObject gameObject);
        /// <summary>
        /// Sets the shader name of a material.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to set the shader name for.</param>
        /// <param name="value">The new shader name for the material.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        public abstract void SetMaterialShaderName(object data, Material material, string value, GameObject gameObject);
        /// <summary>
        /// Removes the shader name of a material.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to remove the shader name from.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        public abstract void RemoveMaterialShaderName(object data, Material material, GameObject gameObject);

        /// <summary>
        /// Gets the original render queue value of a material's shader.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to retrieve the render queue value from.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        /// <returns>The original render queue value of the material's shader.</returns>
        public abstract int? GetMaterialShaderRenderQueueOriginal(object data, Material material, GameObject gameObject);
        /// <summary>
        /// Sets the render queue value of a material's shader.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to set the render queue value for.</param>
        /// <param name="value">The new render queue value for the material's shader.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        public abstract void SetMaterialShaderRenderQueue(object data, Material material, int value, GameObject gameObject);
        /// <summary>
        /// Removes the render queue value of a material's shader.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to remove the render queue value from.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        public abstract void RemoveMaterialShaderRenderQueue(object data, Material material, GameObject gameObject);

        /// <summary>
        /// Gets the original texture value of a material property.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to retrieve the texture value from.</param>
        /// <param name="propertyName">The name of the texture property.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        /// <returns>True if the texture value has changed; otherwise, false.</returns>
        public abstract bool GetMaterialTextureValueOriginal(object data, Material material, string propertyName, GameObject gameObject);
        /// <summary>
        /// Sets the texture value of a material property.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to set the texture value for.</param>
        /// <param name="propertyName">The name of the texture property.</param>
        /// <param name="filePath">The file path of the texture to set.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        public abstract void SetMaterialTexture(object data, Material material, string propertyName, string filePath, GameObject gameObject);
        /// <summary>
        /// Removes the texture value of a material property.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to remove the texture value from.</param>
        /// <param name="propertyName">The name of the texture property.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        public abstract void RemoveMaterialTexture(object data, Material material, string propertyName, GameObject gameObject);

        /// <summary>
        /// Gets the original texture offset value of a material property.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to retrieve the texture offset value from.</param>
        /// <param name="propertyName">The name of the texture property.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        /// <returns>The original texture offset value of the material property.</returns>
        public abstract Vector2? GetMaterialTextureOffsetOriginal(object data, Material material, string propertyName, GameObject gameObject);
        /// <summary>
        /// Sets the texture offset value of a material property.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to set the texture offset value for.</param>
        /// <param name="propertyName">The name of the texture property.</param>
        /// <param name="value">The new texture offset value.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        public abstract void SetMaterialTextureOffset(object data, Material material, string propertyName, Vector2 value, GameObject gameObject);
        /// <summary>
        /// Removes the texture offset value of a material property.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to remove the texture offset value from.</param>
        /// <param name="propertyName">The name of the texture property.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        public abstract void RemoveMaterialTextureOffset(object data, Material material, string propertyName, GameObject gameObject);

        /// <summary>
        /// Gets the original texture scale value of a material property.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to retrieve the texture scale value from.</param>
        /// <param name="propertyName">The name of the texture property.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        /// <returns>The original texture scale value of the material property.</returns>
        public abstract Vector2? GetMaterialTextureScaleOriginal(object data, Material material, string propertyName, GameObject gameObject);
        /// <summary>
        /// Sets the texture scale value of a material property.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to set the texture scale value for.</param>
        /// <param name="propertyName">The name of the texture property.</param>
        /// <param name="value">The new texture scale value.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        public abstract void SetMaterialTextureScale(object data, Material material, string propertyName, Vector2 value, GameObject gameObject);
        /// <summary>
        /// Removes the texture scale value of a material property.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to remove the texture scale value from.</param>
        /// <param name="propertyName">The name of the texture property.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        public abstract void RemoveMaterialTextureScale(object data, Material material, string propertyName, GameObject gameObject);

        /// <summary>
        /// Gets the original color value of a material property.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to retrieve the color value from.</param>
        /// <param name="propertyName">The name of the color property.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        /// <returns>The original color value of the material property.</returns>
        public abstract Color? GetMaterialColorPropertyValueOriginal(object data, Material material, string propertyName, GameObject gameObject);
        /// <summary>
        /// Sets the color value of a material property.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to set the color value for.</param>
        /// <param name="propertyName">The name of the color property.</param>
        /// <param name="value">The new color value.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        public abstract void SetMaterialColorProperty(object data, Material material, string propertyName, Color value, GameObject gameObject);
        /// <summary>
        /// Removes the color value of a material property.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to remove the color value from.</param>
        /// <param name="propertyName">The name of the color property.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        public abstract void RemoveMaterialColorProperty(object data, Material material, string propertyName, GameObject gameObject);

        /// <summary>
        /// Gets the original float value of a material property.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to retrieve the float value from.</param>
        /// <param name="propertyName">The name of the float property.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        /// <returns>The original float value of the material property.</returns>
        public abstract float? GetMaterialFloatPropertyValueOriginal(object data, Material material, string propertyName, GameObject gameObject);
        /// <summary>
        /// Sets the float value of a material property.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to set the float value for.</param>
        /// <param name="propertyName">The name of the float property.</param>
        /// <param name="value">The new float value.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        public abstract void SetMaterialFloatProperty(object data, Material material, string propertyName, float value, GameObject gameObject);
        /// <summary>
        /// Removes the float value of a material property.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to remove the float value from.</param>
        /// <param name="propertyName">The name of the float property.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        public abstract void RemoveMaterialFloatProperty(object data, Material material, string propertyName, GameObject gameObject);

        /// <summary>
        /// Gets the original keyword value of a material property.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to retrieve the keyword value from.</param>
        /// <param name="propertyName">The name of the keyword property.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        /// <returns>The original keyword value of the material property.</returns>
        public abstract bool? GetMaterialKeywordPropertyValueOriginal(object data, Material material, string propertyName, GameObject gameObject);
        /// <summary>
        /// Sets the keyword value of a material property.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to set the keyword value for.</param>
        /// <param name="propertyName">The name of the keyword property.</param>
        /// <param name="value">The new keyword value.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        public abstract void SetMaterialKeywordProperty(object data, Material material, string propertyName, bool value, GameObject gameObject);
        /// <summary>
        /// Removes the keyword value of a material property.
        /// </summary>
        /// <param name="data">The data object associated with the material.</param>
        /// <param name="material">The material to remove the keyword value from.</param>
        /// <param name="propertyName">The name of the keyword property.</param>
        /// <param name="gameObject">The game object associated with the material.</param>
        public abstract void RemoveMaterialKeywordProperty(object data, Material material, string propertyName, GameObject gameObject);

        private void SetupColorPalette(object data, Material material, string title, Color value, Action<Color> onChanged, bool useAlpha)
        {
            var name = material.name;
            if (ColorPalette.IsShowing(title, data, name))
            {
                ColorPalette.Close();
                return;
            }

            try
            {
                ColorPalette.Setup(title, data, name, value, onChanged, useAlpha);
            }
            catch (ArgumentException)
            {
                MaterialEditorPluginBase.Logger.LogError($"Color value is out of range. ({value})");
                ColorPalette.Close();
            }
        }
        private void SetColorToPalette(object data, Material material, string title, Color value)
        {
            if (ColorPalette.IsShowing(title, data, material.name))
            {
                try
                {
                    ColorPalette.SetColor(value);
                }
                catch (ArgumentException)
                {
                    MaterialEditorPluginBase.Logger.LogError($"Color value is out of range. ({value})");
                    ColorPalette.Close();
                }
            }
        }

        private void SelectInterpolableButtonOnClick(GameObject go, ItemInfo.RowItemType rowType, string materialName = "", string propertyName = "", string rendererName = "")
        {
            selectedInterpolable = new SelectedInterpolable(go, rowType, materialName, propertyName, rendererName);
            MaterialEditorPluginBase.Logger.LogMessage($"Activated interpolable(s), {selectedInterpolable}");
#if !API && !EC
            TimelineCompatibilityHelper.RefreshInterpolablesList();
#endif
        }

        private void SelectProjectorInterpolableButtonOnClick(GameObject go, ProjectorProperties property, string projectorName)
        {
            selectedProjectorInterpolable = new SelectedProjectorInterpolable(go, property, projectorName);
            MaterialEditorPluginBase.Logger.LogMessage($"Activated interpolable(s), {selectedProjectorInterpolable}");
#if !API && !EC
            TimelineCompatibilityHelper.RefreshInterpolablesList();
#endif
        }

        internal class SelectedInterpolable
        {
            public string MaterialName;
            public string PropertyName;
            public string RendererName;
            public GameObject GameObject;
            public ItemInfo.RowItemType RowType;

            public SelectedInterpolable(GameObject go, ItemInfo.RowItemType rowType, string materialName, string propertyName, string rendererName)
            {
                GameObject = go;
                RowType = rowType;
                MaterialName = materialName;
                PropertyName = propertyName;
                RendererName = rendererName;
            }

            public override string ToString()
            {
                return $"{RowType}: {string.Join(" - ", new string[] { PropertyName, MaterialName, RendererName, }.Where(x => !x.IsNullOrEmpty()).ToArray())}";
            }
        }

        internal class SelectedProjectorInterpolable
        {
            public string ProjectorName;
            public ProjectorProperties Property;
            public GameObject GameObject;

            public SelectedProjectorInterpolable(GameObject go, ProjectorProperties property, string projectorName)
            {
                GameObject = go;
                Property = property;
                ProjectorName = projectorName;
            }

            public override string ToString()
            {
                return $"Projector: {string.Join(" - ", new string[] { Property.ToString(), ProjectorName, }.Where(x => !x.IsNullOrEmpty()).ToArray())}";
            }
        }
    }
}
