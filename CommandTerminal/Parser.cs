
namespace CommandTerminal
{
    using System.Collections.Generic;

    public struct Scanner
    {
        private int _currentIndex;
        public readonly string Source;

        public Scanner(string source)
        {
            Source = source;
            _currentIndex = 0;
        }

        public bool IsEmpty => _currentIndex >= Source.Length;
        public char Current => Source[_currentIndex];

        public string GetRemaining()
        {
            return Source.Substring(_currentIndex);
        }
        
        public void SkipWhitespace()
        {
            while (!IsEmpty && char.IsWhiteSpace(Current))
            {
                _currentIndex++;
            }
        }

        public string GetCommandName()
        {
            if (IsEmpty) return null;

            int commandNameStart = _currentIndex;
            while (!IsEmpty && !char.IsWhiteSpace(Current))
            {
                _currentIndex++;
            }
            if (_currentIndex == commandNameStart)
            {
                _currentIndex = commandNameStart;
                return null;
            }
            return Source.Substring(commandNameStart, _currentIndex - commandNameStart);
        }

        public string GetName()
        {
            if (IsEmpty) return null;

            int nameStart = _currentIndex;
            while (!IsEmpty && !char.IsWhiteSpace(Current))
            {
                _currentIndex++;
            }
            return Source.Substring(nameStart, _currentIndex - nameStart);
        }

        /// Assume no escapes and double quotes
        public string GetString()
        {
            if (IsEmpty) return null;
            // starting with the option symbol is invalid
            if (Current == '-') return null;
            // "string"
            if (Current == '"')
            {
                int quoteIndex = _currentIndex;
                _currentIndex++;
                while (!IsEmpty && Current != '"')
                {
                    _currentIndex++;
                }
                // "string
                if (IsEmpty) 
                {
                    _currentIndex = quoteIndex;
                    return null;
                }
                _currentIndex++;
                int start  = quoteIndex + 1;
                int length = _currentIndex - start - 1;
                return Source.Substring(start, length);
            }
            return GetName();
        }

        public bool TryGet(out string str)
        {
            str = GetString();
            return str != null;
        }

        public bool TryGet(out Option option)
        {
            option = default;
            if (IsEmpty) return false;
            if (Current != '-') return false;

            int start = _currentIndex;
            // option (the identifier part)
            while (!IsEmpty && !char.IsWhiteSpace(Current) && Current != '=')
            {
                _currentIndex++;
            }

            var nameStart = start + 1;
            option.Name = Source.Substring(nameStart, _currentIndex - nameStart);

            SkipWhitespace();

            // if no =, parse as a flag 
            if (IsEmpty || Current != '=') 
            {
                return true;
            }

            // =
            _currentIndex++;
            int valueStart = _currentIndex;

            option.Value = GetString();
            if (option.Value is null)
            {
                _currentIndex = start;
                return false;
            }

            return true;
        }
    }

    public struct Option
    {
        public string Name;
        public string Value;

        public bool GetFlagValue(bool defaultValue = true) 
        {
            if (Value == null) return defaultValue;
            return bool.Parse(Value);
        }
    }

    public class CommandContext
    {
        public Scanner Scanner;
        public string Command { get; private set; }
        public readonly List<string> Arguments;
        public readonly CaseInsensitiveDictionary<string> Options;
        public readonly CaseInsensitiveDictionary<string> Variables;
        public readonly CommandLogger Logger;

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

        public CommandContext(CommandLogger logger)
        {
            Logger = logger;
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
            if (value != "" && value[0] == varSubChar)
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
        
        public T ParseArgument<T>(int argumentIndex, string argumentName, IValueParser<T> parser)
        {
            if (Arguments.Count >= argumentIndex)
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

    public interface IValueParser<T>
    {
        ParseSummary Parse(string input, out T result);
    }

    public readonly struct ParseSummary
    {
        public readonly string Message;
        public bool IsError => Message != null;
        public ParseSummary(string message) { Message = message; }
        public static readonly ParseSummary Success = new ParseSummary(null);
        public static ParseSummary TypeMismatch(string expectedTypeName, string actualInput) => new ParseSummary($"Expected input compatible with type {expectedTypeName}, got {actualInput}.");
    }

    
    public class BoolParser : IValueParser<bool>
    {
        public ParseSummary Parse(string input, out bool value) => Parsers.Parse(input, out value);
    }

    public static partial class Parsers
    {
        public static readonly BoolParser Bool = new BoolParser();

        public static ParseSummary Parse(this string input, out bool result)
        {
            if (string.Compare(input, "TRUE", ignoreCase: true) == 0) 
            {
                result = true;
                return ParseSummary.Success;
            }

            if (string.Compare(input, "FALSE", ignoreCase: true) == 0) 
            {
                result = false;
                return ParseSummary.Success;
            }

            result = default;
            return ParseSummary.TypeMismatch("bool", input);
        }

        // var value = context.ParseArgument(0, "MyArgument", Parsers.Bool);
        // var option = context.ParseOption("MyOption", Parsers.Bool);
        // var option = context.ParseOption("MyOption", true, Parsers.Bool);
        // var flag = context.ParseFlag("MyOption", defaultValue: false, flagValue: true, Parsers.Bool); 
        // var flag = context.ParseFlag("MyOption"); 
        // var flag = context.ParseFlag("MyOption", defaultValue: false); 
        // var flag = context.ParseFlag("MyOption", defaultValue: true); 
    }



    public abstract class CommandBase
    {
        public abstract void Execute(CommandContext context);
        public readonly int MinimumNumberOfArguments;
        public readonly int MaximumNumberOfArguments;
        public readonly string HelpMessage;

        protected CommandBase(int minimumNumberOfArguments, int maximumNumberOfArguments, string helpMessage)
        {
            this.MinimumNumberOfArguments = minimumNumberOfArguments;
            this.MaximumNumberOfArguments = maximumNumberOfArguments;
            this.HelpMessage = helpMessage;
        }
    }
}