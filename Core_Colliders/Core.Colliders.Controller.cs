using HarmonyLib;
using System.Collections.Generic;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Studio;
using IllusionUtility.GetUtility;
using UnityEngine;
using ExtensibleSaveFormat;
#if AI
using AIChara;
using static AIChara.ChaFileDefine;
#else
using static ChaFileDefine;
#endif

namespace KK_Plugins
{
    public partial class Colliders
    {
        public partial class ColliderController : CharaCustomFunctionController
        {
            public DynamicBoneCollider FloorCollider = null;
            private readonly List<DynamicBoneCollider> ArmColliders = new List<DynamicBoneCollider>();

            private bool applyColliders;
            private bool applyBreastColliders;
            private bool applySkirtColliders;
            private bool applyFloorCollider;
            private bool didSetStates = false;

            private float BreastSize => ChaControl.chaFile.custom.body.shapeValueBody[(int)BodyShapeIdx.BustSize];
            private float BreastCollisionRadiusMultiplier => BreastSize * 1.2f;
            public bool BreastCollidersEnabled { get; set; }
            public bool SkirtCollidersEnabled { get; set; }
            public bool FloorColliderEnabled { get; set; }

            internal void Main()
            {
                //Add the floor collider
                FloorCollider = AddCollider(FloorColliderData);

                //Add the arm and hand colliders
                foreach (var colliderData in ArmColliderDataList)
                    ArmColliders.Add(AddCollider(colliderData));

#if KK
                //Add the leg colliders for skirts
                foreach (var colliderData in LegColliderDataList)
                    LegColliders.Add(AddCollider(colliderData));
#endif
            }

            protected override void OnCardBeingSaved(GameMode currentGameMode)
            {
                var data = new PluginData();
                data.data.Add(nameof(BreastCollidersEnabled), BreastCollidersEnabled);
                data.data.Add(nameof(FloorColliderEnabled), FloorColliderEnabled);
#if KK
                data.data.Add(nameof(SkirtCollidersEnabled), SkirtCollidersEnabled);
#endif
                SetExtendedData(data);
            }
            protected override void OnReload(GameMode currentGameMode, bool maintainState)
            {
                if (StudioAPI.InsideStudio)
                {
                    if (!didSetStates)
                    {
                        if (ConfigDefaultStudioSettings.Value == DefaultStudioSettings.Config)
                        {
                            BreastCollidersEnabled = ConfigBreastColliders.Value;
                            FloorColliderEnabled = ConfigFloorCollider.Value;
#if KK
                            SkirtCollidersEnabled = ConfigSkirtColliders.Value;
#endif
                        }
                        else if (ConfigDefaultStudioSettings.Value == DefaultStudioSettings.On)
                            BreastCollidersEnabled = FloorColliderEnabled = SkirtCollidersEnabled = true;
                        else
                            BreastCollidersEnabled = FloorColliderEnabled = SkirtCollidersEnabled = false;

                        var data = GetExtendedData();
                        if (data != null)
                        {
                            if (data.data.TryGetValue(nameof(BreastCollidersEnabled), out var loadedBreastCollidersEnabled))
                                BreastCollidersEnabled = (bool)loadedBreastCollidersEnabled;
                            if (data.data.TryGetValue(nameof(FloorColliderEnabled), out var loadedFloorColliderEnabled))
                                FloorColliderEnabled = (bool)loadedFloorColliderEnabled;
#if KK
                            if (data.data.TryGetValue(nameof(SkirtCollidersEnabled), out var loadedSkirtCollidersEnabled))
                                SkirtCollidersEnabled = (bool)loadedSkirtCollidersEnabled;
#endif
                        }

                        didSetStates = true;
                    }
                }
                else
                {
                    BreastCollidersEnabled = ConfigBreastColliders.Value;
                    FloorColliderEnabled = ConfigFloorCollider.Value;
#if KK
                    SkirtCollidersEnabled = ConfigSkirtColliders.Value;
#endif
                }

                ApplyColliders();
            }
            protected override void OnCoordinateBeingLoaded(ChaFileCoordinate coordinate) => ApplyColliders();

            /// <summary>
            /// Adjust the dynamic bones in a LateUpdate or it doesn't work properly
            /// </summary>
            internal void LateUpdate()
            {
                if (applyColliders)
                {
                    if (applyFloorCollider || applySkirtColliders)
                    {
                        foreach (DynamicBone dynamicBone in GetComponentsInChildren<DynamicBone>())
                        {
                            if (applyFloorCollider)
                            {
                                // Prevent affecting charas parented to this chara
                                if (!StudioAPI.InsideStudio || dynamicBone.GetComponentInParent<ChaControl>() == ChaControl)
                                    UpdateFloorColliderDB(dynamicBone);
                            }
#if KK
                            UpdateArmCollidersSkirtDB(dynamicBone);
                            if (applySkirtColliders)
                            {
                                UpdateLegCollidersSkirtDB(dynamicBone);
                                UpdateSkirtDB(dynamicBone);
                            }
#endif
                        }
                    }

                    if (applyBreastColliders || applyFloorCollider)
                    {
                        foreach (DynamicBone_Ver02 dynamicBone in GetComponentsInChildren<DynamicBone_Ver02>(true))
                        {
                            // Prevent affecting charas parented to this chara
                            if (!StudioAPI.InsideStudio || dynamicBone.GetComponentInParent<ChaControl>() == ChaControl)
                                UpdateFloorColliderBreastDB(dynamicBone);

                            if (applyBreastColliders)
                            {
                                UpdateArmCollidersBreastDB(dynamicBone);
                                UpdateBreastDB(dynamicBone);
                            }
                        }
                    }

                    if (applyBreastColliders)
                    {
                        //Apply other character's arm colliders to this character's breasts
                        if (StudioAPI.InsideStudio)
                            UpdateArmCollidersBreastDBAll();

#if KK
                        if (FindObjectOfType<HSceneProc>() != null)
                            UpdateArmCollidersBreastDBAll();
#endif
                    }

                    applyBreastColliders = applySkirtColliders = applyFloorCollider = applyColliders = false;
                }
            }

            public void ApplyColliders() => applyBreastColliders = applySkirtColliders = applyFloorCollider = applyColliders = true;
            public void ApplyBreastColliders() => applyColliders = applyBreastColliders = true;
            public void ApplySkirtColliders() => applyColliders = applySkirtColliders = true;
            public void ApplyFloorCollider() => applyColliders = applyFloorCollider = true;

            /// <summary>
            /// Applies the floor collider to the dynamic bone
            /// </summary>
            private void UpdateFloorColliderDB(DynamicBone dynamicBone)
            {
                if (FloorCollider == null) return;

                if (FloorColliderEnabled)
                {
                    if (!dynamicBone.m_Colliders.Contains(FloorCollider))
                        dynamicBone.m_Colliders.Add(FloorCollider);
                }
                else
                    dynamicBone.m_Colliders.Remove(FloorCollider);
            }

            /// <summary>
            /// Applies the floor collider to the breast dynamic bone
            /// </summary>
            private void UpdateFloorColliderBreastDB(DynamicBone_Ver02 dynamicBone)
            {
                if (!BreastDBComments.Contains(dynamicBone.Comment)) return;
                if (FloorCollider == null) return;

                if (!FloorColliderEnabled || !BreastCollidersEnabled)
                    dynamicBone.Colliders.Remove(FloorCollider);
                else if (!dynamicBone.Colliders.Contains(FloorCollider))
                    dynamicBone.Colliders.Add(FloorCollider);
            }

            /// <summary>
            /// Applies this character's arm colliders to breast dynamic bones
            /// </summary>
            private void UpdateArmCollidersBreastDB(DynamicBone_Ver02 dynamicBone)
            {
                if (!BreastDBComments.Contains(dynamicBone.Comment)) return;
                if (ArmColliders == null) return;
                UpdateArmCollidersBreastDB(dynamicBone, ArmColliders);

                foreach (var armCollider in ArmColliders)
                {
                    if (!BreastCollidersEnabled)
                        dynamicBone.Colliders.Remove(armCollider);
                    else if (armCollider != null && !dynamicBone.Colliders.Contains(armCollider))
                        dynamicBone.Colliders.Add(armCollider);
                }
            }
            /// <summary>
            /// Applies arm colliders to breast dynamic bones
            /// </summary>
            private void UpdateArmCollidersBreastDB(DynamicBone_Ver02 dynamicBone, List<DynamicBoneCollider> armColliders)
            {
                if (!BreastDBComments.Contains(dynamicBone.Comment)) return;
                if (armColliders == null) return;

                foreach (var armCollider in armColliders)
                {
                    if (!BreastCollidersEnabled)
                        dynamicBone.Colliders.Remove(armCollider);
                    else if (armCollider != null && !dynamicBone.Colliders.Contains(armCollider))
                        dynamicBone.Colliders.Add(armCollider);
                }
            }
            private void UpdateArmCollidersBreastDBAll()
            {
                var dynamicBones = GetComponentsInChildren<DynamicBone_Ver02>(true);

                foreach (var controller in FindObjectsOfType<ColliderController>())
                    foreach (DynamicBone_Ver02 dynamicBone in dynamicBones)
                        UpdateArmCollidersBreastDB(dynamicBone, controller.ArmColliders);
            }

            /// <summary>
            /// Apply adjustments to breast dynamic bones
            /// </summary>
            private void UpdateBreastDB(DynamicBone_Ver02 dynamicBone)
            {
                if (!BreastDBComments.Contains(dynamicBone.Comment)) return;

                //Expand the collision radius for the breast dynamic bones
                foreach (var pat in dynamicBone.Patterns)
                {
#if KK
                    pat.Params[0].CollisionRadius = BreastCollidersEnabled ? 0.08f * BreastCollisionRadiusMultiplier : 0;
                    pat.Params[1].CollisionRadius = BreastCollidersEnabled ? 0.06f * BreastCollisionRadiusMultiplier : 0;
#elif AI
                    pat.Params[2].CollisionRadius = BreastCollidersEnabled ? 0.8f * BreastCollisionRadiusMultiplier : 0;
                    pat.Params[3].CollisionRadius = BreastCollidersEnabled ? 0.6f * BreastCollisionRadiusMultiplier : 0;
#else
                throw new System.NotImplementedException();
#endif
                }

                dynamicBone.GetType().GetMethod("InitNodeParticle", AccessTools.all).Invoke(dynamicBone, null);
                dynamicBone.GetType().GetMethod("SetupParticles", AccessTools.all).Invoke(dynamicBone, null);
                dynamicBone.InitLocalPosition();
                if ((bool)dynamicBone.GetType().GetMethod("IsRefTransform", AccessTools.all).Invoke(dynamicBone, null))
                    dynamicBone.setPtn(0, true);
                dynamicBone.GetType().GetMethod("InitTransforms", AccessTools.all).Invoke(dynamicBone, null);
            }

            /// <summary>
            /// Add a collider to a bone based on the ColliderData
            /// </summary>
            private DynamicBoneCollider AddCollider(ColliderData colliderData)
            {
                string colliderName = $"{PluginNameInternal}_{colliderData.BoneName}";
                if (!colliderData.ColliderNamePostfix.IsNullOrEmpty())
                    colliderName += $"_{colliderData.ColliderNamePostfix}";
                return AddCollider(colliderData.BoneName, colliderName, colliderData.ColliderRadius, colliderData.CollierHeight, colliderData.ColliderCenter, colliderData.ColliderDirection);
            }

            /// <summary>
            /// Add a collider to the specified bone
            /// </summary>
            private DynamicBoneCollider AddCollider(string boneName, string colliderName, float colliderRadius = 0.5f, float collierHeight = 0f, Vector3 colliderCenter = new Vector3(), DynamicBoneCollider.Direction colliderDirection = default)
            {
                //Check if the bone exists
                var bone = ChaControl.transform.FindLoop(boneName);
                if (bone == null)
                    return null;

                //Build the collider
                var colliderObject = new GameObject(colliderName);
                var collider = colliderObject.AddComponent<DynamicBoneCollider>();
                collider.m_Radius = colliderRadius;
                collider.m_Height = collierHeight;
                collider.m_Center = colliderCenter;
                collider.m_Direction = colliderDirection;
                colliderObject.transform.SetParent(bone.transform, false);
                return collider;
            }
        }
    }
}