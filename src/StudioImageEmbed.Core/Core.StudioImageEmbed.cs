using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using KKAPI;
using KKAPI.Studio.SaveLoad;
using Studio;
using System.IO;
using static KK_Plugins.ImageEmbedConstants;

namespace KK_Plugins
{
    [BepInProcess(Constants.StudioProcessName)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInDependency(MaterialEditor.MaterialEditorPlugin.PluginGUID, MaterialEditor.MaterialEditorPlugin.PluginVersion)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class ImageEmbed : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.studioimageembed";
        public const string PluginName = "Image Embed";
        public const string PluginNameInternal = Constants.Prefix + "_ImageEmbed";
        public const string Version = "1.0";
        internal static new ManualLogSource Logger;

        public static ConfigEntry<bool> SavePattern { get; private set; }
        public static ConfigEntry<bool> SaveBG { get; private set; }
        public static ConfigEntry<bool> SaveFrame { get; private set; }

        internal void Main()
        {
            Logger = base.Logger;

            SavePattern = Config.Bind("Config", "Save pattern images to scene data", true, new ConfigDescription("Whether images from the userdata/pattern folder will be saved to scene data. False is vanilla behavior and such images can only be loaded if the same image exists on disk.", null, new ConfigurationManagerAttributes { Order = 2 }));
            SaveBG = Config.Bind("Config", "Save BG images to scene data", true, new ConfigDescription("Whether images from the userdata/bg folder folder will be saved to scene data. False is vanilla behavior and such images can only be loaded if the same image exists on disk.", null, new ConfigurationManagerAttributes { Order = 1 }));
            SaveFrame = Config.Bind("Config", "Save frame images to scene data", true, new ConfigDescription("Whether images from the userdata/frame folder folder will be saved to scene data. False is vanilla behavior and such images can only be loaded if the same image exists on disk.", null, new ConfigurationManagerAttributes { Order = 1 }));

            StudioSaveLoadApi.RegisterExtraBehaviour<ImageEmbedSceneController>(GUID);
            Harmony.CreateAndPatchAll(typeof(Hooks));
        }

        private static void SavePatternTex(OCIItem item, int patternIndex, string filePath)
        {
            if (item?.itemComponent == null) return;
            if (filePath.IsNullOrEmpty()) return;
            if (!File.Exists(filePath)) return;

            foreach (var rend in MaterialEditorAPI.MaterialAPI.GetRendererList(item.objectItem))
                MaterialEditor.MEStudio.GetSceneController().SetMaterialTextureFromFile(item.objectInfo.dicKey, rend.material, $"PatternMask{patternIndex + 1}", filePath);
            item.SetPatternPath(patternIndex, "");
        }

        private static void SaveBGTex(OCIItem item, string filePath)
        {
            if (item?.panelComponent == null) return;
            if (filePath.IsNullOrEmpty()) return;
            if (!File.Exists(filePath)) return;

            string file = Path.GetFileName(filePath);
            if (DefaultBGs.Contains(file.ToLower())) return;

            for (var i = 0; i < item.panelComponent.renderer.Length; i++)
                MaterialEditor.MEStudio.GetSceneController().SetMaterialTextureFromFile(item.objectInfo.dicKey, item.panelComponent.renderer[i].material, "MainTex", filePath);
            item.itemInfo.panel.filePath = "";
        }

        public static ImageEmbedSceneController GetSceneController() => Chainloader.ManagerObject.transform.GetComponentInChildren<ImageEmbedSceneController>();
    }
}
