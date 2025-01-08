using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.EventSystems;

namespace MaterialEditorAPI
{
    internal class PointerEnterHandler : UIBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Action<PointerEventData> onPointerEnter;
        public Action<PointerEventData> onPointerExit;

        public void OnPointerEnter(PointerEventData eventData)
        {
            onPointerEnter?.Invoke(eventData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            onPointerExit?.Invoke(eventData);
        }
    }
}
