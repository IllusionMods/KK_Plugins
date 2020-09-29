using BepInEx;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using UnityEngine;
using static KK_Plugins.MaterialEditor.MaterialAPI;
#if AI || HS2
using AIChara;
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
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    [BepInDependency(MaterialEditorPlugin.GUID)]
    [BepInDependency(XUnity.ResourceRedirector.Constants.PluginData.Identifier)]
    [BepInDependency(Sideloader.Sideloader.GUID)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class MEMaker : UI
    {
        /// <summary>
        /// MaterialEditor Maker plugin GUID
        /// </summary>
        public const string GUID = MaterialEditorPlugin.GUID + ".maker";
        /// <summary>
        /// MaterialEditor Maker plugin name
        /// </summary>
        public const string PluginName = MaterialEditorPlugin.PluginName + " Maker";
        /// <summary>
        /// MaterialEditor Maker plugin version
        /// </summary>
        public const string Version = MaterialEditorPlugin.Version;

        internal static MEMaker Instance;

        internal void Start()
        {
            Instance = this;
            MakerAPI.MakerBaseLoaded += MakerAPI_MakerBaseLoaded;
            MakerAPI.RegisterCustomSubCategories += MakerAPI_RegisterCustomSubCategories;
        }

        private void MakerAPI_MakerBaseLoaded(object s, RegisterCustomControlsEvent e)
        {
            InitUI();

#if KK || EC
            MakerAPI.AddAccessoryWindowControl(new MakerButton("Open Material Editor", null, this)).OnClick.AddListener(PopulateListAccessory);
            e.AddControl(new MakerButton("Open Material Editor (Body)", MakerConstants.Face.All, this)).OnClick.AddListener(() => PopulateListCharacter("body"));
            e.AddControl(new MakerButton("Open Material Editor (Face)", MakerConstants.Face.All, this)).OnClick.AddListener(() => PopulateListCharacter("face"));
            e.AddControl(new MakerButton("Open Material Editor (All)", MakerConstants.Face.All, this)).OnClick.AddListener(() => PopulateListCharacter());

            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Top, this)).OnClick.AddListener(() => PopulateListClothes(0));
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Bottom, this)).OnClick.AddListener(() => PopulateListClothes(1));
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Bra, this)).OnClick.AddListener(() => PopulateListClothes(2));
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Shorts, this)).OnClick.AddListener(() => PopulateListClothes(3));
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Gloves, this)).OnClick.AddListener(() => PopulateListClothes(4));
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Panst, this)).OnClick.AddListener(() => PopulateListClothes(5));
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Socks, this)).OnClick.AddListener(() => PopulateListClothes(6));
#if KK
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.InnerShoes, this)).OnClick.AddListener(() => PopulateListClothes(7));
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.OuterShoes, this)).OnClick.AddListener(() => PopulateListClothes(8));
#else
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Shoes, this)).OnClick.AddListener(() => PopulateListClothes(7));
#endif
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Hair.Back, this)).OnClick.AddListener(() => PopulateListHair(0));
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Hair.Front, this)).OnClick.AddListener(() => PopulateListHair(1));
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Hair.Side, this)).OnClick.AddListener(() => PopulateListHair(2));
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Hair.Extension, this)).OnClick.AddListener(() => PopulateListHair(3));

            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Face.Eyebrow, this)).OnClick.AddListener(() => PopulateListCharacter("mayuge"));
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Face.Eye, this)).OnClick.AddListener(() => PopulateListCharacter("eyeline,hitomi"));
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Face.Nose, this)).OnClick.AddListener(() => PopulateListCharacter("nose"));
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Face.Mouth, this)).OnClick.AddListener(() => PopulateListCharacter("tang,tooth,canine"));
#endif
        }

        private void MakerAPI_RegisterCustomSubCategories(object sender, RegisterSubCategoriesEvent e)
        {
#if AI || HS2
            MakerAPI.AddAccessoryWindowControl(new MakerButton("Open Material Editor", null, this)).OnClick.AddListener(PopulateListAccessory);
            e.AddControl(new MakerButton("Open Material Editor (Body)", MakerConstants.Body.All, this)).OnClick.AddListener(() => PopulateListCharacter("body"));
            e.AddControl(new MakerButton("Open Material Editor (Head)", MakerConstants.Body.All, this)).OnClick.AddListener(() => PopulateListCharacter("head"));
            e.AddControl(new MakerButton("Open Material Editor (All)", MakerConstants.Body.All, this)).OnClick.AddListener(() => PopulateListCharacter());

            MakerCategory hairCategory = new MakerCategory(MakerConstants.Hair.CategoryName, "ME", 0, "Material Editor");
            e.AddControl(new MakerButton("Open Material Editor (Back)", hairCategory, this)).OnClick.AddListener(() => PopulateListHair(0));
            e.AddControl(new MakerButton("Open Material Editor (Front)", hairCategory, this)).OnClick.AddListener(() => PopulateListHair(1));
            e.AddControl(new MakerButton("Open Material Editor (Side)", hairCategory, this)).OnClick.AddListener(() => PopulateListHair(2));
            e.AddControl(new MakerButton("Open Material Editor (Extension)", hairCategory, this)).OnClick.AddListener(() => PopulateListHair(3));
            e.AddSubCategory(hairCategory);

            MakerCategory clothesCategory = new MakerCategory(MakerConstants.Clothes.CategoryName, "ME", 0, "Material Editor");
            e.AddControl(new MakerButton("Open Material Editor (Top)", clothesCategory, this)).OnClick.AddListener(() => PopulateListClothes(0));
            e.AddControl(new MakerButton("Open Material Editor (Bottom)", clothesCategory, this)).OnClick.AddListener(() => PopulateListClothes(1));
            e.AddControl(new MakerButton("Open Material Editor (Bra)", clothesCategory, this)).OnClick.AddListener(() => PopulateListClothes(2));
            e.AddControl(new MakerButton("Open Material Editor (Underwear)", clothesCategory, this)).OnClick.AddListener(() => PopulateListClothes(3));
            e.AddControl(new MakerButton("Open Material Editor (Gloves)", clothesCategory, this)).OnClick.AddListener(() => PopulateListClothes(4));
            e.AddControl(new MakerButton("Open Material Editor (Pantyhose)", clothesCategory, this)).OnClick.AddListener(() => PopulateListClothes(5));
            e.AddControl(new MakerButton("Open Material Editor (Socks)", clothesCategory, this)).OnClick.AddListener(() => PopulateListClothes(6));
            e.AddControl(new MakerButton("Open Material Editor (Shoes)", clothesCategory, this)).OnClick.AddListener(() => PopulateListClothes(7));
            e.AddSubCategory(clothesCategory);

            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Face.Mouth, this)).OnClick.AddListener(() => PopulateListCharacter("tang,tooth"));
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Face.Eyes, this)).OnClick.AddListener(() => PopulateListCharacter("eyebase,eyeshadow"));
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Face.HL, this)).OnClick.AddListener(() => PopulateListCharacter("eyebase,eyeshadow"));
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Face.Eyelashes, this)).OnClick.AddListener(() => PopulateListCharacter("eyelashes"));
#endif
        }

        internal void PopulateListCharacter(string filter = "")
        {
            var chaControl = MakerAPI.GetCharacterControl();
            PopulateList(chaControl.gameObject, filter: filter);
        }

        private void PopulateListClothes(int index)
        {
            var chaControl = MakerAPI.GetCharacterControl();
            PopulateList(chaControl.objClothes[index], index);
        }

        internal void PopulateListAccessory()
        {
            var chaAccessoryComponent = MakerAPI.GetCharacterControl().GetAccessory(AccessoriesApi.SelectedMakerAccSlot);
            if (chaAccessoryComponent == null)
                PopulateList(null, AccessoriesApi.SelectedMakerAccSlot);
            else
                PopulateList(chaAccessoryComponent.gameObject, AccessoriesApi.SelectedMakerAccSlot);
        }

        private void PopulateListHair(int index)
        {
            var chaControl = MakerAPI.GetCharacterControl();
            PopulateList(chaControl.objHair[index], index);
        }

        internal override string GetRendererPropertyValueOriginal(int slot, Renderer renderer, RendererProperties property, GameObject go) =>
           MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).GetRendererPropertyValueOriginal(slot, renderer, property, go);
        internal override void SetRendererProperty(int slot, Renderer renderer, RendererProperties property, string value, GameObject go) =>
           MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).SetRendererProperty(slot, renderer, property, value, go);
        internal override void RemoveRendererProperty(int slot, Renderer renderer, RendererProperties property, GameObject go) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveRendererProperty(slot, renderer, property, go);

        internal override void MaterialCopyEdits(int slot, Material material, GameObject go) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).MaterialCopyEdits(slot, material, go);
        internal override void MaterialPasteEdits(int slot, Material material, GameObject go) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).MaterialPasteEdits(slot, material, go);

        internal override string GetMaterialShaderNameOriginal(int slot, Material material, GameObject go) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).GetMaterialShaderOriginal(slot, material, go);
        internal override void SetMaterialShaderName(int slot, Material material, string value, GameObject go) =>
           MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).SetMaterialShader(slot, material, value, go);
        internal override void RemoveMaterialShaderName(int slot, Material material, GameObject go) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveMaterialShader(slot, material, go);

        internal override int? GetMaterialShaderRenderQueueOriginal(int slot, Material material, GameObject go) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).GetMaterialShaderRenderQueueOriginal(slot, material, go);
        internal override void SetMaterialShaderRenderQueue(int slot, Material material, int value, GameObject go) =>
           MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).SetMaterialShaderRenderQueue(slot, material, value, go);
        internal override void RemoveMaterialShaderRenderQueue(int slot, Material material, GameObject go) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveMaterialShaderRenderQueue(slot, material, go);

        internal override bool GetMaterialTextureValueOriginal(int slot, Material material, string propertyName, GameObject go) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).GetMaterialTextureOriginal(slot, material, propertyName, go);
        internal override void SetMaterialTexture(int slot, Material material, string propertyName, string filePath, GameObject go) =>
             MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).SetMaterialTextureFromFile(slot, material, propertyName, filePath, go, true);
        internal override void RemoveMaterialTexture(int slot, Material material, string propertyName, GameObject go) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveMaterialTexture(slot, material, propertyName, go);

        internal override Vector2? GetMaterialTextureOffsetOriginal(int slot, Material material, string propertyName, GameObject go) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).GetMaterialTextureOffsetOriginal(slot, material, propertyName, go);
        internal override void SetMaterialTextureOffset(int slot, Material material, string propertyName, Vector2 value, GameObject go) =>
           MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).SetMaterialTextureOffset(slot, material, propertyName, value, go);
        internal override void RemoveMaterialTextureOffset(int slot, Material material, string propertyName, GameObject go) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveMaterialTextureOffset(slot, material, propertyName, go);

        internal override Vector2? GetMaterialTextureScaleOriginal(int slot, Material material, string propertyName, GameObject go) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).GetMaterialTextureScaleOriginal(slot, material, propertyName, go);
        internal override void SetMaterialTextureScale(int slot, Material material, string propertyName, Vector2 value, GameObject go) =>
           MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).SetMaterialTextureScale(slot, material, propertyName, value, go);
        internal override void RemoveMaterialTextureScale(int slot, Material material, string propertyName, GameObject go) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveMaterialTextureScale(slot, material, propertyName, go);

        internal override Color? GetMaterialColorPropertyValueOriginal(int slot, Material material, string propertyName, GameObject go) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).GetMaterialColorPropertyValueOriginal(slot, material, propertyName, go);
        internal override void SetMaterialColorProperty(int slot, Material material, string propertyName, Color value, GameObject go) =>
           MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).SetMaterialColorProperty(slot, material, propertyName, value, go);
        internal override void RemoveMaterialColorProperty(int slot, Material material, string propertyName, GameObject go) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveMaterialColorProperty(slot, material, propertyName, go);

        internal override float? GetMaterialFloatPropertyValueOriginal(int slot, Material material, string propertyName, GameObject go) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).GetMaterialFloatPropertyValueOriginal(slot, material, propertyName, go);
        internal override void SetMaterialFloatProperty(int slot, Material material, string propertyName, float value, GameObject go) =>
           MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).SetMaterialFloatProperty(slot, material, propertyName, value, go);
        internal override void RemoveMaterialFloatProperty(int slot, Material material, string propertyName, GameObject go) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveMaterialFloatProperty(slot, material, propertyName, go);
    }
}
