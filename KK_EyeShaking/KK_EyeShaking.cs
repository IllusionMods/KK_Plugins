using BepInEx;
using Harmony;
using KKAPI;
using KKAPI.Chara;
using System.ComponentModel;
/// <summary>
/// Adds shaking to a character's eye highlights when she is a virgin in an H scene
/// </summary>
namespace KK_EyeShaking
{
    [BepInPlugin(GUID, PluginName, Version)]
    public class KK_EyeShaking : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.eyeshaking";
        public const string PluginName = "Eye Shaking";
        public const string PluginNameInternal = "KK_EyeShaking";
        public const string Version = "1.0";
        [DisplayName("Enabled")]
        [Category("Config")]
        [Description("When enabled, virgins in H scenes will appear to have shaking eye highlights")]
        public static ConfigWrapper<bool> Enabled { get; private set; }

        private void Main()
        {
            var harmony = HarmonyInstance.Create(GUID);
            harmony.PatchAll(typeof(KK_EyeShaking));
            CharacterApi.RegisterExtraBehaviour<EyeShakingController>(GUID);
            Enabled = new ConfigWrapper<bool>("Enabled", PluginNameInternal, true);
        }

        private static EyeShakingController GetController(ChaControl character) => character?.gameObject?.GetComponent<EyeShakingController>();

        public class EyeShakingController : CharaCustomFunctionController
        {
            internal bool IsVirgin { get; set; } = true;
            internal bool IsVirginOrg { get; set; } = true;
            internal bool IsInit { get; set; } = false;

            protected override void OnCardBeingSaved(GameMode currentGameMode) { }
            protected override void OnReload(GameMode currentGameMode, bool maintainState) { }

            internal void HSceneStart(bool virgin)
            {
                IsVirgin = virgin;
                IsVirginOrg = virgin;
                IsInit = true;
            }

            internal void HSceneEnd()
            {
                ChaControl.ChangeEyesShaking(false);
                IsInit = false;
            }

            internal void OnInsert() => IsVirgin = false;
            internal void AddOrgasm() => IsVirginOrg = false;

            protected override void Update()
            {
                if (Enabled.Value && IsInit && (IsVirgin || IsVirginOrg))
                    ChaControl.ChangeEyesShaking(true);
            }
        }
        /// <summary>
        /// Insert vaginal
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuKokanPlay))]
        public static void AddSonyuKokanPlay(HFlag __instance) => GetController(__instance.lstHeroine[0].chaCtrl).OnInsert();
        /// <summary>
        /// Insert anal
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuAnalPlay))]
        public static void AddSonyuAnalPlay(HFlag __instance) => GetController(__instance.lstHeroine[0].chaCtrl).OnInsert();
        /// <summary>
        /// Something that happens at the end of H scene loading, good enough place to hook
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(HSceneProc), "MapSameObjectDisable")]
        public static void MapSameObjectDisable(HSceneProc __instance)
        {
            SaveData.Heroine heroine = __instance.flags.lstHeroine[0];
            GetController(heroine.chaCtrl).HSceneStart(heroine.isVirgin && heroine.isAnalVirgin);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuOrg))]
        public static void AddSonyuOrg(HFlag __instance) => GetController(__instance.lstHeroine[0].chaCtrl).AddOrgasm();
        [HarmonyPrefix, HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuSame))]
        public static void AddSonyuSame(HFlag __instance) => GetController(__instance.lstHeroine[0].chaCtrl).AddOrgasm();
        [HarmonyPrefix, HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuAnalOrg))]
        public static void AddSonyuAnalOrg(HFlag __instance) => GetController(__instance.lstHeroine[0].chaCtrl).AddOrgasm();
        [HarmonyPrefix, HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuAnalSame))]
        public static void AddSonyuAnalSame(HFlag __instance) => GetController(__instance.lstHeroine[0].chaCtrl).AddOrgasm();

        [HarmonyPrefix, HarmonyPatch(typeof(HSceneProc), "EndProc")]
        public static void EndProc(HSceneProc __instance) => GetController(__instance.flags.lstHeroine[0].chaCtrl).HSceneEnd();
    }
}