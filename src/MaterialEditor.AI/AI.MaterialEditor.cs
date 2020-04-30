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
}
