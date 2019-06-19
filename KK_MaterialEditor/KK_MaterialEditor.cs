using BepInEx;
using BepInEx.Bootstrap;
using Harmony;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Studio.SaveLoad;
using Studio;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KK_MaterialEditor
{
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class KK_MaterialEditor : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.materialeditor";
        public const string PluginName = "Material Editor";
        public const string Version = "0.4";

        private void Main()
        {
            SceneManager.sceneLoaded += (s, lsm) => InitStudioUI(s.name);
            MakerAPI.MakerBaseLoaded += MakerAPI_MakerBaseLoaded;
            AccessoriesApi.SelectedMakerAccSlotChanged += AccessoriesApi_SelectedMakerAccSlotChanged;
            AccessoriesApi.AccessoryKindChanged += AccessoriesApi_AccessoryKindChanged;
            AccessoriesApi.AccessoriesCopied += AccessoriesApi_AccessoriesCopied;
            AccessoriesApi.AccessoryTransferred += AccessoriesApi_AccessoryTransferred;

            CharacterApi.RegisterExtraBehaviour<MaterialEditorCharaController>(GUID);
            StudioSaveLoadApi.RegisterExtraBehaviour<MaterialEditorSceneController>(GUID);

            HarmonyInstance.Create(GUID).PatchAll(typeof(Hooks));
        }

        private void AccessoriesApi_AccessoryTransferred(object sender, AccessoryTransferEventArgs e) => GetCharaController(MakerAPI.GetCharacterControl())?.AccessoryTransferredEvent(sender, e);
        private void AccessoriesApi_AccessoriesCopied(object sender, AccessoryCopyEventArgs e) => GetCharaController(MakerAPI.GetCharacterControl())?.AccessoriesCopiedEvent(sender, e);
        private void AccessoriesApi_AccessoryKindChanged(object sender, AccessorySlotEventArgs e) => GetCharaController(MakerAPI.GetCharacterControl())?.AccessoryKindChangeEvent(sender, e);
        private void AccessoriesApi_SelectedMakerAccSlotChanged(object sender, AccessorySlotEventArgs e) => GetCharaController(MakerAPI.GetCharacterControl())?.AccessorySelectedSlotChangeEvent(sender, e);

        private static void SetFloatProperty(GameObject go, Material mat, string property, string value)
        {
            float floatValue = float.Parse(value);

            foreach (var obj in go.GetComponentsInChildren<Renderer>())
                foreach (var objMat in obj.materials)
                    if (objMat.name == mat.name)
                        objMat.SetFloat($"_{property}", floatValue);
        }

        private static void SetColorProperty(GameObject go, Material mat, string property, Color value)
        {
            foreach (var obj in go.GetComponentsInChildren<Renderer>())
                foreach (var objMat in obj.materials)
                    if (objMat.name == mat.name)
                        objMat.SetColor($"_{property}", value);
        }

        private static void SetColorRProperty(GameObject go, Material mat, string property, string value)
        {
            float floatValue = float.Parse(value);
            Color colorOrig = mat.GetColor($"_{property}");

            foreach (var obj in go.GetComponentsInChildren<Renderer>())
                foreach (var objMat in obj.materials)
                    if (objMat.name == mat.name)
                        objMat.SetColor($"_{property}", new Color(floatValue, colorOrig.g, colorOrig.b, colorOrig.a));
        }

        private static void SetColorGProperty(GameObject go, Material mat, string property, string value)
        {
            float floatValue = float.Parse(value);
            Color colorOrig = mat.GetColor($"_{property}");

            foreach (var obj in go.GetComponentsInChildren<Renderer>())
                foreach (var objMat in obj.materials)
                    if (objMat.name == mat.name)
                        objMat.SetColor($"_{property}", new Color(colorOrig.r, floatValue, colorOrig.b, colorOrig.a));
        }

        private static void SetColorBProperty(GameObject go, Material mat, string property, string value)
        {
            float floatValue = float.Parse(value);
            Color colorOrig = mat.GetColor($"_{property}");

            foreach (var obj in go.GetComponentsInChildren<Renderer>())
                foreach (var objMat in obj.materials)
                    if (objMat.name == mat.name)
                        objMat.SetColor($"_{property}", new Color(colorOrig.r, colorOrig.g, floatValue, colorOrig.a));
        }

        private static void SetColorAProperty(GameObject go, Material mat, string property, string value)
        {
            float floatValue = float.Parse(value);
            Color colorOrig = mat.GetColor($"_{property}");

            foreach (var obj in go.GetComponentsInChildren<Renderer>())
                foreach (var objMat in obj.materials)
                    if (objMat.name == mat.name)
                        objMat.SetColor($"_{property}", new Color(colorOrig.r, colorOrig.g, colorOrig.b, floatValue));
        }

        private static void SetRendererProperty(Renderer rend, RendererProperties property, int value)
        {
            if (property == RendererProperties.ShadowCastingMode)
                rend.shadowCastingMode = (UnityEngine.Rendering.ShadowCastingMode)value;
            else if (property == RendererProperties.ReceiveShadows)
                rend.receiveShadows = value == 1;
            else if (property == RendererProperties.Enabled)
                rend.enabled = value == 1;
        }

        private static void SetTextureProperty(GameObject go, Material mat, string property, Texture2D value)
        {
            foreach (var obj in go.GetComponentsInChildren<Renderer>())
                foreach (var objMat in obj.materials)
                    if (objMat.name == mat.name)
                        objMat.SetTexture($"_{property}", value);
        }

        public static Texture2D TextureFromBytes(byte[] texBytes, TextureFormat format)
        {
            if (texBytes == null || texBytes.Length == 0) return null;

            var tex = new Texture2D(2, 2, format, false);
            tex.LoadImage(texBytes);
            return tex;
        }

        public enum ObjectType { StudioItem, Clothing, Accessory, Hair, Other };
        private static int GetObjectID(ObjectCtrlInfo oci) => Studio.Studio.Instance.dicObjectCtrl.First(x => x.Value == oci).Key;
        public static MaterialEditorSceneController GetSceneController() => Chainloader.ManagerObject.transform.GetComponentInChildren<MaterialEditorSceneController>();
        public static MaterialEditorCharaController GetCharaController(ChaControl character) => character?.gameObject?.GetComponent<MaterialEditorCharaController>();
    }
}