
namespace SomeProject.CommandTerminal
{
    using System.Collections.Generic;
    using SomeProject.Generated;

    /// <summary>
    /// Manages command name and argument parsing.
    /// The context is also responsible for variable substitution.
    /// </summary>
    public class CommandContext
    {
        public Scanner Scanner;
        
        /// <summary>
        /// The name of the currently parsed command.
        /// </summary>
        public string Command { get; set; }
        
        /// <summary>
        /// The scanned arguments, as strings.
        /// </summary>
        public readonly List<string> Arguments = new List<string>();
        
        /// <summary>
        /// The scanned options, as strings.
        /// </summary>
        public readonly CaseInsensitiveDictionary<string> Options = new CaseInsensitiveDictionary<string>();

        /// <summary>
        /// A mapping from variable names to values.
        /// All values are strings.
        /// </summary>
        public readonly CaseInsensitiveDictionary<string> Variables = new CaseInsensitiveDictionary<string>();

        
        /// <summary>
        /// The character that must appear before a string for it 
        /// to be parsed as a variable and substituted a value.
        /// Both $var and "$var" are substituted the value of var and the " are dropped.
        /// </summary>
        public const char VarSubChar = '$';


        /// <summary>
        /// A backreference to the terminal, required for meta commands.
        /// </summary>
        public readonly Terminal Terminal;

        public CommandLogger Logger => Terminal.Logger;
        public CommandShell Shell => Terminal.Shell;

        
        /// <summary>
        /// The types of messages emitted since the context had been last reset.
        /// </summary>
        private LogTypes _recordedMessageTypes = 0;
        
        /// <summary>
        /// Whether HasErrors should be true if a warning had been logged.
        /// </summary>
        public static bool TreatWarningsAsErrors 
        { 
            get => MessageTypesConsideredError.HasFlag(LogTypes.Warning);
            set => MessageTypesConsideredError.Set(LogTypes.Warning, value);
        }
        private static LogTypes MessageTypesConsideredError = LogTypes.Warning | LogTypes.Error;

        /// <summary>
        /// Indicates whether an error has been logged via the context since the last time
        /// it had been reset.
        /// </summary>
        public bool HasErrors => _recordedMessageTypes.HasEitherFlag(MessageTypesConsideredError);

        public CommandContext(Terminal terminal)
        {
            Terminal = terminal;
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

        /// <summary>
        /// If the input represents a variable, substitutes it with the value of the variable.
        /// Returns `false` only if the variable represented a variable, but such variable was not found.
        /// </summary>
        public bool MaybeSubstitute(ref string input)
        {
            if (!string.IsNullOrEmpty(input) && input[0] == VarSubChar)
            {
                var variableName = input.Substring(1);
                if (!Variables.TryGetValue(variableName, out input))
                {
                    LogError($"No variable named {variableName}");
                    return false;
                }
            }
            return true;
        }

        
        /// <summary>
        /// Enumerates variable names, with the variable prefix, that match the input.
        /// </summary>
        /// <remarks>
        /// If the input does not start with the variable prefix, nothing is returned.
        /// If the input is empty, all variable names are retuned.
        /// </remarks>
        public IEnumerable<string> GetMatchingWords(string partialWord)
        {
            // Return all if given an empty string
            if (partialWord.Length == 0)
            {
                foreach (var word in Variables.Keys) 
                    yield return VarSubChar + word;
            }
            else if (partialWord[0] == VarSubChar)
            {
                string remaining = partialWord.Substring(1);
                foreach (string word in Variables.Keys)
                {
                    if (word.StartsWith(remaining, System.StringComparison.OrdinalIgnoreCase))
                    {
                        yield return VarSubChar + word;
                    }
                }
            }
        }

        /// <summary>
        /// Scans the input for all arguments, substituting variables as expected.
        /// </summary>
        public void ScanArguments()
        {
            string argument;
            while (Scanner.TryGet(out argument))
            {
                if (!MaybeSubstitute(ref argument)) return;
                Arguments.Add(argument);
                Scanner.SkipWhitespace();
            }
        }

        /// <summary>
        /// Scans the input for all options.
        /// The input must currently start with options, which is to say
        /// that options come after all positional arguments.
        /// </summary>
        public void ScanOptions()
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


        /// <summary>
        /// Tries getting a positional argument as a string.
        /// </summary>
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
                LogError($"Missing {GetOrdinal(argumentIndex + 1)} argument '{argumentName}'");
                return default;
            }
            var argument = Arguments[argumentIndex];
            var summary = parser.Parse(argument, out T result);
            if (summary.IsError)
            {
                LogError($"Error while parsing {GetOrdinal(argumentIndex + 1)} argument '{argumentName}': " + summary.Message);
            }
            return result;
        }

        public T ParseOption<T>(string optionName, IValueParser<T> parser)
        {
            if (!Options.TryRemove(optionName, out string option))
            {
                LogError($"Missing required option '{optionName}'.");
                return default;
            }

            if (option == null)
            {
                LogError($"The option '{optionName}' cannot be used like a flag.");
                return default;
            }

            var summary = parser.Parse(option, out T result);
            if (summary.IsError)
            {
                LogError($"Error while parsing option '{optionName}': " + summary.Message);
            }
            return result;
        }

        public T ParseOption<T>(string optionName, T defaultValue, IValueParser<T> parser)
        {
            if (!Options.TryRemove(optionName, out string option))
            {
                return defaultValue;
            }

            if (option == null)
            {
                LogError($"The option '{optionName}' cannot be used like a flag.");
                return defaultValue;
            }

            var summary = parser.Parse(option, out T result);
            if (summary.IsError)
            {
                LogError($"Error while parsing option '{optionName}': " + summary.Message);
                return defaultValue;
            }
            return result;
        }

        public bool ParseFlag(string optionName, IValueParser<bool> parser, bool defaultValue = false, bool flagValue = true)
        {
            if (!Options.TryRemove(optionName, out string option))
            {
                return defaultValue;
            }

            if (option == null)
            {
                return flagValue;
            }
            
            var summary = parser.Parse(option, out bool result);
            if (summary.IsError)
            {
                LogError($"Error while parsing flag '{optionName}': " + summary.Message);
                return defaultValue;
            }
            return result;
        }

        public bool ParseFlag(string optionName, bool defaultValue = false, bool flagValue = true)
        {
            if (!Options.TryRemove(optionName, out string option))
            {
                return defaultValue;
            }

            if (option == null)
            {
                return flagValue;
            }
            
            var summary = Generated.Parsers.Parse(option, out bool result);
            if (summary.IsError)
            {
                LogError($"Error while parsing flag '{optionName}': " + summary.Message);
                return defaultValue;
            }
            return result;
        }

        /// <summary>
        /// Logs warnings abount unknown arguments.
        /// </summary>
        public void EndParsing()
        {
            foreach (var key in Options.Keys)
            {
                LogWarning($"Unknown argument: {key}.");
            }
        }
    }
}