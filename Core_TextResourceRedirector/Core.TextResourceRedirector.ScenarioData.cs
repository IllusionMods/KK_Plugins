#if !HS
using ADV;
using System;
using System.IO;
using System.Linq;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.AssetRedirection;
using XUnity.AutoTranslator.Plugin.Core.Utilities;
using XUnity.ResourceRedirector;

namespace KK_Plugins
{
    public class ScenarioDataResourceRedirector : AssetLoadedHandlerBaseV2<ScenarioData>
    {
        private readonly TextResourceHelper textResourceHelper;

        public ScenarioDataResourceRedirector(TextResourceHelper helper)
        {
            CheckDirectory = true;
            textResourceHelper = helper;
        }

        protected override string CalculateModificationFilePath(ScenarioData asset, IAssetOrResourceLoadedContext context) =>
            context.GetPreferredFilePathWithCustomFileName(asset, null).Replace(".unity3d", "");

        protected override bool DumpAsset(string calculatedModificationPath, ScenarioData asset, IAssetOrResourceLoadedContext context)
        {
            var defaultTranslationFile = Path.Combine(calculatedModificationPath, "translation.txt");
            var cache = new SimpleTextTranslationCache(
               file: defaultTranslationFile,
               loadTranslationsInFile: false);

            foreach (var param in asset.list)
            {
                if (!textResourceHelper.IsSupportedCommand(param.Command))
                {
                    continue;
                }

                if (param.Command == Command.Text)
                {
                    for (int i = 0; i < param.Args.Length; i++)
                    {
                        var key = param.Args[i];

                        if (!string.IsNullOrEmpty(key) && !textResourceHelper.TextKeysBlacklist.Contains(key) && LanguageHelper.IsTranslatable(key))
                        {
                            cache.AddTranslationToCache(key, key);
                        }
                    }
                }
                else if (param.Command == Command.Calc)
                {
                    if (param.Args.Length >= 3 && textResourceHelper.CalcKeys.Contains(param.Args[0]))
                    {
                        cache.AddTranslationToCache(param.Args[2], param.Args[2]);
                    }
                }
                else if (param.Command == Command.Format)
                {
                    if (param.Args.Length >= 2 && textResourceHelper.FormatKeys.Contains(param.Args[0]))
                    {
                        cache.AddTranslationToCache(param.Args[1], param.Args[1]);
                    }
                }
                else if (param.Command == Command.Choice)
                {
                    for (int i = 0; i < param.Args.Length; i++)
                    {
                        var key = textResourceHelper.GetSpecializedKey(param, i, out string value);

                        if (!key.IsNullOrEmpty())
                        {
                            cache.AddTranslationToCache(key, value);
                        }
                    }
                }
#if false
                else if (param.Command == ADV.Command.Switch)
                {
                    for (int i = 1; i < param.Args.Length; i += 1)
                    {
                        cache.AddTranslationToCache(param.Args[i], param.Args[i]);
                    }
                }
#endif
#if false
                else if (param.Command == ADV.Command.InfoText)
                {
                    for (int i = 1; i < param.Args.Length; i += 1)
                    {
                        cache.AddTranslationToCache(param.Args[i], param.Args[i]);
                    }
                }
#endif
#if false
                else if (param.Command == ADV.Command.Jump)
                {
                    // TODO: detect if should be dumped
                    if (param.Args.Length >= 1)
                    {
                       cache.AddTranslationToCache(param.Args[0], param.Args[0]);
                    }
                }
#endif

            }

            return true;
        }

        protected override bool ReplaceOrUpdateAsset(string calculatedModificationPath, ref ScenarioData asset, IAssetOrResourceLoadedContext context)
        {
            var defaultTranslationFile = Path.Combine(calculatedModificationPath, "translation.txt");
            var redirectedResources = RedirectedDirectory.GetFilesInDirectory(calculatedModificationPath, ".txt");
            var streams = redirectedResources.Select(x => x.OpenStream());
            var cache = new SimpleTextTranslationCache(
               outputFile: defaultTranslationFile,
               inputStreams: streams,
               allowTranslationOverride: false,
               closeStreams: true);

            foreach (var param in asset.list)
            {
                if (!textResourceHelper.IsSupportedCommand(param.Command))
                {
                    continue;
                }

                if (param.Command == Command.Text)
                {
                    for (int i = 0; i < param.Args.Length; i++)
                    {
                        var key = param.Args[i];
                        if (!key.IsNullOrEmpty() && !textResourceHelper.TextKeysBlacklist.Contains(key))
                        {
                            TryRegisterTranslation(cache, param, i);
                        }
                    }
                }
                else if (param.Command == Command.Calc)
                {
                    if (param.Args.Length >= 3 && textResourceHelper.CalcKeys.Contains(param.Args[0]))
                    {
                        TryRegisterTranslation(cache, param, 2);
                    }
                }
                else if (param.Command == Command.Format)
                {
                    if (param.Args.Length >= 2 && textResourceHelper.FormatKeys.Contains(param.Args[0]))
                    {
                        TryRegisterTranslation(cache, param, 1);
                    }
                }
                else if (param.Command == Command.Choice)
                {
                    for (int i = 0; i < param.Args.Length; i++)
                    {
                        TryRegisterTranslation(cache, param, i);
                    }
                }
#if false
                else if (param.Command == ADV.Command.Switch)
                {
                    // TODO
                }
#endif
#if false
                else if (param.Command == ADV.Command.InfoText)
                {
                    // TODO
                }
#endif
#if false
                else if (param.Command == ADV.Command.Jump)
                {
                    // TODO
                }
#endif

            }

            return true;
        }

        private bool TryRegisterTranslation(SimpleTextTranslationCache cache, ScenarioData.Param param, int i)
        {
            var key = textResourceHelper.GetSpecializedKey(param, i, out string value);
            if (!string.IsNullOrEmpty(key))
            {
                if (cache.TryGetTranslation(key, true, out var translated))
                {
                    param.Args[i] = textResourceHelper.GetSpecializedTranslation(param, i, translated);
                    return true;
                }
                else if (AutoTranslatorSettings.IsDumpingRedirectedResourcesEnabled && LanguageHelper.IsTranslatable(key))
                {
                    cache.AddTranslationToCache(key, value);
                }
            }
            return false;
        }

        protected override bool ShouldHandleAsset(ScenarioData asset, IAssetOrResourceLoadedContext context) => !context.HasReferenceBeenRedirectedBefore(asset);
    }
}
#else //Stub for HS which has no ScenarioData
namespace KK_Plugins
{
    public class ScenarioDataResourceRedirector
    {
        public ScenarioDataResourceRedirector(TextResourceHelper _ = null) { }
    }
}
#endif