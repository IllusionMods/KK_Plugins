using BepInEx.Configuration;
using BepInEx.Logging;
using KKAPI.Chara;
using KKAPI.Studio;
using KKAPI.Studio.SaveLoad;
using KKAPI.Studio.UI;
using KKAPI.Utilities;
using Studio;
using UniRx;
#if AI
using AIChara;
#endif

namespace KK_Plugins
{
    public partial class Colliders
    {
        public const string GUID = "com.deathweasel.bepinex.studiocolliders";
        public const string PluginName = "Colliders";
        public const string Version = "1.1";
        internal static new ManualLogSource Logger;

        public static ConfigEntry<bool> ConfigBreastColliders { get; private set; }
        public static ConfigEntry<bool> ConfigFloorCollider { get; private set; }
        public static ConfigEntry<DefaultStudioSettings> ConfigDefaultStudioSettings { get; private set; }
        public enum DefaultStudioSettings { Config, On, Off }

        internal void Main()
        {
            Logger = base.Logger;
            CharacterApi.RegisterExtraBehaviour<ColliderController>(GUID);
            StudioSaveLoadApi.RegisterExtraBehaviour<ColliderSceneController>(GUID);

            ConfigBreastColliders = Config.Bind("Config", "Breast Colliders", true, new ConfigDescription("Whether breast colliders are enabled. Makes breasts interact and collide with arms, hands, etc.", null, new ConfigurationManagerAttributes { Order = 10 }));
            ConfigFloorCollider = Config.Bind("Config", "Floor Collider", true, new ConfigDescription("Adds a floor collider so hair doesn't clip through the floor when laying down.", null, new ConfigurationManagerAttributes { Order = 1 }));
            ConfigDefaultStudioSettings = Config.Bind("Config", "Default Studio Settings", DefaultStudioSettings.Off, new ConfigDescription("Default state of colliders for new characters in Studio or for older scenes.\nScenes made with this plugin will load with colliders enabled or disabled depending on how the character in scene was configured.", null, new ConfigurationManagerAttributes { Order = 0 }));

            ConfigBreastColliders.SettingChanged += ConfigBreastColliders_SettingChanged;
            ConfigFloorCollider.SettingChanged += ConfigFloorCollider_SettingChanged;

            RegisterStudioControls();
        }

        /// <summary>
        /// Apply colliders on setting change
        /// </summary>
        private void ConfigBreastColliders_SettingChanged(object sender, System.EventArgs e)
        {
            if (StudioAPI.InsideStudio) return;

            foreach (var chaControl in FindObjectsOfType<ChaControl>())
            {
                var controller = GetController(chaControl);
                if (controller == null) continue;

                controller.BreastCollidersEnabled = ConfigBreastColliders.Value;
                GetController(chaControl).ApplyBreastColliders();
            }
        }

        /// <summary>
        /// Apply colliders on setting change
        /// </summary>
        private void ConfigFloorCollider_SettingChanged(object sender, System.EventArgs e)
        {
            if (StudioAPI.InsideStudio) return;

            foreach (var chaControl in FindObjectsOfType<ChaControl>())
            {
                var controller = GetController(chaControl);
                if (controller == null) continue;

                controller.FloorColliderEnabled = ConfigFloorCollider.Value;
                GetController(chaControl).ApplyFloorCollider();
            }
        }

        private static void RegisterStudioControls()
        {
            if (!StudioAPI.InsideStudio) return;

            var breast = new CurrentStateCategorySwitch("Breast", ocichar => ocichar.charInfo.GetComponent<ColliderController>().BreastCollidersEnabled);
            breast.Value.Subscribe(value =>
            {
                var controller = GetSelectedController();
                if (controller == null) return;
                if (controller.BreastCollidersEnabled != value)
                {
                    controller.BreastCollidersEnabled = value;
                    controller.ApplyBreastColliders();
                }
            });

#if KK
            var skirt = new CurrentStateCategorySwitch("Skirt", ocichar => ocichar.charInfo.GetComponent<ColliderController>().SkirtCollidersEnabled);
            skirt.Value.Subscribe(value =>
            {
                var controller = GetSelectedController();
                if (controller == null) return;
                if (controller.SkirtCollidersEnabled != value)
                {
                    controller.SkirtCollidersEnabled = value;
                    controller.ApplySkirtColliders();
                }
            });
#endif

            var floor = new CurrentStateCategorySwitch("Floor", ocichar => ocichar.charInfo.GetComponent<ColliderController>().FloorColliderEnabled);
            floor.Value.Subscribe(value =>
            {
                var controller = GetSelectedController();
                if (controller == null) return;
                if (controller.FloorColliderEnabled != value)
                {
                    controller.FloorColliderEnabled = value;
                    controller.ApplyFloorCollider();
                }
            });

            StudioAPI.GetOrCreateCurrentStateCategory("Colliders").AddControl(breast);
#if KK
            StudioAPI.GetOrCreateCurrentStateCategory("Colliders").AddControl(skirt);
#endif
            StudioAPI.GetOrCreateCurrentStateCategory("Colliders").AddControl(floor);
        }

        public static ColliderController GetController(ChaControl chaControl) => chaControl.GetComponent<ColliderController>();
        private static ColliderController GetSelectedController() => FindObjectOfType<MPCharCtrl>()?.ociChar?.charInfo?.GetComponent<ColliderController>();

        public class ColliderSceneController : SceneCustomFunctionController
        {
            protected override void OnSceneSave() { }
            protected override void OnSceneLoad(SceneOperationKind operation, ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems)
            {
                foreach (var controller in FindObjectsOfType<ColliderController>())
                    controller.ApplyColliders();
            }
        }
    }
}