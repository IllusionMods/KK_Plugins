using BepInEx.Harmony;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using HarmonyLib;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using System.Collections;
using System.Linq;
using UniRx;
using UnityEngine;
#if AI
using AIChara;
#endif

namespace KK_Plugins
{
    /// <summary>
    /// Sets the selected characters invisible in Studio or character maker. Invisible state saves and loads with the scene or card.
    /// </summary>
    public partial class InvisibleBody
    {
        public const string GUID = "com.deathweasel.bepinex.invisiblebody";
        public const string PluginName = "Invisible Body";
        public const string PluginNameInternal = "KK_InvisibleBody";
        public const string Version = "1.3.1";
        internal static new ManualLogSource Logger;

        private static MakerToggle InvisibleToggle;

        internal void Start()
        {
            Logger = base.Logger;

            CharacterApi.RegisterExtraBehaviour<InvisibleBodyCharaController>(PluginNameInternal);
            MakerAPI.RegisterCustomSubCategories += MakerAPI_RegisterCustomSubCategories;

            HarmonyWrapper.PatchAll(typeof(InvisibleBody));
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
        internal static void InitShapeFace(ChaControl __instance) => GetController(__instance).UpdateVisible(true);
        /// <summary>
        /// Get the InvisibleBodyCharaController for the character
        /// </summary>
        public static InvisibleBodyCharaController GetController(ChaControl character) => character?.gameObject?.GetComponent<InvisibleBodyCharaController>();

        public class InvisibleBodyCharaController : CharaCustomFunctionController
        {
            private bool _visible = true;
            /// <summary>
            /// Gets or sets the visible state of a character
            /// </summary>
            public bool Visible
            {
                get => _visible;
                set
                {
                    _visible = value;
                    SetVisibleState();
                }
            }
            /// <summary>
            /// Same thing as Visible except backwards. Because logic is hard.
            /// </summary>
            public bool Invisible
            {
                get => !Visible;
                set => Visible = !value;
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
                    _visible = (bool)loadedVisibleState;

                if (MakerAPI.InsideAndLoaded)
                    InvisibleToggle.SetValue(Invisible, false);

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
                if (ChaControl?.objBody?.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(x => x.name == "o_body_a" || x.name == "o_body_cf" || x.name == "o_body_cm")?.GetComponent<Renderer>().enabled == Visible)
                    return;

#if AI
                //male
                Transform p_cm_body_00 = ChaControl.gameObject.transform.Find("BodyTop/p_cm_body_00");
                if (p_cm_body_00 != null)
                    IterateVisible(p_cm_body_00.gameObject);

                //female
                Transform p_cf_body_00 = ChaControl.gameObject.transform.Find("BodyTop/p_cf_body_00");
                if (p_cf_body_00 != null)
                    IterateVisible(p_cf_body_00.gameObject);

                //both
                Transform p_cf_anim = ChaControl.gameObject.transform.Find("BodyTop/p_cf_anim");
                if (p_cf_anim != null)
                    IterateVisible(p_cf_anim.gameObject);
#else

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
#endif
            }
            /// <summary>
            /// Sets the visible state of the game object and all it's children.
            /// </summary>
            private void IterateVisible(GameObject go)
            {
                //Logger.LogInfo($"Game Object:{DebugFullObjectPath(go)}");

                //Search through all child transforms and toggle visibility, except for transforms that contain accessories or studio objects
                for (int i = 0; i < go.transform.childCount; i++)
                    if (!go.name.StartsWith("a_n_") && !go.name.StartsWith("ca_slot"))
                        IterateVisible(go.transform.GetChild(i).gameObject);

                Renderer rend = go.GetComponent<Renderer>();
                if (rend != null)
                {
#if AI
                    if (RendererBlacklist.Contains(rend.name))
                        return;
#endif
                    rend.enabled = Visible;
                }
            }
            /// <summary>
            /// Recursively finds the parents of a game object and builds a string of the full path. Only used for debug purposes.
            /// </summary>
            private static string DebugFullObjectPath(GameObject go) => go.transform.parent == null ? go.name : DebugFullObjectPath(go.transform.parent.gameObject) + "/" + go.name;
        }
    }
}
