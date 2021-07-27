
namespace CommandTerminal
{
    using System.Collections.Generic;

    public class CommandContext
    {
        public Scanner Scanner;
        public string Command { get; set; }
        public readonly List<string> Arguments;
        public readonly CaseInsensitiveDictionary<string> Options;
        public readonly CaseInsensitiveDictionary<string> Variables;
        public readonly Terminal Terminal;
        public CommandLogger Logger => Terminal.Logger;
        public CommandShell Shell => Terminal.Shell;

        private LogTypes _recordedMessageTypes = 0;
        // TODO: implement this when I add the previous flag helper functions
        // public static bool TreatWarningsAsErrors { set => MessageTypesConsideredError |= 
        private static LogTypes MessageTypesConsideredError = LogTypes.Warning | LogTypes.Error;
        public bool HasErrors => (_recordedMessageTypes & MessageTypesConsideredError) != 0;
        // { get { 
        //         for (int i = 0; i < Messages.Count; i++)
        //         {
        //             if (Messages[i].MessageType == MessageType.Error) return true;
        //         }
        //         return false;
        // } }

        public CommandContext(Terminal terminal)
        {
            Terminal = terminal;
            Variables = new CaseInsensitiveDictionary<string>();
            Options = new CaseInsensitiveDictionary<string>();
            Arguments = new List<string>();
        }

        public void Reset()
        {
            _recordedMessageTypes = 0;
            Arguments.Clear();
            Options.Clear();
        }

        public void SetInput(string rawInput)
        {
            Scanner = new Scanner(rawInput);
            Scanner.SkipWhitespace();
        }

        public bool TryParseCommand()
        {
            var command = Scanner.GetCommandName();
            if (command == null) return false;
            Scanner.SkipWhitespace();
            bool result = MaybeSubstitute(ref command);
            Command = command;
            return result;
        }

        const char varSubChar = '$';
        public bool MaybeSubstitute(ref string value)
        {
            if (!string.IsNullOrEmpty(value) && value[0] == varSubChar)
            {
                var variableName = value.Substring(1);
                if (!Variables.TryGetValue(variableName, out value))
                {
                    LogError($"No variable named {variableName}");
                    return false;
                }
            }
            return true;
        }

        public void ParseArguments()
        {
            string argument;
            while (Scanner.TryGet(out argument))
            {
                if (!MaybeSubstitute(ref argument)) return;
                Arguments.Add(argument);
                Scanner.SkipWhitespace();
            }
        }

        // Only allow options after all positional arguments
        public void ParseOptions()
        {
            Option option;
            while (Scanner.TryGet(out option))
            {
                if (!MaybeSubstitute(ref option.Value)) return;
                Options.Add(option.Name, option.Value);
                Scanner.SkipWhitespace();
            }
        }

        
        public void Log(Message message)
        {
            _recordedMessageTypes |= message.Type;
            Logger.Add(message);
        }

        public void LogError(string message)
        {
            Log(new Message(message, LogTypes.Error));
        }

        public void LogWarning(string message)
        {
            Log(new Message(message, LogTypes.Warning));
        }

        public void LogVariables()
        {
            var builder = new EvenTableBuilder("Name", "Value"); 
            foreach (var kv in Variables) 
            {
                builder.Append(column: 0, kv.Key);
                builder.Append(column: 1, kv.Key);
            }
            Log(builder.ToString());
        }

        // 0th, 1st, 2nd, 3rd, 4th, 5th ...
        private static string GetOrdinalSuffix(int number)
        {
            switch (number)
            {
                case 1:  return "st";
                case 2:  return "nd";
                case 3:  return "rd";
                default: return "th";
            }
        }

        private static string GetOrdinal(int number)
        {
            return $"{number.ToString()}{GetOrdinalSuffix(number)}";
        }

        public string ParseArgument(int argumentIndex, string argumentName)
        {
            if (Arguments.Count <= argumentIndex)
            {
                LogError($"Missing {GetOrdinal(argumentIndex)} argument '{argumentName}'");
                return null;
            }
            return Arguments[argumentIndex];
        }
        
        public T ParseArgument<T>(int argumentIndex, string argumentName, IValueParser<T> parser)
        {
            if (Arguments.Count <= argumentIndex)
            {
                LogError($"Missing {GetOrdinal(argumentIndex)} argument '{argumentName}'");
                return default;
            }
            var argument = Arguments[argumentIndex];
            var summary = parser.Parse(argument, out T result);
            if (summary.IsError)
            {
                LogError($"Error while parsing {GetOrdinal(argumentIndex)} argument '{argumentName}': " + summary.Message);
            }
            return result;
        }

        public T ParseOption<T>(string optionName, IValueParser<T> parser)
        {
            if (!Options.TryGetValue(optionName, out string option))
            {
                LogError($"Missing required option '{optionName}'.");
                return default;
            }
            Options.Remove(optionName);

            var summary = parser.Parse(option, out T result);
            if (summary.IsError)
            {
                LogError($"Error while parsing option '{optionName}': " + summary.Message);
            }
            return result;
        }
        
        public T ParseOption<T>(string optionName, T defaultValue, IValueParser<T> parser)
        {
            if (!Options.TryGetValue(optionName, out string option))
            {
                return defaultValue;
            }
            Options.Remove(optionName);

            var summary = parser.Parse(option, out T result);
            if (summary.IsError)
            {
                LogError($"Error while parsing option '{optionName}': " + summary.Message);
                return defaultValue;
            }
            return result;
        }

        public bool ParseFlag(string optionName, bool defaultValue = false, bool flagValue = true)
        {
            if (!Options.TryGetValue(optionName, out string option))
            {
                return defaultValue;
            }
            Options.Remove(optionName);

            if (option == null)
            {
                return flagValue;
            }
            
            var summary = Parsers.Parse(option, out bool result);
            if (summary.IsError)
            {
                LogError($"Error while parsing flag '{optionName}': " + summary.Message);
                return defaultValue;
            }
            return result;
        }

        public void EndParsing()
        {
            foreach (var key in Options.Keys)
            {
                LogWarning($"Unknown argument: {key}.");
            }
        }
    }
}