// Themes the dropdown scrollbar when its template activates.
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal class DropdownThemer : MonoBehaviour
    {
        public static void Apply(Dropdown dropdown) { }

        private void OnEnable()
        {
            bool dark = MaterialEditorPluginBase.DarkMode.Value;
            var trackColor  = dark ? new Color(0.30f, 0.30f, 0.33f, 1f) : new Color(0.45f, 0.45f, 0.48f, 1f);
            var handleColor = dark ? new Color(0.65f, 0.65f, 0.70f, 1f) : new Color(0.20f, 0.20f, 0.22f, 1f);
            foreach (var sb in GetComponentsInChildren<Scrollbar>(true))
            {
                var track = sb.GetComponent<Image>();
                if (track != null) track.color = trackColor;
                var handle = sb.handleRect?.GetComponent<Image>();
                if (handle != null) handle.color = handleColor;
            }
        }
    }
}
