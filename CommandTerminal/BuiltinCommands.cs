using System.Text;
using System.Diagnostics;
using UnityEngine;

namespace CommandTerminal
{
    public static class BuiltinCommands
    {
        [Command(Help = "Clear the command console")]
        public static void Clear(CommandContext context) 
        {
            context.Logger.Clear();
        }

#if DEBUG
        [RegisterCommand(Help = "Output the stack trace of the previous message", MaxArgCount = 0)]
        static void Trace(CommandArg[] args) 
        {
            int log_count = Terminal.Logger.Logs.Count;

            if (log_count - 2 < 0) 
            {
                Terminal.Log("Nothing to trace.");
                return;
            }

            var log_item = Terminal.Logger.Logs[log_count - 2];

            if (log_item.StackTrace == "") 
            {
                Terminal.Log("{0} (no trace)", log_item.Message);
            } else {
                Terminal.Log(log_item.StackTrace);
            }
        }
#endif

        [RegisterCommand(Help = "List all variables or set a variable value")]
        static void Set(CommandArg[] args) 
        {
            if (args.Length == 0) 
            {
                foreach (var kv in Terminal.Shell.Variables) 
                {
                    Terminal.Log("{0}: {1}", kv.Key.PadRight(16), kv.Value);
                }
                return;
            }

            string variable_name = args[0].String;

            if (variable_name[0] == '$') 
            {
                Terminal.Log(LogTypes.Warning, "Warning: Variable name starts with '$', '${0}'.", variable_name);
            }

            Terminal.Shell.SetVariable(variable_name, JoinArguments(args, 1));
        }

        [RegisterCommand(Help = "No operation")]
        static void Noop(CommandArg[] args) 
        { 
        }

        [RegisterCommand(Help = "Quit running application", MaxArgCount = 0)]
        static void Quit(CommandArg[] args) 
        {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
        }

        static string JoinArguments(CommandArg[] args, int start = 0) 
        {
            var sb = new StringBuilder();
            int arg_length = args.Length;

            for (int i = start; i < arg_length; i++) {
                sb.Append(args[i].String);

                if (i < arg_length - 1) {
                    sb.Append(" ");
                }
            }

            return sb.ToString();
        }
    }
}
