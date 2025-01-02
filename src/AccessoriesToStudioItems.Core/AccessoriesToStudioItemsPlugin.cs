using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using BepInEx;
using HarmonyLib;
using KK_Plugins;
using KK_QuickAccessBox;
using KKAPI.Utilities;
using Sideloader.AutoResolver;
using Studio;
using UnityEngine;

#if AI || HS2
using AIChara;
#endif

namespace AccessoriesToStudioItems
{
    [BepInPlugin(GUID, DisplayName, Version)]
    [BepInDependency(Sideloader.Sideloader.GUID, Sideloader.Sideloader.Version)]
    [BepInDependency(QuickAccessBox.GUID, QuickAccessBox.Version)]
    [BepInProcess(Constants.StudioProcessName)]
    public class AccessoriesToStudioItemsPlugin : BaseUnityPlugin
    {
        public const string GUID = "AccessoriesToStudioItems";
        public const string DisplayName = "Accessories to Studio Items";
        public const string Version = "1.0.1";

        internal static new BepInEx.Logging.ManualLogSource Logger;

        private void Awake()
        {
            Logger = base.Logger;

            Harmony.CreateAndPatchAll(typeof(Hooks), GUID);

            QuickAccessBox.RegisterThumbnailProvider(TryGetThumbnail);
        }

        /// <summary>
        /// GroupNo for items added by this plugin. Avoid using this group in zipmods to prevent potential conflicts
        /// DO NOT CHANGE THIS VALUE, doing so will break existing scenes that use modded accessories added by this plugin.
        /// </summary>
        public const int AccessoriesAsItemsGroupNumber = 647158;

        private static void AddAccsToStudioItems()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // Always check for existing groups/categories and reuse them if they exist, even though they shouldn't
            Studio.Info.Instance.dicItemGroupCategory.TryGetValue(AccessoriesAsItemsGroupNumber, out var accGroupInfo);
            if (accGroupInfo == null)
            {
                accGroupInfo = new Info.GroupInfo();
                accGroupInfo.name = "アクセサリー"; //"Accessories";
                Studio.Info.Instance.dicItemGroupCategory[AccessoriesAsItemsGroupNumber] = accGroupInfo;
            }

#if KK || AI || HS2
            var dictListInfo = Manager.Character.Instance.chaListCtrl.dictListInfo;
#elif KKS
            var dictListInfo = Manager.Character.chaListCtrl.dictListInfo;
#endif

            // Add the top item group for Accessories
            // Keys of the dic: group number, category number, item number
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
                // Add the category if it doesn't exist already
                if (!accGroupInfo.dicCategory.ContainsKey((int)categoryNo))
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
#if AI || HS2
                    accGroupInfo.dicCategory.Add((int)categoryNo, new Info.CategoryInfo { name = categoryNo.ToString(), sort = (int)categoryNo });
#else
                    accGroupInfo.dicCategory.Add((int)categoryNo, categoryNo.ToString());
#endif
                }

#if AI || HS2
                dictListInfo.TryGetValue((int)categoryNo, out var accessoryListInfos);
#else
                dictListInfo.TryGetValue(categoryNo, out var accessoryListInfos);
#endif
                if (accessoryListInfos == null)
                    continue;

                accsGroupDic.TryGetValue((int)categoryNo, out var accsCategoryDic);
                if (accsCategoryDic == null)
                {
                    accsCategoryDic = new Dictionary<int, Info.ItemLoadInfo>();
                    accsGroupDic[(int)categoryNo] = accsCategoryDic;
                }

                var count = 0;
                foreach (var accessoryInfo in accessoryListInfos.Values)
                {
                    try
                    {
                        // Add the accessory to the studio item list
                        accessoryInfo.dictInfo.TryGetValue((int)ChaListDefine.KeyType.MainManifest, out var manifest);
                        var itemLoadInfo = new Info.ItemLoadInfo(new List<string>
                        {
                            // First three are ignored but populate them anyways for compatibility with other plugins
                            accessoryInfo.Id.ToString(),
                            AccessoriesAsItemsGroupNumber.ToString(),
                            accessoryInfo.Category.ToString(),
                            accessoryInfo.Name,
                            manifest ?? "",
                            accessoryInfo.dictInfo[(int)ChaListDefine.KeyType.MainAB],
                            accessoryInfo.dictInfo[(int)ChaListDefine.KeyType.MainData],
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
                        accsCategoryDic.Add(accessoryInfo.Id, itemLoadInfo);

                        AddThumbnail(accessoryInfo);

                        // Copy Sideloader resolve info from the original accessory into a new studio item resolve info.
                        // Needed for modded accessories to resolve properly when used as items.
                        var resolveInfo = UniversalAutoResolver.TryGetResolutionInfo(categoryNo, accessoryInfo.Id);
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
                        Logger.LogWarning($"Failed to convert accessory to studio item! ID={accessoryInfo.Id} Category={(ChaListDefine.CategoryNo)accessoryInfo.Category} Name='{accessoryInfo.Name}'\n{e}");
                    }
                }

                Logger.LogDebug($"Added {count} accessories from {categoryNo} to studio item list in {sw.ElapsedMilliseconds}ms");
                sw.Reset();
                sw.Start();
            }
        }

        #region QuickAccessBox Thumbnails

        private static readonly Dictionary<long, KeyValuePair<string, string>> _TextureLookup = new Dictionary<long, KeyValuePair<string, string>>();
        private static Sprite TryGetThumbnail(ItemInfo item)
        {
            if (item.GroupNo != AccessoriesAsItemsGroupNumber)
                return null;

            var cacheId = MakeCacheId(item.LocalSlot, item.CategoryNo);
            if (!_TextureLookup.TryGetValue(cacheId, out var abInfo)) return null;

            return CommonLib.LoadAsset<Texture2D>(abInfo.Key, abInfo.Value)?.ToSprite();
        }
        private static void AddThumbnail(ListInfoBase accessoryInfo)
        {
            var thumbAb = accessoryInfo.dictInfo[(int)ChaListDefine.KeyType.ThumbAB];
            var thumbTex = accessoryInfo.dictInfo[(int)ChaListDefine.KeyType.ThumbTex];
            var cacheId = MakeCacheId(accessoryInfo.Id, accessoryInfo.Category);
            _TextureLookup[cacheId] = new KeyValuePair<string, string>(thumbAb, thumbTex);
        }
        private static long MakeCacheId(int localSlot, int categoryNo) => localSlot + ((long)categoryNo << 32);

        #endregion

        private static class Hooks
        {
            /// <summary>
            /// Entry point for adding accessories to studio items
            /// </summary>
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(Info), nameof(Studio.Info.LoadExcelDataCoroutine), MethodType.Enumerator)]
            private static IEnumerable<CodeInstruction> StudioLoadItemsHook(IEnumerable<CodeInstruction> instructions)
            {
                // Doesn't matter at what part of the original coroutine we inject our code as long as it's after dictionaries are cleared and before isLoadList is set to true.
                // Just to be safe we'll inject it right before isLoadList is set to true. Downside is that the Accessory group will be at the very bottom of the item list (in some games).
                return new CodeMatcher(instructions).End()
                                                    .MatchBack(false, new CodeMatch(OpCodes.Call, AccessTools.PropertySetter(typeof(Info), nameof(Studio.Info.isLoadList))))
                                                    .ThrowIfInvalid("Hook point missing")
                                                    .Advance(-2)
                                                    .Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AccessoriesToStudioItemsPlugin), nameof(AddAccsToStudioItems))))
                                                    .Instructions();
            }
        }
    }
}
