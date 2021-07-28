using System.Diagnostics;
using CommandTerminal;
using Kari;

namespace SomeProject.CommandTerminalBasics
{
    public static class BuiltinCommands
    {
        [FrontCommand(Help = "Clear the command console", NumberOfArguments = 0)]
        public static void Clear(CommandContext context) 
        {
            context.Logger.Clear();
        }

        [FrontCommand(Help = "List all variables or set a variable value", 
            MinimumNumberOfArguments = 0, MaximumNumberOfArguments = 2)]
        public static void Set(CommandContext context) 
        {
            if (context.Arguments.Count == 0)
            {
                context.LogVariables();
                return;
            }

            var name = context.Arguments[0];
            
            if (context.Arguments.Count == 1)
            {
                if (context.Variables.TryGetValue(name, out var varValue))
                {
                    context.Log(context.Variables[name]);
                }
                else
                {
                    context.Log("");
                }
                return;
            }

            var value = context.Arguments[1];
            context.Variables[name] = value;
        }

        [FrontCommand(Help = "Logs help for a command",
            MinimumNumberOfArguments = 0, MaximumNumberOfArguments = 1)]
        public static void Help(CommandContext context)
        {
            if (context.Arguments.Count == 0)
            {
                context.Shell.LogCommands();
                return;
            }

            var commandName = context.Arguments[0];
            context.Shell.LogHelpForCommand(commandName);
        }

        [FrontCommand(Help = "Logs the time a command takes to execute", MinimumNumberOfArguments = 1)]
        public static void Time(CommandContext context)
        {
            var commandName = context.Arguments[0];
            context.Command = commandName;
            context.Arguments.RemoveAt(0);

            var s = new Stopwatch();
            s.Start();
            context.Shell.RunCurrentCommand();
            s.Stop();
            context.Log($"The command {commandName} took {s.ElapsedMilliseconds} ms.");
        }

        [FrontCommand(Help = "No operation", NumberOfArguments = 0)]
        public static void Noop(CommandContext context) 
        { 
        }

        [FrontCommand(Help = "Logs the only argument", NumberOfArguments = 1)]
        public static void Log(CommandContext context)
        {
            context.Log(context.Arguments[0]);
        }

        [Command(Name = "Test", Help = "Prints something")]
        public static void PrintSomething(int i, int b)
        {
            UnityEngine.Debug.Log(i);
            UnityEngine.Debug.Log(b);
        }

        [Command("Hello", "Some parameter")]
        public static string SomeCommand(
            [Argument("pos help")]                    int positional,
            [Argument("optional", "optional help")]   string optional,
            [Option("flag", "idk1", Parser = "Switch")]   bool flag,
            [Option("option", "idk2")]                string option = "44")
        {
            return $"{positional}; {optional}; {flag}; {option};";
        }

        [Parser("Switch")]
        public static ParseSummary ParseSwitch(string input, out bool output)
        {
            if (string.Equals(input, "ON", System.StringComparison.OrdinalIgnoreCase))
            {
                output = true;
                return ParseSummary.Success;
            }
            
            if (string.Equals(input, "OFF", System.StringComparison.OrdinalIgnoreCase))
            {
                output = false;
                return ParseSummary.Success;
            }

            output = false;
            return ParseSummary.TypeMismatch("switch(bool)", input);
        }
    }
}
