using System.Collections;
using System.Collections.Generic;

namespace CommandTerminal
{
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

        private int MapIndex(int index) => (index + _currentIndex - _count + 1) % Capacity;

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
            if (_count < Capacity) _count++;
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
}
