using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal class Tooltip : UIBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IBeginDragHandler,
        IEndDragHandler
    {
        private ShaderHintUnderline _hintUnderline;

        internal string StandardTooltipText { get; private set; }
        internal string ShaderHintText { get; private set; }
        internal bool IsHovered { get; private set; }
        internal bool InteractionSuppressed { get; private set; }
        internal bool HasStandardTooltip =>
            !string.IsNullOrEmpty(StandardTooltipText);
        internal bool HasShaderHint =>
            !string.IsNullOrEmpty(ShaderHintText);

        internal void Configure(
            string standardTooltipText,
            string shaderHintText,
            Text hintLabel)
        {
            StandardTooltipText = standardTooltipText ?? string.Empty;
            ShaderHintText = shaderHintText ?? string.Empty;

            if (HasShaderHint && hintLabel != null)
                _hintUnderline = ShaderHintUnderline.GetOrCreate(hintLabel);
            if (_hintUnderline != null && !HasShaderHint)
                _hintUnderline.SetVisible(false);

            enabled = HasStandardTooltip || HasShaderHint;
            TooltipManager.NotifyTooltipChanged(this);
        }

        internal void SetStandardTooltipText(string text)
        {
            Configure(text, ShaderHintText, null);
        }

        internal void SetHintIndicatorVisible(bool visible)
        {
            if (_hintUnderline != null)
                _hintUnderline.SetVisible(visible && HasShaderHint);
        }

        public override void OnEnable()
        {
            base.OnEnable();
            TooltipManager.Register(this);
        }

        public override void OnDisable()
        {
            IsHovered = false;
            InteractionSuppressed = false;
            SetHintIndicatorVisible(false);
            TooltipManager.Unregister(this);
            base.OnDisable();
        }

        public override void OnDestroy()
        {
            TooltipManager.Unregister(this);
            base.OnDestroy();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            IsHovered = true;
            TooltipManager.PointerStateChanged(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            IsHovered = false;
            InteractionSuppressed = false;
            TooltipManager.PointerStateChanged(this);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            InteractionSuppressed = true;
            TooltipManager.PointerStateChanged(this);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            InteractionSuppressed = false;
            TooltipManager.PointerStateChanged(this);
        }
    }
}
