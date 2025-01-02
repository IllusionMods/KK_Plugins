#if KK || EC || KKS
using ChaCustom;
#elif AI || HS2
using CharaCustom;
using KKAPI.Utilities;
#endif
using MaterialEditorAPI;
using System;
using UnityEngine;
using HarmonyLib;

namespace KK_Plugins.MaterialEditor
{
#if PH
    internal class MakerColorPalette : IMaterialEditorColorPalette
    {
        private int _slot = -1;
        private string _materialName;
        //Normal Maker mode
        private MoveableUI _mui;
        private MoveableUI MUI => _mui != null ? _mui : (_mui = GameObject.Find("EditScene/Canvas/MoveableRoot/ColorUI").GetComponent<MoveableUI>());
        private MoveableColorCustomUI _movcui;
        private MoveableColorCustomUI MCCUI => _movcui != null ? _movcui : (_movcui = GameObject.Find("EditScene/Canvas/MoveableRoot/ColorUI").GetComponent<MoveableColorCustomUI>());
        //H-Scene Maker mode
        private readonly H_Scene hscene = UnityEngine.Object.FindObjectOfType<H_Scene>();
        private MoveableUI _mui_h;
        private MoveableUI MUI_H => _mui_h != null ? _mui_h : (_mui_h = GameObject.Find("Left Middle Canvas/MoveableRoot/ColorUI").GetComponent<MoveableUI>());
        private MoveableColorCustomUI _movcui_h;
        private MoveableColorCustomUI MCCUI_H => _movcui_h != null ? _movcui_h : (_movcui_h = GameObject.Find("Left Middle Canvas/MoveableRoot/ColorUI").GetComponent<MoveableColorCustomUI>());

        public void Close()
        {
            var mccui_all = (hscene != null) ? MCCUI_H : MCCUI;

            if (mccui_all.isOpen)
            {
                mccui_all.Close();
            }
            _slot = -1;
            _materialName = null;
        }

        public bool IsShowing(string title, object data, string materialName)
        {
            var mccui_all = (hscene != null) ? MCCUI_H : MCCUI;
            var mui_all = (hscene != null) ? MUI_H : MUI;

            return
                mccui_all.isOpen
                && mui_all.title.text == title
                && _slot == ((ObjectData)data).Slot
                && _materialName == materialName;
        }

        public void SetColor(Color value)
        {
            var mccui_all = (hscene != null) ? MCCUI_H : MCCUI;

            mccui_all.color = value;
        }

        public void Setup(string title, object data, string materialName, Color color, Action<Color> onChanged, bool useAlpha)
        {
            var mccui_all = (hscene != null) ? MCCUI_H : MCCUI;
            var mui_all = (hscene != null) ? MUI_H : MUI;

            mui_all.SetTitle(title);
            mui_all.Open();
            mccui_all.colorPicker.Setup(color, useAlpha, onChanged);

            //* Turning off lists with Thumbnails.
            //MoveableThumbnailSelectUI[] array = UnityEngine.Object.FindObjectsOfType<MoveableThumbnailSelectUI>();
            //for (int i = 0; i < array.Length; i++)
            //{
            //    array[i].Close();
            //}

            _slot = ((ObjectData)data).Slot;
            _materialName = materialName;
        }
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

            var setup = AccessTools.Method(typeof(CvsColor), nameof(CvsColor.Setup));
            if (setup.GetParameters().Length == 5) //KK, KKS, EC
            {
                setup.Invoke(CvsColor, new object[] { title, kind, color, onChanged, useAlpha });
            }
            else if (setup.GetParameters().Length == 6) //KK Party
            {
                setup.Invoke(CvsColor, new object[] { title, kind, color, onChanged, null, useAlpha });
            }

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
