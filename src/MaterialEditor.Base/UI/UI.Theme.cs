using UnityEngine;

namespace MaterialEditorAPI
{
    internal enum MaterialEditorThemeMode
    {
        Legacy,
        Dark
    }

    internal enum MaterialEditorThemeColorRole
    {
        Transparent,
        PrimaryText,
        SecondaryText,
        DisabledText,
        Accent,
        TooltipSurface,
        NavigatorShaderHeader,
        InputBorder,
        StrongBorder,
        Outline,
        CategoryNavigationHighlight
    }


    // Dynamic visual tokens for the programmatic uGUI surface. Light is the
    // default; Dark is an opt-in presentation of the same semantic roles.
    internal static class MaterialEditorTheme
    {
        private static MaterialEditorThemeMode _mode =
            MaterialEditorThemeMode.Legacy;

        internal static MaterialEditorThemeMode Mode => _mode;

        internal static bool SetMode(MaterialEditorThemeMode mode)
        {
            if (_mode == mode)
                return false;
            _mode = mode;
            return true;
        }

        internal static MaterialEditorThemeMode ToggleMode =>
            _mode == MaterialEditorThemeMode.Legacy
                ? MaterialEditorThemeMode.Dark
                : MaterialEditorThemeMode.Legacy;

        internal static class Colors
        {
            internal static Color Window => Select(Color.white, Rgb(0x1E, 0x23, 0x2B));
            internal static Color LeftPanel => NeutralHeader;
            internal static Color CenterPanel => Select(Color.white, Rgb(0x24, 0x2B, 0x34));
            internal static Color RightPanel => NeutralHeader;
            internal static Color NeutralHeader => Select(Color.gray, Rgb(0x2A, 0x31, 0x3B));
            internal static Color PropertyRow => Select(new Color(1f, 1f, 1f, 0f), Rgb(0x2A, 0x31, 0x3B));
            internal static Color PropertyRowAlternate => Select(new Color(0f, 0f, 0f, 0.08f), Rgb(0x2D, 0x35, 0x40));
            internal static Color InputSurface => Select(Color.white, Rgb(0x16, 0x1B, 0x22));
            internal static Color DropdownSurface => Select(Color.white, Rgb(0x16, 0x1B, 0x22));
            internal static Color PopupSurface => Select(Color.white, Rgb(0x16, 0x1B, 0x22));

            internal static Color RendererHeader => Select(new Color(0.984f, 0.600f, 0.008f, 0.5f), Rgb(0x2F, 0x4F, 0x74));
            internal static Color MaterialHeader => Select(new Color(0.400f, 0.690f, 0.196f, 0.5f), Rgb(0x3F, 0x5A, 0x48));
            internal static Color ShaderHeader => Select(new Color(1f, 1f, 1f, 0f), Rgb(0x36, 0x5C, 0x73));
            internal static Color NavigatorShaderHeader => DropdownSurface;
            internal static Color CategoryHeader => Select(new Color(0.627f, 0.004f, 0.812f, 0.5f), Rgb(0x6F, 0x5A, 0x8E));
            internal static Color CategoryHeaderHover => Select(new Color(0.627f, 0.004f, 0.812f, 0.5f), Rgb(0x76, 0x5F, 0x97));
            internal static Color CategoryHeaderExpanded => Select(new Color(0.627f, 0.004f, 0.812f, 0.5f), Rgb(0x5F, 0x4C, 0x7D));
            internal static Color SubcategoryHeader => Select(new Color(0.72f, 0.72f, 0.72f, 0.5f), Rgb(0x3A, 0x42, 0x4C));
            internal static Color SubcategoryHeaderHover => Select(new Color(0.78f, 0.78f, 0.78f, 0.65f), Rgb(0x44, 0x4D, 0x58));
            internal static Color SubcategoryHeaderExpanded => Select(new Color(0.68f, 0.68f, 0.68f, 0.55f), Rgb(0x34, 0x3B, 0x44));

            internal static Color Hover => Select(Gray(0.882353f), Rgb(0x33, 0x46, 0x5C));
            internal static Color Pressed => Select(Gray(0.698039f), Rgb(0x39, 0x45, 0x53));
            internal static Color Selected => Select(Rgb(0x4D, 0x84, 0xB8), Rgb(0x3A, 0x74, 0xA8));
            internal static Color SelectedText => Select(Color.black, Color.white);
            internal static Color DisabledSurface => Select(Gray(0.521569f), Rgb(0x20, 0x26, 0x2F));
            internal static Color ModifiedIndicator => Select(Gray(0.35f), Rgb(0x8A, 0x98, 0xA8));
            internal static Color ModifiedSurface => Select(new Color(0f, 0f, 0f, 0.3f), Rgb(0x39, 0x42, 0x4D));

            internal static Color Primary => Select(Color.black, Rgb(0xE6, 0xEC, 0xF2));
            internal static Color NativeControlText => Select(Gray(50f / 255f), Primary);
            internal static Color Secondary => Select(Gray(0.20f), Rgb(0xAA, 0xB6, 0xC3));
            internal static Color Disabled => Select(Gray(0.45f), Rgb(0x8A, 0x92, 0x9D));
            internal static Color Accent => Select(new Color(0.05f, 0.45f, 1f, 1f), Rgb(0x4A, 0xA3, 0xFF));
            internal static Color InputBorder => Select(Gray(0.28f), Rgb(0x8D, 0x9B, 0xAA));
            internal static Color StrongBorder => Select(Color.black, Rgb(0x73, 0x82, 0x91));
            internal static Color Divider => Select(Gray(0.60f), Rgb(0x3E, 0x4A, 0x58));
            internal static Color HandlePressed => Select(Rgb(0xA5, 0xCD, 0xF5), Rgb(0x8D, 0xC6, 0xFF));
            internal static Color Warning => Select(Rgb(0x9A, 0x62, 0x00), Rgb(0xD7, 0xA4, 0x4A));
            internal static Color Error => Select(Rgb(0xA5, 0x1E, 0x2D), Rgb(0xDD, 0x66, 0x70));
            internal static Color Success => Select(Rgb(0x1F, 0x78, 0x43), Rgb(0x62, 0xB9, 0x85));

            internal static Color Panel => CenterPanel;
            internal static Color Raised => InputSurface;
            internal static Color Header => NeutralHeader;
            internal static Color Row => Select(new Color(1f, 1f, 1f, 0.6f), PropertyRow);
            internal static Color Border => Divider;

            internal static Color TintIdentity => Color.white;
            internal static Color MainPanel => CenterPanel;
            internal static Color SidePanel => RightPanel;
            internal static Color RendererRow => RendererHeader;
            internal static Color MaterialRow => MaterialHeader;
            internal static Color ShaderRow => ShaderHeader;
            internal static Color CategoryRow => CategoryHeader;
            internal static Color CategoryNavigationHighlight => CategoryRow;
            internal static Color SubcategoryRow => SubcategoryHeader;
            internal static Color TransparentRow => new Color(1f, 1f, 1f, 0f);
            internal static Color ChangedRow => ModifiedSurface;
            internal static Color ControlNormal => InputSurface;
            internal static Color ControlHover => Hover;
            internal static Color ControlPressed => Pressed;
            internal static Color ControlDisabled => DisabledSurface;
            internal static Color ToggleMark => SelectedText;
            internal static Color SliderTrack => Divider;
            internal static Color SliderFill => Accent;
            internal static Color SliderHandle => Select(Color.white, Primary);
            internal static Color SliderHandlePressed => HandlePressed;
            internal static Color ScrollSurface => CenterPanel;
            internal static Color SideListSurface => Select(Gray(0.6f), Rgb(0x22, 0x28, 0x32));
            internal static Color SideListRow => Select(Row, PropertyRow);
            internal static Color ScrollbarTrack => Select(Gray(0.42f), SideListSurface);
            internal static Color Scrollbar => Select(new Color(1f, 1f, 1f, 0.6f), StrongBorder);
            internal static Color ScrollbarHandle => Select(new Color(1f, 1f, 1f, 0.6f), Secondary);
            internal static Color ScrollbarHandlePressed => HandlePressed;
            internal static Color PlaceholderText => Secondary;
            internal static Color ShaderHintUnderline => Accent;
            internal static Color TooltipSurface => Select(
                new Color(0.2f, 0.2f, 0.2f, 0.98f),
                WithAlpha(Rgb(0x16, 0x1B, 0x22), 0.98f));
            internal static Color PrimaryText => Primary;
            internal static Color SecondaryText => Secondary;
            internal static Color DisabledText => Disabled;
            internal static Color Outline => StrongBorder;

            internal static Color Resolve(MaterialEditorThemeColorRole role)
            {
                switch (role)
                {
                    case MaterialEditorThemeColorRole.Transparent:
                        return TransparentRow;
                    case MaterialEditorThemeColorRole.PrimaryText:
                        return PrimaryText;
                    case MaterialEditorThemeColorRole.SecondaryText:
                        return SecondaryText;
                    case MaterialEditorThemeColorRole.DisabledText:
                        return DisabledText;
                    case MaterialEditorThemeColorRole.Accent:
                        return Accent;
                    case MaterialEditorThemeColorRole.CategoryNavigationHighlight:
                        return CategoryNavigationHighlight;
                    case MaterialEditorThemeColorRole.TooltipSurface:
                        return TooltipSurface;
                    case MaterialEditorThemeColorRole.NavigatorShaderHeader:
                        return NavigatorShaderHeader;
                    case MaterialEditorThemeColorRole.InputBorder:
                        return InputBorder;
                    case MaterialEditorThemeColorRole.StrongBorder:
                        return StrongBorder;
                    default:
                        return Outline;
                }
            }


            private static Color Select(Color legacy, Color dark)
            {
                return Select(_mode, legacy, dark);
            }

            private static Color Select(
                MaterialEditorThemeMode mode,
                Color legacy,
                Color dark)
            {
                return mode == MaterialEditorThemeMode.Dark ? dark : legacy;
            }

            private static Color Gray(float value)
            {
                return new Color(value, value, value, 1f);
            }

            private static Color Rgb(int red, int green, int blue)
            {
                return new Color(red / 255f, green / 255f, blue / 255f, 1f);
            }

            private static Color WithAlpha(Color color, float alpha)
            {
                return new Color(color.r, color.g, color.b, alpha);
            }
        }
        internal static class Metrics
        {
            internal const float CanvasReferenceWidth = 1920f;
            internal const float CanvasReferenceHeight = 1080f;
            internal const float UiScaleMinimum = 1f;
            internal const float UiScaleDefault = 1.75f;
            internal const float UiScaleMaximum = 3f;
            internal const float WindowWidthMinimum = 0f;
            internal const float WindowWidthDefault = 0.33f;
            internal const float WindowWidthMaximum = 1f;
            internal const float WindowHeightMinimum = 0f;
            internal const float WindowHeightDefault = 0.3f;
            internal const float WindowHeightMaximum = 1f;
            internal const float Margin = 5f;
            internal const float HeaderHeight = 20f;
            internal const float TopBarHeight = HeaderHeight * 2f;
            internal const float WindowHeaderRecoveryWidth = 96f;
            internal const float SectionHeaderHeight = HeaderHeight;
            internal const float ScrollbarOffset = -15f;
            internal const float RowHeight = 22f;
            internal const float CategoryNavigatorCollapsedWidth = 24f;
            internal const float CategoryActiveMarkerWidth = 3f;
            internal const float SelectionPanelCollapsedWidth = 24f;
            internal const float SelectionPanelHeaderHeight = HeaderHeight;
            internal const float SelectionPanelFilterHeight = HeaderHeight;
            internal const float SidePanelMinimumWidth = 100f;
            internal const float CategoryPanelDefaultWidth = 150f;
            internal const float SidePanelDefaultWidth = 180f;
            internal const float SidePanelMaximumWidth = 500f;
            // Topbar plus one full row in each half-height selection/Rename list.
            internal const float ResponsiveMinimumMainHeight = 138f;
            internal const float ResponsiveOuterMarginFraction = 0.05f;

            internal const float LabelWidth = 0f;
            internal const float ButtonWidth = 100f;
            internal const float SmallButtonWidth = 20f;
            internal const float ResetButtonWidth = SmallButtonWidth;
            internal const float InterpolableButtonWidth = SmallButtonWidth;
            internal const float ContentWidth = 316f;
            internal const float SubcategoryHeaderIndent = 12f;
            internal const float SubcategoryHeaderRightInset = 2f;
            internal const float SubcategoryHeaderVerticalInset = 1f;
            internal const float SubcategoryContentIndent = SubcategoryHeaderIndent;
            internal const float SubcategoryContentRightInset = SubcategoryHeaderRightInset;
            internal const float FoldIndicatorWidth = 16f;
            internal const float RendererButtonWidth = ButtonWidth;
            internal const float RendererToggleWidth = 20f;
            internal const float RendererDropdownWidth = 94f;
            internal const float MaterialButtonWidth = ButtonWidth * 0.75f;
            internal const float MaterialRenameButtonWidth = SmallButtonWidth;
            internal const float ShaderLabelMinimumWidth = 70f;
            internal const float ShaderDropdownMinimumWidth = 220f;
            internal const float ShaderDropdownWidth =
                ContentWidth + Spacing.Control;
            internal const float RenderQueueInputWidth = 94f;
            internal const float OffsetScaleLabelXWidth = 48f;
            internal const float OffsetScaleLabelYWidth = 10f;
            internal const float OffsetScaleInputWidth = 50f;
            internal const float OffsetScaleGroupSpacing = 4f;
            internal const float ColorLabelWidth = 10f;
            internal const float ColorInputWidth = 64f;
            internal const float ColorEditButtonWidth = 20f;
            internal const float FloatSliderWidth = ContentWidth - 94f;
            internal const float FloatInputWidth = 94f;
            internal const float VectorComponentLabelWidth = 14f;
            // Four vector channels, including their labels and internal gaps,
            // must consume the same 318 px editor budget as the other
            // Timeline-capable property rows. This keeps the stable "O"
            // column aligned without shrinking the numeric fields.
            internal const float VectorComponentInputWidth = 62f;
            internal const float KeywordToggleWidth = ContentWidth;

            internal const float DropdownTemplateWidth = 100f;
            internal const float DropdownPopupScreenMargin = 4f;
            internal const float TooltipWidth = 280f;
            internal const float TooltipMaximumHeight = 360f;
            internal const float TooltipDelaySeconds = 0f;
            internal const float SelectionPanelTitleFraction = 0.4f;
            internal const float SelectionToggleSize = 18f;
            internal const float IconSize = 16f;
            internal const float CloseGlyphInset = 8f;
            internal const float CloseGlyphAngle = 45f;
            internal const float ShaderHintDashWidth = 3f;
            internal const float ShaderHintDashGap = 2f;
            internal const float ShaderHintLineThickness = 2f;
            internal const float ShaderHintUnderlineRise = 2f;
        }

        internal static class Typography
        {
            internal const int AdaptiveMinimumFontSize = 2;
            internal const int PrimaryFontSize = 16;
            internal const int SecondaryFontSize = 14;
            internal const int IconFontSize = 16;
            internal const int InputMinimumFontSize = AdaptiveMinimumFontSize;
            internal const int DropdownFontSize = 16;
            internal const int DropdownMinimumFontSize = AdaptiveMinimumFontSize;
            internal const int VectorComponentFontSize = 16;
            internal const int VectorComponentMinimumFontSize = AdaptiveMinimumFontSize;
            internal const int PropertyLabelMinimumFontSize = AdaptiveMinimumFontSize;
            internal const int PropertyCategoryMinimumFontSize = AdaptiveMinimumFontSize;
            internal const int SelectionNameMinimumFontSize = AdaptiveMinimumFontSize;
            internal const int TooltipFontSize = 11;
        }

        internal static class Spacing
        {
            internal const float Horizontal = 2f;
            internal const float Vertical = 1f;
            internal const float Control = 2f;
            internal const float TopBarHorizontalInset = 3f;
            internal const float Section = 5f;
            internal const int NavigatorHeaderHorizontalInset = 3;
            internal const int SelectionEntryPadding = 1;
            internal const int PropertyLabelInset = 1;
            internal const int RowPaddingLeft = 3;
            internal const int RowPaddingRight = 1;
            internal const int RowPaddingTop = 1;
            internal const int RowPaddingBottom = 1;
            internal const int NavigatorContentPadding = 0;
            internal const float NavigatorListSpacing = 1f;
            internal const int CategoryEntryPadding = 1;
            internal const float CategoryEntrySpacing = 2f;
            internal const float PropertyCategorySpacing = 2f;
            internal const float PropertySubcategorySpacing = 2f;
            internal const int TooltipHorizontalPadding = 4;
            internal const int TooltipVerticalPadding = 2;
            internal const float TooltipCursorOffset = 5f;
            internal const float DropdownTextVerticalInset = 1f;
            internal const float DropdownCaptionLeftInset = 5f;
            internal const float DropdownCaptionRightInset = 15f;
            internal const float DropdownCaptionVerticalInset = 2f;
            internal const float SelectionPanelContentInset = 2f;
            internal const float SelectionPanelTitleInset = 5f;
            internal const float SelectionToggleInset = 1f;
        }

        internal static class Glyphs
        {
            internal const string FoldCollapsed = "\u25B8";
            internal const string FoldExpanded = "\u25BE";
            internal const string AllFolded = FoldCollapsed + FoldCollapsed;
            internal const string AllExpanded = FoldExpanded + FoldExpanded;
            internal const string Reset = "R";
            internal const string ChevronRight = ">";
            internal const string ChevronLeft = "<";
            internal const string Interpolable = "O";
        }

        internal static class States
        {
            internal const float VisibleAlpha = 1f;
            internal const float DisabledAlpha = 0.55f;
            internal const float HiddenAlpha = 0f;

            internal static float SelectableFadeDuration =>
                Mode == MaterialEditorThemeMode.Legacy ? 0.1f : 0.08f;

            internal static Color DefaultSurface => Colors.Row;
            internal static Color HoveredSurface => Colors.Hover;
            internal static Color PressedSurface => Colors.Pressed;
            internal static Color SelectedSurface => Colors.Selected;
            internal static Color FocusedIndicator => Colors.Accent;
            internal static Color DisabledText => Colors.Disabled;
            internal static Color MixedIndicator => Colors.Secondary;
            internal static Color ModifiedSurface => Colors.ChangedRow;
        }
    }
}
