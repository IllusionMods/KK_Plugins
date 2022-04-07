using System;
using System.Collections.Generic;
using System.Linq;
using Manager;
using UnityEngine;

namespace ClothesToAccessories
{
    public class ClothesToAccessoriesAdapter : MonoBehaviour
    {
        public ChaClothesComponent ClothesComponent { get; private set; }
        public ChaAccessoryComponent AccessoryComponent { get; private set; }
        public ChaControl Owner { get; private set; }
        public ChaReference Reference { get; private set; }

        public static readonly Dictionary<ChaControl, List<ClothesToAccessoriesAdapter>[]> AllInstances = new Dictionary<ChaControl, List<ClothesToAccessoriesAdapter>[]>();
        public GameObject[] AllObjects { get; private set; }

        private ChaListDefine.CategoryNo _kind;
        private int _clothingKind;
        private ListInfoBase _listInfoBase;
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
            _listInfoBase = listInfoBase;
            _kind = kind;

            // Treat top parts as normal tops
            if (kind >= ChaListDefine.CategoryNo.cpo_sailor_a)
                _clothingKind = 0;
            else
                _clothingKind = kind - ChaListDefine.CategoryNo.co_top;

            AllInstances.TryGetValue(owner, out var instances);
            if (instances == null)
            {
                instances = Enumerable.Range(0, 8).Select(i => new List<ClothesToAccessoriesAdapter>()).ToArray();
                AllInstances[Owner] = instances;
            }
            instances[_clothingKind].Add(this);

            Reference = gameObject.AddComponent<ChaReference>();
            Reference.CreateReferenceInfo((ulong)(_clothingKind + 5), gameObject);

            if (_kind == ChaListDefine.CategoryNo.co_gloves || _kind == ChaListDefine.CategoryNo.co_shoes || _kind == ChaListDefine.CategoryNo.co_socks)
                AllObjects = AccessoryComponent.rendNormal.Select(x => x.transform.parent.gameObject).Distinct().ToArray();

            _colorRend = AccessoryComponent.rendNormal.FirstOrDefault(x => x != null) ?? AccessoryComponent.rendAlpha.FirstOrDefault(x => x != null) ?? AccessoryComponent.rendHair.FirstOrDefault(x => x != null);
            InitializeTextures();

            // bug: if multipe clothes with skirt dynamic bones are spawned then their dynamic bone components all work at the same time on the same body bones, which can cause some weird physics effects
        }

        private void OnDestroy()
        {
            var list = AllInstances[Owner];
            list[_clothingKind].Remove(this);
            if (list.Length == 0) AllInstances.Remove(Owner);

            if (Owner)
            {
                // If character is using masks from this instance, force top clothes refresh to repopulate the masks (if they end up null it will be handled later)
                if (_appliedBodyMask == Owner.texBodyAlphaMask || _appliedBraMask == Owner.texBraAlphaMask)
                {
                    var fileClothes = Owner.nowCoordinate.clothes;
                    Owner.ChangeClothesTopNoAsync(fileClothes.parts[0].id, fileClothes.subPartsId[0], fileClothes.subPartsId[1], fileClothes.subPartsId[2], true, true);
                }
            }

            // DO NOT DESTROY they will be permanently gone
            //Destroy(_appliedBodyMask);
            //Destroy(_appliedBraMask);
        }

        private bool InitializeTextures()
        {
            var lib = _listInfoBase;
            var mainManifest = lib.GetInfo(ChaListDefine.KeyType.MainManifest);
            var mainTexAb = lib.GetInfo(ChaListDefine.KeyType.MainTexAB);
            if ("0" == mainTexAb) mainTexAb = lib.GetInfo(ChaListDefine.KeyType.MainAB);
            var mainTex = lib.GetInfo(ChaListDefine.KeyType.MainTex);
            if ("0" == mainTexAb || "0" == mainTex) return false;
            var t2dMain = CommonLib.LoadAsset<Texture2D>(mainTexAb, mainTex, false, mainManifest, true);
            if (null == t2dMain) return false;


            var colorMaskAb = lib.GetInfo(ChaListDefine.KeyType.ColorMaskAB);
            if ("0" == colorMaskAb) colorMaskAb = lib.GetInfo(ChaListDefine.KeyType.MainAB);
            var colorMaskTex = lib.GetInfo(ChaListDefine.KeyType.ColorMaskTex);
            if ("0" == colorMaskAb || "0" == colorMaskTex)
            {
                Resources.UnloadAsset(t2dMain);
                return false;
            }
            var t2dColorMask = CommonLib.LoadAsset<Texture2D>(colorMaskAb, colorMaskTex, false, mainManifest, true);
            if (null == t2dColorMask)
            {
                Resources.UnloadAsset(t2dMain);
                return false;
            }

            var mainTex02Ab = lib.GetInfo(ChaListDefine.KeyType.MainTex02AB);
            if ("0" == mainTex02Ab) mainTex02Ab = lib.GetInfo(ChaListDefine.KeyType.MainAB);
            var mainTex02 = lib.GetInfo(ChaListDefine.KeyType.MainTex02);
            Texture2D t2dMainTex02 = null;
            if ("0" != mainTex02Ab && "0" != mainTex02) t2dMainTex02 = CommonLib.LoadAsset<Texture2D>(mainTex02Ab, mainTex02, false, mainManifest, true);

            var colorMask02Ab = lib.GetInfo(ChaListDefine.KeyType.ColorMask02AB);
            if ("0" == colorMask02Ab) colorMask02Ab = lib.GetInfo(ChaListDefine.KeyType.MainAB);
            var colorMask02Tex = lib.GetInfo(ChaListDefine.KeyType.ColorMask02Tex);
            Texture2D t2dColorMask02 = null;
            if ("0" != colorMask02Ab && "0" != colorMask02Tex) t2dColorMask02 = CommonLib.LoadAsset<Texture2D>(colorMask02Ab, colorMask02Tex, false, mainManifest, true);

            var mainTex03Ab = lib.GetInfo(ChaListDefine.KeyType.MainTex03AB);
            if ("0" == mainTex03Ab) mainTex03Ab = lib.GetInfo(ChaListDefine.KeyType.MainAB);
            var mainTex03 = lib.GetInfo(ChaListDefine.KeyType.MainTex03);

            Texture2D t2dMainTex03 = null;
            if ("0" != mainTex03Ab && "0" != mainTex03) t2dMainTex03 = CommonLib.LoadAsset<Texture2D>(mainTex03Ab, mainTex03, false, mainManifest, true);

            var colorMask03Tex = lib.GetInfo(ChaListDefine.KeyType.ColorMask03Tex);
            var colorMask03Ab = lib.GetInfo(ChaListDefine.KeyType.ColorMask03AB);
            if ("0" == colorMask03Ab) colorMask03Ab = lib.GetInfo(ChaListDefine.KeyType.MainAB);
            Texture2D t2dColorMask03 = null;
            if ("0" != colorMask03Ab && "0" != colorMask03Tex)
            {
                t2dColorMask03 = CommonLib.LoadAsset<Texture2D>(colorMask03Ab, colorMask03Tex, false, mainManifest, true);
            }

            for (var i = 0; i < 3; i++)
            {
                CustomTextureCreate ctc = null;
                Texture2D main;
                Texture2D color;
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
                    main = t2dMainTex03;
                    color = t2dColorMask03;
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

        public bool UpdateClothesColorAndStuff()
        {
            if (_ctcArr[0] == null)
                return false;

            if (ClothesComponent == null)
                return false;

            // todo pattern support
            var partsInfo = new ChaFileClothes.PartsInfo();
            partsInfo.colorInfo[0].baseColor = _colorRend.material.GetColor(ChaShader._Color);
            partsInfo.colorInfo[1].baseColor = _colorRend.material.GetColor(ChaShader._Color2);
            partsInfo.colorInfo[2].baseColor = _colorRend.material.GetColor(ChaShader._Color3);
            // color4 is not used
                
            var result = true;
            var patternMasks = new[] { ChaShader._PatternMask1, ChaShader._PatternMask2, ChaShader._PatternMask3, ChaShader._PatternMask1 };
            for (var i = 0; i < 3; i++)
            {
                Texture2D tex = null;

                Owner.lstCtrl.GetFilePath(ChaListDefine.CategoryNo.mt_pattern, partsInfo.colorInfo[i].pattern,
                    ChaListDefine.KeyType.MainTexAB, ChaListDefine.KeyType.MainTex, out var abName, out var assetName);

                if (abName != "0" && assetName != "0")
                {
                    tex = CommonLib.LoadAsset<Texture2D>(abName, assetName, false, "", true);
                    Character.AddLoadAssetBundle(abName, "");
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
                    ctc.SetFloat(ChaShader._PatternOffset1u, partsInfo.colorInfo[0].offset.x);
                    ctc.SetFloat(ChaShader._PatternOffset1v, partsInfo.colorInfo[0].offset.y);
                    ctc.SetFloat(ChaShader._PatternRotator1, partsInfo.colorInfo[0].rotate);

                    ctc.SetColor(ChaShader._Color2, partsInfo.colorInfo[1].baseColor);
                    ctc.SetColor(ChaShader._Color2_2, partsInfo.colorInfo[1].patternColor);
                    ctc.SetFloat(ChaShader._PatternScale2u, partsInfo.colorInfo[1].tiling.x);
                    ctc.SetFloat(ChaShader._PatternScale2v, partsInfo.colorInfo[1].tiling.y);
                    ctc.SetColor(ChaShader._Color3, partsInfo.colorInfo[2].baseColor);
                    ctc.SetColor(ChaShader._Color3_2, partsInfo.colorInfo[2].patternColor);
                    ctc.SetFloat(ChaShader._PatternScale3u, partsInfo.colorInfo[2].tiling.x);
                    ctc.SetFloat(ChaShader._PatternScale3v, partsInfo.colorInfo[2].tiling.y);
                    ctc.SetFloat(ChaShader._PatternOffset2u, partsInfo.colorInfo[1].offset.x);
                    ctc.SetFloat(ChaShader._PatternOffset2v, partsInfo.colorInfo[1].offset.y);
                    ctc.SetFloat(ChaShader._PatternRotator2, partsInfo.colorInfo[1].rotate);
                    ctc.SetFloat(ChaShader._PatternOffset3u, partsInfo.colorInfo[2].offset.x);
                    ctc.SetFloat(ChaShader._PatternOffset3v, partsInfo.colorInfo[2].offset.y);
                    ctc.SetFloat(ChaShader._PatternRotator3, partsInfo.colorInfo[2].rotate);
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

        public void ApplyMasks()
        {
            //Owner.AddClothesStateKind(kindNo, _listInfoBase.GetInfo(ChaListDefine.KeyType.StateType));
            Console.WriteLine("2");
            if (Owner.texBodyAlphaMask == null)
            {
                if (_appliedBodyMask == null)
                {
                    Console.WriteLine("2a");

                    Owner.LoadAlphaMaskTexture(_listInfoBase.GetInfo(ChaListDefine.KeyType.OverBodyMaskAB), _listInfoBase.GetInfo(ChaListDefine.KeyType.OverBodyMask), 0);
                    _appliedBodyMask = Owner.texBodyAlphaMask;
                }
                else
                {
                    Console.WriteLine("2b");
                    Owner.texBodyAlphaMask = _appliedBodyMask;
                }

                if (Owner.customMatBody)
                {
                    Owner.customMatBody.SetTexture(ChaShader._AlphaMask, Owner.texBodyAlphaMask);
                }
            }

            if (Owner.texBraAlphaMask == null)
            {
                Console.WriteLine("3");
                if (_appliedBraMask == null)
                {
                    Console.WriteLine("3a");
                    Owner.LoadAlphaMaskTexture(_listInfoBase.GetInfo(ChaListDefine.KeyType.OverBraMaskAB), _listInfoBase.GetInfo(ChaListDefine.KeyType.OverBraMask), 1);
                    _appliedBraMask = Owner.texBraAlphaMask;
                }
                else
                {
                    Console.WriteLine("3b");
                    Owner.texBraAlphaMask = _appliedBraMask;
                }

                if (Owner.rendBra != null)
                {
                    Console.WriteLine("3c");
                    var listInfoBase2 = Owner.infoClothes[2];
                    if (listInfoBase2 != null)
                    {
                        var num2 = ((2 == listInfoBase2.GetInfoInt(ChaListDefine.KeyType.Coordinate)) ? 1 : 0);
                        string b;
                        if (listInfoBase2.dictInfo.TryGetValue(105, out b))
                        {
                            num2 = (("1" == b) ? 1 : 0);
                        }

                        if (Owner.rendBra[0] != null)
                        {
                            Owner.rendBra[0].material.SetTexture(ChaShader._AlphaMask, Owner.texBraAlphaMask);
                            Owner.rendBra[0].material.SetTextureOffset(ChaShader._AlphaMask, ChaListDefine.braOffset[num2]);
                            Owner.rendBra[0].material.SetTextureScale(ChaShader._AlphaMask, ChaListDefine.braTiling[num2]);
                        }

                        if (Owner.rendBra[1] != null)
                        {
                            Owner.rendBra[1].material.SetTexture(ChaShader._AlphaMask, Owner.texBraAlphaMask);
                            Owner.rendBra[1].material.SetTextureOffset(ChaShader._AlphaMask, ChaListDefine.braOffset[num2]);
                            Owner.rendBra[1].material.SetTextureScale(ChaShader._AlphaMask, ChaListDefine.braTiling[num2]);
                        }
                    }
                }
            }

            //todo bustnormals?
        }
    }
}
