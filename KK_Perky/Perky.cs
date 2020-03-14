using BepInEx;
using BepInEx.Harmony;
using BepInEx.Logging;
using HarmonyLib;
using IllusionUtility.GetUtility;
using KKAPI;
using KKAPI.Chara;
using System.Collections;
using UnityEngine;

namespace KK_Plugins
{
    [BepInDependency(KoikatuAPI.GUID)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class Perky : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.perky";
        public const string PluginName = "Perky Tiddies";
        public const string PluginNameInternal = "KK_Perky";
        public const string Version = "0.1";
        internal static new ManualLogSource Logger;

        internal void Main()
        {
            Logger = base.Logger;
            HarmonyWrapper.PatchAll(typeof(Hooks));
            CharacterApi.RegisterExtraBehaviour<PerkyController>(GUID);
        }

        public static PerkyController GetCharaController(ChaControl character) => character?.gameObject?.GetComponent<PerkyController>();

        public class Hooks
        {
            //Pushup compatibility
            [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetClothesState))]
            internal static void SetClothesStatePostfix(ChaControl __instance) => GetCharaController(__instance)?.ClothesStateChangeEvent();
        }

        public class PerkyController : CharaCustomFunctionController
        {
            bool Perky = false;

            Transform cf_d_bust02_L;
            Transform cf_d_bust03_L;
            Transform cf_d_bust02_R;
            Transform cf_d_bust03_R;

            Quaternion cf_d_bust02_L_Rotation;
            Quaternion cf_d_bust03_L_Rotation;
            Quaternion cf_d_bust02_R_Rotation;
            Quaternion cf_d_bust03_R_Rotation;

            protected override void Update()
            {
                if (Input.GetKeyDown(KeyCode.G))
                    if (Perky)
                        DePerkify();
                    else
                        Perkify();

                base.Update();
            }

            protected override void OnReload(GameMode currentGameMode, bool maintainState) => StartCoroutine(PerkifyInit());

            protected override void OnCardBeingSaved(GameMode currentGameMode) { }

            private IEnumerator PerkifyInit()
            {
                yield return null;
                yield return null;

                cf_d_bust02_L = ChaControl.objBodyBone.transform.FindLoop("cf_d_bust02_L").transform;
                cf_d_bust02_L_Rotation = cf_d_bust02_L.localRotation;
                cf_d_bust03_L = ChaControl.objBodyBone.transform.FindLoop("cf_d_bust03_L").transform;
                cf_d_bust03_L_Rotation = cf_d_bust03_L.localRotation;
                cf_d_bust02_R = ChaControl.objBodyBone.transform.FindLoop("cf_d_bust02_R").transform;
                cf_d_bust02_R_Rotation = cf_d_bust02_R.localRotation;
                cf_d_bust03_R = ChaControl.objBodyBone.transform.FindLoop("cf_d_bust03_R").transform;
                cf_d_bust03_R_Rotation = cf_d_bust03_R.localRotation;
                Perkify();
            }

            private IEnumerator PerkifyCo()
            {
                yield return null;
                Perkify();
            }

            private void Perkify()
            {
                if (cf_d_bust02_L == null) return;

                int perkRate;
                if (CurrentlyWearing == Wearing.None)
                    perkRate = 7;
                else
                    perkRate = 15;

                cf_d_bust02_L.localRotation = new Quaternion(ChaControl.chaFile.custom.body.shapeValueBody[4] / perkRate, 0f, 0f, -1f);
                cf_d_bust03_L.localRotation = new Quaternion(ChaControl.chaFile.custom.body.shapeValueBody[4] / perkRate, 0f, 0f, -1f);
                cf_d_bust02_R.localRotation = new Quaternion(ChaControl.chaFile.custom.body.shapeValueBody[4] / perkRate, 0f, 0f, -1f);
                cf_d_bust03_R.localRotation = new Quaternion(ChaControl.chaFile.custom.body.shapeValueBody[4] / perkRate, 0f, 0f, -1f);

                Perky = true;
            }

            private void DePerkify()
            {
                cf_d_bust02_L.localRotation = cf_d_bust02_L_Rotation;
                cf_d_bust03_L.localRotation = cf_d_bust03_L_Rotation;
                cf_d_bust02_R.localRotation = cf_d_bust02_R_Rotation;
                cf_d_bust03_R.localRotation = cf_d_bust03_R_Rotation;

                Perky = false;
            }

            internal void ClothesStateChangeEvent()
            {
                if (!Started) return;
                StartCoroutine(PerkifyCo());
            }

            internal enum Wearing { None, Bra, Top, Both }

            private Wearing CurrentlyWearing
            {
                get
                {
                    var braIsOnAndEnabled = ChaControl.IsClothesStateKind((int)ChaFileDefine.ClothesKind.bra) && ChaControl.fileStatus.clothesState[(int)ChaFileDefine.ClothesKind.bra] == 0;
                    var topIsOnAndEnabled = ChaControl.IsClothesStateKind((int)ChaFileDefine.ClothesKind.top) && ChaControl.fileStatus.clothesState[(int)ChaFileDefine.ClothesKind.top] == 0;

                    if (topIsOnAndEnabled)
                        return braIsOnAndEnabled ? Wearing.Both : Wearing.Top;

                    return braIsOnAndEnabled ? Wearing.Bra : Wearing.None;
                }
            }
        }
    }
}
