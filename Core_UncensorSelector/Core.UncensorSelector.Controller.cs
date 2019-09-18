using ExtensibleSaveFormat;
using HarmonyLib;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
#if KK
using KKAPI.Studio;
#endif
#if AI
using AIChara;
#endif

namespace KK_Plugins
{
    internal partial class UncensorSelector
    {
        public class UncensorSelectorController : CharaCustomFunctionController
        {
            /// <summary> BodyGUID saved to the character. Use BodyData.BodyGUID to get the current BodyGUID.</summary>
            internal string BodyGUID { get; set; }
            /// <summary> PenisGUID saved to the character. Use PenisData.PenisGUID to get the current PenisGUID.</summary>
            internal string PenisGUID { get; set; }
            /// <summary> BallsGUID saved to the character. Use BallsData.BallsGUID to get the current BallsGUID.</summary>
            internal string BallsGUID { get; set; }
            /// <summary> Visibility of the penis as saved to the character.</summary>
            internal bool DisplayPenis { get; set; }
            /// <summary> Visibility of the balls as saved to the character.</summary>
            internal bool DisplayBalls { get; set; }

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
                DisplayPenis = ChaControl.sex == 0;
                DisplayBalls = ChaControl.sex == 0 ? true : DefaultFemaleDisplayBalls.Value;

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
                                if (PenisGUID != null)
                                    DisplayPenis = true;
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

                        if (data.data.TryGetValue("DisplayPenis", out var loadedDisplayPenis))
                            DisplayPenis = (bool)loadedDisplayPenis;

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
#if KK || AI
                                PenisDropdown?.SetValue(DisplayPenis ? 0 : 1, false);
#else
                                PenisDropdown?.SetValue(0, false);
#endif
                                PenisGUID = null;
                            }
                            else
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
#if KK || AI
                        PenisGUID = PenisDropdown?.Value == 0 || PenisDropdown?.Value == 1 ? null : PenisList[PenisDropdown.Value];
                        DisplayPenis = PenisDropdown?.Value == 1 ? false : true;
#else
                        PenisGUID = PenisDropdown.Value == 0 ? null : PenisList[PenisDropdown.Value];
#endif
                        BallsGUID = BallsDropdown?.Value == 0 || BallsDropdown?.Value == 1 ? null : BallsList[BallsDropdown.Value];
                        DisplayBalls = BallsDropdown?.Value == 1 ? false : true;
                    }
                }

#if KK
                //Correct characters if genderbender is not permitted, except in Studio where it may be required for scene compatibility
                if (GenderBender.Value == false && !StudioAPI.InsideStudio)
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
            public void UpdateUncensor() => ChaControl.StartCoroutine(UncensorUpdate.ReloadCharacterUncensor(ChaControl, BodyData, PenisData, DisplayPenis, BallsData, DisplayBalls));
            public void UpdateSkinColor() => SkinMatch.SetSkinColor(ChaControl, BodyData, PenisData, BallsData);
            public void UpdateSkinLine() => SkinMatch.SetLineVisibility(ChaControl, BodyData, PenisData, BallsData);
            public void UpdateSkinGloss() => SkinMatch.SetSkinGloss(ChaControl, BodyData, PenisData, BallsData);
            /// <summary>
            /// Returns the exType or 0 if the exType field does not exists for cross version compatibility
            /// </summary>
            private static int ExType(ChaControl chaControl) => typeof(ChaControl).GetProperties(AccessTools.all).Any(p => p.Name == "exType") ? ExType_internal(chaControl) : 0;
            /// <summary>
            /// In a separate method to avoid missing method exception
            /// </summary>
#if KK || EC
            private static int ExType_internal(ChaControl chaControl) => chaControl.exType;
#else
            private static int ExType_internal(ChaControl _) => 0;
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
#if KK
                    if (!StudioAPI.InsideStudio && bodyData != null && GenderBender.Value == false && ChaControl.sex != bodyData.Sex)
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
                    if (BodyDictionary.TryGetValue(uncensorKey, out BodyData defaultBody))
                        return defaultBody;

                    return GetRandomBody(chaControl);
                }
                /// <summary>
                /// Generate a random number based on character parameters so the same character will generate the same number every time and get a body based on this number.
                /// </summary>
                internal static BodyData GetRandomBody(ChaControl chaControl)
                {
                    var uncensors = BodyDictionary.Where(x => x.Value.Sex == chaControl.sex && x.Value.AllowRandom).Select(x => x.Value).ToArray();
                    if (uncensors.Length == 0)
                        return null;
                    return uncensors[GetRandomNumber(chaControl, uncensors.Length)];
                }

                internal static PenisData GetDefaultOrRandomPenis(ChaControl chaControl)
                {
                    string uncensorKey = DisplayNameToPenisGuid(chaControl.sex == 0 ? DefaultMalePenis.Value : DefaultFemalePenis.Value);

                    //Return the default penis if specified
                    if (PenisDictionary.TryGetValue(uncensorKey, out PenisData defaultPenis))
                        return defaultPenis;

                    return GetRandomPenis(chaControl);
                }
                /// <summary>
                /// Generate a random number based on character parameters so the same character will generate the same number every time and get a penis based on this number.
                /// </summary>
                internal static PenisData GetRandomPenis(ChaControl chaControl)
                {
                    var uncensors = PenisDictionary.Where(x => x.Value.AllowRandom).Select(x => x.Value).ToArray();
                    if (uncensors.Length == 0)
                        return null;
                    return uncensors[GetRandomNumber(chaControl, uncensors.Length)];
                }

                internal static BallsData GetDefaultOrRandomBalls(ChaControl chaControl)
                {
                    string uncensorKey = DisplayNameToBallsGuid(chaControl.sex == 0 ? DefaultMaleBalls.Value : DefaultFemaleBalls.Value);

                    //Return the default balls if specified
                    if (BallsDictionary.TryGetValue(uncensorKey, out BallsData defaultBalls))
                        return defaultBalls;

                    return GetRandomBalls(chaControl);
                }
                /// <summary>
                /// Generate a random number based on character parameters so the same character will generate the same number every time and get balls based on this number.
                /// </summary>
                internal static BallsData GetRandomBalls(ChaControl chaControl)
                {
                    var uncensors = BallsDictionary.Where(x => x.Value.AllowRandom).Select(x => x.Value).ToArray();
                    if (uncensors.Length == 0)
                        return null;
                    return uncensors[GetRandomNumber(chaControl, uncensors.Length)];
                }

                private static int GetRandomNumber(ChaControl chaControl, int uncensorCount)
                {
                    int key = chaControl.fileParam.birthDay + chaControl.fileParam.personality;
#if KK || EC
                    key += chaControl.fileParam.bloodType;
#else
                    key += (int)(chaControl.fileParam.voicePitch * 100);
#endif
                    return new System.Random(key).Next(uncensorCount);
                }
            }
            internal static class UncensorUpdate
            {
                internal static IEnumerator ReloadCharacterUncensor(ChaControl chaControl, BodyData bodyData, PenisData penisData, bool penisVisible, BallsData ballsData, bool ballsVisible)
                {
                    while (chaControl.objBody == null)
                        yield return null;

                    if (ExType(chaControl) == 0) //exType of 1 indicates Janitor, don't modify his body.
                        ReloadCharacterBody(chaControl, bodyData);
                    ReloadCharacterPenis(chaControl, penisData, penisVisible);
                    ReloadCharacterBalls(chaControl, ballsData, ballsVisible);

                    UpdateSkin(chaControl, bodyData);
                    if (ExType(chaControl) == 0)
                    {
                        chaControl.updateBustSize = true;
                        Traverse.Create(chaControl).Method("UpdateSiru", new object[] { true }).GetValue();
                        SetChestNormals(chaControl, bodyData);

#if KK || EC
                        chaControl.customMatBody.SetTexture(ChaShader._AlphaMask, Traverse.Create(chaControl).Property("texBodyAlphaMask").GetValue() as Texture);
#endif
                        Traverse.Create(chaControl).Property("updateAlphaMask").SetValue(true);
                    }
                }
                /// <summary>
                /// Update the mesh of the penis and set the visibility
                /// </summary>
                internal static void ReloadCharacterPenis(ChaControl chaControl, PenisData penisData, bool showPenis)
                {
#if KK
                    bool temp = chaControl.fileStatus.visibleSonAlways;
                    if (chaControl.hiPoly == false)
                        return;
#endif

                    if (penisData != null)
                    {

                        GameObject dick = CommonLib.LoadAsset<GameObject>(penisData.File, penisData.Asset, true);

                        foreach (var mesh in dick.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                            if (PenisParts.Contains(mesh.name))
                                UpdateMeshRenderer(chaControl, mesh, chaControl.objBody.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(x => x.name == mesh.name), true);

                        Destroy(dick);
                    }

#if KK
                    chaControl.fileStatus.visibleSonAlways = StudioAPI.InsideStudio ? temp : showPenis;
#endif
                }
                /// <summary>
                /// Update the mesh of the balls and set the visibility
                /// </summary>
                internal static void ReloadCharacterBalls(ChaControl chaControl, BallsData ballsData, bool showBalls)
                {
#if KK
                    if (chaControl.hiPoly == false)
                        return;
#endif

                    if (ballsData != null)
                    {
                        GameObject balls = CommonLib.LoadAsset<GameObject>(ballsData.File, ballsData.Asset, true);
                        foreach (var mesh in balls.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                            if (BallsParts.Contains(mesh.name))
                                UpdateMeshRenderer(chaControl, mesh, chaControl.objBody.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(x => x.name == mesh.name), true);

                        Destroy(balls);
                    }

                    SkinnedMeshRenderer ballsSMR = chaControl?.objBody?.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(x => x?.name == "o_dan_f");
                    if (ballsSMR != null)
                        ballsSMR.gameObject.GetComponent<Renderer>().enabled = showBalls;
                }
                /// <summary>
                /// Load the body asset, copy its mesh, and delete it
                /// </summary>
                internal static void ReloadCharacterBody(ChaControl chaControl, BodyData bodyData)
                {
                    string OOBase = bodyData?.OOBase ?? Defaults.OOBase;
                    string Asset = bodyData?.Asset ?? (chaControl.sex == 0 ? Defaults.AssetMale : Defaults.AssetFemale);
#if KK
                    if (chaControl.hiPoly == false)
                        Asset += "_low";
#endif

                    GameObject uncensorCopy = CommonLib.LoadAsset<GameObject>(OOBase, Asset, true);
                    SkinnedMeshRenderer bodyMesh = chaControl.objBody.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(x => BodyNames.Contains(x.name));

                    //Copy any additional parts to the character
                    if (bodyData != null && bodyMesh != null && bodyData.AdditionalParts.Count > 0)
                    {
                        foreach (var mesh in uncensorCopy.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                        {
                            if (bodyData.AdditionalParts.Contains(mesh.name))
                            {
                                SkinnedMeshRenderer part = chaControl.objBody.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(x => x.name == mesh.name);
                                Transform parent = bodyMesh.gameObject.GetComponentsInChildren<Transform>(true).FirstOrDefault(c => c.name == mesh.transform.parent.name);
                                if (part == null && parent != null)
                                {
                                    var copy = Instantiate(mesh);
                                    copy.name = mesh.name;
                                    copy.transform.parent = parent;
                                    copy.bones = bodyMesh.bones.Where(b => b != null && copy.bones.Any(t => t.name.Equals(b.name))).ToArray();
                                }
                            }
                        }
                    }

                    foreach (var mesh in chaControl.objBody.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    {
                        if (BodyNames.Contains(mesh.name))
                            UpdateMeshRenderer(chaControl, uncensorCopy.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(x => x.name == mesh.name), mesh);
                        else if (BodyParts.Contains(mesh.name))
                            UpdateMeshRenderer(chaControl, uncensorCopy.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(x => x.name == mesh.name), mesh, true);
                        else if (bodyData != null)
                            foreach (var part in bodyData.ColorMatchList)
                                if (mesh.name == part.Object)
                                    UpdateMeshRenderer(chaControl, uncensorCopy.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>().FirstOrDefault(x => x.name == part.Object), mesh, true);

                        //Destroy all additional parts attached to the current body that shouldn't be there
                        if (AllAdditionalParts.Contains(mesh.name))
                            if (bodyData == null || !bodyData.AdditionalParts.Contains(mesh.name))
                                Destroy(mesh);
                            else
                                UpdateMeshRenderer(chaControl, uncensorCopy.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(x => x.name == mesh.name), mesh, true);
                    }

                    Destroy(uncensorCopy);
                }
                /// <summary>
                /// Rebuild the character's skin textures
                /// </summary>
                internal static void UpdateSkin(ChaControl chaControl, BodyData bodyData)
                {
                    //Method changed number of parameters, check number of parameters for compatibility
                    if (typeof(ChaControl).GetMethod("InitBaseCustomTextureBody", AccessTools.all).GetParameters().Count() == 0)
                        Traverse.Create(chaControl).Method("InitBaseCustomTextureBody").GetValue();
                    else
                        Traverse.Create(chaControl).Method("InitBaseCustomTextureBody", new object[] { bodyData?.Sex ?? chaControl.sex }).GetValue();

#if KK || EC
                    chaControl.AddUpdateCMBodyTexFlags(true, true, true, true, true);
                    chaControl.AddUpdateCMBodyColorFlags(true, true, true, true, true, true);
#else
                    chaControl.AddUpdateCMBodyTexFlags(true, true, true, true);
                    chaControl.AddUpdateCMBodyColorFlags(true, true, true, true);
#endif
                    chaControl.AddUpdateCMBodyLayoutFlags(true, true);
                    chaControl.SetBodyBaseMaterial();
                    chaControl.CreateBodyTexture();
                    chaControl.ChangeCustomBodyWithoutCustomTexture();
                }
                /// <summary>
                /// Copy the mesh from one SkinnedMeshRenderer to another. If there is a significant mismatch in the number of bones
                /// this will fail horribly and create abominations. Verify the uncensor body has the proper number of bones in such a case.
                /// </summary>
                internal static void UpdateMeshRenderer(ChaControl chaControl, SkinnedMeshRenderer src, SkinnedMeshRenderer dst, bool copyMaterials = false)
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
                        newBones.Add(Array.Find(dst.bones, c => c?.name == t?.name));
                    dst.bones = newBones.ToArray();

                    if (copyMaterials)
                        dst.materials = src.materials;

                    if (!PenisParts.Contains(src.sharedMesh.name) && !BallsParts.Contains(src.sharedMesh.name))
                        chaControl.StartCoroutine(HandleUVCorrupionsCo(dst, uvCopy));

                    //Regardless of the receive shadow settings configured for the mesh it's always set to false for dick and balls, change it so shadows work correctly
                    if (PenisParts.Contains(dst.sharedMesh.name) || BallsParts.Contains(dst.sharedMesh.name))
                        dst.receiveShadows = true;
                }

                private static IEnumerator HandleUVCorrupionsCo(SkinnedMeshRenderer dst, Vector2[] uvCopy)
                {
                    // Wait for next frame to let the graphics logic run. Issue seems to happen between frames.
                    yield return null;
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
                internal static void SetChestNormals(ChaControl chaControl, BodyData bodyData)
                {
#if KK || EC
                    if (chaControl.dictBustNormal.TryGetValue(ChaControl.BustNormalKind.NmlBody, out BustNormal bustNormal))
                        bustNormal.Release();

                    bustNormal = new BustNormal();
                    bustNormal.Init(chaControl.objBody, bodyData?.OOBase ?? Defaults.OOBase, bodyData?.Normals ?? Defaults.Normals, string.Empty);
                    chaControl.dictBustNormal[ChaControl.BustNormalKind.NmlBody] = bustNormal;
#endif
                }
            }
            internal static class SkinMatch
            {
                /// <summary>
                /// Do color matching for every object configured in the manifest.xml
                /// </summary>
                internal static void SetSkinColor(ChaControl chaControl, BodyData bodyData, PenisData penisData, BallsData ballsData)
                {
                    SetSkinColor(chaControl, bodyData);
                    SetSkinColor(chaControl, penisData);
                    SetSkinColor(chaControl, ballsData);
                }

                private static void SetSkinColor(ChaControl chaControl, BodyData bodyData)
                {
                    if (bodyData == null)
                        return;

                    foreach (ColorMatchPart colorMatchPart in bodyData.ColorMatchList)
                        SetSkinColor(chaControl, colorMatchPart, bodyData.OOBase);
                }

                private static void SetSkinColor(ChaControl chaControl, PenisData penisData)
                {
                    if (penisData == null)
                        return;

                    foreach (ColorMatchPart colorMatchPart in penisData.ColorMatchList)
                        SetSkinColor(chaControl, colorMatchPart, penisData.File);
                }

                private static void SetSkinColor(ChaControl chaControl, BallsData ballsData)
                {
                    if (ballsData == null)
                        return;

                    foreach (ColorMatchPart colorMatchPart in ballsData.ColorMatchList)
                        SetSkinColor(chaControl, colorMatchPart, ballsData.File);
                }

                private static void SetSkinColor(ChaControl chaControl, ColorMatchPart colorMatchPart, string file)
                {
#if !AI
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
                    findAssist.Initialize(chaControl.objBody.transform);
                    GameObject gameObject = findAssist.GetObjectFromName(colorMatchPart.Object);
                    if (gameObject == null)
                        return;

                    if (!gameObject.GetComponent<Renderer>().material.HasProperty("_MainTex"))
                        return;

                    var customTex = new CustomTextureControl(gameObject.transform);
                    customTex.Initialize(file, colorMatchPart.Material, string.Empty, file, colorMatchPart.MaterialCreate, string.Empty, 2048, 2048);

                    customTex.SetMainTexture(mainTexture);
                    customTex.SetColor(ChaShader._Color, chaControl.chaFile.custom.body.skinMainColor);

                    customTex.SetTexture(ChaShader._ColorMask, colorMask);
                    customTex.SetColor(ChaShader._Color2, chaControl.chaFile.custom.body.skinSubColor);

                    //set the new texture
                    var newTex = customTex.RebuildTextureAndSetMaterial();
                    if (newTex == null)
                        return;

                    Material mat = gameObject.GetComponent<Renderer>().material;
                    var mt = mat.GetTexture("_MainTex");
                    mat.SetTexture("_MainTex", newTex);
                    //Destroy the old texture to prevent memory leak
                    Destroy(mt);
#endif
                }
                /// <summary>
                /// Set the skin line visibility for every color matching object configured in the manifest.xml
                /// </summary>
                internal static void SetLineVisibility(ChaControl chaControl, BodyData bodyData, PenisData penisData, BallsData ballsData)
                {
                    SetLineVisibility(chaControl, bodyData);
                    SetLineVisibility(chaControl, penisData);
                    SetLineVisibility(chaControl, ballsData);
                }

                private static void SetLineVisibility(ChaControl chaControl, BodyData bodyData)
                {
                    if (bodyData == null)
                        return;

                    foreach (ColorMatchPart colorMatchPart in bodyData.ColorMatchList)
                        SetLineVisibility(chaControl, colorMatchPart);
                }

                private static void SetLineVisibility(ChaControl chaControl, PenisData penisData)
                {
                    if (penisData == null)
                        return;

                    foreach (ColorMatchPart colorMatchPart in penisData.ColorMatchList)
                        SetLineVisibility(chaControl, colorMatchPart);
                }

                private static void SetLineVisibility(ChaControl chaControl, BallsData ballsData)
                {
                    if (ballsData == null)
                        return;

                    foreach (ColorMatchPart colorMatchPart in ballsData.ColorMatchList)
                        SetLineVisibility(chaControl, colorMatchPart);
                }

                private static void SetLineVisibility(ChaControl chaControl, ColorMatchPart colorMatchPart)
                {
#if KK || EC
                    FindAssist findAssist = new FindAssist();
                    findAssist.Initialize(chaControl.objBody.transform);
                    GameObject gameObject = findAssist.GetObjectFromName(colorMatchPart.Object);
                    if (gameObject != null)
                        gameObject.GetComponent<Renderer>().material.SetFloat(ChaShader._linetexon, chaControl.chaFile.custom.body.drawAddLine ? 1f : 0f);
#endif
                }
                /// <summary>
                /// Set the skin gloss for every color matching object configured in the manifest.xml
                /// </summary>
                internal static void SetSkinGloss(ChaControl chaControl, BodyData bodyData, PenisData penisData, BallsData ballsData)
                {
                    SetSkinGloss(chaControl, bodyData);
                    SetSkinGloss(chaControl, penisData);
                    SetSkinGloss(chaControl, ballsData);
                }

                private static void SetSkinGloss(ChaControl chaControl, BodyData bodyData)
                {
                    if (bodyData == null)
                        return;

                    foreach (ColorMatchPart colorMatchPart in bodyData.ColorMatchList)
                        SetSkinGloss(chaControl, colorMatchPart);
                }

                private static void SetSkinGloss(ChaControl chaControl, PenisData penisData)
                {
                    if (penisData == null)
                        return;

                    foreach (UncensorSelector.ColorMatchPart colorMatchPart in penisData.ColorMatchList)
                        SetSkinGloss(chaControl, colorMatchPart);
                }

                private static void SetSkinGloss(ChaControl chaControl, BallsData ballsData)
                {
                    if (ballsData == null)
                        return;

                    foreach (ColorMatchPart colorMatchPart in ballsData.ColorMatchList)
                        SetSkinGloss(chaControl, colorMatchPart);
                }

                private static void SetSkinGloss(ChaControl chaControl, ColorMatchPart colorMatchPart)
                {
#if KK || EC
                    FindAssist findAssist = new FindAssist();
                    findAssist.Initialize(chaControl.objBody.transform);
                    GameObject gameObject = findAssist.GetObjectFromName(colorMatchPart.Object);
                    if (gameObject != null)
                        gameObject.GetComponent<Renderer>().material.SetFloat(ChaShader._SpecularPower, Mathf.Lerp(chaControl.chaFile.custom.body.skinGlossPower, 1f, chaControl.chaFile.status.skinTuyaRate));
#endif
                }
            }
        }
    }
}