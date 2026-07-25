using System;
using System.Xml;
using KKAPI;
using MaterialEditorAPI;
using UnityEngine;

namespace KK_Plugins.MaterialEditor
{
    public partial class MaterialEditorPlugin
    {
        private static ShaderTooltipCatalog LoadTooltipCatalogs(
            XmlElement materialEditorElement,
            string sourceId)
        {
            var merged = new ShaderTooltipCatalog();
            foreach (XmlNode child in materialEditorElement.ChildNodes)
            {
                var catalogElement = child as XmlElement;
                if (catalogElement == null || catalogElement.Name != "TooltipCatalog")
                    continue;

                var assetBundlePath = catalogElement.GetAttribute("AssetBundle");
                var assetName = catalogElement.GetAttribute("Asset");
                if (assetBundlePath.IsNullOrEmpty() || assetName.IsNullOrEmpty())
                {
                    Logger.LogWarning(
                        $"Tooltip catalog in '{sourceId}' requires AssetBundle and Asset.");
                    continue;
                }

                try
                {
                    var text = LoadTooltipCatalogText(assetBundlePath, assetName);
                    if (text.IsNullOrEmpty())
                    {
                        Logger.LogWarning(
                            $"Unable to load Material Editor tooltip catalog "
                            + $"'{assetName}' from '{assetBundlePath}' in '{sourceId}'.");
                        continue;
                    }
                    merged.Merge(
                        ShaderTooltipCatalogParser.Parse(
                            text,
                            warning => Logger.LogWarning(
                                $"Tooltip catalog '{assetName}' in '{sourceId}': {warning}")));
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(
                        $"Failed to load Material Editor tooltip catalog "
                        + $"'{assetName}' from '{sourceId}': {ex}");
                }
            }
            return merged;
        }

        private static string LoadTooltipCatalogText(
            string assetBundlePath,
            string assetName)
        {
            if (assetBundlePath.StartsWith("Resources."))
            {
                var bundle = AssetBundle.LoadFromMemory(
                    UILib.Resource.LoadEmbeddedResource(
                        $"{nameof(KK_Plugins)}.{assetBundlePath}"));
                if (bundle == null)
                    return null;
                try
                {
                    var textAsset = bundle.LoadAsset<TextAsset>(assetName);
                    return textAsset == null ? null : textAsset.text;
                }
                finally
                {
                    bundle.Unload(false);
                }
            }

#if PH
            var fileBundle = AssetBundle.LoadFromFile(assetBundlePath);
            if (fileBundle == null)
                return null;
            try
            {
                var textAsset = fileBundle.LoadAsset<TextAsset>(assetName);
                return textAsset == null ? null : textAsset.text;
            }
            finally
            {
                fileBundle.Unload(false);
            }
#else
            var asset = CommonLib.LoadAsset<TextAsset>(
                assetBundlePath,
                assetName);
            return asset == null ? null : asset.text;
#endif
        }
    }
}
