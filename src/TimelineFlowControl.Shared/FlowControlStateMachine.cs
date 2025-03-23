using System;
using System.Collections.Generic;
using System.Linq;

namespace TimelineFlowControl
{
    public class FlowControlStateMachine
    {
        private readonly Dictionary<string, float> _variableValues = new Dictionary<string, float>();

        private float _lastJumpTime;
        private float _lastPlaybackTime;

        internal void Update()
        {
            var currentTime = Timeline.Timeline.playbackTime;

            // TODO: is there a more reliable way to detect playback passing over a keyframe?
            // TODO: check - doesnt this ignore commands when the timeline loops from end to start?
            if (_lastPlaybackTime < currentTime && currentTime - _lastPlaybackTime < 0.5f)
            {
                foreach (var keyframe in FlowControlPlugin.GetAllCommands(true))
                {
                    var keyframeTime = keyframe.Key;
                    if (keyframeTime > _lastPlaybackTime && keyframeTime <= currentTime)
                        RunCommand(currentTime, keyframeTime, keyframe.Value); // TODO: check if the command modified Timeline state and act accordingly, probably run over "allCommands" again
                        
                }
            }

            _lastPlaybackTime = currentTime;
        }
        
        private void JumpToTime(float targetPlaybackTime, float commandTime)
        {
            _lastJumpTime = commandTime;
            Timeline.Timeline.Seek(targetPlaybackTime);
        }
        
        private void RunCommand(float playbackTime, float commandTime, FlowCommand command)
        {
            float deltaTime = playbackTime - commandTime;

            if (!SatisfiesCondition(command)) return;

            switch (command.Command)
            {
                case FlowCommand.CommandType.None:
                case FlowCommand.CommandType.MakeLabel:
                    break;

                case FlowCommand.CommandType.SetValueToVariable:
                    SetVariableValue(command.Param1, GetParamOrVariableValue(command.Param2));
                    break;
                case FlowCommand.CommandType.AddValueToVariable:
                    SetVariableValue(command.Param1, GetVariableValue(command.Param1) + GetParamOrVariableValue(command.Param2));
                    break;
                case FlowCommand.CommandType.SubtractValueFromVariable:
                    SetVariableValue(command.Param1, GetVariableValue(command.Param1) - GetParamOrVariableValue(command.Param2));
                    break;
                
                case FlowCommand.CommandType.PrintToScreen:
                    FlowControlPlugin.Logger.LogMessage(FlowCommand.IsValidLabelName(command.Param1) ? $"{command.Param1} = {GetVariableValue(command.Param1)}" : command.Param1);
                    break;
                
                // These FlowCommands alter the Timeline state, so lets handle these specially and carefully
                // TODO: make sure we dont skip any FlowCommands when jumping
                // TODO: make sure we keep "time fluid" by accounting for delta time
                
                case FlowCommand.CommandType.JumpToLabel:
                    var labelName = command.Param1;
                    if (string.IsNullOrEmpty(labelName)) break;

                    var label = GetAllLabelCommands(true).Where(x => x.Value.Param1 == labelName).FirstOrDefault(x => SatisfiesCondition(x.Value));
                    if (label.Value != null) JumpToTime(label.Key + deltaTime, commandTime); // Take "deltaTime" into account
                    break;
				
                case FlowCommand.CommandType.JumpToTimeAbsolute:
                    JumpToTime(GetParamOrVariableValue(command.Param1) + deltaTime, commandTime); // Take "deltaTime" into account
                    break;
				
                case FlowCommand.CommandType.JumpToTimeRelative:
                    JumpToTime(commandTime + GetParamOrVariableValue(command.Param1) + deltaTime, commandTime); // Take "deltaTime" into account
                    break;
				
                case FlowCommand.CommandType.JumpReturn:
                	JumpToTime(_lastJumpTime + deltaTime, commandTime); // Take "deltaTime" into account
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(command.Command), command.Command, "Unsupported Command");
            }
        }

        private bool SatisfiesCondition(FlowCommand command)
        {
            if (command.Condition == FlowCommand.ConditionType.None) return true;

            var a = GetParamOrVariableValue(command.ConditionParam1);
            var b = GetParamOrVariableValue(command.ConditionParam2);

            switch (command.Condition)
            {
                case FlowCommand.ConditionType.Equals:
                    return Math.Abs(a - b) < 0.0001f;
                case FlowCommand.ConditionType.NotEquals:
                    return Math.Abs(a - b) >= 0.0001f;
                case FlowCommand.ConditionType.GreaterThan:
                    return a > b;
                case FlowCommand.ConditionType.LessThan:
                    return a < b;

                case FlowCommand.ConditionType.None:
                default:
                    throw new ArgumentOutOfRangeException(nameof(command.Condition), command.Condition, "Unsupported Condition");
            }
        }

        private static IEnumerable<KeyValuePair<float, FlowCommand>> GetAllVariableCommands(bool onlyEnabled)
        {
            return FlowControlPlugin.GetAllCommands(onlyEnabled).Where(x =>
            {
                var commandType = x.Value.Command;
                return commandType == FlowCommand.CommandType.AddValueToVariable ||
                       commandType == FlowCommand.CommandType.SetValueToVariable ||
                       commandType == FlowCommand.CommandType.SubtractValueFromVariable;
            });
        }

        private float GetParamOrVariableValue(string rawParameter)
        {
            return float.TryParse(rawParameter, out var result) ? result : GetVariableValue(rawParameter);
        }

        internal bool VariableExists(string variableName)
        {
            return GetAllVariableCommands(false).Any(x => x.Value.Param1 == variableName);
        }

        /// <summary>
        /// Find all labels and their times in the timeline. Duplicate labels can be shown multiple times, but only the first one
        /// with a condition that passes is used.
        /// </summary>
        public IEnumerable<KeyValuePair<float, FlowCommand>> GetAllLabelCommands(bool onlyEnabled)
        {
            return FlowControlPlugin.GetAllCommands(onlyEnabled).Where(x =>
            {
                var commandType = x.Value.Command;
                return commandType == FlowCommand.CommandType.MakeLabel;
            });
        }

        /// <summary>
        /// Get current values of all variables that are used in timeline Flow Command keyframes. If a variable was set before but
        /// is no longer used in any keyframe, it is not returned.
        /// </summary>
        public IEnumerable<KeyValuePair<string, float>> GetAllVariableValues()
        {
            return GetAllVariableCommands(false).Select(x => x.Value.Param1).Distinct().OrderBy(x => x).Select(x =>
            {
                _variableValues.TryGetValue(x, out var val);
                return new KeyValuePair<string, float>(x, val);
            });
        }

        /// <summary>
        /// Get current value of a given variable. If the variable was never set, or if the name is invalid (except null), 0 is returned.
        /// </summary>
        public float GetVariableValue(string variableName)
        {
            // Defaults to 0
            _variableValues.TryGetValue(variableName, out var result);
            return result;
        }

        /// <summary>
        /// Set the value of a variable. If the variable was never set before, it is created.
        /// Name must contain only letters and underscores or it may not be possible to use it.
        /// </summary>
        public void SetVariableValue(string variableName, float value)
        {
            _variableValues[variableName] = value;
        }

        /// <summary>
        /// Reset values of all variables to 0. This is done automatically when a scene is loaded.
        /// </summary>
        public void ClearVariableValues()
        {
            _variableValues.Clear();
        }
    }
}
