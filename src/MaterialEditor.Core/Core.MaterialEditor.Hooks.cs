using HarmonyLib;
using KKAPI.Maker;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
#if AI || HS2
using AIChara;
#elif KK
using ChaCustom;
using TMPro;
#endif
#if EC
using Map;
#else
using Studio;
#endif
#if PH
using ChaControl = Human;
#endif

namespace KK_Plugins.MaterialEditor
{
    internal partial class Hooks
    {
        /// <summary>
        /// Recursively iterates over game objects to create the list of body part renderers
        /// </summary>
        private static void GetBodyRendererList(GameObject gameObject, List<Renderer> rendList)
        {
            if (gameObject == null)
                return;

            //Don't search through clothes since a renderer with the same name as the body might be found there, particularly in PH
            if (MaterialEditorPlugin.ClothesParts.Contains(gameObject.NameFormatted()))
                return;

            //Check if the renderer is one of the specified body parts and add it to the list
            Renderer rend = gameObject.GetComponent<Renderer>();
            if (rend != null && MaterialEditorPlugin.BodyParts.Contains(rend.NameFormatted()))
                rendList.Add(rend);

            for (int i = 0; i < gameObject.transform.childCount; i++)
                GetBodyRendererList(gameObject.transform.GetChild(i).gameObject, rendList);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(MaterialEditorAPI.MaterialAPI), nameof(MaterialEditorAPI.MaterialAPI.GetRendererList))]
        private static bool MaterialAPI_GetRendererList(ref IEnumerable<Renderer> __result, GameObject gameObject)
        {
            if (gameObject == null)
                return true;

            //For ChaControl objects, return only specific renderes (i.e. not clothes, hair, etc.)
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl)
            {
                List<Renderer> rendList = new List<Renderer>();
                GetBodyRendererList(chaControl.gameObject, rendList);
                __result = rendList;
                return false;
            }

#if !PH
            //If this is an ItemComponent return only the renderers in the arrays, otherwise child objects will show in the UI
            var itemComponent = gameObject.GetComponent<ItemComponent>();
            if (itemComponent)
            {
                List<Renderer> rendList = new List<Renderer>();

#if KK
                for (int i = 0; i < itemComponent.rendNormal.Length; i++)
                    if (itemComponent.rendNormal[i] && !rendList.Contains(itemComponent.rendNormal[i]))
                        rendList.Add(itemComponent.rendNormal[i]);
                for (int i = 0; i < itemComponent.rendAlpha.Length; i++)
                    if (itemComponent.rendAlpha[i] && !rendList.Contains(itemComponent.rendAlpha[i]))
                        rendList.Add(itemComponent.rendAlpha[i]);
                for (int i = 0; i < itemComponent.rendGlass.Length; i++)
                    if (itemComponent.rendGlass[i] && !rendList.Contains(itemComponent.rendGlass[i]))
                        rendList.Add(itemComponent.rendGlass[i]);
#elif EC
                for (int i = 0; i < itemComponent.renderers.Length; i++)
                    if (itemComponent.renderers[i] && !rendList.Contains(itemComponent.renderers[i]))
                        rendList.Add(itemComponent.renderers[i]);
#else
                for (int i = 0; i < itemComponent.rendererInfos.Length; i++)
                    if (itemComponent.rendererInfos[i].renderer && !rendList.Contains(itemComponent.rendererInfos[i].renderer))
                        rendList.Add(itemComponent.rendererInfos[i].renderer);
                for (int i = 0; i < itemComponent.renderersSea.Length; i++)
                    if (itemComponent.renderersSea[i] && !rendList.Contains(itemComponent.renderersSea[i]))
                        rendList.Add(itemComponent.renderersSea[i]);
#endif
                if (rendList.Count > 0)
                {
                    __result = rendList;
                    return false;
                }
            }
#endif

            return true;
        }

#if PH
        /// <summary>
        /// Remove any renderers which have materials that correspond to the body material, since these will be overriden by the body material itself
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(MaterialEditorAPI.MaterialAPI), nameof(MaterialEditorAPI.MaterialAPI.GetRendererList))]
        private static void MaterialAPI_GetRendererList_Postfix(ref IEnumerable<Renderer> __result, GameObject gameObject)
        {
            if (gameObject.GetComponent<ChaControl>())
                return;

            List<Renderer> newRenderers = __result.ToList();
            newRenderers.RemoveAll(renderer => renderer.sharedMaterial == null ||
                                   renderer.sharedMaterial.name == "Default-Material" ||
                                   renderer.sharedMaterial.name == "cf_m_body_CustomMaterial" ||
                                   renderer.sharedMaterial.name == "cm_m_body_CustomMaterial");
            __result = newRenderers;
        }
#endif

        [HarmonyPrefix, HarmonyPatch(typeof(MaterialEditorAPI.MaterialAPI), nameof(MaterialEditorAPI.MaterialAPI.GetMaterials))]
        private static bool MaterialAPI_GetMaterials(ref IEnumerable<Material> __result, GameObject gameObject, Renderer renderer)
        {
            //Must use sharedMaterials for character objects or it breaks body masks, etc.
#if KK || EC
            if (gameObject.GetComponent<ChaControl>() && !MaterialEditorPlugin.MouthParts.Contains(renderer.NameFormatted()))
#else
            if (gameObject.GetComponent<ChaControl>())
#endif
            {
                __result = renderer.sharedMaterials.Where(x => x != null);
                return false;
            }

            return true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(MaterialEditorAPI.MaterialAPI), nameof(MaterialEditorAPI.MaterialAPI.SetMaterials))]
        private static bool MaterialAPI_SetMaterials(GameObject gameObject, Renderer renderer, Material[] materials)
        {
            //Must use sharedMaterials for character objects or it breaks body masks, etc.
#if KK || EC
            if (gameObject.GetComponent<ChaControl>() && !MaterialEditorPlugin.MouthParts.Contains(renderer.NameFormatted()))
#else
            if (gameObject.GetComponent<ChaControl>())
#endif
            {
                renderer.sharedMaterials = materials;
                return false;
            }

            return true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(MaterialEditorAPI.MaterialAPI), "LoadShaderDefaultTexture")]
        private static bool MaterialAPI_LoadShaderDefaultTexture(ref Texture2D __result, string assetBundlePath, string assetPath)
        {
            __result = CommonLib.LoadAsset<Texture2D>(assetBundlePath, assetPath);
            return false;
        }

#if PH
        [HarmonyPostfix, HarmonyPatch(typeof(WearCustomEdit), "ChangeOnWear")]
        private static void WearCustomEdit_ChangeOnWear(Character.WEAR_TYPE wear, ChaControl ___human) => MaterialEditorPlugin.GetCharaController(___human).ChangeCustomClothesEvent((int)wear);
        [HarmonyPostfix, HarmonyPatch(typeof(Body), nameof(Body.RendSkinTexture))]
        private static void Body_RendSkinTexture(ChaControl ___human) => MaterialEditorPlugin.GetCharaController(___human).RefreshBodyEdits();
        [HarmonyPostfix, HarmonyPatch(typeof(Head), nameof(Head.RendSkinTexture))]
        private static void Head_RendSkinTexture(ChaControl ___human) => MaterialEditorPlugin.GetCharaController(___human).RefreshBodyEdits();
        [HarmonyPostfix, HarmonyPatch(typeof(HairCustomEdit), nameof(HairCustomEdit.ChangeHair_Back))]
        private static void HairCustomEdit_ChangeHair_Back(ChaControl ___human) => MaterialEditorPlugin.GetCharaController(___human).ChangeHairEvent((int)Character.HAIR_TYPE.BACK);
        [HarmonyPostfix, HarmonyPatch(typeof(HairCustomEdit), nameof(HairCustomEdit.ChangeHair_Front))]
        private static void HairCustomEdit_ChangeHair_Front(ChaControl ___human) => MaterialEditorPlugin.GetCharaController(___human).ChangeHairEvent((int)Character.HAIR_TYPE.FRONT);
        [HarmonyPostfix, HarmonyPatch(typeof(HairCustomEdit), nameof(HairCustomEdit.ChangeHair_Side))]
        private static void HairCustomEdit_ChangeHair_Side(ChaControl ___human) => MaterialEditorPlugin.GetCharaController(___human).ChangeHairEvent((int)Character.HAIR_TYPE.SIDE);
        [HarmonyPostfix, HarmonyPatch(typeof(HairCustomEdit), nameof(HairCustomEdit.ChangeHair_Set))]
        private static void HairCustomEdit_ChangeHair_Set(ChaControl ___human)
        {
            var controller = MaterialEditorPlugin.GetCharaController(___human);
            controller.ChangeHairEvent((int)Character.HAIR_TYPE.BACK);
            controller.ChangeHairEvent((int)Character.HAIR_TYPE.FRONT);
            controller.ChangeHairEvent((int)Character.HAIR_TYPE.SIDE);
        }
#else
        /// <summary>
        /// Apply clothing state changes to all material copies
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeAlphaMask))]
        private static void ChangeAlphaMaskPostfix(ChaControl __instance)
        {
            if (__instance.customMatBody)
            {
#if KK || EC
                var rendBody = __instance.rendBody;
#else
                var rendBody = __instance.cmpBody.targetCustom.rendBody;
#endif
                if (rendBody.sharedMaterials.Length > 1)
                {
                    for (int i = 0; i < rendBody.sharedMaterials.Length; i++)
                    {
                        var mat = rendBody.sharedMaterials[i];
                        mat.SetFloat("_alpha_a", __instance.customMatBody.GetFloat("_alpha_a"));
                        mat.SetFloat("_alpha_b", __instance.customMatBody.GetFloat("_alpha_b"));
                    }
                }

                if (__instance.rendBra != null)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        if (__instance.rendBra[j] != null && __instance.rendBra[j].materials.Length > 1)
                        {
                            for (int i = 0; i < __instance.rendBra[j].materials.Length; i++)
                            {
                                var mat = __instance.rendBra[j].materials[i];
                                if (mat != null)
                                {
                                    mat.SetFloat("_alpha_a", __instance.customMatBody.GetFloat("_alpha_a"));
                                    mat.SetFloat("_alpha_b", __instance.customMatBody.GetFloat("_alpha_b"));
                                }
                            }
                        }
                    }
                }

#if KK || EC
                if (__instance.rendInner != null)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        if (__instance.rendInner[j] != null && __instance.rendInner[j].materials.Length > 1)
                        {
                            for (int i = 0; i < __instance.rendInner[j].materials.Length; i++)
                            {
                                var mat = __instance.rendInner[j].materials[i];
                                if (mat != null)
                                {
                                    mat.SetFloat(ChaShader._alpha_a, __instance.customMatBody.GetFloat(ChaShader._alpha_a));
                                    mat.SetFloat(ChaShader._alpha_b, __instance.customMatBody.GetFloat(ChaShader._alpha_b));
                                }
                            }
                        }
                    }
                }
#endif
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetClothesState))]
        private static void SetClothesStatePostfix(ChaControl __instance)
        {
            var controller = MaterialEditorPlugin.GetCharaController(__instance);
            if (controller != null)
                controller.ClothesStateChangeEvent();
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCustomClothes))]
        private static void ChangeCustomClothes(ChaControl __instance, int kind)
        {
            var controller = MaterialEditorPlugin.GetCharaController(__instance);
            if (controller != null)
                controller.ChangeCustomClothesEvent(kind);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeAccessory), typeof(int), typeof(int), typeof(int), typeof(string), typeof(bool))]
        private static void ChangeAccessory(ChaControl __instance, int slotNo, int type)
        {
            var controller = MaterialEditorPlugin.GetCharaController(__instance);
            if (controller != null)
                controller.ChangeAccessoryEvent(slotNo, type);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeHairAsync), typeof(int), typeof(int), typeof(bool), typeof(bool))]
        private static void ChangeHair(ChaControl __instance, int kind)
        {
            var controller = MaterialEditorPlugin.GetCharaController(__instance);
            if (controller != null)
                controller.ChangeHairEvent(kind);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.CreateBodyTexture))]
        private static void CreateBodyTextureHook(ChaControl __instance)
        {
            var controller = MaterialEditorPlugin.GetCharaController(__instance);
            if (controller != null)
                controller.RefreshBodyMainTex();
        }

#if AI || HS2
        internal static void ClothesColorChangeHook()
        {
            var controller = MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl());
            controller.CustomClothesOverride = true;
            controller.RefreshClothesMainTex();
        }

        [HarmonyPrefix, HarmonyPatch(typeof(CharaCustom.CvsA_Copy), nameof(CharaCustom.CvsA_Copy.CopyAccessory))]
        private static void CopyAccessoryOverride() => MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).CustomClothesOverride = true;
#else
        internal static void AccessoryTransferHook() => MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).CustomClothesOverride = true;

        /// <summary>
        /// Transfer accessory hook
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(ChaCustom.CvsAccessoryChange), nameof(ChaCustom.CvsAccessoryChange.CopyAcs))]
        private static void CopyAcsHook() => MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).CustomClothesOverride = true;

        //Clothing color change hooks
        [HarmonyPrefix, HarmonyPatch(typeof(ChaCustom.CvsClothes), nameof(ChaCustom.CvsClothes.FuncUpdateCosColor))]
        private static void FuncUpdateCosColorHook()
        {
            var controller = MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl());
            controller.CustomClothesOverride = true;
            controller.RefreshClothesMainTex();
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ChaCustom.CvsClothes), nameof(ChaCustom.CvsClothes.FuncUpdatePattern01))]
        private static void FuncUpdatePattern01Hook()
        {
            var controller = MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl());
            controller.CustomClothesOverride = true;
            controller.RefreshClothesMainTex();
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ChaCustom.CvsClothes), nameof(ChaCustom.CvsClothes.FuncUpdatePattern02))]
        private static void FuncUpdatePattern02Hook()
        {
            var controller = MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl());
            controller.CustomClothesOverride = true;
            controller.RefreshClothesMainTex();
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ChaCustom.CvsClothes), nameof(ChaCustom.CvsClothes.FuncUpdatePattern03))]
        private static void FuncUpdatePattern03Hook()
        {
            var controller = MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl());
            controller.CustomClothesOverride = true;
            controller.RefreshClothesMainTex();
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ChaCustom.CvsClothes), nameof(ChaCustom.CvsClothes.FuncUpdatePattern04))]
        private static void FuncUpdatePattern04Hook()
        {
            var controller = MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl());
            controller.CustomClothesOverride = true;
            controller.RefreshClothesMainTex();
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ChaCustom.CvsClothes), nameof(ChaCustom.CvsClothes.FuncUpdateAllPtnAndColor))]
        private static void FuncUpdateAllPtnAndColorHook()
        {
            var controller = MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl());
            controller.CustomClothesOverride = true;
            controller.RefreshClothesMainTex();
        }

        /// <summary>
        /// Apply mask textures to all material copies
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeClothesTopAsync))]
        private static void ChangeClothesTopAsyncPostfix(ChaControl __instance, ref IEnumerator __result)
        {
            var controller = MaterialEditorPlugin.GetCharaController(__instance);
            if (controller != null)
            {
                var original = __result;
                __result = new[] { original, Postfix() }.GetEnumerator();
            }

            IEnumerator Postfix()
            {
                if (__instance.rendBody.sharedMaterials.Length > 1 && __instance.texBodyAlphaMask != null)
                {
                    for (int i = 0; i < __instance.rendBody.sharedMaterials.Length; i++)
                    {
                        var mat = __instance.rendBody.sharedMaterials[i];
                        mat.SetTexture(ChaShader._AlphaMask, __instance.texBodyAlphaMask);
                    }
                }

                if (__instance.rendBra != null && __instance.texBraAlphaMask != null)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        if (__instance.rendBra[j] != null && __instance.rendBra[j].materials.Length > 1)
                        {
                            for (int i = 0; i < __instance.rendBra[j].materials.Length; i++)
                            {
                                var mat = __instance.rendBra[j].materials[i];
                                if (mat != null)
                                {
                                    mat.SetTexture(ChaShader._AlphaMask, __instance.texBraAlphaMask);
                                }
                            }
                        }
                    }
                }

                if (__instance.rendInner != null && __instance.texInnerAlphaMask != null)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        if (__instance.rendInner[j] != null && __instance.rendInner[j].materials.Length > 1)
                        {
                            for (int i = 0; i < __instance.rendInner[j].materials.Length; i++)
                            {
                                var mat = __instance.rendInner[j].materials[i];
                                if (mat != null)
                                {
                                    mat.SetTexture(ChaShader._AlphaMask, __instance.texInnerAlphaMask);
                                }
                            }
                        }
                    }
                }
                yield break;
            }
        }

        /// <summary>
        /// Apply juice to all material copies
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.UpdateSiru))]
        private static void ChaControl_UpdateSiru_Postfix(ChaControl __instance)
        {
            if (__instance.customMatFace && __instance.rendFace && __instance.rendFace.sharedMaterials.Length > 1)
            {
                for (int i = 0; i < __instance.rendFace.sharedMaterials.Length; i++)
                {
                    var mat = __instance.rendFace.sharedMaterials[i];
                    mat.SetFloat("_liquidface", __instance.customMatFace.GetFloat("_liquidface"));
                }
            }

            if (__instance.customMatBody && __instance.rendBody && __instance.rendBody.sharedMaterials.Length > 1)
            {
                for (int i = 0; i < __instance.rendBody.sharedMaterials.Length; i++)
                {
                    var mat = __instance.rendBody.sharedMaterials[i];
                    mat.SetFloat("_liquidftop", __instance.customMatBody.GetFloat("_liquidftop"));
                    mat.SetFloat("_liquidfbot", __instance.customMatBody.GetFloat("_liquidfbot"));
                    mat.SetFloat("_liquidbtop", __instance.customMatBody.GetFloat("_liquidbtop"));
                    mat.SetFloat("_liquidbbot", __instance.customMatBody.GetFloat("_liquidbbot"));
                }
            }
        }
#endif
#endif

#if KK    
        /// <summary>
        /// Apply eye tracking to all material copies
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(EyeLookMaterialControll), nameof(EyeLookMaterialControll.Update))]
        private static void EyeLookMaterialControll_Update_Postfix(EyeLookMaterialControll __instance, Material ____material)
        {
            if (__instance._renderer.sharedMaterials.Length == 1)
                return;

            for (int i = 0; i < __instance.texStates.Length; i++)
            {
                var texState = __instance.texStates[i];

                for (int j = 0; j < __instance._renderer.sharedMaterials.Length; j++)
                {
                    var mat = __instance._renderer.sharedMaterials[j];
                    mat.SetTextureOffset(texState.texID, ____material.GetTextureOffset(texState.texID));
                    mat.SetTextureScale(texState.texID, ____material.GetTextureScale(texState.texID));
                }
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCoordinateType), typeof(ChaFileDefine.CoordinateType), typeof(bool))]
        private static void ChangeCoordinateTypePrefix(ChaControl __instance)
        {
            var controller = MaterialEditorPlugin.GetCharaController(__instance);
            if (controller != null)
                controller.CoordinateChanging = true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCoordinateType), typeof(ChaFileDefine.CoordinateType), typeof(bool))]
        private static void ChangeCoordinateTypePostfix(ChaControl __instance)
        {
            var controller = MaterialEditorPlugin.GetCharaController(__instance);
            if (controller != null)
                controller.CoordinateChangedEvent();
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CvsClothesCopy), nameof(CvsClothesCopy.CopyClothes))]
        private static void CopyClothesPostfix(TMP_Dropdown[] ___ddCoordeType, Toggle[] ___tglKind)
        {
            List<int> copySlots = new List<int>();
            for (int i = 0; i < Enum.GetNames(typeof(ChaFileDefine.ClothesKind)).Length; i++)
                if (___tglKind[i].isOn)
                    copySlots.Add(i);

            var controller = MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl());
            if (controller != null)
                controller.ClothingCopiedEvent(___ddCoordeType[1].value, ___ddCoordeType[0].value, copySlots);
        }
#endif
    }
}
