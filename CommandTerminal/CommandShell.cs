using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CommandTerminal
{
    public class CaseInsensitiveDictionary<T> : Dictionary<string, T>
    {
        public string GetKey(string variableName) => variableName.ToUpper();
        public new T this[string name]
        {
            get => base[GetKey(name)];
            set => base[GetKey(name)] = value;
        }
        public new bool TryGetValue(string name, out T value) => base.TryGetValue(GetKey(name), out value);
        public new bool Remove(string name) => base.Remove(GetKey(name));
        public new void Add(string name, T value) => base.Add(GetKey(name), value);
        public new bool ContainsKey(string name) => base.ContainsKey(GetKey(name));
        public Dictionary<string, T> Raw => this;
    }


    public class CommandShell
    {
        public readonly CaseInsensitiveDictionary<CommandBase> Commands = new CaseInsensitiveDictionary<CommandBase>();
        public readonly CommandContext Context;
        public readonly CommandLogger Logger;

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

            var commandName = Commands.GetKey(Context.Command);

            // The builtin commands
            switch (commandName)
            {
                case "ECHO":
                {
                    // Actually makes a copy of the scanner
                    var scanner = Context.Scanner;
                    var value = scanner.GetString();
                    
                    // Here we check for `echo $var`
                    if (value != null && scanner.IsEmpty && !Context.MaybeSubstitute(ref value))
                    {
                        Context.Log(value);
                    }
                    // Just print the remaining input
                    else
                    {
                        // Prints everything besides echo and the empty spaces
                        Context.Log(Context.Scanner.GetRemaining());
                    }
                    return;
                }
                case "HELP":
                {
                    LogHelpForNextCommand();
                    return;
                }
                case "NOOP":
                {
                    return;
                }
                case "CLEAR":
                {
                    Context.Logger.Clear();
                    return;
                }
            }
                
            if (!Commands.Raw.TryGetValue(commandName, out var command)) 
            {
                Logger.LogError($"Command {command} could not be found");
                return;
            }
            
            Context.ParseArguments();
            if (Context.HasErrors) return;

            Context.ParseOptions();
            if (Context.HasErrors) return;

            if (Context.Options.ContainsKey("HELP"))
            {
                Context.Log(command.HelpMessage);
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
                Context.Log(command.HelpMessage);
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

            command.Execute(Context);
        }

        private void LogHelpForNextCommand()
        {
            if (Context.Arguments.Count == 0) 
            {
                foreach (var command in Commands) 
                {
                    Context.Log($"{command.Key.PadRight(16)}: {command.Value.HelpMessage}");
                }
                return;
            }

            LogHelpForCommand(Context.Arguments[0]);
        }

        private void LogHelpForCommand(string name)
        {
            var commandName = Context.Arguments[0];
            if (!Commands.TryGetValue(commandName, out var command)) 
            {
                Context.Log($"Command {commandName} could not be found.");
                return;
            }

            Context.Log(command.HelpMessage);
        }
    }

        

    // Meta commands cannot be called directly
    // They are handled in a special way and the shell is aware of them
    public class MetaCommand : CommandBase
    {
        public MetaCommand(int minimumNumberOfArguments, int maximumNumberOfArguments, string helpMessage) 
            : base(minimumNumberOfArguments, maximumNumberOfArguments, helpMessage)
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
