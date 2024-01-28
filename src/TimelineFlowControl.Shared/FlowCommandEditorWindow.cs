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
        private const string TitleBase = "Flow Command Editor";

        private readonly ImguiComboBox _boxCommandType = new ImguiComboBox();
        private readonly ImguiComboBox _boxConditionType = new ImguiComboBox();
        private readonly GUIContent[] _commandTypeContent = Enum.GetValues(typeof(FlowCommand.CommandType)).Cast<FlowCommand.CommandType>().Select(x => new GUIContent(x.ToString())).ToArray();
        private readonly GUIContent[] _conditionTypeContent = Enum.GetValues(typeof(FlowCommand.ConditionType)).Cast<FlowCommand.ConditionType>().Select(x => new GUIContent(x.ToString())).ToArray();
        private readonly GUILayoutOption[] _noExpandOptions = { GUILayout.ExpandWidth(false) }, _yesExpandOptions = { GUILayout.ExpandWidth(true) };

        private Vector2 _labelScrollView, _variableScrollView;
        private bool _showHelp, _anyChanged;

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
            _anyChanged = false;
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
                    Title = $"{TitleBase} - editing keyframe at {Utils.FormatTime(_selectedKeyframe.Key)}";
                }
                else
                {
                    Title = TitleBase;
                }
            }
        }

        protected override void DrawContents()
        {
            GUILayout.BeginVertical(IMGUIUtils.EmptyLayoutOptions);
            {
                GUILayout.BeginHorizontal(GUI.skin.box, IMGUIUtils.EmptyLayoutOptions);
                {
                    GUILayout.Label("Current command: ", _noExpandOptions);
                    GUILayout.TextField(_currentCommand.ToString(), GUI.skin.label, _yesExpandOptions);
                }
                GUILayout.EndHorizontal();

                DrawCommandEditor();

                DrawConditionEditor();

                GUILayout.Space(3);

                if (_showHelp)
                {
                    ShowHelpMessage();
                }
                else
                {
                    GUILayout.BeginHorizontal(IMGUIUtils.EmptyLayoutOptions);
                    {
                        DrawLabelList();

                        DrawVariableList();
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Label("You can edit this later by clicking the \"Use Current Value\" button in Keyframe editor. Changes are applied instantly as you type.", IMGUIUtils.EmptyLayoutOptions);
                }

                GUILayout.BeginHorizontal(IMGUIUtils.EmptyLayoutOptions);
                {
                    {
                        if (_showHelp) GUI.color = Color.yellow;

                        if (GUILayout.Button("Help", _noExpandOptions))
                            _showHelp = !_showHelp;

                        GUI.color = Color.white;
                    }

                    if (_originalCommand != null)
                    {
                        if (!_anyChanged) GUI.enabled = false;
                        else GUI.color = Color.yellow;

                        if (GUILayout.Button("Undo changes", _noExpandOptions))
                        {
                            _originalCommand.CopyTo(_currentCommand);
                            _anyChanged = false;
                        }

                        GUI.enabled = true;
                        GUI.color = Color.white;
                    }

                    if (GUILayout.Button("Close", IMGUIUtils.EmptyLayoutOptions))
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
            GUILayout.Label("This plugin is mainly used for making animation loops and reusing pieces of animations. This allows a short timeline track to have a much longer playback, to avoid having to copy keyframes many times over, to easily change the amount of loop-overs if the pacing is off, and more.", IMGUIUtils.EmptyLayoutOptions);
            GUILayout.Space(4);
            GUILayout.Label("You can edit existing Flow Control keyframes by selecting the keyframe and pressing the \"Use current value\" button. These keyframes are not interpolated, commands are executed as the playback cursor passes over them.", IMGUIUtils.EmptyLayoutOptions);
            GUILayout.Space(4);
            GUILayout.Label("To create a simple loop that runs for 5 repetitions, add the following commands in order: SetValueToVariable count 0, MakeLabel loop, AddValueToVariable count 1, JumpToLabel loop if count LessThan 6", IMGUIUtils.EmptyLayoutOptions);
        }

        #region Command Editor

        private readonly GUIContent _labelCommand = new GUIContent("Command type: ", "Command to be executed when timeline playback passes over this keyframe. Graph editor and other features related to interpolation have no effect on this, only keyframe position.");
        private readonly GUIContent _labelLabel = new GUIContent("Label Name: ", "Name of the label. Labels can be created with the MakeLabel command, and then jumped to with the JumpToLabel command.\nJumping to a label immediately moves playback cursor over the first MakeLabel keyframe with that label name that satisfies its condition (if MakeLabel has a condition that fails, the search continues). If no valid labels are found the jump is ignored.");
        private readonly GUIContent _labelPlaybackTime = new GUIContent("Playback time in seconds: ", "Time in seconds as seen on the left side of the main Timeline window. The value can be a variable name.");
        private readonly GUIContent _labelPlaybackTimeRelative = new GUIContent("Relative time in seconds (+/-): ", "Playback cursor will be moved to the position of the current keyframe + this relative time value. The value can be a variable name.");
        private readonly GUIContent _labelVariableValue = new GUIContent("Value: ", "Value to modify the specified variable with. Can be a number or a variable name.");
        private readonly GUIContent _labelVriableName = new GUIContent("Variable Name: ", "Name of the variable. Variables can be used in conditions and as values of most commands.\n\nVariables don't have to be defined before use, every variable has a value of 0 by default.\n\nVariable values are NOT reset when stopping playback! If you need to reset a variable, add commands at the start to set it to 0.");
        private readonly GUIContent _labelPrintText = new GUIContent("Variable name or any text: ", "Text to show in the top left corner of the game (and write to log).\nIf the text is a valid label name, the value of the variable with that name will be shown.");

        private void DrawCommandEditor()
        {
            GUILayout.BeginVertical(GUI.skin.box, IMGUIUtils.EmptyLayoutOptions);
            {
                GUILayout.BeginHorizontal(IMGUIUtils.EmptyLayoutOptions);
                GUILayout.Label(_labelCommand, _noExpandOptions);
                _currentCommand.Command = (FlowCommand.CommandType)_boxCommandType.Show((int)_currentCommand.Command, _commandTypeContent, (int)WindowRect.yMax);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(IMGUIUtils.EmptyLayoutOptions);
                switch (_currentCommand.Command)
                {
                    case FlowCommand.CommandType.None:
                        GUILayout.Label("This does nothing.", IMGUIUtils.EmptyLayoutOptions);
                        break;

                    case FlowCommand.CommandType.MakeLabel:
                    case FlowCommand.CommandType.JumpToLabel:
                        GUI.color = FlowCommand.IsValidLabelName(_currentCommand.Param1) ? Color.white : Color.red;
                        GUILayout.Label(_labelLabel, _noExpandOptions);
                        GUI.color = Color.white;
                        var labelname = GUILayout.TextField(_currentCommand.Param1, _yesExpandOptions);
                        _currentCommand.Param1 = MakeLabelNameSafe(labelname, "Label");
                        break;

                    case FlowCommand.CommandType.JumpToTimeAbsolute:
                        GUI.color = IsValidNumberParam(_currentCommand.Param1) ? Color.white : Color.red;

                        GUILayout.Label(_labelPlaybackTime, _noExpandOptions);
                        GUI.color = Color.white;
                        _currentCommand.Param1 = GUILayout.TextField(_currentCommand.Param1, _yesExpandOptions);
                        break;

                    case FlowCommand.CommandType.JumpToTimeRelative:
                        GUI.color = IsValidNumberParam(_currentCommand.Param1, true) ? Color.white : Color.red;
                        GUILayout.Label(_labelPlaybackTimeRelative, _noExpandOptions);
                        GUI.color = Color.white;
                        _currentCommand.Param1 = GUILayout.TextField(_currentCommand.Param1, _yesExpandOptions);
                        break;

                    case FlowCommand.CommandType.JumpReturn:
                        GUILayout.Label("This command will move playback back to the position of the last Jump command that was executed, and resume from there.\nFor example, this can be used to create a short animation clip that can be later used multiple times simply by jumping to its start (most likely a label).", IMGUIUtils.EmptyLayoutOptions);
                        break;

                    case FlowCommand.CommandType.SubtractValueFromVariable:
                    case FlowCommand.CommandType.AddValueToVariable:
                    case FlowCommand.CommandType.SetValueToVariable:
                        GUILayout.Label(_labelVriableName, _noExpandOptions);
                        var varname = GUILayout.TextField(_currentCommand.Param1, _yesExpandOptions);
                        _currentCommand.Param1 = MakeLabelNameSafe(varname, "Var");

                        GUI.color = IsValidNumberParam(_currentCommand.Param2, true) ? Color.white : Color.red;
                        GUILayout.Label(_labelVariableValue, _noExpandOptions);
                        GUI.color = Color.white;
                        _currentCommand.Param2 = GUILayout.TextField(_currentCommand.Param2, _yesExpandOptions);
                        break;

                    case FlowCommand.CommandType.PrintToScreen:
                        GUILayout.Label(_labelPrintText, _noExpandOptions);
                        _currentCommand.Param1 = GUILayout.TextField(_currentCommand.Param1, _yesExpandOptions);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(_currentCommand.Command), _currentCommand.Command, "Unsupported Command");
                }

                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        #endregion

        #region Condition Editor

        private readonly GUIContent _labelCondition = new GUIContent("Condition: ", "Condition attached to this command. The condition is checked every time before executing the above command during playback. If the condition ends up false, the above command won't be executed.");
        private readonly GUIContent _labelConditionNone = new GUIContent("This command will always be executed during playback.", "You can add a condition if you want this command to only work some of the time. This can be used to, for example, make a loop that runs only a couple of times.");
        private readonly GUIContent _labelVarLeftParam = new GUIContent("Left parameter: ", "Can be either a decimal number or a name of a variable.\n\nThe condition is evaluated as: 'Left Parameter' Condition 'Right Parameter'\nFor example: variableName Equals 69  - This will only run the command if value of variable 'variableName' is equal to the number 69.");
        private readonly GUIContent _labelVarRightParam = new GUIContent("Right parameter: ", "Can be either a decimal number or a name of a variable.\n\nThe condition is evaluated as: 'Left Parameter' Condition 'Right Parameter'\nFor example: variableName Equals 69  - This will only run the command if value of variable 'variableName' is equal to the number 69.");

        private void DrawConditionEditor()
        {
            GUILayout.BeginVertical(GUI.skin.box, IMGUIUtils.EmptyLayoutOptions);
            {
                GUILayout.BeginHorizontal(IMGUIUtils.EmptyLayoutOptions);
                GUILayout.Label(_labelCondition, _noExpandOptions);
                _currentCommand.Condition = (FlowCommand.ConditionType)_boxConditionType.Show((int)_currentCommand.Condition, _conditionTypeContent, (int)WindowRect.yMax);
                GUILayout.EndHorizontal();

                switch (_currentCommand.Condition)
                {
                    case FlowCommand.ConditionType.None:
                        GUILayout.Label(_labelConditionNone, IMGUIUtils.EmptyLayoutOptions);
                        break;

                    case FlowCommand.ConditionType.Equals:
                    case FlowCommand.ConditionType.NotEquals:
                    case FlowCommand.ConditionType.GreaterThan:
                    case FlowCommand.ConditionType.LessThan:
                        GUILayout.BeginHorizontal(IMGUIUtils.EmptyLayoutOptions);
                        {
                            GUI.color = IsValidNumberParam(_currentCommand.ConditionParam1, true) ? Color.white : Color.red;
                            GUILayout.Label(_labelVarLeftParam, _noExpandOptions);
                            GUI.color = Color.white;
                            _currentCommand.ConditionParam1 = GUILayout.TextField(_currentCommand.ConditionParam1, _yesExpandOptions);
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal(IMGUIUtils.EmptyLayoutOptions);
                        {
                            GUI.color = IsValidNumberParam(_currentCommand.ConditionParam2, true) ? Color.white : Color.red;
                            GUILayout.Label(_labelVarRightParam, _noExpandOptions);
                            GUI.color = Color.white;
                            _currentCommand.ConditionParam2 = GUILayout.TextField(_currentCommand.ConditionParam2, _yesExpandOptions);
                        }
                        GUILayout.EndHorizontal();
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(_currentCommand.Condition), _currentCommand.Condition, "Unsupported Condition");
                }
            }
            GUILayout.EndVertical();
        }

        #endregion

        private void DrawLabelList()
        {
            GUILayout.BeginVertical(GUI.skin.box, IMGUIUtils.EmptyLayoutOptions);
            GUILayout.Label("Time | Label name", IMGUIUtils.EmptyLayoutOptions);
            _labelScrollView = GUILayout.BeginScrollView(_labelScrollView, false, false, IMGUIUtils.EmptyLayoutOptions);
            {
                foreach (var label in FlowControlPlugin.StateMachine.GetAllLabelCommands(false).OrderBy(x => x.Key))
                    GUILayout.TextField($"{Utils.FormatTime(label.Key)} - {label.Value.Param1}", GUI.skin.label, IMGUIUtils.EmptyLayoutOptions);
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawVariableList()
        {
            GUILayout.BeginVertical(GUI.skin.box, IMGUIUtils.EmptyLayoutOptions);
            GUILayout.Label("Variable name | Value", IMGUIUtils.EmptyLayoutOptions);
            _variableScrollView = GUILayout.BeginScrollView(_variableScrollView, false, false, IMGUIUtils.EmptyLayoutOptions);
            {
                foreach (var var in FlowControlPlugin.StateMachine.GetAllVariableValues())
                    GUILayout.TextField($"{var.Key,-10} = {var.Value}", GUI.skin.label, IMGUIUtils.EmptyLayoutOptions);
            }
            GUILayout.EndScrollView();
            if (GUILayout.Button("Clear all values", IMGUIUtils.EmptyLayoutOptions))
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
