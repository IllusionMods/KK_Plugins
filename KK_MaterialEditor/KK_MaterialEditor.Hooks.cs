using ChaCustom;
using Harmony;
using KKAPI.Maker;
using Studio;

namespace KK_MaterialEditor
{
    internal class Hooks
    {
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCoordinateType), new[] { typeof(ChaFileDefine.CoordinateType), typeof(bool) })]
        public static void ChangeCoordinateTypePrefix(ChaControl __instance) => KK_MaterialEditor.GetCharaController(__instance)?.CoordinateChangeEvent();

        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetClothesState))]
        public static void SetClothesStatePostfix(ChaControl __instance) => KK_MaterialEditor.GetCharaController(__instance)?.ClothesStateChangeEvent();

        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCustomClothes))]
        public static void ChangeCustomClothes(ChaControl __instance, int kind) => KK_MaterialEditor.GetCharaController(__instance)?.ChangeCustomClothesEvent(kind);

        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeAccessory), new[] { typeof(int), typeof(int), typeof(int), typeof(string), typeof(bool) })]
        public static void ChangeAccessory(ChaControl __instance, int slotNo, int type) => KK_MaterialEditor.GetCharaController(__instance)?.ChangeAccessoryEvent(slotNo, type);

        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeHairAsync), new[] { typeof(int), typeof(int), typeof(bool), typeof(bool) })]
        public static void ChangeHair(ChaControl __instance, int kind) => KK_MaterialEditor.GetCharaController(__instance)?.ChangeHairEvent(kind);

        [HarmonyPrefix, HarmonyPatch(typeof(OCIItem), nameof(OCIItem.OnDelete))]
        public static void OCIItemOnDelete(OCIItem __instance) => KK_MaterialEditor.GetSceneController()?.ItemDeleteEvent(__instance.objectInfo.dicKey);

        [HarmonyPrefix, HarmonyPatch(typeof(CvsAccessory), nameof(CvsAccessory.UpdateSelectAccessoryKind))]
        public static void UpdateSelectAccessoryKindPrefix(CvsAccessory __instance, ref int __state) => __state = MakerAPI.GetCharacterControl().nowCoordinate.accessory.parts[(int)__instance.slotNo].id;

        [HarmonyPostfix, HarmonyPatch(typeof(CvsAccessory), nameof(CvsAccessory.UpdateSelectAccessoryKind))]
        public static void UpdateSelectAccessoryKindPostfix(CvsAccessory __instance, ref int __state)
        {
            if (__state != MakerAPI.GetCharacterControl().nowCoordinate.accessory.parts[(int)__instance.slotNo].id)
                KK_MaterialEditor.GetCharaController(MakerAPI.GetCharacterControl()).AccessoryKindChangeEvent((int)__instance.slotNo);
        }
    }
}
