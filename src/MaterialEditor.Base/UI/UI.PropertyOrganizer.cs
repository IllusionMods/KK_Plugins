using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Linq;
using static MaterialEditorAPI.MaterialEditorPluginBase;

namespace MaterialEditorAPI
{
    internal class PropertyOrganizer
    {
        // Shader, category, property
        internal static Dictionary<string, Dictionary<string, List<ShaderPropertyData>>> PropertyOrganization = new Dictionary<string, Dictionary<string, List<ShaderPropertyData>>>();
        internal static string UncategorizedName = "Uncategorized";
        internal static void Refresh() {
            foreach (var shader in XMLShaderProperties)
            {
                PropertyOrganization[shader.Key] = shader.Value
                    .Where(kv => !kv.Value.Hidden)
                    .GroupBy(kv => string.IsNullOrEmpty(kv.Value.Category) ? UncategorizedName : char.ToUpper(kv.Value.Category[0]) + kv.Value.Category.Substring(1))
                    .OrderBy(g => g.Key == UncategorizedName ? 1 : 0)  // Ensure "Uncategorized" goes to the end
                    .ToDictionary(
                        g => g.Key,
                        g =>
                        {
                            var kvs = g.AsEnumerable();
                            if (SortPropertiesByType.Value && SortPropertiesByName.Value)
                            {
                                kvs = kvs.OrderBy(kv => kv.Value.Type).ThenBy(kv => kv.Value.Name);
                            }
                            else if (SortPropertiesByType.Value)
                            {
                                kvs = kvs.OrderBy(kv => kv.Value.Type);
                            }
                            else if (SortPropertiesByName.Value)
                            {
                                kvs = kvs.OrderBy(kv => kv.Value.Name);
                            }
                            return kvs.Select(kv=>kv.Value).ToList();
                        }
                );

            }
        }
    }
}
