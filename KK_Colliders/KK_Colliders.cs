//Additional floor, tit and hands db/colliders (eroigame.net)
// Physics tweaks as described in http://eroigame.net/archives/1387
// Done programatically, so it works on any skeleton and can adapt to tit sizes
// Ported from Patchwork
using BepInEx;
using Harmony;
using KKAPI;
using KKAPI.Chara;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using static ChaFileDefine;

namespace KK_Colliders
{
    [BepInDependency(KoikatuAPI.GUID)]
    [BepInPlugin(GUID, PluginName, Version)]
    public class KK_Colliders : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.colliders";
        public const string PluginName = "Colliders";
        public const string PluginNameInternal = "KK_Colliders";
        public const string Version = "1.0";

        [DisplayName("Breast Colliders")]
        [Category("Config")]
        public static ConfigWrapper<bool> BreastColliders { get; private set; }
        [DisplayName("Skirt Colliders")]
        [Category("Config")]
        public static ConfigWrapper<bool> SkirtColliders { get; private set; }

        private void Main()
        {
            CharacterApi.RegisterExtraBehaviour<ColliderController>("com.deathweasel.bepinex.colliders");
            var harmony = HarmonyInstance.Create(GUID);
            harmony.PatchAll(typeof(KK_Colliders));

            BreastColliders = new ConfigWrapper<bool>("BreastColliders", PluginNameInternal, true);
            SkirtColliders = new ConfigWrapper<bool>("SkirtColliders", PluginNameInternal, true);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.Reload))]
        public static void Reload(ChaControl __instance) => GetController(__instance).SetUpColliders();

        private static ColliderController GetController(ChaControl character) => character?.gameObject?.GetComponent<ColliderController>();

        public class ColliderController : CharaCustomFunctionController
        {
            private bool DidColliders = false;

            protected override void OnCardBeingSaved(GameMode currentGameMode) { }
            protected override void OnReload(GameMode currentGameMode, bool maintainState) => SetUpColliders();

            public void SetUpColliders()
            {
                if (DidColliders)
                    return;

                if (BreastColliders.Value)
                    foreach (var x in ChaControl?.gameObject?.GetComponentsInChildren<Transform>(true))
                        if (x.name == "p_cf_body_bone")
                            TweakBody(x.gameObject);

                if (SkirtColliders.Value)
                    foreach (DynamicBone x in ChaControl?.gameObject?.GetComponentsInChildren<DynamicBone>(true))
                        if (x.name == "ct_clothesBot")
                            TweakSkirt(x);

                DidColliders = true;
            }

            private void TweakSkirt(DynamicBone db)
            {
                var radius = new float[] { 0.03f, 0.045f, 0.02f, 0.045f, 0.03f, 0.045f, 0.02f, 0.045f };

                int idx = int.Parse(db.m_Root.name.Split('_')[3]);
                db.m_Radius = radius[idx];
                db.m_FreezeAxis = DynamicBone.FreezeAxis.X;
                if (idx == 0)
                {
                    var keys = db.m_RadiusDistrib.keys;
                    keys[keys.Length - 1].value = 1.6f;
                    db.m_RadiusDistrib.keys = keys;
                }
                db.GetType().GetMethod("SetupParticles", AccessTools.all).Invoke(db, null);
            }

            private void TweakBody(GameObject go)
            {
                var bc = new List<DynamicBoneCollider>();

                // floor pseudo-collider
                bc.Add(AddCollider(go, "cf_j_root", sz: 100, t: new Vector3(0, -10.01f, -0.01f)));

                float titsize = ChaControl.chaFile.custom.body.shapeValueBody[(int)BodyShapeIdx.BustSize];
                //XXX: maybe skip for small tits in general?
                //if (ctrl.sex == 0) return;
                //if (titsize < 0.2f) return;

                // bind colliders to hands
                foreach (var n in "forearm02 arm02 hand".Split(' '))
                {
                    bc.Add(AddCollider(go, "cf_s_" + n + "_L", sx: 0.35f, sy: 0.35f, sz: 0.3f));
                    bc.Add(AddCollider(go, "cf_s_" + n + "_R", sx: 0.35f, sy: 0.35f, sz: 0.3f));
                }

                // large tits need special case as the standard hitbox morph doesn't like shape values above 1
                var cowfactor = System.Math.Max(1f, titsize);

                // tell the tits that hands collide it
                foreach (var tit in go.GetComponentsInChildren<DynamicBone_Ver02>(true))
                {
                    if ((tit.Comment != "右胸") && (tit.Comment != "左胸"))
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

            private DynamicBoneCollider AddCollider(GameObject go, string bone, float sx = 1, float sy = 1, float sz = 1, Vector3 r = new Vector3(), Vector3 t = new Vector3())
            {
                FindAssist fa = new FindAssist();
                fa.Initialize(go.transform);
                var hitbone = bone + "_hit";

                var bo = fa.GetObjectFromName(hitbone);
                if (bo != null)
                    return null;

                // some collider is already in there, so just keep that
                var parent = fa.GetObjectFromName(bone);
                if (parent == null)
                    return null;

                // build the collider
                var nb = new GameObject(hitbone);
                var col = nb.AddComponent<DynamicBoneCollider>();
                col.m_Radius = 0.1f;
                col.m_Direction = DynamicBoneCollider.Direction.Y;
                nb.transform.SetParent(parent.transform, false);
                nb.transform.localScale = new Vector3(sx, sy, sz);
                nb.transform.localEulerAngles = r;
                nb.transform.localPosition = t;
                nb.layer = 12;
                return col;
            }
        }
    }
}