using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.EventSystems;

namespace MaterialEditorAPI
{
    internal class Tooltip : UIBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Action<PointerEventData> onPointerEnter;
        public Action<PointerEventData> onPointerExit;

        public string TooltipText;

        public override void Start()
        {
            onPointerEnter = (e) => { TooltipManager.SetToolTipText(TooltipText, true); };
            onPointerExit = (e) => { TooltipManager.SetActive(false); };
        }

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
