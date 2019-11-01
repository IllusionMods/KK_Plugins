using BepInEx;
using KKAPI;
using System.Collections.Generic;

namespace KK_Plugins
{
    [BepInDependency(KoikatuAPI.GUID)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class KK_Colliders : BaseUnityPlugin
    {
        private const string RootBoneName = "cf_J_Root";
        private static readonly HashSet<string> ArmBoneNames = new HashSet<string>() { "cf_J_ArmLow02_s_L", "cf_J_ArmLow02_s_R", "cf_J_ArmUp02_s_L", "cf_J_ArmUp02_s_R", "cf_J_Hand_s_L", "cf_J_Hand_s_R" };
        private static readonly HashSet<string> TitComments = new HashSet<string>() { "Mune_L", "Mune_R" };
    }
}