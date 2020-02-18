#if !HS

using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.AssetRedirection;
using XUnity.AutoTranslator.Plugin.Core.Utilities;
using XUnity.ResourceRedirector;

namespace KK_Plugins
{
    using BepinLogLevel = BepInEx.Logging.LogLevel;
    public class TextAssetResourceRedirector : AssetLoadedHandlerBaseV2<TextAsset>
    {
        private readonly TextAssetHelper textAssetHelper;

        private static readonly object replacementSync = new object();
        private static readonly Dictionary<string, string> replacements = new Dictionary<string, string>();
        private static readonly Dictionary<string, List<WeakReference>> replacementsTracker = new Dictionary<string, List<WeakReference>>();

        protected static ManualLogSource Logger => TextResourceRedirector.Logger;

        public bool Enabled => textAssetHelper?.Enabled ?? false;

        public TextAssetResourceRedirector(TextAssetHelper textAssetHelper)
        {
            CheckDirectory = true;
            this.textAssetHelper = textAssetHelper;
            if (Enabled)
            {
                Logger.Log(BepinLogLevel.Info, $"{this.GetType()} enabled");
                SceneManager.sceneLoaded += this.SceneManager_sceneLoaded;
                TextAssetPatcher.PatchTextAsset();
            }
            else
            {
                Logger.Log(BepinLogLevel.Warning, $"{this.GetType()} disabled");
            }
        }

        protected override string CalculateModificationFilePath(TextAsset asset, IAssetOrResourceLoadedContext context)
        {
            return context.GetPreferredFilePathWithCustomFileName(asset, null).Replace(".unity3d", "");
        }

        protected override bool DumpAsset(string calculatedModificationPath, TextAsset asset, IAssetOrResourceLoadedContext context)
        {
            if (!Enabled || !textAssetHelper.IsTable(asset))
            {
                return false;
            }

            var defaultTranslationFile = Path.Combine(calculatedModificationPath, "translation.txt");
            var redirectedResources = RedirectedDirectory.GetFilesInDirectory(calculatedModificationPath, ".txt");
            var streams = redirectedResources.Select(x => x.OpenStream());
            var cache = new SimpleTextTranslationCache(
               outputFile: defaultTranslationFile,
               inputStreams: streams,
               allowTranslationOverride: false,
               closeStreams: true);

            bool DumpCell(string cellText)
            {
                if (!string.IsNullOrEmpty(cellText) && LanguageHelper.IsTranslatable(cellText))
                {
                    cache.AddTranslationToCache(cellText, cellText);
                    return true;
                }
                return false;
            }

            return textAssetHelper.ActOnCells(asset, DumpCell, out TextAssetHelper.TableResult tableResult);
        }

        protected override bool ReplaceOrUpdateAsset(string calculatedModificationPath, ref TextAsset asset, IAssetOrResourceLoadedContext context)
        {
            if (!Enabled || !textAssetHelper.IsTable(asset))
            {
                return false;
            }

            var defaultTranslationFile = Path.Combine(calculatedModificationPath, "translation.txt");
            var redirectedResources = RedirectedDirectory.GetFilesInDirectory(calculatedModificationPath, ".txt");
            var streams = redirectedResources.Select(x => x.OpenStream());
            var cache = new SimpleTextTranslationCache(
               outputFile: defaultTranslationFile,
               inputStreams: streams,
               allowTranslationOverride: false,
               closeStreams: true);

            return TryRegisterTranslation(cache, ref asset);
        }

        private bool TryRegisterTranslation(SimpleTextTranslationCache cache, ref TextAsset textAsset)
        {
            string TranslateCell(string cellText)
            {
                if (cache.TryGetTranslation(cellText, false, out string newText))
                {
                    return newText;
                }
                else
                {
                    if (AutoTranslatorSettings.IsDumpingRedirectedResourcesEnabled)
                    {
                        if (!string.IsNullOrEmpty(cellText) && LanguageHelper.IsTranslatable(cellText))
                        {
                            cache.AddTranslationToCache(cellText, cellText);
                        }
                    }
                }
                return null;
            }

            string result = textAssetHelper.ProcessTable(textAsset, TranslateCell, out TextAssetHelper.TableResult tableResult);
            Logger.Log(BepinLogLevel.Debug, $"{this.GetType()}: {tableResult.RowsUpdated}/{tableResult.Rows} rows updated");
            if (tableResult.RowsUpdated > 0)
            {
                RegisterReplacement(textAsset, result);
                return true;
            }
            return false;
        }

        public static bool TryLoadReplacement(string orig, out string result)
        {
            return replacements.TryGetValue(orig, out result);
        }

        protected override bool ShouldHandleAsset(TextAsset asset, IAssetOrResourceLoadedContext context)
        {
            return Enabled && textAssetHelper.IsTable(asset) && !context.HasReferenceBeenRedirectedBefore(asset);
        }

        private static void RegisterReplacement(TextAsset textAsset, string replacement)
        {
            // Direct references to textAsset.text increments the TextAsset reference count
            // preventing it from being garbage collected, so make a copy
            string key = string.Copy(textAsset.TextGetterOrig());
            lock (replacementSync)
            {
                replacements[key] = replacement;

                WeakReference taRef = new WeakReference(textAsset);

                if (!replacementsTracker.ContainsKey(key))
                {
                    replacementsTracker.Add(key, new List<WeakReference>());
                }
                replacementsTracker[key].Add(taRef);
            }
        }

        private static void CleanupReplacements()
        {
            if (replacements.Count > 0)
            {
                int releasedCount = 0;
                lock (replacementSync)
                {
                    foreach (string key in replacementsTracker.Keys.ToArray())
                    {
                        replacementsTracker[key].RemoveAll((m) => !m.IsAlive);
                        if (replacementsTracker[key].Count == 0)
                        {
                            replacements.Remove(key);
                            replacementsTracker.Remove(key);
                            releasedCount++;
                        }
                    }
                }
                Logger.Log(BepinLogLevel.Debug, $"Released replacements for {releasedCount} unloaded TextAsset(s) (now loaded: {replacements.Count})");
            }
        }

        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            CleanupReplacements();
        }
    }
}

#else //Stub for HS
namespace KK_Plugins
{
    public class TextAssetResourceRedirector
    {
        public TextAssetResourceRedirector(TextAssetHelper textAssetHelper) { }
    }
}
#endif