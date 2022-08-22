using MaterialEditorAPI;
using System;
using UnityEngine;
#if PH
using Studio;
#endif

namespace KK_Plugins.MaterialEditor
{
#if PH
    internal class StudioColorPalette : IMaterialEditorColorPalette
    {
        private int _slot = -1;
        private string _materialName;
        private UI_ColorInfo.UpdateColor _onChanged = null;

        private Studio.Studio Studio => Singleton<Studio.Studio>.Instance;

        public void Close()
        {
            Studio.colorPaletteCtrl.visible = false;
            Studio.colorMenu.winTitle.text = "Color"; //Set as default name.
        }

        public bool IsShowing(string title, object data, string materialName)
        {
            return
                   Studio.colorMenu.winTitle.text == title
                   && Studio.colorPaletteCtrl.visible == true
                   && _slot == DataToSlot(data)
                   && _materialName == materialName;
        }

        public void SetColor(Color value)
        {
            Studio.colorMenu.SetColor(value, UI_ColorInfo.ControlType.PresetsSample);
        }

        public void Setup(string title, object data, string materialName, Color color, Action<Color> onChanged, bool useAlpha)
        {
            if (Studio.colorMenu.updateColorFunc == _onChanged)
                Studio.colorPaletteCtrl.visible = !Studio.colorPaletteCtrl.visible;
            else
                Studio.colorPaletteCtrl.visible = true;
            if (Studio.colorPaletteCtrl.visible)
            {
                Studio.colorMenu.winTitle.text = title;
                Studio.colorMenu.updateColorFunc = _onChanged;
                Studio.colorMenu.updateColorFunc = new UI_ColorInfo.UpdateColor(onChanged);
                Studio.colorMenu.SetColor(color, UI_ColorInfo.ControlType.PresetsSample);
            }
            _slot = DataToSlot(data);
            _materialName = materialName;
        }

        private static int DataToSlot(object data)
        {
            if (data is ObjectData objectData) //Character
                return objectData.Slot;
            else //Item
                return (int)data;
        }
    }
#else
    internal class StudioColorPalette : IMaterialEditorColorPalette
    {
        private int _slot = -1;
        private string _materialName;
        private string _title;
        private Action<Color> _onChanged;
        private bool _useAlpha;

        private Studio.Studio Studio => Singleton<Studio.Studio>.Instance;

        public void Close()
        {
            Studio.colorPalette.visible = false;
        }

        public bool IsShowing(string title, object data, string materialName)
        {
            return Studio.colorPalette.Check(title)
                   && _slot == DataToSlot(data)
                   && _materialName == materialName;
        }

        public void SetColor(Color value)
        {
            if (_title == null || _onChanged == null)
                return;

            Studio.colorPalette.Setup(_title, value, _onChanged, _useAlpha);
        }

        public void Setup(string title, object data, string materialName, Color color, Action<Color> onChanged, bool useAlpha)
        {
            Studio.colorPalette.Setup(title, color, onChanged, useAlpha);

            _slot = DataToSlot(data);

            _materialName = materialName;
            _title = title;
            _onChanged = onChanged;
            _useAlpha = useAlpha;
        }

        private static int DataToSlot(object data)
        {
            if (data is ObjectData objectData) //Character
                return objectData.Slot;
            else //Item
                return (int)data;
        }
    }
#endif
}
