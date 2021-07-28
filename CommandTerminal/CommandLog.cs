namespace SomeProject.CommandTerminal
{
    [Kari.NiceFlags]
    public enum LogTypes
    {
        Error     = 1 << 0,
        Assert    = 1 << 1,
        Warning   = 1 << 2,
        Message   = 1 << 3,
        Exception = 1 << 4,
        Input     = 1 << 5,
        ShellMessage = 1 << 6
    }

    public readonly struct Message
    {
        public readonly LogTypes Type;
        public readonly string String;

        public Message(string message, LogTypes type = LogTypes.Message)
        {
            Type = type;
            String = message;
        }
        public static implicit operator Message(string message) => new Message(message);
    }

    public class CommandLogger : CyclicBuffer<Message>
    {
        public CommandLogger(int capacity) : base(capacity) 
        {
        }

        public void Log(string message, LogTypes type = LogTypes.ShellMessage)
        {
            Add(new Message(message, type));
        }

        public void LogError(string message)
        {
            Add(new Message(message, LogTypes.Error));
        }
    }
}
