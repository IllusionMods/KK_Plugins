using BepInEx;
using HarmonyLib;
using System;
using AIChara;

namespace KK_Plugins
{
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class MaterialEditor : BaseUnityPlugin
    {
        public const string PluginNameInternal = "AI_MaterialEditor";

    }
    internal static class AccessoriesApi
    {
        private static Func<ChaControl, int, CmpAccessory> _getChaAccessoryCmp;
        private static bool _initialized;

        internal static CmpAccessory GetAccessory(this ChaControl character, int accessoryIndex)
        {
            if (!_initialized)
                Init();

            return _getChaAccessoryCmp(character, accessoryIndex);
        }

        private static void Init()
        {
            _getChaAccessoryCmp = (control, i) => control.GetAccessoryComponent(i);
            _initialized = true;
        }
    }
}
