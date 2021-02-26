using BepInEx;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using HarmonyLib;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;

namespace KK_Plugins.DynamicBoneEditor
{
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInDependency(ExtendedSave.GUID, ExtendedSave.Version)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.deathweasel.bepinex.dynamicboneeditor";
        public const string PluginName = "Dynamic Bone Editor";
        public const string PluginNameInternal = Constants.Prefix + "_DynamicBoneEditor";
        public const string PluginVersion = "1.0";
        internal static new ManualLogSource Logger;
        internal static Plugin PluginInstance;

        private void Start()
        {
            Logger = base.Logger;
            PluginInstance = this;
            Harmony.CreateAndPatchAll(typeof(Hooks));

            MakerAPI.MakerBaseLoaded += MakerAPI_MakerBaseLoaded;
            MakerAPI.MakerFinishedLoading += MakerAPI_MakerFinishedLoading;
            CharacterApi.RegisterExtraBehaviour<CharaController>(PluginGUID);
            AccessoriesApi.SelectedMakerAccSlotChanged += AccessoriesApi_SelectedMakerAccSlotChanged;
            AccessoriesApi.AccessoryKindChanged += AccessoriesApi_AccessoryKindChanged;
            AccessoriesApi.AccessoryTransferred += AccessoriesApi_AccessoryTransferred;
            AccessoriesApi.AccessoriesCopied += AccessoriesApi_AccessoriesCopied;
        }

        private void MakerAPI_MakerBaseLoaded(object s, RegisterCustomControlsEvent e)
        {
            UI.InitUI();
        }

        private void MakerAPI_MakerFinishedLoading(object sender, System.EventArgs e)
        {
            UI.ToggleButtonVisibility();
        }

        private void AccessoriesApi_SelectedMakerAccSlotChanged(object sender, AccessorySlotEventArgs e)
        {
            if (MakerAPI.InsideAndLoaded)
            {
                if (UI.Visible)
                    UI.ShowUI(0);
                UI.ToggleButtonVisibility();
            }
        }

        private static void AccessoriesApi_AccessoryKindChanged(object sender, AccessorySlotEventArgs e)
        {
            var controller = GetMakerCharaController();
            if (controller != null)
                controller.AccessoryKindChangeEvent(sender, e);
        }

        private void AccessoriesApi_AccessoryTransferred(object sender, AccessoryTransferEventArgs e)
        {
            var controller = GetMakerCharaController();
            if (controller != null)
                controller.AccessoryTransferredEvent(sender, e);
        }

        private void AccessoriesApi_AccessoriesCopied(object sender, AccessoryCopyEventArgs e)
        {
            var controller = GetMakerCharaController();
            if (controller != null)
                controller.AccessoriesCopiedEvent(sender, e);
        }

        public static CharaController GetCharaController(ChaControl chaControl) => chaControl == null ? null : chaControl.gameObject.GetComponent<CharaController>();
        public static CharaController GetMakerCharaController() => MakerAPI.GetCharacterControl().gameObject.GetComponent<CharaController>();
    }
}
