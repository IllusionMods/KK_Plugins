using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static MaterialEditorAPI.MaterialEditorUI;

namespace MaterialEditorAPI
{
    /// <summary>
    /// Sticky section bars that sit just below the ME title bar.
    /// Uses scroll-position math (not world-space) to decide which bar is visible.
    /// Each bar mirrors the real section header and includes a working collapse button.
    /// </summary>
    internal class StickySectionBar : MonoBehaviour
    {
        public ScrollRect ScrollRect;

        public Image RendererBar;
        public Text  RendererBarText;
        public Button RendererBarCollapseButton;

        public Image MaterialBar;
        public Text  MaterialBarText;
        public Button MaterialBarCollapseButton;

        //Set by PopulateList after each rebuild
        public int RendererSectionIndex = 0;
        public int MaterialSectionIndex = -1;
        public int TotalItemCount = 0;

        private void LateUpdate()
        {
            if (ScrollRect == null || RendererBar == null || MaterialBar == null) return;
            if (TotalItemCount == 0 || MaterialSectionIndex < 0) { HideBoth(); return; }

            //The virtual list simulates scrolling by adjusting padding.top rather than moving rows
            float scrollY = Mathf.Max(0f, ScrollRect.content.localPosition.y);
            int topItemIndex = Mathf.FloorToInt(scrollY / PanelHeight);

            bool rendererPast = topItemIndex > RendererSectionIndex;
            bool materialPast = topItemIndex >= MaterialSectionIndex;

            if (materialPast)
            {
                RendererBar.gameObject.SetActive(false);
                MaterialBar.gameObject.SetActive(true);
            }
            else if (rendererPast)
            {
                RendererBar.gameObject.SetActive(true);
                MaterialBar.gameObject.SetActive(false);
            }
            else
            {
                HideBoth();
            }
        }

        private void HideBoth()
        {
            RendererBar.gameObject.SetActive(false);
            MaterialBar.gameObject.SetActive(false);
        }

        /// <summary>
        /// Updates section indices and label text. Call from PopulateList after rebuilding the item list.
        /// </summary>
        public void UpdateLayout(int materialSectionIndex, int totalItemCount,
                                 string rendererText, string materialText,
                                 bool rendererCollapsed, bool materialCollapsed)
        {
            MaterialSectionIndex = materialSectionIndex;
            TotalItemCount       = totalItemCount;

            if (RendererBarText != null)
                RendererBarText.text = rendererText;
            if (MaterialBarText != null)
                MaterialBarText.text = materialText;

            //Mirror the collapse button state of the real section headers
            if (RendererBarCollapseButton != null)
                RendererBarCollapseButton.GetComponentInChildren<Text>().text = rendererCollapsed ? "+" : "-";
            if (MaterialBarCollapseButton != null)
                MaterialBarCollapseButton.GetComponentInChildren<Text>().text = materialCollapsed ? "+" : "-";
        }
    }
}
