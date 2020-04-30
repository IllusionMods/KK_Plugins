using BepInEx.Harmony;
using HarmonyLib;
using Studio;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.Events;

namespace KK_Plugins
{
    public partial class PoseFolders
    {
        public const string GUID = "com.deathweasel.bepinex.posefolders";
        public const string PluginName = "Pose Folders";
        public const string Version = "1.0";

        private const string USERDATA = "userdata";
        private const string DEFAULT_ROOT = "studio/pose";
        private static readonly int UserdataRoot = new DirectoryInfo(USERDATA).FullName.Length + 1;  //+1 for slash
        private static DirectoryInfo CurrentDirectory = new DirectoryInfo(USERDATA + "/" + DEFAULT_ROOT);
        private static readonly int DefaultRootLength = CurrentDirectory.FullName.Length;

        private static GameObject v_prefabNode;
        private static Transform v_transformRoot;

        internal void Main()
        {
            if (!CurrentDirectory.Exists) CurrentDirectory.Create();
            HarmonyWrapper.PatchAll(typeof(Hooks));
        }

        private static string GetFolder()
        {
            if (!CurrentDirectory.Exists)
                CurrentDirectory = new DirectoryInfo(USERDATA + "/" + DEFAULT_ROOT);
            return CurrentDirectory.FullName.Substring(UserdataRoot);
        }

        private static IEnumerable<CodeInstruction> ReplacePath(IEnumerable<CodeInstruction> _instructions)
        {
            var instructions = new List<CodeInstruction>(_instructions);

            for (var i = 0; i < instructions.Count; i++)
            {
                var inst = instructions[i];
                if (inst.opcode == OpCodes.Ldstr && inst.operand?.ToString() == DEFAULT_ROOT)
                {
                    inst.opcode = OpCodes.Call;
                    inst.operand = AccessTools.Method(typeof(PoseFolders), nameof(GetFolder));
                    break;
                }
            }

            return instructions;
        }

        private static Transform AddListButton(string text, UnityAction callback)
        {
            var prefabNode = Instantiate(v_prefabNode);
            prefabNode.transform.SetParent(v_transformRoot, false);
            var component = prefabNode.GetComponent<StudioNode>();
            component.active = true;
            component.addOnClick = callback;
            component.text = text;

            return prefabNode.transform;
        }
    }
}
