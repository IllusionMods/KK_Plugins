using BepInEx;
using HarmonyLib;
using KKAPI;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using MaterialEditor;
using UnityEngine;
using static MaterialEditor.MaterialAPI;
#if AI || HS2
using AIChara;
using ChaClothesComponent = AIChara.CmpClothes;
using ChaCustomHairComponent = AIChara.CmpHair;
#endif

namespace KK_Plugins.MaterialEditorWrapper
{
    /// <summary>
    /// Plugin responsible for handling events from the character maker
    /// </summary>
#if KK
    [BepInProcess(Constants.MainGameProcessNameSteam)]
#endif
    [BepInProcess(Constants.MainGameProcessName)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInDependency(Plugin.PluginGUID, Plugin.PluginVersion)]
    [BepInDependency(XUnity.ResourceRedirector.Constants.PluginData.Identifier, XUnity.ResourceRedirector.Constants.PluginData.Version)]
    [BepInDependency(Sideloader.Sideloader.GUID, Sideloader.Sideloader.Version)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class MEMaker : MaterialEditorUI
    {
        /// <summary>
        /// MaterialEditor Maker plugin GUID
        /// </summary>
        public const string GUID = Plugin.PluginGUID + ".maker";
        /// <summary>
        /// MaterialEditor Maker plugin name
        /// </summary>
        public const string PluginName = Plugin.PluginName + " Maker";
        /// <summary>
        /// MaterialEditor Maker plugin version
        /// </summary>
        public const string Version = Plugin.PluginVersion;
        /// <summary>
        /// Instance of the plugin
        /// </summary>
        public static MEMaker Instance;

        internal static int currentHairIndex;
        internal static int currentClothesIndex;

        private void Start()
        {
            Instance = this;
            MakerAPI.MakerBaseLoaded += MakerAPI_MakerBaseLoaded;
            MakerAPI.RegisterCustomSubCategories += MakerAPI_RegisterCustomSubCategories;

            Harmony.CreateAndPatchAll(typeof(MakerHooks));
        }

        private void MakerAPI_MakerBaseLoaded(object s, RegisterCustomControlsEvent e)
        {
            InitUI();

#if KK || EC
            MakerAPI.AddAccessoryWindowControl(new MakerButton("Open Material Editor", null, this)).OnClick.AddListener(UpdateUIAccessory);
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Body.All, this)).OnClick.AddListener(() => UpdateUICharacter("body"));
            e.AddControl(new MakerButton("Open Material Editor (Body)", MakerConstants.Face.All, this)).OnClick.AddListener(() => UpdateUICharacter("body"));
            e.AddControl(new MakerButton("Open Material Editor (Face)", MakerConstants.Face.All, this)).OnClick.AddListener(() => UpdateUICharacter("face"));
            e.AddControl(new MakerButton("Open Material Editor (All)", MakerConstants.Face.All, this)).OnClick.AddListener(() => UpdateUICharacter());

            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Top, this)).OnClick.AddListener(() => UpdateUIClothes(0));
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Bottom, this)).OnClick.AddListener(() => UpdateUIClothes(1));
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Bra, this)).OnClick.AddListener(() => UpdateUIClothes(2));
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Shorts, this)).OnClick.AddListener(() => UpdateUIClothes(3));
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Gloves, this)).OnClick.AddListener(() => UpdateUIClothes(4));
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Panst, this)).OnClick.AddListener(() => UpdateUIClothes(5));
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Socks, this)).OnClick.AddListener(() => UpdateUIClothes(6));
#if KK
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.InnerShoes, this)).OnClick.AddListener(() => UpdateUIClothes(7));
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.OuterShoes, this)).OnClick.AddListener(() => UpdateUIClothes(8));
#else
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Shoes, this)).OnClick.AddListener(() => UpdateUIClothes(7));
#endif
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Hair.Back, this)).OnClick.AddListener(() => UpdateUIHair(0));
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Hair.Front, this)).OnClick.AddListener(() => UpdateUIHair(1));
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Hair.Side, this)).OnClick.AddListener(() => UpdateUIHair(2));
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Hair.Extension, this)).OnClick.AddListener(() => UpdateUIHair(3));

            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Face.Eyebrow, this)).OnClick.AddListener(() => UpdateUICharacter("mayuge"));
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Face.Eye, this)).OnClick.AddListener(() => UpdateUICharacter("eyeline,hitomi"));
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Face.Nose, this)).OnClick.AddListener(() => UpdateUICharacter("nose"));
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Face.Mouth, this)).OnClick.AddListener(() => UpdateUICharacter("tang,tooth,canine"));
#endif

            currentHairIndex = 0;
            currentClothesIndex = 0;
        }

        private void MakerAPI_RegisterCustomSubCategories(object sender, RegisterSubCategoriesEvent e)
        {
#if AI || HS2
            MakerAPI.AddAccessoryWindowControl(new MakerButton("Open Material Editor", null, this)).OnClick.AddListener(UpdateUIAccessory);
            e.AddControl(new MakerButton("Open Material Editor (Body)", MakerConstants.Body.All, this)).OnClick.AddListener(() => UpdateUICharacter("body"));
            e.AddControl(new MakerButton("Open Material Editor (Head)", MakerConstants.Body.All, this)).OnClick.AddListener(() => UpdateUICharacter("head"));
            e.AddControl(new MakerButton("Open Material Editor (All)", MakerConstants.Body.All, this)).OnClick.AddListener(() => UpdateUICharacter());

            MakerCategory hairCategory = new MakerCategory(MakerConstants.Hair.CategoryName, "ME", 0, "Material Editor");
            e.AddControl(new MakerButton("Open Material Editor (Back)", hairCategory, this)).OnClick.AddListener(() => UpdateUIHair(0));
            e.AddControl(new MakerButton("Open Material Editor (Front)", hairCategory, this)).OnClick.AddListener(() => UpdateUIHair(1));
            e.AddControl(new MakerButton("Open Material Editor (Side)", hairCategory, this)).OnClick.AddListener(() => UpdateUIHair(2));
            e.AddControl(new MakerButton("Open Material Editor (Extension)", hairCategory, this)).OnClick.AddListener(() => UpdateUIHair(3));
            e.AddSubCategory(hairCategory);

            MakerCategory clothesCategory = new MakerCategory(MakerConstants.Clothes.CategoryName, "ME", 0, "Material Editor");
            e.AddControl(new MakerButton("Open Material Editor (Top)", clothesCategory, this)).OnClick.AddListener(() => UpdateUIClothes(0));
            e.AddControl(new MakerButton("Open Material Editor (Bottom)", clothesCategory, this)).OnClick.AddListener(() => UpdateUIClothes(1));
            e.AddControl(new MakerButton("Open Material Editor (Bra)", clothesCategory, this)).OnClick.AddListener(() => UpdateUIClothes(2));
            e.AddControl(new MakerButton("Open Material Editor (Underwear)", clothesCategory, this)).OnClick.AddListener(() => UpdateUIClothes(3));
            e.AddControl(new MakerButton("Open Material Editor (Gloves)", clothesCategory, this)).OnClick.AddListener(() => UpdateUIClothes(4));
            e.AddControl(new MakerButton("Open Material Editor (Pantyhose)", clothesCategory, this)).OnClick.AddListener(() => UpdateUIClothes(5));
            e.AddControl(new MakerButton("Open Material Editor (Socks)", clothesCategory, this)).OnClick.AddListener(() => UpdateUIClothes(6));
            e.AddControl(new MakerButton("Open Material Editor (Shoes)", clothesCategory, this)).OnClick.AddListener(() => UpdateUIClothes(7));
            e.AddSubCategory(clothesCategory);

            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Face.Mouth, this)).OnClick.AddListener(() => UpdateUICharacter("tang,tooth"));
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Face.Eyes, this)).OnClick.AddListener(() => UpdateUICharacter("eyebase,eyeshadow"));
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Face.HL, this)).OnClick.AddListener(() => UpdateUICharacter("eyebase,eyeshadow"));
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Face.Eyelashes, this)).OnClick.AddListener(() => UpdateUICharacter("eyelashes"));
#endif
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
            PopulateList(chaControl.gameObject, 0, filter);
        }

        /// <summary>
        /// Shows the MaterialEditor UI for the specified clothing index or refreshes the UI if already open
        /// </summary>
        /// <param name="index"></param>
        public void UpdateUIClothes(int index)
        {
            if (!MakerAPI.InsideAndLoaded)
                return;

#if KK
            if (index > 8)
#else
            if (index > 7)
#endif
            {
                Visible = false;
                return;
            }

            var chaControl = MakerAPI.GetCharacterControl();
            if (chaControl.objClothes[index] == null || chaControl.objClothes[index].GetComponentInChildren<ChaClothesComponent>() == null)
                Visible = false;
            else
                PopulateList(chaControl.objClothes[index], index);
        }

        /// <summary>
        /// Shows the MaterialEditor UI for the currently selected accesory or refreshes the UI if already open
        /// </summary>
        public void UpdateUIAccessory()
        {
            if (!MakerAPI.InsideAndLoaded)
                return;

            var chaAccessoryComponent = MakerAPI.GetCharacterControl().GetAccessory(AccessoriesApi.SelectedMakerAccSlot);
            if (chaAccessoryComponent == null)
                Visible = false;
            else
                PopulateList(chaAccessoryComponent.gameObject, AccessoriesApi.SelectedMakerAccSlot);
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
            if (chaControl.objHair[index].GetComponent<ChaCustomHairComponent>() == null)
                Visible = false;
            else
                PopulateList(chaControl.objHair[index], index);
        }

        public override string GetRendererPropertyValueOriginal(object data, Renderer renderer, RendererProperties property, GameObject go) =>
           Plugin.GetCharaController(MakerAPI.GetCharacterControl()).GetRendererPropertyValueOriginal((int)data, renderer, property, go);
        public override void SetRendererProperty(object data, Renderer renderer, RendererProperties property, string value, GameObject go) =>
           Plugin.GetCharaController(MakerAPI.GetCharacterControl()).SetRendererProperty((int)data, renderer, property, value, go);
        public override void RemoveRendererProperty(object data, Renderer renderer, RendererProperties property, GameObject go) =>
            Plugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveRendererProperty((int)data, renderer, property, go);

        public override void MaterialCopyEdits(object data, Material material, GameObject go) =>
            Plugin.GetCharaController(MakerAPI.GetCharacterControl()).MaterialCopyEdits((int)data, material, go);
        public override void MaterialPasteEdits(object data, Material material, GameObject go) =>
            Plugin.GetCharaController(MakerAPI.GetCharacterControl()).MaterialPasteEdits((int)data, material, go);

        public override string GetMaterialShaderNameOriginal(object data, Material material, GameObject go) =>
            Plugin.GetCharaController(MakerAPI.GetCharacterControl()).GetMaterialShaderOriginal((int)data, material, go);
        public override void SetMaterialShaderName(object data, Material material, string value, GameObject go) =>
           Plugin.GetCharaController(MakerAPI.GetCharacterControl()).SetMaterialShader((int)data, material, value, go);
        public override void RemoveMaterialShaderName(object data, Material material, GameObject go) =>
            Plugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveMaterialShader((int)data, material, go);

        public override int? GetMaterialShaderRenderQueueOriginal(object data, Material material, GameObject go) =>
            Plugin.GetCharaController(MakerAPI.GetCharacterControl()).GetMaterialShaderRenderQueueOriginal((int)data, material, go);
        public override void SetMaterialShaderRenderQueue(object data, Material material, int value, GameObject go) =>
           Plugin.GetCharaController(MakerAPI.GetCharacterControl()).SetMaterialShaderRenderQueue((int)data, material, value, go);
        public override void RemoveMaterialShaderRenderQueue(object data, Material material, GameObject go) =>
            Plugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveMaterialShaderRenderQueue((int)data, material, go);

        public override bool GetMaterialTextureValueOriginal(object data, Material material, string propertyName, GameObject go) =>
            Plugin.GetCharaController(MakerAPI.GetCharacterControl()).GetMaterialTextureOriginal((int)data, material, propertyName, go);
        public override void SetMaterialTexture(object data, Material material, string propertyName, string filePath, GameObject go) =>
             Plugin.GetCharaController(MakerAPI.GetCharacterControl()).SetMaterialTextureFromFile((int)data, material, propertyName, filePath, go, true);
        public override void RemoveMaterialTexture(object data, Material material, string propertyName, GameObject go) =>
            Plugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveMaterialTexture((int)data, material, propertyName, go);

        public override Vector2? GetMaterialTextureOffsetOriginal(object data, Material material, string propertyName, GameObject go) =>
            Plugin.GetCharaController(MakerAPI.GetCharacterControl()).GetMaterialTextureOffsetOriginal((int)data, material, propertyName, go);
        public override void SetMaterialTextureOffset(object data, Material material, string propertyName, Vector2 value, GameObject go) =>
           Plugin.GetCharaController(MakerAPI.GetCharacterControl()).SetMaterialTextureOffset((int)data, material, propertyName, value, go);
        public override void RemoveMaterialTextureOffset(object data, Material material, string propertyName, GameObject go) =>
            Plugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveMaterialTextureOffset((int)data, material, propertyName, go);

        public override Vector2? GetMaterialTextureScaleOriginal(object data, Material material, string propertyName, GameObject go) =>
            Plugin.GetCharaController(MakerAPI.GetCharacterControl()).GetMaterialTextureScaleOriginal((int)data, material, propertyName, go);
        public override void SetMaterialTextureScale(object data, Material material, string propertyName, Vector2 value, GameObject go) =>
           Plugin.GetCharaController(MakerAPI.GetCharacterControl()).SetMaterialTextureScale((int)data, material, propertyName, value, go);
        public override void RemoveMaterialTextureScale(object data, Material material, string propertyName, GameObject go) =>
            Plugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveMaterialTextureScale((int)data, material, propertyName, go);

        public override Color? GetMaterialColorPropertyValueOriginal(object data, Material material, string propertyName, GameObject go) =>
            Plugin.GetCharaController(MakerAPI.GetCharacterControl()).GetMaterialColorPropertyValueOriginal((int)data, material, propertyName, go);
        public override void SetMaterialColorProperty(object data, Material material, string propertyName, Color value, GameObject go) =>
           Plugin.GetCharaController(MakerAPI.GetCharacterControl()).SetMaterialColorProperty((int)data, material, propertyName, value, go);
        public override void RemoveMaterialColorProperty(object data, Material material, string propertyName, GameObject go) =>
            Plugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveMaterialColorProperty((int)data, material, propertyName, go);

        public override float? GetMaterialFloatPropertyValueOriginal(object data, Material material, string propertyName, GameObject go) =>
            Plugin.GetCharaController(MakerAPI.GetCharacterControl()).GetMaterialFloatPropertyValueOriginal((int)data, material, propertyName, go);
        public override void SetMaterialFloatProperty(object data, Material material, string propertyName, float value, GameObject go) =>
           Plugin.GetCharaController(MakerAPI.GetCharacterControl()).SetMaterialFloatProperty((int)data, material, propertyName, value, go);
        public override void RemoveMaterialFloatProperty(object data, Material material, string propertyName, GameObject go) =>
            Plugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveMaterialFloatProperty((int)data, material, propertyName, go);
    }
}
