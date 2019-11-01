//Additional floor, tit and hands db/colliders (eroigame.net)
// Physics tweaks as described in http://eroigame.net/archives/1387
// Done programatically, so it works on any skeleton and can adapt to tit sizes
// Ported from Patchwork
using BepInEx.Configuration;
using BepInEx.Harmony;
using HarmonyLib;
using KKAPI;
using KKAPI.Chara;
using System.Collections.Generic;
using UnityEngine;
using BepInEx.Logging;
using IllusionUtility.GetUtility;
#if AI
using AIChara;
using static AIChara.ChaFileDefine;
#else
using static ChaFileDefine;
#endif

namespace KK_Plugins
{
    public partial class KK_Colliders
    {
        public const string GUID = "com.deathweasel.bepinex.colliders";
        public const string PluginName = "Colliders";
        public const string Version = "1.0";
        internal static new ManualLogSource Logger;

        public static ConfigEntry<bool> BreastColliders { get; private set; }
        public static ConfigEntry<bool> SkirtColliders { get; private set; }

        internal void Main()
        {
            Logger = base.Logger;

            CharacterApi.RegisterExtraBehaviour<ColliderController>("com.deathweasel.bepinex.colliders");
            HarmonyWrapper.PatchAll(typeof(KK_Colliders));

            BreastColliders = Config.AddSetting("Config", "Breast Colliders", true, "Whether breast colliders are enabled. Makes breasts interact and collide with arms, hands, etc.");
            SkirtColliders = Config.AddSetting("Config", "Skirt Colliders", true, "Whether breast colliders are enabled. Makes breasts interact and collide with legs, hands, etc.");
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.Reload))]
        public static void Reload(ChaControl __instance) => GetController(__instance).SetUpColliders();

        private static ColliderController GetController(ChaControl character) => character?.gameObject?.GetComponent<ColliderController>();

        public partial class ColliderController : CharaCustomFunctionController
        {
            private bool DidColliders = false;

            protected override void OnCardBeingSaved(GameMode currentGameMode) { }
            protected override void OnReload(GameMode currentGameMode, bool maintainState) => SetUpColliders();

            public void SetUpColliders()
            {
                if (DidColliders)
                    return;

                TweakBody();
#if KK
                TweakSkirt();
#endif
                DidColliders = true;
            }

            private void TweakBody()
            {
                if (!BreastColliders.Value)
                    return;

                var bc = new List<DynamicBoneCollider>();

                // floor pseudo-collider
                bc.Add(AddCollider(RootBoneName, sz: 100, t: new Vector3(0, -10.01f, -0.01f)));

                float titsize = ChaControl.chaFile.custom.body.shapeValueBody[(int)BodyShapeIdx.BustSize];
                //XXX: maybe skip for small tits in general?
                //if (ctrl.sex == 0) return;
                //if (titsize < 0.2f) return;

                // bind colliders to hands
                foreach (var armBone in ArmBoneNames)
                    bc.Add(AddCollider(armBone, sx: 0.35f, sy: 0.35f, sz: 0.3f));

                // large tits need special case as the standard hitbox morph doesn't like shape values above 1
                var cowfactor = System.Math.Max(1f, titsize);

                // tell the tits that hands collide it
                foreach (var tit in ChaControl.objAnim.GetComponentsInChildren<DynamicBone_Ver02>(true))
                {
                    if (!TitComments.Contains(tit.Comment))
                        continue;

                    // register the colliders if not already there
                    foreach (var c in bc)
                        if (c != null && !tit.Colliders.Contains(c))
                            tit.Colliders.Add(c);

                    // expand the collision radius for the first two dynbones
                    foreach (var pat in tit.Patterns)
                    {
                        pat.Params[0].CollisionRadius = 0.08f * cowfactor;
                        pat.Params[1].CollisionRadius = 0.06f * cowfactor;
                    }

                    tit.GetType().GetMethod("InitNodeParticle", AccessTools.all).Invoke(tit, null);
                    tit.GetType().GetMethod("SetupParticles", AccessTools.all).Invoke(tit, null);
                    tit.InitLocalPosition();
                    if ((bool)tit.GetType().GetMethod("IsRefTransform", AccessTools.all).Invoke(tit, null))
                        tit.setPtn(0, true);
                    tit.GetType().GetMethod("InitTransforms", AccessTools.all).Invoke(tit, null);
                }
            }

            private DynamicBoneCollider AddCollider(string boneName, float sx = 1, float sy = 1, float sz = 1, Vector3 r = new Vector3(), Vector3 t = new Vector3())
            {
                string hitbone = boneName + "_hit";

                // check if the bone exists
                var bone = ChaControl.objAnim.transform.FindLoop(boneName);
                if (bone == null)
                    return null;

                // some collider is already in there, so just keep that
                var bo = ChaControl.objAnim.transform.FindLoop(hitbone);
                if (bo != null)
                    return null;

                // build the collider
                var nb = new GameObject(hitbone);
                var col = nb.AddComponent<DynamicBoneCollider>();
                col.m_Radius = 0.1f;
                col.m_Direction = DynamicBoneCollider.Direction.Y;
                nb.transform.SetParent(bone.transform, false);
                nb.transform.localScale = new Vector3(sx, sy, sz);
                nb.transform.localEulerAngles = r;
                nb.transform.localPosition = t;
                nb.layer = 12;
                return col;
            }
        }
    }
}