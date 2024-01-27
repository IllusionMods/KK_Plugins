using System;
using System.Collections.Generic;
using System.Linq;
using KKAPI.Utilities;
using UnityEngine;
using Keyframe = Timeline.Keyframe;

namespace TimelineFlowControl
{
    public sealed class FlowCommandEditorWindow : ImguiWindow<FlowCommandEditorWindow>
    {
        private const string TitleBase = "Flow Command Interpolable Editor";

        private readonly ImguiComboBox _boxCommandType = new ImguiComboBox();
        private readonly ImguiComboBox _boxConditionType = new ImguiComboBox();
        private readonly GUIContent[] _commandTypeContent = Enum.GetValues(typeof(FlowCommand.CommandType)).Cast<FlowCommand.CommandType>().Select(x => new GUIContent(x.ToString())).ToArray();
        private readonly GUIContent[] _conditionTypeContent = Enum.GetValues(typeof(FlowCommand.ConditionType)).Cast<FlowCommand.ConditionType>().Select(x => new GUIContent(x.ToString())).ToArray();

        private Vector2 _labelScrollView, _variableScrollView;
        private bool _showHelp;

        private FlowCommand _currentCommand, _originalCommand;

        private KeyValuePair<float, Keyframe> _selectedKeyframe;

        private void Awake()
        {
            enabled = false;
            Title = TitleBase;
        }

        protected override Rect GetDefaultWindowRect(Rect screenRect)
        {
            var timelineWindowRect = Utils.GetScreenCoordinates(Timeline.Timeline.MainWindowRectTransform);
            const int width = 400;
            const int height = 400;
            return new Rect(Mathf.RoundToInt(timelineWindowRect.xMax - timelineWindowRect.width / 2 - width / 2f),
                            Mathf.RoundToInt(timelineWindowRect.yMax - timelineWindowRect.height / 2 - height / 2f),
                            width, height);
        }

        public void EditCommand(FlowCommand command, FlowCommand originalCommand)
        {
            _currentCommand = command ?? throw new ArgumentNullException(nameof(command));
            _originalCommand = originalCommand;
            _selectedKeyframe = new KeyValuePair<float, Keyframe>();
            enabled = true;
        }

        private void Update()
        {
            if (_currentCommand == null)
            {
                enabled = false;
            }
            else if (_selectedKeyframe.Value?.value != _currentCommand)
            {
                var pair = FlowControlPlugin.GetAllKeyframes(false).FirstOrDefault(x => x.Value?.value == _currentCommand);
                if (pair.Value != null)
                {
                    _selectedKeyframe = pair;
                    Title = $"{TitleBase} - Keyframe at {Utils.FormatTime(_selectedKeyframe.Key)}";
                }
                else
                {
                    Title = TitleBase;
                }
            }
        }

        protected override void DrawContents()
        {
            GUILayout.BeginVertical();
            {
                DrawCommandEditor();

                DrawConditionEditor();

                //GUILayout.Space(3);

                GUILayout.TextField(_currentCommand.ToString(), GUI.skin.box);

                //GUILayout.Space(3);

                GUILayout.BeginHorizontal();
                {
                    if (_showHelp)
                    {
                        ShowHelpMessage();
                    }
                    else
                    {
                        DrawLabelList();

                        DrawVariableList();
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.Label("You can edit this later by clicking the \"Use Current Value\" button in Keyframe editor. Changes are applied instantly as you type.");

                GUILayout.BeginHorizontal();
                {
                    if (_showHelp) GUI.color = Color.yellow;
                    if (GUILayout.Button("Help"))
                        _showHelp = !_showHelp;
                    GUI.color = Color.white;

                    if (_originalCommand != null && GUILayout.Button("Undo changes"))
                        _originalCommand.CopyTo(_currentCommand);

                    if (GUILayout.Button("Close"))
                        enabled = false;
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            _boxCommandType.DrawDropdownIfOpen();
            _boxConditionType.DrawDropdownIfOpen();
        }

        private void ShowHelpMessage()
        {
            GUILayout.Label("This plugin is mainly used for making animation loops and reusing pieces of animations. This allows a short timeline track to have a much longer playback, to avoid having to copy keyframes many times over, to easily change the amount of loop-overs if the pacing is off, and more.");
            GUILayout.Space(4);
            GUILayout.Label("You can edit existing Flow Control keyframes by selecting the keyframe and pressing the \"Use current value\" button. These keyframes are not interpolated, commands are executed as the playback cursor passes over them.");
            GUILayout.Space(4);
            GUILayout.Label("To create a simple loop that runs for 5 repetitions, add the following commands in order: SetValueToVariable count 0, MakeLabel loop, AddValueToVariable count 1, JumpToLabel loop if count LessThan 6");
        }

        private void DrawCommandEditor()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(
                    new GUIContent("Command to execute: ",
                                   "Command to be executed when timeline playback passes over this keyframe. Graph editor and other features related to interpolation have no effect on this, only keyframe position."),
                    GUILayout.ExpandWidth(false));
                _currentCommand.Command = (FlowCommand.CommandType)_boxCommandType.Show((int)_currentCommand.Command, _commandTypeContent, (int)WindowRect.yMax);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                switch (_currentCommand.Command)
                {
                    case FlowCommand.CommandType.None:
                        GUILayout.Label("This does nothing.");
                        break;

                    case FlowCommand.CommandType.MakeLabel:
                    case FlowCommand.CommandType.JumpToLabel:
                        GUI.color = FlowCommand.IsValidLabelName(_currentCommand.Param1) ? Color.white : Color.red;
                        GUILayout.Label(
                            new GUIContent("Label Name: ",
                                           "Name of the label. Labels can be created with the MakeLabel command, and then jumped to with the JumpToLabel command. Jumping to a label immediately moves playback cursor over the MakeLabel keyframe with that label name."),
                            GUILayout.ExpandWidth(false));
                        GUI.color = Color.white;
                        var labelname = GUILayout.TextField(_currentCommand.Param1, GUILayout.ExpandWidth(true));
                        _currentCommand.Param1 = MakeLabelNameSafe(labelname, "Label");
                        break;

                    case FlowCommand.CommandType.JumpToTimeAbsolute:
                        GUI.color = IsValidNumberParam(_currentCommand.Param1) ? Color.white : Color.red;
                        GUILayout.Label(new GUIContent("Playback time in seconds: ", "Time in seconds as seen on the left side of the main Timeline window. The value can be a variable name."),
                                        GUILayout.ExpandWidth(false));
                        GUI.color = Color.white;
                        _currentCommand.Param1 = GUILayout.TextField(_currentCommand.Param1, GUILayout.ExpandWidth(true));
                        break;

                    case FlowCommand.CommandType.JumpToTimeRelative:
                        GUI.color = IsValidNumberParam(_currentCommand.Param1, true) ? Color.white : Color.red;
                        GUILayout.Label(
                            new GUIContent("Relative time in seconds (+/-): ",
                                           "Playback cursor will be moved to the position of the current keyframe + this relative time value. The value can be a variable name."), GUILayout.ExpandWidth(false));
                        GUI.color = Color.white;
                        _currentCommand.Param1 = GUILayout.TextField(_currentCommand.Param1, GUILayout.ExpandWidth(true));
                        break;

                    case FlowCommand.CommandType.JumpReturn:
                        GUILayout.Label(
                            "This command will move playback back to the position of the last Jump command that was executed, and resume from there.\nFor example, this can be used to create a short animation clip that can be later used multiple times simply by jumping to its start (most likely a label).");
                        break;

                    case FlowCommand.CommandType.SubtractValueFromVariable:
                    case FlowCommand.CommandType.AddValueToVariable:
                    case FlowCommand.CommandType.SetValueToVariable:
                        GUILayout.Label(
                            new GUIContent("Variable Name: ",
                                           "Name of the variable. Variables can be used in conditions and as values of most commands.\n\nVariables don't have to be defined before use, every variable has a value of 0 by default.\n\nVariable values are NOT reset when stopping playback! If you need to reset a variable, add commands at the start to set it to 0."),
                            GUILayout.ExpandWidth(false));
                        var varname = GUILayout.TextField(_currentCommand.Param1, GUILayout.ExpandWidth(true));
                        _currentCommand.Param1 = MakeLabelNameSafe(varname, "Var");

                        GUI.color = IsValidNumberParam(_currentCommand.Param2, true) ? Color.white : Color.red;
                        GUILayout.Label(new GUIContent("Value: ", "Value to modify the specified variable with. Can be a number or a variable name."), GUILayout.ExpandWidth(false));
                        GUI.color = Color.white;
                        _currentCommand.Param2 = GUILayout.TextField(_currentCommand.Param2, GUILayout.ExpandWidth(true));
                        break;

                    case FlowCommand.CommandType.PrintToScreen:
                        GUILayout.Label(new GUIContent("Variable name or any text: ",
                                                       "Text to show in the top left corner of the game (and write to log).\nIf the text is a valid label name, the value of the variable with that name will be shown."),
                                        GUILayout.ExpandWidth(false));
                        _currentCommand.Param1 = GUILayout.TextField(_currentCommand.Param1, GUILayout.ExpandWidth(true));
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(_currentCommand.Command), _currentCommand.Command, "Unsupported Command");
                }

                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        private void DrawConditionEditor()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(
                    new GUIContent("Condition: ",
                                   "Condition attached to this command. The condition is checked every time before executing the above command during playback. If the condition ends up false, the above command won't be executed."),
                    GUILayout.ExpandWidth(false));
                _currentCommand.Condition = (FlowCommand.ConditionType)_boxConditionType.Show((int)_currentCommand.Condition, _conditionTypeContent, (int)WindowRect.yMax);
                GUILayout.EndHorizontal();

                switch (_currentCommand.Condition)
                {
                    case FlowCommand.ConditionType.None:
                        GUILayout.Label("This command will always be executed during playback.");
                        break;

                    case FlowCommand.ConditionType.Equals:
                    case FlowCommand.ConditionType.NotEquals:
                    case FlowCommand.ConditionType.GreaterThan:
                    case FlowCommand.ConditionType.LessThan:
                        GUILayout.BeginHorizontal();
                        {
                            GUI.color = IsValidNumberParam(_currentCommand.ConditionParam1, true) ? Color.white : Color.red;
                            GUILayout.Label("Left variable or number: ", GUILayout.ExpandWidth(false));
                            GUI.color = Color.white;
                            _currentCommand.ConditionParam1 = GUILayout.TextField(_currentCommand.ConditionParam1, GUILayout.ExpandWidth(true));
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        {
                            GUI.color = IsValidNumberParam(_currentCommand.ConditionParam2, true) ? Color.white : Color.red;
                            GUILayout.Label("Right variable or number: ", GUILayout.ExpandWidth(false));
                            GUI.color = Color.white;
                            _currentCommand.ConditionParam2 = GUILayout.TextField(_currentCommand.ConditionParam2, GUILayout.ExpandWidth(true));
                        }
                        GUILayout.EndHorizontal();
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(_currentCommand.Condition), _currentCommand.Condition, "Unsupported Condition");
                }
            }
            GUILayout.EndVertical();
        }

        private void DrawLabelList()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Time | Label name");
            _labelScrollView = GUILayout.BeginScrollView(_labelScrollView, false, false);
            {
                foreach (var label in FlowControlPlugin.StateMachine.GetAllLabelCommands(false).OrderBy(x => x.Key))
                    GUILayout.TextField($"{Utils.FormatTime(label.Key)} - {label.Value.Param1}", GUI.skin.label);
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawVariableList()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Variable name | Value");
            _variableScrollView = GUILayout.BeginScrollView(_variableScrollView, false, false);
            {
                foreach (var var in FlowControlPlugin.StateMachine.GetAllVariableValues())
                    GUILayout.TextField($"{var.Key,-10} = {var.Value}", GUI.skin.label);
            }
            GUILayout.EndScrollView();
            if (GUILayout.Button("Clear all values"))
                FlowControlPlugin.StateMachine.ClearVariableValues();
            GUILayout.EndVertical();
        }

        private static bool IsValidNumberParam(string param, bool allowNegative = false)
        {
            if (string.IsNullOrEmpty(param)) return false;
            if (param[0] == '-' && !allowNegative) return false;

            return float.TryParse(param, out _) || FlowControlPlugin.StateMachine.VariableExists(param);
        }

        private static string MakeLabelNameSafe(string labelname, string defaultValue)
        {
            var result = new string(labelname.Where(c => char.IsLetter(c) || c == '_').ToArray());
            if (result.Length == 0)
                result = defaultValue;
            return result;
        }
    }
}
