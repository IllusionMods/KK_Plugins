using HarmonyLib;
using System.Collections.Generic;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Studio;
using IllusionUtility.GetUtility;
using UnityEngine;
using ExtensibleSaveFormat;
using static KK_Plugins.ColliderConstants;
#if AI || HS2
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
            public DynamicBoneCollider FloorCollider;
            private readonly List<DynamicBoneCollider> ArmColliders = new List<DynamicBoneCollider>();
#if KK
            private readonly List<DynamicBoneCollider> LegColliders = new List<DynamicBoneCollider>();
#endif

            private bool applyColliders;
            private bool applyBreastColliders;
            internal bool applySkirtColliders;
            private bool applyFloorCollider;

            private bool didSetStates;

            private float BreastSize => ChaControl.chaFile.custom.body.shapeValueBody[(int)BodyShapeIdx.BustSize];
            public bool BreastCollidersEnabled { get; set; }
            public bool SkirtCollidersEnabled { get; set; }
            public bool FloorColliderEnabled { get; set; }

            internal void Main()
            {
                //Add the floor collider
                FloorCollider = AddCollider(FloorColliderData);

                //Add the arm and hand colliders
                for (var i = 0; i < ArmColliderDataList.Count; i++)
                    ArmColliders.Add(AddCollider(ArmColliderDataList[i]));

#if KK
                //Add the leg colliders for skirts
                for (var i = 0; i < LegColliderDataList.Count; i++)
                    LegColliders.Add(AddCollider(LegColliderDataList[i]));
#endif
            }

            protected override void OnCardBeingSaved(GameMode currentGameMode)
            {
                if (StudioAPI.InsideStudio)
                {
                    var data = new PluginData();
                    data.data.Add(nameof(BreastCollidersEnabled), BreastCollidersEnabled);
                    data.data.Add(nameof(FloorColliderEnabled), FloorColliderEnabled);
#if KK
                    data.data.Add(nameof(SkirtCollidersEnabled), SkirtCollidersEnabled);
#endif
                    SetExtendedData(data);
                }
                else
                    SetExtendedData(null);
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
#if KK
                    if (applyFloorCollider || applySkirtColliders)
                    {
                        var dynamicBones = GetComponentsInChildren<DynamicBone>();
                        for (var i = 0; i < dynamicBones.Length; i++)
                        {
                            DynamicBone dynamicBone = dynamicBones[i];
                            if (applyFloorCollider)
                            {
                                // Prevent affecting charas parented to this chara
                                if (!StudioAPI.InsideStudio || dynamicBone.GetComponentInParent<ChaControl>() == ChaControl)
                                    UpdateFloorColliderDB(dynamicBone);
                            }
                            UpdateArmCollidersSkirtDB(dynamicBone);
                            if (applySkirtColliders)
                            {
                                UpdateLegCollidersSkirtDB(dynamicBone);
                                UpdateSkirtDB(dynamicBone);
                            }
                        }
                    }
#endif

                    if (applyBreastColliders || applyFloorCollider)
                    {
                        var dynamicBones = GetComponentsInChildren<DynamicBone_Ver02>(true);
                        for (var i = 0; i < dynamicBones.Length; i++)
                        {
                            DynamicBone_Ver02 dynamicBone = dynamicBones[i];
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

#if KK
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
#endif

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

                for (var i = 0; i < ArmColliders.Count; i++)
                {
                    var armCollider = ArmColliders[i];
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

                for (var i = 0; i < armColliders.Count; i++)
                {
                    var armCollider = armColliders[i];
                    if (!BreastCollidersEnabled)
                        dynamicBone.Colliders.Remove(armCollider);
                    else if (armCollider != null && !dynamicBone.Colliders.Contains(armCollider))
                        dynamicBone.Colliders.Add(armCollider);
                }
            }
            private void UpdateArmCollidersBreastDBAll()
            {
                var controllers = FindObjectsOfType<ColliderController>();
                var dynamicBones = GetComponentsInChildren<DynamicBone_Ver02>(true);

                for (var i = 0; i < controllers.Length; i++)
                    for (var j = 0; j < dynamicBones.Length; j++)
                        UpdateArmCollidersBreastDB(dynamicBones[j], controllers[i].ArmColliders);
            }

            /// <summary>
            /// Apply adjustments to breast dynamic bones
            /// </summary>
            private void UpdateBreastDB(DynamicBone_Ver02 dynamicBone)
            {
                if (!BreastDBComments.Contains(dynamicBone.Comment)) return;

                //Expand the collision radius for the breast dynamic bones
                for (var index = 0; index < dynamicBone.Patterns.Count; index++)
                {
                    var pat = dynamicBone.Patterns[index];
#if KK
                    pat.Params[0].CollisionRadius = BreastCollidersEnabled ? 0.10f * BreastSize : 0;
                    pat.Params[1].CollisionRadius = BreastCollidersEnabled ? 0.08f * BreastSize : 0;
#elif AI || HS2
                    pat.Params[2].CollisionRadius = BreastCollidersEnabled ? 1.0f * BreastSize : 0;
                    pat.Params[3].CollisionRadius = BreastCollidersEnabled ? 0.8f * BreastSize : 0;
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

#if KK
            /// <summary>
            /// Update the skirt dynamic bones. Locks the skirt's front dynamic bone on the X axis to reduce clipping.
            /// </summary>
            private void UpdateSkirtDB(DynamicBone dynamicBone)
            {
                if (dynamicBone.name != "ct_clothesBot") return;

                var nameSplit = dynamicBone.m_Root.name.Split('_');
                if (nameSplit.Length >= 4)
                    if (int.TryParse(nameSplit[3], out int idx))
                        if (idx == 0)
                            if (SkirtCollidersEnabled)
                                dynamicBone.m_FreezeAxis = DynamicBone.FreezeAxis.X;
                            else
                                dynamicBone.m_FreezeAxis = DynamicBone.FreezeAxis.None;
            }

            /// <summary>
            /// Prevent arm colliders from affecting the skirt dynamic bones
            /// </summary>
            private void UpdateArmCollidersSkirtDB(DynamicBone dynamicBone)
            {
                if (dynamicBone.name != "ct_clothesBot") return;
                if (ArmColliders == null) return;

                for (var i = 0; i < ArmColliders.Count; i++)
                    dynamicBone.m_Colliders.Remove(ArmColliders[i]);
            }

            /// <summary>
            /// Apply leg colliders to skirt dynamic bones
            /// </summary>
            private void UpdateLegCollidersSkirtDB(DynamicBone dynamicBone)
            {
                if (dynamicBone.name != "ct_clothesBot") return;
                if (LegColliders == null) return;

                for (var i = 0; i < LegColliders.Count; i++)
                {
                    var legCollider = LegColliders[i];
                    if (!SkirtCollidersEnabled)
                        dynamicBone.m_Colliders.Remove(legCollider);
                    else if (legCollider != null && !dynamicBone.m_Colliders.Contains(legCollider))
                        dynamicBone.m_Colliders.Add(legCollider);
                }
            }
#endif
        }
    }
}