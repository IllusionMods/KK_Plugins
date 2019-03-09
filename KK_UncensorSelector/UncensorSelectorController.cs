using ExtensibleSaveFormat;
using Harmony;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Studio;
using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;

namespace KK_UncensorSelector
{
    partial class KK_UncensorSelector
    {
        public class UncensorSelectorController : CharaCustomFunctionController
        {
            public string BodyGUID { get; set; }
            public string PenisGUID { get; set; }
            public string BallsGUID { get; set; }
            public bool DisplayPenis { get; set; }
            public bool DisplayBalls { get; set; }

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

            protected override void OnReload(GameMode currentGameMode)
            {
                BodyGUID = null;
                PenisGUID = null;
                BallsGUID = null;
                DisplayPenis = ChaControl.sex == 0;
                DisplayBalls = ChaControl.sex == 0;

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
                            BodyGUID = loadedUncensorGUID.ToString();
                            if (BodyGUID.IsNullOrWhiteSpace())
                                BodyGUID = null;
                        }
                    }
                    else
                    {
                        if (data.data.TryGetValue("BodyGUID", out var loadedUncensorGUID) && loadedUncensorGUID != null)
                        {
                            BodyGUID = loadedUncensorGUID.ToString();
                            if (BodyGUID.IsNullOrWhiteSpace())
                                BodyGUID = null;
                        }
                        if (data.data.TryGetValue("PenisGUID", out var loadedPenisGUID) && loadedPenisGUID != null)
                        {
                            PenisGUID = loadedPenisGUID.ToString();
                            if (PenisGUID.IsNullOrWhiteSpace())
                                PenisGUID = null;
                        }
                        if (data.data.TryGetValue("BallsGUID", out var loadedBallsGUID) && loadedBallsGUID != null)
                        {
                            BallsGUID = loadedBallsGUID.ToString();
                            if (BallsGUID.IsNullOrWhiteSpace())
                                BallsGUID = null;
                        }
                        if (data.data.TryGetValue("DisplayPenis", out var loadedDisplayPenis))
                        {
                            DisplayPenis = (bool)loadedDisplayPenis;
                        }
                        if (data.data.TryGetValue("DisplayBalls", out var loadedDisplayBalls))
                        {
                            DisplayBalls = (bool)loadedDisplayBalls;
                        }
                    }
                }

                if (MakerAPI.InsideAndLoaded)
                {
                    SetDropdownEvents(false);
                    if (MakerAPI.GetCharacterLoadFlags().Body)
                    {
                        if (MakerAPI.GetMakerBase().chaCtrl == ChaControl)
                        {
                            //Update the UI to match the loaded character
                            if (BodyList.IndexOf(BodyGUID) == -1)
                            {
                                //The loaded uncensor isn't on the list, possibly due to being forbidden
                                BodyDropdown.Value = 0;
                                BodyGUID = null;
                            }
                            else
                            {
                                BodyDropdown.Value = BodyList.IndexOf(BodyGUID);
                            }

                            if (PenisList.IndexOf(PenisGUID) == -1)
                            {
                                PenisDropdown.Value = DisplayPenis ? 0 : 1;
                                PenisGUID = null;
                            }
                            else
                            {
                                PenisDropdown.Value = PenisList.IndexOf(PenisGUID);
                            }

                            if (BallsList.IndexOf(BallsGUID) == -1)
                            {
                                BallsDropdown.Value = DisplayBalls ? 0 : 1;
                                BallsGUID = null;
                            }
                            else
                            {
                                BallsDropdown.Value = BallsList.IndexOf(BallsGUID);
                            }
                        }
                    }
                    else
                    {
                        //Set the uncensor stuff to whatever is set in the maker
                        BodyGUID = BodyDropdown.Value == 0 ? null : BodyList[BodyDropdown.Value];
                        PenisGUID = PenisDropdown.Value == 0 || PenisDropdown.Value == 1 ? null : PenisList[PenisDropdown.Value];
                        BallsGUID = BallsDropdown.Value == 0 || BallsDropdown.Value == 1 ? null : BallsList[BallsDropdown.Value];
                        DisplayPenis = PenisDropdown.Value == 1 ? false : true;
                        DisplayBalls = BallsDropdown.Value == 1 ? false : true;
                    }
                    SetDropdownEvents(true);
                }
                //Update the uncensor on every load or reload
                UpdateUncensor();
            }
            /// <summary>
            /// Reload this character's uncensor
            /// </summary>
            public void UpdateUncensor() => UncensorUpdate.ReloadCharacterUncensor(ChaControl, BodyData, PenisData, DisplayPenis, BallsData, DisplayBalls);
            public void UpdateSkinColor() => SkinMatch.SetSkinColor(ChaControl, BodyData, PenisData, BallsData);
            public void UpdateSkinLine() => SkinMatch.SetLineVisibility(ChaControl, BodyData, PenisData, BallsData);
            public void UpdateSkinGloss() => SkinMatch.SetSkinGloss(ChaControl, BodyData, PenisData, BallsData);
            /// <summary>
            /// BodyData for this character
            /// </summary>
            public BodyData BodyData
            {
                get
                {
                    BodyData bodyData = null;

                    if (BodyGUID != null && BodyDictionary.TryGetValue(BodyGUID, out var body))
                        bodyData = body;

                    if (bodyData == null)
                        bodyData = DefaultData.GetDefaultOrRandomBody(ChaControl);

                    return bodyData;
                }
            }
            /// <summary>
            /// PenisData for this character
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
            /// BallsData for this character
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

                    //None/default body
                    if (uncensorKey == UncensorKeyNone)
                        return null;

                    //Random
                    if (uncensorKey == UncensorKeyRandom)
                        return GetRandomBody(chaControl);

                    //Return the default body if specified
                    if (BodyDictionary.TryGetValue(uncensorKey, out BodyData defaultBody))
                        return defaultBody;

                    //Something was selected but can no longer be found
                    if (chaControl.sex == 0 && MaleBodyDefaultValue == UncensorKeyNone)
                        return null;
                    if (chaControl.sex == 1 && FemaleBodyDefaultValue == UncensorKeyNone)
                        return null;

                    return GetRandomBody(chaControl);
                }

                internal static BodyData GetRandomBody(ChaControl chaControl)
                {
                    //Calculate a value that is unique for a character and unlikely to change
                    //Use System.Random to spread the results out to full int span while keeping them deterministic (so most girls don't use the same uncensor)
                    var charaHash = new System.Random(chaControl.fileParam.birthDay + chaControl.fileParam.personality + chaControl.fileParam.bloodType).Next();

                    // Find a close match that is unlikely to change even if number of uncensors change
                    var query = from unc in BodyDictionary
                                where unc.Value.Sex == chaControl.sex && unc.Value.AllowRandom
                                let uncHash = new System.Random(unc.Key.GetHashCode()).Next()
                                orderby Mathf.Abs(uncHash - charaHash)
                                select unc.Value;
                    return query.FirstOrDefault();
                }

                internal static PenisData GetDefaultOrRandomPenis(ChaControl chaControl)
                {
                    string uncensorKey = DisplayNameToPenisGuid(chaControl.sex == 0 ? DefaultMalePenis.Value : DefaultFemalePenis.Value);

                    //None/default penis
                    if (uncensorKey == UncensorKeyNone)
                        return null;

                    //Random
                    if (uncensorKey == UncensorKeyRandom)
                        return GetRandomPenis(chaControl);

                    //Return the default penis if specified
                    if (PenisDictionary.TryGetValue(uncensorKey, out PenisData defaultPenis))
                        return defaultPenis;

                    //Something was selected but can no longer be found
                    if (chaControl.sex == 0 && MalePenisDefaultValue == UncensorKeyNone)
                        return null;
                    if (chaControl.sex == 1 && FemalePenisDefaultValue == UncensorKeyNone)
                        return null;

                    return GetRandomPenis(chaControl);
                }

                internal static PenisData GetRandomPenis(ChaControl chaControl)
                {
                    //Calculate a value that is unique for a character and unlikely to change
                    //Use System.Random to spread the results out to full int span while keeping them deterministic (so most girls don't use the same uncensor)
                    var charaHash = new System.Random(chaControl.fileParam.birthDay + chaControl.fileParam.personality + chaControl.fileParam.bloodType).Next();

                    // Find a close match that is unlikely to change even if number of uncensors change
                    var query = from unc in PenisDictionary
                                where unc.Value.AllowRandom
                                let uncHash = new System.Random(unc.Key.GetHashCode()).Next()
                                orderby Mathf.Abs(uncHash - charaHash)
                                select unc.Value;
                    return query.FirstOrDefault();
                }

                internal static BallsData GetDefaultOrRandomBalls(ChaControl chaControl)
                {
                    string uncensorKey = DisplayNameToPenisGuid(chaControl.sex == 0 ? DefaultMaleBalls.Value : DefaultFemaleBalls.Value);

                    //None/default penis
                    if (uncensorKey == UncensorKeyNone)
                        return null;

                    //Random
                    if (uncensorKey == UncensorKeyRandom)
                        return GetRandomBalls(chaControl);

                    //Return the default penis if specified
                    if (BallsDictionary.TryGetValue(uncensorKey, out BallsData defaultBalls))
                        return defaultBalls;

                    //Something was selected but can no longer be found
                    if (chaControl.sex == 0 && MalePenisDefaultValue == UncensorKeyNone)
                        return null;
                    if (chaControl.sex == 1 && FemalePenisDefaultValue == UncensorKeyNone)
                        return null;

                    return GetRandomBalls(chaControl);
                }

                internal static BallsData GetRandomBalls(ChaControl chaControl)
                {
                    //Calculate a value that is unique for a character and unlikely to change
                    //Use System.Random to spread the results out to full int span while keeping them deterministic (so most girls don't use the same uncensor)
                    var charaHash = new System.Random(chaControl.fileParam.birthDay + chaControl.fileParam.personality + chaControl.fileParam.bloodType).Next();

                    // Find a close match that is unlikely to change even if number of uncensors change
                    var query = from unc in BallsDictionary
                                where unc.Value.AllowRandom
                                let uncHash = new System.Random(unc.Key.GetHashCode()).Next()
                                orderby Mathf.Abs(uncHash - charaHash)
                                select unc.Value;
                    return query.FirstOrDefault();
                }
            }
            internal static class UncensorUpdate
            {
                internal static void ReloadCharacterUncensor(ChaControl chaControl, BodyData bodyData, PenisData penisData, bool penisVisible, BallsData ballsData, bool ballsVisible)
                {
                    ReloadCharacterBody(chaControl, bodyData);
                    ReloadCharacterPenis(chaControl, penisData, penisVisible);
                    ReloadCharacterBalls(chaControl, ballsData, ballsVisible);

                    UpdateSkin(chaControl, bodyData);
                    SetChestNormals(chaControl, bodyData);

                    chaControl.customMatBody.SetTexture(ChaShader._AlphaMask, Traverse.Create(chaControl).Property("texBodyAlphaMask").GetValue() as Texture);
                    Traverse.Create(chaControl).Property("updateAlphaMask").SetValue(true);
                }
                /// <summary>
                /// Update the mesh of the penis and set the visibility
                /// </summary>
                internal static void ReloadCharacterPenis(ChaControl chaControl, PenisData penisData, bool showPenis)
                {
                    bool temp = chaControl.fileStatus.visibleSonAlways;
                    if (chaControl.hiPoly == false)
                        return;

                    if (penisData != null)
                    {

                        GameObject dick = CommonLib.LoadAsset<GameObject>(penisData.File, penisData.Asset, true);

                        foreach (var mesh in dick.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                            if (PenisParts.Contains(mesh.name))
                                UpdateMeshRenderer(mesh, chaControl.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(x => x.name == mesh.name), true);

                        Destroy(dick);
                    }

                    chaControl.fileStatus.visibleSonAlways = StudioAPI.InsideStudio ? temp : showPenis;
                }
                /// <summary>
                /// Update the mesh of the balls and set the visibility
                /// </summary>
                internal static void ReloadCharacterBalls(ChaControl chaControl, BallsData ballsData, bool showBalls)
                {
                    if (chaControl.hiPoly == false)
                        return;

                    if (ballsData != null)
                    {
                        GameObject balls = CommonLib.LoadAsset<GameObject>(ballsData.File, ballsData.Asset, true);
                        foreach (var mesh in balls.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                            if (BallsParts.Contains(mesh.name))
                                UpdateMeshRenderer(mesh, chaControl.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(x => x.name == mesh.name), true);

                        Destroy(balls);
                    }

                    SkinnedMeshRenderer ballsSMR = chaControl?.gameObject?.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(x => x?.name == "o_dan_f");
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
                    if (chaControl.hiPoly == false)
                        Asset += "_low";

                    GameObject uncensorCopy = CommonLib.LoadAsset<GameObject>(OOBase, Asset, true);
                    SkinnedMeshRenderer o_body_a = chaControl.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(x => x.name == "o_body_a");

                    //Copy any additional parts to the character
                    if (bodyData != null && bodyData.AdditionalParts.Count > 0)
                    {
                        foreach (var mesh in uncensorCopy.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                        {
                            if (bodyData.AdditionalParts.Contains(mesh.name))
                            {
                                SkinnedMeshRenderer part = chaControl.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(x => x.name == mesh.name);
                                Transform parent = o_body_a.gameObject.GetComponentsInChildren<Transform>(true).FirstOrDefault(c => c.name == mesh.transform.parent.name);
                                if (part == null && parent != null)
                                {
                                    var copy = Instantiate(mesh);
                                    copy.name = mesh.name;
                                    copy.transform.parent = parent;
                                    copy.bones = o_body_a.bones.Where(b => b != null && copy.bones.Any(t => t.name.Equals(b.name))).ToArray();
                                }
                            }
                        }
                    }

                    foreach (var mesh in chaControl.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    {
                        if (mesh.name == "o_body_a")
                            UpdateMeshRenderer(uncensorCopy.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(x => x.name == mesh.name), mesh);
                        else if (BodyParts.Contains(mesh.name))
                            UpdateMeshRenderer(uncensorCopy.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(x => x.name == mesh.name), mesh, true);
                        else if (bodyData != null)
                            foreach (var part in bodyData.ColorMatchList)
                                if (mesh.name == part.Object)
                                    UpdateMeshRenderer(uncensorCopy.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>().FirstOrDefault(x => x.name == part.Object), mesh, true);

                        //Destroy all additional parts attached to the current body that shouldn't be there
                        if (AllAdditionalParts.Contains(mesh.name))
                            if (bodyData == null || !bodyData.AdditionalParts.Contains(mesh.name))
                                Destroy(mesh);
                            else
                                UpdateMeshRenderer(uncensorCopy.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(x => x.name == mesh.name), mesh, true);
                    }

                    Destroy(uncensorCopy);
                }
                /// <summary>
                /// Rebuild the character's skin textures
                /// </summary>
                internal static void UpdateSkin(ChaControl chaControl, BodyData bodyData)
                {
                    Traverse.Create(chaControl).Method("InitBaseCustomTextureBody", new object[] { bodyData?.Sex ?? chaControl.sex }).GetValue();
                    chaControl.AddUpdateCMBodyTexFlags(true, true, true, true, true);
                    chaControl.AddUpdateCMBodyColorFlags(true, true, true, true, true, true);
                    chaControl.AddUpdateCMBodyLayoutFlags(true, true);
                    chaControl.SetBodyBaseMaterial();
                    chaControl.CreateBodyTexture();
                    chaControl.ChangeCustomBodyWithoutCustomTexture();
                }
                /// <summary>
                /// Copy the mesh from one SkinnedMeshRenderer to another. If there is a significant mismatch in the number of bones
                /// this will fail horribly and create abominations. Verify the uncensor body has the proper number of bones in such a case.
                /// </summary>
                internal static void UpdateMeshRenderer(SkinnedMeshRenderer src, SkinnedMeshRenderer dst, bool copyMaterials = false)
                {
                    if (src == null || dst == null)
                        return;

                    //Copy the mesh
                    dst.sharedMesh = src.sharedMesh;

                    Transform[] originalBones = dst.bones;

                    //Sort the bones
                    List<Transform> newBones = new List<Transform>();
                    foreach (Transform t in src.bones)
                    {
                        try
                        {
                            newBones.Add(Array.Find(originalBones, c => c.name == t.name));
                        }
                        catch { }
                    }
                    dst.bones = newBones.ToArray();

                    if (copyMaterials)
                        dst.materials = src.materials;
                }
                /// <summary>
                /// Set the normals for the character's chest. This fixes the shadowing for small-chested characters.
                /// By default it is not applied to males so we do it manually for all characters in case the male is using a female body.
                /// </summary>
                internal static void SetChestNormals(ChaControl chaControl, BodyData bodyData)
                {
                    if (chaControl.dictBustNormal.TryGetValue(ChaControl.BustNormalKind.NmlBody, out BustNormal bustNormal))
                        bustNormal.Release();

                    bustNormal = new BustNormal();
                    bustNormal.Init(chaControl.objBody, bodyData?.OOBase ?? Defaults.OOBase, bodyData?.Normals ?? Defaults.Normals, string.Empty);
                    chaControl.dictBustNormal[ChaControl.BustNormalKind.NmlBody] = bustNormal;
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
                    var mt = mat.GetTexture(ChaShader._MainTex);
                    mat.SetTexture(ChaShader._MainTex, newTex);
                    //Destroy the old texture to prevent memory leak
                    Destroy(mt);
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
                    FindAssist findAssist = new FindAssist();
                    findAssist.Initialize(chaControl.objBody.transform);
                    GameObject gameObject = findAssist.GetObjectFromName(colorMatchPart.Object);
                    if (gameObject != null)
                        gameObject.GetComponent<Renderer>().material.SetFloat(ChaShader._linetexon, chaControl.chaFile.custom.body.drawAddLine ? 1f : 0f);
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

                    foreach (KK_UncensorSelector.ColorMatchPart colorMatchPart in penisData.ColorMatchList)
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
                    FindAssist findAssist = new FindAssist();
                    findAssist.Initialize(chaControl.objBody.transform);
                    GameObject gameObject = findAssist.GetObjectFromName(colorMatchPart.Object);
                    if (gameObject != null)
                        gameObject.GetComponent<Renderer>().material.SetFloat(ChaShader._SpecularPower, Mathf.Lerp(chaControl.chaFile.custom.body.skinGlossPower, 1f, chaControl.chaFile.status.skinTuyaRate));
                }
            }
        }
    }
}