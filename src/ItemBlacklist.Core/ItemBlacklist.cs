using BepInEx;
using BepInEx.Logging;
using ChaCustom;
using HarmonyLib;
using Manager;
using Sideloader.AutoResolver;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KK_Plugins
{
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
        public const string Version = "1.1";
        internal static new ManualLogSource Logger;

        private static CustomSelectListCtrl CustomSelectListCtrlInstance;
        private static CustomSelectInfoComponent CurrentCustomSelectInfoComponent;
        private static string BlacklistDirectory;
        private static string BlacklistFile;
        //GUID/Category/ID
        private static readonly Dictionary<string, Dictionary<int, HashSet<int>>> Blacklist = new Dictionary<string, Dictionary<int, HashSet<int>>>();
        private static readonly Dictionary<CustomSelectListCtrl, ListVisibilityType> ListVisibility = new Dictionary<CustomSelectListCtrl, ListVisibilityType>();

        private static bool MouseIn;

        internal void Main()
        {
            BlacklistDirectory = Path.Combine(UserData.Path, "save");
            BlacklistFile = Path.Combine(BlacklistDirectory, "itemblacklist.xml");
            Padding = new RectOffset(3, 3, 0, 1);

            Logger = base.Logger;

            Harmony.CreateAndPatchAll(typeof(Hooks));
            SceneManager.sceneLoaded += (s, lsm) => SetMenuVisibility(false);

            LoadBlacklist();
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(1))
                ShowMenu();
        }

        private static bool CheckBlacklist(string guid, int category, int id)
        {
            if (Blacklist.TryGetValue(guid, out var x))
                if (x.TryGetValue(category, out var y))
                    if (y.Contains(id))
                        return true;
            return false;
        }

        private static void LoadBlacklist()
        {
            Directory.CreateDirectory(BlacklistDirectory);

            XDocument blacklistXML;
            if (File.Exists(BlacklistFile))
            {
                blacklistXML = XDocument.Load(BlacklistFile);
            }
            else
            {
                blacklistXML = new XDocument();
                blacklistXML.Add(new XElement("itemBlacklist"));
                blacklistXML.Save(BlacklistFile);
            }

            var itemBlacklist = blacklistXML.Element("itemBlacklist");
            if (itemBlacklist != null)
            {
                foreach (var modElement in itemBlacklist.Elements("mod"))
                {
                    string guid = modElement.Attribute("guid")?.Value;
                    if (!string.IsNullOrEmpty(guid))
                    {
                        foreach (var categoryElement in modElement.Elements("category"))
                        {
                            if (int.TryParse(categoryElement.Attribute("number")?.Value, out int category))
                            {
                                foreach (var itemElement in categoryElement.Elements("item"))
                                {
                                    if (int.TryParse(itemElement.Attribute("id")?.Value, out int id))
                                    {
                                        if (!Blacklist.ContainsKey(guid))
                                            Blacklist[guid] = new Dictionary<int, HashSet<int>>();
                                        if (!Blacklist[guid].ContainsKey(category))
                                            Blacklist[guid][category] = new HashSet<int>();
                                        Blacklist[guid][category].Add(id);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void SaveBlacklist()
        {
            File.Delete(BlacklistFile);

            XDocument blacklistXML = new XDocument();
            XElement itemBlacklistElement = new XElement("itemBlacklist");
            blacklistXML.Add(itemBlacklistElement);

            foreach (var x in Blacklist)
            {
                XElement modElement = new XElement("mod");
                modElement.SetAttributeValue("guid", x.Key);
                itemBlacklistElement.Add(modElement);

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

            blacklistXML.Save(BlacklistFile);
        }

        public void ChangeListFilter(ListVisibilityType visibilityType) => ChangeListFilter(CustomSelectListCtrlInstance, visibilityType);

        public static void ChangeListFilter(CustomSelectListCtrl customSelectListCtrl, ListVisibilityType visibilityType)
        {
            List<CustomSelectInfo> lstSelectInfo = (List<CustomSelectInfo>)Traverse.Create(customSelectListCtrl).Field("lstSelectInfo").GetValue();

            int count = 0;
            for (var i = 0; i < lstSelectInfo.Count; i++)
            {
                CustomSelectInfo customSelectInfo = lstSelectInfo[i];
                if (visibilityType == ListVisibilityType.All)
                {
                    customSelectListCtrl.DisvisibleItem(customSelectInfo.index, false);
                    continue;
                }

                bool hide = visibilityType != ListVisibilityType.Filtered;

                if (customSelectInfo.index >= UniversalAutoResolver.BaseSlotID)
                {
                    ResolveInfo Info = UniversalAutoResolver.TryGetResolutionInfo((ChaListDefine.CategoryNo)customSelectInfo.category, customSelectInfo.index);
                    if (Info != null)
                    {
                        if (CheckBlacklist(Info.GUID, (int)Info.CategoryNo, Info.Slot))
                        {
                            hide = visibilityType == ListVisibilityType.Filtered;
                            count++;
                        }
                    }
                }
                customSelectListCtrl.DisvisibleItem(customSelectInfo.index, hide);
            }
            ListVisibility[customSelectListCtrl] = visibilityType;

            if (count == 0 && visibilityType == ListVisibilityType.Hidden)
            {
                Logger.LogMessage("No items are hidden");
                ChangeListFilter(customSelectListCtrl, ListVisibilityType.Filtered);
            }
        }

        private void BlacklistItem(string guid, int category, int id, int index)
        {
            if (!Blacklist.ContainsKey(guid))
                Blacklist[guid] = new Dictionary<int, HashSet<int>>();
            if (!Blacklist[guid].ContainsKey(category))
                Blacklist[guid][category] = new HashSet<int>();
            Blacklist[guid][category].Add(id);
            SaveBlacklist();

            var controls = CustomBase.Instance.GetComponentsInChildren<CustomSelectListCtrl>(true);
            for (var i = 0; i < controls.Length; i++)
            {
                var customSelectListCtrl = controls[i];
                if (customSelectListCtrl.GetSelectInfoFromIndex(index)?.category == category)
                    if (ListVisibility.TryGetValue(customSelectListCtrl, out var visibilityType))
                        if (visibilityType == ListVisibilityType.Filtered)
                            customSelectListCtrl.DisvisibleItem(index, true);
            }

            SetMenuVisibility(false);
        }
        private void UnblacklistItem(string guid, int category, int id, int index)
        {
            if (!Blacklist.ContainsKey(guid))
                Blacklist[guid] = new Dictionary<int, HashSet<int>>();
            if (!Blacklist[guid].ContainsKey(category))
                Blacklist[guid][category] = new HashSet<int>();
            Blacklist[guid][category].Remove(id);
            SaveBlacklist();

            bool changeFilter = false;
            var controls = CustomBase.Instance.GetComponentsInChildren<CustomSelectListCtrl>(true);
            for (var i = 0; i < controls.Length; i++)
            {
                var customSelectListCtrl = controls[i];
                if (customSelectListCtrl.GetSelectInfoFromIndex(index)?.category == category)
                {
                    if (ListVisibility.TryGetValue(customSelectListCtrl, out var visibilityType))
                        if (visibilityType == ListVisibilityType.Hidden)
                            customSelectListCtrl.DisvisibleItem(index, true);

                    List<CustomSelectInfo> lstSelectInfo = (List<CustomSelectInfo>)Traverse.Create(customSelectListCtrl).Field("lstSelectInfo").GetValue();

                    if (lstSelectInfo.All(x => x.disvisible))
                        changeFilter = true;
                }
            }

            if (changeFilter)
                ChangeListFilter(ListVisibilityType.Filtered);
            SetMenuVisibility(false);
        }

        private void BlacklistMod(string guid)
        {
            List<CustomSelectInfo> lstSelectInfo = (List<CustomSelectInfo>)Traverse.Create(CustomSelectListCtrlInstance).Field("lstSelectInfo").GetValue();
            for (var i = 0; i < lstSelectInfo.Count; i++)
            {
                CustomSelectInfo customSelectInfo = lstSelectInfo[i];
                if (customSelectInfo.index >= UniversalAutoResolver.BaseSlotID)
                {
                    ResolveInfo info = UniversalAutoResolver.TryGetResolutionInfo((ChaListDefine.CategoryNo)customSelectInfo.category, customSelectInfo.index);
                    if (info != null && info.GUID == guid)
                    {
                        if (!Blacklist.ContainsKey(info.GUID))
                            Blacklist[info.GUID] = new Dictionary<int, HashSet<int>>();
                        if (!Blacklist[info.GUID].ContainsKey(customSelectInfo.category))
                            Blacklist[info.GUID][customSelectInfo.category] = new HashSet<int>();
                        Blacklist[info.GUID][customSelectInfo.category].Add(info.Slot);
                        SaveBlacklist();

                        var controls = CustomBase.Instance.GetComponentsInChildren<CustomSelectListCtrl>(true);
                        for (var j = 0; j < controls.Length; j++)
                        {
                            var customSelectListCtrl = controls[j];
                            if (customSelectListCtrl.GetSelectInfoFromIndex(customSelectInfo.index)?.category == customSelectInfo.category)
                                if (ListVisibility.TryGetValue(customSelectListCtrl, out var visibilityType))
                                    if (visibilityType == ListVisibilityType.Filtered)
                                        customSelectListCtrl.DisvisibleItem(customSelectInfo.index, true);
                        }
                    }
                }
            }

            SetMenuVisibility(false);
        }
        private void UnblacklistMod(string guid)
        {
            List<CustomSelectInfo> lstSelectInfo = (List<CustomSelectInfo>)Traverse.Create(CustomSelectListCtrlInstance).Field("lstSelectInfo").GetValue();

            bool changeFilter = false;
            for (var i = 0; i < lstSelectInfo.Count; i++)
            {
                CustomSelectInfo customSelectInfo = lstSelectInfo[i];
                if (customSelectInfo.index >= UniversalAutoResolver.BaseSlotID)
                {
                    ResolveInfo info = UniversalAutoResolver.TryGetResolutionInfo((ChaListDefine.CategoryNo)customSelectInfo.category, customSelectInfo.index);
                    if (info != null && info.GUID == guid)
                    {
                        if (!Blacklist.ContainsKey(info.GUID))
                            Blacklist[info.GUID] = new Dictionary<int, HashSet<int>>();
                        if (!Blacklist[info.GUID].ContainsKey(customSelectInfo.category))
                            Blacklist[info.GUID][customSelectInfo.category] = new HashSet<int>();
                        Blacklist[info.GUID][customSelectInfo.category].Remove(info.Slot);
                        SaveBlacklist();

                        var controls = CustomBase.Instance.GetComponentsInChildren<CustomSelectListCtrl>(true);
                        for (var j = 0; j < controls.Length; j++)
                        {
                            var customSelectListCtrl = controls[j];
                            if (customSelectListCtrl.GetSelectInfoFromIndex(customSelectInfo.index)?.category == customSelectInfo.category)
                            {
                                if (ListVisibility.TryGetValue(customSelectListCtrl, out var visibilityType))
                                    if (visibilityType == ListVisibilityType.Hidden)
                                        customSelectListCtrl.DisvisibleItem(customSelectInfo.index, true);

                                List<CustomSelectInfo> lstSelectInfo2 = (List<CustomSelectInfo>)Traverse.Create(customSelectListCtrl).Field("lstSelectInfo").GetValue();

                                if (lstSelectInfo2.All(x => x.disvisible))
                                    changeFilter = true;
                            }
                        }
                    }
                }
            }

            if (changeFilter)
                ChangeListFilter(ListVisibilityType.Filtered);
            SetMenuVisibility(false);
        }

        private void PrintInfo(int index)
        {
            List<CustomSelectInfo> lstSelectInfo = (List<CustomSelectInfo>)Traverse.Create(CustomSelectListCtrlInstance).Field("lstSelectInfo").GetValue();
            var customSelectInfo = lstSelectInfo.First(x => x.index == index);

            if (index >= UniversalAutoResolver.BaseSlotID)
            {
                ResolveInfo info = UniversalAutoResolver.TryGetResolutionInfo((ChaListDefine.CategoryNo)customSelectInfo.category, customSelectInfo.index);
                if (info != null)
                {
                    Logger.LogMessage($"Item GUID:{info.GUID} Category:{(int)info.CategoryNo}({info.CategoryNo}) ID:{info.Slot}");

                    Dictionary<int, ListInfoBase> dictionary = Singleton<Character>.Instance.chaListCtrl.GetCategoryInfo(info.CategoryNo);
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

                Dictionary<int, ListInfoBase> dictionary = Singleton<Character>.Instance.chaListCtrl.GetCategoryInfo((ChaListDefine.CategoryNo)CurrentCustomSelectInfoComponent.info.category);
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

        public enum ListVisibilityType { Filtered, Hidden, All }
    }
}