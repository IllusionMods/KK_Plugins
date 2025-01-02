using BepInEx;
using BepInEx.Logging;
using ChaCustom;
using HarmonyLib;
using Manager;
using Sideloader.AutoResolver;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KK_Plugins
{
    using ItemGroup = Dictionary<string, Dictionary<int, HashSet<int>>>;

#if KK
    [BepInProcess(Constants.MainGameProcessNameSteam)]
#endif
    [BepInProcess(Constants.MainGameProcessName)]
    [BepInDependency(Sideloader.Sideloader.GUID, Sideloader.Sideloader.Version)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class ItemBlacklist : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.itemblacklist";
        public const string PluginName = "Item Blacklist";
        public const string PluginNameInternal = "KK_ItemBlacklist";
        public const string Version = "3.0";
        internal const string BaseGameItemGuid = "[['BASE'GAME'ITEM']]";
        internal static new ManualLogSource Logger;

        private static CustomSelectListCtrl CustomSelectListCtrlInstance;
        private static CustomSelectInfoComponent CurrentCustomSelectInfoComponent;
        private static string FavoritesDirectory;
        private static string FavoritesFile;
        private static string BlacklistDirectory;
        private static string BlacklistFile;
        //GUID/Category/ID
        private static readonly Dictionary<string, Dictionary<int, HashSet<int>>> Favorites = new ItemGroup();
        private static readonly Dictionary<string, Dictionary<int, HashSet<int>>> Blacklist = new ItemGroup();
        private static readonly Dictionary<CustomSelectListCtrl, ListVisibilityType> ListVisibility = new Dictionary<CustomSelectListCtrl, ListVisibilityType>();

        private static bool MouseIn;

        internal void Main()
        {
            BlacklistDirectory = Path.Combine(UserData.Path, "save");
            BlacklistFile = Path.Combine(BlacklistDirectory, "itemblacklist.xml");
            FavoritesDirectory = BlacklistDirectory;
            FavoritesFile = Path.Combine(FavoritesDirectory, "itemfavorites.xml");
            Padding = new RectOffset(3, 3, 0, 1);

            Logger = base.Logger;

            Harmony.CreateAndPatchAll(typeof(Hooks));
            SceneManager.sceneLoaded += (s, lsm) => SetMenuVisibility(false);

            LoadFavorites();
            LoadBlacklist();
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(1))
                ShowMenu();
        }

        private static bool CheckGroup(ItemGroup group, string guid, int category, int id)
        {
            if (group.TryGetValue(guid, out var x))
                if (x.TryGetValue(category, out var y))
                    if (y.Contains(id))
                        return true;
            return false;
        }
        private static bool CheckFavorites(string guid, int category, int id)
        {
            return CheckGroup(Favorites, guid, category, id);
        }
        private static bool CheckBlacklist(string guid, int category, int id)
        {
            return CheckGroup(Blacklist, guid, category, id);
        }

        private static void LoadGroup(ItemGroup group, string directory, string file, string xmlElement)
        {
            Directory.CreateDirectory(directory);

            XDocument xml;
            if (File.Exists(file))
            {
                xml = XDocument.Load(file);
            }
            else
            {
                xml = new XDocument();
                xml.Add(new XElement(xmlElement));
                xml.Save(file);
            }

            var itemGroup = xml.Element(xmlElement);
            if (itemGroup != null)
            {
                foreach (var modElement in itemGroup.Elements("mod"))
                {
                    string guid = modElement.Attribute("guid")?.Value;
                    foreach (var categoryElement in modElement.Elements("category"))
                    {
                        if (int.TryParse(categoryElement.Attribute("number")?.Value, out int category))
                        {
                            foreach (var itemElement in categoryElement.Elements("item"))
                            {
                                if (int.TryParse(itemElement.Attribute("id")?.Value, out int id))
                                {
                                    if (!group.ContainsKey(guid))
                                        group[guid] = new Dictionary<int, HashSet<int>>();
                                    if (!group[guid].ContainsKey(category))
                                        group[guid][category] = new HashSet<int>();
                                    group[guid][category].Add(id);
                                }
                            }
                        }
                    }
                }
            }
        }
        private static void LoadFavorites()
        {
            LoadGroup(Favorites, FavoritesDirectory, FavoritesFile, "itemFavorites");
        }
        private static void LoadBlacklist()
        {
            LoadGroup(Blacklist, BlacklistDirectory, BlacklistFile, "itemBlacklist");
        }

        private static void SaveGroup(ItemGroup group, string file, string xmlElement)
        {
            XDocument xml = new XDocument();
            XElement itemGroupElement = new XElement(xmlElement);
            xml.Add(itemGroupElement);

            foreach (var x in group)
            {
                XElement modElement = new XElement("mod");
                modElement.SetAttributeValue("guid", x.Key);
                itemGroupElement.Add(modElement);

                foreach (var y in x.Value)
                {
                    XElement categoryElement = new XElement("category");
                    categoryElement.SetAttributeValue("number", y.Key);
                    modElement.Add(categoryElement);

                    foreach (var z in y.Value)
                    {
                        XElement itemElement = new XElement("item");
                        itemElement.SetAttributeValue("id", z);
                        categoryElement.Add(itemElement);
                    }
                }
            }

            var retryCount = 3;
        retry:
            try
            {
                using (var fs = new FileStream(file, FileMode.Create, FileAccess.ReadWrite))
                using (var tw = new StreamWriter(fs, Encoding.UTF8))
                    xml.Save(tw);
                return;
            }
            catch (IOException)
            {
                System.Threading.Thread.Sleep(500);
                if (retryCount-- > 0)
                    goto retry;
                else
                    throw;
            }
        }
        private static void SaveFavorites()
        {
            SaveGroup(Favorites, FavoritesFile, "itemFavorites");
        }
        private static void SaveBlacklist()
        {
            SaveGroup(Blacklist, BlacklistFile, "itemBlacklist");
        }

        public void ChangeListFilter(ListVisibilityType visibilityType) => ChangeListFilter(CustomSelectListCtrlInstance, visibilityType);

        public static void ChangeListFilter(CustomSelectListCtrl customSelectListCtrl, ListVisibilityType visibilityType)
        {
            int count = 0;
            for (var i = 0; i < customSelectListCtrl.lstSelectInfo.Count; i++)
            {
                CustomSelectInfo customSelectInfo = customSelectListCtrl.lstSelectInfo[i];
                if (visibilityType == ListVisibilityType.All)
                {
                    customSelectListCtrl.DisvisibleItem(customSelectInfo.index, false);
                    continue;
                }

                bool hide = visibilityType != ListVisibilityType.Filtered;

                ResolveInfo Info = UniversalAutoResolver.TryGetResolutionInfo((ChaListDefine.CategoryNo)customSelectInfo.category, customSelectInfo.index);
                string guid = Info == null ? BaseGameItemGuid : Info.GUID ?? string.Empty;
                int category = Info == null ? customSelectInfo.category : (int)Info.CategoryNo;
                int slot = Info == null ? customSelectInfo.index : Info.Slot;

                if (visibilityType == ListVisibilityType.Filtered && CheckBlacklist(guid, category, slot))
                {
                    hide = true;
                    count++;
                }
                if ((visibilityType == ListVisibilityType.Favorites && CheckFavorites(guid, category, slot)) ||
                    (visibilityType == ListVisibilityType.Hidden && CheckBlacklist(guid, category, slot)))
                {
                    hide = false;
                    count++;
                }
                customSelectListCtrl.DisvisibleItem(customSelectInfo.index, hide);
            }

            ListVisibility[customSelectListCtrl] = visibilityType;

            if (count == 0 && visibilityType == ListVisibilityType.Favorites)
            {
                Logger.LogMessage("No items are favorited");
                ChangeListFilter(customSelectListCtrl, ListVisibilityType.Filtered);
            }
            if (count == 0 && visibilityType == ListVisibilityType.Hidden)
            {
                Logger.LogMessage("No items are hidden");
                ChangeListFilter(customSelectListCtrl, ListVisibilityType.Filtered);
            }
        }

        private void GroupItem(ItemGroup group, ListVisibilityType? hideFrom, string guid, int category, int id, int index)
        {
            if (!group.ContainsKey(guid))
                group[guid] = new Dictionary<int, HashSet<int>>();
            if (!group[guid].ContainsKey(category))
                group[guid][category] = new HashSet<int>();
            group[guid][category].Add(id);
            SetMenuVisibility(false);

            var controls = CustomBase.Instance.GetComponentsInChildren<CustomSelectListCtrl>(true);
            for (var i = 0; i < controls.Length; i++)
            {
                var customSelectListCtrl = controls[i];
                if (customSelectListCtrl.GetSelectInfoFromIndex(index)?.category == category)
                    if (ListVisibility.TryGetValue(customSelectListCtrl, out var visibilityType))
                        if (visibilityType == hideFrom)
                            customSelectListCtrl.DisvisibleItem(index, true);
            }
        }
        private void FavoriteItem(string guid, int category, int id, int index)
        {
            UnblacklistItem(guid, category, id, index);
            GroupItem(Favorites, null, guid, category, id, index);
            SaveFavorites();
        }
        private void BlacklistItem(string guid, int category, int id, int index)
        {
            UnfavoriteItem(guid, category, id, index);
            GroupItem(Blacklist, ListVisibilityType.Filtered, guid, category, id, index);
            SaveBlacklist();
        }

        private void UngroupItem(ItemGroup group, ListVisibilityType boundVisibility, string guid, int category, int id, int index)
        {
            if (!group.ContainsKey(guid))
                group[guid] = new Dictionary<int, HashSet<int>>();
            if (!group[guid].ContainsKey(category))
                group[guid][category] = new HashSet<int>();
            group[guid][category].Remove(id);
            SaveBlacklist();

            bool changeFilter = false;
            var controls = CustomBase.Instance.GetComponentsInChildren<CustomSelectListCtrl>(true);
            for (var i = 0; i < controls.Length; i++)
            {
                var customSelectListCtrl = controls[i];
                if (customSelectListCtrl.GetSelectInfoFromIndex(index)?.category == category)
                {
                    if (ListVisibility.TryGetValue(customSelectListCtrl, out var visibilityType))
                        if (visibilityType == boundVisibility)
                            customSelectListCtrl.DisvisibleItem(index, true);

                    if (customSelectListCtrl.lstSelectInfo.All(x => x.disvisible))
                        changeFilter = true;
                }
            }

            if (changeFilter)
                ChangeListFilter(ListVisibilityType.Filtered);
            SetMenuVisibility(false);
        }
        private void UnfavoriteItem(string guid, int category, int id, int index)
        {
            UngroupItem(Favorites, ListVisibilityType.Favorites, guid, category, id, index);
        }
        private void UnblacklistItem(string guid, int category, int id, int index)
        {
            UngroupItem(Blacklist, ListVisibilityType.Hidden, guid, category, id, index);
        }

        private int GroupMod(ItemGroup group, ItemGroup skipItemsIn, ListVisibilityType? hideFrom, string guid, bool onlyCurrentList)
        {
            var allLists = CustomBase.Instance.GetComponentsInChildren<CustomSelectListCtrl>(true);

            int ProcessList(CustomSelectListCtrl targetListCtrl1)
            {
                int skipped = 0;
                for (var i = 0; i < targetListCtrl1.lstSelectInfo.Count; i++)
                {
                    CustomSelectInfo customSelectInfo = targetListCtrl1.lstSelectInfo[i];
                    int category = customSelectInfo.category;
                    ResolveInfo info = UniversalAutoResolver.TryGetResolutionInfo((ChaListDefine.CategoryNo)category, customSelectInfo.index);
                    int slot = info == null ? customSelectInfo.index : info.Slot;

                    if (guid != (info == null ? BaseGameItemGuid : info.GUID ?? string.Empty))
                        continue;

                    if (skipItemsIn.ContainsKey(guid) && skipItemsIn[guid].ContainsKey(category) && skipItemsIn[guid][category].Contains(slot))
                    {
                        skipped++;
                        continue;
                    }

                    if (!group.ContainsKey(guid))
                        group[guid] = new Dictionary<int, HashSet<int>>();
                    if (!group[guid].ContainsKey(category))
                        group[guid][category] = new HashSet<int>();
                    group[guid][category].Add(slot);

                    for (var j = 0; j < allLists.Length; j++)
                    {
                        var customSelectListCtrl = allLists[j];
                        if (customSelectListCtrl.GetSelectInfoFromIndex(customSelectInfo.index)?.category == category)
                            if (ListVisibility.TryGetValue(customSelectListCtrl, out var visibilityType))
                                if (visibilityType == hideFrom)
                                    customSelectListCtrl.DisvisibleItem(customSelectInfo.index, true);
                    }
                }

                return skipped;
            }

            var allSkipped = onlyCurrentList ? ProcessList(CustomSelectListCtrlInstance) : allLists.Sum(ProcessList);
            SetMenuVisibility(false);
            return allSkipped;
        }
        private void FavoriteMod(string guid, bool onlyCurrentList)
        {
            int skipped = GroupMod(Favorites, Blacklist, null, guid, onlyCurrentList);
            if (skipped > 0)
                Logger.LogMessage($"Skipped {skipped} blacklisted items");
            SaveFavorites();
        }
        private void BlacklistMod(string guid, bool onlyCurrentList)
        {
            int skipped = GroupMod(Blacklist, Favorites, ListVisibilityType.Filtered, guid, onlyCurrentList);
            if (skipped > 0)
                Logger.LogMessage($"Skipped {skipped} items that are in favorites");
            SaveBlacklist();
        }

        private void UngroupMod(ItemGroup group, ListVisibilityType boundVisibility, string guid, bool onlyCurrentList)
        {
            var allLists = CustomBase.Instance.GetComponentsInChildren<CustomSelectListCtrl>(true);

            bool ProcessList(CustomSelectListCtrl targetListCtrl)
            {
                var result = false;
                for (var i = 0; i < targetListCtrl.lstSelectInfo.Count; i++)
                {
                    CustomSelectInfo customSelectInfo = targetListCtrl.lstSelectInfo[i];
                    int category = customSelectInfo.category;
                    ResolveInfo info = UniversalAutoResolver.TryGetResolutionInfo((ChaListDefine.CategoryNo)category, customSelectInfo.index);
                    int slot = info == null ? customSelectInfo.index : info.Slot;

                    if (guid != (info == null ? BaseGameItemGuid : info.GUID ?? string.Empty))
                        continue;

                    if (!group.ContainsKey(guid))
                        group[guid] = new Dictionary<int, HashSet<int>>();
                    if (!group[guid].ContainsKey(category))
                        group[guid][category] = new HashSet<int>();
                    group[guid][category].Remove(slot);

                    for (var j = 0; j < allLists.Length; j++)
                    {
                        var customSelectListCtrl = allLists[j];
                        if (customSelectListCtrl.GetSelectInfoFromIndex(customSelectInfo.index)?.category == category)
                        {
                            if (ListVisibility.TryGetValue(customSelectListCtrl, out var visibilityType))
                                if (visibilityType == boundVisibility)
                                    customSelectListCtrl.DisvisibleItem(customSelectInfo.index, true);

                            if (customSelectListCtrl.lstSelectInfo.All(x => x.disvisible))
                                result = true;
                        }
                    }
                }

                return result;
            }

            var changeFilter = onlyCurrentList ? ProcessList(CustomSelectListCtrlInstance) : allLists.Count(ProcessList) > 0;

            if (changeFilter)
                ChangeListFilter(ListVisibilityType.Filtered);
            SetMenuVisibility(false);
        }
        private void UnfavoriteMod(string guid, bool onlyCurrentList)
        {
            UngroupMod(Favorites, ListVisibilityType.Favorites, guid, onlyCurrentList);
            SaveFavorites();
        }
        private void UnblacklistMod(string guid, bool onlyCurrentList)
        {
            UngroupMod(Blacklist, ListVisibilityType.Hidden, guid, onlyCurrentList);
            SaveBlacklist();
        }

        private void PrintInfo(int index)
        {
            var customSelectInfo = CustomSelectListCtrlInstance.lstSelectInfo.First(x => x.index == index);

            if (index >= UniversalAutoResolver.BaseSlotID)
            {
                ResolveInfo info = UniversalAutoResolver.TryGetResolutionInfo((ChaListDefine.CategoryNo)customSelectInfo.category, customSelectInfo.index);
                if (info != null)
                {
                    Logger.LogMessage($"Item GUID:{info.GUID} Category:{(int)info.CategoryNo}({info.CategoryNo}) ID:{info.Slot}");

#if KKS
                    Dictionary<int, ListInfoBase> dictionary = Character.chaListCtrl.GetCategoryInfo(info.CategoryNo);
#else
                    Dictionary<int, ListInfoBase> dictionary = Singleton<Character>.Instance.chaListCtrl.GetCategoryInfo(info.CategoryNo);
#endif
                    if (dictionary != null && dictionary.TryGetValue(customSelectInfo.index, out ListInfoBase listInfoBase))
                    {
                        string assetBundle = listInfoBase.GetInfo(ChaListDefine.KeyType.MainAB);
                        if (!assetBundle.IsNullOrEmpty() && assetBundle != "0")
                        {
                            string asset = TryGetMainAsset(listInfoBase);
                            if (asset == null)
                                Logger.LogMessage($"AssetBundle:{assetBundle}");
                            else
                                Logger.LogMessage($"AssetBundle:{assetBundle} Asset:{asset}");
                        }
                    }

                    if (Sideloader.Sideloader.ZipArchives.TryGetValue(info.GUID, out string zipFileName))
                        Logger.LogMessage($"Zip File:{Path.GetFileName(zipFileName)}");
                }
            }
            else
            {
                Logger.LogMessage($"Item Category:{CurrentCustomSelectInfoComponent.info.category}({(ChaListDefine.CategoryNo)CurrentCustomSelectInfoComponent.info.category}) ID:{CurrentCustomSelectInfoComponent.info.index}");

#if KKS
                Dictionary<int, ListInfoBase> dictionary = Character.chaListCtrl.GetCategoryInfo((ChaListDefine.CategoryNo)CurrentCustomSelectInfoComponent.info.category);
#else
                Dictionary<int, ListInfoBase> dictionary = Singleton<Character>.Instance.chaListCtrl.GetCategoryInfo((ChaListDefine.CategoryNo)CurrentCustomSelectInfoComponent.info.category);
#endif
                if (dictionary != null && dictionary.TryGetValue(customSelectInfo.index, out var listInfoBase))
                {
                    string assetBundle = listInfoBase.GetInfo(ChaListDefine.KeyType.MainAB);
                    if (!assetBundle.IsNullOrEmpty() && assetBundle != "0")
                    {
                        string asset = TryGetMainAsset(listInfoBase);
                        if (asset == null)
                            Logger.LogMessage($"AssetBundle:{assetBundle}");
                        else
                            Logger.LogMessage($"AssetBundle:{assetBundle} Asset:{asset}");
                    }
                }
            }
            SetMenuVisibility(false);
        }

        /// <summary>
        /// Try to get the primary asset by trying various things
        /// </summary>
        /// <param name="listInfoBase"></param>
        /// <returns>Null if the main asset wasn't found</returns>
        private static string TryGetMainAsset(ListInfoBase listInfoBase)
        {
            string asset = listInfoBase.GetInfo(ChaListDefine.KeyType.MainData);
            if (!asset.IsNullOrEmpty() && asset != "0")
                return asset;
            asset = listInfoBase.GetInfo(ChaListDefine.KeyType.MainTex);
            if (!asset.IsNullOrEmpty() && asset != "0")
                return asset;
            asset = listInfoBase.GetInfo(ChaListDefine.KeyType.PaintTex);
            if (!asset.IsNullOrEmpty() && asset != "0")
                return asset;
            asset = listInfoBase.GetInfo(ChaListDefine.KeyType.NipTex);
            if (!asset.IsNullOrEmpty() && asset != "0")
                return asset;
            asset = listInfoBase.GetInfo(ChaListDefine.KeyType.SunburnTex);
            if (!asset.IsNullOrEmpty() && asset != "0")
                return asset;
            asset = listInfoBase.GetInfo(ChaListDefine.KeyType.UnderhairTex);
            if (!asset.IsNullOrEmpty() && asset != "0")
                return asset;
            return null;
        }

        public enum ListVisibilityType { Filtered, Favorites, Hidden, All }
    }
}
