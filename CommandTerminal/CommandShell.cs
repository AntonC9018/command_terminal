using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace CommandTerminal
{
    public class CommandShell
    {
        public readonly CaseInsensitiveDictionary<CommandBase> Commands = new CaseInsensitiveDictionary<CommandBase>();
        public readonly CommandContext Context;
        public readonly CommandLogger Logger;

        private static readonly string ECHO = CaseInsensitiveDictionary<CommandBase>._GetKey("echo");
        private static readonly MetaCommand EchoCommand = new MetaCommand(0, -1, "Logs the arguments"); 
        private static readonly string HELP = CaseInsensitiveDictionary<string>._GetKey("help");

        public CommandShell(Terminal terminal)
        {
            Commands.Raw.Add(ECHO, EchoCommand);
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
            if (commandName == ECHO)
            {
                // Prints everything besides echo and the empty spaces
                Context.Log(Context.Scanner.GetRemaining());
                return;
            }
                
            if (!Commands.Raw.TryGetValue(commandName, out var command)) 
            {
                Logger.LogError($"Command `{commandName}` could not be found");
                return;
            }
            
            Context.ParseArguments();
            if (Context.HasErrors) return;

            Context.ParseOptions();
            if (Context.HasErrors) return;

            if (Context.Options.ContainsKey(HELP))
            {
                Context.Log(command.ExtendedHelpMessage);
                return;
            }

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

        private readonly EvenTableBuilder _commandTableBuilder = new EvenTableBuilder("Name", "Description");
        private string _commandTableBuilderResult = null;

        public void LogCommands()
        {
            _commandTableBuilderResult ??= _commandTableBuilder.ToString();
            Logger.Log(_commandTableBuilderResult);
        }

        public void LogHelpForCommand(string name)
        {
            var commandName = Context.Arguments[0];
            if (!Commands.TryGetValue(commandName, out var command)) 
            {
                Context.LogError($"Command `{commandName}` could not be found.");
                return;
            }

            Context.Log(command.HelpMessage);
        }

        public void RegisterCommands()
        {
            var builtin = CommandTerminal.Generated.Commands.BuiltinCommands;
            for (int i = 0; i < builtin.Length; i++)
            {
                RegisterCommand(builtin[i].Name, builtin[i].Command);
            }
        }

        public void RegisterCommand(string name, CommandBase command)
        {
            _commandTableBuilderResult = null;
            _commandTableBuilder.Append(column: 0, name);
            _commandTableBuilder.Append(column: 1, command.HelpMessage);
            Commands.Add(name, command);
        }

        public IEnumerable<string> GetMatchingWords(string partialWord)
        {
            // Context matches variables.
            var enumerable = Context.GetMatchingWords(partialWord);
            // If it's a variable, it cannot be a command.
            if (enumerable.Any())
            {
                foreach (string word in enumerable)
                {
                    yield return word;
                }
                yield break;
            }
            
            foreach (string word in Commands.Keys)
            {
                if (word.StartsWith(partialWord, StringComparison.OrdinalIgnoreCase))
                {
                    yield return word;
                }
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

    public class MetaCommand : CommandBase
    {
        public MetaCommand(int minimumNumberOfArguments, int maximumNumberOfArguments, string helpMessage) : base(minimumNumberOfArguments, maximumNumberOfArguments, helpMessage)
        {
        }

        public override void Execute(CommandContext context)
        {
            throw new NotImplementedException();
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
