using BepInEx;
using Harmony;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using System.Collections;
using System.Linq;
using UniRx;
using UnityEngine;
#if KK
using ExtensibleSaveFormat;
#elif EC
using EC.Core.ExtensibleSaveFormat;
#endif
/// <summary>
/// Sets the selected characters invisible in Studio. Invisible state saves and loads with the scene.
/// Also sets female characters invisible in H scenes.
/// </summary>
namespace InvisibleBody
{
    public partial class InvisibleBody : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.invisiblebody";
        public const string PluginName = "Invisible Body";
        public const string PluginNameInternal = "KK_InvisibleBody";
        public const string Version = "1.2.1";

        private static MakerToggle InvisibleToggle;

        private void Awake()
        {
            CharacterApi.RegisterExtraBehaviour<InvisibleBodyCharaController>("KK_InvisibleBody");
            MakerAPI.RegisterCustomSubCategories += MakerAPI_RegisterCustomSubCategories;
        }

        private void MakerAPI_RegisterCustomSubCategories(object sender, RegisterSubCategoriesEvent e)
        {
            InvisibleToggle = e.AddControl(new MakerToggle(MakerConstants.Body.All, "Invisible Body", false, this));
            InvisibleToggle.ValueChanged.Subscribe(Observer.Create<bool>(delegate { GetController(MakerAPI.GetCharacterControl()).Visible = !InvisibleToggle.Value; }));
        }
        /// <summary>
        /// For changing head shape. Also for low poly.
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.InitShapeFace))]
        public static void InitShapeFace(ChaControl __instance) => GetController(__instance).UpdateVisible(true);
        /// <summary>
        /// Get the InvisibleBodyCharaController for the character
        /// </summary>
        public static InvisibleBodyCharaController GetController(ChaControl character) => character?.gameObject?.GetComponent<InvisibleBodyCharaController>();

        public class InvisibleBodyCharaController : CharaCustomFunctionController
        {
            private bool visible = true;
            public bool Visible
            {
                get => visible;
                set
                {
                    visible = value;
                    SetVisibleState();
                }
            }

            protected override void OnCardBeingSaved(GameMode currentGameMode)
            {
                var data = new PluginData();
                data.data.Add("Visible", Visible);
                SetExtendedData(data);
            }

            protected override void OnReload(GameMode currentGameMode, bool maintainState)
            {
                Visible = true;

                var data = GetExtendedData();
                if (data != null && data.data.TryGetValue("Visible", out var loadedVisibleState))
                    visible = (bool)loadedVisibleState;

                if (MakerAPI.InsideAndLoaded)
                    InvisibleToggle.SetValue(!Visible, false);

                if (Visible)
                    SetVisibleState();
                else
                    //Visible state will be set next frame, otherwise the head will be visible and not the body
                    ChaControl.StartCoroutine(WaitAndSetVisibleState());
            }
            /// <summary>
            /// Update the visibility state of the character
            /// </summary>
            /// <param name="wait"></param>
            public void UpdateVisible(bool wait)
            {
                if (wait)
                    ChaControl.StartCoroutine(WaitAndSetVisibleState());
                else
                    SetVisibleState();
            }
            /// <summary>
            /// Wait one frame and set visible state
            /// </summary>
            private IEnumerator WaitAndSetVisibleState()
            {
                yield return null;
                while (ChaControl.objBody == null || ChaControl.objHead == null || ChaControl.objHeadBone == null || ChaControl.objAnim == null)
                    yield return null;

                SetVisibleState();
            }
            /// <summary>
            /// Sets the visibility state of a character.
            /// </summary>
            private void SetVisibleState()
            {
                //Don't set the visible state if it is already set
                if (ChaControl.objBody.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(x => x.name == "o_body_a").GetComponent<Renderer>().enabled == Visible)
                    return;

                Transform cf_j_root = ChaControl.gameObject.transform.Find("BodyTop/p_cf_body_bone/cf_j_root");
                if (cf_j_root != null)
                    IterateVisible(cf_j_root.gameObject);

                //low poly
                Transform cf_j_root_low = ChaControl.gameObject.transform.Find("BodyTop/p_cf_body_bone_low/cf_j_root");
                if (cf_j_root_low != null)
                    IterateVisible(cf_j_root_low.gameObject);

                //female
                Transform cf_o_rootf = ChaControl.gameObject.transform.Find("BodyTop/p_cf_body_00/cf_o_root/");
                if (cf_o_rootf != null)
                    IterateVisible(cf_o_rootf.gameObject);

                //female low poly
                Transform cf_o_rootf_low = ChaControl.gameObject.transform.Find("BodyTop/p_cf_body_00_low/cf_o_root/");
                if (cf_o_rootf_low != null)
                    IterateVisible(cf_o_rootf_low.gameObject);

                //male
                Transform cf_o_rootm = ChaControl.gameObject.transform.Find("BodyTop/p_cm_body_00/cf_o_root/");
                if (cf_o_rootm != null)
                    IterateVisible(cf_o_rootm.gameObject);

                //male low poly
                Transform cf_o_rootm_low = ChaControl.gameObject.transform.Find("BodyTop/p_cm_body_00_low/cf_o_root/");
                if (cf_o_rootm_low != null)
                    IterateVisible(cf_o_rootm_low.gameObject);
            }
            /// <summary>
            /// Sets the visible state of the game object and all it's children.
            /// </summary>
            private void IterateVisible(GameObject go)
            {
                //Log($"Game Object:{DebugFullObjectPath(go)}");
                for (int i = 0; i < go.transform.childCount; i++)
                {
                    if (HideHairAccessories.Value && go.name.StartsWith("a_n_") && go.transform.parent.gameObject.name == "ct_hairB")
                        //change visibility of accessories built in to back hairs
                        IterateVisible(go.transform.GetChild(i).gameObject);
                    else if (go.name.StartsWith("a_n_")) { }
                    //do not change visibility of attached items such as studio items and character accessories
                    //Log(LogLevel.None, $"not hiding attached items for {go.name}");
                    else
                        //change visibility of everything else
                        IterateVisible(go.transform.GetChild(i).gameObject);
                }

                if (go.GetComponent<Renderer>())
                    go.GetComponent<Renderer>().enabled = Visible;
            }
            /// <summary>
            /// Recursively finds the parents of a game object and builds a string of the full path. Only used for debug purposes.
            /// </summary>
            private static string DebugFullObjectPath(GameObject go) => go.transform.parent == null ? go.name : DebugFullObjectPath(go.transform.parent.gameObject) + "/" + go.name;
        }
    }
}
