using System.Collections;
using System.Collections.Generic;

namespace CommandTerminal
{
    [System.Flags]
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
        public readonly string StackTrace;

        public Message(string message, LogTypes type = LogTypes.Message, string stackTrace = "")
        {
            Type = type;
            String = message;
            StackTrace = stackTrace;
        }
        public static implicit operator Message(string message) => new Message(message);
    }

    public class CyclicBuffer<T> : IEnumerable<T> where T : struct
    {
        private readonly T[] _underlyingArray;
        private int _currentIndex = -1;
        private int _count;

        public bool IsFull => _count == _underlyingArray.Length;
        public int Count => _count;
        public int Capacity => _underlyingArray.Length;

        public CyclicBuffer(int capacity)
        {
            _underlyingArray = new T[capacity];
        }

        public void ThrowIfOutOfRange(int index)
        {
#if DEBUG
            if (!IsFull && index > _currentIndex)
            {
                throw new System.IndexOutOfRangeException($"Trying to index with {index} on {Count}-length buffer.");                    
            }
#endif
        }

        private int MapIndex(int index) => (index + _currentIndex + _count - 1) % Capacity;

        public T this[int index]
        {
            get 
            { 
                ThrowIfOutOfRange(index);
                return _underlyingArray[MapIndex(index)];
            }
            set 
            {  
                ThrowIfOutOfRange(index);          
                _underlyingArray[MapIndex(index)] = value;
            }
        }

        
        public void Clear()
        {
            _currentIndex = -1;
            _count = 0;
        }

        public void Add(T value)
        {
            _currentIndex = (_currentIndex + 1) % Capacity;
            _underlyingArray[_currentIndex] = value;
        }
        public T Last => _underlyingArray[_currentIndex];

        public IEnumerator<T> GetEnumerator()
        {
            int cached = _currentIndex - _count + 1;
            for (int i = 0; i < _count; i++)
            {
                yield return _underlyingArray[(i + cached) % Capacity]; 
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class CommandLogger : CyclicBuffer<Message>
    {
        public CommandLogger(int capacity) : base(capacity) 
        {
        }

        public void Log(string message)
        {
            Log(message, "", LogTypes.ShellMessage);
        }

        public void Log(string message, LogTypes type)
        {
            Log(message, "", type);
        }

        public void Log(string message, string stackTrace, LogTypes type) 
        {
            Add(new Message(message, type, stackTrace));
        }
        
        public void LogError(string message)
        {
            Add(new Message(message, LogTypes.Error));
        }
    }
}
