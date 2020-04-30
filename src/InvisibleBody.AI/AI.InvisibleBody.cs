using BepInEx;
using System.Collections.Generic;

namespace KK_Plugins
{
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    [BepInDependency(ExtensibleSaveFormat.ExtendedSave.GUID)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class InvisibleBody : BaseUnityPlugin
    {
        //Items attached to characters during specific animations. No clear way to filter these except on an individual basis.
        private static readonly HashSet<string> RendererBlacklist = new HashSet<string>() { "o_ai_mi_pod01h_light00green", "o_ai_mi_pod01h_light00red", "o_ai_mi_pod01h_light00", "o_ai_mi_pod01h_00", "o_ai_mi_pod01k_light00green", "o_ai_mi_pod01k_light00red", "o_ai_mi_pod01k_light00", "o_ai_mi_pod01k_00", "o_ai_mi_pod01s_light01green", "o_ai_mi_pod01s_light01red", "o_ai_mi_pod01s_00", "o_ai_mi_pod01u_00", "anim_ai_mi_pod01_00stu", "o_ai_mi_kasa01_01", "p_ai_mi_kasa00_01", "p_ai_mi_obon00_01", "o_ai_mi_branko00_l", "o_ai_mi_branko00_m", "o_ai_mi_branko00_s", "p_ai_hi_sberi00_00stu", "o_ai_mi_catslang", "Water_jouro", "o_ai_mi_buiya02_00", "p_ai_mi_jouro00_01stu", "p_ai_mi_houki00_01stu", "p_ai_hi_kabeana00_00stu", "p_ai_mi_pickaxe00_00stu", "p_ai_mi_pickaxe01_00stu", "o_ai_mi_ami00a_00", "o_ai_mi_ami00d_00", "p_ai_mi_sao00", "o_ai_hi_idopump02_01", "o_ai_hi_idopump03_01", "o_ai_hi_idopump01_01", "o_ai_hi_idopump00_01" };
    }
}
