using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace MaterialEditorAPI
{
    internal sealed class ShaderUiMetadata
    {
        internal string TooltipText;
        internal readonly Dictionary<string, string> CategoryTooltips =
            new Dictionary<string, string>(StringComparer.Ordinal);
        internal readonly Dictionary<string, string> PropertyTooltips =
            new Dictionary<string, string>(StringComparer.Ordinal);

        internal ShaderUiMetadata Clone()
        {
            var clone = new ShaderUiMetadata { TooltipText = TooltipText };
            foreach (var item in CategoryTooltips)
                clone.CategoryTooltips[item.Key] = item.Value;
            foreach (var item in PropertyTooltips)
                clone.PropertyTooltips[item.Key] = item.Value;
            return clone;
        }

        internal void Merge(ShaderUiMetadata other)
        {
            if (other == null)
                return;
            if (!string.IsNullOrEmpty(other.TooltipText))
                TooltipText = other.TooltipText;
            foreach (var item in other.CategoryTooltips)
                CategoryTooltips[item.Key] = item.Value;
            foreach (var item in other.PropertyTooltips)
                PropertyTooltips[item.Key] = item.Value;
        }
    }

    internal sealed class ShaderTooltipCatalog
    {
        private readonly Dictionary<string, ShaderUiMetadata> _shaders =
            new Dictionary<string, ShaderUiMetadata>(StringComparer.Ordinal);

        internal IEnumerable<KeyValuePair<string, ShaderUiMetadata>> Shaders => _shaders;

        internal ShaderUiMetadata GetShader(string shaderName)
        {
            ShaderUiMetadata metadata;
            return shaderName != null && _shaders.TryGetValue(shaderName, out metadata)
                ? metadata
                : null;
        }

        internal void SetShader(string shaderName, ShaderUiMetadata metadata)
        {
            if (!string.IsNullOrEmpty(shaderName) && metadata != null)
                _shaders[shaderName] = metadata;
        }

        internal void Merge(ShaderTooltipCatalog other)
        {
            if (other == null)
                return;
            foreach (var item in other.Shaders)
            {
                ShaderUiMetadata current;
                if (!_shaders.TryGetValue(item.Key, out current))
                {
                    _shaders[item.Key] = item.Value.Clone();
                    continue;
                }
                current.Merge(item.Value);
            }
        }
    }

    internal static class ShaderTooltipCatalogParser
    {
        internal static ShaderTooltipCatalog Parse(
            string xml,
            Action<string> warning = null)
        {
            if (string.IsNullOrEmpty(xml) || xml.Trim().Length == 0)
                throw new ArgumentException("Tooltip catalog is empty.", nameof(xml));

            var document = new XmlDocument();
            document.LoadXml(xml);
            var root = document.DocumentElement;
            if (root == null || root.Name != "MaterialEditorTooltips")
                throw new FormatException(
                    "Tooltip catalog root must be MaterialEditorTooltips.");
            if (root.GetAttribute("SchemaVersion") != "1")
                throw new FormatException(
                    "Tooltip catalog SchemaVersion must be 1.");

            var sets = ReadTooltipSets(root, warning);
            var catalog = new ShaderTooltipCatalog();
            foreach (var shaderElement in ChildElements(root, "Shader"))
            {
                var shaderName = shaderElement.GetAttribute("Name");
                if (string.IsNullOrEmpty(shaderName))
                    continue;

                var metadata = new ShaderUiMetadata();
                foreach (var useElement in ChildElements(shaderElement, "UseTooltipSet"))
                {
                    var reference = useElement.GetAttribute("Ref");
                    ShaderUiMetadata set;
                    if (sets.TryGetValue(reference, out set))
                        metadata.Merge(set);
                    else if (!string.IsNullOrEmpty(reference))
                        warning?.Invoke($"TooltipSet '{reference}' was not found.");
                }

                var shaderTooltip = FirstChild(shaderElement, "Tooltip");
                if (shaderTooltip != null)
                    metadata.TooltipText = ReadText(shaderTooltip);

                ReadNamedTooltips(
                    shaderElement,
                    "Category",
                    metadata.CategoryTooltips,
                    metadata,
                    warning);
                ReadNamedTooltips(
                    shaderElement,
                    "Property",
                    metadata.PropertyTooltips,
                    metadata,
                    warning);
                catalog.SetShader(shaderName, metadata);
            }
            return catalog;
        }

        private static Dictionary<string, ShaderUiMetadata> ReadTooltipSets(
            XmlElement root,
            Action<string> warning)
        {
            var sets = new Dictionary<string, ShaderUiMetadata>(StringComparer.Ordinal);
            foreach (var setElement in ChildElements(root, "TooltipSet"))
            {
                var id = setElement.GetAttribute("Id");
                if (string.IsNullOrEmpty(id))
                    continue;
                var metadata = new ShaderUiMetadata();
                ReadNamedTooltips(
                    setElement,
                    "Category",
                    metadata.CategoryTooltips,
                    metadata,
                    warning);
                ReadNamedTooltips(
                    setElement,
                    "Property",
                    metadata.PropertyTooltips,
                    metadata,
                    warning);
                sets[id] = metadata;
            }
            return sets;
        }

        private static void ReadNamedTooltips(
            XmlElement parent,
            string elementName,
            IDictionary<string, string> destination,
            ShaderUiMetadata resolved,
            Action<string> warning)
        {
            foreach (var element in ChildElements(parent, elementName))
            {
                var name = element.GetAttribute("Name");
                if (string.IsNullOrEmpty(name))
                    continue;

                var text = ReadText(element);
                var reference = element.GetAttribute("Ref");
                if (string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(reference))
                {
                    var resolvedReference = elementName == "Category"
                        ? resolved.CategoryTooltips.TryGetValue(reference, out text)
                        : resolved.PropertyTooltips.TryGetValue(reference, out text);
                    if (!resolvedReference)
                        warning?.Invoke(
                            $"{elementName} tooltip reference '{reference}' was not found.");
                }
                if (!string.IsNullOrEmpty(text))
                    destination[name] = text;
            }
        }

        private static IEnumerable<XmlElement> ChildElements(
            XmlElement parent,
            string name)
        {
            foreach (XmlNode child in parent.ChildNodes)
            {
                var element = child as XmlElement;
                if (element != null && element.Name == name)
                    yield return element;
            }
        }

        private static XmlElement FirstChild(XmlElement parent, string name)
        {
            foreach (var element in ChildElements(parent, name))
                return element;
            return null;
        }

        private static string ReadText(XmlElement element)
        {
            var lines = (element.InnerText ?? string.Empty)
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Split('\n');
            var result = new StringBuilder();
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0)
                    continue;
                if (result.Length > 0)
                    result.Append('\n');
                result.Append(trimmed);
            }
            return result.ToString();
        }
    }

    internal static class ShaderUiMetadataRegistry
    {
        private static readonly Dictionary<string, ShaderUiMetadata> Shaders =
            new Dictionary<string, ShaderUiMetadata>(StringComparer.Ordinal);

        internal static void SetShader(string shaderName, ShaderUiMetadata metadata)
        {
            if (string.IsNullOrEmpty(shaderName))
                return;
            if (metadata == null)
                Shaders.Remove(shaderName);
            else
                Shaders[shaderName] = metadata.Clone();
        }

        internal static string GetShaderTooltip(string shaderName)
        {
            var metadata = GetShader(shaderName);
            return metadata?.TooltipText;
        }

        internal static string GetCategoryTooltip(string shaderName, string categoryName)
        {
            var metadata = GetShader(shaderName);
            string tooltip;
            return metadata != null
                   && categoryName != null
                   && metadata.CategoryTooltips.TryGetValue(categoryName, out tooltip)
                ? tooltip
                : null;
        }

        internal static string GetPropertyTooltip(string shaderName, string propertyName)
        {
            var metadata = GetShader(shaderName);
            string tooltip;
            return metadata != null
                   && propertyName != null
                   && metadata.PropertyTooltips.TryGetValue(propertyName, out tooltip)
                ? tooltip
                : null;
        }

        private static ShaderUiMetadata GetShader(string shaderName)
        {
            ShaderUiMetadata metadata;
            return shaderName != null && Shaders.TryGetValue(shaderName, out metadata)
                ? metadata
                : null;
        }
    }
}
