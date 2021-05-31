#if KK || EC || KKS
using ChaCustom;
#elif AI || HS2
using CharaCustom;
using KKAPI.Utilities;
#endif
using MaterialEditorAPI;
using System;
using UnityEngine;

namespace KK_Plugins.MaterialEditor
{
#if PH
    internal class MakerColorPalette : IMaterialEditorColorPalette
    {
        public void Close() { }
        public bool IsShowing(string title, object data, string materialName) => false;
        public void SetColor(Color value) { }
        public void Setup(string title, object data, string materialName, Color color, Action<Color> onChanged, bool useAlpha) { }
    }
#elif KK || EC || KKS
    internal class MakerColorPalette : IMaterialEditorColorPalette
    {
        private int _slot = -1;
        private string _materialName;
        private CvsColor _cvsColor;

        private CvsColor CvsColor => _cvsColor != null ? _cvsColor : (_cvsColor = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsColor/Top").GetComponent<CvsColor>());

        public void Close()
        {
            var cvs = CvsColor;
            if (cvs.isOpen)
                cvs.Close();

            _slot = -1;
            _materialName = null;
        }

        public bool IsShowing(string title, object data, string materialName)
        {
            var kind = (CvsColor.ConnectColorKind)title.GetHashCode();
            var cvs = CvsColor;

            return cvs.isOpen
                && cvs.connectColorKind == kind
                && _slot == ((ObjectData)data).Slot
                && _materialName == materialName;
        }

        public void SetColor(Color value)
        {
            CvsColor.SetColor(value);
        }

        public void Setup(string title, object data, string materialName, Color color, Action<Color> onChanged, bool useAlpha)
        {
            var kind = (CvsColor.ConnectColorKind)title.GetHashCode();
            CvsColor.Setup(title, kind, color, onChanged, useAlpha);

            _slot = ((ObjectData)data).Slot;
            _materialName = materialName;
        }
    }
#elif AI || HS2
    internal class MakerColorPalette : IMaterialEditorColorPalette
    {
        private int _slot = -1;
        private string _materialName;
        private CustomColorSet _set;

        private CustomColorSet ColorSet => _set != null ? _set : (_set = CreateCustomColorSet());

        private CustomColorCtrl ColorCtrl => Singleton<CustomBase>.Instance.customColorCtrl;

        public void Close()
        {
            ColorCtrl.Close();

            _slot = -1;
            _materialName = null;
            _set.title.text = "";
        }

        public bool IsShowing(string title, object data, string materialName)
        {
            var set = ColorSet;
            var ctrl = ColorCtrl;

            return ctrl.isOpen
                && set.title.text == title
                && ctrl.linkCustomColorSet == set
                && _slot == ((ObjectData)data).Slot
                && _materialName == materialName;
        }

        public void SetColor(Color value)
        {
            ColorCtrl.SetColor(ColorSet, value);
        }

        public void Setup(string title, object data, string materialName, Color color, Action<Color> onChanged, bool useAlpha)
        {
            var set = ColorSet;
            ColorCtrl.Setup(set, color, onChanged, useAlpha);

            set.title.text = title;

            _slot = ((ObjectData)data).Slot;
            _materialName = materialName;
        }

        private CustomColorSet CreateCustomColorSet()
        {
            var tf = UnityEngine.Object.Instantiate(GameObject.Find("SettingWindow/WinFace/F_Mole/Setting/Setting02/Scroll View/Viewport/Content/ColorSet"), Singleton<CustomBase>.Instance.transform, false);
            var set = tf.GetComponent<CustomColorSet>();

            tf.name = "MaterialEditorColorSet";
            set.title.text = "";
            set.button.onClick.ActuallyRemoveAllListeners();
            set.image.color = Color.white;
            set.actUpdateColor = null;

            return set;
        }
    }
#endif
}
