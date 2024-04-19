using BepInEx;
using HarmonyLib;
using KKAPI;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using MaterialEditorAPI;
using System;
using System.Collections;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;
#if AI || HS2
using AIChara;
using ChaClothesComponent = AIChara.CmpClothes;
using ChaCustomHairComponent = AIChara.CmpHair;
#endif

namespace KK_Plugins.MaterialEditor
{
    /// <summary>
    /// Plugin responsible for handling events from the character maker
    /// </summary>
#if KK
    [BepInProcess(Constants.MainGameProcessNameSteam)]
#endif
    [BepInProcess(Constants.MainGameProcessName)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInDependency(MaterialEditorPlugin.PluginGUID, MaterialEditorPlugin.PluginVersion)]
    [BepInDependency(XUnity.ResourceRedirector.Constants.PluginData.Identifier, XUnity.ResourceRedirector.Constants.PluginData.Version)]
#if !PH
    [BepInDependency(Sideloader.Sideloader.GUID, Sideloader.Sideloader.Version)]
#endif
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class MEMaker : MaterialEditorUI
    {
        /// <summary>
        /// MaterialEditor Maker plugin GUID
        /// </summary>
        public const string GUID = MaterialEditorPlugin.PluginGUID + ".maker";
        /// <summary>
        /// MaterialEditor Maker plugin name
        /// </summary>
        public const string PluginName = MaterialEditorPlugin.PluginName + " Maker";
        /// <summary>
        /// MaterialEditor Maker plugin version
        /// </summary>
        public const string Version = MaterialEditorPlugin.PluginVersion;
        /// <summary>
        /// Instance of the plugin
        /// </summary>
        public static MEMaker Instance;

        public static MakerButton MaterialEditorButton;
        internal static int currentHairIndex;
        internal static int currentClothesIndex;

        private void Start()
        {
            Instance = this;
            MakerAPI.MakerBaseLoaded += MakerAPI_MakerBaseLoaded;
            MakerAPI.RegisterCustomSubCategories += MakerAPI_RegisterCustomSubCategories;
            MakerAPI.MakerFinishedLoading += (s, e) => ToggleButtonVisibility();
            MakerAPI.ReloadCustomInterface += (s, e) =>
            {
                StartCoroutine(Wait());
                IEnumerator Wait()
                {
                    yield return null;
                    ToggleButtonVisibility();
                }
            };
            MakerAPI.MakerExiting += (s, e) => ColorPalette = null;
            AccessoriesApi.SelectedMakerAccSlotChanged += (s, e) => ToggleButtonVisibility();
            AccessoriesApi.AccessoryKindChanged += (s, e) => ToggleButtonVisibility();
            AccessoriesApi.AccessoryTransferred += (s, e) => ToggleButtonVisibility();
#if KK || KKS
            AccessoriesApi.AccessoriesCopied += (s, e) => ToggleButtonVisibility();
#endif

            Harmony.CreateAndPatchAll(typeof(MakerHooks));
        }

        private void MakerAPI_MakerBaseLoaded(object s, RegisterCustomControlsEvent e)
        {
            InitUI();

#if KK || EC || KKS
            MaterialEditorButton = MakerAPI.AddAccessoryWindowControl(new MakerButton("Material Editor", null, this));
            MaterialEditorButton.GroupingID = "Buttons";
            MaterialEditorButton.OnClick.AddListener(UpdateUIAccessory);
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Body.All, this)).OnClick.AddListener(() => UpdateUICharacter("body"));
            e.AddControl(new MakerButton("Material Editor (Body)", MakerConstants.Face.All, this)).OnClick.AddListener(() => UpdateUICharacter("body"));
            e.AddControl(new MakerButton("Material Editor (Face)", MakerConstants.Face.All, this)).OnClick.AddListener(() => UpdateUICharacter("face"));
            e.AddControl(new MakerButton("Material Editor (All)", MakerConstants.Face.All, this)).OnClick.AddListener(() => UpdateUICharacter());

            e.AddControl(new MakerButton("Material Editor", MakerConstants.Clothes.Top, this)).OnClick.AddListener(() => UpdateUIClothes(0));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Clothes.Bottom, this)).OnClick.AddListener(() => UpdateUIClothes(1));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Clothes.Bra, this)).OnClick.AddListener(() => UpdateUIClothes(2));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Clothes.Shorts, this)).OnClick.AddListener(() => UpdateUIClothes(3));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Clothes.Gloves, this)).OnClick.AddListener(() => UpdateUIClothes(4));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Clothes.Panst, this)).OnClick.AddListener(() => UpdateUIClothes(5));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Clothes.Socks, this)).OnClick.AddListener(() => UpdateUIClothes(6));
#if KK
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Clothes.InnerShoes, this)).OnClick.AddListener(() => UpdateUIClothes(7));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Clothes.OuterShoes, this)).OnClick.AddListener(() => UpdateUIClothes(8));
#elif KKS
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Clothes.OuterShoes, this)).OnClick.AddListener(() => UpdateUIClothes(8));
#elif EC
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Clothes.Shoes, this)).OnClick.AddListener(() => UpdateUIClothes(7));
#endif
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Hair.Back, this)).OnClick.AddListener(() => UpdateUIHair(0));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Hair.Front, this)).OnClick.AddListener(() => UpdateUIHair(1));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Hair.Side, this)).OnClick.AddListener(() => UpdateUIHair(2));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Hair.Extension, this)).OnClick.AddListener(() => UpdateUIHair(3));

            e.AddControl(new MakerButton("Material Editor", MakerConstants.Face.Eyebrow, this)).OnClick.AddListener(() => UpdateUICharacter("mayuge"));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Face.Eye, this)).OnClick.AddListener(() => UpdateUICharacter("eyeline,hitomi,sirome"));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Face.Nose, this)).OnClick.AddListener(() => UpdateUICharacter("nose"));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Face.Mouth, this)).OnClick.AddListener(() => UpdateUICharacter("tang,tooth,canine"));
#if KKS
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Face.Iris, this)).OnClick.AddListener(() => UpdateUICharacter("eyeline,hitomi,sirome"));

#endif
#endif

#if PH
            MaterialEditorButton = MakerAPI.AddAccessoryWindowControl(new MakerButton("Material Editor", null, this));
            MaterialEditorButton.OnClick.AddListener(UpdateUIAccessory);
            e.AddControl(new MakerButton("Material Editor (Body)", MakerConstants.Body.General, this)).OnClick.AddListener(() => UpdateUICharacter("body"));
            e.AddControl(new MakerButton("Material Editor (All)", MakerConstants.Body.General, this)).OnClick.AddListener(() => UpdateUICharacter());
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Body.Nail, this)).OnClick.AddListener(() => UpdateUICharacter("nail"));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Body.Lower, this)).OnClick.AddListener(() => UpdateUICharacter("mnpk"));

            e.AddControl(new MakerButton("Material Editor (Face)", MakerConstants.Face.General, this)).OnClick.AddListener(() => UpdateUICharacter("head,face"));
            e.AddControl(new MakerButton("Material Editor (All)", MakerConstants.Face.General, this)).OnClick.AddListener(() => UpdateUICharacter());
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Face.Eye, this)).OnClick.AddListener(() => UpdateUICharacter("eye"));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Face.Eyebrow, this)).OnClick.AddListener(() => UpdateUICharacter("mayuge"));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Face.Eyelash, this)).OnClick.AddListener(() => UpdateUICharacter("matuge"));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Face.Mouth, this)).OnClick.AddListener(() => UpdateUICharacter("ha,sita"));

            e.AddControl(new MakerButton("Material Editor", MakerConstants.Wear.Tops, this)).OnClick.AddListener(() => UpdateUIClothes(0));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Wear.Bottoms, this)).OnClick.AddListener(() => UpdateUIClothes(1));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Wear.Bra, this)).OnClick.AddListener(() => UpdateUIClothes(2));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Wear.Shorts, this)).OnClick.AddListener(() => UpdateUIClothes(3));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Wear.SwimTops, this)).OnClick.AddListener(() => UpdateUIClothes(4));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Wear.SwimBottoms, this)).OnClick.AddListener(() => UpdateUIClothes(5));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Wear.SwimWear, this)).OnClick.AddListener(() => UpdateUIClothes(6));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Wear.Glove, this)).OnClick.AddListener(() => UpdateUIClothes(7));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Wear.Panst, this)).OnClick.AddListener(() => UpdateUIClothes(8));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Wear.Socks, this)).OnClick.AddListener(() => UpdateUIClothes(9));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Wear.Shoes, this)).OnClick.AddListener(() => UpdateUIClothes(10));

            e.AddControl(new MakerButton("Material Editor", MakerConstants.Hair.Set, this)).OnClick.AddListener(() => UpdateUIHair(0));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Hair.Back, this)).OnClick.AddListener(() => UpdateUIHair(0));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Hair.Front, this)).OnClick.AddListener(() => UpdateUIHair(1));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Hair.Side, this)).OnClick.AddListener(() => UpdateUIHair(2));

#endif
            currentHairIndex = 0;
            currentClothesIndex = 0;

            ColorPalette = new MakerColorPalette();
        }

        private void MakerAPI_RegisterCustomSubCategories(object sender, RegisterSubCategoriesEvent e)
        {
#if AI || HS2
            MaterialEditorButton = MakerAPI.AddAccessoryWindowControl(new MakerButton("Material Editor", null, this));
            MaterialEditorButton.GroupingID = "Buttons";
            MaterialEditorButton.OnClick.AddListener(UpdateUIAccessory);
            e.AddControl(new MakerButton("Material Editor (Body)", MakerConstants.Body.All, this)).OnClick.AddListener(() => UpdateUICharacter("body"));
            e.AddControl(new MakerButton("Material Editor (Head)", MakerConstants.Body.All, this)).OnClick.AddListener(() => UpdateUICharacter("head"));
            e.AddControl(new MakerButton("Material Editor (All)", MakerConstants.Body.All, this)).OnClick.AddListener(() => UpdateUICharacter());

            MakerCategory hairCategory = new MakerCategory(MakerConstants.Hair.CategoryName, "ME", 0, "Material Editor");
            e.AddControl(new MakerButton("Material Editor (Back)", hairCategory, this)).OnClick.AddListener(() => UpdateUIHair(0));
            e.AddControl(new MakerButton("Material Editor (Front)", hairCategory, this)).OnClick.AddListener(() => UpdateUIHair(1));
            e.AddControl(new MakerButton("Material Editor (Side)", hairCategory, this)).OnClick.AddListener(() => UpdateUIHair(2));
            e.AddControl(new MakerButton("Material Editor (Extension)", hairCategory, this)).OnClick.AddListener(() => UpdateUIHair(3));
            e.AddSubCategory(hairCategory);

            MakerCategory clothesCategory = new MakerCategory(MakerConstants.Clothes.CategoryName, "ME", 0, "Material Editor");
            e.AddControl(new MakerButton("Material Editor (Top)", clothesCategory, this)).OnClick.AddListener(() => UpdateUIClothes(0));
            e.AddControl(new MakerButton("Material Editor (Bottom)", clothesCategory, this)).OnClick.AddListener(() => UpdateUIClothes(1));
            e.AddControl(new MakerButton("Material Editor (Bra)", clothesCategory, this)).OnClick.AddListener(() => UpdateUIClothes(2));
            e.AddControl(new MakerButton("Material Editor (Underwear)", clothesCategory, this)).OnClick.AddListener(() => UpdateUIClothes(3));
            e.AddControl(new MakerButton("Material Editor (Gloves)", clothesCategory, this)).OnClick.AddListener(() => UpdateUIClothes(4));
            e.AddControl(new MakerButton("Material Editor (Pantyhose)", clothesCategory, this)).OnClick.AddListener(() => UpdateUIClothes(5));
            e.AddControl(new MakerButton("Material Editor (Socks)", clothesCategory, this)).OnClick.AddListener(() => UpdateUIClothes(6));
            e.AddControl(new MakerButton("Material Editor (Shoes)", clothesCategory, this)).OnClick.AddListener(() => UpdateUIClothes(7));
            e.AddSubCategory(clothesCategory);

            e.AddControl(new MakerButton("Material Editor", MakerConstants.Face.Mouth, this)).OnClick.AddListener(() => UpdateUICharacter("tang,tooth"));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Face.Eyes, this)).OnClick.AddListener(() => UpdateUICharacter("eyebase,eyeshadow"));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Face.HL, this)).OnClick.AddListener(() => UpdateUICharacter("eyebase,eyeshadow"));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Face.Eyelashes, this)).OnClick.AddListener(() => UpdateUICharacter("eyelashes"));
#endif
        }

        public static void ToggleButtonVisibility()
        {
            if (!MakerAPI.InsideMaker || MaterialEditorButton == null)
                return;

            var accessory = MakerAPI.GetCharacterControl().GetAccessoryObject(AccessoriesApi.SelectedMakerAccSlot);
            if (accessory == null)
            {
                MaterialEditorButton.Visible.OnNext(false);
            }
            else
            {
                MaterialEditorButton.Visible.OnNext(true);
            }
        }

        /// <summary>
        /// Shows the MaterialEditor UI for the character or refreshes the UI if already open
        /// </summary>
        /// <param name="filter"></param>
        public void UpdateUICharacter(string filter = "")
        {
            if (!MakerAPI.InsideAndLoaded)
                return;

            var chaControl = MakerAPI.GetCharacterControl();
            PopulateList(chaControl.gameObject, new ObjectData(0, MaterialEditorCharaController.ObjectType.Character), filter);
        }

        /// <summary>
        /// Shows the MaterialEditor UI for the specified clothing index or refreshes the UI if already open
        /// </summary>
        /// <param name="index"></param>
        public void UpdateUIClothes(int index)
        {
            if (!MakerAPI.InsideAndLoaded)
                return;

#if KK || KKS
            if (index > 8)
#elif PH
            if (index > 10)
#else
            if (index > 7)
#endif
            {
                Visible = false;
                return;
            }

            var chaControl = MakerAPI.GetCharacterControl();
            var clothes = chaControl.GetClothes(index);
#if PH
            if (clothes == null)
#else
            if (clothes == null || clothes.GetComponentInChildren<ChaClothesComponent>() == null)
#endif
                Visible = false;
            else
                PopulateList(clothes, new ObjectData(index, MaterialEditorCharaController.ObjectType.Clothing));
        }

        /// <summary>
        /// Shows the MaterialEditor UI for the currently selected accesory or refreshes the UI if already open
        /// </summary>
        public void UpdateUIAccessory()
        {
            if (!MakerAPI.InsideAndLoaded)
                return;

            var accessory = MakerAPI.GetCharacterControl().GetAccessoryObject(AccessoriesApi.SelectedMakerAccSlot);
            if (accessory == null)
                Visible = false;
            else
                PopulateList(accessory, new ObjectData(AccessoriesApi.SelectedMakerAccSlot, MaterialEditorCharaController.ObjectType.Accessory));
        }

        /// <summary>
        /// Shows the MaterialEditor UI for the specified hair index or refreshes the UI if already open
        /// </summary>
        public void UpdateUIHair(int index)
        {
            if (!MakerAPI.InsideAndLoaded)
                return;

            if (index > 3)
            {
                Visible = false;
                return;
            }

            var chaControl = MakerAPI.GetCharacterControl();
            var hair = chaControl.GetHair(index);
#if PH
            if (hair == null)
#else
            if (hair.GetComponent<ChaCustomHairComponent>() == null)
#endif
                Visible = false;
            else
                PopulateList(hair, new ObjectData(index, MaterialEditorCharaController.ObjectType.Hair));
        }

        public override string GetRendererPropertyValueOriginal(object data, Renderer renderer, RendererProperties property, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            return MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).GetRendererPropertyValueOriginal(objectData.Slot, objectData.ObjectType, renderer, property, go);
        }
        public override string GetRendererPropertyValue(object data, Renderer renderer, RendererProperties property, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            return MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).GetRendererPropertyValue(objectData.Slot, objectData.ObjectType, renderer, property, go);
        }
        public override void SetRendererProperty(object data, Renderer renderer, RendererProperties property, string value, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).SetRendererProperty(objectData.Slot, objectData.ObjectType, renderer, property, value, go);
        }
        public override void RemoveRendererProperty(object data, Renderer renderer, RendererProperties property, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveRendererProperty(objectData.Slot, objectData.ObjectType, renderer, property, go);
        }

        public override float? GetProjectorPropertyValueOriginal(object data, Projector renderer, ProjectorProperties property, GameObject gameObject)
        {
            throw new NotImplementedException();
        }

        public override float? GetProjectorPropertyValue(object data, Projector renderer, ProjectorProperties property, GameObject gameObject)
        {
            throw new NotImplementedException();
        }

        public override void SetProjectorProperty(object data, Projector projector, ProjectorProperties property, float value, GameObject gameObject)
        {
            throw new NotImplementedException();
        }

        public override void RemoveProjectorProperty(object data, Projector projector, ProjectorProperties property, GameObject gameObject)
        {
            throw new NotImplementedException();
        }

        public override void MaterialCopyEdits(object data, Material material, GameObject go, Projector projector = null)
        {
            ObjectData objectData = (ObjectData)data;
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).MaterialCopyEdits(objectData.Slot, objectData.ObjectType, material, go);
        }
        public override void MaterialPasteEdits(object data, Material material, GameObject go, Projector projector = null)
        {
            ObjectData objectData = (ObjectData)data;
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).MaterialPasteEdits(objectData.Slot, objectData.ObjectType, material, go);
        }
        public override void MaterialCopyRemove(object data, Material material, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).MaterialCopyRemove(objectData.Slot, objectData.ObjectType, material, go);
        }

        public override string GetMaterialShaderNameOriginal(object data, Material material, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            return MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).GetMaterialShaderOriginal(objectData.Slot, objectData.ObjectType, material, go);
        }
        public override void SetMaterialShaderName(object data, Material material, string value, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).SetMaterialShader(objectData.Slot, objectData.ObjectType, material, value, go);
        }
        public override void RemoveMaterialShaderName(object data, Material material, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveMaterialShader(objectData.Slot, objectData.ObjectType, material, go);
        }

        public override int? GetMaterialShaderRenderQueueOriginal(object data, Material material, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            return MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).GetMaterialShaderRenderQueueOriginal(objectData.Slot, objectData.ObjectType, material, go);
        }
        public override void SetMaterialShaderRenderQueue(object data, Material material, int value, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).SetMaterialShaderRenderQueue(objectData.Slot, objectData.ObjectType, material, value, go);
        }
        public override void RemoveMaterialShaderRenderQueue(object data, Material material, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveMaterialShaderRenderQueue(objectData.Slot, objectData.ObjectType, material, go);
        }

        public override bool GetMaterialTextureValueOriginal(object data, Material material, string propertyName, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            return MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).GetMaterialTextureOriginal(objectData.Slot, objectData.ObjectType, material, propertyName, go);
        }
        public override void SetMaterialTexture(object data, Material material, string propertyName, string filePath, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).SetMaterialTextureFromFile(objectData.Slot, objectData.ObjectType, material, propertyName, filePath, go, true);
        }
        public override void RemoveMaterialTexture(object data, Material material, string propertyName, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveMaterialTexture(objectData.Slot, objectData.ObjectType, material, propertyName, go);
        }

        public override Vector2? GetMaterialTextureOffsetOriginal(object data, Material material, string propertyName, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            return MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).GetMaterialTextureOffsetOriginal(objectData.Slot, objectData.ObjectType, material, propertyName, go);
        }
        public override void SetMaterialTextureOffset(object data, Material material, string propertyName, Vector2 value, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).SetMaterialTextureOffset(objectData.Slot, objectData.ObjectType, material, propertyName, value, go);
        }
        public override void RemoveMaterialTextureOffset(object data, Material material, string propertyName, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveMaterialTextureOffset(objectData.Slot, objectData.ObjectType, material, propertyName, go);
        }

        public override Vector2? GetMaterialTextureScaleOriginal(object data, Material material, string propertyName, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            return MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).GetMaterialTextureScaleOriginal(objectData.Slot, objectData.ObjectType, material, propertyName, go);
        }
        public override void SetMaterialTextureScale(object data, Material material, string propertyName, Vector2 value, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).SetMaterialTextureScale(objectData.Slot, objectData.ObjectType, material, propertyName, value, go);
        }
        public override void RemoveMaterialTextureScale(object data, Material material, string propertyName, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveMaterialTextureScale(objectData.Slot, objectData.ObjectType, material, propertyName, go);
        }

        public override Color? GetMaterialColorPropertyValueOriginal(object data, Material material, string propertyName, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            return MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).GetMaterialColorPropertyValueOriginal(objectData.Slot, objectData.ObjectType, material, propertyName, go);
        }
        public override void SetMaterialColorProperty(object data, Material material, string propertyName, Color value, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).SetMaterialColorProperty(objectData.Slot, objectData.ObjectType, material, propertyName, value, go);
        }
        public override void RemoveMaterialColorProperty(object data, Material material, string propertyName, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveMaterialColorProperty(objectData.Slot, objectData.ObjectType, material, propertyName, go);
        }

        public override float? GetMaterialFloatPropertyValueOriginal(object data, Material material, string propertyName, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            return MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).GetMaterialFloatPropertyValueOriginal(objectData.Slot, objectData.ObjectType, material, propertyName, go);
        }
        public override void SetMaterialFloatProperty(object data, Material material, string propertyName, float value, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).SetMaterialFloatProperty(objectData.Slot, objectData.ObjectType, material, propertyName, value, go);
        }
        public override void RemoveMaterialFloatProperty(object data, Material material, string propertyName, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveMaterialFloatProperty(objectData.Slot, objectData.ObjectType, material, propertyName, go);
        }

        public override bool? GetMaterialKeywordPropertyValueOriginal(object data, Material material, string propertyName, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            return MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).GetMaterialKeywordPropertyValueOriginal(objectData.Slot, objectData.ObjectType, material, propertyName, go);
        }
        public override void SetMaterialKeywordProperty(object data, Material material, string propertyName, bool value, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).SetMaterialKeywordProperty(objectData.Slot, objectData.ObjectType, material, propertyName, value, go);
        }
        public override void RemoveMaterialKeywordProperty(object data, Material material, string propertyName, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveMaterialKeywordProperty(objectData.Slot, objectData.ObjectType, material, propertyName, go);
        }
    }
}
