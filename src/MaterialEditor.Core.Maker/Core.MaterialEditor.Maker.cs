using BepInEx.Harmony;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using UnityEngine;
using static KK_Plugins.MaterialEditor.MaterialAPI;
#if AI || HS2
using AIChara;
#endif

namespace KK_Plugins.MaterialEditor
{
    public partial class MEMaker : UI
    {
        public const string GUID = MaterialEditorPlugin.GUID + ".maker";
        public const string PluginName = MaterialEditorPlugin.PluginName + " Maker";
        public const string Version = MaterialEditorPlugin.Version;

        internal void Start()
        {
            MakerAPI.MakerBaseLoaded += MakerAPI_MakerBaseLoaded;
            MakerAPI.RegisterCustomSubCategories += MakerAPI_RegisterCustomSubCategories;
            AccessoriesApi.SelectedMakerAccSlotChanged += AccessoriesApi_SelectedMakerAccSlotChanged;
            AccessoriesApi.AccessoryKindChanged += AccessoriesApi_AccessoryKindChanged;
            AccessoriesApi.AccessoryTransferred += AccessoriesApi_AccessoryTransferred;
#if KK
            AccessoriesApi.AccessoriesCopied += AccessoriesApi_AccessoriesCopied;
#endif

            HarmonyWrapper.PatchAll(typeof(MakerHooks));
        }

        private void AccessoriesApi_AccessoryTransferred(object sender, AccessoryTransferEventArgs e) => HideUI();
        private void AccessoriesApi_AccessoryKindChanged(object sender, AccessorySlotEventArgs e) => HideUI();
        private void AccessoriesApi_SelectedMakerAccSlotChanged(object sender, AccessorySlotEventArgs e) => HideUI();
#if KK
        private void AccessoriesApi_AccessoriesCopied(object sender, AccessoryCopyEventArgs e) => HideUI();
#endif

        private void MakerAPI_MakerBaseLoaded(object s, RegisterCustomControlsEvent e)
        {
            InitUI();

#if KK || EC
            MakerAPI.AddAccessoryWindowControl(new MakerButton("Open Material Editor", null, this)).OnClick.AddListener(delegate { PopulateListAccessory(); });
            e.AddControl(new MakerButton("Open Material Editor (Body)", MakerConstants.Face.All, this)).OnClick.AddListener(delegate { PopulateListCharacter("body"); });
            e.AddControl(new MakerButton("Open Material Editor (Face)", MakerConstants.Face.All, this)).OnClick.AddListener(delegate { PopulateListCharacter("face"); });
            e.AddControl(new MakerButton("Open Material Editor (All)", MakerConstants.Face.All, this)).OnClick.AddListener(delegate { PopulateListCharacter(); });

            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Top, this)).OnClick.AddListener(delegate { PopulateListClothes(0); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Bottom, this)).OnClick.AddListener(delegate { PopulateListClothes(1); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Bra, this)).OnClick.AddListener(delegate { PopulateListClothes(2); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Shorts, this)).OnClick.AddListener(delegate { PopulateListClothes(3); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Gloves, this)).OnClick.AddListener(delegate { PopulateListClothes(4); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Panst, this)).OnClick.AddListener(delegate { PopulateListClothes(5); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Socks, this)).OnClick.AddListener(delegate { PopulateListClothes(6); });
#if KK
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.InnerShoes, this)).OnClick.AddListener(delegate { PopulateListClothes(7); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.OuterShoes, this)).OnClick.AddListener(delegate { PopulateListClothes(8); });
#else
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Shoes, this)).OnClick.AddListener(delegate { PopulateListClothes(7); });
#endif
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Hair.Back, this)).OnClick.AddListener(delegate { PopulateListHair(0); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Hair.Front, this)).OnClick.AddListener(delegate { PopulateListHair(1); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Hair.Side, this)).OnClick.AddListener(delegate { PopulateListHair(2); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Hair.Extension, this)).OnClick.AddListener(delegate { PopulateListHair(3); });

            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Face.Eyebrow, this)).OnClick.AddListener(delegate { PopulateListCharacter("mayuge"); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Face.Eye, this)).OnClick.AddListener(delegate { PopulateListCharacter("eyeline,hitomi"); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Face.Nose, this)).OnClick.AddListener(delegate { PopulateListCharacter("nose"); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Face.Mouth, this)).OnClick.AddListener(delegate { PopulateListCharacter("tang,tooth"); });
#endif
        }

        private void MakerAPI_RegisterCustomSubCategories(object sender, RegisterSubCategoriesEvent e)
        {
#if AI || HS2
            MakerAPI.AddAccessoryWindowControl(new MakerButton("Open Material Editor", null, this)).OnClick.AddListener(delegate { PopulateListAccessory(); });
            e.AddControl(new MakerButton("Open Material Editor (Body)", MakerConstants.Body.All, this)).OnClick.AddListener(delegate { PopulateListCharacter("body"); });
            e.AddControl(new MakerButton("Open Material Editor (Head)", MakerConstants.Body.All, this)).OnClick.AddListener(delegate { PopulateListCharacter("head"); });
            e.AddControl(new MakerButton("Open Material Editor (All)", MakerConstants.Body.All, this)).OnClick.AddListener(delegate { PopulateListCharacter(); });

            MakerCategory hairCategory = new MakerCategory(MakerConstants.Hair.CategoryName, "ME", 0, "Material Editor");
            e.AddControl(new MakerButton("Open Material Editor (Back)", hairCategory, this)).OnClick.AddListener(delegate { PopulateListHair(0); });
            e.AddControl(new MakerButton("Open Material Editor (Front)", hairCategory, this)).OnClick.AddListener(delegate { PopulateListHair(1); });
            e.AddControl(new MakerButton("Open Material Editor (Side)", hairCategory, this)).OnClick.AddListener(delegate { PopulateListHair(2); });
            e.AddControl(new MakerButton("Open Material Editor (Extension)", hairCategory, this)).OnClick.AddListener(delegate { PopulateListHair(3); });
            e.AddSubCategory(hairCategory);

            MakerCategory clothesCategory = new MakerCategory(MakerConstants.Clothes.CategoryName, "ME", 0, "Material Editor");
            e.AddControl(new MakerButton("Open Material Editor (Top)", clothesCategory, this)).OnClick.AddListener(delegate { PopulateListClothes(0); });
            e.AddControl(new MakerButton("Open Material Editor (Bottom)", clothesCategory, this)).OnClick.AddListener(delegate { PopulateListClothes(1); });
            e.AddControl(new MakerButton("Open Material Editor (Bra)", clothesCategory, this)).OnClick.AddListener(delegate { PopulateListClothes(2); });
            e.AddControl(new MakerButton("Open Material Editor (Underwear)", clothesCategory, this)).OnClick.AddListener(delegate { PopulateListClothes(3); });
            e.AddControl(new MakerButton("Open Material Editor (Gloves)", clothesCategory, this)).OnClick.AddListener(delegate { PopulateListClothes(4); });
            e.AddControl(new MakerButton("Open Material Editor (Pantyhose)", clothesCategory, this)).OnClick.AddListener(delegate { PopulateListClothes(5); });
            e.AddControl(new MakerButton("Open Material Editor (Socks)", clothesCategory, this)).OnClick.AddListener(delegate { PopulateListClothes(6); });
            e.AddControl(new MakerButton("Open Material Editor (Shoes)", clothesCategory, this)).OnClick.AddListener(delegate { PopulateListClothes(7); });
            e.AddSubCategory(clothesCategory);

            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Face.Mouth, this)).OnClick.AddListener(delegate { PopulateListCharacter("tang,tooth"); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Face.Eyes, this)).OnClick.AddListener(delegate { PopulateListCharacter("eyebase,eyeshadow"); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Face.HL, this)).OnClick.AddListener(delegate { PopulateListCharacter("eyebase,eyeshadow"); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Face.Eyelashes, this)).OnClick.AddListener(delegate { PopulateListCharacter("eyelashes"); });
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
            int coordinateIndex = MaterialEditorPlugin.GetCharaController(chaControl).CurrentCoordinateIndex;
            PopulateList(chaControl.objClothes[index], coordinateIndex: coordinateIndex, slot: index);
        }

        private void PopulateListAccessory()
        {
            var chaControl = MakerAPI.GetCharacterControl();
            int coordinateIndex = MaterialEditorPlugin.GetCharaController(chaControl).CurrentCoordinateIndex;
            var chaAccessoryComponent = AccessoriesApi.GetAccessory(MakerAPI.GetCharacterControl(), AccessoriesApi.SelectedMakerAccSlot);
            PopulateList(chaAccessoryComponent?.gameObject, coordinateIndex: coordinateIndex, slot: AccessoriesApi.SelectedMakerAccSlot);
        }

        private void PopulateListHair(int index)
        {
            var chaControl = MakerAPI.GetCharacterControl();
            PopulateList(chaControl.objHair[index], slot: index);
        }

        internal override string GetRendererPropertyValueOriginal(int coordinateIndex, int slot, string rendererName, RendererProperties property, GameObject gameObject) =>
           MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).GetRendererPropertyValueOriginal(coordinateIndex, slot, rendererName, property, gameObject);
        internal override void AddRendererProperty(int coordinateIndex, int slot, string rendererName, RendererProperties property, string value, string valueOriginal, GameObject gameObject) =>
           MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).AddRendererProperty(coordinateIndex, slot, rendererName, property, value, valueOriginal, gameObject);
        internal override void RemoveRendererProperty(int coordinateIndex, int slot, string rendererName, RendererProperties property, GameObject gameObject) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveRendererProperty(coordinateIndex, slot, rendererName, property, gameObject);

        internal override string GetMaterialShaderNameOriginal(int coordinateIndex, int slot, string materialName, GameObject gameObject) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).GetMaterialShaderOriginal(coordinateIndex, slot, materialName, gameObject);
        internal override void AddMaterialShaderName(int coordinateIndex, int slot, string materialName, string value, string valueOriginal, GameObject gameObject) =>
           MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).AddMaterialShader(coordinateIndex, slot, materialName, value, valueOriginal, gameObject);
        internal override void RemoveMaterialShaderName(int coordinateIndex, int slot, string materialName, GameObject gameObject) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveMaterialShader(coordinateIndex, slot, materialName, gameObject);

        internal override int? GetMaterialShaderRenderQueueOriginal(int coordinateIndex, int slot, string materialName, GameObject gameObject) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).GetMaterialShaderRenderQueueOriginal(coordinateIndex, slot, materialName, gameObject);
        internal override void AddMaterialShaderRenderQueue(int coordinateIndex, int slot, string materialName, int value, int valueOriginal, GameObject gameObject) =>
           MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).AddMaterialShaderRenderQueue(coordinateIndex, slot, materialName, value, valueOriginal, gameObject);
        internal override void RemoveMaterialShaderRenderQueue(int coordinateIndex, int slot, string materialName, GameObject gameObject) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveMaterialShaderRenderQueue(coordinateIndex, slot, materialName, gameObject);

        internal override bool GetMaterialTextureValueOriginal(int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).GetMaterialTextureOriginal(coordinateIndex, slot, materialName, propertyName, gameObject);
        internal override void AddMaterialTexture(int coordinateIndex, int slot, string materialName, string propertyName, string filePath, GameObject gameObject) =>
             MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).AddMaterialTextureFromFile(coordinateIndex, slot, materialName, propertyName, filePath, gameObject, true);
        internal override void RemoveMaterialTexture(int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveMaterialTexture(coordinateIndex, slot, materialName, propertyName, gameObject);

        internal override Vector2? GetMaterialTextureOffsetOriginal(int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).GetMaterialTextureOffsetOriginal(coordinateIndex, slot, materialName, propertyName, gameObject);
        internal override void AddMaterialTextureOffset(int coordinateIndex, int slot, string materialName, string propertyName, Vector2 value, Vector2 valueOriginal, GameObject gameObject) =>
           MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).AddMaterialTextureOffset(coordinateIndex, slot, materialName, propertyName, value, valueOriginal, gameObject);
        internal override void RemoveMaterialTextureOffset(int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveMaterialTextureOffset(coordinateIndex, slot, materialName, propertyName, gameObject);

        internal override Vector2? GetMaterialTextureScaleOriginal(int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).GetMaterialTextureScaleOriginal(coordinateIndex, slot, materialName, propertyName, gameObject);
        internal override void AddMaterialTextureScale(int coordinateIndex, int slot, string materialName, string propertyName, Vector2 value, Vector2 valueOriginal, GameObject gameObject) =>
           MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).AddMaterialTextureScale(coordinateIndex, slot, materialName, propertyName, value, valueOriginal, gameObject);
        internal override void RemoveMaterialTextureScale(int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveMaterialTextureScale(coordinateIndex, slot, materialName, propertyName, gameObject);

        internal override Color? GetMaterialColorPropertyValueOriginal(int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).GetMaterialColorPropertyValueOriginal(coordinateIndex, slot, materialName, propertyName, gameObject);
        internal override void AddMaterialColorProperty(int coordinateIndex, int slot, string materialName, string propertyName, Color value, Color valueOriginal, GameObject gameObject) =>
           MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).AddMaterialColorProperty(coordinateIndex, slot, materialName, propertyName, value, valueOriginal, gameObject);
        internal override void RemoveMaterialColorProperty(int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveMaterialColorProperty(coordinateIndex, slot, materialName, propertyName, gameObject);

        internal override string GetMaterialFloatPropertyValueOriginal(int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).GetMaterialFloatPropertyValueOriginal(coordinateIndex, slot, materialName, propertyName, gameObject);
        internal override void AddMaterialFloatProperty(int coordinateIndex, int slot, string materialName, string propertyName, float value, float valueOriginal, GameObject gameObject) =>
           MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).AddMaterialFloatProperty(coordinateIndex, slot, materialName, propertyName, value, valueOriginal, gameObject);
        internal override void RemoveMaterialFloatProperty(int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveMaterialFloatProperty(coordinateIndex, slot, materialName, propertyName, gameObject);
    }
}
