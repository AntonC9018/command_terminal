using System.Diagnostics;
using CommandTerminal.Generated;
using Kari.Plugins.Terminal;
namespace CommandTerminal.Basics
{
    public static class BuiltinCommands
    {
        /// <summary>
        /// Clear the command console.
        /// </summary>
        [FrontCommand(NumberOfArguments = 0)]
        public static void Clear(CommandContext context) 
        {
            context.Logger.Clear();
        }

        /// <summary>
        /// List all variables or set a variable value.
        /// `set  x  5`  sets the variable x to value 5.
        /// The value can be read back by doing `set x` or `log x`.
        /// </summary>
        /// <remarks>
        /// Do not put a $ in front of the variable name.
        /// The prefixed variable name in this case will be substituted with the value of the variable
        /// and a new variable with that name will be set to the value. Simply put:
        /// `set x y` and then `set $x z` will set `x` to "y" and `y` to "z". 
        /// </remarks>
        [FrontCommand(MinimumNumberOfArguments = 0, MaximumNumberOfArguments = 2)]
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

        /// <summary>
        /// Logs help for a command.
        /// Internally calls to Shell.LogHelpForCommand().
        /// </summary>
        [FrontCommand(MinimumNumberOfArguments = 0, MaximumNumberOfArguments = 1)]
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
            var commandName = context.ParseArgument(0, "command");
            if (context.HasErrors) return;

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

        [FrontCommand(Help = "Logs the only argument, evaluating variables", NumberOfArguments = 1)]
        public static void Log(CommandContext context)
        {
            var argumentToLog = context.ParseArgument(0, "argumentToLog");
            if (!context.HasErrors)
                context.Log(argumentToLog);
        }

        [FrontCommand(Help = "Quit the game")]
        public static void Quit(CommandContext context)
        {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
        }

        /// <summary>
        /// A test, sandbox command made to illustrate the features of the system.
        /// Feel free to experiment with this command.
        /// </summary>
        [Command(Name = "Hello")]
        public static string SomeCommand(
            [Argument("pos help", Parser = "Switch")]                   bool positional,
            [Argument("optional", "optional help")]                     string optional,
            [Option("flag", "idk1", Parser = "Switch", IsFlag = true)]  bool flag,
            [Option("option", "idk2")]                                  string option = "44")
        {
            return $"{positional}; {optional}; {flag}; {option};";
        }

        // Self-reference is a nice technique here.
        // The code generator is able to infer that `nameof(Parsers.Switch)` evaluates to "Switch"
        // even if the corresponding symbol does not exist.
        // This really is extra: you don't really need that, since the code generator gives you errors 
        // if it finds a reference to a non-existent parser.
        [Parser(nameof(Parsers.Switch))]
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
            return ParseSummary.TypeMismatch("Switch (on/off)", input);
        }
    }
}
