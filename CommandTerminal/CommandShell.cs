using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using CommandTerminal.Generated;

namespace CommandTerminal
{
    public class CommandShell
    {
        public readonly CaseInsensitiveDictionary<CommandBase> Commands = new CaseInsensitiveDictionary<CommandBase>();
        public readonly CommandContext Context;
        public readonly CommandLogger Logger;

        private static readonly string HELP = CaseInsensitiveDictionary<string>._GetKey("help");
        private static readonly string ECHO = CaseInsensitiveDictionary<CommandBase>._GetKey("echo");

        /// <summary>
        /// The echo command must never be called directly, it is intercepted and treated differently.
        /// However, the info about it should be available to autocompletion and things like that.
        /// TODO: 
        /// this is mostly an artifact of the fact that the arguments are not in fact treated as tokens,
        /// but as plain strings. Because of this, information about their position in the source string is lost.
        /// </summary>
        private static readonly InterceptableCommand EchoCommand = new InterceptableCommand(0, -1, "Logs the arguments"); 

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

            // The echo command needs info on the input string just after the command,
            // before it is actually scanned or parsed. 
            if (commandName == ECHO)
            {
                // Prints everything besides echo and the empty spaces
                Context.Log(Context.Scanner.GetRemaining());
                return;
            }
                
            if (!Commands.Raw.TryGetValue(commandName, out var command)) 
            {
                Logger.LogError($"Command `{commandName}` could not be found.");
                return;
            }
            
            Context.ScanArguments();
            if (Context.HasErrors) return;

            Context.ScanOptions();
            if (Context.HasErrors) return;

            // The help on any command means we print the help message instead of running the command.
            // This behavior cannot be controlled from the commands themselves.
            if (Context.ParseFlag(HELP))
            {
                Context.Log(command.ExtendedHelpMessage);
                return;
            }

            if (Context.HasErrors) return;

            RunCommand(command);
        }

        private void RunCommand(CommandBase command) 
        {

            // If the command expects no arguments, show the help message.
            if (command.MinimumNumberOfArguments > 0 
                && Context.Arguments.Count == 0 && Context.Options.Count == 0)
            {
                Context.Log(command.ExtendedHelpMessage);
                return;
            }


            if (command.MaximumNumberOfArguments != -1)
            {
                for (int i = command.MaximumNumberOfArguments; i < Context.Arguments.Count; i++)
                {
                    var extraArgument = Context.Arguments[i];
                    Context.LogWarning($"Extra argument '{extraArgument}' at position {i}.");
                }
            }

            try
            {
                command.Execute(Context);
            }
            catch (Exception exception)
            {
                // TODO: 
                // We lose info on stack trace here. 
                // Ideally, it should be logged in a special way, with a tree view or something.
                Logger.LogError(exception.Message);
                Logger.LogError(exception.StackTrace);
            }
        }

        private readonly EvenTableBuilder _commandTableBuilder = new EvenTableBuilder("Name", "Description");
        private string _commandTableBuilderResult = null;

        public void LogCommands()
        {
            _commandTableBuilderResult ??= _commandTableBuilder.ToString();
            Logger.Log(_commandTableBuilderResult);
        }

        public void LogHelpForCommand(string commandName)
        {
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
            // Mark the builder dirty.
            _commandTableBuilderResult = null;
            _commandTableBuilder.Append(column: 0, name);
            _commandTableBuilder.Append(column: 1, command.HelpMessage);
            Commands.Add(name, command);
        }

        // TODO: clean up     
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

                if (partialWord != "")
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
    
    /// <summary>
    /// Such commands should always be intercepted by the shell 
    /// and so are assumed to never be called directly.
    /// </summary>
    public class InterceptableCommand : CommandBase
    {
        public InterceptableCommand(int minimumNumberOfArguments, int maximumNumberOfArguments, string helpMessage) : base(minimumNumberOfArguments, maximumNumberOfArguments, helpMessage)
        {
        }

        public override void Execute(CommandContext context)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// This class can be used to define runtime commands via a lambda.
    /// </summary>
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
