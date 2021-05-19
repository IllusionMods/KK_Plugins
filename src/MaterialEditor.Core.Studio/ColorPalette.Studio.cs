using MaterialEditorAPI;
using System;
using UnityEngine;

namespace KK_Plugins.MaterialEditor
{
#if PH
    internal class StudioColorPalette : IMaterialEditorColorPalette
    {
        public void Close() { }
        public bool IsShowing(string title, object data, string materialName) => false;
        public void SetColor(Color value) { }
        public void Setup(string title, object data, string materialName, Color color, Action<Color> onChanged, bool useAlpha) { }
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
                && _slot == ((ObjectData)data).Slot
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

            _slot = ((ObjectData)data).Slot;
            _materialName = materialName;
            _title = title;
            _onChanged = onChanged;
            _useAlpha = useAlpha;
        }
    }
#endif
}
