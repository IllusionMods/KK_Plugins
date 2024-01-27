using System.Collections.Generic;
using System.Linq;
using System.Xml;
using BepInEx;
using BepInEx.Logging;
using KKAPI;
using KKAPI.Studio.SaveLoad;
using KKAPI.Utilities;
using Studio;
using Timeline;

namespace TimelineFlowControl
{
    [BepInPlugin(GUID, DisplayName, Version)]
    [BepInProcess(KoikatuAPI.StudioProcessName)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInDependency(Timeline.Timeline.GUID, Timeline.Timeline.Version)]
    public class FlowControlPlugin : BaseUnityPlugin
    {
        public const string GUID = "TimelineFlowControl";
        public const string DisplayName = "Timeline Flow Control Logic";
        public const string Version = "1.0";

        private const string OwnerStr = "Flow Control";
        private const string CommandId = "Command";

        internal static new ManualLogSource Logger;
        internal static FlowControlPlugin Instance;

        private static FlowCommandEditorWindow _editorWindow;
        internal static FlowCommandEditorWindow EditorWindow => _editorWindow ?? (_editorWindow = Instance.gameObject.AddComponent<FlowCommandEditorWindow>());

        private static FlowControlStateMachine _stateMachine;
        public static FlowControlStateMachine StateMachine => _stateMachine ?? (_stateMachine = new FlowControlStateMachine());

        private void Start()
        {
            Logger = base.Logger;
            Instance = this;

            StudioSaveLoadApi.SceneLoad += (sender, args) =>
            {
                if (args.Operation != SceneOperationKind.Import)
                    _stateMachine?.ClearVariableValues();
            };

            TimelineCompatibility.AddInterpolableModelStatic<FlowCommand, object>(OwnerStr, CommandId, null, "Flow command", null, null, info => true, GetCurrentValue, ReadValueFromXml, WriteValueToXml, useOciInHash: false);

            FlowCommand GetCurrentValue(ObjectCtrlInfo __, object _)
            {
                var newCommand = FlowCommand.MakeCommand();

                // If there is a selected command, we are most likely editing it, but it's safer to not reuse it and instead copy the values
                var selectedCommand = GetSelectedCommands().FirstOrDefault();
                selectedCommand.Value?.CopyTo(newCommand);

                // Open the editor window with this command selected
                EditorWindow.EditCommand(newCommand, selectedCommand.Value);

                // Return the command as it is. It's passed by reference and will be updated in the window later, with changes taking effect immediately
                return newCommand;
            }

            FlowCommand ReadValueFromXml(object _, XmlNode arg2)
            {
                var result = new FlowCommand();
                result.ReadFromXml(arg2);
                return result;
            }

            void WriteValueToXml(object _, XmlTextWriter arg2, FlowCommand flowCommand)
            {
                flowCommand.WriteToXml(arg2);
            }
        }

        private void Update()
        {
            if (_editorWindow != null && _editorWindow.enabled && !Timeline.Timeline.InterfaceVisible)
                _editorWindow.enabled = false;

            if (Timeline.Timeline.isPlaying)
                StateMachine.Update();
        }

        internal static IEnumerable<KeyValuePair<float, FlowCommand>> GetSelectedCommands()
        {
            return Timeline.Timeline.GetSelectedKeyframes()
                           .Select(x => new KeyValuePair<float, FlowCommand>(x.Key, x.Value?.value as FlowCommand))
                           .Where(x => x.Value != null);
        }

        internal static IEnumerable<KeyValuePair<float, FlowCommand>> GetAllCommands(bool onlyEnabled)
        {
            return GetAllKeyframes(onlyEnabled)
                           .Select(x => new KeyValuePair<float, FlowCommand>(x.Key, x.Value?.value as FlowCommand))
                           .Where(x => x.Value != null);
        }

        internal static IEnumerable<KeyValuePair<float, Keyframe>> GetAllKeyframes(bool onlyEnabled)
        {
            return Timeline.Timeline.GetAllInterpolables(onlyEnabled)
                           .Where(x => (!onlyEnabled || x.enabled) && x.id == CommandId && x.owner == OwnerStr)
                           .SelectMany(x => x.keyframes);
        }
    }
}
