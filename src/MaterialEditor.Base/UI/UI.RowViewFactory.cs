using UnityEngine;
using UnityEngine.UI;
using static MaterialEditorAPI.MaterialEditorUI;

namespace MaterialEditorAPI
{
    internal static class RowViewFactory
    {
        internal static GameObject CreateTemplate(Transform parent)
        {
            var contentList = MaterialEditorControlFactory.CreatePanel("ListEntry", parent);
            contentList.gameObject.AddComponent<LayoutElement>().preferredHeight = PanelHeight;
            contentList.gameObject.AddComponent<Mask>();
            contentList.color = RowColor;

            RendererRowViewFactory.CreateRows(contentList.transform);
            MaterialShaderRowViewFactory.CreateRows(contentList.transform);
            TextureRowViewFactory.CreateRows(contentList.transform);
            ColorRowViewFactory.CreateRows(contentList.transform);
            FloatKeywordRowViewFactory.CreateRows(contentList.transform);

            RowStyle.Apply(contentList.gameObject);
            RowLayoutCatalog.Apply(contentList.gameObject);
            return contentList.gameObject;
        }
    }
}
