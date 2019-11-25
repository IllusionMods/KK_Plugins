using BepInEx;
using BepInEx.Configuration;
using BepInEx.Harmony;
using CommonCode;
using KKAPI;
using KKAPI.Studio;
using System.Collections.Generic;
using UnityEngine;

namespace KK_Plugins
{
    [BepInDependency(KoikatuAPI.GUID)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class Colliders : BaseUnityPlugin
    {
        public const string PluginNameInternal = "KK_Colliders";
        public static ConfigEntry<bool> ConfigSkirtColliders { get; private set; }

        internal static ColliderData FloorColliderData = new ColliderData("cf_j_root", 100f, 0f, new Vector3(0, -100.01f, 0f));
        internal static readonly List<ColliderData> ArmColliderDataList = new List<ColliderData>() {
            { new ColliderData("cf_s_hand_L", 0.020f, 0.075f, new Vector3(-0.03f, -0.005f, 0f)) },
            { new ColliderData("cf_s_hand_R", 0.020f, 0.075f, new Vector3(0.03f, -0.005f, 0f)) },
            { new ColliderData("cf_s_forearm02_L", 0.025f, 0.25f, new Vector3(-0.03f, 0f, 0f)) },
            { new ColliderData("cf_s_forearm02_R", 0.025f, 0.25f, new Vector3(0.03f, 0f, 0f)) },
            { new ColliderData("cf_s_arm02_L", 0.025f, 0.25f, new Vector3(0f, 0f, 0f)) },
            { new ColliderData("cf_s_arm02_R", 0.025f, 0.25f, new Vector3(0f, 0f, 0f)) },
            };
        internal static readonly List<ColliderData> LegColliderDataList = new List<ColliderData>() {
            { new ColliderData("cf_s_thigh01_L", 0.095f, 0.32f, new Vector3(0.05f, -0.1f, -0.015f), DynamicBoneCollider.Direction.Y) },
            { new ColliderData("cf_s_thigh01_R", 0.095f, 0.32f, new Vector3(-0.05f, -0.1f, -0.015f), DynamicBoneCollider.Direction.Y) },
            { new ColliderData("cf_s_thigh01_L", 0.095f, 0.32f, new Vector3(0.01f, -0.125f, -0.015f), DynamicBoneCollider.Direction.Y, "_2") },
            { new ColliderData("cf_s_thigh01_R", 0.095f, 0.32f, new Vector3(-0.01f, -0.125f, -0.015f), DynamicBoneCollider.Direction.Y, "_2") },
            { new ColliderData("cf_s_thigh02_L", 0.083f, 0.35f, new Vector3(-0.0065f, 0f, -0.012f), DynamicBoneCollider.Direction.Y) },
            { new ColliderData("cf_s_thigh02_R", 0.083f, 0.35f, new Vector3(0.0065f, 0f, -0.012f), DynamicBoneCollider.Direction.Y) },
            };

        internal static readonly HashSet<string> BreastDBComments = new HashSet<string>() { "右胸", "左胸" };

        internal void Start()
        {
            ConfigSkirtColliders = Config.Bind("Config", "Skirt Colliders", true, new ConfigDescription("Extra colliders for the legs to cause less skirt clipping.", null, new ConfigurationManagerAttributes { Order = 5 }));
            ConfigSkirtColliders.SettingChanged += ConfigSkirtColliders_SettingChanged;

            HarmonyWrapper.PatchAll(typeof(Hooks));
        }

        /// <summary>
        /// Apply colliders on setting change
        /// </summary>
        private void ConfigSkirtColliders_SettingChanged(object sender, System.EventArgs e)
        {
            if (StudioAPI.InsideStudio) return;

            foreach (var chaControl in FindObjectsOfType<ChaControl>())
            {
                var controller = GetController(chaControl);
                if (controller == null) continue;

                controller.SkirtCollidersEnabled = ConfigSkirtColliders.Value;
                GetController(chaControl).ApplySkirtColliders();
            }
        }
    }
}