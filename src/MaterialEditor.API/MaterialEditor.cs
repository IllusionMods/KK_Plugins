using BepInEx;

namespace MaterialEditorAPI
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    internal class MaterialEditorPlugin :  MaterialEditorPluginBase
    {
        /// <summary>
        /// MaterialEditor plugin GUID
        /// </summary>
        public const string PluginGUID = "MaterialEditor";
        /// <summary>
        /// MaterialEditor plugin name
        /// </summary>
        public const string PluginName = "Material Editor";
        /// <summary>
        /// MaterialEditor plugin version
        /// </summary>
        public const string PluginVersion = "1.0";
    }
}
