using BepInEx;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using HarmonyLib;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Maker.UI;

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

        private void Start()
        {
            Logger = base.Logger;

            MakerAPI.MakerBaseLoaded += MakerAPI_MakerBaseLoaded;
            CharacterApi.RegisterExtraBehaviour<CharaController>(PluginGUID);
            AccessoriesApi.AccessoryKindChanged += AccessoriesApi_AccessoryKindChanged;
            var harmony = Harmony.CreateAndPatchAll(typeof(Hooks));
        }

        private void MakerAPI_MakerBaseLoaded(object s, RegisterCustomControlsEvent e)
        {
            UI.InitUI();
            MakerAPI.AddAccessoryWindowControl(new MakerButton("Dynamic Bone Editor", null, this)).OnClick.AddListener(() => UI.ShowUI(0));
        }

        private static void AccessoriesApi_AccessoryKindChanged(object sender, AccessorySlotEventArgs e)
        {
            var controller = GetMakerCharaController();
            if (controller != null)
                controller.AccessoryKindChangeEvent(sender, e);
        }

        public static CharaController GetCharaController(ChaControl chaControl) => chaControl == null ? null : chaControl.gameObject.GetComponent<CharaController>();
        public static CharaController GetMakerCharaController() => MakerAPI.GetCharacterControl().gameObject.GetComponent<CharaController>();
    }
}
