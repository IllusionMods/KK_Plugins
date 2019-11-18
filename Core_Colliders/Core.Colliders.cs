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
using System.Collections;
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

            BreastColliders = Config.Bind("Config", "Breast Colliders", true, "Whether breast colliders are enabled. Makes breasts interact and collide with arms, hands, etc.");
            SkirtColliders = Config.Bind("Config", "Skirt Colliders", true, "Whether skirt colliders will be modified. Modifies skirt colliders to cause less clipping problems");
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.Reload))]
        public static void Reload(ChaControl __instance) => GetController(__instance).SetUpColliders();

        private static ColliderController GetController(ChaControl character) => character?.gameObject?.GetComponent<ColliderController>();

        public partial class ColliderController : CharaCustomFunctionController
        {
            protected override void OnCardBeingSaved(GameMode currentGameMode) { }
            protected override void OnReload(GameMode currentGameMode, bool maintainState) => SetUpColliders();

            public void SetUpColliders()
            {
                if (DidColliders) return;

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

                float titsize = ChaControl.chaFile.custom.body.shapeValueBody[(int)BodyShapeIdx.BustSize];
                float cowfactor = titsize * 1.2f;

                // bind colliders to hands
                foreach (var collider in ColliderList)
                    bc.Add(AddCollider(collider.BoneName, collider.ColliderRadius, collider.CollierHeight, collider.ColliderCenter));

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
#if KK
                        pat.Params[0].CollisionRadius = 0.08f * cowfactor;
                        pat.Params[1].CollisionRadius = 0.06f * cowfactor;
#elif AI
                        pat.Params[2].CollisionRadius = 0.8f * cowfactor;
                        pat.Params[3].CollisionRadius = 0.6f * cowfactor;
#endif
                    }

                    tit.GetType().GetMethod("InitNodeParticle", AccessTools.all).Invoke(tit, null);
                    tit.GetType().GetMethod("SetupParticles", AccessTools.all).Invoke(tit, null);
                    tit.InitLocalPosition();
                    if ((bool)tit.GetType().GetMethod("IsRefTransform", AccessTools.all).Invoke(tit, null))
                        tit.setPtn(0, true);
                    tit.GetType().GetMethod("InitTransforms", AccessTools.all).Invoke(tit, null);
                }
            }

            private DynamicBoneCollider AddCollider(string boneName, float colliderRadius = 0.5f, float collierHeight = 0f, Vector3 colliderCenter = new Vector3())
            {
                string hitbone = "KK_Colliders_" + boneName;

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
                col.m_Radius = colliderRadius;
                col.m_Height = collierHeight;
                col.m_Center = colliderCenter;
                col.m_Direction = DynamicBoneCollider.Direction.X;
                nb.transform.SetParent(bone.transform, false);
                return col;
            }

            private bool didColliders = false;
            public bool DidColliders
            {
                get => didColliders;
                set
                {
                    didColliders = value;
                    ChaControl.StartCoroutine(Reset());
                    IEnumerator Reset()
                    {
                        yield return null;
                        yield return null;
                        yield return null;
                        didColliders = false;
                    }
                }
            }
        }

        private class ColliderData
        {
            public string BoneName;
            public float ColliderRadius;
            public float CollierHeight;
            public Vector3 ColliderCenter;

            public ColliderData(string boneName, float colliderRadius, float collierHeight, Vector3 colliderCenter)
            {
                BoneName = boneName;
                ColliderRadius = colliderRadius;
                CollierHeight = collierHeight;
                ColliderCenter = colliderCenter;
            }
        }
    }
}