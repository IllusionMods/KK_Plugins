using BepInEx;
using HarmonyLib;
using System.IO;

namespace KK_Plugins
{
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    public class PoseUnlocker : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.poseunlocker";
        public const string PluginName = "Pose Gender Restriction Unlocker";
        public const string PluginNameInternal = Constants.Prefix + "_PoseUnlocker";
        public const string Version = "1.0";

        internal void Main()
        {
            Harmony.CreateAndPatchAll(typeof(Hooks));
        }

        private static class Hooks
        {
            /// <summary>
            /// Same as vanilla but without the check against sex
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(Studio.PauseCtrl), nameof(Studio.PauseCtrl.CheckIdentifyingCode))]
            private static bool CheckIdentifyingCode(string _path, ref bool __result)
            {
                using (FileStream input = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (BinaryReader binaryReader = new BinaryReader(input))
                    if (string.Compare(binaryReader.ReadString(), "【pose】") != 0)
                    {
                        __result = false;
                        return false;
                    }

                __result = true;
                return false;
            }
        }
    }
}
