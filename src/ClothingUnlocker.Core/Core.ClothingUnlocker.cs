using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using HarmonyLib;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using MessagePack;
using System;
using System.Collections.Generic;
using UniRx;

namespace KK_Plugins
{
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    public partial class ClothingUnlocker : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.clothingunlocker";
        public const string PluginName = "Clothing Unlocker";
        public const string PluginNameInternal = Constants.Prefix + "_ClothingUnlocker";
        public const string Version = "2.0.1";
        internal static new ManualLogSource Logger;

        public static ConfigEntry<bool> EnableCrossdressing;
        public static MakerToggle ClothingUnlockToggle;

        private void Start()
        {
            Logger = base.Logger;

            EnableCrossdressing = Config.Bind("Config", "Enable clothing for either gender", true, "Allows any clothing to be worn by either gender.");

            Harmony.CreateAndPatchAll(typeof(Hooks));

            CharacterApi.RegisterExtraBehaviour<ClothingUnlockerController>(GUID);
            MakerAPI.RegisterCustomSubCategories += MakerAPI_RegisterCustomSubCategories;
            MakerAPI.MakerFinishedLoading += MakerAPI_MakerFinishedLoading;
            MakerAPI.ReloadCustomInterface += MakerAPI_ReloadCustomInterface;

#if EC || KKS
            ExtendedSave.CardBeingImported += ExtendedSave_CardBeingImported;
#endif
        }

        private void MakerAPI_RegisterCustomSubCategories(object sender, RegisterSubCategoriesEvent ev)
        {
            MakerCategory category = new MakerCategory("03_ClothesTop", "tglSettings", MakerConstants.Clothes.Copy.Position + 1, "Settings");

            var label = new MakerText("Unlock bras with all tops, bottoms with all tops, and underwear with all bras", category, this);
            ev.AddControl(label);

            ClothingUnlockToggle = new MakerToggle(category, "Clothing Unlock", this);
            ev.AddControl(ClothingUnlockToggle);

            var observer = Observer.Create<bool>(value =>
            {
                if (MakerAPI.InsideAndLoaded)
                {
                    var chaControl = MakerAPI.GetCharacterControl();
                    var controller = GetController(chaControl);
                    bool clothingUnlocked = controller.GetClothingUnlocked();
                    if (clothingUnlocked != value)
                    {
                        controller.SetClothingUnlocked(value);
                        chaControl.Reload();
                    }
                }
            });
            ClothingUnlockToggle.ValueChanged.Subscribe(observer);

            ev.AddSubCategory(category);
        }

#if EC || KKS
        private void ExtendedSave_CardBeingImported(Dictionary<string, PluginData> importedExtendedData)
        {
            if (importedExtendedData.TryGetValue(GUID, out var pluginData))
            {
                if (pluginData != null && pluginData.data.ContainsKey("ClothingUnlocked"))
                {
                    if (pluginData.data.TryGetValue("ClothingUnlocked", out var loadedClothingUnlocked))
                    {
                        Dictionary<int, bool> clothingUnlocked = MessagePackSerializer.Deserialize<Dictionary<int, bool>>((byte[])loadedClothingUnlocked);

                        //Remove all data except for the first outfit
                        List<int> keysToRemove = new List<int>();
                        foreach (var entry in clothingUnlocked)
                            if (entry.Key != 0)
                                keysToRemove.Add(entry.Key);
                        foreach (var key in keysToRemove)
                            clothingUnlocked.Remove(key);

                        if (clothingUnlocked.Count == 0)
                        {
                            importedExtendedData.Remove(GUID);
                        }
                        else
                        {
                            var data = new PluginData();
                            data.data.Add("ClothingUnlocked", MessagePackSerializer.Serialize(clothingUnlocked));
                            importedExtendedData[GUID] = data;
                        }
                    }
                }
            }
        }
#endif

        private static void MakerAPI_MakerFinishedLoading(object sender, EventArgs e) => ClothingUnlockToggle.SetValue(GetController(MakerAPI.GetCharacterControl()).GetClothingUnlocked());
        private static void MakerAPI_ReloadCustomInterface(object sender, EventArgs e) => ClothingUnlockToggle.SetValue(GetController(MakerAPI.GetCharacterControl()).GetClothingUnlocked());

        public static ClothingUnlockerController GetController(ChaControl chaControl) => chaControl == null ? null : chaControl.gameObject.GetComponent<ClothingUnlockerController>();
    }
}