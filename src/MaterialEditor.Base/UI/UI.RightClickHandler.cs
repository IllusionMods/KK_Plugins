using UnityEngine;
using UnityEngine.EventSystems;

namespace MaterialEditorAPI
{
    /// <summary>
    /// Handles left-click and right-click on a UI element without using a Button component,
    /// which avoids layout recalculation side-effects on Text elements.
    /// </summary>
    internal class RightClickHandler : MonoBehaviour, IPointerClickHandler
    {
        public System.Action OnLeftClick;
        public System.Action OnRightClick;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                OnLeftClick?.Invoke();
            else if (eventData.button == PointerEventData.InputButton.Right)
                OnRightClick?.Invoke();
        }
    }
}
