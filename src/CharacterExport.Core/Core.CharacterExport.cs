using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.IO;
using UnityEngine;
#if AI || HS2
using AIChara;
#endif

namespace KK_Plugins
{
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class CharacterExport : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.characterexport";
        public const string PluginName = "Character Export";
        public const string PluginNameInternal = Constants.Prefix + "_CharacterExport";
        public const string Version = "1.0";
        internal static new ManualLogSource Logger;
        public static readonly string ExportPath = Path.Combine(Paths.GameRootPath, @"UserData\chara\export");

        public static ConfigEntry<KeyboardShortcut> CharacterExportHotkey { get; private set; }
        public static ConfigEntry<bool> OpenFolderAfterExport { get; private set; }

        private void Start()
        {
            Logger = base.Logger;

            CharacterExportHotkey = Config.Bind("Keyboard Shortcuts", "Export Characters", new KeyboardShortcut(KeyCode.E, KeyCode.LeftControl), "Export all currently loaded characters in the game.");
            OpenFolderAfterExport = Config.Bind("Config", "Open Folder After Export", true, "Whether to open the folder after exporting characters.");
        }

        private void Update()
        {
            if (CharacterExportHotkey.Value.IsDown())
                ExportCharacters();
        }

        /// <summary>
        /// Exports all currently loaded characters. Probably wont export characters that have not been loaded yet, like characters in a different classroom.
        /// </summary>
        public void ExportCharacters()
        {
            int counter = 0;
            var charas = FindObjectsOfType<ChaControl>();
            string filenamePrefix = Path.Combine(ExportPath, $"_CharacterExport_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}");
            bool openedFile = false;

            for (; counter < charas.Length; counter++)
            {
                string sex = charas[counter].chaFile.parameter.sex == 0 ? "Male" : "Female";
                string filename = $"{filenamePrefix}_{counter:00}_{sex}.png";
#if AI || EC || HS2
                Traverse.Create(charas[counter].chaFile).Method("SaveFile", filename, 0).GetValue();
#elif KK
                Traverse.Create(charas[counter].chaFile).Method("SaveFile", filename).GetValue();
#else
                Logger.LogError($"Exporting not yet implemented");
#endif

                if (!openedFile)
                {
                    if (OpenFolderAfterExport.Value)
                        CC.OpenFileInExplorer(filename);
                    openedFile = true;
                }
            }

            string s = counter == 1 ? "" : "s";
            Logger.Log(BepInEx.Logging.LogLevel.Info | BepInEx.Logging.LogLevel.Message, $"Exported {counter} character{s}.");
        }
    }
}