using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Harmony;
using BepInEx.Logging;
using KKAPI.Studio.SaveLoad;
using Studio;
using System.IO;

namespace KK_Plugins
{
    public partial class ImageEmbed
    {
        public const string GUID = "com.deathweasel.bepinex.imageembed";
        public const string PluginName = "Image Embed";
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
            HarmonyWrapper.PatchAll(typeof(Hooks));
        }

        private static void SavePatternTex(OCIItem item, int patternIndex, string filePath)
        {
            if (item?.itemComponent == null) return;
            if (filePath.IsNullOrEmpty()) return;
            if (!File.Exists(filePath)) return;

            foreach (var rend in item.itemComponent.GetRenderers())
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

            foreach (var rend in item.panelComponent.renderer)
                MaterialEditor.MEStudio.GetSceneController().SetMaterialTextureFromFile(item.objectInfo.dicKey, rend.material, "MainTex", filePath);
            item.itemInfo.panel.filePath = "";
        }

        public static ImageEmbedSceneController GetSceneController() => Chainloader.ManagerObject.transform.GetComponentInChildren<ImageEmbedSceneController>();
    }
}
