using System;
using System.Collections.Generic;
using System.Linq;
using Manager;
using UnityEngine;

namespace KK_Plugins
{
    /// <summary>
    /// Object attached to clothes used as accessories to let them act like normal accessories
    /// </summary>
    public class ClothesToAccessoriesAdapter : MonoBehaviour
    {
        public ChaClothesComponent ClothesComponent { get; private set; }
        public ChaAccessoryComponent AccessoryComponent { get; private set; }
        public ChaControl Owner { get; private set; }
        public ChaReference Reference { get; private set; }
        public ListInfoBase InfoBase { get; private set; }

        public static readonly Dictionary<ChaControl, List<ClothesToAccessoriesAdapter>[]> AllInstances = new Dictionary<ChaControl, List<ClothesToAccessoriesAdapter>[]>();
        public GameObject[] AllObjects { get; private set; }

        private ChaListDefine.CategoryNo _kind;
        private int _clothingKind;
        readonly CustomTextureCreate[] _ctcArr = new CustomTextureCreate[3];
        private Renderer _colorRend;
        private Texture _appliedBodyMask;
        private Texture _appliedBraMask;

        public void Initialize(ChaControl owner, ChaClothesComponent clothesComponent, ChaAccessoryComponent accessoryComponent, ListInfoBase listInfoBase, ChaListDefine.CategoryNo kind)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (clothesComponent == null) throw new ArgumentNullException(nameof(clothesComponent));
            if (accessoryComponent == null) throw new ArgumentNullException(nameof(accessoryComponent));
            if (listInfoBase == null) throw new ArgumentNullException(nameof(listInfoBase));
            ClothesComponent = clothesComponent;
            AccessoryComponent = accessoryComponent;
            Owner = owner;
            InfoBase = listInfoBase;
            _kind = kind;

            // Treat top parts as normal tops
            if (kind >= ChaListDefine.CategoryNo.cpo_sailor_a)
                _clothingKind = 0;
            else
                _clothingKind = kind - ChaListDefine.CategoryNo.co_top;

            var firstInstanceForCharacter = false;
            AllInstances.TryGetValue(owner, out var instances);
            if (instances == null)
            {
                // + 1 for inner shoes. Only outer / index 7 are used, but some hooks can look for index 8 
                instances = Enumerable.Range(0, 8 /*+ 1*/).Select(i => new List<ClothesToAccessoriesAdapter>()).ToArray();
                AllInstances[Owner] = instances;
                firstInstanceForCharacter = true;
            }
            instances[_clothingKind].Add(this);

            Reference = gameObject.AddComponent<ChaReference>();
            Reference.CreateReferenceInfo((ulong)(_clothingKind + 5), gameObject);

            if (_kind == ChaListDefine.CategoryNo.co_gloves || _kind == ChaListDefine.CategoryNo.co_shoes || _kind == ChaListDefine.CategoryNo.co_socks)
                AllObjects = AccessoryComponent.rendNormal.Select(x => x.transform.parent.gameObject).Distinct().ToArray();

            _colorRend = AccessoryComponent.rendNormal.FirstOrDefault(x => x != null) ??
                         AccessoryComponent.rendAlpha.FirstOrDefault(x => x != null) ?? 
                         AccessoryComponent.rendHair.FirstOrDefault(x => x != null);

            if (!InitializeCreateTextures())
                ClothesToAccessoriesPlugin.Logger.LogWarning($"InitializeCreateTextures failed for kind={kind} id={listInfoBase.Id}");

            // If there's already a bra mask active it needs to be applied again to a newly loaded accessory bra
            if (_kind == ChaListDefine.CategoryNo.co_bra)
            {
                // If this bra is the first clothing accessory added to the character, LastTopState needs to be calculated by UpdateVisibleAccessoryClothes before it can be used below
                if (firstInstanceForCharacter)
                    ClothesToAccessoriesPlugin.UpdateVisibleAccessoryClothes(Owner);

                if (ClothesToAccessoriesPlugin.LastTopState.TryGetValue(Owner, out var topState))
                    ChangeAlphaMask(ClothesToAccessoriesPlugin.alphaState[topState, 0], ClothesToAccessoriesPlugin.alphaState[topState, 1]);
            }

            // bug: if multipe clothes with skirt dynamic bones are spawned then their dynamic bone components all work at the same time on the same body bones, which can cause some weird physics effects
        }

        /// <summary>
        /// Ends up being called when changing an accessory from the maker UI, or when the whole character is destroyed
        /// DO NOT destroy _appliedBodyMask and _appliedBraMask or they will be permanently gone
        /// </summary>
        private void OnDestroy()
        {
            var allLists = AllInstances[Owner];
            var list = allLists[_clothingKind];
            list.Remove(this);
            if (allLists.Sum(x => x.Count) == 0) AllInstances.Remove(Owner);

            if (Owner)
            {
                // If character is using masks from this instance, force top clothes refresh to repopulate the masks (if they end up null it will be handled later)
                if (_appliedBodyMask == Owner.texBodyAlphaMask || _appliedBraMask == Owner.texBraAlphaMask)
                {
                    var fileClothes = Owner.nowCoordinate.clothes;
                    // No need to catch exceptions since this is the last line and we're inside OnDestroy
#if KK
                    Owner.ChangeClothesTopAsync(fileClothes.parts[0].id, fileClothes.subPartsId[0], fileClothes.subPartsId[1], fileClothes.subPartsId[2], true, false);
#else
                    Owner.ChangeClothesTopNoAsync(fileClothes.parts[0].id, fileClothes.subPartsId[0], fileClothes.subPartsId[1], fileClothes.subPartsId[2], true, true);
#endif
                }
            }
        }

        /// <summary>
        /// Clothes use shaders that do not support applying colors directly, instead they rely on the main texture having all colors and effects already applied to it
        /// CustomTextureCreate is used to apply colors and masks to the original main texture of the clothes
        /// </summary>
        private bool InitializeCreateTextures()
        {
            var lib = InfoBase;
            var mainManifest = lib.GetInfo(ChaListDefine.KeyType.MainManifest);
            var mainTexAb = lib.GetInfo(ChaListDefine.KeyType.MainTexAB);
            if (ZeroOrBlank(mainTexAb)) mainTexAb = lib.GetInfo(ChaListDefine.KeyType.MainAB);
            var mainTex = lib.GetInfo(ChaListDefine.KeyType.MainTex);
            if (ZeroOrBlank(mainTexAb) || ZeroOrBlank(mainTex)) return false;
            var t2dMain = CommonLib.LoadAsset<Texture2D>(mainTexAb, mainTex, false, mainManifest);
            if (null == t2dMain) return false;

            var colorMaskAb = lib.GetInfo(ChaListDefine.KeyType.ColorMaskAB);
            if (ZeroOrBlank(colorMaskAb)) colorMaskAb = lib.GetInfo(ChaListDefine.KeyType.MainAB);
            var colorMaskTex = lib.GetInfo(ChaListDefine.KeyType.ColorMaskTex);
            if (ZeroOrBlank(colorMaskAb) || ZeroOrBlank(colorMaskTex))
            {
                Resources.UnloadAsset(t2dMain);
                return false;
            }
            var t2dColorMask = CommonLib.LoadAsset<Texture2D>(colorMaskAb, colorMaskTex, false, mainManifest);
            if (null == t2dColorMask)
            {
                Resources.UnloadAsset(t2dMain);
                return false;
            }

            var mainTex02Ab = lib.GetInfo(ChaListDefine.KeyType.MainTex02AB);
            if (ZeroOrBlank(mainTex02Ab)) mainTex02Ab = lib.GetInfo(ChaListDefine.KeyType.MainAB);
            var mainTex02 = lib.GetInfo(ChaListDefine.KeyType.MainTex02);
            Texture2D t2dMainTex02 = null;
            if (!ZeroOrBlank(mainTex02Ab) && !ZeroOrBlank(mainTex02)) t2dMainTex02 = CommonLib.LoadAsset<Texture2D>(mainTex02Ab, mainTex02, false, mainManifest);

            var colorMask02Ab = lib.GetInfo(ChaListDefine.KeyType.ColorMask02AB);
            if (ZeroOrBlank(colorMask02Ab)) colorMask02Ab = lib.GetInfo(ChaListDefine.KeyType.MainAB);
            var colorMask02Tex = lib.GetInfo(ChaListDefine.KeyType.ColorMask02Tex);
            Texture2D t2dColorMask02 = null;
            if (!ZeroOrBlank(colorMask02Ab) && !ZeroOrBlank(colorMask02Tex)) t2dColorMask02 = CommonLib.LoadAsset<Texture2D>(colorMask02Ab, colorMask02Tex, false, mainManifest);

#if KKS
            var mainTex03Ab = lib.GetInfo(ChaListDefine.KeyType.MainTex03AB);
            if (ZeroOrBlank(mainTex03Ab)) mainTex03Ab = lib.GetInfo(ChaListDefine.KeyType.MainAB);
            var mainTex03 = lib.GetInfo(ChaListDefine.KeyType.MainTex03);

            Texture2D t2dMainTex03 = null;
            if (!ZeroOrBlank(mainTex03Ab) && !ZeroOrBlank(mainTex03)) t2dMainTex03 = CommonLib.LoadAsset<Texture2D>(mainTex03Ab, mainTex03, false, mainManifest, true);

            var colorMask03Tex = lib.GetInfo(ChaListDefine.KeyType.ColorMask03Tex);
            var colorMask03Ab = lib.GetInfo(ChaListDefine.KeyType.ColorMask03AB);
            if (ZeroOrBlank(colorMask03Ab)) colorMask03Ab = lib.GetInfo(ChaListDefine.KeyType.MainAB);
            Texture2D t2dColorMask03 = null;
            if (!ZeroOrBlank(colorMask03Ab) && !ZeroOrBlank(colorMask03Tex))
            {
                t2dColorMask03 = CommonLib.LoadAsset<Texture2D>(colorMask03Ab, colorMask03Tex, false, mainManifest, true);
            }
#endif

            for (var i = 0; i < 3; i++)
            {
                CustomTextureCreate ctc = null;
                Texture2D main = null;
                Texture2D color = null;
                if (i == 0)
                {
                    main = t2dMain;
                    color = t2dColorMask;
                }
                else if (1 == i)
                {
                    main = t2dMainTex02;
                    color = t2dColorMask02;
                }
                else
                {
#if KKS
                    main = t2dMainTex03;
                    color = t2dColorMask03;
#endif
                }
                if (null != main)
                {
                    ctc = new CustomTextureCreate(Owner.objRoot.transform);
                    ctc.Initialize("chara/mm_base.unity3d", "cf_m_clothesN_create", "", main.width, main.height, RenderTextureFormat.ARGB32);
                    ctc.SetMainTexture(main);
                    ctc.SetTexture(ChaShader._ColorMask, color);
                }

                _ctcArr[i] = ctc;
            }

            return true;
        }
        private bool ZeroOrBlank(string input) => input.IsNullOrEmpty() || input == "0";
        public bool UpdateClothesColorAndStuff()
        {
            if (_ctcArr[0] == null)
                return false;

            if (ClothesComponent == null)
                return false;

            // todo pattern support
            var partsInfo = new ChaFileClothes.PartsInfo();
            // Accessories have colors applied to materials of all of their renderers, so it's safe to grab it from there (as a bonus this supports ME or other plugin edits)
            partsInfo.colorInfo[0].baseColor = _colorRend.material.GetColor(ChaShader._Color);
            partsInfo.colorInfo[1].baseColor = _colorRend.material.GetColor(ChaShader._Color2);
            partsInfo.colorInfo[2].baseColor = _colorRend.material.GetColor(ChaShader._Color3);
            // color4 is not used in clothes, it's a fake color for some parts of inner top clothes (it's used as color 1 in those)

            var result = true;
            var patternMasks = new[] { ChaShader._PatternMask1, ChaShader._PatternMask2, ChaShader._PatternMask3, ChaShader._PatternMask1 };
            for (var i = 0; i < 3; i++)
            {
                Texture2D tex = null;

#if KKS
                Owner.lstCtrl.GetFilePath(ChaListDefine.CategoryNo.mt_pattern, partsInfo.colorInfo[i].pattern,
                                          ChaListDefine.KeyType.MainTexAB, ChaListDefine.KeyType.MainTex, out var abName, out var assetName);
#elif KK
                var listInfo = Owner.lstCtrl.GetListInfo(ChaListDefine.CategoryNo.mt_pattern, partsInfo.colorInfo[i].pattern);
                var abName = listInfo.GetInfo(ChaListDefine.KeyType.MainTexAB);
                var assetName = listInfo.GetInfo(ChaListDefine.KeyType.MainTex);
#endif

                if (!ZeroOrBlank(abName) && !ZeroOrBlank(assetName))
                {
                    tex = CommonLib.LoadAsset<Texture2D>(abName, assetName, false, "");
#if KK
                    Character.Instance.AddLoadAssetBundle(abName, "");
#else
                    Character.AddLoadAssetBundle(abName, "");
#endif
                }

                foreach (var ctc in _ctcArr)
                {
                    if (ctc != null) ctc.SetTexture(patternMasks[i], tex);
                }
            }

            foreach (var ctc in _ctcArr)
            {
                if (ctc != null)
                {
                    ctc.SetColor(ChaShader._Color, partsInfo.colorInfo[0].baseColor);
                    ctc.SetColor(ChaShader._Color1_2, partsInfo.colorInfo[0].patternColor);
                    ctc.SetFloat(ChaShader._PatternScale1u, partsInfo.colorInfo[0].tiling.x);
                    ctc.SetFloat(ChaShader._PatternScale1v, partsInfo.colorInfo[0].tiling.y);
#if KKS
                    ctc.SetFloat(ChaShader._PatternOffset1u, partsInfo.colorInfo[0].offset.x);
                    ctc.SetFloat(ChaShader._PatternOffset1v, partsInfo.colorInfo[0].offset.y);
                    ctc.SetFloat(ChaShader._PatternRotator1, partsInfo.colorInfo[0].rotate);
#endif
                    ctc.SetColor(ChaShader._Color2, partsInfo.colorInfo[1].baseColor);
                    ctc.SetColor(ChaShader._Color2_2, partsInfo.colorInfo[1].patternColor);
                    ctc.SetFloat(ChaShader._PatternScale2u, partsInfo.colorInfo[1].tiling.x);
                    ctc.SetFloat(ChaShader._PatternScale2v, partsInfo.colorInfo[1].tiling.y);
                    ctc.SetColor(ChaShader._Color3, partsInfo.colorInfo[2].baseColor);
                    ctc.SetColor(ChaShader._Color3_2, partsInfo.colorInfo[2].patternColor);
                    ctc.SetFloat(ChaShader._PatternScale3u, partsInfo.colorInfo[2].tiling.x);
                    ctc.SetFloat(ChaShader._PatternScale3v, partsInfo.colorInfo[2].tiling.y);
#if KKS
                    ctc.SetFloat(ChaShader._PatternOffset2u, partsInfo.colorInfo[1].offset.x);
                    ctc.SetFloat(ChaShader._PatternOffset2v, partsInfo.colorInfo[1].offset.y);
                    ctc.SetFloat(ChaShader._PatternRotator2, partsInfo.colorInfo[1].rotate);
                    ctc.SetFloat(ChaShader._PatternOffset3u, partsInfo.colorInfo[2].offset.x);
                    ctc.SetFloat(ChaShader._PatternOffset3v, partsInfo.colorInfo[2].offset.y);
                    ctc.SetFloat(ChaShader._PatternRotator3, partsInfo.colorInfo[2].rotate);
#endif
                }
            }

            var hasRendNormal = ClothesComponent.rendNormal01 != null && ClothesComponent.rendNormal01.Length != 0;
            var hasRendAlpha = ClothesComponent.rendAlpha01 != null && ClothesComponent.rendAlpha01.Length != 0;
            if (hasRendNormal || hasRendAlpha)
            {
                var tex = _ctcArr[0].RebuildTextureAndSetMaterial();
                if (tex != null)
                {
                    if (hasRendNormal)
                    {
                        for (var k = 0; k < ClothesComponent.rendNormal01.Length; k++)
                        {
                            if (ClothesComponent.rendNormal01[k])
                                ClothesComponent.rendNormal01[k].material.SetTexture(ChaShader._MainTex, tex);
                            else
                                result = false;
                        }
                    }
                    if (hasRendAlpha)
                    {
                        for (var l = 0; l < ClothesComponent.rendAlpha01.Length; l++)
                        {
                            if (ClothesComponent.rendAlpha01[l])
                                ClothesComponent.rendAlpha01[l].material.SetTexture(ChaShader._MainTex, tex);
                            else
                                result = false;
                        }
                    }
                }
                else
                {
                    result = false;
                }
            }
            else
            {
                result = false;
            }

            var hasRendNormal02 = ClothesComponent.rendNormal02 != null && ClothesComponent.rendNormal02.Length != 0;
            if (hasRendNormal02 && _ctcArr[1] != null)
            {
                var tex = _ctcArr[1].RebuildTextureAndSetMaterial();
                if (tex != null)
                {
                    for (var m = 0; m < ClothesComponent.rendNormal02.Length; m++)
                    {
                        if (ClothesComponent.rendNormal02[m])
                            ClothesComponent.rendNormal02[m].material.SetTexture(ChaShader._MainTex, tex);
                        else
                            result = false;
                    }
                }
                else
                {
                    result = false;
                }
            }
#if KKS
            var hasRendNormal03 = ClothesComponent.rendNormal03 != null && ClothesComponent.rendNormal03.Length != 0;
            if (hasRendNormal03 && _ctcArr[2] != null)
            {
                var tex = _ctcArr[2].RebuildTextureAndSetMaterial();
                if (tex != null)
                {
                    for (var n = 0; n < ClothesComponent.rendNormal03.Length; n++)
                    {
                        if (ClothesComponent.rendNormal03[n])
                        {
                            ClothesComponent.rendNormal03[n].material.SetTexture(ChaShader._MainTex, tex);
                        }
                        else
                        {
                            result = false;
                        }
                    }
                }
                else
                {
                    result = false;
                }
            }
#endif
            var hasRendAccessory = ClothesComponent.rendAccessory != null;
            if (hasRendAccessory && _ctcArr[0] != null)
            {
                var tex = _ctcArr[0].RebuildTextureAndSetMaterial();
                if (tex != null)
                {
                    if (ClothesComponent.rendAccessory)
                        ClothesComponent.rendAccessory.material.SetTexture(ChaShader._MainTex, tex);
                    else
                        result = false;
                }
                else
                {
                    result = false;
                }
            }

            return result;
        }

        private Texture _lastAppliedMask = null;

        static Vector2[] braOffset =
#if KKS
            ChaListDefine.braOffset;
#elif KK
            new Vector2[] { new Vector2(0f, 0f), new Vector2(0f, -1f) };
#endif

        static Vector2[] braTiling =
#if KKS
            ChaListDefine.braTiling;
#elif KK
            new Vector2[] { new Vector2(1f, 1f), new Vector2(1f, 2f) };
#endif

        private void ApplyChaControlMasksToThis()
        {
            if (_kind != ChaListDefine.CategoryNo.co_bra) throw new Exception("tried running ApplyChaControlMasksToThis on type " + _kind);

            if (_lastAppliedMask == Owner.texBraAlphaMask) return;

            var offsetKind = InfoBase.GetInfoInt(ChaListDefine.KeyType.Coordinate) == 2 ? 1 : 0;
            if (InfoBase.dictInfo.TryGetValue(105, out var b)) offsetKind = b == "1" ? 1 : 0;

            var obj0 = Reference.GetReferenceInfo(UniversalRefObjKey.ObjBraDef);
            if (obj0 != null)
            {
                var rend0 = obj0.GetComponent<Renderer>();
                if (rend0 != null)
                {
                    rend0.material.SetTexture(ChaShader._AlphaMask, Owner.texBraAlphaMask);
                    rend0.material.SetTextureOffset(ChaShader._AlphaMask, braOffset[offsetKind]);
                    rend0.material.SetTextureScale(ChaShader._AlphaMask, braTiling[offsetKind]);
                    _lastAppliedMask = Owner.texBraAlphaMask;
                }
            }

            var obj1 = Reference.GetReferenceInfo(UniversalRefObjKey.ObjBraNuge);
            if (obj1 != null)
            {
                var rend1 = obj1.GetComponent<Renderer>();
                if (rend1 != null)
                {
                    rend1.material.SetTexture(ChaShader._AlphaMask, Owner.texBraAlphaMask);
                    rend1.material.SetTextureOffset(ChaShader._AlphaMask, braOffset[offsetKind]);
                    rend1.material.SetTextureScale(ChaShader._AlphaMask, braTiling[offsetKind]);
                    _lastAppliedMask = Owner.texBraAlphaMask;
                }
            }
        }

        public void ApplyMasksToChaControl()
        {
            if (_kind != ChaListDefine.CategoryNo.co_top) throw new Exception("tried running ApplyMasksToChaControl on type " + _kind);

            //Console.WriteLine("ApplyMasksToChaControl");

            //Owner.AddClothesStateKind(kindNo, _listInfoBase.GetInfo(ChaListDefine.KeyType.StateType));
            if (Owner.texBodyAlphaMask == null)
            {
                if (_appliedBodyMask == null)
                {
                    Owner.LoadAlphaMaskTexture(InfoBase.GetInfo(ChaListDefine.KeyType.OverBodyMaskAB), InfoBase.GetInfo(ChaListDefine.KeyType.OverBodyMask), 0);
                    _appliedBodyMask = Owner.texBodyAlphaMask;
                }
                else
                {
                    Owner.texBodyAlphaMask = _appliedBodyMask;
                }

                if (Owner.customMatBody)
                {
                    Owner.customMatBody.SetTexture(ChaShader._AlphaMask, Owner.texBodyAlphaMask);
                }
            }

            if (Owner.texBraAlphaMask == null)
            {
                if (_appliedBraMask == null)
                {
                    Owner.LoadAlphaMaskTexture(InfoBase.GetInfo(ChaListDefine.KeyType.OverBraMaskAB), InfoBase.GetInfo(ChaListDefine.KeyType.OverBraMask), 1);
                    _appliedBraMask = Owner.texBraAlphaMask;
                }
                else
                {
                    Owner.texBraAlphaMask = _appliedBraMask;
                }

                if (Owner.rendBra != null)
                {
                    var infoBra = Owner.infoClothes[2];
                    if (infoBra != null)
                    {
                        var coordId = infoBra.GetInfoInt(ChaListDefine.KeyType.Coordinate) == 2 ? 1 : 0;
                        if (infoBra.dictInfo.TryGetValue((int)ChaListDefine.KeyType.MabUV, out var b)) coordId = b == "1" ? 1 : 0;

                        if (Owner.rendBra[0] != null)
                        {
                            Owner.rendBra[0].material.SetTexture(ChaShader._AlphaMask, Owner.texBraAlphaMask);
                            Owner.rendBra[0].material.SetTextureOffset(ChaShader._AlphaMask, braOffset[coordId]);
                            Owner.rendBra[0].material.SetTextureScale(ChaShader._AlphaMask, braTiling[coordId]);
                        }

                        if (Owner.rendBra[1] != null)
                        {
                            Owner.rendBra[1].material.SetTexture(ChaShader._AlphaMask, Owner.texBraAlphaMask);
                            Owner.rendBra[1].material.SetTextureOffset(ChaShader._AlphaMask, braOffset[coordId]);
                            Owner.rendBra[1].material.SetTextureScale(ChaShader._AlphaMask, braTiling[coordId]);
                        }
                    }
                }
            }

            //todo bustnormals?
        }

        public void ChangeAlphaMask(byte state0, byte state1)
        {
            if (_kind != ChaListDefine.CategoryNo.co_bra) throw new Exception("tried running ChangeAlphaMask on type " + _kind);

            var any = false;
            foreach (var renderer in AccessoryComponent.rendNormal.Concat(AccessoryComponent.rendAlpha))
            {
                foreach (var mat in renderer.materials)
                {
                    mat.SetFloat(ChaShader._alpha_a, state0);
                    mat.SetFloat(ChaShader._alpha_b, state1);
                    any = true;
                }
            }

            //Console.WriteLine($"set {state0} {state1} {any}");

            if (any) ApplyChaControlMasksToThis();
        }
    }
}
