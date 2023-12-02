using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using BepInEx;
using HarmonyLib;
using KK_QuickAccessBox;
using KKAPI.Utilities;
using Sideloader.AutoResolver;
using Studio;
using UnityEngine;

namespace AccessoriesToStudioItems
{
    [BepInPlugin(GUID, DisplayName, Version)]
    [BepInDependency(Sideloader.Sideloader.GUID, Sideloader.Sideloader.Version)]
    [BepInDependency(QuickAccessBox.GUID, QuickAccessBox.Version)]
    public class AccessoriesToStudioItemsPlugin : BaseUnityPlugin
    {
        public const string GUID = nameof(AccessoriesToStudioItems);
        public const string DisplayName = "Accessories to Studio Items";
        public const string Version = "1.0";

        internal static new BepInEx.Logging.ManualLogSource Logger;
        private static readonly Dictionary<long, KeyValuePair<string, string>> _textureLookup  = new Dictionary<long, KeyValuePair<string, string>>();

        private void Awake()
        {
            Logger = base.Logger;

            Harmony.CreateAndPatchAll(typeof(AccessoriesToStudioItemsPlugin));

            QuickAccessBox.RegisterThumbnailProvider(TryGetThumbnail);
        }

        private static Sprite TryGetThumbnail(ItemInfo item)
        {
            if(item.GroupNo != AccessoriesAsItemsGroupNumber)
                return null;

            // use item.LocalSlot and item.CategoryNo as id
            var cacheId = item.LocalSlot + ((long)item.CategoryNo << 32);
            if(!_textureLookup.TryGetValue(cacheId , out var abInfo)) return null;
            
            return CommonLib.LoadAsset<Texture2D>(abInfo.Key, abInfo.Value, false, "", true)?.ToSprite();
        }

        /// <summary>
        /// Entry point for adding accessories to studio items
        /// </summary>
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(Info), nameof(Studio.Info.LoadExcelDataCoroutine), MethodType.Enumerator)]
        private static IEnumerable<CodeInstruction> StudioLoadItemsHook(IEnumerable<CodeInstruction> instructions)
        {
            // Doesn't matter at what part of the original coroutine we inject our code as long as it's after dictionaries are cleared and before isLoadList is set to true.
            // Just to be safe we'll inject it right before isLoadList is set to true. Downside is that the Accessory group will be at the very bottom of the item list.
            return new CodeMatcher(instructions).End()
                                                .MatchBack(false, new CodeMatch(OpCodes.Call, AccessTools.PropertySetter(typeof(Info), nameof(Studio.Info.isLoadList))))
                                                .ThrowIfInvalid("Hook point missing")
                                                .Advance(-2)
                                                .Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AccessoriesToStudioItemsPlugin), nameof(AddAccsToStudioItems))))
                                                .Instructions();
        }

        /// <summary>
        /// DO NOT CHANGE THIS VALUE, will break existing scenes that use these accessories as items.
        /// Also do not use this value for any other group, it's only used to identify the group of accessories in studio and should be unique.
        /// </summary>
        public const int AccessoriesAsItemsGroupNumber = 647158;

        /// <summary>
        /// todo
        ///     qab compat (add api to it?)
        ///     add missing translations
        /// </summary>
        private static void AddAccsToStudioItems()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // Always check for existing groups/categories and reuse them if they exist, even though they shouldn't
            Studio.Info.Instance.dicItemGroupCategory.TryGetValue(AccessoriesAsItemsGroupNumber, out var accGroupInfo);
            if (accGroupInfo == null)
            {
                accGroupInfo = new Info.GroupInfo();
                accGroupInfo.name = "アクセサリー";//"Accessories";
                Studio.Info.Instance.dicItemGroupCategory[AccessoriesAsItemsGroupNumber] = accGroupInfo;
            }

            var dictListInfo = Manager.Character.chaListCtrl.dictListInfo;

            // Keys: group number, category number, item number
            var dicItemLoadInfo = Studio.Info.Instance.dicItemLoadInfo;
            dicItemLoadInfo.TryGetValue(AccessoriesAsItemsGroupNumber, out var accsGroupDic);
            if (accsGroupDic == null)
            {
                accsGroupDic = new Dictionary<int, Dictionary<int, Info.ItemLoadInfo>>();
                dicItemLoadInfo[AccessoriesAsItemsGroupNumber] = accsGroupDic;
            }

            foreach (var categoryNo in Enum.GetValues(typeof(ChaListDefine.CategoryNo))
                                           .Cast<ChaListDefine.CategoryNo>()
                                           .Where(categoryNo => categoryNo > ChaListDefine.CategoryNo.ao_none && categoryNo <= ChaListDefine.CategoryNo.ao_kokan))
            {
                //todo proper jp category names?
                // 髪=Hair
                // 頭=Head
                // 顔=Face
                // 首=Neck
                // 胴=Torso
                // 腰=Hips
                // 脚=Legs
                // 腕=Arms
                // 手=Hands
                // 股間=Crotch
                accGroupInfo.dicCategory.Add((int)categoryNo, categoryNo.ToString());

                dictListInfo.TryGetValue(categoryNo, out var listInfoBases);
                if (listInfoBases == null)
                    continue;

                accsGroupDic.TryGetValue((int)categoryNo, out var accsCategoryDic);
                if (accsCategoryDic == null)
                {
                    accsCategoryDic = new Dictionary<int, Info.ItemLoadInfo>();
                    accsGroupDic[(int)categoryNo] = accsCategoryDic;
                }

                var count = 0;
                foreach (var listInfoBase in listInfoBases.Values)
                {
                    try
                    {
                        var itemLoadInfo = new Info.ItemLoadInfo(new List<string>
                        {
                            // First three are ignored but populate them anyways for compatibility with other plugins
                            listInfoBase.Id.ToString(),
                            AccessoriesAsItemsGroupNumber.ToString(),
                            listInfoBase.Category.ToString(),
                            listInfoBase.Name,
                            listInfoBase.dictInfo[(int)ChaListDefine.KeyType.MainManifest],
                            listInfoBase.dictInfo[(int)ChaListDefine.KeyType.MainAB],
                            listInfoBase.dictInfo[(int)ChaListDefine.KeyType.MainData],
                            // No idea what this does
                            "",      // child root
                            "False", // isAnime
                            // Color and pattern are ignored for items with accessory components
                            "False", // color[0]
                            "False", // pattren[0]
                            "False", // color[1]
                            "False", // pattren[1]
                            "False", // color[2]
                            "False", // pattren[2]
                            // This is the only important one
                            "True",  // isScale
                        });


                        var thumbAb = listInfoBase.dictInfo[(int)ChaListDefine.KeyType.ThumbAB];
                        var thumbTex = listInfoBase.dictInfo[(int)ChaListDefine.KeyType.ThumbTex];
                        var cacheId = listInfoBase.Id + ((long)categoryNo << 32);
                        _textureLookup[cacheId] = new KeyValuePair<string, string>(thumbAb, thumbTex);

                        accsCategoryDic.Add(listInfoBase.Id, itemLoadInfo);

                        // Copy resolve info from the original accessory into a new studio item resolve info
                        var resolveInfo = UniversalAutoResolver.TryGetResolutionInfo(categoryNo, listInfoBase.Id);
                        if (resolveInfo != null)
                        {
                            var studioResolveInfo = new StudioResolveInfo
                            {
                                Author = resolveInfo.Author,
                                Category = (int)categoryNo,
                                GUID = resolveInfo.GUID,
                                Group = AccessoriesAsItemsGroupNumber,
                                LocalSlot = resolveInfo.LocalSlot,
                                Slot = resolveInfo.Slot,
                                Name = resolveInfo.Name,
                                ResolveItem = true,
                                Website = resolveInfo.Website
                            };
                            UniversalAutoResolver.AddStudioResolutionInfo(studioResolveInfo);
                        }

                        count++;
                    }
                    catch (Exception e)
                    {
                        Logger.LogWarning($"Failed to convert accessory to studio item! ID={listInfoBase.Id} Category={(ChaListDefine.CategoryNo)listInfoBase.Category} Name='{listInfoBase.Name}'\n{e}");
                    }
                }

                Logger.LogDebug($"Added {count} accessories from {categoryNo} to studio item list in {sw.ElapsedMilliseconds}ms");
                sw.Restart();
            }
        }
    }
}
