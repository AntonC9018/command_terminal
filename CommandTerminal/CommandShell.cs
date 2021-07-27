using System;
using System.Diagnostics;

namespace CommandTerminal
{
    public class CommandShell
    {
        public readonly CaseInsensitiveDictionary<CommandBase> Commands = new CaseInsensitiveDictionary<CommandBase>();
        public readonly CommandContext Context;
        public readonly CommandLogger Logger;

        public CommandShell(Terminal terminal)
        {
            Context = new CommandContext(terminal);
            Logger = terminal.Logger;
        }

        /// <summary>
        /// Parses an input line into a command and runs that command.
        /// </summary>
        public void TryRunCommand(string line) 
        {
            Context.Reset();
            Context.SetInput(line);

            if (!Context.TryParseCommand())
            {
                return;
            }

            RunCurrentCommand();
        }

        public void RunCurrentCommand()
        {
            var commandName = Commands.GetKey(Context.Command);

            // The builtin commands
            if (commandName == "ECHO")
            {
                // Prints everything besides echo and the empty spaces
                Context.Log(Context.Scanner.GetRemaining());
                return;
            }
                
            if (!Commands.Raw.TryGetValue(commandName, out var command)) 
            {
                Logger.LogError($"Command {commandName} could not be found");
                return;
            }
            
            Context.ParseArguments();
            if (Context.HasErrors) return;

            Context.ParseOptions();
            if (Context.HasErrors) return;

            if (Context.Options.ContainsKey("HELP"))
            {
                Context.Log(command.ExtendedHelpMessage);
                return;
            }

            Context.EndParsing();
            if (Context.HasErrors) return;

            RunCommand(command);
        }

        private void RunCommand(CommandBase command) 
        {
            int numArguments = Context.Arguments.Count;
            string errorMessage = null;
            int numRequiredArguments = 0;

            if (command.MinimumNumberOfArguments > 0 && numArguments == 0)
            {
                Context.Log(command.ExtendedHelpMessage);
                return;
            }

            if (numArguments < command.MinimumNumberOfArguments) 
            {
                if (command.MinimumNumberOfArguments == command.MaximumNumberOfArguments) 
                {
                    errorMessage = "exactly";
                } 
                else 
                {
                    errorMessage = "at least";
                }
                numRequiredArguments = command.MinimumNumberOfArguments;
            } 
            else if (command.MaximumNumberOfArguments > -1 && numArguments > command.MaximumNumberOfArguments) 
            {
                // Do not check max allowed number of arguments if it is -1
                if (command.MinimumNumberOfArguments == command.MaximumNumberOfArguments) 
                {
                    errorMessage = "exactly";
                } 
                else 
                {
                    errorMessage = "at most";
                }
                numRequiredArguments = command.MaximumNumberOfArguments;
            }

            if (errorMessage != null) 
            {
                string pluralFix = numRequiredArguments == 1 ? "" : "s";
                Logger.LogError($"{Context.Command} requires {errorMessage} {numRequiredArguments} argument{pluralFix}");
                return;
            }
            try
            {
                command.Execute(Context);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception.Message);
            }
        }

        public void LogCommands()
        {
            var builder = new EvenTableBuilder("Name", "Description"); 
            foreach (var kv in Commands) 
            {
                builder.Append(column: 0, kv.Key);
                builder.Append(column: 1, kv.Value.HelpMessage);
            }
            Logger.Log(builder.ToString());
        }

        public void LogHelpForCommand(string name)
        {
            var commandName = Context.Arguments[0];
            if (!Commands.TryGetValue(commandName, out var command)) 
            {
                Context.Log($"Command {commandName} could not be found.");
                return;
            }

            Context.Log(command.HelpMessage);
        }

        public void RegisterCommands()
        {
            var builtin = CommandTerminal.Generated.Commands.BuiltinCommands;
            for (int i = 0; i < builtin.Length; i++)
            {
                Commands.Add(builtin[i].Name, builtin[i].Command);
            }
        }
    }

    public abstract class CommandBase
    {
        public abstract void Execute(CommandContext context);
        public readonly int MinimumNumberOfArguments;
        public readonly int MaximumNumberOfArguments;
        public readonly string HelpMessage;
        public readonly string ExtendedHelpMessage;

        protected CommandBase(int minimumNumberOfArguments, int maximumNumberOfArguments, string helpMessage, string extendedHelpMessage)
        {
            MinimumNumberOfArguments = minimumNumberOfArguments;
            MaximumNumberOfArguments = maximumNumberOfArguments;
            HelpMessage = helpMessage;
            ExtendedHelpMessage = extendedHelpMessage;
        }

        protected CommandBase(int minimumNumberOfArguments, int maximumNumberOfArguments, string helpMessage)
        {
            MinimumNumberOfArguments = minimumNumberOfArguments;
            MaximumNumberOfArguments = maximumNumberOfArguments;
            HelpMessage = helpMessage;
            ExtendedHelpMessage = helpMessage;
        }
    }

    public class GenericCommand : CommandBase
    {
        public readonly Action<CommandContext> _proc;
        public override void Execute(CommandContext context) => _proc(context);

        public GenericCommand(int minimumNumberOfArguments, int maximumNumberOfArguments, string helpMessage, Action<CommandContext> proc) 
            : base(minimumNumberOfArguments, maximumNumberOfArguments, helpMessage)
        {
            _proc = proc;
        }
    }
}
