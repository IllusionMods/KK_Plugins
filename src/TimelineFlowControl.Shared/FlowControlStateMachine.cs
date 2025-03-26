using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TimelineFlowControl
{
    public class FlowControlStateMachine
    {
        private const int _maxJumpDepth = 128;
        private readonly Dictionary<string, float> _variableValues = new Dictionary<string, float>();

        private float _lastJumpTime;
        private float _lastJumpTargetTime;
        private float _lastPlaybackTime;
        private bool _isCurrentlyRecursivelyJumping = false;

        internal void Update()
        {
            var currentTime = Timeline.Timeline.playbackTime;
            
            // TODO: Ask nicely at HSPlugins for Timeline hooks - when scrubbing the timeline + when it overflows/loops?
            // TODO: is there a more reliable way to detect playback passing over a keyframe? ^^ this would make that easier

            // SANITY CHECK - VV doesnt this ignore commands when the timeline loops from end to start? for example with duration 1.0s and 10fps (0.1s frametime) 0.97 loops to --> 0.07
            // therefore on next Update() _lastPlaybackTime will be larger than currentTime and 0.0 to 0.07 will get ignored
            // ^^ Confirmed this happens ... it also does not play nicely with >1.0 Timescale, both could be fixed by the hooks

            if (_lastPlaybackTime < currentTime && currentTime - _lastPlaybackTime < 0.5f)
            {
                foreach (var keyframe in FlowControlPlugin.GetAllCommands(true))
                {
                    var keyframeTime = keyframe.Key;
                    if (keyframeTime > _lastPlaybackTime && keyframeTime <= currentTime)
                    {
                        if (RunCommand(currentTime, keyframeTime, keyframe.Value))
                        {
                            // There was a jump, so Timeline changed and our time is no longer valid
                            currentTime = Timeline.Timeline.playbackTime;
                            break;
                        }
                    }
                }
            }

            _lastPlaybackTime = currentTime;
            // TODO: _lastPlaybackTime is not updated when scrubbing the Timeline if Timeline is paused
            // so if you scrub from a time before a jump command to a time after the jump command, it will still run through it
            // since _lastPlaybackTime is updated at the very end here, could be fixed by the hooks aswell
        }

        private bool JumpToTime(float targetTime, float playbackTime, float commandTime) // Returns true when Timeline state was altered, which is always since we are jumping
        {
            if (_isCurrentlyRecursivelyJumping) // Ignore deltaTime, since this is expected to be recursively called from within this function, which is currently handling the compensation
            {
                _lastJumpTime = commandTime;
                _lastJumpTargetTime = targetTime;
                return true;
            }
            
            // We want to alter the Timeline state and respect deltaTime at the same time (to keep looping/jumping actions fluid and smooth)
            // Setup variables for the jump to "target time + deltaTime", but only do this internally, not touching Timeline yet
            // Then run through FlowCommands between "target time" and "target time + deltaTime" carefully "using up" deltaTime and making sure we dont skip any!
            // Do this for all the jump commands we encounter on the way, updating the variables, rinse n repeat
            // until there are no more jump commands on our way to "final target time + remaining delta time"
            // Then just set Timeline to this final time + the remainder of deltaTime (remainingTimeForJumps) and we are done
            
            float deltaTime = playbackTime - commandTime;
            float remainingTimeForJumps = deltaTime;
            float currentTime = commandTime; // NOTE to myself: this does NOT include deltaTime
            int jumpDepthCounter = 0;
            
            _lastJumpTime = currentTime;
            _lastJumpTargetTime = targetTime;
            _isCurrentlyRecursivelyJumping = true;
            // TODO: find a way to avoid calling FlowControlPlugin.GetAllCommands() multiple times (seems like it is not very expensive though), but also without having to store the entire array/list in memory
            // Perhaps using IEnumerator directly would work
            
            while (true)
            {
                jumpDepthCounter = jumpDepthCounter + 1;
                if (jumpDepthCounter > _maxJumpDepth) // Prevent a lock up, if this happens preserve the original behaviour even though it might produce a stutter, but we wont be skipping any FlowCommands
                {
                    // NOTE: To test this, make a JumpToTimeRelative command with a really low time jumping BACK, on my machine a value of -0.001 got me somewhere around 90-110 depth at 60fps
                    _isCurrentlyRecursivelyJumping = false;
                    Timeline.Timeline.Seek(targetTime); // Set to the last jump target position, this way we have jumped ahead only some/or most percent of the deltaTime but we have ran all the FlowCommands on the way, not skipping any!
                    FlowControlPlugin.Logger.LogError("[TLFC]: Depth limit of '" + _maxJumpDepth.ToString() + "' reached, the system either cannot keep up or you are using too many FlowCommand JUMPs in a sequence VERY close to eachother");
                    return true;
                }
                
                bool thereWasAJump = false;
                float targetTimeWithDeltaTime = targetTime + remainingTimeForJumps;
                float clampedTargetTimeWithDeltaTime = Math.Min(targetTimeWithDeltaTime, Timeline.Timeline.duration); // Make sure we are not looping through keyframes that are hidden outside Timeline duration

                // Check if there are FlowCommands we would skip by just jumping with deltaTime
                foreach (var keyframe in FlowControlPlugin.GetAllCommands(true))
                {
                    float keyframeTime = keyframe.Key;
                    if (keyframeTime >= targetTime && keyframeTime <= clampedTargetTimeWithDeltaTime && keyframeTime != Timeline.Timeline.duration) // Time equal to Timeline duration would overflow/loop the Timeline, this is handled in the loop below!
                    {
                        if (RunCommand(currentTime, keyframeTime, keyframe.Value))
                        {
                            // We jumped, calculate how much time we would use up during normal Timeline playback to get to this FlowCommand, then break and start from the new time
                            float timeRequired = keyframeTime - currentTime; // Should always be >= 0.0
                            remainingTimeForJumps = remainingTimeForJumps - timeRequired;
                            if (remainingTimeForJumps < 0.0f)
                            {
                                FlowControlPlugin.Logger.LogWarning("[TLFC]: BUG?! remainingTimeForJumps is '" + remainingTimeForJumps.ToString() + "', which is less than 0.0");
                                remainingTimeForJumps = 0.0f;
                            }


                            thereWasAJump = true;
                            targetTime = _lastJumpTargetTime;
                            currentTime = targetTime;
                            break;
                        }
                    }
                }
                if (thereWasAJump) continue;


                // Check if Timeline would overflow/loop if we just jumped with deltaTime, if so, also check if there are FlowCommands we would skip
                float overflowedTargetTimeWithDeltaTime = targetTimeWithDeltaTime % Timeline.Timeline.duration; // TODO: detect if we overflow/loop the Timeline multiple times (when we have like 1 fps, which will make deltaTime huge) - Timeline hooks would work for this
                if (overflowedTargetTimeWithDeltaTime < targetTimeWithDeltaTime)
                {
                    foreach (var keyframe in FlowControlPlugin.GetAllCommands(true))
                    {
                        float keyframeTime = keyframe.Key;
                        if (keyframeTime <= overflowedTargetTimeWithDeltaTime)
                        {
                            if (RunCommand(currentTime, keyframeTime, keyframe.Value))
                            {
                                // We jumped, calculate how much time we would use up during normal Timeline playback to get to this FlowCommand, then break and start from the new time
                                float timeToEndOfTimeline = Timeline.Timeline.duration - currentTime;
                                float timeFromStartToKeyframe = keyframeTime;
                                float timeRequired = timeToEndOfTimeline + timeFromStartToKeyframe; // Should always be >= 0.0
                                remainingTimeForJumps = remainingTimeForJumps - timeRequired;
                                if (remainingTimeForJumps < 0.0f)
                                {
                                    FlowControlPlugin.Logger.LogWarning("[TLFC]: BUG?! remainingTimeForJumps is '" + remainingTimeForJumps.ToString() + "', which is less than 0.0");
                                    remainingTimeForJumps = 0.0f;
                                }

                                thereWasAJump = true;
                                targetTime = _lastJumpTargetTime;
                                currentTime = targetTime;
                                break;
                            }
                        }
                    }
                }
                if (!thereWasAJump) break; // We have ran through all the jump FlowCommands on our way which means we can safely do a final jump to targetTime + remainingTimeForJump
            }

            _isCurrentlyRecursivelyJumping = false;
            Timeline.Timeline.Seek(targetTime + remainingTimeForJumps);
            return true;
        }
        
        private bool RunCommand(float playbackTime, float commandTime, FlowCommand command) // Returns true when Timeline state was altered (like when jumping)
        {
            if (!SatisfiesCondition(command)) return false;

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
                
                //
                // These FlowCommands alter the Timeline state, so lets handle these with care and in a single function (JumpToTime) to possibly prevent bugs related to state changes
                //
                
                case FlowCommand.CommandType.JumpToLabel:
                    var labelName = command.Param1;
                    if (string.IsNullOrEmpty(labelName)) break;

                    var label = GetAllLabelCommands(true).Where(x => x.Value.Param1 == labelName).FirstOrDefault(x => SatisfiesCondition(x.Value));
                    if (label.Value != null) return JumpToTime(label.Key, playbackTime, commandTime);
                    break;
				
                case FlowCommand.CommandType.JumpToTimeAbsolute:
                    return JumpToTime(GetParamOrVariableValue(command.Param1), playbackTime, commandTime);
				
                case FlowCommand.CommandType.JumpToTimeRelative:
                    return JumpToTime(commandTime + GetParamOrVariableValue(command.Param1), playbackTime, commandTime);
				
                case FlowCommand.CommandType.JumpReturn:
                    return JumpToTime(_lastJumpTime, playbackTime, commandTime);
                
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(command.Command), command.Command, "Unsupported Command");
            }
            
            return false;
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
