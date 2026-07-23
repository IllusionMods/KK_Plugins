using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MaterialEditorAPI
{
    /// <summary>
    /// Identifies the semantic Material Editor label that was clicked.
    /// </summary>
    public enum MaterialEditorLabelType
    {
        /// <summary>Renderer name.</summary>
        Renderer,
        /// <summary>Material name.</summary>
        Material,
        /// <summary>Currently selected shader name.</summary>
        Shader,
        /// <summary>Shader render queue property.</summary>
        ShaderRenderQueue,
        /// <summary>Texture property name.</summary>
        TextureProperty,
        /// <summary>Texture offset and scale controls.</summary>
        TextureOffsetScale,
        /// <summary>Color property name.</summary>
        ColorProperty,
        /// <summary>Float property name.</summary>
        FloatProperty,
        /// <summary>Keyword property name.</summary>
        KeywordProperty
    }

    /// <summary>
    /// Context supplied to Material Editor label click handlers.
    /// </summary>
    public sealed class MaterialEditorLabelClickEventArgs : EventArgs
    {
        /// <summary>
        /// Type of label that was clicked.
        /// </summary>
        public MaterialEditorLabelType LabelType { get; }

        /// <summary>
        /// Renderer, material, shader, or property name represented by the label.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Root object currently being edited.
        /// </summary>
        public GameObject GameObject { get; }

        /// <summary>
        /// Opaque data object supplied when the Material Editor UI was populated.
        /// </summary>
        public object Data { get; }

        /// <summary>
        /// Renderer represented by the label, when applicable.
        /// </summary>
        public Renderer Renderer { get; }

        /// <summary>
        /// Material represented by the label or owning the clicked property, when applicable.
        /// </summary>
        public Material Material { get; }

        /// <summary>
        /// Projector owning the clicked property, when applicable.
        /// </summary>
        public Projector Projector { get; }

        /// <summary>
        /// Unity pointer event for the click, including the mouse button and pointer position.
        /// </summary>
        public PointerEventData PointerEventData { get; }

        /// <summary>
        /// Whether either Shift key was held when the label was clicked.
        /// </summary>
        public bool ShiftPressed { get; }

        /// <summary>
        /// Whether either Control key was held when the label was clicked.
        /// </summary>
        public bool ControlPressed { get; }

        /// <summary>
        /// Whether either Alt key was held when the label was clicked.
        /// </summary>
        public bool AltPressed { get; }

        internal MaterialEditorLabelClickEventArgs(
            MaterialEditorLabelType labelType,
            string name,
            GameObject gameObject,
            object data,
            Renderer renderer,
            Material material,
            Projector projector,
            PointerEventData pointerEventData)
        {
            LabelType = labelType;
            Name = name ?? string.Empty;
            GameObject = gameObject;
            Data = data;
            Renderer = renderer;
            Material = material;
            Projector = projector;
            PointerEventData = pointerEventData;
            ShiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            ControlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            AltPressed = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        }
    }

    internal sealed class LabelClickTrigger : MonoBehaviour, IPointerClickHandler
    {
        internal Action<PointerEventData> Clicked;

        public void OnPointerClick(PointerEventData eventData)
        {
            Clicked?.Invoke(eventData);
        }
    }
}
