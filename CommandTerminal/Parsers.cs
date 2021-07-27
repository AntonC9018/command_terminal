
namespace CommandTerminal
{
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
}