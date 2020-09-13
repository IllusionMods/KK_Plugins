using BepInEx;
using BepInEx.Logging;
using ChaCustom;
using HarmonyLib;
using Sideloader.AutoResolver;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UILib;
using UnityEngine;

namespace KK_Plugins
{

    [BepInPlugin(GUID, PluginName, Version)]
    public partial class ItemBlacklist : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.itemblacklist";
        public const string PluginName = "Item Blacklist";
        public const string PluginNameInternal = "KK_ItemBlacklist";
        public const string Version = "1.0";
        internal static new ManualLogSource Logger;

        private static CustomSelectListCtrl CustomSelectListCtrlInstance;
        private static CustomSelectInfoComponent CurrentCustomSelectInfoComponent;
        private const string BlacklistDirectory = "UserData/Item Blacklist/";
        private const string BlacklistFile = "UserData/Item Blacklist/Blacklist.xml";
        private static readonly Dictionary<string, Dictionary<int, HashSet<int>>> Blacklist = new Dictionary<string, Dictionary<int, HashSet<int>>>();

        private static bool MouseIn = false;

        internal void Main()
        {
            Logger = base.Logger;

            Harmony.CreateAndPatchAll(typeof(Hooks));

            LoadBlacklist();
        }

        internal void Update()
        {
            if (Input.GetMouseButtonDown(1))
                ShowMenu();
        }

        static bool CheckBlacklist(string guid, int category, int id)
        {
            if (Blacklist.TryGetValue(guid, out var x))
                if (x.TryGetValue(category, out var y))
                    if (y.Contains(id))
                        return true;
            return false;
        }

        void LoadBlacklist()
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
            foreach (var modElement in itemBlacklist.Elements("mod"))
            {
                string guid = modElement.Attribute("guid")?.Value;
                if (!guid.IsNullOrEmpty())
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

        void SaveBlacklist()
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

        void ShowMenu()
        {
            InitUI();

            ContextMenu.gameObject.SetActive(false);
            if (CurrentCustomSelectInfoComponent == null) return;
            if (!MouseIn) return;

            int index = CurrentCustomSelectInfoComponent.info.index;
            var xPosition = (Event.current.mousePosition.x / Screen.width) + 0.01f;
            var yPosition = 1 - (Event.current.mousePosition.y / Screen.height) - UIHeight - 0.01f;

            ContextMenuPanel.transform.SetRect(xPosition, yPosition, UIWidth + xPosition, UIHeight + yPosition);
            ContextMenu.gameObject.SetActive(true);

            BlacklistButton.onClick.RemoveAllListeners();
            BlacklistButton.onClick.AddListener(delegate () { BlacklistItem(index); });

            BlacklistModButton.onClick.RemoveAllListeners();
            BlacklistModButton.onClick.AddListener(delegate () { BlacklistMod(index); });

            InfoButton.onClick.RemoveAllListeners();
            InfoButton.onClick.AddListener(delegate () { PrintInfo(index); });
        }
        void HideMenu() => ContextMenu?.gameObject?.SetActive(false);

        private void BlacklistItem(int index)
        {
            if (index >= UniversalAutoResolver.BaseSlotID)
            {
                List<CustomSelectInfo> lstSelectInfo = (List<CustomSelectInfo>)Traverse.Create(CustomSelectListCtrlInstance).Field("lstSelectInfo").GetValue();
                var customSelectInfo = lstSelectInfo.FirstOrDefault(x => x.index == index);

                ResolveInfo Info = UniversalAutoResolver.TryGetResolutionInfo((ChaListDefine.CategoryNo)customSelectInfo.category, customSelectInfo.index);
                if (Info != null)
                {
                    Logger.LogInfo($"Blacklisting item GUID:{Info.GUID} Category:{customSelectInfo.category} ID:{Info.Slot}");

                    if (!Blacklist.ContainsKey(Info.GUID))
                        Blacklist[Info.GUID] = new Dictionary<int, HashSet<int>>();
                    if (!Blacklist[Info.GUID].ContainsKey(customSelectInfo.category))
                        Blacklist[Info.GUID][customSelectInfo.category] = new HashSet<int>();
                    Blacklist[Info.GUID][customSelectInfo.category].Add(Info.Slot);
                    SaveBlacklist();

                    CustomSelectListCtrlInstance.DisvisibleItem(customSelectInfo.index, true);
                }
            }
            HideMenu();
        }

        private void BlacklistMod(int index)
        {
            if (index >= UniversalAutoResolver.BaseSlotID)
            {
                List<CustomSelectInfo> lstSelectInfo = (List<CustomSelectInfo>)Traverse.Create(CustomSelectListCtrlInstance).Field("lstSelectInfo").GetValue();
                var customSelectInfoItem = lstSelectInfo.FirstOrDefault(x => x.index == index);

                string GUID = null;
                {
                    ResolveInfo Info = UniversalAutoResolver.TryGetResolutionInfo((ChaListDefine.CategoryNo)customSelectInfoItem.category, customSelectInfoItem.index);
                    if (Info != null)
                        GUID = Info.GUID;
                }

                if (!GUID.IsNullOrEmpty())
                {
                    foreach (CustomSelectInfo customSelectInfo in lstSelectInfo)
                    {
                        if (customSelectInfo.index >= UniversalAutoResolver.BaseSlotID)
                        {
                            ResolveInfo Info = UniversalAutoResolver.TryGetResolutionInfo((ChaListDefine.CategoryNo)customSelectInfo.category, customSelectInfo.index);
                            if (Info != null && Info.GUID == GUID)
                            {
                                if (!Blacklist.ContainsKey(Info.GUID))
                                    Blacklist[Info.GUID] = new Dictionary<int, HashSet<int>>();
                                if (!Blacklist[Info.GUID].ContainsKey(customSelectInfo.category))
                                    Blacklist[Info.GUID][customSelectInfo.category] = new HashSet<int>();
                                Blacklist[Info.GUID][customSelectInfo.category].Add(Info.Slot);
                                SaveBlacklist();

                                CustomSelectListCtrlInstance.DisvisibleItem(customSelectInfo.index, true);
                            }
                        }
                    }
                }
            }
            HideMenu();
        }

        private void PrintInfo(int index)
        {
            List<CustomSelectInfo> lstSelectInfo = (List<CustomSelectInfo>)Traverse.Create(CustomSelectListCtrlInstance).Field("lstSelectInfo").GetValue();
            var customSelectInfo = lstSelectInfo.FirstOrDefault(x => x.index == index);

            if (index >= UniversalAutoResolver.BaseSlotID)
            {
                ResolveInfo Info = UniversalAutoResolver.TryGetResolutionInfo((ChaListDefine.CategoryNo)customSelectInfo.category, customSelectInfo.index);
                if (Info != null)
                {
                    Logger.LogMessage($"Item GUID:{Info.GUID} Category:{(int)Info.CategoryNo}({Info.CategoryNo}) ID:{Info.Slot}");
                    if (Sideloader.Sideloader.ZipArchives.TryGetValue(Info.GUID, out string zipFileName))
                        Logger.LogMessage($"Zip File:{Path.GetFileName(zipFileName)}");
                }
            }
            else
            {
                Logger.LogMessage($"Item Category:{CurrentCustomSelectInfoComponent.info.category}({(ChaListDefine.CategoryNo)CurrentCustomSelectInfoComponent.info.category}) ID:{CurrentCustomSelectInfoComponent.info.index}");
            }
            HideMenu();
        }
    }
}