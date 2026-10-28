using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal static class MaterialEditorInputStyles
    {
        private static Sprite _darkToggleMarkSprite;

        internal static void ApplyInputField(InputField inputField)
        {
            if (inputField == null)
                return;

            MaterialEditorControlStyleState.Assign(
                inputField,
                MaterialEditorControlStyleRole.InputField);
            MaterialEditorScrollSelectableStyles.ApplySelectable(
                inputField,
                MaterialEditorTheme.Colors.InputSurface,
                MaterialEditorTheme.Colors.ControlHover,
                MaterialEditorTheme.Colors.ControlPressed,
                MaterialEditorTheme.Colors.ControlDisabled);
            MaterialEditorScrollSelectableStyles.ApplyControlOutline(
                inputField.targetGraphic,
                MaterialEditorThemeColorRole.InputBorder);
            inputField.lineType = InputField.LineType.SingleLine;
            if (inputField.textComponent != null)
            {
                inputField.textComponent.resizeTextForBestFit = true;
                inputField.textComponent.resizeTextMinSize =
                    MaterialEditorTheme.Typography.InputMinimumFontSize;
                inputField.textComponent.resizeTextMaxSize =
                    MaterialEditorTheme.Typography.PrimaryFontSize;
                inputField.textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
                inputField.textComponent.verticalOverflow = VerticalWrapMode.Truncate;
                inputField.textComponent.maskable = true;
                inputField.textComponent.raycastTarget = false;
                MaterialEditorPanelTextStyles.ApplyText(inputField.textComponent, MaterialEditorTextRole.Input);
            }
            if (inputField.placeholder is Text placeholder)
            {
                placeholder.resizeTextForBestFit = true;
                placeholder.resizeTextMinSize =
                    MaterialEditorTheme.Typography.InputMinimumFontSize;
                placeholder.resizeTextMaxSize =
                    MaterialEditorTheme.Typography.PrimaryFontSize;
                placeholder.maskable = true;
                placeholder.raycastTarget = false;
                MaterialEditorPanelTextStyles.ApplyText(placeholder, MaterialEditorTextRole.Placeholder);
            }

            var numericInput = inputField.GetComponent<NumericInputView>();
            if (numericInput != null)
                numericInput.RestoreConfiguration();
        }

        internal static void ApplyToggle(Toggle toggle)
        {
            if (toggle == null)
                return;

            MaterialEditorControlStyleState.Assign(
                toggle,
                MaterialEditorControlStyleRole.Toggle);
            // Virtualized rows reuse the same Toggle for different values.
            // DefaultControls uses Fade, which can leave the previous check
            // visible while a recycled row is being rebound. The logical
            // value owns the mark immediately; hover only tints the surface.
            toggle.toggleTransition = Toggle.ToggleTransition.None;
            MaterialEditorScrollSelectableStyles.ApplySelectable(
                toggle,
                MaterialEditorTheme.Colors.ControlNormal,
                MaterialEditorTheme.Colors.ControlHover,
                MaterialEditorTheme.Colors.ControlPressed,
                MaterialEditorTheme.Colors.ControlDisabled);
            ApplyToggleMark(toggle);
            foreach (var text in toggle.GetComponentsInChildren<Text>(true))
            {
                MaterialEditorPanelTextStyles.ApplyText(
                    text,
                    MaterialEditorPanelTextStyles.HasAssignedTextRole(text)
                        ? MaterialEditorPanelTextStyles.GetAssignedTextRole(text)
                        : MaterialEditorTextRole.Button);
            }
        }

        internal static void ApplyToggleMark(Toggle toggle)
        {
            var graphic = toggle.graphic;
            if (graphic == null)
                return;

            var image = graphic as Image;
            if (image != null)
            {
                image.sprite = GetToggleMarkSprite();
                image.type = Image.Type.Simple;
                image.preserveAspect = false;
            }

            graphic.color = MaterialEditorTheme.Mode == MaterialEditorThemeMode.Legacy
                ? MaterialEditorTheme.Colors.TintIdentity
                : MaterialEditorTheme.Colors.ToggleMark;
            graphic.canvasRenderer.SetColor(MaterialEditorTheme.Colors.TintIdentity);
            graphic.canvasRenderer.SetAlpha(
                toggle.isOn
                    ? MaterialEditorTheme.States.VisibleAlpha
                    : MaterialEditorTheme.States.HiddenAlpha);
            graphic.SetVerticesDirty();
        }

        private static Sprite GetToggleMarkSprite()
        {
            var source = UIUtility.checkMark;
            if (MaterialEditorTheme.Mode == MaterialEditorThemeMode.Legacy || source == null)
                return source;
            if (_darkToggleMarkSprite != null)
                return _darkToggleMarkSprite;

            // Keep the native mark's resolution, padding and alpha; only whiten its dark pixels.
            // The bundled texture is not CPU-readable, so read it back once for the Dark theme.
            var sourceTexture = source.texture;
            var texture = new Texture2D(
                sourceTexture.width,
                sourceTexture.height,
                TextureFormat.ARGB32,
                sourceTexture.mipmapCount > 1)
            {
                name = "MaterialEditorDarkToggleMarkTexture",
                filterMode = sourceTexture.filterMode,
                wrapMode = sourceTexture.wrapMode,
                anisoLevel = sourceTexture.anisoLevel,
                hideFlags = HideFlags.HideAndDontSave
            };
            var previous = RenderTexture.active;
            RenderTexture temporary = null;
            try
            {
                temporary = RenderTexture.GetTemporary(
                    sourceTexture.width, sourceTexture.height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(sourceTexture, temporary);
                RenderTexture.active = temporary;
                texture.ReadPixels(new Rect(0f, 0f, texture.width, texture.height), 0, 0, false);
                var pixels = texture.GetPixels32();
                for (var i = 0; i < pixels.Length; i++)
                    pixels[i] = new Color32(0xFF, 0xFF, 0xFF, pixels[i].a);
                texture.SetPixels32(pixels);
                texture.Apply(true, true);

                var sprite = Sprite.Create(
                    texture, source.rect,
                    new Vector2(source.pivot.x / source.rect.width, source.pivot.y / source.rect.height),
                    source.pixelsPerUnit, 0, SpriteMeshType.FullRect, source.border);
                sprite.name = "MaterialEditorDarkToggleMark";
                sprite.hideFlags = HideFlags.HideAndDontSave;
                _darkToggleMarkSprite = sprite;
                return sprite;
            }
            catch
            {
                Object.Destroy(texture);
                throw;
            }
            finally
            {
                RenderTexture.active = previous;
                if (temporary != null)
                    RenderTexture.ReleaseTemporary(temporary);
            }
        }

        internal static void ReapplyTheme(
            MaterialEditorControlStyleState state)
        {
            if (state == null)
                return;

            var selectable = state.GetComponent<Selectable>();
            switch (state.Role)
            {
                case MaterialEditorControlStyleRole.InputField:
                    var inputField = selectable as InputField;
                    ApplyInputField(inputField);
                    break;
                case MaterialEditorControlStyleRole.Toggle:
                    var toggle = selectable as Toggle;
                    ApplyToggle(toggle);
                    break;
                case MaterialEditorControlStyleRole.Slider:
                    ApplySlider(selectable as Slider);
                    break;
            }
        }

        internal static void ApplySlider(Slider slider)
        {
            if (slider == null)
                return;

            MaterialEditorControlStyleState.Assign(
                slider,
                MaterialEditorControlStyleRole.Slider);
            var legacy = MaterialEditorTheme.Mode
                         == MaterialEditorThemeMode.Legacy;
            var legacyColors = ColorBlock.defaultColorBlock;
            MaterialEditorScrollSelectableStyles.ApplySelectable(
                slider,
                legacy
                    ? legacyColors.normalColor
                    : MaterialEditorTheme.Colors.SliderHandle,
                legacy
                    ? legacyColors.highlightedColor
                    : MaterialEditorTheme.Colors.Accent,
                legacy
                    ? legacyColors.pressedColor
                    : MaterialEditorTheme.Colors.SliderHandlePressed,
                legacy ? legacyColors.disabledColor : MaterialEditorTheme.Colors.ControlDisabled);

            var background = slider.transform.Find("Background");
            if (background != null)
            {
                var backgroundImage = background.GetComponent<Image>();
                if (backgroundImage != null)
                    backgroundImage.color = legacy ? Color.white : MaterialEditorTheme.Colors.SliderTrack;
            }
            if (slider.fillRect != null)
            {
                var fillImage = slider.fillRect.GetComponent<Image>();
                if (fillImage != null)
                    fillImage.color = legacy ? Color.white : MaterialEditorTheme.Colors.SliderFill;
            }
            if (slider.handleRect != null)
            {
                var handleImage = slider.handleRect.GetComponent<Image>();
                if (handleImage != null)
                    handleImage.color = MaterialEditorTheme.Colors.TintIdentity;
            }
        }
    }
}
