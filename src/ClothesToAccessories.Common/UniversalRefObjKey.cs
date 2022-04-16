using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace KK_Plugins
{
    /// <summary>
    /// ChaReference.RefObjKey alternative that's safe to use in all versions of KK.
    /// Why: Everything below CORRECT_HAND_R changed its int value in darkness vs other versions of the game, so if you
    /// compile with darkness dll you will unexpectedly get a different enum value in games without darkness.
    /// This isn't necessary in KKS, but it does work in case your code targets both KK and KKS.
    /// Only values present in all game versions are provided (the lowest common denominator being KK without Dankness).
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "UnassignedReadonlyField")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static class UniversalRefObjKey
    {
        static UniversalRefObjKey()
        {
            // Need to get values of the enum at runtime because they differ between games. Do it with reflection to save on some space
            foreach (var f in typeof(UniversalRefObjKey).GetFields(BindingFlags.Static | BindingFlags.Public))
                f.SetValue(null, Enum.Parse(typeof(ChaReference.RefObjKey), f.Name));
        }

        public static readonly ChaReference.RefObjKey HeadParent;
        public static readonly ChaReference.RefObjKey HairParent;
        public static readonly ChaReference.RefObjKey a_n_hair_pony;
        public static readonly ChaReference.RefObjKey a_n_hair_twin_L;
        public static readonly ChaReference.RefObjKey a_n_hair_twin_R;
        public static readonly ChaReference.RefObjKey a_n_hair_pin;
        public static readonly ChaReference.RefObjKey a_n_hair_pin_R;
        public static readonly ChaReference.RefObjKey a_n_headtop;
        public static readonly ChaReference.RefObjKey a_n_headflont;
        public static readonly ChaReference.RefObjKey a_n_head;
        public static readonly ChaReference.RefObjKey a_n_headside;
        public static readonly ChaReference.RefObjKey a_n_megane;
        public static readonly ChaReference.RefObjKey a_n_earrings_L;
        public static readonly ChaReference.RefObjKey a_n_earrings_R;
        public static readonly ChaReference.RefObjKey a_n_nose;
        public static readonly ChaReference.RefObjKey a_n_mouth;
        public static readonly ChaReference.RefObjKey a_n_neck;
        public static readonly ChaReference.RefObjKey a_n_bust_f;
        public static readonly ChaReference.RefObjKey a_n_bust;
        public static readonly ChaReference.RefObjKey a_n_nip_L;
        public static readonly ChaReference.RefObjKey a_n_nip_R;
        public static readonly ChaReference.RefObjKey a_n_back;
        public static readonly ChaReference.RefObjKey a_n_back_L;
        public static readonly ChaReference.RefObjKey a_n_back_R;
        public static readonly ChaReference.RefObjKey a_n_waist;
        public static readonly ChaReference.RefObjKey a_n_waist_f;
        public static readonly ChaReference.RefObjKey a_n_waist_b;
        public static readonly ChaReference.RefObjKey a_n_waist_L;
        public static readonly ChaReference.RefObjKey a_n_waist_R;
        public static readonly ChaReference.RefObjKey a_n_leg_L;
        public static readonly ChaReference.RefObjKey a_n_leg_R;
        public static readonly ChaReference.RefObjKey a_n_knee_L;
        public static readonly ChaReference.RefObjKey a_n_knee_R;
        public static readonly ChaReference.RefObjKey a_n_ankle_L;
        public static readonly ChaReference.RefObjKey a_n_ankle_R;
        public static readonly ChaReference.RefObjKey a_n_heel_L;
        public static readonly ChaReference.RefObjKey a_n_heel_R;
        public static readonly ChaReference.RefObjKey a_n_shoulder_L;
        public static readonly ChaReference.RefObjKey a_n_shoulder_R;
        public static readonly ChaReference.RefObjKey a_n_elbo_L;
        public static readonly ChaReference.RefObjKey a_n_elbo_R;
        public static readonly ChaReference.RefObjKey a_n_arm_L;
        public static readonly ChaReference.RefObjKey a_n_arm_R;
        public static readonly ChaReference.RefObjKey a_n_wrist_L;
        public static readonly ChaReference.RefObjKey a_n_wrist_R;
        public static readonly ChaReference.RefObjKey a_n_hand_L;
        public static readonly ChaReference.RefObjKey a_n_hand_R;
        public static readonly ChaReference.RefObjKey a_n_ind_L;
        public static readonly ChaReference.RefObjKey a_n_ind_R;
        public static readonly ChaReference.RefObjKey a_n_mid_L;
        public static readonly ChaReference.RefObjKey a_n_mid_R;
        public static readonly ChaReference.RefObjKey a_n_ring_L;
        public static readonly ChaReference.RefObjKey a_n_ring_R;
        public static readonly ChaReference.RefObjKey a_n_dan;
        public static readonly ChaReference.RefObjKey a_n_kokan;
        public static readonly ChaReference.RefObjKey a_n_ana;
        public static readonly ChaReference.RefObjKey k_f_handL_00;
        public static readonly ChaReference.RefObjKey k_f_handR_00;
        public static readonly ChaReference.RefObjKey k_f_shoulderL_00;
        public static readonly ChaReference.RefObjKey k_f_shoulderR_00;
        public static readonly ChaReference.RefObjKey ObjEyeline;
        public static readonly ChaReference.RefObjKey ObjEyelineLow;
        public static readonly ChaReference.RefObjKey ObjEyebrow;
        public static readonly ChaReference.RefObjKey ObjNoseline;
        public static readonly ChaReference.RefObjKey ObjEyeL;
        public static readonly ChaReference.RefObjKey ObjEyeR;
        public static readonly ChaReference.RefObjKey ObjEyeWL;
        public static readonly ChaReference.RefObjKey ObjEyeWR;
        public static readonly ChaReference.RefObjKey ObjFace;
        public static readonly ChaReference.RefObjKey ObjDoubleTooth;
        public static readonly ChaReference.RefObjKey ObjBody;
        public static readonly ChaReference.RefObjKey ObjNip;
        public static readonly ChaReference.RefObjKey N_FaceSpecial;
        public static readonly ChaReference.RefObjKey CORRECT_ARM_L;
        public static readonly ChaReference.RefObjKey CORRECT_ARM_R;
        public static readonly ChaReference.RefObjKey CORRECT_HAND_L;
        public static readonly ChaReference.RefObjKey CORRECT_HAND_R;
        public static readonly ChaReference.RefObjKey S_ANA;
        public static readonly ChaReference.RefObjKey S_TongueF;
        public static readonly ChaReference.RefObjKey S_TongueB;
        public static readonly ChaReference.RefObjKey S_Son;
        public static readonly ChaReference.RefObjKey S_SimpleTop;
        public static readonly ChaReference.RefObjKey S_SimpleBody;
        public static readonly ChaReference.RefObjKey S_SimpleTongue;
        public static readonly ChaReference.RefObjKey S_MNPA;
        public static readonly ChaReference.RefObjKey S_MNPB;
        public static readonly ChaReference.RefObjKey S_MOZ_ALL;
        public static readonly ChaReference.RefObjKey S_GOMU;
        public static readonly ChaReference.RefObjKey S_CTOP_T_DEF;
        public static readonly ChaReference.RefObjKey S_CTOP_T_NUGE;
        public static readonly ChaReference.RefObjKey S_CTOP_B_DEF;
        public static readonly ChaReference.RefObjKey S_CTOP_B_NUGE;
        public static readonly ChaReference.RefObjKey S_CBOT_T_DEF;
        public static readonly ChaReference.RefObjKey S_CBOT_T_NUGE;
        public static readonly ChaReference.RefObjKey S_CBOT_B_DEF;
        public static readonly ChaReference.RefObjKey S_CBOT_B_NUGE;
        public static readonly ChaReference.RefObjKey S_UWT_T_DEF;
        public static readonly ChaReference.RefObjKey S_UWT_T_NUGE;
        public static readonly ChaReference.RefObjKey S_UWT_B_DEF;
        public static readonly ChaReference.RefObjKey S_UWT_B_NUGE;
        public static readonly ChaReference.RefObjKey S_UWB_T_DEF;
        public static readonly ChaReference.RefObjKey S_UWB_T_NUGE;
        public static readonly ChaReference.RefObjKey S_UWB_B_DEF;
        public static readonly ChaReference.RefObjKey S_UWB_B_NUGE;
        public static readonly ChaReference.RefObjKey S_UWB_B_NUGE2;
        public static readonly ChaReference.RefObjKey S_PANST_DEF;
        public static readonly ChaReference.RefObjKey S_PANST_NUGE;
        public static readonly ChaReference.RefObjKey S_TPARTS_00_DEF;
        public static readonly ChaReference.RefObjKey S_TPARTS_00_NUGE;
        public static readonly ChaReference.RefObjKey S_TPARTS_01_DEF;
        public static readonly ChaReference.RefObjKey S_TPARTS_01_NUGE;
        public static readonly ChaReference.RefObjKey S_TPARTS_02_DEF;
        public static readonly ChaReference.RefObjKey S_TPARTS_02_NUGE;
        public static readonly ChaReference.RefObjKey ObjBraDef;
        public static readonly ChaReference.RefObjKey ObjBraNuge;
        public static readonly ChaReference.RefObjKey ObjInnerDef;
        public static readonly ChaReference.RefObjKey ObjInnerNuge;
        public static readonly ChaReference.RefObjKey S_TEARS_01;
        public static readonly ChaReference.RefObjKey S_TEARS_02;
        public static readonly ChaReference.RefObjKey S_TEARS_03;
        public static readonly ChaReference.RefObjKey N_EyeBase;
        public static readonly ChaReference.RefObjKey N_Hitomi;
        public static readonly ChaReference.RefObjKey N_Gag00;
        public static readonly ChaReference.RefObjKey N_Gag01;
        public static readonly ChaReference.RefObjKey N_Gag02;
        public static readonly ChaReference.RefObjKey DB_SKIRT_TOP;
        public static readonly ChaReference.RefObjKey DB_SKIRT_TOPA;
        public static readonly ChaReference.RefObjKey DB_SKIRT_TOPB;
        public static readonly ChaReference.RefObjKey DB_SKIRT_BOT;
        public static readonly ChaReference.RefObjKey F_ADJUSTWIDTHSCALE;
        public static readonly ChaReference.RefObjKey A_ROOTBONE;
        public static readonly ChaReference.RefObjKey BUSTUP_TARGET;
        public static readonly ChaReference.RefObjKey NECK_LOOK_TARGET;
    }
}
