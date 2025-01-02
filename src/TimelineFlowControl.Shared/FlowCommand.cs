using System;
using System.Linq;
using System.Xml;

namespace TimelineFlowControl
{
    public class FlowCommand
    {
        public enum CommandType
        {
            // values must be sequential since it's used in the dropdown as array indexes
            None = 0,
            MakeLabel,
            JumpToLabel,
            JumpToTimeAbsolute,
            JumpToTimeRelative,
            JumpReturn,
            SetValueToVariable,
            AddValueToVariable,
            SubtractValueFromVariable,
            PrintToScreen
        }

        public enum ConditionType
        {
            // values must be sequential since it's used in the dropdown as array indexes
            None = 0,
            Equals,
            NotEquals,
            GreaterThan,
            LessThan
        }

        public CommandType Command;

        public ConditionType Condition;
        public string ConditionParam1 = string.Empty, ConditionParam2 = string.Empty;
        public string Param1 = string.Empty, Param2 = string.Empty;

        public static FlowCommand MakeCommand()
        {
            return new FlowCommand { Command = CommandType.MakeLabel, Param1 = "Label" };
        }

        public void WriteToXml(XmlTextWriter xmlTextWriter)
        {
            xmlTextWriter.WriteStartElement(nameof(FlowCommand));
            xmlTextWriter.WriteAttributeString(nameof(Command), Command.ToString());
            xmlTextWriter.WriteAttributeString(nameof(Param1), Param1);
            xmlTextWriter.WriteAttributeString(nameof(Param2), Param2);
            xmlTextWriter.WriteAttributeString(nameof(Condition), Condition.ToString());
            xmlTextWriter.WriteAttributeString(nameof(ConditionParam1), ConditionParam1);
            xmlTextWriter.WriteAttributeString(nameof(ConditionParam2), ConditionParam2);
            xmlTextWriter.WriteEndElement();
        }

        public void ReadFromXml(XmlNode xmlNode)
        {
            try
            {
                var attributes = xmlNode.ChildNodes.Cast<XmlNode>().FirstOrDefault(x => x.Name == nameof(FlowCommand))?.Attributes ?? throw new Exception("node not found or empty");
                Command = (CommandType)Enum.Parse(typeof(CommandType), attributes[nameof(Command)].Value);
                Param1 = attributes[nameof(Param1)].Value;
                Param2 = attributes[nameof(Param2)].Value;
                Condition = (ConditionType)Enum.Parse(typeof(ConditionType), attributes[nameof(Condition)].Value);
                ConditionParam1 = attributes[nameof(ConditionParam1)].Value;
                ConditionParam2 = attributes[nameof(ConditionParam2)].Value;
            }
            catch (Exception e)
            {
                FlowControlPlugin.Logger.LogError($"Error reading FlowCommand from XML: {e.Message}\nOffending XML: {xmlNode?.OuterXml}");
            }
        }

        public void CopyTo(FlowCommand target)
        {
            target.Command = Command;
            target.Param1 = Param1;
            target.Param2 = Param2;
            target.Condition = Condition;
            target.ConditionParam1 = ConditionParam1;
            target.ConditionParam2 = ConditionParam2;
        }

        public override string ToString()
        {
            var str = "DO (" + Command;
            if (!string.IsNullOrEmpty(Param1) && Command != CommandType.JumpReturn && Command != CommandType.None)
                str += $" {Param1}";
            if (!string.IsNullOrEmpty(Param2) && (Command == CommandType.SetValueToVariable || Command == CommandType.AddValueToVariable || Command == CommandType.SubtractValueFromVariable))
                str += $" {Param2}";
            str += ")";
            if (Condition != ConditionType.None)
                str += $" IF ({ConditionParam1} {Condition} {ConditionParam2})";
            return str;
        }

        public static bool IsValidLabelName(string labelname)
        {
            return !string.IsNullOrEmpty(labelname) && labelname.All(c => char.IsLetter(c) || c == '_');
        }
    }
}
