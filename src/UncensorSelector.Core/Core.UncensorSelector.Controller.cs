#if AI || HS2
using AIChara;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ExtensibleSaveFormat;
using HarmonyLib;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using KoiSkinOverlayX;
using Manager;
using UnityEngine;
using Random = System.Random;
#if KK || AI || HS2
using KKAPI.Studio;
#endif

namespace KK_Plugins
{
    internal partial class UncensorSelector
    {
        public class UncensorSelectorController : CharaCustomFunctionController
        {
            private bool DoHandleUVCorrupions = true;
            private KoiSkinOverlayController _ksox;
            /// <summary> BodyGUID saved to the character. Use BodyData.BodyGUID to get the current BodyGUID.</summary>
            internal string BodyGUID { get; set; }
            /// <summary> PenisGUID saved to the character. Use PenisData.PenisGUID to get the current PenisGUID.</summary>
            internal string PenisGUID { get; set; }
            /// <summary> BallsGUID saved to the character. Use BallsData.BallsGUID to get the current BallsGUID.</summary>
            internal string BallsGUID { get; set; }
            /// <summary> Visibility of the penis as saved to the character.</summary>
#if AI || HS2
            internal bool DisplayPenis => ChaControl.sex == 0 || IsFuta;
#else
            internal bool DisplayPenis { get; set; }
#endif
            /// <summary> Visibility of the balls as saved to the character.</summary>
            internal bool DisplayBalls { get; set; }

            protected override void Start()
            {
                _ksox = GetComponent<KoiSkinOverlayController>();
                base.Start();
            }

            protected override void OnCardBeingSaved(GameMode currentGameMode)
            {
                var data = new PluginData();
                data.data.Add("BodyGUID", BodyGUID);
                data.data.Add("PenisGUID", PenisGUID);
                data.data.Add("BallsGUID", BallsGUID);
                data.data.Add("DisplayPenis", DisplayPenis);
                data.data.Add("DisplayBalls", DisplayBalls);
                data.version = 2;
                SetExtendedData(data);
            }

            protected override void OnReload(GameMode currentGameMode, bool maintainState)
            {
                BodyGUID = null;
                PenisGUID = null;
                BallsGUID = null;
#if KK || EC
                DisplayPenis = ChaControl.sex == 0;
#endif
                DisplayBalls = ChaControl.sex == 0 || DefaultFemaleDisplayBalls.Value;

                var data = GetExtendedData();
                if (data != null)
                {
                    if (data.version == 1)
                    {
                        if (data.data.TryGetValue("DisplayBalls", out var loadedDisplayBalls))
                        {
                            DisplayBalls = (bool)loadedDisplayBalls;
                        }
                        if (data.data.TryGetValue("UncensorGUID", out var loadedUncensorGUID) && loadedUncensorGUID != null)
                        {
                            string UncensorGUID = loadedUncensorGUID.ToString();
                            if (!UncensorGUID.IsNullOrWhiteSpace() && MigrationDictionary.TryGetValue(UncensorGUID, out MigrationData migrationData))
                            {
                                BodyGUID = migrationData.BodyGUID;
                                PenisGUID = migrationData.PenisGUID;
                                BallsGUID = migrationData.BallsGUID;
#if KK || EC
                                if (PenisGUID != null)
                                    DisplayPenis = true;
#endif
                            }
                        }
                    }
                    else
                    {
                        if (data.data.TryGetValue("BodyGUID", out var loadedUncensorGUID) && loadedUncensorGUID != null)
                            BodyGUID = loadedUncensorGUID.ToString();

                        if (data.data.TryGetValue("PenisGUID", out var loadedPenisGUID) && loadedPenisGUID != null)
                            PenisGUID = loadedPenisGUID.ToString();

                        if (data.data.TryGetValue("BallsGUID", out var loadedBallsGUID) && loadedBallsGUID != null)
                            BallsGUID = loadedBallsGUID.ToString();
#if KK || EC
                        if (data.data.TryGetValue("DisplayPenis", out var loadedDisplayPenis))
                            DisplayPenis = (bool)loadedDisplayPenis;
#endif

                        if (data.data.TryGetValue("DisplayBalls", out var loadedDisplayBalls))
                            DisplayBalls = (bool)loadedDisplayBalls;
                    }
                }

                if (BodyGUID.IsNullOrWhiteSpace())
                    BodyGUID = null;
                if (PenisGUID.IsNullOrWhiteSpace())
                    PenisGUID = null;
                if (BallsGUID.IsNullOrWhiteSpace())
                    BallsGUID = null;

                if (MakerAPI.InsideAndLoaded)
                {
                    if (MakerAPI.GetCharacterLoadFlags().Body)
                    {
                        if (MakerAPI.GetMakerBase().chaCtrl == ChaControl)
                        {
                            //Update the UI to match the loaded character
                            if (BodyGUID == null || BodyList.IndexOf(BodyGUID) == -1)
                            {
                                //The loaded uncensor isn't on the list, possibly due to being forbidden
                                BodyDropdown.SetValue(0, false);
                                BodyGUID = null;
                            }
                            else
                            {
                                BodyDropdown.SetValue(BodyList.IndexOf(BodyGUID), false);
                            }

                            if (PenisGUID == null || PenisList?.IndexOf(PenisGUID) == -1)
                            {
#if KK
                                PenisDropdown?.SetValue(DisplayPenis ? 0 : 1, false);
#else
                                PenisDropdown?.SetValue(0, false);
#endif
                                PenisGUID = null;
                            }
                            else if (PenisList != null)
                            {
                                PenisDropdown?.SetValue(PenisList.IndexOf(PenisGUID), false);
                            }

                            if (BallsGUID == null || BallsList.IndexOf(BallsGUID) == -1)
                            {
                                BallsDropdown?.SetValue(DisplayBalls ? 0 : 1, false);
                                BallsGUID = null;
                            }
                            else
                            {
                                BallsDropdown?.SetValue(BallsList.IndexOf(BallsGUID), false);
                            }
                        }
                    }
                    else
                    {
                        //Set the uncensor stuff to whatever is set in the maker
                        BodyGUID = BodyDropdown.Value == 0 ? null : BodyList[BodyDropdown.Value];
#if KK
                        PenisGUID = PenisDropdown?.Value == 0 || PenisDropdown?.Value == 1 || PenisDropdown == null ? null : PenisList[PenisDropdown.Value];
                        DisplayPenis = PenisDropdown?.Value != 1;
#else
                        PenisGUID = PenisDropdown.Value == 0 ? null : PenisList[PenisDropdown.Value];
#endif
                        BallsGUID = BallsDropdown?.Value == 0 || BallsDropdown?.Value == 1 || BallsDropdown == null ? null : BallsList[BallsDropdown.Value];
                        DisplayBalls = BallsDropdown?.Value != 1;
                    }
#if AI || HS2
                    TogglePenisBallsUI(DisplayPenis);
#endif
                }

#if KK
                //Correct characters if gender bender is not permitted, except in Studio where it may be required for scene compatibility
                if (GenderBender == false && !StudioAPI.InsideStudio)
                {
                    DisplayPenis = ChaControl.sex == 0;
                    DisplayBalls = ChaControl.sex == 0;
                }
#endif

                //Update the uncensor on every load or reload
                UpdateUncensor();
            }
            /// <summary>
            /// Reload this character's uncensor
            /// </summary>
            public void UpdateUncensor() => ChaControl.StartCoroutine(ReloadCharacterUncensor());
            public void UpdateSkinColor() => SetSkinColor();
            public void UpdateSkinLine() => SetLineVisibility();
            public void UpdateSkinGloss() => SetSkinGloss();
#if KK || EC
            /// <summary>
            /// Returns the exType or 0 if the exType field does not exists for cross version compatibility
            /// </summary>
            private static int ExType(ChaControl chaControl) => typeof(ChaControl).GetProperties(AccessTools.all).Any(p => p.Name == "exType") ? ExType_internal(chaControl) : 0;
            /// <summary>
            /// In a separate method to avoid missing method exception
            /// </summary>
            private static int ExType_internal(ChaControl chaControl) => chaControl.exType;
#else
            private static int ExType(ChaControl _) => 0;
#endif

#if AI || HS2
            public bool IsFuta => ChaControl.chaFile.parameter.futanari && ChaControl.sex == 1;
#endif
            /// <summary>
            /// Current BodyData for this character
            /// </summary>
            public BodyData BodyData
            {
                get
                {
                    BodyData bodyData = null;

                    if (BodyGUID != null && BodyDictionary.TryGetValue(BodyGUID, out var body))
                        bodyData = body;
#if !EC
                    if (!StudioAPI.InsideStudio && bodyData != null && GenderBender == false && ChaControl.sex != bodyData.Sex)
                        bodyData = null;
#endif
                    if (bodyData == null)
                        bodyData = DefaultData.GetDefaultOrRandomBody(ChaControl);

                    return bodyData;
                }
            }
            /// <summary>
            /// Current PenisData for this character
            /// </summary>
            public PenisData PenisData
            {
                get
                {
                    PenisData penisData = null;

                    if (PenisGUID != null && PenisDictionary.TryGetValue(PenisGUID, out var penis))
                        penisData = penis;

                    if (penisData == null)
                        penisData = DefaultData.GetDefaultOrRandomPenis(ChaControl);

                    return penisData;
                }
            }
            /// <summary>
            /// Current BallsData for this character
            /// </summary>
            public BallsData BallsData
            {
                get
                {
                    BallsData ballsData = null;

                    if (BallsGUID != null && BallsDictionary.TryGetValue(BallsGUID, out var balls))
                        ballsData = balls;

                    if (ballsData == null)
                        ballsData = DefaultData.GetDefaultOrRandomBalls(ChaControl);

                    return ballsData;
                }
            }

            internal static class DefaultData
            {
                internal static BodyData GetDefaultOrRandomBody(ChaControl chaControl)
                {
                    string uncensorKey = DisplayNameToBodyGuid(chaControl.sex == 0 ? DefaultMaleBody.Value : DefaultFemaleBody.Value);

                    //Return the default body if specified
                    if (BodyDictionary.TryGetValue(uncensorKey, out BodyData bodyData))
                        return bodyData;

                    //Get random if none specified
                    bodyData = GetRandomBody(chaControl);

                    //None available, return the default
                    if (bodyData == null)
                        BodyDictionary.TryGetValue(chaControl.sex == 0 ? DefaultBodyMaleGUID : DefaultBodyFemaleGUID, out bodyData);

                    return bodyData;
                }
                /// <summary>
                /// Generate a random number based on character parameters so the same character will generate the same number every time and get a body based on this number.
                /// </summary>
                internal static BodyData GetRandomBody(ChaControl chaControl)
                {
                    var uncensors = BodyDictionary.Where(x => x.Value.Sex == chaControl.sex && x.Value.AllowRandom && !IsExcludedFromRandom(chaControl.sex, "Body", x.Key)).Select(x => x.Value).ToArray();
                    if (uncensors.Length == 0)
                        return null;
                    return uncensors[GetRandomNumber(chaControl, uncensors.Length)];
                }

                internal static PenisData GetDefaultOrRandomPenis(ChaControl chaControl)
                {
                    string uncensorKey = DisplayNameToPenisGuid(chaControl.sex == 0 ? DefaultMalePenis.Value : DefaultFemalePenis.Value);

                    //Return the default penis if specified
                    if (PenisDictionary.TryGetValue(uncensorKey, out PenisData penisData))
                        return penisData;

                    //Get random if none specified
                    penisData = GetRandomPenis(chaControl);

                    //None available, return the default
                    if (penisData == null)
                        PenisDictionary.TryGetValue(DefaultPenisGUID, out penisData);

                    return penisData;
                }
                /// <summary>
                /// Generate a random number based on character parameters so the same character will generate the same number every time and get a penis based on this number.
                /// </summary>
                internal static PenisData GetRandomPenis(ChaControl chaControl)
                {
                    var uncensors = PenisDictionary.Where(x => x.Value.AllowRandom && !IsExcludedFromRandom(chaControl.sex, "Penis", x.Key)).Select(x => x.Value).ToArray();
                    if (uncensors.Length == 0)
                        return null;
                    return uncensors[GetRandomNumber(chaControl, uncensors.Length)];
                }

                internal static BallsData GetDefaultOrRandomBalls(ChaControl chaControl)
                {
                    string uncensorKey = DisplayNameToBallsGuid(chaControl.sex == 0 ? DefaultMaleBalls.Value : DefaultFemaleBalls.Value);

                    //Return the default balls if specified
                    if (BallsDictionary.TryGetValue(uncensorKey, out BallsData ballsData))
                        return ballsData;

                    //Get random if none specified
                    ballsData = GetRandomBalls(chaControl);

                    //None available, return the default
                    if (ballsData == null)
                        BallsDictionary.TryGetValue(DefaultBallsGUID, out ballsData);

                    return ballsData;
                }
                /// <summary>
                /// Generate a random number based on character parameters so the same character will generate the same number every time and get balls based on this number.
                /// </summary>
                private static BallsData GetRandomBalls(ChaControl chaControl)
                {
                    var uncensors = BallsDictionary.Where(x => x.Value.AllowRandom && !IsExcludedFromRandom(chaControl.sex, "Balls", x.Key)).Select(x => x.Value).ToArray();
                    if (uncensors.Length == 0)
                        return null;
                    return uncensors[GetRandomNumber(chaControl, uncensors.Length)];
                }

                private static int GetRandomNumber(ChaControl chaControl, int uncensorCount)
                {
                    int key = chaControl.fileParam.birthDay + chaControl.fileParam.personality;
                    key += (int)(chaControl.fileParam.voicePitch * 100);
                    return new Random(key).Next(uncensorCount);
                }
            }

            private IEnumerator ReloadCharacterUncensor()
            {
                while (ChaControl.objBody == null)
                    yield return null;

                if (ExType(ChaControl) == 0) //exType of 1 indicates Janitor, don't modify his body.
                    ReloadCharacterBody();
                ReloadCharacterPenis();
                ReloadCharacterBalls();

                UpdateSkin();
                if (ExType(ChaControl) == 0)
                {
                    ChaControl.updateBustSize = true;
                    Traverse.Create(ChaControl).Method("UpdateSiru", true).GetValue();
                    SetChestNormals();

#if KK || EC
                    ChaControl.customMatBody.SetTexture(ChaShader._AlphaMask, Traverse.Create(ChaControl).Property("texBodyAlphaMask").GetValue() as Texture);
#endif
                    Traverse.Create(ChaControl).Property("updateAlphaMask").SetValue(true);
                }

                UpdateSkinOverlay();
                UpdateMonochromeBody();
            }
            /// <summary>
            /// Update the mesh of the penis and set the visibility
            /// </summary>
            private void ReloadCharacterPenis()
            {
                bool temp = ChaControl.fileStatus.visibleSonAlways;

                if (PenisData != null)
                {
                    GameObject dick = CommonLib.LoadAsset<GameObject>(PenisData.File, PenisData.Asset, true);

                    var renderers = dick.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                    for (var i = 0; i < renderers.Length; i++)
                    {
                        var renderer = renderers[i];
                        if (PenisParts.Contains(renderer.name))
                        {

                            var renderers2 = ChaControl.objBody.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                            for (var j = 0; j < renderers2.Length; j++)
                            {
                                var renderer2 = renderers2[j];
                                if (renderer2.name == renderer.name)
                                {
                                    UpdateMeshRenderer(renderer, renderer2, true);
                                    break;
                                }
                            }
                        }
                    }

                    Destroy(dick);
                }

#if KK || AI || HS2
                ChaControl.fileStatus.visibleSonAlways = StudioAPI.InsideStudio ? temp : DisplayPenis;
#endif
            }
            /// <summary>
            /// Update the mesh of the balls and set the visibility
            /// </summary>
            private void ReloadCharacterBalls()
            {
                if (BallsData != null)
                {
                    GameObject balls = CommonLib.LoadAsset<GameObject>(BallsData.File, BallsData.Asset, true);
                    var renderers = balls.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                    for (var i = 0; i < renderers.Length; i++)
                    {
                        var renderer = renderers[i];
                        if (BallsParts.Contains(renderer.name))
                        {
                            var renderers2 = ChaControl.objBody.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                            for (var j = 0; j < renderers2.Length; j++)
                            {
                                var renderer2 = renderers2[j];
                                if (renderer2.name == renderer.name)
                                {
                                    UpdateMeshRenderer(renderer, renderer2, true);
                                    break;
                                }
                            }
                        }
                    }

                    Destroy(balls);
                }

                if (ChaControl != null && ChaControl.objBody != null)
                {
                    var ballsSMR = ChaControl.objBody.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(x => BallsParts.Any(y => y == x.name));
                    if (ballsSMR != null)
                        ballsSMR.gameObject.GetComponent<Renderer>().enabled = DisplayBalls;
                }
            }
            /// <summary>
            /// Load the body asset, copy its mesh, and delete it
            /// </summary>
            private void ReloadCharacterBody()
            {
                string OOBase = BodyData?.OOBase ?? Defaults.OOBase;
                string Asset = BodyData?.Asset ?? (ChaControl.sex == 0 ? Defaults.AssetMale : Defaults.AssetFemale);
#if KK
                if (ChaControl.hiPoly == false)
                    Asset += "_low";
#endif

                GameObject uncensorCopy = CommonLib.LoadAsset<GameObject>(OOBase, Asset, true);
                SkinnedMeshRenderer bodyMesh = ChaControl.objBody.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(x => BodyNames.Contains(x.name));

                //Copy any additional parts to the character
                if (BodyData != null && bodyMesh != null && BodyData.AdditionalParts.Count > 0)
                {
                    var renderers = uncensorCopy.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                    for (var i = 0; i < renderers.Length; i++)
                    {
                        var renderer = renderers[i];
                        if (BodyData.AdditionalParts.Contains(renderer.name))
                        {
                            SkinnedMeshRenderer part = null;
                            var renderers2 = ChaControl.objBody.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                            for (var j = 0; j < renderers2.Length; j++)
                            {
                                var renderer2 = renderers2[j];
                                if (renderer2.name == renderer.name)
                                {
                                    part = renderer2;
                                    break;
                                }
                            }
                            if (part != null)
                                continue;

                            Transform parent = null;
                            foreach (var c in bodyMesh.gameObject.GetComponentsInChildren<Transform>(true))
                            {
                                if (c.name == renderer.transform.parent.name)
                                {
                                    parent = c;
                                    break;
                                }
                            }
                            if (parent == null)
                                continue;

                            var copy = Instantiate(renderer, parent, true);
                            copy.name = renderer.name;
                            copy.bones = bodyMesh.bones.Where(b => b != null && copy.bones.Any(t => t.name.Equals(b.name))).ToArray();
                        }
                    }
                }

                foreach (var mesh in ChaControl.objBody.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (BodyNames.Contains(mesh.name))
                        UpdateMeshRenderer(uncensorCopy.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(x => x.name == mesh.name), mesh);
                    else if (BodyParts.Contains(mesh.name))
                        UpdateMeshRenderer(uncensorCopy.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(x => x.name == mesh.name), mesh, true);
                    else if (BodyData != null)
                        foreach (var part in BodyData.ColorMatchList)
                            if (mesh.name == part.Object)
                                UpdateMeshRenderer(uncensorCopy.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>().FirstOrDefault(x => x.name == part.Object), mesh, true);

                    //Destroy all additional parts attached to the current body that shouldn't be there
                    if (AllAdditionalParts.Contains(mesh.name))
                        if (BodyData == null || !BodyData.AdditionalParts.Contains(mesh.name))
                            Destroy(mesh);
                        else
                            UpdateMeshRenderer(uncensorCopy.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(x => x.name == mesh.name), mesh, true);
                }

                Destroy(uncensorCopy);
            }
            /// <summary>
            /// Rebuild the character's skin textures
            /// </summary>
            private void UpdateSkin()
            {
                //Method changed number of parameters, check number of parameters for compatibility
                if (!typeof(ChaControl).GetMethod("InitBaseCustomTextureBody", AccessTools.all).GetParameters().Any())
                    Traverse.Create(ChaControl).Method("InitBaseCustomTextureBody").GetValue();
                else
                    Traverse.Create(ChaControl).Method("InitBaseCustomTextureBody", BodyData?.Sex ?? ChaControl.sex).GetValue();

#if KK || EC
                ChaControl.AddUpdateCMBodyTexFlags(true, true, true, true, true);
                ChaControl.AddUpdateCMBodyColorFlags(true, true, true, true, true, true);
#else
                ChaControl.AddUpdateCMBodyTexFlags(true, true, true, true);
                ChaControl.AddUpdateCMBodyColorFlags(true, true, true, true);
                ChaControl.AddUpdateCMBodyGlossFlags(true, true);
#endif
                ChaControl.AddUpdateCMBodyLayoutFlags(true, true);
                ChaControl.SetBodyBaseMaterial();
                ChaControl.CreateBodyTexture();
                ChaControl.ChangeCustomBodyWithoutCustomTexture();
            }
            /// <summary>
            /// Apply overlay textures of the uncensor if any
            /// </summary>
            private void UpdateSkinOverlay()
            {
                _ksox.AdditionalTextures.RemoveAll(x => x.Tag is UncensorSelectorController);

                //Apply uncensor overlay texture
                try
                {
                    if (BodyData?.UncensorOverlay != null && BodyData?.OOBase != Defaults.OOBase)
                    {
                        Texture2D uncensorTexture = CommonLib.LoadAsset<Texture2D>(BodyData.OOBase, BodyData.UncensorOverlay);
                        _ksox.AdditionalTextures.Add(new AdditionalTexture(uncensorTexture, TexType.BodyOver, this));
                    }
                }
                catch
                {
                    Logger.LogError("Unable to apply uncensor overlay");
                }

                //Apply uncensor underlay texture
                try
                {
                    if (BodyData?.UncensorUnderlay != null && BodyData?.OOBase != Defaults.OOBase)
                    {
                        Texture2D uncensorTexture = CommonLib.LoadAsset<Texture2D>(BodyData.OOBase, BodyData.UncensorUnderlay);
                        _ksox.AdditionalTextures.Add(new AdditionalTexture(uncensorTexture, TexType.BodyUnder, this));
                    }
                }
                catch
                {
                    Logger.LogError("Unable to apply uncensor underlay");
                }

                _ksox.UpdateTexture(TexType.BodyOver);
                _ksox.UpdateTexture(TexType.BodyUnder);
            }
            /// <summary>
            /// Disable dick and balls for the monochrome body since the main body already has the parts
            /// </summary>
            private void UpdateMonochromeBody()
            {
#if AI || HS2
                foreach (var renderer in ChaControl.objSimpleBody.GetComponentsInChildren<SkinnedMeshRenderer>())
                    if (PenisParts.Contains(renderer.name) || BallsParts.Contains(renderer.name))
                        renderer.enabled = false;
#endif
            }
            /// <summary>
            /// Copy the mesh from one SkinnedMeshRenderer to another. If there is a significant mismatch in the number of bones
            /// this will fail horribly and create abominations. Verify the uncensor body has the proper number of bones in such a case.
            /// </summary>
            private void UpdateMeshRenderer(SkinnedMeshRenderer src, SkinnedMeshRenderer dst, bool copyMaterials = false)
            {
                if (src == null || dst == null)
                    return;

                // Check if UVs got corrupted when we loaded the asset, uncommon
                var uvCopy = src.sharedMesh.uv.ToArray();
                if (AreUVsCorrupted(uvCopy) && !DidErrorMessage)
                {
                    Logger.LogError($"UVs got corrupted when creating uncensor mesh {src.sharedMesh.name}, body textures might be corrupted. Consider updating your GPU drivers.");
                    DidErrorMessage = true;
                }

                //Copy the mesh
                dst.sharedMesh = src.sharedMesh;

                //Sort the bones
                List<Transform> newBones = new List<Transform>();
                foreach (Transform t in src.bones)
                    newBones.Add(Array.Find(dst.bones, c => c != null && t != null && c.name == t.name));
                dst.bones = newBones.ToArray();

                if (copyMaterials)
                    dst.materials = src.materials;

                if (!PenisParts.Contains(src.sharedMesh.name) && !BallsParts.Contains(src.sharedMesh.name) && DoHandleUVCorrupions)
                {
                    DoHandleUVCorrupions = false;
                    ChaControl.StartCoroutine(HandleUVCorrupionsCo(dst, uvCopy));
                }

                //Regardless of the receive shadow settings configured for the mesh it's always set to false for dick and balls, change it so shadows work correctly
                if (PenisParts.Contains(dst.sharedMesh.name) || BallsParts.Contains(dst.sharedMesh.name))
                    dst.receiveShadows = true;
            }

            private IEnumerator HandleUVCorrupionsCo(SkinnedMeshRenderer dst, Vector2[] uvCopy)
            {
                // Wait for next frame to let the graphics logic run. Issue seems to happen between frames.
                yield return null;

#if KK
                if (Scene.Instance.NowSceneNames.Contains("ClassRoomSelect"))
                {
                    yield return null;
                    yield return null;
                    yield return null;
                    yield return null;
                }
#endif
                DoHandleUVCorrupions = true;

                // Check if UVs got corrupted after moving the mesh, most common fail point
                if (!dst.sharedMesh.uv.SequenceEqual(uvCopy))
                {
                    Logger.LogWarning($"UVs got corrupted when changing uncensor mesh {dst.sharedMesh.name}, attempting to fix");
                    dst.sharedMesh.uv = uvCopy;
                    yield return null;

                    if (!dst.sharedMesh.uv.SequenceEqual(uvCopy))
                        Logger.LogError("Failed to fix UVs, body textures might be displayed corrupted. Consider updating your GPU drivers.");
                }
            }

            private static bool AreUVsCorrupted(Vector2[] uvCopy)
            {
                var count = 0;
                foreach (var uv in uvCopy)
                {
                    // UVs can fail to load and be all 0s in a solid chunk (not spread around) with some GPU drivers
                    if (uv.Equals(Vector2.zero))
                    {
                        if (count++ >= 3)
                            return true;
                    }
                    else
                    {
                        count = 0;
                    }
                }

                return false;
            }

            /// <summary>
            /// Set the normals for the character's chest. This fixes the shadowing for small-chested characters.
            /// By default it is not applied to males so we do it manually for all characters in case the male is using a female body.
            /// </summary>
            private void SetChestNormals()
            {
#if KK || EC
                if (ChaControl.dictBustNormal.TryGetValue(ChaControl.BustNormalKind.NmlBody, out BustNormal bustNormal))
                    bustNormal.Release();

                bustNormal = new BustNormal();
                bustNormal.Init(ChaControl.objBody, BodyData?.OOBase ?? Defaults.OOBase, BodyData?.Normals ?? Defaults.Normals, string.Empty);
                ChaControl.dictBustNormal[ChaControl.BustNormalKind.NmlBody] = bustNormal;
#else
                ChaControl.bustNormal?.Release();
                ChaControl.bustNormal?.Init(ChaControl.objBody, BodyData?.OOBase ?? Defaults.OOBase, BodyData?.Normals ?? Defaults.Normals, string.Empty);
#endif
            }

            /// <summary>
            /// Do color matching for every object configured in the manifest.xml
            /// </summary>
            private void SetSkinColor()
            {
                if (BodyData != null)
                    foreach (ColorMatchPart colorMatchPart in BodyData.ColorMatchList)
                        SetSkinColor(colorMatchPart, BodyData.OOBase);
                if (PenisData != null)
                    foreach (ColorMatchPart colorMatchPart in PenisData.ColorMatchList)
                        SetSkinColor(colorMatchPart, PenisData.File);
                if (BallsData != null)
                    foreach (ColorMatchPart colorMatchPart in BallsData.ColorMatchList)
                        SetSkinColor(colorMatchPart, BallsData.File);
            }

            private void SetSkinColor(ColorMatchPart colorMatchPart, string file)
            {
#if AI || HS2
                if (ChaControl.objBody == null)
                    return;

                //find the game object
                FindAssist findAssist = new FindAssist();
                findAssist.Initialize(ChaControl.objBody.transform);
                GameObject colorMatchgameObject = findAssist.GetObjectFromName(colorMatchPart.Object);
                if (colorMatchgameObject == null)
                    return;
                Renderer colorMatchRenderer = colorMatchgameObject.GetComponent<Renderer>();
                if (colorMatchRenderer == null)
                    return;

                Material mat = colorMatchRenderer.material;
                if (mat == null)
                    return;

                mat.SetColor(ChaShader.SkinColor, ChaControl.chaFile.custom.body.skinColor);
#else
                //get main tex
                Texture2D mainTexture = CommonLib.LoadAsset<Texture2D>(file, colorMatchPart.MainTex, false, string.Empty);
                if (mainTexture == null)
                    return;

                //get color mask
                Texture2D colorMask = CommonLib.LoadAsset<Texture2D>(file, colorMatchPart.ColorMask, false, string.Empty);
                if (colorMask == null)
                    return;

                //find the game object
                FindAssist findAssist = new FindAssist();
                findAssist.Initialize(ChaControl.objBody.transform);
                GameObject go = findAssist.GetObjectFromName(colorMatchPart.Object);
                if (go == null)
                    return;

                if (!go.GetComponent<Renderer>().material.HasProperty("_MainTex"))
                    return;

                var customTex = new CustomTextureControl(go.transform);
                customTex.Initialize(file, colorMatchPart.Material, string.Empty, file, colorMatchPart.MaterialCreate, string.Empty, 2048, 2048);

                customTex.SetMainTexture(mainTexture);
                customTex.SetColor(ChaShader._Color, ChaControl.chaFile.custom.body.skinMainColor);

                customTex.SetTexture(ChaShader._ColorMask, colorMask);
                customTex.SetColor(ChaShader._Color2, ChaControl.chaFile.custom.body.skinSubColor);

                //set the new texture
                var newTex = customTex.RebuildTextureAndSetMaterial();
                if (newTex == null)
                    return;

                Material mat = go.GetComponent<Renderer>().material;
                var mt = mat.GetTexture("_MainTex");
                mat.SetTexture("_MainTex", newTex);
                //Destroy the old texture to prevent memory leak
                Destroy(mt);
#endif
            }
            /// <summary>
            /// Set the skin line visibility for every color matching object configured in the manifest.xml
            /// </summary>
            private void SetLineVisibility()
            {
                if (BodyData != null)
                    foreach (ColorMatchPart colorMatchPart in BodyData.ColorMatchList)
                        SetLineVisibility(colorMatchPart);
                if (PenisData != null)
                    foreach (ColorMatchPart colorMatchPart in PenisData.ColorMatchList)
                        SetLineVisibility(colorMatchPart);
                if (BallsData != null)
                    foreach (ColorMatchPart colorMatchPart in BallsData.ColorMatchList)
                        SetLineVisibility(colorMatchPart);
            }

            private void SetLineVisibility(ColorMatchPart colorMatchPart)
            {
#if KK || EC
                FindAssist findAssist = new FindAssist();
                findAssist.Initialize(ChaControl.objBody.transform);
                GameObject go = findAssist.GetObjectFromName(colorMatchPart.Object);
                if (go != null)
                    go.GetComponent<Renderer>().material.SetFloat(ChaShader._linetexon, ChaControl.chaFile.custom.body.drawAddLine ? 1f : 0f);
#endif
            }
            /// <summary>
            /// Set the skin gloss for every color matching object configured in the manifest.xml
            /// </summary>
            private void SetSkinGloss()
            {
                if (BodyData != null)
                    foreach (ColorMatchPart colorMatchPart in BodyData.ColorMatchList)
                        SetSkinGloss(colorMatchPart);
                if (PenisData != null)
                    foreach (ColorMatchPart colorMatchPart in PenisData.ColorMatchList)
                        SetSkinGloss(colorMatchPart);
                if (BallsData != null)
                    foreach (ColorMatchPart colorMatchPart in BallsData.ColorMatchList)
                        SetSkinGloss(colorMatchPart);
            }

            private void SetSkinGloss(ColorMatchPart colorMatchPart)
            {
#if KK || EC
                FindAssist findAssist = new FindAssist();
                findAssist.Initialize(ChaControl.objBody.transform);
                GameObject go = findAssist.GetObjectFromName(colorMatchPart.Object);
                if (go != null)
                    go.GetComponent<Renderer>().material.SetFloat(ChaShader._SpecularPower, Mathf.Lerp(ChaControl.chaFile.custom.body.skinGlossPower, 1f, ChaControl.chaFile.status.skinTuyaRate));
#endif
            }
        }
    }
}